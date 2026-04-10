using System;
using System.Collections.Concurrent;

namespace AutoMappic;

/// <summary>
///   Internal registry used for Hot Reload fallback and dynamic mapping lookup.
///   This allows the runtime mapper to find source-generated shims even when static interception is bypassed.
/// </summary>
public static class HotReloadRegistry
{
    private static readonly ConcurrentDictionary<(Type Source, Type Dest), Delegate> _shims = new();

    /// <summary>Registers a generated mapping shim.</summary>
    public static void Register(Type sourceType, Type destType, Delegate shim)
    {
        _shims[(sourceType, destType)] = shim;
    }

    /// <summary>Attempts to find a registered shim for the given type pair.</summary>
    public static bool TryGetShim(Type sourceType, Type destType, out Delegate? shim)
    {
        return _shims.TryGetValue((sourceType, destType), out shim);
    }
}
