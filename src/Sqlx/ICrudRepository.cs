// -----------------------------------------------------------------------
// <copyright file="ICrudRepository.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx
{
    using System.Linq;

    /// <summary>
    /// Standard CRUD repository interface combining query and command operations.
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TKey">Primary key type</typeparam>
    public interface ICrudRepository<TEntity, TKey> :
        IQueryRepository<TEntity, TKey>,
        ICommandRepository<TEntity, TKey>
        where TEntity : class
    {
        /// <summary>
        /// Returns an IQueryable for building complex LINQ queries.
        /// </summary>
        /// <returns>IQueryable for the entity type.</returns>
        /// <example>
        /// <code>
        /// var query = repo.AsQueryable()
        ///     .Where(e => e.IsActive)
        ///     .OrderBy(e => e.Name)
        ///     .Take(10);
        /// var results = await query.ToListAsync();
        /// </code>
        /// </example>
        IQueryable<TEntity> AsQueryable();
    }
}
