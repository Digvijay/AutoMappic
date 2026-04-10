using System.Diagnostics.CodeAnalysis;

namespace AutoMappic;

/// <summary>
///   The primary mapping interface. Consumers call this exactly as they would call AutoMapper's
///   <c>IMapper</c>. At compile time, the AutoMappic source generator emits
///   <c>[InterceptsLocation]</c> shims that reroute every call site to a generated static
///   method - eliminating reflection and enabling Native AOT.
/// </summary>
public interface IMapper
{
    /// <summary>Maps <paramref name="source" /> to a new instance of <typeparamref name="TDestination" />.</summary>
    /// <typeparam name="TDestination">The destination type to create.</typeparam>
    /// <param name="source">The source object to map from. Must not be <see langword="null" />.</param>
    /// <returns>A fully populated instance of <typeparamref name="TDestination" />.</returns>
    [RequiresUnreferencedCode("Object mapping via IMapper interface requires reflection if not intercepted by the source generator.")]
    [RequiresDynamicCode("Object mapping via IMapper interface requires dynamic code generation if not intercepted by the source generator.")]
    TDestination Map<TDestination>(object source);

    /// <summary>Maps <paramref name="source" /> to a new instance of <typeparamref name="TDestination" />.</summary>
    /// <typeparam name="TSource">The concrete source type.</typeparam>
    /// <typeparam name="TDestination">The destination type to create.</typeparam>
    /// <param name="source">The source object to map from. Must not be <see langword="null" />.</param>
    /// <returns>A fully populated instance of <typeparamref name="TDestination" />.</returns>
    [RequiresUnreferencedCode("Object mapping via IMapper interface requires reflection if not intercepted by the source generator.")]
    [RequiresDynamicCode("Object mapping via IMapper interface requires dynamic code generation if not intercepted by the source generator.")]
    TDestination Map<TSource, TDestination>(TSource source);

    /// <summary>
    ///   Maps <paramref name="source" /> onto an existing <paramref name="destination" /> instance,
    ///   overwriting matched properties in place.
    /// </summary>
    /// <typeparam name="TSource">The concrete source type.</typeparam>
    /// <typeparam name="TDestination">The concrete destination type.</typeparam>
    /// <param name="source">The source object to map from.</param>
    /// <param name="destination">The destination object to map into.</param>
    /// <returns>The mutated <paramref name="destination" /> instance.</returns>
    [RequiresUnreferencedCode("Object mapping via IMapper interface requires reflection if not intercepted by the source generator.")]
    [RequiresDynamicCode("Object mapping via IMapper interface requires dynamic code generation if not intercepted by the source generator.")]
    TDestination Map<TSource, TDestination>(TSource source, TDestination destination);

    /// <summary>Asynchronously maps <paramref name="source" /> to a new instance of <typeparamref name="TDestination" />.</summary>
    /// <typeparam name="TDestination">The destination type to create.</typeparam>
    /// <param name="source">The source object to map from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the mapping operation.</returns>
    [RequiresUnreferencedCode("Object mapping via IMapper interface requires reflection if not intercepted by the source generator.")]
    [RequiresDynamicCode("Object mapping via IMapper interface requires dynamic code generation if not intercepted by the source generator.")]
    System.Threading.Tasks.Task<TDestination> MapAsync<TDestination>(object source, System.Threading.CancellationToken ct = default);

    /// <summary>Asynchronously maps <paramref name="source" /> to a new instance of <typeparamref name="TDestination" />.</summary>
    [RequiresUnreferencedCode("Object mapping via IMapper interface requires reflection if not intercepted by the source generator.")]
    [RequiresDynamicCode("Object mapping via IMapper interface requires dynamic code generation if not intercepted by the source generator.")]
    System.Threading.Tasks.Task<TDestination> MapAsync<TSource, TDestination>(TSource source, System.Threading.CancellationToken ct = default);

    /// <summary>Asynchronously maps <paramref name="source" /> onto an existing <paramref name="destination" /> instance.</summary>
    [RequiresUnreferencedCode("Object mapping via IMapper interface requires reflection if not intercepted by the source generator.")]
    [RequiresDynamicCode("Object mapping via IMapper interface requires dynamic code generation if not intercepted by the source generator.")]
    System.Threading.Tasks.Task<TDestination> MapAsync<TSource, TDestination>(TSource source, TDestination destination, System.Threading.CancellationToken ct = default);

    /// <summary>Gets the configuration for this mapper.</summary>
    IConfigurationProvider ConfigurationProvider { get; }
}
