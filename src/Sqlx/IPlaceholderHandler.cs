// <copyright file="IPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System.Collections.Generic;

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
    /// </summary>
    PlaceholderType GetType(string options);

    /// <summary>
    /// Processes a static placeholder during Prepare phase.
    /// </summary>
    string Process(PlaceholderContext context, string options);

    /// <summary>
    /// Renders a dynamic placeholder during Render phase.
    /// </summary>
    string Render(PlaceholderContext context, string options, IReadOnlyDictionary<string, object?>? parameters);
}
