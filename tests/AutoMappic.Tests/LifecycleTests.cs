using System.Threading.Tasks;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public sealed class LifecycleTests
{
    private IMapper CreateMapper<TProfile>() where TProfile : Profile, new()
    {
        return new MapperConfiguration(cfg => cfg.AddProfile<TProfile>())
            .CreateMapper();
    }

    [Fact]
    public void BeforeAfterMap_Sync_ExecutedInOrder()
    {
        var mapper = CreateMapper<LifecycleProfile>();
        var source = new LifecycleSource { Value = 10 };
        var result = mapper.Map<LifecycleSource, LifecycleDest>(source);

        Assert.Equal(10, result.Value); 
        Assert.True(result.WasBeforeCalled);
        Assert.True(result.WasAfterCalled);
    }

    [Fact]
    public async Task BeforeAfterMap_Async_ExecutedInOrder()
    {
        var mapper = CreateMapper<LifecycleProfile>();
        var source = new LifecycleSource { Value = 10 };
        var result = await mapper.MapAsync<LifecycleSource, LifecycleDest>(source);

        Assert.Equal(10, result.Value);
        Assert.True(result.WasBeforeAsyncCalled);
        // Sync AfterMap is also called during async mapping (dual-emission)
        Assert.True(result.WasAfterCalled);
        Assert.True(result.WasAfterAsyncCalled);
    }
}
