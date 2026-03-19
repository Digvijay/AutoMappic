# Queryable & DataReader Projection Tutorial

AutoMappic provides high-performance projection for data access layers, specifically designed to be **Native AOT friendly**.

## 1. `ProjectTo<T>` for Entity Framework Core

When using Entity Framework or any LINQ-based data source, you often want to project your entities directly into DTOs to avoid over-fetching columns.

```csharp
using AutoMappic;

public class OrderService(OrderDbContext context)
{
    public async Task<List<OrderDto>> GetOrdersAsync()
    {
        return await context.Orders
            .ProjectTo<OrderDto>()
            .ToListAsync();
    }
}
```

### How It Works

Instead of building a complex expression tree at runtime (which is slow and uses reflection), AutoMappic generates a dedicated, static `.Select()` expression at compile-time.

**The generated code looks like this:**
```csharp
public static IQueryable<OrderDto> ProjectToOrderDto(this IQueryable<Order> source)
{
    return source.Select(src => new OrderDto
    {
        Id = src.Id,
        CustomerName = src.Customer != null ? src.Customer.Name : string.Empty
    });
}
```

The EF Core provider then translates this standard C# `Select` directly into SQL.

## 2. `IDataReader.Map<T>` for Dapper/ADO.NET

If you're using plain `IDataReader` or Dapper, AutoMappic provides a way to map the raw database reader into objects efficiently.

```csharp
using var reader = await command.ExecuteReaderAsync();
var users = reader.Map<UserDto>().ToList();
```

### How It Works

AutoMappic generates a loop that reads the columns from the reader and assigns them to your DTO:

```csharp
// Generated code snippet:
while (reader.Read())
{
    yield return new UserDto
    {
        Id = (int)reader["Id"],
        Name = (string)reader["Name"]
    };
}
```

This is significantly faster than using a generic reflection-based reader mapper.

## 3. Why Use Projections?

- **Database Efficiency**: Using `ProjectTo` tells your database provider exactly which columns to fetch, reducing network traffic.
- **Native AOT**: Projections are statically generated, meaning no dynamic code generation is required at runtime.
- **Zero Overhead**: Because the projection is generated at compile-time, it has the same raw performance as manual `Select` mapping.

---

Projections turn your data access code into a sleek, type-safe pipeline that is both fast to write and fast to run.
