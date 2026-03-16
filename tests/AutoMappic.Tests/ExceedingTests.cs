using AutoMappic.Tests.Fixtures;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public class SnakeSource
{
    public int user_id { get; set; }
    public string first_name { get; set; } = "";
    public string last_name { get; set; } = "";
}

public class SnakeDto
{
    public int UserId { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
}

public class NestedCollSource
{
    public List<List<int>> Matrix { get; set; } = new();
}

public class NestedCollDto
{
    public List<List<int>> Matrix { get; set; } = new();
}

public sealed class ExceedingTests
{
    private sealed class ExceedingProfile : Profile
    {
        public ExceedingProfile()
        {
            CreateMap<SnakeSource, SnakeDto>();
            CreateMap<NestedCollSource, NestedCollDto>();
        }
    }

    private readonly IMapper _mapper;

    public ExceedingTests()
    {
        _mapper = new MapperConfiguration(cfg => cfg.AddProfile<ExceedingProfile>())
            .CreateMapper();
    }

    [Fact]
    public void Map_SnakeCase_To_PascalCase()
    {
        var source = new SnakeSource { user_id = 42, first_name = "John", last_name = "Doe" };
        var dto = _mapper.Map<SnakeSource, SnakeDto>(source);

        Assert.Equal(42, dto.UserId);
        Assert.Equal("John", dto.FirstName);
        Assert.Equal("Doe", dto.LastName);
    }

    [Fact]
    public void Map_NestedCollections_MappedCorrectly()
    {
        var source = new NestedCollSource
        {
            Matrix = new List<List<int>>
            {
                new List<int> { 1, 2 },
                new List<int> { 3, 4 }
            }
        };

        var dto = _mapper.Map<NestedCollSource, NestedCollDto>(source);

        Assert.Equal(2, dto.Matrix.Count);
        Assert.Equal(1, dto.Matrix[0][0]);
        Assert.Equal(4, dto.Matrix[1][1]);
    }
}
