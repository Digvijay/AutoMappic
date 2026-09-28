using AutoMappic.Tests.Fixtures;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

/// <summary>
///   Covers the shapes of collection and dictionary destination the runtime fallback has to
///   materialise.
/// </summary>
/// <remarks>
///   These exist because the fallback used to build every destination collection as
///   <c>List&lt;destItemType&gt;</c> via <c>MakeGenericType</c>, and every destination dictionary
///   as <c>Dictionary&lt;k, v&gt;</c> the same way. Closing a generic type over a value type at
///   runtime is the one thing Native AOT cannot do, so those two lines forced
///   <c>RequiresDynamicCode</c> onto the library's entire public mapping surface - which in turn
///   made a correct consumer's AOT build fail.
///
///   Neither call was needed: the destination type is already closed at both sites. The fallback
///   now activates the destination type directly, or produces an array when the destination is an
///   array or an interface. That is a behavioural change to core functionality, so each shape is
///   pinned here rather than inferred from the annotation tests.
/// </remarks>
public sealed class CollectionMaterialisationTests
{
    private sealed class MaterialisationProfile : Profile
    {
        public MaterialisationProfile()
        {
            CreateMap<User, UserSummaryDto>();
            CreateMap<ListHolder, ListHolderDto>();
            CreateMap<ListHolder, ArrayHolderDto>();
            CreateMap<ListHolder, EnumerableHolderDto>();
            CreateMap<ListHolder, ReadOnlyListHolderDto>();
            CreateMap<ValueHolder, ValueHolderDto>();
            CreateMap<ValueDictHolder, ValueDictHolderDto>();
        }
    }

    public sealed class ListHolder { public List<User> Items { get; set; } = []; }
    public sealed class ListHolderDto { public List<UserSummaryDto> Items { get; set; } = []; }
    public sealed class ArrayHolderDto { public UserSummaryDto[] Items { get; set; } = []; }
    public sealed class EnumerableHolderDto { public IEnumerable<UserSummaryDto> Items { get; set; } = []; }
    public sealed class ReadOnlyListHolderDto { public IReadOnlyList<UserSummaryDto> Items { get; set; } = []; }

    public sealed class ValueHolder { public List<int> Numbers { get; set; } = []; }
    public sealed class ValueHolderDto { public List<int> Numbers { get; set; } = []; }

    public sealed class ValueDictHolder { public Dictionary<int, int> Scores { get; set; } = []; }
    public sealed class ValueDictHolderDto { public Dictionary<int, int> Scores { get; set; } = []; }

    private static IMapper CreateMapper() =>
        new MapperConfiguration(cfg => cfg.AddProfile<MaterialisationProfile>()).CreateMapper();

    private static ListHolder Source() => new()
    {
        Items = [new() { Username = "alice" }, new() { Username = "bob" }],
    };

    /// <summary> A concrete List destination is activated directly rather than rebuilt generically </summary>
    [Fact]
    public void Concrete_list_destination_is_populated()
    {
        var dto = CreateMapper().Map<ListHolder, ListHolderDto>(Source());

        Assert.Equal(2, dto.Items.Count);
        Assert.Equal("alice", dto.Items[0].Username);
        Assert.Equal("bob", dto.Items[1].Username);
    }

    /// <summary> An array destination is materialised as a real array of the destination item type </summary>
    [Fact]
    public void Array_destination_is_populated()
    {
        var dto = CreateMapper().Map<ListHolder, ArrayHolderDto>(Source());

        Assert.Equal(2, dto.Items.Length);
        Assert.Equal("alice", dto.Items[0].Username);
    }

    /// <summary> An interface-typed destination is satisfied without closing a generic type at runtime </summary>
    [Fact]
    public void Enumerable_destination_is_populated()
    {
        var dto = CreateMapper().Map<ListHolder, EnumerableHolderDto>(Source());

        Assert.Equal(2, dto.Items.Count());
        Assert.Equal("alice", dto.Items.First().Username);
    }

    /// <summary> IReadOnlyList is satisfied too, which an array provides and a List also would </summary>
    [Fact]
    public void ReadOnlyList_destination_is_populated()
    {
        var dto = CreateMapper().Map<ListHolder, ReadOnlyListHolderDto>(Source());

        Assert.Equal(2, dto.Items.Count);
        Assert.Equal("bob", dto.Items[1].Username);
    }

    /// <summary> A collection of value types is the case that used to require runtime code generation </summary>
    [Fact]
    public void Value_type_collection_is_populated()
    {
        var source = new ValueHolder { Numbers = [1, 2, 3] };

        var dto = CreateMapper().Map<ValueHolder, ValueHolderDto>(source);

        Assert.Equal(3, dto.Numbers.Count);
        Assert.Equal(1, dto.Numbers[0]);
        Assert.Equal(3, dto.Numbers[2]);
    }

    /// <summary> A dictionary keyed and valued by value types is the dictionary equivalent of that case </summary>
    [Fact]
    public void Value_type_dictionary_is_populated()
    {
        var source = new ValueDictHolder { Scores = new Dictionary<int, int> { [1] = 10, [2] = 20 } };

        var dto = CreateMapper().Map<ValueDictHolder, ValueDictHolderDto>(source);

        Assert.Equal(2, dto.Scores.Count);
        Assert.Equal(10, dto.Scores[1]);
        Assert.Equal(20, dto.Scores[2]);
    }
}
