using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

/// <summary>
/// Regression tests for assembly-name sanitisation in the generated registration file.
///
/// The generator derives C# identifiers (marker class, registration class, DI extension method)
/// from the compilation's assembly name. An assembly name is far more permissive than a C#
/// identifier: '-' in particular is legal in an assembly name and illegal in an identifier.
///
/// Before the fix, sanitisation for identifier generation only replaced a hardcoded list of
/// characters that did not include '-'. A project named "my-app" therefore produced
///
///     public static class AutoMappic_Extension_my-app
///
/// which the compiler parsed as a subtraction expression and reported as CS0116 / CS1106 / CS0548.
/// This is not an exotic case: hyphenated assembly names are common, and BenchmarkDotNet's
/// generated host assembly is always of the form "&lt;Project&gt;-&lt;Job&gt;-&lt;N&gt;", so *every*
/// BenchmarkDotNet project that referenced AutoMappic failed to build.
/// </summary>
public class AssemblyNameSanitisationTests
{
    private const string Source = @"
using AutoMappic;

public class User { public int Id { get; set; } public string Name { get; set; } }
public class UserDto { public int Id { get; set; } public string Name { get; set; } }

public class MyProfile : Profile
{
    public MyProfile()
    {
        CreateMap<User, UserDto>();
    }
}
";

    [Theory]
    [InlineData("my-app")]
    [InlineData("VikingAir.Benchmarks-DefaultJob-1")]
    [InlineData("Contoso.Api-v2")]
    [Description("Generated registration code must compile when the assembly name contains characters that are legal in an assembly name but illegal in a C# identifier.")]
    public void Registration_Compiles_For_Assembly_Names_With_Illegal_Identifier_Characters(string assemblyName)
    {
        var result = GeneratorTestHelper.RunGenerator(Source, assemblyName: assemblyName);

        var registration = result.Sources
            .FirstOrDefault(s => s.HintName.Contains("Registration"));

        Assert.NotNull(registration.SourceText);

        string generated = registration.SourceText!.ToString();

        // The sanitised name must be a valid identifier, so the raw assembly name -- which is not --
        // must never appear verbatim in the generated code.
        Assert.False(
            generated.Contains(assemblyName),
            $"Generated registration embedded the raw assembly name '{assemblyName}' verbatim.");

        // The authoritative check: parse the generated file and require it to be syntactically valid.
        var tree = CSharpSyntaxTree.ParseText(generated);
        var syntaxErrors = tree.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.True(
            syntaxErrors.Count == 0,
            "Generated registration is not valid C#: " +
            string.Join("; ", syntaxErrors.Select(d => $"{d.Id} {d.GetMessage()}")));
    }

    [Fact]
    [Description("A sanitised identifier may not begin with a digit even if the assembly name does.")]
    public void Registration_Compiles_For_Assembly_Name_Starting_With_Digit()
    {
        var result = GeneratorTestHelper.RunGenerator(Source, assemblyName: "7Eleven.Api");

        var registration = result.Sources.FirstOrDefault(s => s.HintName.Contains("Registration"));
        Assert.NotNull(registration.SourceText);

        var tree = CSharpSyntaxTree.ParseText(registration.SourceText!.ToString());
        var syntaxErrors = tree.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        Assert.True(
            syntaxErrors.Count == 0,
            "Generated registration is not valid C#: " +
            string.Join("; ", syntaxErrors.Select(d => $"{d.Id} {d.GetMessage()}")));
    }
}
