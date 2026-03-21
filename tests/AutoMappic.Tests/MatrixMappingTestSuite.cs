using System;
using System.Collections.Generic;
using System.Linq;
using AutoMappic;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

#region Matrix Models

public enum MatrixEnum { V1 = 1, V2 = 2 }

public class KitchenSinkSource
{
    public string S { get; set; } = "S";
    public int I { get; set; } = 1;
    public double D { get; set; } = 2.2;
    public decimal M { get; set; } = 3.3m;
    public bool B { get; set; } = true;
    public DateTime Dt { get; set; } = new DateTime(2025, 1, 1);
    public MatrixEnum E { get; set; } = MatrixEnum.V2;
}

public class KitchenSinkDto
{
    public string S { get; set; } = "";
    public int I { get; set; }
    public double D { get; set; }
    public decimal M { get; set; }
    public bool B { get; set; }
    public DateTime Dt { get; set; }
    public MatrixEnum E { get; set; }
}

public class EnumNumericSource { public MatrixEnum E { get; set; } public int Numeric { get; set; } }
public class EnumNumericDto { public int E { get; set; } public MatrixEnum Numeric { get; set; } }

public class DeepNav1 { public DeepNav2 Nav2 { get; set; } = new(); }
public class DeepNav2 { public DeepNav3 Nav3 { get; set; } = new(); }
public class DeepNav3 { public string TargetValue { get; set; } = "Success"; }

public class DeepFlattenedSource { public DeepNav1 Nav1 { get; set; } = new(); }
public class DeepFlattenedDto { public string Nav1Nav2Nav3TargetValue { get; set; } = ""; }

public class MultiGenericSource<T1, T2> { public T1 V1 { get; set; } = default!; public T2 V2 { get; set; } = default!; }
public class MultiGenericDto<T1, T2> { public T1 V1 { get; set; } = default!; public T2 V2 { get; set; } = default!; }

#endregion

public class MatrixProfile : Profile
{
    public MatrixProfile()
    {
        CreateMap<KitchenSinkSource, KitchenSinkDto>();
        CreateMap<EnumNumericSource, EnumNumericDto>();
        CreateMap<DeepFlattenedSource, DeepFlattenedDto>();
        CreateMap(typeof(MultiGenericSource<,>), typeof(MultiGenericDto<,>));
    }
}

public class MatrixMappingTestSuite
{
    private static IMapper GetMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MatrixProfile>());
        return config.CreateMapper();
    }

    /// <summary> Verify mapping for a class with a wide variety of primitive and built-in types. </summary>
    [Fact]
    public void Test_KitchenSink_Matrix()
    {
        var mapper = GetMapper();
        var source = new KitchenSinkSource();
        var result = mapper.Map<KitchenSinkDto>(source);

        Assert.Equal("S", result.S);
        Assert.Equal(1, result.I);
        Assert.Equal(2.2, result.D);
        Assert.Equal(3.3m, result.M);
        Assert.Equal(true, result.B);
        Assert.Equal(source.Dt, result.Dt);
        Assert.Equal((int)MatrixEnum.V2, (int)result.E);
    }

    /// <summary> Verify automatic conversion between enums and numeric types (Int to Enum / Enum to Int). </summary>
    [Fact]
    public void Test_EnumNumeric_Matrix()
    {
        var mapper = GetMapper();
        var source = new EnumNumericSource { E = MatrixEnum.V1, Numeric = 2 };
        var result = mapper.Map<EnumNumericDto>(source);

        Assert.Equal(1, result.E);
        Assert.Equal((int)MatrixEnum.V2, (int)result.Numeric);
    }

    /// <summary> Verify deep flattening across 3 levels (A.B.C -> ABC). </summary>
    [Fact]
    public void Test_DeepFlattening_Matrix()
    {
        var mapper = GetMapper();
        var source = new DeepFlattenedSource();
        source.Nav1.Nav2.Nav3.TargetValue = "FinalDest";
        var result = mapper.Map<DeepFlattenedDto>(source);

        Assert.Equal("FinalDest", result.Nav1Nav2Nav3TargetValue);
    }

    /// <summary> Verify mapping of complex generic types with multiple type parameters (Binary Matrix). </summary>
    [Fact]
    public void Test_MultiGeneric_Matrix()
    {
        var mapper = GetMapper();
        var source = new MultiGenericSource<int, string> { V1 = 5, V2 = "Five" };
        var result = mapper.Map<MultiGenericDto<int, string>>(source);

        Assert.Equal(5, result.V1);
        Assert.Equal("Five", result.V2);
    }
}
