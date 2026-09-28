using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public sealed class AsyncDeepTests
{
    private IMapper CreateMapper<TProfile>() where TProfile : Profile, new()
    {
        return new MapperConfiguration(cfg => cfg.AddProfile<TProfile>())
            .CreateMapper();
    }

    /// <summary> Verify that mapping a collection of objects that require async resolution still produces correct results. </summary>
    [Fact]
    public async Task MapAsync_CollectionOfAsyncObjects_Works()
    {
        IMapper mapper = CreateMapper<DeepAsyncCollProfile>();
        var source = new CollParentSource { Children = [new() { Val = "a" }, new() { Val = "b" }] };

        CollParentDest result = await mapper.MapAsync<CollParentSource, CollParentDest>(source);

        Assert.Equal(2, result.Children.Count);
        Assert.Equal("A-A", result.Children[0].Val);
        Assert.Equal("B-B", result.Children[1].Val);
    }

    public class CollParentSource { public List<ChildAsyncSource> Children { get; set; } = []; }
    public class ChildAsyncSource { public string Val { get; set; } = ""; }
    public class CollParentDest { public List<ChildDest> Children { get; set; } = []; }
    public class ChildDest { public string Val { get; set; } = ""; }


    public class DeepAsyncCollProfile : Profile
    {
        public DeepAsyncCollProfile()
        {
            CreateMap<CollParentSource, CollParentDest>();
            CreateMap<ChildAsyncSource, ChildDest>()
                .ForMember(d => d.Val, opt => opt.MapFromAsync<ValDoubleResolver>());
        }
    }

    public class ValDoubleResolver : IAsyncValueResolver<ChildAsyncSource, string>
    {
        public Task<string> ResolveAsync(ChildAsyncSource source) => Task.FromResult(source.Val.ToUpperInvariant() + "-" + source.Val.ToUpperInvariant());
    }
}
