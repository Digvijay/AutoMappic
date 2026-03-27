using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

#pragma warning disable AM0003 // CreateMap called outside constructor (intentional for runtime-only tests)
public class RuntimeCoverageTests
{
    /// <summary> Verify that MapAsync correctly returns null (not faulted) when source is null </summary>
    [Fact]
    public async Task MapAsync_WhenSourceIsNull_ReturnsNull()
    {
        var mapper = new MapperConfiguration(p => { }).CreateMapper();
        var result = await mapper.MapAsync<UserDto>(null!);
        Assert.Null(result);
    }

    /// <summary> Ensure that the generic MapAsync overload returns null when source is null </summary>
    [Fact]
    public async Task MapAsyncGeneric_WhenSourceIsNull_ReturnsNull()
    {
        var mapper = new MapperConfiguration(p => { }).CreateMapper();
        var result = await mapper.MapAsync<User, UserDto>(null!);
        Assert.Null(result);
    }

    /// <summary> Test dictionary value transformation where numeric source values are converted to destination strings </summary>
    [Fact]
    public void Mapper_DictionaryValueMapping_ToString()
    {
        var profile = new TestProfile();
        profile.Register<DictSourceIntVal, DictDestStringVal>();

        var mapper = new MapperConfiguration(p => p.AddProfile(profile)).CreateMapper();

        var source = new DictSourceIntVal { Items = new Dictionary<string, int> { { "a", 123 } } };
        var dest = mapper.Map<DictDestStringVal>(source);

        Assert.Equal("123", dest.Items["a"]);
    }

    /// <summary> Validate that the runtime mapper can handle dictionary mapping with nested MapCore conversions for keys and values </summary>
    [Fact]
    public void Mapper_DictionaryMapping_WithNestedMapCore()
    {
        var profile = new TestProfile();
        // int -> long needs MapCore if not string
        profile.Register<long, int>();
        profile.Register<DictSourceLong, DictDestInt>();

        var mapper = new MapperConfiguration(p => p.AddProfile(profile)).CreateMapper();

        var source = new DictSourceLong { Items = new Dictionary<long, long> { { 1L, 100L } } };
        var dest = mapper.Map<DictDestInt>(source);

        Assert.NotNull(dest.Items);
        Assert.True(dest.Items.ContainsKey(1));
        Assert.Equal(100, dest.Items[1]);
    }

    /// <summary> Verify dictionary value transformation when nested MapCore logic is required for type conversion </summary>
    [Fact]
    public void Mapper_DictionaryValueMapping_WithNestedMapCore()
    {
        var profile = new TestProfile();
        profile.Register<int, long>();
        profile.Register<DictSourceIntVal, DictDestLongVal>();

        var mapper = new MapperConfiguration(p => p.AddProfile(profile)).CreateMapper();

        var source = new DictSourceIntVal { Items = new Dictionary<string, int> { { "a", 1 } } };
        var dest = mapper.Map<DictDestLongVal>(source);

        Assert.Equal(1L, dest.Items["a"]);
    }

    /// <summary> Verify that mapping throws a descriptive AutoMappicException when a nested property type has no registered mapping. </summary>
    [Fact]
    public void Mapper_PropertyMapping_WithSkippedMapping()
    {
        var profile = new TestProfile();
        profile.Register<SkippedMapSource, SkippedMapDest>();
        // No mapping for UnregisteredSource -> UnregisteredDest

        var mapper = new MapperConfiguration(p => p.AddProfile(profile)).CreateMapper();

        var source = new SkippedMapSource { Item = new UnregisteredSource() };

        // AutoMappic now throws a clear exception rather than silently skipping unmapped nested types.
        // Users must either register the mapping or use ForMemberIgnore to suppress.
        bool threw = false;
        try { mapper.Map<SkippedMapDest>(source); }
        catch (AutoMappicException ex) when (ex.Message.Contains("UnregisteredSource"))
        { threw = true; }
        Assert.True(threw);
    }

