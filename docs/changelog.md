# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.0] - 2026-03-19

### Added
- **Asynchronous Mapping Support:** Introduced `MapAsync` and `IAsyncValueResolver<TSource, TMember>` to support non-blocking, I/O-bound mapping workflows without thread-pool starvation.
- **Dual-Emission Generator:** The source generator now emits both synchronous and asynchronous versions of every mapping, allowing for optimal call-site interception.
- **Enhanced `ReverseMap` Configuration:** Expanded `.ReverseMap()` to support chained member configurations, making it easier to manage complex bidirectional mapping rules.
- **Deep Open Generics:** Support for recursive member mapping within unbounded generic types like `PagedList<T>`.
- **Project-to-Project Discovery:** Refined static registration system to ensure zero-reflection metadata propagation across multi-assembly solutions.
- **Advanced Collection Buffering:** Initial support for pre-allocated list capacity to further reduce GC allocations when mapping large collections.

### Fixed
- **Top-level Collection Interception:** Fixed a critical performance bug where direct `mapper.Map<List<D>>(listS)` calls were silently falling back to runtime reflection.
- **Async Collection Signatures:** Resolved build-time signature mismatches when intercepting `MapAsync<List<D>>` for collections containing synchronous child mappings.
- **Identifier Sanitization:** Unified the sanitization logic across the entire generator pipeline to ensure consistent interceptor matching for complex generics and arrays.
- **Collection Shim Type-Safety:** Fixed incorrect type-casting logic in generated collection shims that prevented successful compilation for certain collection-based mappings.
- **Interceptor Signature Matching:** Resolved "Signature mismatch" errors by implementing an explicit tracking flag for destination-mapped (`Map(S, D)`) calls.
- **Nullable Type Mapping:** Fixed type conversion warnings for when mapping between nullable and non-nullable value types.
- **Ambiguity Detection:** Improved AM002 diagnostics to prevent false positives when direct property matches exist alongside potential flattening paths.
- **Documentation Rendering:** Corrected LaTeX-style Markdown symbols (`$\to$`) that were incorrectly displayed in the VitePress tutorials.

## [0.1.0] - 2026-03-16

### Added
- **Initial Release:** Welcome to AutoMappic!
- **Source Generator Engine:** High-performance mapping via C# 12 Interceptors, completely bypassing `System.Reflection` at runtime.
- **Convention-based Mapping:** Automatic name matching between source and destination types.
- **PascalCase Flattening:** Automatically resolves things like `Order.Customer.Name` onto `OrderDto.CustomerName`.
- **Bidirectional Mapping:** Added `.ReverseMap()` functionality to automatically generate two-way mappings from a single `CreateMap` configuration.
- **Explicit Member Overrides:** Support for `.ForMember(dest => dest.Prop, opt => opt.MapFrom(src => src.OtherProp))`.
- **Dependency Injection:** Seamless configuration via `services.AddAutoMappic()`. Profiles are discovered at compile-time across your entire solution for zero-reflection startup.
- **Complex Hierarchies:** Deep list and dictionary (`Dictionary<TKey, TValue>`) projection generation natively mapped into arrays or generic lists.
- **Enterprise Proven:** Fully verified against complex base classes and manually mapped DTOs in the official Microsoft `eShopOnWeb` architecture.
- **Diagnostic Analyzers:** Ships with build-time guards (`AM001`, `AM002`) to help developers catch mapping mistakes immediately within the IDE.
- **Native AOT & Trimming Support:** Initial release is 100% compatible with Native AOT, featuring robust handling for `object` mappings and null-conditional flattened paths.
- **Transitive Analyzer Support:** Improved NuGet packaging ensuring the source generator is correctly referenced by all consumer projects.
