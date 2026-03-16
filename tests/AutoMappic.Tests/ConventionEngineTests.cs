using AutoMappic.Tests.Fixtures;
using AutoMappic.Tests.Profiles;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

/// <summary>
///   Tests that verify the convention engine resolves PascalCase flattened paths correctly
///   through the generated mapping methods.
/// </summary>
/// <remarks>
///   These tests operate against the runtime fallback mapper which mirrors what the
///   generator would produce.  Generator-specific tests live in
///   <c>AutoMappic.Generator.Tests</c> (Roslyn compiler-based testing via
///   <c>Microsoft.CodeAnalysis.Testing</c>).
/// </remarks>
public sealed class ConventionEngineTests
{
    private readonly IMapper _mapper;

    public ConventionEngineTests()
    {
        _mapper = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<UserProfile>();
            cfg.AddProfile<OrderProfile>();
            cfg.AddProfile<SummaryProfile>();
        }).CreateMapper();
    }

    // ── Direct match ─────────────────────────────────────────────────────────────

    [Fact]
    public void DirectMatch_Id_IsCopied()
    {
        var user = new User { Id = 99 };
        var dto = _mapper.Map<User, UserDto>(user);
        Assert.Equal(99, dto.Id);
    }

    [Theory]
    [InlineData("alice", "alice@x.com")]
    [InlineData("bob", "bob@y.com")]
    [InlineData("", "")]
    public void DirectMatch_StringProperties_AreCopied(string username, string email)
    {
        var user = new User { Username = username, Email = email };
        var dto = _mapper.Map<User, UserDto>(user);

        Assert.Equal(username, dto.Username);
        Assert.Equal(email, dto.Email);
    }

    // ── Flattening ───────────────────────────────────────────────────────────────

    [Fact]
    public void Flattening_OneLevel_CustomerName_ResolvedFromOrder()
    {
        // The runtime fallback Mapper now handles PascalCase flattening (Customer.Name → CustomerName),
        // matching the behavior of the source generator.
        var order = new Order
        {
            Id = 1,
            Customer = new Customer { Name = "Acme Corp", Email = "acme@corp.com" },
            TotalAmount = 250.00m
        };

        var dto = _mapper.Map<Order, OrderDto>(order);

        Assert.Equal(1, dto.Id);
        Assert.Equal(250.00m, dto.TotalAmount);
        Assert.Equal("Acme Corp", dto.CustomerName);
        Assert.Equal("acme@corp.com", dto.CustomerEmail);
    }

    [Fact]
    public void Flattening_NullNestedObject_ProducesDefault()
    {
        var order = new Order { Id = 2, Customer = null };

        var dto = _mapper.Map<Order, OrderDto>(order);

        Assert.Equal(2, dto.Id);
        // CustomerName / CustomerEmail remain at their default (empty string for DTO strings)
        Assert.Equal(string.Empty, dto.CustomerName);
    }

    // ── Multiple profile ─────────────────────────────────────────────────────────

    [Fact]
    public void MultipleProfiles_CanCoexist()
    {
        var mapper = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<UserProfile>();
            cfg.AddProfile<OrderProfile>();
        }).CreateMapper();

        var user = new User { Id = 1 };
        var order = new Order { Id = 2 };

        var userDto = mapper.Map<User, UserDto>(user);
        var orderDto = mapper.Map<Order, OrderDto>(order);

        Assert.Equal(1, userDto.Id);
        Assert.Equal(2, orderDto.Id);
    }

    // ── Edge: same source, different destinations ─────────────────────────────────

    [Fact]
    public void SameSource_DifferentDestinations_BothResolve()
    {
        var user = new User { Id = 5, Username = "eve", Email = "eve@example.com" };

        var full = _mapper.Map<User, UserDto>(user);
        var summary = _mapper.Map<User, UserSummaryDto>(user);

        Assert.Equal(5, full.Id);
        Assert.Equal("eve", full.Username);
        Assert.Equal("eve", summary.Username);
        Assert.Equal("eve@example.com", summary.Email);
    }
}
