using System.Collections.Generic;
using System.Linq;
using AutoMappic.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoMappic.Generator.Pipeline;

/// <summary>
///   Syntax-level and semantic extraction helpers for the mapping profile pipeline.
/// </summary>
internal static class ProfileExtractor
{
    private const string ProfileBaseTypeName = "Profile";
    private const string CreateMapMethodName = "CreateMap";
    private const string ForMemberMethodName = "ForMember";
    private const string ForMemberIgnoreMethodName = "ForMemberIgnore";
    private const string MapFromMethodName = "MapFrom";
    private const string IgnoreMethodName = "Ignore";

    /// <summary>
    ///   Fast syntax predicate for the incremental pipeline's <c>CreateSyntaxProvider</c>.
    ///   Returns <see langword="true" /> for any class declaration — we refine with the
    ///   semantic model in <see cref="ExtractMappingModels" />.
    /// </summary>
    public static bool IsProfileClassCandidate(SyntaxNode node, System.Threading.CancellationToken _) =>
        node is ClassDeclarationSyntax cls && cls.BaseList is not null;

    /// <summary>
    ///   Semantic transform: given a <see cref="GeneratorSyntaxContext" /> whose
    ///   <c>Node</c> is a class declaration, extracts a <see cref="MappingModel" /> for
    ///   each <c>CreateMap</c> call found in the constructor, or returns
    ///   <see langword="null" /> if this is not a Profile subclass.
    /// </summary>
    public static IReadOnlyList<(MappingModel Model, IReadOnlyList<Diagnostic> Diagnostics)>
        ExtractMappingModels(
            GeneratorSyntaxContext context,
            System.Threading.CancellationToken cancellationToken)
    {
        var classDecl = (ClassDeclarationSyntax)context.Node;
        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classDecl, cancellationToken) as INamedTypeSymbol;
        if (classSymbol is null) return System.Array.Empty<(MappingModel, IReadOnlyList<Diagnostic>)>();

        if (!InheritsFromProfile(classSymbol)) return System.Array.Empty<(MappingModel, IReadOnlyList<Diagnostic>)>();

        var results = new List<(MappingModel, IReadOnlyList<Diagnostic>)>();

