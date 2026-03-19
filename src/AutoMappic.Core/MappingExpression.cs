using System.Linq.Expressions;
using System.Threading.Tasks;

namespace AutoMappic;

/// <summary>
///   Runtime representation of a single source-to-destination mapping declaration.
/// </summary>
/// <remarks>
///   This class is only used at runtime by the fallback <see cref="Mapper" /> implementation
///   (useful for unit tests that run without the source generator).  The generator itself
///   reads <c>CreateMap&lt;TSource, TDestination&gt;()</c> calls from the Roslyn syntax tree
///   and does not instantiate this class.
/// </remarks>
internal sealed class MappingExpression<TSource, TDestination> :
    IMappingExpression<TSource, TDestination>
{
    // Keyed by destination member name.
    internal readonly Dictionary<string, string?> ExplicitMaps = new(StringComparer.Ordinal);
    internal readonly Dictionary<string, Func<object, object?>> RuntimeMaps = new(StringComparer.Ordinal);
    private readonly HashSet<string> _ignoredMembers = new(StringComparer.Ordinal);
    private readonly Profile? _profile;
    private Type? _converterType;
    private Action<TSource, TDestination>? _beforeMap;
    private Action<TSource, TDestination>? _afterMap;
    private Func<TSource, TDestination, Task>? _beforeMapAsync;
    private Func<TSource, TDestination, Task>? _afterMapAsync;
    private Func<TSource, TDestination>? _constructionFactory;
    private readonly Dictionary<string, Func<TSource, TDestination, bool>> _memberConditions = new(StringComparer.Ordinal);

    public MappingExpression(Profile? profile = null)
    {
        _profile = profile;
    }

    /// <inheritdoc />
    public Type SourceType => typeof(TSource);

    /// <inheritdoc />
    public Type DestinationType => typeof(TDestination);

    /// <inheritdoc />
    public IReadOnlyCollection<string> IgnoredMembers => _ignoredMembers;

    /// <inheritdoc />
    IReadOnlyDictionary<string, string?> IMappingExpression.ExplicitMaps => ExplicitMaps;

    /// <inheritdoc />
    IReadOnlyDictionary<string, Func<object, object?>> IMappingExpression.RuntimeMaps => RuntimeMaps;

    /// <inheritdoc />
    public Type? ConverterType => _converterType;

    /// <inheritdoc />
    public string? ConstructionExpression => null; // Only available at compile-time via source gen

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> MemberConditions => new Dictionary<string, string>(); // Only available at compile-time

    Delegate? IMappingExpression.ConstructionFactory => _constructionFactory;

    IReadOnlyDictionary<string, Delegate> IMappingExpression.RuntimeConditions => _memberConditions.ToDictionary(k => k.Key, v => (Delegate)v.Value, StringComparer.Ordinal);

    internal Func<TSource, TDestination>? ConstructionFactory => _constructionFactory;

    internal IReadOnlyDictionary<string, Func<TSource, TDestination, bool>> RuntimeConditions => _memberConditions;

    /// <inheritdoc />
    public IMappingExpression<TSource, TDestination> ForMember<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> memberOptions)
    {
        var memberName = GetMemberName(destinationMember);
        var config = new MemberConfigurationExpression<TSource, TDestination, TMember>();
        memberOptions(config);

        if (config.IsIgnored)
        {
            _ignoredMembers.Add(memberName);
        }
        else if (config.MapFromExpression is not null)
        {
            // Store the compiled delegate for runtime fallback.
            ExplicitMaps[memberName] = null; // Placeholder; generator reads the text.

            var compiled = config.MapFromExpression.Compile();
            RuntimeMaps[memberName] = src => compiled((TSource)src);
        }

        if (config.ConditionPredicate is not null)
        {
            _memberConditions[memberName] = config.ConditionPredicate;
        }

        return this;
    }

    /// <inheritdoc />
    public IMappingExpression<TSource, TDestination> ForMemberIgnore<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember)
    {
        _ignoredMembers.Add(GetMemberName(destinationMember));
        return this;
    }

    /// <inheritdoc />
    public IMappingExpression<TDestination, TSource> ReverseMap()
    {
        var reverse = new MappingExpression<TDestination, TSource>(_profile);
        _profile?.AddMapping(reverse);
        return reverse;
    }

    /// <inheritdoc />
    public IMappingExpression<TSource, TDestination> ConvertUsing<TConverter>() where TConverter : ITypeConverter<TSource, TDestination>, new()
    {
        _converterType = typeof(TConverter);
        return this;
    }

    /// <inheritdoc />
    public IMappingExpression ConvertUsing(Type converterType)
    {
        _converterType = converterType;
        return this;
    }

    /// <inheritdoc />
    public IMappingExpression<TSource, TDestination> BeforeMap(Action<TSource, TDestination> action)
    {
        _beforeMap = action;
        return this;
    }

    /// <inheritdoc />
    public IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination> action)
    {
        _afterMap = action;
        return this;
    }

    /// <inheritdoc />
    public IMappingExpression<TSource, TDestination> BeforeMapAsync(Func<TSource, TDestination, Task> action)
    {
        _beforeMapAsync = action;
        return this;
    }

    /// <inheritdoc />
    public IMappingExpression<TSource, TDestination> AfterMapAsync(Func<TSource, TDestination, Task> action)
    {
        _afterMapAsync = action;
        return this;
    }

    /// <inheritdoc />
    public IMappingExpression<TSource, TDestination> ConstructUsing(Expression<Func<TSource, TDestination>> ctor)
    {
        _constructionFactory = ctor.Compile();
        return this;
    }

    internal void ExecuteBefore(TSource source, TDestination destination) => _beforeMap?.Invoke(source, destination);
    internal void ExecuteAfter(TSource source, TDestination destination) => _afterMap?.Invoke(source, destination);

    internal async Task ExecuteBeforeAsync(TSource source, TDestination destination)
    {
        _beforeMap?.Invoke(source, destination);
        if (_beforeMapAsync != null) await _beforeMapAsync(source, destination);
    }

    internal async Task ExecuteAfterAsync(TSource source, TDestination destination)
    {
        _afterMap?.Invoke(source, destination);
        if (_afterMapAsync != null) await _afterMapAsync(source, destination);
    }

    void IMappingExpression.ExecuteBefore(object source, object destination) => ExecuteBefore((TSource)source, (TDestination)destination);
    void IMappingExpression.ExecuteAfter(object source, object destination) => ExecuteAfter((TSource)source, (TDestination)destination);
    Task IMappingExpression.ExecuteBeforeAsync(object source, object destination) => ExecuteBeforeAsync((TSource)source, (TDestination)destination);
    Task IMappingExpression.ExecuteAfterAsync(object source, object destination) => ExecuteAfterAsync((TSource)source, (TDestination)destination);

    private static string GetMemberName<TMember>(Expression<Func<TDestination, TMember>> selector)
    {
        if (selector.Body is MemberExpression memberExpr)
        {
            return memberExpr.Member.Name;
        }

        throw new ArgumentException(
            $"The selector must be a simple member access expression, e.g. 'dest => dest.{typeof(TMember).Name}'.",
            nameof(selector));
    }
}

