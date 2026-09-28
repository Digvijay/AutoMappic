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

    /// <summary>
    ///   The generic overload must <em>not</em> be marked as requiring unreferenced code, and
    ///   must instead constrain its type parameters.
    /// </summary>
    /// <remarks>
    ///   This assertion is the inverse of what it used to be. <c>RequiresUnreferencedCode</c> on
    ///   the generic overload made the library's advertised path unusable under trimming: this is
    ///   the declarative API the source generator reads, so a consumer got a trim error for
    ///   merely declaring a profile, even when every mapping it declared was generated at compile
    ///   time and no reflection ever ran. The requirement belongs to the runtime fallback.
    ///
    ///   Constraining the type parameters with <c>DynamicallyAccessedMembers</c> expresses the
    ///   same requirement without penalising correct use: the trimmer preserves exactly the
    ///   members the fallback would reflect over, which makes the fallback genuinely safe rather
    ///   than merely warned about.
    /// </remarks>
    [Fact]
    public void Generic_CreateMap_constrains_its_type_parameters_instead_of_requiring_unreferenced_code()
    {
        var method = GenericCreateMap();

        Assert.Null(method.GetCustomAttribute<RequiresUnreferencedCodeAttribute>());

        var parameters = method.GetGenericArguments();
        foreach (var parameter in parameters)
        {
            var dam = parameter.GetCustomAttribute<DynamicallyAccessedMembersAttribute>();
            Assert.NotNull(dam);
            Assert.True(
                dam!.MemberTypes.HasFlag(DynamicallyAccessedMemberTypes.PublicProperties),
                parameter.Name + " must preserve public properties for the runtime fallback.");
            Assert.True(
                dam.MemberTypes.HasFlag(DynamicallyAccessedMemberTypes.PublicMethods),
                parameter.Name + " must preserve public methods for the runtime fallback.");
        }

        var destination = parameters.Single(p => p.Name == "TDestination");
        Assert.True(
            destination.GetCustomAttribute<DynamicallyAccessedMembersAttribute>()!
                .MemberTypes.HasFlag(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor),
            "TDestination must preserve its parameterless constructor; the fallback activates it.");
    }

    /// <summary>
    ///   The Type-based overload names its requirements with DAM on the parameters rather than
    ///   declaring a blanket trimming or AOT requirement.
    /// </summary>
    /// <remarks>
    ///   The method itself does nothing reflective: it records the two types on an
    ///   <c>OpenGenericMappingExpression</c>. The reflection happens later, in the fallback
    ///   engine, which is now behind the feature switch and annotated where it lives.
    ///
    ///   What the method does need is for those two types to keep their members, so DAM says so
    ///   precisely: public methods and properties on both, plus the parameterless constructor on
    ///   the destination, which the fallback activates. That is an obligation a caller can
    ///   actually satisfy, unlike RequiresUnreferencedCode, which only propagates upward until
    ///   someone suppresses it.
    /// </remarks>
    [Fact]
    public void Type_based_CreateMap_names_its_requirements_with_DAM()
    {
        var method = typeof(Profile)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .Single(m => m.Name == "CreateMap" && !m.IsGenericMethodDefinition);

        Assert.Null(method.GetCustomAttribute<RequiresDynamicCodeAttribute>());
        Assert.Null(method.GetCustomAttribute<RequiresUnreferencedCodeAttribute>());

        var parameters = method.GetParameters();
        foreach (var parameter in parameters)
        {
            var dam = parameter.GetCustomAttribute<DynamicallyAccessedMembersAttribute>();
            Assert.NotNull(dam);
            Assert.True(
                dam!.MemberTypes.HasFlag(DynamicallyAccessedMemberTypes.PublicProperties),
                parameter.Name + " must preserve public properties for the runtime fallback.");
            Assert.True(
                dam.MemberTypes.HasFlag(DynamicallyAccessedMemberTypes.PublicMethods),
                parameter.Name + " must preserve public methods for the runtime fallback.");
        }

        var destination = parameters.Single(p => p.Name == "destinationType");
        Assert.True(
            destination.GetCustomAttribute<DynamicallyAccessedMembersAttribute>()!
                .MemberTypes.HasFlag(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor),
            "destinationType must preserve its parameterless constructor; the fallback activates it.");
    }

    /// <summary>
    ///   <see cref="IMapper" />'s mapping methods declare no trimming or AOT requirement, and
    ///   must stay in step with the <see cref="Mapper" /> implementation.
    /// </summary>
    /// <remarks>
    ///   This is parity as much as it is policy. An interface member and its implementation must
    ///   declare exactly the same requirements or the build fails IL2046/IL3051, so the moment
    ///   the implementation dropped these attributes the interface had to as well.
    ///
    ///   The requirement was removed rather than suppressed because it stopped being true. The
    ///   reflective fallback now sits behind a feature switch that ILLink.Substitutions.xml stubs
    ///   to <c>false</c> when trimming, so the trimmer removes the reflective engine instead of
    ///   warning about it. Before that change, the annotation made every correct consumer - one
    ///   whose call sites the generator had intercepted, where no reflection runs at all - see an
    ///   error they could not act on. An unfixable warning does not make anyone safer; it teaches
    ///   people to silence the category, and the category is where the real warnings live.
    /// </remarks>
    [Fact]
    public void IMapper_map_methods_declare_no_requirement_and_match_the_implementation()
    {
        var methods = typeof(IMapper)
            .GetMethods()
            .Where(m => m.Name is "Map" or "MapAsync")
            .ToList();

        Assert.True(methods.Count > 0, "Expected IMapper to expose Map/MapAsync methods.");

        foreach (var method in methods)
        {
            Assert.Null(method.GetCustomAttribute<RequiresUnreferencedCodeAttribute>());
            Assert.Null(method.GetCustomAttribute<RequiresDynamicCodeAttribute>());
        }
    }
}
