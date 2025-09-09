// -----------------------------------------------------------------------
// <copyright file="OptimizedStringBuilder.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Sqlx.Core;

/// <summary>
/// High-performance string builder utilities for code generation.
/// </summary>
internal static class OptimizedStringBuilder
{
    private const int DefaultCapacity = 1024;
    
    /// <summary>
    /// Creates a new StringBuilder with optimized initial capacity.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringBuilder Create(int estimatedLength = DefaultCapacity)
    {
        // Use power of 2 for better memory allocation
        var capacity = GetNextPowerOfTwo(Math.Max(estimatedLength, 256));
        return new StringBuilder(capacity);
    }
    
    /// <summary>
    /// Efficiently appends multiple strings with a separator.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringBuilder AppendJoin(this StringBuilder sb, string separator, string[] values)
    {
        if (values.Length == 0) return sb;
        
        sb.Append(values[0]);
        for (int i = 1; i < values.Length; i++)
        {
            sb.Append(separator).Append(values[i]);
        }
        return sb;
    }
    
    /// <summary>
    /// Appends a string if condition is true, otherwise appends fallback.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringBuilder AppendIf(this StringBuilder sb, bool condition, string trueValue, string? falseValue = null)
    {
        return condition ? sb.Append(trueValue) : 
               falseValue != null ? sb.Append(falseValue) : sb;
    }
    
    /// <summary>
    /// Appends a line if condition is true.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringBuilder AppendLineIf(this StringBuilder sb, bool condition, string value)
    {
        return condition ? sb.AppendLine(value) : sb;
    }
    
    /// <summary>
    /// Appends a formatted template with parameter substitution.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static StringBuilder AppendTemplate(this StringBuilder sb, string template, params object[] args)
    {
        return sb.AppendFormat(template, args);
    }
    
    /// <summary>
    /// Gets the next power of two greater than or equal to the input.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetNextPowerOfTwo(int value)
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
}

