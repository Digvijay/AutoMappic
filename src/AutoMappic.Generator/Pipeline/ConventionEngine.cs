using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AutoMappic.Generator.Models;
using Microsoft.CodeAnalysis;

namespace AutoMappic.Generator.Pipeline;

internal static class ConventionEngine
{

    public static (IReadOnlyList<PropertyMap> Properties, IReadOnlyList<PropertyMap> ConstructorArguments) Resolve(
        ITypeSymbol source,
        ITypeSymbol destination,
        IReadOnlyDictionary<string, (string? Expression, string? Condition, bool IsAsync)> explicitMaps,
        IReadOnlyCollection<string> ignoredMembers,
        Location? profileLocation,
        Action<Diagnostic> reportDiagnostic,
        HashSet<(ITypeSymbol, ITypeSymbol)>? mappingStack = null,
        string? sourceNaming = null,
        string? destNaming = null)
    {
        var properties = new List<PropertyMap>();
        var constructorArgs = new List<PropertyMap>();
        mappingStack ??= new HashSet<(ITypeSymbol, ITypeSymbol)>(new TypePairComparer());

        var key = (UnwrapNullable(source), UnwrapNullable(destination));
        if (mappingStack.Contains(key))
        {
            reportDiagnostic(Diagnostic.Create(AutoMappicDiagnostics.CircularReference, profileLocation ?? Location.None, source.Name, destination.Name));
            return (properties, constructorArgs);
        }

        mappingStack.Add(key);
        try
        {
            // 1. Constructor Matching
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
                        var paramMap = ResolveSourceForMember(source, param.Name, param.Type, explicitMaps, ignoredMembers, profileLocation, reportDiagnostic, mappingStack, sourceNaming, destNaming, "source", destination.Name);
                        if (paramMap is null || paramMap.Kind == PropertyMapKind.Ignored)
                        {
                            allSatisfied = false;
                            break;
                        }
                        candidateArgs.Add(paramMap);
                    }
                    if (allSatisfied)
                    {
                        constructorArgs = candidateArgs;
                        break;
                    }
                }
            }

            // 2. Property/Field Matching
            var destMembers = GetAllWritableMembers(destination);

            if (constructorArgs.Count == 0 && destination is INamedTypeSymbol classDest && classDest.TypeKind == TypeKind.Class)
            {
                if (!classDest.Constructors.Any(c => c.DeclaredAccessibility == Accessibility.Public && c.Parameters.Length == 0))
                {
                    reportDiagnostic(Diagnostic.Create(AutoMappicDiagnostics.MissingConstructor, profileLocation ?? Location.None, destination.Name));
                }
            }

            var constructorParamNames = new HashSet<string>(constructorArgs.Select(a => a.DestinationProperty), StringComparer.OrdinalIgnoreCase);

            foreach (var memberName in destMembers.Keys)
            {
                var member = destMembers[memberName];
                if (member.GetAttributes().Any(a => a.AttributeClass?.Name == "AutoMappicIgnoreAttribute"))
                {
                    properties.Add(new PropertyMap(memberName, null, PropertyMapKind.Ignored));
                    continue;
                }

                var map = ResolveSourceForMember(source, memberName, GetMemberType(member), explicitMaps, ignoredMembers, profileLocation, reportDiagnostic, mappingStack, sourceNaming, destNaming, "source", destination.Name);
                if (map is not null)
                {
                    bool isInit = false;
                    bool isReadOnly = false;
                    if (member is IPropertySymbol p)
                    {
                        isInit = p.SetMethod?.IsInitOnly ?? false;
                        isReadOnly = p.SetMethod == null || p.SetMethod.DeclaredAccessibility != Accessibility.Public;
                    }
                    else if (member is IFieldSymbol f)
                    {
                        isReadOnly = f.IsReadOnly;
                    }
                    properties.Add(map with { IsInitOnly = isInit, IsReadOnly = isReadOnly });
                }
                else if (!constructorParamNames.Contains(memberName))
                {
                    if (source.Name != "IDataReader" && source.Name != "DataRow" && source.Name != "SqlDataReader")
                    {
                        reportDiagnostic(Diagnostic.Create(AutoMappicDiagnostics.UnmappedProperty, member.Locations.FirstOrDefault() ?? Location.None, memberName, destination.Name, source.Name));
                    }
                }
            }

            if (properties.Count == 0 && constructorArgs.Count == 0)
            {
                reportDiagnostic(Diagnostic.Create(AutoMappicDiagnostics.AsymmetricMapping, profileLocation ?? Location.None, source.Name, destination.Name));
            }

            return (properties, constructorArgs);
        }
        finally
        {
            mappingStack.Remove(key);
        }
    }

    private static PropertyMap? ResolveSourceForMember(
        ITypeSymbol source,
        string targetName,
        ITypeSymbol targetType,
        IReadOnlyDictionary<string, (string? Expression, string? Condition, bool IsAsync)> explicitMaps,
        IReadOnlyCollection<string> ignoredMembers,
        Location? profileLocation,
        Action<Diagnostic> reportDiagnostic,
        HashSet<(ITypeSymbol, ITypeSymbol)> mappingStack,
        string? sourceNaming = null,
        string? destNaming = null,
        string sourceAccess = "source",
        string destTypeName = "Unknown")
    {
        if (ignoredMembers.Contains(targetName))
        {
            return new PropertyMap(targetName, null, PropertyMapKind.Ignored);
        }

        // Tuple Handling
        if (source.IsTupleType && source is INamedTypeSymbol tupleSource)
        {
            for (int i = 0; i < tupleSource.TupleElements.Length; i++)
            {
                var element = tupleSource.TupleElements[i];
                var subMap = ResolveSourceForMember(element.Type, targetName, targetType, explicitMaps, Array.Empty<string>(), profileLocation, reportDiagnostic, mappingStack, sourceNaming, destNaming, $"{sourceAccess}.Item{i + 1}", destTypeName);
                if (subMap is not null && subMap.Kind != PropertyMapKind.Ignored)
                {
                    return subMap;
                }
            }
        }

        // Explicit Maps
        string? conditionBody = null;
        if (explicitMaps.TryGetValue(targetName, out var explicitData))
        {
            conditionBody = explicitData.Condition;
            if (explicitData.Expression != null)
            {
                return new PropertyMap(targetName, explicitData.Expression, PropertyMapKind.Explicit, IsAsync: explicitData.IsAsync, ConditionBody: conditionBody);
            }
        }

        // IDataReader projection
        if (source.Name == "IDataReader" || source.Name == "DataRow" || source.Name == "SqlDataReader")
        {
            var keyName = targetName;
            if (sourceNaming?.Contains("Kebab") == true)
                keyName = NamingUtility.ToKebabCase(targetName);
            else if (sourceNaming?.Contains("LowerUnderscore") == true || sourceNaming?.Contains("Snake") == true)
                keyName = NamingUtility.ToSnakeCase(targetName);

            var unwrapped = UnwrapNullable(targetType);
            var method = unwrapped.SpecialType switch
            {
                SpecialType.System_String => "GetString",
                SpecialType.System_Int32 => "GetInt32",
                SpecialType.System_Int64 => "GetInt64",
                SpecialType.System_Int16 => "GetInt16",
                SpecialType.System_Decimal => "GetDecimal",
                SpecialType.System_Double => "GetDouble",
                SpecialType.System_Single => "GetFloat",
                SpecialType.System_Boolean => "GetBoolean",
                SpecialType.System_Byte => "GetByte",
                SpecialType.System_DateTime => "GetDateTime",
                _ => "GetValue"
            };

            var ordinal = $"{sourceAccess}.GetOrdinal(\"{keyName}\")";
            var expr = $"{sourceAccess}.{method}({ordinal})";
            if (method == "GetValue" || unwrapped.TypeKind == TypeKind.Enum)
                expr = $"({unwrapped.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){expr}";

            var typeStr = targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var isNullable = targetType.IsReferenceType || (targetType.IsValueType && targetType.NullableAnnotation == NullableAnnotation.Annotated);
            if (isNullable)
                expr = $"{sourceAccess}.IsDBNull({ordinal}) ? default({typeStr})! : {expr}";

            return new PropertyMap(targetName, expr, PropertyMapKind.Direct, DataReaderColumn: keyName, ConditionBody: conditionBody);
        }

        // Dictionary -> Object indexer projection
        if (IsDictionary(source, out var srcKey, out var srcValue) && srcKey.SpecialType == SpecialType.System_String)
        {
            var keyName = targetName;
            if (sourceNaming?.Contains("Kebab") == true)
                keyName = NamingUtility.ToKebabCase(targetName);
            else if (sourceNaming?.Contains("LowerUnderscore") == true || sourceNaming?.Contains("Snake") == true)
                keyName = NamingUtility.ToSnakeCase(targetName);

            var sourceExpr = $"{sourceAccess} != null && {sourceAccess}.ContainsKey(\"{keyName}\") ? {sourceAccess}[\"{keyName}\"] : default!";
            var (nE, nS, nD, iC, iA, iE, rE, nCond) = WrapWithNestedMapper(sourceExpr, srcValue, targetType, profileLocation, reportDiagnostic, mappingStack, targetName);
            return new PropertyMap(targetName, nE, PropertyMapKind.Direct, NestedSourceTypeFullName: nS, NestedDestTypeFullName: nD, NestedFullDestTypeFullName: targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), IsCollection: iC, IsArray: iA, NestedExpression: iE, SourceRawExpression: rE, ConditionBody: conditionBody ?? nCond);
        }

        // Discovery
        var readableMembers = GetReadableMembers(source);
        var directMatches = readableMembers.Where(m =>
            string.Equals(m.Name, targetName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(NamingUtility.Normalize(m.Name), NamingUtility.Normalize(targetName), StringComparison.OrdinalIgnoreCase)).ToList();

        var sourceMethods = GetAllZeroArgMethods(source);
        var methodMatches = sourceMethods.Where(m =>
            string.Equals(m.Name, "Get" + targetName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(NamingUtility.Normalize(m.Name), NamingUtility.Normalize("Get" + targetName), StringComparison.OrdinalIgnoreCase)).ToList();

        // Flattening
        var flatPath = ResolveFlattenedPath(source, targetName, sourceNaming, destNaming, targetType);

        // Ambiguity Detection: Check if direct/method matches and a flattened path both exist
        if ((directMatches.Count + methodMatches.Count > 0) && flatPath != null)
        {
            reportDiagnostic(Diagnostic.Create(AutoMappicDiagnostics.AmbiguousMapping, profileLocation ?? Location.None, targetName, destTypeName, source.Name));
        }

        // Return direct/method match if found
        ISymbol? directMatch = directMatches.FirstOrDefault();
        IMethodSymbol? methodMatch = methodMatches.FirstOrDefault();
        if (directMatch is not null || methodMatch is not null)
        {
            var sourceType = directMatch is not null ? GetMemberType(directMatch) : methodMatch!.ReturnType;
            var sourceExpr = directMatch is not null ? $"{sourceAccess}.{directMatch.Name}" : $"{sourceAccess}.{methodMatch!.Name}()";

            var (nestedExpr, nSrc, nDest, isColl, isArr, itemExpr, rawExpr, nCond) = WrapWithNestedMapper(sourceExpr, sourceType, targetType, profileLocation, reportDiagnostic, mappingStack, targetName);
            return new PropertyMap(targetName, nestedExpr, PropertyMapKind.Direct,
                NestedSourceTypeFullName: nSrc, NestedDestTypeFullName: nDest,
                NestedFullDestTypeFullName: targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                IsCollection: isColl, IsArray: isArr, NestedExpression: itemExpr, SourceRawExpression: rawExpr, ConditionBody: conditionBody ?? nCond);
        }

        if (flatPath != null)
        {
            return new PropertyMap(targetName, $"{sourceAccess}.{flatPath}", PropertyMapKind.Flattened, ConditionBody: conditionBody);
        }

        return null;
    }

    private static (string Expression, string? NestedSource, string? NestedDest, bool IsCollection, bool IsArray, string? ItemExpression, string? RawExpression, string? Condition) WrapWithNestedMapper(
        string expression, ITypeSymbol sourceType, ITypeSymbol destType, Location? profileLoc, Action<Diagnostic> reportDiag, HashSet<(ITypeSymbol, ITypeSymbol)> stack, string targetName)
    {
        var sBase = UnwrapNullable(sourceType);
        var dBase = UnwrapNullable(destType);
        string? cond = null;

        if (destType.SpecialType == SpecialType.System_String)
        {
            if (sourceType.SpecialType == SpecialType.System_String)
            {
                if (sourceType.NullableAnnotation == NullableAnnotation.Annotated && destType.NullableAnnotation == NullableAnnotation.NotAnnotated)
                {
                    return ($"{expression} ?? \"\"", null, null, false, false, null, expression, cond);
                }
                return (expression, null, null, false, false, null, expression, cond);
            }

            if (sourceType.IsReferenceType || IsNullableStruct(sourceType))
            {
                return ($"{expression}?.ToString() ?? \"\"", null, null, false, false, null, expression, cond);
            }
            else
            {
                return ($"{expression}.ToString()", null, null, false, false, null, expression, cond);
            }
        }

        if (IsTypeCompatible(sourceType, destType) && !IsCollection(destType, out _) && !IsDictionary(destType, out _, out _))
        {
            if (IsNullableStruct(sourceType) && !IsNullableStruct(destType))
            {
                return ($"{expression}.GetValueOrDefault()", null, null, false, false, null, expression, cond);
            }
            if (sBase.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) != dBase.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) && IsNumeric(sBase) && IsNumeric(dBase))
            {
                return ($"({destType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){expression}", null, null, false, false, null, expression, cond);
            }
            return (expression, null, null, false, false, null, expression, cond);
        }

        if (destType.TypeKind == TypeKind.Enum && IsNumeric(sourceType))
        {
            return ($"({destType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){expression}", null, null, false, false, null, expression, cond);
        }

        if (sourceType.TypeKind == TypeKind.Enum && IsNumeric(destType))
        {
            return ($"({destType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){expression}", null, null, false, false, null, expression, cond);
        }

        if (sourceType.TypeKind == TypeKind.Enum && destType.TypeKind == TypeKind.Enum)
        {
            return ($"({destType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){expression}", null, null, false, false, null, expression, cond);
        }

        // Dictionary logic
        if (IsDictionary(sourceType, out var sKey, out var sValue) && IsDictionary(destType, out var dKey, out var dValue))
        {
            var keyMap = WrapWithNestedMapper("x.Key", sKey, dKey, profileLoc, reportDiag, stack, "Key");
            var valueMap = WrapWithNestedMapper("x.Value", sValue, dValue, profileLoc, reportDiag, stack, "Value");
            return ($"({expression} ?? new()).ToDictionary(x => {keyMap.Expression}, x => {valueMap.Expression})", null, GetDisplayString(destType), false, false, null, expression, cond);
        }

        // Recursion / Collection logic
        if (IsCollection(sourceType, out var sItem) && IsCollection(destType, out var dItem))
        {
            var isArr = destType.TypeKind == TypeKind.Array || destType.ToDisplayString().Contains("[]");
            var itemMap = WrapWithNestedMapper("x", sItem, dItem, profileLoc, reportDiag, stack, targetName + "Item");

            string linq;
            string sItemName = GetDisplayString(sItem);
            string cast = $"(global::System.Collections.Generic.IEnumerable<{sItemName}>?)";
            if (itemMap.Expression == "x")
            {
                linq = $"({cast}{expression} ?? global::System.Array.Empty<{sItemName}>())";
            }
            else
            {
                linq = $"({cast}{expression} ?? global::System.Array.Empty<{sItemName}>()).Select(x => {itemMap.Expression})";
            }

            string toContainer = "";
            string finalExpr = "";

            if (destType.Name == "Stack")
                finalExpr = $"new global::System.Collections.Generic.Stack<{GetDisplayString(dItem)}>({linq})";
            else if (destType.Name == "Queue")
                finalExpr = $"new global::System.Collections.Generic.Queue<{GetDisplayString(dItem)}>({linq})";
            else if (destType.Name == "HashSet" || destType.Name == "ISet")
                finalExpr = $"new global::System.Collections.Generic.HashSet<{GetDisplayString(dItem)}>({linq})";
            else if (destType.Name == "ReadOnlyCollection")
                finalExpr = $"{linq}.ToList().AsReadOnly()";
            else if (isArr)
            {
                toContainer = ".ToArray()";
                finalExpr = $"{linq}{toContainer}";
            }
            else
            {
                toContainer = ".ToList()";
                finalExpr = $"{linq}{toContainer}";
            }

            return (finalExpr, GetDisplayString(sItem), GetDisplayString(dItem), false, false, null, expression, cond);
        }

        var key = (sBase, dBase);
        if (stack.Contains(key))
        {
            reportDiag(Diagnostic.Create(AutoMappicDiagnostics.CircularReference, profileLoc ?? Location.None, sBase.Name, dBase.Name));
        }
        else
        {
            // run dry validation to detect deeper indirect cycles
            Resolve(sBase, dBase, new Dictionary<string, (string? Expression, string? Condition, bool IsAsync)>(), Array.Empty<string>(), profileLoc, reportDiag, stack, null, null);
        }

        if (destType.NullableAnnotation == NullableAnnotation.NotAnnotated && !destType.IsValueType && sourceType.NullableAnnotation == NullableAnnotation.Annotated)
        {
            cond = $"{expression} != null";
            return ($"{expression}.MapTo{destType.Name}()", null, GetDisplayString(destType), false, false, null, expression, cond);
        }

        return ($"({expression} == null ? default! : {expression}.MapTo{destType.Name}())", null, GetDisplayString(destType), false, false, null, expression, cond);
    }

    private static ITypeSymbol UnwrapNullable(ITypeSymbol type)
    {
        if (IsNullableStruct(type) && type is INamedTypeSymbol named && named.TypeArguments.Length == 1)
        {
            return named.TypeArguments[0];
        }
        return type.WithNullableAnnotation(NullableAnnotation.None);
    }

    private static bool IsNullableStruct(ITypeSymbol type)
    {
        return type.IsValueType && type.NullableAnnotation == NullableAnnotation.Annotated;
    }

    private static List<ISymbol> GetReadableMembers(ITypeSymbol type)
    {
        var list = new List<ISymbol>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var current = type;
        while (current is not null)
        {
            foreach (var m in current.GetMembers())
            {
                if (m.IsStatic || m.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }
                if (m is IPropertySymbol p && p.GetMethod is not null)
                {
                    if (seen.Add(p.Name)) list.Add(p);
                }
                else if (m is IFieldSymbol f)
                {
                    if (seen.Add(f.Name)) list.Add(f);
                }
            }
            current = current.BaseType;
        }
        return list;
    }

    private static IEnumerable<IMethodSymbol> GetAllZeroArgMethods(ITypeSymbol type)
    {
        var current = type;
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (current is not null)
        {
            foreach (var m in current.GetMembers().OfType<IMethodSymbol>())
            {
                if (m.IsStatic || m.MethodKind != MethodKind.Ordinary || m.Parameters.Length != 0 || m.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }
                if (seen.Add(m.Name)) yield return m;
            }
            current = current.BaseType;
        }
    }

    private static Dictionary<string, ISymbol> GetAllWritableMembers(ITypeSymbol type)
    {
        var dict = new Dictionary<string, ISymbol>(StringComparer.Ordinal);
        var current = type;
        while (current is not null)
        {
            foreach (var m in current.GetMembers())
            {
                if (m.IsStatic || dict.ContainsKey(m.Name) || m.DeclaredAccessibility != Accessibility.Public)
                {
                    continue;
                }
                if (m is IPropertySymbol p && ((p.SetMethod is not null && (p.SetMethod.DeclaredAccessibility == Accessibility.Public || p.SetMethod.IsInitOnly)) || IsCollection(p.Type, out _) || IsDictionary(p.Type, out _, out _)))
                {
                    dict.Add(p.Name, p);
                }
                else if (m is IFieldSymbol f && !f.IsReadOnly)
                {
                    dict.Add(f.Name, f);
                }
            }
            current = current.BaseType;
        }
        return dict;
    }

    private static bool IsTypeCompatible(ITypeSymbol source, ITypeSymbol dest)
    {
        var sBase = UnwrapNullable(source);
        var dBase = UnwrapNullable(dest);

        var s = sBase.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var d = dBase.WithNullableAnnotation(NullableAnnotation.None).ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        if (s == d || (IsNumeric(sBase) && IsNumeric(dBase)))
        {
            return true;
        }
        return false;
    }

    private static bool IsDictionary(ITypeSymbol type, out ITypeSymbol keyType, out ITypeSymbol valueType)
    {
        keyType = null!;
        valueType = null!;
        if (type is INamedTypeSymbol named && named.IsGenericType && named.TypeArguments.Length == 2)
        {
            if (named.Name == "Dictionary" || named.Name == "IDictionary" || named.Name == "IReadOnlyDictionary")
            {
                keyType = named.TypeArguments[0];
                valueType = named.TypeArguments[1];
                return true;
            }
        }
        foreach (var inf in type.AllInterfaces)
        {
            if (inf.IsGenericType && inf.TypeArguments.Length == 2 && inf.Name.Contains("Dictionary"))
            {
                keyType = inf.TypeArguments[0];
                valueType = inf.TypeArguments[1];
                return true;
            }
        }
        return false;
    }

    private static bool IsCollection(ITypeSymbol type, out ITypeSymbol itemType)
    {
        itemType = null!;
        if (type.SpecialType == SpecialType.System_String)
        {
            return false;
        }

        if (type is IArrayTypeSymbol array)
        {
            itemType = array.ElementType;
            return true;
        }
        if (type is INamedTypeSymbol named)
        {
            if (named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                return false;
            }

            if (named.IsGenericType && named.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
            {
                itemType = named.TypeArguments[0];
                return true;
            }

            foreach (var i in named.AllInterfaces)
            {
                if (i.IsGenericType && i.OriginalDefinition.SpecialType == SpecialType.System_Collections_Generic_IEnumerable_T)
                {
                    itemType = i.TypeArguments[0];
                    return true;
                }
            }
        }
        return false;
    }


    private static string Sanitise(string name)
    {
        var res = new System.Text.StringBuilder();
        foreach (var c in name)
        {
            if (char.IsLetterOrDigit(c))
            {
                res.Append(c);
            }
            else
            {
                res.Append('_');
            }
        }
        return res.ToString();
    }

    private static ITypeSymbol GetMemberType(ISymbol symbol) => symbol switch { IPropertySymbol p => p.Type, IFieldSymbol f => f.Type, _ => throw new InvalidOperationException() };
    private static string GetDisplayString(ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    private static string? ResolveFlattenedPath(ITypeSymbol source, string name, string? sEnv, string? dEnv, ITypeSymbol destType)
    {
        return TryMatchFlattenedPath(source, name, "", "", destType);
    }

    private static string? TryMatchFlattenedPath(ITypeSymbol currentType, string targetName, string currentPath, string currentNameMatch, ITypeSymbol destType, int depth = 0)
    {
        if (depth > 6) return null; // Safety limit

        foreach (var m in GetReadableMembers(currentType))
        {
            // Do not allow single-level exact matches in flatten logic since they are Direct Matches
            if (depth == 0 && string.Equals(m.Name, targetName, StringComparison.OrdinalIgnoreCase)) continue;

            var nextNameMatch = currentNameMatch + m.Name;
            var nextPath = string.IsNullOrEmpty(currentPath) ? m.Name : $"{currentPath}?.{m.Name}";

            if (string.Equals(nextNameMatch, targetName, StringComparison.OrdinalIgnoreCase))
            {
                var t = GetMemberType(m);
                var defaultVal = $"default({GetDisplayString(t)})!";
                if (destType.SpecialType == SpecialType.System_String && destType.NullableAnnotation == NullableAnnotation.NotAnnotated)
                {
                    defaultVal = "\"\"";
                }
                return $"{nextPath} ?? {defaultVal}";
            }

            var tMember = GetMemberType(m);
            if (tMember.TypeKind == TypeKind.Class && tMember.SpecialType == SpecialType.None)
            {
                var result = TryMatchFlattenedPath(tMember, targetName, nextPath, nextNameMatch, destType, depth + 1);
                if (result != null) return result;
            }
        }
        return null;
    }

    private static bool IsNumeric(ITypeSymbol type)
    {
        var unwrapped = UnwrapNullable(type);
        return unwrapped.SpecialType == SpecialType.System_Int16 || unwrapped.SpecialType == SpecialType.System_Int32 || unwrapped.SpecialType == SpecialType.System_Int64 ||
               unwrapped.SpecialType == SpecialType.System_Decimal || unwrapped.SpecialType == SpecialType.System_Double || unwrapped.SpecialType == SpecialType.System_Single ||
               unwrapped.SpecialType == SpecialType.System_Byte || unwrapped.SpecialType == SpecialType.System_UInt16 || unwrapped.SpecialType == SpecialType.System_UInt32 || unwrapped.SpecialType == SpecialType.System_UInt64;
    }

    private sealed class TypePairComparer : IEqualityComparer<(ITypeSymbol, ITypeSymbol)>
    {
        public bool Equals((ITypeSymbol, ITypeSymbol) x, (ITypeSymbol, ITypeSymbol) y) => SymbolEqualityComparer.Default.Equals(x.Item1, y.Item1) && SymbolEqualityComparer.Default.Equals(x.Item2, y.Item2);
        public int GetHashCode((ITypeSymbol, ITypeSymbol) obj) => (SymbolEqualityComparer.Default.GetHashCode(obj.Item1) * 397) ^ SymbolEqualityComparer.Default.GetHashCode(obj.Item2);
    }
}
