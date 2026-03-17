using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

public class RuntimeCoverageTests
{
    [Fact]
    public async Task MapAsync_WhenExceptionOccurs_ReturnsFaultedTask()
    {
        // Null source will trigger ArgumentNullException
        var mapper = new MapperConfiguration(p => {}).CreateMapper();
        
        var task = mapper.MapAsync<UserDto>(null!);
        
        Assert.True(task.IsFaulted, "Task should be faulted");
        // Prova's Assert.Throws for Tasks might not be exactly what I expect.
        // Let's just try-catch.
        try
        {
            await task;
            Assert.True(false, "Should have thrown");
        }
        catch (ArgumentNullException)
        {
            // Expected
        }
    }

    [Fact]
    public void Mapper_PrimitiveConversionFailure_FallsThrough()
    {
        var profile = new TestProfile();
        profile.Register<SourceWithBadType, DestWithGoodType>();
        
        var mapper = new MapperConfiguration(p => p.AddProfile(profile)).CreateMapper();
        
        var source = new SourceWithBadType { Value = new object() };
        
        // This should fall through to object mapping which might fail or skip
        var result = mapper.Map<DestWithGoodType>(source);
        Assert.NotNull(result);
    }

    [Fact]
    public void Mapper_DictionaryMapping_WithConversions()
    {
        var profile = new TestProfile();
        profile.Register<DictSource, DictDest>();
        
        var mapper = new MapperConfiguration(p => p.AddProfile(profile)).CreateMapper();
        
        var source = new DictSource 
        { 
            Items = new Dictionary<int, int> { { 1, 100 } } 
        };
        
        var dest = mapper.Map<DictDest>(source);
        
        Assert.NotNull(dest.Items);
        Assert.True(dest.Items.ContainsKey("1"));
        Assert.Equal("100", dest.Items["1"]);
    }

    [Fact]
    public void Mapper_Normalize_RespectsSnakeCase()
    {
        var profile = new TestProfile();
        profile.Register<SnakeSource, PascalDest>();
        
        var mapper = new MapperConfiguration(p => p.AddProfile(profile)).CreateMapper();
        
        var source = new SnakeSource { first_name = "Alice" };
        var dest = mapper.Map<PascalDest>(source);
        
        Assert.Equal("Alice", dest.FirstName);
    }

    [Fact]
    public void MemberConfiguration_MapFromResolver_WorksAtRuntime()
    {
        var profile = new TestProfile();
        profile.Register<User, UserDto>(exp => exp.ForMember(d => d.Name, opt => opt.MapFrom<NameResolver>()));
        
        var mapper = new MapperConfiguration(p => p.AddProfile(profile)).CreateMapper();
        
        var source = new User { Name = "Alice" };
        var dest = mapper.Map<UserDto>(source);
        
        Assert.Equal("Resolved: Alice", dest.Name);
    }

    [Fact]
    public void GetMemberName_InvalidExpression_Throws()
    {
        var profile = new TestProfile();
        Assert.Throws<ArgumentException>(() => {
            profile.Register<User, UserDto>(opt => opt.ForMember(d => d.ToString(), o => o.Ignore()));
        });
    }

    private class User { public string Name { get; set; } = ""; }
    private class UserDto { public string Name { get; set; } = ""; }
    
    private class NameResolver : IValueResolver<User, string>
    {
        public string Resolve(User source) => "Resolved: " + source.Name;
    }

    private class SourceWithBadType { public object Value { get; set; } = new(); }
    private class DestWithGoodType { public int Value { get; set; } }

    private class DictSource { public Dictionary<int, int> Items { get; set; } = new(); }
    private class DictDest { public Dictionary<string, string> Items { get; set; } = new(); }

    private class SnakeSource { public string first_name { get; set; } = ""; }
    private class PascalDest { public string FirstName { get; set; } = ""; }

    private class TestProfile : Profile 
    {
        public void Register<S, D>(Action<IMappingExpression<S, D>>? opt = null)
        {
            var exp = CreateMap<S, D>();
            opt?.Invoke(exp);
        }
    }
}
