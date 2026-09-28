using System.Diagnostics.CodeAnalysis;

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

    /// <summary>Naming strategy for source member names (defaults to <see cref="PascalCaseNamingConvention" />).</summary>
    public INamingConvention SourceNamingConvention { get; set; } = new PascalCaseNamingConvention();

    /// <summary>Naming strategy for destination member names (defaults to <see cref="PascalCaseNamingConvention" />).</summary>
    public INamingConvention DestinationNamingConvention { get; set; } = new PascalCaseNamingConvention();

    /// <summary>
    ///   When <see langword="true" />, the source generator will inject diagnostic performance markers
    ///   into the generated mapping code to help identify high-latency property assignments.
    /// </summary>
    public bool EnablePerformanceProfiling { get; set; }

    /// <summary>
    ///   When <see langword="true" />, the source generator will enable "Smart-Sync" for collections,
    ///   matching existing destination entities by key and updating them in-place instead of replacing the entire collection.
    /// </summary>
    public bool EnableEntitySync { get; set; }

    /// <summary>
    ///   When <see langword="true" />, the source generator will enable identity management (caching),
    ///   ensuring that the same source entity instance is always mapped to the same destination instance
    ///   within the same mapping context.
    /// </summary>
    public bool EnableIdentityManagement { get; set; }

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
    /// <remarks>
    ///   This method itself contains no dynamic code: it allocates
    ///   <c>MappingExpression&lt;TSource, TDestination&gt;</c> over two statically known type
    ///   arguments. It is therefore <em>not</em> annotated with
    ///   <see cref="RequiresDynamicCodeAttribute" />.
    /// </remarks>
    /// <remarks>
    ///   It is no longer annotated with <see cref="RequiresUnreferencedCodeAttribute" /> either.
    ///   That annotation made the library's own advertised path unusable under trimming: this is
    ///   the declarative API the source generator reads, so every correct consumer - including
    ///   one that only ever executes generated code - got a trim warning for merely declaring a
    ///   profile. The requirement belongs to the runtime fallback, not to the declaration, so the
    ///   type parameters now carry <see cref="DynamicallyAccessedMembersAttribute" /> instead.
    ///   The trimmer preserves exactly the members the fallback would reflect over, which makes
    ///   the fallback genuinely safe rather than merely warned about.
    /// </remarks>
    protected internal IMappingExpression<TSource, TDestination> CreateMap<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TSource,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TDestination>()
    {
        var expression = new MappingExpression<TSource, TDestination>(this);
        _mappings.Add(expression);
        return expression;
    }

    /// <summary>
    ///   Declares a mapping between open generic types or other runtime-resolved types.
    /// </summary>
    /// <param name="sourceType">The source type (generic or closed).</param>
    /// <param name="destinationType">The destination type (generic or closed).</param>
    /// <returns>A non-generic configuration expression.</returns>
    protected internal IMappingExpression CreateMap(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)] Type sourceType,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)] Type destinationType)
    {
        var expr = new OpenGenericMappingExpression(sourceType, destinationType);
        _mappings.Add(expr);
        return expr;
    }
}

internal sealed class OpenGenericMappingExpression(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] Type s,
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type d) : IMappingExpression
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
    public Type SourceType => s;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
    public Type DestinationType => d;

    public IReadOnlyCollection<string> IgnoredMembers => [];
    public IReadOnlyDictionary<string, string?> ExplicitMaps => new Dictionary<string, string?>();
    public IReadOnlyDictionary<string, Func<object, object?>> RuntimeMaps => new Dictionary<string, Func<object, object?>>();

    [field: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicMethods)]
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicMethods)]
    public Type? ConverterType { get; private set; }
    public string? ConstructionExpression => null;
    public IReadOnlyDictionary<string, string> MemberConditions => new Dictionary<string, string>();
    public Delegate? ConstructionFactory => null;
    public IReadOnlyDictionary<string, Delegate> RuntimeConditions => new Dictionary<string, Delegate>();
    public INamingConvention? SourceNaming => null;
    public INamingConvention? DestinationNaming => null;
    public bool SuppressUnmapped { get; private set; }
    public IMappingExpression ConvertUsing([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicMethods)] Type converterType)
    {
        ConverterType = converterType;
        return this;
    }

    public void ExecuteBefore(object source, object destination) { }
    public void ExecuteAfter(object source, object destination) { }
    public global::System.Threading.Tasks.Task ExecuteBeforeAsync(object source, object destination) => global::System.Threading.Tasks.Task.CompletedTask;
    public global::System.Threading.Tasks.Task ExecuteAfterAsync(object source, object destination) => global::System.Threading.Tasks.Task.CompletedTask;

    public IMappingExpression IgnoreUnmapped()
    {
        SuppressUnmapped = true;
        return this;
    }
}


