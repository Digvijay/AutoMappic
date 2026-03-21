using System.Linq;
using AutoMappic.Generator.Pipeline;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Prova;

namespace AutoMappic.Tests;

public class CircularityTests
{
    [Fact]
    [Prova.Description("Verify that circular references in the mapping graph are detected at compile-time and report AM006.")]
    public void Generator_CircularReference_ReportsError()
    {
        var sourceCode = @"
using AutoMappic;
public class Node { public Node? Next { get; set; } }
public class NodeDto { public NodeDto? Next { get; set; } }

public class CircularProfile : Profile
{
    public CircularProfile()
    {
        CreateMap<Node, NodeDto>();
    }
}";

        var result = GeneratorTestHelper.RunGenerator(sourceCode);
        
        var errors = result.Diagnostics.Where(d => d.Id == "AM006").ToList();
        Prova.Assertions.Assert.NotEmpty(errors);
        Prova.Assertions.Assert.Contains("Circular reference detected", errors[0].GetMessage());
    }
}
