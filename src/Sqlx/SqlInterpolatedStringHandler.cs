// <copyright file="SqlInterpolatedStringHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

#if NET6_0_OR_GREATER

namespace Sqlx;

using System;
using System.Runtime.CompilerServices;

/// <summary>
/// Interpolated string handler for SqlBuilder.
/// Automatically converts interpolated values to SQL parameters for injection safety.
/// </summary>
/// <remarks>
/// This handler is used by the C# compiler when using interpolated strings with SqlBuilder.
/// It automatically parameterizes all interpolated values to prevent SQL injection.
/// </remarks>
[InterpolatedStringHandler]
public ref struct SqlInterpolatedStringHandler
{
    private SqlBuilder _builder;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlInterpolatedStringHandler"/> struct.
    /// </summary>
    /// <param name="literalLength">The estimated length of literal text in the interpolated string.</param>
    /// <param name="formattedCount">The number of interpolated values.</param>
    /// <param name="builder">The SqlBuilder instance to append to.</param>
    public SqlInterpolatedStringHandler(int literalLength, int formattedCount, SqlBuilder builder)
    {
        _builder = builder;
    }

    /// <summary>
    /// Appends a literal string to the SQL builder.
    /// </summary>
    /// <param name="value">The literal string value.</param>
    public void AppendLiteral(string value)
    {
        if (string.IsNullOrEmpty(value))
            return;

        _builder.AppendLiteralInternal(value);
    }

    /// <summary>
    /// Appends a formatted value as a SQL parameter.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to parameterize.</param>
    public void AppendFormatted<T>(T value)
    {
        _builder.AppendParameterInternal(value);
    }

    /// <summary>
    /// Appends a formatted value as a SQL parameter with format string.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to parameterize.</param>
    /// <param name="format">The format string (currently ignored, value is used as-is).</param>
    public void AppendFormatted<T>(T value, string? format)
    {
        // For now, we ignore the format string and just parameterize the value
        // In the future, we could use format for type conversion hints
        _builder.AppendParameterInternal(value);
    }
}

#endif
