using AutoMappic;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Note: For native AOT or high performance ASP.NET Core, an InMemoryDb works fine 
// to demonstrate the EF Core AutoMappic interactions.
builder.Services.AddDbContext<TodoDb>(opt => opt.UseInMemoryDatabase("TodoList"));

// Register AutoMappic Profiles
builder.Services.AddAutoMappic();

var app = builder.Build();

// Seed some Initial Data
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TodoDb>();
    var initialList = new TodoList { Title = "Groceries" };
    initialList.Items.Add(new TodoItem { Description = "Milk", IsDone = false });
    initialList.Items.Add(new TodoItem { Description = "Bread", IsDone = true });
    db.Lists.Add(initialList);
    db.SaveChanges();
}

// === Routes === //

app.MapGet("/", () => "Welcome to AutoMappic EF Core Todo API Sample. Go to /todo-lists to see data.");

app.MapGet("/todo-lists", async (TodoDb db, IMapper mapper) =>
{
    var lists = await db.Lists.Include(l => l.Items).ToListAsync();
    // Using mapping extension methods, or map manually
    return lists.Select(l => mapper.Map<TodoListDto>(l));
});

app.MapGet("/todo-lists/projected", (TodoDb db) =>
{
    // AutoMappic v0.6.0 landmark: ProjectTo<T> translates the mapping 
    // directly to SQL, omitting unused columns from the database wire.
    // Extremely fast, zero allocation beyond the result objects.
    return db.Lists.ProjectTo<TodoListDto>().ToListAsync();
});

app.MapPost("/todo-lists", async (UpdateTodoListDto input, TodoDb db, IMapper mapper) =>
{
    var list = mapper.Map<TodoList>(input);
    db.Lists.Add(list);
    await db.SaveChangesAsync();
    return Results.Created($"/todo-lists/{list.Id}", mapper.Map<TodoListDto>(list));
});

app.MapPut("/todo-lists/{id}", async (int id, UpdateTodoListDto input, TodoDb db, IMapper mapper) =>
{
    var list = await db.Lists.Include(l => l.Items).FirstOrDefaultAsync(l => l.Id == id);
    if (list is null) return Results.NotFound();

    // The Magic Happens Here:
    // AutoMappic Smart-Sync updates the "Title" AND synchronizes the nested "Items" collection.
    // Existing elements with matching Ids are updated in-place (no EF Detach/Attach issues!).
    // New elements are inserted, missing elements are ignored/left behind (or removed based on sync mode).
    // This perfectly matches how Entity Framework expects graph operations to happen.
    list = mapper.Map(input, list);

    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

// === Models === //

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

public class TodoDb : DbContext
{
    public TodoDb(DbContextOptions<TodoDb> options) : base(options) { }
    public DbSet<TodoList> Lists => Set<TodoList>();
}

// === DTOs === //

// Use [AutoMap] on the DTO to tell AutoMappic to generate the mapping logic.
// This partial class is processed at compile-time—no reflection used.
[AutoMap(typeof(TodoList))]
public partial class TodoListDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<TodoItemDto> Items { get; set; } = new();
}

[AutoMap(typeof(TodoItem), ReverseMap = true)]
public partial class TodoItemDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsDone { get; set; }
}

public class UpdateTodoListDto
{
    public string Title { get; set; } = string.Empty;
    public List<TodoItemDto> Items { get; set; } = new();
}

// === AutoMappic Configuration === //

public class TodoProfile : Profile
{
    public TodoProfile()
    {
        // Global setting: Enable Smart-Sync so nested EF Core collections map cleanly.
        // This ensures the same entity instances are updated rather than replaced.
        EnableEntitySync = true;

        // Since we used [AutoMap] on TodoListDto and TodoItemDto, read-mappings are automatic.
        // We only use the Profile for complex mappings that need specialized overrides:

        CreateMap<UpdateTodoListDto, TodoList>()
            .ForMemberIgnore(d => d.Id); // ID is NOT on the incoming DTO

        CreateMap<TodoItemDto, TodoItem>(); // Mapper can also map from DTO back to Entity
    }
}
