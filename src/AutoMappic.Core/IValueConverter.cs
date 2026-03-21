namespace AutoMappic;

/// <summary>
///   Provides a reusable mechanism for converting a single source value into a destination value.
///   Unlike <see cref="IValueResolver{TSource, TDestinationMember}"/> which has access to the 
///   entire source object, a converter only operates on the specific member value passed to it.
/// </summary>
/// <typeparam name="TSourceMember">The type of the source member.</typeparam>
/// <typeparam name="TDestinationMember">The type of the destination member.</typeparam>
public interface IValueConverter<in TSourceMember, out TDestinationMember>
{
    /// <summary>
    ///   Performs the conversion.
    /// </summary>
    /// <param name="sourceMember">The raw source value.</param>
    /// <returns>The converted destination value.</returns>
    TDestinationMember Convert(TSourceMember sourceMember);
}
