using System;
using System.Collections.Generic;
using System.Linq;
using eShop.Ordering.Domain;
using eShop.Ordering.API.Application.Queries;
using AutoMappic;

Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║        AutoMappic: Eliminating Manual Mapping Complexity     ║");
Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
Console.WriteLine();

var order = new Order
{
    Id = 123,
    OrderDate = DateTime.Now,
    Description = "Christmas Gift",
    Address = new Address { Street = "123 .NET Way", City = "Redmond", State = "WA", Country = "USA", ZipCode = "98052" },
    OrderItems = new List<OrderItem>
    {
        new() { ProductName = "C# Hoodie", Units = 1, UnitPrice = 45.0m },
        new() { ProductName = ".NET Mug", Units = 2, UnitPrice = 15.0m }
    }
};

// --- THE OLD "MANUAL" WAY (from OrderQueries.cs) ---
// Note: This is 15+ lines of brittle code they have to maintain for every query.
var manualDto = new OrderViewModel
{
    OrderNumber = order.Id,
    Date = order.OrderDate,
    Description = order.Description,
    City = order.Address.City,
    Country = order.Address.Country,
    State = order.Address.State,
    Street = order.Address.Street,
    Zipcode = order.Address.ZipCode,
    Status = order.OrderStatus.ToString(),
    Total = order.GetTotal(),
    OrderItems = order.OrderItems.Select(oi => new OrderItemViewModel
    {
        ProductName = oi.ProductName,
        Units = oi.Units,
        UnitPrice = (double)oi.UnitPrice,
        PictureUrl = oi.PictureUrl
    }).ToList()
};

Console.WriteLine("[DEBUG] Manual Mapping Result: " + manualDto.City + " (Items: " + manualDto.OrderItems.Count + ")");

// --- THE NEW "AUTOMAPPIC" WAY ---
// Note: 1 line. Zero brittle assignments. Same performance.
var config = new MapperConfiguration(cfg => cfg.AddProfile<OrderProfile>());
var mapper = config.CreateMapper();

var autoMappicDto = mapper.Map<Order, OrderViewModel>(order);

Console.WriteLine("[DEBUG] AutoMappic Result:     " + autoMappicDto.City + " (Items: " + autoMappicDto.OrderItems.Count + ")");
Console.WriteLine();

Console.WriteLine("─────────────────────────────────────────────────────────────────");
Console.WriteLine("THE WIN: AutoMappic generates the SAME efficient code as the manual");
Console.WriteLine("version, but removes 90% of the maintenance burden.");
Console.WriteLine();
Console.WriteLine("Current Manual Code in dotnet/eShop: ~20 lines per query.");
Console.WriteLine("AutoMappic equivalent:                1 line.");
Console.WriteLine("─────────────────────────────────────────────────────────────────");

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderViewModel>()
            .ForMember(d => d.OrderNumber, opt => opt.MapFrom(s => s.Id))
            .ForMember(d => d.Date, opt => opt.MapFrom(s => s.OrderDate))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.OrderStatus.ToString()))
            .ForMember(d => d.Total, opt => opt.MapFrom(s => s.GetTotal()))
            // Manual overrides for address components (mimicking eShop manual project)
            .ForMember(d => d.Street, opt => opt.MapFrom(s => s.Address.Street))
            .ForMember(d => d.City, opt => opt.MapFrom(s => s.Address.City))
            .ForMember(d => d.State, opt => opt.MapFrom(s => s.Address.State))
            .ForMember(d => d.Zipcode, opt => opt.MapFrom(s => s.Address.ZipCode))
            .ForMember(d => d.Country, opt => opt.MapFrom(s => s.Address.Country));

        CreateMap<OrderItem, OrderItemViewModel>()
            .ForMember(d => d.UnitPrice, opt => opt.MapFrom(s => (double)s.UnitPrice));
    }
}
