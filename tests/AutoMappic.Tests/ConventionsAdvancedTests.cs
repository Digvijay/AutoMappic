using AutoMappic.Tests.Fixtures;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

// ─── Fixtures ─────────────────────────────────────────────────────────────

public sealed class SecretUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public sealed class MethodDto
{
    public string DisplayName { get; set; } = string.Empty;
}

// ─── Tests ────────────────────────────────────────────────────────────────

public sealed class IgnoredPropertyTests
{
    private sealed class IgnoreProfile : Profile
    {
        public IgnoreProfile()
        {
            CreateMap<User, SecretUserDto>()
                .ForMemberIgnore(d => d.Email)
                .ForMemberIgnore(d => d.Username);
        }
    }

    /// <summary> Verify that specific source members can be explicitly ignored using the 'ForMemberIgnore' configuration </summary>
    [Fact]
    public void Map_WithIgnoredMembers_DoesNotCopyThem()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<IgnoreProfile>())
            .CreateMapper();

        var source = new User { Id = 1, Username = "secret", Email = "hide@me.com" };
        var dto = mapper.Map<User, SecretUserDto>(source);

        Assert.Equal(1, dto.Id);
        Assert.Equal(string.Empty, dto.Username);
        Assert.Equal(string.Empty, dto.Email);
    }
}

public sealed class MethodMappingTests
{
    private sealed class MethodProfile : Profile
    {
        public MethodProfile()
        {
            CreateMap<User, MethodDto>();
        }
    }

    /// <summary> Confirm that source 'Get' methods are automatically resolved to matching destination properties by convention </summary>
    [Fact]
    public void Map_SourceMethod_MapsToDestinationProperty()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MethodProfile>())
            .CreateMapper();

        var source = new User { Username = "dev", Email = "dev@example.com" };
        // User has GetDisplayName(), DTO has DisplayName
        var dto = mapper.Map<User, MethodDto>(source);

        Assert.Equal("dev <dev@example.com>", dto.DisplayName);
    }
}
