using System;
using System.Collections.Generic;
using System.Linq;
using AutoMappic;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

#region Permutation Models

public class Outer<T> { public T Inner { get; set; } = default!; }
public class InnerGeneric<T> { public T Value { get; set; } = default!; }

public class BiWrapper<T1, T2> 
{ 
    public T1 Item1 { get; set; } = default!; 
    public T2 Item2 { get; set; } = default!; 
}

public class BiDto<T1, T2> 
{ 
    public T1 Item1 { get; set; } = default!; 
    public T2 Item2 { get; set; } = default!; 
}

public class BaseSource<T> { public T BaseValue { get; set; } = default!; }
public class DerivedSource<T> : BaseSource<T> { public T DerivedValue { get; set; } = default!; }

public class BaseDto<T> { public T BaseValue { get; set; } = default!; }
public class DerivedDto<T> : BaseDto<T> { public T DerivedValue { get; set; } = default!; }

public enum PermutationSourceEnum { Alpha, Beta, Gamma }
public enum PermutationDestEnum { Alpha, Beta, Gamma }

public class EnumSource { public PermutationSourceEnum Value { get; set; } }
public class EnumDto { public PermutationDestEnum Value { get; set; } }

public class NullableSource { public int? Val1 { get; set; } public int Val2 { get; set; } }
public class NullableDto { public int Val1 { get; set; } public int? Val2 { get; set; } }

public class CollectionPermutationSource { public Dictionary<string, List<int>> Data { get; set; } = new(); }
public class CollectionPermutationDto { public Dictionary<string, int[]> Data { get; set; } = new(); }

public class MultiEnumSource { public PermutationSourceEnum E1 { get; set; } public PermutationSourceEnum E2 { get; set; } }
public class MultiEnumDto { public PermutationDestEnum E1 { get; set; } public PermutationDestEnum E2 { get; set; } }

public class DeepNestingSource { public Outer<InnerGeneric<InnerGeneric<string>>> Deep { get; set; } = new(); }
public class DeepNestingDto { public Outer<InnerGeneric<InnerGeneric<string>>> Deep { get; set; } = new(); }

public class ComplexTupleWrapper { public (string Name, int Age) Identity { get; set; } }
public class ComplexClassDto { public (string Name, int Age) Identity { get; set; } }

#endregion

/// <summary>
/// A profile containing an exhaustive set of mapping permutations for advanced generic and structure testing.
/// </summary>
public class PermutationProfile : Profile
{
    public PermutationProfile()
    {
        // Open Generics
        CreateMap(typeof(Outer<>), typeof(Outer<>));
        CreateMap(typeof(InnerGeneric<>), typeof(InnerGeneric<>));
        CreateMap(typeof(BiWrapper<,>), typeof(BiDto<,>));
        CreateMap(typeof(BaseSource<>), typeof(BaseDto<>));
        CreateMap(typeof(DerivedSource<>), typeof(DerivedDto<>));

        // Enums and Nullables
        CreateMap<EnumSource, EnumDto>();
        CreateMap<NullableSource, NullableDto>();

        // Complex Collections
        CreateMap<CollectionPermutationSource, CollectionPermutationDto>();

        // More Permutations
        CreateMap<MultiEnumSource, MultiEnumDto>();
        CreateMap<DeepNestingSource, DeepNestingDto>();
        CreateMap<ComplexTupleWrapper, ComplexClassDto>();
    }
}

