// <copyright file="IResultReader.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx;

using System.Collections.Generic;
using System.Data.Common;
using System.Threading;

/// <summary>
/// Reads entities from DbDataReader without reflection.
/// Returns IEnumerable/IAsyncEnumerable for flexible consumption with LINQ.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public interface IResultReader<TEntity>
{
    /// <summary>
    /// Reads all entities from the reader as IEnumerable.
    /// Caller is responsible for reader lifecycle.
    /// Use LINQ methods like .ToList(), .FirstOrDefault() for further processing.
    /// </summary>
    /// <param name="reader">The data reader.</param>
    /// <returns>Enumerable of entities.</returns>
    IEnumerable<TEntity> Read(DbDataReader reader);

#if NET8_0_OR_GREATER
    /// <summary>
    /// Reads all entities from the reader as IAsyncEnumerable.
    /// Caller is responsible for reader lifecycle.
    /// Use LINQ methods like .ToListAsync(), .FirstOrDefaultAsync() for further processing.
    /// </summary>
    /// <param name="reader">The data reader.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Async enumerable of entities.</returns>
    IAsyncEnumerable<TEntity> ReadAsync(DbDataReader reader, CancellationToken cancellationToken = default);
#endif
}
