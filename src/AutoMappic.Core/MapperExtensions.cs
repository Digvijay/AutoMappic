using System.Threading;
using System.Threading.Tasks;

namespace AutoMappic;

/// <summary>
///   Provides fluent extension methods for the <see cref="IMapper" /> interface, 
///   mirroring common mapping patterns for better readability.
/// </summary>
public static class MapperExtensions
{
    /// <summary>
    ///   Maps the source object to a new instance of <typeparamref name="TDestination" /> 
    ///   using the provided <paramref name="mapper" />.
    /// </summary>
    public static TDestination MapTo<TDestination>(this object source, IMapper mapper)
        => mapper.Map<TDestination>(source);

    /// <summary>
    ///   Maps the source object to a new instance of <typeparamref name="TDestination" /> 
    ///   using the provided <paramref name="mapper" />.
    /// </summary>
    public static TDestination MapTo<TSource, TDestination>(this TSource source, IMapper mapper)
        => mapper.Map<TSource, TDestination>(source);

    /// <summary>
    ///   Maps the source object onto an existing <paramref name="destination" /> 
    ///   using the provided <paramref name="mapper" />.
    /// </summary>
    public static TDestination MapTo<TSource, TDestination>(this TSource source, TDestination destination, IMapper mapper)
        => mapper.Map<TSource, TDestination>(source, destination);

    /// <summary>
    ///   Asynchronously maps the source object to a new instance of <typeparamref name="TDestination" /> 
    ///   using the provided <paramref name="mapper" />.
    /// </summary>
    public static Task<TDestination> MapToAsync<TDestination>(this object source, IMapper mapper, CancellationToken ct = default)
        => mapper.MapAsync<TDestination>(source, ct);

    /// <summary>
    ///   Asynchronously maps the source object to a new instance of <typeparamref name="TDestination" /> 
    ///   using the provided <paramref name="mapper" />.
    /// </summary>
    public static Task<TDestination> MapToAsync<TSource, TDestination>(this TSource source, IMapper mapper, CancellationToken ct = default)
        => mapper.MapAsync<TSource, TDestination>(source, ct);

    /// <summary>
    ///   Asynchronously maps the source object onto an existing <paramref name="destination" />
    ///   using the provided <paramref name="mapper" />.
    /// </summary>
    public static Task<TDestination> MapToAsync<TSource, TDestination>(this TSource source, TDestination destination, IMapper mapper, CancellationToken ct = default)
        => mapper.MapAsync<TSource, TDestination>(source, destination, ct);
}
