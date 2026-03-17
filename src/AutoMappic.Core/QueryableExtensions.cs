using System.Linq;

namespace AutoMappic;

/// <summary>
///   Provides high-performance <see cref="IQueryable{T}"/> projections through AutoMappic source generation.
/// </summary>
public static class QueryableExtensions
{
    /// <summary>
    ///   Projects an <see cref="IQueryable{TSource}"/> into an <see cref="IQueryable{TDestination}"/> using AutoMappic configurations.
    ///   This method must be intercepted at compile time by the AutoMappic source generator;
    ///   do NOT rely on its runtime fallback logic in production.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The target projection type.</typeparam>
    /// <param name="source">The source queryable to project from.</param>
    /// <returns>A projected queryable.</returns>
    /// <exception cref="AutoMappicException">Thrown if the method is executed natively at runtime instead of being intercepted.</exception>
    public static IQueryable<TDestination> ProjectTo<TSource, TDestination>(this IQueryable<TSource> source)
    {
        throw new AutoMappicException("ProjectTo<TDestination>() must be intercepted by the AutoMappic source generator. Ensure you have the generator referenced and your mapping profiles registered, and that your target project uses a compatible MSBuild property namespace structure.");
    }
}
