using AutoMappic;
using Microsoft.Extensions.DependencyInjection;
using Prova;

namespace AutoMappic.Tests;

public class ConflictSource { public string Name { get; set; } = ""; }
public class ConflictDest { public string Info { get; set; } = ""; }

public class Profile1Conflict : Profile
{
    public Profile1Conflict()
    {
        CreateMap<ConflictSource, ConflictDest>()
            .ForMember(d => d.Info, opt => opt.MapFrom(s => "From Profile 1: " + s.Name));
    }
}

public class Profile2Conflict : Profile
{
    public Profile2Conflict()
    {
        CreateMap<ConflictSource, ConflictDest>()
            .ForMember(d => d.Info, opt => opt.MapFrom(s => "From Profile 2: " + s.Name));
    }
}

public class ConflictTests
{
    [Fact]
    public void DuplicateMapping_ShouldNotCrashBuild_AndDeterministic()
    {
        var services = new ServiceCollection();
        services.AddSingleton<Profile, Profile1Conflict>();
        services.AddSingleton<Profile, Profile2Conflict>();
        services.AddAutoMappic();

        var sp = services.BuildServiceProvider();
        var mapper = sp.GetRequiredService<IMapper>();

        var src = new ConflictSource { Name = "Test" };
        var dest = mapper.Map<ConflictSource, ConflictDest>(src);

        Prova.Assertions.Assert.True(dest.Info.Contains("From Profile"), "Should have mapped from a profile");
    }
}
