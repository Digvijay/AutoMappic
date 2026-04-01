using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Prova;
using Assert = Prova.Assertions.Assert;
using AutoMappic.Generator;

namespace AutoMappic.Tests;

public class IncrementalGeneratorTests
{
    [Fact]
    [Description("0.6.0 Hardening: Verify the incremental generator correctly caches results when non-structural changes occur.")]
    public void Incremental_Generator_Caches_Models_On_MetadataChanges()
    {
        var source = @"
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
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { syntaxTree },
            references: new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(AutoMappic.Profile).Assembly.Location)
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new AutoMappicGenerator();
        var driver = CSharpGeneratorDriver.Create(
            new[] { generator.AsSourceGenerator() },
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, true));

        // 1. First run
        var run1 = driver.RunGenerators(compilation);
        var res1 = run1.GetRunResult().Results[0];

        // 2. Structural identity change (comment)
        var newSource = source + "\n// This is a comment that doesn't change anything structural.";
        var newTree = CSharpSyntaxTree.ParseText(newSource);
        var newCompilation = compilation.ReplaceSyntaxTree(syntaxTree, newTree);

        var run2 = run1.RunGenerators(newCompilation);
        var res2 = run2.GetRunResult().Results[0];

        // Validate that steps were cached (Unchanged)
        // Note: Step names are internal to the generator, but we can verify that the final 'SourceOutput' for mapping classes was cached.
        
        var mappingClassSteps = res2.TrackedSteps.Values.SelectMany(x => x).Where(x => x.Outputs.Any(o => o.Reason == IncrementalStepRunReason.Cached));
        
        // This is a bit implementation-dependent, but the goal is to show we are using trackers.
        Assert.True(res2.TrackedOutputSteps.Values.SelectMany(x => x).Any(x => x.Outputs.All(o => o.Reason == IncrementalStepRunReason.Cached)), 
            "The generator output should be fully cached when only comments are added to the file.");
    }
}
