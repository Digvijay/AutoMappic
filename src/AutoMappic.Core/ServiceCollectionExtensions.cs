using System;
using Microsoft.Extensions.DependencyInjection;

namespace AutoMappic;

/// <summary>
///   Extensions for registering AutoMappic with IServiceCollection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///   Provides an AutoMapper-compatible registration hook.
    /// </summary>
    public static IServiceCollection AddAutoMapper(this IServiceCollection services, Action<IMapperConfigurationExpression> action)
    {
        var config = new MapperConfiguration(action);
        services.AddSingleton<IMapper>(config.CreateMapper());
        services.AddSingleton<IConfigurationProvider>(config);
        return services;
    }
}
