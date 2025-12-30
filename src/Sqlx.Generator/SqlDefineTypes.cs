// -----------------------------------------------------------------------
// <copyright file="SqlDefineTypes.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Generator;

/// <summary>
/// SQL dialect types enumeration for code generation.
/// This is a local copy to avoid dependency on the target assembly.
/// </summary>
internal enum SqlDefineTypes
{
    /// <summary>
    /// MySQL database dialect type.
    /// </summary>
    MySql = 0,
    /// <summary>
    /// SQL Server database dialect type.
    /// </summary>
    SqlServer = 1,
    /// <summary>
    /// PostgreSQL database dialect type.
    /// </summary>
    PostgreSql = 2,
    /// <summary>
    /// Oracle database dialect type.
    /// </summary>
    Oracle = 3,
    /// <summary>
    /// DB2 database dialect type.
    /// </summary>
    DB2 = 4,
    /// <summary>
    /// SQLite database dialect type.
    /// </summary>
    SQLite = 5,
}

