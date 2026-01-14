// -----------------------------------------------------------------------
// <copyright file="ICommandRepository.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
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
        /// <summary>Inserts entity and returns generated primary key.</summary>
        [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
        [ReturnInsertedId]
        Task<TKey> InsertAndGetIdAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>Updates entity by primary key.</summary>
        [SqlTemplate("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = {{arg --param id}}")]
        Task<int> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

        /// <summary>Deletes entity by primary key.</summary>
        [SqlTemplate("DELETE FROM {{table}} WHERE id = {{arg --param id}}")]
        Task<int> DeleteAsync(TKey id, CancellationToken cancellationToken = default);

        /// <summary>Deletes entities matching predicate.</summary>
        [SqlTemplate("DELETE FROM {{table}} WHERE {{where --param predicate}}")]
        Task<int> DeleteWhereAsync([ExpressionToSql] Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
    }
}
