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
services.AddAutoMappic(); // Discovers UserMappingProfile and OrderMappingProfile at compile-time
var serviceProvider = services.BuildServiceProvider();
IMapper mapper = serviceProvider.GetRequiredService<IMapper>();

// ── Simulate a service using the mapper (identical to AutoMapper usage) ──────
var service = new UserService(mapper);
var orderService = new OrderService(mapper);

var userDto = service.GetUser(1);
var orderDto = orderService.GetOrder(42);

Console.WriteLine("=== AutoMappic Sample ===");
Console.WriteLine();
Console.WriteLine("User mapping (direct + flattened + method):");
Console.WriteLine($"  Id:          {userDto.Id}");
Console.WriteLine($"  Username:    {userDto.Username}");
Console.WriteLine($"  Email:       {userDto.Email}");
Console.WriteLine($"  City:        {userDto.AddressCity}   ← flattened from Address.City");
Console.WriteLine($"  DisplayName: {userDto.DisplayName}  ← mapped from GetDisplayName()");
Console.WriteLine();
Console.WriteLine("Order mapping (two-level flattening):");
Console.WriteLine($"  Id:            {orderDto.Id}");
Console.WriteLine($"  CustomerName:  {orderDto.CustomerName}   ← flattened from Customer.Name");
Console.WriteLine($"  CustomerEmail: {orderDto.CustomerEmail}  ← flattened from Customer.Email");
Console.WriteLine($"  TotalAmount:   {orderDto.TotalAmount:C}");
Console.WriteLine();
Console.WriteLine("AutoMappic: Convention-Based · Source-Generated · Zero Reflection · Native AOT");

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

// ─── Services (identical to how they'd look with AutoMapper) ──────────────────

public sealed class UserService(IMapper mapper)
{
    public UserDto GetUser(int id) =>
        mapper.Map<User, UserDto>(new User
        {
            Id = id,
            Username = "alice",
            Email = "alice@automappic.digvijay.dev",
            Address = new Address { City = "Stockholm", Street = "Kungsgatan 1" },
        });
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
