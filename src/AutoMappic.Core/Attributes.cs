namespace AutoMappic;

/// <summary>
///   Marker attribute to signify that an assembly contains AutoMappic Profiles.
///   The source generator reads this during dependency injection setup to automatically
///   include profiles from referenced libraries without extra configuration.
/// </summary>
[global::System.AttributeUsage(global::System.AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class HasAutoMappicProfilesAttribute(params global::System.Type[] profiles) : global::System.Attribute
{
    /// <summary>Types of the profiles discovered in this assembly.</summary>
    public global::System.Type[] Profiles { get; } = profiles;
}

/// <summary>
///   Internal marker attribute to enable cross-project mapping discovery.
/// </summary>
[global::System.AttributeUsage(global::System.AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class MappingDiscoveryAttribute(global::System.Type source, global::System.Type destination) : global::System.Attribute
{
    /// <summary>Source type.</summary>
    public global::System.Type Source { get; } = source;
    /// <summary>Destination type.</summary>
    public global::System.Type Destination { get; } = destination;
}
