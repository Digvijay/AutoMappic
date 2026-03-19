using Microsoft.Extensions.DependencyInjection;

namespace AutoMappic;

/// <summary>
///   Extension methods for setting up AutoMappic in an <see cref="IServiceCollection" />.
/// </summary>
public static class AutoMappicDependencyInjection
{
    /// <summary>
    ///   Legacy support for manually registering profiles. 
    ///   For zero-reflection, use the parameterless AddAutoMappic() provided by the source generator.
    /// </summary>
    public static IServiceCollection AddAutoMappic(this IServiceCollection services, params Profile[] profiles)
        => AddAutoMappic(services, ServiceLifetime.Singleton, profiles);

    /// <summary>
    ///   Manual registration with specified lifetime.
    /// </summary>
    public static IServiceCollection AddAutoMappic(this IServiceCollection services, ServiceLifetime lifetime, params Profile[] profiles)
    {
        var descriptor = ServiceDescriptor.Describe(typeof(IMapper), sp =>
        {
            var config = new MapperConfiguration(cfg =>
            {
                foreach (var profile in profiles) cfg.AddProfile(profile);
            });
            return config.CreateMapper();
        }, lifetime);

        services.Add(descriptor);
        return services;
    }

    /// <summary>
    ///   Registers a named/keyed mapper instance accessible via Keyed Service resolution.
    /// </summary>
    public static IServiceCollection AddAutoMappic(this IServiceCollection services, string key, ServiceLifetime lifetime, params Profile[] profiles)
    {
        var descriptor = ServiceDescriptor.DescribeKeyed(typeof(IMapper), key, (sp, k) =>
        {
            var config = new MapperConfiguration(cfg =>
            {
                foreach (var profile in profiles) cfg.AddProfile(profile);
            });
            return config.CreateMapper();
        }, lifetime);

        services.Add(descriptor);
        return services;
    }
}
