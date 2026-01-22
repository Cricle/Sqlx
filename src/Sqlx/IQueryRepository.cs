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
        // ==================== Single Entity Queries ====================
        
        /// <summary>Gets entity by primary key (async).</summary>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = {{arg --param id}}")]
        Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

        /// <summary>Gets entity by primary key (sync).</summary>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = {{arg --param id}}")]
        TEntity? GetById(TKey id);

        /// <summary>Gets first entity matching predicate (async).</summary>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}} {{limit --count 1}}")]
        Task<TEntity?> GetFirstWhereAsync([ExpressionToSql] Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>Gets first entity matching predicate (sync).</summary>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}} {{limit --count 1}}")]
        TEntity? GetFirstWhere([ExpressionToSql] Expression<Func<TEntity, bool>> predicate);

        // ==================== List Queries ====================

        /// <summary>Gets multiple entities by IDs (async).</summary>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id IN ({{values --param ids}})")]
        Task<List<TEntity>> GetByIdsAsync(List<TKey> ids, CancellationToken cancellationToken = default);

        /// <summary>Gets multiple entities by IDs (sync).</summary>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id IN ({{values --param ids}})")]
        List<TEntity> GetByIds(List<TKey> ids);

        /// <summary>Gets all entities with limit (async).</summary>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} {{limit --param limit}}")]
        Task<List<TEntity>> GetAllAsync(int limit = 1000, CancellationToken cancellationToken = default);

        /// <summary>Gets all entities with limit (sync).</summary>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} {{limit --param limit}}")]
        List<TEntity> GetAll(int limit = 1000);

        /// <summary>Gets entities matching predicate (async).</summary>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}} {{limit --param limit}}")]
        Task<List<TEntity>> GetWhereAsync([ExpressionToSql] Expression<Func<TEntity, bool>> predicate, int limit = 1000, CancellationToken cancellationToken = default);

        /// <summary>Gets entities matching predicate (sync).</summary>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}} {{limit --param limit}}")]
        List<TEntity> GetWhere([ExpressionToSql] Expression<Func<TEntity, bool>> predicate, int limit = 1000);

        // ==================== Pagination ====================

        /// <summary>Gets entities with pagination (async).</summary>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} {{limit --param pageSize}} {{offset --param offset}}")]
        Task<List<TEntity>> GetPagedAsync(int pageSize = 20, int offset = 0, CancellationToken cancellationToken = default);

        /// <summary>Gets entities with pagination (sync).</summary>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} {{limit --param pageSize}} {{offset --param offset}}")]
        List<TEntity> GetPaged(int pageSize = 20, int offset = 0);

        /// <summary>Gets entities with pagination and predicate (async).</summary>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}} {{limit --param pageSize}} {{offset --param offset}}")]
        Task<List<TEntity>> GetPagedWhereAsync([ExpressionToSql] Expression<Func<TEntity, bool>> predicate, int pageSize = 20, int offset = 0, CancellationToken cancellationToken = default);

        /// <summary>Gets entities with pagination and predicate (sync).</summary>
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}} {{limit --param pageSize}} {{offset --param offset}}")]
        List<TEntity> GetPagedWhere([ExpressionToSql] Expression<Func<TEntity, bool>> predicate, int pageSize = 20, int offset = 0);

        // ==================== Existence & Count ====================

        /// <summary>Checks if entity exists by ID (async).</summary>
        [SqlTemplate("SELECT CASE WHEN EXISTS(SELECT 1 FROM {{table}} WHERE id = {{arg --param id}}) THEN 1 ELSE 0 END")]
        Task<bool> ExistsByIdAsync(TKey id, CancellationToken cancellationToken = default);

        /// <summary>Checks if entity exists by ID (sync).</summary>
        [SqlTemplate("SELECT CASE WHEN EXISTS(SELECT 1 FROM {{table}} WHERE id = {{arg --param id}}) THEN 1 ELSE 0 END")]
        bool ExistsById(TKey id);

        /// <summary>Checks if any entity exists matching predicate (async).</summary>
        [SqlTemplate("SELECT CASE WHEN EXISTS(SELECT 1 FROM {{table}} WHERE {{where --param predicate}}) THEN 1 ELSE 0 END")]
        Task<bool> ExistsAsync([ExpressionToSql] Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>Checks if any entity exists matching predicate (sync).</summary>
        [SqlTemplate("SELECT CASE WHEN EXISTS(SELECT 1 FROM {{table}} WHERE {{where --param predicate}}) THEN 1 ELSE 0 END")]
        bool Exists([ExpressionToSql] Expression<Func<TEntity, bool>> predicate);

        /// <summary>Gets total count of all entities (async).</summary>
        [SqlTemplate("SELECT COUNT(*) FROM {{table}}")]
        Task<long> CountAsync(CancellationToken cancellationToken = default);

        /// <summary>Gets total count of all entities (sync).</summary>
        [SqlTemplate("SELECT COUNT(*) FROM {{table}}")]
        long Count();

        /// <summary>Gets count of entities matching predicate (async).</summary>
        [SqlTemplate("SELECT COUNT(*) FROM {{table}} WHERE {{where --param predicate}}")]
        Task<long> CountWhereAsync([ExpressionToSql] Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>Gets count of entities matching predicate (sync).</summary>
        [SqlTemplate("SELECT COUNT(*) FROM {{table}} WHERE {{where --param predicate}}")]
        long CountWhere([ExpressionToSql] Expression<Func<TEntity, bool>> predicate);
    }
}
