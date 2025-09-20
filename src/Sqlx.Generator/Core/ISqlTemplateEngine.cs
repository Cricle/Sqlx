// -----------------------------------------------------------------------
// <copyright file="ISqlTemplateEngine.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Sqlx.Generator.Core;

/// <summary>
/// Interface for SQL template processing engine.
/// This is the unified way to handle all SQL template processing in Sqlx.
/// </summary>
public interface ISqlTemplateEngine
{
    /// <summary>
    /// Processes a SQL template and returns the final SQL with parameter handling.
    /// </summary>
    /// <param name="templateSql">The SQL template with placeholders.</param>
    /// <param name="method">The method being processed.</param>
    /// <param name="entityType">The entity type if available.</param>
    /// <param name="tableName">The table name.</param>
    /// <returns>The processed SQL template result.</returns>
    SqlTemplateResult ProcessTemplate(string templateSql, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName);

    /// <summary>
    /// Validates a SQL template for correctness.
    /// </summary>
    /// <param name="templateSql">The SQL template to validate.</param>
    /// <returns>Validation result with errors if any.</returns>
    TemplateValidationResult ValidateTemplate(string templateSql);
}

/// <summary>
/// Result of SQL template processing.
/// </summary>
public class SqlTemplateResult
{
    /// <summary>
    /// Gets or sets the processed SQL.
    /// </summary>
    public string ProcessedSql { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parameter mappings.
    /// </summary>
    public List<ParameterMapping> Parameters { get; set; } = new();

    /// <summary>
    /// Gets or sets whether the template uses dynamic features.
    /// </summary>
    public bool HasDynamicFeatures { get; set; }

    /// <summary>
    /// Gets or sets any processing warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Parameter mapping information.
/// </summary>
public class ParameterMapping
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
    /// Gets or sets whether the parameter is nullable.
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Gets or sets the DB type.
    /// </summary>
    public string DbType { get; set; } = string.Empty;
}

/// <summary>
/// Template validation result.
/// </summary>
public class TemplateValidationResult
{
    /// <summary>
    /// Gets or sets whether the template is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets validation errors.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets or sets validation warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = new();
}


