using System.Collections.Generic;
using System.Linq;
using AutoMappic.Generator.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AutoMappic.Generator.Pipeline;

/// <summary>
///   Finds all <c>IMapper.Map&lt;T&gt;(source)</c> call sites in the user's code and
///   captures their exact file path, line, and column so the emitter can generate
///   <c>[InterceptsLocation]</c> attributes pointing at those exact coordinates.
/// </summary>
internal static class InterceptorCollector
{
    public static bool IsInvocationCandidate(SyntaxNode node, System.Threading.CancellationToken _)
    {
        if (node is InvocationExpressionSyntax inv &&
            inv.Expression is MemberAccessExpressionSyntax ma)
        {
            var text = ma.Name.Identifier.Text;
            return text == "Map" || text == "MapAsync" || text == "ProjectTo";
        }
        return false;
    }

    public static InterceptLocation? ExtractInterceptLocation(
        GeneratorSyntaxContext context,
        System.Threading.CancellationToken cancellationToken)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var symbol = context.SemanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol as IMethodSymbol;

        if (symbol is null) return null;
        var name = symbol.Name;
        if (name != "Map" && name != "MapAsync" && name != "ProjectTo") return null;

        var container = symbol.ContainingType?.Name;
        if (symbol.ContainingType?.ContainingNamespace?.ToDisplayString() != "AutoMappic") return null;

        InterceptKind kind;
        if (container == "IMapper" && (name == "Map" || name == "MapAsync")) kind = InterceptKind.Map;
        else if (container == "QueryableExtensions" && name == "ProjectTo") kind = InterceptKind.ProjectTo;
        else if (container == "DataReaderExtensions" && name == "Map") kind = InterceptKind.DataReaderMap;
        else return null;

        if (symbol.TypeArguments.Length == 0) return null;

        var destType = symbol.TypeArguments[symbol.TypeArguments.Length == 2 ? 1 : 0];
        ITypeSymbol? sourceType = null;

        if (kind == InterceptKind.Map)
        {
            if (symbol.TypeArguments.Length == 2)
            {
                sourceType = symbol.TypeArguments[0];
            }
            else if (invocation.ArgumentList.Arguments.Count > 0)
            {
                sourceType = context.SemanticModel.GetTypeInfo(invocation.ArgumentList.Arguments[0].Expression, cancellationToken).Type;
            }
        }
        else if (kind == InterceptKind.ProjectTo)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax ma && ma.Expression is ExpressionSyntax expr)
            {
                var typeInfo = context.SemanticModel.GetTypeInfo(expr, cancellationToken).Type;
                if (typeInfo is INamedTypeSymbol n && n.IsGenericType && n.TypeArguments.Length > 0)
                {
                    sourceType = n.TypeArguments[0];
                }
                else if (typeInfo is IArrayTypeSymbol array)
                {
                    sourceType = array.ElementType;
                }
            }
            // Fallback to symbol parameters if expression analysis failed
            if (sourceType is null)
            {
                var reduced = symbol.ReducedFrom ?? symbol;
                if (reduced.Parameters.Length > 0 && reduced.Parameters[0].Type is INamedTypeSymbol queryableType && queryableType.TypeArguments.Length > 0)
                {
                    sourceType = queryableType.TypeArguments[0];
                }
            }
        }
        else if (kind == InterceptKind.DataReaderMap)
        {
            sourceType = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Data.IDataReader");
        }

        if (destType is null || sourceType is null) return null;

        // Collection awareness: if mapping List<S> to List<D>, the effective types for coordination are S and D.
        var isCollectionMapping = false;
        var effectiveSource = sourceType;
        var effectiveDest = destType;

        if (IsCollection(sourceType, out var sItem) && IsCollection(destType, out var dItem))
        {
            isCollectionMapping = true;
            effectiveSource = sItem;
            effectiveDest = dItem;
        }

        var lineSpan = invocation.GetLocation().GetLineSpan();
        if (!lineSpan.IsValid) return null;

        var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;
        var mapNameSpan = memberAccess.Name.GetLocation().GetLineSpan();

        return new InterceptLocation(
            FilePath: lineSpan.Path,
            Line: mapNameSpan.StartLinePosition.Line + 1,
            Column: mapNameSpan.StartLinePosition.Character + 1,
            SourceTypeFullName: sourceType.ToDisplayString(),
            DestinationTypeFullName: destType.ToDisplayString(),
            MethodSignatureKey: BuildSignatureKey(symbol),
            ParameterSourceTypeFullName: symbol.Parameters.Length > 0 ? symbol.Parameters[0].Type.ToDisplayString() : "object",
            Kind: kind,
            IsCollectionMapping: isCollectionMapping,
            EffectiveSourceTypeFullName: effectiveSource.ToDisplayString(),
            EffectiveDestTypeFullName: effectiveDest.ToDisplayString());
    }

    private static bool IsCollection(ITypeSymbol type, out ITypeSymbol itemType)
    {
        itemType = null!;
        if (type is IArrayTypeSymbol array)
        {
            itemType = array.ElementType;
            return true;
        }

        if (type is INamedTypeSymbol named && named.IsGenericType && (named.Name == "List" || named.AllInterfaces.Any(i => i.Name == "IEnumerable")))
        {
            if (named.TypeArguments.Length > 0)
            {
                itemType = named.TypeArguments[0];
                return true;
            }
        }

        return false;
    }

    private static string BuildSignatureKey(IMethodSymbol method)
    {
        var typeArgs = method.TypeArguments.Length > 0
            ? $"<{string.Join(", ", method.TypeArguments.Select(t => t.ToDisplayString()))}>"
            : string.Empty;
        var paramTypes = string.Join(", ", method.Parameters.Select(p => p.Type.ToDisplayString()));
        return $"{method.Name}{typeArgs}({paramTypes})";
    }
}
