// -----------------------------------------------------------------------
// <copyright file="IUserServiceWithTransaction.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.RepositoryExample;

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Extended user service interface with transaction support.
/// This demonstrates advanced repository pattern capabilities.
/// </summary>
public interface IUserServiceWithTransaction : IUserService
{
    /// <summary>
    /// Creates multiple users in a single transaction.
    /// </summary>
    /// <param name="users">The users to create.</param>
    /// <returns>The number of affected rows.</returns>
    int CreateUsersBatch(IEnumerable<User> users);
    
    /// <summary>
    /// Asynchronously creates multiple users in a single transaction.
    /// </summary>
    /// <param name="users">The users to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation containing the number of affected rows.</returns>
    Task<int> CreateUsersBatchAsync(IEnumerable<User> users, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Transfers users between different states with transaction safety.
    /// </summary>
    /// <param name="sourceUserId">The source user ID.</param>
    /// <param name="targetUserId">The target user ID.</param>
    /// <param name="transferData">Additional transfer data.</param>
    /// <returns>True if transfer was successful.</returns>
    bool TransferUserData(int sourceUserId, int targetUserId, string transferData);
    
    /// <summary>
    /// Asynchronously transfers users between different states with transaction safety.
    /// </summary>
    /// <param name="sourceUserId">The source user ID.</param>
    /// <param name="targetUserId">The target user ID.</param>
    /// <param name="transferData">Additional transfer data.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation containing true if transfer was successful.</returns>
    Task<bool> TransferUserDataAsync(int sourceUserId, int targetUserId, string transferData, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes a complex operation with custom transaction management.
    /// </summary>
    /// <param name="transaction">The database transaction to use.</param>
    /// <param name="userId">The user ID to operate on.</param>
    /// <param name="operationType">The type of operation to perform.</param>
    /// <returns>The result of the operation.</returns>
    int ExecuteWithTransaction(DbTransaction transaction, int userId, string operationType);
    
    /// <summary>
    /// Gets users with pagination support.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <returns>A paginated list of users.</returns>
    (IList<User> Users, int TotalCount) GetUsersPaginated(int pageNumber, int pageSize);
    
    /// <summary>
    /// Asynchronously gets users with pagination support.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation containing a paginated list of users.</returns>
    Task<(IList<User> Users, int TotalCount)> GetUsersPaginatedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Searches users by criteria with full-text search capabilities.
    /// </summary>
    /// <param name="searchTerm">The search term.</param>
    /// <param name="searchType">The type of search to perform.</param>
    /// <returns>A list of matching users.</returns>
    IList<User> SearchUsers(string searchTerm, UserSearchType searchType);
    
    /// <summary>
    /// Gets user statistics and analytics.
    /// </summary>
    /// <returns>User statistics data.</returns>
    UserStatistics GetUserStatistics();
}
