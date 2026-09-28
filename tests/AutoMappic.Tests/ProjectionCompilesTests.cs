using System;
using System.Linq;
using AutoMappic.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

/// <summary>
/// Guards the projection expression emitter against emitting C# that does not compile.
/// </summary>
/// <remarks>
/// These cover a shipped defect: a flattened path to a non-nullable value type produced
/// <c>source.Metadata.LastLogin ?? (default!)!</c>, which fails with CS0019 ("Operator '??'
/// cannot be applied to operands of type 'DateTime' and 'default'") and CS8715 ("Duplicate
/// null suppression operator"). The generator's own unit tests passed throughout, because
/// none of them compiled the generated source - the break only surfaced in a consumer's
/// build. Every test here therefore asserts on compiler diagnostics, not on emitted text.
/// </remarks>
public class ProjectionCompilesTests
{
    private const string FlattenedValueTypeSource = @"
using System;
using AutoMappic;

public class Metadata { public DateTime LastLogin { get; set; } public string Note { get; set; } }
public class User { public int Id { get; set; } public string Name { get; set; } public Metadata Metadata { get; set; } }
public class UserDto { public int Id { get; set; } public string Name { get; set; } public DateTime MetadataLastLogin { get; set; } public string MetadataNote { get; set; } }

public class MyProfile : Profile
{
    public MyProfile()
    {
        CreateMap<User, UserDto>();
    }
}
";

    [Fact]
    [Description("Generated projection for a flattened non-nullable value type must compile (regression: CS0019/CS8715).")]
    public void Projection_For_Flattened_ValueType_Compiles()
    {
        string[] errors = CompileWithGenerator(FlattenedValueTypeSource);

        Assert.True(
            errors.Length == 0,
            "Generated code must compile. Errors: " + string.Join("; ", errors));
    }

    [Fact]
    [Description("The emitted projection must not contain a doubled null-suppression operator.")]
    public void Projection_Does_Not_Emit_Duplicate_Null_Suppression()
    {
        var result = GeneratorTestHelper.RunGenerator(FlattenedValueTypeSource);
        string text = string.Join("\n", result.Sources.Select(s => s.SourceText.ToString()));

        Assert.False(text.Contains("!)!"), "Emitted source contains a doubled null-suppression operator.");
    }

    [Fact]
    [Description("A flattened reference-type path keeps its string fallback, which stays valid after '?.' is stripped.")]
    public void Projection_For_Flattened_ReferenceType_Keeps_Fallback()
    {
        string expr = Generator.Pipeline.SourceEmitter.ToProjectionExpression("Metadata?.Note ?? \"\"");

        Assert.Equal("Metadata.Note ?? \"\"!", expr);
    }

    [Fact]
    [Description("A flattened value-type path drops the '?? (default!)' guard that '?.' removal invalidates.")]
    public void Projection_For_Flattened_ValueType_Drops_Guard()
    {
        string expr = Generator.Pipeline.SourceEmitter.ToProjectionExpression("Metadata?.LastLogin ?? (default!)");

        Assert.Equal("Metadata.LastLogin!", expr);
    }

    /// <summary>
    /// Runs the generator and compiles the original source together with every generated file,
    /// returning the resulting compilation errors.
    /// </summary>
    private static string[] CompileWithGenerator(string source)
    {
        var result = GeneratorTestHelper.RunGenerator(source);

        var trees = new[] { CSharpSyntaxTree.ParseText(source) }
            .Concat(result.Sources.Select(s => CSharpSyntaxTree.ParseText(s.SourceText.ToString())))
            .ToArray();

        string runtimeDir = System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var trusted = (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? string.Empty)
            .Split(System.IO.Path.PathSeparator)
            .Where(p => p.Length > 0 && System.IO.File.Exists(p));

        var references = trusted
            .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
            .Concat(new[]
            {
                MetadataReference.CreateFromFile(System.IO.Path.Combine(runtimeDir, "netstandard.dll")),
                MetadataReference.CreateFromFile(typeof(Profile).Assembly.Location)
            })
            .GroupBy(r => System.IO.Path.GetFileName(((PortableExecutableReference)r).FilePath))
            .Select(g => g.First())
            .ToArray();

        var compilation = CSharpCompilation.Create(
            "ProjectionCompilationTest",
            trees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return [.. compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.Id + " " + d.GetMessage())];
    }
}
