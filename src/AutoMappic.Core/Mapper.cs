using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AutoMappic;

#nullable enable

/// <summary>
///   Runtime fallback implementation of <see cref="IMapper" />.
/// </summary>
public sealed class Mapper : IMapper, IDisposable
{
    // Signatures take (mapper, source, destination) -> result
    private readonly Dictionary<(Type Source, Type Destination), (IMappingExpression Mapping, Func<Mapper, object, object?, global::System.Threading.Tasks.Task<object>> Delegate)> _maps
        = new();
    // Per-async-context stack to detect circular references in runtime fallback
    private readonly global::System.Threading.AsyncLocal<HashSet<object>> _mappingStack = new();
    private bool _disposed;
    /// <inheritdoc />
    public IConfigurationProvider ConfigurationProvider { get; private set; } = null!;

    /// <summary>
    ///   Initialises the mapper from a collection of <see cref="Profile" /> instances and global configuration.
    /// </summary>
    internal Mapper(IEnumerable<Profile> profiles, IConfigurationProvider config)
    {
        ConfigurationProvider = config;
        foreach (var profile in profiles)
        {
            foreach (var mapping in profile.Mappings)
            {
                var key = (mapping.SourceType, mapping.DestinationType);
                _maps[key] = (mapping, BuildFallbackDelegate(mapping));
            }
        }
    }

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("Object mapping via runtime Mapper requires reflection.")]
    [RequiresDynamicCode("Object mapping via runtime Mapper requires dynamic code generation.")]
    public TDestination Map<TDestination>(object source)
    {
        if (source is null) return default!;
        return (TDestination)MapCore(source.GetType(), typeof(TDestination), source, null);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("Object mapping via runtime Mapper requires reflection.")]
    [RequiresDynamicCode("Object mapping via runtime Mapper requires dynamic code generation.")]
    public TDestination Map<TSource, TDestination>(TSource source)
    {
        if (source is null) return default!;
        return (TDestination)MapCore(typeof(TSource), typeof(TDestination), source, null);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("Object mapping via runtime Mapper requires reflection.")]
    [RequiresDynamicCode("Object mapping via runtime Mapper requires dynamic code generation.")]
    public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        return (TDestination)MapCore(typeof(TSource), typeof(TDestination), source, destination);
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("Object mapping via runtime Mapper requires reflection.")]
    [RequiresDynamicCode("Object mapping via runtime Mapper requires dynamic code generation.")]
    public async global::System.Threading.Tasks.Task<TDestination> MapAsync<TDestination>(object source, global::System.Threading.CancellationToken ct = default)
    {
        try
        {
            if (source is null) return default!;
            return (TDestination)await MapCoreAsync(source.GetType(), typeof(TDestination), source, null).ConfigureAwait(false);
        }
        catch (global::System.Exception ex)
        {
            return await global::System.Threading.Tasks.Task.FromException<TDestination>(ex).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("Object mapping via runtime Mapper requires reflection.")]
    [RequiresDynamicCode("Object mapping via runtime Mapper requires dynamic code generation.")]
    public async global::System.Threading.Tasks.Task<TDestination> MapAsync<TSource, TDestination>(TSource source, global::System.Threading.CancellationToken ct = default)
    {
        try
        {
            if (source is null) return default!;
            return (TDestination)await MapCoreAsync(typeof(TSource), typeof(TDestination), source, null).ConfigureAwait(false);
        }
        catch (global::System.Exception ex)
        {
            return await global::System.Threading.Tasks.Task.FromException<TDestination>(ex).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    [RequiresUnreferencedCode("Object mapping via runtime Mapper requires reflection.")]
    [RequiresDynamicCode("Object mapping via runtime Mapper requires dynamic code generation.")]
    public async global::System.Threading.Tasks.Task<TDestination> MapAsync<TSource, TDestination>(TSource source, TDestination destination, global::System.Threading.CancellationToken ct = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(destination);
            return (TDestination)await MapCoreAsync(typeof(TSource), typeof(TDestination), source, destination).ConfigureAwait(false);
        }
        catch (global::System.Exception ex)
        {
            return await global::System.Threading.Tasks.Task.FromException<TDestination>(ex).ConfigureAwait(false);
        }
    }

    /// <summary>Internal core mapping logic used for recursive resolution and fallback mapping.</summary>
    public object MapCore(Type sourceType, Type destType, object source, object? destination)
    {
        return MapCoreAsync(sourceType, destType, source, destination).GetAwaiter().GetResult();
    }

    /// <summary>Asynchronous core mapping logic.</summary>
    public async global::System.Threading.Tasks.Task<object> MapCoreAsync(Type sourceType, Type destType, object source, object? destination)
    {
        if (destType.IsAssignableFrom(sourceType))
        {
            return source!;
        }

        var sourceUnderlying = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
        var destUnderlying = Nullable.GetUnderlyingType(destType) ?? destType;

        if (destType == typeof(string))
        {
            return (source?.ToString() ?? string.Empty)!;
        }

        if (destUnderlying.IsPrimitive && sourceUnderlying.IsPrimitive)
        {
            try { return Convert.ChangeType(source!, destUnderlying!, System.Globalization.CultureInfo.InvariantCulture)!; } catch { /* Fallthrough */ }
        }

        if (IsCollection(destType, out var destItemType) && IsCollection(sourceType, out var sourceItemType))
        {
            var sourceList = (System.Collections.IEnumerable)source!;
            var resultList = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(destItemType!))!;

            foreach (var item in sourceList!)
            {
                if (item is null)
                {
                    resultList.Add(null);
                    continue;
                }

                if (destItemType.IsAssignableFrom(item.GetType()))
                {
                    resultList.Add(item);
                }
                else
                {
                    try
                    {
                        var mapped = MapCore(item.GetType(), destItemType, item, null);
                        resultList.Add(mapped);
                    }
                    catch (AutoMappicException ex)
                    {
                        throw new AutoMappicException(
                            $"AutoMappic: Failed to map a collection item of type '{item.GetType().FullName}' to '{destItemType.FullName}'. "
                            + $"Ensure a CreateMap<{item.GetType().Name}, {destItemType.Name}>() exists in a Profile. Inner: {ex.Message}", ex);
                    }
                }
            }

            if (destType.IsArray)
            {
                var array = Array.CreateInstance(destItemType, resultList.Count);
                resultList.CopyTo(array, 0);
                return array;
            }
            return resultList;
        }

        var key = (sourceType, destType);
        if (!_maps.TryGetValue(key, out var entry))
        {
            // NEW in v0.7.0: Hot Reload Fallback - check for registered shims
            if (HotReloadRegistry.TryGetShim(sourceType, destType, out var shim) && shim != null)
            {
                // Execute the fast shim directly!
                // Most shims match (mapper, source) or (mapper, source, dest)
                try
                {
                    if (shim is Func<Mapper, object, object> fastShim)
                    {
                        return fastShim(this, source);
                    }
                    if (shim is Func<Mapper, object, object?, object> fastShimWithDest)
                    {
                        return fastShimWithDest(this, source, destination);
                    }
                }
                catch { /* Fallback to standard if shim fails */ }
            }

            throw new AutoMappicException(
                $"No mapping registered from '{sourceType.FullName}' to '{destType.FullName}'. " +
                $"Ensure a Profile.CreateMap<{sourceType.Name}, {destType.Name}>() call exists " +
                $"and the generator has run, or register the mapping explicitly.");
        }

        var currentStack = _mappingStack.Value;
        if (currentStack == null)
        {
            currentStack = new HashSet<object>(ReferenceEqualityComparer.Instance);
            _mappingStack.Value = currentStack;
        }

        if (!currentStack.Add(source!))
        {
            throw new AutoMappicException($"Circular reference detected in runtime mapping for '{sourceType.Name}' -> '{destType.Name}'. Runtime tracking prevents StackOverflow.");
        }

        try
        {
            var result = await entry.Delegate(this, source!, destination).ConfigureAwait(false);
            await entry.Mapping.ExecuteAfterAsync(source!, result).ConfigureAwait(false);
            return result;
        }
        finally
        {
            _mappingStack.Value?.Remove(source!);
        }
    }

    private static Func<Mapper, object, object?, global::System.Threading.Tasks.Task<object>> BuildFallbackDelegate(IMappingExpression mapping)
    {
        if (mapping.ConverterType != null)
        {
            var converter = Activator.CreateInstance(mapping.ConverterType);
            var method = mapping.ConverterType.GetMethod("Convert")!;
            return (mapper, src, dst) => global::System.Threading.Tasks.Task.FromResult(method.Invoke(converter, new[] { src })!);
        }

        var sourceProps = mapping.SourceType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .GroupBy(p => p.Name)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        var destProps = mapping.DestinationType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite || IsCollection(p.PropertyType, out _))
            .ToArray();

        var sourceMethods = mapping.SourceType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.ReturnType != typeof(void) && m.GetParameters().Length == 0 && m.Name.StartsWith("Get", StringComparison.Ordinal))
            .GroupBy(m => m.Name.Substring(3))
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        return async (mapper, src, dst) =>
        {
            if (dst == null)
            {
                if (mapping.ConstructionFactory != null)
                {
                    dst = mapping.ConstructionFactory.DynamicInvoke(src);
                }
                else
                {
                    dst = Activator.CreateInstance(mapping.DestinationType)
                        ?? throw new AutoMappicException($"Could not create an instance of '{mapping.DestinationType.FullName}'.");
                }
            }

            await mapping.ExecuteBeforeAsync(src!, dst!).ConfigureAwait(false);

            foreach (var destProp in destProps)
            {
                if (mapping.IgnoredMembers.Contains(destProp.Name))
                {
                    continue;
                }

                if (mapping.RuntimeConditions.TryGetValue(destProp.Name, out var condition))
                {
                    if (!(bool)condition.DynamicInvoke(src, dst)!)
                    {
                        continue;
                    }
                }

                if (mapping.RuntimeMaps.TryGetValue(destProp.Name, out var runtimeMap))
                {
                    var customVal = runtimeMap(src!);
                    destProp.SetValue(dst, customVal);
                    continue;
                }

                if (src is System.Collections.IDictionary dict && IsDictionary(mapping.SourceType, out var kType, out var vType) && kType == typeof(string))
                {
                    var parts = mapping.DestinationNaming?.Split(destProp.Name) ?? new[] { destProp.Name };
                    var keyName = JoinName(parts, mapping.SourceNaming);
                    if (dict.Contains(keyName))
                    {
                        var dictVal = dict[keyName];
                        if (dictVal != null && !destProp.PropertyType.IsAssignableFrom(dictVal.GetType()))
                        {
                            dictVal = mapper.MapCore(dictVal.GetType(), destProp.PropertyType, dictVal, null);
                        }
                        destProp.SetValue(dst, dictVal);
                        continue;
                    }
                }

                PropertyInfo? srcProp = null;
                if (!sourceProps.TryGetValue(destProp.Name, out srcProp))
                {
                    var matches = sourceProps.Values.Where(p =>
                        string.Equals(Normalize(p.Name, mapping.SourceNaming), Normalize(destProp.Name, mapping.DestinationNaming), StringComparison.OrdinalIgnoreCase)).ToList();
                    if (matches.Count > 1)
                    {
                        throw new AutoMappicException($"Ambiguous mapping for '{destProp.Name}'. Multiple source properties match: '{matches[0].Name}' and '{matches[1].Name}'. Use an explicit MapFrom or Ignore rule.");
                    }

                    srcProp = matches.FirstOrDefault();
                }

                if (srcProp != null)
                {
                    var val = srcProp.GetValue(src);
                    if (val is null)
                    {
                        destProp.SetValue(dst, null);
                        continue;
                    }

                    if (destProp.PropertyType.IsAssignableFrom(srcProp.PropertyType))
                    {
                        var isSimple = destProp.PropertyType.IsValueType || destProp.PropertyType == typeof(string);
                        if (isSimple)
                        {
                            if (destProp.CanWrite) destProp.SetValue(dst, val);
                        }
                        else if (!IsCollection(destProp.PropertyType, out _))
                        {
                            var mappedVal = mapper.MapCore(val.GetType(), destProp.PropertyType, val, null);
                            destProp.SetValue(dst, mappedVal);
                        }
                        else if (IsCollection(destProp.PropertyType, out _))
                        {
                            var targetColl = destProp.GetValue(dst) as System.Collections.IList;
                            if (targetColl != null)
                            {
                                targetColl.Clear();
                                foreach (var item in (System.Collections.IEnumerable)val) targetColl.Add(item);
                            }
                        }
                    }
                    else if (IsDictionary(destProp.PropertyType, out var dK, out var dV) && IsDictionary(srcProp.PropertyType, out var sK, out var sV))
                    {
                        var sourceDict = (System.Collections.IDictionary)val;
                        var resultDict = (System.Collections.IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(dK, dV))!;

                        foreach (System.Collections.DictionaryEntry entry in sourceDict)
                        {
                            var entryKey = entry.Key;
                            var entryVal = entry.Value;

                            object? mappedKey = entryKey;
                            if (entryKey != null && !dK.IsAssignableFrom(entryKey.GetType()))
                            {
                                if (dK == typeof(string)) mappedKey = entryKey.ToString();
                                else mappedKey = mapper.MapCore(entryKey.GetType(), dK, entryKey, null);
                            }

                            object? mappedVal = entryVal;
                            if (entryVal != null && !dV.IsAssignableFrom(entryVal.GetType()))
                            {
                                if (dV == typeof(string)) mappedVal = entryVal.ToString();
                                else mappedVal = mapper.MapCore(entryVal.GetType(), dV, entryVal, null);
                            }

                            resultDict.Add(mappedKey!, mappedVal);
                        }
                        destProp.SetValue(dst, resultDict);
                    }
                    else
                    {
                        try
                        {
                            if (!destProp.CanWrite && IsCollection(destProp.PropertyType, out _))
                            {
                                var targetColl = destProp.GetValue(dst) as System.Collections.IList;
                                if (targetColl != null)
                                {
                                    var items = mapper.MapCore(srcProp.PropertyType, destProp.PropertyType, val, null) as System.Collections.IEnumerable;
                                    if (items != null)
                                    {
                                        targetColl.Clear();
                                        foreach (var item in items) targetColl.Add(item);
                                    }
                                }
                            }
                            else
                            {
                                var nested = mapper.MapCore(srcProp.PropertyType, destProp.PropertyType, val, null);
                                destProp.SetValue(dst, nested);
                            }
                        }
                        catch (AutoMappicException ex)
                        {
                            throw new AutoMappicException(
                                $"AutoMappic: Failed to map property '{destProp.Name}' on '{mapping.DestinationType.FullName}'. "
                                + $"No mapping found for type '{srcProp!.PropertyType.FullName}' -> '{destProp.PropertyType.FullName}'. "
                                + $"Add a CreateMap or ForMember rule. Inner: {ex.Message}", ex);
                        }
                        catch (Exception ex)
                        {
                            throw new AutoMappicException(
                                $"AutoMappic: Unexpected error assigning property '{destProp.Name}' on '{mapping.DestinationType.FullName}'. "
                                + $"Source type: '{srcProp!.PropertyType.FullName}'. Check that the property has a public setter and the types are compatible.", ex);
                        }
                    }
                }
                else if (sourceMethods.TryGetValue(destProp.Name, out var srcMethod))
                {
                    destProp.SetValue(dst, srcMethod.Invoke(src, null));
                }
                else
                {
                    // Fallback to Flattening check
                    var flattenedValue = ResolveFlattenedValue(src!, destProp.Name, mapping.SourceNaming, mapping.DestinationNaming);
                    if (flattenedValue != null)
                    {
                        destProp.SetValue(dst, flattenedValue);
                    }
                }
            }

            return dst!;
        };
    }

    private static object? ResolveFlattenedValue(object source, string destName, INamingConvention? sourceNaming, INamingConvention? destNaming)
    {
        var props = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var destParts = destNaming?.Split(destName) ?? new[] { destName };

        if (destParts.Length == 0) return null;

        for (int i = 1; i <= destParts.Length; i++)
        {
            var segment = string.Concat(destParts.Take(i));
            foreach (var prop in props)
            {
                if (string.Equals(Normalize(prop.Name, sourceNaming), Normalize(segment, destNaming), StringComparison.OrdinalIgnoreCase))
                {
                    var val = prop.GetValue(source);
                    if (val == null) return null;

                    if (i == destParts.Length) return val;

                    var remaining = string.Concat(destParts.Skip(i));
                    var result = ResolveFlattenedValue(val, remaining, sourceNaming, destNaming);
                    if (result != null) return result;
                }
            }
        }

        return null;
    }

    private static bool IsDictionary(Type type, out Type keyType, out Type valueType)
    {
        keyType = null!;
        valueType = null!;

        var dictIntf = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>)
            ? type
            : type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

        if (dictIntf != null)
        {
            keyType = dictIntf.GetGenericArguments()[0];
            valueType = dictIntf.GetGenericArguments()[1];
            return true;
        }

        // Support non-generic IDictionary too
        if (typeof(System.Collections.IDictionary).IsAssignableFrom(type) && type.IsGenericType && type.GetGenericArguments().Length == 2)
        {
            keyType = type.GetGenericArguments()[0];
            valueType = type.GetGenericArguments()[1];
            return true;
        }

        return false;
    }

    private static bool IsCollection(Type type, out Type itemType)
    {
        itemType = null!;
        if (type.IsArray)
        {
            itemType = type.GetElementType()!;
            return true;
        }

        if (type.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(type))
        {
            itemType = type.GetGenericArguments()[0];
            return true;
        }

        return false;
    }

    private static string JoinName(string[] parts, INamingConvention? conv)
    {
        if (conv is LowerUnderscoreNamingConvention) return string.Join("_", parts).ToLowerInvariant();
        if (conv is KebabCaseNamingConvention) return string.Join("-", parts).ToLowerInvariant();
        if (conv is CamelCaseNamingConvention)
        {
            if (parts.Length == 0) return string.Empty;
            return parts[0].ToLowerInvariant() + string.Concat(parts.Skip(1).Select(p =>
                p.Length > 0 ? char.ToUpperInvariant(p[0]) + p.Substring(1).ToLowerInvariant() : string.Empty));
        }

        // Default to Pascal
        return string.Concat(parts.Select(p =>
            p.Length > 0 ? char.ToUpperInvariant(p[0]) + p.Substring(1).ToLowerInvariant() : string.Empty));
    }

    private static string Normalize(string name, INamingConvention? conv = null)
    {
        if (string.IsNullOrEmpty(name)) return name;
        if (conv == null) return name.Replace("-", "").Replace("_", "");
        return string.Concat(conv.Split(name));
    }
}
