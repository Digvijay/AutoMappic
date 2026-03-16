using System;
using System.Collections.Generic;
using AutoMappic;
using eShopSample;

// ─────────────────────────────────────────────────────────────────────────────
// AutoMappic eShop Modern Case Study
//
// This sample demonstrates AutoMappic handling the exact mapping patterns 
// found in the official dotnet/eShop microservices reference architecture.
// ─────────────────────────────────────────────────────────────────────────────

var config = new MapperConfiguration(cfg =>
{
    cfg.AddProfile<eShopMappingProfile>();
});

IMapper mapper = config.CreateMapper();

Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║        AutoMappic: Official dotnet/eShop Comparison          ║");
Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
Console.WriteLine();

// 1. Catalog Mapping (Direct Property Projection)
var item = new CatalogItem 
{ 
    Id = 1, 
    Name = ".NET Blue Hoodie", 
    Description = "Premium .NET branded hoodie",
    Price = 45.00m,
    PictureFileName = "hoodie_blue.png"
};

var itemDto = mapper.Map<CatalogItem, CatalogItemDto>(item);

Console.WriteLine("--- 1. Catalog API Mapping ---");
Console.WriteLine($"Entity Name: {item.Name}");
Console.WriteLine($"DTO Name:    {itemDto.Name} (Primary Ctor Mapped)");

// Verify ReverseMap
var itemFromDto = mapper.Map<CatalogItemDto, CatalogItem>(itemDto);
Console.WriteLine($"Reverse Map: {itemFromDto.Name} (Back to Entity)");
Console.WriteLine();

// 2. Ordering Mapping (Complex Flattening + Nested Collections)
var order = new Order
{
    OrderNumber = 56231,
    OrderDate = DateTime.Now,
    Status = "Submitted",
    ShippingAddress = new Address { City = "Redmond", State = "WA", Street = "One Microsoft Way", ZipCode = "98052" },
    OrderItems = new List<OrderItem>
    {
        new() { ProductName = ".NET Mug", UnitPrice = 12.00m, Units = 2 },
        new() { ProductName = "C# T-Shirt", UnitPrice = 25.00m, Units = 1 }
    },
    Total = 49.00m
};

var orderSummary = mapper.Map<Order, OrderSummaryDto>(order);

Console.WriteLine("--- 2. Ordering API (Deep Flattening + Nested Collection) ---");
Console.WriteLine($"Order #: {orderSummary.OrderNumber}");
Console.WriteLine($"Address: {orderSummary.ShippingAddressCity}, {orderSummary.ShippingAddressState}  ← Flattened");
Console.WriteLine();

// 3. Efficiency Check
Console.WriteLine("--- Performance Insight ---");
Console.WriteLine("In dotnet/eShop on Kubernetes, every millisecond of startup and every ");
Console.WriteLine("byte of allocation matters.");
Console.WriteLine();
Console.WriteLine("[AutoMapper Strategy]:   Runtime Reflection + Dynamic IL emission");
Console.WriteLine("[AutoMappic Strategy]:   Compile-time Interception (0% Reflection)");
Console.WriteLine();
Console.WriteLine("Result: Instantly Native AOT ready. Zero startup delay.");
Console.WriteLine("─────────────────────────────────────────────────────────────────");

// --- Profile ---

public class eShopMappingProfile : Profile
{
    public eShopMappingProfile()
    {
        // Mirrors dotnet/eShop's mapping registration:
        CreateMap<CatalogItem, CatalogItemDto>()
            .ReverseMap(); // Now supported!
        
        CreateMap<Order, OrderSummaryDto>()
            .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.OrderDate));
            
        CreateMap<OrderItem, OrderItemDto>();
    }
}
