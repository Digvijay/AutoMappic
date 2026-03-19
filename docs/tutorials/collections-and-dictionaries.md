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

## 4. Collection Pre-allocation Logic

For any source that implements `ICollection<T>`, we pre-allocate the destination capacity:

```csharp
// Generated code looks like this:
var resultList = new global::System.Collections.Generic.List<LineItemDto>(source.Items.Count);
foreach (var item in source.Items) resultList.Add(item.MapToLineItemDto());
```

This makes AutoMappic significantly faster than standard mappers that use `.Select(...).ToList()`.

---

With these optimizations, collection mapping becomes a zero-overhead operation at both compile-time and runtime.
