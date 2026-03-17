# Reference Apps & Case Studies

To validate the robustness of AutoMappic’s compile-time interception, we maintain several reference implementations ranging from foundational demonstrations to full-scale enterprise migrations.

## 1. Foundation: SampleApp
*   **Location**: `samples/SampleApp`
*   **Purpose**: Demonstrates the core capabilities of the library in a modern .NET 9 environment.
*   **Key Features**:
    *   **Cross-Project Discovery**: Showcases how `AddAutoMappic()` chains registration across project boundaries without reflection.
    *   **PascalCase Flattening**: Validates the automatic resolution of deep object graphs.
    *   **Native AOT Profile**: Configured for verification of zero-reflection execution paths.

## 2. Drop-in Compatibility: eShopOnWeb Migration
*   **Location**: `samples/eShopOnWebWin`
*   **Source**: [Microsoft eShopOnWeb Reference Architecture](https://github.com/dotnet-architecture/eShopOnWeb)
*   **Purpose**: Proves that AutoMappic can serve as an immediate replacement for AutoMapper in legacy enterprise systems.
*   **Case Study**: We replaced the AutoMapper dependency in the Web and Core projects with AutoMappic. By maintaining identical `Profile` and `ForMember` syntax, the migration required zero changes to the underlying mapping business logic, successfully resolving complex `CatalogItem` and `CatalogBrand` mappings at compile-time.

## 3. High-Performance Parity: Modern eShop 
*   **Location**: `samples/eShopModernWin`
*   **Source**: [Modern dotnet/eShop Aspire Version](https://github.com/dotnet/eShop)
*   **Purpose**: Evaluates AutoMappic against the "Gold Standard" of manual object assignment.
*   **Context**: The modern `dotnet/eShop` architecture avoids mappers entirely in favor of manual mapping to ensure 100% Native AOT compliance and maximum throughput.
*   **Success Metric**: AutoMappic achieved bit-for-bit parity with the manual implementation while reducing several hundred lines of boilerplate code into a single, centralized configuration.
