using AutoMappic.Tests.Fixtures;

namespace AutoMappic.Tests.Profiles;

/// <summary>Profile that exercises direct matching, enums, and PascalCase flattening.</summary>
public sealed class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<User, UserDto>();
    }
}

/// <summary>Profile that exercises nested flattening across two levels.</summary>
public sealed class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
    }
}

/// <summary>Profile for deep hierarchical flattening (3+ levels).</summary>
public sealed class OrganizationProfile : Profile
{
    public OrganizationProfile()
    {
        CreateMap<Organization, OrganizationDto>();
    }
}

/// <summary>Profile for testing value type and nullable conversions.</summary>
public sealed class ValueTypeProfile : Profile
{
    public ValueTypeProfile()
    {
        CreateMap<ValueTypeSource, ValueTypeDto>();
    }
}

/// <summary>A simple summary profile.</summary>
public sealed class SummaryProfile : Profile
{
    public SummaryProfile()
    {
        CreateMap<User, UserSummaryDto>();
    }
}
