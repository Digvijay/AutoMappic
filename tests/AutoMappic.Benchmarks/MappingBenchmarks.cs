using AutoMapper;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Riok.Mapperly.Abstractions;

// Run benchmarks when executed in Release mode.
BenchmarkRunner.Run<MappingBenchmarks>();

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
    [MapProperty(nameof(BenchUser.Address) + "." + nameof(BenchAddress.City), nameof(BenchUserDto.AddressCity))]
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
///   Run with: <c>dotnet run -c Release</c>
///   Expected result: AutoMappic ≈ Mapperly ≈ Manual (all ≪ AutoMapper).
/// </remarks>
[MemoryDiagnoser]
[SimpleJob]
public class MappingBenchmarks
{
    private global::AutoMapper.IMapper _autoMapper = null!;
    private AutoMappic.IMapper _autoMappic = null!;
    private BenchMapperlyMapper _mapperly = null!;
    private BenchUser _source = null!;

    [GlobalSetup]
    public void Setup()
    {
        _autoMapper = new MapperConfiguration(cfg => cfg.AddProfile<BenchAutoMapperProfile>())
            .CreateMapper();

        _autoMappic = new AutoMappic.MapperConfiguration(cfg => cfg.AddProfile<BenchAutoMappicProfile>())
            .CreateMapper();

        _mapperly = new BenchMapperlyMapper();

        _source = new BenchUser
        {
            Id = 1,
            Username = "alice",
            Email = "alice@automappic.dev",
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
