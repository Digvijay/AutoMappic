using System;
using System.Collections.Generic;
using System.Linq;
using AutoMappic;
using AutoMappic.Samples.EntitySync;

Console.WriteLine("AutoMappic v0.5.0: Advanced Entity Sync (Smart-Sync) Sample");
Console.WriteLine("-----------------------------------------------------------");

// Setup AutoMappic
var mapper = new MapperConfiguration(cfg => cfg.AddProfile<BlogMappingProfile>())
    .CreateMapper();

// 1. Initial State: A blog with existing posts and tags in "Database"
var blogEntity = new Blog
{
    Id = 1,
    Title = "AutoMappic Development Journal",
    Posts = new List<Post>
    {
        new Post 
        { 
            Id = 101, 
            Content = "High Performance 0.2.0", 
            Status = PostStatus.Published,
            Tags = new List<Tag> { new Tag { Id = 501, Name = "DotNet" } }
        }
    }
};

// Keep track of references to verify preservation
var initialPost101 = blogEntity.Posts[0];
var initialTag501 = blogEntity.Posts[0].Tags[0];

Console.WriteLine($"[DB] Existing State:");
Console.WriteLine($" - Blog ID {blogEntity.Id}: \"{blogEntity.Title}\"");
Console.WriteLine($"   - Post {initialPost101.Id}: \"{initialPost101.Content}\" (Status: {initialPost101.Status})");
Console.WriteLine($"     - Tag {initialTag501.Id}: \"{initialTag501.Name}\" (Reference: {initialTag501.GetHashCode()})");

// 2. Incoming DTO: Update content, change status, and add a NEW tag to existing post
var blogUpdate = new BlogDto
{
    Id = 1,
    Title = "AutoMappic Official Blog",
    SourceApp = "AdminPortal.v2", // Demonstrates BeforeMap hook
    Posts = new List<PostDto>
    {
        new PostDto 
        { 
            Id = 101, 
            Content = "High Performance Core - Optimized", 
            Status = "Archived", // Demonstrates ValueConverter mapping string -> Enum
            Tags = new List<TagDto> 
            { 
                new TagDto { Id = 501, Name = "DotNet Core" }, // update existing tag
                new TagDto { Id = 502, Name = "Performance" }  // add new tag
            }
        }
    }
};

Console.WriteLine("\n[API] Mapping incoming BlogDto complex update...");
mapper.Map(blogUpdate, blogEntity);

// 3. Verification
Console.WriteLine("\n[DB] Updated State Verification:");
Console.WriteLine($" - Blog ID {blogEntity.Id}: \"{blogEntity.Title}\"");
Console.WriteLine($" - Last Modified By: {blogEntity.LastModifiedBy} (at {blogEntity.UpdatedAt:HH:mm:ss})");

var post101 = blogEntity.Posts.First();
Console.WriteLine($"   - Post {post101.Id}: \"{post101.Content}\" (Status: {post101.Status})");
Console.WriteLine($"     - Tag Count: {post101.Tags.Count}");
foreach(var t in post101.Tags) Console.WriteLine($"       - Tag {t.Id}: \"{t.Name}\" (Reference: {t.GetHashCode()})");

// Reference Checks
bool postRefPreserved = ReferenceEquals(post101, initialPost101);
bool tagRefPreserved = ReferenceEquals(post101.Tags.FirstOrDefault(x => x.Id == 501), initialTag501);

Console.WriteLine("\n-----------------------------------------------------------");
if (postRefPreserved && tagRefPreserved)
{
    Console.WriteLine("SUCCESS: Deep Reference Sync works across nested collections (Many-to-Many)!");
}
else
{
    Console.WriteLine("FAILURE: Reference preservation failed.");
}

if (post101.Status == PostStatus.Archived)
{
    Console.WriteLine("SUCCESS: Enum ValueConverter string-to-enum mapping worked!");
}

if (blogEntity.LastModifiedBy == "AdminPortal.v2")
{
    Console.WriteLine("SUCCESS: Lifecycle Hook (BeforeMap) updated audit fields!");
}
