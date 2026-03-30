# Porting from AutoMapper to AutoMappic

AutoMappic was designed to be a high-performance, Native AOT-friendly replacement for AutoMapper. Transitioning an existing codebase is usually as simple as swapping the namespace and enabling Roslyn Interceptors.

## Why Migrate?

*   **Native AOT Ready**: AutoMapper relies heavily on reflection and dynamic code generation (`System.Reflection.Emit`), which are incompatible with Native AOT. AutoMappic generates all mapping logic as static C# classes at compile time.
*   **Startup Performance**: AutoMappic eliminates the need for reflection-based profile scanning at startup.
*   **Compile-Time Verification**: Potential mapping issues (ambiguous paths, missing members) are reported as compiler diagnostics rather than runtime exceptions.

---

## 1. NuGet Package Replacement

In your project file (`.csproj`), remove the AutoMapper packages and add AutoMappic.

```xml
<ItemGroup>
  <!-- Remove these -->
  <!-- <PackageReference Include="AutoMapper" Version="..." /> -->
  <!-- <PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="..." /> -->

  <!-- Add this -->
  <PackageReference Include="AutoMappic" Version="0.5.0" />
</ItemGroup>
```

## 2. Enable Interceptors

AutoMappic uses **Roslyn Interceptors** to achieve its zero-reflection magic. You must enable the interceptors namespace in your project file so the compiler can reroute `mapper.Map` calls to the generated code.

```xml
<PropertyGroup>
  <InterceptorsNamespaces>$(InterceptorsNamespaces);AutoMappic.Generated</InterceptorsNamespaces>
</PropertyGroup>
```

## 3. Global Usings

If your project uses C# `GlobalUsings.cs`, update it to point to AutoMappic. AutoMappic mirrors the standard AutoMapper API surface (`IMapper`, `Profile`, `CreateMap`, etc.).

```csharp
// GlobalUsings.cs
global using AutoMappic;
// global using AutoMapper.QueryableExtensions; // No longer needed, included in AutoMappic
```

## 4. Dependency Injection

### The Compatible Way (Bridge)
AutoMappic includes an `AddAutoMapper` extension for `IServiceCollection` to keep your existing registration code working:

```csharp
// Still works!
builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());
```

### The Optimized Way (Native AOT)
For the best performance and smallest binary size, use the source-generated registration method. This method is generated at compile-time and contains a hard-coded list of all your profiles.

```csharp
// Recommended
builder.Services.AddAutoMappic();
```

---

## 5. Feature Compatibility

| AutoMapper Feature | AutoMappic Equivalent | Status |
| :--- | :--- | :--- |
| `CreateMap<S, D>()` | `CreateMap<S, D>()` | Supported |
| `ForMember(opt => ...)` | `ForMember(opt => ...)` | Supported |
| `ReverseMap()` | `ReverseMap()` | Supported |
| `ProjectTo<T>()` | `.ProjectTo<T>()` | Supported (Intercepted) |
| `BeforeMap / AfterMap` | `.BeforeMap() / .AfterMap()` | Supported |
| `ITypeConverter` | `ConvertUsing<T>()` | Supported |
| `ValueResolver` | `MapFrom<TResolver>()` | Supported |

## Troubleshooting Common Issues

### AM0002: Ambiguous Mapping
AutoMappic is stricter than AutoMapper regarding ambiguous paths. If a property `UserCity` on a DTO could be mapped from either a direct property `UserCity` or a flattened property `User.City`, AutoMappic will prioritize the direct match but may warn you if it's unclear.

### AM0008: Unsupported ProjectTo Feature
Because `ProjectTo` generates Entity Framework-compatible `Expression` trees, it does not support procedural logic like `BeforeMap` or `AfterMap`. If your mapping uses these, AutoMappic will report a diagnostic suggesting you use `Map<T>` instead of `ProjectTo<T>`.
