using AutoMappic.Tests.Fixtures;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

// ─── Fixtures ─────────────────────────────────────────────────────────────

public class Result<T>
{
    public T? Data { get; set; }
    public bool Success { get; set; }
}

public class ResultDto<T>
{
    public T? Data { get; set; }
    public bool Success { get; set; }
}

// ─── Tests ────────────────────────────────────────────────────────────────

public sealed class GenericsMappingTests
{
    private sealed class GenericProfile : Profile
    {
        public GenericProfile()
        {
            CreateMap<Result<User>, ResultDto<UserSummaryDto>>();
            CreateMap<User, UserSummaryDto>();
        }
    }

    /// <summary> Verify that generic wrapper types (e.g., Result&lt;T&gt;) can be mapped when their nested generic arguments have valid profiles registered </summary>
    [Fact]
    public void Map_GenericWrapper_MapsNestedGenericArgument()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<GenericProfile>())
            .CreateMapper();

        var source = new Result<User>
        {
            Success = true,
            Data = new User { Username = "generic_user", Email = "g@x.com" }
        };

        var dto = mapper.Map<Result<User>, ResultDto<UserSummaryDto>>(source);

        Assert.True(dto.Success);
        Assert.NotNull(dto.Data);
        Assert.Equal("generic_user", dto.Data!.Username);
    }
}
