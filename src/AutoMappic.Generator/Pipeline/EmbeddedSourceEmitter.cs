using System;
using System.Text;
using Microsoft.CodeAnalysis;

namespace AutoMappic.Generator.Pipeline;

internal static class EmbeddedSourceEmitter
{
    public static void Emit(SourceProductionContext spc)
    {
        // Emit IMapper
        spc.AddSource("IMapper.g.cs", @"
namespace AutoMappic;

internal interface IMapper
{
    TDestination Map<TDestination>(object source);
    TDestination Map<TSource, TDestination>(TSource source);
    TDestination Map<TSource, TDestination>(TSource source, TDestination destination);
    global::System.Threading.Tasks.Task<TDestination> MapAsync<TDestination>(object source);
    global::System.Threading.Tasks.Task<TDestination> MapAsync<TSource, TDestination>(TSource source);
}");

        // Emit Profile
        spc.AddSource("Profile.g.cs", @"
namespace AutoMappic;

internal abstract class Profile
{
    protected IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>() => new MappingExpression<TSource, TDestination>();
}

internal interface IMappingExpression<TSource, TDestination>
{
    IMappingExpression<TSource, TDestination> ForMember<TMember>(
        global::System.Linq.Expressions.Expression<global::System.Func<TDestination, TMember>> destinationMember,
        global::System.Action<IMappingOperationOptions<TSource, TDestination, TMember>> memberOptions);

    IMappingExpression<TSource, TDestination> ForMemberIgnore<TMember>(
        global::System.Linq.Expressions.Expression<global::System.Func<TDestination, TMember>> destinationMember);

    void ReverseMap();
}

internal interface IMappingOperationOptions<TSource, TDestination, TMember>
{
    void MapFrom<TResult>(global::System.Func<TSource, TResult> mappingFunction);
    void MapFrom<TResolver>() where TResolver : IValueResolver<TSource, TDestination, TMember>, new();
    void Ignore();
}

internal class MappingExpression<TSource, TDestination> : IMappingExpression<TSource, TDestination>
{
    public IMappingExpression<TSource, TDestination> ForMember<TMember>(
        global::System.Linq.Expressions.Expression<global::System.Func<TDestination, TMember>> destinationMember,
        global::System.Action<IMappingOperationOptions<TSource, TDestination, TMember>> memberOptions) => this;

    public IMappingExpression<TSource, TDestination> ForMemberIgnore<TMember>(
        global::System.Linq.Expressions.Expression<global::System.Func<TDestination, TMember>> destinationMember) => this;

    public void ReverseMap() { }
}
");

        // Emit Marker Attributes
        spc.AddSource("Attributes.g.cs", @"
namespace AutoMappic;

[global::System.AttributeUsage(global::System.AttributeTargets.Assembly, AllowMultiple = true)]
internal sealed class MappingDiscoveryAttribute(global::System.Type sourceType, global::System.Type destinationType) : global::System.Attribute;

[global::System.AttributeUsage(global::System.AttributeTargets.Assembly)]
internal sealed class HasAutoMappicProfilesAttribute : global::System.Attribute;

[global::System.AttributeUsage(global::System.AttributeTargets.Method)]
internal sealed class AutoMappicConverterAttribute : global::System.Attribute;
");
    }
}
