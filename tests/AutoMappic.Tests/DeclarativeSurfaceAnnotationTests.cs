using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

/// <summary>
///   The declarative configuration surface - ForMember, ConvertUsing, ConstructUsing, MapFrom,
///   Condition and friends - must not declare trimming or AOT requirements.
/// </summary>
/// <remarks>
///   This is the defect that produced 72 of viking-air's ILC errors, every one of them reported
///   against viking-air's own BookingProfile rather than against AutoMappic. The configuration
///   methods compiled each expression eagerly to populate the runtime fallback, so they honestly
///   needed RequiresDynamicCode - and that requirement then landed on every consumer who wrote a
///   perfectly ordinary profile, in an application where the generator had already emitted the
///   member assignments and the compiled delegate could never run.
///
///   The fix was the same one applied to Mapper's constructor: build the fallback only when the
///   fallback is reachable. Each Expression.Compile now sits behind
///   AutoMappicFeatures.IsReflectionFallbackEnabled, so the trimmer removes it along with the
///   expression-compilation dependency, and the requirement stops being true.
///
///   Without this test the annotations creep straight back, because adding one makes a local
///   analyzer error disappear and nothing else objects.
/// </remarks>
public sealed class DeclarativeSurfaceAnnotationTests
{
    private const string SwitchName = "AutoMappic.IsReflectionFallbackEnabled";

    private static readonly string[] ConfigurationMethods =
    [
        "ForMember", "ForMemberIgnore", "ReverseMap", "ConvertUsing", "ConstructUsing",
        "MapFrom", "MapFromAsync", "Condition", "Ignore",
    ];

    private static Type Resolve(string name) =>
        typeof(IMapper).Assembly.GetType(name, throwOnError: true)!;

    public static IEnumerable<object[]> DeclarativeTypes() =>
    [
        [typeof(IMappingExpression<,>)],
        [typeof(IMemberConfigurationExpression<,,>)],
        [Resolve("AutoMappic.MappingExpression`2")],
        [Resolve("AutoMappic.MemberConfigurationExpression`3")],
    ];

    [Theory]
    [MemberData(nameof(DeclarativeTypes))]
    public void Configuration_methods_declare_no_trim_or_aot_requirement(Type declaringType)
    {
        var offenders = declaringType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => ConfigurationMethods.Contains(m.Name, StringComparer.Ordinal))
            .Where(m => m.GetCustomAttribute<RequiresUnreferencedCodeAttribute>() is not null
                     || m.GetCustomAttribute<RequiresDynamicCodeAttribute>() is not null)
            .Select(m => m.Name)
            .Distinct()
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            offenders.Count == 0,
            $"{declaringType.Name} declares a trimming or AOT requirement on its declarative "
            + "configuration surface. Every consumer profile would inherit it, including ones the "
            + "generator fully intercepts. Gate the reflective work behind "
            + $"AutoMappicFeatures.IsReflectionFallbackEnabled instead: {string.Join(", ", offenders)}");
    }

    private sealed class ShapeProfile : Profile
    {
        public IMappingExpression<SourceShape, DestinationShape> Configure()
        {
            IMappingExpression<SourceShape, DestinationShape> expression = CreateMap<SourceShape, DestinationShape>();
            expression.ForMember(d => d.Label, o => o.MapFrom(s => s.Name.ToUpperInvariant()));
            return expression;
        }
    }

    /// <summary>
    ///   With the switch at its default the delegate is built exactly as before, so the change is
    ///   invisible to an untrimmed application.
    /// </summary>
    /// <remarks>
    ///   There is deliberately no sibling test that flips the switch off and asserts the delegate
    ///   is skipped. <c>AppContext</c> switches are process-global and this suite has no
    ///   parallelism controls, so a test that sets the switch to <see langword="false" /> opens a
    ///   window in which every concurrently running test sees a mapper with no fallback. An
    ///   earlier draft did exactly that and turned 14 unrelated tests red on one target framework
    ///   and not the other - a flaky test that would have been blamed on the framework.
    ///
    ///   The disabled path is covered where it actually matters and without global state: the
    ///   <c>aot-publish-and-run</c> CI job publishes the sample with <c>PublishAot=true</c>, which
    ///   applies ILLink.Substitutions.xml for real, and then executes the native binary. That is
    ///   stronger evidence than an AppContext flip, because it proves the substitution itself
    ///   works rather than simulating its effect.
    /// </remarks>
    [Fact]
    public void Configuration_with_the_fallback_enabled_still_builds_delegates()
    {
        var expression = (IMappingExpression)new ShapeProfile().Configure();

        Assert.True(
            expression.RuntimeMaps.ContainsKey("Label"),
            "An untrimmed application still needs the runtime fallback, so the delegate must be built.");
        Assert.Equal("ADA", expression.RuntimeMaps["Label"](new SourceShape { Name = "Ada" }));
    }

    public sealed class SourceShape
    {
        public string Name { get; set; } = string.Empty;
    }

    public sealed class DestinationShape
    {
        public string Label { get; set; } = string.Empty;
    }
}

