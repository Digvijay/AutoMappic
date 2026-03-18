namespace AutoMappic;

/// <summary>
///   Defines a custom type converter for mapping a source object to a new destination object.
/// </summary>
public interface ITypeConverter<in TSource, out TDestination>
{
    /// <summary>
    ///   Converts the source object to a new destination object.
    /// </summary>
    TDestination Convert(TSource source);
}