/// <summary>
/// Exhaustive test suite for complex mapping permutations and edge cases.
/// </summary>
public class PermutationTestSuite
{
    private static IMapper GetMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<PermutationProfile>());
        return config.CreateMapper();
    }

    /// <summary> Verify mapping of nested generic types like Outer&lt;InnerGeneric&lt;int&gt;&gt;. </summary>
    [Fact]
    public void Test_NestedGenerics_Exhaustive()
    {
        var mapper = GetMapper();
        var source = new Outer<InnerGeneric<int>> 
        { 
            Inner = new InnerGeneric<int> { Value = 42 } 
        };
        var result = mapper.Map<Outer<InnerGeneric<int>>>(source);

        Assert.Equal(42, result.Inner.Value);
    }

    /// <summary> Verify mapping of types with multiple type parameters (BiWrapper&lt;T1, T2&gt;). </summary>
    [Fact]
    public void Test_MultiParamGenerics_Exhaustive()
    {
        var mapper = GetMapper();
        var source = new BiWrapper<int, string> { Item1 = 100, Item2 = "Gold" };
        var result = mapper.Map<BiDto<int, string>>(source);

        Assert.Equal(100, result.Item1);
        Assert.Equal("Gold", result.Item2);
    }

    /// <summary> Verify that inheritance is respected in generic mappings (Base/Derived). </summary>
    [Fact]
    public void Test_InheritedGenerics_Exhaustive()
    {
        var mapper = GetMapper();
        var source = new DerivedSource<double> { BaseValue = 1.1, DerivedValue = 2.2 };
        var result = mapper.Map<DerivedDto<double>>(source);

        Assert.Equal(1.1, result.BaseValue);
        Assert.Equal(2.2, result.DerivedValue);
    }

    /// <summary> Verify mapping between matching enums. </summary>
    [Fact]
    public void Test_EnumMapping_Permutation()
    {
        var mapper = GetMapper();
        var source = new EnumSource { Value = PermutationSourceEnum.Gamma };
        var result = mapper.Map<EnumDto>(source);

        Assert.Equal((int)PermutationDestEnum.Gamma, (int)result.Value);
    }

    /// <summary> Verify automatic mapping between nullable and non-nullable primitives. </summary>
    [Fact]
    public void Test_NullableMapping_Permutation()
    {
        var mapper = GetMapper();
        var source = new NullableSource { Val1 = 123, Val2 = 456 };
        var result = mapper.Map<NullableDto>(source);

        Assert.Equal(123, result.Val1);
        Assert.Equal((int?)456, result.Val2);

        // Test with null
        source.Val1 = null;
        result = mapper.Map<NullableDto>(source);
        Assert.Equal(0, result.Val1); // default(int)
    }

    /// <summary> Verify mapping of dictionaries containing nested collections with type conversion (List to Array). </summary>
    [Fact]
    public void Test_ComplexCollectionMapping_Exhaustive()
    {
        var mapper = GetMapper();
        var source = new CollectionPermutationSource 
        { 
            Data = new Dictionary<string, List<int>> 
            { 
                { "A", new List<int> { 1, 2, 3 } } 
            } 
        };
        var result = mapper.Map<CollectionPermutationDto>(source);

        Assert.True(result.Data.ContainsKey("A"));
        Assert.Equal(3, result.Data["A"].Length);
        Assert.Equal(2, result.Data["A"][1]);
    }

    /// <summary> Verify that multiple enums in the same object are correctly converted with casts. </summary>
    [Fact]
    public void Test_MultiEnumMapping_Exhaustive()
    {
        var mapper = GetMapper();
        var source = new MultiEnumSource { E1 = PermutationSourceEnum.Alpha, E2 = PermutationSourceEnum.Beta };
        var result = mapper.Map<MultiEnumDto>(source);

        Assert.Equal((int)PermutationDestEnum.Alpha, (int)result.E1);
        Assert.Equal((int)PermutationDestEnum.Beta, (int)result.E2);
    }

    /// <summary> Verify that deeply nested generic structures (3 levels) are correctly navigated and mapped. </summary>
    [Fact]
    public void Test_DeepNesting_Exhaustive()
    {
        var mapper = GetMapper();
        var source = new DeepNestingSource 
        { 
            Deep = new Outer<InnerGeneric<InnerGeneric<string>>> 
            { 
                Inner = new InnerGeneric<InnerGeneric<string>> 
                { 
                    Value = new InnerGeneric<string> { Value = "DeepValue" } 
                } 
            } 
        };
        var result = mapper.Map<DeepNestingDto>(source);

        Assert.Equal("DeepValue", result.Deep.Inner.Value.Value);
    }

    /// <summary> Verify that mapping complex tuples inside classes works correctly. </summary>
    [Fact]
    public void Test_ComplexTupleInClass_Exhaustive()
    {
        var mapper = GetMapper();
        var source = new ComplexTupleWrapper { Identity = ("Alice", 30) };
        var result = mapper.Map<ComplexClassDto>(source);

        Assert.Equal("Alice", result.Identity.Name);
        Assert.Equal(30, result.Identity.Age);
    }
}
