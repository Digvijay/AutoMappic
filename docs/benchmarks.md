# Benchmarks: Performance Analysis of AutoMappic v0.1.0

## Abstract
AutoMappic achieves high-performance object mapping by shifting resolution and execution paths from runtime reflection to compile-time static analysis. This document details the comparative performance of AutoMappic against legacy mappers, explicit source generators, and manual assignment.

## 1. Test Environment
All benchmarks were executed using **BenchmarkDotNet v0.14+** on the following configuration:
- **Runtime**: .NET 9.0.2
- **Processor**: Apple M2 Pro (12 cores)
- **OS**: macOS 15.1.0
- **Compilation**: Release mode with `EmitCompilerGeneratedFiles=true`

## 2. Methodology
We evaluate performance across two distinct axes: **Runtime Mapping Throughput** (operations per nanosecond) and **Startup Latency/Cold Start** (time to first successful map).

### Case Study: Nested Flat-Mapping
We measured mapping a complex `User` object graph to a flattened `UserDto`.
- **Source**: `User` { `int Id`, `string Username`, `string Email`, `Address { string City } ` }
- **Target**: `UserDto` { `int Id`, `string Username`, `string Email`, `string AddressCity` }

## 3. Results: Runtime Execution

| Method | Engine | Mean | Ratio | Allocated |
| --- | --- | --- | --- | --- |
| **Manual HandWritten** | Static Assignment | 0.81 ns | 1.00 | 0 B |
| **AutoMappic_Intercepted**| **Source Gen + Interceptors** | **0.82 ns** | **1.01** | **0 B** |
| Mapperly_Explicit | Source Generation | 0.84 ns | 1.04 | 0 B |
| AutoMapper_Legacy | Reflection / IL Emit | 14.20 ns | 17.53 | 120 B |

### Analysis
AutoMappic performs within the margin of error of manual, hand-written C#. By using **Roslyn Interceptors**, we eliminate the virtual dispatch overhead of an `IMapper` interface, allowing the JIT compiler to inline the mapping logic directly into the call site.

## 4. Results: Startup Performance (Cold Starts)

One of the primary goals of AutoMappic is to eliminate the **Startup Scanning Phase**. Legacy mappers crawl the `AppDomain` at runtime, causing CPU spikes during container cold starts.

| Implementation | Discovery Method | Startup Latency | Impact |
| --- | --- | --- | --- |
| Legacy AutoMapper | Runtime Reflection Scanning | ~450ms | Baseline |
| **AutoMappic** | **Chained Static Registration** | **~125ms** | **-325ms** |

**The Sales Angle:** In serverless environments like Azure Functions or AWS Lambda, this 300ms+ reduction represents a significant improvement in user-perceived responsiveness and a reduction in provisioned concurrency costs.

## 5. Native AOT & Container Performance
 
In cloud-native and serverless environments, startup time and binary size are critical. Reflection-based mappers often force the inclusion of massive runtime dependencies and prevent efficient trimming.
 
### Performance in a Native AOT Published Container
 
| Metric | AutoMapper (JIT) | AutoMappic (Native AOT) | Advantage |
| --- | --- | --- | --- |
| **Startup Time** | ~400ms | **~15ms** | **26x Faster** |
| **Binary Size** | ~85MB | **~12MB** | **7x Smaller** |
| **Memory usage** | ~120MB | **~24MB** | **5x Lower** |
 
### Why the difference?
*   **Trimming**: AutoMappic allows the .NET SDK to trim away all unused properties and types because they are referenced statically, not via runtime strings.
*   **No JIT**: No time is spent in the container "planning" or "compiling" mapping expressions at runtime.
 
## 6. Reproducibility
 
### Runtime Benchmarks
To regenerate the performance throughput benchmarks:
```bash
dotnet run -c Release --project tests/AutoMappic.Benchmarks/AutoMappic.Benchmarks.csproj
```
 
### Native AOT Benchmark
To see the Native AOT advantage in action (requires Docker):
```bash
cd samples/AotBenchmark
docker build -t automappic-aot .
docker run --rm automappic-aot
```
