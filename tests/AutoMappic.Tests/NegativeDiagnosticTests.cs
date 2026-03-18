using System.Linq;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public sealed class NegativeDiagnosticTests
{
    /// <summary> Verify that AM001 is reported when a property is missing and not ignored </summary>
    [Fact]
    public void Generator_ReportAM001_WhenPropertyIsMissingInSource()
    {
        var source = @"
using AutoMappic;

public class S { public int Id { get; set; } }
public class D { public int Id { get; set; } public string Name { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() 
    { 
        CreateMap<S, D>(); 
    }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var am001 = result.Diagnostics.FirstOrDefault(d => d.Id == "AM001");
        Assert.NotNull(am001);
    }

    /// <summary> Verify that AM005 is reported when the destination class lacks a parameterless constructor </summary>
    [Fact]
    public void Generator_ReportAM005_WhenDestConstructorIsInvalid()
    {
        var source = @"
using AutoMappic;

public class S { public int Id { get; set; } }
public class D { public D(int id) {} }

public class MyProfile : Profile
{
    public MyProfile() 
    { 
        CreateMap<S, D>(); 
    }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var am005 = result.Diagnostics.FirstOrDefault(d => d.Id == "AM005");
        Assert.NotNull(am005);
    }

    /// <summary> Verify that AM003 is reported when CreateMap is called outside a Profile constructor (as a warning) </summary>
    [Fact]
    public void Generator_ReportAM003_WhenCreateMapIsOutsideProfile()
    {
        var source = @"
using AutoMappic;

public class MyService
{
    public void DoSomething()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MyProfile>());
    }
}

public class MyProfile : Profile
{
    public MyProfile() { }
    
    public void NotInConstructor()
    {
        CreateMap<S, D>();
    }
}
public class S { }
public class D { }
";
        var result = GeneratorTestHelper.RunGenerator(source);
        var am003 = result.Diagnostics.FirstOrDefault(d => d.Id == "AM003");
        Assert.NotNull(am003);
    }
}
