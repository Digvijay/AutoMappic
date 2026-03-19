# Custom Constructors & DI Integration

AutoMappic allows you to take full control of **destination type instantiation** through the `.ConstructUsing()` method. This is critical for mapping to types that don't have a parameterless constructor or when you need to use a Dependency Injection factory.

## Standard Usage

To provide a custom factory for the destination type:

```csharp
public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>()
            .ConstructUsing(src => new OrderDto(src.Id, "Manual Construction"));
    }
}
```

This is emitted as a **direct New statement** in the generated code.

## Accessing Both Source and Destination

You can also access the source within your lambda to pass complex data into your custom constructor:

```csharp
.ConstructUsing((src, dest) => new OrderDto(src.SubsidizedPrice + src.Tax))
```

## Advanced Scenario: Dependency Injection

AutoMappic's **Zero-Reflection Registration** is designed to work seamlessly with `Microsoft.Extensions.DependencyInjection`.

When you call `services.AddAutoMappic()`, the generator automatically discovers your `Profile` classes across your solution. This means:
*   You can have **Profiles as Services**.
*   You can use **Constructors in your Profiles** to inject database contexts or encryption services.

### Example: Using a Service inside a Condition
```csharp
public class SecureProfile : Profile
{
    private readonly IEncryptionService _encryption;

    public SecureProfile(IEncryptionService encryption)
    {
        _encryption = encryption;
        CreateMap<User, UserDto>()
            .ForMember(d => d.Token, opt => opt.MapFrom(s => _encryption.Decrypt(s.RawToken)));
    }
}
```

AutoMappic will automatically resolve `IEncryptionService` from the DI container when your app starts!

## Performance & AOT
Because `ConstructUsing` lambdas are parsed at compile-time, they are and **fully compatible with Native AOT**. No runtime expression compilation (`Lambda.Compile()`) is ever performed.
