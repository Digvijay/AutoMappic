using AutoMappic.Tests.Fixtures;
using AutoMappic.Tests.Profiles;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public sealed class EnumMappingTests
{
    private readonly IMapper _mapper;

    public EnumMappingTests()
    {
        _mapper = new MapperConfiguration(cfg => cfg.AddProfile<UserProfile>())
            .CreateMapper();
    }

    /// <summary> Confirm that enum values are correctly copied between matching enum types </summary>
    [Fact]
    public void Map_Enum_CopiedCorrectly()
    {
        var source = new User { Username = "tester", Status = UserStatus.Pending };
        var dto = _mapper.Map<User, UserDto>(source);

        Assert.Equal(UserStatus.Pending, dto.Status);
    }
}

public sealed class ValueTypeMappingTests
{
    private readonly IMapper _mapper;

    public ValueTypeMappingTests()
    {
        _mapper = new MapperConfiguration(cfg => cfg.AddProfile<ValueTypeProfile>())
            .CreateMapper();
    }

    /// <summary> Validate that nullable source primitives are correctly mapped to non-nullable destination types </summary>
    [Theory]
    [InlineData(10, 10)]
    [InlineData(0, 0)]
    [InlineData(-5, -5)]
    public void Map_NullableToNonNullable_Theories(int sourceVal, int expected)
    {
        var source = new ValueTypeSource { NonNullableInt = sourceVal };
        var dto = _mapper.Map<ValueTypeSource, ValueTypeDto>(source);
        Assert.Equal(expected, dto.NonNullableInt);
    }

    /// <summary> Ensure that a null source value is correctly handled by using its default sentinel </summary>
    [Fact]
    public void Map_NullableWithNull_UsesDefault()
    {
        var source = new ValueTypeSource { NonNullableInt = null };
        var dto = _mapper.Map<ValueTypeSource, ValueTypeDto>(source);
        Assert.Equal(0, dto.NonNullableInt);
    }

    /// <summary> Verify that boolean flags are correctly transferred between types </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Map_Bool_CopiedCorrectly(bool val)
    {
        var source = new ValueTypeSource { Flag = val };
        var dto = _mapper.Map<ValueTypeSource, ValueTypeDto>(source);
        Assert.Equal(val, dto.Flag);
    }

    /// <summary> Validate mapping from a non-nullable type to its nullable counterpart </summary>
    [Fact]
    public void Map_NonNullableToNullable_CopiedCorrectly()
    {
        var source = new ValueTypeSource { NullableInt = 100 };
        var dto = _mapper.Map<ValueTypeSource, ValueTypeDto>(source);

        Assert.Equal(100, dto.NullableInt);
    }
}
