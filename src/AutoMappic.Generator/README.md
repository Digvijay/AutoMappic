# AutoMappic.Generator

The **AutoMappic.Generator** is a Roslyn-based incremental source generator and set of analyzers for **Zero-Reflection** and **Zero-Overhead** mapping in .NET projects.

## Project Role

This project implements the **Roslyn Incremental Source Generator**. It is an internal build-time asset (an Analyzer) that is automatically bundled into the primary **AutoMappic** NuGet package.

### Consumption

End users do not reference this project directly. Instead, they install the main library:

```xml
<PackageReference Include="AutoMappic" Version="0.5.0" />
```

At build time, the NuGet package provides both the runtime abstractions and this generator in the `analyzers/` folder, enabling zero-configuration mapping generation.

At build time, the generator:
1.  **Extracts** all `Profile` and `CreateMap` declarations from your project.
2.  **Resolves** mappings based on naming conventions and explicit configurations using a powerful **Convention Engine**.
3.  **Detects** potential issues (e.g., circular references, unmapped members, performance regressions) and reports them as **Diagnostics** (AM0001-AM0017).
4.  **Emits** optimized, statically-linked C# code that intercepts standard `IMapper.Map` calls at their call site via C# 12 **Interceptors**.

## Key Features

- **Interception at Call Site**: Automatically replaces `IMapper.Map<S, D>(src)` with a direct, static, reflection-free method call.
- **Incremental Pipeline**: Engineered for responsiveness in large IDE environments, recalculating only the parts of your mapping graph that have actually changed.
- **Smart-Match Analyzer**: A built-in string similarity engine (AM0015) providing intelligent suggestions for unmapped properties.
- **Performance Guardrails**: Analyzes collection mapping logic to ensure it remains JIT-inlineable and vectorization-friendly.

This generator is the cornerstone of the v0.5.0 stable release, enabling Native AOT and first-class performance on .NET 9 and .NET 10.
