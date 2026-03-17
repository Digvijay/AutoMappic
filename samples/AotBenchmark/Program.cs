using System.Diagnostics;
using AutoMappic;

Console.WriteLine("🚀 AutoMappic Native AOT Benchmark");
Console.WriteLine("----------------------------------");

var mapper = new MapperConfiguration(cfg => cfg.AddProfile<BenchmarkProfile>())
    .CreateMapper();

var source = new User 
{ 
    Id = 1, 
    Name = "Principled Engineer", 
    Email = "builder@automappic.dev",
    Metadata = new UserMetadata { LastLogin = DateTime.UtcNow }
};

// Warm up
mapper.Map<UserDto>(source);

var sw = Stopwatch.StartNew();
for (int i = 0; i < 100_000; i++)
{
    var dto = mapper.Map<UserDto>(source);
}
sw.Stop();

Console.WriteLine($"✅ Mapped 100,000 objects in: {sw.ElapsedMilliseconds}ms");
Console.WriteLine($"⏱️ Average: {sw.Elapsed.TotalMicroseconds / 100_000:F4}μs per map");
Console.WriteLine("----------------------------------");
Console.WriteLine("Done.");

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public UserMetadata? Metadata { get; set; }
}

public class UserMetadata
{
    public DateTime LastLogin { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTime MetadataLastLogin { get; set; }
}

public class BenchmarkProfile : Profile
{
    public BenchmarkProfile()
    {
        CreateMap<User, UserDto>();
    }
}
