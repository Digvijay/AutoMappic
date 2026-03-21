namespace AutoMappic;

/// <summary>
///   Registers one or more <see cref="Profile" /> instances and produces an
///   <see cref="IMapper" /> that is handed to the application's DI container.
/// </summary>
/// <remarks>
///   In a project that references the <c>AutoMappic.Generator</c> package, the
///   <see cref="IMapper" /> returned here is still the <see cref="Mapper" /> class -
///   but every call site where <c>_mapper.Map&lt;T&gt;(source)</c> is used will
///   have been silently replaced by the compiler with the generated static method
///   via an <c>[InterceptsLocation]</c> attribute.  The runtime instance is only
///   invoked as a fallback (e.g., in dynamic or reflection scenarios).
/// </remarks>
public sealed class MapperConfiguration : IConfigurationProvider
{
    private readonly List<Profile> _profiles = [];

    /// <summary>
    ///   Initialises a new <see cref="MapperConfiguration" /> using a configuration delegate.
    /// </summary>
    /// <param name="configure">
    ///   A delegate that calls <see cref="IMapperConfigurationExpression.AddProfile{TProfile}" />
    ///   (and similar) to register profiles.
    /// </param>
    public MapperConfiguration(Action<IMapperConfigurationExpression> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        var expression = new MapperConfigurationExpression();
        configure(expression);
        _profiles.AddRange(expression.Profiles);
    }

    /// <summary>Creates an <see cref="IMapper" /> backed by the registered profiles.</summary>
    /// <returns>A fully configured <see cref="IMapper" /> instance.</returns>
    public IMapper CreateMapper() => new Mapper(_profiles, this);
}

/// <summary>Fluent entry point for <see cref="MapperConfiguration" />.</summary>
public interface IMapperConfigurationExpression
{
    /// <summary>Registers a profile by type, instantiating it with the default constructor.</summary>
    /// <typeparam name="TProfile">A concrete <see cref="Profile" /> subclass.</typeparam>
    void AddProfile<TProfile>() where TProfile : Profile, new();

    /// <summary>Registers an already-constructed profile instance.</summary>
    /// <param name="profile">The profile to register.</param>
    void AddProfile(Profile profile);

    /// <summary>Registers profiles from assemblies.</summary>
    void AddMaps(params System.Reflection.Assembly[] assemblies);

    /// <summary>Creates a new mapping configuration directly on the expression.</summary>
    IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>();
}

/// <summary>Empty marker interface for AutoMapper compatibility.</summary>
public interface IConfigurationProvider { }

internal sealed class MapperConfigurationExpression : IMapperConfigurationExpression
{
    internal readonly List<Profile> Profiles = [];
    private readonly AdHocProfile _adHoc = new();

    public MapperConfigurationExpression() => Profiles.Add(_adHoc);

    public void AddProfile<TProfile>() where TProfile : Profile, new()
        => Profiles.Add(new TProfile());

    public void AddProfile(Profile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        Profiles.Add(profile);
    }

    public void AddMaps(params System.Reflection.Assembly[] assemblies)
    {
        // For runtime fallback, we might need real scanning, but for source-gen test 
        // we just need it to compile. We can add minimal scanning here.
        foreach (var assembly in assemblies)
        {
            var profileTypes = assembly.GetTypes().Where(t => typeof(Profile).IsAssignableFrom(t) && !t.IsAbstract && t.IsClass);
            foreach (var type in profileTypes)
            {
                try { Profiles.Add((Profile)Activator.CreateInstance(type)!); } catch { /* skip */ }
            }
        }
    }

    public IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
        => _adHoc.CreateMap<TSource, TDestination>();

    private sealed class AdHocProfile : Profile { }
}
