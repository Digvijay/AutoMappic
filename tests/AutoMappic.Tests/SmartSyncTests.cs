using System.Collections.Generic;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

// ─── Fixtures (shared) ───────────────────────────────────────────────────────

public class SSyncItem { public int Id { get; set; } public string Name { get; set; } = ""; }

public class DSyncItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    [AutoMappicIgnore]
    public int KeepState { get; set; }
}

public class SSync { public IList<SSyncItem> Items { get; set; } = new List<SSyncItem>(); }
public class DSync { public IList<DSyncItem> Items { get; set; } = new List<DSyncItem>(); }

public class SmartSyncProfile : Profile
{
    public SmartSyncProfile()
    {
        CreateMap<SSync, DSync>();
        CreateMap<SSyncItem, DSyncItem>();
    }
}

// ─── Tests ───────────────────────────────────────────────────────────────────

/// <summary>
///   Tests for the EF Core-aware identity-keyed collection syncing (Smart-Sync).
///   Verifies Upsert semantics: update existing, add new, preserve state.
/// </summary>
public sealed class SmartSyncTests
{
    private AutoMappic.IMapper CreateMapper()
    {
        return new MapperConfiguration(cfg => cfg.AddProfile<SmartSyncProfile>())
            .CreateMapper();
    }

    // ─── Core Upsert Behaviour ───────────────────────────────────────────

    /// <summary> Existing entity is updated in-place, added entity is appended </summary>
    [Fact]
    public void IdentitySync_UpdatesExistingEntities_DoesNotReplaceThem()
    {
        var src = new SSync
        {
            Items = new List<SSyncItem>
            {
                new SSyncItem { Id = 1, Name = "Updated Name" },
                new SSyncItem { Id = 2, Name = "New Item" }
            }
        };

        var destItem1 = new DSyncItem { Id = 1, Name = "Old Name", KeepState = 42 };
        var dest = new DSync
        {
            Items = new List<DSyncItem> { destItem1 }
        };

        var mapper = CreateMapper();
        mapper.Map(src, dest);

        // Existing item updated in place
        Assert.Equal(2, dest.Items.Count);
        Assert.Equal("Updated Name", dest.Items[0].Name);
        Assert.Equal(42, dest.Items[0].KeepState); // State preserved!
        Assert.Same(destItem1, dest.Items[0]); // Reference unchanged

        // New item added
        Assert.Equal(2, dest.Items[1].Id);
        Assert.Equal("New Item", dest.Items[1].Name);
    }

    /// <summary> When source has matching key, dest object reference stays the same </summary>
    [Fact]
    public void IdentitySync_PreservesObjectReference_WhenKeyMatches()
    {
        var destItem = new DSyncItem { Id = 5, Name = "Original", KeepState = 99 };
        var dest = new DSync { Items = new List<DSyncItem> { destItem } };

        var src = new SSync
        {
            Items = new List<SSyncItem> { new SSyncItem { Id = 5, Name = "Modified" } }
        };

        CreateMapper().Map(src, dest);

        Assert.Equal(1, dest.Items.Count);
        Assert.Same(destItem, dest.Items[0]);
        Assert.Equal("Modified", dest.Items[0].Name);
        Assert.Equal(99, dest.Items[0].KeepState);
    }

    // ─── Add-Only Scenarios ──────────────────────────────────────────────

    /// <summary> All source items are new — they should be appended </summary>
    [Fact]
    public void IdentitySync_AllNewItems_AppendsToDestination()
    {
        var dest = new DSync { Items = new List<DSyncItem>() };
        var src = new SSync
        {
            Items = new List<SSyncItem>
            {
                new SSyncItem { Id = 1, Name = "A" },
                new SSyncItem { Id = 2, Name = "B" }
            }
        };

        CreateMapper().Map(src, dest);

        Assert.Equal(2, dest.Items.Count);
        Assert.Equal("A", dest.Items[0].Name);
        Assert.Equal("B", dest.Items[1].Name);
    }

    // ─── Empty Source ────────────────────────────────────────────────────

    /// <summary> Empty source collection clears destination (full replacement semantics) </summary>
    [Fact]
    public void IdentitySync_EmptySource_DestinationIsCleared()
    {
        var dest = new DSync
        {
            Items = new List<DSyncItem> { new DSyncItem { Id = 1, Name = "Survivor" } }
        };
        var src = new SSync { Items = new List<SSyncItem>() };

        CreateMapper().Map(src, dest);

        // Clear-then-Upsert: empty source means destination is cleared
        Assert.Equal(0, dest.Items.Count);
    }

