using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AutoMappic;

/// <summary>
///   Runtime fallback implementation of <see cref="IMapper" />.
/// </summary>
public sealed class Mapper : IMapper
{
    // Signatures take (mapper, source, destination) -> result
    private readonly Dictionary<(Type Source, Type Destination), Func<Mapper, object, object?, object>> _maps
        = new();

    /// <summary>
    ///   Initialises the mapper from a collection of <see cref="Profile" /> instances.
    /// </summary>
    internal Mapper(IEnumerable<Profile> profiles)
    {
        foreach (var profile in profiles)
        {
            foreach (var mapping in profile.Mappings)
            {
                var key = (mapping.SourceType, mapping.DestinationType);
                _maps[key] = BuildFallbackDelegate(mapping);
            }
        }
    }

    /// <inheritdoc />
    public TDestination Map<TDestination>(object source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return (TDestination)MapCore(source.GetType(), typeof(TDestination), source, null);
    }

    /// <inheritdoc />
    public TDestination Map<TSource, TDestination>(TSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return (TDestination)MapCore(typeof(TSource), typeof(TDestination), source, null);
    }

    /// <inheritdoc />
    public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        return (TDestination)MapCore(typeof(TSource), typeof(TDestination), source, destination);
    }

    private object MapCore(Type sourceType, Type destType, object source, object? destination)
    {
        if (destType.IsAssignableFrom(sourceType))
        {
            return source;
        }

        var sourceUnderlying = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
        var destUnderlying = Nullable.GetUnderlyingType(destType) ?? destType;

        if (destType == typeof(string))
        {
            return source?.ToString() ?? string.Empty;
        }

        if (destUnderlying.IsPrimitive && sourceUnderlying.IsPrimitive)
        {
            try { return Convert.ChangeType(source, destUnderlying); } catch { /* Fallthrough */ }
        }

        var key = (sourceType, destType);

        if (!_maps.TryGetValue(key, out var @delegate))
        {
            throw new AutoMappicException(
                $"No mapping registered from '{sourceType.FullName}' to '{destType.FullName}'. " +
                $"Ensure a Profile.CreateMap<{sourceType.Name}, {destType.Name}>() call exists " +
                $"and the generator has run, or register the mapping explicitly.");
        }

        return @delegate(this, source, destination);
    }

    private static Func<Mapper, object, object?, object> BuildFallbackDelegate(IMappingExpression mapping)
    {
        var sourceProps = mapping.SourceType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .ToDictionary(p => p.Name, StringComparer.Ordinal);

        var destProps = mapping.DestinationType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToArray();

        var sourceMethods = mapping.SourceType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.ReturnType != typeof(void) && m.GetParameters().Length == 0 && m.Name.StartsWith("Get", StringComparison.Ordinal))
            .ToDictionary(m => m.Name.Substring(3), StringComparer.Ordinal);

        return (mapper, src, dst) =>
        {
            dst ??= Activator.CreateInstance(mapping.DestinationType)
                ?? throw new AutoMappicException(
                    $"Could not create an instance of '{mapping.DestinationType.FullName}'.");

            foreach (var destProp in destProps)
            {
                if (mapping.IgnoredMembers.Contains(destProp.Name))
                {
                    continue;
                }

                if (mapping.RuntimeMaps.TryGetValue(destProp.Name, out var runtimeMap))
                {
                    var customVal = runtimeMap(src);
                    destProp.SetValue(dst, customVal);
                    continue;
                }

                PropertyInfo? srcProp = null;
                if (!sourceProps.TryGetValue(destProp.Name, out srcProp))
                {
                    srcProp = sourceProps.Values.FirstOrDefault(p =>
                        string.Equals(Normalize(p.Name), Normalize(destProp.Name), StringComparison.OrdinalIgnoreCase));
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
                        destProp.SetValue(dst, val);
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
                    else if (IsCollection(destProp.PropertyType, out var destItemType) && IsCollection(srcProp.PropertyType, out var srcItemType))
                    {
                        var sourceList = (System.Collections.IEnumerable)val;
                        var resultList = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(destItemType))!;

                        foreach (var item in sourceList)
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
                                    var mapped = mapper.MapCore(item.GetType(), destItemType, item, null);
                                    resultList.Add(mapped);
                                }
                                catch (AutoMappicException)
                                {
                                    // Skip
                                }
                            }
                        }

                        if (destProp.PropertyType.IsArray)
                        {
                            var array = Array.CreateInstance(destItemType, resultList.Count);
                            resultList.CopyTo(array, 0);
                            destProp.SetValue(dst, array);
                        }
                        else
                        {
                            destProp.SetValue(dst, resultList);
                        }
                    }
                    else
                    {
                        try
                        {
                            var nested = mapper.MapCore(srcProp.PropertyType, destProp.PropertyType, val, null);
                            destProp.SetValue(dst, nested);
                        }
                        catch (AutoMappicException)
                        {
                            // Skip
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
                    var flattenedValue = ResolveFlattenedValue(src, destProp.Name);
                    if (flattenedValue != null)
                    {
                        destProp.SetValue(dst, flattenedValue);
                    }
                }
            }

            return dst;
        };
    }

    private static object? ResolveFlattenedValue(object source, string destName)
    {
        var props = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Try to find a property that starts the chain
        foreach (var prop in props)
        {
            if (destName.StartsWith(prop.Name, StringComparison.OrdinalIgnoreCase))
            {
                var val = prop.GetValue(source);
                if (val == null) return null;

                if (prop.Name.Length == destName.Length) return val;

                var remaining = destName.Substring(prop.Name.Length);
                var result = ResolveFlattenedValue(val, remaining);
                if (result != null) return result;
            }
        }

        return null;
    }

    private static bool IsDictionary(Type type, out Type keyType, out Type valueType)
    {
        keyType = null!;
        valueType = null!;
        if (type.IsGenericType && typeof(System.Collections.IDictionary).IsAssignableFrom(type) && type.GetGenericArguments().Length == 2)
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

    private static string Normalize(string name) => name.Replace("_", "");
}
