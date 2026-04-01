using System.Linq;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public sealed class DiagnosticTests
{
    /// <summary> Verify that AM0001 is reported when a destination property cannot be mapped from the source </summary>
    [Fact]
    public void Generator_ReportAM0001_WhenPropertyIsUnmapped()
    {
        var source = @"
using AutoMappic;

public class S { public int Id { get; set; } }
public class D { public int Id { get; set; } public string Name { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<S, D>(); }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var diagnostics = result.Diagnostics;

        var am001 = diagnostics.FirstOrDefault(d => d.Id == "AM0001");
        Assert.NotNull(am001);
        Assert.Contains("'Name' on 'D'", am001!.GetMessage());
    }

    /// <summary> Ensure AM0002 is reported when multiple source paths could resolve to the same destination property </summary>
    [Fact]
    public void Generator_ReportAM0002_WhenMappingIsAmbiguous()
    {
        var source = @"
using AutoMappic;

public class Info { public string Name { get; set; } }
public class S { public Info Info { get; set; } public string InfoName { get; set; } }
public class D { public string InfoName { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<S, D>(); }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var diagnostics = result.Diagnostics;

        var am002 = diagnostics.FirstOrDefault(d => d.Id == "AM0002");
        Assert.NotNull(am002);
        Assert.Contains("is ambiguous", am002!.GetMessage());
    }

    /// <summary> Confirm AM0005 is reported when the destination type lacks a public parameterless constructor </summary>
    [Fact]
    public void Generator_ReportAM0005_WhenConstructorIsMissing()
    {
        var source = @"
using AutoMappic;
using System;

public class S { public int Id { get; set; } }
public class D { public D(Guid unreachable) {} }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<S, D>(); }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var diagnostics = result.Diagnostics;

        var am005 = diagnostics.FirstOrDefault(d => d.Id == "AM0005");
        Assert.NotNull(am005);
        Assert.Contains("must have a public parameterless constructor", am005!.GetMessage());
    }

    /// <summary> Ensure AM0012 is reported when a mapping results in 0 writable properties </summary>
    [Fact]
    public void Generator_ReportAM0012_WhenNoPropertiesMapped()
    {
        var source = @"
using AutoMappic;

public class S { public int Id { get; set; } }
public class EmptyD { public string? Note { get; private set; } }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<S, EmptyD>(); }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var diagnostics = result.Diagnostics;

        var am012 = diagnostics.FirstOrDefault(d => d.Id == "AM0012");
        Assert.NotNull(am012);
        Assert.Contains("has no writable destination properties", am012!.GetMessage());
    }

    /// <summary> Verify AM0014 is reported for Entity syncs missing source Identity </summary>
    [Fact]
    public void Generator_ReportAM0014_WhenSourcePrimaryKeyIsMissing()
    {
        var source = @"
using AutoMappic;
using System.Collections.Generic;

public class SItem { public string Name { get; set; } }
public class DItem { public int Id { get; set; } public string Name { get; set; } }

public class S { public IList<SItem> Items { get; set; } }
public class D { public IList<DItem> Items { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<S, D>(); }
}";
        var result = GeneratorTestHelper.RunGenerator(source, options: new Dictionary<string, string> { { "build_property.automappic_enableidentitymanagement", "true" } });
        var diagnostics = result.Diagnostics;

        var am014 = diagnostics.FirstOrDefault(d => d.Id == "AM0014");
        Assert.NotNull(am014);
        Assert.Contains("lacks a mapped primary key", am014!.GetMessage());
    }

    /// <summary> Verify AM0015 Smart-Match is reported for closely matching names </summary>
    [Fact]
    public void Generator_ReportAM0015_WhenPropertyCloselyMatches()
    {
        var source = @"
using AutoMappic;

public class S { public string FullName { get; set; } }
public class D { public string Name { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<S, D>(); }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var diagnostics = result.Diagnostics;

        var am015 = diagnostics.FirstOrDefault(d => d.Id == "AM0015");
        Assert.NotNull(am015);
        Assert.Contains("Did you mean to map it from 'FullName'?", am015!.GetMessage());
    }

    /// <summary> Verify AM0016 translates to Performance regression on Collection Custom Resolvers </summary>
    [Fact]
    public void Generator_ReportAM0016_WhenCustomResolverUsedInCollection()
    {
        var source = @"
using AutoMappic;
using System.Collections.Generic;
using System.Linq;

public class S { public List<int> Items { get; set; } }
public class D { public List<int> Items { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() 
    { 
        CreateMap<S, D>()
            .ForMember(d => d.Items, opt => opt.MapFrom(s => s.Items.Select(x => x * 2).ToList()));
    }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var diagnostics = result.Diagnostics;

        var am016 = diagnostics.FirstOrDefault(d => d.Id == "AM0016");
        Assert.NotNull(am016);
        Assert.Contains("preventing compiler loop vectorization", am016!.GetMessage());
    }

    /// <summary> Verify AM0017 reports Ambiguous Keys for Entities </summary>
    [Fact]
    public void Generator_ReportAM0017_WhenEntityHasAmbiguousId()
    {
        var source = @"
using AutoMappic;
using System.Collections.Generic;

public class SItem { public int MainId { get; set; } public string Name { get; set; } }
public class DItem { public int PrimaryId { get; set; } public int SecondaryId { get; set; } public string Name { get; set; } }

public class S { public IList<SItem> Items { get; set; } }
public class D { public IList<DItem> Items { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<S, D>(); }
}";
        var result = GeneratorTestHelper.RunGenerator(source, options: new Dictionary<string, string> { { "build_property.automappic_enableidentitymanagement", "true" } });
        var diagnostics = result.Diagnostics;

        var am017 = diagnostics.FirstOrDefault(d => d.Id == "AM0017");
        Assert.NotNull(am017);
        Assert.Contains("has no identifiable primary key", am017!.GetMessage());
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Negative Tests (diagnostic should NOT fire)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary> AM0014 should NOT fire when source has a matching Id property </summary>
    [Fact]
    public void Generator_DoNotReportAM0014_WhenSourceHasMatchingKey()
    {
        var source = @"
using AutoMappic;
using System.Collections.Generic;

public class SItem { public int Id { get; set; } public string Name { get; set; } }
public class DItem { public int Id { get; set; } public string Name { get; set; } }

public class S { public IList<SItem> Items { get; set; } }
public class D { public IList<DItem> Items { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<S, D>(); }
}";
        var result = GeneratorTestHelper.RunGenerator(source, options: new Dictionary<string, string> { { "build_property.automappic_enableidentitymanagement", "true" } });
        var diagnostics = result.Diagnostics;

        var am014 = diagnostics.FirstOrDefault(d => d.Id == "AM0014");
        Assert.Null(am014);
    }

    /// <summary> AM0015 should NOT fire when names are completely unrelated </summary>
    [Fact]
    public void Generator_DoNotReportAM0015_WhenNamesAreUnrelated()
    {
        var source = @"
using AutoMappic;

public class S { public string Apple { get; set; } }
public class D { public string Zebra { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<S, D>(); }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var diagnostics = result.Diagnostics;

        var am015 = diagnostics.FirstOrDefault(d => d.Id == "AM0015");
        Assert.Null(am015);
    }

    /// <summary> AM0015 should NOT fire when properties match exactly (no fuzzy needed) </summary>
    [Fact]
    public void Generator_DoNotReportAM0015_WhenNamesMatchExactly()
    {
        var source = @"
using AutoMappic;

public class S { public string Name { get; set; } }
public class D { public string Name { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<S, D>(); }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var diagnostics = result.Diagnostics;

        var am015 = diagnostics.FirstOrDefault(d => d.Id == "AM0015");
        Assert.Null(am015);
    }

    /// <summary> AM0015 with high threshold should filter out moderate matches </summary>
    [Fact]
    public void Generator_DoNotReportAM0015_WhenScoreBelowConfiguredThreshold()
    {
        var source = @"
using AutoMappic;

public class S { public string FullName { get; set; } }
public class D { public string Name { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<S, D>(); }
}";
        // FullName vs Name = 0.5, so a threshold of 0.75 should suppress it
        var result = GeneratorTestHelper.RunGenerator(source, options: new Dictionary<string, string> { { "build_property.automappic_smartmatchthreshold", "0.75" } });
        var diagnostics = result.Diagnostics;

        var am015 = diagnostics.FirstOrDefault(d => d.Id == "AM0015");
        Assert.Null(am015);
    }

    /// <summary> AM0015 with low threshold should emit even moderate matches </summary>
    [Fact]
    public void Generator_ReportAM0015_WhenScoreAboveConfiguredThreshold()
    {
        var source = @"
using AutoMappic;

public class S { public string FullName { get; set; } }
public class D { public string Name { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<S, D>(); }
}";
        // FullName vs Name = 0.5, so a threshold of 0.4 should include it
        var result = GeneratorTestHelper.RunGenerator(source, options: new Dictionary<string, string> { { "build_property.automappic_smartmatchthreshold", "0.4" } });
        var diagnostics = result.Diagnostics;

        var am015 = diagnostics.FirstOrDefault(d => d.Id == "AM0015");
        Assert.NotNull(am015);
    }

    /// <summary> AM0016 should NOT fire for non-collection explicit mapping with Select </summary>
    [Fact]
    public void Generator_DoNotReportAM0016_WhenSelectUsedOnNonCollection()
    {
        var source = @"
using AutoMappic;

public class S { public string Raw { get; set; } }
public class D { public string Formatted { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() 
    { 
        CreateMap<S, D>()
            .ForMember(d => d.Formatted, opt => opt.MapFrom(s => s.Raw.ToUpper()));
    }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var diagnostics = result.Diagnostics;

        var am016 = diagnostics.FirstOrDefault(d => d.Id == "AM0016");
        Assert.Null(am016);
    }

    /// <summary> AM0017 should NOT fire when entity has a clear single 'Id' property </summary>
    [Fact]
    public void Generator_DoNotReportAM0017_WhenEntityHasClearId()
    {
        var source = @"
using AutoMappic;
using System.Collections.Generic;

public class SItem { public int Id { get; set; } public string Name { get; set; } }
public class DItem { public int Id { get; set; } public string Name { get; set; } }

public class S { public IList<SItem> Items { get; set; } }
public class D { public IList<DItem> Items { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<S, D>(); }
}";
        var result = GeneratorTestHelper.RunGenerator(source, options: new Dictionary<string, string> { { "build_property.automappic_enableidentitymanagement", "true" } });
        var diagnostics = result.Diagnostics;

        var am017 = diagnostics.FirstOrDefault(d => d.Id == "AM0017");
        Assert.Null(am017);
    }

    // ═══════════════════════════════════════════════════════════════════════
    //  Edge Cases
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary> AM0015 diagnostic includes the Score in its properties for filtering </summary>
    [Fact]
    public void Generator_AM0015_ContainsScoreInProperties()
    {
        var source = @"
using AutoMappic;

public class S { public string FullName { get; set; } }
public class D { public string Name { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<S, D>(); }
}";
        // Use a very low threshold so the diagnostic passes through
        var result = GeneratorTestHelper.RunGenerator(source, options: new Dictionary<string, string> { { "build_property.automappic_smartmatchthreshold", "0.1" } });
        var diagnostics = result.Diagnostics;

        var am015 = diagnostics.FirstOrDefault(d => d.Id == "AM0015");
        Assert.NotNull(am015);
        Assert.True(am015!.Properties.ContainsKey("Score"));
        Assert.True(am015.Properties.ContainsKey("SuggestedName"));
        Assert.Equal("FullName", am015.Properties["SuggestedName"]);
    }

    /// <summary> AM0014 message includes both type names for clarity </summary>
    [Fact]
    public void Generator_AM0014_MessageIncludesTypeNames()
    {
        var source = @"
using AutoMappic;
using System.Collections.Generic;

public class OrderLine { public string ProductName { get; set; } }
public class OrderLineDto { public int Id { get; set; } public string ProductName { get; set; } }

public class Order { public IList<OrderLine> Lines { get; set; } }
public class OrderDto { public IList<OrderLineDto> Lines { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<Order, OrderDto>(); }
}";
        var result = GeneratorTestHelper.RunGenerator(source, options: new Dictionary<string, string> { { "build_property.automappic_enableidentitymanagement", "true" } });
        var diagnostics = result.Diagnostics;

        var am014 = diagnostics.FirstOrDefault(d => d.Id == "AM0014");
        Assert.NotNull(am014);
        Assert.Contains("OrderLineDto", am014!.GetMessage());
        Assert.Contains("OrderLine", am014.GetMessage());
    }

    /// <summary> AM0018 (0.6.0 Roadmap): Enforce partial modifier for [AutoMap] classes </summary>
    [Fact]
    public void Generator_ReportAM0018_WhenAutoMapClassIsNonPartial()
    {
        var source = @"
using AutoMappic;

public class S { public int Id { get; set; } }

[AutoMap(typeof(S))]
public class NonPartialD { public int Id { get; set; } }
";
        var result = GeneratorTestHelper.RunGenerator(source);
        var am018 = result.Diagnostics.FirstOrDefault(d => d.Id == "AM0018");
        
        Assert.NotNull(am018);
        Assert.Contains("must be partial", am018!.GetMessage());
    }

    /// <summary> 0.6.0 Hardening: Verify that diagnostics in Profiles are anchored to the CreateMap call with a real span. </summary>
    [Fact]
    public void Generator_AnchorsProfileDiagnosticsToCreateMapCall()
    {
        var source = @"
using AutoMappic;

public class S { public int Id { get; set; } }
public class D { public int Id { get; set; } public string? Name { get; set; } }

public class MyProfile : Profile
{
    public MyProfile()
    {
        CreateMap<S, D>();
    }
}
";
        var result = GeneratorTestHelper.RunGenerator(source);
        var am001 = result.Diagnostics.FirstOrDefault(d => d.Id == "AM0001");
        
        Assert.NotNull(am001);
        
        // Verify span length is non-zero
        var span = am001!.Location.SourceSpan;
        Assert.True(span.Length > 0, "Diagnostic span should be non-zero length.");
        
        // Verify it's on the CreateMap line (Line 11 in this source)
        var lineSpan = am001.Location.GetLineSpan();
        Assert.Equal(10, lineSpan.StartLinePosition.Line); // 0-indexed
        
        // Extra: Verify the text at that location starts with 'CreateMap'
        var tree = am001.Location.SourceTree!;
        var text = tree.GetText().ToString(span);
        Assert.Contains("CreateMap", text);
    }
}
