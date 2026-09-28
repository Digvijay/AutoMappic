using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

/// <summary>
///   Keeps the two copies of <c>MappingContext</c> in step.
/// </summary>
/// <remarks>
///   <para>
///     <c>MappingContext</c> exists twice: as a real type in <c>AutoMappic.Core</c>, and as an
///     embedded string in the generator that is emitted into each consuming assembly. Generated
///     code has to compile against whichever copy is present, so a member added to one and not
///     the other produces a compile error in the consumer's project — the worst place to find
///     it, because it is the one place neither this repository's build nor its test suite looks.
///   </para>
///   <para>
///     Nothing kept the two in step. This test does, in the same spirit as the solution-parity
///     check: a duplicated definition is only safe when something fails the moment the copies
///     disagree.
///   </para>
/// </remarks>
public class MappingContextParityTests
{
    /// <summary>
    ///   The embedded copy is emitted only in source-only mode, where the consumer deliberately
    ///   does not reference <c>AutoMappic.Core</c>, so the option has to be switched on here.
    /// </summary>
    private static readonly Dictionary<string, string> SourceOnly =
        new() { ["build_property.automappic_sourceonly"] = "true" };

    private static string EmittedMappingContext()
    {
        const string trivial = """
            using AutoMappic;

            namespace TestApp;

            public class A { public int Id { get; set; } }
            public class B { public int Id { get; set; } }

            public sealed class P : Profile
            {
                public P() => CreateMap<A, B>();
            }
            """;

        var result = GeneratorTestHelper.RunGenerator(trivial, SourceOnly);
        var context = result.Sources.FirstOrDefault(s => s.HintName == "MappingContext.g.cs");
        return context.SourceText?.ToString() ?? string.Empty;
    }

    /// <summary>The generator must actually emit its own copy of the context.</summary>
    [Fact]
    public void Generator_emits_a_mapping_context()
    {
        Assert.True(EmittedMappingContext().Length > 0,
            "Expected the generator to emit MappingContext.g.cs.");
    }

    /// <summary>
    ///   Every public member of the runtime context must exist in the emitted copy, because
    ///   generated code may be compiled against either one.
    /// </summary>
    [Fact]
    public void Emitted_context_exposes_every_member_of_the_runtime_context()
    {
        string emitted = EmittedMappingContext();

        var members = typeof(AutoMappic.Generated.MappingContext)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m is not ConstructorInfo)
            .Select(m => m.Name)
            .Where(n => !n.StartsWith("get_", System.StringComparison.Ordinal)
                     && !n.StartsWith("set_", System.StringComparison.Ordinal)
                     && !n.StartsWith("op_", System.StringComparison.Ordinal)
                     && n is not ("ToString" or "Equals" or "GetHashCode"))
            .Distinct()
            .ToList();

        Assert.True(members.Count > 0, "Expected the runtime MappingContext to expose members.");

        var missing = members
            .Where(m => !emitted.Contains(m, System.StringComparison.Ordinal))
            .ToList();

        Assert.True(missing.Count == 0,
            "The generator's embedded MappingContext has drifted from AutoMappic.Core. Missing: "
            + string.Join(", ", missing)
            + ". Generated code compiled against the embedded copy would fail in the consumer's "
            + "project, where neither this build nor this test suite would see it.");
    }

    /// <summary>
    ///   <c>IsTracking</c> specifically must exist in both, because generated code calls it on
    ///   the hot path to avoid boxing the identity key.
    /// </summary>
    [Fact]
    public void Both_contexts_expose_IsTracking()
    {
        Assert.NotNull(typeof(AutoMappic.Generated.MappingContext).GetProperty("IsTracking"));

        Assert.True(EmittedMappingContext().Contains("IsTracking", System.StringComparison.Ordinal),
            "The emitted MappingContext is missing IsTracking, which generated mapping code calls.");
    }
}
