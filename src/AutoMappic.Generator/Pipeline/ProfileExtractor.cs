using System;
using System.Collections.Generic;
using System.Linq;
using AutoMappic.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
    ///   High-level extraction for CLI/Test use: Extracts all mapping models from a full compilation.
    /// </summary>
    public static IReadOnlyList<MappingModel> ExtractFromCompilation(Compilation compilation)
    {
        var results = new List<MappingModel>();
        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var cls in classes)
            {
                var symbol = model.GetDeclaredSymbol(cls) as INamedTypeSymbol;
                if (symbol == null || !InheritsFromProfile(symbol)) continue;

                var isProfilingEnabled = IsPerformanceProfilingEnabled(cls);
                var (sourceN, destN) = ExtractProfileNamingConventions(cls);

                var invocations = cls.DescendantNodes().OfType<InvocationExpressionSyntax>()
                    .Where(inv => IsCreateMapCall(inv, model, default));

                foreach (var inv in invocations)
                {
                    var extracted = TryExtractModels(inv, symbol, model, default, isProfilingEnabled, sourceN, destN);
                    foreach (var (m, _) in extracted)
                    {
                        if (m != null) results.Add(m);
                    }
                }
            }
        }
        return results;
    }

    /// <summary>
    ///   Fast syntax predicate for the incremental pipeline's <c>CreateSyntaxProvider</c>.
    ///   Returns <see langword="true" /> for any class declaration -- we refine with the
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

        var isProfilingEnabled = IsPerformanceProfilingEnabled(classDecl);
        var (sourceN, destN) = ExtractProfileNamingConventions(classDecl);

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
                cancellationToken,
                isProfilingEnabled,
                sourceN,
                destN);

            results.AddRange(models);
        }

        return results;
    }

    // --- Private helpers ----------------------------------------------------------

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

            if (ma.Name.Identifier.Text == CreateMapMethodName)
                return true;
        }

        return false;
    }

    private static List<(MappingModel Model, IReadOnlyList<Diagnostic> Diagnostics)>
        TryExtractModels(
            InvocationExpressionSyntax createMapCall,
            INamedTypeSymbol profileClass,
            SemanticModel semanticModel,
            System.Threading.CancellationToken ct,
            bool profileProfilingEnabled = false,
            string? profileSourceNaming = null,
            string? profileDestNaming = null)
    {
        var results = new List<(MappingModel, IReadOnlyList<Diagnostic>)>();
        var methodSymbol = semanticModel.GetSymbolInfo(createMapCall, ct).Symbol as IMethodSymbol;
        if (methodSymbol is null)
        {
            results.Add((null!, new[]
            {
                Diagnostic.Create(
                    AutoMappicDiagnostics.UnresolvedCreateMapSymbol,
                    createMapCall.GetLocation())
            }));
            return results;
        }

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

        if (sourceType is INamedTypeSymbol nSource && nSource.IsUnboundGenericType)
            sourceType = nSource.OriginalDefinition;
        if (destType is INamedTypeSymbol nDest && nDest.IsUnboundGenericType)
            destType = nDest.OriginalDefinition;

        if (sourceType is null || destType is null)
        {
            results.Add((null!, new[]
            {
                Diagnostic.Create(
                    AutoMappicDiagnostics.UnresolvedCreateMapSymbol,
                    createMapCall.GetLocation())
            }));
            return results;
        }

        // Collect settings for both forward and reverse directions.
        var forwardMaps = new Dictionary<string, (string? Expression, string? Condition, bool IsAsync)>(System.StringComparer.Ordinal);
        var forwardIgnored = new HashSet<string>(System.StringComparer.Ordinal);
        var reverseMaps = new Dictionary<string, (string? Expression, string? Condition, bool IsAsync)>(System.StringComparer.Ordinal);
        var reverseIgnored = new HashSet<string>(System.StringComparer.Ordinal);

        string? typeConverterFullName = null;
        string? beforeMapBody = null;
        string? afterMapBody = null;
        string? beforeMapAsyncBody = null;
        string? afterMapAsyncBody = null;
        string? constructionBody = null;
        bool isConvertUsingUsed = false;
        bool hasReverseMap = false;
        bool currentlyConfiguringReverse = false;
        string? sourceNaming = profileSourceNaming;
        string? destNaming = profileDestNaming;

        // Walk up the chain to collect settings.
        // Orders: CreateMap().ForMember(FWD).ReverseMap().ForMember(REV)
        SyntaxNode? current = createMapCall.Parent;
        while (current is MemberAccessExpressionSyntax memberAccess)
        {
            var invocation = memberAccess.Parent as InvocationExpressionSyntax;
            if (invocation is null) break;

            var methodName = memberAccess.Name.Identifier.Text;
            if (methodName == "ReverseMap")
            {
                hasReverseMap = true;
                currentlyConfiguringReverse = true;
            }
            else if (methodName == ForMemberMethodName)
            {
                var args = invocation.ArgumentList.Arguments;
                if (args.Count >= 1)
                {
                    var destName = ExtractMemberName(args[0].Expression);
                    if (destName is not null && args.Count >= 2)
                    {
                        var (sourceExpr, condition, isAsync, isIgnored) = ExtractMapFromBody(args[1].Expression, semanticModel);
                        if (isIgnored)
                        {
                            if (currentlyConfiguringReverse) reverseIgnored.Add(destName);
                            else forwardIgnored.Add(destName);
                        }
                        else
                        {
                            if (currentlyConfiguringReverse) reverseMaps[destName] = (sourceExpr, condition, isAsync);
                            else forwardMaps[destName] = (sourceExpr, condition, isAsync);
                        }
                    }
                }
            }
            else if (methodName == ForMemberIgnoreMethodName)
            {
                var args = invocation.ArgumentList.Arguments;
                if (args.Count >= 1)
                {
                    var destName = ExtractMemberName(args[0].Expression);
                    if (destName is not null)
                    {
                        if (currentlyConfiguringReverse) reverseIgnored.Add(destName);
                        else forwardIgnored.Add(destName);
                    }
                }
            }
            else if (methodName == "BeforeMap")
            {
                var (body, _) = ExtractActionBody(invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression);
                if (currentlyConfiguringReverse) { /* reverse not supported yet for these */ }
                else beforeMapBody = body;
            }
            else if (methodName == "AfterMap")
            {
                var (body, _) = ExtractActionBody(invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression);
                if (currentlyConfiguringReverse) { /* reverse not supported yet for these */ }
                else afterMapBody = body;
            }
            else if (methodName == "BeforeMapAsync")
            {
                var (body, isEx) = ExtractActionBody(invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression);
                if (currentlyConfiguringReverse) { /* reverse not supported yet for these */ }
                else beforeMapAsyncBody = isEx ? $"await ({body}).ConfigureAwait(false);" : body;
            }
            else if (methodName == "AfterMapAsync")
            {
                var (body, isEx) = ExtractActionBody(invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression);
                if (currentlyConfiguringReverse) { /* reverse not supported yet for these */ }
                else afterMapAsyncBody = isEx ? $"await ({body}).ConfigureAwait(false);" : body;
            }
            else if (methodName == "ConvertUsing")
            {
                if (memberAccess.Name is GenericNameSyntax gn && gn.TypeArgumentList.Arguments.Count == 1)
                {
                    typeConverterFullName = semanticModel.GetTypeInfo(gn.TypeArgumentList.Arguments[0], ct).Type?.ToDisplayString();
                }
                else if (invocation.ArgumentList.Arguments.Count == 1)
                {
                    var argExpr = invocation.ArgumentList.Arguments[0].Expression;
                    if (argExpr is TypeOfExpressionSyntax typeOfExpr)
                    {
                        typeConverterFullName = semanticModel.GetTypeInfo(typeOfExpr.Type, ct).Type?.ToDisplayString();
                    }
                    else
                    {
                        // Assume it's a lambda converter
                        var (body, isExpr) = ExtractActionBody(argExpr);
                        if (currentlyConfiguringReverse) { /* reverse not supported yet */ }
                        else
                        {
                            constructionBody = isExpr ? body?.TrimEnd(';') : body;
                            isConvertUsingUsed = true;
                        }
                    }
                }
                if (!currentlyConfiguringReverse) isConvertUsingUsed = true;
            }
            else if (methodName == "ConstructUsing")
            {
                var (body, isExpr) = ExtractActionBody(invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression);
                if (currentlyConfiguringReverse) { /* reverse not supported yet for these */ }
                else constructionBody = isExpr ? body?.TrimEnd(';') : body;
            }
            else if (methodName == "WithNamingConvention")
            {
                var args = invocation.ArgumentList.Arguments;
                if (args.Count >= 2)
                {
                    sourceNaming = semanticModel.GetTypeInfo(args[0].Expression, ct).Type?.ToDisplayString();
                    destNaming = semanticModel.GetTypeInfo(args[1].Expression, ct).Type?.ToDisplayString();
                }
            }

            current = invocation.Parent;
        }

        var diagnostics = new List<Diagnostic>();
        var profileLocation = profileClass.Locations.Length > 0 ? profileClass.Locations[0] : Location.None;

        IReadOnlyList<PropertyMap> properties = new List<PropertyMap>();
        IReadOnlyList<PropertyMap> constructorArgs = new List<PropertyMap>();

        // If we have a whole-type converter (Type or Lambda), skip convention engine property resolution.
        bool hasWholeTypeConverter = !string.IsNullOrEmpty(typeConverterFullName) || isConvertUsingUsed;

        if (!hasWholeTypeConverter)
        {
            (properties, constructorArgs) = ConventionEngine.Resolve(
                sourceType!,
                destType!,
                forwardMaps,
                forwardIgnored,
                profileLocation,
                d => diagnostics.Add(d),
                null,
                sourceNaming,
                destNaming);
        }

        var callLocation = createMapCall.GetLocation();
        var lineSpan = callLocation.GetLineSpan();

        var typeParams = new List<string>();
        if (sourceType is INamedTypeSymbol namedSource && (namedSource.IsDefinition || namedSource.IsUnboundGenericType))
        {
            typeParams.AddRange(namedSource.TypeParameters.Select(tp => tp.Name));
        }
        else if (destType is INamedTypeSymbol namedDest && (namedDest.IsDefinition || namedDest.IsUnboundGenericType))
        {
            typeParams.AddRange(namedDest.TypeParameters.Select(tp => tp.Name));
        }
        var eqTypeParams = typeParams.Count > 0 ? new EquatableArray<string>(typeParams) : null;

        var model = new MappingModel(
            SourceTypeFullName: SourceEmitter.GetDisplayString(sourceType!),
            SourceTypeName: sourceType!.Name,
            DestinationTypeFullName: SourceEmitter.GetDisplayString(destType!),
            DestinationTypeName: destType!.Name,
            Properties: new EquatableArray<PropertyMap>(properties),
            ConstructorArguments: new EquatableArray<PropertyMap>(constructorArgs),
            SourceNamespace: sourceType?.ContainingNamespace?.IsGlobalNamespace == false ? sourceType.ContainingNamespace.ToDisplayString() : null,
            DestinationNamespace: destType?.ContainingNamespace?.IsGlobalNamespace == false ? destType.ContainingNamespace.ToDisplayString() : null,
            TypeConverterFullName: typeConverterFullName,
            FilePath: lineSpan.Path,
            Line: lineSpan.StartLinePosition.Line + 1,
            Column: lineSpan.StartLinePosition.Character + 1,
            BeforeMapBody: beforeMapBody,
            AfterMapBody: afterMapBody,
            BeforeMapAsyncBody: beforeMapAsyncBody,
            AfterMapAsyncBody: afterMapAsyncBody,
            ConstructionBody: constructionBody,
            SourceNamingStrategyFullName: sourceNaming,
            DestinationNamingStrategyFullName: destNaming,
            EnablePerformanceProfiling: profileProfilingEnabled,
            ProfileName: profileClass.Name,
            IsSourceValueType: sourceType!.IsValueType || sourceType.IsTupleType,
            IsDestinationValueType: destType!.IsValueType || destType.IsTupleType,
            TypeParameters: eqTypeParams);

        results.Add((model, diagnostics));

        if (hasReverseMap)
        {
            // The base convention resolving logic (ConventionEngine.Resolve) handles
            // asymmetrical property mapping if not overridden.
            var revDiags = new List<Diagnostic>();
            var (revProps, revConstructorArgs) = ConventionEngine.Resolve(
                destType!,
                sourceType!,
                reverseMaps,
                reverseIgnored,
                profileLocation,
                d => revDiags.Add(d),
                null,
                destNaming, // reversed
                sourceNaming);

            var revModel = new MappingModel(
                SourceTypeFullName: SourceEmitter.GetDisplayString(destType!),
                SourceTypeName: destType!.Name,
                DestinationTypeFullName: SourceEmitter.GetDisplayString(sourceType!),
                DestinationTypeName: sourceType!.Name,
                Properties: new EquatableArray<PropertyMap>(revProps),
                ConstructorArguments: new EquatableArray<PropertyMap>(revConstructorArgs),
                SourceNamespace: destType?.ContainingNamespace?.IsGlobalNamespace == false ? destType.ContainingNamespace.ToDisplayString() : null,
                DestinationNamespace: sourceType?.ContainingNamespace?.IsGlobalNamespace == false ? sourceType.ContainingNamespace.ToDisplayString() : null,
                FilePath: lineSpan.Path,
                Line: lineSpan.StartLinePosition.Line + 1,
                Column: lineSpan.StartLinePosition.Character + 1,
                SourceNamingStrategyFullName: destNaming,
                DestinationNamingStrategyFullName: sourceNaming,
                EnablePerformanceProfiling: profileProfilingEnabled,
                ProfileName: profileClass.Name,
                IsSourceValueType: destType!.IsValueType || destType.IsTupleType,
                IsDestinationValueType: sourceType!.IsValueType || sourceType.IsTupleType,
                TypeParameters: eqTypeParams);

            results.Add((revModel, revDiags));
        }

        return results;
    }
    private static bool IsPerformanceProfilingEnabled(ClassDeclarationSyntax classDecl)
    {
        return classDecl.DescendantNodes()
            .OfType<AssignmentExpressionSyntax>()
            .Any(assign =>
            {
                var name = GetMemberName(assign.Left);
                return name == "EnablePerformanceProfiling" &&
                       assign.Right is LiteralExpressionSyntax lit &&
                       lit.Token.IsKind(SyntaxKind.TrueKeyword);
            });
    }

    private static (string? Source, string? Dest) ExtractProfileNamingConventions(ClassDeclarationSyntax classDecl)
    {
        string? src = null;
        string? dest = null;

        var assignments = classDecl.DescendantNodes().OfType<AssignmentExpressionSyntax>();
        foreach (var assign in assignments)
        {
            var name = GetMemberName(assign.Left);
            if (name == "SourceNamingConvention" || name == "DestinationNamingConvention")
            {
                string? conventionName = null;
                if (assign.Right is ObjectCreationExpressionSyntax create)
                {
                    conventionName = GetTypeName(create.Type);
                }

                if (name == "SourceNamingConvention") src = conventionName;
                else dest = conventionName;
            }
        }
        return (src, dest);
    }

    private static string? GetMemberName(ExpressionSyntax expr)
    {
        if (expr is IdentifierNameSyntax id) return id.Identifier.Text;
        if (expr is MemberAccessExpressionSyntax ma) return ma.Name.Identifier.Text;
        return null;
    }

    private static string? GetTypeName(TypeSyntax type)
    {
        if (type is IdentifierNameSyntax id) return id.Identifier.Text;
        if (type is QualifiedNameSyntax qn) return qn.Right.Identifier.Text;
        if (type is SimpleNameSyntax sn) return sn.Identifier.Text;
        return null;
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
    ///   Extracts configurations like MapFrom and Condition from the ForMember options lambda.
    /// </summary>
    private static (string? Expression, string? Condition, bool IsAsync, bool IsIgnored) ExtractMapFromBody(ExpressionSyntax optExpression, SemanticModel semanticModel)
    {
        // opt => opt.MapFrom(...).Condition(...)
        ExpressionSyntax? outerBody = null;
        if (optExpression is SimpleLambdaExpressionSyntax oLambda) outerBody = (ExpressionSyntax)oLambda.Body;
        else if (optExpression is ParenthesizedLambdaExpressionSyntax poLambda) outerBody = (ExpressionSyntax)poLambda.Body;

        if (outerBody is null) return default;

        string? expression = null;
        string? condition = null;
        bool isAsync = false;
        bool isIgnored = false;

        var current = outerBody;
        while (current is InvocationExpressionSyntax invocation)
        {
            var memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
            var methodName = memberAccess?.Name.Identifier.Text;

            if (methodName == "MapFrom" || methodName == "MapFromAsync")
            {
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

                    expression = System.Text.RegularExpressions.Regex.Replace(
                        body,
                        $@"\b{System.Text.RegularExpressions.Regex.Escape(param)}\b",
                        "source");
                }
                else if (memberAccess?.Name is GenericNameSyntax gn && gn.TypeArgumentList.Arguments.Count == 1)
                {
                    var resolverTypeSymbol = semanticModel.GetTypeInfo(gn.TypeArgumentList.Arguments[0]).Type;
                    var resolverType = resolverTypeSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? gn.TypeArgumentList.Arguments[0].ToString();
                    if (methodName == "MapFromAsync")
                    {
                        expression = $"await new {resolverType}().ResolveAsync(source).ConfigureAwait(false)";
                        isAsync = true;
                    }
                    else
                    {
                        expression = $"global::AutoMappic.Generated.MapperInterceptors.Cache<{resolverType}>.Instance.Resolve(source)";
                    }
                }
            }
            else if (methodName == "ConvertUsing")
            {
                if (memberAccess?.Name is GenericNameSyntax gn && gn.TypeArgumentList.Arguments.Count == 2)
                {
                    var converterTypeSymbol = semanticModel.GetTypeInfo(gn.TypeArgumentList.Arguments[0]).Type;
                    var converterType = converterTypeSymbol?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? gn.TypeArgumentList.Arguments[0].ToString();
                    var sourceExprLambda = invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression;

                    if (sourceExprLambda is SimpleLambdaExpressionSyntax sl || sourceExprLambda is ParenthesizedLambdaExpressionSyntax pl)
                    {
                        string param;
                        string body;

                        if (sourceExprLambda is SimpleLambdaExpressionSyntax sll)
                        {
                            param = sll.Parameter.Identifier.Text;
                            body = sll.Body.ToString();
                        }
                        else
                        {
                            var pll = (ParenthesizedLambdaExpressionSyntax)sourceExprLambda;
                            param = pll.ParameterList.Parameters.FirstOrDefault()?.Identifier.Text ?? "src";
                            body = pll.Body.ToString();
                        }

                        var sourceMember = System.Text.RegularExpressions.Regex.Replace(
                            body,
                            $@"\b{System.Text.RegularExpressions.Regex.Escape(param)}\b",
                            "source");

                        expression = $"global::AutoMappic.Generated.MapperInterceptors.Cache<{converterType}>.Instance.Convert({sourceMember})";
                    }
                }
            }
            else if (methodName == "Condition")
            {
                var conditionLambda = invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression;
                if (conditionLambda is SimpleLambdaExpressionSyntax sLambda || conditionLambda is ParenthesizedLambdaExpressionSyntax pLambda)
                {
                    string srcParam;
                    string destParam;
                    string body;

                    if (conditionLambda is SimpleLambdaExpressionSyntax sl)
                    {
                        srcParam = sl.Parameter.Identifier.Text;
                        destParam = "dest";
                        body = sl.Body.ToString();
                    }
                    else
                    {
                        var pl = (ParenthesizedLambdaExpressionSyntax)conditionLambda;
                        srcParam = pl.ParameterList.Parameters.ElementAtOrDefault(0)?.Identifier.Text ?? "src";
                        destParam = pl.ParameterList.Parameters.ElementAtOrDefault(1)?.Identifier.Text ?? "dest";
                        body = pl.Body.ToString();
                    }

                    condition = System.Text.RegularExpressions.Regex.Replace(
                        body,
                        $@"\b{System.Text.RegularExpressions.Regex.Escape(srcParam)}\b",
                        "source");
                    condition = System.Text.RegularExpressions.Regex.Replace(
                        condition,
                        $@"\b{System.Text.RegularExpressions.Regex.Escape(destParam)}\b",
                        "result");
                }
            }

            else if (methodName == "Ignore")
            {
                isIgnored = true;
            }

            current = memberAccess?.Expression;
        }

        return (expression, condition, isAsync, isIgnored);
    }


    private static (string? Body, bool IsExpression) ExtractActionBody(ExpressionSyntax? lambdaExpression)
    {
        if (lambdaExpression is null) return (null, false);
        bool isExpression = false;

        ParameterSyntax? srcParam = null;
        ParameterSyntax? destParam = null;
        SyntaxNode? body = null;

        if (lambdaExpression is ParenthesizedLambdaExpressionSyntax pLambda)
        {
            srcParam = pLambda.ParameterList.Parameters.ElementAtOrDefault(0);
            destParam = pLambda.ParameterList.Parameters.ElementAtOrDefault(1);
            body = (SyntaxNode)pLambda.Body;
            isExpression = pLambda.ExpressionBody != null;
        }
        else if (lambdaExpression is SimpleLambdaExpressionSyntax sLambda)
        {
            srcParam = sLambda.Parameter;
            body = (SyntaxNode)sLambda.Body;
            isExpression = sLambda.ExpressionBody != null;
        }

        if (body is null) return (null, false);

        var srcName = srcParam?.Identifier.Text ?? "src";
        var destName = destParam?.Identifier.Text ?? "dest";

        string bodyText;
        if (body is BlockSyntax block)
        {
            // Join lines manually to avoid losing structure
            bodyText = string.Join("\n", block.Statements.Select(s => s.ToString()));
        }
        else
        {
            bodyText = body.ToString();
            if (!bodyText.TrimEnd().EndsWith(";", StringComparison.Ordinal)) bodyText += ";";
        }

        var result = System.Text.RegularExpressions.Regex.Replace(
            bodyText,
            $@"\b{System.Text.RegularExpressions.Regex.Escape(srcName)}\b",
            "source");

        result = System.Text.RegularExpressions.Regex.Replace(
            result,
            $@"\b{System.Text.RegularExpressions.Regex.Escape(destName)}\b",
            "result");

        return (result, isExpression);
    }
}
