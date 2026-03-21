# Basic Mapping Tutorials

AutoMappic follows the **Convention over Configuration** principle. In many cases, it "just works" out of the box without any extra code.

## 1. Property Name Matching

The most basic scenario is when the source and destination properties have identical names.

```csharp
public class User { public string Name { get; set; } }
public class UserDto { public string Name { get; set; } }

public class MyProfile : Profile
{
    public MyProfile()
    {
        CreateMap<User, UserDto>();
    }
}
```

AutoMappic will generate a direct assignment: `result.Name = source.Name;`.

## 2. PascalCase Flattening

AutoMappic automatically "flattens" complex object graphs into a flat DTO using PascalCase conventions. This is a common pattern for reducing API payload sizes.

Suppose you have:
```csharp
public class Order 
{ 
    public Customer Customer { get; set; } 
}
public class Customer { public string Name { get; set; } }

public class OrderDto 
{ 
    public string CustomerName { get; set; } 
}
```

AutoMappic will intelligently resolve `OrderDto.CustomerName` by looking for `Order.Customer.Name`. The generated code will even handle null-safety:

```csharp
// Generated code looks like this:
result.CustomerName = source.Customer?.Name ?? string.Empty;
```

## 3. Naming Conventions (Snake to Pascal)

Real-world APIs often interact with database columns or JSON properties using `snake_case`. AutoMappic can bridge this gap automatically.

To enable, configure the conventions in your profile:

```csharp
public class MyProfile : Profile
{
    public MyProfile()
    {
        // Automatically maps first_name -> FirstName
        CreateMap<DatabaseEntity, DomainModel>();
    }
}
```

AutoMappic strips underscores and ignores case when performing initial matching, making it extremely flexible.

## 4. Method-to-Property Mapping

If your source class has a method starting with `Get`, it will be automatically matched to a corresponding property.

```csharp
public class Product 
{ 
    public decimal GetPrice() => 15.0m; 
}
public class ProductDto { public decimal Price { get; set; } }
```

AutoMappic will detect `GetPrice()` and map it to `Price`.

## 5. Value Type Conversions

AutoMappic handles common value type conversions natively with zero reflection:
- **Nullable to Non-Nullable**: `int?` -> `int` (uses `.GetValueOrDefault()`).
- **Primitive to String**: Implicitly calls `.ToString()`.
- **Numeric Widening**: `int` -> `double` (implicit cast).

---

By leveraging these conventions, you can typically reduce your mapping configuration by 80% compared to manual code.
