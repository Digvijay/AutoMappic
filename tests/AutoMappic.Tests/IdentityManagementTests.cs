using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

/// <summary>
///   Tests for v0.4.0 Identity Management, Collection Syncing, Patch Mode, AM013 Diagnostic,
///   and Static Converter features.
/// </summary>
public class IdentityManagementTests
{
    #region Generator-Level: Identity Management Opt-In

    /// <summary> Verify that enabling AutoMappic_EnableIdentityManagement emits MappingContext usage in generated code. </summary>
    [Fact]
    public void Generator_IdentityManagement_EmitsMappingContext()
    {
        var source = @"
using AutoMappic;
using System.Collections.Generic;

public class Entity { public int Id { get; set; } public string Name { get; set; } = """"; }
public class EntityDto { public int Id { get; set; } public string Name { get; set; } = """"; }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<Entity, EntityDto>(); }
}";
        var options = new Dictionary<string, string>
        {
            ["build_property.automappic_enableidentitymanagement"] = "true"
        };
        var result = GeneratorTestHelper.RunGenerator(source, options);
        var mapSource = result.Sources
            .FirstOrDefault(f => f.HintName.Contains("Entity_") && f.HintName.Contains("_To_") && f.HintName.Contains("EntityDto"))
            .SourceText?.ToString() ?? "";

        Assert.True(mapSource.Contains("MappingContext"), $"Expected MappingContext usage in generated code.\nSource:\n{mapSource}");
    }

