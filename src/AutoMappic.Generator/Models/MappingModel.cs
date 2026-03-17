using System.Collections.Generic;

namespace AutoMappic.Generator.Models;

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
///   cache.  It must implement value equality precisely so that the generator pipeline can
///   short-circuit and avoid re-emitting code when nothing structurally changed.
/// </remarks>
internal sealed record PropertyMap(
    string DestinationProperty,
    string? SourceExpression,
    PropertyMapKind Kind,
    bool IsInitOnly = false);

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

    /// <summary>The property was explicitly ignored via <c>ForMember … Ignore()</c>.</summary>
    Ignored,
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
    EquatableArray<PropertyMap> Properties)
{
    /// <summary>
    ///   A stable, file-system-safe identifier used as the hint name for
    ///   <c>context.AddSource()</c>, e.g. <c>Order_To_OrderDto</c>.
    /// </summary>
    public string HintName =>
        $"{Sanitise(SourceTypeFullName)}_To_{Sanitise(DestinationTypeFullName)}";

    private static string Sanitise(string name) =>
        name.Replace('.', '_').Replace('<', '_').Replace('>', '_');
}

internal enum InterceptKind
{
    Map,
    ProjectTo,
    DataReaderMap
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
    InterceptKind Kind = InterceptKind.Map);
