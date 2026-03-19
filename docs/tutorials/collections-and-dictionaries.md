# Mapping Collections & Dictionaries

AutoMappic takes collection mapping seriously, with a focus on **efficiency** and **performance**.

## 1. Zero-Allocation Mapping

When mapping a list of objects, AutoMappic generates specialized synchronous code instead of using LINQ.

```csharp
public class Order { public List<LineItem> Items { get; set; } = new(); }
public class OrderDto { public List<LineItemDto> Items { get; set; } = new(); }

CreateMap<Order, OrderDto>();
CreateMap<LineItem, LineItemDto>();
```

AutoMappic will generate a `foreach` loop that:
1.  **Measures** the source collection's count or length.
2.  **Pre-allocates** the destination list with the exact required capacity (minimizing intermediate array allocations).
3.  **Iterates** and maps each element statically.

## 2. Arrays, Lists, and Mixed Collections

AutoMappic supports all common collection types and handles their conversions automatically:
- `List<S>` $\to$ `List<D>`
- `S[]` $\to$ `D[]`
- `List<S>` $\to$ `D[]` (uses `.ToArray()`)
- `S[]` $\to$ `List<D>` (uses `new List<D>(source.Length)`)

```csharp
public class Source { public string[] Tags { get; set; } }
public class Dest { public List<string> Tags { get; set; } }
```

The generator will correctly unroll those assignments and ensure the destination is correctly initialized.

## 3. Dictionary Mapping

AutoMappic also supports `IDictionary<K, V>` mapping, where it maps both the key and value independently.

```csharp
public class Stats { public Dictionary<string, int> Values { get; set; } }
public class StatsDto { public Dictionary<string, int> Values { get; set; } }
```

AutoMappic will emit a loop that:
1.  Iterates through `KeyValuePair` entries in the source.
2.  Maps the key (if necessary).
3.  Maps the value (if necessary).
4.  Adds to the destination dictionary efficiently.

## 4. Deep Dictionary Projection

AutoMappic can map dictionaries where **both the Key and Value** need conversion. This is a common requirement for API normalization.

```csharp
public class Stats { public Dictionary<int, TaskSource> Tasks { get; set; } }
public class StatsDto { public Dictionary<string, TaskDest> Tasks { get; set; } }

CreateMap<Stats, StatsDto>();
CreateMap<TaskSource, TaskDest>();
```

In a single pass, AutoMappic will:
1.  **Iterate** through the source dictionary.
2.  **Convert the Key** from `int` $\to$ `string` (via an optimized `.ToString()` call).
3.  **Recursively Map the Value** from `TaskSource` $\to$ `TaskDest`.
4.  **Populate** the destination dictionary.

---

## 5. Zero-LINQ Technology

AutoMappic avoids the allocation and runtime overhead of LINQ by generating specialized `for` and `foreach` loops.

| Feature | LINQ (`.Select().ToList()`) | AutoMappic (Zero-LINQ) |
| :--- | :--- | :--- |
| **Throughput** | Moderate | Extreme |
| **Allocations** | High (Intermediate Enumerators) | Minimal (Pre-allocated) |
| **JIT Overhead** | High (Generic JIT) | Zero (Static loops) |
| **AOT Friendly** | Partially | 100% |

By unrolling the loops at compile-time, AutoMappic gives you the performance of hand-written code with the maintenance simplicity of a mapping library.

---

With these optimizations, collection mapping becomes a zero-overhead operation at both compile-time and runtime.
