// -----------------------------------------------------------------------
// <copyright file="ICommandRepository.cs" company="Cricle">
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
    /// Command repository interface for modifying data (Insert, Update, Delete).
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TKey">Primary key type</typeparam>
    public interface ICommandRepository<TEntity, TKey>
        where TEntity : class
    {
        // ==================== Insert Operations ====================

        /// <summary>Inserts entity and returns generated primary key (async).</summary>
        [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
        [ReturnInsertedId]
        Task<TKey> InsertAndGetIdAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>Inserts entity and returns generated primary key (sync).</summary>
        [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
        [ReturnInsertedId]
        TKey InsertAndGetId(TEntity entity);

        /// <summary>Inserts entity and returns affected rows (async).</summary>
        [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
        Task<int> InsertAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>Inserts entity and returns affected rows (sync).</summary>
        [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
        int Insert(TEntity entity);

        /// <summary>Batch inserts multiple entities (async).</summary>
        [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
        Task<int> BatchInsertAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        /// <summary>Batch inserts multiple entities (sync).</summary>
        [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
        int BatchInsert(IEnumerable<TEntity> entities);

        // ==================== Update Operations ====================

        /// <summary>Updates entity by primary key (async).</summary>
        [SqlTemplate("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = {{arg --param id}}")]
        Task<int> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>Updates entity by primary key (sync).</summary>
        [SqlTemplate("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = {{arg --param id}}")]
        int Update(TEntity entity);

        /// <summary>Updates entities matching predicate (async).</summary>
        [SqlTemplate("UPDATE {{table}} SET {{set --exclude Id}} WHERE {{where --param predicate}}")]
        Task<int> UpdateWhereAsync(TEntity entity, [ExpressionToSql] Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>Updates entities matching predicate (sync).</summary>
        [SqlTemplate("UPDATE {{table}} SET {{set --exclude Id}} WHERE {{where --param predicate}}")]
        int UpdateWhere(TEntity entity, [ExpressionToSql] Expression<Func<TEntity, bool>> predicate);

        /// <summary>Batch updates multiple entities (async).</summary>
        [SqlTemplate("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = {{arg --param id}}")]
        Task<int> BatchUpdateAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

        /// <summary>Batch updates multiple entities (sync).</summary>
        [SqlTemplate("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = {{arg --param id}}")]
        int BatchUpdate(IEnumerable<TEntity> entities);

        // ==================== Delete Operations ====================

        /// <summary>Deletes entity by primary key (async).</summary>
        [SqlTemplate("DELETE FROM {{table}} WHERE id = {{arg --param id}}")]
        Task<int> DeleteAsync(TKey id, CancellationToken cancellationToken = default);

        /// <summary>Deletes entity by primary key (sync).</summary>
        [SqlTemplate("DELETE FROM {{table}} WHERE id = {{arg --param id}}")]
        int Delete(TKey id);

        /// <summary>Deletes multiple entities by IDs (async).</summary>
        [SqlTemplate("DELETE FROM {{table}} WHERE id IN ({{values --param ids}})")]
        Task<int> DeleteByIdsAsync(List<TKey> ids, CancellationToken cancellationToken = default);

        /// <summary>Deletes multiple entities by IDs (sync).</summary>
        [SqlTemplate("DELETE FROM {{table}} WHERE id IN ({{values --param ids}})")]
        int DeleteByIds(List<TKey> ids);

        /// <summary>Deletes entities matching predicate (async).</summary>
        [SqlTemplate("DELETE FROM {{table}} WHERE {{where --param predicate}}")]
        Task<int> DeleteWhereAsync([ExpressionToSql] Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>Deletes entities matching predicate (sync).</summary>
        [SqlTemplate("DELETE FROM {{table}} WHERE {{where --param predicate}}")]
        int DeleteWhere([ExpressionToSql] Expression<Func<TEntity, bool>> predicate);

        /// <summary>Deletes all entities (async). Use with caution!</summary>
        [SqlTemplate("DELETE FROM {{table}}")]
        Task<int> DeleteAllAsync(CancellationToken cancellationToken = default);

        /// <summary>Deletes all entities (sync). Use with caution!</summary>
        [SqlTemplate("DELETE FROM {{table}}")]
        int DeleteAll();
    }
}
