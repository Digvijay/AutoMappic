using AutoMapper;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Riok.Mapperly.Abstractions;

namespace AutoMappic.Benchmarks;

// ─── Fixtures ────────────────────────────────────────────────────────────────

public sealed class BenchUser
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public BenchAddress? Address { get; set; }
}

public sealed class BenchAddress
{
    public string City { get; set; } = string.Empty;
}

public sealed class BenchUserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AddressCity { get; set; } = string.Empty;
}

// ─── AutoMapper profile ───────────────────────────────────────────────────────

public sealed class BenchAutoMapperProfile : global::AutoMapper.Profile
{
    public BenchAutoMapperProfile()
    {
        CreateMap<BenchUser, BenchUserDto>()
            .ForMember(d => d.AddressCity, opt => opt.MapFrom(s => s.Address != null ? s.Address.City : string.Empty));
    }
}

// ─── Mapperly mapper (explicit partial method) ────────────────────────────────

[Mapper]
public partial class BenchMapperlyMapper
{
    [Riok.Mapperly.Abstractions.MapProperty(nameof(BenchUser.Address) + "." + nameof(BenchAddress.City), nameof(BenchUserDto.AddressCity))]
    public partial BenchUserDto MapToDto(BenchUser user);
}

// ─── AutoMappic profile ────────────────────────────────────────────────────────

public sealed class BenchAutoMappicProfile : AutoMappic.Profile
{
    public BenchAutoMappicProfile()
    {
        CreateMap<BenchUser, BenchUserDto>();
    }
}

// ─── Manual mapping (gold standard) ──────────────────────────────────────────

public static class ManualMapper
{
    public static BenchUserDto Map(BenchUser source) => new()
    {
        Id = source.Id,
        Username = source.Username,
        Email = source.Email,
        AddressCity = source.Address?.City ?? string.Empty,
    };
}

// ─── Benchmark suite ──────────────────────────────────────────────────────────

/// <summary>
///   Head-to-head comparison of AutoMapper, Mapperly, AutoMappic, and manual mapping.
/// </summary>
/// <remarks>
///   <para>
///     The project targets net10.0, so the job measures net10.0: <c>dotnet run -c Release</c>.
///     A figure may only be quoted for the framework it was actually measured on.
///   </para>
///   <para>Expected result: AutoMappic ≈ Mapperly ≈ Manual (all ≪ AutoMapper).</para>
/// </remarks>
[MemoryDiagnoser]
// No RuntimeMoniker: the job follows the project's TargetFramework. A pinned moniker is a
// hardcoded runtime that drifts away from what is actually shipped, which is how these
// benchmarks came to quote .NET 10 while measuring .NET 9 -- a runtime this repository no
// longer targets at all.
[SimpleJob(iterationCount: 30, warmupCount: 10)]
public class MappingBenchmarks
{
    private global::AutoMapper.IMapper _autoMapper = null!;
    private AutoMappic.IMapper _autoMappic = null!;
    private BenchMapperlyMapper _mapperly = null!;
    private BenchUser _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        using var loggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
        var autoMapperConfig = new global::AutoMapper.MapperConfigurationExpression();
        autoMapperConfig.AddProfile<BenchAutoMapperProfile>();
        _autoMapper = new global::AutoMapper.MapperConfiguration(autoMapperConfig, loggerFactory)
            .CreateMapper();

        _autoMappic = new AutoMappic.MapperConfiguration(cfg => cfg.AddProfile<BenchAutoMappicProfile>())
            .CreateMapper();

        _mapperly = new BenchMapperlyMapper();

        _source = new BenchUser
        {
            Id = 1,
            Username = "alice",
            Email = "alice@automappic.digvijay.dev",
            Address = new BenchAddress { City = "Stockholm" },
        };
    }

    /// <summary>Baseline: AutoMapper with reflection-backed expression trees.</summary>
    [Benchmark(Baseline = true)]
    public BenchUserDto AutoMapper_Legacy() =>
        _autoMapper.Map<BenchUser, BenchUserDto>(_source);

    /// <summary>Mapperly: source-generated explicit method call.</summary>
    [Benchmark]
    public BenchUserDto Mapperly_Explicit() =>
        _mapperly.MapToDto(_source);

    /// <summary>
    ///   AutoMappic: the call below looks identical to AutoMapper_Legacy above,
    ///   but at compile time the generator rewrites it to call the static generated method
    ///   via <c>[InterceptsLocation]</c>.
    /// </summary>
    [Benchmark]
    public BenchUserDto AutoMappic_Intercepted() =>
        _autoMappic.Map<BenchUser, BenchUserDto>(_source);

    /// <summary>Gold standard: hand-written direct assignment.</summary>
    [Benchmark]
    public BenchUserDto Manual_HandWritten() =>
        ManualMapper.Map(_source);
}
