using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace AutoMappic;

/// <summary>
///   Extension methods for setting up AutoMappic in an <see cref="IServiceCollection" />.
/// </summary>
public static class AutoMappicDependencyInjection
{
    /// <summary>
    ///   Adds AutoMappic to the service collection, scanning the specified assemblies for profiles.
    ///   This provides the same "zero-touch" DI setup as AutoMapper, but with zero runtime reflection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">Assemblies to scan for <see cref="Profile" /> classes.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAutoMappic(this IServiceCollection services, params Assembly[] assemblies)
    {
        var config = new MapperConfiguration(cfg =>
        {
            foreach (var assembly in assemblies)
            {
                var profileTypes = assembly.GetTypes()
                    .Where(t => typeof(Profile).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass);

                foreach (var profileType in profileTypes)
                {
                    if (Activator.CreateInstance(profileType) is Profile profile)
                    {
                        cfg.AddProfile(profile);
                    }
                }
            }
        });

        var mapper = config.CreateMapper();
        services.AddSingleton<IMapper>(mapper);

        return services;
    }
}
