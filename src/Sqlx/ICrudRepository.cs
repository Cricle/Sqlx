// -----------------------------------------------------------------------
// <copyright file="ICrudRepository.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx
{
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
    }
}
