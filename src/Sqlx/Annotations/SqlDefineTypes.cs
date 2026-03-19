// -----------------------------------------------------------------------
// <copyright file="SqlDefineTypes.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#nullable enable

namespace Sqlx.Annotations
{
    /// <summary>
    /// SQL dialect types enumeration.
    /// </summary>
    public enum SqlDefineTypes
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
}

