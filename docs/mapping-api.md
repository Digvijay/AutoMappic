# Mapping API Reference

AutoMappic provides a fluent, type-safe API for configuring object-to-object mappings. All configurations are declared within a `Profile` constructor and are used by the Roslyn source generator to emit statically-optimized C# code.

## 1. Object Creation

### ConstructUsing
Defines a custom factory for the destination type. This replaces the default constructor call in the generated code.

```csharp
CreateMap<Order, OrderDto>()
    .ConstructUsing(src => new OrderDto(src.OrderId, DateTime.UtcNow));
```

## 2. Member Mapping

### ForMember
The primary way to override default conventions for a specific destination property.

```csharp
CreateMap<User, UserDto>()
    .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
```

### MapFrom (Custom Logic)
Redirects the source of a destination property.
- **Lambda Expressions:** Fully supported for direct assignments.
- **Value Resolvers:** Use `opt.MapFrom<TResolver>()` for complex logic that requires DI services. AutoMappic generates a direct instantiation of your resolver, ensuring zero-reflection overhead.

### Condition
Gates a property assignment with a predicate. If the condition is not met, the property assignment is skipped in the generated code.

```csharp
CreateMap<Product, ProductDto>()
    .ForMember(dest => dest.Price, opt => opt.Condition((src, dest) => src.IsVisible));
```

### Ignore
Explicitly prevents a property from being mapped. This is required for properties that have no source match to satisfy the **AM0001** (Unmapped Property) diagnostic.

```csharp
CreateMap<Employee, EmployeeDto>()
    .ForMemberIgnore(dest => dest.InternalSecret);
```

## 3. Directional Mapping

### ReverseMap
Automatically generates a mapping in the opposite direction (`TDestination` -> `TSource`). You can continue the fluent chain to add specific overrides for the reverse direction.

```csharp
CreateMap<User, UserDto>()
    .ReverseMap()
    .ForMember(src => src.InternalId, opt => opt.Ignore());
```

## 4. Lifecycle Hooks

### BeforeMap / AfterMap
Executes custom logic before or after the property assignment phase.
- **Synchronous:** `.BeforeMap((src, dest) => ...)`
- **Asynchronous:** `.BeforeMapAsync(async (src, dest) => ...)`
- **Interceptors:** When using these hooks, AutoMappic generates an internal wrapper to ensure they are executed in the correct sequence within the intercepted call.

## 5. Projections and Collections

### Zero-LINQ Collections
AutoMappic automatically handles `List<T>`, `T[]`, and `IEnumerable<T>` using specialized generators:
- **Loop Emission**: Emits a standard `for` loop (or `foreach` for iterables) instead of calling LINQ methods.
- **Pre-allocation**: Uses `new List<T>(count)` or `new T[count]` to optimize memory pressure when the source size is known at runtime.

### ProjectTo (EF Core)
Converts an `IQueryable<T>` into an `IQueryable<U>` at compile-time.
```csharp
// Standard usage
var dtos = dbContext.Users.ProjectTo<UserDto>(_mapper.ConfigurationProvider);
```

### DataReader.Map
High-performance, non-reflective projection from an ADO.NET `IDataReader`.
```csharp
using var reader = cmd.ExecuteReader();
var users = reader.Map<UserDto>();
```

## 6. Advanced v0.5.0 Features

AutoMappic v0.5.0 introduces state-aware mapping for complex scenarios like EF Core entity synchronization and cyclic graphs.

### Identity Management
Ensures that the same source object instance is always mapped to the same destination instance within a single mapping tree, preventing infinite recursion in cyclic models.

```csharp
public class MyProfile : Profile
{
    public MyProfile()
    {
        // Globally enable for this profile
        EnableIdentityManagement = true;
        
        CreateMap<Category, CategoryDto>();
    }
}
```

### Entity Smart-Sync (Match-and-Update)
When mapping to an existing collection on a destination entity, AutoMappic matches items by their Primary Key (`Id` or `[AutoMappicKey]`). 
- **Match Found**: Updates the existing instance in-place (preserving EF Core Change Tracker state).
- **No Match**: Adds a new instance to the collection.
- **Removed**: Removes the instance from the collection.

```csharp
public class OrderProfile : Profile
{
    public OrderProfile()
    {
        // Enable differential collection syncing
        EnableEntitySync = true;
        
        CreateMap<Order, OrderEntity>();
    }
}
```

## 7. Choosing Your Mapping Path

AutoMappic offers two ways to invoke generated code. Understanding the difference is key to long-term architectural success.

### Path A: Automatic Interception (Recommended)
This is the default path. You use the standard `IMapper` interface exactly like you would with AutoMapper.

```csharp
using AutoMappic;
// ...
var dto = mapper.Map<User, UserDto>(source);
```

*   **How it works**: At compile-time, a **Roslyn Interceptor** Diverts this call to a highly optimized static method. 
*   **Namespace Required**: `using AutoMappic;`
*   **Benefit**: 100% syntactical compatibility with existing codebases. No changes required to your business logic.

### Path B: Explicit Extension Methods (High Performance)
For scenarios where every microsecond counts (e.g., tight loops or Native AOT hot paths), you can call the generated code directly as an extension method on your source object.

```csharp
using AutoMappic.Generated;
// ...
var dto = source.MapToUserDto();
```

*   **How it works**: You call the generated extension method directly. This bypasses the `IMapper` interface resolution entirely. 
*   **Namespace Required**: `using AutoMappic.Generated;` (This is where the generator places its output).
*   **Benefit**: Eliminates the (minor) overhead of interface dispatch and `IMapper` injection. Ideal for low-latency systems.
