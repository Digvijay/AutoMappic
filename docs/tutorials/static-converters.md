# Static Converters

AutoMappic v0.5.0 introduces `[AutoMappicConverter]` -- a way to define zero-allocation, compile-time custom type conversions using plain static methods.

## Why Static Converters?

Traditional `ITypeConverter<TSource, TDest>` requires class instantiation and virtual dispatch. Static converters eliminate this overhead entirely -- the source generator delegates directly to your static method.

## Usage

Decorate any static method with `[AutoMappicConverter]`:

```csharp
public static class Converters
{
    [AutoMappicConverter]
    public static MoneyView ToView(Money money) 
        => new MoneyView 
        { 
            Display = $"{money.Amount:C} {money.Currency}" 
        };

    [AutoMappicConverter]
    public static DateOnly ToDateOnly(DateTime dt) 
        => DateOnly.FromDateTime(dt);
}
```

### Requirements

The method must be:
- **`static`** -- instance methods are not supported.
- **Single parameter** -- the source type.
- **Non-void return** -- must return the destination type.

## Generated Code

When the generator discovers an `[AutoMappicConverter]`, it emits a mapping method that delegates directly:

```csharp
// Generated (simplified)
public static MoneyView MapToMoneyView(this Money source, MappingContext? context = null)
{
    if (source is null) throw new ArgumentNullException(nameof(source));
    return Converters.ToView(source);  // Direct static call, zero overhead
}
```

## Combining with Profiles

Static converters work alongside profile-defined mappings. If both a `CreateMap<A, B>()` and an `[AutoMappicConverter]` exist for the same type pair, the converter takes precedence.

```csharp
public class MyProfile : Profile
{
    public MyProfile()
    {
        // This mapping exists but the [AutoMappicConverter] below wins
        CreateMap<Money, MoneyView>();
    }
}

public static class Converters
{
    [AutoMappicConverter]
    public static MoneyView ToView(Money m) => new() { Display = m.Amount.ToString() };
}
```

## In-Place Mapping

Static converters only support **instance creation** (returning a new object). In-place mapping (`Map(source, destination)`) is not supported for converter-backed type pairs and will throw `NotSupportedException`.

## Performance

| Approach | Overhead | AOT Safe |
| :--- | :--- | :--- |
| `ITypeConverter<T1,T2>` | Virtual dispatch + allocation | Yes |
| `[AutoMappicConverter]` | **Direct static call** | **Yes** |
| `Func<T1,T2>` delegate | Delegate invocation | No (closure) |

Static converters are the fastest way to define custom type translations in AutoMappic.
