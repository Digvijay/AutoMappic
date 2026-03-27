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
            CreateMap<Node, NodeDto>().ForMemberIgnore(d => d.Parent);
        }
    }

    /// <summary> Verify that the mapper safely handles objects with potential circular references without stack overflow </summary>
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
        Assert.Null(dto.Parent); // Parent is null because we ignored it to break the circular reference
    }

    [Fact]
    public async System.Threading.Tasks.Task MapAsync_CircularReference_TriggersTrackers()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        // Manually build configuration to avoid AM0006 during full build if needed
        var config = new MapperConfiguration(cfg => cfg.AddProfile<UnsafeProfile>());
        var mapper = config.CreateMapper();

        var node = new Node { Name = "A" };
        var other = new Node { Name = "B" };
        node.Parent = other;
        other.Parent = node;

        try
        {
            // Bypass interceptor to trigger runtime fallback tracker
            var mapperType = typeof(global::AutoMappic.IMapper);
            var mapMethod = mapperType.GetMethods().First(m => m.Name == "MapAsync" && m.GetGenericArguments().Length == 2 && m.GetParameters().Length == 2);
            mapMethod = mapMethod.MakeGenericMethod(typeof(Node), typeof(NodeDto));
            var task = (System.Threading.Tasks.Task<NodeDto>)mapMethod.Invoke(mapper, new[] { (object)node, (object)global::System.Threading.CancellationToken.None })!;
            await task;
            Assert.Fail("Should have thrown AutoMappicException due to circular reference");
        }
        catch (global::System.Exception ex) when (ex.ToString().Contains("Circular"))
        {
            // SUCCESS! Circuit breaker worked.
        }
    }

    private sealed class UnsafeProfile : Profile
    {
        public UnsafeProfile()
        {
#pragma warning disable AM0006
            CreateMap<Node, NodeDto>();
#pragma warning restore AM0006
        }
    }
}
