// ─────────────────────────────────────────────────────────────────────────────
// AutoMappic Sample Application
//
// This sample demonstrates the "zero-touch migration" story.  The service
// below uses the standard IMapper interface just as it would with AutoMapper.
// When the AutoMappic.Generator runs, every Map<T>() call is silently replaced
// at compile time with a direct static method call — zero reflection, zero
// startup cost, fully Native AOT compatible.
// ─────────────────────────────────────────────────────────────────────────────

using AutoMappic;
using Microsoft.Extensions.DependencyInjection;

// ── Setup (Zero-Reflection / AOT-Friendly Registration) ──
var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
services.AddAutoMappic(); // Discovers Profiles AND [AutoMap] candidates
var serviceProvider = services.BuildServiceProvider();
IMapper mapper = serviceProvider.GetRequiredService<IMapper>();

// ── Simulate services ─────────────────────────────────────────────────────────
var service = new UserService(mapper);
var orderService = new OrderService(mapper);

var userDto = service.GetUser(1);
var orderDto = orderService.GetOrder(42);

Console.WriteLine("=== AutoMappic v0.7.0 \"The Ultimate\" Sample ===");
Console.WriteLine();
Console.WriteLine("User mapping (direct + flattened + method):");
Console.WriteLine($"  Id:          {userDto.Id}");
Console.WriteLine($"  Username:    {userDto.Username}");
Console.WriteLine($"  Email:       {userDto.Email}");
Console.WriteLine($"  City:        {userDto.AddressCity}   ← flattened from Address.City");
Console.WriteLine($"  DisplayName: {userDto.DisplayName}  ← mapped from GetDisplayName()");
Console.WriteLine();

// ── New in v0.6: High-Performance LINQ Projections ─────────────────────────
Console.WriteLine("LINQ Projections (ProjectTo):");
var mockDb = MockDatabase.Users.AsQueryable();

// ProjectTo<T> translates the mapping directly to the LINQ provider (IQueryable).
// This is ultra-performant as it avoids mapping entire source objects into memory.
var projectedUsers = mockDb.ProjectTo<UserDto>().ToList();
Console.WriteLine($"  Projected {projectedUsers.Count} users efficiently from IQueryable");

foreach (var u in projectedUsers)
{
    Console.WriteLine($"  - {u.Username} ({u.AddressCity})");
}
Console.WriteLine();

// ── New in v0.6: Attribute-Based 'Zero-Touch' Mapping ───────────────────────
Console.WriteLine("Attribute-Based Mapping ([AutoMap]):");
var profile = new ProfileItem { Name = "Full Access", CreatedAt = DateTime.Now };

// ProfileDto is mapped via [AutoMap] attribute below — no Profile class needed!
var profileDto = mapper.Map<ProfileDto>(profile);
Console.WriteLine($"  Profile: {profileDto.Name} (Created: {profileDto.CreatedAt})");
Console.WriteLine();

Console.WriteLine("AutoMappic: High-Performance · Zero Reflection · ProjectTo support · Native AOT");

// ─── Domain model ─────────────────────────────────────────────────────────────

public sealed class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Address? Address { get; set; }
    public string GetDisplayName() => $"{Username} <{Email}>";
}

public sealed class Address
{
    public string City { get; set; } = string.Empty;
    public string Street { get; set; } = string.Empty;
}

public sealed class Order
{
    public int Id { get; set; }
    public Customer? Customer { get; set; }
    public decimal TotalAmount { get; set; }
}

public sealed class Customer
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public sealed class ProfileItem
{
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public sealed class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AddressCity { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class OrderDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
}

// v0.6 Attribute Mapping: This partial class is automatically discovered.
// AutoMappic generates the mapping code directly inside this partial class.
[AutoMap(typeof(ProfileItem))]
public partial class ProfileDto
{
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// ─── Mock Database ───────────────────────────────────────────────────────────

public static class MockDatabase
{
    public static List<User> Users = new()
    {
        new User { Id = 1, Username = "alice", Email = "alice@example.com", Address = new Address { City = "Stockholm" } },
        new User { Id = 2, Username = "bob", Email = "bob@oslo.no", Address = new Address { City = "Oslo" } },
        new User { Id = 3, Username = "charlie", Email = "charlie@denmark.dk", Address = new Address { City = "Copenhagen" } }
    };
}

// ─── Profiles ─────────────────────────────────────────────────────────────────

public sealed class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        CreateMap<User, UserDto>();
    }
}

public sealed class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}

// ─── Services ─────────────────────────────────────────────────────────────────

public sealed class UserService(IMapper mapper)
{
    public UserDto GetUser(int id) =>
        mapper.Map<User, UserDto>(MockDatabase.Users.First(u => u.Id == id));
}

public sealed class OrderService(IMapper mapper)
{
    public OrderDto GetOrder(int id) =>
        mapper.Map<Order, OrderDto>(new Order
        {
            Id = id,
            Customer = new Customer { Name = "Acme Corp", Email = "acme@corp.com" },
            TotalAmount = 1_250.00m,
        });
}
