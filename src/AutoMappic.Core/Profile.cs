namespace AutoMappic;

/// <summary>
///   Base class for AutoMappic mapping profiles.  Derive from this class and call
///   <see cref="CreateMap{TSource, TDestination}" /> inside the constructor to register
///   a type-pair mapping.
/// </summary>
/// <remarks>
///   The AutoMappic source generator scans classes that inherit from <see cref="Profile" />
///   and treats every <c>CreateMap&lt;TSource, TDestination&gt;()</c> call it finds as the
///   declaration of a mapping pair.  No runtime reflection is involved; the generator reads
///   the call purely at the Roslyn syntax/semantic level and emits static C# code.
/// </remarks>
public abstract class Profile
{
    private readonly List<IMappingExpression> _mappings = [];

    /// <summary>
    ///   Gets all mapping expressions declared inside this profile.
    ///   Used internally by the <see cref="MapperConfiguration" /> to build the static engine.
    /// </summary>
    internal IReadOnlyList<IMappingExpression> Mappings => _mappings;

    internal void AddMapping(IMappingExpression expression) => _mappings.Add(expression);

    /// <summary>
    ///   Declares a mapping from <typeparamref name="TSource" /> to
    ///   <typeparamref name="TDestination" /> using AutoMappic's convention engine.
    /// </summary>
    /// <typeparam name="TSource">The type to map from.</typeparam>
    /// <typeparam name="TDestination">The type to map to.</typeparam>
    /// <returns>
    ///   A fluent <see cref="IMappingExpression{TSource, TDestination}" /> that lets you
    ///   override conventions with explicit <c>ForMember</c> calls.
    /// </returns>
    protected IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
    {
        var expression = new MappingExpression<TSource, TDestination>(this);
        _mappings.Add(expression);
        return expression;
    }
}
