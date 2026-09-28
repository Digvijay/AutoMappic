using System.CommandLine;
using System.Globalization;
using System.Text.Json;
using AutoMappic.Generator.Models;
using AutoMappic.Generator.Pipeline;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace AutoMappic.Cli;

internal sealed class Program
{
    internal static async Task<int> Main(string[] args)
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }

        var root = new RootCommand("AutoMappic CLI - Mapping validation and visualization tool.");

        var projectArg = new Argument<string>("project") { Description = "The path to the .csproj file." };
        var validateFormatOpt = new Option<string>("--format") { Description = "The output format (text, json).", DefaultValueFactory = _ => "text" };
        var validate = new Command("validate", "Validates all mapping declarations in a project.")
        {
            projectArg,
            validateFormatOpt
        };

        validate.SetAction((parseResult, cancellationToken) =>
            ValidateProject(parseResult.GetValue(projectArg)!, parseResult.GetValue(validateFormatOpt)!));
        root.Subcommands.Add(validate);

        var vizProjectArg = new Argument<string>("project") { Description = "The path to the .csproj file." };
        var formatOpt = new Option<string>("--format") { Description = "The visualization format (mermaid).", DefaultValueFactory = _ => "mermaid" };
        var visualize = new Command("visualize", "Generates a mapping graph visualization.")
        {
            vizProjectArg,
            formatOpt
        };

        visualize.SetAction((parseResult, cancellationToken) =>
            VisualizeProject(parseResult.GetValue(vizProjectArg)!, parseResult.GetValue(formatOpt)!));
        root.Subcommands.Add(visualize);

        var migrateProjectArg = new Argument<string>("project") { Description = "The path to the .csproj file to migrate." };
        var migrate = new Command("migrate", "Migrates a codebase from AutoMapper to AutoMappic.")
        {
            migrateProjectArg
        };
        migrate.SetAction((parseResult, cancellationToken) =>
            MigrateProject(parseResult.GetValue(migrateProjectArg)!));
        root.Subcommands.Add(migrate);

        return await root.Parse(args).InvokeAsync().ConfigureAwait(false);
    }

    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    private static async Task ValidateProject(string projectPath, string format)
    {
        bool isJson = string.Equals(format, "json", StringComparison.OrdinalIgnoreCase);

        if (!isJson)
        {
            Console.WriteLine($"[INFO] Validating mappings in: {projectPath}...");
        }

        Compilation? compilation = await GetCompilation(projectPath);
        if (compilation == null)
        {
            if (isJson)
            {
                Console.WriteLine("{\"error\": \"Could not load compilation\"}");
            }

            return;
        }

        IReadOnlyList<(MappingModel Model, EquatableArray<DiagnosticInfo> Diagnostics)> mappings = ProfileExtractor.ExtractFromCompilation(compilation);
        int errorCount = 0;
        int warningCount = 0;
        var issues = new List<object>();

        foreach ((MappingModel? model, EquatableArray<DiagnosticInfo>? diagnostics) in mappings)
        {
            if (model == null)
            {
                continue;
            }

            if (!isJson)
            {
                Console.WriteLine($"[ANALYZING] {model.SourceTypeFullName} -> {model.DestinationTypeFullName}");
            }

            foreach (DiagnosticInfo diag in diagnostics)
            {
                if (diag.Severity == DiagnosticSeverity.Error)
                {
                    errorCount++;
                }
                else if (diag.Severity == DiagnosticSeverity.Warning)
                {
                    warningCount++;
                }

                if (isJson)
                {
                    issues.Add(new
                    {
                        diag.Id,
                        Severity = diag.Severity.ToString(),
                        Message = diag.GetMessage(CultureInfo.InvariantCulture),
                        SourceType = model.SourceTypeFullName,
                        DestinationType = model.DestinationTypeFullName,
                        File = diag.Location.FilePath,
                        Line = diag.Location.StartLine + 1
                    });
                }
                else
                {
                    ConsoleColor color = diag.Severity == DiagnosticSeverity.Error ? ConsoleColor.Red : ConsoleColor.Yellow;
                    Console.ForegroundColor = color;
                    Console.WriteLine($"  [{diag.Severity.ToString().ToUpper(CultureInfo.InvariantCulture)}] {diag.Id}: {diag.GetMessage(CultureInfo.InvariantCulture)}");
                    Console.ResetColor();
                }
            }
        }

        if (isJson)
        {
            var result = new
            {
                ProjectPath = projectPath,
                IsValid = errorCount == 0,
                ErrorCount = errorCount,
                WarningCount = warningCount,
                Issues = issues
            };
            Console.WriteLine(JsonSerializer.Serialize(result, _jsonOptions));
        }
        else
        {
            if (errorCount == 0)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"\n[SUCCESS] All mapping profiles are valid.");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[FAILURE] Found {errorCount} configuration errors.");
                Console.ResetColor();
                Environment.Exit(1);
            }
        }
    }

    private static async Task VisualizeProject(string projectPath, string format)
    {
        Compilation? compilation = await GetCompilation(projectPath);
        if (compilation == null)
        {
            return;
        }

        IReadOnlyList<(MappingModel Model, EquatableArray<DiagnosticInfo> Diagnostics)> mappings = ProfileExtractor.ExtractFromCompilation(compilation);
        Console.WriteLine($"\n--- AutoMappic Mapping Graph ({format}) ---");

        if (string.Equals(format, "mermaid", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("graph TD");
            foreach ((MappingModel? m, EquatableArray<DiagnosticInfo> _) in mappings)
            {
                if (m == null)
                {
                    continue;
                }

                Console.WriteLine($"    subgraph \"{m.SourceTypeName} to {m.DestinationTypeName}\"");
                foreach (PropertyMap? p in m.Properties.Where(x => x.Kind != PropertyMapKind.Ignored))
                {
                    string sourcePath = p.NestedExpression ?? p.SourceExpression ?? "Explicit";
                    Console.WriteLine($"        {m.SourceTypeName}.{sourcePath} --> {m.DestinationTypeName}.{p.DestinationProperty}");
                }
                Console.WriteLine("    end");
            }
        }
        else
        {
            Console.WriteLine("[ERROR] Unsupported format. Use --format mermaid.");
        }
        Console.WriteLine("-------------------------------------------\n");
    }

    private static async Task<Compilation?> GetCompilation(string projectPath)
    {
        try
        {
            using var workspace = MSBuildWorkspace.Create();
            Project project = await workspace.OpenProjectAsync(projectPath);
            return await project.GetCompilationAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Error loading project: {ex.Message}");
            return null;
        }
    }

    private static async Task MigrateProject(string projectPath)
    {
        Console.WriteLine($"\n[INFO] Starting experimental migration for: {projectPath}");
        Compilation? comp = await GetCompilation(projectPath);
        if (comp == null)
        {
            return;
        }

        int fileCount = 0;
        int replacements = 0;

        var regex = new System.Text.RegularExpressions.Regex(@"\b(?<mapper>[a-zA-Z0-9_]+)\.Map<(?<type>[^>]+)>\((?<src>[^)]+)\)");

        foreach (SyntaxTree tree in comp.SyntaxTrees)
        {
            string path = tree.FilePath;
            if (string.IsNullOrEmpty(path) || path.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string text = (await tree.GetTextAsync()).ToString();
            if (regex.IsMatch(text))
            {
                string newText = regex.Replace(text, match =>
                    $"{match.Groups["src"].Value}.MapTo<{match.Groups["type"].Value}>({match.Groups["mapper"].Value})");

                if (text != newText)
                {
                    System.IO.File.WriteAllText(path, newText);
                    fileCount++;
                    replacements += regex.Count(text);
                    Console.WriteLine($"  [REF] Updated: {System.IO.Path.GetFileName(path)}");
                }
            }
        }

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n[SUCCESS] Migrated {replacements} usages across {fileCount} files!");
        Console.ResetColor();
    }
}
