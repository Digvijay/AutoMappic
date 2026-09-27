using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

/// <summary>
///   Pins which public APIs are annotated as requiring dynamic code or unreferenced code.
/// </summary>
/// <remarks>
///   <para>
///     These annotations are the project's AOT contract, and they are load-bearing for its
///     headline claim. An annotation that is too loose lets an AOT consumer publish something
///     that fails at runtime. An annotation that is too tight is worse in a different way: it
///     fires <c>IL3050</c> on the most discoverable API in the library, so every AOT consumer
///     sees a warning telling them the reflection-free mapper needs reflection, and any project
///     treating IL3050 as an error cannot build at all.
///   </para>
///   <para>
///     <c>Profile.CreateMap&lt;TSource, TDestination&gt;()</c> was annotated with
///     <see cref="RequiresDynamicCodeAttribute" />. It does not generate any dynamic code: the
///     whole body allocates <c>MappingExpression&lt;TSource, TDestination&gt;</c> over two
///     statically known type arguments, and that constructor assigns one field. The annotation
///     was simply wrong, and it was wrong on the first line of every profile anyone writes.
///   </para>
///   <para>
///     <see cref="RequiresUnreferencedCodeAttribute" /> is kept there deliberately. It is about
///     trimming, not code generation, and the runtime fallback mapper does reflect over the
///     members of both types, which trimming can remove.
///   </para>
/// </remarks>
public class ProfileAotAnnotationTests
{
    private static MethodInfo GenericCreateMap() =>
        typeof(Profile)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Single(m => m.Name == "CreateMap" && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);

    /// <summary>Declaring a map over statically known types generates no dynamic code.</summary>
    [Fact]
    public void Generic_CreateMap_is_not_marked_as_requiring_dynamic_code()
    {
        var method = GenericCreateMap();

        Assert.Null(method.GetCustomAttribute<RequiresDynamicCodeAttribute>());
    }

    /// <summary>The runtime fallback still reflects over members, so trimming remains a risk.</summary>
    [Fact]
    public void Generic_CreateMap_is_still_marked_as_requiring_unreferenced_code()
    {
        var method = GenericCreateMap();

        Assert.NotNull(method.GetCustomAttribute<RequiresUnreferencedCodeAttribute>());
    }

    /// <summary>The open-generic overload resolves types at runtime and genuinely needs both.</summary>
    [Fact]
    public void Type_based_CreateMap_remains_fully_annotated()
    {
        var method = typeof(Profile)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Single(m => m.Name == "CreateMap" && !m.IsGenericMethodDefinition);

        Assert.NotNull(method.GetCustomAttribute<RequiresDynamicCodeAttribute>());
        Assert.NotNull(method.GetCustomAttribute<RequiresUnreferencedCodeAttribute>());
    }

    /// <summary>
    ///   The reflection-backed mapper genuinely constructs closed generic types at runtime, so
    ///   its annotations must stay. This is the API that is not AOT-safe, and it must keep
    ///   saying so.
    /// </summary>
    [Fact]
    public void IMapper_map_methods_remain_annotated()
    {
        var methods = typeof(IMapper)
            .GetMethods()
            .Where(m => m.Name is "Map" or "MapAsync")
            .ToList();

        Assert.True(methods.Count > 0, "Expected IMapper to expose Map/MapAsync methods.");

        foreach (var method in methods)
        {
            Assert.NotNull(method.GetCustomAttribute<RequiresDynamicCodeAttribute>());
            Assert.NotNull(method.GetCustomAttribute<RequiresUnreferencedCodeAttribute>());
        }
    }
}
