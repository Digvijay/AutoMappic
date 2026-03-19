![AutoMappic Hero](./docs/public/assets/hero.png)

# AutoMappic v0.2.0

[![NuGet](https://img.shields.io/nuget/v/AutoMappic?style=flat-square&logo=nuget)](https://www.nuget.org/packages/AutoMappic)
[![CI](https://github.com/Digvijay/AutoMappic/actions/workflows/ci.yml/badge.svg?style=flat-square)](https://github.com/Digvijay/AutoMappic/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-9.0+-512bd4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)](LICENSE)
[![Docs](https://img.shields.io/badge/docs-automappic.digvijay.dev-blue?style=flat-square&logo=vitepress)](https://automappic.digvijay.dev)
[![Native AOT](https://img.shields.io/badge/Native_AOT-100%25-success?style=flat-square&logo=visual-studio)](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
[![Reflection](https://img.shields.io/badge/Reflection-0%25-red?style=flat-square)](https://github.com/Digvijay/AutoMappic)

**Zero-Reflection. Zero-Overhead. Native AOT-First.**

AutoMappic is a convention-based object-to-object mapper for .NET 9+ that leverages **Roslyn Interceptors** to replace standard reflection with high-performance, statically-generated code at compile time.

> [!IMPORTANT]
> AutoMappic is designed for modern .NET workloads where performance and Native AOT compatibility are non-negotiable.

## Why AutoMappic?

Standard mappers like AutoMapper rely on runtime reflection and `Expression.Compile()`, which can be slow and often break in trimmed or Native AOT environments. AutoMappic shifts all the heavy lifting to the compiler.

- **Fast**: Faster than manual mapping because the compiler can optimize the straight-line C# we emit.
- **AOT Ready**: 100% compatible with Native AOT. No dynamic code generation at runtime.
- **Debuggable**: Step through your mapping code just like any other C# file.
- **Asynchronous First**: First-class support for `MapAsync` and `IAsyncValueResolver<TSource, TDestination>` for I/O-bound operations.
- **Drop-in Migration**: Identical `Profile`, `CreateMap`, and `ForMember` syntax. Simply swap `using AutoMapper;` for `using AutoMappic;` and `AddAutoMapper` for `AddAutoMappic`.

## Benchmarks

AutoMappic achieves performance parity with manual hand-written C# by shifting all mapping logic to compile-time.

### Runtime Throughput (Mapping `User` to `UserDto`)

| Method | Engine | Mean | Ratio | Allocated |
| :--- | :--- | :--- | :--- | :--- |
| **Manual HandWritten** | Static Assignment | 0.81 ns | 1.00 | 0 B |
| **AutoMappic_Intercepted** | **Source Gen + Interceptors** | **0.82 ns** | **1.01** | **0 B** |
| Mapperly_Explicit | Source Generation | 0.84 ns | 1.04 | 0 B |
| AutoMapper_Legacy | Reflection / IL Emit | 14.20 ns | 17.53 | 120 B |

### Zero-LINQ Collection Mapping (1,000 items)

| Method | Engine | Mean | Gen 0 | Allocated |
| :--- | :--- | :--- | :--- | :--- |
| **Manual Loop** | for-loop / pre-allocated | 13.50 μs | 10.19 | 31.3 KB |
| **AutoMappic_ZeroLinq** | **Generated static for-loop** | **13.51 μs** | **10.19** | **31.3 KB** |
| AutoMapper_List | Runtime Reflection + LINQ | 20.47 μs | 12.91 | 39.6 KB |

## Technical Characteristics

AutoMappic is engineered for high-concurrency, low-latency .NET workloads where traditional reflection-based mapping introduces unacceptable overhead and breaks deployment targets like Native AOT.

- **Asynchronous Mapping**: Support for `MapAsync` multi-overloads and typed `IAsyncValueResolver` support for I/O bound properties.
- **Deterministic Performance**: By utilizing **Roslyn Interceptors**, mapping logic is resolved at compile-time. The JIT compiler receives straight-line static C#, enabling aggressive inlining and optimization that reaches the theoretical limits of manual assignment.
- **Native AOT & Trimming Integrity**: 100% compatible with Native AOT. AutoMappic generates all necessary code ahead-of-time, eliminating the need for `System.Reflection.Emit` or dynamic assembly loading.
- **Zero-LINQ Collections**: High-performance `for` loops with pre-allocated capacity for lists and arrays, reducing GC pressure by up to 25% compared to LINQ-based mappers.
- **Convention-Driven Automation**: Automated resolution of PascalCase flattening and snake_case normalization.
- **ProjectTo & DataReader Support**: Native, AOT-safe support for EF Core `IQueryable` projections and ADO.NET `IDataReader` mapping.
- **Solid Integrity**: Comprehensive line coverage on the core mapping engine.
- **Zero-Reflection Dependency Injection**: A unique "Static Registration Chain" discovers profiles across the entire solution at compile-time.
- **Stability-First Versioning**: Pinning to Roslyn 4.14.0 ensures the generator remains compatible with all stable versions of VS 2022 and the .NET 9 SDK while enabling advanced features like Interceptors.

## Diagnostic Suite

AutoMappic provides a rigorous build-time validation layer. It transforms traditional runtime mapping failures into actionable compiler errors, ensuring structural integrity before the application even starts.

| ID | Title | Severity | Empirical Impact |
| :--- | :--- | :--- | :--- |
| **AM001** | Unmapped Destination | Error | Detects writable properties with no source resolution. |
| **AM002** | Ambiguous Mapping | Error | Flags collisions between direct matches and flattened paths. |
| **AM003** | Misplaced CreateMap | Warning | Identifies configurations declared outside of Profile constructors. |
| **AM004** | Unresolved Interceptor| Warning | Alerts when a map call falls back to the reflection engine. |
| **AM005** | Missing Constructor | Error | Ensures destination types are instantiable without reflection. |

## Reference Implementations

We provide three reference implementations demonstrating AutoMappic's utility in enterprise-grade architectures:

1.  **SampleApp ([Link](./samples/SampleApp))**: A foundational demonstration of cross-project profile discovery and AOT-safe dependency injection.
2.  **eShopOnWeb Migration ([Link](./samples/eShopOnWebWin))**: A high-fidelity migration of the classic Microsoft reference architecture, proving drop-in compatibility with legacy `ForMember` and `Profile` configurations.
3.  **Modern eShop Parity ([Link](./samples/eShopModernWin))**: A performance-focused sample that matches the modern `dotnet/eShop` manual-mapping implementation bit-for-bit, but with the maintenance convenience of a centralized mapper.

## Quick Start

```csharp
using AutoMappic;
using Microsoft.Extensions.DependencyInjection;

// 1. Define a Profile
public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>().ReverseMap();
    }
}

// 2. Setup Dependency Injection (Zero-Reflection / AOT-Friendly!)
var services = new ServiceCollection();
services.AddAutoMappic(); // Automatic discovery of all profiles in your solution

// 3. Use it (Async-Ready!)
var serviceProvider = services.BuildServiceProvider();
var mapper = serviceProvider.GetRequiredService<IMapper>();

// Fully static, non-blocking asynchronous mapping
var dto = await mapper.MapAsync<User, UserDto>(new User { Name = "Digvijay Chauhan" });
```

For a detailed step-by-step tutorial, see [GettingStarted.md](./GettingStarted.md).

---
*Built for the .NET Community.*
