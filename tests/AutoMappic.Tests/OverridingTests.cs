using AutoMappic;
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

    /// <summary> Confirm that custom member mapping configurations (ForMember) correctly override automatic PascalCase flattening rules </summary>
    [Fact]
    public void Map_WithForMember_OverridesFlattening()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<OverrideProfile>())
            .CreateMapper();

        var source = new Order
        {
            Customer = new Customer { Name = "Acme" }
        };

        // We use MapCore directly to bypass the source generator's interception and test the fallback engine.
        var dto = (OrderDto)((Mapper)mapper).MapCore(typeof(Order), typeof(OrderDto), source, null);

        Assert.NotNull(dto);
        // Since RuntimeMaps implementation, the fallback correctly executes the delegate.
        Assert.Equal("Override: Acme", dto.CustomerName);
    }
}
