namespace AutoMappic.Tests;

public class LifecycleProfile : Profile
{
    public LifecycleProfile()
    {
        CreateMap<LifecycleSource, LifecycleDest>()
            .ForMemberIgnore(d => d.WasBeforeCalled)
            .ForMemberIgnore(d => d.WasBeforeAsyncCalled)
            .ForMemberIgnore(d => d.WasAfterCalled)
            .ForMemberIgnore(d => d.WasAfterAsyncCalled)
            .BeforeMap((src, dest) => { dest.WasBeforeCalled = true; })
            .AfterMap((src, dest) => { dest.WasAfterCalled = true; })
            .BeforeMapAsync(async (src, dest) => { await System.Threading.Tasks.Task.Yield(); dest.WasBeforeAsyncCalled = true; })
            .AfterMapAsync(async (src, dest) => { await System.Threading.Tasks.Task.Yield(); dest.WasAfterAsyncCalled = true; });
    }
}
