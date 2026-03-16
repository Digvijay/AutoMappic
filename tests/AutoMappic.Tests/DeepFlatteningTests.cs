using AutoMappic.Tests.Fixtures;
using AutoMappic.Tests.Profiles;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public sealed class DeepFlatteningTests
{
    private readonly IMapper _mapper;

    public DeepFlatteningTests()
    {
        _mapper = new MapperConfiguration(cfg => cfg.AddProfile<OrganizationProfile>())
            .CreateMapper();
    }

    [Fact]
    public void Map_DeepHierarchy_ResolvesThreeLevelFlattening()
    {
        // Setup: Organization -> SupportTeam (Department) -> Lead (Manager) -> FullName
        var org = new Organization
        {
            Name = "OpenAI",
            SupportTeam = new Department
            {
                Code = "SUPP",
                Lead = new Manager
                {
                    FullName = "Sam Altman",
                    OfficeLocation = "San Francisco"
                }
            }
        };

        // Note: The generator intercepts this. The runtime fallback won't flatten.
        // But for diagnostic testing of the Interceptor itself, we rely on build success.
        // Here we test the fallback or generic behavior.
        var dto = _mapper.Map<Organization, OrganizationDto>(org);

        Assert.Equal("OpenAI", dto.Name);

        // Due to the Interceptor working in the Test Project, we get the true AOT result here!
        Assert.Equal("SUPP", dto.SupportTeamCode);
        Assert.Equal("Sam Altman", dto.SupportTeamLeadFullName);
    }
}
