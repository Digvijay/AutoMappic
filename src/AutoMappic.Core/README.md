# AutoMappic.Core

The **AutoMappic.Core** library contains the public-facing abstractions, attributes, and base classes for the AutoMappic v0.5.0 "Zero-Reflection" mapper.

## Getting Started

This package is intended to be used by all consumer projects (APIs, Domain, Persistence) as it defines the shared mapping contracts and configuration attributes.

1. Install the package via NuGet:
   ```bash
   dotnet add package AutoMappic
   ```

2. Add a `Profile` to your project and define your mappings:
   ```csharp
   using AutoMappic;

   public sealed class MappingProfile : Profile
   {
       public MappingProfile()
       {
           CreateMap<Source, Destination>();
       }
   }
   ```

3. Register your mappings in your `IServiceCollection`:
   ```csharp
   services.AddAutoMappic(typeof(MappingProfile).Assembly);
   ```

The runtime portion of this library is intentionally lightweight and 100% compatible with **Native AOT** and **Aggressive Trimming** environments.
