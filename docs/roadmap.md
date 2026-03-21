# AutoMappic Roadmap & Known Limitations

AutoMappic is a high-performance, AOT-friendly code generator designed to replace AutoMapper with zero-reflection static code.

## Known Limitations (v0.3.0 Stable)

- **Circular Reference Enforcement**: AutoMappic prioritizes safety by preventing infinite recursion disasters. Circular references are detected at compile-time and reported as an error (AM006). You must manually break cycles via `ForMemberIgnore` to maintain static code integrity.
- **Advanced LINQ Translation in ProjectTo**: While standard property projection and flattening are robust, complex custom expressions inside a profile (e.g., custom I/O or complex logic blocks) may not always translate correctly to every ORM provider (EF Core / Dapper / NHibernate).
- **Struct Support**: Basic struct mapping is supported, but complex nested struct-to-class hierarchies are less optimized than class-to-class mapping in the current release.

## Roadmap

### Phase 1: DX & Performance Hardening (Current)
- [x] **Zero-LINQ Collection Mapping**: Use high-performance `for` loops with pre-allocated capacity for lists and arrays to reduce GC pressure.
- [x] **Compile-time Cycle Detection**: Prevents infinite recursion disasters before the app even runs.
- [x] **Immediate IDE Feedback**: Leveraging Incremental Generators for as-you-type diagnostics.
- [x] **High-Performance DataReader mapping**: Optimized mapping from `IDataReader` straight to DTOs via generated ordinal lookups.

### Phase 2: Advanced Mapping Features
- [x] **Constructor Mapping**: Support mapping source properties to destination constructor arguments.
- [x] **Global Value Converters**: Define reusable conversion logic for common type pairs (e.g. `DateTime` to `string`).
- [x] **Naming Conventions**: Better support for custom naming strategies beyond PascalCase and snake_case.
- [x] **Open Generics**: Support `CreateMap(typeof(Source<>), typeof(Dest<>))`.

### Phase 3: Ecosystem & Tooling
- [x] **Validation CLI**: A standalone tool (`automappic validate`) to verify all mappings in a CI/CD pipeline.
- [x] **Dependency Injection Overhaul**: Support for multiple/named mappers and better scoped lifetime management via Keyed Services.
- [x] **Visualizer**: A tool (`automappic visualize`) to generate Mermaid graphs of the mapping architecture.

### Phase 4: Advanced Optimizations & Parity (v0.3.0 Release)
- [x] **ReverseMap Lifecycle**: Support full configuration (like `ForMember`) for the reversed direction.
- [x] **Async Value Resolvers**: Enable true asynchronous resolution during mapping for I/O-bound properties.
- [x] **MapAsync Multi-overload Support**: Full parity with synchronous `Map` for both instance mapping and in-place updates.
- [x] **Deep Open Generics**: Support recursive member mapping within `CreateMap(typeof(S<>), typeof(D<>))`.

### Phase 5: Production Readiness (v0.3.0 Stable)
- [x] **Production Grade Diagnostics**: All AM001-AM012 diagnostics are verified and stable.
- [x] **Native AOT & Trimming**: 100% compatibility across all mapping patterns.
- [x] **High-Frequency Parity**: Performance throughput matches manual code for all primitive and object-level mappings.

## Upcoming & Experimental
- [ ] **High-Performance Memory Buffers**: Optional pooling for collection mapping to further reduce GC allocations during peak traffic (planned for v0.4.0).
- [ ] **Interactive Visualizer (CLI + Web)**: Explore mapping graphs and real-time performance metrics via a visual dashboard.
- [ ] **Advanced AOT Hardening**: Further refining the edge cases of generic resolution in highly aggressive trimming environments.
- [ ] **Native SQLite/PostgreSQL optimizations**: Specific generated code paths for high-speed IDataReader mapping.
