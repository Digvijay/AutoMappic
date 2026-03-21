using System;
using System.Collections.Generic;
using System.Linq;
using AutoMappic;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

#region Numeric & Nullable Models
public class NumericSource { public int IntVal { get; set; } public int LongVal { get; set; } public double DoubleVal { get; set; } public int? NullInt { get; set; } public int? NullIntNotNull { get; set; } }
public class NumericDto { public long IntVal { get; set; } public long LongVal { get; set; } public double DoubleVal { get; set; } public int NullInt { get; set; } public int? NullIntNotNull { get; set; } }
#endregion

#region Inheritance Models
public class BaseSource { public string CreatedBy { get; set; } }
public class DerivedSource : BaseSource { public string ModifiedBy { get; set; } }
public class CombinedDto { public string CreatedBy { get; set; } public string ModifiedBy { get; set; } }
#endregion

#region Condition & Lifecycle Models
public class LogicSource { public int Status { get; set; } public string Note { get; set; } }
public class LogicDto { public bool IsActive { get; set; } public string Note { get; set; } [AutoMappicIgnore] public string Audit { get; set; } }
#endregion

#region Open Generics Models
public class GenericWrapper<T> { public T Value { get; set; } }
public class GenericDto<T> { public T Value { get; set; } }
#endregion

public class ComplexScenarioProfile : Profile
{
    public ComplexScenarioProfile()
    {
        // Numerics (Widening only for now)
        CreateMap<NumericSource, NumericDto>();

        // Inheritance
        CreateMap<DerivedSource, CombinedDto>();

        // Complex Logic
        CreateMap<LogicSource, LogicDto>()
            .ForMember(d => d.IsActive, opt => opt.MapFrom(s => s.Status > 0))
            .ForMember(d => d.Note, opt => opt.Condition((s, d) => !string.IsNullOrEmpty(s.Note)))
            .AfterMap((s, d) => d.Audit = "PROCESSED");

        // Open Generics
        CreateMap(typeof(GenericWrapper<>), typeof(GenericDto<>));
    }
}

public class ComplexScenarioTests
{
    private static IMapper GetMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ComplexScenarioProfile>());
        return config.CreateMapper();
    }

    [Fact]
    [Description("Complex: Verify numeric widening and nullable-to-non-nullable transitions.")]
    public void Test_Numeric_Transitions()
    {
        var mapper = GetMapper();
        var source = new NumericSource 
        { 
            IntVal = 42, 
            LongVal = 100, 
            DoubleVal = 3.14, 
            NullInt = null, 
            NullIntNotNull = 7 
        };

        var result = mapper.Map<NumericDto>(source);

        Assert.Equal(42L, result.IntVal);
        Assert.Equal(100L, result.LongVal);
        Assert.Equal(3.14, result.DoubleVal);
        Assert.Equal(0, result.NullInt);
        Assert.Equal(7, result.NullIntNotNull!.Value);
    }

    [Fact]
    [Description("Complex: Verify that properties from base classes are correctly mapped in derived types.")]
    public void Test_Inheritance_Mapping()
    {
        var mapper = GetMapper();
        var source = new DerivedSource { CreatedBy = "System", ModifiedBy = "Admin" };
        var result = mapper.Map<CombinedDto>(source);

        Assert.Equal("System", result.CreatedBy);
        Assert.Equal("Admin", result.ModifiedBy);
    }

    [Fact]
    [Description("Complex: Verify combined member conditions and lifecycle hooks.")]
    public void Test_Logic_And_Hooks()
    {
        var mapper = GetMapper();
        
        // Scenario 1: Condition met
        var s1 = new LogicSource { Status = 1, Note = "Valid" };
        var r1 = mapper.Map<LogicDto>(s1);
        Assert.True(r1.IsActive);
        Assert.Equal("Valid", r1.Note);
        Assert.Equal("PROCESSED", r1.Audit);

        // Scenario 2: Condition NOT met
        var s2 = new LogicSource { Status = 0, Note = null! };
        var r2 = new LogicDto { Note = "KEEP ME" };
        mapper.Map(s2, r2);
        Assert.False(r2.IsActive);
        Assert.Equal("KEEP ME", r2.Note); // Condition failed, so old value kept
        Assert.Equal("PROCESSED", r2.Audit);
    }

    [Fact]
    [Description("Complex: Verify open generic mappings for custom types.")]
    public void Test_OpenGenerics_Custom()
    {
        var mapper = GetMapper();

        // Custom Generic
        var wrapper = new GenericWrapper<int> { Value = 123 };
        var genericDto = mapper.Map<GenericDto<int>>(wrapper);
        Assert.Equal(123, genericDto.Value);
    }
}
