![AutoMappic Hero](./docs/public/assets/hero.png)

# AutoMappic v0.7.0

[![NuGet](https://img.shields.io/nuget/v/AutoMappic?style=flat-square&logo=nuget)](https://www.nuget.org/packages/AutoMappic)
[![CI](https://github.com/Digvijay/AutoMappic/actions/workflows/ci.yml/badge.svg?style=flat-square)](https://github.com/Digvijay/AutoMappic/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-9.0%2B_/_10.0-512bd4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)](LICENSE)
[![Docs](https://img.shields.io/badge/docs-automappic.digvijay.dev-blue?style=flat-square&logo=vitepress)](https://automappic.digvijay.dev)
[![Native AOT](https://img.shields.io/badge/Native_AOT-100%25-success?style=flat-square&logo=visual-studio)](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)

**Zero-Reflection. Zero-Overhead. Native AOT-First.**

AutoMappic is a high-performance object-to-object mapper for .NET 8 and .NET 10. It uses **Roslyn Interceptors** to replace standard reflection-based mapping with statically-generated C# at compile time.

---

## Table of Contents
1. [What's New in v0.7.0](#whats-new-in-v070)
2. [Goals](#goals)
3. [Benchmarks](#benchmarks)
4. [Quick Start](#quick-start)
5. [NuGet Packages](#nuget-packages)
6. [Diagnostics](#diagnostics)
7. [Proven at Scale](#proven-at-scale)
8. [Documentation](#documentation)

---

## What's New in v0.7.0

v0.7.0 is our "Ultimate" release, bringing advanced database streaming, a new fluent mapping API, and robust Hot-Reload support to make the development experience faster and more fluid.

### "The Ultimate" Database Streaming
`DbDataReader.MapAsync` now returns an `IAsyncEnumerable<T>`, enabling zero-allocation, non-blocking streaming of database results directly into your application logic via `await foreach`.

### Fluent MapTo API
Introduced `source.MapTo<TDest>(mapper)` extension methods for a more natural, object-oriented syntax that is still 100% statically intercepted. We've also included a built-in `migrate` CLI command to easily transition your legacy code.

### Cross-Environment Excellence
From native AOT hardening and Wasm/Blazor optimizations to a robust recursion safety guard, v0.7.0 runs everywhere seamlessly. We also added first-class support for mapping between PascalCase domain models and camelCase JSON DTOs.

| Feature | Description |
| :--- | :--- |
| **Hot-Reload Support** | High-performance `HotReloadRegistry` ensures ultra-fast mapping continues uninterrupted during aggressive edit-and-continue sessions. |
| **CamelCase Support** | `CamelCaseNamingConvention` solves mismatches between C# record properties and external JSON schemas. |
| **Enhanced Diagnostics** | Improved `AM0006` (Circular Reference) detection and recursion safety guard safeguard your mappings. |
| **Integrity Checks** | Robust 128-level recursion limiter and the eradication of `MappingContext` CS0436 conflicts across multi-project solutions. |

---

## Goals
*   **High Performance**: Generates straight-line C# assignments that the JIT can inline. Substantially faster than reflection-based mapping; see [Benchmarks](#benchmarks) for how it compares to a hand-written mapper.
*   **Native AOT**: Mapping is generated at compile time, with no dynamic code generation or reflection at runtime. Note that the AutoMapper-compatible `Profile` / `IMapper` API surface is currently annotated `RequiresDynamicCode`, so AOT consumers of that API see IL3050 warnings — see [docs/known-issues.md](docs/known-issues.md).
*   **Build-Time Safety**: Mapping errors (ambiguity, missing members, circular references) are caught during compilation, not at runtime.
*   **Zero-Startup Cost**: Eliminates reflection-based profile scanning. Dependency injection initializes instantly.

> **Before adopting:** `PrivateAssets="all"` on the package reference causes a runtime
> `FileNotFoundException` rather than a build error, and the AutoMapper-compatible
> `Profile` / `IMapper` surface is annotated `RequiresDynamicCode`, so AOT consumers of that API
> see IL3050 warnings. Both are documented with workarounds in
> [docs/known-issues.md](docs/known-issues.md). The previous requirement to add
> `InternalsVisibleTo` across assembly boundaries has been **fixed** — the generated registration
> class is now public.

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
| **Manual HandWritten** | Static Assignment | 28.04 ns | 0.15 | 48 B |
| **AutoMappic_Intercepted** | **Source Gen + Interceptors** | **29.12 ns** | **0.15** | **48 B** |
| Mapperly_Explicit | Source Generation | 28.50 ns | 0.15 | 48 B |
| AutoMapper_Legacy | Reflection / IL Emit | 188.00 ns | 1.00 | 48 B |

*Hardware and .NET version for this run are not recorded. Treat the absolute numbers as
indicative and reproduce them on your own hardware before relying on them.*

### Independent re-measurement

Measured in this repository's own benchmark suite on a Snapdragon X Elite X1E80100 (ARM64),
Windows 11, BenchmarkDotNet 0.15.8, 30 iterations, mapping a five-member object. Both frameworks
the package ships are measured, because the answer is not the same on each:

**.NET 10**

| Method | Mean | Allocated |
| :--- | ---: | ---: |
| Hand-written | 7.82 ns | 48 B |
| **AutoMappic** | **7.51 ns** | **48 B** |
| Mapperly | 7.65 ns | 48 B |
| AutoMapper | 57.27 ns | 48 B |

**.NET 8**

| Method | Mean | Allocated |
| :--- | ---: | ---: |
| Hand-written | 10.33 ns | 48 B |
| **AutoMappic** | **11.63 ns** | **48 B** |
| Mapperly | 10.77 ns | 48 B |
| AutoMapper | 60.20 ns | 48 B |

Allocation is identical to a hand-written mapper on both frameworks, and roughly 7.6x faster than
AutoMapper on .NET 10. AutoMappic is marginally faster than hand-written mapping on .NET 10 and
about 12% slower on .NET 8; the per-framework difference is the reason both are published.

Reproduce with `dotnet run -c Release -f net10.0` from `tests/AutoMappic.Benchmarks`, substituting
the framework you care about.

> **An earlier version of this section published different numbers** — 136 B against 208 B, and a
> 22% deficit to hand-written mapping — and described the allocation overhead as an open issue.
> Those figures were not reproducible. The benchmark suite could not build at all, so no result it
> reported was meaningful. Both the build failure and a genuine boxing defect that the working
> benchmarks then exposed are recorded in [docs/known-issues.md](docs/known-issues.md), items 8 and
> 12.

## Quick Start

1.  **Add the package**: `dotnet add package AutoMappic`
2.  **Define your Profile**:
    ```csharp
    public class MyProfile : AutoMappic.Profile {
        public MyProfile() { CreateMap<User, UserDto>(); }
    }
    ```
3.  **Map with Zero Overhead**:
    ```csharp
    var dto = mapper.Map<User, UserDto>(user); // Intercepted at compile-time!
    ```

## Why AutoMappic?

- **Native AOT Compatible**: No runtime reflection, no dynamic IL emission.
- **Zero Resolution Tax**: Call-site interception binds source to destination at compile-time.
- **Enterprise Ready**: Support for cyclic graphs, identity management, and collection syncing.
- **Diagnostic Safety**: Rich build-time feedback (e.g., **AM0013** for patch-mode integrity).

## Performance Comparison (v0.7.0)

| Metric | AutoMapper (JIT) | AutoMappic (Native AOT) | Advantage |
| :--- | :--- | :--- | :--- |
| **Startup Time** | ~400ms | **~15ms** | **26x Faster** |
| **Throughput (1k List)**| 30.19 μs | **20.89 μs** | **1.45x Faster** |
| **Memory usage** | ~120MB | **~24MB** | **5x Lower** |

*This table compares two different deployment models as well as two libraries, so most of the
startup and memory difference is attributable to Native AOT rather than to mapping. It is a fair
description of the end-to-end outcome, not of mapping performance in isolation.*

## NuGet Packages

*   **AutoMappic**: The main package containing the `IMapper` abstractions and the runtime core.
*   **AutoMappic.Generator**: The Roslyn incremental source generator.

> Do **not** set `PrivateAssets="all"` on the `AutoMappic` package reference. It suppresses
> `AutoMappic.Core.dll` as well as the generator, so consuming projects compile successfully and
> then throw `FileNotFoundException` when mapping first executes. See
> [docs/known-issues.md](docs/known-issues.md).

```xml
<PackageReference Include="AutoMappic" Version="0.7.0" />
```

## Diagnostics

AutoMappic provides a rigorous build-time validation layer, ensuring structural integrity before the application starts.

| ID | Name | Severity | Description |
| :--- | :--- | :--- | :--- |
| **AM0001** | Unmapped Property | Error | Destination property has no source match. |
| **AM0004** | Fallback Warning | Warning | Call-site not intercepted; falling back to reflection. |
| **AM0006** | Circular Map | Error | Infinite recursion detected in static graph. |
| **AM0008** | ProjectTo Warning | Warning | Procedural logic found in database projection. |
| **AM0013** | Patch Integrity | Warning | Warns when patching `required` properties from nullable sources. |
| **AM0014** | Unmapped Key | Warning | Nested entity has a primary key but the source mapping lacks it. |
| **AM0015** | Smart-Match | Warning | Unmapped property has a close name match at source. Includes Code-Fix. |
| **AM0016** | Performance | Warning | Custom collection logic breaks JIT loop vectorization. |
| **AM0017** | Ambiguous Key | Warning | Entity has multiple potential keys; explicit `[AutoMappicKey]` required. |

## Proven at Scale

AutoMappic has been verified as a drop-in replacement for complex enterprise templates like [`jasontaylordev/CleanArchitecture`](https://github.com/jasontaylordev/CleanArchitecture). By simply switching the namespace and enabling interceptors, even projects with deep Entity Framework projections and complex profiles can achieve Native AOT compatibility.

## Documentation

For detailed guides, API reference, and advanced tutorials, visit our documentation site:
**[automappic.digvijay.dev](https://automappic.digvijay.dev)**

---
*Built for the .NET Community.*
