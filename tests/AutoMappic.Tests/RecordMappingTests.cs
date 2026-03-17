using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

// ─── Fixtures ─────────────────────────────────────────────────────────────

public record UserRecord(string Username, string Email);

public record UserRecordDto
{
    // Records can have parameterless constructors if we define them or don't use primary ctors
    public string Username { get; init; } = "";
    public string Email { get; init; } = "";
}

public record DeepRecord(UserRecord Owner, string Name);

public record DeepRecordDto
{
    public string OwnerUsername { get; init; } = "";
    public string Name { get; init; } = "";
}

// ─── Tests ────────────────────────────────────────────────────────────────

public sealed class RecordMappingTests
{
    private sealed class RecordProfile : Profile
    {
        public RecordProfile()
        {
            CreateMap<UserRecord, UserRecordDto>();
            CreateMap<DeepRecord, DeepRecordDto>();
        }
    }

    /// <summary> Verify that source records can be mapped to DTOs with 'init-only' properties </summary>
    [Fact]
    public void Map_SimpleRecord_ToInitOnlyDto()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<RecordProfile>())
            .CreateMapper();

        var source = new UserRecord("fowler", "david@ms.com");
        var dto = mapper.Map<UserRecord, UserRecordDto>(source);

        Assert.Equal("fowler", dto.Username);
        Assert.Equal("david@ms.com", dto.Email);
    }

    /// <summary> Ensure that nested source records are correctly flattened into a flat DTO structure </summary>
    [Fact]
    public void Map_DeepRecord_FlattenedCorrectly()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<RecordProfile>())
            .CreateMapper();

        var source = new DeepRecord(new UserRecord("boss", "b@x.com"), "Project X");
        var dto = mapper.Map<DeepRecord, DeepRecordDto>(source);

        Assert.Equal("boss", dto.OwnerUsername);
        Assert.Equal("Project X", dto.Name);
    }
}
