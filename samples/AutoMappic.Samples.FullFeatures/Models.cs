using System;
using System.Collections.Generic;
using AutoMappic;

namespace AutoMappic.Samples.FullFeatures;

// --- Domain Models ---

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public Address? Address { get; set; }
    public UserStatus Status { get; set; }
    
    // Demonstrates lifecycle hook target
    public string AuditLog { get; set; } = "";
}

public class Address
{
    public string City { get; set; } = "";
    public string ZipCode { get; set; } = "";
}

public enum UserStatus
{
    Pending,
    Active,
    Suspended
}

// Demonstrates Circular References
public class WorkItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public WorkItem? Parent { get; set; }
}

// Demonstrates Smart-Sync (Collection Identity)
public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<TaskItem> Tasks { get; set; } = new();
}

public class TaskItem
{
    public int Id { get; set; }
    public string Description { get; set; } = "";
    public bool IsDone { get; set; }
}

// --- DTOs ---

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    
    // Demonstrates Flattening (Address.City -> AddressCity)
    public string AddressCity { get; set; } = "";
    
    // Demonstrates Enum conversion (UserStatus -> string)
    public string Status { get; set; } = "";
}

public class WorkItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public WorkItemDto? Parent { get; set; }
}

public class ProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<TaskItemDto> Tasks { get; set; } = new();
}

public class TaskItemDto
{
    public int Id { get; set; }
    public string Description { get; set; } = "";
    public bool IsDone { get; set; }
}

// Demonstrates custom static mapping logic
public static class UserConverters
{
    public static string MaskEmail(string email) => string.IsNullOrEmpty(email) ? "" : email[0] + "***@" + email.Split('@')[1];
}
