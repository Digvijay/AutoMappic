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
            .ForMemberIgnore(d => d.HookOrder)
            .BeforeMap((src, dest) => { dest.WasBeforeCalled = true; dest.HookOrder.Add("Before"); })
            .AfterMap((src, dest) => { dest.WasAfterCalled = true; dest.HookOrder.Add("After"); })
            .BeforeMapAsync(async (src, dest) => { await System.Threading.Tasks.Task.Yield(); dest.WasBeforeAsyncCalled = true; dest.HookOrder.Add("BeforeAsync"); })
            .AfterMapAsync(async (src, dest) => { await System.Threading.Tasks.Task.Yield(); dest.WasAfterAsyncCalled = true; dest.HookOrder.Add("AfterAsync"); });
    }
}
