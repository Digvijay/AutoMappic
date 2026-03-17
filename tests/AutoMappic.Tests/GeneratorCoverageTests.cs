using Microsoft.CodeAnalysis;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public class GeneratorCoverageTests
{
    [Fact]
    public void Generator_DiagnosticSource_ProducesFiles()
    {
        var source = @"
using AutoMappic;

public class S { public int Id { get; set; } }
public class D { public int Id { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<S, D>(); }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var fileNames = result.Sources.Select(s => s.HintName).ToList();

        Assert.True(fileNames.Count > 0, $"Expected files, got 0. Diagnostics: {string.Join(", ", result.Diagnostics.Select(d => d.Id))}");
    }

    [Fact]
    public void Generator_SourceOnly_ProducesEmbeddedFiles()
    {
        var source = @"
using AutoMappic;

public class S { public int Id { get; set; } }
public class D { public int Id { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<S, D>(); }
}";
        var options = new Dictionary<string, string> { ["automappic_sourceonly"] = "true" };
        var result = GeneratorTestHelper.RunGenerator(source, options);
        
        var fileNames = result.Sources.Select(s => s.HintName).ToList();
        Assert.True(fileNames.Count > 0, $"Expected embedded files, got 0. Diagnostics: {string.Join(", ", result.Diagnostics.Select(d => d.Id))}");
        Assert.Contains("IMapper.g.cs", fileNames);
    }

    [Fact]
    public void Generator_DeepCoverage_TriggerManyBranches()
    {
        var source = @"
using AutoMappic;
using System.Collections.Generic;
using System.Threading.Tasks;

public class ComplexProfile : Profile
{
    public ComplexProfile()
    {
        CreateMap<Source, Dest>()
            .ForMember(d => d.Resolved, opt => opt.MapFrom<TestResolver>())
            .ReverseMap();

        CreateMap<Item, ItemDto>();
    }
}

public class TestResolver : IValueResolver<Source, string>
{
    public string Resolve(Source source) => ""Resolved"";
}

public class Source
{
    public string Name { get; set; } = """";
    public string Resolved { get; set; } = """";
    public List<Item> Items { get; set; } = new();
}

public class Dest
{
    public string Name { get; set; } = """";
    public string Resolved { get; set; } = """";
    public List<ItemDto> Items { get; set; } = new();
}

public class Item { public int Id { get; set; } }
public class ItemDto { public int Id { get; set; } }

public class Program
{
    public async Task Run(IMapper mapper)
    {
        var s = new Source();
        var d = mapper.Map<Dest>(s);
        var r = mapper.Map<Source>(d); 
        var d2 = await mapper.MapAsync<Dest>(s);
    }
}
";
        var result = GeneratorTestHelper.RunGenerator(source);
        var fileNames = result.Sources.Select(s => s.HintName).ToList();

        Assert.True(fileNames.Count >= 5, $"Expected many files, got {fileNames.Count}. Files: {string.Join(", ", fileNames)}");
        Assert.True(fileNames.Any(f => f.Contains("Interceptors.g.cs")), "Interceptors should be generated");
    }
}
