using System.Threading.Tasks;
using AutoMappic;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public class ArchitecturalHardeningTests
{
    public class Blog { public int Id { get; set; } public string Name { get; set; } = ""; public global::System.Collections.Generic.List<Post> Posts { get; set; } = new(); }
    public class Post { public int Id { get; set; } public string Title { get; set; } = ""; }
    public class BlogDto { public int Id { get; set; } public string Name { get; set; } = ""; public global::System.Collections.Generic.List<PostDto> Posts { get; set; } = new(); }
    public class PostDto { public int Id { get; set; } public string Title { get; set; } = ""; }

    public class PostAsyncResolver : IAsyncValueResolver<Post, string>
    {
        public global::System.Threading.Tasks.Task<string> ResolveAsync(Post source)
        {
            return global::System.Threading.Tasks.Task.FromResult(source.Title + " (Async)");
        }
    }

    public class HardenedProfile : Profile
    {
        public HardenedProfile()
        {
            EnableEntitySync = true;
            CreateMap<Post, PostDto>()
                .ForMember(dest => dest.Title, opt => opt.MapFromAsync<PostAsyncResolver>());

            CreateMap<Blog, BlogDto>(); // Should be promoted to Async because Post is Async
        }
    }

    [Fact]
    public async Task TransitiveAsyncPropagation_Works()
    {
        var blog = new Blog { Id = 1, Name = "Test Blog", Posts = new global::System.Collections.Generic.List<Post> { new Post { Id = 101, Title = "Post 1" } } };
        IMapper mapper = new MapperConfiguration(cfg => cfg.AddProfile<HardenedProfile>()).CreateMapper();

        // This would have failed to compile/run correctly before the fix
        var dto = await mapper.MapAsync<Blog, BlogDto>(blog);

        Assert.NotNull(dto);
        Assert.Single(dto.Posts);
        Assert.Equal("Post 1 (Async)", dto.Posts[0].Title);
    }

    public class SyncHardeningProfile : Profile
    {
        public SyncHardeningProfile()
        {
            EnableEntitySync = true;
            CreateMap<PostDto, Post>();
            CreateMap<BlogDto, Blog>();
            CreateMap<BlogDto, ReadOnlyBlog>();
        }
    }

    [Fact]
    public void SmartSync_KeyCollision_PreventsSilentOverwrite()
    {
        var blog = new Blog
        {
            Id = 1,
            Posts = new global::System.Collections.Generic.List<Post> {
                new Post { Id = 101, Title = "Original 1" },
                new Post { Id = 101, Title = "Original 2 (Collision)" }
            }
        };
        var update = new BlogDto
        {
            Id = 1,
            Posts = new global::System.Collections.Generic.List<PostDto> {
                new PostDto { Id = 101, Title = "Updated" }
            }
        };

        IMapper mapper = new MapperConfiguration(cfg => cfg.AddProfile<SyncHardeningProfile>()).CreateMapper();

        // Before fix: loop would just overwrite.
        // After fix: TryAdd handles it (takes first one found in destination).
        mapper.Map(update, blog);

        Assert.Equal(2, blog.Posts.Count);
        Assert.Equal("Updated", blog.Posts[0].Title);
        Assert.Equal("Original 2 (Collision)", blog.Posts[1].Title);
    }

    public class ReadOnlyBlog { public global::System.Collections.Generic.IReadOnlyList<Post> Posts { get; } = new global::System.Collections.Generic.List<Post>().AsReadOnly(); }

    [Fact]
    public void SmartSync_ReadOnlyCollection_PreventsCrash()
    {
        var blog = new ReadOnlyBlog();
        var update = new BlogDto { Posts = new global::System.Collections.Generic.List<PostDto> { new PostDto { Id = 1, Title = "Test" } } };

        IMapper mapper = new MapperConfiguration(cfg => cfg.AddProfile<SyncHardeningProfile>()).CreateMapper();

        // Should NOT throw global::System.NotSupportedException because of the added IsReadOnly check
        try
        {
            mapper.Map(update, blog);
        }
        catch (global::System.Exception ex)
        {
            Assert.Fail($"Mapping should not throw on ReadOnly collections. Exception: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
        }
    }
}
