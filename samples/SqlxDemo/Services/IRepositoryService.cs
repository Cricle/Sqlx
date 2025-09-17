// -----------------------------------------------------------------------
// <copyright file="IRepositoryService.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using SqlxDemo.Models;
using Sqlx.Annotations;

namespace SqlxDemo.Services;

/// <summary>
/// Repository service interface for demonstration of RepositoryFor attribute.
/// </summary>
public interface IRepositoryService
{
    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    [Sqlx("SELECT * FROM Users WHERE Id = @id")]
    Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active users.
    /// </summary>
    [Sqlx("SELECT * FROM Users WHERE IsActive = 1")]
    Task<List<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new user.
    /// </summary>
    [SqlExecuteType(SqlOperation.Insert, "Users")]
    Task<int> CreateUserAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing user.
    /// </summary>
    [SqlExecuteType(SqlOperation.Update, "Users")]
    Task<int> UpdateUserAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user by ID.
    /// </summary>
    [SqlExecuteType(SqlOperation.Delete, "Users")]
    Task<int> DeleteUserAsync(int id, CancellationToken cancellationToken = default);
}

