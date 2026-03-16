using AutoMappic.Tests.Fixtures;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public enum Status { Pending, Active, Deleted }
public class StatusWrapper { public Status Status { get; set; } }
public class StatusWrapperDto { public string Status { get; set; } = ""; }

public class WideningWrapper { public int Value { get; set; } }
public class WideningWrapperDto { public long Value { get; set; } }

public sealed class MilestoneProfile : Profile
{
    public MilestoneProfile()
    {
        CreateMap<StatusWrapper, StatusWrapperDto>();
        CreateMap<WideningWrapper, WideningWrapperDto>();
        CreateMap<User, UserSummaryDto>();
        CreateMap<Order, OrderDto>();
        CreateMap<CollWrapper, CollWrapperDto>();
        CreateMap<WideningWrapperDto, WideningWrapperDto>();
    }
}

public sealed class FinalMilestoneTests
{
    private readonly IMapper _mapper;

    public FinalMilestoneTests()
    {
        _mapper = new MapperConfiguration(cfg => cfg.AddProfile<MilestoneProfile>())
            .CreateMapper();
    }

    [Fact]
    public void Map_EnumToString()
    {
        var source = new StatusWrapper { Status = Status.Active };
        var dto = _mapper.Map<StatusWrapper, StatusWrapperDto>(source);

        Assert.Equal("Active", dto.Status);
    }

    [Fact]
    public void Map_IntToLong_Widening()
    {
        var source = new WideningWrapper { Value = 42 };
        var dto = _mapper.Map<WideningWrapper, WideningWrapperDto>(source);

        Assert.Equal(42L, dto.Value);
    }

    [Fact]
    public void Map_ArrayToList_Complex()
    {
        var sourceUsers = new[] { new User { Username = "alice" }, new User { Username = "bob" } };
        var source = new CollWrapper { Items = new HashSet<User>(sourceUsers) };

        var dto = _mapper.Map<CollWrapper, CollWrapperDto>(source);

        Assert.NotNull(dto);
        Assert.Equal(2, dto.Items.Count);
        Assert.Equal("alice", dto.Items[0].Username);
    }

    [Fact]
    public void Map_DeepNull_DoesNotThrow()
    {
        var order = new Order { Customer = null };
        var dto = _mapper.Map<Order, OrderDto>(order);

        Assert.Equal(string.Empty, dto.CustomerName);
    }

    [Fact]
    public void Map_MultipleEnumValues_MapsCorrectStrings()
    {
        var s1 = new StatusWrapper { Status = Status.Pending };
        var s2 = new StatusWrapper { Status = Status.Deleted };

        Assert.Equal("Pending", _mapper.Map<StatusWrapper, StatusWrapperDto>(s1).Status);
        Assert.Equal("Deleted", _mapper.Map<StatusWrapper, StatusWrapperDto>(s2).Status);
    }

    [Fact]
    public void Map_LargeLong_ToLong()
    {
        var source = new WideningWrapperDto { Value = long.MaxValue };
        var dest = _mapper.Map<WideningWrapperDto, WideningWrapperDto>(source);

        Assert.Equal(long.MaxValue, dest.Value);
    }
}
