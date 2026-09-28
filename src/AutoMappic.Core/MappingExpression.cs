using System.Diagnostics.CodeAnalysis;
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
internal sealed class MappingExpression<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TSource,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TDestination>(Profile? profile = null) :
    IMappingExpression<TSource, TDestination>
{
    // Keyed by destination member name.
    internal readonly Dictionary<string, string?> ExplicitMaps = new(StringComparer.Ordinal);
    internal readonly Dictionary<string, Func<object, object?>> RuntimeMaps = new(StringComparer.Ordinal);
    private readonly HashSet<string> _ignoredMembers = new(StringComparer.Ordinal);
    private readonly Profile? _profile = profile;
    private Action<TSource, TDestination>? _beforeMap;
    private Action<TSource, TDestination>? _afterMap;
    private Func<TSource, TDestination, Task>? _beforeMapAsync;
    private Func<TSource, TDestination, Task>? _afterMapAsync;
    private readonly Dictionary<string, Func<TSource, TDestination, bool>> _memberConditions = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public INamingConvention? SourceNaming { get => field ?? _profile?.SourceNamingConvention; private set; }

    /// <inheritdoc />
    public INamingConvention? DestinationNaming { get => field ?? _profile?.DestinationNamingConvention; private set; }

    /// <inheritdoc />
    public bool SuppressUnmapped { get; private set; }

    /// <inheritdoc />
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
    public Type SourceType => typeof(TSource);

    /// <inheritdoc />
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    public Type DestinationType => typeof(TDestination);

    /// <inheritdoc />
    public IReadOnlyCollection<string> IgnoredMembers => _ignoredMembers;

    /// <inheritdoc />
    IReadOnlyDictionary<string, string?> IMappingExpression.ExplicitMaps => ExplicitMaps;

    /// <inheritdoc />
    IReadOnlyDictionary<string, Func<object, object?>> IMappingExpression.RuntimeMaps => RuntimeMaps;

    /// <inheritdoc />
    [field: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicMethods)]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicMethods)]
    public Type? ConverterType { get; private set; }

    /// <inheritdoc />
    public string? ConstructionExpression => null; // Only available at compile-time via source gen

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> MemberConditions => new Dictionary<string, string>(); // Only available at compile-time

    Delegate? IMappingExpression.ConstructionFactory => ConstructionFactory;

    IReadOnlyDictionary<string, Delegate> IMappingExpression.RuntimeConditions => _memberConditions.ToDictionary(k => k.Key, v => (Delegate)v.Value, StringComparer.Ordinal);

    internal Func<TSource, TDestination>? ConstructionFactory { get; private set; }

    internal IReadOnlyDictionary<string, Func<TSource, TDestination, bool>> RuntimeConditions => _memberConditions;

    /// <inheritdoc />
    [global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
        "AotAnalysis", "IL3050:RequiresDynamicCode",
        Justification = "Guarded by AutoMappicFeatures.IsReflectionFallbackEnabled, which "
            + "ILLink.Substitutions.xml stubs to false. When trimming or publishing Native AOT the "
            + "branch is folded away before analysis, so the expression compile is not reachable.")]
    public IMappingExpression<TSource, TDestination> ForMember<TMember>(
        Expression<Func<TDestination, TMember>> destinationMember,
        Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> memberOptions)
    {
        string memberName = GetMemberName(destinationMember);
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

            // Compiling is what makes this method need runtime code generation, and in a
            // generated application the delegate can never run: the generator reads the
            // expression at compile time and emits the member assignment directly. Gating the
            // compile behind the feature switch lets the trimmer remove it - and with it the
            // whole Expression.Compile dependency - instead of forcing every consumer of the
            // declarative API to declare a requirement they do not have.
            if (AutoMappicFeatures.IsReflectionFallbackEnabled)
            {
                RuntimeMaps[memberName] = BuildRuntimeMap(config.MapFromExpression);
            }
        }

        if (config.ConditionPredicate is not null)
        {
            _memberConditions[memberName] = config.ConditionPredicate;
        }

        return this;
    }

    /// <summary>
    ///   Isolates the <c>Expression.Compile()</c> so the trimmer sees a single reference
    ///   to it, reachable only from the feature-switched branch above.
    /// </summary>
    [RequiresDynamicCode("Compiling an expression tree needs runtime code generation.")]
    private static Func<object, object?> BuildRuntimeMap(Expression<Func<TSource, object?>> mapFrom)
    {
        var compiled = mapFrom.Compile();
        return src => compiled((TSource)src);
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
    public IMappingExpression<TSource, TDestination> ConvertUsing<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicMethods)] TConverter>() where TConverter : ITypeConverter<TSource, TDestination>, new()
    {
        ConverterType = typeof(TConverter);
        return this;
    }

    /// <inheritdoc />
    public IMappingExpression<TSource, TDestination> ConvertUsing(Expression<Func<TSource, TDestination>> converter)
    {
        if (AutoMappicFeatures.IsReflectionFallbackEnabled)
        {
            ConstructionFactory = converter.Compile();
        }

        return this;
    }

    /// <inheritdoc />
    public IMappingExpression ConvertUsing([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicMethods)] Type converterType)
    {
        ConverterType = converterType;
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
        if (AutoMappicFeatures.IsReflectionFallbackEnabled)
        {
            ConstructionFactory = ctor.Compile();
        }

        return this;
    }

    internal void ExecuteBefore(TSource source, TDestination destination) => _beforeMap?.Invoke(source, destination);
    internal void ExecuteAfter(TSource source, TDestination destination) => _afterMap?.Invoke(source, destination);

    internal async Task ExecuteBeforeAsync(TSource source, TDestination destination)
    {
        _beforeMap?.Invoke(source, destination);
        if (_beforeMapAsync != null) await _beforeMapAsync(source, destination).ConfigureAwait(false);
    }

    internal async Task ExecuteAfterAsync(TSource source, TDestination destination)
    {
        _afterMap?.Invoke(source, destination);
        if (_afterMapAsync != null) await _afterMapAsync(source, destination).ConfigureAwait(false);
    }

    void IMappingExpression.ExecuteBefore(object source, object destination)
    {
        if (source is null || destination is null) return;
        ExecuteBefore((TSource)source, (TDestination)destination);
    }
    void IMappingExpression.ExecuteAfter(object source, object destination)
    {
        if (source is null || destination is null) return;
        ExecuteAfter((TSource)source, (TDestination)destination);
    }
    Task IMappingExpression.ExecuteBeforeAsync(object source, object destination) => source is null || destination is null ? Task.CompletedTask : ExecuteBeforeAsync((TSource)source, (TDestination)destination);
    Task IMappingExpression.ExecuteAfterAsync(object source, object destination) => source is null || destination is null ? Task.CompletedTask : ExecuteAfterAsync((TSource)source, (TDestination)destination);

    /// <inheritdoc />
    public IMappingExpression<TSource, TDestination> WithNamingConvention(INamingConvention sourceNaming, INamingConvention destinationNaming)
    {
        SourceNaming = sourceNaming;
        DestinationNaming = destinationNaming;
        return this;
    }

    /// <inheritdoc />
    public IMappingExpression<TSource, TDestination> IgnoreUnmapped()
    {
        SuppressUnmapped = true;
        return this;
    }

    IMappingExpression IMappingExpression.IgnoreUnmapped() => IgnoreUnmapped();

    private static string GetMemberName<TMember>(Expression<Func<TDestination, TMember>> selector)
    {
        return selector.Body is MemberExpression memberExpr
            ? memberExpr.Member.Name
            : throw new ArgumentException(
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
        // The generated code stitches the raw lambda body; the compiled delegate exists only
        // for the runtime fallback, so it is built only when that fallback is reachable.
        if (AutoMappicFeatures.IsReflectionFallbackEnabled)
        {
            MapFromExpression = src => mapExpression.Compile()(src)!;
        }
    }

    /// <inheritdoc />
    public void Ignore() => IsIgnored = true;

    /// <inheritdoc />
    public void MapFrom<TResolver>() where TResolver : IValueResolver<TSource, TMember>, new() => MapFromExpression = src => new TResolver().Resolve(src);

    /// <inheritdoc />
    public void MapFromAsync<TResolver>() where TResolver : IAsyncValueResolver<TSource, TMember>, new() =>
        // For runtime fallback, we use Task.Run/Result which is NOT recommended but 
        // this path is only for un-generated fallback/testing.
        MapFromExpression = src => new TResolver().ResolveAsync(src).GetAwaiter().GetResult();

    /// <inheritdoc />
    [global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
        "AotAnalysis", "IL3050:RequiresDynamicCode",
        Justification = "Guarded by AutoMappicFeatures.IsReflectionFallbackEnabled, which "
            + "ILLink.Substitutions.xml stubs to false. When trimming or publishing Native AOT the "
            + "branch is folded away before analysis, so the expression compile is not reachable.")]
    public void Condition(Expression<Func<TSource, TDestination, bool>> condition)
    {
        if (AutoMappicFeatures.IsReflectionFallbackEnabled)
        {
            ConditionPredicate = CompileCondition(condition);
        }
    }

    /// <inheritdoc />
    public void ConvertUsing<TConverter, TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember) where TConverter : IValueConverter<TSourceMember, TMember>, new()
    {
        if (AutoMappicFeatures.IsReflectionFallbackEnabled)
        {
            MapFromExpression = src => new TConverter().Convert(sourceMember.Compile()(src));
        }
    }

    /// <summary>
    ///   Isolates the one <c>Expression.Compile()</c> that runs during configuration, so
    ///   the trimmer sees a single reference reachable only from the feature-switched branch.
    /// </summary>
    /// <remarks>
    ///   The other compiles on this type sit inside expression-tree bodies and only run if the
    ///   fallback actually evaluates them; this one is a direct call, so it needs isolating.
    /// </remarks>
    [RequiresDynamicCode("Compiling an expression tree needs runtime code generation.")]
    private static Func<TSource, TDestination, bool> CompileCondition(Expression<Func<TSource, TDestination, bool>> condition) =>
        condition.Compile();
}




