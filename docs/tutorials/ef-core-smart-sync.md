# AutoMappic EF Core Smart-Sync API Tutorial

A crucial, often painful scenario in any robust Line-of-Business API is updating nested lists of entity objects via a JSON payload. In this tutorial, we will build a modern ASP.NET Core Todo API that demonstrates the classic **"Entity Framework Graph Detach"** problem you face with AutoMapper, and how AutoMappic completely solves it with zero reflection and native AOT compatibility.

## The AutoMapper Pitfall

When updating a hierarchical aggregate—like a `TodoList` entity that contains multiple `TodoItem` sub-entities—AutoMapper struggles natively.

**Standard AutoMapper Behavior:**
```csharp
// AutoMapper clears the target list and creates brand new references from the source DTO
list = _mapper.Map(inputDto, list);
```
Because AutoMapper replaces existing entity instances with *new* entity instances, Entity Framework intercepts the Primary Keys on those new instances and thinks you're trying to track two unique objects with the identical `Id`. 
This results in the dreaded EF InvalidOperationException: 

> "The instance of entity type 'TodoItem' cannot be tracked because another instance with the same key value is already being tracked."

### Historical Solutions
To fix this in AutoMapper, developers have historically had to:
1. Install third-party extensions like `AutoMapper.Collection`.
2. Hand-write `EquivalencyExpression` properties.
3. Completely abandon AutoMapper on that specific graph, writing manual `foreach` and LINQ loops to map `Id` to `Id` cleanly.

## The AutoMappic "Smart-Sync" Solution

AutoMappic solves this directly inside its source-generated compile step with **Smart-Sync**, building `Dictionary` lookups natively in C# to safely replace properties in-place.

Let's build a quick API.

### 1. The Models

Start with standard EF Core Entities and your associated DTOs.

```csharp
// === Entities ===
public class TodoList
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<TodoItem> Items { get; set; } = new();
}

public class TodoItem
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsDone { get; set; }
}

// === Incoming Updates ===
public class UpdateTodoListDto
{
    public string Title { get; set; } = string.Empty;
    
    // Note how items also come in via DTOs
    public List<TodoItemDto> Items { get; set; } = new();
}

public class TodoItemDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsDone { get; set; }
}
```

### 2. AutoMappic Configuration

Tell AutoMappic to enable Smart-Sync. This lets the compiler know it should inspect nested collection items for primary keys (like `Id`).

```csharp
using AutoMappic;

public class TodoProfile : Profile
{
    public TodoProfile()
    {
        // Enable Smart-Sync so nested EF Core collections map cleanly!
        EnableEntitySync = true;

        CreateMap<UpdateTodoListDto, TodoList>()
            .ForMemberIgnore(dest => dest.Id); // We don't overwrite the Main Id on updates
            
        CreateMap<TodoItemDto, TodoItem>();
    }
}
```

### 3. The API Endpoint

Using Minimal APIs, watch how perfectly seamless a hierarchical `PUT` operation becomes. There's no longer a need to write manual ID-checking `foreach` loops.

```csharp
app.MapPut("/todo-lists/{id}", async (int id, UpdateTodoListDto input, TodoDb db, IMapper mapper) =>
{
    // 1. Fetch the existing entity graph
    var list = await db.Lists.Include(l => l.Items).FirstOrDefaultAsync(l => l.Id == id);
    if (list is null) return Results.NotFound();

    // 2. The Magic Happens Here:
    // AutoMappic updates the "Title" AND safely synchronizes the nested "Items" collection automatically!
    // -> Matches incoming Sub-Items by 'Id' to existing tracked EF Core entities.
    // -> Updates existing elements in-place (No dropped EF tracking, no thrown exceptions!).
    // -> Inserts new elements where Ids don't match.
    // -> Safely removes stale items that exist in the DB but not in the incoming DTO payload.
    list = mapper.Map(input, list);
    
    // 3. Save purely unmodified tracking graphs.
    await db.SaveChangesAsync(); 
    return Results.NoContent();
});
```

### Why this is a Massive Win
Because AutoMappic relies entirely on **Source Generation**, the mapping block that executes under the hood emits highly-optimized C# `System.Collections.Generic.Dictionary<int, TodoItem>` lookup buffers entirely at compile time. 

You get the ease-of-use of complex, external AutoMapper configurations, but with the massive performance profile and deep safety bounds of someone hand-writing the most optimized native C# possible. Best of all, it works perfectly with `.NET 9+ Native AOT`.
