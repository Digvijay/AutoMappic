using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

// ─── Fixtures ─────────────────────────────────────────────────────────────

public class AccessSource
{
    public string Name { get; set; } = string.Empty;
    public string InternalInfo { get; internal set; } = "Secret";
    private string PrivateInfo { get; set; } = "Top Secret";
}

public class AccessDto
{
    public string Name { get; set; } = string.Empty;

    // AutoMappic should ignore this because it can't write to it.
    public string ReadOnly { get; } = "Fixed";

    // AutoMappic should ignore this because it's not public.
    internal string InternalOnly { get; set; } = "NoTouch";
}

// ─── Tests ────────────────────────────────────────────────────────────────

public sealed class AccessibilityTests
{
    private sealed class AccessProfile : Profile
    {
        public AccessProfile()
        {
            CreateMap<AccessSource, AccessDto>();
        }
    }

    [Fact]
    public void Map_RespectsAccessibility_SkipsInaccessibleMembers()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<AccessProfile>())
            .CreateMapper();

        var source = new AccessSource { Name = "Tester" };
        var dto = mapper.Map<AccessSource, AccessDto>(source);

        Assert.Equal("Tester", dto.Name);
        Assert.Equal("Fixed", dto.ReadOnly);
        Assert.Equal("NoTouch", dto.InternalOnly);
    }
}
