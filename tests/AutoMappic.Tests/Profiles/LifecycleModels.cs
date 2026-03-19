namespace AutoMappic.Tests;

public class LifecycleSource { public int Value { get; set; } }
public class LifecycleDest 
{ 
    public int Value { get; set; } 
    public bool WasBeforeCalled { get; set; }
    public bool WasBeforeAsyncCalled { get; set; }
    public bool WasAfterCalled { get; set; }
    public bool WasAfterAsyncCalled { get; set; }
}
