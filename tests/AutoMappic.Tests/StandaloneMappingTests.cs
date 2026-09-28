using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

[AutoMap(typeof(SourceDto), ReverseMap = true, DeleteOrphans = true)]
public partial class DestinationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<ChildDto> Children { get; set; } = [];
}

public class SourceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<SourceChild> Children { get; set; } = [];
}

[AutoMap(typeof(SourceChild), ReverseMap = true)]
public partial class ChildDto
{
    [AutoMappicKey]
    public int Id { get; set; }
    public string Val { get; set; } = "";
}

public class SourceChild
{
    public int Id { get; set; }
    public string Val { get; set; } = "";
}

public class StandaloneMappingTests
{
    private IMapper GetMapper()
    {
        // Even without explicit profiles, Standalone [AutoMap] classes are discovered by the generator
        // and hooked into the Mapper via interceptors. For the runtime fallback or manual use:
        var config = new MapperConfiguration(cfg => { });
        return config.CreateMapper();
    }

    [Fact]
    [Description("0.6.0: Verify standalone [AutoMap] attribute discovery and mapping.")]
    public void Standalone_AutoMap_Works()
    {
        var source = new SourceDto { Id = 1, Name = "Test" };
        IMapper mapper = GetMapper();

        DestinationDto dest = mapper.Map<DestinationDto>(source);

        Assert.Equal(1, dest.Id);
        Assert.Equal("Test", dest.Name);
    }

    [Fact]
    [Description("0.6.0: Verify [AutoMap(ReverseMap = true)] generates the reverse mapping.")]
    public void Standalone_ReverseMap_Works()
    {
        var dest = new DestinationDto { Id = 2, Name = "Reverse" };
        IMapper mapper = GetMapper();

        SourceDto source = mapper.Map<SourceDto>(dest);

        Assert.Equal(2, source.Id);
        Assert.Equal("Reverse", source.Name);
    }

    [Fact]
    [Description("0.6.0: Verify [AutoMap(DeleteOrphans = true)] removes missing items from collections.")]
    public void DeleteOrphans_Works_When_Enabled()
    {
        var source = new SourceDto
        {
            Children = [new() { Id = 1, Val = "A" }]
        };

        var dest = new DestinationDto
        {
            Children = [
                new() { Id = 1, Val = "Old" },
                new() { Id = 2, Val = "Gone" }
            ]
        };

        IMapper mapper = GetMapper();
        mapper.Map(source, dest);

        Assert.Equal(1, dest.Children.Count);
        Assert.Equal(1, dest.Children[0].Id);
        Assert.Equal("A", dest.Children[0].Val);
    }

    [Fact]
    [Description("0.6.0: Verify ProjectTo uses the new high-performance static Projection fields.")]
    public void ProjectTo_Uses_Static_Expression()
    {
        IQueryable<SourceDto> data = new List<SourceDto> {
            new() { Id = 1, Name = "A" },
            new() { Id = 2, Name = "B" }
        }.AsQueryable();
        _ = GetMapper();
        var projected = data.ProjectTo<DestinationDto>().ToList();

        Assert.Equal(2, projected.Count);
        Assert.Equal("A", projected[0].Name);
    }
}
