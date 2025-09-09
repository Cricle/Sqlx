// -----------------------------------------------------------------------
// <copyright file="IGenericRepository.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sqlx.RepositoryExample;

/// <summary>
/// Generic repository interface that demonstrates RepositoryFor with generic type parameters.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TKey">The primary key type.</typeparam>
public interface IGenericRepository<TEntity, TKey>
    where TEntity : class
    where TKey : struct
{
    /// <summary>
    /// Gets all entities.
    /// </summary>
    /// <returns>A list of all entities.</returns>
    IList<TEntity> GetAll();
    
    /// <summary>
    /// Asynchronously gets all entities.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation containing a list of all entities.</returns>
    Task<IList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets an entity by its ID.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    /// <returns>The entity if found, otherwise null.</returns>
    TEntity? GetById(TKey id);
    
    /// <summary>
    /// Asynchronously gets an entity by its ID.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation containing the entity if found, otherwise null.</returns>
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new entity.
    /// </summary>
    /// <param name="entity">The entity to create.</param>
    /// <returns>The number of affected rows.</returns>
    int Create(TEntity entity);
    
    /// <summary>
    /// Asynchronously creates a new entity.
    /// </summary>
    /// <param name="entity">The entity to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation containing the number of affected rows.</returns>
    Task<int> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <returns>The number of affected rows.</returns>
    int Update(TEntity entity);
    
    /// <summary>
    /// Deletes an entity by its ID.
    /// </summary>
    /// <param name="id">The entity ID to delete.</param>
    /// <returns>The number of affected rows.</returns>
    int Delete(TKey id);
}

/// <summary>
/// Simplified generic repository interface with single type parameter.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Gets all entities.
    /// </summary>
    /// <returns>A list of all entities.</returns>
    IList<T> GetAll();
    
    /// <summary>
    /// Gets an entity by its ID.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    /// <returns>The entity if found, otherwise null.</returns>
    T? GetById(int id);
    
    /// <summary>
    /// Creates a new entity.
    /// </summary>
    /// <param name="entity">The entity to create.</param>
    /// <returns>The number of affected rows.</returns>
    int Create(T entity);
    
    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <returns>The number of affected rows.</returns>
    int Update(T entity);
    
    /// <summary>
    /// Deletes an entity by its ID.
    /// </summary>
    /// <param name="id">The entity ID to delete.</param>
    /// <returns>The number of affected rows.</returns>
    int Delete(int id);
}
