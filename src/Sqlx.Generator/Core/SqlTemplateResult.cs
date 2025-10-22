// -----------------------------------------------------------------------
// <copyright file="SqlTemplateResult.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

namespace Sqlx.Generator;

/// <summary>
/// Represents the result of SQL template processing.
/// </summary>
public sealed class SqlTemplateResult
{
    /// <summary>
    /// Gets the processed SQL string.
    /// </summary>
    public string ProcessedSql { get; init; } = string.Empty;

    /// <summary>
    /// Gets the extracted parameters.
    /// </summary>
    public Dictionary<string, object?> Parameters { get; init; } = new();

    /// <summary>
    /// Gets the validation warnings.
    /// </summary>
    public List<string> Warnings { get; init; } = new();

    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public List<string> Errors { get; init; } = new();

    /// <summary>
    /// Gets a value indicating whether the template processing was successful.
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Gets a value indicating whether the template has dynamic features.
    /// </summary>
    public bool HasDynamicFeatures { get; init; } = false;

    /// <summary>
    /// Gets the column names in the order they appear in the SQL SELECT statement.
    /// Used for direct ordinal access optimization.
    /// </summary>
    public List<string> ColumnOrder { get; init; } = new();
}
