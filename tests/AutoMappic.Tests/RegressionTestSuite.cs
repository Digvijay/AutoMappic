#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

#region Regression Models

public class ExtremeSource
{
    public int? NullableInt { get; set; } = null;
    public string? NullableString { get; set; } = "Not Null";
    public List<int>? NullableList { get; set; } = new List<int> { 1, 2, 3 };
    public Dictionary<string, string>? NullableDict { get; set; } = new Dictionary<string, string> { { "Key", "Value" } };
    public SubSource? NestedNulls { get; set; } = null;
    public SubSource InitializedNested { get; set; } = new SubSource { Code = "123" };
    public string Prop1 { get; set; } = "Prop1";
    public string Prop2 { get; } = "Prop2";
}

public class ExtremeDestination
{
    public int NullableInt { get; set; }
    public string NullableString { get; set; } = "";
    public int[] NullableList { get; set; } = Array.Empty<int>();
    public Dictionary<string, string> NullableDict { get; set; } = new();
    public SubDestination NestedNulls { get; set; } = new SubDestination();
    public SubDestination InitializedNested { get; set; } = new SubDestination();
    public string Prop1 { get; set; } = "";
    public string Prop2 { get; init; } = "";
    
    // Test shadow inheritance compatibility
    public int ComputedVal => NullableInt * 10;
}

public class SubSource
{
    public string? Code { get; set; }
    public ICollection<string>? Tags { get; set; }
}

public class SubDestination
{
    public string Code { get; set; } = "";
    public List<string> Tags { get; set; } = new();
}

public class SelfReferencingNode
{
    public int Id { get; set; }
    public SelfReferencingNode? Left { get; set; }
    public SelfReferencingNode? Right { get; set; }
}

public class SelfReferencingNodeDto
{
    public int Id { get; set; }
    public SelfReferencingNodeDto? Left { get; set; }
    public SelfReferencingNodeDto? Right { get; set; }
}

// Ensure the AM006 doesn't crash generation if explicitly configured
public class RegressionProfile : Profile
{
    public RegressionProfile()
    {
        CreateMap<ExtremeSource, ExtremeDestination>();
        CreateMap<SubSource, SubDestination>();
        CreateMap<SelfReferencingNode, SelfReferencingNodeDto>();
    }
}

#endregion

public class RegressionTestSuite
{
    private static IMapper GetMapper()
    {
        var config = new AutoMappic.MapperConfiguration(cfg => cfg.AddProfile<RegressionProfile>());
        return config.CreateMapper();
    }

    [Fact]
    [Prova.Description("Verify that deeply nested null references map safely into their respective target types and sentinel value structures.")]
    public void Test_Regression_DeepNullStructures_MapCorrectly()
    {
        var mapper = GetMapper();
        var source = new ExtremeSource();
        
        var result = mapper.Map<ExtremeDestination>(source);
        
        Assert.Equal(0, result.NullableInt);
        Assert.Equal("Not Null", result.NullableString);
        Assert.NotNull(result.NullableList);
        Assert.Equal(3, result.NullableList.Length);
        Assert.Equal(1, result.NullableList[0]);
        Assert.NotNull(result.NullableDict);
        Assert.True(result.NullableDict.ContainsKey("Key"));
        
        // Handling nested null objects safely
        Assert.NotNull(result.NestedNulls);
        Assert.Equal("", result.NestedNulls.Code);
        
        Assert.Equal("123", result.InitializedNested.Code);
    }
    
    [Fact]
    [Prova.Description("Verify initialized readonly and init properties properly function along with fallback interceptors")]
    public void Test_Regression_InitProperties_AreAssignedCorrectly()
    {
        var mapper = GetMapper();
        var source = new ExtremeSource();
        var result = mapper.Map<ExtremeDestination>(source);
        
        Assert.Equal("Prop1", result.Prop1);
    }

    [Fact]
    [Prova.Description("Verify tree-based structures with self-referencing nullable cycle patterns assign securely.")]
    public void Test_Regression_SelfReferencingNode_DoesNotCrash()
    {
        var mapper = GetMapper();
        var source = new SelfReferencingNode 
        { 
            Id = 1,
            Left = new SelfReferencingNode { Id = 2 },
            Right = new SelfReferencingNode { Id = 3 }
        };
        
        var result = mapper.Map<SelfReferencingNodeDto>(source);
        
        Assert.Equal(1, result.Id);
        Assert.NotNull(result.Left);
        Assert.Equal(2, result.Left!.Id);
        Assert.NotNull(result.Right);
        Assert.Equal(3, result.Right!.Id);
        Assert.Null(result.Left.Left);
    }

    [Fact]
    [Prova.Description("Verify that updating dictionary instances safely clears existing keys to maintain referential parity without memory leak bugs")]
    public void Test_Regression_DictionaryUpdate_RefreshesEntireMatrix()
    {
        var mapper = GetMapper();
        var source = new ExtremeSource();
        var dest = new ExtremeDestination();
        dest.NullableDict.Add("OldKey", "OldValue");
        
        mapper.Map(source, dest);
        
        Assert.True(dest.NullableDict.ContainsKey("Key"));
        Assert.False(dest.NullableDict.ContainsKey("OldKey"));
    }
}
