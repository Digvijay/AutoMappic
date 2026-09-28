using Microsoft.Extensions.DependencyInjection;
using Prova;

namespace AutoMappic.Tests;

public class FluentApiTests
{
    public class Source { public string Name { get; set; } = ""; }
    public class Dest { public string Name { get; set; } = ""; }

    public class FluentProfile : Profile
    {
        public FluentProfile() => CreateMap<Source, Dest>();
    }

    [Fact]
    [Description("Verifies that the new MapTo<T> extension method works correctly and is intercepted.")]
    public void MapTo_Extension_Works()
    {
        var services = new ServiceCollection();
        services.AddAutoMappicFromAutoMappic_Tests();
        ServiceProvider sp = services.BuildServiceProvider();
        IMapper mapper = sp.GetRequiredService<IMapper>();

        var source = new Source { Name = "Fluent" };

        // This should use the intercepted extension method
        Dest dest = source.MapTo<Dest>(mapper);

        Prova.Assertions.Assert.Equal("Fluent", dest.Name);
    }
}
