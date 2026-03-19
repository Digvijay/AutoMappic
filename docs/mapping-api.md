# Mapping API Reference

AutoMappic provides a fluent API for configuring how properties are mapped between source and destination types. All configurations are declared within a `Profile` constructor.

## 1. Destination Instantiation

### ConstructUsing
Sets a custom factory expression for the destination type. This is used instead of the default constructor and provides manual control over object creation.

```csharp
CreateMap<Order, OrderDto>()
    .ConstructUsing(src => new OrderDto(src.OrderId, DateTime.UtcNow));
```

## 2. Member Configuration

### ForMember
Configures a specific destination member.

```csharp
CreateMap<User, UserDto>()
    .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
```

### MapFrom (Custom Source)
Redirects the source of a destination property.
- **Lambda:** `opt.MapFrom(src => ...)`
- **Member Name:** Not yet supported (use lambda).
- **Value Resolver:** Use `opt.MapFrom<MyResolver>()` for complex logic.

### Condition
Gates a property assignment with a predicate. If the predicate returns `false`, the property is not assigned.

```csharp
CreateMap<Product, ProductDto>()
    .ForMember(dest => dest.Price, opt => opt.Condition((src, dest) => src.IsVisible));
```

### Ignore
Suppresses the mapping of a destination member, also satisfying the **AM001 (Unmapped Property)** diagnostic.

```csharp
CreateMap<Employee, EmployeeDto>()
    .ForMemberIgnore(dest => dest.InternalSecret);
```

### ConvertUsing
Provides a custom converter for the entire type-pair.
```csharp
CreateMap<String, int>().ConvertUsing<StringToIntConverter>();
```

## 3. Directional Mapping

### ReverseMap
Automatically creates a mapping in the opposite direction (`TDestination` $\to$ `TSource`). 

```csharp
CreateMap<User, UserDto>()
    .ReverseMap()
    .ForMember(src => src.InternalId, opt => opt.Ignore());
```

## 4. Lifecycle Hooks

### BeforeMap / AfterMap
Executes custom logic before or after the property mapping phase. Supports both synchronous and asynchronous (`BeforeMapAsync`) implementations.

```csharp
CreateMap<Source, Dest>()
    .AfterMap((src, dest) => dest.Timestamp = DateTime.UtcNow);
```

## 5. Projections and Collections

### Zero-LINQ Collections
AutoMappic automatically handles `List<T>`, `T[]`, and `IEnumerable<T>`. 
- Deep mapping of items is generated as optimized `for` loops.
- Pre-allocation of list capacity is used whenever the source count is known.

### ProjectTo
Converts an `IQueryable<T>` into an `IQueryable<U>` at compile-time by rewriting the `Select` tree.
```csharp
var dtos = dbContext.Users.ProjectTo<User, UserDto>();
```

### IDataReader.Map
Fast projection from `IDataReader` to DTOs without runtime column name scanning.
```csharp
var dto = reader.MapToUserDto();
```
