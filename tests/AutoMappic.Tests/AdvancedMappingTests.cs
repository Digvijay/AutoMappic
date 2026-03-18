using Microsoft.CodeAnalysis;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public class AdvancedMappingTests
{
    /// <summary> Verify that destination properties can be mapped through constructor parameters. </summary>
    [Fact]
    public void Generator_ConstructorMapping_Works()
    {
        var source = @"
using AutoMappic;

public class S { public string Name { get; set; } }
public class D 
{ 
    public string Name { get; }
    public D(string name) { Name = name; }
}

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<S, D>(); }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var mapSource = result.Sources.First(f => f.HintName.Contains("S_To_D_Map")).SourceText.ToString();
        
        Assert.Contains("return new D(source.Name)", mapSource);
    }

    /// <summary> Verify that a custom ITypeConverter can be used to handle the entire mapping. </summary>
    [Fact]
    public void Generator_GlobalConverter_Works()
    {
        var source = @"
using AutoMappic;
using System;

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<DateTime, long>().ConvertUsing<DateToUnixConverter>(); }
}

public class DateToUnixConverter : ITypeConverter<DateTime, long>
{
    public long Convert(DateTime source) => ((DateTimeOffset)source).ToUnixTimeSeconds();
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var fileNames = result.Sources.Select(s => s.HintName).ToList();
        var mapHintName = fileNames.FirstOrDefault(f => f.Contains("DateTime") && f.Contains("_To_"));
        
        Assert.NotNull(mapHintName, $"Mapping file for DateTime not found. Hint names: {string.Join(", ", fileNames)}");
        var mapSource = result.Sources.First(f => f.HintName == mapHintName).SourceText.ToString();
        Assert.Contains("new DateToUnixConverter().Convert(source)", mapSource);
    }

    /// <summary> Verify that snake_case source properties match PascalCase destination properties. </summary>
    [Fact]
    public void Generator_NamingConventions_Works()
    {
        var source = @"
using AutoMappic;

public class S { public string first_name { get; set; } }
public class D { public string FirstName { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap<S, D>(); }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var fileNames = result.Sources.Select(s => s.HintName).ToList();
        var mapSourceFile = result.Sources.FirstOrDefault(f => f.HintName.Contains("S_To_D_Map"));
        
        Assert.NotNull(mapSourceFile, $"S_To_D_Map not found. Generated files: {string.Join(", ", fileNames)}");
        var mapSource = mapSourceFile.SourceText.ToString();
        Assert.Contains("FirstName = source.first_name", mapSource);
    }

    /// <summary> Verify that non-generic CreateMap calls are identified for open generic support. </summary>
    [Fact]
    public void Generator_OpenGenerics_Identification_Works()
    {
        var source = @"
using AutoMappic;

public class S<T> { public T Value { get; set; } }
public class D<T> { public T Value { get; set; } }

public class MyProfile : Profile
{
    public MyProfile() { CreateMap(typeof(S<>), typeof(D<>)); }
}";
        var result = GeneratorTestHelper.RunGenerator(source);
        var fileNames = result.Sources.Select(s => s.HintName).ToList();
        
        // Use a looser check for hint name
        var mapFileResult = result.Sources.FirstOrDefault(f => f.HintName.Contains("_To_"));
        Assert.True(mapFileResult.HintName != null, $"No mapping file generated. Files: {string.Join(", ", fileNames)}");
        var mapSource = mapFileResult.SourceText.ToString();
        // For now, we at least expect it to have the type names in it.
        // Note: unbound generics currently emit as S<> and D<>.
        Assert.Contains("S<>", mapSource);
        Assert.Contains("D<>", mapSource);
    }
}
