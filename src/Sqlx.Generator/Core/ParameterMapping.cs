// -----------------------------------------------------------------------
// <copyright file="ParameterMapping.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Generator;

/// <summary>
/// Represents a parameter mapping for SQL templates.
/// </summary>
public sealed class ParameterMapping
{
    /// <summary>
    /// Gets or sets the parameter name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parameter type.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parameter value.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the parameter is nullable.
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Gets or sets the database type.
    /// </summary>
    public string DbType { get; set; } = string.Empty;
}
