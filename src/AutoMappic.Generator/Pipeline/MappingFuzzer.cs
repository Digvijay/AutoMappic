using System;
using System.Buffers;

namespace AutoMappic.Generator.Pipeline;

/// <summary>
/// Provides string similarity calculations for smart-matching property names.
/// High-performance implementation using O(M) space Levenshtein distance.
/// </summary>
public static class MappingFuzzer
{
    private const int StackAllocThreshold = 256;

    /// <summary>
    /// Calculates the normalized Levenshtein similarity between two strings (1.0 = exact match).
    /// </summary>
    public static double GetSimilarity(string source, string target)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target)) return 0;
        if (source == target) return 1.0;

        int stepsToSame = ComputeLevenshteinDistance(source, target);
        return 1.0 - ((double)stepsToSame / Math.Max(source.Length, target.Length));
    }

    private static int ComputeLevenshteinDistance(string source, string target)
    {
        if (source.Length < target.Length)
        {
            var temp = source;
            source = target;
            target = temp;
        }

        int n = source.Length;
        int m = target.Length;

        if (m == 0) return n;

        // High-performance strategy: Use only two rows (O(M) space) and ArrayPool for large strings.
        int rowSize = m + 1;
        int[]? pooledBuffer = null;
        Span<int> prevRow = rowSize <= StackAllocThreshold
            ? stackalloc int[rowSize]
            : (pooledBuffer = ArrayPool<int>.Shared.Rent(rowSize));

        Span<int> currRow = rowSize <= StackAllocThreshold
            ? stackalloc int[rowSize]
            : ArrayPool<int>.Shared.Rent(rowSize); // This is a bit clumsy, but for O(M) it's fine.

        // Actually, we can just use one row if we're clever, but two rows is simpler and still O(M).
        // Let's use a single array and partition it.
        int totalSize = rowSize * 2;
        int[]? combinedPool = null;
        Span<int> combined = totalSize <= StackAllocThreshold
            ? stackalloc int[totalSize]
            : (combinedPool = ArrayPool<int>.Shared.Rent(totalSize));

        try
        {
            var prev = combined.Slice(0, rowSize);
            var curr = combined.Slice(rowSize, rowSize);

            for (int j = 0; j <= m; j++) prev[j] = j;

            for (int i = 1; i <= n; i++)
            {
                curr[0] = i;
                for (int j = 1; j <= m; j++)
                {
                    int cost = (source[i - 1] == target[j - 1]) ? 0 : 1;
                    int insertion = curr[j - 1] + 1;
                    int deletion = prev[j] + 1;
                    int substitution = prev[j - 1] + cost;

                    curr[j] = Math.Min(Math.Min(insertion, deletion), substitution);
                }
                curr.CopyTo(prev);
            }

            return prev[m];
        }
        finally
        {
            if (combinedPool != null)
            {
                ArrayPool<int>.Shared.Return(combinedPool);
            }
        }
    }
}
