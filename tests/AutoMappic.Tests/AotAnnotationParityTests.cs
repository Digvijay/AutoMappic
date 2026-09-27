using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

/// <summary>
/// <para>
/// When an interface member carries <see cref="RequiresUnreferencedCodeAttribute"/> or
/// <see cref="RequiresDynamicCodeAttribute"/>, the member implementing it has to carry the
/// same attribute. The ILLink and ILCompiler analysers treat a mismatch as an error in its
/// own right -- IL2046 for the trimming attribute and IL3051 for the dynamic-code attribute --
/// because an unannotated implementation can be reached through the interface without the
/// caller ever being warned.
/// </para>
/// <para>
/// This is not theoretical. Every member of <c>IMappingExpression&lt;,&gt;</c> and
/// <c>IMemberConfigurationExpression&lt;,,&gt;</c> was annotated while none of the members
/// implementing them were, and a consumer publishing with Native AOT got sixty analyser
/// errors attributed to their own project for code they did not write.
/// </para>
/// </summary>
public sealed class AotAnnotationParityTests
{
    private static IEnumerable<(Type Type, MethodInfo Interface, MethodInfo Implementation)> InterfaceMappings()
    {
        var assembly = typeof(Mapper).Assembly;

        foreach (var type in assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false }))
        {
            foreach (var contract in type.GetInterfaces().Where(i => i.Assembly == assembly))
            {
                InterfaceMapping map;
                try
                {
                    map = type.GetInterfaceMap(contract);
                }
                catch (ArgumentException)
                {
                    // Open generic definitions cannot be mapped; they are covered through their
                    // constructed forms.
                    continue;
                }

                for (var i = 0; i < map.InterfaceMethods.Length; i++)
                {
                    yield return (type, map.InterfaceMethods[i], map.TargetMethods[i]);
                }
            }
        }
    }

    private static IEnumerable<string> Mismatches<TAttribute>()
        where TAttribute : Attribute =>
        InterfaceMappings()
            .Where(m => m.Interface.GetCustomAttribute<TAttribute>() is not null)
            .Where(m => m.Implementation.GetCustomAttribute<TAttribute>() is null)
            .Select(m => $"{m.Type.Name}.{m.Implementation.Name} (implements {m.Interface.DeclaringType!.Name}.{m.Interface.Name})")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(n => n, StringComparer.Ordinal);

    /// <summary> An implementation of a RequiresUnreferencedCode interface member declares it too, so ILLink does not raise IL2046 </summary>
    [Fact]
    public void Implementations_MatchRequiresUnreferencedCode()
    {
        var mismatches = Mismatches<RequiresUnreferencedCodeAttribute>().ToList();

        Assert.True(
            mismatches.Count == 0,
            $"These members implement a RequiresUnreferencedCode interface member without declaring it, which ILLink reports as IL2046: {string.Join(", ", mismatches)}");
    }

    /// <summary> An implementation of a RequiresDynamicCode interface member declares it too, so ILCompiler does not raise IL3051 </summary>
    [Fact]
    public void Implementations_MatchRequiresDynamicCode()
    {
        var mismatches = Mismatches<RequiresDynamicCodeAttribute>().ToList();

        Assert.True(
            mismatches.Count == 0,
            $"These members implement a RequiresDynamicCode interface member without declaring it, which ILCompiler reports as IL3051: {string.Join(", ", mismatches)}");
    }
}
