using System.Collections.Immutable;
using System.Linq;

namespace AutoMappic.Generator.Models;

/// <summary>Serializable diagnostic information that doesn't pin live SyntaxTree references.</summary>
internal sealed record DiagnosticInfo(
    string DescriptorId,
    LocationInfo Location,
    EquatableArray<string> MessageArgs,
    global::System.Collections.Immutable.ImmutableDictionary<string, string?> Properties,
    Microsoft.CodeAnalysis.DiagnosticSeverity Severity = Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
{
    public string Id => DescriptorId;

    public static DiagnosticInfo Create(Microsoft.CodeAnalysis.DiagnosticDescriptor descriptor, Microsoft.CodeAnalysis.Location loc, global::System.Collections.Immutable.ImmutableDictionary<string, string?> props, params object[] messageArgs)
    {
        return new DiagnosticInfo(
            descriptor.Id,
            LocationInfo.Create(loc),
            new EquatableArray<string>(messageArgs.Select(static a => a?.ToString() ?? "")),
            props,
            descriptor.DefaultSeverity);
    }

    public static DiagnosticInfo Create(Microsoft.CodeAnalysis.DiagnosticDescriptor descriptor, Microsoft.CodeAnalysis.Location loc, params object[] messageArgs)
    {
        return Create(descriptor, loc, global::System.Collections.Immutable.ImmutableDictionary<string, string?>.Empty, messageArgs);
    }

    public string GetMessage(global::System.Globalization.CultureInfo? culture = null)
    {
        string? format = DescriptorId switch
        {
            "AM0001" => AutoMappicDiagnostics.UnmappedProperty.MessageFormat.ToString(culture),
            "AM0002" => AutoMappicDiagnostics.AmbiguousMapping.MessageFormat.ToString(culture),
            "AM0003" => AutoMappicDiagnostics.CreateMapOutsideProfile.MessageFormat.ToString(culture),
            "AM0004" => AutoMappicDiagnostics.UnresolvedInterceptorMapping.MessageFormat.ToString(culture),
            "AM0005" => AutoMappicDiagnostics.MissingConstructor.MessageFormat.ToString(culture),
            "AM0006" => AutoMappicDiagnostics.CircularReference.MessageFormat.ToString(culture),
            "AM0007" => AutoMappicDiagnostics.UnresolvedCreateMapSymbol.MessageFormat.ToString(culture),
            "AM0008" => AutoMappicDiagnostics.UnsupportedProjectToFeature.MessageFormat.ToString(culture),
            "AM0009" => AutoMappicDiagnostics.DuplicateMapping.MessageFormat.ToString(culture),
            "AM0010" => AutoMappicDiagnostics.PerformanceHotpath.MessageFormat.ToString(culture),
            "AM0011" => AutoMappicDiagnostics.UnsupportedMultiSourceProjectTo.MessageFormat.ToString(culture),
            "AM0012" => AutoMappicDiagnostics.AsymmetricMapping.MessageFormat.ToString(culture),
            "AM0013" => AutoMappicDiagnostics.PatchIntoRequired.MessageFormat.ToString(culture),
            "AM0014" => AutoMappicDiagnostics.UnmappedPrimaryKey.MessageFormat.ToString(culture),
            "AM0015" => AutoMappicDiagnostics.SmartMatchSuggestion.MessageFormat.ToString(culture),
            "AM0016" => AutoMappicDiagnostics.PerformanceRegression.MessageFormat.ToString(culture),
            "AM0017" => AutoMappicDiagnostics.AmbiguousEntityKey.MessageFormat.ToString(culture),
            "AM0018" => AutoMappicDiagnostics.NonPartialClass.MessageFormat.ToString(culture),
            _ => "Diagnostic " + DescriptorId
        };
        try
        {
            return string.Format(culture ?? global::System.Globalization.CultureInfo.InvariantCulture, format, MessageArgs.ToArray());
        }
        catch { return format; }
    }
}

/// <summary>Serializable location information.</summary>
internal sealed record LocationInfo(
    string FilePath,
    int StartLine,
    int StartColumn,
    int EndLine,
    int EndColumn,
    int SourceStart = 0,
    int SourceLength = 0)
{
    public static LocationInfo Create(Microsoft.CodeAnalysis.Location loc)
    {
        var span = loc.GetLineSpan();
        return new LocationInfo(
            span.Path ?? "",
            span.StartLinePosition.Line,
            span.StartLinePosition.Character,
            span.EndLinePosition.Line,
            span.EndLinePosition.Character,
            loc.SourceSpan.Start,
            loc.SourceSpan.Length);
    }
}

/// <summary>
///   Immutable, fully equality-comparable model representing a single property assignment
///   to be emitted into the generated mapping method.
/// </summary>
/// <remarks>
///   <para>
///     <see cref="DestinationProperty" />: The name of the destination property, e.g. <c>CustomerCity</c>.
///   </para>
///   <para>
///     <see cref="SourceExpression" />: The resolved source expression to emit, e.g. <c>source.Customer?.City</c>.
///     When <see langword="null" /> the property is explicitly ignored.
///   </para>
///   <para>
///     <see cref="Kind" />: Indicates how this assignment was resolved.
///   </para>
///   This record is extracted from Roslyn symbols and stored in the Incremental Generator
///   cache. It must implement value equality precisely so that the generator pipeline can
///   short-circuit and avoid re-emitting code when nothing structurally changed.
/// </remarks>
internal sealed record PropertyMap(
    string DestinationProperty,
    string? SourceExpression,
    PropertyMapKind Kind,
    bool IsInitOnly = false,
    string? NestedSourceTypeFullName = null,
    string? NestedDestTypeFullName = null,
    bool IsCollection = false,
    bool IsArray = false,
    string? NestedExpression = null,
    string? SourceRawExpression = null,
    bool IsReadOnly = false,
    bool IsAsync = false,
    string? ConditionBody = null,
    string? NestedFullDestTypeFullName = null,
    string? DataReaderColumn = null,
    string? NestedDestKeyProperty = null,
    string? NestedDestKeyTypeFullName = null,
    bool IsNestedDestKeyValueType = false,
    bool SourceCanBeNull = false,
    bool IsRequired = false,
    bool IsKey = false,
    bool DeleteOrphans = false);

/// <summary>Describes how a <see cref="PropertyMap" /> was resolved by the convention engine.</summary>
internal enum PropertyMapKind
{
    /// <summary>A direct name match: <c>source.Name</c>.</summary>
    Direct,

    /// <summary>A flattened path match: <c>source.Customer?.Name</c>.</summary>
    Flattened,

    /// <summary>A method-to-property match: <c>source.GetTotal()</c>.</summary>
    Method,

    /// <summary>An explicit <c>MapFrom</c> lambda stitched from source text.</summary>
    Explicit,

    /// <summary>The property was explicitly ignored via <c>ForMember ... Ignore()</c>.</summary>
    Ignored,

    /// <summary>The property is unmapped but a smart-match suggestion was reported (suppresses generic error).</summary>
    Suggested,
}

/// <summary>
///   Immutable model for a complete type-pair mapping discovered from a
///   <c>CreateMap&lt;TSource, TDestination&gt;()</c> call inside a <c>Profile</c> subclass.
/// </summary>
/// <remarks>
///   <para>
///     <see cref="SourceTypeFullName" />: The fully-qualified name of the source type.
///   </para>
///   <para>
///     <see cref="SourceTypeName" />: The simple (unqualified) name of the source type.
///   </para>
///   <para>
///     <see cref="DestinationTypeFullName" />: The fully-qualified name of the destination type.
///   </para>
///   <para>
///     <see cref="DestinationTypeName" />: The simple (unqualified) name of the destination type.
///   </para>
///   <para>
///     <see cref="Properties" />: All property assignments to emit for this mapping pair.
///   </para>
/// </remarks>
internal sealed record MappingModel(
    string SourceTypeFullName,
    string SourceTypeName,
    string DestinationTypeFullName,
    string DestinationTypeName,
    EquatableArray<PropertyMap> Properties,
    EquatableArray<PropertyMap> ConstructorArguments,
    EquatableArray<PropertyMap> ProjectionProperties,
    EquatableArray<PropertyMap> ProjectionConstructorArguments,
    string? SourceNamespace = null,
    string? DestinationNamespace = null,
    string? TypeConverterFullName = null,
    string? FilePath = null,
    int Line = 0,
    int Column = 0,
    string? BeforeMapBody = null,
    string? AfterMapBody = null,
    string? BeforeMapAsyncBody = null,
    string? AfterMapAsyncBody = null,
    string? ConstructionBody = null,
    string? SourceNamingStrategyFullName = null,
    string? DestinationNamingStrategyFullName = null,
    bool EnablePerformanceProfiling = false,
    string? ProfileName = null,
    bool IsSourceValueType = false,
    bool IsDestinationValueType = false,
    EquatableArray<string>? UnmappedProperties = null,
    EquatableArray<string>? TypeParameters = null,
    bool EnableIdentityManagement = true,
    bool EnableEntitySync = true,
    double SmartMatchThreshold = 0.5,
    string? StaticConverterMethodFullName = null,
    bool DeleteOrphans = true)
{
    /// <summary>Returns true if any property in this mapping is resolved asynchronously.</summary>
    public bool IsAsync => Properties.Any(p => p.IsAsync) || ConstructorArguments.Any(p => p.IsAsync) || !string.IsNullOrEmpty(BeforeMapAsyncBody) || !string.IsNullOrEmpty(AfterMapAsyncBody);

    /// <summary>
    ///   A stable, file-system-safe identifier used as the hint name for
    ///   <c>context.AddSource()</c>, e.g. <c>Order_To_OrderDto</c>.
    /// </summary>
    public string HintName =>
        $"{Pipeline.SourceEmitter.Sanitise(SourceTypeFullName, true)}_To_{Pipeline.SourceEmitter.Sanitise(DestinationTypeFullName, true)}";
}

internal enum InterceptKind
{
    Map,
    ProjectTo,
    DataReaderMap,
    DataReaderMapAsync
}

/// <summary>
///   Carries the file path, one-based line, and one-based column of an interception
///   call site for use in emitting <c>[InterceptsLocation]</c> attributes.
/// </summary>
internal sealed record InterceptLocation(
    string FilePath,
    int Line,
    int Column,
    string SourceTypeFullName,
    string DestinationTypeFullName,
    string MethodSignatureKey,
    string ParameterSourceTypeFullName,
    InterceptKind Kind,
    bool IsCollectionMapping = false,
    bool IsDestinationMapped = false,
    bool IsExtensionMap = false,
    string? EffectiveSourceTypeFullName = null,
    string? EffectiveDestTypeFullName = null,
    string? GenericParameters = null,
    EquatableArray<string>? TypeArguments = null,
    EquatableArray<string>? ExtraParameters = null);
