// -----------------------------------------------------------------------
// <copyright file="IndentedStringBuilder.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Generator.Core;

using System;
using System.Runtime.CompilerServices;
using System.Text;

/// <summary>
/// 提供具有自动缩进功能的字符串构建器，用于生成格式化的代码。
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
    /// 向字符串构建器追加指定的字符串，并在需要时添加缩进。
    /// </summary>
    /// <param name="value">要追加的字符串。</param>
    /// <returns>当前 IndentedStringBuilder 实例，以支持链式调用。</returns>
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

    /// <summary>
    /// 向字符串构建器追加指定的字符，并在需要时添加缩进。
    /// </summary>
    /// <param name="value">要追加的字符。</param>
    /// <returns>当前 IndentedStringBuilder 实例，以支持链式调用。</returns>
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
    /// 向字符串构建器追加一个换行符。
    /// </summary>
    /// <returns>当前 IndentedStringBuilder 实例，以支持链式调用。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IndentedStringBuilder AppendLine()
    {
        builder.AppendLine();
        needsIndent = true;
        return this;
    }

    /// <summary>
    /// 根据条件向字符串构建器追加一行内容。
    /// </summary>
    /// <param name="condition">判断条件。</param>
    /// <param name="trueValue">条件为 true 时追加的字符串。</param>
    /// <param name="falseValue">条件为 false 时追加的字符串（可选）。</param>
    /// <returns>当前 IndentedStringBuilder 实例，以支持链式调用。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IndentedStringBuilder AppendLineIf(bool condition, string? trueValue, string? falseValue)
    {
        AppendLine(condition ? trueValue : falseValue);
        return this;
    }

    /// <summary>
    /// 向字符串构建器追加指定的字符串并换行。
    /// </summary>
    /// <param name="value">要追加的字符串。</param>
    /// <returns>当前 IndentedStringBuilder 实例，以支持链式调用。</returns>
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

    /// <summary>
    /// 增加缩进级别，后续的内容将使用更深的缩进。
    /// </summary>
    /// <returns>当前 IndentedStringBuilder 实例，以支持链式调用。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public IndentedStringBuilder PushIndent()
    {
        depthLevel++;
        needsIndent = true; // Next append should be indented
        return this;
    }

    /// <summary>
    /// 减少缩进级别，后续的内容将使用更浅的缩进。
    /// </summary>
    /// <returns>当前 IndentedStringBuilder 实例，以支持链式调用。</returns>
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
