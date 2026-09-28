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

    /// <summary>
    ///   No public mapping entry point declares RequiresDynamicCode, because none of them needs
    ///   it: the fallback is plain reflection.
    /// </summary>
    /// <remarks>
    ///   This assertion is the inverse of what it used to be. The fallback never compiles an
    ///   expression tree - it uses <c>Activator.CreateInstance</c>, <c>GetMethod</c>,
    ///   <c>Invoke</c> and <c>SetValue</c>, all of which Native AOT supports. The only genuine
    ///   need for runtime code generation was <c>MakeGenericType</c> in the collection and
    ///   dictionary paths, and the destination type is already closed at both sites, so that call
    ///   was removed. Declaring the requirement anyway told every consumer their AOT application
    ///   was unsafe when it was not, which is the same failure mode as an unannotated hazard:
    ///   the annotation stops carrying information.
    ///
    ///   The one remaining case - closing <c>Dictionary&lt;,&gt;</c> for an interface-typed
    ///   destination - is guarded at runtime by <c>RuntimeFeature.IsDynamicCodeSupported</c>, so
    ///   an unsupported combination is reported with an actionable message instead of failing
    ///   obscurely inside the runtime.
    /// </remarks>
    [Fact]
    public void PublicMappingMethods_DoNotDeclareRequiresDynamicCode()
    {
        var annotated = PublicMappingMethods()
            .Where(m => m.GetCustomAttribute<RequiresDynamicCodeAttribute>() is not null)
            .Select(m => m.Name)
            .Distinct()
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            annotated.Count == 0,
            "These public mapping methods declare [RequiresDynamicCode] but the fallback only uses "
            + $"plain reflection, so the requirement is not real: {string.Join(", ", annotated)}");
    }

    /// <summary> Every public mapping entry point declares RequiresUnreferencedCode so trimming warnings surface at the caller, not inside AutoMappic </summary>
    [Fact]
    public void PublicMappingMethods_DeclareRequiresUnreferencedCode()
    {
        var unannotated = PublicMappingMethods()
            .Where(m => m.GetCustomAttribute<RequiresUnreferencedCodeAttribute>() is null)
            .Select(m => m.Name)
            .Distinct()
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            unannotated.Count == 0,
            $"These public mapping methods reach the reflection fallback without [RequiresUnreferencedCode]: {string.Join(", ", unannotated)}");
    }

    /// <summary>
    ///   MapCore and MapCoreAsync are covered by name, because these were the two that shipped
    ///   unannotated and broke a consumer's AOT publish.
    /// </summary>
    /// <remarks>
    ///   They keep RequiresUnreferencedCode: both recurse into nested and collection members
    ///   using the runtime type of each value, so the members they reach cannot be named
    ///   statically and a trimmer can legitimately remove them. They no longer declare
    ///   RequiresDynamicCode, which was never true of them.
    /// </remarks>
    [Fact]
    public void MapCoreEntryPoints_AreAnnotated()
    {
        foreach (var name in new[] { "MapCore", "MapCoreAsync" })
        {
            var method = typeof(Mapper).GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(method);
            Assert.NotNull(method!.GetCustomAttribute<RequiresUnreferencedCodeAttribute>());
            Assert.Null(method!.GetCustomAttribute<RequiresDynamicCodeAttribute>());
        }
    }
}
