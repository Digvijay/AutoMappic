using System;
using System.Buffers;

namespace AutoMappic.Generator.Pipeline;

/// <summary>
/// Optimized naming utility for zero-allocation case transformations.
/// </summary>
internal static class NamingUtility
{
    private const int StackAllocThreshold = 256;

    public static string ToSnakeCase(string name) => ToSeparatedCase(name, '_');

    public static string ToKebabCase(string name) => ToSeparatedCase(name, '-');

    private static string ToSeparatedCase(string name, char separator)
    {
        if (string.IsNullOrEmpty(name)) return name;

        // Bounded length: worst case "A" -> "a", but "ABC" could be "a_b_c" if we're not careful.
        // Actually for PascalCase, it's at most N + (N/2) separators. 2N is safe.
        int maxLength = name.Length * 2;
        char[]? pooledBuffer = null;

        // High-performance strategy: threshold for stackalloc, then pool for large names.
        Span<char> buffer = maxLength <= StackAllocThreshold
            ? stackalloc char[maxLength]
            : (pooledBuffer = ArrayPool<char>.Shared.Rent(maxLength));

        try
        {
            int position = 0;
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (i > 0 && char.IsUpper(c))
                {
                    // Word boundary logic:
                    // 1. If previous char is lower: "aB" -> "a_b"
                    // 2. If next char is lower (acronym end): "IDName" -> "id_name"
                    if (!char.IsUpper(name[i - 1]) || (i < name.Length - 1 && char.IsLower(name[i + 1])))
                    {
                        buffer[position++] = separator;
                    }
                }
                buffer[position++] = char.ToLowerInvariant(c);
            }
            return new string(buffer.Slice(0, position).ToArray());
        }
        finally
        {
            if (pooledBuffer != null)
            {
                ArrayPool<char>.Shared.Return(pooledBuffer);
            }
        }
    }

    public static string Normalize(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        // High-performance strategy: One-pass scan to see if we need a new string at all.
        bool needsNormalization = false;
        foreach (char c in name)
        {
            if (c == '_' || c == '-' || char.IsUpper(c))
            {
                needsNormalization = true;
                break;
            }
        }

        if (!needsNormalization) return name;

        char[]? pooledBuffer = null;
        Span<char> buffer = name.Length <= StackAllocThreshold
            ? stackalloc char[name.Length]
            : (pooledBuffer = ArrayPool<char>.Shared.Rent(name.Length));

        try
        {
            int position = 0;
            foreach (char c in name)
            {
                if (c != '_' && c != '-')
                {
                    buffer[position++] = char.ToLowerInvariant(c);
                }
            }
            return new string(buffer.Slice(0, position).ToArray());
        }
        finally
        {
            if (pooledBuffer != null)
            {
                ArrayPool<char>.Shared.Return(pooledBuffer);
            }
        }
    }
}
