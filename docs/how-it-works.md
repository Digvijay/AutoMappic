# How it Works

AutoMappic is fundamentally different from traditional .NET object mappers. While most libraries rely on runtime reflection and IL generation, AutoMappic shifts all the "heavy lifting" to compile-time using **Roslyn Interceptors** and **Source Generators**.

## The Architectural Shift

Traditional mappers like AutoMapper follow a **"Plan-at-Runtime"** strategy. AutoMappic follows a **"Verify-at-Compile-time"** strategy.

| Feature | Traditional Mappers (AutoMapper) | AutoMappic |
| :--- | :--- | :--- |
| **Discovery** | Runtime Reflection (`GetProperties`) | Compile-time Roslyn Symbols |
| **Execution** | `Expression.Compile()` at Runtime | Static C# Code emitted at Build |
| **JIT Overhead** | High (First call penalty) | Zero (Direct calls) |
| **AOT Compatibility**| ❌ Trimming & JIT issues | ✅ 100% Native AOT Friendly |
| **Performance** | O(N) where N is reflection cost | O(1) direct static calls |

---

## 1. Zero-Reflection Interception

The "Magic" of AutoMappic lies in [C# Interceptors](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-12#interceptors). 

When you write:
```csharp
var dto = _mapper.Map<UserDto>(user);
```

AutoMappic doesn't execute that `Map` method at runtime. Instead, the Source Generator finds the exact file and character position of that call and emits an **Interceptor**. This "hijacks" the call and reroutes it to a statically generated mapping method specifically optimized for those two types.

```mermaid
graph LR
    A[User Code: _mapper.Map] --> B{Interceptor}
    B -- Reroutes to --> C[Generated Static Map]
    C --> D[UserDto Result]
    style B fill:#f96,stroke:#333,stroke-width:2px
```

## 2. Compile-Time Validation (Zero Slop)

Because AutoMappic understands your code before it runs, it can provide immediate feedback. If you attempt to map a `Source` to a `Destination` where a property is missing or types are incompatible, you don't find out in Production—you find out in your IDE.

> [!IMPORTANT]
> **Diagnostics AM001–AM005** ensure that your mapping profiles are always in sync with your models. If a build passes, the mapping is guaranteed to work.

## 3. High-Performance Collection Mapping

AutoMappic avoids the allocation overhead of LINQ when mapping collections. Instead of generic `.Select().ToList()`, the generator emits optimized `for` loops that pre-allocate the destination capacity whenever possible.

```csharp
// Example of generated optimized collection mapping
public static List<UserDto> MapToUserDtoList(this List<User> source)
{
    var list = new List<UserDto>(source.Count);
    for (int i = 0; i < source.Count; i++)
    {
        list.Add(source[i].MapToUserDto());
    }
    return list;
}
```

## 4. Native AOT & Trimming

Modern cloud-native applications require small binaries and instant startup.
*   **No Reflection**: Trimmers can safely remove unused properties because they are never accessed via strings/reflection.
*   **No JIT**: All code is ready to be compiled to machine code (Native AOT) at build time.
*   **Sustainability**: Reduced CPU cycles for cold starts means lower carbon footprint for serverless environments.

---

[Next: Sustainability & ESG →](./sustainability.md)