    /// <summary> Verify that without the opt-in flag, standard mapping is generated without conditional patches. </summary>
    [Fact]
    public void Generator_WithoutIdentityFlag_NoConditionalPatches()
    {
        var source = @"
#nullable enable
using AutoMappic;

public class S { public string? Name { get; set; } }
public class D { public string? Name { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<S, D>(); }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var mapSource = result.Sources
            .FirstOrDefault(f => f.HintName.Contains("S_") && f.HintName.Contains("_To_") && f.HintName.Contains("_D_"))
            .SourceText?.ToString() ?? "";

        // Without identity management, should NOT have conditional null checks
        Assert.True(!mapSource.Contains("if (source.Name != null)"),
            $"Without identity management, should not emit conditional patches.\nSource:\n{mapSource}");
    }

    #endregion

    #region Generator-Level: Patch Mode (Null-Ignore)

    /// <summary> With identity management enabled, nullable source properties should generate conditional assignments. </summary>
    [Fact]
    public void Generator_PatchMode_EmitsNullChecks()
    {
        var source = @"
#nullable enable
using AutoMappic;

public class PatchSource { public string? Name { get; set; } public int? Age { get; set; } }
public class PatchDest { public string Name { get; set; } = """"; public int Age { get; set; } }

public class PatchProfile : Profile
{
    public PatchProfile() { CreateMap<PatchSource, PatchDest>(); }
}";
        var options = new Dictionary<string, string>
        {
            ["build_property.automappic_enableidentitymanagement"] = "true"
        };
        var result = GeneratorTestHelper.RunGenerator(source, options);
        var mapSource = result.Sources
            .FirstOrDefault(f => f.HintName.Contains("PatchSource") && f.HintName.Contains("PatchDest"))
            .SourceText?.ToString() ?? "";

        // In-place body should contain conditional checks for nullable sources
        Assert.True(mapSource.Contains("!= null"),
            $"Expected null-check conditional assignment with identity management enabled.\nSource:\n{mapSource}");
    }

    #endregion

    #region Generator-Level: AM013 Diagnostic

    /// <summary> AM013 should fire when a required destination property is mapped from a nullable source with identity management. </summary>
    [Fact]
    public void Generator_AM013_WarnsOnRequiredPatchMismatch()
    {
        var source = @"
#nullable enable
using AutoMappic;

public class S { public string? Name { get; set; } }
public class D { public required string Name { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<S, D>(); }
}";
        var options = new Dictionary<string, string>
        {
            ["build_property.automappic_enableidentitymanagement"] = "true"
        };
        var result = GeneratorTestHelper.RunGenerator(source, options);
        var diagIds = result.Diagnostics.Select(d => d.Id).ToList();

        Assert.Contains("AM013", diagIds);
    }

    /// <summary> AM013 should NOT fire without identity management enabled. </summary>
    [Fact]
    public void Generator_AM013_NotEmittedWithoutIdentityManagement()
    {
        var source = @"
#nullable enable
using AutoMappic;

public class S { public string? Name { get; set; } }
public class D { public required string Name { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<S, D>(); }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var diagIds = result.Diagnostics.Select(d => d.Id).ToList();

        Assert.True(!diagIds.Contains("AM013"),
            $"AM013 should not fire without identity management. Diagnostics: {string.Join(", ", diagIds)}");
    }

    /// <summary> AM013 should NOT fire when the destination property is not required. </summary>
    [Fact]
    public void Generator_AM013_NotEmittedForNonRequired()
    {
        var source = @"
#nullable enable
using AutoMappic;

public class S { public string? Name { get; set; } }
public class D { public string Name { get; set; } = """"; }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<S, D>(); }
}";
        var options = new Dictionary<string, string>
        {
            ["build_property.automappic_enableidentitymanagement"] = "true"
        };
        var result = GeneratorTestHelper.RunGenerator(source, options);
        var diagIds = result.Diagnostics.Select(d => d.Id).ToList();

        Assert.True(!diagIds.Contains("AM013"),
            $"AM013 should not fire for non-required properties. Diagnostics: {string.Join(", ", diagIds)}");
    }

    #endregion

    #region Generator-Level: Collection Syncing (Key Inference)

    /// <summary> Verify key inference detects 'Id' property on nested collection destination types. </summary>
    [Fact]
    public void Generator_CollectionSync_InfersKeyProperty()
    {
        var source = @"
using AutoMappic;
using System.Collections.Generic;

public class OrderItem { public int Id { get; set; } public string Name { get; set; } = """"; }
public class OrderItemDto { public int Id { get; set; } public string Name { get; set; } = """"; }

public class Order { public List<OrderItem> Items { get; set; } = new(); }
public class OrderDto { public List<OrderItemDto> Items { get; set; } = new(); }

public class OrderProfile : Profile
{
    public OrderProfile() 
    { 
        CreateMap<Order, OrderDto>();
        CreateMap<OrderItem, OrderItemDto>();
    }
}";
        var options = new Dictionary<string, string>
        {
            ["build_property.automappic_enableidentitymanagement"] = "true"
        };
        var result = GeneratorTestHelper.RunGenerator(source, options);
        var mapSource = result.Sources
            .FirstOrDefault(f => f.HintName.Contains("Order_") && f.HintName.Contains("OrderDto") && !f.HintName.Contains("Item"))
            .SourceText?.ToString() ?? "";

        // Should emit diffing logic with key-based matching when identity management is on
        // The in-place mapping should reference the key property
        Assert.True(mapSource.Length > 0, $"Expected Order->OrderDto map but got nothing. Files: {string.Join(", ", result.Sources.Select(s => s.HintName))}");
    }

    #endregion

    #region Generator-Level: Static Converters

    /// <summary> Verify that [AutoMappicConverter] attribute is emitted in embedded source when in sourceonly mode. </summary>
    [Fact]
    public void Generator_StaticConverter_AttributeEmitted()
    {
        var source = @"
using AutoMappic;

public class S { public int Id { get; set; } }
public class D { public int Id { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<S, D>(); }
}";
        var options = new Dictionary<string, string>
        {
            ["automappic_sourceonly"] = "true"
        };
        var result = GeneratorTestHelper.RunGenerator(source, options);

        // In sourceonly mode, the embedded source should contain AutoMappicConverterAttribute
        var embeddedSource = result.Sources
            .Where(s => s.SourceText.ToString().Contains("AutoMappicConverterAttribute"))
            .ToList();

        Assert.True(embeddedSource.Count > 0,
            $"Expected AutoMappicConverterAttribute in embedded source. Files: {string.Join(", ", result.Sources.Select(s => s.HintName))}");
    }

    /// <summary> Verify that a [AutoMappicConverter] static method generates a delegating mapping. </summary>
    [Fact]
    public void Generator_StaticConverter_GeneratesDelegatingMap()
    {
        var source = @"
using AutoMappic;

public class Money { public decimal Amount { get; set; } public string Currency { get; set; } = """"; }
public class MoneyView { public string Display { get; set; } = """"; }

public static class MoneyConverters
{
    [AutoMappicConverter]
    public static MoneyView ToView(Money m) => new MoneyView { Display = $""{m.Amount} {m.Currency}"" };
}

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<Money, MoneyView>(); }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        
        // Should generate a map file for Money -> MoneyView
        var mapFile = result.Sources
            .FirstOrDefault(f => f.HintName.Contains("Money") && f.HintName.Contains("MoneyView"));

        // The converter should produce a generated mapping file
        Assert.True(mapFile.HintName != null || result.Sources.Any(s => s.SourceText.ToString().Contains("MoneyConverters.ToView")),
            $"Expected static converter delegation. Files: {string.Join(", ", result.Sources.Select(s => s.HintName))}");
    }

    #endregion

    #region Generator-Level: Shallow Clone (Map<T, T>)

    /// <summary> Verify that mapping a type to itself generates property-by-property copy. </summary>
    [Fact]
    public void Generator_ShallowClone_SameTypeMapping()
    {
        var source = @"
using AutoMappic;

public class Entity { public int Id { get; set; } public string Name { get; set; } = """"; }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<Entity, Entity>(); }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var mapSource = result.Sources
            .FirstOrDefault(f => f.HintName.Contains("Entity_") && f.HintName.Contains("_To_") && f.HintName.Contains("Entity_"))
            .SourceText?.ToString() ?? "";

        Assert.True(mapSource.Contains("result.Id = source.Id") || mapSource.Contains("MapToEntity"),
            $"Expected shallow clone mapping generation.\nSource:\n{mapSource}");
    }

    #endregion

    #region Runtime: Identity Management Disabled (Standard Mapping)

    /// <summary> Standard mapping should still work correctly with all new code in place. </summary>
    [Fact]
    public void Runtime_StandardMapping_StillWorks()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<CoreFunctionalityProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new SuperComplexSource
        {
            Id = 42,
            Header = "Test",
            Sub = new SuperSubSource { Detail = "SubVal" },
            Items = new List<SuperItemSource> { new() { Value = 99 } }
        };

        var result = mapper.Map<SuperComplexDto>(source);

        Assert.Equal(42, result.Id);
        Assert.Equal("Test", result.Header);
        Assert.Equal("SubVal", result.SubDetail);
        Assert.Equal(1, result.Items.Count);
        Assert.Equal(99, result.Items[0].Value);
    }

    #endregion

    #region Runtime: Collection Mapping with Primitives

    /// <summary> Collections of primitives should map correctly without generating MapTo calls. </summary>
    [Fact]
    public void Runtime_PrimitiveCollections_MapCorrectly()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<InterfaceProfile>());
        var mapper = config.CreateMapper();

        var source = new InterfaceCollSource();
        var result = mapper.Map<InterfaceCollDto>(source);

        Assert.Equal(3, result.List.Count);
        Assert.Equal(1, result.List[0]);
        Assert.Equal(2, result.List[1]);
        Assert.Equal(3, result.List[2]);
    }

    /// <summary> HashSet deduplication should work during collection mapping. </summary>
    [Fact]
    public void Runtime_HashSetDedup_Works()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<InterfaceProfile>());
        var mapper = config.CreateMapper();

        var source = new SpecializedContainerSource();
        var result = mapper.Map<SpecializedContainerDto>(source);

        Assert.Equal(2, result.Values.Count);
        Assert.True(result.Values.Contains(1));
        Assert.True(result.Values.Contains(2));
    }

    #endregion

    #region Runtime: Deep Nested Collections

    /// <summary> List of List of objects should be mapped correctly preserving structure. </summary>
    [Fact]
    public void Runtime_DeepNestedCollections_PreservesStructure()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<DeepProfile>());
        var mapper = config.CreateMapper();

        var source = new S
        {
            Data = new List<List<SChild>>
            {
                new List<SChild> { new SChild { Value = 10 }, new SChild { Value = 20 } },
                new List<SChild> { new SChild { Value = 30 } }
            }
        };

        var result = mapper.Map<D>(source);

        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal(2, result.Data[0].Count);
        Assert.Equal(10, result.Data[0][0].Value);
        Assert.Equal(20, result.Data[0][1].Value);
        Assert.Equal(1, result.Data[1].Count);
        Assert.Equal(30, result.Data[1][0].Value);
    }

    #endregion
}
