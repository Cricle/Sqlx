// <copyright file="IEntityProvider.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System;
using System.Collections.Generic;

/// <summary>
/// Provides entity metadata for SQL generation without reflection.
/// </summary>
public interface IEntityProvider
{
    /// <summary>
    /// Gets the entity type this provider is for.
    /// </summary>
    Type EntityType { get; }

    /// <summary>
    /// Gets all column metadata for the entity.
    /// Column names respect [Column] attribute if present.
    /// </summary>
    IReadOnlyList<ColumnMeta> Columns { get; }
}
