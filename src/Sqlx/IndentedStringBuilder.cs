// -----------------------------------------------------------------------
// <copyright file="IndentedStringBuilder.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx;

using System;
using System.Text;

internal sealed class IndentedStringBuilder
{
    private const byte IndentSize = 4;
    private readonly StringBuilder builder;
    private byte depthLevel;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndentedStringBuilder"/> class.
    /// </summary>
    /// <param name="content">Initial content for the string builder.</param>
    public IndentedStringBuilder(string? content)
    {
        builder = new StringBuilder(content ?? string.Empty);
    }

    public IndentedStringBuilder Append(string? value)
    {
        // Apply indentation for null strings, but not for empty or whitespace-only strings
        if (value is null || !string.IsNullOrWhiteSpace(value))
        {
            WriteIndent();
        }
        builder.Append(value ?? string.Empty);
        return this;
    }

    public IndentedStringBuilder Append(char value)
    {
        WriteIndent();
        builder.Append(value);
        return this;
    }

    public IndentedStringBuilder AppendLine()
    {
        builder.AppendLine();
        return this;
    }

    public IndentedStringBuilder AppendLineIf(bool condition, string? trueValue, string? falseValue)
    {
        AppendLine(condition ? trueValue : falseValue);
        return this;
    }

    public IndentedStringBuilder AppendLine(string? value)
    {
        // Apply indentation for null strings, but not for empty or whitespace-only strings
        if (value is null || (!string.IsNullOrEmpty(value) && !string.IsNullOrWhiteSpace(value)))
        {
            WriteIndent();
        }
        builder.AppendLine(value ?? string.Empty);
        return this;
    }

    public IndentedStringBuilder PushIndent()
    {
        depthLevel++;
        return this;
    }

    public IndentedStringBuilder PopIndent()
    {
        if (depthLevel == 0)
            throw new InvalidOperationException("Cannot pop at depthlevel 0");

        depthLevel--;
        return this;
    }

    /// <inheritdoc/>
    public override string ToString() => builder.ToString();

    private void WriteIndent()
    {
        if (depthLevel > 0)
        {
            builder.Append(' ', depthLevel * IndentSize);
        }
    }
}
