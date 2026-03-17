using System.Linq;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public sealed class DiagnosticTests
{
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

    [Fact]
    public void Generator_ReportAM005_WhenConstructorIsMissing()
    {
        var source = @"
using AutoMappic;

public class S { public int Id { get; set; } }
public class D { public D(int id) {} }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<S, D>(); }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var diagnostics = result.Diagnostics;

        var am005 = diagnostics.FirstOrDefault(d => d.Id == "AM005");
        Assert.NotNull(am005);
        Assert.Contains("'D' must have a public parameterless constructor", am005!.GetMessage());
    }

}
