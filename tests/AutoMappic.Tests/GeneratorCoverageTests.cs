using Microsoft.CodeAnalysis;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public class GeneratorCoverageTests
{
    /// <summary> Ensure the source generator identifies profiles and produces 'IMapper.g.cs' and 'ServiceExtensions.g.cs' </summary>
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

    /// <summary> Verify 'automappic_sourceonly' option embeds full library source into the project </summary>
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

    /// <summary> Stress test many generator branches: Resolvers, ReverseMap, ProjectTo, and DataReader </summary>
    [Fact]
    public void Generator_DeepCoverage_TriggerManyBranches()
    {
        var source = @"
using AutoMappic;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Data;

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
    public async Task Run(IMapper mapper, global::System.Linq.IQueryable<Source> queryable, global::System.Data.IDataReader reader)
    {
        var s = new Source();
        var d = mapper.Map<Dest>(s);
        var r = mapper.Map<Source>(d); 
        var d2 = await mapper.MapAsync<Dest>(s);
        var q = queryable.ProjectTo<Source, Dest>();
        var items = reader.Map<ItemDto>();
    }
}
";
        var result = GeneratorTestHelper.RunGenerator(source);

        var allDiags = string.Join("\n", result.Diagnostics.Select(d => $"{d.Id}: {d.GetMessage()}"));
        var fileNames = result.Sources.Select(s => s.HintName).ToList();

        Assert.True(fileNames.Count >= 5, $"Expected many files, got {fileNames.Count}. Files: {string.Join(", ", fileNames)}\nDiags: {allDiags}");
        Assert.True(fileNames.Any(f => f.Contains("Interceptors.g.cs")), "Interceptors should be generated");

        var interceptorResult = result.Sources.FirstOrDefault(f => f.HintName.Contains("Interceptors.g.cs"));
        Assert.NotNull(interceptorResult, "Interceptors.g.cs not found");
        var interceptors = interceptorResult.SourceText.ToString();

        Assert.True(interceptors.Contains("ProjectTo"), $"Interceptors does not contain ProjectTo. Source:\n{interceptors}\nDiags: {allDiags}");
        Assert.True(interceptors.Contains("MapShim_Map"), $"Interceptors does not contain DataReader Map. Source:\n{interceptors}");
    }

    /// <summary> Validate generation of dictionary map assignments for complex-key/value pairs </summary>
    [Fact]
    public void Generator_DictionaryComplex_TriggerBranches()
    {
        var source = @"
using AutoMappic;
using System.Collections.Generic;

public class Profile1 : Profile
{
    public Profile1()
    {
        CreateMap<Source, Dest>();
        CreateMap<SKey, DKey>();
        CreateMap<SVal, DVal>();
    }
}

public class SKey { public string Name { get; set; } }
public class DKey { public string Name { get; set; } }
public class SVal { public int Id { get; set; } }
public class DVal { public int Id { get; set; } }

public class Source
{
    public Dictionary<SKey, SVal> Items { get; set; } = new();
}

public class Dest
{
    public Dictionary<DKey, DVal> Items { get; set; } = new();
}

public class Program
{
    public void Main(IMapper mapper, Source s)
    {
        var d = mapper.Map<Dest>(s);
    }
}
";
        var result = GeneratorTestHelper.RunGenerator(source);
        var mapSource = result.Sources.First(f => f.HintName.Contains("Source_") && f.HintName.Contains("_To_") && f.HintName.Contains("_Dest")).SourceText.ToString();
        Assert.Contains("x.Key.MapToDKey(context)", mapSource);
        Assert.Contains("x.Value.MapToDVal(context)", mapSource);
    }

    /// <summary> Confirm nullability handling logic when mapping nullable types to non-nullable destinations </summary>
    [Fact]
    public void Generator_MixedTypes_TriggerNullabilityBranches()
    {
        var source = @"
#nullable enable
using AutoMappic;

public class Profile1 : Profile
{
    public Profile1()
    {
        CreateMap<Source, Dest>();
    }
}

public class Source
{
    public int? Value1 { get; set; }
    public string? Value2 { get; set; }
    public int[]? Value3 { get; set; }
}

public class Dest
{
    public int Value1 { get; set; }
    public string Value2 { get; set; } = """";
    public int[] Value3 { get; set; } = new int[0];
}
";
        var result = GeneratorTestHelper.RunGenerator(source);
        var mapSource = result.Sources.First(f => f.HintName.Contains("Source_") && f.HintName.Contains("_To_") && f.HintName.Contains("_Dest")).SourceText.ToString();
        Assert.Contains("Value1.GetValueOrDefault()", mapSource);
        Assert.Contains("Value2 ?? \"\"", mapSource);
        Assert.Contains(".ToArray()", mapSource);
    }

    /// <summary> Explicitly trigger branches for ConstructUsing and Condition in the generator </summary>
    [Fact]
    public void Generator_Condition_ConstructUsing_TriggerBranches()
    {
        var source = @"
using AutoMappic;

public class Profile1 : Profile
{
    public Profile1()
    {
        CreateMap<Source, Dest>()
            .ConstructUsing(src => new Dest(src.Name))
            .ForMember(d => d.Age, opt => opt.Condition((src, dest) => src.Id > 0));
    }
}

public class Source { public int Id { get; set; } public string Name { get; set; } = """"; public int Age { get; set; } }
public class Dest { 
    public Dest(string name) { Name = name; }
    public string Name { get; set; }
    public int Age { get; set; }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var mapSource = result.Sources.First(f => f.HintName.Contains("Source_") && f.HintName.Contains("_To_") && f.HintName.Contains("_Dest")).SourceText.ToString();
        Assert.Contains("new Dest(source.Name)", mapSource);
        Assert.Contains("if (source.Id > 0)", mapSource);
    }

    /// <summary> Verify that AM008 is emitted when ProjectTo is used with a profile containing runtime features. </summary>
    [Fact]
    public void Generator_AM008_ProjectTo_WarnsOnRuntimeFeatures()
    {
        var source = @"
using AutoMappic;
using System.Linq;

public class S { public int Id { get; set; } }
public class D { public int Id { get; set; } }

public class BadProfile : Profile
{
    public BadProfile()
    {
        CreateMap<S, D>()
            .ForMember(d => d.Id, opt => opt.Condition((src, dest) => src.Id > 0));
    }
}

public class Program
{
    public void Run(global::AutoMappic.IMapper mapper, IQueryable<S> query)
    {
        var result = query.ProjectTo<S, D>();
    }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var ids = result.Diagnostics.Select(d => d.Id).ToList();
        Assert.Contains("AM008", ids);
    }
}
