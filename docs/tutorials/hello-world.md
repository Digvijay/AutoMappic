# Getting Started: Hello World

AutoMappic provides multiple sample projects demonstrating how to integrate the library seamlessly. If you prefer to test interactions immediately, we highly recommend trying out the **Hello World Sample** included natively in our repository!

You can find the runnable project at: `samples/AutoMappic.Samples.HelloWorld`

## The Native "Hello World"

AutoMappic exposes two primary paths for dependency resolving. You can integrate natively with **ASP.NET Dependency Injection** (`ServiceCollection`), or you can **Standalone Instantiation** for bare-metal performance.

### Setup and Dependency
Ensure you install the required packages:
```bash
dotnet add package AutoMappic
dotnet add package Microsoft.Extensions.DependencyInjection
```

### The Setup

This small `Program.cs` illustrates both workflows simultaneously. Because we use AutoMappic's Source Generator, **both** DI and Zero-DI mapping approaches generate hyper-efficient, 100% Native AOT compatible static methods at compile-time transparently!

```csharp
using AutoMappic;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("=== AutoMappic Hello World ===\n");

var sourceUser = new User { FirstName = "John", LastName = "Doe" };

// ---------------------------------------------------------
// APPROACH 1: STANDARD DEPENDENCY INJECTION (Recommended)
// ---------------------------------------------------------
Console.WriteLine(">> Approach 1: Standard DI");

var services = new ServiceCollection();

// The Source Generator automatically creates this extension 
// method based on your assembly name! It registers your Profiles.
services.AddAutoMappicFromAutoMappic_Samples_HelloWorld(); 

var provider = services.BuildServiceProvider();
var diMapper = provider.GetRequiredService<IMapper>();

// The mapper.Map call is intercepted at compile time for AOT performance
var dto1 = diMapper.Map<UserDto>(sourceUser);
Console.WriteLine($"Mapped via DI: {dto1.FullName}");

// ---------------------------------------------------------
// APPROACH 2: ZERO-DI STATIC CONFIGURATION
// ---------------------------------------------------------
Console.WriteLine("\n>> Approach 2: Zero-DI Instantiation");

// You can explicitly configure exactly what you want without 
// needing Microsoft.Extensions.DependencyInjection at runtime!
IMapper standaloneMapper = new MapperConfiguration(cfg =>
{
    cfg.AddProfile<UserProfile>();
}).CreateMapper();

var dto2 = standaloneMapper.Map<UserDto>(sourceUser);
Console.WriteLine($"Mapped without DI: {dto2.FullName}");


// =========================================================
// DATA MODELS & MAPPING PROFILES
// =========================================================

public class User
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class UserDto
{
    public string FullName { get; set; } = string.Empty;
}

// AutoMappic discovers this automatically during AddAutoMappicFrom...()
public class UserProfile : Profile
{
    public UserProfile()
    {
        // Custom mapping definition
        CreateMap<User, UserDto>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
    }
}
```

## Running the Examples

All samples within the repository are designed to simply hit compile and play:
```bash
cd samples/AutoMappic.Samples.HelloWorld
dotnet run
```

If you are looking for advanced use cases (e.g., Deeply nested graphs, custom type conversions), check out our `"Full Features"` sample directory: `samples/AutoMappic.Samples.FullFeatures`.

---
[Next: Basic Mapping <-](./basic-mapping.md)
