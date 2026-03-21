# Sustainability & ESG

In the modern era of cloud computing, performance isn't just about speed--it's about **Environmental Impact**. Every CPU cycle saved is a reduction in carbon emissions.

## The Green Engineering Advantage

AutoMappic is built on the principles of **Green Engineering**. By moving the computational cost of object mapping from the "Execution Phase" (runtime) to the "Preparation Phase" (compile-time), we significantly reduce the environmental footprint of .NET applications.

### 1. Reduced Server Density
Traditional mappers use Runtime Reflection and JIT-compilation (`Expression.Compile()`), which causes massive CPU spikes during application startup (Cold Start). In elastic cloud environments (Serverless, K8s), this forces you to provision:
*   **Higher CPU Limits** just to handle startup.
*   **More Memory** to store JIT-compiled IL delegates.

**AutoMappic eliminates these spikes**, allowing you to run your services with smaller instances and higher density, leading to lower energy consumption.

### 2. Native AOT & Energy Efficiency
By being 100% Native AOT compatible, AutoMappic applications achieve:
*   **Instant Startup**: No JIT compilation means zero energy wasted on JIT'ing the same mapping logic thousands of times.
*   **Smaller Binary Size**: Reduced storage and network bandwidth for CI/CD pipelines and container registries.

### 3. Zero-LINQ Collection Mapping
Most mappers rely on LINQ's `.Select().ToList()` for collection mapping, which generates a significant number of internal allocations and garbage collection (GC) pressure. 
AutoMappic generates specialized `for` loops with pre-allocated capacity for all collection mappings. This leads to:
- **No LINQ overhead**: Fewer hidden objects and fewer "boxing" conversions.
- **Lower GC Pressure**: Less energy spent on garbage collection in high-throughput applications. 
- **Sustainable Throughput**: Consistent performance without the "GC spikes" typical of LINQ-heavy mappers.

### 4. Asynchronous Scaling
By providing first-class support for `MapAsync`, AutoMappic ensures that your mapping rules don't block threads. This is crucial for **Thread Density**:
- **Non-blocking I/O**: Wait for external data (DB/API) without pinning a thread.
- **Higher Throughput**: Handle more requests with fewer active threads, reducing context switching and overall server energy load.

## The ESG (Environmental, Social, and Governance) Impact

For enterprises with ESG targets, switching to AutoMappic is a simple technical decision with measurable sustainability benefits.

| Metric | Traditional Mapper | AutoMappic |
| :--- | :--- | :--- |
| **Startup Energy Cost** | High (Reflection + JIT) | **Zero** (Native machine code) |
| **Steady State CPU Leak** | Moderate (Reflection cache) | **Zero** (Static dispatch) |
| **Carbon Footprint** | Cumulative over every restart | **Net-Zero** per-request overhead |

---

## Sustainable Engineering Badge

You can display the AutoMappic badge on your project to show your commitment to building high-efficiency, sustainable .NET software.

![Sustainable Engineering](https://img.shields.io/badge/Engineering-Sustainable-green?style=for-the-badge&logo=leaf)

```markdown
![Sustainable Engineering](https://img.shields.io/badge/Engineering-Sustainable-green?style=for-the-badge&logo=leaf)
```

---

[Back: How it Works <-](./how-it-works.md)
