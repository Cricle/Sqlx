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

    public void Append(string value)
    {
        WriteIndent();
        builder.Append(value);
    }

    public void Append(char value)
    {
        WriteIndent();
        builder.Append(value);
    }

    public void AppendLine()
    {
        builder.AppendLine();
    }

    public void AppendLine(string value)
    {
        WriteIndent();
        builder.AppendLine(value);
    }

    public void PushIndent()
    {
        depthLevel++;
    }

    public void PopIndent()
    {
        if (depthLevel == 0)
            throw new InvalidOperationException("Cannot pop at depthlevel 0");

        depthLevel--;
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
