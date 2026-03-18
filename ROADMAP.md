# AutoMappic Roadmap & Known Limitations

AutoMappic is a high-performance, AOT-friendly code generator designed to replace AutoMapper with zero-reflection static code.

## Known Limitations (v0.1)

- **Parameterless Constructor Required**: Currently, the destination type MUST have a public parameterless constructor. Constructor mapping (injecting parameters) is not yet supported.
- **Circular Reference Restrictions**: Circular references are detected at compile-time and reported as an error (AM006) to prevent StackOverflow. You must manually break the cycle using `ForMemberIgnore` or a custom resolver.
- **Limited Custom Converters**: While `MapFrom` and `IValueResolver` are supported, complex custom type converters that apply globally are not yet implemented.
- **IQueryable.ProjectTo limitations**: Projection currently supports simple property mappings and flattening; complex expressions inside the mapping profile may not always translate perfectly to every ORM provider.
- **Structs**: Mapping to or from `struct` types is partially supported but less tested than `class` types.

## Roadmap

### Phase 1: DX & Performance Hardening (Current)
- [x] **Zero-LINQ Collection Mapping**: Use high-performance `for` loops with pre-allocated capacity for lists and arrays to reduce GC pressure.
- [x] **Compile-time Cycle Detection**: Prevents infinite recursion disasters before the app even runs.
- [x] **Immediate IDE Feedback**: Leveraging Incremental Generators for as-you-type diagnostics.

### Phase 2: Advanced Mapping Features
- [x] **Constructor Mapping**: Support mapping source properties to destination constructor arguments.
- [x] **Global Value Converters**: Define reusable conversion logic for common type pairs (e.g. `DateTime` to `string`).
- [x] **Naming Conventions**: Better support for custom naming strategies beyond PascalCase and snake_case.
- [x] **Open Generics**: Support `CreateMap(typeof(Source<>), typeof(Dest<>))`.

### Phase 3: Ecosystem & Tooling
- [ ] **Validation CLI**: A standalone tool to verify all mappings in a CI/CD pipeline without full compilation.
- [ ] **Visualizer**: A tool to visualize the mapping graph and identify bottleneck or complex paths.
