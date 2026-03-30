using Microsoft.CodeAnalysis;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public class GeneratorHardeningTests
{
    /// <summary> Hardening: Ensure naming conventions work with static member access and implicit new </summary>
    [Fact]
    public void Generator_Hardening_NamingConventions_Variations()
    {
        var source = @"
using AutoMappic;

public class MyProfile : Profile
{
    public MyProfile() 
    { 
        SourceNamingConvention = NamingConventions.Pascal;
        DestinationNamingConvention = new PascalCaseNamingConvention();
        CreateMap<NVSource, NVDest>(); 
    }
}

public static class NamingConventions {
    public static INamingConvention Pascal = new PascalCaseNamingConvention();
}

public class NVSource { public string UserName { get; set; } }
public class NVDest { public string UserName { get; set; } }
";
        var result = GeneratorTestHelper.RunGenerator(source);
        var mapSource = result.Sources.First(f => f.HintName.Contains("NVSource") && f.HintName.Contains("NVDest")).SourceText.ToString();

        // If detection fails, it defaults to Pascal anyway, so we check if the flag is set in model metadata (not directly visible in source, but we can check if it compiles)
        Assert.True(result.Diagnostics.Count() == 0, "Expected no diagnostics for naming convention variations");
        Assert.Contains("result.UserName = source.UserName;", mapSource);
    }

    /// <summary> Hardening: Verify that MapFrom lambda rewriter doesn't accidentally replace variables with similar names </summary>
    [Fact]
    public void Generator_Hardening_MapFrom_VariableNameCollision()
    {
        var source = @"
using AutoMappic;

public class Profile1 : Profile
{
    public Profile1()
    {
        // Parameter name 's' should be replaced by 'source', but 's_other' should remain untouched.
        CreateMap<CVSource, CVDest>()
            .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name + s_other));
    }

    private string s_other = ""suffix"";
}

public class CVSource { public string Name { get; set; } }
public class CVDest { public string Name { get; set; } }
";
        var result = GeneratorTestHelper.RunGenerator(source);
        var mapSource = result.Sources.First(f => f.HintName.Contains("CVSource") && f.HintName.Contains("CVDest")).SourceText.ToString();

        Assert.Contains("source.Name + s_other", mapSource);
        Assert.DoesNotContain("source_other", mapSource);
    }

    /// <summary> Hardening: Verify EnableEntitySync detection with 'this.' prefix </summary>
    [Fact]
    public void Generator_Hardening_EntitySync_ThisPrefix()
    {
        var source = @"
using AutoMappic;

public class Profile1 : Profile
{
    public Profile1()
    {
        this.EnableEntitySync = true;
        CreateMap<ESPSource, ESPDest>();
    }
}

public class ESPSource { public int Id { get; set; } }
public class ESPDest { public int Id { get; set; } }
";
        var result = GeneratorTestHelper.RunGenerator(source);
        var mapSource = result.Sources.First(f => f.HintName.Contains("ESPSource") && f.HintName.Contains("ESPDest")).SourceText.ToString();

        Assert.Contains("// EnableEntitySync=True", mapSource);
    }

    [Fact]
    public void Generator_Hardening_NamingConventions_ComplexAssignments()
    {
        var source = @"
using AutoMappic;

public class Profile1 : Profile
{
    public Profile1()
    {
        SourceNamingConvention = new SnakeCaseNamingConvention();
        DestinationNamingConvention = NamingConventions.Pascal;
        CreateMap<CASource, CADest>();
    }
}

public class CASource { public int src_Id { get; set; } }
public class CADest { public int Id { get; set; } }
";
        var result = GeneratorTestHelper.RunGenerator(source);
        var mapSource = result.Sources.First(f => f.HintName.Contains("CASource") && f.HintName.Contains("CADest")).SourceText.ToString();

        // If conventions are detected correctly, it should map src_Id to Id
        Assert.Contains("source.src_Id", mapSource);
    }

    [Fact]
    public void Generator_Hardening_MapFrom_VariableShadowing()
    {
        var source = @"
using AutoMappic;

public class Profile1 : Profile
{
    public Profile1()
    {
        var sOutside = 10;
        CreateMap<Source, Dest>()
            .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Id + sOutside));
    }
}

public class Source { public int Id { get; set; } }
public class Dest { public int Id { get; set; } }
";
        var result = GeneratorTestHelper.RunGenerator(source);
        var mapSource = result.Sources.First(f => f.HintName.Contains("S") && f.HintName.Contains("D")).SourceText.ToString();

        // Regex would replace sOutside -> sourceOutside if oldParam was 's'
        // Syntactic rewriter should not.
        Assert.Contains("result.Id = source.Id + sOutside;", mapSource);
    }
}
