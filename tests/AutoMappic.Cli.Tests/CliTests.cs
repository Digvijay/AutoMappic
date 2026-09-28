using System.CommandLine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AutoMappic.Cli;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Cli.Tests;

public sealed class CliTests
{
    private static readonly SemaphoreSlim ConsoleLock = new(1, 1);
    private readonly string _sampleProjectPath;

    public CliTests()
    {
        string root = GetSolutionRoot();
        _sampleProjectPath = Path.Combine(root, "samples", "SampleApp", "SampleApp.csproj");
    }

    private static string GetSolutionRoot()
    {
        string current = AppContext.BaseDirectory;
        while (!File.Exists(Path.Combine(current, "AutoMappic.sln")))
        {
            current = Path.GetDirectoryName(current) ?? throw new DirectoryNotFoundException("Could not find solution root.");
        }
        return current;
    }

    /// <summary> Verify that the CLI validate command correctly identifies all valid mapping profiles in the sample application. </summary>
    [Fact]
    public async Task Validate_SampleApp_Successful()
    {
        await ConsoleLock.WaitAsync();
        var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);
        try
        {
            string[] args = new[] { "validate", _sampleProjectPath };
            int exitCode = await Program.Main(args);

            string output = sw.ToString();
            Assert.Equal(0, exitCode);
            Assert.Contains("[SUCCESS] All mapping profiles are valid.", output);
            Assert.Contains("User -> UserDto", output.Replace("global::", ""));
        }
        finally
        {
            Console.SetOut(originalOut);
            ConsoleLock.Release();
        }
    }

    /// <summary> Verify that the CLI visualize command generates valid Mermaid graph output for the sample application's mappings. </summary>
    [Fact]
    public async Task Visualize_SampleApp_Mermaid_Successful()
    {
        await ConsoleLock.WaitAsync();
        var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);
        try
        {
            string[] args = new[] { "visualize", _sampleProjectPath, "--format", "mermaid" };
            int exitCode = await Program.Main(args);

            string output = sw.ToString();
            Assert.Equal(0, exitCode);
            Assert.Contains("graph TD", output);
            Assert.Contains("UserDto.Username", output);
            Assert.Contains("OrderDto.Id", output);
        }
        finally
        {
            Console.SetOut(originalOut);
            ConsoleLock.Release();
        }
    }

    /// <summary> Verify that the CLI visualize command generates valid Mermaid graph output for the project itself. </summary>
    [Fact]
    public async Task Visualize_Project_Successful()
    {
        await ConsoleLock.WaitAsync();
        var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);
        try
        {
            string root = GetSolutionRoot();
            string project = Path.Combine(root, "tests", "AutoMappic.Tests", "AutoMappic.Tests.csproj");
            string[] args = new[] { "visualize", project, "--format", "mermaid" };
            int exitCode = await Program.Main(args);

            string output = sw.ToString();
            Assert.Equal(0, exitCode);
            Assert.Contains("graph TD", output);
            // It should at least contain some of our test models
            Assert.Contains("UserDto.Email", output);
        }
        finally
        {
            Console.SetOut(originalOut);
            ConsoleLock.Release();
        }
    }

    /// <summary> Verify that the migrate command automatically refactors standard mapping calls to the modern fluent extensions. </summary>
    [Fact]
    public async Task Migrate_Project_Refactors_Source_Correctly()
    {
        await ConsoleLock.WaitAsync();
        var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        string tempPath = Path.Combine(Path.GetTempPath(), "AutoMappicCliTestDir");
        Directory.CreateDirectory(tempPath);

        try
        {
            string projectFile = Path.Combine(tempPath, "TempProj.csproj");
            string sourceFile = Path.Combine(tempPath, "Program.cs");

            File.WriteAllText(projectFile, @"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
  </PropertyGroup>
</Project>");

            File.WriteAllText(sourceFile, @"using System;
class Program {
    static void Main() {
        var user = new object();
        var _mapper = new object();
        var res = _mapper.Map<UserDto>(user);
    }
}");

            string[] args = new[] { "migrate", projectFile };
            int exitCode = await Program.Main(args);

            string output = sw.ToString();
            Assert.Equal(0, exitCode);
            Assert.Contains("[SUCCESS]", output);
            Assert.Contains("Migrated 1 usages", output);

            string updatedCode = File.ReadAllText(sourceFile);
            Assert.Contains("var res = user.MapTo<UserDto>(_mapper);", updatedCode);
            Assert.DoesNotContain("_mapper.Map<UserDto>", updatedCode);
        }
        finally
        {
            if (Directory.Exists(tempPath))
                Directory.Delete(tempPath, true);

            Console.SetOut(originalOut);
            ConsoleLock.Release();
        }
    }
}
