using AutoMappic.Tests.Fixtures;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public class DictSource
{
    public Dictionary<string, User> Users { get; set; } = new();
}

public class DictDto
{
    public Dictionary<string, UserSummaryDto> Users { get; set; } = new();
}

public sealed class DictionaryMappingTests
{
    private sealed class DictProfile : Profile
    {
        public DictProfile()
        {
            CreateMap<DictSource, DictDto>();
            CreateMap<User, UserSummaryDto>();
            CreateMap<CustomDictSource, CustomDictDto>();
        }
    }

    public class MyDict : Dictionary<string, int> { }
    public class CustomDictSource { public MyDict Stats { get; set; } = new(); }
    public class CustomDictDto { public Dictionary<string, int> Stats { get; set; } = new(); }

    /// <summary> Confirm that dictionary values are correctly transformed while maintaining their associated keys </summary>
    [Fact]
    public void Map_Dictionary_ComplexValuesTransformed()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<DictProfile>())
            .CreateMapper();

        var source = new DictSource();
        source.Users["admin"] = new User { Username = "root", Email = "admin@x.com" };

        var dto = mapper.Map<DictSource, DictDto>(source);

        Assert.Single(dto.Users);
        Assert.Equal("root", dto.Users["admin"].Username);
    }

    [Fact]
    [Prova.Description("Verify that custom classes inheriting from generic dictionaries correctly map as dictionaries.")]
    public void Map_InheritedDictionary_Works()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DictProfile>());
        var mapper = config.CreateMapper();

        var source = new CustomDictSource { Stats = new MyDict { { "a", 1 }, { "b", 2 } } };
        var dto = mapper.Map<CustomDictSource, CustomDictDto>(source);

        Assert.Equal(2, dto.Stats.Count);
        Assert.Equal(1, dto.Stats["a"]);
        Assert.Equal(2, dto.Stats["b"]);
    }
}
