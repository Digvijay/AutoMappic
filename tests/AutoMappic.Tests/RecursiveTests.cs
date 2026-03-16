using AutoMappic.Tests.Fixtures;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

// ─── Fixtures ─────────────────────────────────────────────────────────────

public sealed class Node
{
    public string Name { get; set; } = string.Empty;
    public Node? Parent { get; set; }
}

public sealed class NodeDto
{
    public string Name { get; set; } = string.Empty;
    public NodeDto? Parent { get; set; }
}

// ─── Tests ────────────────────────────────────────────────────────────────

public sealed class CircularReferenceTests
{
    private sealed class NodeProfile : Profile
    {
        public NodeProfile()
        {
            CreateMap<Node, NodeDto>();
        }
    }

    [Fact]
    public void Map_CircularReference_DoesNotStackOverflowInFallback()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<NodeProfile>())
            .CreateMapper();

        var node = new Node { Name = "Child" };
        var parent = new Node { Name = "Parent" };
        node.Parent = parent;
        // Circular: parent.Parent = node;

        var dto = mapper.Map<Node, NodeDto>(node);

        Assert.Equal("Child", dto.Name);
        Assert.NotNull(dto.Parent);
        Assert.Equal("Parent", dto.Parent!.Name);
    }
}
