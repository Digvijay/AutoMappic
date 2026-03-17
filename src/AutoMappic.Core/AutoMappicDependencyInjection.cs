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
    {
        services.AddSingleton<IMapper>(sp =>
        {
            var config = new MapperConfiguration(cfg =>
            {
                foreach (var profile in profiles)
                {
                    cfg.AddProfile(profile);
                }
            });
            return config.CreateMapper();
        });

        return services;
    }
}
