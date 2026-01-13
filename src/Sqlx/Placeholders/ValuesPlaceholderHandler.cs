// <copyright file="ValuesPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Placeholders;

using System.Linq;

/// <summary>
/// Handles the {{values}} placeholder for generating INSERT parameter placeholders.
/// </summary>
/// <remarks>
/// <para>
/// This handler is always static and generates a comma-separated list of parameter placeholders.
/// </para>
/// <para>
/// Supported options:
/// </para>
/// <list type="bullet">
/// <item><description><c>--exclude col1,col2</c> - Excludes specified columns from the list</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// // Template: INSERT INTO users ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})
/// // Output:   INSERT INTO users ([name], [email]) VALUES (@name, @email)
/// </code>
/// </example>
public sealed class ValuesPlaceholderHandler : PlaceholderHandlerBase
{
    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static ValuesPlaceholderHandler Instance { get; } = new();

    /// <inheritdoc/>
    public override string Name => "values";

    /// <inheritdoc/>
    public override string Process(PlaceholderContext context, string options)
    {
        var columns = FilterColumns(context.Columns, options);
        return string.Join(", ", columns.Select(c => "@" + c.Name));
    }
}
