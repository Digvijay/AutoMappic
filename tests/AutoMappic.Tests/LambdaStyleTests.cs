using AutoMappic;

namespace AutoMappic.Tests;

public class LambdaStyleProfile : Profile
{
    public LambdaStyleProfile()
    {
        // Parenthesized lambda style - might fail extraction!
        CreateMap<Source, Dest>()
            .ForMember((d) => d.Value, (opt) => opt.MapFrom((src) => src.SourceValue));
    }

    public class Source { public int SourceValue { get; set; } }
    public class Dest { public int Value { get; set; } }
}
