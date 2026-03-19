namespace AutoMappic;

/// <summary>
///   Defines a custom value resolver for mapping a source object to a specific destination member.
/// </summary>
public interface IValueResolver<in TSource, out TMember>
{
    /// <summary>
    ///   Resolves the value for the destination member.
    /// </summary>
    TMember Resolve(TSource source);
}

/// <summary>
///   Defines an asynchronous custom value resolver.
/// </summary>
public interface IAsyncValueResolver<in TSource, TMember>
{
    /// <summary>
    ///   Asynchronously resolves the value for the destination member.
    /// </summary>
    System.Threading.Tasks.Task<TMember> ResolveAsync(TSource source);
}
