![AutoMappic Hero](./docs/public/assets/hero.png)

# AutoMappic v0.1.0

[![.NET](https://img.shields.io/badge/.NET-9.0+-512bd4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green?style=flat-square)](LICENSE)
[![Docs](https://img.shields.io/badge/docs-automappic.digvijay.dev-blue?style=flat-square&logo=vitepress)](https://automappic.digvijay.dev)
[![Native AOT](https://img.shields.io/badge/Native_AOT-100%25-success?style=flat-square&logo=visual-studio)](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/)
[![Reflection](https://img.shields.io/badge/Reflection-0%25-red?style=flat-square)](https://github.com/LuckyPennySoftware/AutoMappic)

**Zero-Reflection. Zero-Overhead. Native AOT-First.**

AutoMappic is a convention-based object-to-object mapper for .NET 9+ that leverages **Roslyn Interceptors** to replace standard reflection with high-performance, statically-generated code at compile time.

> [!IMPORTANT]
> AutoMappic is designed for modern .NET workloads where performance and Native AOT compatibility are non-negotiable.

## Why AutoMappic?

Standard mappers like AutoMapper rely on runtime reflection and `Expression.Compile()`, which can be slow and often break in trimmed or Native AOT environments. AutoMappic shifts all the heavy lifting to the compiler.

- **Fast**: Faster than manual mapping because the compiler can optimize the straight-line C# we emit.
- **AOT Ready**: 100% compatible with Native AOT. No dynamic code generation at runtime.
- **Debuggable**: Step through your mapping code just like any other C# file.
- **Drop-in Migration**: Identical `Profile`, `CreateMap`, and `ForMember` syntax. Simply swap `using AutoMapper;` for `using AutoMappic;` and `AddAutoMapper` for `AddAutoMappic`.

## Technical Characteristics

AutoMappic is engineered for high-concurrency, low-latency .NET workloads where traditional reflection-based mapping introduces unacceptable overhead and breaks deployment targets like Native AOT.

- **Deterministic Performance**: By utilizing **Roslyn Interceptors**, mapping logic is resolved at compile-time. The JIT compiler receives straight-line static C#, enabling aggressive inlining and optimization that reaches the theoretical limits of manual assignment.
- **Native AOT & Trimming Integrity**: 100% compatible with Native AOT. AutoMappic generates all necessary code ahead-of-time, eliminating the need for `System.Reflection.Emit` or dynamic assembly loading.
- **Convention-Driven Automation**: Automated resolution of PascalCase flattening (e.g., `Order.Customer.Name` to `OrderDto.CustomerName`) and snake_case normalization without manual configuration.
- **Zero-Reflection Dependency Injection**: A unique "Static Registration Chain" discovers profiles across the entire solution at compile-time. `services.AddAutoMappic()` executes as a hard-coded sequence of static calls, providing near-zero startup latency.

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

// 3. Use it
var serviceProvider = services.BuildServiceProvider();
var mapper = serviceProvider.GetRequiredService<IMapper>();

var dto = mapper.Map<User, UserDto>(new User { Name = "Alice" });
```

For a detailed step-by-step tutorial, see [GettingStarted.md](./GettingStarted.md).

---
*Built for the .NET Community.*
