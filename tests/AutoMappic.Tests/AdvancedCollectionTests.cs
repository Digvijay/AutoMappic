using AutoMappic.Tests.Fixtures;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public class CollWrapper { public HashSet<User> Items { get; set; } = new(); }
public class CollWrapperDto { public List<UserSummaryDto> Items { get; set; } = new(); }

public class DictWrapper { public Dictionary<int, User> Dict { get; set; } = new(); }
public class DictWrapperDto { public Dictionary<string, UserSummaryDto> Dict { get; set; } = new(); }

public class NestedOrderWrapper { public List<Order> Orders { get; set; } = new(); }
public class NestedOrderWrapperDto { public List<OrderDto> Orders { get; set; } = new(); }

public sealed class AdvancedCollectionMappingTests
{
    private sealed class AdvCollProfile : Profile
    {
        public AdvCollProfile()
        {
            CreateMap<User, UserSummaryDto>();
            CreateMap<Order, OrderDto>();
            CreateMap<CollWrapper, CollWrapperDto>();
            CreateMap<DictWrapper, DictWrapperDto>();
            CreateMap<NestedOrderWrapper, NestedOrderWrapperDto>();
        }
    }

    [Fact]
    public void Map_HashSetToList()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<AdvCollProfile>())
            .CreateMapper();

        var source = new CollWrapper { Items = new HashSet<User> { new User { Username = "alice" } } };
        var dto = mapper.Map<CollWrapper, CollWrapperDto>(source);

        Assert.Single(dto.Items);
        Assert.Equal("alice", dto.Items[0].Username);
    }

    [Fact]
    public void Map_DictionaryWithKeyTypeChange()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<AdvCollProfile>())
            .CreateMapper();

        var source = new DictWrapper();
        source.Dict[1] = new User { Username = "bob" };

        var dto = mapper.Map<DictWrapper, DictWrapperDto>(source);

        Assert.Single(dto.Dict);
        Assert.Equal("bob", dto.Dict["1"].Username);
    }

    [Fact]
    public void Map_DeepNestedCollection_NullSafety()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<AdvCollProfile>())
            .CreateMapper();

        var source = new NestedOrderWrapper
        {
            Orders = new List<Order> {
                new Order { Customer = null },
                new Order { Customer = new Customer { Name = "X" } }
            }
        };

        var dto = mapper.Map<NestedOrderWrapper, NestedOrderWrapperDto>(source);

        Assert.Equal(2, dto.Orders.Count);
        Assert.Equal(string.Empty, dto.Orders[0].CustomerName);
        Assert.Equal("X", dto.Orders[1].CustomerName);
    }
}
