using System;
using System.Collections.Generic;
using AutoMappic;

namespace AutoMappic.Samples.EntitySync;

// ─── Enums & Value Objects ──────────────────────────────────────────────────

public enum PostStatus
{
    Draft,
    Published,
    Archived
}

// ─── Entities (EF Core style) ───────────────────────────────────────────────

public class Blog
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    [AutoMappicIgnore]
    public string? LastModifiedBy { get; set; }
    [AutoMappicIgnore]
    public DateTime? UpdatedAt { get; set; }
    
    // 1-to-Many
    public List<Post> Posts { get; set; } = new();
}

public class Post
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int BlogId { get; set; }
    public PostStatus Status { get; set; }
    
    // Many-to-Many (Tags)
    public List<Tag> Tags { get; set; } = new();
}

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// ─── DTOs (The input for the mapping) ────────────────────────────────────────

public class BlogDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    
    // We'll map this using BeforeMap to LastModifiedBy
    [AutoMappicIgnore]
    public string? SourceApp { get; set; }
    
    public List<PostDto> Posts { get; set; } = new();
}

public class PostDto
{
    [AutoMappicKey]
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Status { get; set; } = "Draft"; // string in DTO -> enum in Entity
    public int BlogId { get; set; }
    
    public List<TagDto> Tags { get; set; } = new();
}

public class TagDto
{
    [AutoMappicKey]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
