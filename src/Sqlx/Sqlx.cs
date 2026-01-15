// -----------------------------------------------------------------------
// <copyright file="Sqlx.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Sqlx
{
    /// <summary>
    /// Entry point for creating IQueryable SQL queries.
    /// </summary>
    public static class SqlQuery
    {
        /// <summary>
        /// Creates a query for the specified SQL dialect.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="dialect">The SQL dialect.</param>
        /// <returns>An IQueryable for building SQL queries.</returns>
        public static IQueryable<T> For<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(SqlDialect dialect)
        {
            return new SqlxQueryable<T>(new SqlxQueryProvider(dialect));
        }

        /// <summary>
        /// Creates a query for SQLite.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>An IQueryable for building SQLite queries.</returns>
        public static IQueryable<T> ForSqlite<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>()
        {
            return For<T>(SqlDefine.SQLite);
        }

        /// <summary>
        /// Creates a query for SQL Server.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>An IQueryable for building SQL Server queries.</returns>
        public static IQueryable<T> ForSqlServer<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>()
        {
            return For<T>(SqlDefine.SqlServer);
        }

        /// <summary>
        /// Creates a query for MySQL.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>An IQueryable for building MySQL queries.</returns>
        public static IQueryable<T> ForMySql<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>()
        {
            return For<T>(SqlDefine.MySql);
        }

        /// <summary>
        /// Creates a query for PostgreSQL.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>An IQueryable for building PostgreSQL queries.</returns>
        public static IQueryable<T> ForPostgreSQL<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>()
        {
            return For<T>(SqlDefine.PostgreSql);
        }

        /// <summary>
        /// Creates a query for Oracle.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>An IQueryable for building Oracle queries.</returns>
        public static IQueryable<T> ForOracle<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>()
        {
            return For<T>(SqlDefine.Oracle);
        }

        /// <summary>
        /// Creates a query for DB2.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <returns>An IQueryable for building DB2 queries.</returns>
        public static IQueryable<T> ForDB2<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>()
        {
            return For<T>(SqlDefine.DB2);
        }
    }
}
