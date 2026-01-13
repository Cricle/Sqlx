// <copyright file="IPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

/// <summary>
/// Handles a specific placeholder type in SQL templates.
/// </summary>
public interface IPlaceholderHandler
{
    /// <summary>
    /// Gets the placeholder name (e.g., "columns", "where", "limit").
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Determines the placeholder type based on options.
    /// For example, {{limit --count 1}} is Static, {{limit --param limit}} is Dynamic.
    /// </summary>
    /// <param name="options">The options string after the placeholder name.</param>
    /// <returns>The placeholder type.</returns>
    PlaceholderType GetType(string options);

    /// <summary>
    /// Processes the placeholder and returns the replacement SQL.
    /// </summary>
    /// <param name="context">The placeholder context.</param>
    /// <param name="options">The options string after the placeholder name.</param>
    /// <returns>The replacement SQL string.</returns>
    string Process(PlaceholderContext context, string options);
}
