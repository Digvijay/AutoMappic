using System;
using AutoMappic;

namespace AutoMappic.Samples.EntitySync;

public sealed class BlogMappingProfile : Profile
{
    public BlogMappingProfile()
    {
        // 1. Blog Setup (Direct & Audit)
        CreateMap<Blog, BlogDto>()
            .ReverseMap()
            .BeforeMap((src, dest) => {
                // Demonstrates updating audit information during mapping
                dest.LastModifiedBy = src.SourceApp ?? "Unknown";
                dest.UpdatedAt = DateTime.UtcNow;
            });

        // 2. Post Setup (Collections & Enums)
        CreateMap<Post, PostDto>()
            .ReverseMap()
            // This ForMember applies to the REVERSE mapping (PostDto -> Post)
            // Use ConvertUsing<TConverter, TSourceMember>(src => src.Member)
            .ForMember(dest => dest.Status, opt => opt.ConvertUsing<PostStatusConverter, string>(src => src.Status));

        // 3. Many-to-Many Sync
        CreateMap<Tag, TagDto>().ReverseMap();
    }
}

// Demonstrates a custom value converter for string-to-enum mapping
public sealed class PostStatusConverter : IValueConverter<string, PostStatus>
{
    public PostStatus Convert(string source)
    {
        if (Enum.TryParse<PostStatus>(source, true, out var result))
        {
            return result;
        }
        return PostStatus.Draft;
    }
}
