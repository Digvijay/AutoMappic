using AutoMappic.Tests.Fixtures;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public class Address { public string City { get; set; } = ""; }
public class DeepUser { public string Name { get; set; } = ""; public Address? Address { get; set; } }
public class DeepOrder { public DeepUser? User { get; set; } }

public class DeepOrderDto { public string UserAddressCity { get; set; } = ""; }

public sealed class AdvancedFlatteningTests
{
    private sealed class DeepProfile : Profile
    {
        public DeepProfile()
        {
            CreateMap<DeepOrder, DeepOrderDto>();
        }
    }

    [Fact]
    public void Map_DeepFlattening_ThreeLevels()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<DeepProfile>())
            .CreateMapper();

        var source = new DeepOrder
        {
            User = new DeepUser
            {
                Address = new Address { City = "Redmond" }
            }
        };

        var dto = mapper.Map<DeepOrder, DeepOrderDto>(source);
        Assert.Equal("Redmond", dto.UserAddressCity);
    }

    [Fact]
    public void Map_DeepFlattening_MiddleNull_ReturnsDefault()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<DeepProfile>())
            .CreateMapper();

        var source = new DeepOrder { User = new DeepUser { Address = null } };
        var dto = mapper.Map<DeepOrder, DeepOrderDto>(source);

        Assert.Equal(string.Empty, dto.UserAddressCity);
    }
}