    /// <summary> Validate that ReverseMap correctly allows mapping from destination back to source at runtime </summary>
    [Fact]
    public void Mapper_ReverseMap_WorksAtRuntime()
    {
        var profile = new TestProfile();
        // Register S -> D with reverse map D -> S
        profile.Register<User, UserDto>().ReverseMap();

        var mapper = new MapperConfiguration(p => p.AddProfile(profile)).CreateMapper();

        var source = new UserDto { Name = "Alice" };
        var dest = mapper.Map<User>(source);

        Assert.Equal("Alice", dest.Name);
    }

    /// <summary> Confirm that ForMember(..., opt.Ignore()) is correctly respected by the runtime fallback mapper </summary>
    [Fact]
    public void Mapper_ForMemberIgnore_WorksAtRuntime()
    {
        var profile = new TestProfile();
        profile.Register<User, UserDto>(opt => opt.ForMemberIgnore(d => d.Name));

        var mapper = new MapperConfiguration(p => p.AddProfile(profile)).CreateMapper();

        var source = new User { Name = "Alice" };
        var dest = mapper.Map<UserDto>(source);

        Assert.Equal("", dest.Name); // Should be ignored
    }

    /// <summary> Verify that AddAutoMappic correctly registers the IMapper service in the .NET DI container </summary>
    [Fact]
    public void DependencyInjection_AddAutoMappic_WorksAtRuntime()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddAutoMappic(new TestProfile());
        var sp = services.BuildServiceProvider();
        var mapper = sp.GetService<IMapper>();
        Assert.NotNull(mapper);
    }

    /// <summary> Confirm that the 'Ignore()' option within a 'ForMember' configuration is correctly enforced at runtime </summary>
    [Fact]
    public void Mapper_ForMemberWithIgnore_WorksAtRuntime()
    {
        var profile = new TestProfile();
        profile.Register<User, UserDto>(opt => opt.ForMember(d => d.Name, o => o.Ignore()));

        var mapper = new MapperConfiguration(p => p.AddProfile(profile)).CreateMapper();
        var dest = mapper.Map<UserDto>(new User { Name = "Alice" });
        Assert.Equal("", dest.Name);
    }

    /// <summary> Validate that explicit member mappings are correctly recorded in the mapping expression metadata </summary>
    [Fact]
    public void IMappingExpression_ExplicitMaps_ReturnsDictionary()
    {
        var profile = new TestProfile();
        IMappingExpression exp = profile.Register<User, UserDto>(opt => opt.ForMember(d => d.Name, o => o.MapFrom(s => s.Name)));
        Assert.True(exp.ExplicitMaps.ContainsKey("Name"));
    }

    /// <summary> Ensure that custom ValueResolvers can be correctly executed by the runtime mapper </summary>
    [Fact]
    public void MappingExpression_MapFromResolver_SetsExpression()
    {
        var profile = new TestProfile();
        IMappingExpression<User, UserDto> exp = profile.Register<User, UserDto>(opt =>
            opt.ForMember(d => d.Name, o => o.MapFrom<NameResolver>()));

        var mapper = new MapperConfiguration(p => p.AddProfile(profile)).CreateMapper();
        var result = mapper.Map<UserDto>(new User { Name = "Test" });
        Assert.Equal("Resolved: Test", result.Name);
    }

    /// <summary> Confirm that calling ProjectTo via reflection (bypassing interceptors) correctly throws the shim exception </summary>
    [Fact]
    public void QueryableExtensions_ProjectTo_ThrowsAtRuntime()
    {
        var queryable = new List<User>().AsQueryable();
        var method = typeof(QueryableExtensions).GetMethods()
            .First(m => m.Name == "ProjectTo" && m.GetGenericArguments().Length == 2)
            .MakeGenericMethod(typeof(User), typeof(UserDto));
        // Calling via reflection avoids interceptor
        var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() => method.Invoke(null, new object[] { queryable }));
        Assert.True(ex.InnerException is AutoMappicException);
    }

    /// <summary> Confirm that calling IDataReader.Map via reflection correctly throws the shim exception </summary>
    [Fact]
    public void DataReaderExtensions_Map_ThrowsAtRuntime()
    {
        var reader = new MockDataReader();
        var method = typeof(DataReaderExtensions).GetMethod("Map")!.MakeGenericMethod(typeof(UserDto));
        // Calling via reflection avoids interceptor
        var ex = Assert.Throws<System.Reflection.TargetInvocationException>(() => method.Invoke(null, new object[] { reader }));
        Assert.True(ex.InnerException is AutoMappicException);
    }

    /// <summary> Ensure that providing an invalid expression (non-member access) to 'ForMember' results in a descriptive ArgumentException </summary>
    [Fact]
    public void GetMemberName_InvalidExpression_Throws()
    {
        var profile = new TestProfile();
        Assert.Throws<ArgumentException>(() =>
        {
            profile.Register<User, UserDto>(opt => opt.ForMember(d => d.ToString(), o => o.Ignore()));
        });
    }

    private class MockDataReader : System.Data.IDataReader
    {
        private bool _read = false;
        public object this[int i] => i == 0 ? "Mocked" : null!;
        public object this[string name] => name == "Name" ? "Mocked" : null!;
        public int Depth => 0;
        public bool IsClosed => false;
        public int RecordsAffected => 0;
        public int FieldCount => 1;
        public void Close() { }
        public void Dispose() { }
        public string GetName(int i) => i == 0 ? "Name" : "";
        public string GetDataTypeName(int i) => "string";
        public Type GetFieldType(int i) => typeof(string);
        public object GetValue(int i) => this[i];
        public int GetValues(object[] values) { values[0] = "Mocked"; return 1; }
        public int GetOrdinal(string name) => name == "Name" ? 0 : -1;
        public bool GetBoolean(int i) => false;
        public byte GetByte(int i) => 0;
        public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => 0;
        public char GetChar(int i) => ' ';
        public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => 0;
        public Guid GetGuid(int i) => Guid.Empty;
        public short GetInt16(int i) => 0;
        public int GetInt32(int i) => 0;
        public long GetInt64(int i) => 0;
        public float GetFloat(int i) => 0;
        public double GetDouble(int i) => 0;
        public string GetString(int i) => "Mocked";
        public decimal GetDecimal(int i) => 0;
        public DateTime GetDateTime(int i) => DateTime.MinValue;
        public System.Data.IDataReader GetData(int i) => null!;
        public bool IsDBNull(int i) => false;
        public bool NextResult() => false;
        public bool Read() { if (!_read) { _read = true; return true; } return false; }
        public System.Data.DataTable GetSchemaTable() => null!;
    }

    /// <summary> Verify that nested type conversions within dictionary mappings are correctly resolved and executed </summary>
    [Fact]
    public void Mapper_DictionaryMapping_WithNestedConversions()
    {
        var profile = new TestProfile();
        profile.Register<NestedSource, NestedDest>();
        profile.Register<DictSourceNested, DictDestNested>();

        var mapper = new MapperConfiguration(p => p.AddProfile(profile)).CreateMapper();

        var source = new DictSourceNested
        {
            Items = new Dictionary<int, NestedSource> { { 1, new NestedSource { Id = 100 } } }
        };

        var dest = mapper.Map<DictDestNested>(source);

        Assert.NotNull(dest.Items);
        // Key 1 -> "1", Value NestedSource(100) -> NestedDest(100)
        Assert.True(dest.Items.ContainsKey("1"));
        Assert.Equal(100, dest.Items["1"].Id);
    }

    /// <summary> Confirm that collection mapping correctly handles and preserves null items within the source collection </summary>
    [Fact]
    public void Mapper_CollectionMapping_WithNullItems()
    {
        var profile = new TestProfile();
        profile.Register<NestedSource, NestedDest>();
        profile.Register<ListSource, ListDest>();

        var mapper = new MapperConfiguration(p => p.AddProfile(profile)).CreateMapper();

        var source = new ListSource { Items = new List<NestedSource?> { new NestedSource { Id = 1 }, null, new NestedSource { Id = 3 } } };
        var dest = mapper.Map<ListDest>(source);

        Assert.Equal(3, dest.Items.Count);
        Assert.Equal(1, dest.Items[0].Id);
        Assert.Null(dest.Items[1]);
        Assert.Equal(3, dest.Items[2].Id);
    }

    /// <summary> Verify that collection mapping throws a descriptive AutoMappicException when items have no registered mapping. </summary>
    [Fact]
    public void Mapper_CollectionMapping_WithSkippedItems()
    {
        var profile = new TestProfile();
        profile.Register<ListSourceUnreg, ListDestUnreg>();

        var mapper = new MapperConfiguration(p => p.AddProfile(profile)).CreateMapper();

        var source = new ListSourceUnreg { Items = new List<object> { new UnregisteredSource() } };

        // AutoMappic now throws a clear exception rather than silently skipping unmapped items.
        // Users must register a mapping or exclude the collection.
        bool threw = false;
        try { mapper.Map<ListDestUnreg>(source); }
        catch (AutoMappicException ex) when (ex.Message.Contains("UnregisteredSource"))
        { threw = true; }
        Assert.True(threw);
    }

    /// <summary> Validate that primitive overflows during mapping safely fall through to handled error states </summary>
    [Fact]
    public void Mapper_PrimitiveOverflow_FallsThrough()
    {
        var profile = new TestProfile();
        profile.Register<long, int>(); // This registration doesn't really matter for MapCore's primitive check

        var mapper = new MapperConfiguration(p => p.AddProfile(profile)).CreateMapper();

        // long.MaxValue to int will overflow in Convert.ChangeType
        var val = long.MaxValue;
        // Should fall through to registered maps or throw if no map
        try
        {
            mapper.Map<int>(val);
        }
        catch (AutoMappicException) { /* Expected fallthrough if no registered map */ }
    }

    private class DictDestStringVal { public Dictionary<string, string> Items { get; set; } = new(); }
    private class DictSourceLong { public Dictionary<long, long> Items { get; set; } = new(); }
    private class DictDestInt { public Dictionary<int, int> Items { get; set; } = new(); }

    private class DictSourceIntVal { public Dictionary<string, int> Items { get; set; } = new(); }
    private class DictDestLongVal { public Dictionary<string, long> Items { get; set; } = new(); }

    private class SkippedMapSource { public UnregisteredSource Item { get; set; } = new(); }
    private class SkippedMapDest { public UnregisteredDest? Item { get; set; } }

    private class ListSource { public List<NestedSource?> Items { get; set; } = new(); }
    private class ListDest { public List<NestedDest> Items { get; set; } = new(); }
    private class ListSourceUnreg { public List<object> Items { get; set; } = new(); }
    private class ListDestUnreg { public List<UnregisteredDest> Items { get; set; } = new(); }

    private class NestedSource { public int Id { get; set; } }
    private class NestedDest { public int Id { get; set; } }
    private class DictSourceNested { public Dictionary<int, NestedSource> Items { get; set; } = new(); }
    private class DictDestNested { public Dictionary<string, NestedDest> Items { get; set; } = new(); }

    private class UnregisteredSource { }
    private class UnregisteredDest { }

    public class User { public string Name { get; set; } = ""; }
    public class UserDto { public string Name { get; set; } = ""; }

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
        public IMappingExpression<S, D> Register<S, D>(Action<IMappingExpression<S, D>>? opt = null)
        {
            var exp = CreateMap<S, D>();
            opt?.Invoke(exp);
            return exp;
        }
    }
}
