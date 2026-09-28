using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public sealed class ReverseSource { public string FullName { get; set; } = string.Empty; }
public sealed class ReverseDest { public string Name { get; set; } = string.Empty; }

public sealed class ReverseMappingTests
{
    private sealed class ReverseProfile : Profile
    {
        public ReverseProfile()
        {
            CreateMap<ReverseSource, ReverseDest>()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.FullName))
                .ReverseMap()
                .ForMember(s => s.FullName, opt => opt.MapFrom(d => d.Name));
        }
    }

    /// <summary> Verify that ForMember calls following a ReverseMap() call are correctly applied to the reverse mapping. </summary>
    [Fact]
    public void ReverseMap_WithExplicitConfig_WorksBothWays()
    {
        IMapper mapper = new MapperConfiguration(cfg => cfg.AddProfile<ReverseProfile>())
            .CreateMapper();

        // Forward
        var source = new ReverseSource { FullName = "Digvijay Chauhan" };
        ReverseDest dest = mapper.Map<ReverseSource, ReverseDest>(source);
        Assert.Equal("Digvijay Chauhan", dest.Name);

        // Reverse
        ReverseSource revSource = mapper.Map<ReverseDest, ReverseSource>(dest);
        Assert.Equal("Digvijay Chauhan", revSource.FullName);
    }
}
