using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace AutoMappic.Benchmarks;

public class ProjectToSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class ProjectToDest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ProjectToProfile : AutoMappic.Profile
{
    public ProjectToProfile()
    {
        CreateMap<ProjectToSource, ProjectToDest>();
    }
}

public class ProjectToAutoMapperProfile : global::AutoMapper.Profile
{
    public ProjectToAutoMapperProfile()
    {
        CreateMap<ProjectToSource, ProjectToDest>();
    }
}

[MemoryDiagnoser]
public class ProjectToBenchmarks
{
    private IQueryable<ProjectToSource> _dataSource = null!;
    private global::AutoMapper.IMapper _autoMapper = null!;
    private AutoMappic.IMapper _autoMappic = null!;

    [GlobalSetup]
    public void Setup()
    {
        var list = new List<ProjectToSource>();
        for (int i = 0; i < 1000; i++)
        {
            list.Add(new ProjectToSource { Id = i, Name = $"Name {i}", Description = $"Desc {i}" });
        }
        _dataSource = list.AsQueryable();

        var amConfig = new global::AutoMapper.MapperConfigurationExpression();
        amConfig.AddProfile<ProjectToAutoMapperProfile>();
        _autoMapper = new global::AutoMapper.MapperConfiguration(amConfig, Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance)
            .CreateMapper();

        _autoMappic = new global::AutoMappic.MapperConfiguration(cfg => cfg.AddProfile<ProjectToProfile>()).CreateMapper();
    }

    [Benchmark(Baseline = true)]
    public List<ProjectToDest> AutoMapper_ProjectTo()
    {
        // AutoMapper ProjectTo uses reflection to build the expression tree at runtime
        return _dataSource.ProjectTo<ProjectToDest>(_autoMapper.ConfigurationProvider).ToList();
    }

    [Benchmark]
    public List<ProjectToDest> AutoMappic_ProjectTo()
    {
        // AutoMappic ProjectTo uses the source-generated static 'Projection' field
        return _dataSource.ProjectTo<ProjectToDest>().ToList();
    }
}
