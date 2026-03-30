using AutoMappic;
using System;

namespace AutoMappic.Samples.FullFeatures;

public class FullFeaturesProfile : Profile
{
    public FullFeaturesProfile()
    {
        // 1. Identity Management & EntitySync (Globally defined in .csproj but can be overridden)
        EnableIdentityManagement = true;
        EnableEntitySync = true;

        // 2. Direct Mapping + Flattening + Reverse Map
        CreateMap<User, UserDto>()
            .ForMember(d => d.AddressCity, opt => opt.MapFrom(s => s.Address != null ? s.Address.City : "Unknown"))
            .ForMember(d => d.Email, opt => opt.MapFrom(src => UserConverters.MaskEmail(src.Email)))
            .BeforeMap((src, dest) => {
                src.AuditLog = $"Last mapped at {DateTime.Now}";
            });

        // 3. Nested Graph Mapping
        CreateMap<WorkItem, WorkItemDto>();

        // 4. Smart-Sync Collection mapping (Tasks)
        CreateMap<Project, ProjectDto>()
            .AfterMap((src, dest) => {
                Console.WriteLine($"   [HOOK: AfterMap] Mapping Project '{src.Name}' with {src.Tasks.Count} tasks.");
            })
            .ReverseMap();

        CreateMap<TaskItem, TaskItemDto>().ReverseMap();
    }
}