    // ─── Multiple Updates ────────────────────────────────────────────────

    /// <summary> Multiple existing items all get updated in the correct order </summary>
    [Fact]
    public void IdentitySync_MultipleExistingItems_AllUpdated()
    {
        var d1 = new DSyncItem { Id = 1, Name = "Old1", KeepState = 10 };
        var d2 = new DSyncItem { Id = 2, Name = "Old2", KeepState = 20 };
        var d3 = new DSyncItem { Id = 3, Name = "Old3", KeepState = 30 };
        var dest = new DSync { Items = new List<DSyncItem> { d1, d2, d3 } };

        var src = new SSync
        {
            Items = new List<SSyncItem>
            {
                new SSyncItem { Id = 1, Name = "New1" },
                new SSyncItem { Id = 2, Name = "New2" },
                new SSyncItem { Id = 3, Name = "New3" }
            }
        };

        CreateMapper().Map(src, dest);

        Assert.Equal(3, dest.Items.Count);
        Assert.Equal("New1", dest.Items[0].Name);
        Assert.Equal(10, dest.Items[0].KeepState);
        Assert.Same(d1, dest.Items[0]);

        Assert.Equal("New2", dest.Items[1].Name);
        Assert.Equal(20, dest.Items[1].KeepState);
        Assert.Same(d2, dest.Items[1]);

        Assert.Equal("New3", dest.Items[2].Name);
        Assert.Equal(30, dest.Items[2].KeepState);
        Assert.Same(d3, dest.Items[2]);
    }

    // ─── Mixed: Update + Add ─────────────────────────────────────────────

    /// <summary> Some items update, some are new additions </summary>
    [Fact]
    public void IdentitySync_MixedUpdateAndAdd_BothWork()
    {
        var d1 = new DSyncItem { Id = 1, Name = "Existing", KeepState = 77 };
        var dest = new DSync { Items = new List<DSyncItem> { d1 } };

        var src = new SSync
        {
            Items = new List<SSyncItem>
            {
                new SSyncItem { Id = 1, Name = "Updated" },
                new SSyncItem { Id = 10, Name = "Brand New" },
                new SSyncItem { Id = 20, Name = "Also New" }
            }
        };

        CreateMapper().Map(src, dest);

        Assert.Equal(3, dest.Items.Count);

        // Updated in place
        Assert.Same(d1, dest.Items[0]);
        Assert.Equal("Updated", dest.Items[0].Name);
        Assert.Equal(77, dest.Items[0].KeepState);

        // New items appended
        Assert.Equal(10, dest.Items[1].Id);
        Assert.Equal("Brand New", dest.Items[1].Name);
        Assert.Equal(20, dest.Items[2].Id);
        Assert.Equal("Also New", dest.Items[2].Name);
    }

    // ─── No Key Match ────────────────────────────────────────────────────

    /// <summary> When no source Id matches any dest Id, old items are cleared, new are added </summary>
    [Fact]
    public void IdentitySync_NoKeyOverlap_OldClearedNewAdded()
    {
        var d1 = new DSyncItem { Id = 100, Name = "Unmatched", KeepState = 5 };
        var dest = new DSync { Items = new List<DSyncItem> { d1 } };

        var src = new SSync
        {
            Items = new List<SSyncItem>
            {
                new SSyncItem { Id = 1, Name = "NewA" },
                new SSyncItem { Id = 2, Name = "NewB" }
            }
        };

        CreateMapper().Map(src, dest);

        // Clear-then-Upsert: d1 had no match so was cleared, 2 new items added
        Assert.Equal(2, dest.Items.Count);
        Assert.Equal("NewA", dest.Items[0].Name);
        Assert.Equal("NewB", dest.Items[1].Name);
    }

    // ─── Id = 0 Edge Case ────────────────────────────────────────────────

    /// <summary> Id == 0 is often "unsaved" in EF Core — should still match correctly </summary>
    [Fact]
    public void IdentitySync_ZeroId_TreatedAsValidKey()
    {
        var d0 = new DSyncItem { Id = 0, Name = "ZeroId", KeepState = 1 };
        var dest = new DSync { Items = new List<DSyncItem> { d0 } };

        var src = new SSync
        {
            Items = new List<SSyncItem> { new SSyncItem { Id = 0, Name = "StillZero" } }
        };

        CreateMapper().Map(src, dest);

        Assert.Equal(1, dest.Items.Count);
        Assert.Same(d0, dest.Items[0]);
        Assert.Equal("StillZero", dest.Items[0].Name);
    }
}
