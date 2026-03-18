using System.Linq;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public sealed class DiagnosticTests
{
    /// <summary> Verify that AM001 is reported when a destination property cannot be mapped from the source </summary>
    [Fact]
    public void Generator_ReportAM001_WhenPropertyIsUnmapped()
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

        var am001 = diagnostics.FirstOrDefault(d => d.Id == "AM001");
        Assert.NotNull(am001);
        Assert.Contains("'Name' on 'D'", am001!.GetMessage());
    }

    /// <summary> Ensure AM002 is reported when multiple source paths could resolve to the same destination property </summary>
    [Fact]
    public void Generator_ReportAM002_WhenMappingIsAmbiguous()
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

        var am002 = diagnostics.FirstOrDefault(d => d.Id == "AM002");
        Assert.NotNull(am002);
        Assert.Contains("is ambiguous", am002!.GetMessage());
    }

    /// <summary> Confirm AM005 is reported when the destination type lacks a public parameterless constructor </summary>
    [Fact]
    public void Generator_ReportAM005_WhenConstructorIsMissing()
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

        var am005 = diagnostics.FirstOrDefault(d => d.Id == "AM005");
        Assert.NotNull(am005);
        Assert.Contains("must have a public parameterless constructor or one whose parameters can be satisfied", am005!.GetMessage());
    }

}
