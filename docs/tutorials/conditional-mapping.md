# Conditional Mapping & AM0008

AutoMappic v0.5.0 introduces **Conditional Member Mapping**, allowing you to gate property assignments with custom predicates. This is essential for complex business logic where you only want to map a value if certain criteria are met.

## Basic Usage

To add a condition, use the `.Condition()` option inside a `ForMember` call:

```csharp
public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(d => d.AdminStatus, opt => opt.Condition((src, dest) => src.IsAdmin));
    }
}
```

If the condition evaluates to `false`, the property on the destination object will **not be overwritten**.

## Dual-Instance Access

Unlike some other mappers, AutoMappic gives you access to **both** the source and the current state of the destination instance within the condition:

```csharp
.ForMember(d => d.Score, opt => opt.Condition((src, dest) => src.Score > dest.Score))
```

This is extremely useful for "Only update if newer/better" logic.

## The AM0008 Shield: ProjectTo Protection

One of the most powerful features of AutoMappic is its **Build-Time Analytics**. Because `Condition` predicates are emitted as procedural C# code (e.g., `if (src.IsAdmin) { ... }`), they cannot be translated into SQL by LINQ providers like Entity Framework Core.

To protect you from runtime crashes, AutoMappic includes the **AM0008 Diagnostic**:

### What triggers AM0008?
If you have a profile with a `Condition` and you attempt to use it inside a `.ProjectTo<T>()` call, the compiler will issue a warning:

> **AM0008**: ProjectTo may fail at runtime for mapping 'User' -> 'UserDto' because the profile contains procedural logic (Condition) that cannot be translated to SQL.

### How to resolve AM0008?
1. **Use `Map` instead of `ProjectTo`**: Fetch the entity first, then map it in-memory.
2. **Move logic to the SQL layer**: Use a `MapFrom` with a ternary expression that the LINQ provider *can* translate (e.g., `src => src.IsAdmin ? src.Value : dest.Value`).

## Performance Note
Because AutoMappic is a **Zero-Reflection** mapper, your `Condition` is converted into a direct `if` statement in the generated code. There is **zero overhead** from expression tree parsing or delegate invocation at runtime.
