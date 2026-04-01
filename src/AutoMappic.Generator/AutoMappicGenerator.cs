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
        // -- Pipeline 0: Source-Only Injection ------------------------------------
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

        // -- Pipeline 1: Mapping Profiles -----------------------------------------

        var profileCandidates = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: ProfileExtractor.IsProfileClassCandidate,
            transform: ProfileExtractor.ExtractMappingModels);

        var converterCandidates = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: ProfileExtractor.IsConverterMethodCandidate,
            transform: ProfileExtractor.ExtractConverterModels);

        var optionsProvider = context.AnalyzerConfigOptionsProvider;

        var allCandidates = profileCandidates.Collect()
            .Combine(converterCandidates.Collect())
            .SelectMany(static (pair, _) => pair.Left.AddRange(pair.Right));

        var mappingResults = allCandidates
            .SelectMany(static (list, _) => list)
            .Combine(optionsProvider)
            .Select(static (pair, _) =>
            {
                var (result, options) = pair;
                options.GlobalOptions.TryGetValue("build_property.automappic_enableidentitymanagement", out var idFlagStr);
                bool enableIdentity = "true".Equals(idFlagStr, System.StringComparison.OrdinalIgnoreCase);

                options.GlobalOptions.TryGetValue("build_property.automappic_enableentitysync", out var syncFlagStr);
                bool enableSync = "true".Equals(syncFlagStr, System.StringComparison.OrdinalIgnoreCase);

                options.GlobalOptions.TryGetValue("build_property.automappic_smartmatchthreshold", out var thresholdStr);
                double threshold = 0.5;
                if (!string.IsNullOrEmpty(thresholdStr) && double.TryParse(thresholdStr, global::System.Globalization.NumberStyles.Any, global::System.Globalization.CultureInfo.InvariantCulture, out var parsedThreshold))
                {
                    threshold = parsedThreshold;
                }

                if (result.Model != null)
                {
                    var diags = new List<DiagnosticInfo>(result.Diagnostics);
                    diags.RemoveAll(d => d.DescriptorId == "AM0015" &&
                                         d.Properties.TryGetValue("Score", out var scrStr) &&
                                         double.TryParse(scrStr, global::System.Globalization.NumberStyles.Any, global::System.Globalization.CultureInfo.InvariantCulture, out var score) &&
                                         score < threshold);

                    if (enableIdentity)
                    {
                        foreach (var prop in result.Model.Properties)
                        {
                            if (prop.IsRequired && prop.SourceCanBeNull && prop.ConditionBody == null)
                            {
                                var location = new LocationInfo(result.Model.FilePath!, result.Model.Line > 0 ? result.Model.Line - 1 : 0, result.Model.Column > 0 ? result.Model.Column - 1 : 0, result.Model.Line > 0 ? result.Model.Line - 1 : 0, result.Model.Column > 0 ? result.Model.Column - 1 : 0);

                                diags.Add(new DiagnosticInfo(
                                    "AM0013",
                                    location,
                                    global::System.Collections.Immutable.ImmutableArray.Create(prop.DestinationProperty, result.Model.DestinationTypeName, prop.SourceRawExpression ?? prop.SourceExpression ?? "unknown"),
                                    global::System.Collections.Immutable.ImmutableDictionary<string, string?>.Empty));
                            }
                        }
                    }
                    return (Model: result.Model with
                    {
                        EnableIdentityManagement = enableIdentity || result.Model.EnableIdentityManagement,
                        EnableEntitySync = enableSync || result.Model.EnableEntitySync,
                        SmartMatchThreshold = threshold
                    }, Diagnostics: new EquatableArray<DiagnosticInfo>(diags));
                }
                return result;
            })
            .WithComparer(MappingResultComparer.Instance);

        var mappingModels = mappingResults
            .Select(static (pair, _) => pair.Model)
            .Where(static m => m is not null);

        var diagnostics = mappingResults
            .SelectMany(static (pair, _) => pair.Diagnostics);

        context.RegisterSourceOutput(diagnostics, static (spc, d) => spc.ReportDiagnostic(ToRoslynDiagnostic(d)));

        // Deduplicate mapping models by their hint name.
        var uniqueMappingModels = mappingModels
            .Collect()
            .SelectMany<ImmutableArray<MappingModel>, MappingModel>(static (models, _) => models.GroupBy(m => m.HintName).Select(g => g.First()));

        // Collect all unique models for linking and diagnostics.
        var allUniqueModels = uniqueMappingModels.Collect();

        // Pipeline 1: Mapping Classes (with linked registry)
        context.RegisterSourceOutput(uniqueMappingModels.Combine(allUniqueModels), static (spc, pair) =>
        {
            var (model, allModels) = pair;
            var registry = allModels.ToDictionary(
                m => m.HintName,
                m => m,
                System.StringComparer.Ordinal);

            var (hintName, source) = SourceEmitter.EmitMappingClass(model, registry);
            spc.AddSource(hintName, source);
        });

        // -- Pipeline 1.5: Cycle Detection -----------------------------------------
        context.RegisterSourceOutput(allUniqueModels, static (spc, models) =>
        {
            if (models.IsEmpty) return;
            var cycleDiagnostics = CycleDetector.Detect(models);
            foreach (var d in cycleDiagnostics)
            {
                spc.ReportDiagnostic(d);
            }
        });

        // -- Pipeline 2: Interceptors ----------------------------------------------

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
            var collisionCheck = new Dictionary<string, MappingModel>(System.StringComparer.Ordinal);

            // 1. Local mappings
            foreach (var m in models)
            {
                var fullKey = $"{m.SourceTypeFullName}_To_{m.DestinationTypeFullName}";
                if (collisionCheck.TryGetValue(fullKey, out var existing))
                {
                    spc.ReportDiagnostic(Diagnostic.Create(
                        AutoMappicDiagnostics.DuplicateMapping,
                        global::Microsoft.CodeAnalysis.Location.None,
                        m.SourceTypeName, m.DestinationTypeName,
                        existing.ProfileName ?? "UnknownProfile", m.ProfileName ?? "UnknownProfile"));
                }
                else
                {
                    collisionCheck.Add(fullKey, m);
                }

                var key = $"{SourceEmitter.Sanitise(m.SourceTypeFullName)}_To_{SourceEmitter.Sanitise(m.DestinationTypeFullName)}";
                if (!mappingsByKey.ContainsKey(key))
                {
                    mappingsByKey[key] = m;
                }

                // Also add an unbound fallback key
                var unboundKey = $"{SourceEmitter.Sanitise(SourceEmitter.GetUnbound(m.SourceTypeFullName))}_To_{SourceEmitter.Sanitise(SourceEmitter.GetUnbound(m.DestinationTypeFullName))}";
                if (unboundKey != key && !mappingsByKey.ContainsKey(unboundKey))
                {
                    mappingsByKey[unboundKey] = m;
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
                        var srcFull = Pipeline.SourceEmitter.GetDisplayString(src);
                        var destFull = Pipeline.SourceEmitter.GetDisplayString(dest);
                        var key = $"{Pipeline.SourceEmitter.Sanitise(srcFull)}_To_{Pipeline.SourceEmitter.Sanitise(destFull)}";

                        if (!mappingsByKey.ContainsKey(key))
                        {
                            // Create a skeleton model for the shim to use.
                            mappingsByKey[key] = new MappingModel(
                                srcFull, src.Name,
                                destFull, dest.Name,
                                EquatableArray<PropertyMap>.Empty,
                                EquatableArray<PropertyMap>.Empty);
                        }
                    }
                }
            }

            // 2.5 Transitive Async Promotion Pass
            // If a nested child mapping is async, the parent must also become async.
            bool changed = true;
            while (changed)
            {
                changed = false;
                var keys = mappingsByKey.Keys.ToList();
                foreach (var key in keys)
                {
                    var m = mappingsByKey[key];
                    if (m.IsAsync) continue;

                    foreach (var prop in m.Properties)
                    {
                        if (prop.IsCollection || (prop.NestedDestTypeFullName != null && prop.NestedSourceTypeFullName != null))
                        {
                            var sType = prop.NestedSourceTypeFullName ?? m.SourceTypeFullName;
                            var childKey = $"{Pipeline.SourceEmitter.Sanitise(sType, true)}_To_{Pipeline.SourceEmitter.Sanitise(prop.NestedDestTypeFullName!, true)}";
                            if (mappingsByKey.TryGetValue(childKey, out var child) && child.IsAsync)
                            {
                                var firstNonIgnored = m.Properties.FirstOrDefault(p => p.Kind != PropertyMapKind.Ignored);
                                if (firstNonIgnored != null)
                                {
                                    var updatedProps = m.Properties.ToList();
                                    int idx = updatedProps.IndexOf(firstNonIgnored);
                                    updatedProps[idx] = firstNonIgnored with { IsAsync = true };
                                    mappingsByKey[key] = m with { Properties = new EquatableArray<PropertyMap>(updatedProps) };
                                    changed = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            // 3. Emit AM0008 for potential ProjectTo incompatibilities
            foreach (var loc in locations.Where(l => l.Kind == InterceptKind.ProjectTo))
            {
                var key = $"{SourceEmitter.Sanitise(loc.SourceTypeFullName)}_To_{SourceEmitter.Sanitise(loc.DestinationTypeFullName)}";
                if (mappingsByKey.TryGetValue(key, out var model))
                {
                    bool hasRuntimeFeatures = model.Properties.Any(p => !string.IsNullOrEmpty(p.ConditionBody) || p.Kind == PropertyMapKind.Explicit) ||
                                              !string.IsNullOrEmpty(model.BeforeMapBody) ||
                                              !string.IsNullOrEmpty(model.AfterMapBody) ||
                                              !string.IsNullOrEmpty(model.ConstructionBody) ||
                                              !string.IsNullOrEmpty(model.BeforeMapAsyncBody) ||
                                              !string.IsNullOrEmpty(model.AfterMapAsyncBody);

                    if (hasRuntimeFeatures)
                    {
                        var linePos = new global::Microsoft.CodeAnalysis.Text.LinePosition(loc.Line - 1, loc.Column - 1);
                        var location = global::Microsoft.CodeAnalysis.Location.Create(loc.FilePath,
                            new global::Microsoft.CodeAnalysis.Text.TextSpan(0, 0),
                            new global::Microsoft.CodeAnalysis.Text.LinePositionSpan(linePos, linePos));

                        spc.ReportDiagnostic(global::Microsoft.CodeAnalysis.Diagnostic.Create(
                            AutoMappicDiagnostics.UnsupportedProjectToFeature,
                            location,
                            model.SourceTypeName,
                            model.DestinationTypeName));
                    }
                }
            }

            var (hintName, source) = SourceEmitter.EmitInterceptors(locations, mappingsByKey);
            // Hint name for the single Interceptors file is constant.
            spc.AddSource(hintName, source);
        });

        // -- Pipeline 3: DI Registration ------------------------------------------

        var profileClasses = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => node is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax cls && cls.BaseList is not null,
            transform: static (ctx, ct) =>
            {
                var classDecl = (Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax)ctx.Node;
                var symbol = ctx.SemanticModel.GetDeclaredSymbol(classDecl, ct) as INamedTypeSymbol;
                if (symbol is null) return null;

                if (ProfileExtractor.InheritsFromProfile(symbol))
                {
                    // CS0616 fix: Ensure we only register types that are actual classes inheriting from Profile,
                    // and are accessible to the generated code.
                    if (symbol.TypeKind == TypeKind.Class && !symbol.IsAbstract &&
                        symbol.DeclaredAccessibility != Accessibility.Private &&
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
            .Combine(allUniqueModels);

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


    private sealed class MappingResultComparer
        : IEqualityComparer<(MappingModel Model, EquatableArray<DiagnosticInfo> Diagnostics)>
    {
        public static readonly MappingResultComparer Instance = new();

        public bool Equals(
            (MappingModel Model, EquatableArray<DiagnosticInfo> Diagnostics) x,
            (MappingModel Model, EquatableArray<DiagnosticInfo> Diagnostics) y)
        {
            if (x.Model is null && y.Model is null) return x.Diagnostics.Equals(y.Diagnostics);
            if (x.Model is null || y.Model is null) return false;
            return x.Model.Equals(y.Model) && x.Diagnostics.Equals(y.Diagnostics);
        }

        public int GetHashCode(
            (MappingModel Model, EquatableArray<DiagnosticInfo> Diagnostics) obj)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (obj.Model?.GetHashCode() ?? 0);
                hash = hash * 31 + obj.Diagnostics.GetHashCode();
                return hash;
            }
        }
    }

    private static Diagnostic ToRoslynDiagnostic(DiagnosticInfo info)
    {
        var descriptor = GetDescriptor(info.DescriptorId);
        var linePos = new global::Microsoft.CodeAnalysis.Text.LinePosition(info.Location.StartLine, info.Location.StartColumn);
        var endPos = new global::Microsoft.CodeAnalysis.Text.LinePosition(info.Location.EndLine, info.Location.EndColumn);
        var location = global::Microsoft.CodeAnalysis.Location.Create(info.Location.FilePath,
             new global::Microsoft.CodeAnalysis.Text.TextSpan(0, 0),
             new global::Microsoft.CodeAnalysis.Text.LinePositionSpan(linePos, endPos));

        return Diagnostic.Create(descriptor, location, info.Properties, info.MessageArgs.ToArray());
    }

    private static DiagnosticDescriptor GetDescriptor(string id) => id switch
    {
        "AM0001" => AutoMappicDiagnostics.UnmappedProperty,
        "AM0002" => AutoMappicDiagnostics.AmbiguousMapping,
        "AM0003" => AutoMappicDiagnostics.CreateMapOutsideProfile,
        "AM0004" => AutoMappicDiagnostics.UnresolvedInterceptorMapping,
        "AM0005" => AutoMappicDiagnostics.MissingConstructor,
        "AM0006" => AutoMappicDiagnostics.CircularReference,
        "AM0007" => AutoMappicDiagnostics.UnresolvedCreateMapSymbol,
        "AM0008" => AutoMappicDiagnostics.UnsupportedProjectToFeature,
        "AM0009" => AutoMappicDiagnostics.DuplicateMapping,
        "AM0010" => AutoMappicDiagnostics.PerformanceHotpath,
        "AM0011" => AutoMappicDiagnostics.UnsupportedMultiSourceProjectTo,
        "AM0012" => AutoMappicDiagnostics.AsymmetricMapping,
        "AM0013" => AutoMappicDiagnostics.PatchIntoRequired,
        "AM0014" => AutoMappicDiagnostics.UnmappedPrimaryKey,
        "AM0015" => AutoMappicDiagnostics.SmartMatchSuggestion,
        "AM0016" => AutoMappicDiagnostics.PerformanceRegression,
        "AM0017" => AutoMappicDiagnostics.AmbiguousEntityKey,
        _ => AutoMappicDiagnostics.UnmappedProperty // Fallback
    };
}
