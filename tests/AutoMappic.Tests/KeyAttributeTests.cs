using System.Linq;
using Microsoft.CodeAnalysis;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public class KeyAttributeTests
{
    /// <summary> Identity Hardening: Verify [Key] attribute correctly identifies the primary key even if not named 'Id' </summary>
    [Fact]
    public void Generator_Key_Attribute_Detection()
    {
        var source = @"
using AutoMappic;
using System.ComponentModel.DataAnnotations;

[AutoMappicProfile]
public class MyProfile : Profile
{
    public MyProfile() 
    { 
        EnableEntitySync = true;
        EnableIdentityManagement = true;
        CreateMap<KASource, KADest>(); 
    }
}

public class KASource { public int SSN { get; set; } public string Name { get; set; } }
public class KADest { [Key] public int SSN { get; set; } public string Name { get; set; } }
";
        var result = GeneratorTestHelper.RunGenerator(source);
        var mapSource = result.Sources.First(f => f.HintName.Contains("KASource") && f.HintName.Contains("KADest")).SourceText.ToString();

        // Should use SSN for identity management
        Assert.Contains("var __keyVal = (object?)source.SSN;", mapSource);
        Assert.Contains("context.TryGetEntity<global::KADest>(__keyVal, out var existing)", mapSource);
        Assert.Contains("return existing;", mapSource);
    }

    /// <summary> Identity Hardening: Verify [AutoMappicKey] attribute works same as [Key] </summary>
    [Fact]
    public void Generator_AutoMappicKey_Attribute_Detection()
    {
        var source = @"
using AutoMappic;

[AutoMappicProfile]
public class MyProfile : Profile
{
    public MyProfile() 
    { 
        EnableEntitySync = true;
        EnableIdentityManagement = true;
        CreateMap<AMKSource, AMKDest>(); 
    }
}

public class AMKSource { public string Code { get; set; } }
public class AMKDest { [AutoMappicKey] public string Code { get; set; } }
";
        var result = GeneratorTestHelper.RunGenerator(source);
        var mapSource = result.Sources.First(f => f.HintName.Contains("AMKSource") && f.HintName.Contains("AMKDest")).SourceText.ToString();

        Assert.Contains("var __keyVal = (object?)source.Code;", mapSource);
    }

    /// <summary> Identity Hardening: Verify Smart-Sync collection mapping uses [Key] attribute </summary>
    [Fact]
    public void Generator_SmartSync_Collection_KeyAttribute()
    {
        var source = @"
using AutoMappic;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

[AutoMappicProfile]
public class MyProfile : Profile
{
    public MyProfile() 
    { 
        EnableEntitySync = true;
        CreateMap<SSSource, SSDest>();
        CreateMap<SSItem, SSItemDto>();
    }
}

public class SSSource { public List<SSItem> Items { get; set; } }
public class SSDest { public List<SSItemDto> Items { get; set; } }

public class SSItem { public int InternalCode { get; set; } public string Value { get; set; } }
public class SSItemDto { [Key] public int InternalCode { get; set; } public string Value { get; set; } }
";
        var result = GeneratorTestHelper.RunGenerator(source);

        // Check for AM0014 (Unmapped Primary Key) - should NOT be present because InternalCode is marked [Key]
        var diagnostic = result.Diagnostics.FirstOrDefault(d => d.Id == "AM0014");
        Assert.True(diagnostic == null, "Generated AM0014 even though [Key] was present on InternalCode");

        var mapSource = result.Sources.First(f => f.SourceText.ToString().Contains("existingMap")).SourceText.ToString();
        Assert.Contains("existingMap.TryGetValue(__sKey, out var existingItem)", mapSource);
        Assert.Contains("sItem.InternalCode", mapSource);
    }
}
