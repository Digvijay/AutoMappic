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

---

With these advanced configurations, you can handle 100% of your mapping scenarios without ever sacrificing performance.
