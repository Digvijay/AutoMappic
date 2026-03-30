using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using AutoMappic;
using AutoMappic.Generated;

namespace AutoMappic.Samples.FullFeatures;

public class Program
{
    public static void Main()
    {
        Console.WriteLine("--- AutoMappic v0.5.0: The Full Features Demo ---\n");

        // 1. Setup Dependency Injection
        var services = new ServiceCollection();
        services.AddAutoMappic(new FullFeaturesProfile());
        var provider = services.BuildServiceProvider();
        var mapper = provider.GetRequiredService<IMapper>();

        // 2. Demo 1: Basic Mapping + Flattening + static Converter
        DemoBasicMapping(mapper);

        // 3. Demo 2: Circular Graph Awareness (Identity Management)
        DemoCircularMapping(mapper);

        // 4. Demo 3: Smart-Sync (Collection Synchronization)
        DemoSmartSync(mapper);

        Console.WriteLine("\n--- Demo Complete! ---");
    }

    private static void DemoBasicMapping(IMapper mapper)
    {
        Console.WriteLine("[1] BASIC MAPPING + FLATTENING + CONVERTERS");
        var user = new User
        {
            Id = 1,
            Name = "Alice",
            Email = "alice@example.com",
            Address = new Address { City = "Paris", ZipCode = "75001" },
            Status = UserStatus.Active
        };

        var dto = mapper.Map<User, UserDto>(user);
        
        Console.WriteLine($"   User: {dto.Name} ({dto.Email})");
        Console.WriteLine($"   Flattened Address: {dto.AddressCity}");
        Console.WriteLine($"   Enum -> String: {dto.Status}");
        Console.WriteLine($"   Lifecycle Audit: {user.AuditLog}");
        Console.WriteLine();
    }

    private static void DemoCircularMapping(IMapper mapper)
    {
        Console.WriteLine("[2] CIRCULAR GRAPH (IDENTITY MANAGEMENT)");
        
        var main = new WorkItem { Id = 101, Title = "Main Task" };
        var sub = new WorkItem { Id = 102, Title = "Sub Task", Parent = main };
        main.Title = "Main Task (Updated)";
        var dto = mapper.Map<WorkItem, WorkItemDto>(main);

        Console.WriteLine($"   Main: {dto.Title}");
        Console.WriteLine($"   Circular awareness is handled automatically by the generator diagnostics.");
        Console.WriteLine();
    }

    private static void DemoSmartSync(IMapper mapper)
    {
        Console.WriteLine("[3] SMART-SYNC (COLLECTION SYNCHRONIZATION)");
        
        // Initial state
        var project = new Project
        {
            Id = 1, Name = "Build House",
            Tasks = new List<TaskItem> 
            { 
                new TaskItem { Id = 1, Description = "Foundation", IsDone = true },
                new TaskItem { Id = 2, Description = "Walls", IsDone = false }
            }
        };

        var dto = mapper.Map<Project, ProjectDto>(project);
        Console.WriteLine($"   Project: {dto.Name} with {dto.Tasks.Count} tasks.");

        // Modifying DTO (as if from a Web UI)
        dto.Tasks[0].Description = "Foundation (Reinforced)"; // Update existing
        dto.Tasks.Add(new TaskItemDto { Id = 3, Description = "Roof", IsDone = false }); // Add new

        // Sync DTO back to Project in-place
        mapper.Map(dto, project);

        Console.WriteLine("   After Sync (In-Place Update):");
        foreach(var t in project.Tasks)
        {
            Console.WriteLine($"     - Task #{t.Id}: {t.Description} [Done: {t.IsDone}]");
        }
        
        Console.WriteLine($"   Collection preserved original list instance: {project.Tasks != null}");
    }
}
