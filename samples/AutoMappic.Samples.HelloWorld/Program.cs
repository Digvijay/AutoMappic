using AutoMappic;
using Microsoft.Extensions.DependencyInjection;

Console.WriteLine("=== AutoMappic Hello World ===\n");

var sourceUser = new User { FirstName = "John", LastName = "Doe" };

// ---------------------------------------------------------
// APPROACH 1: STANDARD DEPENDENCY INJECTION (Recommended)
// ---------------------------------------------------------
Console.WriteLine(">> Approach 1: Standard DI");

var services = new ServiceCollection();

// The Source Generator automatically creates this extension 
// method based on your assembly name! It registers your Profiles.
services.AddAutoMappicFromAutoMappic_Samples_HelloWorld(); 

var provider = services.BuildServiceProvider();
var diMapper = provider.GetRequiredService<IMapper>();

// The mapper.Map call is intercepted at compile time for AOT performance
var dto1 = diMapper.Map<UserDto>(sourceUser);
Console.WriteLine($"Mapped via DI: {dto1.FullName}");

// ---------------------------------------------------------
// APPROACH 2: ZERO-DI STATIC CONFIGURATION
// ---------------------------------------------------------
Console.WriteLine("\n>> Approach 2: Zero-DI Instantiation");

// You can explicitly configure exactly what you want without 
// needing Microsoft.Extensions.DependencyInjection at runtime!
IMapper standaloneMapper = new MapperConfiguration(cfg =>
{
    cfg.AddProfile<UserProfile>();
}).CreateMapper();

var dto2 = standaloneMapper.Map<UserDto>(sourceUser);
Console.WriteLine($"Mapped without DI: {dto2.FullName}");


// =========================================================
// DATA MODELS & MAPPING PROFILES
// =========================================================

public class User
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

public class UserDto
{
    public string FullName { get; set; } = string.Empty;
}

// AutoMappic discovers this automatically during AddAutoMappicFrom...()
public class UserProfile : Profile
{
    public UserProfile()
    {
        // Custom mapping definition
        CreateMap<User, UserDto>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"));
    }
}
