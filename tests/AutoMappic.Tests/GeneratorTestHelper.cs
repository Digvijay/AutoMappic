using System.Collections.Immutable;
using System.IO;
using AutoMappic.Generator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AutoMappic.Tests;

public static class GeneratorTestHelper
{
    public record GeneratorResult(
        ImmutableArray<Diagnostic> Diagnostics,
        ImmutableArray<GeneratedSourceResult> Sources,
        GeneratorDriverRunResult RunResult);

    public static GeneratorResult RunGenerator(
        string source,
        IReadOnlyDictionary<string, string>? options = null)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { syntaxTree },
            references: new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Runtime.dll")),
                MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Collections.dll")),
                MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Linq.dll")),
                MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Linq.Expressions.dll")),
                MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Linq.Queryable.dll")),
                MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Data.Common.dll")),
                MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Text.RegularExpressions.dll")),
                MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "netstandard.dll")),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Profile).Assembly.Location)
            },
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new AutoMappicGenerator();

        var optionsProvider = options != null ? new TestOptionsProvider(options) : null;
        var driver = CSharpGeneratorDriver.Create(
            new[] { generator.AsSourceGenerator() },
            parseOptions: compilation.SyntaxTrees.First().Options as CSharpParseOptions,
            optionsProvider: optionsProvider);

        var runDriver = driver.RunGenerators(compilation);
        var runResult = runDriver.GetRunResult();

        var sources = runResult.Results
            .SelectMany(r => r.GeneratedSources)
            .ToImmutableArray();

        return new GeneratorResult(runResult.Diagnostics, sources, runResult);
    }

    private class TestOptionsProvider : AnalyzerConfigOptionsProvider
    {
        private readonly TestOptions _options;
        public TestOptionsProvider(IReadOnlyDictionary<string, string> options) => _options = new TestOptions(options);
        public override AnalyzerConfigOptions GlobalOptions => _options;
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => _options;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => _options;
    }

    private class TestOptions : AnalyzerConfigOptions
    {
        private readonly IReadOnlyDictionary<string, string> _options;
        public TestOptions(IReadOnlyDictionary<string, string> options)
            => _options = new Dictionary<string, string>(options, StringComparer.OrdinalIgnoreCase);
        public override bool TryGetValue(string key, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? value) => _options.TryGetValue(key, out value);
    }
}
