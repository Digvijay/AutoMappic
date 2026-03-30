# AutoMappic EF Core Todo App Sample

A modern, highly optimized ASP.NET Core Todo API demonstrating the differences between **AutoMappic** (with Native Smart-Sync) vs standard **AutoMapper**.

## The Problem with AutoMapper and EF Core
When updating a hierarchical aggregate (e.g., a `TodoList` entity that contains multiple `TodoItem` sub-entities) from a DTO, AutoMapper struggles to synchronize the collections natively:

**With AutoMapper:**
```csharp
// Standard AutoMapper creates *brand new* list instances, dropping EF Core tracking.
// This often leads to EF Exceptions: "The instance of entity type 'TodoItem' cannot be tracked because another instance with the same key value is already being tracked."
list = _mapper.Map(inputDto, list);
```
To fix this in AutoMapper, developers have to install 3rd party extensions (`AutoMapper.Collection`), configure `EquivalencyExpression` profiles manually, or manually write looping logic to stitch ID's together to avoid destroying the Object Reference graph.

## The AutoMappic Solution
AutoMappic solves this directly in its compiled, zero-reflection source generation step.

In our `TodoProfile.cs`:
```csharp
public class TodoProfile : Profile
{
    public TodoProfile()
    {
        // This single line enables Native Smart-Sync
        EnableEntitySync = true;
        
        CreateMap<UpdateTodoListDto, TodoList>().ForMemberIgnore(d => d.Id);
        CreateMap<TodoItemDto, TodoItem>();
    }
}
```

Now, during the API `PUT` operation:
```csharp
app.MapPut("/todo-lists/{id}", async (int id, UpdateTodoListDto input, TodoDb db, IMapper mapper) =>
{
    var list = await db.Lists.Include(l => l.Items).FirstOrDefaultAsync(l => l.Id == id);
    
    // AutoMappic generates an intelligent, high-performance synchronization block!
    // -> Matches incoming Sub-Items by 'Id' to existing tracked Entities
    // -> Updates existing elements in-place (No dropped EF tracking!)
    // -> Inserts new elements where Ids don't match
    // -> Automatically cleans up missing elements
    list = mapper.Map(input, list);
    
    await db.SaveChangesAsync(); // Saves perfectly cleanly with ZERO disconnected graph errors.
});
```

Because AutoMappic generates native C# `Dictionary` lookups at compile-time instead of relying on runtime Reflection, this is entirely Native AOT friendly and processes in single-digit microseconds.

## Try it out
Run the API directly using:
```bash
dotnet run
```
And check out `Program.cs` for the complete implementation.
