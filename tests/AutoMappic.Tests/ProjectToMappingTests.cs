using System.Collections.Generic;
using System.Linq;
using AutoMappic;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public class ProjectToSource { public int Id { get; set; } public string Name { get; set; } = ""; }
public class ProjectToDest { public int Id { get; set; } public string Name { get; set; } = ""; }

public class ProjectToProfile : Profile
{
    public ProjectToProfile() { CreateMap<ProjectToSource, ProjectToDest>(); }
}

public class ProjectToMappingTests
{
    [Fact]
    [Prova.Description("Verify that IQueryable.ProjectTo works as an in-memory transformation using TopLevel classes.")]
    public void Queryable_ProjectTo_WorksInMemory()
    {
        var sourceList = new List<ProjectToSource>
        {
            new ProjectToSource { Id = 1, Name = "A" },
            new ProjectToSource { Id = 2, Name = "B" }
        }.AsQueryable();

        // ProjectTo is an extension on IQueryable
        var projected = sourceList.ProjectTo<ProjectToDest>().ToList();

        Assert.Equal(2, projected.Count);
        Assert.Equal(1, projected[0].Id);
        Assert.Equal("A", projected[0].Name);
    }
}
