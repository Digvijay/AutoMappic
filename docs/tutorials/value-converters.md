# Reusable Value Converters

In modern applications, you often find yourself repeating the same data transformation logic across multiple mappings. Instead of writing the same lambda every time, AutoMappic allows you to encapsulate this logic into **Value Converters**.

## Defining a Value Converter

To create a converter, implement the `IValueConverter<TSourceMember, TDestinationMember>` interface.

```csharp
using AutoMappic;

public class DateTimeToLocalConverter : IValueConverter<DateTime, DateTime>
{
    public DateTime Convert(DateTime source)
    {
        return source.ToLocalTime();
    }
}
```

## Using a Value Converter

Register the converter at the member level using the `ConvertUsing` method.

```csharp
public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.LastLogin, opt => opt.ConvertUsing<DateTimeToLocalConverter, DateTime>(src => src.LastLoginUtc));
    }
}
```

## Global Type-to-Type Converters (v0.4.0)

In v0.4.0, you can also register a converter to handle **all mappings** between two types across an entire application.

```csharp
public class MoneyConverter : IValueConverter<decimal, MoneyDto>
{
    public MoneyDto Convert(decimal source) => new MoneyDto { Amount = source, Currency = "USD" };
}

public class MyProfile : Profile
{
    public MyProfile()
    {
        // Now every decimal mapped to a MoneyDto will use this converter!
        CreateMap<decimal, MoneyDto>().ConvertUsing<MoneyConverter>();
    }
}
```

## Difference between Value Converters and Type Converters

- **Value Converters**: Operate on a **single property**. Use `.ForMember(..., opt => opt.ConvertUsing<...>)`.
- **Type Converters**: Handle the **entire mapping** between two types. Use `CreateMap<S, D>().ConvertUsing<...>()`.

Use Value Converters for formatting (dates, currency) or specific field logic. Use Type Converters when you need full control over the instantiation and mapping of the entire destination object.
