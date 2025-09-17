// -----------------------------------------------------------------------
// <copyright file="IndentedStringBuilder.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Generator.Core;

using System;
using System.Runtime.CompilerServices;
using System.Text;

public sealed class IndentedStringBuilder
{
    private const byte IndentSize = 4;
    private readonly StringBuilder builder;
    private byte depthLevel;
    private bool needsIndent = true;

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
                needsIndent = false;
            }
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
            needsIndent = false;
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
        needsIndent = true; // Next append should be indented
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IndentedStringBuilder PopIndent()
    {
        if (depthLevel == 0)
            throw new InvalidOperationException("Cannot pop at depthlevel 0");

        depthLevel--;
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
            builder.Append(' ', depthLevel * IndentSize);
        }
    }
}
