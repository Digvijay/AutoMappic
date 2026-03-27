# Identity Management & Patch Mode

AutoMappic v0.4.0 introduces **Identity Management** -- an opt-in feature that transforms the mapper from a simple property copier into an entity-aware graph engine compatible with EF Core change trackers.

## Enabling Identity Management

Add the MSBuild property to your `.csproj`:

```xml
<PropertyGroup>
  <AutoMappic_EnableIdentityManagement>true</AutoMappic_EnableIdentityManagement>
</PropertyGroup>
```

When active, the source generator will:

1. **Track object instances** via a `MappingContext` to prevent cyclic recursion.
2. **Infer primary keys** (`Id`, `ClassNameId`, `[Key]`) on destination types automatically.
3. **Generate conditional assignments** for nullable source properties (Patch Mode).
4. **Emit key-based collection diffing** instead of "Clear-and-Add".

## MappingContext

All generated mapping methods accept an optional `MappingContext? context` parameter:

```csharp
// The context tracks visited objects to prevent infinite loops
var dto = entity.MapToEntityDto(context: new MappingContext());

// Or let it default to null for simple mappings
var dto = entity.MapToEntityDto();
```

## Patch Mode (Null-Ignore)

When identity management is active, nullable source properties generate conditional assignments:

```csharp
// Generated code (simplified)
if (source.Name != null)
{
    result.Name = source.Name;
}
```

This makes HTTP PATCH endpoints trivial -- only non-null values trigger property setters:

```csharp
public class PatchUserRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
}

public class User
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

// Only provided fields are updated
mapper.Map(patchRequest, existingUser);
```

## Collection Syncing (Diffing)

With identity management active, collections use key-based matching:

```csharp
public class Order
{
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }  // Auto-detected as key
    public string Name { get; set; } = "";
}
```

The generated in-place mapping will:
- **Match** source and destination items by `Id`.
- **Update** existing items in-place (preserving EF Core change tracking).
- **Add** new items that exist in source but not in destination.

## AM013 Diagnostic

When using Patch Mode, AutoMappic warns you about dangerous patterns:

```csharp
// WARNING AM013: Patching into required 'Name' from nullable source
public class Dest { public required string Name { get; set; } }
public class Source { public string? Name { get; set; } }
```

See the [Diagnostic Suite](/diagnostics#am0013-required-property-patch-mismatch) for remediation options.

## Performance

All identity management features are:
- **Opt-in**: Standard mapping remains zero-overhead for existing users.
- **AOT-compatible**: Generated as static, type-safe C# code.
- **Zero-reflection**: No runtime reflection or heavy DI required.
