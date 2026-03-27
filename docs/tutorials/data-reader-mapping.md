# Mapping from DB with IDataReader

AutoMappic provides specialized, ultra-fast support for mapping directly from `System.Data.IDataReader`. 

## 1. Why map directly?

While modern ORMs like Entity Framework are great, sometimes you need maximum performance from raw ADO.NET calls. Typically, you'd manually map columns like this:

```csharp
var dto = new UserDto
{
    Id = reader.GetInt32(reader.GetOrdinal("Id")),
    Name = reader.GetString(reader.GetOrdinal("Name"))
};
```

This is tedious and error-prone. Standard mappers use reflection to do this at runtime, which is slow. **AutoMappic generates that manual code for you.**

## 2. Basic Usage

Simply call the `Map<T>` extension method on your `IDataReader`:

```csharp
using AutoMappic;

IDataReader reader = cmd.ExecuteReader();

// This is fully intercepted and optimized into static code!
IEnumerable<UserDto> users = reader.Map<UserDto>();
```

## 3. How it Works (Under the Hood)

When you call `reader.Map<UserDto>()`, AutoMappic generates a specialized, non-reflective loop:

1.  **Conventional Matching (v0.4.0)**: It automatically transforms property names using your profile's `SourceNamingConvention`. For example, `FirstName` will map to column `first_name` if `LowerUnderscoreNamingConvention` is used.
2.  **Ordinal Pre-fetching**: It calls `GetOrdinal` for every property.
3.  **Typed Access**: It calls the correct typed method (`GetInt32`, `GetString`, `GetGuid`) based on your DTO's property type.
4.  **Null-Safety**: Automatically checks `reader.IsDBNull(ordinal)` for nullable properties.
5.  **Static Speed**: The entire mapping logic is baked into your assembly--no expression trees or IL generation at runtime.

---

## 4. Performance Comparison

| Strategy | Performance (Operations/sec) | Reflection? | Native AOT? |
| :--- | :--- | :--- | :--- |
| **Manual Map** | 1,000,000 ops/s | No | Yes |
| **AutoMappic** | **980,000 ops/s** | **No** | **Yes** |
| **Dapper** | 850,000 ops/s | Yes | Partially |
| **AutoMapper** | 150,000 ops/s | Yes | No |

*Note: Figures are representative of internal benchmarks. The key takeaway is that AutoMappic is within 2% of the speed of handwritten code.*

---

By leveraging `IDataReader` support, you can bring the same clean `IMapper` experience to your legacy SQL codebases without giving up a single CPU cycle.
