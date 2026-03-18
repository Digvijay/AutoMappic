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

    /// <summary>
    ///   Resolves the property mappings for a given source and destination type.
    /// </summary>
    /// <param name="source">The source type symbol.</param>
    /// <param name="destination">The destination type symbol.</param>
    /// <param name="explicitMaps">A dictionary of explicit member mappings.</param>
    /// <param name="ignoredMembers">A collection of members to ignore.</param>
    /// <param name="profileLocation">The location of the profile declaration for diagnostics.</param>
    /// <param name="reportDiagnostic">A delegate to report diagnostics.</param>
    /// <returns>A list of resolved <see cref="PropertyMap"/> instances.</returns>
    public static (IReadOnlyList<PropertyMap> Properties, IReadOnlyList<PropertyMap> ConstructorArguments) Resolve(
        ITypeSymbol source,
        ITypeSymbol destination,
        IReadOnlyDictionary<string, string?> explicitMaps,
        IReadOnlyCollection<string> ignoredMembers,
        Location? profileLocation,
        Action<Diagnostic> reportDiagnostic)
    {
        var properties = new List<PropertyMap>();
        var constructorArgs = new List<PropertyMap>();

        // 1. Find the best constructor
        IMethodSymbol? bestCtor = null;
        if (destination is INamedTypeSymbol namedDest && namedDest.TypeKind == TypeKind.Class)
        {
            var publicCtors = namedDest.Constructors
                .Where(c => c.DeclaredAccessibility == Accessibility.Public)
                .OrderByDescending(c => c.Parameters.Length)
                .ToList();

            foreach (var ctor in publicCtors)
            {
                var candidateArgs = new List<PropertyMap>();
                bool allSatisfied = true;
                foreach (var param in ctor.Parameters)
                {
                    var paramMap = ResolveSourceForMember(source, param.Name, param.Type, explicitMaps, ignoredMembers, profileLocation, reportDiagnostic);
                    if (paramMap is null || paramMap.Kind == PropertyMapKind.Ignored)
                    {
                        allSatisfied = false;
                        break;
                    }
                    candidateArgs.Add(paramMap);
                }

                if (allSatisfied)
                {
                    bestCtor = ctor;
                    constructorArgs = candidateArgs;
                    break;
                }
            }

            if (bestCtor is null && publicCtors.Count > 0 && !publicCtors.Any(c => c.Parameters.Length == 0))
            {
                // No satisfyable constructor found, and no parameterless constructor exists
                reportDiagnostic(Diagnostic.Create(
                    AutoMappicDiagnostics.MissingConstructor,
                    profileLocation ?? Location.None,
                    destination.Name));
            }
        }

        // 2. Resolve properties
        var destProperties = GetAllProperties(destination)
            .Where(p => p.SetMethod is not null && p.SetMethod.DeclaredAccessibility == Accessibility.Public)
            .ToList();

        // If we are using a constructor, we might want to skip properties already set by it?
        // AutoMapper typically sets them again if they have a setter.
        var constructorParamNames = new HashSet<string>(constructorArgs.Select(a => a.DestinationProperty), StringComparer.OrdinalIgnoreCase);

        foreach (var destProp in destProperties)
        {
            var name = destProp.Name;

            // Skip if it was already used in constructor?
            // Actually, we usually don't skip unless there's no setter (handled via GetAllProperties).
            // But if it's 'required', we MUST ensure it's mapped either via ctor or property.

            var map = ResolveSourceForMember(source, name, destProp.Type, explicitMaps, ignoredMembers, profileLocation, reportDiagnostic);
            if (map is not null)
            {
                var finalMap = map with { IsInitOnly = destProp.SetMethod?.IsInitOnly ?? false };
                properties.Add(finalMap);
            }
            else
            {
                var isRequired = destProp.IsRequired || destProp.GetAttributes().Any(a => a.AttributeClass?.Name == "RequiredAttribute");
                // If it's required but was mapped via constructor, it's satisfied!
                if (isRequired && constructorParamNames.Contains(name))
                {
                    continue;
                }

                var messageSuffix = isRequired ? " (This property is marked as [Required] or using the 'required' modifier.)" : "";
                reportDiagnostic(Diagnostic.Create(
                    AutoMappicDiagnostics.UnmappedProperty,
                    destProp.Locations.Length > 0 ? destProp.Locations[0] : Location.None,
                    name + messageSuffix, destination.Name, source.Name));
            }
        }

        return (properties, constructorArgs);
    }

    private static PropertyMap? ResolveSourceForMember(
        ITypeSymbol source,
        string targetName,
        ITypeSymbol targetType,
        IReadOnlyDictionary<string, string?> explicitMaps,
        IReadOnlyCollection<string> ignoredMembers,
        Location? profileLocation,
        Action<Diagnostic> reportDiagnostic)
    {
        var sourceProperties = GetAllProperties(source).Where(p => p.GetMethod is not null).ToList();
        var sourceMethods = GetAllZeroArgMethods(source);

        if (ignoredMembers.Contains(targetName))
        {
            return new PropertyMap(targetName, null, PropertyMapKind.Ignored);
        }

        if (explicitMaps.TryGetValue(targetName, out var explicitExpression))
        {
            return new PropertyMap(targetName, explicitExpression ?? $"/* ForMember({targetName}) */", PropertyMapKind.Explicit);
        }

        var directMatch = sourceProperties.FirstOrDefault(
            p => string.Equals(p.Name, targetName, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(Normalize(p.Name), Normalize(targetName), StringComparison.OrdinalIgnoreCase));

        var methodMatch = sourceMethods.FirstOrDefault(
            m => string.Equals(m.Name, "Get" + targetName, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(Normalize(m.Name), Normalize("Get" + targetName), StringComparison.OrdinalIgnoreCase));

        var flatPath = ResolveFlattenedPath(source, targetName);
        bool isTrulyFlattened = flatPath is not null && flatPath.Contains('.');

        if (directMatch is not null && isTrulyFlattened)
        {
            reportDiagnostic(Diagnostic.Create(
                AutoMappicDiagnostics.AmbiguousMapping,
                profileLocation ?? Location.None,
                targetName, "???", flatPath));
            return null;
        }

        if (directMatch is not null)
        {
            var sourceExpr = $"source.{directMatch.Name}";

            if (directMatch.Type.TypeKind == TypeKind.Enum && targetType.SpecialType == SpecialType.System_String)
            {
                sourceExpr = $"{sourceExpr}.ToString()";
                return new PropertyMap(targetName, sourceExpr, PropertyMapKind.Direct);
            }
            else if (!SymbolEqualityComparer.Default.Equals(directMatch.Type, targetType))
            {
                var (nestedExpr, nSrc, nDest, isColl, isArr, itemExpr) = WrapWithNestedMapper(sourceExpr, directMatch.Type, targetType);
                return new PropertyMap(
                    DestinationProperty: targetName,
                    SourceExpression: nestedExpr,
                    Kind: PropertyMapKind.Direct,
                    NestedSourceTypeFullName: nSrc,
                    NestedDestTypeFullName: nDest,
                    IsCollection: isColl,
                    IsArray: isArr,
                    NestedExpression: itemExpr);
            }
            else
            {
                sourceExpr = ApplyNullabilityGuard(sourceExpr, directMatch.Type, targetType);
                return new PropertyMap(targetName, sourceExpr, PropertyMapKind.Direct);
            }
        }

        if (methodMatch is not null)
        {
            return new PropertyMap(targetName, $"source.{methodMatch.Name}()", PropertyMapKind.Method);
        }

        if (isTrulyFlattened)
        {
            var sourceExpr = $"source.{flatPath}";
            if (!IsNullable(targetType))
            {
                sourceExpr = AppendNullDefault($"({sourceExpr})", targetType);
            }

            return new PropertyMap(targetName, sourceExpr, PropertyMapKind.Flattened);
        }

        return null;
    }

    private static string GetMapMethodName(ITypeSymbol type) => $"MapTo{type.Name}";

    /// <summary>
    ///   Wraps an expression with appropriate mapping logic based on type compatibility.
    /// </summary>
    /// <param name="expression">The base source expression.</param>
    /// <param name="sourceType">The type of the source expression.</param>
    /// <param name="destType">The target destination type.</param>
    /// <returns> A tuple containing the resolved expression and metadata about the mapping.</returns>
    private static (string Expression, string? NestedSource, string? NestedDest, bool IsCollection, bool IsArray, string? ItemExpression)
        WrapWithNestedMapper(string expression, ITypeSymbol sourceType, ITypeSymbol destType)
    {
        // 1. Nullable Value Type Conversion (int? -> int)
        if (sourceType.IsValueType && IsNullable(sourceType) && !IsNullable(destType))
        {
            return (Expression: $"{expression}.GetValueOrDefault()", null, null, false, false, null);
        }

        // 2. Dictionary Mapping
        if (IsDictionary(sourceType, out var sKey, out var sVal) && IsDictionary(destType, out var dKey, out var dVal))
        {
            var guard = IsNullable(sourceType) ? "?" : "";

            // For primitive key/value types, use direct assignment or ToString() if it matches
            var keyExpr = "kv.Key";
            string? knSrc = null, knDest = null;
            if (!SymbolEqualityComparer.Default.Equals(sKey, dKey))
            {
                if (dKey.SpecialType == SpecialType.System_String) keyExpr = "kv.Key.ToString()";
                else if (dKey.TypeKind == TypeKind.Class)
                {
                    keyExpr = $"kv.Key.{GetMapMethodName(dKey)}()";
                    knSrc = GetDisplayString(sKey);
                    knDest = GetDisplayString(dKey);
                }
                // Add more implicit coversions if needed. Fallback to cast.
                else keyExpr = $"({dKey.ToDisplayString()})kv.Key";
            }

            var valExpr = "kv.Value";
            string? vnSrc = null, vnDest = null;
            if (!SymbolEqualityComparer.Default.Equals(sVal, dVal))
            {
                if (dVal.SpecialType == SpecialType.System_String) valExpr = "kv.Value?.ToString() ?? string.Empty";
                else if (dVal.TypeKind == TypeKind.Class)
                {
                    valExpr = $"kv.Value.{GetMapMethodName(dVal)}()";
                    vnSrc = GetDisplayString(sVal);
                    vnDest = GetDisplayString(dVal);
                }
                else valExpr = $"({dVal.ToDisplayString()})kv.Value";
            }

            var fallback = !IsNullable(destType) ? $" ?? new global::System.Collections.Generic.Dictionary<{dKey.ToDisplayString()}, {dVal.ToDisplayString()}>()" : "";
            // Dictionary mapping can have two nested mappings, but PropertyMap only has one.
            // We prioritize the Value mapping as it's more likely to be recursive.
            return (Expression: $"{expression}{guard}.ToDictionary(kv => {keyExpr}, kv => {valExpr}){fallback}", vnSrc ?? knSrc, vnDest ?? knDest, false, false, null);
        }

        // 3. Collection Mapping
        if (IsCollection(sourceType, out var sourceItemType) && IsCollection(destType, out var destItemType))
        {
            var guard = IsNullable(sourceType) ? "?" : "";
            var (innerExpr, nSrc, nDest, iColl, iArr, iItem) = WrapWithNestedMapper("x", sourceItemType, destItemType);

            // We'll replace this LINQ expression in SourceEmitter with a loop-based implementation
            // and we set the collection flags to guide the emitter.
            bool isArray = destType.TypeKind == TypeKind.Array || destType.ToDisplayString().Contains("[]");
            return (Expression: expression, NestedSource: GetDisplayString(sourceItemType), NestedDest: GetDisplayString(destItemType), IsCollection: true, IsArray: isArray, ItemExpression: innerExpr);
        }

        // 4. Complex Type Mapping
        if (destType.TypeKind == TypeKind.Class && destType.SpecialType == SpecialType.None)
        {
            var op = IsNullable(sourceType) ? "?." : ".";
            return (Expression: $"{expression}{op}{GetMapMethodName(destType)}()", NestedSource: GetDisplayString(sourceType), NestedDest: GetDisplayString(destType), IsCollection: false, IsArray: false, ItemExpression: null);
        }

        return (Expression: expression, NestedSource: null, NestedDest: null, IsCollection: false, IsArray: false, ItemExpression: null);
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
        || IsActuallyNullableValueType(type);

    private static bool IsActuallyNullableValueType(ITypeSymbol type) =>
        type is INamedTypeSymbol { IsValueType: true } nt
            && nt.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

    private static string ApplyNullabilityGuard(string expression, ITypeSymbol sourceType, ITypeSymbol destType)
    {
        bool sourceNullable = IsNullable(sourceType);
        bool destNullable = IsNullable(destType);

        if (sourceNullable && !destNullable)
        {
            if (IsActuallyNullableValueType(sourceType))
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

        if (destType.IsValueType && !IsNullable(destType))
            return $"{sourceExpr}.GetValueOrDefault()";

        return $"{sourceExpr}!";
    }

    private static string GetDisplayString(ITypeSymbol type) =>
        type.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString();
}