        // Only look inside constructors.
        var ctors = classDecl.Members.OfType<ConstructorDeclarationSyntax>();
        foreach (var ctor in ctors)
        {
            var createMapCalls = ctor.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Where(inv => IsCreateMapCall(inv, context.SemanticModel, cancellationToken));

            foreach (var createMapCall in createMapCalls)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var models = TryExtractModels(
                    createMapCall,
                    classSymbol,
                    context.SemanticModel,
                    cancellationToken);

                results.AddRange(models);
            }
        }

        return results;
    }

    // ─── Private helpers ──────────────────────────────────────────────────────────

    private static bool InheritsFromProfile(INamedTypeSymbol classSymbol)
    {
        var baseType = classSymbol.BaseType;
        while (baseType is not null)
        {
            if (baseType.Name == ProfileBaseTypeName &&
                baseType.ContainingNamespace.ToDisplayString() == "AutoMappic")
            {
                return true;
            }
            baseType = baseType.BaseType;
        }
        return false;
    }

    private static bool IsCreateMapCall(
        InvocationExpressionSyntax inv,
        SemanticModel model,
        System.Threading.CancellationToken ct)
    {
        if (inv.Expression is GenericNameSyntax gn && gn.Identifier.Text == CreateMapMethodName)
            return true;

        if (inv.Expression is MemberAccessExpressionSyntax ma)
        {
            if (ma.Name is GenericNameSyntax mgn && mgn.Identifier.Text == CreateMapMethodName)
                return true;

            if (ma.Name.Identifier.Text == "ReverseMap")
                return true;
        }

        return false;
    }

    private static List<(MappingModel Model, IReadOnlyList<Diagnostic> Diagnostics)>
        TryExtractModels(
            InvocationExpressionSyntax createMapCall,
            INamedTypeSymbol profileClass,
            SemanticModel semanticModel,
            System.Threading.CancellationToken ct)
    {
        var results = new List<(MappingModel, IReadOnlyList<Diagnostic>)>();
        var methodSymbol = semanticModel.GetSymbolInfo(createMapCall, ct).Symbol as IMethodSymbol;
        if (methodSymbol is null || methodSymbol.TypeArguments.Length < 2) return results;

        var sourceType = methodSymbol.TypeArguments[0] as INamedTypeSymbol;
        var destType = methodSymbol.TypeArguments[1] as INamedTypeSymbol;
        if (sourceType is null || destType is null) return results;

        // Collect ForMember / ForMemberIgnore chained on this CreateMap call.
        var explicitMaps = new Dictionary<string, string?>(System.StringComparer.Ordinal);
        var ignoredMembers = new HashSet<string>(System.StringComparer.Ordinal);
        bool hasReverseMap = false;

        // Walk up the chain to collect settings and also see if ReverseMap is present.
        SyntaxNode? current = createMapCall.Parent;
        while (current is MemberAccessExpressionSyntax memberAccess)
        {
            var invocation = memberAccess.Parent as InvocationExpressionSyntax;
            if (invocation is null) break;

            var methodName = memberAccess.Name.Identifier.Text;
            if (methodName == "ReverseMap")
            {
                hasReverseMap = true;
            }
            // For now, settings apply to the FORWARD map.
            else if (methodName == ForMemberMethodName)
            {
                var args = invocation.ArgumentList.Arguments;
                if (args.Count >= 1)
                {
                    var destName = ExtractMemberName(args[0].Expression);
                    if (destName is not null && args.Count >= 2)
                    {
                        var sourceExpr = ExtractMapFromBody(args[1].Expression);
                        explicitMaps[destName] = sourceExpr;
                    }
                }
            }
            else if (methodName == ForMemberIgnoreMethodName)
            {
                var args = invocation.ArgumentList.Arguments;
                if (args.Count >= 1)
                {
                    var destName = ExtractMemberName(args[0].Expression);
                    if (destName is not null) ignoredMembers.Add(destName);
                }
            }

            current = invocation.Parent;
        }

        var diagnostics = new List<Diagnostic>();
        var profileLocation = profileClass.Locations.Length > 0 ? profileClass.Locations[0] : Location.None;

        var properties = ConventionEngine.Resolve(
            sourceType,
            destType,
            explicitMaps,
            ignoredMembers,
            profileLocation,
            d => diagnostics.Add(d));

        var model = new MappingModel(
            SourceTypeFullName: sourceType.ToDisplayString(),
            SourceTypeName: sourceType.Name,
            DestinationTypeFullName: destType.ToDisplayString(),
            DestinationTypeName: destType.Name,
            Properties: new EquatableArray<PropertyMap>(properties));

        results.Add((model, diagnostics));

        if (hasReverseMap)
        {
            var revDiags = new List<Diagnostic>();
            var revProps = ConventionEngine.Resolve(
                destType,
                sourceType,
                new Dictionary<string, string?>(),
                new HashSet<string>(),
                profileLocation,
                d => revDiags.Add(d));

            var revModel = new MappingModel(
                SourceTypeFullName: destType.ToDisplayString(),
                SourceTypeName: destType.Name,
                DestinationTypeFullName: sourceType.ToDisplayString(),
                DestinationTypeName: sourceType.Name,
                Properties: new EquatableArray<PropertyMap>(revProps));

            results.Add((revModel, revDiags));
        }

        return results;
    }

    /// <summary>
    ///   Extracts the member name from a <c>dest =&gt; dest.PropertyName</c> selector.
    /// </summary>
    private static string? ExtractMemberName(ExpressionSyntax selector)
    {
        if (selector is SimpleLambdaExpressionSyntax lambda &&
            lambda.Body is MemberAccessExpressionSyntax ma)
        {
            return ma.Name.Identifier.Text;
        }
        return null;
    }

    /// <summary>
    ///   Extracts the raw C# text of the body of a <c>MapFrom(src =&gt; …)</c> lambda.
    ///   The parameter name is rewritten to <c>source</c> to match the generated method.
    /// </summary>
    private static string? ExtractMapFromBody(ExpressionSyntax optExpression)
    {
        // opt => opt.MapFrom(src => ...)
        if (optExpression is not SimpleLambdaExpressionSyntax outerLambda) return null;

        var invocation = outerLambda.Body as InvocationExpressionSyntax;
        if (invocation is null) return null;

        var methodName = (invocation.Expression as MemberAccessExpressionSyntax)?.Name.Identifier.Text;
        if (methodName != MapFromMethodName) return null;

        var innerLambda = invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression
            as SimpleLambdaExpressionSyntax;
        if (innerLambda is null) return null;

        var param = innerLambda.Parameter.Identifier.Text;
        var body = innerLambda.Body.ToString();

        // Rewrite the lambda parameter to "source" so it aligns with the generated method.
        // We use a regex with word boundaries to ensure we only replace the parameter name
        // when used as an identifier (e.g., "s.Prop" or "(double)s.Prop").
        return System.Text.RegularExpressions.Regex.Replace(
            body,
            $@"\b{System.Text.RegularExpressions.Regex.Escape(param)}(?=\.)",
            "source");
    }
}
