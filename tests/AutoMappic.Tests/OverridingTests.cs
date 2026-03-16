using AutoMappic.Tests.Fixtures;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public sealed class OverridingTests
{
    private sealed class OverrideProfile : Profile
    {
        public OverrideProfile()
        {
            CreateMap<Order, OrderDto>()
                .ForMember(d => d.CustomerName, opt => opt.MapFrom(s => "Override: " + s.Customer!.Name));
        }
    }

    [Fact]
    public void Map_WithForMember_OverridesFlattening()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<OverrideProfile>())
            .CreateMapper();

        var source = new Order
        {
            Customer = new Customer { Name = "Acme" }
        };

        // Note: Runtime fallback doesn't support ForMember expressions yet (returns null placeholder).
        // This test documents the CURRENT LACK of support in fallback or verifies it doesn't crash.
        var dto = mapper.Map<Order, OrderDto>(source);

        Assert.NotNull(dto);
        // Since RuntimeMaps implementation, the fallback correctly executes the delegate.
        Assert.Equal("Override: Acme", dto.CustomerName);
    }
}
