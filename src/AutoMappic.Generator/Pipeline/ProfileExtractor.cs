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

        // Find all CreateMap calls in this class.
        var invocations = classDecl.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Where(inv => IsCreateMapCall(inv, context.SemanticModel, cancellationToken));

        foreach (var inv in invocations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var isInConstructor = inv.Ancestors().Any(a => a is ConstructorDeclarationSyntax);
            if (!isInConstructor)
            {
                var methodSymbol = context.SemanticModel.GetSymbolInfo(inv, cancellationToken).Symbol as IMethodSymbol;
                var sName = methodSymbol?.TypeArguments.ElementAtOrDefault(0)?.Name ?? "TSource";
                var dName = methodSymbol?.TypeArguments.ElementAtOrDefault(1)?.Name ?? "TDestination";

                results.Add((null!, new[]
                {
                    Diagnostic.Create(
                        AutoMappicDiagnostics.CreateMapOutsideProfile,
                        inv.GetLocation(),
                        sName, dName)
                }));
                continue;
            }

            var models = TryExtractModels(
                inv,
                classSymbol,
                context.SemanticModel,
                cancellationToken);

            results.AddRange(models);
        }

        return results;
    }

    // ─── Private helpers ──────────────────────────────────────────────────────────

    internal static bool InheritsFromProfile(INamedTypeSymbol classSymbol)
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

        if (inv.Expression is IdentifierNameSyntax id && id.Identifier.Text == CreateMapMethodName)
            return true;

        if (inv.Expression is MemberAccessExpressionSyntax ma)
        {
            if (ma.Name is GenericNameSyntax mgn && mgn.Identifier.Text == CreateMapMethodName)
                return true;

            if (ma.Name is IdentifierNameSyntax mid && mid.Identifier.Text == CreateMapMethodName)
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
        if (methodSymbol is null) return results;

        ITypeSymbol? sourceType = null;
        ITypeSymbol? destType = null;

        if (methodSymbol.IsGenericMethod && methodSymbol.TypeArguments.Length >= 2)
        {
            sourceType = methodSymbol.TypeArguments[0];
            destType = methodSymbol.TypeArguments[1];
        }
        else if (createMapCall.ArgumentList.Arguments.Count >= 2)
        {
            sourceType = semanticModel.GetTypeInfo((createMapCall.ArgumentList.Arguments[0].Expression as TypeOfExpressionSyntax)?.Type ?? createMapCall.ArgumentList.Arguments[0].Expression, ct).Type;
            destType = semanticModel.GetTypeInfo((createMapCall.ArgumentList.Arguments[1].Expression as TypeOfExpressionSyntax)?.Type ?? createMapCall.ArgumentList.Arguments[1].Expression, ct).Type;
        }

        if (sourceType is null || destType is null) return results;

        // Collect ForMember / ForMemberIgnore chained on this CreateMap call.
        var explicitMaps = new Dictionary<string, string?>(System.StringComparer.Ordinal);
        var ignoredMembers = new HashSet<string>(System.StringComparer.Ordinal);
        string? typeConverterFullName = null;
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
            else if (methodName == "ConvertUsing")
            {
                if (memberAccess.Name is GenericNameSyntax gn && gn.TypeArgumentList.Arguments.Count == 1)
                {
                    typeConverterFullName = semanticModel.GetTypeInfo(gn.TypeArgumentList.Arguments[0], ct).Type?.ToDisplayString();
                }
            }

            current = invocation.Parent;
        }

        var diagnostics = new List<Diagnostic>();
        var profileLocation = profileClass.Locations.Length > 0 ? profileClass.Locations[0] : Location.None;

        var (properties, constructorArgs) = ConventionEngine.Resolve(
            sourceType,
            destType,
            explicitMaps,
            ignoredMembers,
            profileLocation,
            d => diagnostics.Add(d));

        var callLocation = createMapCall.GetLocation();
        var lineSpan = callLocation.GetLineSpan();

        var model = new MappingModel(
            SourceTypeFullName: GetDisplayString(sourceType),
            SourceTypeName: sourceType.Name,
            DestinationTypeFullName: GetDisplayString(destType),
            DestinationTypeName: destType.Name,
            Properties: new EquatableArray<PropertyMap>(properties),
            ConstructorArguments: new EquatableArray<PropertyMap>(constructorArgs),
            TypeConverterFullName: typeConverterFullName,
            FilePath: lineSpan.Path,
            Line: lineSpan.StartLinePosition.Line + 1,
            Column: lineSpan.StartLinePosition.Character + 1);

        results.Add((model, diagnostics));

        if (hasReverseMap)
        {
            var revDiags = new List<Diagnostic>();
            var (revProps, revConstructorArgs) = ConventionEngine.Resolve(
                destType,
                sourceType,
                new Dictionary<string, string?>(),
                new HashSet<string>(),
                profileLocation,
                d => revDiags.Add(d));

            var revModel = new MappingModel(
                SourceTypeFullName: GetDisplayString(destType),
                SourceTypeName: destType.Name,
                DestinationTypeFullName: GetDisplayString(sourceType),
                DestinationTypeName: sourceType.Name,
                Properties: new EquatableArray<PropertyMap>(revProps),
                ConstructorArguments: new EquatableArray<PropertyMap>(revConstructorArgs),
                FilePath: lineSpan.Path,
                Line: lineSpan.StartLinePosition.Line + 1,
                Column: lineSpan.StartLinePosition.Character + 1);

            results.Add((revModel, revDiags));
        }

        return results;
    }

    /// <summary>
    ///   Extracts the member name from a <c>dest =&gt; dest.PropertyName</c> selector.
    /// </summary>
    private static string? ExtractMemberName(ExpressionSyntax selector)
    {
        ExpressionSyntax? body = null;
        if (selector is SimpleLambdaExpressionSyntax lambda) body = (ExpressionSyntax)lambda.Body;
        else if (selector is ParenthesizedLambdaExpressionSyntax pLambda) body = (ExpressionSyntax)pLambda.Body;

        if (body is MemberAccessExpressionSyntax ma)
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
        // opt => opt.MapFrom(...)
        ExpressionSyntax? outerBody = null;
        if (optExpression is SimpleLambdaExpressionSyntax oLambda) outerBody = (ExpressionSyntax)oLambda.Body;
        else if (optExpression is ParenthesizedLambdaExpressionSyntax poLambda) outerBody = (ExpressionSyntax)poLambda.Body;

        if (outerBody is not InvocationExpressionSyntax invocation) return null;

        var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
        var methodName = memberAccess?.Name.Identifier.Text;
        if (methodName != MapFromMethodName) return null;

        // Case 1: lambda-based MapFrom(src => ...)
        var innerGenericLambda = invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression;
        if (innerGenericLambda is SimpleLambdaExpressionSyntax sLambda || innerGenericLambda is ParenthesizedLambdaExpressionSyntax pLambda)
        {
            string param;
            string body;

            if (innerGenericLambda is SimpleLambdaExpressionSyntax sl)
            {
                param = sl.Parameter.Identifier.Text;
                body = sl.Body.ToString();
            }
            else
            {
                var pl = (ParenthesizedLambdaExpressionSyntax)innerGenericLambda;
                param = pl.ParameterList.Parameters.FirstOrDefault()?.Identifier.Text ?? "src";
                body = pl.Body.ToString();
            }

            // Rewrite the lambda parameter to "source" so it aligns with the generated method.
            // Using a boundaries check \b ensures we don't accidentally replace sub-strings.
            return System.Text.RegularExpressions.Regex.Replace(
                body,
                $@"\b{System.Text.RegularExpressions.Regex.Escape(param)}\b",
                "source");
        }

        // Case 2: Resolver-based MapFrom<TResolver>()
        if (memberAccess?.Name is GenericNameSyntax gn && gn.TypeArgumentList.Arguments.Count == 1)
        {
            var resolverType = gn.TypeArgumentList.Arguments[0].ToString();
            return $"new {resolverType}().Resolve(source)";
        }

        return null;
    }

    private static string GetDisplayString(ITypeSymbol type) =>
        type.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString();
}
