using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using AutoMappic.Generator.Models;
using AutoMappic.Generator.Pipeline;
using Microsoft.CodeAnalysis;

namespace AutoMappic.Generator;

/// <summary>
///   The AutoMappic incremental source generator.
/// </summary>
[Generator]
public sealed class AutoMappicGenerator : IIncrementalGenerator
{
    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // ── Pipeline 1: Mapping Profiles ─────────────────────────────────────────

        var profileCandidates = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: ProfileExtractor.IsProfileClassCandidate,
            transform: ProfileExtractor.ExtractMappingModels);

        var mappingResults = profileCandidates
            .SelectMany(static (list, _) => list)
            .WithComparer(MappingResultComparer.Instance);

        var mappingModels = mappingResults
            .Select(static (pair, _) => pair.Model);

        var diagnostics = mappingResults
            .SelectMany(static (pair, _) => pair.Diagnostics);

        context.RegisterSourceOutput(diagnostics, static (spc, d) => spc.ReportDiagnostic(d));

        // Deduplicate mapping models by their hint name before emitting source files.
        // This prevents build errors when the same pair is registered in multiple profiles.
        var uniqueMappingModels = mappingModels
            .Collect()
            .SelectMany(static (models, _) => models.GroupBy(m => m.HintName).Select(g => g.First()));

        context.RegisterSourceOutput(uniqueMappingModels, static (spc, model) =>
        {
            var (hintName, source) = SourceEmitter.EmitMappingClass(model);
            spc.AddSource(hintName, source);
        });

        // ── Pipeline 2: Interceptors ──────────────────────────────────────────────

        var interceptLocations = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: InterceptorCollector.IsInvocationCandidate,
            transform: InterceptorCollector.ExtractInterceptLocation)
            .Where(static loc => loc is not null)
            .Select(static (loc, _) => loc!);

        var allMappings = mappingModels.Collect();
        var allLocations = interceptLocations.Collect();

        var combined = allMappings.Combine(allLocations);

        context.RegisterSourceOutput(combined, static (spc, pair) =>
        {
            var (models, locations) = pair;
            if (locations.IsEmpty) return;

            // Use the first registration found for each pair to resolve interception.
            var mappingsByKey = new Dictionary<string, MappingModel>(System.StringComparer.Ordinal);
            foreach (var m in models)
            {
                var key = $"{Sanitise(m.SourceTypeFullName)}_To_{Sanitise(m.DestinationTypeFullName)}";
                if (!mappingsByKey.ContainsKey(key))
                {
                    mappingsByKey[key] = m;
                }
            }

            var (hintName, source) = SourceEmitter.EmitInterceptors(locations, mappingsByKey);
            // Hint name for the single Interceptors file is constant.
            spc.AddSource(hintName, source);
        });
    }

    private static string Sanitise(string name) =>
        name.Replace('.', '_').Replace('<', '_').Replace('>', '_');

    private sealed class MappingResultComparer
        : IEqualityComparer<(MappingModel Model, IReadOnlyList<Diagnostic> Diagnostics)>
    {
        public static readonly MappingResultComparer Instance = new();

        public bool Equals(
            (MappingModel Model, IReadOnlyList<Diagnostic> Diagnostics) x,
            (MappingModel Model, IReadOnlyList<Diagnostic> Diagnostics) y)
            => x.Model.Equals(y.Model);

        public int GetHashCode(
            (MappingModel Model, IReadOnlyList<Diagnostic> Diagnostics) obj)
            => obj.Model.GetHashCode();
    }
}
