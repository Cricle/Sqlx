// <copyright file="DatabaseType.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests.E2E.Infrastructure;

/// <summary>
/// Supported database types for E2E testing.
/// </summary>
public enum DatabaseType
{
    /// <summary>
    /// MySQL database.
    /// </summary>
    MySQL,

    /// <summary>
    /// PostgreSQL database.
    /// </summary>
    PostgreSQL,

    /// <summary>
    /// SQL Server database.
    /// </summary>
    SqlServer,

    /// <summary>
    /// SQLite database.
    /// </summary>
    SQLite,

    /// <summary>
    /// Oracle database (optional).
    /// </summary>
    Oracle,

    /// <summary>
    /// IBM DB2 database (optional).
    /// </summary>
    DB2,
}
