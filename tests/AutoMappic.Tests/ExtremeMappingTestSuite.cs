using System;
using System.Collections.Generic;
using System.Linq;
using AutoMappic;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

#region Extreme Models

public class ShadowBase { public string Value { get; set; } = "Base"; }
public class ShadowDerived : ShadowBase { public new int Value { get; set; } = 42; }

public class ShadowBaseDto { public string Value { get; set; } = ""; }
public class ShadowDerivedDto : ShadowBaseDto { public new int Value { get; set; } }

public class DeepResult<T> { public T Data { get; set; } = default!; }
public class DeepWrapper<T> { public T Content { get; set; } = default!; }

public class ExtremeOuter { public ExtremeLevel1 L1 { get; set; } = new(); }
public class ExtremeLevel1 { public ExtremeLevel2 L2 { get; set; } = new(); }
public class ExtremeLevel2 { public ExtremeLevel3 L3 { get; set; } = new(); }
public class ExtremeLevel3 { public ExtremeLevel4 L4 { get; set; } = new(); }
public class ExtremeLevel4 { public string FinalValue { get; set; } = "DeepEnd"; }

public class ExtremeOuterDto { public string L1L2L3L4FinalValue { get; set; } = ""; }

#endregion

public class ExtremeProfile : Profile
{
    public ExtremeProfile()
    {
        CreateMap<ShadowBase, ShadowBaseDto>();
        CreateMap<ShadowDerived, ShadowDerivedDto>();
        CreateMap(typeof(DeepResult<>), typeof(DeepResult<>));
        CreateMap<ExtremeOuter, ExtremeOuterDto>();
    }
}

public class ExtremeMappingTestSuite
{
    private static IMapper GetMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExtremeProfile>());
        return config.CreateMapper();
    }

    /// <summary> Verify that AutoMappic correctly handles the 'new' keyword shadowing in inheritance hierarchies. </summary>
    [Fact]
    public void Test_PropertyShadowing_Extreme()
    {
        var mapper = GetMapper();
        var source = new ShadowDerived { Value = 100 };
        // We need to be careful here because 'Value' is shadowed. 
        // AutoMappic's ConventionEngine uses GetAllWritableMembers which uses a dictionary by name.
        // The most derived member should win.
        var result = mapper.Map<ShadowDerivedDto>(source);

        Assert.Equal(100, result.Value);
        // Ensure base value didn't magically get set (it remains the target class default, e.g. string.Empty) 
        // because object initializers cannot bind to shadowed members.
        Assert.Equal("", ((ShadowBaseDto)result).Value);
    }

    /// <summary> Verify that AutoMappic can flatten a path reaching 5 levels deep (L1.L2.L3.L4.FinalValue -> L1L2L3L4FinalValue). </summary>
    [Fact]
    public void Test_HighDepthFlattening_Extreme()
    {
        var mapper = GetMapper();
        var source = new ExtremeOuter();
        source.L1.L2.L3.L4.FinalValue = "FoundIt";
        var result = mapper.Map<ExtremeOuterDto>(source);

        Assert.Equal("FoundIt", result.L1L2L3L4FinalValue);
    }
}
