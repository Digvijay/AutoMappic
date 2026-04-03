# Tutorial: Standalone Mapping with [AutoMap]

In AutoMappic v0.6.0+, you can define mappings directly on your DTO or Entity classes using the `[AutoMap]` attribute. This "standalone" approach is perfect for reducing boilerplate when you don't need the complex configuration of a full `Profile` class.

## 1. Quick Start

Instead of creating a separate mapping profile, decorate your destination class with `[AutoMap]` and specify the source type.

```csharp
using AutoMappic;

// The Source
public sealed class UserRecord 
{ 
    public int Id { get; init; } 
    public string Name { get; init; } 
}

// THE STANDALONE DTO
[AutoMap(typeof(UserRecord))]
public partial class UserDto 
{ 
    public int Id { get; set; } 
    public string Name { get; set; } 
}
```

## 2. The `partial` Requirement

For standalone mapping to work, your class **must** be marked as `partial`. 

Why? Because the AutoMappic Source Generator identifies classes with `[AutoMap]` and generates matching mapping methods directly inside a sibling partial class at compile time. This ensures zero reflection and full Native AOT compatibility.

### 💡 IDE Support: The Automatic Code-Fix
If you forget to add the `partial` keyword, AutoMappic will report a diagnostic error (**AM0018**). In Visual Studio or VS Code, you can simply click the **Lightbulb** 💡 (or press `Ctrl+.`) and select **"Add partial keyword"** to fix it automatically.

## 3. Configuration Options

The `[AutoMap]` attribute allows you to configure advanced features just like a Profile:

| Option | Description |
| :--- | :--- |
| `ReverseMap` | Automatically generates the reverse mapping from DTO back to Source. |
| `EnableIdentityManagement` | Opts-in to object reference tracking (prevents circularity). |
| `DeleteOrphans` | Automatically removes items from child collections if they aren't in the source. |
| `SourceNamingConvention` | Overrides how source property names are interpreted (e.g., `SnakeCaseNaming`). |

### Example: Bi-directional Entity Sync
```csharp
[AutoMap(typeof(User), ReverseMap = true, EnableIdentityManagement = true)]
public partial class UserDto 
{ 
    public string Email { get; set; }
}
```

## 4. How it is Discovered

You don't need to manually register standalone mappings. When you call `services.AddAutoMappic()`, the engine automatically scans for all classes decorated with `[AutoMap]` and registers them into the global `IMapper`.

```csharp
// Usage remains identical to Profile-based mapping
var userDto = mapper.Map<UserDto>(user);
```

## 5. When to use [AutoMap] vs. Profile

- **Use `[AutoMap]`**: For standard DTOs where property names match or use standard flattening. It keeps your code localized and easy to read.
- **Use `Profile`**: When you need complex logic like `.ForMember()`, `.AfterMap()`, or custom `ITypeConverter` implementations that require access to the mapping expression tree.

---

[Next: Queryable Projection ->](./queryable-projection.md)
[Back: Basic Mapping <-](./basic-mapping.md)
