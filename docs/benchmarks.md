# Benchmarks: Performance Analysis of AutoMappic v0.4.0

## Abstract
AutoMappic achieves high-performance object mapping by shifting resolution and execution paths from runtime reflection to compile-time static analysis. This document details the comparative performance of AutoMappic against legacy mappers, explicit source generators, and manual assignment.

## 1. Test Environment
All benchmarks were executed using **BenchmarkDotNet v0.15.8** on the following configuration:
- **Runtime**: .NET 9.0.11 (RyuJIT x86-64-v3)
- **Processor**: Intel Core i7-4980HQ CPU 2.80GHz (Haswell), 4 physical / 8 logical cores
- **OS**: macOS Sequoia 15.7
- **Stabilization**: `iterationCount: 30`, `warmupCount: 10`

## 2. Results: Runtime Execution (Single Object)

Mapping a complex `User` object graph to a flattened `UserDto`.

| Method | Engine | Mean | Ratio | Allocated |
| --- | --- | --- | --- | --- |
| AutoMapper_Legacy | Reflection / IL Emit | 188.00 ns | 1.00 (baseline) | 48 B |
| **Manual_HandWritten** | Static Assignment | **28.04 ns** | **0.15** | **48 B** |
| **AutoMappic_Intercepted**| **Source Gen + Interceptors** | **29.12 ns** | **0.15** | **48 B** |
| Mapperly_Explicit | Source Generation | 28.50 ns | 0.15 | 48 B |

### Analysis
AutoMappic maintains **parity with hand-written manual mapping** (~29ns vs ~28ns) and is **6.5x faster** than AutoMapper. The v0.4.0 additions (MappingContext, Patch Mode, Static Converters) introduce **zero measurable overhead** as the generated mapping path remains straight-line assignment code.

## 3. Results: Collection Mapping (IMapper Interception)

v0.4.0-refined introduces full interception for `IMapper.Map<List<T1>, List<T2>>()`. This closes the gap where collection wrappers previously fell back to the reflection engine.

### Comparison: mapping `List<PointSource>` to `List<PointDto>` (1,000 items)

| Method | Engine | Mean | Ratio | Allocated |
| --- | --- | --- | --- | --- |
| AutoMapper_List | Runtime Reflection + LINQ | 30.19 μs | 1.00 (baseline) | 39.6 KB |
| **Manual_List** | for-loop / manually-sized List | **21.37 μs** | **0.71** | **31.3 KB** |
| **AutoMappic_ZeroLinq** | **Intercepted static for-loop**| **20.89 μs** | **0.69** | **31.3 KB** |

### Analysis
By intercepting the high-level `IMapper.Map<List<T>>` calls, AutoMappic now outperforms AutoMapper by **45%** in collection mapping throughput and matches manual loop performance exactly. Memory allocation is reduced by **21%** by bypassing the "LINQ tax" and pre-initializing list capacity based on source count.

## 4. Zero-Allocation Optimization: MappingContext
In v0.4.0, `MappingContext` was converted from a `class` to a `readonly struct`. 
- **Hot Path**: When identity management is not enabled (default), the `default(MappingContext)` occupies zero heap space.
- **Result**: Even if the generator passes a context parameter to every method, there is **zero allocation overhead** for the context itself.

## 5. Native AOT & Container Performance
 
| Metric | AutoMapper (JIT) | AutoMappic (Native AOT) | Advantage |
| --- | --- | --- | --- |
| **Startup Time** | ~400ms | **~15ms** | **26x Faster** |
| **Throughput (100k)**| ~140ms | **~7ms** | **20x Faster** |
| **Binary Size** | ~85MB | **~12MB** | **7x Smaller** |
| **Memory usage** | ~120MB | **~24MB** | **5x Lower** |
 
## 6. Reproducibility
 
To reproduce these results, run the following from the project root:
```bash
dotnet run -c Release --project tests/AutoMappic.Benchmarks/AutoMappic.Benchmarks.csproj
```
