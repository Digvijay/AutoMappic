# LINQ Projections: `ProjectTo<T>`

AutoMappic v0.6.0 introduces the **Performance King**: `ProjectTo<T>`. This feature allows object mapping to occur directly inside the database query (SQL), drastically reducing database I/O and memory pressure by only selecting the columns required for the DTO.

## 1. How It Works

Traditional mapping requires fetching the entire entity from the database into memory, then mapping it to a DTO. 

```csharp
// ❌ UNOPTIMIZED: Selects all columns from 'Users' (including large Blobs/Passwords)
var users = dbContext.Users.ToList(); 
var dtos = mapper.Map<List<UserDto>>(users);
```

ProjectTo emits a **LINQ Expression Tree** that translates the mapping directly into a `SELECT` statement before the database executes it.

```csharp
using AutoMappic;

// ✅ OPTIMIZED: Translates to 'SELECT Id, Name FROM Users'
var dtos = dbContext.Users.ProjectTo<UserDto>().ToList();
```

## 2. Key Benefits

- **Reduced Data Transfer**: Only specific columns are sent over the network from the database.
- **Lower Memory Usage**: No intermediate "fat" entity objects are materialized in memory.
- **Native Performance**: AutoMappic generates the expression tree at **compile-time**, eliminating the runtime overhead that other mappers incur when building dynamic expressions for LINQ.

## 3. Supported Features

AutoMappic's projection engine supports:
- **Direct Assignments**: Simple property matches.
- **Flattening**: Deep property access (e.g., `User.Address.City`).
- **Recursive Nested Objects**: Mapping a child entity to a child DTO (e.g., `User` -> `UserDto` containing `AddressDto`).
- **Collections and Lists**: Deeply projected nested collections (e.g., `List<TodoItem>` -> `List<TodoItemDto>`).
- **Convention-Based Naming**: Automatic mapping for similarly named properties.

## 4. Unsupported Scenarios

By design, Projections are limited to what the database (SQL) can translate. The following will trigger **AM0008** (Unsupported ProjectTo Feature):

- **Procedural Logic**: `BeforeMap`, `AfterMap`, or complex custom resolvers.
- **In-Memory Hooks**: Predicates that rely on local state or non-translatable C# methods.

## 5. Performance Benchmarks

In comparison to standard `IMapper.Map`, `ProjectTo` demonstrates:
- **5x - 10x faster query execution** on tables with 20+ columns.
- **90% reduction in allocation pressure** during DB materialization.

[Next: Standalone Mapping ->](./standalone-mapping.md)
