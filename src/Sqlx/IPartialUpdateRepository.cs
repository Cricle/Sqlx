// -----------------------------------------------------------------------
// <copyright file="IPartialUpdateRepository.cs" company="Cricle">
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
    /// Repository interface for partial updates with compile-time type safety.
    /// Unlike ICommandRepository.UpdatePartialAsync&lt;TUpdates&gt; which uses method-level generics,
    /// this interface uses interface-level generics, allowing the source generator to analyze
    /// the TUpdates type at compile time and generate direct property access code without reflection.
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TKey">Primary key type</typeparam>
    /// <typeparam name="TUpdates">Type containing the properties to update (must be a concrete type, not anonymous)</typeparam>
    /// <remarks>
    /// <para>
    /// This interface is AOT-compatible because TUpdates is resolved at compile time when the
    /// repository class implements this interface.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// // Define your update type
    /// public record UserNameUpdate(string Name, DateTime UpdatedAt);
    /// 
    /// // Implement the interface with concrete types
    /// [RepositoryFor&lt;IPartialUpdateRepository&lt;User, int, UserNameUpdate&gt;&gt;]
    /// public partial class UserRepository
    /// {
    ///     private readonly DbConnection _connection;
    ///     public UserRepository(DbConnection connection) => _connection = connection;
    /// }
    /// 
    /// // Use it
    /// var repo = new UserRepository(connection);
    /// await repo.UpdatePartialAsync(userId, new UserNameUpdate("Alice", DateTime.Now));
    /// </code>
    /// </para>
    /// </remarks>
    public interface IPartialUpdateRepository<TEntity, TKey, TUpdates>
        where TEntity : class
        where TUpdates : class
    {
        /// <summary>Updates specific columns only (partial update) by primary key.</summary>
        /// <param name="id">Entity primary key</param>
        /// <param name="updates">Object with column-value pairs to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rows affected (0 = not found, 1 = success)</returns>
        /// <remarks>
        /// The source generator analyzes TUpdates properties at compile time and generates
        /// direct property access code (e.g., updates.Name) without reflection.
        /// Property names are automatically converted to snake_case column names.
        /// </remarks>
        [SqlTemplate("UPDATE {{table}} SET {{set --from updates}} WHERE id = @id")]
        Task<int> UpdatePartialAsync(TKey id, TUpdates updates, CancellationToken cancellationToken = default);

        /// <summary>Updates specific columns for entities matching expression predicate.</summary>
        /// <param name="predicate">Expression predicate for WHERE clause</param>
        /// <param name="updates">Object with column-value pairs to update</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rows affected</returns>
        [SqlTemplate("UPDATE {{table}} SET {{set --from updates}} {{where}}")]
        Task<int> UpdateWhereAsync([ExpressionToSql] Expression<Func<TEntity, bool>> predicate, TUpdates updates, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Repository interface for expression-based partial updates.
    /// Uses expression trees to specify which properties to update, providing full AOT compatibility.
    /// </summary>
    /// <typeparam name="TEntity">Entity type</typeparam>
    /// <typeparam name="TKey">Primary key type</typeparam>
    /// <remarks>
    /// <para>
    /// This interface provides an alternative to IPartialUpdateRepository that doesn't require
    /// defining a separate update type. Instead, you use expression trees to specify updates.
    /// </para>
    /// <para>
    /// Example usage:
    /// <code>
    /// // Implement the interface
    /// [RepositoryFor&lt;IExpressionUpdateRepository&lt;User, int&gt;&gt;]
    /// public partial class UserRepository
    /// {
    ///     private readonly DbConnection _connection;
    ///     public UserRepository(DbConnection connection) => _connection = connection;
    /// }
    /// 
    /// // Use expression-based updates
    /// var repo = new UserRepository(connection);
    /// await repo.UpdateFieldsAsync(userId, u => new User { Name = "Alice", UpdatedAt = DateTime.Now });
    /// </code>
    /// </para>
    /// </remarks>
    public interface IExpressionUpdateRepository<TEntity, TKey>
        where TEntity : class
    {
        /// <summary>Updates specific fields using an expression to specify the new values.</summary>
        /// <param name="id">Entity primary key</param>
        /// <param name="updateExpression">Expression specifying which fields to update and their new values</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rows affected (0 = not found, 1 = success)</returns>
        /// <remarks>
        /// The expression is analyzed at compile time to extract property assignments.
        /// Only properties that are explicitly set in the expression will be updated.
        /// </remarks>
        /// <example>
        /// await repo.UpdateFieldsAsync(userId, u => new User { Name = "Alice", Age = 30 });
        /// // Generated SQL: UPDATE users SET name = @p0, age = @p1 WHERE id = @id
        /// </example>
        [SqlTemplate("UPDATE {{table}} SET {{set --expr updateExpression}} WHERE id = @id")]
        Task<int> UpdateFieldsAsync(TKey id, [ExpressionToSql] Expression<Func<TEntity, TEntity>> updateExpression, CancellationToken cancellationToken = default);

        /// <summary>Updates specific fields for entities matching a predicate.</summary>
        /// <param name="predicate">Expression predicate for WHERE clause</param>
        /// <param name="updateExpression">Expression specifying which fields to update and their new values</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Rows affected</returns>
        [SqlTemplate("UPDATE {{table}} SET {{set --expr updateExpression}} {{where}}")]
        Task<int> UpdateFieldsWhereAsync(
            [ExpressionToSql] Expression<Func<TEntity, bool>> predicate,
            [ExpressionToSql] Expression<Func<TEntity, TEntity>> updateExpression,
            CancellationToken cancellationToken = default);
    }
}
