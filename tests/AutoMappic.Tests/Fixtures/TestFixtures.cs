// ─────────────────────────────────────────────────────────────────────────────
// Test fixtures
// ─────────────────────────────────────────────────────────────────────────────

namespace AutoMappic.Tests.Fixtures;

// ─── Enums & Value Types ──────────────────────────────────────────────────

public enum UserStatus
{
    Active,
    Inactive,
    Pending
}

// ─── Base Models ─────────────────────────────────────────────────────────

public sealed class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Address? Address { get; set; }
    public UserStatus Status { get; set; }
    public string GetDisplayName() => $"{Username} <{Email}>";
}

public sealed class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
}

// ─── Hierarchy / Deep Flattening ────────────────────────────────────────

public sealed class Organization
{
    public string Name { get; set; } = string.Empty;
    public Department? SupportTeam { get; set; }
}

public sealed class Department
{
    public string Code { get; set; } = string.Empty;
    public Manager? Lead { get; set; }
}

public sealed class Manager
{
    public string FullName { get; set; } = string.Empty;
    public string OfficeLocation { get; set; } = string.Empty;
}

// ─── Orders & Customers ──────────────────────────────────────────────────

public sealed class Order
{
    public int Id { get; set; }
    public Customer? Customer { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
}

public sealed class Customer
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// ─── Destination (DTO) types ─────────────────────────────────────────────

public sealed class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AddressCity { get; set; } = string.Empty;     // Flattened
    public string AddressStreet { get; set; } = string.Empty;   // Flattened
    public UserStatus Status { get; set; }
    public string DisplayName { get; set; } = string.Empty;     // Method map
}

public sealed class OrganizationDto
{
    public string Name { get; set; } = string.Empty;
    public string SupportTeamCode { get; set; } = string.Empty;           // 2-level flatten
    public string SupportTeamLeadFullName { get; set; } = string.Empty;   // 3-level flatten
}

public sealed class OrderDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;    // Flattened
    public string CustomerEmail { get; set; } = string.Empty;   // Flattened
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
}

public sealed class ValueTypeDto
{
    public int? NullableInt { get; set; }
    public int NonNullableInt { get; set; }
    public bool Flag { get; set; }
}

public sealed class ValueTypeSource
{
    public int NullableInt { get; set; }
    public int? NonNullableInt { get; set; }
    public bool Flag { get; set; }
}

public sealed class UserSummaryDto
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
