// -----------------------------------------------------------------------
// <copyright file="IndentedStringBuilder.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Generator;

using System;
using System.Runtime.CompilerServices;
using System.Text;
using Sqlx.Generator;

/// <summary>
/// Provides a string builder with automatic indentation functionality for generating formatted code.
/// </summary>
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

    /// <summary>
    /// Append the specified string to the string builder, adding indentation when needed.
    /// </summary>
    /// <param name="value">The string to append.</param>
    /// <returns>The current IndentedStringBuilder instance to support method chaining.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IndentedStringBuilder Append(string? value)
    {
        if (needsIndent && (value == null || (value.Length > 0 && !string.IsNullOrWhiteSpace(value))))
        {
            WriteIndent();
            needsIndent = false;
        }

        if (!string.IsNullOrEmpty(value)) builder.Append(value);
        return this;
    }

    /// <summary>
    /// Append the specified character to the string builder, adding indentation when needed.
    /// </summary>
    /// <param name="value">The character to append.</param>
    /// <returns>The current IndentedStringBuilder instance to support method chaining.</returns>
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

    /// <summary>
    /// Append a newline character to the string builder.
    /// </summary>
    /// <returns>The current IndentedStringBuilder instance to support method chaining.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IndentedStringBuilder AppendLine()
    {
        builder.AppendLine();
        needsIndent = true;
        return this;
    }

    /// <summary>
    /// Conditionally append a line of content to the string builder.
    /// </summary>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="trueValue">The string to append when condition is true.</param>
    /// <param name="falseValue">The string to append when condition is false (optional).</param>
    /// <returns>The current IndentedStringBuilder instance to support method chaining.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IndentedStringBuilder AppendLineIf(bool condition, string? trueValue, string? falseValue) => AppendLine(condition ? trueValue : falseValue);

    /// <summary>
    /// Append the specified string to the string builder and add a newline.
    /// </summary>
    /// <param name="value">The string to append.</param>
    /// <returns>The current IndentedStringBuilder instance to support method chaining.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IndentedStringBuilder AppendLine(string? value)
    {
        if (needsIndent && (value == null || (value.Length > 0 && !string.IsNullOrWhiteSpace(value)))) WriteIndent();
        builder.AppendLine(value ?? string.Empty);
        needsIndent = true;
        return this;
    }

    /// <summary>
    /// Increase the indentation level, subsequent content will use deeper indentation.
    /// </summary>
    /// <returns>The current IndentedStringBuilder instance to support method chaining.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IndentedStringBuilder PushIndent()
    {
        depthLevel++;
        needsIndent = true;
        return this;
    }

    /// <summary>
    /// Decrease the indentation level, subsequent content will use shallower indentation.
    /// </summary>
    /// <returns>The current IndentedStringBuilder instance to support method chaining.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IndentedStringBuilder PopIndent()
    {
        if (depthLevel == 0) throw new InvalidOperationException(SqlxExceptionMessages.InvalidDepthLevel);
        depthLevel--;
        needsIndent = true;
        return this;
    }

    /// <inheritdoc/>
    public override string ToString() => builder.ToString();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void WriteIndent()
    {
        if (depthLevel > 0) builder.Append(' ', depthLevel * IndentSize);
    }
}
