# Diagnostic Suite

AutoMappic transforms the traditional runtime "configuration validation" phase into a series of rigorous, build-time static analysis passes. This ensures that mapping inconsistencies are caught during development, effectively eliminating a broad category of runtime exceptions.

## Structural Validation (Errors)

A diagnostic with **Error** severity will cause the build to fail. This is intentional: AutoMappic prioritizes application correctness and Native AOT safety.

### AM001: Unmapped Destination Property
*   **Severity**: Error
*   **Target**: Destination Member
*   **Description**: Emitted when a writable property on the destination type has no matching source (via convention or explicit `ForMember` configuration). 
*   **Special Case**: If a property is marked with the `[Required]` attribute or the C# 11 `required` modifier, the error message will specifically highlight this to ensure non-nullability constraints are respected.
*   **Remediation**: 
    1.  Add a matching source property.
    2.  Use `.ForMember()` to specify a source path.
    3.  Use `.ForMemberIgnore()` to explicitly exclude the property from mapping.

### AM002: Ambiguous Property mapping
*   **Severity**: Error
*   **Target**: Destination Member
*   **Description**: Occurs when a destination property name matches both a direct source property and a flattened path (e.g., `CustomerName` vs `Customer.Name`).
*   **Remediation**: Use `.ForMember()` to explicitly declare the intended mapping source, resolving the ambiguity.

### AM005: Missing Parameterless Constructor
*   **Severity**: Error
*   **Target**: Destination Type
*   **Description**: AutoMappic requires a public constructor to instantiate the destination type. It can use a parameterless constructor or a parameterized one (for Records/DDD) if all its arguments can be resolved from the source by name convention or explicit mapping.
*   **Remediation**: Ensure the destination class has a public parameterless constructor or parameters that match source properties.

### AM006: Circular Reference Detected
*   **Severity**: Error
*   **Target**: Type Pair
*   **Description**: AutoMappic's static generator does not support recursive object graphs (e.g., Parent $\to$ Child $\to$ Parent) by default, as they would cause `StackOverflowException` in the generated static code and require expensive runtime object trackers.
*   **Remediation**: 
    1. Use `.ForMemberIgnore()` on the property causing the recursion.
    2. Use a custom `IValueResolver` or `AfterMap` hook to manually handle the back-reference after the primary mapping is complete.

## Developer Experience (Warnings)

Warnings identify potential configuration issues that do not necessarily block code generation but may result in unexpected behavior or fallback to less efficient engines.

### AM003: Misplaced CreateMap Call
*   **Severity**: Warning
*   **Target**: Call Site
*   **Description**: Emitted when `CreateMap<S, D>` is called outside of a `Profile` subclass constructor.
*   **Analysis**: The Source Generator only processes mappings declared within Profile constructors. Calls in other locations are ignored by the generator, though they may still function in the reflection-based fallback engine.

### AM004: Unresolved Interceptor Mapping
*   **Severity**: Warning
*   **Target**: Call Site
*   **Description**: An `IMapper.Map` call was detected for a type pair that has no corresponding source-generated mapping.
*   **Impact**: The call will fall back to the runtime `Mapper` engine, which utilizes reflection and is not optimized for Native AOT environments.

### AM007: Unresolved CreateMap Symbol
*   **Severity**: Warning
*   **Target**: Call Site
*   **Description**: The generator found a `CreateMap` call but could not resolve its symbol. 
*   **Impact**: This usually means the project is missing a reference to `AutoMappic.Core` or there are compilation errors preventing semantic analysis.
