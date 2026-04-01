using System.Linq;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public sealed class NegativeDiagnosticTests
{
    /// <summary> Verify that AM0001 is reported when a property is missing and not ignored </summary>
    [Fact]
    public void Generator_ReportAM0001_WhenPropertyIsMissingInSource()
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
        var am001 = result.Diagnostics.FirstOrDefault(d => d.Id == "AM0001");
        Assert.NotNull(am001);
    }

    /// <summary> Verify that AM0005 is reported when the destination class lacks a parameterless constructor </summary>
    [Fact]
    public void Generator_ReportAM0005_WhenDestConstructorIsInvalid()
    {
        var source = @"
using AutoMappic;
using System;

public class S { public int Id { get; set; } }
public class D { public D(Guid unreachable) {} }

public class MyProfile : Profile
{
    public MyProfile() 
    { 
        CreateMap<S, D>(); 
    }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var am005 = result.Diagnostics.FirstOrDefault(d => d.Id == "AM0005");
        Assert.NotNull(am005);
    }

    /// <summary> Verify that AM0003 is reported when CreateMap is called outside a Profile constructor (as a warning) </summary>
    [Fact]
    public void Generator_ReportAM0003_WhenCreateMapIsOutsideProfile()
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
        var am003 = result.Diagnostics.FirstOrDefault(d => d.Id == "AM0003");
        Assert.NotNull(am003);
    }

    /// <summary> Verify that AM0010 is reported when nested collection mapping is detected </summary>
    [Fact]
    public void Generator_ReportAM0010_WhenNestedCollectionIsMapped()
    {
        var source = @"
using AutoMappic;
using System.Collections.Generic;

public class S { public List<List<int>> Values { get; set; } }
public class D { public List<List<int>> Values { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<S, D>(); }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var am010 = result.Diagnostics.FirstOrDefault(d => d.Id == "AM0010");
        Assert.NotNull(am010);
    }

    /// <summary> Verify that AM0013 is reported when 'required' property is mapped from nullable in patch mode </summary>
    [Fact]
    public void Generator_ReportAM0013_WhenRequiredPropertyIsMappedFromNullable()
    {
        var source = @"
using AutoMappic;

public class S { public string? Name { get; set; } }
public class D { public required string Name { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() 
    { 
        EnableIdentityManagement = true;
        CreateMap<S, D>(); 
    }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var am013 = result.Diagnostics.FirstOrDefault(d => d.Id == "AM0013");
        Assert.NotNull(am013);
    }

    /// <summary> Verify that AM0011 is reported for multi-source ProjectTo </summary>
    [Fact]
    public void Generator_ReportAM0011_WhenMultiSourceProjectToIsUsed()
    {
        var source = @"
using AutoMappic;
using System.Linq;

public class S1 { }
public class S2 { }
public class D { }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<(S1, S2), D>(); }
}

public class Usage
{
    public void Run(IQueryable<(S1, S2)> query)
    {
        query.ProjectTo<D>();
    }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var am011 = result.Diagnostics.FirstOrDefault(d => d.Id == "AM0011");
        Assert.NotNull(am011);
    }

    /// <summary> Verify that AM0004 is reported for unresolved interceptor calls </summary>
    [Fact]
    public void Generator_ReportAM0004_WhenUnresolvedMapCallExists()
    {
        var source = @"
using AutoMappic;

public class S { }
public class D { }

public class Usage
{
    public void Run(IMapper mapper, S s)
    {
        mapper.Map<D>(s); // No CreateMap<S, D> in any profile
    }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var am004 = result.Diagnostics.FirstOrDefault(d => d.Id == "AM0004");
        Assert.NotNull(am004);
    }
}
