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
        // ── Pipeline 0: Source-Only Injection ────────────────────────────────────
        context.RegisterSourceOutput(context.AnalyzerConfigOptionsProvider, static (spc, options) =>
        {
            options.GlobalOptions.TryGetValue("build_property.automappic_sourceonly", out var v1);
            options.GlobalOptions.TryGetValue("automappic_sourceonly", out var v2);

            if ("true".Equals(v1, System.StringComparison.OrdinalIgnoreCase) ||
                "true".Equals(v2, System.StringComparison.OrdinalIgnoreCase))
            {
                EmbeddedSourceEmitter.Emit(spc);
            }
        });

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

        var combined = allMappings.Combine(allLocations).Combine(context.CompilationProvider);

        context.RegisterSourceOutput(combined, static (spc, triple) =>
        {
            var (pair, compilation) = triple;
            var (models, locations) = pair;
            if (locations.IsEmpty) return;

            // Use the first registration found for each pair to resolve interception.
            var mappingsByKey = new Dictionary<string, MappingModel>(System.StringComparer.Ordinal);

            // 1. Local mappings
            foreach (var m in models)
            {
                var key = $"{Sanitise(m.SourceTypeFullName)}_To_{Sanitise(m.DestinationTypeFullName)}";
                if (!mappingsByKey.ContainsKey(key))
                {
                    mappingsByKey[key] = m;
                }
            }

            // 2. Discover mappings from referenced assemblies (Sannr 1.6 style)
            foreach (var reference in compilation.SourceModule.ReferencedAssemblySymbols)
            {
                foreach (var attr in reference.GetAttributes().Where(a => a.AttributeClass?.Name == "MappingDiscoveryAttribute"))
                {
                    if (attr.ConstructorArguments.Length == 2 &&
                        attr.ConstructorArguments[0].Value is INamedTypeSymbol src &&
                        attr.ConstructorArguments[1].Value is INamedTypeSymbol dest)
                    {
                        var srcFull = src.ToDisplayString();
                        var destFull = dest.ToDisplayString();
                        var key = $"{Sanitise(srcFull)}_To_{Sanitise(destFull)}";

                        if (!mappingsByKey.ContainsKey(key))
                        {
                            // Create a skeleton model for the shim to use.
                            mappingsByKey[key] = new MappingModel(
                                srcFull, src.Name,
                                destFull, dest.Name,
                                EquatableArray<PropertyMap>.Empty);
                        }
                    }
                }
            }

            var (hintName, source) = SourceEmitter.EmitInterceptors(locations, mappingsByKey);
            // Hint name for the single Interceptors file is constant.
            spc.AddSource(hintName, source);
        });

        // ── Pipeline 3: DI Registration ──────────────────────────────────────────

        var profileClasses = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => node is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax cls && cls.BaseList is not null,
            transform: static (ctx, ct) =>
            {
                var classDecl = (Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax)ctx.Node;
                var symbol = ctx.SemanticModel.GetDeclaredSymbol(classDecl, ct) as INamedTypeSymbol;
                if (symbol is null) return null;

                if (ProfileExtractor.InheritsFromProfile(symbol))
                {
                    // Only register types that are accessible to the generated code.
                    // Private nested classes in tests should be skipped for static registration.
                    if (symbol.DeclaredAccessibility != Accessibility.Private &&
                        symbol.DeclaredAccessibility != Accessibility.Protected)
                    {
                        return symbol.ToDisplayString();
                    }
                }
                return null;
            })
            .Where(static x => x is not null);

        // Combine profiles, compilation, and unique mappings for the registration emitter.
        var registrationData = profileClasses.Collect()
            .Combine(context.CompilationProvider)
            .Combine(uniqueMappingModels.Collect());

        context.RegisterSourceOutput(registrationData, static (spc, data) =>
        {
            var (pair, localMappings) = data;
            var (profiles, compilation) = pair;
            var assemblyName = compilation.AssemblyName ?? "Unknown";

            // Find referenced assemblies with marker attributes (Sannr-style metadata discovery).
            var referencedRegistrations = new List<string>();
            foreach (var reference in compilation.SourceModule.ReferencedAssemblySymbols)
            {
                var attributes = reference.GetAttributes();
                if (attributes.Any(a => a.AttributeClass?.Name == "HasAutoMappicProfilesAttribute"))
                {
                    referencedRegistrations.Add(reference.Name);
                }
            }

            // Only emit if we have local profiles, local mappings, referenced profiles, or we are the entry point.
            if (profiles.IsDefaultOrEmpty && localMappings.IsDefaultOrEmpty && referencedRegistrations.Count == 0 && compilation.GetEntryPoint(spc.CancellationToken) == null)
                return;

            var (hintName, source) = SourceEmitter.EmitRegistration(
                assemblyName,
                profiles,
                localMappings,
                referencedRegistrations.ToImmutableArray(),
                compilation.GetEntryPoint(spc.CancellationToken) != null);

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
