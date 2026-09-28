using Prova;
using Assert = Prova.Assertions.Assert;

namespace AutoMappic.Tests;

#region Interface Models

public class InterfaceCollSource
{
    public IReadOnlyList<int> List { get; set; } = [1, 2, 3];
    public HashSet<string> Names { get; set; } = ["A", "B"];
    public Stack<int> Numbers { get; set; } = new([10, 20]);
    public Queue<string> Commands { get; set; } = new(["Cmd1", "Cmd2"]);
}

public class InterfaceCollDto
{
    public List<int> List { get; set; } = [];
    public IReadOnlyList<string> Names { get; set; } = [];
    public List<int> Numbers { get; set; } = [];
    public List<string> Commands { get; set; } = [];
}

public class SpecializedContainerSource
{
    public List<int> Values { get; set; } = [1, 1, 2, 2];
}

public class SpecializedContainerDto
{
    public HashSet<int> Values { get; set; } = [];
}

public class StackToQueueSource { public Stack<int> Data { get; set; } = new([1, 2, 3]); }
public class StackToQueueDto { public Queue<int> Data { get; set; } = new(); }

#endregion

public class InterfaceProfile : Profile
{
    public InterfaceProfile()
    {
        CreateMap<InterfaceCollSource, InterfaceCollDto>();
        CreateMap<SpecializedContainerSource, SpecializedContainerDto>();
        CreateMap<StackToQueueSource, StackToQueueDto>();
    }
}

public class InterfaceMappingTestSuite
{
    private static IMapper GetMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<InterfaceProfile>());
        return config.CreateMapper();
    }

    /// <summary> Verify mapping between various .NET collection interfaces and concrete types. </summary>
    [Fact]
    public void Test_CollectionInterfaces_Exhaustive()
    {
        IMapper mapper = GetMapper();
        var source = new InterfaceCollSource();
        InterfaceCollDto result = mapper.Map<InterfaceCollDto>(source);

        Assert.Equal(3, result.List.Count);
        Assert.Equal(2, result.Names.Count);
        Assert.Contains("A", result.Names);
        Assert.Contains(20, result.Numbers);
    }

    /// <summary> Verify that AutoMappic can deduplicate values when mapping from a List to a HashSet. </summary>
    [Fact]
    public void Test_ListToHashSet_Deduplication()
    {
        IMapper mapper = GetMapper();
        var source = new SpecializedContainerSource();
        SpecializedContainerDto result = mapper.Map<SpecializedContainerDto>(source);

        Assert.Equal(2, result.Values.Count);
        Assert.True(result.Values.Contains(1));
        Assert.True(result.Values.Contains(2));
    }

    /// <summary> Verify that a Stack can be mapped to a Queue (order may be reversed depending on stack implementation). </summary>
    [Fact]
    public void Test_StackToQueue_Exhaustive()
    {
        IMapper mapper = GetMapper();
        var source = new StackToQueueSource();
        StackToQueueDto result = mapper.Map<StackToQueueDto>(source);

        Assert.Equal(3, result.Data.Count);
    }
}
