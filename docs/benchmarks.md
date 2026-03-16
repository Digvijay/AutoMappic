# Benchmarks

We believe performance shouldn't come at the cost of developer experience. AutoMappic generates all mapping logic at compile-time using Source Generators and routes it using Interceptors. This removes the interface dispatch overhead entirely, bringing the performance down to that of manual, hand-written C# code.

## The Comparison

We benchmarked AutoMappic against other popular tools and approaches for .NET mapping. Here's a quick comparison of the three eras of mapping in .NET:

| Library | Mapping Engine | Approach | Setup Effort | Fast/AOT Compatible? |
| --- | --- | --- | --- | --- |
| **AutoMapper** | Reflection / IL Emit at Runtime | Convention-based | Low | ❌ |
| **Mapperly** | Source Generator | Explicit Partial Methods | Medium (Per method) | ✅ |
| **AutoMappic** | Source Generator + Interceptors | Convention-based | Lowest (Drop-in) | ✅ |

### Code Setup

In the benchmark, we measured mapping a simple `User` object (with an ID, a Username, an Email, and an `Address.City`) onto a `UserDto` (which flattens `AddressCity`). 

```csharp
[Benchmark(Baseline = true)]
public BenchUserDto AutoMapper_Legacy() =>
    _autoMapper.Map<BenchUser, BenchUserDto>(_source);

[Benchmark]
public BenchUserDto Mapperly_Explicit() =>
    _mapperly.MapToDto(_source);

// AutoMappic looks identical to AutoMapper_Legacy, but is intercepted!
[Benchmark]
public BenchUserDto AutoMappic_Intercepted() =>
    _autoMappic.Map<BenchUser, BenchUserDto>(_source);

[Benchmark]
public BenchUserDto Manual_HandWritten() =>
    ManualMapper.Map(_source);
```

### The Results

The goal is to be identical to **Manual HandWritten logic**. With AutoMappic, you get identical performance and **zero memory allocations** (`Gen0` and `Allocated` bytes are effectively nil on the framework overhead side!).

*Expect AutoMappic to drastically outperform legacy reflection-based mappers and run neck-and-neck with bare-metal manual assignment.*
