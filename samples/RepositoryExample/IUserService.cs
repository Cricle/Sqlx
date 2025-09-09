// -----------------------------------------------------------------------
// <copyright file="IUserService.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.RepositoryExample;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Sqlx.Annotations;

/// <summary>
/// User service interface that defines business operations for user management.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets all users from the database.
    /// </summary>
    /// <returns>A list of all users.</returns>
    IList<User> GetAllUsers();
    
    /// <summary>
    /// Asynchronously gets all users from the database.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation containing a list of all users.</returns>
    Task<IList<User>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a user by their ID.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <returns>The user if found, otherwise null.</returns>
    User? GetUserById(int id);
    
    /// <summary>
    /// Asynchronously gets a user by their ID.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation containing the user if found, otherwise null.</returns>
    Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="user">The user to create.</param>
    /// <returns>The number of affected rows.</returns>
    int CreateUser(User user);
    
    /// <summary>
    /// Asynchronously creates a new user.
    /// </summary>
    /// <param name="user">The user to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation containing the number of affected rows.</returns>
    Task<int> CreateUserAsync(User user, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <param name="user">The user to update.</param>
    /// <returns>The number of affected rows.</returns>
    int UpdateUser(User user);
    
    /// <summary>
    /// Deletes a user by their ID.
    /// </summary>
    /// <param name="id">The user ID to delete.</param>
    /// <returns>The number of affected rows.</returns>
    int DeleteUser(int id);
}

