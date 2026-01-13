// -----------------------------------------------------------------------
// <copyright file="IQueryRepository.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Sqlx
{
    /// <summary>
    /// Query repository interface for reading data.
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TKey">Primary key type</typeparam>
    public interface IQueryRepository<TEntity, TKey>
        where TEntity : class
    {
        /// <summary>Gets entity by primary key.</summary>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
        Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

        /// <summary>Gets multiple entities by IDs.</summary>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id IN {{values --param ids}}")]
        Task<List<TEntity>> GetByIdsAsync(List<TKey> ids, CancellationToken cancellationToken = default);

        /// <summary>Gets all entities with limit.</summary>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} {{limit --param limit}}")]
        Task<List<TEntity>> GetAllAsync(int limit = 1000, CancellationToken cancellationToken = default);

        /// <summary>Gets entities matching predicate.</summary>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}} {{limit --param limit}}")]
        Task<List<TEntity>> GetWhereAsync([ExpressionToSql] Expression<Func<TEntity, bool>> predicate, int limit = 1000, CancellationToken cancellationToken = default);

        /// <summary>Gets first entity matching predicate.</summary>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}} LIMIT 1")]
        Task<TEntity?> GetFirstWhereAsync([ExpressionToSql] Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>Gets entities with pagination.</summary>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} {{limit --param pageSize}} {{offset --param offset}}")]
        Task<List<TEntity>> GetPagedAsync(int pageSize = 20, int offset = 0, CancellationToken cancellationToken = default);

        /// <summary>Checks if any entity exists matching predicate.</summary>
        [SqlTemplate("SELECT CASE WHEN EXISTS(SELECT 1 FROM {{table}} WHERE {{where --param predicate}}) THEN 1 ELSE 0 END")]
        Task<bool> ExistsAsync([ExpressionToSql] Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>Gets total count of all entities.</summary>
        [SqlTemplate("SELECT COUNT(*) FROM {{table}}")]
        Task<long> CountAsync(CancellationToken cancellationToken = default);

        /// <summary>Gets count of entities matching predicate.</summary>
        [SqlTemplate("SELECT COUNT(*) FROM {{table}} WHERE {{where --param predicate}}")]
        Task<long> CountWhereAsync([ExpressionToSql] Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    }
}
