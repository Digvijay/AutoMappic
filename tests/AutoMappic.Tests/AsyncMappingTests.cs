using Prova;
using Assert = Prova.Assertions.Assert;
using System.Threading.Tasks;

namespace AutoMappic.Tests;

public sealed class AsyncMappingTests
{
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
        }
    }
}
