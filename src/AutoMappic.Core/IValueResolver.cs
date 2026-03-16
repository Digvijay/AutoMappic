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
