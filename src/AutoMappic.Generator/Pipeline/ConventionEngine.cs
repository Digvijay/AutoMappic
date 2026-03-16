using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AutoMappic.Generator.Models;
using Microsoft.CodeAnalysis;

namespace AutoMappic.Generator.Pipeline;

/// <summary>
///   The convention engine: given a source <see cref="ITypeSymbol" /> and a destination
///   <see cref="ITypeSymbol" />, resolves a <see cref="PropertyMap" /> for every writable
///   destination property.
/// </summary>
internal static class ConventionEngine
{
    private static readonly Regex PascalSplitter =
        new(@"([A-Z][a-z0-9]*)", RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));

    public static IReadOnlyList<PropertyMap> Resolve(
        ITypeSymbol source,
        ITypeSymbol destination,
        IReadOnlyDictionary<string, string?> explicitMaps,
        IReadOnlyCollection<string> ignoredMembers,
        Location? profileLocation,
        Action<Diagnostic> reportDiagnostic)
    {
        var result = new List<PropertyMap>();

        // 1. Validate that the destination has a parameterless constructor
        if (destination is INamedTypeSymbol namedDest && namedDest.TypeKind == TypeKind.Class)
        {
            var hasParameterlessCtor = namedDest.Constructors.Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public);
            if (!hasParameterlessCtor)
            {
                reportDiagnostic(Diagnostic.Create(
                    AutoMappicDiagnostics.MissingConstructor,
                    profileLocation ?? Location.None,
                    destination.Name));
            }
        }

        var destProperties = GetAllProperties(destination).Where(p => p.SetMethod is not null && p.SetMethod.DeclaredAccessibility == Accessibility.Public).ToList();
        var sourceProperties = GetAllProperties(source).Where(p => p.GetMethod is not null).ToList();
        var sourceMethods = GetAllZeroArgMethods(source);

        foreach (var destProp in destProperties)
        {
            var name = destProp.Name;
            bool isInitOnly = destProp.SetMethod?.IsInitOnly ?? false;

            if (ignoredMembers.Contains(name))
            {
                result.Add(new PropertyMap(name, null, PropertyMapKind.Ignored, isInitOnly));
                continue;
            }

            if (explicitMaps.TryGetValue(name, out var explicitExpression))
            {
                result.Add(new PropertyMap(name, explicitExpression ?? $"/* ForMember({name}) */", PropertyMapKind.Explicit, isInitOnly));
                continue;
            }

            var directMatch = sourceProperties.FirstOrDefault(
                p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(Normalize(p.Name), Normalize(name), StringComparison.OrdinalIgnoreCase));

            var methodMatch = sourceMethods.FirstOrDefault(
                m => string.Equals(m.Name, "Get" + name, StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(Normalize(m.Name), Normalize("Get" + name), StringComparison.OrdinalIgnoreCase));

            var flatPath = ResolveFlattenedPath(source, name);
            bool isTrulyFlattened = flatPath is not null && flatPath.Contains('.');

            if (directMatch is not null && isTrulyFlattened)
            {
                reportDiagnostic(Diagnostic.Create(
                    AutoMappicDiagnostics.AmbiguousMapping,
                    profileLocation ?? Location.None,
                    name, destination.Name, flatPath));
                continue;
            }

            if (directMatch is not null)
            {
                var sourceExpr = $"source.{directMatch.Name}";

                if (directMatch.Type.TypeKind == TypeKind.Enum && destProp.Type.SpecialType == SpecialType.System_String)
                {
                    sourceExpr = $"{sourceExpr}.ToString()";
                }
                else if (!SymbolEqualityComparer.Default.Equals(directMatch.Type, destProp.Type))
                {
                    sourceExpr = WrapWithNestedMapper(sourceExpr, directMatch.Type, destProp.Type);
                }
                else
                {
                    sourceExpr = ApplyNullabilityGuard(sourceExpr, directMatch.Type, destProp.Type);
                }

                result.Add(new PropertyMap(name, sourceExpr, PropertyMapKind.Direct, isInitOnly));
                continue;
            }

            if (methodMatch is not null)
            {
                result.Add(new PropertyMap(name, $"source.{methodMatch.Name}()", PropertyMapKind.Method, isInitOnly));
                continue;
            }

            if (isTrulyFlattened)
            {
                var sourceExpr = $"source.{flatPath}";
                if (!IsNullable(destProp.Type))
                {
                    sourceExpr = AppendNullDefault(sourceExpr, destProp.Type);
                }

                result.Add(new PropertyMap(name, sourceExpr, PropertyMapKind.Flattened, isInitOnly));
                continue;
            }

            reportDiagnostic(Diagnostic.Create(
                AutoMappicDiagnostics.UnmappedProperty,
                destProp.Locations.Length > 0 ? destProp.Locations[0] : Location.None,
                name, destination.Name, source.Name));
        }

        return result;
    }

    private static string GetMapMethodName(ITypeSymbol type) => $"MapTo{type.Name}";

    private static string WrapWithNestedMapper(string expression, ITypeSymbol sourceType, ITypeSymbol destType)
    {
        // 1. Nullable Value Type Conversion (int? -> int)
        if (sourceType.IsValueType && IsNullable(sourceType) && !IsNullable(destType))
        {
            return $"{expression}.GetValueOrDefault()";
        }

        // 2. Dictionary Mapping
        if (IsDictionary(sourceType, out var sKey, out var sVal) && IsDictionary(destType, out var dKey, out var dVal))
        {
            var guard = IsNullable(sourceType) ? "?" : "";

            // For primitive key/value types, use direct assignment or ToString() if it matches
            var keyExpr = "kv.Key";
            if (!SymbolEqualityComparer.Default.Equals(sKey, dKey))
            {
                if (dKey.SpecialType == SpecialType.System_String) keyExpr = "kv.Key.ToString()";
                else if (dKey.TypeKind == TypeKind.Class) keyExpr = $"kv.Key.{GetMapMethodName(dKey)}()";
                // Add more implicit coversions if needed. Fallback to cast.
                else keyExpr = $"({dKey.ToDisplayString()})kv.Key";
            }

            var valExpr = "kv.Value";
            if (!SymbolEqualityComparer.Default.Equals(sVal, dVal))
            {
                if (dVal.SpecialType == SpecialType.System_String) valExpr = "kv.Value?.ToString() ?? string.Empty";
                else if (dVal.TypeKind == TypeKind.Class) valExpr = $"kv.Value.{GetMapMethodName(dVal)}()";
                else valExpr = $"({dVal.ToDisplayString()})kv.Value";
            }

            var fallback = !IsNullable(destType) ? $" ?? new global::System.Collections.Generic.Dictionary<{dKey.ToDisplayString()}, {dVal.ToDisplayString()}>()" : "";
            return $"{expression}{guard}.ToDictionary(kv => {keyExpr}, kv => {valExpr}){fallback}";
        }

        // 3. Collection Mapping
        if (IsCollection(sourceType, out var sourceItemType) && IsCollection(destType, out var destItemType))
        {
            var guard = IsNullable(sourceType) ? "?" : "";
            var innerExpr = WrapWithNestedMapper("x", sourceItemType, destItemType);
            var selectExpr = (innerExpr == "x")
                ? ""
                : $".Select(x => {innerExpr})";

            var terminal = destType.TypeKind == TypeKind.Array ? ".ToArray()" : ".ToList()";
            var fallback = !IsNullable(destType)
                ? (destType.TypeKind == TypeKind.Array
                    ? $" ?? global::System.Array.Empty<{destItemType.ToDisplayString()}>()"
                    : $" ?? new global::System.Collections.Generic.List<{destItemType.ToDisplayString()}>()")
                : "";

            return $"{expression}{guard}{selectExpr}{terminal}{fallback}";
        }

        // 4. Complex Type Mapping
        if (destType.TypeKind == TypeKind.Class && destType.SpecialType == SpecialType.None)
        {
            var op = IsNullable(sourceType) ? "?." : ".";
            return $"{expression}{op}{GetMapMethodName(destType)}()";
        }

        return expression;
    }

    private static bool IsDictionary(ITypeSymbol type, out ITypeSymbol keyType, out ITypeSymbol valueType)
    {
        keyType = null!;
        valueType = null!;

        if (type is INamedTypeSymbol named && named.IsGenericType)
        {
            // Simple check for IDictionary<K,V> or Dictionary<K,V>
            if (named.TypeArguments.Length == 2)
            {
                keyType = named.TypeArguments[0];
                valueType = named.TypeArguments[1];
                return true;
            }
        }
        return false;
    }

    private static bool IsCollection(ITypeSymbol type, out ITypeSymbol itemType)
    {
        itemType = null!;
        if (type is IArrayTypeSymbol array)
        {
            itemType = array.ElementType;
            return true;
        }

        if (type is INamedTypeSymbol named && named.IsGenericType)
        {
            if (named.TypeArguments.Length == 1)
            {
                itemType = named.TypeArguments[0];
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<IPropertySymbol> GetAllProperties(ITypeSymbol type)
    {
        var current = type;
        while (current is not null)
        {
            foreach (var prop in current.GetMembers().OfType<IPropertySymbol>().Where(p => !p.IsStatic && p.DeclaredAccessibility == Accessibility.Public))
            {
                yield return prop;
            }
            current = current.BaseType;
        }
    }

    private static IEnumerable<IMethodSymbol> GetAllZeroArgMethods(ITypeSymbol type)
    {
        var current = type;
        while (current is not null)
        {
            foreach (var method in current.GetMembers().OfType<IMethodSymbol>()
                .Where(m => !m.IsStatic && m.MethodKind == MethodKind.Ordinary && m.Parameters.Length == 0 && m.DeclaredAccessibility == Accessibility.Public && m.Name.StartsWith("Get", StringComparison.Ordinal)))
            {
                yield return method;
            }
            current = current.BaseType;
        }
    }

    private static string? ResolveFlattenedPath(ITypeSymbol source, string destPropertyName)
    {
        var parts = SplitPascalCase(destPropertyName);
        if (parts.Count < 2) return null;
        return WalkPath(source, parts, 0);
    }

    private static string? WalkPath(ITypeSymbol current, IReadOnlyList<string> parts, int index)
    {
        if (index == parts.Count) return string.Empty;
        for (var length = parts.Count - index; length >= 1; length--)
        {
            var segment = string.Concat(parts.Skip(index).Take(length));
            var prop = GetAllProperties(current)
                .FirstOrDefault(p => string.Equals(p.Name, segment, StringComparison.OrdinalIgnoreCase));

            if (prop is null) continue;

            // If we found a match at the root with the full name, it's not flattening.
            if (index == 0 && length == parts.Count) continue;

            if (index + length == parts.Count) return prop.Name;

            var rest = WalkPath(prop.Type, parts, index + length);
            if (rest is not null)
            {
                var separator = prop.Type.IsReferenceType ? "?." : ".";
                return rest.Length > 0 ? $"{prop.Name}{separator}{rest}" : prop.Name;
            }
        }
        return null;
    }

    private static List<string> SplitPascalCase(string name)
    {
        var matches = PascalSplitter.Matches(name);
        var parts = new List<string>(matches.Count);
        foreach (System.Text.RegularExpressions.Match m in matches) parts.Add(m.Value);
        return parts;
    }

    private static string Normalize(string name) => name.Replace("_", "");

    private static bool IsNullable(ITypeSymbol type) =>
        type.NullableAnnotation == NullableAnnotation.Annotated
        || (type is INamedTypeSymbol { IsValueType: true } nt
            && nt.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T);

    private static string ApplyNullabilityGuard(string expression, ITypeSymbol sourceType, ITypeSymbol destType)
    {
        bool sourceNullable = IsNullable(sourceType);
        bool destNullable = IsNullable(destType);

        if (sourceNullable && !destNullable)
        {
            if (sourceType.IsValueType)
            {
                return $"{expression}.GetValueOrDefault()";
            }
            return AppendNullDefault(expression, destType);
        }

        return expression;
    }

    private static string AppendNullDefault(string sourceExpr, ITypeSymbol destType)
    {
        if (destType.SpecialType == SpecialType.System_String)
            return $"{sourceExpr} ?? string.Empty";

        if (destType.TypeKind == TypeKind.Array)
            return $"{sourceExpr} ?? global::System.Array.Empty<{((IArrayTypeSymbol)destType).ElementType.ToDisplayString()}>()";

        if (!destType.IsReferenceType)
            return sourceExpr;

        return $"{sourceExpr}!";
    }
}
