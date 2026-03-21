using System.Collections.Generic;
using System.Linq;
using AutoMappic.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoMappic.Generator.Pipeline;

/// <summary> Finds all call sites and attempts to intercept them if they match AutoMappic patterns. </summary>
internal static class InterceptorCollector
{
    public static bool IsInvocationCandidate(SyntaxNode node, System.Threading.CancellationToken _)
    {
        return node is InvocationExpressionSyntax;
    }

    public static InterceptLocation? ExtractInterceptLocation(
        GeneratorSyntaxContext context,
        System.Threading.CancellationToken cancellationToken)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var info = context.SemanticModel.GetSymbolInfo(invocation, cancellationToken);
        var symbol = info.Symbol as IMethodSymbol ?? info.CandidateSymbols.FirstOrDefault() as IMethodSymbol;

        if (symbol is null)
        {
            return null;
        }

        var name = symbol.Name;
        bool isProjectTo = name == "ProjectTo";
        bool isMap = name == "Map" || name == "MapAsync";

        if (!isProjectTo && !isMap)
        {
            return null;
        }

        var containingType = symbol.ContainingType;
        if (containingType == null)
        {
            return null;
        }

        var fullContainingType = containingType.ToDisplayString();
        InterceptKind kind;

        if (isProjectTo)
        {
            kind = InterceptKind.ProjectTo;
        }
        else if (fullContainingType.IndexOf("DataReaderExtensions", System.StringComparison.Ordinal) >= 0)
        {
            kind = InterceptKind.DataReaderMap;
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
        var isAutoMappic = fullContainingType.IndexOf("AutoMappic", System.StringComparison.Ordinal) >= 0 ||
                          containingType.AllInterfaces.Any(i => i.Name == "IMapper" && i.ContainingNamespace?.ToDisplayString().IndexOf("AutoMappic", System.StringComparison.Ordinal) >= 0);

        if (!isAutoMappic)
        {
            return null;
        }

        // Coordination types (S and D) must be concrete call-site types
        var callSiteDest = symbol.TypeArguments.Length > 0 ? symbol.TypeArguments[symbol.TypeArguments.Length >= 2 ? 1 : 0] : null;
        ITypeSymbol? callSiteSource = null;

        if (kind == InterceptKind.Map)
        {
            callSiteSource = symbol.TypeArguments.Length >= 2 ? symbol.TypeArguments[0] : (invocation.ArgumentList.Arguments.Count > 0 ? context.SemanticModel.GetTypeInfo(invocation.ArgumentList.Arguments[0].Expression, cancellationToken).Type : null);
        }
        else if (kind == InterceptKind.ProjectTo)
        {
            // Try explicit TSource first (2-arg version)
            callSiteSource = symbol.TypeArguments.Length >= 2 ? symbol.TypeArguments[0] : null;

            // Try the ACTUAL expression type of the receiver (most accurate for extension methods)
            if (callSiteSource == null)
            {
                var receiverExpr = invocation.Expression is MemberAccessExpressionSyntax ma ? ma.Expression : null;
                if (receiverExpr != null)
                {
                    var receiverType = context.SemanticModel.GetTypeInfo(receiverExpr, cancellationToken).Type;
                    if (receiverType is INamedTypeSymbol nr && nr.IsGenericType && nr.TypeArguments.Length > 0)
                    {
                        callSiteSource = nr.TypeArguments[0];
                    }
                    else if (receiverType != null)
                    {
                        // Check interfaces (e.g. List<T> -> IQueryable<T>)
                        var iqt = receiverType.AllInterfaces.FirstOrDefault(i => i.Name == "IQueryable" && i.IsGenericType && i.TypeArguments.Length > 0);
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
                var receiver = symbol.ReceiverType;
                if (receiver is INamedTypeSymbol nr && nr.IsGenericType && nr.TypeArguments.Length > 0)
                {
                    callSiteSource = nr.TypeArguments[0];
                }
                else if (receiver != null)
                {
                    var iQueryableT = receiver.AllInterfaces.FirstOrDefault(i => i.Name == "IQueryable" && i.IsGenericType && i.TypeArguments.Length > 0);
                    if (iQueryableT != null)
                    {
                        callSiteSource = iQueryableT.TypeArguments[0];
                    }
                }
            }
        }
        else if (kind == InterceptKind.DataReaderMap)
        {
            callSiteSource = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Data.IDataReader");
        }

        if (callSiteDest is null || callSiteSource is null)
        {
            return null;
        }

        var lineSpan = invocation.GetLocation().GetLineSpan();
        if (!lineSpan.IsValid)
        {
            return null;
        }

        var mapToken = invocation.Expression switch
        {
            MemberAccessExpressionSyntax ma => ma.Name,
            SimpleNameSyntax sn => sn,
            _ => invocation.Expression
        };
        var mapNameSpan = mapToken.GetLocation().GetLineSpan();

        var originalMethod = symbol.ReducedFrom ?? symbol;

        return new InterceptLocation(
            FilePath: lineSpan.Path,
            Line: mapNameSpan.StartLinePosition.Line + 1,
            Column: mapNameSpan.StartLinePosition.Character + 1,
            SourceTypeFullName: SourceEmitter.GetDisplayString(callSiteSource),
            DestinationTypeFullName: SourceEmitter.GetDisplayString(callSiteDest),
            MethodSignatureKey: BuildSignatureKey(symbol),
            ParameterSourceTypeFullName: originalMethod.Parameters.Length > 0 ? SourceEmitter.GetDisplayString(originalMethod.Parameters[0].Type) : "object",
            Kind: kind,
            IsCollectionMapping: false,
            IsDestinationMapped: originalMethod.Parameters.Length > 1 && originalMethod.Parameters[1].Name == "destination",
            EffectiveSourceTypeFullName: SourceEmitter.GetDisplayString(callSiteSource),
            EffectiveDestTypeFullName: SourceEmitter.GetDisplayString(callSiteDest),
            GenericParameters: originalMethod.TypeParameters.Length > 0 ? "<" + string.Join(", ", originalMethod.TypeParameters.Select(p => p.Name)) + ">" : null,
            TypeArguments: symbol.TypeArguments.Length > 0 ? new EquatableArray<string>(symbol.TypeArguments.Select(t => SourceEmitter.GetDisplayString(t))) : null,
            ExtraParameters: originalMethod.Parameters.Length > 1 ? new EquatableArray<string>(originalMethod.Parameters.Skip(1).Select(p => SourceEmitter.GetDisplayString(p.Type))) : null);
    }

    private static string BuildSignatureKey(IMethodSymbol method)
    {
        var typeArgs = method.TypeArguments.Length > 0
            ? $"<{string.Join(", ", method.TypeArguments.Select(t => SourceEmitter.GetDisplayString(t)))}>"
            : string.Empty;
        var paramTypes = string.Join(", ", method.Parameters.Select(p => SourceEmitter.GetDisplayString(p.Type)));
        return $"{method.Name}{typeArgs}({paramTypes})";
    }
}
