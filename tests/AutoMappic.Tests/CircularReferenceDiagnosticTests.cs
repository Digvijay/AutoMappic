using System.Collections.Immutable;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public sealed class CircularReferenceDiagnosticTests
{
    /// <summary> Verify that AM0006 is reported when a self-referencing model is detected </summary>
    [Fact]
    public void Generator_ReportAM0006_WhenSelfReferencingModelIsDetected()
    {
        string source = @"
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
        GeneratorTestHelper.GeneratorResult result = GeneratorTestHelper.RunGenerator(source);
        ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> diagnostics = result.Diagnostics;

        Microsoft.CodeAnalysis.Diagnostic? am006 = diagnostics.FirstOrDefault(d => d.Id == "AM0006");
        Assert.NotNull(am006);
        Assert.Contains("Circular reference detected", am006!.GetMessage());

        // V0.6.0 ANCHOR CHECK: Verify it's on property 'Manager' in 'EmployeeDto'
        // In the 'source' string below, 'Manager' is on the 12th line (0-indexed).
        int line = am006.Location.GetLineSpan().StartLinePosition.Line;
        Assert.True(line is >= 11 and <= 13, $"Expected line near 12, but got {line}");
    }

    /// <summary> Verify that AM0006 is reported for indirect circular references (A -> B -> A) </summary>
    [Fact]
    public void Generator_ReportAM0006_WhenIndirectCircularReferenceIsDetected()
    {
        string source = @"
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
        GeneratorTestHelper.GeneratorResult result = GeneratorTestHelper.RunGenerator(source);
        ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> diagnostics = result.Diagnostics;

        Microsoft.CodeAnalysis.Diagnostic? am006 = diagnostics.FirstOrDefault(d => d.Id == "AM0006");
        Assert.NotNull(am006);
        Assert.Contains("Circular reference detected", am006!.GetMessage());

        // V0.6.0 ANCHOR CHECK: Verify it's on property B in ADto (Line 6 or 7 relative)
        int line = am006.Location.GetLineSpan().StartLinePosition.Line;
        Assert.True(line is >= 5 and <= 8, $"Expected line near 6, but got {line}");
    }
}
