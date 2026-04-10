# ⚡ Hot Reloading AutoMappic Mappings

AutoMappic allows you to modify your mapping code during development and instantly see the results without restarting your application. This drastically cuts down on the standard "edit-recompile-test" lifecycle feedback loop.

## How it Works
When you edit an object mapping configuration inside a `.NET Core` or `ASP.NET Core` application with Hot Reload enabled, the .NET compiler invalidates the generated source files on the fly. 

To bridge this runtime dynamic behavior without sacrificing AOT-safe generation speed, AutoMappic employs a specialized **`HotReloadRegistry`**. The Source Generator recognizes that it is running in *Watch* mode and seamlessly swaps the static pointers intercepting the `Map` execution to your freshly patched lambda rules!

## Step-by-Step Guide

### 1. Enable `dotnet watch`
Launch your application from the command line using `dotnet watch`:

```bash
dotnet watch run --project src/MyApp
```
*Alternatively natively supported through Visual Studio and Rider using "Start with Hot Reload" (Flame icon).*

### 2. Verify Initial Mapping
Assume you have the following mapped code executing in your application route or console loop:

**Mapping Profile:**
```csharp
public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
    }
}
```

### 3. Modify Mappings Live
While `dotnet watch` is running, keep your application open and modify the configuration in your IDE:

**Modify the Profile:**
```csharp
public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>()
            // Instantly change projection rules
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName} (Updated Live!)"));
    }
}
```

### 4. Watch the Magic 🚀
Hit `Ctrl+S` (or `Cmd+S`). Back in your terminal, the Roslyn compiler will report:

> `Hot reload of changes succeeded`

Your mapped endpoints will immediately yield the string `(Updated Live!)`! AutoMappic's generic extensions fall back gracefully strictly during development bounds to resolve the dynamically replaced logic.

## Limitations
*   Hot Reload targets standard property bindings and `.ForMember` invocations.
*   If you enable rigorous *Ahead-of-Time* (AOT) compiler constraints, true dynamic generation swapping is naturally limited by .NET Native AOT capabilities during standalone execution mode. Hot Reload is designed exclusively for your `.NET inner-loop` developer experience.
