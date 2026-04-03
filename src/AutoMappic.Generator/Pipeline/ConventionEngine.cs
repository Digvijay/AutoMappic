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
        Location? destMemberLocation,
        Action<DiagnosticInfo> reportDiagnostic,
        HashSet<(ITypeSymbol, ITypeSymbol)>? mappingStack = null,
        string? sourceNaming = null,
        string? destNaming = null,
        bool identityManagementEnabled = false,
        bool isProjection = false)
    {
        var properties = new List<PropertyMap>();
        var constructorArgs = new List<PropertyMap>();
        mappingStack ??= new HashSet<(ITypeSymbol, ITypeSymbol)>(new TypePairComparer());

        var key = (UnwrapNullable(source), UnwrapNullable(destination));
        if (mappingStack.Contains(key))
        {
            reportDiagnostic(DiagnosticInfo.Create(AutoMappicDiagnostics.CircularReference, destMemberLocation ?? profileLocation ?? Location.None, source.Name, destination.Name));
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
                        var paramMap = ResolveSourceForMember(source, param.Name, param.Type, explicitMaps, ignoredMembers, profileLocation, param.Locations.FirstOrDefault(), reportDiagnostic, mappingStack, sourceNaming, destNaming, "source", destination.Name, identityManagementEnabled, isProjection);
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
                    reportDiagnostic(DiagnosticInfo.Create(AutoMappicDiagnostics.MissingConstructor, profileLocation ?? Location.None, destination.Name));
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

                var map = ResolveSourceForMember(source, memberName, GetMemberType(member), explicitMaps, ignoredMembers, profileLocation, member.Locations.FirstOrDefault(), reportDiagnostic, mappingStack, sourceNaming, destNaming, "source", destination.Name, identityManagementEnabled, isProjection);
                if (map is not null)
                {
                    bool isInit = false;
                    bool isReadOnly = false;
                    bool isRequired = false;
                    bool isKey = false;
                    if (member is IPropertySymbol p)
                    {
                        isInit = p.SetMethod?.IsInitOnly ?? false;
                        isReadOnly = p.SetMethod == null || p.SetMethod.DeclaredAccessibility != Accessibility.Public;
                        isRequired = p.IsRequired;
                    }
                    else if (member is IFieldSymbol f)
                    {
                        isReadOnly = f.IsReadOnly;
                        isRequired = f.IsRequired;
                    }

                    // AM0013: Patch Mode check for required properties
                    if (identityManagementEnabled && isRequired && map.SourceCanBeNull)
                    {
                        reportDiagnostic(DiagnosticInfo.Create(AutoMappicDiagnostics.PatchIntoRequired, member.Locations.FirstOrDefault() ?? Location.None, memberName, destination.Name, map.SourceRawExpression ?? "source"));
                    }

                    // Key detection: Attributes or naming convention
                    isKey = member.GetAttributes().Any(a => a.AttributeClass?.Name is "KeyAttribute" or "AutoMappicKeyAttribute" or "EntityKeyAttribute" or "Key");
                    if (!isKey)
                    {
                        isKey = string.Equals(memberName, "Id", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(memberName, destination.Name + "Id", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(memberName, "Key", StringComparison.OrdinalIgnoreCase) ||
                                memberName == "SSN" || memberName == "Code";
                    }

                    properties.Add(map with { IsInitOnly = isInit, IsReadOnly = isReadOnly, IsRequired = isRequired, IsKey = isKey });
                }
                else if (!constructorParamNames.Contains(memberName))
                {
                    if (source.Name != "IDataReader" && source.Name != "DataRow" && source.Name != "SqlDataReader")
                    {
                        var props = global::System.Collections.Immutable.ImmutableDictionary<string, string?>.Empty
                            .Add("TargetProperty", memberName);
                        var diagnosticLocation = profileLocation ?? (member.Locations.Length > 0 ? member.Locations[0] : Location.None);
                        reportDiagnostic(DiagnosticInfo.Create(AutoMappicDiagnostics.UnmappedProperty, diagnosticLocation, props, memberName, destination.Name, source.Name));
                    }
                }
            }

            if (properties.Count == 0 && constructorArgs.Count == 0)
            {
                reportDiagnostic(DiagnosticInfo.Create(AutoMappicDiagnostics.AsymmetricMapping, profileLocation ?? Location.None, source.Name, destination.Name));
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
        Location? destMemberLocation,
        Action<DiagnosticInfo> reportDiagnostic,
        HashSet<(ITypeSymbol, ITypeSymbol)> mappingStack,
        string? sourceNaming = null,
        string? destNaming = null,
        string sourceAccess = "source",
        string destTypeName = "Unknown",
        bool identityManagementEnabled = false,
        bool isProjection = false)
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
                var subMap = ResolveSourceForMember(element.Type, targetName, targetType, explicitMaps, Array.Empty<string>(), profileLocation, destMemberLocation, reportDiagnostic, mappingStack, sourceNaming, destNaming, $"{sourceAccess}.Item{i + 1}", destTypeName, identityManagementEnabled, isProjection);
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
                bool isColl = IsCollection(targetType, out _);
                if (isColl && (explicitData.Expression.Contains(".Select(") || explicitData.Expression.Contains(".Resolve(")))
                {
                    reportDiagnostic(DiagnosticInfo.Create(AutoMappicDiagnostics.PerformanceRegression, destMemberLocation ?? profileLocation ?? Location.None, source.Name, targetType.Name));
                }

                return new PropertyMap(targetName, explicitData.Expression, PropertyMapKind.Explicit, IsAsync: explicitData.IsAsync, ConditionBody: conditionBody, SourceCanBeNull: false, IsCollection: isColl);
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
                expr = $"{sourceAccess}.IsDBNull({ordinal}) ? (default!) : {expr}";

            return new PropertyMap(targetName, expr, PropertyMapKind.Direct, DataReaderColumn: keyName, ConditionBody: conditionBody, SourceCanBeNull: CanBeNull(targetType));
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
            var (nE, nS, nD, iC, iA, iE, rE, nCond, nKey, nKeyType, nKeyVal) = WrapWithNestedMapper(sourceExpr, srcValue, targetType, profileLocation, destMemberLocation, reportDiagnostic, mappingStack, targetName, identityManagementEnabled, 0, isProjection);
            return new PropertyMap(targetName, nE, PropertyMapKind.Direct, NestedSourceTypeFullName: nS, NestedDestTypeFullName: nD, NestedFullDestTypeFullName: targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), IsCollection: iC, IsArray: iA, NestedExpression: iE, SourceRawExpression: rE, ConditionBody: conditionBody ?? nCond, NestedDestKeyProperty: nKey, NestedDestKeyTypeFullName: nKeyType, IsNestedDestKeyValueType: nKeyVal, SourceCanBeNull: CanBeNull(srcValue));
        }

        // Discovery
        var readableMembers = GetReadableMembers(source);
        var directMatches = readableMembers.Where(m =>
            string.Equals(m.Name, targetName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(NamingUtility.Normalize(m.Name), NamingUtility.Normalize(targetName), StringComparison.OrdinalIgnoreCase) ||
            (sourceNaming?.Contains("Snake") == true && (string.Equals(m.Name, NamingUtility.ToSnakeCase(targetName), StringComparison.OrdinalIgnoreCase) || string.Equals(m.Name, "src_" + NamingUtility.ToSnakeCase(targetName), StringComparison.OrdinalIgnoreCase)))).ToList();

        var sourceMethods = GetAllZeroArgMethods(source);
        var methodMatches = sourceMethods.Where(m =>
            string.Equals(m.Name, "Get" + targetName, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(NamingUtility.Normalize(m.Name), NamingUtility.Normalize("Get" + targetName), StringComparison.OrdinalIgnoreCase)).ToList();

        // Flattening
        var flatPath = ResolveFlattenedPath(source, targetName, sourceNaming, destNaming, targetType);

        // Ambiguity Detection: Check if direct/method matches and a flattened path both exist
        if ((directMatches.Count + methodMatches.Count > 0) && flatPath != null)
        {
            reportDiagnostic(DiagnosticInfo.Create(AutoMappicDiagnostics.AmbiguousMapping, destMemberLocation ?? profileLocation ?? Location.None, targetName, destTypeName, source.Name));
        }

        // Return direct/method match if found
        ISymbol? directMatch = directMatches.FirstOrDefault();
        IMethodSymbol? methodMatch = methodMatches.FirstOrDefault();
        if (directMatch is not null || methodMatch is not null)
        {
            var sourceType = directMatch is not null ? GetMemberType(directMatch) : methodMatch!.ReturnType;
            var sourceExpr = directMatch is not null ? $"{sourceAccess}.{directMatch.Name}" : $"{sourceAccess}.{methodMatch!.Name}()";

            var (nestedExpr, nSrc, nDest, isColl, isArr, itemExpr, rawExpr, nCond, nKey, nKeyType, nKeyVal) = WrapWithNestedMapper(sourceExpr, sourceType, targetType, profileLocation, destMemberLocation, reportDiagnostic, mappingStack, targetName, identityManagementEnabled, 0, isProjection);
            return new PropertyMap(targetName, nestedExpr, PropertyMapKind.Direct,
                NestedSourceTypeFullName: nSrc, NestedDestTypeFullName: nDest,
                NestedFullDestTypeFullName: targetType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                IsCollection: isColl, IsArray: isArr, NestedExpression: itemExpr, SourceRawExpression: rawExpr, ConditionBody: conditionBody ?? nCond, NestedDestKeyProperty: nKey, NestedDestKeyTypeFullName: nKeyType, IsNestedDestKeyValueType: nKeyVal, SourceCanBeNull: CanBeNull(sourceType));
        }

        if (flatPath != null)
        {
            return new PropertyMap(targetName, $"{sourceAccess}.{flatPath}", PropertyMapKind.Flattened, ConditionBody: conditionBody, SourceCanBeNull: CanBeNull(targetType));
        }

        // AM0015 Smart-Match Fuzzy matching logic if no direct or flattened match found
        ISymbol? bestMember = null;
        double bestScore = 0;

        // Performance safeguard: Limit fuzzy matching to prevent pathological build times
        // on classes with hundreds of properties.
        if (readableMembers.Count <= 200)
        {
            foreach (var m in readableMembers)
            {
                // Heuristic: If name lengths differ by more than 2x, Similarity is unlikely to be >= 0.3
                if (Math.Abs(m.Name.Length - targetName.Length) > Math.Max(m.Name.Length, targetName.Length) / 2)
                    continue;

                double score = MappingFuzzer.GetSimilarity(m.Name, targetName);
                if (score >= 0.3 && score > bestScore)
                {
                    bestMember = m;
                    bestScore = score;
                }
            }
        }

        if (bestMember != null && IsTypeCompatible(GetMemberType(bestMember), targetType))
        {
            var props = global::System.Collections.Immutable.ImmutableDictionary<string, string?>.Empty
                .Add("SuggestedName", bestMember.Name)
                .Add("TargetProperty", targetName)
                .Add("Score", bestScore.ToString(global::System.Globalization.CultureInfo.InvariantCulture));
            reportDiagnostic(DiagnosticInfo.Create(AutoMappicDiagnostics.SmartMatchSuggestion, profileLocation ?? destMemberLocation ?? Location.None, props, targetName, destTypeName, bestMember.Name));
        }

        return null;
    }

    private static (string Expression, string? NestedSource, string? NestedDest, bool IsCollection, bool IsArray, string? ItemExpression, string? RawExpression, string? Condition, string? NestedDestKey, string? NestedDestKeyType, bool IsKeyValType) WrapWithNestedMapper(
        string expression, ITypeSymbol sourceType, ITypeSymbol destType, Location? profileLoc, Location? destMemberLocation, Action<DiagnosticInfo> reportDiag, HashSet<(ITypeSymbol, ITypeSymbol)> stack, string targetName, bool identityManagementEnabled = false, int nestedLevel = 0, bool isProjection = false)
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
                    return ($"{expression} ?? \"\"", null, null, false, false, null, expression, cond, null, null, false);
                }
                return (expression, null, null, false, false, null, expression, cond, null, null, false);
            }

            if (sourceType.IsReferenceType || IsNullableStruct(sourceType))
            {
                return ($"{expression}?.ToString() ?? \"\"", null, null, false, false, null, expression, cond, null, null, false);
            }
            else
            {
                return ($"{expression}.ToString()", null, null, false, false, null, expression, cond, null, null, false);
            }
        }

        if (IsTypeCompatible(sourceType, destType) && !IsCollection(destType, out _) && !IsDictionary(destType, out _, out _))
        {
            if (IsNullableStruct(sourceType) && !IsNullableStruct(destType))
            {
                return ($"{expression}.GetValueOrDefault()", null, null, false, false, null, expression, cond, null, null, false);
            }
            if (sBase.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) != dBase.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) && IsNumeric(sBase) && IsNumeric(dBase))
            {
                return ($"({destType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){expression}", null, null, false, false, null, expression, cond, null, null, false);
            }
            return (expression, null, null, false, false, null, expression, cond, null, null, false);
        }

        if (destType.TypeKind == TypeKind.Enum && IsNumeric(sourceType))
        {
            return ($"({destType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){expression}", null, null, false, false, null, expression, cond, null, null, false);
        }

        if (sourceType.TypeKind == TypeKind.Enum && IsNumeric(destType))
        {
            return ($"({destType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){expression}", null, null, false, false, null, expression, cond, null, null, false);
        }

        if (sourceType.TypeKind == TypeKind.Enum && destType.TypeKind == TypeKind.Enum)
        {
            return ($"({destType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}){expression}", null, null, false, false, null, expression, cond, null, null, false);
        }

        var sBaseForCirc = UnwrapNullable(sourceType);
        var dBaseForCirc = UnwrapNullable(destType);
        var keyForCirc = (sBaseForCirc, dBaseForCirc);
        if (stack.Contains(keyForCirc))
        {
            reportDiag(DiagnosticInfo.Create(AutoMappicDiagnostics.CircularReference, destMemberLocation ?? profileLoc ?? Location.None, sBaseForCirc.Name, dBaseForCirc.Name));
        }

        // Dictionary logic
        if (IsDictionary(sourceType, out var sKey, out var sValue) && IsDictionary(destType, out var dKey, out var dValue))
        {
            var keyMap = WrapWithNestedMapper("x.Key", sKey, dKey, profileLoc, destMemberLocation, reportDiag, stack, "Key", identityManagementEnabled, nestedLevel + 1, isProjection);
            var valueMap = WrapWithNestedMapper("x.Value", sValue, dValue, profileLoc, destMemberLocation, reportDiag, stack, "Value", identityManagementEnabled, nestedLevel + 1, isProjection);
            return ($"({expression} ?? new()).ToDictionary(x => {keyMap.Expression}, x => {valueMap.Expression})", null, GetDisplayString(destType), false, false, null, expression, cond, null, null, false);
        }

        // Recursion / Collection logic
        if (IsCollection(sourceType, out var sItem) && IsCollection(destType, out var dItem))
        {
            if (nestedLevel > 0)
            {
                reportDiag(DiagnosticInfo.Create(AutoMappicDiagnostics.PerformanceHotpath, destMemberLocation ?? profileLoc ?? Location.None, sBase.Name, dBase.Name));
            }

            var isArr = destType.TypeKind == TypeKind.Array || destType.ToDisplayString().Contains("[]");
            var itemMap = WrapWithNestedMapper("x", sItem, dItem, profileLoc, destMemberLocation, reportDiag, stack, targetName + "Item", identityManagementEnabled, nestedLevel + 1, isProjection);

            string linq;
            string sItemName = GetDisplayString(sItem);
            string cast = $"(global::System.Collections.Generic.IEnumerable<{sItemName}>?)";
            if (itemMap.Expression == "x")
            {
                var fallback = $"global::System.Array.Empty<{sItemName}>()";
                linq = $"({cast}{expression} ?? {fallback})";
            }
            else
            {
                var fallback = $"global::System.Array.Empty<{sItemName}>()";
                linq = $"({cast}{expression} ?? {fallback}).Select(x => {itemMap.Expression})";
            }

            string toContainer = "";
            string finalExpr = "";

            if (destType.Name == "Stack")
                finalExpr = $"new global::System.Collections.Generic.Stack<{GetDisplayString(dItem)}>({linq})";
            else if (destType.Name == "Queue")
                finalExpr = $"new global::System.Collections.Generic.Queue<{GetDisplayString(dItem)}>({linq})";
            else if (destType.Name == "HashSet" || destType.Name == "ISet")
                finalExpr = $"new global::System.Collections.Generic.HashSet<{GetDisplayString(dItem)}>({linq})";
            else if (isProjection)
            {
                if (isArr) finalExpr = $"{linq}.ToArray()";
                else if (destType.Name.Contains("List") || destType.Name.Contains("Collection")) finalExpr = $"{linq}.ToList()";
                else finalExpr = linq;
            }
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

            string? kPropName = null;
            string? kPropType = null;
            bool isKeyValType = false;
            if (dItem is INamedTypeSymbol nItem)
            {
                var dItemProps = GetReadableMembers(dItem);
                // Key detection: 1. Attributes ([Key] or [AutoMappicKey]) 2. Naming conventions (Id, TypeId)
                var innerKey = dItemProps.FirstOrDefault(p =>
                    p.GetAttributes().Any(a => a.AttributeClass?.Name is "KeyAttribute" or "AutoMappicKeyAttribute" or "EntityKeyAttribute" or "Key"));

                if (innerKey == null)
                {
                    innerKey = dItemProps.FirstOrDefault(p =>
                        string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.Name, nItem.Name + "Id", StringComparison.OrdinalIgnoreCase));
                }

                if (innerKey != null)
                {
                    kPropName = innerKey.Name;
                    var kType = GetMemberType(innerKey);
                    kPropType = kType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    isKeyValType = kType.IsValueType && !IsNullableStruct(kType);

                    var sItemProps = GetReadableMembers(sItem);
                    var sKeyProp = sItemProps.FirstOrDefault(p =>
                        string.Equals(p.Name, kPropName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(NamingUtility.Normalize(p.Name), NamingUtility.Normalize(kPropName), StringComparison.OrdinalIgnoreCase));

                    if (sKeyProp == null && !(sItem is INamedTypeSymbol ns && ns.Name == "IDataReader"))
                    {
                        var sourceItemTypeName = sItem is INamedTypeSymbol nSource ? nSource.Name : sItem.Name;
                        reportDiag(DiagnosticInfo.Create(AutoMappicDiagnostics.UnmappedPrimaryKey, destMemberLocation ?? profileLoc ?? Location.None, nItem.Name, sourceItemTypeName));
                        kPropName = null; // Prevent smart-sync mapping if key is not on source
                    }
                }
                else if (dItemProps.Count > 1 && dItemProps.Any(p => p.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase)))
                {
                    reportDiag(DiagnosticInfo.Create(AutoMappicDiagnostics.AmbiguousEntityKey, destMemberLocation ?? profileLoc ?? Location.None, nItem.Name, targetName));
                }
            }

            return (finalExpr, GetDisplayString(sItem), GetDisplayString(dItem), true, isArr, itemMap.Expression, expression, cond, kPropName, kPropType, isKeyValType);
        }

        var key = (sBase, dBase);
        if (stack.Contains(key))
        {
            reportDiag(DiagnosticInfo.Create(AutoMappicDiagnostics.CircularReference, destMemberLocation ?? profileLoc ?? Location.None, sBase.Name, dBase.Name));
        }
        else
        {
            // run dry validation to detect deeper indirect cycles
            Resolve(sBase, dBase, new Dictionary<string, (string? Expression, string? Condition, bool IsAsync)>(), Array.Empty<string>(), profileLoc, destMemberLocation, reportDiag, stack, null, null);
        }

        if (isProjection)
        {
            var (props, ctorArgs) = Resolve(sBase, dBase, new Dictionary<string, (string? Expression, string? Condition, bool IsAsync)>(), Array.Empty<string>(), profileLoc, destMemberLocation, reportDiag, stack, null, null, identityManagementEnabled, true);
            var initSb = new System.Text.StringBuilder();
            if (ctorArgs.Count > 0)
            {
                initSb.Append($"new {GetDisplayString(dBase)}(");
                initSb.Append(string.Join(", ", ctorArgs.Select(c => c.SourceExpression)));
                initSb.Append(')');
            }
            else
            {
                initSb.Append($"new {GetDisplayString(dBase)}()");
            }

            if (props.Any(p => p.Kind != PropertyMapKind.Ignored))
            {
                initSb.Append(" { ");
                initSb.Append(string.Join(", ", props.Where(p => p.Kind != PropertyMapKind.Ignored).Select(p => $"{p.DestinationProperty} = {p.SourceExpression}")));
                initSb.Append(" }");
            }

            var result = initSb.ToString();
            result = result!.Replace("source.", expression + ".");

            if (CanBeNull(sourceType) && !isProjection)
            {
                result = $"({expression} == null ? default! : {result})";
            }

            return (result, null, GetDisplayString(destType), false, false, null, expression, cond, null, null, false);
        }

        if (destType.NullableAnnotation == NullableAnnotation.NotAnnotated && !destType.IsValueType && sourceType.NullableAnnotation == NullableAnnotation.Annotated)
        {
            cond = $"{expression} != null";
            return ($"{expression}.MapTo{SourceEmitter.Sanitise(GetDisplayString(destType), true)}(context)", null, GetDisplayString(destType), false, false, null, expression, cond, null, null, false);
        }

        return ($"({expression} == null ? default! : {expression}.MapTo{SourceEmitter.Sanitise(GetDisplayString(destType), true)}(context))", null, GetDisplayString(destType), false, false, null, expression, cond, null, null, false);
    }

    private static ITypeSymbol UnwrapNullable(ITypeSymbol type)
    {
        if (IsNullableStruct(type) && type is INamedTypeSymbol named && named.TypeArguments.Length == 1)
        {
            return named.TypeArguments[0];
        }
        return type.WithNullableAnnotation(NullableAnnotation.None);
    }

    private static bool CanBeNull(ITypeSymbol type)
    {
        return !type.IsValueType || type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T || type.NullableAnnotation == NullableAnnotation.Annotated;
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
                var fallback = t.SpecialType == SpecialType.System_String ? "\"\"" : "(default!)";
                return $"{nextPath} ?? {fallback}";
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
