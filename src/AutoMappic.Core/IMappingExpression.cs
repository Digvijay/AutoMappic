using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AutoMappic;

/// <summary>Non-generic marker interface for internal collection storage.</summary>
public interface IMappingExpression
{
    /// <summary>Gets the CLR type that is the source of this mapping.</summary>
    Type SourceType { get; }

    /// <summary>Gets the CLR type that is the destination of this mapping.</summary>
    Type DestinationType { get; }

    /// <summary>Members that should be skipped during mapping.</summary>
    IReadOnlyCollection<string> IgnoredMembers { get; }

    /// <summary>Explicit mapping rules (dest name -> C# expression).</summary>
    IReadOnlyDictionary<string, string?> ExplicitMaps { get; }

    /// <summary>Explicit mapping rules for runtime fallback (dest name -> delegate).</summary>
    IReadOnlyDictionary<string, Func<object, object?>> RuntimeMaps { get; }

    /// <summary>Custom type converter for the entire mapping.</summary>
    Type? ConverterType { get; }

    /// <summary>Specifies a custom expression for constructing the destination type.</summary>
    string? ConstructionExpression { get; }

    /// <summary>Conditional rules per destination member (dest name -> condition expression).</summary>
    IReadOnlyDictionary<string, string> MemberConditions { get; }

    /// <summary>Internal use: Factory for creating the destination instance.</summary>
    Delegate? ConstructionFactory { get; }

    /// <summary>Internal use: Runtime predicates for conditional mapping.</summary>
    IReadOnlyDictionary<string, Delegate> RuntimeConditions { get; }

    /// <summary>Specifies a custom type converter for this mapping (non-generic version).</summary>
    IMappingExpression ConvertUsing(Type converterType);

    /// <summary>Internal use: Executes BeforeMap for runtime fallback.</summary>
    void ExecuteBefore(object source, object destination);

    /// <summary>Internal use: Executes AfterMap for runtime fallback.</summary>
    void ExecuteAfter(object source, object destination);

    /// <summary>Internal use: Executes BeforeMapAsync for runtime fallback.</summary>
    Task ExecuteBeforeAsync(object source, object destination);

    /// <summary>Internal use: Executes AfterMapAsync for runtime fallback.</summary>
    Task ExecuteAfterAsync(object source, object destination);
}

