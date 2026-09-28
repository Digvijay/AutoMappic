using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using AutoMappic;

Console.WriteLine("AutoMappic Native AOT Benchmark");
Console.WriteLine("----------------------------------");

var mapper = new MapperConfiguration(cfg => cfg.AddProfile<BenchmarkProfile>())
    .CreateMapper();

var source = new User 
{ 
    Id = 1, 
    Name = "Principled Engineer", 
    Email = "builder@automappic.digvijay.dev",
    Metadata = new UserMetadata { LastLogin = DateTime.UtcNow }
};

Benchmark.Run(mapper, source);

/// <summary>Runs the mapping benchmark.</summary>
internal static class Benchmark
{
    /// <summary>Maps <paramref name="source" /> in a tight loop and reports the throughput.</summary>
    /// <remarks>
    ///   <para>
    ///     The <c>IL2026</c> suppression below is a workaround for an analyzer limitation, not a
    ///     waiver of a real requirement. AutoMappic rewrites these call sites with
    ///     <c>[InterceptsLocation]</c>, so the IL that ships calls a generated static mapping
    ///     method and never touches the reflective fallback that carries the annotation. The
    ///     trimming analyzer runs on the semantic model *before* the compiler applies
    ///     interceptors, so it resolves the call to <c>IMapper.Map</c> and cannot observe the
    ///     substitution. Every interceptor-based library hits this.
    ///   </para>
    ///   <para>
    ///     The claim is not taken on trust. The <c>aot-publish-and-run</c> job publishes this
    ///     project with <c>PublishAot=true</c> and then executes the produced native binary:
    ///     ILC analyses the post-interception IL, and a surviving reflective path would either
    ///     be reported by ILC or fail when the binary runs. Interception is also verifiable
    ///     directly - build with <c>-p:EmitCompilerGeneratedFiles=true</c> and the call sites
    ///     below appear as <c>InterceptsLocation</c> entries in
    ///     <c>AutoMappic.Interceptors.g.cs</c>.
    ///   </para>
    /// </remarks>
    [UnconditionalSuppressMessage(
        "TrimAnalysis", "IL2026:RequiresUnreferencedCode",
        Justification = "These call sites are rewritten by the AutoMappic source generator via "
            + "[InterceptsLocation] and resolve to generated static mapping methods, so the "
            + "annotated reflective fallback is never reached. The analyzer runs before "
            + "interceptors are applied and cannot see this; ILC verifies the shipped IL and CI "
            + "executes the resulting native binary.")]
    public static void Run(IMapper mapper, User source)
    {
        // Warm up
        mapper.Map<User, UserDto>(source);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < 100_000; i++)
        {
            var dto = mapper.Map<User, UserDto>(source);
        }
        sw.Stop();

        Console.WriteLine($"Mapped 100,000 objects in: {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"Average: {sw.Elapsed.TotalMicroseconds / 100_000:F4}μs per map");
        Console.WriteLine("----------------------------------");
        Console.WriteLine("Done.");
    }
}

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
