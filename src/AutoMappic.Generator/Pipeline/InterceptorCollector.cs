using System.Linq;
using AutoMappic.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoMappic.Generator.Pipeline;

/// <summary> Finds all call sites and attempts to intercept them if they match AutoMappic patterns. </summary>
internal static class InterceptorCollector
{
    public static bool IsInvocationCandidate(SyntaxNode node, System.Threading.CancellationToken _) => node is InvocationExpressionSyntax;

    public static InterceptLocation? ExtractInterceptLocation(
        GeneratorSyntaxContext context,
        System.Threading.CancellationToken cancellationToken)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        SymbolInfo info = context.SemanticModel.GetSymbolInfo(invocation, cancellationToken);
        IMethodSymbol? symbol = info.Symbol as IMethodSymbol ?? info.CandidateSymbols.FirstOrDefault() as IMethodSymbol;

        if (symbol is null)
        {
            return null;
        }

        // Skip internal dispatch calls in MapperExtensions to avoid AM0004 errors on open generics
        ISymbol? caller = context.SemanticModel.GetEnclosingSymbol(invocation.SpanStart, cancellationToken);
        if (caller?.ContainingType?.Name == "MapperExtensions" && caller.ContainingNamespace?.ToDisplayString() == "AutoMappic")
        {
            return null;
        }

        string name = symbol.Name;
        bool isProjectTo = name == "ProjectTo";
        bool isMap = name is "Map" or "MapAsync" or "MapTo" or "MapToAsync";

        if (!isProjectTo && !isMap)
        {
            return null;
        }

        INamedTypeSymbol containingType = symbol.ContainingType;
        if (containingType == null)
        {
            return null;
        }

        string fullContainingType = containingType.ToDisplayString();
        InterceptKind kind;

        if (isProjectTo)
        {
            kind = InterceptKind.ProjectTo;
        }
        else if (fullContainingType.IndexOf("DataReaderExtensions", System.StringComparison.Ordinal) >= 0)
        {
            kind = name == "MapAsync" ? InterceptKind.DataReaderMapAsync : InterceptKind.DataReaderMap;
        }
        else if (isMap)
        {
            kind = InterceptKind.Map;
        }
        else
        {
            return null;
        }

        // Verify it belongs to AutoMappic directly or implements IMapper
        bool isAutoMappic = fullContainingType.StartsWith("AutoMappic", System.StringComparison.Ordinal) ||
                          containingType.AllInterfaces.Any(i => i.Name == "IMapper" && (i.ContainingNamespace?.ToDisplayString() ?? "").StartsWith("AutoMappic", System.StringComparison.Ordinal));

        if (!isAutoMappic)
        {
            return null;
        }

        // Coordination types (S and D) must be concrete call-site types
        ITypeSymbol? callSiteDest = symbol.TypeArguments.Length > 0 ? symbol.TypeArguments[symbol.TypeArguments.Length >= 2 ? 1 : 0] : null;
        ITypeSymbol? callSiteSource = null;

        if (kind == InterceptKind.Map)
        {
            if (symbol.TypeArguments.Length >= 2)
            {
                callSiteSource = symbol.TypeArguments[0];
            }
            else if (symbol.ReducedFrom != null && invocation.Expression is MemberAccessExpressionSyntax ma)
            {
                callSiteSource = context.SemanticModel.GetTypeInfo(ma.Expression, cancellationToken).Type;
            }
            else if (invocation.ArgumentList.Arguments.Count > 0)
            {
                callSiteSource = context.SemanticModel.GetTypeInfo(invocation.ArgumentList.Arguments[0].Expression, cancellationToken).Type;
            }
        }
        else if (kind == InterceptKind.ProjectTo)
        {
            // Try explicit TSource first (2-arg version)
            callSiteSource = symbol.TypeArguments.Length >= 2 ? symbol.TypeArguments[0] : null;

            // Try the ACTUAL expression type of the receiver (most accurate for extension methods)
            if (callSiteSource == null)
            {
                ExpressionSyntax? receiverExpr = invocation.Expression is MemberAccessExpressionSyntax ma ? ma.Expression : null;
                if (receiverExpr != null)
                {
                    ITypeSymbol? receiverType = context.SemanticModel.GetTypeInfo(receiverExpr, cancellationToken).Type;
                    if (receiverType is INamedTypeSymbol nr && nr.IsGenericType && nr.TypeArguments.Length > 0)
                    {
                        callSiteSource = nr.TypeArguments[0];
                    }
                    else if (receiverType != null)
                    {
                        // Check interfaces (e.g. List<T> -> IQueryable<T>)
                        INamedTypeSymbol? iqt = receiverType.AllInterfaces.FirstOrDefault(i => i.Name == "IQueryable" && i.IsGenericType && i.TypeArguments.Length > 0);
                        if (iqt != null)
                        {
                            callSiteSource = iqt.TypeArguments[0];
                        }
                    }
                }
            }

            // Fallback: look at the symbol's receiver type
            if (callSiteSource == null)
            {
                ITypeSymbol? receiver = symbol.ReceiverType;
                if (receiver is INamedTypeSymbol nr && nr.IsGenericType && nr.TypeArguments.Length > 0)
                {
                    callSiteSource = nr.TypeArguments[0];
                }
                else if (receiver != null)
                {
                    INamedTypeSymbol? iQueryableT = receiver.AllInterfaces.FirstOrDefault(i => i.Name == "IQueryable" && i.IsGenericType && i.TypeArguments.Length > 0);
                    if (iQueryableT != null)
                    {
                        callSiteSource = iQueryableT.TypeArguments[0];
                    }
                }
            }
        }
        else if (kind is InterceptKind.DataReaderMap or InterceptKind.DataReaderMapAsync)
        {
            string metaName = kind == InterceptKind.DataReaderMap ? "System.Data.IDataReader" : "System.Data.Common.DbDataReader";
            callSiteSource = context.SemanticModel.Compilation.GetTypeByMetadataName(metaName);
        }

