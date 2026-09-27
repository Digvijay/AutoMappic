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

    /// <summary> Every public mapping entry point declares RequiresDynamicCode so AOT warnings surface at the caller, not inside AutoMappic </summary>
    [Fact]
    public void PublicMappingMethods_DeclareRequiresDynamicCode()
    {
        var unannotated = PublicMappingMethods()
            .Where(m => m.GetCustomAttribute<RequiresDynamicCodeAttribute>() is null)
            .Select(m => m.Name)
            .Distinct()
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            unannotated.Count == 0,
            $"These public mapping methods reach the reflection fallback without [RequiresDynamicCode]: {string.Join(", ", unannotated)}");
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

    /// <summary> MapCore and MapCoreAsync are covered by name, because these were the two that shipped unannotated and broke a consumer's AOT publish </summary>
    [Fact]
    public void MapCoreEntryPoints_AreAnnotated()
    {
        foreach (var name in new[] { "MapCore", "MapCoreAsync" })
        {
            var method = typeof(Mapper).GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(method);
            Assert.NotNull(method!.GetCustomAttribute<RequiresDynamicCodeAttribute>());
            Assert.NotNull(method!.GetCustomAttribute<RequiresUnreferencedCodeAttribute>());
        }
    }
}
