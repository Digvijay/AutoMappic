using System.Linq;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public sealed class CircularReferenceDiagnosticTests
{
    /// <summary> Verify that AM0006 is reported when a self-referencing model is detected </summary>
    [Fact]
    public void Generator_ReportAM0006_WhenSelfReferencingModelIsDetected()
    {
        var source = @"
using AutoMappic;

public class Employee 
{ 
    public string Name { get; set; } 
    public Employee Manager { get; set; } 
}

public class EmployeeDto 
{ 
    public string Name { get; set; } 
    public EmployeeDto Manager { get; set; } 
}

public class MyProfile : Profile
{
    public MyProfile() 
    { 
        CreateMap<Employee, EmployeeDto>(); 
    }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var diagnostics = result.Diagnostics;

        var am006 = diagnostics.FirstOrDefault(d => d.Id == "AM0006");
        Assert.NotNull(am006);
        Assert.Contains("Circular reference detected", am006!.GetMessage());
    }

    /// <summary> Verify that AM0006 is reported for indirect circular references (A -> B -> A) </summary>
    [Fact]
    public void Generator_ReportAM0006_WhenIndirectCircularReferenceIsDetected()
    {
        var source = @"
using AutoMappic;

public class A { public B B { get; set; } }
public class B { public A A { get; set; } }

public class ADto { public BDto B { get; set; } }
public class BDto { public ADto A { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() 
    { 
        CreateMap<A, ADto>(); 
        CreateMap<B, BDto>();
    }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var diagnostics = result.Diagnostics;

        var am006 = diagnostics.FirstOrDefault(d => d.Id == "AM0006");
        Assert.NotNull(am006);
        Assert.Contains("Circular reference detected", am006!.GetMessage());
    }
}
