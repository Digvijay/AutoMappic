# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.0] - 2026-03-16

### Added
- **Initial Release:** Welcome to AutoMappic! 🚀
- **Source Generator Engine:** High-performance mapping via C# 12 Interceptors, completely bypassing `System.Reflection` at runtime.
- **Convention-based Mapping:** Automatic name matching between source and destination types.
- **PascalCase Flattening:** Automatically resolves things like `Order.Customer.Name` onto `OrderDto.CustomerName`.
- **Bidirectional Mapping:** Added `.ReverseMap()` functionality to automatically generate two-way mappings from a single `CreateMap` configuration.
- **Explicit Member Overrides:** Support for `.ForMember(dest => dest.Prop, opt => opt.MapFrom(src => src.OtherProp))`.
- **Dependency Injection:** Seamless configuration via `services.AddAutoMappic(typeof(Program).Assembly)` to drop-in replace existing enterprise architectures.
- **Complex Hierarchies:** Deep list and dictionary (`Dictionary<TKey, TValue>`) projection generation natively mapped into arrays or generic lists.
- **Enterprise Proven:** Fully verified against complex base classes and manually mapped DTOs in the official Microsoft `eShopOnWeb` architecture.
- **Diagnostic Analyzers:** Ships with build-time guards (`AM001`, `AM002`) to help developers catch mapping mistakes immediately within the IDE.
