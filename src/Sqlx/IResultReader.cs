// <copyright file="IResultReader.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Reads entities from DbDataReader without reflection.
/// Returns List for direct consumption without additional allocations.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public interface IResultReader<TEntity>
{
    /// <summary>
    /// Reads all entities from the reader into a List.
    /// Caller is responsible for reader lifecycle.
    /// </summary>
    /// <param name="reader">The data reader.</param>
    /// <returns>List of entities.</returns>
    List<TEntity> Read(DbDataReader reader);

    /// <summary>
    /// Reads all entities from the reader into a List asynchronously.
    /// Caller is responsible for reader lifecycle.
    /// </summary>
    /// <param name="reader">The data reader.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task containing list of entities.</returns>
    Task<List<TEntity>> ReadAsync(DbDataReader reader, CancellationToken cancellationToken = default);
}
