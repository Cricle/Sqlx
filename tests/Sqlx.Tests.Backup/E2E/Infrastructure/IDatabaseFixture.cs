// <copyright file="IDatabaseFixture.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using System.Data.Common;

namespace Sqlx.Tests.E2E.Infrastructure;

/// <summary>
/// Represents an isolated test database fixture with random name.
/// </summary>
public interface IDatabaseFixture : IAsyncDisposable
{
    /// <summary>
    /// Gets the unique database name for this fixture.
    /// </summary>
    string DatabaseName { get; }

    /// <summary>
    /// Gets the database connection.
    /// </summary>
    DbConnection Connection { get; }

    /// <summary>
    /// Gets the database type.
    /// </summary>
    DatabaseType DatabaseType { get; }

    /// <summary>
    /// Creates schema in the database using the provided DDL.
    /// </summary>
    /// <param name="schemaDefinition">The DDL statements to execute.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CreateSchemaAsync(string schemaDefinition);

    /// <summary>
    /// Inserts test data into the database.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="data">The data to insert.</param>
    /// <returns>The number of rows inserted.</returns>
    Task<int> InsertTestDataAsync<T>(IEnumerable<T> data);

    /// <summary>
    /// Cleans up the test database.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CleanupAsync();

    /// <summary>
    /// Creates a new connection to the same database.
    /// </summary>
    /// <returns>A new database connection.</returns>
    Task<DbConnection> CreateNewConnectionAsync();
}
