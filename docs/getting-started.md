# Getting Started with AutoMappic v0.2.0

AutoMappic is a zero-reflection, Native AOT-friendly object mapper for .NET 9+. It uses Roslyn Interceptors to replace reflection with fast, static code at compile time.

## 1. Installation

Add the AutoMappic NuGet package to your projects:

```bash
dotnet add package AutoMappic
```

## 2. Enable Interceptors

Interceptors are a preview feature in C# 12+. You must enable them in your `.csproj`:

```xml
<PropertyGroup>
  <InterceptorsPreviewNamespaces>$(InterceptorsPreviewNamespaces);AutoMappic.Generated</InterceptorsPreviewNamespaces>
</PropertyGroup>
```

## 3. Define Your Models

```csharp
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public Address? Address { get; set; }
}

public class Address
{
    public string City { get; set; } = "";
}

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string AddressCity { get; set; } = ""; // PascalCase Flattening!
}
```

## 4. Create a Profile

Profiles are how you tell AutoMappic which types to map.

```csharp
using AutoMappic;

public class MyProfile : Profile
{
    public MyProfile()
    {
        CreateMap<User, UserDto>();
    }
}
```

## 5. Setup & Use the Mapper

For the best performance and Native AOT compatibility, AutoMappic uses a **Zero-Reflection Registration** system. 

### Dependency Injection (Recommended)

Add AutoMappic to your `IServiceCollection`. The source generator automatically discovers all profiles in your project and its references at compile-time.

```csharp
using Microsoft.Extensions.DependencyInjection;
using AutoMappic;

var services = new ServiceCollection();

// Statically discovery all profiles across your entire solution
services.AddAutoMappic(); 

var serviceProvider = services.BuildServiceProvider();
var mapper = serviceProvider.GetRequiredService<IMapper>();
```

### Direct Usage (Async-Ready!)

```csharp
var user = new User { Id = 1, Username = "digvijay", Address = new Address { City = "Seattle" } };

// This call is intercepted at compile-time and replaced with:
// return new UserDto { Id = source.Id, Username = source.Username, AddressCity = source.Address?.City ?? "" };
var dto = await mapper.MapAsync<User, UserDto>(user);

Console.WriteLine(dto.AddressCity); // Seattle
```

## 6. Advanced Features

### Asynchronous Mapping
Perform I/O-bound operations during mapping with non-blocking `MapAsync` and `IAsyncValueResolver`. See [Asynchronous Mapping](./asynchronous-mapping.md) for more details.

### Collection Mapping
AutoMappic automatically handles `IEnumerable<T>`, `List<T>`, and arrays. Using **Zero-LINQ technology**, it generates high-performance `for` loops with pre-allocated capacity, eliminating the GC pressure and throughput overhead of standard LINQ operators.

```csharp
CreateMap<User, UserSummaryDto>(); // Projecting elements
var dtos = await mapper.MapAsync<List<User>, List<UserSummaryDto>>(users);
```

### Dictionary Mapping
Maps keys and values, transforming complex items as needed.

```csharp
var dict = await mapper.MapAsync<Dictionary<int, User>, Dictionary<string, UserSummaryDto>>(source);
```

### Flattening & Unflattening
Automatically resolves `Address.City` into `AddressCity`. Use `ForMember` to override conventions if needed.

### IValueResolver (Custom Logic)
Need a complex derivation? Wire up custom typed interceptors via `IValueResolver<TSource, TMember>`.
```csharp
CreateMap<Order, OrderDto>()
    .ForMember(d => d.TotalPrice, opt => opt.MapFrom<TaxCalculatorResolver>());
```
At compile time, AutoMappic generates exactly `TotalPrice = new TaxCalculatorResolver().Resolve(source)`. Zero reflection penalty.

### Entity Framework Core IQueryable Projections
Mapping over an ORM like EF Core? Typical mappers construct massive expression trees at runtime using Reflection (breaking Native AOT).
AutoMappic does this natively at compile time via extension methods:
```csharp
using AutoMappic;

IQueryable<User> query = dbContext.Users.Where(u => u.IsActive);
// Explicitly providing both types ensures the generator can resolve the mapping perfectly.
IQueryable<UserDto> projected = query.ProjectTo<User, UserDto>();
```
AutoMappic physically rewrites the call to a static `Select(src => new UserDto{ ... })` tree that EF Core naturally understands.

### Native DataReader Performance
Looking for specialized performance for flat data?
```csharp
using System.Data;
using AutoMappic;

IDataReader reader = command.ExecuteReader();
// Project directly from a reader with a statically expanded map!
IEnumerable<UserDto> users = reader.Map<UserDto>(); 
```
See the [DataReader Mapping Tutorial](./tutorials/data-reader-mapping.md) for more performance details.

## 7. Performance & AOT
AutoMappic generates source code that you can see and debug. Because it's "just C#", it is 100% compatible with Native AOT and Linker trimming. No more `UnreferencedCode` warnings in your mapping code!
