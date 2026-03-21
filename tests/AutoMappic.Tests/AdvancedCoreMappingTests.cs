using System;
using System.Collections.Generic;
using System.Linq;
using AutoMappic;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

#region Unique Models for Permutations

public struct ExhaustiveStruct { public int X; public int Y; }

public class ExhaustiveValueTypeSource { public (int, string) Tuple { get; set; } public ExhaustiveStruct Struct { get; set; } }
public class ExhaustiveValueTypeDto { public (int, string) Tuple { get; set; } public ExhaustiveStruct Struct { get; set; } }

public class ExhaustiveNamingSource { public string pascal_case { get; set; } public string camelCase { get; set; } }
public class ExhaustiveNamingDto { public string PascalCase { get; set; } public string camel_case { get; set; } }

public class ExhaustiveFieldSource { public string FieldVal; public string PropertyVal { get; set; } }
public class ExhaustiveFieldDto { public string FieldVal; public string PropertyVal { get; set; } }

#endregion

public class ExhaustiveMappingProfile : Profile
{
    public ExhaustiveMappingProfile()
    {
        CreateMap<ExhaustiveValueTypeSource, ExhaustiveValueTypeDto>();
        CreateMap<ExhaustiveNamingSource, ExhaustiveNamingDto>();
        CreateMap<ExhaustiveFieldSource, ExhaustiveFieldDto>();
    }
}

public class ExhaustiveMappingTests
{
    private static IMapper GetMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<ExhaustiveMappingProfile>());
        return config.CreateMapper();
    }

    [Fact]
    [Description("Exhaustive: Verify mapping for structs and tuples (value types) uniquely.")]
    public void Test_ValueTypes_Exhaustive()
    {
        var mapper = GetMapper();
        var source = new ExhaustiveValueTypeSource 
        { 
            Tuple = (1, "A"), 
            Struct = new ExhaustiveStruct { X = 10, Y = 20 } 
        };
        var result = mapper.Map<ExhaustiveValueTypeDto>(source);

        Assert.Equal(1, result.Tuple.Item1);
        Assert.Equal("A", result.Tuple.Item2);
        Assert.Equal(10, result.Struct.X);
    }

    [Fact]
    [Description("Exhaustive: Verify naming convention transformation (snake_case -> PascalCase).")]
    public void Test_Naming_Exhaustive()
    {
        var mapper = GetMapper();
        var source = new ExhaustiveNamingSource { pascal_case = "P", camelCase = "C" };
        var result = mapper.Map<ExhaustiveNamingDto>(source);

        Assert.Equal("P", result.PascalCase);
        Assert.Equal("C", result.camel_case);
    }

    [Fact]
    [Description("Exhaustive: Verify public field mapping support.")]
    public void Test_FieldMapping_Exhaustive()
    {
        var mapper = GetMapper();
        var source = new ExhaustiveFieldSource { FieldVal = "F", PropertyVal = "P" };
        var result = mapper.Map<ExhaustiveFieldDto>(source);

        Assert.Equal("F", result.FieldVal);
        Assert.Equal("P", result.PropertyVal);
    }
}
