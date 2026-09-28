using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;

namespace AutoMappic.Benchmarks;

[MemoryDiagnoser]
// No RuntimeMoniker: the job follows the project's TargetFramework. Pinning a moniker here
// silently measured a runtime other than the one the project ships, and the published
// numbers then cited a framework the benchmark had never run on.
[SimpleJob(iterationCount: 30, warmupCount: 10)]
public class ListMappingBenchmarks
{
    private global::AutoMapper.IMapper _autoMapper = null!;
    private AutoMappic.IMapper _autoMappic = null!;
    private List<PointSource> _source = null!;

    public sealed class PointSource { public int X { get; set; } public int Y { get; set; } }
    public sealed class PointDto { public int X { get; set; } public int Y { get; set; } }

    private sealed class BenchProfile : AutoMappic.Profile
    {
        public BenchProfile() => CreateMap<PointSource, PointDto>();
    }

    private sealed class AMProfile : global::AutoMapper.Profile
    {
        public AMProfile() => CreateMap<PointSource, PointDto>();
    }

    [Params(100, 1000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        using NullLoggerFactory loggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
        var autoMapperConfig = new global::AutoMapper.MapperConfigurationExpression();
        autoMapperConfig.AddProfile<AMProfile>();
        _autoMapper = new global::AutoMapper.MapperConfiguration(autoMapperConfig, loggerFactory)
            .CreateMapper();

        _autoMappic = new global::AutoMappic.MapperConfiguration(cfg => cfg.AddProfile<BenchProfile>())
            .CreateMapper();

        _source = [.. Enumerable.Range(0, Count).Select(i => new PointSource { X = i, Y = i })];
    }

    [Benchmark(Baseline = true)]
    public List<PointDto> AutoMapper_List() =>
        _autoMapper.Map<List<PointSource>, List<PointDto>>(_source);

    [Benchmark]
    public List<PointDto> AutoMappic_List_ZeroLinq() =>
        _source.MapTo<List<PointDto>>(_autoMappic);

    [Benchmark]
    public List<PointDto> Manual_List()
    {
        var list = new List<PointDto>(_source.Count);
        foreach (PointSource s in _source)
        {
            list.Add(new PointDto { X = s.X, Y = s.Y });
        }
        return list;
    }
}
