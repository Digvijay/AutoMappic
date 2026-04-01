# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.5.1] - 2026-04-01

### Fixed
- **IDE Stability (Incremental Generator Fix)**: Resolved a critical issue where the source generator could crash with a "SyntaxTree is not part of the compilation" error after multiple edits. Switched to serializable `DiagnosticInfo` records in the incremental pipeline to avoid pinning stale syntax nodes in the cache.
- **Smart-Match Code-Fix Precision**: Fixed a bug where the AM0015 "lightbulb" fix would fail to appear in the IDE due to location mismatches. Diagnostics are now correctly anchored to the offending property in the destination type, and the provider uses robust metadata to locate the syntax node correctly.

## [0.5.0] - 2026-03-30

### Added
- **Smart-Sync (EF Core Identity Mapping)**: Enhanced collection mapping logic using a keyed-lookup approach to identify and update existing entity instances. This preserves EF Core's change tracking state by performing in-place updates instead of full collection replacements.
- **Fuzzy-Match (AM0015 Smart-Match Analyzer)**: High-performance string similarity analyzer using Levenshtein distance to detect unmapped properties with similar names.
- **Smart-Match IDE Code-Fix**: Integrated Roslyn Code-Fix provider that suggests valid property mappings directly in the IDE and automatically applies the `[MapProperty]` attribute.
- **Performance Guardrails (AM0016)**: New diagnostic to warn when manual collection mapping logic (e.g. LINQ `.Select`) is used in a way that prevents compiler loop vectorization.
- **Explicit Key Management**: New `[AutoMappicKey]` attribute in `AutoMappic.Core` to manually designate primary keys for entity synchronization when naming conventions are ambiguous.
- **Explicit Mapping Overrides**: New `[MapProperty]` attribute to override default convention-based mapping for individual properties.
- **Configurable Thresholds**: Support for `<AutoMappic_SmartMatchThreshold>` and `<AutoMappic_EnableEntitySync>` MSBuild properties to fine-tune analyzer sensitivity and opt-in to advanced features.

### Breaking Changes
- **In-Place Collection Updates**: Standard collection mapping for entities with identifiable keys now uses **Smart-Sync**. This preserves object references for matched IDs instead of recreating them. Applications relying on complete instance replacement for nested collections must now explicitly configure "Clear-and-Add" via `MappingContext`.
- **Strict XML Enforcement**: Removed `#pragma warning disable CS1591` from generated source. All generated mapping methods now include formal XML Documentation.

### Fixed
- **Analyzer Stability**: Improved diagnostic reporting accuracy for ambiguous entity keys (AM0017).
- **Type Casting Safety**: Standardized null-coalescing fallbacks to `(default!)` to resolve `CS8601` warnings in strict .NET 10 environments.

## [0.4.0] - 2026-03-26

### Added
- **Identity Management (Graph Mapping)**: Opt-in MSBuild property `<AutoMappic_EnableIdentityManagement>true` enables entity-aware mapping with `MappingContext` for tracking object instances and preventing cyclic recursion.
- **Smart Key Inference**: The source generator automatically detects primary keys (`Id`, `ClassNameId`, `[Key]`) on destination types when identity management is active.
- **Collection Syncing (Diffing Engine)**: When identity management is active, collection mapping uses a key-based "Match-and-Sync" algorithm instead of "Clear-and-Add", preserving EF Core change tracker state.
- **Conditional Patch Mode (Null-Ignore)**: With identity management active, property assignments are wrapped in `if (source.Prop != null)` checks, enabling seamless HTTP PATCH support.
- **AM0013 Diagnostic**: New build-time warning when patching a `required` destination property from a nullable source without a default fallback.
- **Static Converters**: New `[AutoMappicConverter]` attribute allows developers to define zero-allocation static conversion methods that the generator delegates to directly.
- **Shallow Clone Optimization**: `Map<T, T>` generates efficient property-by-property copy for same-type mappings.

### Changed
- **MappingContext Parameter**: All generated mapping extension methods now accept an optional `MappingContext?` parameter for identity tracking. This is backward-compatible as it defaults to `null`.
- **Collection Helper Variables**: Internal loop variable in collection helpers changed from `element` to `x` for consistency with LINQ expressions.

### Fixed
- **ProjectTo Context Scope**: Fixed an issue where `MappingContext` references in nested collection expressions caused compilation errors in IQueryable `ProjectTo` expression trees.
- **Read-Only Collection In-Place Mapping**: Fixed a missing closing brace in generated code for in-place mapping of read-only collection properties.

## [0.3.0] - 2026-03-21

### Added
- **Multi-Targeting Support**: Explicitly added support for both .NET 9 and .NET 10 to ensure first-class performance on the latest .NET SDKs.
- **Enhanced ProjectTo Interception**: Upgraded the source generator to handle ProjectTo calls with additional arguments (e.g. IConfigurationProvider), enabling seamless migration for CleanArchitecture-style projects.
- **AutoMapper Bridge**: Added an AddAutoMapper extension method and IConfigurationProvider interface to AutoMappic.Core to provide 1:1 API compatibility for migrating projects.
- **Ambiguity Resolution**: Refined the mapping convention engine to prioritize direct property matches over flattened paths, resolving internal conflicts (AM0002) in complex domain models.
- **Full Diagnostic Suite**: Expanded build-time safety checks to include AM0001 through AM0009, covering duplicate mappings and symbol resolution failures.
- **Professional Documentation**: Comprehensive overhaul of README and VitePress tutorials, drawing inspiration from the Mediator project for clarity and precision.

### Fixed
- **Interceptor Signature Mismatches**: Resolved CS9144 errors when intercepting methods with variable argument counts.
- **XML Documentation**: Added missing XML comments to all public APIs to satisfy high-quality build requirements.
- **Standardized Encoding**: Replaced all emojis and non-ASCII characters with professional ASCII equivalents across the entire repository.

## [0.2.0] - 2026-03-18

### Added
- **Zero-LINQ Collection Mapping:** Implemented high-performance, statically generated `for` loops for list and array mapping, bypassing `System.Linq` overhead.
- **Pre-allocation Optimization:** Automatically sizes destination collections (`List<T>` with capacity or `new T[]`) to eliminate unnecessary Gen 0 allocations.
- **Improved AM0001 Diagnostics:** Enhanced "Unmapped Destination" detection to intelligently handle `[Required]` and C# 11 `required` modifiers.
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
- **Diagnostic Analyzers:** Ships with build-time guards (`AM0001`, `AM0002`) to help developers catch mapping mistakes immediately within the IDE.
- **Native AOT & Trimming Support:** Initial release is 100% compatible with Native AOT, featuring robust handling for `object` mappings and null-conditional flattened paths.
- **Transitive Analyzer Support:** Improved NuGet packaging ensuring the source generator is correctly referenced by all consumer projects.
