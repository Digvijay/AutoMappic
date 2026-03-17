using AutoMappic.Tests.Fixtures;
using AutoMappic.Tests.Profiles;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

/// <summary>Tests for the runtime <see cref="Mapper" /> fallback (no generator running).</summary>
public sealed class MapperConfigurationTests
{
    private readonly IMapper _mapper;

    public MapperConfigurationTests()
    {
        _mapper = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<UserProfile>();
            cfg.AddProfile<OrderProfile>();
            cfg.AddProfile<SummaryProfile>();
        }).CreateMapper();
    }

    /// <summary> Verify that the mapper configuration factory returns a valid IMapper instance </summary>
    [Fact]
    public void CreateMapper_ReturnsNonNullMapper()
    {
        Assert.NotNull(_mapper);
    }

    /// <summary> Confirm that basic property-to-property mapping works on the runtime fallback mapper </summary>
    [Fact]
    public void Map_DirectProperties_AreCopied()
    {
        var user = new User { Id = 42, Username = "alice", Email = "alice@example.com" };

        var dto = _mapper.Map<User, UserDto>(user);

        Assert.Equal(42, dto.Id);
        Assert.Equal("alice", dto.Username);
        Assert.Equal("alice@example.com", dto.Email);
    }

    /// <summary> Ensure that passing null as a source to the generic Map method throws the appropriate exception </summary>
    [Fact]
    public void Map_WithNullSource_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _mapper.Map<User, UserDto>(null!));
    }

    /// <summary> Verify that attempting to map between types without a valid Profile registration throws a configuration exception </summary>
    [Fact]
    public void Map_UnregisteredTypePair_ThrowsAutoMappicException()
    {
        Assert.Throws<AutoMappicException>(() => _mapper.Map<User, OrderDto>(new User()));
    }

    /// <summary> Validate that the mapper can correctly update an existing object instance rather than creating a new one </summary>
    [Fact]
    public void Map_IntoExistingDestination_MutatesInPlace()
    {
        var user = new User { Id = 7, Username = "bob", Email = "bob@example.com" };
        var existing = new UserDto { Id = 0, Username = "stale" };

        var result = _mapper.Map<User, UserDto>(user, existing);

        Assert.Same(existing, result);
        Assert.Equal(7, existing.Id);
        Assert.Equal("bob", existing.Username);
    }

    /// <summary> Confirm that mapping into a Summary DTO correctly initializes only the requested overlapping fields </summary>
    [Fact]
    public void Map_SummaryDto_OnlyMapsRequestedFields()
    {
        var user = new User { Id = 1, Username = "carol", Email = "carol@example.com" };

        var summary = _mapper.Map<User, UserSummaryDto>(user);

        Assert.Equal("carol", summary.Username);
        Assert.Equal("carol@example.com", summary.Email);
    }
}

/// <summary>Verifies <see cref="MapperConfiguration" /> factory behaviour.</summary>
public sealed class MapperConfigurationFactoryTests
{
    /// <summary> Ensure that Profile instances can be manually added to the configuration </summary>
    [Fact]
    public void AddProfile_ByInstance_IsRegistered()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile(new UserProfile()));
        var mapper = config.CreateMapper();

        var user = new User { Id = 1, Username = "dave" };
        var dto = mapper.Map<User, UserDto>(user);

        Assert.Equal(1, dto.Id);
    }

    /// <summary> Verify that attempting to add a null Profile instance results in an immediate exception </summary>
    [Fact]
    public void AddProfile_NullInstance_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new MapperConfiguration(cfg => cfg.AddProfile(null!)));
    }
}

/// <summary>Tests for the <see cref="AutoMappicException" /> type.</summary>
public sealed class AutoMappicExceptionTests
{
    /// <summary> Validate that the custom library exception correctly stores and returns its error message </summary>
    [Fact]
    public void Constructor_WithMessage_SetsMessage()
    {
        var ex = new AutoMappicException("test message");
        Assert.Equal("test message", ex.Message);
    }

    /// <summary> Validate that the custom library exception correctly preserves the inner cause of the failure </summary>
    [Fact]
    public void Constructor_WithInner_SetsInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new AutoMappicException("outer", inner);

        Assert.Same(inner, ex.InnerException);
    }

    /// <summary> Sanity check to confirm the library exception properly inherits from the base System.Exception class </summary>
    [Fact]
    public void AutoMappicException_IsException()
    {
        Assert.True(typeof(AutoMappicException).IsSubclassOf(typeof(Exception)));
    }
}
