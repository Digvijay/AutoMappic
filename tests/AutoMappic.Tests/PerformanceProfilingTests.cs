using Microsoft.Extensions.DependencyInjection;
using Prova;

namespace AutoMappic.Tests;

public class PerformanceProfilingTests
{
    [Fact]
    public void Profiling_ShouldCompile_WhenEnabledInProfile()
    {
        var services = new ServiceCollection();
        services.AddAutoMappicFromAutoMappic_Tests();
        var sp = services.BuildServiceProvider();
        var mapper = sp.GetRequiredService<IMapper>();

        var source = new ProfileSource { Name = "Test" };
        var dest = mapper.Map<ProfileDest>(source);

        Prova.Assertions.Assert.Equal("Test", dest.Name);
    }
}

public class ProfileSource { public string Name { get; set; } = string.Empty; }
public class ProfileDest { public string Name { get; set; } = string.Empty; }

public class ProfilingEnabledProfile : Profile
{
    public ProfilingEnabledProfile()
    {
        EnablePerformanceProfiling = true;
        CreateMap<ProfileSource, ProfileDest>();
    }
}
