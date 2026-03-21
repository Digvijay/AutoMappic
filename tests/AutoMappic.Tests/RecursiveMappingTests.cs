using AutoMappic;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public class SChild { public int Value { get; set; } }
public class DChild { public int Value { get; set; } }
public class S { public List<List<SChild>> Data { get; set; } = new(); }
public class D { public List<List<DChild>> Data { get; set; } = new(); }

public class DeepProfile : Profile
{
    public DeepProfile()
    {
        CreateMap<S, D>();
        CreateMap<SChild, DChild>();
    }
}

public class RecursiveMappingTests
{

    [Fact]
    [Prova.Description("Verify that deeply nested collections like List<List<T>> are correctly mapped.")]
    public void Map_DeepNestedCollections_Works()
    {
        var sourceCode = @"
using AutoMappic;
using System.Collections.Generic;

public class SChild { public int Value { get; set; } }
public class DChild { public int Value { get; set; } }
public class S { public List<List<SChild>> Data { get; set; } = new(); }
public class D { public List<List<DChild>> Data { get; set; } = new(); }

public class DeepProfile : Profile
{
    public DeepProfile()
    {
        CreateMap<S, D>();
        CreateMap<SChild, DChild>();
    }
}";
        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        var fileNames = result.Sources.Select(s => s.HintName).ToList();
        var mapFile = result.Sources.FirstOrDefault(f => f.HintName.Contains("S_") && f.HintName.Contains("_To_") && f.HintName.Contains("_D_"));

        if (mapFile.HintName == null)
        {
            throw new System.Exception("S_To_D_Map not found. Found: " + string.Join(", ", fileNames));
        }

        var mapCode = mapFile.SourceText.ToString();
        Assert.Contains("=> (x == null ? default! : x.MapToDChild())", mapCode);
        Assert.Contains(".ToList()", mapCode);

        var config = new MapperConfiguration(cfg => cfg.AddProfile<DeepProfile>());
        var mapper = config.CreateMapper();

        var source = new S
        {
            Data = new List<List<SChild>>
            {
                new List<SChild> { new SChild { Value = 1 }, new SChild { Value = 2 } },
                new List<SChild> { new SChild { Value = 3 } }
            }
        };

        var dest = mapper.Map<D>(source);

        Assert.NotNull(dest.Data);

        Assert.Equal(2, dest.Data.Count);
        Assert.Equal(1, dest.Data[0][0].Value);
        Assert.Equal(2, dest.Data[0][1].Value);
        Assert.Equal(3, dest.Data[1][0].Value);
    }
}