internal sealed class MemberConfigurationExpression<TSource, TDestination, TMember>
    : IMemberConfigurationExpression<TSource, TDestination, TMember>
{
    internal bool IsIgnored { get; private set; }
    internal Expression<Func<TSource, object?>>? MapFromExpression { get; private set; }
    internal Func<TSource, TDestination, bool>? ConditionPredicate { get; private set; }

    /// <inheritdoc />
    public void MapFrom<TResult>(Expression<Func<TSource, TResult>> mapExpression)
    {
        // The generated code stitches the raw lambda body; we keep the compiled delegate
        // only for the runtime fallback path.
        MapFromExpression = src => mapExpression.Compile()(src)!;
    }

    /// <inheritdoc />
    public void Ignore() => IsIgnored = true;

    /// <inheritdoc />
    public void MapFrom<TResolver>() where TResolver : IValueResolver<TSource, TMember>, new()
    {
        MapFromExpression = src => new TResolver().Resolve(src);
    }

    /// <inheritdoc />
    public void MapFromAsync<TResolver>() where TResolver : IAsyncValueResolver<TSource, TMember>, new()
    {
        // For runtime fallback, we use Task.Run/Result which is NOT recommended but 
        // this path is only for un-generated fallback/testing.
        MapFromExpression = src => new TResolver().ResolveAsync(src).GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public void Condition(Expression<Func<TSource, TDestination, bool>> condition)
    {
        ConditionPredicate = condition.Compile();
    }
}
