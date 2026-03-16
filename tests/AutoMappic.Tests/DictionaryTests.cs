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
        }
    }

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
}
