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
    [Sqlx("SELECT * FROM users")]
    IList<User> GetAllUsers();
    
    /// <summary>
    /// Asynchronously gets all users from the database.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation containing a list of all users.</returns>
    [Sqlx("SELECT * FROM users")]
    Task<IList<User>> GetAllUsersAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a user by their ID.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <returns>The user if found, otherwise null.</returns>
    [Sqlx("SELECT * FROM users WHERE Id = @id")]
    User? GetUserById(int id);
    
    /// <summary>
    /// Asynchronously gets a user by their ID.
    /// </summary>
    /// <param name="id">The user ID.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation containing the user if found, otherwise null.</returns>
    [Sqlx("SELECT * FROM users WHERE Id = @id")]
    Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new user.
    /// </summary>
    /// <param name="user">The user to create.</param>
    /// <returns>The number of affected rows.</returns>
    [SqlExecuteType(SqlExecuteTypes.Insert, "users")]
    int CreateUser(User user);
    
    /// <summary>
    /// Asynchronously creates a new user.
    /// </summary>
    /// <param name="user">The user to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation containing the number of affected rows.</returns>
    [SqlExecuteType(SqlExecuteTypes.Insert, "users")]
    Task<int> CreateUserAsync(User user, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates an existing user.
    /// </summary>
    /// <param name="user">The user to update.</param>
    /// <returns>The number of affected rows.</returns>
    [SqlExecuteType(SqlExecuteTypes.Update, "users")]
    int UpdateUser(User user);
    
    /// <summary>
    /// Deletes a user by their ID.
    /// </summary>
    /// <param name="id">The user ID to delete.</param>
    /// <returns>The number of affected rows.</returns>
    [SqlExecuteType(SqlExecuteTypes.Delete, "users")]
    int DeleteUser(int id);
}

