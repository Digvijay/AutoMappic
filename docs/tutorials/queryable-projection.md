# Queryable and DataReader Projection

AutoMappic provides high-performance projection for data access layers, specifically designed for **Native AOT compatibility** and enterprise-grade performance.

## 1. ProjectTo for Entity Framework Core

When using Entity Framework, you often want to project your entities directly into DTOs to avoid over-fetching columns.

```csharp
using AutoMappic;

public class OrderService(OrderDbContext context, IMapper mapper)
{
    public async Task<List<OrderDto>> GetOrdersAsync()
    {
        return await context.Orders
            .ProjectTo<OrderDto>(mapper.ConfigurationProvider)
            .ToListAsync();
    }
}
```

### How It Works

Instead of building a massive expression tree at runtime (which is slow and uses reflection), AutoMappic generates a dedicated, static `.Select()` expression at compile-time.

**The generated code looks like this:**
```csharp
public static IQueryable<OrderDto> ProjectToOrderDto(this IQueryable<Order> source)
{
    // The SELECT tree is exactly what the compiler would emit for manual mapping
    return source.Select(src => new OrderDto
    {
        Id = src.Id,
        CustomerName = src.Customer != null ? src.Customer.Name : string.Empty
    });
}
```

### API Compatibility

AutoMappic supports the standard `ProjectTo<T>(configuration)` signature. This ensures that you can simply replace `AutoMapper.QueryableExtensions` without significantly modifying your service layer logic.

## 2. IDataReader.Map for Dapper and ADO.NET

If you're using plain `IDataReader` or Dapper, AutoMappic provides a way to map basic database readers into objects efficiently.

```csharp
using var reader = await command.ExecuteReaderAsync();
var users = reader.Map<UserDto>().ToList();
```

### How It Works

AutoMappic generates a loop that reads the columns from the reader and assigns them to your DTO directly. This is significantly faster than reflection-based reader mappers and is fully compatible with trimmed or AOT environments.

## 3. Why Use Projections?

- **Database Efficiency**: Using `ProjectTo` tells your database provider exactly which columns to fetch, reducing network traffic and memory usage on the SQL server.
- **Native AOT Ready**: Projections are statically generated, meaning no dynamic code generation is required at runtime.
- **Zero Overhead**: Because the projection is generated at compile-time, it has the same raw performance as manual `Select` mapping.
- **Validated at Build-Time**: If a projection is invalid (e.g., trying to use `BeforeMap` which isn't supported by EF Core), AutoMappic emits the **AM0008** diagnostic early.

---

Projections allow your data access layer to scale to high-volume environments while maintaining modern AOT standards.
