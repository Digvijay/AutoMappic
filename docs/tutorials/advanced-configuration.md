# Advanced Configuration Tutorial

When conventions are not enough, AutoMappic provide a fluent, type-safe API to customize your mappings.

## 1. Explicit Property Mapping (`ForMember`)

You can override any convention by explicitly specifying the source for a destination property.

```csharp
public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FirstName + " " + src.LastName));
    }
}
```

The generator will extract the C# snippet `src.FirstName + " " + src.LastName` and inline it directly into the static mapping method. **No delegate or expression is executed at runtime.**

## 2. Ignoring Properties

If you want to prevent a property from being mapped, use `Ignore()`.

```csharp
CreateMap<Order, OrderDto>()
    .ForMember(dest => dest.InternalNote, opt => opt.Ignore());
```

The generated code will skip this assignment entirely, ensuring sensitive data doesn't leak into DTOs.

## 3. Reverse Mapping

Building bidirectional mappings is common for UI models and entities. AutoMappic generates reverse mappings with a single call:

```csharp
CreateMap<Customer, CustomerDto>().ReverseMap();
```

This will automatically create both `Customer → CustomerDto` and `CustomerDto → Customer`.

## 4. Custom Type Converters

For complete control over the entire mapping (e.g., converting a complex object to a single string), use `ITypeConverter`.

```csharp
public class UserConverter : ITypeConverter<User, UserSummaryDto>
{
    public UserSummaryDto Convert(User source)
    {
        return new UserSummaryDto { Summary = source.Name + " (" + source.Email + ")" };
    }
}

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserSummaryDto>().ConvertUsing<UserConverter>();
    }
}
```

AutoMappic will emit `new UserConverter().Convert(source)` in its static shim.

## 5. Constructor Mapping

AutoMappic automatically detects if the destination type has a constructor that matches the source properties.

```csharp
public class UserDto
{
    public UserDto(string name, int id) { Name = name; Id = id; }
    public string Name { get; }
    public int Id { get; }
}

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>(); // Automatically uses the constructor
    }
}
```

The generator will find the best-matching constructor and generate `new UserDto(source.Name, source.Id)`. This is perfect for **immutable records** and **Domain-Driven Design (DDD)**.

## 6. Lifecycle Hooks (BeforeMap / AfterMap)

Sometimes, property mapping isn't enough. You may need to perform side effects, initialize the destination object, or perform complex cross-property calculations.

AutoMappic supports synchronous and asynchronous lifecycle hooks:

### Synchronous Hooks
```csharp
CreateMap<Order, OrderDto>()
    .BeforeMap((src, dest) => {
        // Prepare the destination (e.g., set a default value)
        dest.CreatedDate = DateTime.UtcNow;
    })
    .AfterMap((src, dest) => {
        // Perform a complex derivation after mapping is finished
        dest.IsEligibleForExpressShipping = dest.LineItems.Any(i => i.RequiresSpecialHandling);
    });
```

### Asynchronous Hooks
These are perfect for fetching extra data from a cache or database *during* the mapping process.
```csharp
CreateMap<User, UserDto>()
    .BeforeMapAsync(async (src, dest) => {
        // Non-blocking I/O during mapping!
        var preference = await _cache.GetAsync($"pref_{src.Id}");
        dest.ThemeColor = preference ?? "Blue";
    })
    .AfterMapAsync(async (src, dest) => {
         await AuditService.LogMappingAsync(src.Id, dest.Id);
    });
```

AutoMappic ensures that these hooks are executed in a predictable sequence:
`BeforeMap` $\to$ `BeforeMapAsync` $\to$ **Property Mapping Logic** $\to$ `AfterMap` $\to$ `AfterMapAsync`.

---

## 7. Zero-Allocation Enum Mapping

Mapping an Enum to a String is common in API development. AutoMappic handles this natively without any extra configuration.

```csharp
public enum OrderStatus { Pending, Shipped, Delivered }
public class Order { public OrderStatus Status { get; set; } }
public class OrderDto { public string Status { get; set; } }

// Just works!
CreateMap<Order, OrderDto>();
```

At compile-time, AutoMappic generates a high-efficiency `.ToString()` call. This avoids the overhead of reflection or generic `Enum.GetName()` calls, making it safe for Native AOT and high-throughput scenarios.

---

[Next: Collections and Dictionaries →](./collections-and-dictionaries.md)
