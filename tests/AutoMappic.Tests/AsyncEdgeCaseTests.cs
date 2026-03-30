using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public sealed class AsyncEdgeCaseTests
{
    private IMapper CreateMapper<TProfile>() where TProfile : Profile, new()
    {
        return new MapperConfiguration(cfg => cfg.AddProfile<TProfile>())
            .CreateMapper();
    }

    /// <summary> Verify that multiple async resolvers in a single map work correctly without blocking. </summary>
    [Fact]
    public async Task MapAsync_MultipleResolvers_Works()
    {
        var mapper = CreateMapper<MultiAsyncProfile>();
        var source = new MultiSource { A = "a", B = "b", C = "c" };

        var result = await mapper.MapAsync<MultiSource, MultiDest>(source);

        Assert.Equal("A-VAL", result.A);
        Assert.Equal("B-VAL", result.B);
        Assert.Equal("C-VAL", result.C);
    }

    /// <summary> Ensure nested async mappings are correctly orchestrated. </summary>
    [Fact]
    public async Task MapAsync_DeepNesting_Works()
    {
        var mapper = CreateMapper<DeepAsyncProfile>();
        var source = new ParentSource { Child = new ChildSource { Value = "inner" } };

        var result = await mapper.MapAsync<ParentSource, ParentDest>(source);

        Assert.NotNull(result.Child);
        Assert.Equal("INNER", result.Child.Value);
    }

    /// <summary> Verify that async collection members work via the fallback logic when not explicitly mapped as async. </summary>
    [Fact]
    public async Task MapAsync_CollectionMember_Works()
    {
        var mapper = CreateMapper<CollectionAsyncProfile>();
        var source = new CollSource { Items = new List<string> { "one", "two" } };

        var result = await mapper.MapAsync<CollSource, CollDest>(source);

        Assert.Equal(2, result.Items.Count);
        Assert.Contains("one", result.Items);
        Assert.Contains("two", result.Items);
    }

    /// <summary> Test exception handling: if an async resolver fails, the Task should fail properly. </summary>
    [Fact]
    public async Task MapAsync_FailingResolver_Throws()
    {
        var mapper = CreateMapper<FailingAsyncProfile>();
        var source = new FailingSource { Value = "fail" };

        var task = mapper.MapAsync<FailingSource, FailingDest>(source);

        // C# Task.Wait() or await will throw. Prova's Assert.Throws is sync, 
        // but we can use try-catch or await it.
        bool thrown = false;
        try
        {
            await task;
        }
        catch (InvalidOperationException)
        {
            thrown = true;
        }
        Assert.True(thrown);
    }

    // --- Models ---

    public class MultiSource { public string A { get; set; } = ""; public string B { get; set; } = ""; public string C { get; set; } = ""; }
    public class MultiDest { public string A { get; set; } = ""; public string B { get; set; } = ""; public string C { get; set; } = ""; }

    public class ParentSource { public ChildSource Child { get; set; } = new(); }
    public class ChildSource { public string Value { get; set; } = ""; }
    public class ParentDest { public ChildDest Child { get; set; } = new(); }
    public class ChildDest { public string Value { get; set; } = ""; }

    public class CollSource { public List<string> Items { get; set; } = new(); }
    public class CollDest { public List<string> Items { get; set; } = new(); }

    public class FailingSource { public string Value { get; set; } = ""; }
    public class FailingDest { public string Value { get; set; } = ""; }

    // --- Profiles ---

    public class MultiAsyncProfile : Profile
    {
        public MultiAsyncProfile()
        {
            CreateMap<MultiSource, MultiDest>()
                .ForMember(d => d.A, opt => opt.MapFromAsync<AResolver>())
                .ForMember(d => d.B, opt => opt.MapFromAsync<BResolver>())
                .ForMember(d => d.C, opt => opt.MapFromAsync<CResolver>());
        }
    }

    public class DeepAsyncProfile : Profile
    {
        public DeepAsyncProfile()
        {
            CreateMap<ParentSource, ParentDest>();
            CreateMap<ChildSource, ChildDest>()
                .ForMember(d => d.Value, opt => opt.MapFromAsync<UpperResolver>());
        }
    }

    public class CollectionAsyncProfile : Profile
    {
        public CollectionAsyncProfile()
        {
            CreateMap<CollSource, CollDest>();
        }
    }

    public class FailingAsyncProfile : Profile
    {
        public FailingAsyncProfile()
        {
            CreateMap<FailingSource, FailingDest>()
                .ForMember(d => d.Value, opt => opt.MapFromAsync<FailingResolver>());
        }
    }

    // --- Resolvers ---

    public class AResolver : IAsyncValueResolver<MultiSource, string> { public Task<string> ResolveAsync(MultiSource s) => Task.FromResult("A-VAL"); }
    public class BResolver : IAsyncValueResolver<MultiSource, string> { public Task<string> ResolveAsync(MultiSource s) => Task.FromResult("B-VAL"); }
    public class CResolver : IAsyncValueResolver<MultiSource, string> { public Task<string> ResolveAsync(MultiSource s) => Task.FromResult("C-VAL"); }
    public class UpperResolver : IAsyncValueResolver<ChildSource, string> { public Task<string> ResolveAsync(ChildSource s) => Task.FromResult(s.Value.ToUpperInvariant()); }
    public class FailingResolver : IAsyncValueResolver<FailingSource, string> { public Task<string> ResolveAsync(FailingSource s) => throw new InvalidOperationException("BOOM"); }
}
