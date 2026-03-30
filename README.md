![AutoMappic Hero](./docs/public/assets/hero.png)

# AutoMappic v0.5.0

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
1. [What's New in v0.5.0](#whats-new-in-v050)
2. [Goals](#goals)
3. [Benchmarks](#benchmarks)
4. [Quick Start](#quick-start)
5. [NuGet Packages](#nuget-packages)
6. [Diagnostics](#diagnostics)
7. [Proven at Scale](#proven-at-scale)
8. [Documentation](#documentation)

---

## What's New in v0.5.0

v0.5.0 introduces **Persistence Intelligence** and **Developer Productivity** features, bridging the gap between simple mapping and complex entity synchronization while making the mapping process even faster with Smart-Match suggestions.

### Smart-Sync (EF Core Identity Mapping)
AutoMappic now intelligently synchronizes nested collections by comparing primary keys (`Id`, `[Key]`, or `[AutoMappicKey]`). Existing entities are updated **in-place**, preserving EF Core's Change Tracker state and eliminating redundant database operations.

### Smart-Match (AI-Lite Fuzzy Suggestions)
Never manually map a typo again. The AM0015 Smart-Match analyzer detects unmapped properties with similar names and provides a direct IDE Code-Fix to automatically apply the correct `[MapProperty]` attribute.

### Performance Guardrails
AM0016 warns you if your custom collection mapping logic (like LINQ `.Select`) is preventing compiler loop vectorization, ensuring your hot paths stay truly zero-overhead.

| Feature | Description |
| :--- | :--- |
| **Identity Management** | Opt-in `MappingContext` tracks object instances, prevents cyclic recursion, and enables entity-aware graph resolution. Enable via `<AutoMappic_EnableIdentityManagement>true` in your `.csproj`. |
| **Collection Syncing** | Key-based "Match-and-Sync" diffing replaces "Clear-and-Add" for collections, preserving EF Core change tracker state. |
| **Patch Mode** | With identity management active, nullable source properties generate conditional assignments (`if (source.Prop != null)`), enabling seamless HTTP PATCH. |
| **Static Converters** | `[AutoMappicConverter]` attribute enables zero-allocation custom mapping methods that bypass reflection entirely. |
| **AM0013 Diagnostic** | Build-time warning for required property/nullable source mismatches in patch mode. |

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
| **Manual HandWritten** | Static Assignment | 28.04 ns | 0.15 | 48 B |
| **AutoMappic_Intercepted** | **Source Gen + Interceptors** | **29.12 ns** | **0.15** | **48 B** |
| Mapperly_Explicit | Source Generation | 28.50 ns | 0.15 | 48 B |
| AutoMapper_Legacy | Reflection / IL Emit | 188.00 ns | 1.00 | 48 B |

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

## Performance Comparison (v0.5.0)

| Metric | AutoMapper (JIT) | AutoMappic (Native AOT) | Advantage |
| :--- | :--- | :--- | :--- |
| **Startup Time** | ~400ms | **~15ms** | **26x Faster** |
| **Throughput (1k List)**| 30.19 μs | **20.89 μs** | **1.45x Faster** |
| **Memory usage** | ~120MB | **~24MB** | **5x Lower** |

## NuGet Packages

*   **AutoMappic**: The main package containing the `IMapper` abstractions and the runtime core.
*   **AutoMappic.Generator**: The Roslyn incremental source generator. Typically included as a private asset/analyzer.

```xml
<PackageReference Include="AutoMappic" Version="0.5.0" />
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
