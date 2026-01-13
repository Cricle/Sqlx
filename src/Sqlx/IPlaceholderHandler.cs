// <copyright file="IPlaceholderHandler.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System.Collections.Generic;

/// <summary>
/// Defines the contract for handling a specific placeholder type in SQL templates.
/// </summary>
/// <remarks>
/// <para>
/// Placeholder handlers are responsible for processing placeholders like <c>{{columns}}</c>,
/// <c>{{where --param predicate}}</c>, etc. Each handler has a unique name and can be either
/// static (resolved at prepare time) or dynamic (resolved at render time).
/// </para>
/// <para>
/// To create a custom handler, inherit from <see cref="PlaceholderHandlerBase"/> and register
/// it using <see cref="PlaceholderProcessor.RegisterHandler"/>.
/// </para>
/// </remarks>
public interface IPlaceholderHandler
{
    /// <summary>
    /// Gets the placeholder name (e.g., "columns", "where", "limit").
    /// </summary>
    /// <remarks>
    /// This name is used to match placeholders in SQL templates. Names are case-insensitive.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Determines the placeholder type based on the provided options.
    /// </summary>
    /// <param name="options">The options string from the placeholder (e.g., "--param predicate").</param>
    /// <returns>
    /// <see cref="PlaceholderType.Static"/> if the placeholder can be resolved at prepare time;
    /// <see cref="PlaceholderType.Dynamic"/> if it requires runtime parameters.
    /// </returns>
    PlaceholderType GetType(string options);

    /// <summary>
    /// Processes a static placeholder during the Prepare phase.
    /// </summary>
    /// <param name="context">The placeholder context containing dialect, table, and column information.</param>
    /// <param name="options">The options string from the placeholder.</param>
    /// <returns>The SQL fragment to replace the placeholder with.</returns>
    string Process(PlaceholderContext context, string options);

    /// <summary>
    /// Renders a dynamic placeholder during the Render phase.
    /// </summary>
    /// <param name="context">The placeholder context containing dialect, table, and column information.</param>
    /// <param name="options">The options string from the placeholder.</param>
    /// <param name="parameters">The dynamic parameters provided at render time.</param>
    /// <returns>The SQL fragment to replace the placeholder with.</returns>
    string Render(PlaceholderContext context, string options, IReadOnlyDictionary<string, object?>? parameters);
}
