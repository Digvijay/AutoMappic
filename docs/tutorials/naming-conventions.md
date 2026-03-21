# Naming Conventions

By default, AutoMappic uses Case-Insensitive matching and can handle `PascalCase` to `camelCase` and `snake_case` mappings because its "Normalization" logic ignores underscores.

However, for enterprise-grade projects that require consistent naming patterns (e.g., when mapping to JSON-compliant DTOs), you can specify **Naming Conventions**.

## Built-in Conventions

AutoMappic comes with three standard naming conventions:

- `PascalCaseNamingConvention`: Matches words that start with an uppercase letter (e.g., `FirstName`).
- `CamelCaseNamingConvention`: Similar to Pascal but starts with a lowercase letter (e.g., `firstName`).
- `LowerUnderscoreNamingConvention`: Standard `snake_case` matching (e.g., `first_name`).
- `KebabCaseNamingConvention`: Standard `kebab-case` matching (e.g., `first-name`). (v0.3.0)

## Configuring Conventions

You can configure naming conventions for a specific mapping using the `.WithNamingConvention()` method.

```csharp
public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>()
            .WithNamingConvention(new PascalCaseNamingConvention(), new KebabCaseNamingConvention());
    }
}
```

By default, AutoMappic will match:
- `OrderDate` → `order-date`
- `CustomerName` → `customer-name`

## Global Conventions

You can set the default convention for all mappings in a profile by assigning them in the constructor.

```csharp
public class ExternalApiProfile : Profile
{
    public ExternalApiProfile()
    {
        SourceNamingConvention = new PascalCaseNamingConvention();
        DestinationNamingConvention = new LowerUnderscoreNamingConvention();

        // This mapping will now use the profile's conventions by default.
        CreateMap<User, UserRecord>();
    }
}
```

## Dictionary & Reader Conventions (v0.3.0)

AutoMappic now uses naming conventions when mapping from specialized sources like `IDataReader` and `IDictionary<string, T>`.

```csharp
public class DbProfile : Profile
{
    public DbProfile()
    {
        // When source is a DB/Dictionary, use snake_case
        SourceNamingConvention = new LowerUnderscoreNamingConvention();
        
        // This will now map target.FirstName to reader["first_name"] or dict["first_name"]
        CreateMap<IDictionary<string, object>, UserDto>();
        CreateMap<IDataReader, OrderDto>();
    }
}
```

### Acronym Support

In v0.3.0, the `PascalCase` and `CamelCase` conventions have been upgraded to handle acronyms (e.g., `CustomerID` → `customerId` or `customer-id`).
