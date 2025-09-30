// -----------------------------------------------------------------------
// <copyright file="TemplateValidationResult.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;

namespace Sqlx.Generator;

/// <summary>
/// Represents the result of template validation.
/// </summary>
public sealed class TemplateValidationResult
{
    /// <summary>
    /// Gets the validation warnings.
    /// </summary>
    public List<string> Warnings { get; init; } = new();

    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public List<string> Errors { get; init; } = new();

    /// <summary>
    /// Gets a value indicating whether the validation was successful.
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Gets the validation suggestions.
    /// </summary>
    public List<string> Suggestions { get; init; } = new();
}
