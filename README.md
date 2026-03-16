![AutoMappic Hero](/Users/digvijay/.gemini/antigravity/brain/193ab200-f83b-488b-bbb7-f317790d9744/automappic_hero_1773693156600.png)

# AutoMappic v0.1.0 🚀

**Zero-Reflection. Zero-Overhead. Native AOT-First.**

AutoMappic is a convention-based object-to-object mapper for .NET 9+ that leverages **Roslyn Interceptors** to replace standard reflection with high-performance, statically-generated code at compile time.

> [!IMPORTANT]
> AutoMappic is designed for modern .NET workloads where performance and Native AOT compatibility are non-negotiable.

## Why AutoMappic?

Standard mappers like AutoMapper rely on runtime reflection and `Expression.Compile()`, which can be slow and often break in trimmed or Native AOT environments. AutoMappic shifts all the heavy lifting to the compiler.

- **Fast**: Faster than manual mapping because the compiler can optimize the straight-line C# we emit.
- **AOT Ready**: 100% compatible with Native AOT. No dynamic code generation at runtime.
- **Debuggable**: Step through your mapping code just like any other C# file.
- **Drop-in Migration**: Identical `Profile`, `CreateMap`, and `ForMember` syntax. Simply swap `using AutoMapper;` for `using AutoMappic;` and `AddAutoMapper` for `AddAutoMappic`.

## Core Features

- **Bidirectional Mapping**: Supports `ReverseMap()` to automatically generate two-way mappings from a single declaration.
- **PascalCase & SnakeCase**: `Order.Customer.Name` maps to `OrderDto.CustomerName` and `first_name` maps to `FirstName`.
- **Deep Collections & Dictionaries**: Automated mapping for lists, arrays, `Dictionary<TKey, TValue>`, and nested collections.
- **Null-Safe by Design**: Automatically handles nested null navigation without `NullReferenceException`.
- **Explicit Overrides**: Use `.ForMember()` and runtime fallback map resolutions matching AutoMapper's behavior.
- **IValueResolver Support**: Fully AOT-compatible `IValueResolver<TSource, TMember>` interceptors that compile purely to `new Interceptor().Resolve(source)` static outputs.
- **Entity Framework ProjectTo**: Statically expand `IQueryable<T>` `.ProjectTo<TDto>()` into optimized `.Select` LINQ trees allowing EF Core to hit the database with zero reflection.
- **Native DataReader Support**: `.Map<T>()` over standard `IDataReader` enumerables allowing hyper-optimized data conversion loops directly from the ADO connection context.
- **Dependency Injection**: One-line setup with `builder.Services.AddAutoMappic(typeof(Program).Assembly);`.
- **Comprehensive Diagnostics**: Catch unmapped properties (AM001) or ambiguous paths (AM002) at build time.

## Quick Start

```csharp
using AutoMappic;
using Microsoft.Extensions.DependencyInjection;

// 1. Define a Profile
public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>().ReverseMap();
    }
}

// 2. Setup Dependency Injection (just like AutoMapper!)
var services = new ServiceCollection();
services.AddAutoMappic(typeof(UserProfile).Assembly);

// 3. Use it
var serviceProvider = services.BuildServiceProvider();
var mapper = serviceProvider.GetRequiredService<IMapper>();

var dto = mapper.Map<User, UserDto>(new User { Name = "Alice" });
```

For a detailed step-by-step tutorial, see [GettingStarted.md](./GettingStarted.md).

## Implementation Learnings

Interested in how we built a Roslyn Incremental Source Generator? Check out our [learnings.md](./learnings.md) for a deep dive into the technical challenges we solved.

---
*Built with ❤️ for the .NET Community.*
