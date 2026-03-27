# Diagnostic Suite

AutoMappic transforms the traditional runtime "configuration validation" phase into a series of rigorous, build-time static analysis passes. This ensures that mapping inconsistencies are caught during development, effectively eliminating a broad category of runtime exceptions and ensuring Native AOT safety.

## Structural Validation (Errors)

A diagnostic with **Error** severity will cause the build to fail. This is intentional: AutoMappic prioritizes application correctness.

### AM0001: Unmapped Destination Property
*   **Severity**: Error
*   **Target**: Destination Member
*   **Description**: Emitted when a writable property on the destination type has no matching source (via convention or explicit ForMember configuration). 
*   **Note**: If a property is marked with the [Required] attribute or the C# 11 required modifier, the error message will specifically highlight this to ensure non-nullability constraints are respected.
*   **Remediation**: 
    1.  Add a matching source property.
    2.  Use .ForMember() to specify a source path.
    3.  Use .ForMemberIgnore() to explicitly exclude the property.

### AM0002: Ambiguous Property Mapping
*   **Severity**: Error
*   **Target**: Destination Member
*   **Description**: Occurs when a destination property name matches both a direct source property and a flattened path (e.g., CustomerName vs Customer.Name). AutoMappic prioritizes the direct match but warns when a collision exists.
*   **Remediation**: Use .ForMember() to explicitly declare the intended mapping source.

### AM0005: Missing Parameterless Constructor
*   **Severity**: Error
*   **Target**: Destination Type
*   **Description**: AutoMappic requires a public constructor to instantiate the destination type. It can use a parameterless constructor or a parameterized one if all its arguments can be resolved from the source by name convention or explicit mapping.
*   **Remediation**: Ensure the destination class has a public parameterless constructor or parameters that match source properties.

### AM0006: Circular Reference Detected
*   **Severity**: Error
*   **Target**: Type Pair
*   **Description**: AutoMappic's static generator does not support recursive object graphs (e.g., Parent -> Child -> Parent) by default, as they would cause StackOverflowException in the generated static code.
*   **Remediation**: Use .ForMemberIgnore() on the property causing the circular path.

## Operational Awareness (Warnings)

Warnings identify potential configuration issues that do not block code generation but may result in unexpected behavior or fallback to less efficient engines.

### AM0003: Misplaced CreateMap Call
*   **Severity**: Warning
*   **Target**: Call Site
*   **Description**: Emitted when CreateMap is called outside of a Profile subclass constructor.
*   **Analysis**: The source generator only processes mappings declared within Profile constructors. Calls in other locations are ignored by the generator, though they may still function in the reflection-based fallback engine at runtime.

### AM0004: Unresolved Interceptor Mapping
*   **Severity**: Warning
*   **Target**: Call Site
*   **Description**: An IMapper.Map call was detected for a type pair that has no corresponding source-generated mapping.
*   **Impact**: The call will fall back to the runtime Mapper engine, which utilizes reflection and is not optimized for Native AOT environments.

### AM0007: Unresolved CreateMap Symbol
*   **Severity**: Warning
*   **Target**: Call Site
*   **Description**: The generator found a CreateMap call but could not resolve its symbol.
*   **Impact**: This usually means the project is missing a reference to AutoMappic.Core or there are compilation errors preventing semantic analysis.

### AM0008: Unsupported ProjectTo Feature
*   **Severity**: Warning
*   **Target**: Call Site
*   **Description**: A ProjectTo call was detected using a mapping that contains procedural logic (e.g., BeforeMap, AfterMap) that cannot be translated to SQL.
*   **Impact**: The query would fail at runtime; AutoMappic warns you about this at compile-time to prevent production errors.

### AM0009: Duplicate Mapping Configuration
*   **Severity**: Warning
*   **Target**: Type Pair
*   **Description**: The same source-to-destination type pair is configured across multiple different profiles.
*   **Analysis**: AutoMappic will only generate a single interceptor for the first discovered configuration.

### AM0010: Performance Hotpath Detected
*   **Severity**: Info
*   **Target**: Mapping Path
*   **Description**: Identifies deeply nested collection mappings that may lead to high allocation pressure in performance-critical paths.
*   **Advice**: Consider flattening the models or using a specialized DTO to reduce GC load.

### AM0011: Multi-Source ProjectTo
*   **Severity**: Error
*   **Target**: ProjectTo Call
*   **Description**: Database projections currently only support mapping from a single entity source.
*   **Remediation**: Use in-memory mapping or a manual Select tree for complex multi-source LINQ queries.

### AM0012: Asymmetric Mapping Configuration
*   **Severity**: Warning
*   **Target**: Mapping Configuration
*   **Description**: Emitted when a mapping is configured between two types, but zero property assignments or constructor arguments are actually generated.
*   **Analysis**: This usually indicates that the source and destination members have conflicting accessibilities (e.g., all destination properties are private or read-only) or that no members match our naming conventions.
*   **Remediation**: 
    1.  Ensure destination properties have public/internal setters.
    2.  Use .ForMember() to bridge non-convention matches.
    3.  Check if the target type is intended to be a marker class.

### AM0013: Required Property Patch Mismatch
*   **Severity**: Warning
*   **Target**: Property Mapping
*   **Description**: Emitted when Identity Management is active (`<AutoMappic_EnableIdentityManagement>true`) and a destination property marked with C# 11's `required` modifier is being mapped from a nullable source property without a default fallback. In Patch Mode, nullable sources generate conditional assignments (`if (source.Prop != null)`), which means the `required` destination property may never be assigned.
*   **Impact**: Silent data corruption -- the destination object may be created with an uninitialized required field, violating the type's invariants.
*   **Remediation**: 
    1.  Provide a non-nullable source property.
    2.  Use `.ForMember(d => d.Prop, opt => opt.MapFrom(src => src.Prop ?? "default"))` to guarantee a value.
    3.  Remove the `required` modifier if the property is intentionally optional during patching.
