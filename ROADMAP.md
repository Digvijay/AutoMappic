# AutoMappic Roadmap & Known Limitations

AutoMappic is a high-performance, AOT-friendly code generator designed to replace AutoMapper with zero-reflection static code.

## Known Limitations (v0.5.0 Stable)

- **Circular Reference Enforcement**: AutoMappic prioritizes safety by preventing infinite recursion disasters. Circular references are detected at compile-time and reported as an error (AM0006). You must manually break cycles via `ForMemberIgnore` to maintain static code integrity.
- **Advanced LINQ Translation in ProjectTo**: While standard property projection and flattening are robust, complex custom expressions inside a profile (e.g., custom I/O or complex logic blocks) may not always translate correctly to every ORM provider (EF Core / Dapper / NHibernate).
- **Struct Support**: Basic struct mapping is supported, but complex nested struct-to-class hierarchies are less optimized than class-to-class mapping in the current release.

## Roadmap

### Phase 1: DX & Performance Hardening (Completed)
- [x] **Zero-LINQ Collection Mapping**: Use high-performance `for` loops with pre-allocated capacity for lists and arrays to reduce GC pressure.
- [x] **Compile-time Cycle Detection**: Prevents infinite recursion disasters before the app even runs.
- [x] **Immediate IDE Feedback**: Leveraging Incremental Generators for as-you-type diagnostics.
- [x] **High-Performance DataReader mapping**: Optimized mapping from `IDataReader` straight to DTOs via generated ordinal lookups.

### Phase 2: Advanced Mapping Features (Completed)
- [x] **Constructor Mapping**: Support mapping source properties to destination constructor arguments.
- [x] **Global Value Converters**: Define reusable conversion logic for common type pairs (e.g. `DateTime` to `string`).
- [x] **Naming Conventions**: Better support for custom naming strategies beyond PascalCase and snake_case.
- [x] **Open Generics**: Support `CreateMap(typeof(Source<>), typeof(Dest<>))`.

### Phase 3: Ecosystem & Tooling (Completed)
- [x] **Validation CLI**: A standalone tool (`automappic validate`) to verify all mappings in a CI/CD pipeline.
- [x] **Dependency Injection Overhaul**: Support for multiple/named mappers and better scoped lifetime management via Keyed Services.
- [x] **Visualizer**: A tool (`automappic visualize`) to generate Mermaid graphs of the mapping architecture.

### Phase 4: Persistence & Productivity (v0.5.0 Release)
- [x] **Smart-Sync (EF Core Identity Mapping)**: Key-based collection synchronization that preserves existing entity references.
- [x] **Fuzzy-Match (Smart-Match Analyzer)**: Built-in string similarity engine (AM0015) to suggest unmapped properties.
- [x] **IDE Code-Fix Integration**: Roslyn Code-Fixes to automatically apply suggested mappings via lightbulb actions.
- [x] **Performance Regression Diagnostics**: Build-time warnings (AM0016) for custom collection logic that breaks vectorization.
- [x] **Explicit Attribute Mapping**: Support for `[MapProperty]` and `[AutoMappicKey]` for fine-grained configuration.

### Phase 5: Advanced Optimizations & Parity (Upcoming)
- [ ] **High-Performance Memory Buffers**: Optional pooling for collection mapping to further reduce GC allocations during peak traffic.
- [ ] **Interactive Visualizer (CLI + Web)**: Explore mapping graphs and real-time performance metrics via a visual dashboard.
- [ ] **Native SQLite/PostgreSQL optimizations**: Specific generated code paths for high-speed IDataReader mapping.
- [ ] **Native AOT Hardening**: Further refining the edge cases of generic resolution in highly aggressive trimming environments.

## Upcoming & Experimental
- [ ] **Hot-Reload Support**: Instant mapping regeneration during active debugging sessions.
- [ ] **Source-to-Source Refactoring**: Tooling to migrate manual mapping code to AutoMappic profiles automatically.
- [ ] **Wasm/Blazor Optimizations**: Specialized code paths for client-side web assembly environments.
