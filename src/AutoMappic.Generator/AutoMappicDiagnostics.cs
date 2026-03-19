using Microsoft.CodeAnalysis;

namespace AutoMappic.Generator;

/// <summary>
///   All Roslyn <see cref="DiagnosticDescriptor" /> instances emitted by AutoMappic.
/// </summary>
/// <remarks>
///   Diagnostics follow the <c>AM</c> prefix convention.  Every diagnostic that is an
///   error blocks a successful build, turning what AutoMapper deferred to a runtime
///   <c>AutoMapperConfigurationException</c> into a compile-time failure.
/// </remarks>
internal static class AutoMappicDiagnostics
{
    private const string Category = "AutoMappic";

    /// <summary>
    ///   Emitted when a destination property cannot be mapped by any convention, has no
    ///   explicit <c>MapFrom</c> rule, and has not been ignored.
    /// </summary>
    public static readonly DiagnosticDescriptor UnmappedProperty = new(
        id: "AM001",
        title: "Unmapped destination property",
        messageFormat: "Property '{0}' on '{1}' has no matching source in '{2}'. " +
                       "Add a matching property, use ForMember to map it explicitly, or call ForMemberIgnore to suppress this error.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
            "AutoMappic requires every writable destination property to be " +
            "mapped or explicitly ignored for Native AOT safety. " +
            "This replaces AutoMapper's runtime AssertConfigurationIsValid().");

    /// <summary>
    ///   Emitted when a destination property name is ambiguous between a direct match and
    ///   a flattened path match, e.g. <c>CustomerName</c> exists both as a direct property
    ///   and as a flattened path <c>Customer.Name</c>.
    /// </summary>
    public static readonly DiagnosticDescriptor AmbiguousMapping = new(
        id: "AM002",
        title: "Ambiguous property mapping",
        messageFormat: "Property '{0}' on '{1}' is ambiguous. It matches both a direct source property and a flattened path '{2}'. " +
                       "Use ForMember to resolve the ambiguity explicitly.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
            "When both a direct property and a flattened path match the same destination property name, " +
            "AutoMappic cannot safely choose. You must specify the intended source via ForMember.");

    /// <summary>
    ///   Emitted when a <c>CreateMap</c> call is found outside of a <c>Profile</c> subclass
    ///   constructor, which is the only supported location.
    /// </summary>
    public static readonly DiagnosticDescriptor CreateMapOutsideProfile = new(
        id: "AM003",
        title: "CreateMap called outside a Profile constructor",
        messageFormat: "CreateMap<{0}, {1}>() must be called inside the constructor of a class that inherits from AutoMappic.Profile",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "AutoMappic only processes CreateMap calls that appear inside the constructor of a Profile subclass. " +
            "Calls in other locations are silently ignored by the generator.");

    /// <summary>
    ///   Emitted when a call to <c>IMapper.Map</c> is detected but no generated mapping
    ///   exists for that specific type pair, so the interceptor cannot be emitted.
    /// </summary>
    public static readonly DiagnosticDescriptor UnresolvedInterceptorMapping = new(
        id: "AM004",
        title: "No generated mapping for intercepted call",
        messageFormat: "No AutoMappic mapping was generated for the call 'IMapper.Map<{0}>({1})'. " +
                       "Ensure a Profile.CreateMap<{1}, {0}>() exists and the generator has run.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "An IMapper.Map call was detected but no corresponding static mapping method was generated. " +
            "The call will fall through to the runtime Mapper, which uses reflection.");

    /// <summary>
    ///   Emitted when a destination type does not have a parameterless constructor,
    ///   making it impossible for the generator to instantiate it.
    /// </summary>
    public static readonly DiagnosticDescriptor MissingConstructor = new(
        id: "AM005",
        title: "No compatible constructor found",
        messageFormat: "Destination type '{0}' must have a public parameterless constructor or one whose parameters can be satisfied by matching source members",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
            "AutoMappic requires a public constructor to instantiate the destination type. " +
            "It can use a parameterless constructor or a parameterized one if all its arguments " +
            "can be resolved from the source by name convention or explicit mapping.");

    /// <summary>
    ///   Emitted when a circular reference is detected in the mapping graph, which would
    ///   cause a StackOverflowException at runtime.
    /// </summary>
    public static readonly DiagnosticDescriptor CircularReference = new(
        id: "AM006",
        title: "Circular reference detected",
        messageFormat: "Circular reference detected: mapping '{0}' -> '{1}' eventually maps back to itself. " +
                       "For AOT safety and to prevent StackOverflow, circular references are only supported via ForMemberIgnore or explicit manual mapping.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
            "AutoMappic's static generator does not support recursive object graphs by default " +
            "as they require a runtime object tracker which is expensive for performance and AOT. " +
            "Ignore the recursive property or use a custom resolver.");

    /// <summary>
    ///   Emitted when a <c>CreateMap</c> call is identified by syntax but the semantic model
    ///   cannot resolve the method symbol.
    /// </summary>
    public static readonly DiagnosticDescriptor UnresolvedCreateMapSymbol = new(
        id: "AM007",
        title: "Could not resolve CreateMap symbol",
        messageFormat: "The generator found a CreateMap call but could not resolve its symbol. Ensure AutoMappic.Core is correctly referenced.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "A CreateMap call was identified by syntax but the semantic model couldn't find the corresponding method symbol. " +
            "This usually means the project is missing a reference to AutoMappic.Core or there are compilation errors preventing resolution.");
}
