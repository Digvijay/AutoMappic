using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AutoMappic.Generator.Models;

/// <summary>
///   An immutable array wrapper that implements structural value equality so that
///   Roslyn's incremental generator pipeline can correctly cache and short-circuit
///   pipeline stages when the contents have not changed.
/// </summary>
/// <remarks>
///   Passing a raw <c>ImmutableArray&lt;T&gt;</c> or <c>List&lt;T&gt;</c> through
///   the incremental pipeline breaks caching because those types use reference equality.
///   This wrapper provides the necessary <see cref="Equals(EquatableArray{T})" /> and
///   <see cref="GetHashCode" /> overrides so the pipeline sees "nothing changed."
/// </remarks>
/// <typeparam name="T">The element type. Must itself implement value equality.</typeparam>
internal sealed class EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T>
{
    /// <summary>An empty <see cref="EquatableArray{T}" />.</summary>
    public static readonly EquatableArray<T> Empty = new(Array.Empty<T>());

    private readonly T[] _items;

    /// <summary>Initialises the array from an existing sequence.</summary>
    public EquatableArray(IEnumerable<T> items) =>
        _items = items is T[] arr ? arr : items.ToArray();

    /// <summary>Gets the number of elements.</summary>
    public int Count => _items.Length;

    /// <inheritdoc />
    public bool Equals(EquatableArray<T>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (_items.Length != other._items.Length) return false;

        for (var i = 0; i < _items.Length; i++)
        {
            if (!_items[i].Equals(other._items[i])) return false;
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        // Manual hash accumulation — HashCode is not available in netstandard2.0.
        unchecked
        {
            var hash = 17;
            foreach (var item in _items)
                hash = hash * 31 + (item?.GetHashCode() ?? 0);
            return hash;
        }
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_items).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
