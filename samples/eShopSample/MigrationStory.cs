using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// 🟢 STEP 1: Just change the namespaces
// from: using AutoMapper;
using AutoMappic; 
using Microsoft.Extensions.DependencyInjection;

namespace eShopSample;

// --- In your Program.cs / Startup.cs ---
// builder.Services.AddAutoMappic(typeof(Program).Assembly); // 🟢 STEP 2: Swap the DI extension

// ─────────────────────────────────────────────────────────────────────────────
// The "Microsoft eShop Win" Demonstration
//
// This file contains a Service/Controller exactly as it would appear in the
// official dotnet/eShop repository. It uses IMapper via Dependency Injection.
//
// AUTO-MAPPIC WIN: 
// 1. Identical Syntax (CreateMap, ReverseMap, ForMember).
// 2. 0% Reflection at Runtime.
// 3. 100% Native AOT Compatible.
// ─────────────────────────────────────────────────────────────────────────────

public class CatalogService
{
    private readonly IMapper _mapper;

    public CatalogService(IMapper mapper)
    {
        _mapper = mapper;
    }

    public async Task<CatalogItemDto> GetItemById(int id)
    {
        // Simulate DB fetch
        var entity = new CatalogItem 
        { 
            Id = id, 
            Name = ".NET Blue Hoodie", 
            PictureFileName = "blue_h.png",
            Price = 45.00m 
        };

        // 🟢 STEP 2: Use the exact same Mapping call
        return _mapper.Map<CatalogItem, CatalogItemDto>(entity);
    }
}

// The mapping profile is already defined in Program.cs.
// Defining it again here would cause a duplicate mapping error (AM009),
// which verifies AutoMappic's strict architecture enforcement.
