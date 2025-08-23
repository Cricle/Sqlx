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
    public IndentedStringBuilder(string content)
    {
        builder = new StringBuilder(content);
    }

    public IndentedStringBuilder Append(string value)
    {
        WriteIndent();
        builder.Append(value);
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

    public IndentedStringBuilder AppendLineIf(bool condition, string trueValue, string falseValue)
    {
        AppendLine(condition ? trueValue : falseValue);
        return this;
    }

    public IndentedStringBuilder AppendLine(string value)
    {
        WriteIndent();
        builder.AppendLine(value);
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
