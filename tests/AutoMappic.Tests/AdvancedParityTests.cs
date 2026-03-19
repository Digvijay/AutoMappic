using AutoMappic;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public class AdvancedSource
{
    public string Name { get; set; } = "";
    public bool ShouldMap { get; set; }
    public int Age { get; set; }
}

public class AdvancedDestination
{
    public string FullName { get; set; } = "";
    public int Age { get; set; }
    public string ConstructionMode { get; set; } = "Default";

    public AdvancedDestination() { }
    public AdvancedDestination(string name, string mode)
    {
        FullName = name;
        ConstructionMode = mode;
    }
}

public class AdvancedParityTests
{

    private class AdvancedProfile : Profile
    {
        public AdvancedProfile()
        {
            CreateMap<AdvancedSource, AdvancedDestination>()
                .ConstructUsing(src => new AdvancedDestination(src.Name, "Custom"))
                .ForMember(dest => dest.Age, opt => opt.Condition((src, dest) => src.ShouldMap))
                .ForMemberIgnore(dest => dest.FullName)
                .ForMemberIgnore(dest => dest.ConstructionMode);
        }
    }

    /// <summary> Verify that ConstructUsing uses the custom factory expression. </summary>
    [Fact]
    public void ConstructUsing_ShouldInvokeCustomFactory()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<AdvancedProfile>());
        var mapper = config.CreateMapper();
        var source = new AdvancedSource { Name = "Alice" };

        var result = mapper.Map<AdvancedSource, AdvancedDestination>(source);

        Assert.Equal("Alice", result.FullName);
        Assert.Equal("Custom", result.ConstructionMode);
    }

    /// <summary> Verify that Condition correctly gates the property assignment. </summary>
    [Fact]
    public void Condition_ShouldRespectPredicate()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<AdvancedProfile>());
        var mapper = config.CreateMapper();

        // Case 1: Should map
        var s1 = new AdvancedSource { Age = 25, ShouldMap = true };
        var r1 = mapper.Map<AdvancedSource, AdvancedDestination>(s1);
        Assert.Equal(25, r1.Age);

        // Case 2: Should NOT map (Age should remain default 0)
        var s2 = new AdvancedSource { Age = 30, ShouldMap = false };
        var r2 = mapper.Map<AdvancedSource, AdvancedDestination>(s2);
        Assert.Equal(0, r2.Age);
    }

    private class ComplexProfile : Profile
    {
        public ComplexProfile()
        {
            CreateMap<ComplexSource, ComplexDestination>()
                .ForMember(d => d.Age, opt => opt.Condition((src, dest) => src.Age > 10 && dest.ConstructionMode == "Target"))
                .ForMemberIgnore(d => d.FullName)
                .ForMemberIgnore(d => d.ConstructionMode);
        }
    }

    /// <summary> Verify that Condition can access both source and destination instances efficiently. </summary>
    [Fact]
    public void Condition_WithSourceAndDest_ShouldWork()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ComplexProfile>());
        var mapper = config.CreateMapper();

        // Case 1: Both true
        var s1 = new ComplexSource { Age = 15 };
        var d1 = new ComplexDestination { ConstructionMode = "Target" };
        mapper.Map(s1, d1);
        Assert.Equal(15, d1.Age);

        // Case 2: Source false
        var s2 = new ComplexSource { Age = 5 };
        var d2 = new ComplexDestination { ConstructionMode = "Target" };
        mapper.Map(s2, d2);
        Assert.Equal(0, d2.Age);

        // Case 3: Dest false
        var s3 = new ComplexSource { Age = 15 };
        var d3 = new ComplexDestination { ConstructionMode = "Other" };
        mapper.Map(s3, d3);
        Assert.Equal(0, d3.Age);
    }

    private class ReverseConditionProfile : Profile
    {
        public ReverseConditionProfile()
        {
            CreateMap<ComplexSource, ComplexDestination>()
                .ForMemberIgnore(d => d.FullName)
                .ForMemberIgnore(d => d.ConstructionMode)
                .ReverseMap()
                .ForMember(s => s.Age, opt => opt.Condition((dest, src) => dest.Age > 50));
        }
    }

    /// <summary> Regression test: Ensure Condition works on the reverse mapping path. </summary>
    [Fact]
    public void ReverseMap_WithCondition_ShouldWork()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ReverseConditionProfile>());
        var mapper = config.CreateMapper();

        // Case 1: Should map back to source
        var d1 = new ComplexDestination { Age = 60 };
        var s1 = mapper.Map<ComplexDestination, ComplexSource>(d1);
        Assert.Equal(60, s1.Age);

        // Case 2: Should NOT map back (Age <= 50)
        var d2 = new ComplexDestination { Age = 40 };
        var s2 = new ComplexSource { Age = -1 };
        mapper.Map(d2, s2);
        Assert.Equal(-1, s2.Age);
    }
}

public class ComplexSource
{
    public int Age { get; set; }
}

public class ComplexDestination
{
    public string FullName { get; set; } = "";
    public int Age { get; set; }
    public string ConstructionMode { get; set; } = "Default";
}
