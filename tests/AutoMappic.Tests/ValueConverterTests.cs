using Microsoft.Extensions.DependencyInjection;
using Prova;

namespace AutoMappic.Tests;

public class ValueConverterTests
{
    [Fact]
    public void Map_WithValueConverter_ForMember_Works()
    {
        var services = new ServiceCollection();
        services.AddAutoMappicFromAutoMappic_Tests();
        var sp = services.BuildServiceProvider();
        var mapper = sp.GetRequiredService<IMapper>();

        var source = new ConvSource { Value = 100 };
        var dest = mapper.Map<ConvDest>(source);

        Prova.Assertions.Assert.Equal(200, dest.DoubledValue);
    }

    [Fact]
    public void Map_WithMoneyConverter_Works()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<ConverterProfile>())
            .CreateMapper();

        var source = new ConvOrderSource { Price = 99.99m };
        var dest = mapper.Map<ConvOrderSource, ConvOrderDest>(source);

        Prova.Assertions.Assert.Equal("$99.99", dest.Price);
    }
}

public class ConvSource { public int Value { get; set; } }
public class ConvDest { public int DoubledValue { get; set; } }

public class DoublingConverter : IValueConverter<int, int>
{
    public int Convert(int source) => source * 2;
}

public class MoneyConverter : IValueConverter<decimal, string>
{
    public string Convert(decimal sourceMember) => $"${sourceMember:F2}";
}

public class ConvOrderSource { public decimal Price { get; set; } }
public class ConvOrderDest { public string Price { get; set; } = string.Empty; }

public class ConverterProfile : Profile
{
    public ConverterProfile()
    {
        CreateMap<ConvOrderSource, ConvOrderDest>()
            .ForMember(d => d.Price, opt => opt.ConvertUsing<MoneyConverter, decimal>(s => s.Price));

        CreateMap<ConvSource, ConvDest>()
            .ForMember(d => d.DoubledValue, opt => opt.ConvertUsing<DoublingConverter, int>(s => s.Value));
    }
}