/// <summary>
///   Fluent configuration surface for a single source-to-destination type pair.
///   The source generator reads the lambda bodies of <see cref="ForMember" /> calls
///   at compile time and stitches the raw C# directly into the generated static method –
///   no <c>Expression.Compile()</c> is ever executed at runtime.
/// </summary>
/// <typeparam name="TSource">The type to map from.</typeparam>
/// <typeparam name="TDestination">The type to map to.</typeparam>
public interface IMappingExpression<TSource, TDestination> : IMappingExpression
{
    /// <summary>
    ///   Provides an explicit mapping rule for a single destination member, overriding or
    ///   supplementing the convention engine.
    /// </summary>
    /// <typeparam name="TMember">The type of the destination member.</typeparam>
    /// <param name="destinationMember">
    ///   A selector lambda targeting the destination member to configure,
    ///   e.g. <c>dest =&gt; dest.FullName</c>.
    /// </param>
    /// <param name="memberOptions">
    ///   A configuration delegate, e.g. <c>opt =&gt; opt.MapFrom(src =&gt; src.FirstName + " " + src.LastName)</c>.
    /// </param>
    /// <returns>The same expression for chaining.</returns>
    IMappingExpression<TSource, TDestination> ForMember<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> memberOptions);

    /// <summary>
    ///   Instructs the convention engine to skip the specified destination member entirely.
    ///   The generated code will not attempt to assign a value to that property.
    /// </summary>
    /// <typeparam name="TMember">The type of the destination member.</typeparam>
    /// <param name="destinationMember">Selector for the member to ignore.</param>
    /// <returns>The same expression for chaining.</returns>
    IMappingExpression<TSource, TDestination> ForMemberIgnore<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember);

    /// <summary>
    ///   Creates a reverse mapping from the destination type back to the source type.
    ///   The source generator will automatically register both <c>TSource → TDestination</c>
    ///   and <c>TDestination → TSource</c>.
    /// </summary>
    /// <returns>A mapping expression for the reverse direction.</returns>
    IMappingExpression<TDestination, TSource> ReverseMap();

    /// <summary>
    ///   Specifies a custom type converter for this mapping.
    /// </summary>
    /// <typeparam name="TConverter">The converter type to use.</typeparam>
    IMappingExpression<TSource, TDestination> ConvertUsing<TConverter>() where TConverter : ITypeConverter<TSource, TDestination>, new();

    /// <summary>
    ///   Executes the provided action before the property mapping begins.
    /// </summary>
    IMappingExpression<TSource, TDestination> BeforeMap(Action<TSource, TDestination> action);

    /// <summary>
    ///   Executes the provided action after all properties have been mapped.
    /// </summary>
    IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination> action);

    /// <summary>
    ///   Executes the provided asynchronous action before the property mapping begins.
    /// </summary>
    IMappingExpression<TSource, TDestination> BeforeMapAsync(Func<TSource, TDestination, Task> action);

    /// <summary>
    ///   Executes the provided asynchronous action after all properties have been mapped.
    /// </summary>
    IMappingExpression<TSource, TDestination> AfterMapAsync(Func<TSource, TDestination, Task> action);

    /// <summary>
    ///   Specifies a custom factory expression for creating the destination instance.
    ///   The source generator will use this expression instead of the default constructor.
    /// </summary>
    IMappingExpression<TSource, TDestination> ConstructUsing(Expression<Func<TSource, TDestination>> ctor);
}

/// <summary>
///   Options available when configuring a single destination member via
///   <see cref="IMappingExpression{TSource,TDestination}.ForMember{TMember}" />.
/// </summary>
/// <typeparam name="TSource">The source type.</typeparam>
/// <typeparam name="TDestination">The destination type.</typeparam>
/// <typeparam name="TMember">The member type.</typeparam>
public interface IMemberConfigurationExpression<TSource, TDestination, TMember>
{
    /// <summary>
    ///   Specifies a custom mapping expression for this member.
    ///   The source generator extracts the raw C# text of <paramref name="mapExpression" />
    ///   and inlines it directly into the generated code, replacing all occurrences of the
    ///   lambda parameter with <c>source</c>.
    /// </summary>
    /// <param name="mapExpression">A lambda expression resolving the source value, e.g. <c>src =&gt; src.Address.City</c>.</param>
    void MapFrom<TResult>(Expression<Func<TSource, TResult>> mapExpression);

    /// <summary>Ignores this destination member; no assignment will be emitted for it.</summary>
    void Ignore();

    /// <summary>
    ///   Specifies a custom value resolver type to use for populating this destination member.
    ///   The Source Generator will intelligently emit <c>new TResolver().Resolve(source)</c>.
    /// </summary>
    /// <typeparam name="TResolver">An <see cref="IValueResolver{TSource, TMember}"/> used to construct the logic.</typeparam>
    void MapFrom<TResolver>() where TResolver : IValueResolver<TSource, TMember>, new();

    /// <summary>
    ///   Specifies an asynchronous value resolver for this member. 
    ///   When an async resolver is used, the mapping MUST be executed via <see cref="IMapper.MapAsync{TSource,TDestination}(TSource)"/>.
    /// </summary>
    void MapFromAsync<TResolver>() where TResolver : IAsyncValueResolver<TSource, TMember>, new();

    /// <summary>
    ///   Specifies a condition that must be met before this member is mapped.
    ///   The source generator will wrap the assignment in an 'if' block.
    /// </summary>
    /// <param name="condition">A lambda resolving to a boolean, e.g. <c>(src, dest) => src.IsActive</c>.</param>
    void Condition(Expression<Func<TSource, TDestination, bool>> condition);
}
