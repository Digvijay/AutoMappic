using Microsoft.CodeAnalysis;

namespace AutoMappic.Generator;

internal static class AutoMappicDiagnostics
{
    private const string Category = "AutoMappic";

    public static readonly DiagnosticDescriptor UnmappedProperty = new DiagnosticDescriptor("AM0001", "Unmapped destination property", "Property '{0}' on '{1}' has no matching source in '{2}'. Add a matching property or use [AutoMappicIgnore].", Category, DiagnosticSeverity.Error, true, "AutoMappic requires every writable destination property to be mapped or explicitly ignored.");

    public static readonly DiagnosticDescriptor AmbiguousMapping = new DiagnosticDescriptor("AM0002", "Ambiguous property mapping", "Property '{0}' on '{1}' is ambiguous between a direct match and a flattened path", Category, DiagnosticSeverity.Error, true, "Ambiguity between a direct property match and a flattened path match must be resolved explicitly.");

    public static readonly DiagnosticDescriptor CreateMapOutsideProfile = new DiagnosticDescriptor("AM0003", "CreateMap called outside a Profile constructor", "CreateMap<{0}, {1}>() must be called inside a Profile constructor", Category, DiagnosticSeverity.Warning, true, "CreateMap calls are only processed when located in the constructor of a Profile subclass.");

    public static readonly DiagnosticDescriptor UnresolvedInterceptorMapping = new DiagnosticDescriptor("AM0004", "No generated mapping for intercepted call", "No AutoMappic mapping was generated for 'IMapper.Map<{0}>({1})'", Category, DiagnosticSeverity.Warning, true, "No static mapping method was generated so the call will use reflective fallback.");

    public static readonly DiagnosticDescriptor MissingConstructor = new DiagnosticDescriptor("AM0005", "No compatible constructor found", "Destination type '{0}' must have a public parameterless constructor", Category, DiagnosticSeverity.Error, true, "A public constructor is required to instantiate the destination type.");

    public static readonly DiagnosticDescriptor CircularReference = new DiagnosticDescriptor("AM0006", "Circular reference detected", "Circular reference detected: mapping '{0}' -> '{1}' eventually maps back to itself", Category, DiagnosticSeverity.Warning, true, "Recursive object graphs are not supported by the static generator and will trigger runtime fallback.");

    public static readonly DiagnosticDescriptor UnresolvedCreateMapSymbol = new DiagnosticDescriptor("AM0007", "Could not resolve CreateMap symbol", "The generator found a CreateMap call but could not resolve its symbol", Category, DiagnosticSeverity.Warning, true, "This usually indicates a missing library reference or compilation errors.");

    public static readonly DiagnosticDescriptor UnsupportedProjectToFeature = new DiagnosticDescriptor("AM0008", "Unsupported ProjectTo feature", "ProjectTo for mapping '{0}' -> '{1}' contains procedural logic unsupported by SQL", Category, DiagnosticSeverity.Warning, true, "LINQ providers cannot translate procedural C# logic like conditions or hooks to SQL.");

    public static readonly DiagnosticDescriptor DuplicateMapping = new DiagnosticDescriptor("AM0009", "Duplicate mapping configuration", "Mapping for '{0}' -> '{1}' is defined in multiple profiles", Category, DiagnosticSeverity.Warning, true, "Only the first discovered mapping configuration will be used by interceptors.");

    public static readonly DiagnosticDescriptor PerformanceHotpath = new DiagnosticDescriptor("AM0010", "Performance hotpath detected", "Mapping '{0}' -> '{1}' uses nested collection mapping", Category, DiagnosticSeverity.Info, true, "Nested collection mapping in deep graphs can impact allocation performance.");

    public static readonly DiagnosticDescriptor UnsupportedMultiSourceProjectTo = new DiagnosticDescriptor("AM0011", "Multi-Source ProjectTo not supported", "ProjectTo for multi-source mapping '{0}' -> '{1}' is not supported", Category, DiagnosticSeverity.Error, true, "Projection from multiple sources is currently only supported for in-memory mapping.");

    public static readonly DiagnosticDescriptor AsymmetricMapping = new DiagnosticDescriptor("AM0012", "Asymmetric mapping configuration", "Mapping for '{0}' -> '{1}' has no writable destination properties", Category, DiagnosticSeverity.Warning, true, "The mapping might be intended to be a projection only, but no writable properties were found on the destination.");

    public static readonly DiagnosticDescriptor PatchIntoRequired = new DiagnosticDescriptor("AM0013", "Required property patch mismatch", "Property '{0}' on '{1}' is 'required' but mapped from nullable source '{2}' without a default fallback", Category, DiagnosticSeverity.Warning, true, "Patch Mode skips nulls, leaving 'required' properties uninitialized if no existing value was present.");

    public static readonly DiagnosticDescriptor UnmappedPrimaryKey = new DiagnosticDescriptor("AM0014", "Unmapped primary key", "Entity collection mapping for '{0}' lacks a mapped primary key from source '{1}'. Existing entities cannot be updated and will be appended.", Category, DiagnosticSeverity.Warning, true, "Syncing Entity collections requires a matching primary key between Source and Destination types to avoid data duplication.");

    public static readonly DiagnosticDescriptor SmartMatchSuggestion = new DiagnosticDescriptor("AM0015", "Smart-Match property suggestion", "Property '{0}' on '{1}' is unmapped. Did you mean to map it from '{2}'?.", Category, DiagnosticSeverity.Error, true, "The generator detected a closely named property in the source which might be a typo or slight naming mismatch.");

    public static readonly DiagnosticDescriptor PerformanceRegression = new DiagnosticDescriptor("AM0016", "Performance regression warning", "Mapping collection '{0}' -> '{1}' uses a custom resolver for items, preventing compiler loop vectorization optimizations", Category, DiagnosticSeverity.Warning, true, "Heavy custom logic inside loops impacts performance. Consider static mapping or Global Type Converters.");

    public static readonly DiagnosticDescriptor AmbiguousEntityKey = new DiagnosticDescriptor("AM0017", "Ambiguous entity key detected", "Potential Entity '{0}' mapped in collection '{1}' has no identifiable primary key. Syncing will fallback to 'Clear and Add'.", Category, DiagnosticSeverity.Warning, true, "To enable robust Entity sync, ensure the Destination has a [Key] attribute or an 'Id' property.");

    public static readonly DiagnosticDescriptor NonPartialClass = new DiagnosticDescriptor("AM0018", "Class must be partial to support source generation", "Class '{0}' decorated with [AutoMap] must be partial", Category, DiagnosticSeverity.Error, true, "Standalone mappings require the class to be partial so the generator can inject additional members into the type.");
}
