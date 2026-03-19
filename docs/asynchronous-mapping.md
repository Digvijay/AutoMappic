# Asynchronous Mapping Tutorial

AutoMappic provides first-class support for asynchronous mapping, a feature that sets it apart in the world of .NET object mappers.

## 1. Why `MapAsync` matters

In modern ASP.NET Core development, blocking the thread while waiting for I/O operations (like database or API calls) can lead to thread starvation and deadlocks. If a mapping rule requires an external look-up, it **must** be asynchronous.

## 2. Basic `MapAsync` Usage

To map an object asynchronously, use the `MapAsync` extension method on `IMapper`.

```csharp
var mapper = serviceProvider.GetRequiredService<IMapper>();

var source = new User { Id = 1, Name = "Digvijay Chauhan" };

// Intercepted and executed as a static, non-blocking Task
var dto = await mapper.MapAsync<User, UserDto>(source);
```

### Supported Overloads:
- `Task<TDestination> MapAsync<TSource, TDestination>(TSource source)`
- `Task<TDestination> MapAsync<TSource, TDestination>(TSource source, TDestination destination)` (In-place update)
- `Task<TDestination> MapAsync<TDestination>(object source)` (Object-based shorthand)

## 3. Async Value Resolvers (`IAsyncValueResolver`)

For scenarios where a property's value must be resolved asynchronously, implement `IAsyncValueResolver`.

```csharp
public class UserAvatarResolver : IAsyncValueResolver<User, string>
{
    private readonly IAvatarService _avatarService;
    public UserAvatarResolver(IAvatarService avatarService) => _avatarService = avatarService;

    public async Task<string> ResolveAsync(User source)
    {
        // Non-blocking I/O call
        return await _avatarService.GetUrlAsync(source.Id);
    }
}
```

Then register it in your `Profile`:

```csharp
public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(d => d.AvatarUrl, opt => opt.MapFromAsync<UserAvatarResolver>());
    }
}
```

## 4. How It Works: The Dual-Emission Engine

When AutoMappic generates its static mapping classes, it emits **two versions** of each method:
1.  **Synchronous**: `MapToUserDto(source)`
2.  **Asynchronous**: `MapToUserDtoAsync(source)`

If you call `MapAsync`, the interceptor redirects the call to the **async version**, which is fully non-blocking. If you call `Map` on a class that has an async resolver, AutoMappic "makes it work" by wrapping the async call in a thread-safe `.GetAwaiter().GetResult()` block.

## 5. Performance of Asynchronous Mapping

- **Zero Allocation**: Since the mapping is static and pre-allocated, it has minimal GC overhead.
- **Native AOT Ready**: Like all AutoMappic features, `MapAsync` works perfectly with Native AOT.
- **Optimized for Scalability**: By staying non-blocking, your application can handle significantly more simultaneous requests on the same hardware.

---

Asynchronous mapping in AutoMappic gives you the power of non-blocking I/O with the same clean, fluent API you know and love.
