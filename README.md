![AutoMappic Hero](./docs/public/assets/hero.png)

# AutoMappic v0.3.0

[![NuGet](https://img.shields.io/nuget/v/AutoMappic?style=flat-square&logo=nuget)](https://www.nuget.org/packages/AutoMappic)
[![CI](https://github.com/Digvijay/AutoMappic/actions/workflows/ci.yml/badge.svg?style=flat-square)](https://github.com/Digvijay/AutoMappic/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-9.0%2B_/_10.0-512bd4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)](LICENSE)
[![Docs](https://img.shields.io/badge/docs-automappic.digvijay.dev-blue?style=flat-square&logo=vitepress)](https://automappic.digvijay.dev)
[![Native AOT](https://img.shields.io/badge/Native_AOT-100%25-success?style=flat-square&logo=visual-studio)](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)

**Zero-Reflection. Zero-Overhead. Native AOT-First.**

AutoMappic is a high-performance object-to-object mapper for .NET 9 and .NET 10. It uses **Roslyn Interceptors** to replace standard reflection-based mapping with statically-generated C# at compile time.

---

## Table of Contents
1. [Goals](#goals)
2. [Benchmarks](#benchmarks)
3. [Quick Start](#quick-start)
4. [NuGet Packages](#nuget-packages)
5. [Diagnostics](#diagnostics)
6. [Proven at Scale](#proven-at-scale)
7. [Documentation](#documentation)

---

## Goals
*   **High Performance**: Faster than manual mapping by enabling aggressive JIT inlining of straight-line C# assignments.
*   **Native AOT Ready**: 100% compatible with Native AOT and trimming. No dynamic code generation or reflection at runtime.
*   **Build-Time Safety**: Mapping errors (ambiguity, missing members, circular references) are caught during compilation, not at runtime.
*   **Zero-Startup Cost**: Eliminates reflection-based profile scanning. Dependency injection initializes instantly.

## Development

AutoMappic is built for extreme performance using modern .NET tooling.

### Tooling

- **CSharpier**: Opinionated code formatter for consistent semantics across the tree.
- **Roslyn SDK**: Powers the incremental source generator and interceptors.

### Common Tasks

- **Build**: `dotnet build /p:TreatWarningsAsErrors=true`
- **Test**: `dotnet run --project tests/AutoMappic.Tests/AutoMappic.Tests.csproj`
- **Bench**: `dotnet run -c Release --project tests/AutoMappic.Benchmarks/AutoMappic.Benchmarks.csproj`

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Benchmarks

AutoMappic achieves performance parity with manual hand-written C# by shifting all mapping logic to compile-time.

### Runtime Throughput (Mapping `User` to `UserDto`)

| Method | Engine | Mean | Ratio | Allocated |
| :--- | :--- | :--- | :--- | :--- |
| **Manual HandWritten** | Static Assignment | 0.81 ns | 1.00 | 0 B |
| **AutoMappic_Intercepted** | **Source Gen + Interceptors** | **0.82 ns** | **1.01** | **0 B** |
| Mapperly_Explicit | Source Generation | 0.84 ns | 1.04 | 0 B |
| AutoMapper_Legacy | Reflection / IL Emit | 14.20 ns | 17.53 | 120 B |

## Quick Start

```csharp
using AutoMappic;
using Microsoft.Extensions.DependencyInjection;

// 1. Define a Profile (Exactly like AutoMapper)
public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>().ReverseMap();
    }
}

// 2. Setup Dependency Injection (AOT-Friendly!)
var services = new ServiceCollection();
services.AddAutoMappic(); // Compile-time discovery of all profiles

// 3. Use it (Async-Ready!)
var serviceProvider = services.BuildServiceProvider();
var mapper = serviceProvider.GetRequiredService<IMapper>();

// Zero dynamic code, 100% static execution
var dto = mapper.Map<User, UserDto>(new User { Name = "Digvijay Chauhan" });
```

## NuGet Packages

*   **AutoMappic**: The main package containing the `IMapper` abstractions and the runtime core.
*   **AutoMappic.Generator**: The Roslyn incremental source generator. Typically included as a private asset/analyzer.

```xml
<PackageReference Include="AutoMappic" Version="0.3.0" />
```

## Diagnostics

AutoMappic provides a rigorous build-time validation layer, ensuring structural integrity before the application starts.

| ID | Title | Severity | Impact |
| :--- | :--- | :--- | :--- |
| **AM001** | Unmapped Destination | Error | Detects writable properties with no source resolution. |
| **AM002** | Ambiguous Mapping | Error | Flags collisions between direct matches and flattened paths. |
| **AM003** | Misplaced CreateMap | Warning | Identifies mappings declared outside of Profile constructors. |
| **AM004** | Unresolved Interceptor| Warning | Alerts when a map call falls back to the reflection engine. |
| **AM005** | Missing Constructor | Error | Ensures destination types are instantiable without reflection. |
| **AM006** | Circular Reference | Error | Detects recursive mapping loops to prevent StackOverflow. |
| **AM007** | Symbol Resolution | Warning | Reports when Roslyn cannot fully resolve a mapping's types. |
| **AM008** | ProjectTo Interjection | Warning | Identifies procedural logic that cannot be converted to SQL. |
| **AM009** | Duplicate Mapping | Warning | Detects conflicting configurations for the same type-pair. |
| **AM010** | Performance Hotpath | Info | Flags high-allocation nested collection mappings. |
| **AM011** | Multi-Source ProjectTo | Error | Prevents unsupported multi-source database projections. |
| **AM012** | Asymmetric Mapping | Warning | Flags mappings with no writable destination properties. |

## Proven at Scale

AutoMappic has been verified as a drop-in replacement for complex enterprise templates like [`jasontaylordev/CleanArchitecture`](https://github.com/jasontaylordev/CleanArchitecture). By simply switching the namespace and enabling interceptors, even projects with deep Entity Framework projections and complex profiles can achieve Native AOT compatibility.

## Documentation

For detailed guides, API reference, and advanced tutorials, visit our documentation site:
**[automappic.digvijay.dev](https://automappic.digvijay.dev)**

---
*Built for the .NET Community.*
