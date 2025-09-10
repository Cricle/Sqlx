// -----------------------------------------------------------------------
// <copyright file="MemoryOptimizer.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace Sqlx.Core;

/// <summary>
/// High-performance memory optimization utilities for source generation.
/// </summary>
internal static class MemoryOptimizer
{
    private static readonly ArrayPool<char> CharPool = ArrayPool<char>.Shared;
    private static readonly ArrayPool<byte> BytePool = ArrayPool<byte>.Shared;

    /// <summary>
    /// Creates a StringBuilder with optimal initial capacity based on estimated size.
    /// </summary>
    /// <param name="estimatedSize">The estimated final size.</param>
    /// <returns>An optimized StringBuilder instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringBuilder CreateOptimizedStringBuilder(int estimatedSize = 1024)
    {
        // Use next power of 2 for better memory alignment
        var capacity = RoundUpToPowerOf2(Math.Max(estimatedSize, 256));
        return new StringBuilder(capacity);
    }

    /// <summary>
    /// Efficiently concatenates strings using pooled memory.
    /// </summary>
    /// <param name="strings">The strings to concatenate.</param>
    /// <returns>The concatenated result.</returns>
    public static string ConcatenateEfficiently(params string[] strings)
    {
        if (strings.Length == 0) return string.Empty;
        if (strings.Length == 1) return strings[0] ?? string.Empty;

        // Calculate total length
        var totalLength = 0;
        foreach (var str in strings)
        {
            if (str != null) totalLength += str.Length;
        }

        if (totalLength == 0) return string.Empty;

        // Use pooled array for better performance
        var buffer = CharPool.Rent(totalLength);
        try
        {
            var position = 0;
            foreach (var str in strings)
            {
                if (str != null)
                {
                    str.AsSpan().CopyTo(buffer.AsSpan(position));
                    position += str.Length;
                }
            }

            return new string(buffer, 0, totalLength);
        }
        finally
        {
            CharPool.Return(buffer);
        }
    }

    /// <summary>
    /// Creates an optimized string with pre-allocated capacity.
    /// </summary>
    /// <param name="action">The action to build the string.</param>
    /// <param name="estimatedCapacity">The estimated capacity needed.</param>
    /// <returns>The built string.</returns>
    public static string BuildString(Action<StringBuilder> action, int estimatedCapacity = 1024)
    {
        using var builder = new PooledStringBuilder(estimatedCapacity);
        action(builder.StringBuilder);
        return builder.ToString();
    }

    /// <summary>
    /// Rounds up to the next power of 2 for optimal memory allocation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int RoundUpToPowerOf2(int value)
    {
        if (value <= 0) return 1;

        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        return value + 1;
    }

    /// <summary>
    /// A pooled StringBuilder wrapper for automatic memory management.
    /// </summary>
    private readonly struct PooledStringBuilder : IDisposable
    {
        private readonly StringBuilder _stringBuilder;

        public PooledStringBuilder(int capacity)
        {
            _stringBuilder = CreateOptimizedStringBuilder(capacity);
        }

        public StringBuilder StringBuilder => _stringBuilder;

        public override string ToString() => _stringBuilder.ToString();

        public void Dispose()
        {
            _stringBuilder.Clear();
            // Note: StringBuilder doesn't use pooled memory, but we clear it to help GC
        }
    }
}

/// <summary>
/// High-performance string interpolation helpers.
/// </summary>
internal static class StringInterpolation
{
    /// <summary>
    /// Efficiently formats a string with up to 3 parameters using spans.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Format(string format, string? arg0 = null, string? arg1 = null, string? arg2 = null)
    {
        if (string.IsNullOrEmpty(format)) return string.Empty;

        // Fast path for no arguments
        if (arg0 == null && arg1 == null && arg2 == null) return format;

        // Use StringBuilder for complex formatting
        var capacity = format.Length + (arg0?.Length ?? 0) + (arg1?.Length ?? 0) + (arg2?.Length ?? 0);
        var builder = new StringBuilder(capacity);

        var formatSpan = format.AsSpan();
        var position = 0;

        while (position < formatSpan.Length)
        {
            var nextBrace = formatSpan.Slice(position).IndexOf('{');
            if (nextBrace == -1)
            {
                builder.Append(formatSpan.Slice(position).ToString());
                break;
            }

            // Add text before the brace
            if (nextBrace > 0)
            {
                builder.Append(formatSpan.Slice(position, nextBrace).ToString());
            }

            position += nextBrace;

            // Check for parameter index
            if (position + 2 < formatSpan.Length && formatSpan[position + 2] == '}')
            {
                var paramIndex = formatSpan[position + 1] - '0';
                var arg = paramIndex switch
                {
                    0 => arg0,
                    1 => arg1,
                    2 => arg2,
                    _ => null
                };

                if (arg != null) builder.Append(arg);
                position += 3; // Skip {n}
            }
            else
            {
                builder.Append(formatSpan[position].ToString());
                position++;
            }
        }

        return builder.ToString();
    }
}
