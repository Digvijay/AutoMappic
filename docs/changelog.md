# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.0] - 2026-03-21

### Added
- **Multi-Targeting Support**: Explicitly added support for both .NET 9 and .NET 10 to ensure first-class performance on the latest .NET SDKs.
- **Enhanced ProjectTo Interception**: Upgraded the source generator to handle ProjectTo calls with additional arguments (e.g. IConfigurationProvider), enabling seamless migration for CleanArchitecture-style projects.
- **AutoMapper Bridge**: Added an AddAutoMapper extension method and IConfigurationProvider interface to AutoMappic.Core to provide 1:1 API compatibility for migrating projects.
- **Ambiguity Resolution**: Refined the mapping convention engine to prioritize direct property matches over flattened paths, resolving internal conflicts (AM002) in complex domain models.
- **Full Diagnostic Suite**: Expanded build-time safety checks to include AM001 through AM009, covering duplicate mappings and symbol resolution failures.
- **Professional Documentation**: Comprehensive overhaul of README and VitePress tutorials, drawing inspiration from the Mediator project for clarity and precision.

### Fixed
- **Interceptor Signature Mismatches**: Resolved CS9144 errors when intercepting methods with variable argument counts.
- **XML Documentation**: Added missing XML comments to all public APIs to satisfy high-quality build requirements.
- **Standardized Encoding**: Replaced all emojis and non-ASCII characters with professional ASCII equivalents across the entire repository.

## [0.2.0] - 2026-03-18

### Added
- **Zero-LINQ Collection Mapping:** Implemented high-performance, statically generated `for` loops for list and array mapping, bypassing `System.Linq` overhead.
- **Pre-allocation Optimization:** Automatically sizes destination collections (`List<T>` with capacity or `new T[]`) to eliminate unnecessary Gen 0 allocations.
- **Improved AM001 Diagnostics:** Enhanced "Unmapped Destination" detection to intelligently handle `[Required]` and C# 11 `required` modifiers.
- **Expanded Interceptor Support:** Improved `ProjectTo` and `DataReader` interceptors to support advanced collection-to-collection mapping shims.
- **Official NuGet Icon:** Included a high-resolution, square branding asset (`icon.png`) within the package metadata.
- **Performance Benchmarks:** Added a new `ListMappingBenchmarks` suite to provide transparent performance data for high-volume collection mapping.
- **Sustainability Case Study:** Documented the environmental and server-density advantages of our Zero-LINQ strategy for cloud-native workloads.

### Changed
- **Roslyn Versioning:** Pinned `Microsoft.CodeAnalysis` to 4.14.0 to guarantee host compatibility across all stable versions of Visual Studio 2022 and .NET 9 while enabling Interceptors.
- **Documentation Refinement:** Comprehensive cleanup of project-wide documentation (README, VitePress, Roadmap) to reflect new v0.3.0 features.

### Security
- **Version Hardening:** Updated all NuGet dependencies to their latest stable patches.

## [0.1.0] - 2026-03-16

### Added
- **Initial Release:** Welcome to AutoMappic!
- **Source Generator Engine:** High-performance mapping via C# 12 Interceptors, completely bypassing `System.Reflection` at runtime.
- **Convention-based Mapping:** Automatic name matching between source and destination types.
- **PascalCase Flattening:** Automatically resolves things like `Order.Customer.Name` onto `OrderDto.CustomerName`.
- **Bidirectional Mapping:** Added `.ReverseMap()` functionality to automatically generate two-way mappings from a single `CreateMap` configuration.
- **Explicit Member Overrides:** Support for `.ForMember(dest => dest.Prop, opt => opt.MapFrom(src => src.OtherProp))`.
- **Dependency Injection:** Seamless configuration via `services.AddAutoMappic(typeof(Program).Assembly)` to drop-in replace existing enterprise architectures.
- **Complex Hierarchies:** Deep list and dictionary (`Dictionary<TKey, TValue>`) projection generation natively mapped into arrays or generic lists.
- **Enterprise Proven:** Fully verified against complex base classes and manually mapped DTOs in the official Microsoft `eShopOnWeb` architecture.
- **Diagnostic Analyzers:** Ships with build-time guards (`AM001`, `AM002`) to help developers catch mapping mistakes immediately within the IDE.
- **Native AOT & Trimming Support:** Initial release is 100% compatible with Native AOT, featuring robust handling for `object` mappings and null-conditional flattened paths.
- **Transitive Analyzer Support:** Improved NuGet packaging ensuring the source generator is correctly referenced by all consumer projects.
