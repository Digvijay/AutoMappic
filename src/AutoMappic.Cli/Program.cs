using System;
using System.CommandLine;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using AutoMappic.Generator.Pipeline;

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

        var projectArg = new Argument<string>("project", "The path to the .csproj file.");
        var validate = new Command("validate", "Validates all mapping declarations in a project.")
        {
            projectArg
        };

        validate.SetHandler(ValidateProject, projectArg);
        root.AddCommand(validate);

        var vizProjectArg = new Argument<string>("project", "The path to the .csproj file.");
        var formatOpt = new Option<string>("--format", () => "mermaid", "The output format (mermaid, dot).");
        var visualize = new Command("visualize", "Generates a mapping graph visualization.")
        {
            vizProjectArg,
            formatOpt
        };

        visualize.SetHandler(VisualizeProject, vizProjectArg, formatOpt);
        root.AddCommand(visualize);

        return await root.InvokeAsync(args);
    }

    private static async Task ValidateProject(string projectPath)
    {
        Console.WriteLine($"[INFO] Validating project: {projectPath}...");
        var compilation = await GetCompilation(projectPath);
        if (compilation == null)
        {
            Environment.Exit(1);
            return;
        }

        var models = ProfileExtractor.ExtractFromCompilation(compilation);
        var errors = new List<string>();

        // Check for cycles using the Detector
        var diagnostics = CycleDetector.Detect(models);
        foreach (var diag in diagnostics)
        {
            errors.Add($"{diag.Id}: {diag.GetMessage()}");
        }

        if (errors.Count == 0 && models.Count > 0)
        {
            Console.WriteLine($"[SUCCESS] Validation successful! Found {models.Count} valid mapping pairs.");
            foreach(var model in models)
            {
                Console.WriteLine($"  -> {model.SourceTypeFullName} to {model.DestinationTypeFullName}");
            }
        }
        else if (models.Count == 0)
        {
            Console.WriteLine("[WARNING] No mapping declarations found. Ensure you have Profile classes calling CreateMap.");
        }
        else
        {
            Console.WriteLine($"[ERROR] Validation failed with {errors.Count} errors:");
            foreach (var err in errors) Console.WriteLine($"  ! {err}");
            Environment.Exit(1);
        }
    }

    private static async Task VisualizeProject(string projectPath, string format)
    {
        Console.WriteLine($"[INFO] Generating {format} visualization for: {projectPath}...");
        var compilation = await GetCompilation(projectPath);
        if (compilation == null)
        {
            Environment.Exit(1);
            return;
        }

        var models = ProfileExtractor.ExtractFromCompilation(compilation);

        if (string.Equals(format, "mermaid", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("\n--- Mermaid Graph ---\n");
            Console.WriteLine("graph LR");
            foreach (var m in models)
            {
                Console.WriteLine($"    {m.SourceTypeName} --> {m.DestinationTypeName}");
            }
            Console.WriteLine("\n---------------------\n");
        }
        else
        {
            Console.WriteLine("[ERROR] Unsupported format. Use --format mermaid.");
        }
    }

    private static async Task<Compilation?> GetCompilation(string projectPath)
    {
        try 
        {
            using var workspace = MSBuildWorkspace.Create();
            var project = await workspace.OpenProjectAsync(projectPath);
            return await project.GetCompilationAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Error loading project: {ex.Message}");
            return null;
        }
    }
}
