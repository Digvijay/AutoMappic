using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using AutoMappic;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public class SqlMappingTests
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
    }

    public class SqlProfile : Profile
    {
        public SqlProfile()
        {
            CreateMap<IDataReader, UserDto>();
            CreateMap<Source, UserDto>(); // For ProjectTo
        }
    }

    public class Source { public int Id { get; set; } public string Name { get; set; } public string? Email { get; set; } }

    [Fact]
    [Description("Tests mapping from a real DataReader (DataTable) with Nulls and Ordinals.")]
    public void DataReader_Mapping_WithRealData_Works()
    {
        // 1. Setup DataTable source
        using var dt = new DataTable();
        dt.Columns.Add("Id", typeof(int));
        dt.Columns.Add("Name", typeof(string));
        dt.Columns.Add("Email", typeof(string));
        
        dt.Rows.Add(1, "Alice", "alice@example.com");
        dt.Rows.Add(2, "Bob", DBNull.Value); // Test DBNull -> null
        
        // 2. Setup AutoMappic
        var config = new MapperConfiguration(cfg => {
             cfg.AddProfile<SqlProfile>();
        });
        
        using var reader = dt.CreateDataReader();
        
        // 3. Act - Use the intercepted DataReader extension
        var results = reader.Map<UserDto>().ToList();
        
        // 4. Assert
        Assert.Equal(2, (long)results.Count);
        Assert.Equal("Alice", results[0].Name);
        Assert.Equal("alice@example.com", results[0].Email);
        Assert.Equal("Bob", results[1].Name);
        Assert.Equal(null, results[1].Email); // Verified DBNull safety
    }

    [Fact]
    [Description("Verifies ProjectTo generates an expression that can expand correctly.")]
    public void ProjectTo_Expansion_IsExpressionReady()
    {
        // Interceptor for ProjectTo<UserDto> should trigger here
        var source = new List<Source> { new() { Id = 1, Name = "Alice", Email = "alice@a.com" } }.AsQueryable();
        
        var results = source.ProjectTo<UserDto>().ToList();
        
        Assert.Equal(1, (long)results.Count);
        Assert.Equal("Alice", results[0].Name);
        Assert.Equal("alice@a.com", results[0].Email);
    }
}
