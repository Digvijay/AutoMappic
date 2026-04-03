# Case Study: The Full-Features Demo

This walkthrough covers the ultimate "Full-Features" sample application that demonstrates every major mechanism of AutoMappic v0.6.0 in action.

## Scenario: Complex Entity Synchronization

Our goal is to map a domain-driven `User` model to a DTO, perform updates on a collection of `Project Tasks`, and then sync those changes back to the original database entity using **Smart-Sync**.

---

## The Implementation

### 1. The Mapping Profile
The profile defines our rules, including flattening, custom mapping for masked emails, and synchronization logic.

```csharp
public class FullFeaturesProfile : Profile
{
    public FullFeaturesProfile()
    {
        // Opt-in to the v0.5.0 synchronization engine
        EnableIdentityManagement = true;
        EnableEntitySync = true;

        CreateMap<User, UserDto>()
            .ForMember(d => d.AddressCity, opt => opt.MapFrom(s => s.Address != null ? s.Address.City : "Unknown"))
            .ForMember(d => d.Email, opt => opt.MapFrom(src => UserConverters.MaskEmail(src.Email)))
            .BeforeMap((src, dest) => {
                src.AuditLog = $"Last mapped at {DateTime.Now}";
            });

        // Enable Smart-Sync for Projects and Tasks via ReverseMap
        CreateMap<Project, ProjectDto>().ReverseMap();
        CreateMap<TaskItem, TaskItemDto>().ReverseMap();
    }
}
```

### 2. The Execution Logic
We set up a simple console app that walks through basic mapping, cyclic awareness, and identity-aware collection syncing.

```csharp
public static void Main()
{
    Console.WriteLine("--- AutoMappic v0.6.0: The Full Features Demo ---\n");

    // 1. Setup DI (Zero startup cost)
    var services = new ServiceCollection();
    services.AddAutoMappic(new FullFeaturesProfile());
    var provider = services.BuildServiceProvider();
    var mapper = provider.GetRequiredService<IMapper>();

    // 2. Demo: Smart-Sync
    var project = new Project {
        Id = 1, Name = "Build House",
        Tasks = new List<TaskItem> { 
            new TaskItem { Id = 1, Description = "Foundation", IsDone = true },
            new TaskItem { Id = 2, Description = "Walls", IsDone = false }
        }
    };

    var dto = mapper.Map<Project, ProjectDto>(project);
    dto.Tasks[0].Description = "Foundation (Reinforced)";
    dto.Tasks.Add(new TaskItemDto { Id = 3, Description = "Roof", IsDone = false });

    // Sync DTO back to Project in-place - preserving identities!
    mapper.Map(dto, project);
}
```

---

## Sample Output

When running the full sample, the following output is generated, showing **Smart-Sync** preserving original entity instances while applying DTO updates:

```text
--- AutoMappic v0.5.0: The Full Features Demo ---

[1] BASIC MAPPING + FLATTENING + CONVERTERS
   User: Alice (a***@example.com)
   Flattened Address: Paris
   Enum -> String: Active
   Lifecycle Audit: Last mapped at 3/30/2026 7:27:59 PM

[2] CIRCULAR GRAPH (IDENTITY MANAGEMENT)
   Main: Main Task (Updated)
   Circular awareness is handled automatically by the generator diagnostics.

[3] SMART-SYNC (COLLECTION SYNCHRONIZATION)
   [HOOK: AfterMap] Mapping Project 'Build House' with 2 tasks.
   Project: Build House with 2 tasks.
   After Sync (In-Place Update):
     - Task #1: Foundation (Reinforced) [Done: True]
     - Task #2: Walls [Done: False]
     - Task #3: Roof [Done: False]
   Collection preserved original list instance: True

--- Demo Complete! ---
```

---

## Tips & Lessons Learned

While building this sample, we encountered several common developer pitfalls that AutoMappic's diagnostic engine caught at compile-time:

### 1. Bidirectional Mapping Integrity (AM0001)
When using **`.ReverseMap()`**, AutoMappic enforces 100% coverage in both directions. If your destination model (`User`) has domain-only properties like `Address` and `AuditLog` that are not present in your DTO, the build will fail with **AM0001**.
*   **Resolution**: Always use `.ForMemberIgnore()` on the reverse profile to explicitly acknowledge domain-only fields.

### 2. Single Source of Truth (AM0009)
AutoMappic prevents defining the same mapping twice in a profile (e.g., mapping `User` $\to$ `UserDto` in two different locations). This ensures that interceptors are deterministic.
*   **Resolution**: Combine all configuration for a specific type-pair into a single `CreateMap` block.

### 3. Cyclic Reference Awareness (AM0006)
Even with **Identity Management** enabled, the source generator will warn you (**AM0006**) if it detects a potential infinite loop in your static object graph. 
*   **Pro-Tip**: While Identity Management handles this at runtime, these warnings are essential for performance hygiene—they remind you to keep DTO graphs shallow whenever possible.

### 4. Interceptor Overload Resolution
Interceptors replace standard `Map<TSource, TDestination>` calls. If you need to use advanced generated features (like passing a specific `MappingContext`), you can call the generated extension methods directly or rely on the mapper's automatic context creation for collections.

---

## The Technology Behind the Scenes

By migrating these features to **Roslyn Interceptors**, AutoMappic v0.6.0 achieves:
- **Zero Startup Lag**: No reflection-based profile scanning.
- **Extreme Performance**: 1.45x faster throughout than traditional JIT mappers.
- **Native AOT Ready**: 100% compatible with trimming and single-file publication.

You can find the full source code for this demo in the [AutoMappic GitHub Repository](https://github.com/Digvijay/AutoMappic/tree/main/samples/AutoMappic.Samples.FullFeatures).
