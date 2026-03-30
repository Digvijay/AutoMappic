using System;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.PublicApi;

// THE WIN: Just swap the namespace
// From: using AutoMapper;
using AutoMappic; 

Console.WriteLine("╔═══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║        AutoMappic: Official eShopOnWeb Migration Win         ║");
Console.WriteLine("╚═══════════════════════════════════════════════════════════════╝");
Console.WriteLine();

// 1. Setup Configuration (Exact same syntax as AutoMapper)
var config = new MapperConfiguration(cfg =>
{
    cfg.AddProfile<MappingProfile>();
});

IMapper mapper = config.CreateMapper();

// 2. Map an Item (Exact same syntax)
var entity = new CatalogItem(1, 2, 3, "Essential .NET Book", "Modern C# 12", 29.99m, "book.png");
var dto = mapper.Map<CatalogItem, CatalogItemDto>(entity);

Console.WriteLine("--- Mapping CatalogItem (Entity -> DTO) ---");
Console.WriteLine($"Entity: {entity.Name} (Price: {entity.Price})");
Console.WriteLine($"DTO:    {dto.Name} (Price: {dto.Price})");
Console.WriteLine();

// 3. Map a Brand (Custom ForMember win)
var brand = new CatalogBrand(10, "Microsoft Press");
var brandDto = mapper.Map<CatalogBrand, CatalogBrandDto>(brand);

Console.WriteLine("--- Mapping CatalogBrand (Custom ForMember Win) ---");
Console.WriteLine($"Source: {brand.Brand}");
Console.WriteLine($"DTO:    {brandDto.Name}  ← Mapped via .ForMember(d => d.Name, s => s.Brand)");
Console.WriteLine();

// 4. Batch Mapping (Mirrors eShopOnWeb's CatalogItemListPagedEndpoint)
var items = new List<CatalogItem>
{
    new(1, 1, 1, "Hoodie", "Blue Hoodie", 45.00m, "h1.png"),
    new(2, 1, 1, "Mug", "C# Mug", 12.00m, "m1.png")
};

var dtos = items.Select(mapper.Map<CatalogItem, CatalogItemDto>).ToList();

Console.WriteLine("--- Batch Mapping (List Projection) ---");
Console.WriteLine($"Mapped {dtos.Count} items from the catalog.");
foreach(var d in dtos) Console.WriteLine($"  - {d.Name} (${d.Price})");
Console.WriteLine();

Console.WriteLine("─────────────────────────────────────────────────────────────────");
Console.WriteLine("RESULT: One Namespace change. Zero Reflection. 100% AOT Ready.");
Console.WriteLine("─────────────────────────────────────────────────────────────────");

// --- THE EXACT PROFILE FROM ESHOPONWEB ---
// Just the 'using' changed at the top of the file!

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CatalogItem, CatalogItemDto>();
        
        // Custom name mappings exactly as in Microsoft's reference app
        CreateMap<CatalogType, CatalogTypeDto>()
            .ForMember(dto => dto.Name, options => options.MapFrom(src => src.Type));
            
        CreateMap<CatalogBrand, CatalogBrandDto>()
            .ForMember(dto => dto.Name, options => options.MapFrom(src => src.Brand));
    }
}
