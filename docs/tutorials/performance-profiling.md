# Integrated Performance Profiler

AutoMappic is fast by design, but sometimes your custom resolvers, value converters, or complex collections can introduce latency. To identify exactly which parts of your mapping logic are causing delays, you can use the **Performance Profiler**.

## Enabling Profiling

To enable diagnostic performance markers for all mappings in a profile, set the `EnablePerformanceProfiling` property to `true` in your profile's constructor.

```csharp
public class OrderProfile : Profile
{
    public OrderProfile()
    {
        EnablePerformanceProfiling = true;

        CreateMap<Order, OrderDto>();
    }
}
```

## How it works

When profiling is enabled, the AutoMappic source generator injects `Stopwatch` and `Debug` markers into the generated code.

1.  **Stopwatch Injection**: Resets a high-precision timer for each mapped member.
2.  **Telemetry Reporting**: Logs the elapsed ticks to the `System.Diagnostics.Debug` output.

### Reading Profiler Output

Open your IDE's **Debug Output** or **Output Window** while running your application in debug mode. You will see detailed logs:

```text
AutoMappic Profiler (Order -> OrderDto):
  - Id: 43 ticks
  - OrderDate: 121 ticks
  - TotalAmount: 55 ticks (if condition met)
  - DiscountCode: 12 ticks (skipped - condition not met)
  - Items (Collection): 42,912 ticks (High Allocation!)
```

## When to use profiling?

- **Bottleneck Detection**: Find the one field that is slowing down a mapping of 500 members.
- **Allocation Discovery**: Identify if a specific collection mapping is causing large spikes in managed heap usage.
- **Conditional Overhead**: Check how many ticks the condition predicate itself takes to evaluate, even if the mapping is skipped.

> [!TIP]
> Always disable profiling in **production code** by setting `EnablePerformanceProfiling = false` or using a `#if DEBUG` preprocessor directive in your profile constructor. AutoMappic profiles are scanned at build-time, so this setting must be present during the compilation phase.

```csharp
#if DEBUG
    EnablePerformanceProfiling = true;
#endif
```
