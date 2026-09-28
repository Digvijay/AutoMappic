using AutoMappic.Tests.Fixtures;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public class CollWrapper { public HashSet<User> Items { get; set; } = []; }
public class CollWrapperDto { public List<UserSummaryDto> Items { get; set; } = []; }

public class DictWrapper { public Dictionary<int, User> Dict { get; set; } = []; }
public class DictWrapperDto { public Dictionary<string, UserSummaryDto> Dict { get; set; } = []; }

public class NestedOrderWrapper { public List<Order> Orders { get; set; } = []; }
public class NestedOrderWrapperDto { public List<OrderDto> Orders { get; set; } = []; }

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

    /// <summary> Validate transformation from HashSet source to List destination, ensuring all items are preserved </summary>
    [Fact]
    public void Map_HashSetToList()
    {
        IMapper mapper = new MapperConfiguration(cfg => cfg.AddProfile<AdvCollProfile>())
            .CreateMapper();

        var source = new CollWrapper { Items = [new User { Username = "alice" }] };
        CollWrapperDto dto = mapper.Map<CollWrapper, CollWrapperDto>(source);

        Assert.Single(dto.Items);
        Assert.Equal("alice", dto.Items[0].Username);
    }

    /// <summary> Confirm that dictionary keys can be correctly converted to different types (e.g., int to string) during mapping </summary>
    [Fact]
    public void Map_DictionaryWithKeyTypeChange()
    {
        IMapper mapper = new MapperConfiguration(cfg => cfg.AddProfile<AdvCollProfile>())
            .CreateMapper();

        var source = new DictWrapper();
        source.Dict[1] = new User { Username = "bob" };

        DictWrapperDto dto = mapper.Map<DictWrapper, DictWrapperDto>(source);

        Assert.Single(dto.Dict);
        Assert.Equal("bob", dto.Dict["1"].Username);
    }

    /// <summary> Ensure deep nested collections are mapped with proper null safety for intermediate elements </summary>
    [Fact]
    public void Map_DeepNestedCollection_NullSafety()
    {
        IMapper mapper = new MapperConfiguration(cfg => cfg.AddProfile<AdvCollProfile>())
            .CreateMapper();

        var source = new NestedOrderWrapper
        {
            Orders = [
                new Order { Id = 1, Customer = null },
                new Order { Id = 2, Customer = new Customer { Name = "X" } }
            ]
        };

        NestedOrderWrapperDto dto = mapper.Map<NestedOrderWrapper, NestedOrderWrapperDto>(source);

        Assert.Equal(2, dto.Orders.Count);
        Assert.Equal(string.Empty, dto.Orders[0].CustomerName);
        Assert.Equal("X", dto.Orders[1].CustomerName);
    }
}
