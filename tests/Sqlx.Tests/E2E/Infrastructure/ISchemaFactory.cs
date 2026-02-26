// <copyright file="ISchemaFactory.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests.E2E.Infrastructure;

/// <summary>
/// Factory for generating dialect-specific DDL statements.
/// </summary>
public interface ISchemaFactory
{
    /// <summary>
    /// Translates a common schema definition to dialect-specific DDL.
    /// </summary>
    /// <param name="commonSchema">The common schema definition.</param>
    /// <param name="dbType">The target database type.</param>
    /// <returns>The dialect-specific DDL statements.</returns>
    string TranslateSchema(string commonSchema, DatabaseType dbType);

    /// <summary>
    /// Gets the CREATE DATABASE SQL for the specified database type.
    /// </summary>
    /// <param name="dbName">The database name.</param>
    /// <param name="dbType">The database type.</param>
    /// <returns>The CREATE DATABASE SQL statement.</returns>
    string GetCreateDatabaseSql(string dbName, DatabaseType dbType);

    /// <summary>
    /// Gets the DROP DATABASE SQL for the specified database type.
    /// </summary>
    /// <param name="dbName">The database name.</param>
    /// <param name="dbType">The database type.</param>
    /// <returns>The DROP DATABASE SQL statement.</returns>
    string GetDropDatabaseSql(string dbName, DatabaseType dbType);
}