        if (callSiteDest is null || callSiteSource is null)
        {
            return null;
        }

        // ─── Collection mapping detection ───────────────────────────────
        // When Map<List<A>, List<B>>() is called, callSiteSource=List<A>
        // and callSiteDest=List<B>. We need to unwrap the element types
        // so the shim can route to the per-element MapToX extension.
        bool isCollectionMapping = false;
        ITypeSymbol effectiveSource = callSiteSource;
        ITypeSymbol effectiveDest = callSiteDest;

        if (kind == InterceptKind.Map)
        {
            ITypeSymbol? srcElement = TryGetCollectionElementType(callSiteSource);
            ITypeSymbol? dstElement = TryGetCollectionElementType(callSiteDest);
            if (srcElement != null && dstElement != null)
            {
                isCollectionMapping = true;
                effectiveSource = srcElement;
                effectiveDest = dstElement;
            }
        }

        FileLinePositionSpan lineSpan = invocation.GetLocation().GetLineSpan();
        if (!lineSpan.IsValid)
        {
            return null;
        }

        ExpressionSyntax mapToken = invocation.Expression switch
        {
            MemberAccessExpressionSyntax ma => ma.Name,
            SimpleNameSyntax sn => sn,
            _ => invocation.Expression
        };
        FileLinePositionSpan mapNameSpan = mapToken.GetLocation().GetLineSpan();

        IMethodSymbol originalMethod = symbol.ReducedFrom ?? symbol;

        return new InterceptLocation(
            FilePath: lineSpan.Path,
            Line: mapNameSpan.StartLinePosition.Line + 1,
            Column: mapNameSpan.StartLinePosition.Character + 1,
            SourceTypeFullName: SourceEmitter.GetDisplayString(callSiteSource),
            DestinationTypeFullName: SourceEmitter.GetDisplayString(callSiteDest),
            MethodSignatureKey: BuildSignatureKey(symbol),
            ParameterSourceTypeFullName: originalMethod.Parameters.Length > 0 ? SourceEmitter.GetDisplayString(originalMethod.Parameters[0].Type) : "object",
            Kind: kind,
            IsCollectionMapping: isCollectionMapping,
            IsDestinationMapped: originalMethod.Parameters.Length > 1 && originalMethod.Parameters[1].Name == "destination",
            IsExtensionMap: symbol.ReducedFrom != null,
            EffectiveSourceTypeFullName: SourceEmitter.GetDisplayString(effectiveSource),
            EffectiveDestTypeFullName: SourceEmitter.GetDisplayString(effectiveDest),
            GenericParameters: originalMethod.TypeParameters.Length > 0 ? "<" + string.Join(", ", originalMethod.TypeParameters.Select(p => p.Name)) + ">" : null,
            TypeArguments: symbol.TypeArguments.Length > 0 ? new EquatableArray<string>(symbol.TypeArguments.Select(SourceEmitter.GetDisplayString)) : null,
            ExtraParameters: originalMethod.Parameters.Length > 1 ? new EquatableArray<string>(originalMethod.Parameters.Skip(1).Select(p => SourceEmitter.GetDisplayString(p.Type))) : null);
    }

    private static string BuildSignatureKey(IMethodSymbol method)
    {
        string typeArgs = method.TypeArguments.Length > 0
            ? $"<{string.Join(", ", method.TypeArguments.Select(SourceEmitter.GetDisplayString))}>"
            : string.Empty;
        string paramTypes = string.Join(", ", method.Parameters.Select(p => SourceEmitter.GetDisplayString(p.Type)));
        return $"{method.Name}{typeArgs}({paramTypes})";
    }

    /// <summary>
    ///   Unwraps known collection wrappers to extract the element type.
    ///   Returns <c>null</c> if the type is not a recognized collection wrapper.
    /// </summary>
    private static ITypeSymbol? TryGetCollectionElementType(ITypeSymbol type)
    {
        // Arrays: T[]
        if (type is IArrayTypeSymbol array)
        {
            return array.ElementType;
        }

        // Named generics: List<T>, IList<T>, IEnumerable<T>, ICollection<T>, etc.
        if (type is INamedTypeSymbol named && named.IsGenericType && named.TypeArguments.Length == 1)
        {
            _ = named.Name;
            string ns = named.ContainingNamespace?.ToDisplayString() ?? "";

            // System.Collections.Generic types
            if (ns == "System.Collections.Generic" || ns.StartsWith("System.Collections.Generic", System.StringComparison.Ordinal))
            {
                return named.TypeArguments[0];
            }

            // Also check if it implements IEnumerable<T> (custom collections)
            foreach (INamedTypeSymbol iface in named.AllInterfaces)
            {
                if (iface.Name == "IEnumerable" && iface.IsGenericType && iface.TypeArguments.Length == 1 &&
                    iface.ContainingNamespace?.ToDisplayString() == "System.Collections.Generic")
                {
                    return iface.TypeArguments[0];
                }
            }
        }

        return null;
    }
}
