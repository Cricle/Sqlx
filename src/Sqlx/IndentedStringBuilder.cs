// -----------------------------------------------------------------------
// <copyright file="IndentedStringBuilder.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

using System;
using System.Runtime.CompilerServices;
using System.Text;

internal sealed class IndentedStringBuilder
{
    private const byte IndentSize = 4;
    private readonly StringBuilder builder;
    private byte depthLevel;
    private bool needsIndent = true;
    private string? cachedIndent;

    // Pre-allocated indent strings for common depths (performance optimization)
    private static readonly string[] PrecomputedIndents =
    {
        "",                    // 0
        "    ",                // 1
        "        ",            // 2  
        "            ",        // 3
        "                ",    // 4
        "                    ", // 5
        "                        ", // 6
        "                            ", // 7
        "                                " // 8
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="IndentedStringBuilder"/> class.
    /// </summary>
    /// <param name="content">Initial content for the string builder.</param>
    public IndentedStringBuilder(string? content)
    {
        builder = new StringBuilder(content ?? string.Empty);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IndentedStringBuilder Append(string? value)
    {
        if (needsIndent)
        {
            // Add indent for null strings or non-whitespace strings
            if (value == null || (value.Length > 0 && !string.IsNullOrWhiteSpace(value)))
            {
                WriteIndent();
            }
            // Don't set needsIndent = false here anymore
        }

        if (!string.IsNullOrEmpty(value))
        {
            builder.Append(value);
        }
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IndentedStringBuilder Append(char value)
    {
        if (needsIndent)
        {
            WriteIndent();
            // Don't set needsIndent = false here anymore
        }
        builder.Append(value);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IndentedStringBuilder AppendLine()
    {
        builder.AppendLine();
        needsIndent = true;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IndentedStringBuilder AppendLineIf(bool condition, string? trueValue, string? falseValue)
    {
        AppendLine(condition ? trueValue : falseValue);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IndentedStringBuilder AppendLine(string? value)
    {
        if (needsIndent)
        {
            // Add indent for null strings or non-whitespace strings
            if (value == null || (value.Length > 0 && !string.IsNullOrWhiteSpace(value)))
            {
                WriteIndent();
            }
        }
        builder.AppendLine(value ?? string.Empty);
        needsIndent = true;
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IndentedStringBuilder PushIndent()
    {
        depthLevel++;
        cachedIndent = null; // Invalidate cache
        needsIndent = true; // Next append should be indented
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IndentedStringBuilder PopIndent()
    {
        if (depthLevel == 0)
            throw new InvalidOperationException("Cannot pop at depthlevel 0");

        depthLevel--;
        cachedIndent = null; // Invalidate cache
        needsIndent = true; // Next append should be indented
        return this;
    }

    /// <inheritdoc/>
    public override string ToString() => builder.ToString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteIndent()
    {
        if (depthLevel > 0)
        {
            // Use precomputed indents for common depths to avoid allocations
            if (depthLevel < PrecomputedIndents.Length)
            {
                builder.Append(PrecomputedIndents[depthLevel]);
            }
            else
            {
                // Fallback for deep nesting (rare case)
                cachedIndent ??= new string(' ', depthLevel * IndentSize);
                builder.Append(cachedIndent);
            }
        }
    }
}
