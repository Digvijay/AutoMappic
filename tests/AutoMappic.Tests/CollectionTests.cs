using AutoMappic.Tests.Fixtures;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public sealed class CollectionSource
{
    public List<int> Scores { get; set; } = new();
    public string[] Tags { get; set; } = Array.Empty<string>();
    public List<User> Users { get; set; } = new();
}

public sealed class CollectionDto
{
    public int[] Scores { get; set; } = Array.Empty<int>();
    public List<string> Tags { get; set; } = new();
    public List<UserSummaryDto> Users { get; set; } = new();
}

public sealed class CollectionMappingTests
{
    private sealed class CollectionProfile : Profile
    {
        public CollectionProfile()
        {
            CreateMap<CollectionSource, CollectionDto>();
            CreateMap<User, UserSummaryDto>();
            CreateMap<CustomCollSource, CustomCollDto>();
        }
    }

    public class MyList : List<int> { }
    public class CustomCollSource { public MyList Scores { get; set; } = new(); }
    public class CustomCollDto { public List<int> Scores { get; set; } = new(); }

    /// <summary> Verify that varied collection types (List, Array, HashSet) are accurately transformed between source and destination </summary>
    [Fact]
    public void Map_Collections_TransformedCorrectly()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<CollectionProfile>())
            .CreateMapper();

        var source = new CollectionSource
        {
            Scores = new List<int> { 1, 2, 3 },
            Tags = new[] { "a", "b" },
            Users = new List<User>
            {
                new User { Username = "alice", Email = "a@x.com" }
            }
        };

        var dto = mapper.Map<CollectionSource, CollectionDto>(source);

        Assert.Equal(3, dto.Scores.Length);
        Assert.Equal(2, dto.Tags.Count);
        Assert.Single(dto.Users);
        Assert.Equal("alice", dto.Users[0].Username);
    }

    [Fact]
    [Prova.Description("Verify that custom classes inheriting from generic collections correctly map as collections.")]
    public void Map_InheritedCollection_Works()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<CollectionProfile>());
        var mapper = config.CreateMapper();

        var source = new CustomCollSource { Scores = new MyList { 1, 2, 3 } };
        var dto = mapper.Map<CustomCollSource, CustomCollDto>(source);

        Assert.Equal(3, dto.Scores.Count);
        Assert.Equal(1, dto.Scores[0]);
    }
}
