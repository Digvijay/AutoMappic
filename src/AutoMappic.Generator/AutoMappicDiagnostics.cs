using Microsoft.CodeAnalysis;

namespace AutoMappic.Generator;

internal static class AutoMappicDiagnostics
{
    private const string Category = "AutoMappic";

    public static readonly DiagnosticDescriptor UnmappedProperty = new DiagnosticDescriptor("AM001", "Unmapped destination property", "Property '{0}' on '{1}' has no matching source in '{2}'. Add a matching property or use [AutoMappicIgnore].", Category, DiagnosticSeverity.Error, true, "AutoMappic requires every writable destination property to be mapped or explicitly ignored.");

    public static readonly DiagnosticDescriptor AmbiguousMapping = new DiagnosticDescriptor("AM002", "Ambiguous property mapping", "Property '{0}' on '{1}' is ambiguous between a direct match and a flattened path", Category, DiagnosticSeverity.Error, true, "Ambiguity between a direct property match and a flattened path match must be resolved explicitly.");

    public static readonly DiagnosticDescriptor CreateMapOutsideProfile = new DiagnosticDescriptor("AM003", "CreateMap called outside a Profile constructor", "CreateMap<{0}, {1}>() must be called inside a Profile constructor", Category, DiagnosticSeverity.Warning, true, "CreateMap calls are only processed when located in the constructor of a Profile subclass.");

    public static readonly DiagnosticDescriptor UnresolvedInterceptorMapping = new DiagnosticDescriptor("AM004", "No generated mapping for intercepted call", "No AutoMappic mapping was generated for 'IMapper.Map<{0}>({1})'", Category, DiagnosticSeverity.Warning, true, "No static mapping method was generated so the call will use reflective fallback.");

    public static readonly DiagnosticDescriptor MissingConstructor = new DiagnosticDescriptor("AM005", "No compatible constructor found", "Destination type '{0}' must have a public parameterless constructor", Category, DiagnosticSeverity.Error, true, "A public constructor is required to instantiate the destination type.");

    public static readonly DiagnosticDescriptor CircularReference = new DiagnosticDescriptor("AM006", "Circular reference detected", "Circular reference detected: mapping '{0}' -> '{1}' eventually maps back to itself", Category, DiagnosticSeverity.Error, true, "Recursive object graphs are not supported by the static generator.");

    public static readonly DiagnosticDescriptor UnresolvedCreateMapSymbol = new DiagnosticDescriptor("AM007", "Could not resolve CreateMap symbol", "The generator found a CreateMap call but could not resolve its symbol", Category, DiagnosticSeverity.Warning, true, "This usually indicates a missing library reference or compilation errors.");

    public static readonly DiagnosticDescriptor UnsupportedProjectToFeature = new DiagnosticDescriptor("AM008", "Unsupported ProjectTo feature", "ProjectTo for mapping '{0}' -> '{1}' contains procedural logic unsupported by SQL", Category, DiagnosticSeverity.Warning, true, "LINQ providers cannot translate procedural C# logic like conditions or hooks to SQL.");

    public static readonly DiagnosticDescriptor DuplicateMapping = new DiagnosticDescriptor("AM009", "Duplicate mapping configuration", "Mapping for '{0}' -> '{1}' is defined in multiple profiles", Category, DiagnosticSeverity.Warning, true, "Only the first discovered mapping configuration will be used by interceptors.");

    public static readonly DiagnosticDescriptor PerformanceHotpath = new DiagnosticDescriptor("AM010", "Performance hotpath detected", "Mapping '{0}' -> '{1}' uses nested collection mapping", Category, DiagnosticSeverity.Info, true, "Nested collection mapping in deep graphs can impact allocation performance.");

    public static readonly DiagnosticDescriptor UnsupportedMultiSourceProjectTo = new DiagnosticDescriptor("AM011", "Multi-Source ProjectTo not supported", "ProjectTo for multi-source mapping '{0}' -> '{1}' is not supported", Category, DiagnosticSeverity.Error, true, "Projection from multiple sources is currently only supported for in-memory mapping.");

    public static readonly DiagnosticDescriptor AsymmetricMapping = new DiagnosticDescriptor("AM012", "Asymmetric mapping configuration", "Mapping for '{0}' -> '{1}' has no writable destination properties", Category, DiagnosticSeverity.Warning, true, "The mapping might be intended to be a projection only, but no writable properties were found on the destination.");
}
