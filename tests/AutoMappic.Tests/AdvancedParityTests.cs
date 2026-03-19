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
}
