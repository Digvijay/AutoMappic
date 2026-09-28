using System.Linq;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

/// <summary>
///   Pins the fix for the identity-key boxing defect.
/// </summary>
/// <remarks>
///   <para>
///     The generator emits an identity-map lookup for any mapping with a key property, so that
///     a repeated entity in an object graph maps to a single destination instance. The key has
///     to be boxed to <see cref="object" /> to live in that map.
///   </para>
///   <para>
///     The emitted code used to box unconditionally:
///     <c>var __keyVal = (object?)source.Id;</c>. Identity tracking is off for the overwhelming
///     majority of maps — the interceptor passes <c>new MappingContext(false)</c>, which leaves
///     the tracking dictionary null and turns both <c>TryGetEntity</c> and <c>Register</c> into
///     no-ops — so that cast allocated on every single call to support a feature nobody had
///     switched on. For an <see cref="int" /> key that is 24 bytes of pure waste per map, and
///     it was the visible part of AutoMappic measuring as allocating more than the hand-written
///     baseline it claims parity with.
///   </para>
///   <para>
///     The boxing is now deferred behind <c>context.IsTracking</c>. These tests assert on the
///     generated text rather than on a benchmark number, because a benchmark can absorb a
///     regression silently whereas this cannot.
///   </para>
/// </remarks>
public class IdentityKeyBoxingTests
{
    private const string KeyedMappingSource = """
        using AutoMappic;

        namespace TestApp;

        public class Entity
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }

        public class EntityDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
        }

        public sealed class EntityProfile : Profile
        {
            public EntityProfile() => CreateMap<Entity, EntityDto>();
        }
        """;

    private static string GeneratedMappingBody()
    {
        var result = GeneratorTestHelper.RunGenerator(KeyedMappingSource);
        var map = result.Sources.FirstOrDefault(s => s.HintName.EndsWith("_Map.g.cs", System.StringComparison.Ordinal));
        return map.SourceText?.ToString() ?? string.Empty;
    }

    /// <summary>The generated mapping must still support identity tracking.</summary>
    [Fact]
    public void Generated_map_still_emits_the_identity_lookup()
    {
        var body = GeneratedMappingBody();

        Assert.True(body.Contains("__keyVal", System.StringComparison.Ordinal),
            "Expected the generated mapping to keep its identity-map key. Removing it entirely would "
            + "break circular-graph mapping rather than fix the allocation.");
        Assert.True(body.Contains("TryGetEntity", System.StringComparison.Ordinal),
            "Expected the generated mapping to keep its identity-map lookup.");
    }

    /// <summary>The key must only be boxed when identity tracking is actually enabled.</summary>
    [Fact]
    public void Key_is_only_boxed_when_tracking_is_enabled()
    {
        var body = GeneratedMappingBody();

        Assert.True(body.Contains("context.IsTracking ? (object?)", System.StringComparison.Ordinal),
            "Expected the key cast to be guarded by context.IsTracking so that a non-tracking map "
            + "allocates nothing. Generated code was:\n" + body);
    }

    /// <summary>An unconditional cast to object must never reappear on the hot path.</summary>
    [Fact]
    public void Generated_map_never_boxes_the_key_unconditionally()
    {
        var body = GeneratedMappingBody();

        Assert.False(body.Contains("var __keyVal = (object?)", System.StringComparison.Ordinal),
            "The generated mapping boxes its key unconditionally again. This allocates on every "
            + "map even when identity tracking is off. Generated code was:\n" + body);
    }
}
