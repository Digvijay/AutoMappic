using AutoMappic;
using eShopSample;

namespace eShopSample;

public class eShopMappingProfile : Profile
{
    public eShopMappingProfile()
    {
        CreateMap<CatalogItem, CatalogItemDto>()
            .ReverseMap();
        
        CreateMap<Order, OrderSummaryDto>()
            .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.OrderDate));
            
        CreateMap<OrderItem, OrderItemDto>();
    }
}
