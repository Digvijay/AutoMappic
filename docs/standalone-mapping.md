# Standalone Mapping: The [AutoMap] Attribute

AutoMappic v0.6.0 introduces **Standalone Mappings**, allowing you to define mapping configurations directly on your DTO or Entity classes using the `[AutoMap]` attribute. This eliminates the need to create a separate `Profile` class for simple mappings and keeps your codebase highly localized.

## 1. Basic Usage

By decorating your destination class with `[AutoMap]`, the source generator automatically creates a mapping from the specified source type.

```csharp
using AutoMappic;

public class EmployeeSource { public int Id { get; set; } public string Name { get; set; } }

[AutoMap(typeof(EmployeeSource))]
public partial class EmployeeDto 
{ 
    public int Id { get; set; } 
    public string Name { get; set; } 
}
```

::: warning
**IMPORTANT**: Standalone mapping classes **MUST** be marked as `partial`. AutoMappic needs this to inject common mapping interfaces (like `IMapTo<T>`) into your type at compile-time.
:::

## 2. Advanced Configuration

The `[AutoMap]` attribute supports several configuration options found in traditional Profiles:

- **ReverseMap**: Automatically generates a mapping in the opposite direction.
- **DeleteOrphans**: When mapping collections of Entities, removes items from the destination collection that do not exist in the source.
- **EnableIdentityManagement**: Ensures object-references are preserved throughout the mapping graph.
- **SourceNamingConvention / DestinationNamingConvention**: Overrides the naming strategy for this specific pair.

```csharp
[AutoMap(typeof(User), ReverseMap = true, EnableIdentityManagement = true)]
public partial class UserDto { ... }
```

## 3. Mapping Discovery

Standalone mappings are automatically discovered by the generator and integrated into the global `IMapper` registry. You can use them via standard interception just like Profile-based mappings:

```csharp
var dto = _mapper.Map<UserDto>(user);
```

## 4. Comparison with Profiles

| Feature | Profiles | [AutoMap] |
| :--- | :--- | :--- |
| **Location** | Separate class | Directly on DTO |
| **Custom Logic** | Full (.ForMember, .AfterMap) | Limited (Convention-based) |
| **Discovery** | Automatic | Automatic |
| **Partial Required** | No | **Yes** |

**Recommendation**: Use `[AutoMap]` for 90% of your simple DTOs to reduce boilerplate. Use `Profile` subclasses when you need complex transformations, specific `.ForMember()` logic, or asynchronous lifestyle hooks.
