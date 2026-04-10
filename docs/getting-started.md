---
title: Getting Started with AutoMappic v0.7.0
description: Learn how to install and configure AutoMappic, the zero-reflection object mapper for .NET 9 and .NET 10 with Native AOT support.
---

# Getting Started with AutoMappic v0.7.0

AutoMappic is a zero-reflection, Native AOT-friendly object mapper for .NET 9 and .NET 10. It uses Roslyn Interceptors to replace reflection with fast, static code at compile time.

## 1. Installation

Add the AutoMappic NuGet package to your projects:

```bash
dotnet add package AutoMappic
```

## 2. Enable Interceptors

Interceptors are the core technology behind AutoMappic. You must enable the generated namespace in your `.csproj` so the compiler can reroute your mapping calls:

```xml
<PropertyGroup>
  <InterceptorsNamespaces>$(InterceptorsNamespaces);AutoMappic.Generated</InterceptorsNamespaces>
</PropertyGroup>
```

## 3. Define Your Models

AutoMappic works best with standard POCOs and DTOs.

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
    public string AddressCity { get; set; } = ""; // PascalCase Flattening
}
```

## 4. Create a Profile

Profiles are how you define mapping configurations. They are structure-compatible with AutoMapper.

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

### Alternative: Standalone Mapping ([AutoMap])
For many DTOs, you don't even need a `Profile` class. Just decorate the DTO with `[AutoMap]` and make sure it is `partial`:

```csharp
[AutoMap(typeof(User))]
public partial class UserDto 
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
}
```

::: tip
See the [Standalone Mapping Tutorial](./tutorials/standalone-mapping.md) for more details.
:::

## 5. Setup and Use the Mapper

AutoMappic uses a zero-reflection registration system for maximum performance.

### Dependency Injection (Recommended)

Add AutoMappic to your `IServiceCollection`. The source generator automatically discovers all profiles in your project and its dependencies at compile-time.

```csharp
using Microsoft.Extensions.DependencyInjection;
using AutoMappic;

var services = new ServiceCollection();

// Statically discovers all profiles across your entire solution
services.AddAutoMappic(); 

var serviceProvider = services.BuildServiceProvider();
var mapper = serviceProvider.GetRequiredService<IMapper>();
```

### Direct Usage (Async-Ready)

```csharp
var user = new User { Id = 1, Username = "alice", Address = new Address { City = "Seattle" } };

// This call is intercepted at compile-time and replaced with direct assignments
var dto = mapper.Map<User, UserDto>(user);

// NEW in v0.7.0: Fluent API
var dto2 = user.MapTo<UserDto>(mapper);

Console.WriteLine(dto.AddressCity); // Seattle
```

## 6. Advanced Features

### Asynchronous Mapping
Perform I/O-bound operations during mapping with non-blocking `MapAsync` and `IAsyncValueResolver`.

### Collection Mapping
AutoMappic handles `IEnumerable<T>`, `List<T>`, and arrays using Zero-LINQ technology. It generates high-performance `for` loops with pre-allocated capacity.

### Entity Framework Core Projections
Mapping over an ORM like EF Core remains AOT-safe via `ProjectTo`:

```csharp
using AutoMappic;

IQueryable<User> query = dbContext.Users.Where(u => u.IsActive);
// Intercepted and replaced with a static Select(src => new UserDto{ ... }) expression
var projected = query.ProjectTo<UserDto>(_mapper.ConfigurationProvider);
```

### Database Streaming (New in v0.7.0)
AutoMappic now supports zero-allocation streaming for database results. Use `MapAsync` on a `DbDataReader` to get an `IAsyncEnumerable<T>`:

```csharp
using var reader = await command.ExecuteReaderAsync();
await foreach (var customer in reader.MapAsync<CustomerDto>())
{
    // Process customer as it streams from the database
}
```

## 7. Performance and Native AOT
AutoMappic generates source code that you can see and debug. Because it is static C#, it is 100% compatible with Native AOT and Linker trimming. v0.7.0 includes explicit transparency for AOT-safety via `RequiresUnreferencedCode` annotations on all runtime fallbacks.

---

[Next: LINQ Projections ->](./project-to.md)
[Back: Home <-](./index.md)
