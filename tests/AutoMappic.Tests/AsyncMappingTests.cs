using System.Threading.Tasks;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public sealed class AsyncMappingTests
{
    /// <summary> Ensure that asynchronous mapping to a newly created instance produces correct results through the interceptor </summary>
    [Fact]
    public async Task MapAsync_ToNewInstance_MapsCorrectly()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<AsyncProfile>())
            .CreateMapper();

        var source = new User { Id = 1, Name = "Alice" };

        // This will be intercepted and use the generated MapToUserDto()
        var dto = await mapper.MapAsync<User, UserDto>(source);

        Assert.NotNull(dto);
        Assert.Equal(1, dto.Id);
        Assert.Equal("Alice", dto.Name);
    }

    /// <summary> Validate that the non-generic MapAsync overload correctly resolves and executes the generated mapping logic </summary>
    [Fact]
    public async Task MapAsync_NonGeneric_MapsCorrectly()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<AsyncProfile>())
            .CreateMapper();

        var source = new User { Id = 2, Name = "Bob" };

        // This uses the object-based overload
        var dto = await mapper.MapAsync<UserDto>(source);

        Assert.NotNull(dto);
        Assert.Equal(2, dto.Id);
        Assert.Equal("Bob", dto.Name);
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class AsyncProfile : Profile
    {
        public AsyncProfile()
        {
            CreateMap<User, UserDto>();

            CreateMap<AsyncSource, AsyncDest>()
                .ForMember(d => d.Name, opt => opt.MapFromAsync<AsyncNameResolver>());
        }
    }

    /// <summary> Verify that MapFromAsync works with Mapper.MapAsync. </summary>
    [Fact]
    public async Task MapAsync_WithAsyncResolver_Works()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<AsyncProfile>())
            .CreateMapper();

        var source = new AsyncSource { Name = "Digvijay" };
        var dest = await mapper.MapAsync<AsyncSource, AsyncDest>(source);

        Assert.Equal("DIGVIJAY", dest.Name);
    }

    /// <summary> Verify that Async in-place mapping works. </summary>
    [Fact]
    public async Task MapAsync_InPlace_Works()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<AsyncProfile>())
            .CreateMapper();

        var source = new AsyncSource { Name = "Digvijay" };
        var dest = new AsyncDest { Name = "Empty" };
        
        await mapper.MapAsync(source, dest);

        Assert.Equal("DIGVIJAY", dest.Name);
    }
}

public sealed class AsyncSource { public string Name { get; set; } = string.Empty; }
public sealed class AsyncDest { public string Name { get; set; } = string.Empty; }

public sealed class AsyncNameResolver : IAsyncValueResolver<AsyncSource, string>
{
    public async Task<string> ResolveAsync(AsyncSource source)
    {
        await Task.Delay(10).ConfigureAwait(false); // Simulate I/O
        return source.Name.ToUpperInvariant();
    }
}
