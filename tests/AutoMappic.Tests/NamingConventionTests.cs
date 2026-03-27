using Microsoft.Extensions.DependencyInjection;
using Prova;

namespace AutoMappic.Tests;

public class NamingConventionTests
{
    [Fact]
    public void NamingConvention_ShouldWork_WithManualOverride()
    {
        var services = new ServiceCollection();
        services.AddAutoMappicFromAutoMappic_Tests();
        var sp = services.BuildServiceProvider();
        var mapper = sp.GetRequiredService<IMapper>();

        var source = new NamingSnakeSource { first_name = "John", last_name = "Doe" };
        var dest = mapper.Map<NamingSnakeDto>(source);

        Prova.Assertions.Assert.Equal("John", dest.FirstName);
        Prova.Assertions.Assert.Equal("Doe", dest.LastName);
    }

    [Fact]
    public void NamingConvention_ShouldWork_WithProfileLevel()
    {
        var services = new ServiceCollection();
        services.AddAutoMappicFromAutoMappic_Tests();
        var sp = services.BuildServiceProvider();
        var mapper = sp.GetRequiredService<IMapper>();

        var source = new ProfileSnakeSource { email_address = "test@example.com" };
        var dest = mapper.Map<NamingStandardDto>(source);

        Prova.Assertions.Assert.Equal("test@example.com", dest.EmailAddress);
    }
    [Fact]
    public void NamingConvention_ShouldWork_WithKebabCase()
    {
        var services = new ServiceCollection();
        services.AddAutoMappicFromAutoMappic_Tests();
        var sp = services.BuildServiceProvider();
        var mapper = sp.GetRequiredService<IMapper>();

        var source = new KebabSource { ["product-id"] = "PROD-123" };
        var dest = mapper.Map<NamingKebabDto>(source);

        Prova.Assertions.Assert.Equal("PROD-123", dest.ProductId);
    }

    [Fact]
    public void NamingConvention_ShouldWork_WithAcronyms()
    {
        var services = new ServiceCollection();
        services.AddAutoMappicFromAutoMappic_Tests();
        var sp = services.BuildServiceProvider();
        var mapper = sp.GetRequiredService<IMapper>();

        var source = new AcronymSource { CustomerID = 42 };
        var dest = mapper.Map<AcronymDto>(source);

        Prova.Assertions.Assert.Equal(42, dest.CustomerId);
    }

}

public class CycleProfile : Profile
{
    public CycleProfile()
    {
#pragma warning disable AM0006
        CreateMap<CycleNode, CycleNode>();
#pragma warning restore AM0006
    }
}

public class CycleNode
{
    public string Name { get; set; } = "";
    public CycleNode? Next { get; set; }
}

public class NamingSnakeSource { public string first_name { get; set; } = string.Empty; public string last_name { get; set; } = string.Empty; }
public class NamingSnakeDto { public string FirstName { get; set; } = string.Empty; public string LastName { get; set; } = string.Empty; }

public class ProfileSnakeSource { public string email_address { get; set; } = string.Empty; }
public class NamingStandardDto { public string EmailAddress { get; set; } = string.Empty; }

public class KebabSource : System.Collections.Generic.Dictionary<string, string> { }
public class NamingKebabDto { public string ProductId { get; set; } = string.Empty; }

public class AcronymSource { public int CustomerID { get; set; } }
public class AcronymDto { public int CustomerId { get; set; } }

public class ProfileNamingProfile : Profile
{
    public ProfileNamingProfile()
    {
        SourceNamingConvention = new LowerUnderscoreNamingConvention();
        DestinationNamingConvention = new PascalCaseNamingConvention();

        CreateMap<ProfileSnakeSource, NamingStandardDto>();
    }
}

public class OverrideNamingProfile : Profile
{
    public OverrideNamingProfile()
    {
        CreateMap<NamingSnakeSource, NamingSnakeDto>()
            .WithNamingConvention(new LowerUnderscoreNamingConvention(), new PascalCaseNamingConvention());

        CreateMap<AcronymSource, AcronymDto>()
            .WithNamingConvention(new PascalCaseNamingConvention(), new CamelCaseNamingConvention());
    }
}

public class KebabNamingProfile : Profile
{
    public KebabNamingProfile()
    {
        CreateMap<KebabSource, NamingKebabDto>()
            .WithNamingConvention(new KebabCaseNamingConvention(), new PascalCaseNamingConvention());
    }
}
