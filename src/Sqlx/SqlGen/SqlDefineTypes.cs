// -----------------------------------------------------------------------
// <copyright file="SqlDefineTypes.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.SqlGen;

/// <summary>
/// Defines database dialect types for SQL generation.
/// </summary>
public enum SqlDefineTypes
{
    /// <summary>MySQL dialect with backtick column wrapping.</summary>
    MySql = 0,
    /// <summary>SQL Server dialect with square bracket column wrapping.</summary>
    SqlServer = 1,
    /// <summary>PostgreSQL dialect with double quote column wrapping.</summary>
    Postgresql = 2,
    /// <summary>Oracle dialect with double quote column wrapping and colon parameters.</summary>
    Oracle = 3,
    /// <summary>DB2 dialect with double quote column wrapping and question mark parameters.</summary>
    DB2 = 4,
    /// <summary>SQLite dialect with square bracket column wrapping.</summary>
    SQLite = 5,
}

