using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace AutoMappic;

#nullable enable

/// <summary>
///   Runtime fallback implementation of <see cref="IMapper" />.
/// </summary>
public sealed class Mapper : IMapper, IDisposable
{
    // Signatures take (mapper, source, destination) -> result
    private readonly Dictionary<(Type Source, Type Destination), (IMappingExpression Mapping, Func<Mapper, object, object?, global::System.Threading.Tasks.Task<object>>? Delegate)> _maps
        = [];
    // Per-async-context stack to detect circular references in runtime fallback
    private readonly global::System.Threading.AsyncLocal<HashSet<object>> _mappingStack = new();
    private bool _disposed;
    /// <inheritdoc />
    public IConfigurationProvider ConfigurationProvider { get; private set; } = null!;

    /// <summary>
    ///   Initialises the mapper from a collection of <see cref="Profile" /> instances and global configuration.
    /// </summary>
    [global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
        "TrimAnalysis", "IL2026:RequiresUnreferencedCode",
        Justification = "The call to BuildFallbackDelegate is guarded by a feature switch that "
            + "ILLink.Substitutions.xml stubs to false, so the trimmer folds the branch away and removes the "
            + "reflective engine. This is what stops a generated-only application from rooting reflection.")]
    [global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
        "AotAnalysis", "IL3050:RequiresDynamicCode",
        Justification = "Same feature switch: Native AOT publishing implies trimming, so the branch is removed "
            + "before ILC analyses the application.")]
    internal Mapper(IEnumerable<Profile> profiles, IConfigurationProvider config)
    {
        ConfigurationProvider = config;
        foreach (Profile profile in profiles)
        {
            foreach (IMappingExpression mapping in profile.Mappings)
            {
                (Type SourceType, Type DestinationType) key = (mapping.SourceType, mapping.DestinationType);

                // Guarded so that a trimmed or Native AOT application drops the reflective engine
                // entirely: ILLink.Substitutions.xml stubs the switch to false, the trimmer folds
                // this branch away, and BuildFallbackDelegate becomes unreachable and is removed.
                _maps[key] = AutoMappicFeatures.IsReflectionFallbackEnabled
                    ? (mapping, BuildFallbackDelegate(mapping))
                    : (mapping, null);
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
    public TDestination Map<TDestination>(object source) => source is null ? default! : (TDestination)ReflectiveMap(source.GetType(), typeof(TDestination), source, null);

    /// <inheritdoc />
    public TDestination Map<TSource, TDestination>(TSource source) => source is null ? default! : (TDestination)ReflectiveMap(typeof(TSource), typeof(TDestination), source, null);

    /// <inheritdoc />
    public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        return (TDestination)ReflectiveMap(typeof(TSource), typeof(TDestination), source, destination);
    }

    /// <inheritdoc />
    public async global::System.Threading.Tasks.Task<TDestination> MapAsync<TDestination>(object source, global::System.Threading.CancellationToken ct = default)
    {
        try
        {
            return source is null
                ? default!
                : (TDestination)await ReflectiveMapAsync(source.GetType(), typeof(TDestination), source, null).ConfigureAwait(false);
        }
        catch (global::System.Exception ex)
        {
            return await global::System.Threading.Tasks.Task.FromException<TDestination>(ex).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async global::System.Threading.Tasks.Task<TDestination> MapAsync<TSource, TDestination>(TSource source, global::System.Threading.CancellationToken ct = default)
    {
        try
        {
            return source is null
                ? default!
                : (TDestination)await ReflectiveMapAsync(typeof(TSource), typeof(TDestination), source, null).ConfigureAwait(false);
        }
        catch (global::System.Exception ex)
        {
            return await global::System.Threading.Tasks.Task.FromException<TDestination>(ex).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async global::System.Threading.Tasks.Task<TDestination> MapAsync<TSource, TDestination>(TSource source, TDestination destination, global::System.Threading.CancellationToken ct = default)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(destination);
            return (TDestination)await ReflectiveMapAsync(typeof(TSource), typeof(TDestination), source, destination).ConfigureAwait(false);
        }
        catch (global::System.Exception ex)
        {
            return await global::System.Threading.Tasks.Task.FromException<TDestination>(ex).ConfigureAwait(false);
        }
    }

    /// <summary>
    ///   Single entry point from the public mapping surface into the reflective engine.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The guard is what makes the public surface trim-safe. When
    ///     <c>ILLink.Substitutions.xml</c> stubs
    ///     <see cref="AutoMappicFeatures.IsReflectionFallbackEnabled" /> to
    ///     <see langword="false" />, the trimmer constant-folds this method down to the throw,
    ///     the call to <see cref="MapCore" /> disappears, and the entire reflective engine
    ///     becomes unreachable and is removed. Nothing is left for trim analysis to complain
    ///     about, which is why <see cref="IMapper" /> no longer has to declare requirements its
    ///     consumers cannot satisfy.
    ///   </para>
    ///   <para>
    ///     Reaching this method in a trimmed application means the call site was not intercepted,
    ///     so the mapping was never generated. Throwing is the correct outcome: the alternative
    ///     is reflecting over members the trimmer has already removed and failing later with an
    ///     error that names neither the mapping nor the cause.
    ///   </para>
    /// </remarks>
    [global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
        "TrimAnalysis", "IL2026:RequiresUnreferencedCode",
        Justification = "The call is guarded by a feature switch that ILLink.Substitutions.xml stubs to false, "
            + "so the trimmer removes this branch and the reflective engine with it. In an application that is "
            + "not trimmed the branch runs, and there is no trimming for it to be unsafe with respect to.")]
    [global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
        "AotAnalysis", "IL3050:RequiresDynamicCode",
        Justification = "Same feature switch: Native AOT publishing implies trimming, so this branch is removed "
            + "before ILC analyses the application.")]
    private object ReflectiveMap(Type sourceType, Type destType, object source, object? destination)
    {
        return !AutoMappicFeatures.IsReflectionFallbackEnabled
            ? throw ReflectionFallbackTrimmed(sourceType, destType)
            : MapCore(sourceType, destType, source, destination);
    }

    /// <summary>Asynchronous counterpart to <see cref="ReflectiveMap" />.</summary>
    [global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
        "TrimAnalysis", "IL2026:RequiresUnreferencedCode",
        Justification = "See ReflectiveMap: the call is removed by the trimmer via a substituted feature switch.")]
    [global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
        "AotAnalysis", "IL3050:RequiresDynamicCode",
        Justification = "See ReflectiveMap: the call is removed by the trimmer via a substituted feature switch.")]
    private global::System.Threading.Tasks.Task<object> ReflectiveMapAsync(Type sourceType, Type destType, object source, object? destination)
    {
        return !AutoMappicFeatures.IsReflectionFallbackEnabled
            ? throw ReflectionFallbackTrimmed(sourceType, destType)
            : MapCoreAsync(sourceType, destType, source, destination);
    }

    /// <summary>Builds the error raised when a mapping was not generated and the fallback is gone.</summary>
    private static AutoMappicException ReflectionFallbackTrimmed(Type sourceType, Type destType) =>
        new($"AutoMappic: No generated mapping from '{sourceType.FullName}' to '{destType.FullName}' was available, "
            + "and the reflective fallback has been trimmed out of this application. That is the intended "
            + "behaviour for a trimmed or Native AOT build - mappings are meant to be generated at compile time. "
            + $"Declare CreateMap<{sourceType.Name}, {destType.Name}>() in a Profile so the generator emits it, "
            + "or re-enable the fallback with <RuntimeHostConfigurationOption "
            + "Include=\"AutoMappic.IsReflectionFallbackEnabled\" Value=\"true\" />.");

    /// <summary>Internal core mapping logic used for recursive resolution and fallback mapping.</summary>
    [RequiresUnreferencedCode("Object mapping via runtime Mapper requires reflection.")]
    [RequiresDynamicCode("Building a destination collection needs the array or generic type for the runtime item type.")]
    public object MapCore(Type sourceType, Type destType, object source, object? destination) => MapCoreAsync(sourceType, destType, source, destination).GetAwaiter().GetResult();

    /// <summary>Asynchronous core mapping logic.</summary>
    [RequiresUnreferencedCode("Object mapping via runtime Mapper requires reflection.")]
    [RequiresDynamicCode("Building a destination collection needs the array or generic type for the runtime item type.")]
    public async global::System.Threading.Tasks.Task<object> MapCoreAsync(Type sourceType, Type destType, object source, object? destination)
    {
        if (destType.IsAssignableFrom(sourceType))
        {
            return source!;
        }

        Type sourceUnderlying = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
        Type destUnderlying = Nullable.GetUnderlyingType(destType) ?? destType;

        if (destType == typeof(string))
        {
            return (source?.ToString() ?? string.Empty)!;
        }

        if (destUnderlying.IsPrimitive && sourceUnderlying.IsPrimitive)
        {
            try { return Convert.ChangeType(source!, destUnderlying!, System.Globalization.CultureInfo.InvariantCulture)!; } catch { /* Fallthrough */ }
        }

        if (IsCollection(destType, out Type? destItemType) && IsCollection(sourceType, out Type? sourceItemType))
        {
            var sourceList = (System.Collections.IEnumerable)source!;

            // Staged in a non-generic list so that no closed generic type has to be constructed
            // at runtime. Building List<destItemType> via MakeGenericType would require dynamic
            // code whenever destItemType is a value type, which Native AOT cannot provide - and
            // the destination type is already closed here, so it was never necessary.
            var staged = new List<object?>();

            foreach (object? item in sourceList!)
            {
                if (item is null)
                {
                    staged.Add(null);
                    continue;
                }

                if (destItemType.IsAssignableFrom(item.GetType()))
                {
                    staged.Add(item);
                }
                else
                {
                    try
                    {
                        object mapped = MapCore(item.GetType(), destItemType, item, null);
                        staged.Add(mapped);
                    }
                    catch (AutoMappicException ex)
                    {
                        throw new AutoMappicException(
                            $"AutoMappic: Failed to map a collection item of type '{item.GetType().FullName}' to '{destItemType.FullName}'. "
                            + $"Ensure a CreateMap<{item.GetType().Name}, {destItemType.Name}>() exists in a Profile. Inner: {ex.Message}", ex);
                    }
                }
            }

            // An array satisfies IEnumerable<T>, IReadOnlyList<T>, IList<T> and ICollection<T>,
            // so it serves every interface-typed destination as well as an explicit array.
            if (destType.IsArray || destType.IsInterface)
            {
                var array = Array.CreateInstance(destItemType, staged.Count);
                for (int i = 0; i < staged.Count; i++)
                {
                    array.SetValue(staged[i], i);
                }
                return array;
            }

            if (Activator.CreateInstance(destType) is not System.Collections.IList concrete)
            {
                throw new AutoMappicException(
                    $"AutoMappic: Cannot populate the collection type '{destType.Name}' in the runtime fallback, "
                    + "because it does not implement IList. Declare the mapping in a Profile so the source generator "
                    + $"emits it at compile time, or type the destination as {destItemType.Name}[], "
                    + $"List<{destItemType.Name}> or one of the collection interfaces.");
            }

            foreach (object? item in staged)
            {
                concrete.Add(item);
            }
            return concrete;
        }

        (Type sourceType, Type destType) key = (sourceType, destType);
        if (!_maps.TryGetValue(key, out (IMappingExpression Mapping, Func<Mapper, object, object?, Task<object>>? Delegate) entry))
        {
            // NEW in v0.7.0: Hot Reload Fallback - check for registered shims
            if (HotReloadRegistry.TryGetShim(sourceType, destType, out Delegate? shim) && shim != null)
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

        if (entry.Delegate is null)
        {
            throw new AutoMappicException(
                $"A mapping from '{sourceType.FullName}' to '{destType.FullName}' is registered, but the "
                + "reflective fallback that would execute it has been trimmed out of this application. "
                + "That is the intended behaviour for a trimmed or Native AOT build: mappings are meant to be "
                + "generated at compile time. This mapping was not, which usually means the call site could not "
                + "be intercepted or the mapping was closed from an open generic at runtime. Declare the "
                + "concrete mapping in a Profile so the generator emits it, or re-enable the fallback with "
                + "<RuntimeHostConfigurationOption Include=\"AutoMappic.IsReflectionFallbackEnabled\" Value=\"true\" />.");
        }

        HashSet<object>? currentStack = _mappingStack.Value;
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
            object result = await entry.Delegate(this, source!, destination).ConfigureAwait(false);
            await entry.Mapping.ExecuteAfterAsync(source!, result).ConfigureAwait(false);
            return result;
        }
        finally
        {
            _mappingStack.Value?.Remove(source!);
        }
    }

    /// <summary>
    ///   Creates a dictionary instance assignable to <paramref name="dictType" /> without
    ///   constructing a closed generic type at runtime wherever that can be avoided.
    /// </summary>
    /// <remarks>
    ///   When the destination is a concrete type it is already closed, so it can simply be
    ///   activated. Only an interface-typed destination needs <c>Dictionary&lt;,&gt;</c> to be
    ///   closed here. That is safe under Native AOT for reference type arguments, because the
    ///   runtime shares one canonical instantiation for all of them; a value type argument
    ///   genuinely needs code that AOT cannot generate, so it is detected up front and reported
    ///   with an actionable message rather than failing obscurely deep inside the runtime.
    /// </remarks>
    private static System.Collections.IDictionary CreateDictionary(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type dictType,
        Type keyType,
        Type valueType)
    {
        return !dictType.IsInterface && !dictType.IsAbstract
            ? (System.Collections.IDictionary)Activator.CreateInstance(dictType)!
            : !global::System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported
            && (keyType.IsValueType || valueType.IsValueType)
            ? throw new AutoMappicException(
                $"AutoMappic: Cannot build a Dictionary<{keyType.Name}, {valueType.Name}> for the interface-typed "
                + $"destination '{dictType.Name}' in a Native AOT application, because a generic instantiation over a "
                + "value type cannot be created at runtime. Declare the mapping in a Profile so the source generator "
                + "emits it at compile time, or type the destination property as a concrete Dictionary<,>.")
            : (System.Collections.IDictionary)Activator.CreateInstance(MakeDictionaryType(keyType, valueType))!;
    }

    [global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
        "AotAnalysis", "IL3050:RequiresDynamicCode",
        Justification = "CreateDictionary rejects value type arguments up front when dynamic code is unavailable. "
            + "Reference type arguments share a canonical instantiation, which Native AOT already provides.")]
    [global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
        "TrimAnalysis", "IL2055:MakeGenericType",
        Justification = "Dictionary<,> is referenced statically by this assembly, so the trimmer keeps it and its "
            + "public parameterless constructor.")]
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    private static Type MakeDictionaryType(Type keyType, Type valueType)
        => typeof(Dictionary<,>).MakeGenericType(keyType, valueType);

    [RequiresUnreferencedCode("The runtime fallback reflects over members of types only known at runtime.")]

    [RequiresDynamicCode("The runtime fallback builds destination collections for runtime item types.")]

    private static Func<Mapper, object, object?, global::System.Threading.Tasks.Task<object>> BuildFallbackDelegate(IMappingExpression mapping)
    {
        if (mapping.ConverterType != null)
        {
            object? converter = Activator.CreateInstance(mapping.ConverterType);
            MethodInfo method = mapping.ConverterType.GetMethod("Convert")!;
            return (mapper, src, dst) => global::System.Threading.Tasks.Task.FromResult(method.Invoke(converter, [src])!);
        }

        var sourceProps = mapping.SourceType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .GroupBy(p => p.Name)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        PropertyInfo[] destProps = [.. mapping.DestinationType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite || IsCollection(p.PropertyType, out _))];

        var sourceMethods = mapping.SourceType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(m => m.ReturnType != typeof(void) && m.GetParameters().Length == 0 && m.Name.StartsWith("Get", StringComparison.Ordinal))
            .GroupBy(m => m.Name[3..])
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

        return async (mapper, src, dst) =>
        {
            dst ??= mapping.ConstructionFactory != null
                    ? mapping.ConstructionFactory.DynamicInvoke(src)
                    : Activator.CreateInstance(mapping.DestinationType)
                        ?? throw new AutoMappicException($"Could not create an instance of '{mapping.DestinationType.FullName}'.");

            await mapping.ExecuteBeforeAsync(src!, dst!).ConfigureAwait(false);

            foreach (PropertyInfo? destProp in destProps)
            {
                if (mapping.IgnoredMembers.Contains(destProp.Name))
                {
                    continue;
                }

                if (mapping.RuntimeConditions.TryGetValue(destProp.Name, out Delegate? condition))
                {
                    if (!(bool)condition.DynamicInvoke(src, dst)!)
                    {
                        continue;
                    }
                }

                if (mapping.RuntimeMaps.TryGetValue(destProp.Name, out Func<object, object?>? runtimeMap))
                {
                    object? customVal = runtimeMap(src!);
                    destProp.SetValue(dst, customVal);
                    continue;
                }

                if (src is System.Collections.IDictionary dict && IsDictionary(mapping.SourceType, out Type? kType, out Type? vType) && kType == typeof(string))
                {
                    string[] parts = mapping.DestinationNaming?.Split(destProp.Name) ?? [destProp.Name];
                    string keyName = JoinName(parts, mapping.SourceNaming);
                    if (dict.Contains(keyName))
                    {
                        object? dictVal = dict[keyName];
                        if (dictVal != null && !destProp.PropertyType.IsAssignableFrom(dictVal.GetType()))
                        {
                            dictVal = mapper.MapCore(dictVal.GetType(), destProp.PropertyType, dictVal, null);
                        }
                        destProp.SetValue(dst, dictVal);
                        continue;
                    }
                }

                if (!sourceProps.TryGetValue(destProp.Name, out PropertyInfo? srcProp))
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
                    object? val = srcProp.GetValue(src);
                    if (val is null)
                    {
                        destProp.SetValue(dst, null);
                        continue;
                    }

                    if (destProp.PropertyType.IsAssignableFrom(srcProp.PropertyType))
                    {
                        bool isSimple = destProp.PropertyType.IsValueType || destProp.PropertyType == typeof(string);
                        if (isSimple)
                        {
                            if (destProp.CanWrite)
                            {
                                destProp.SetValue(dst, val);
                            }
                        }
                        else if (!IsCollection(destProp.PropertyType, out _))
                        {
                            object mappedVal = mapper.MapCore(val.GetType(), destProp.PropertyType, val, null);
                            destProp.SetValue(dst, mappedVal);
                        }
                        else if (IsCollection(destProp.PropertyType, out _))
                        {
                            if (destProp.GetValue(dst) is System.Collections.IList targetColl)
                            {
                                targetColl.Clear();
                                foreach (object? item in (System.Collections.IEnumerable)val)
                                {
                                    targetColl.Add(item);
                                }
                            }
                        }
                    }
                    else if (IsDictionary(destProp.PropertyType, out Type? dK, out Type? dV) && IsDictionary(srcProp.PropertyType, out Type? sK, out Type? sV))
                    {
                        var sourceDict = (System.Collections.IDictionary)val;
                        IDictionary resultDict = CreateDictionary(destProp.PropertyType, dK, dV);

                        foreach (System.Collections.DictionaryEntry entry in sourceDict)
                        {
                            object entryKey = entry.Key;
                            object? entryVal = entry.Value;

                            object? mappedKey = entryKey;
                            if (entryKey != null && !dK.IsAssignableFrom(entryKey.GetType()))
                            {
                                mappedKey = dK == typeof(string) ? entryKey.ToString() : mapper.MapCore(entryKey.GetType(), dK, entryKey, null);
                            }

                            object? mappedVal = entryVal;
                            if (entryVal != null && !dV.IsAssignableFrom(entryVal.GetType()))
                            {
                                mappedVal = dV == typeof(string) ? entryVal.ToString() : mapper.MapCore(entryVal.GetType(), dV, entryVal, null);
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
                                if (destProp.GetValue(dst) is System.Collections.IList targetColl)
                                {
                                    if (mapper.MapCore(srcProp.PropertyType, destProp.PropertyType, val, null) is System.Collections.IEnumerable items)
                                    {
                                        targetColl.Clear();
                                        foreach (object? item in items)
                                        {
                                            targetColl.Add(item);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                object nested = mapper.MapCore(srcProp.PropertyType, destProp.PropertyType, val, null);
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
                else if (sourceMethods.TryGetValue(destProp.Name, out MethodInfo? srcMethod))
                {
                    destProp.SetValue(dst, srcMethod.Invoke(src, null));
                }
                else
                {
                    // Fallback to Flattening check
                    object? flattenedValue = ResolveFlattenedValue(src!, destProp.Name, mapping.SourceNaming, mapping.DestinationNaming);
                    if (flattenedValue != null)
                    {
                        destProp.SetValue(dst, flattenedValue);
                    }
                }
            }

            return dst!;
        };
    }

    [RequiresUnreferencedCode("Flattening resolves properties on the runtime type of each value.")]

    private static object? ResolveFlattenedValue(object source, string destName, INamingConvention? sourceNaming, INamingConvention? destNaming)
    {
        PropertyInfo[] props = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        string[] destParts = destNaming?.Split(destName) ?? [destName];

        if (destParts.Length == 0)
        {
            return null;
        }

        for (int i = 1; i <= destParts.Length; i++)
        {
            string segment = string.Concat(destParts.Take(i));
            foreach (PropertyInfo prop in props)
            {
                if (string.Equals(Normalize(prop.Name, sourceNaming), Normalize(segment, destNaming), StringComparison.OrdinalIgnoreCase))
                {
                    object? val = prop.GetValue(source);
                    if (val == null)
                    {
                        return null;
                    }

                    if (i == destParts.Length)
                    {
                        return val;
                    }

                    string remaining = string.Concat(destParts.Skip(i));
                    object? result = ResolveFlattenedValue(val, remaining, sourceNaming, destNaming);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
        }

        return null;
    }

    private static bool IsDictionary([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type, out Type keyType, out Type valueType)
    {
        keyType = null!;
        valueType = null!;

        Type? dictIntf = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>)
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

    private static bool IsCollection([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type type, out Type itemType)
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
        if (conv is LowerUnderscoreNamingConvention)
        {
            return string.Join("_", parts).ToLowerInvariant();
        }

        if (conv is KebabCaseNamingConvention)
        {
            return string.Join("-", parts).ToLowerInvariant();
        }

        if (conv is CamelCaseNamingConvention)
        {
            return parts.Length == 0
                ? string.Empty
                : parts[0].ToLowerInvariant() + string.Concat(parts.Skip(1).Select(p =>
                p.Length > 0 ? char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant() : string.Empty));
        }

        // Default to Pascal
        return string.Concat(parts.Select(p =>
            p.Length > 0 ? char.ToUpperInvariant(p[0]) + p[1..].ToLowerInvariant() : string.Empty));
    }

    private static string Normalize(string name, INamingConvention? conv = null) => string.IsNullOrEmpty(name) ? name : conv == null ? name.Replace("-", "").Replace("_", "") : string.Concat(conv.Split(name));
}


