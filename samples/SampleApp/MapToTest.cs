using System.Threading.Tasks;
using AutoMappic;

namespace MapToTest;

public record User(string Name, int Age);
public record UserDto(string Name, int Age);

public class MapToProfile : Profile
{
    public MapToProfile()
    {
        CreateMap<User, UserDto>();
    }
}

public class TestRunner
{
    public void Run(IMapper mapper)
    {
        var user = new User("Alice", 30);

        // Fluent MapTo
        var dto = user.MapTo<UserDto>(mapper);
        System.Console.WriteLine($"MapTo: {dto.Name}, {dto.Age}");

        // MapTo with explicit types
        var dto2 = user.MapTo<User, UserDto>(mapper);
        System.Console.WriteLine($"MapTo<S,D>: {dto2.Name}, {dto2.Age}");
    }
}
