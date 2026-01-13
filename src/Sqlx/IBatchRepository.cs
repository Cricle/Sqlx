// -----------------------------------------------------------------------
// <copyright file="IBatchRepository.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Sqlx
{
    /// <summary>
    /// Batch repository interface for bulk operations.
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TKey">Primary key type</typeparam>
    public interface IBatchRepository<TEntity, TKey>
        where TEntity : class
    {
        /// <summary>Batch inserts multiple entities.</summary>
        [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES {{batch_values --exclude Id}}")]
        [BatchOperation(MaxBatchSize = 1000)]
        Task<int> BatchInsertAsync(List<TEntity> entities, CancellationToken cancellationToken = default);

        /// <summary>Batch deletes entities by primary keys.</summary>
        [SqlTemplate("DELETE FROM {{table}} WHERE id IN {{values --param ids}}")]
        [BatchOperation(MaxBatchSize = 1000)]
        Task<int> BatchDeleteAsync(List<TKey> ids, CancellationToken cancellationToken = default);
    }
}
