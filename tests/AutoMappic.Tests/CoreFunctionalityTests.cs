using System;
using System.Collections.Generic;
using System.Linq;
using AutoMappic;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

#region Models

public record FeatureRecord(int Id, string Title);
public record FeatureDto(int Id, string Title);

public class SuperComplexSource
{
    public int Id { get; set; }
    public string Header { get; set; }
    public SuperSubSource Sub { get; set; }
    public List<SuperItemSource> Items { get; set; } = new();
}

public class SuperSubSource { public string Detail { get; set; } public string HiddenVal { get; set; } }
public class SuperItemSource { public int Value { get; set; } }

public class SuperComplexDto
{
    public int Id { get; set; }
    public string Header { get; set; }
    public string SubDetail { get; set; } // Flattened
    public List<SuperItemDto> Items { get; set; }
}

public class SuperItemDto { public int Value { get; set; } }

public class SuperLifecycleSource { public string Input { get; set; } }
public class SuperLifecycleDto 
{ 
    public string Output { get; set; } 
    
    [AutoMappicIgnore]
    public bool BeforeCalled { get; set; } 
    
    [AutoMappicIgnore]
    public bool AfterCalled { get; set; } 
}

public class SuperAmbiguitySource { public string SpecialName { get; set; } public string Other { get; set; } }
public class SuperAmbiguityDto { public string SpecialName { get; set; } }

#endregion

public class CoreFunctionalityProfile : Profile
{
    public CoreFunctionalityProfile()
    {
        CreateMap<FeatureRecord, FeatureDto>();
        CreateMap<SuperComplexSource, SuperComplexDto>();
        CreateMap<SuperItemSource, SuperItemDto>();
        
        CreateMap<SuperLifecycleSource, SuperLifecycleDto>()
            .ForMember(d => d.Output, opt => opt.Ignore())
            .BeforeMap((s, d) => d.BeforeCalled = true)
            .AfterMap((s, d) => {
                d.AfterCalled = true;
                d.Output = (s.Input ?? "").ToUpper();
            });

        CreateMap<SuperAmbiguitySource, SuperAmbiguityDto>();
    }
}

public class CoreFunctionalityTests
{
    private static IMapper GetMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CoreFunctionalityProfile>());
        return config.CreateMapper();
    }

    [Fact]
    [Description("Core: Test mapping for C# 9 Record types with primary constructors.")]
    public void Test_RecordMapping()
    {
        var mapper = GetMapper();
        var source = new FeatureRecord(42, "Stelar");
        var result = mapper.Map<FeatureDto>(source);

        Assert.Equal(42, result.Id);
        Assert.Equal("Stelar", result.Title);
    }

    [Fact]
    [Description("Core: Test complex mapping with flattening and nested collection projection.")]
    public void Test_ComplexDeepMapping()
    {
        var mapper = GetMapper();
        var source = new SuperComplexSource
        {
            Id = 101,
            Header = "Super",
            Sub = new SuperSubSource { Detail = "DetailedValue" },
            Items = new List<SuperItemSource> { new() { Value = 1 }, new() { Value = 2 } }
        };

        var result = mapper.Map<SuperComplexDto>(source);

        Assert.Equal(101, result.Id);
        Assert.Equal("Super", result.Header);
        Assert.Equal("DetailedValue", result.SubDetail); // Flattened check
        Assert.NotNull(result.Items);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.Items[0].Value);
    }

    [Fact]
    [Description("Core: Test lifecycle hooks execution order.")]
    public void Test_LifecycleHooks()
    {
        var mapper = GetMapper();
        var source = new SuperLifecycleSource { Input = "hello" };
        var result = mapper.Map<SuperLifecycleDto>(source);

        Assert.True(result.BeforeCalled, "BeforeMap should be called");
        Assert.True(result.AfterCalled, "AfterMap should be called");
        Assert.Equal("HELLO", result.Output);
    }

    [Fact]
    [Description("Core: Test ProjectTo with complex nested types and flattened paths.")]
    public void Test_ProjectTo_Deep()
    {
        var sourceList = new List<SuperComplexSource>
        {
            new SuperComplexSource 
            { 
                Id = 1, Sub = new SuperSubSource { Detail = "D1" },
                Items = new List<SuperItemSource> { new() { Value = 10 } }
            }
        }.AsQueryable();

        var result = sourceList.ProjectTo<SuperComplexDto>().ToList();

        Assert.Equal(1, result.Count);
        Assert.Equal("D1", result[0].SubDetail);
        Assert.Equal(10, result[0].Items[0].Value);
    }
}
