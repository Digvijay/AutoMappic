using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using AutoMappic;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

/// <summary>
/// AutoMappic is published as a Native AOT-friendly mapper. The source generator intercepts
/// the common calls, but <see cref="Mapper"/> still carries a reflection-based fallback engine.
/// Every public entry point into that engine has to be annotated, otherwise the trimming and
/// AOT analysers report the reflection from inside AutoMappic against the consumer's own
/// project, where it cannot be understood or acted on.
/// </summary>
public sealed class AotAnnotationTests
{
    private static IEnumerable<MethodInfo> PublicMappingMethods() =>
        typeof(Mapper)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName && m.Name.StartsWith("Map", StringComparison.Ordinal));

    /// <summary>The six public Map/MapAsync overloads, excluding the MapCore entry points.</summary>
    private static IEnumerable<MethodInfo> PublicMappingSurface() =>
        PublicMappingMethods().Where(m => !m.Name.StartsWith("MapCore", StringComparison.Ordinal));

    /// <summary>
    ///   The public mapping surface declares neither trimming nor AOT requirements, because after
    ///   the feature switch was introduced it no longer has any.
    /// </summary>
    /// <remarks>
    ///   This assertion is the inverse of what it used to be, and the reason is worth recording.
    ///   Annotating the public surface was the correct response to a real hazard - the reflective
    ///   fallback - but it made the advertised AOT-safe path warn for every consumer who was using
    ///   it correctly, because the source generator intercepts those call sites and no reflection
    ///   runs at all. The Roslyn trim analyser cannot see interceptors: they are applied after
    ///   analysers run, so it resolves the call to the annotated method and reports a hazard that
    ///   is not present in the shipped IL.
    ///
    ///   The fix was not to delete the annotation and hope. The fallback now sits behind
    ///   <c>AutoMappicFeatures.IsReflectionFallbackEnabled</c>, which ILLink.Substitutions.xml
    ///   stubs to <c>false</c> whenever the application is trimmed or published Native AOT.
    ///   Substitution happens before trim analysis, so the trimmer folds the branch away, removes
    ///   the reflective engine entirely, and has nothing left to warn about. The hazard is gone
    ///   rather than merely declared, which is why removing the annotation is honest here.
    /// </remarks>
    [Fact]
    public void PublicMappingSurface_DeclaresNoTrimOrAotRequirement()
    {
        var annotated = PublicMappingSurface()
            .Where(m => m.GetCustomAttribute<RequiresUnreferencedCodeAttribute>() is not null
                     || m.GetCustomAttribute<RequiresDynamicCodeAttribute>() is not null)
            .Select(m => m.Name)
            .Distinct()
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            annotated.Count == 0,
            "These public mapping methods still declare a trimming or AOT requirement, but the "
            + "reflective fallback they reach is removed by the feature switch when trimming, so "
            + "the requirement would warn every correct consumer for no reason: "
            + string.Join(", ", annotated));
    }

    /// <summary>
    ///   MapCore and MapCoreAsync declare both RequiresUnreferencedCode and RequiresDynamicCode,
    ///   because both requirements are genuinely true of them.
    /// </summary>
    /// <remarks>
    ///   RequiresUnreferencedCode is true because both recurse into nested and collection members
    ///   using the runtime type of each value, so the members they reach cannot be named
    ///   statically and a trimmer can legitimately remove them.
    ///
    ///   RequiresDynamicCode is true because materialising a destination collection calls
    ///   <c>Array.CreateInstance(Type, int)</c> for an item type known only at runtime, and
    ///   closing <c>Dictionary&lt;,&gt;</c> for an interface-typed destination needs
    ///   <c>MakeGenericType</c>. An earlier revision of this file asserted the opposite on the
    ///   grounds that the fallback is plain reflection. That was right about the property copying
    ///   and wrong about the collection path, and the assertion is corrected here rather than
    ///   left standing - an annotation test that asserts a convenient falsehood is worse than no
    ///   annotation test, because it actively defends the mistake.
    ///
    ///   These are public, so a consumer can call them directly and bypass the generator. They
    ///   are also the two that shipped unannotated and broke a consumer's AOT publish, which is
    ///   why they are covered by name.
    /// </remarks>
    [Fact]
    public void MapCoreEntryPoints_DeclareBothRequirements()
    {
        foreach (var name in new[] { "MapCore", "MapCoreAsync" })
        {
            var method = typeof(Mapper).GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(method);
            Assert.NotNull(method!.GetCustomAttribute<RequiresUnreferencedCodeAttribute>());
            Assert.NotNull(method!.GetCustomAttribute<RequiresDynamicCodeAttribute>());
        }
    }

    /// <summary>
    ///   The feature switch and its substitution file both exist and agree on the property name.
    /// </summary>
    /// <remarks>
    ///   Every claim above depends on the trimmer actually substituting the switch. If the
    ///   embedded resource were dropped, renamed, or the property renamed on one side only, the
    ///   substitution would silently stop applying: the build stays green, the tests stay green,
    ///   and consumers quietly get the reflective engine rooted in their AOT binary again. This
    ///   test pins the three things that have to line up.
    /// </remarks>
    [Fact]
    public void ReflectionFallbackFeatureSwitch_IsSubstitutable()
    {
        var xml = typeof(Mapper).Assembly.GetManifestResourceStream("ILLink.Substitutions.xml");
        Assert.NotNull(xml);

        using var reader = new StreamReader(xml!);
        var content = reader.ReadToEnd();

        Assert.Contains("AutoMappic.AutoMappicFeatures", content);
        Assert.Contains("get_IsReflectionFallbackEnabled", content);
        Assert.Contains("value=\"false\"", content);

        var property = typeof(Mapper).Assembly
            .GetType("AutoMappic.AutoMappicFeatures", throwOnError: true)!
            .GetProperty("IsReflectionFallbackEnabled", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(property);
        Assert.True(
            (bool)property!.GetValue(null)!,
            "The reflection fallback must default to enabled, so an untrimmed application keeps "
            + "working. It is the trimmer that turns it off, via substitution.");
    }
}
