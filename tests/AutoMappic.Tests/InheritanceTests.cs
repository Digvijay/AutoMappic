using AutoMappic.Tests.Fixtures;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

// ─── Fixtures ─────────────────────────────────────────────────────────────

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class Employee : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}

public class EmployeeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}

// ─── Tests ────────────────────────────────────────────────────────────────

public sealed class InheritanceMappingTests
{
    private sealed class EmployeeProfile : Profile
    {
        public EmployeeProfile()
        {
            CreateMap<Employee, EmployeeDto>();
        }
    }

    /// <summary>Verify that properties from base classes are correctly mapped into the destination DTO</summary>
    [Fact]
    public void Map_DerivedType_IncludesBaseProperties()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<EmployeeProfile>())
            .CreateMapper();

        var employee = new Employee
        {
            Id = 501,
            Name = "Alice",
            CreatedAt = DateTime.UtcNow,
            Department = "Engineering"
        };

        var dto = mapper.Map<Employee, EmployeeDto>(employee);

        Assert.Equal(501, dto.Id);
        Assert.Equal("Alice", dto.Name);
        Assert.Equal("Engineering", dto.Department);
    }
}
