// <copyright file="PlaceholderType.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

/// <summary>
/// Indicates when a placeholder is resolved.
/// </summary>
public enum PlaceholderType
{
    /// <summary>
    /// Resolved once during Prepare phase.
    /// </summary>
    Static,

    /// <summary>
    /// Resolved each time during Render phase.
    /// </summary>
    Dynamic,
}
