// -----------------------------------------------------------------------
// <copyright file="Sqlx.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Sqlx
{
    /// <summary>
    /// Entry point for creating IQueryable SQL queries with cached entity metadata.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    public static class SqlQuery<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>
    {
        private static IEntityProvider? _cachedEntityProvider;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets or sets the cached entity provider for type T.
        /// </summary>
        public static IEntityProvider? EntityProvider
        {
            get => _cachedEntityProvider;
            set
            {
                lock (_lock)
                {
                    _cachedEntityProvider = value;
                }
            }
        }

        /// <summary>
        /// Gets the column metadata by property name.
        /// </summary>
        internal static ColumnMeta? GetColumnByProperty(string propertyName)
        {
            if (_cachedEntityProvider == null)
                return null;

            foreach (var column in _cachedEntityProvider.Columns)
            {
                if (column.PropertyName == propertyName)
                    return column;
            }
            return null;
        }

        /// <summary>
        /// Creates a query for the specified SQL dialect.
        /// </summary>
        /// <param name="dialect">The SQL dialect.</param>
        /// <param name="entityProvider">Optional entity provider for metadata.</param>
        /// <returns>An IQueryable for building SQL queries.</returns>
        public static IQueryable<T> For(SqlDialect dialect, IEntityProvider? entityProvider = null)
        {
            if (entityProvider != null)
            {
                EntityProvider = entityProvider;
            }
            return new SqlxQueryable<T>(new SqlxQueryProvider<T>(dialect));
        }

        /// <summary>
        /// Creates a query for SQLite.
        /// </summary>
        /// <param name="entityProvider">Optional entity provider for metadata.</param>
        /// <returns>An IQueryable for building SQLite queries.</returns>
        public static IQueryable<T> ForSqlite(IEntityProvider? entityProvider = null)
        {
            return For(SqlDefine.SQLite, entityProvider);
        }

        /// <summary>
        /// Creates a query for SQL Server.
        /// </summary>
        /// <param name="entityProvider">Optional entity provider for metadata.</param>
        /// <returns>An IQueryable for building SQL Server queries.</returns>
        public static IQueryable<T> ForSqlServer(IEntityProvider? entityProvider = null)
        {
            return For(SqlDefine.SqlServer, entityProvider);
        }

        /// <summary>
        /// Creates a query for MySQL.
        /// </summary>
        /// <param name="entityProvider">Optional entity provider for metadata.</param>
        /// <returns>An IQueryable for building MySQL queries.</returns>
        public static IQueryable<T> ForMySql(IEntityProvider? entityProvider = null)
        {
            return For(SqlDefine.MySql, entityProvider);
        }

        /// <summary>
        /// Creates a query for PostgreSQL.
        /// </summary>
        /// <param name="entityProvider">Optional entity provider for metadata.</param>
        /// <returns>An IQueryable for building PostgreSQL queries.</returns>
        public static IQueryable<T> ForPostgreSQL(IEntityProvider? entityProvider = null)
        {
            return For(SqlDefine.PostgreSql, entityProvider);
        }

        /// <summary>
        /// Creates a query for Oracle.
        /// </summary>
        /// <param name="entityProvider">Optional entity provider for metadata.</param>
        /// <returns>An IQueryable for building Oracle queries.</returns>
        public static IQueryable<T> ForOracle(IEntityProvider? entityProvider = null)
        {
            return For(SqlDefine.Oracle, entityProvider);
        }

        /// <summary>
        /// Creates a query for DB2.
        /// </summary>
        /// <param name="entityProvider">Optional entity provider for metadata.</param>
        /// <returns>An IQueryable for building DB2 queries.</returns>
        public static IQueryable<T> ForDB2(IEntityProvider? entityProvider = null)
        {
            return For(SqlDefine.DB2, entityProvider);
        }
    }

    /// <summary>
    /// Non-generic convenience class for SqlQuery&lt;T&gt; (for backward compatibility).
    /// </summary>
    public static class SqlQuery
    {
        /// <summary>
        /// Creates a query for the specified SQL dialect.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="dialect">The SQL dialect.</param>
        /// <param name="entityProvider">Optional entity provider for metadata.</param>
        /// <returns>An IQueryable for building SQL queries.</returns>
        public static IQueryable<T> For<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(SqlDialect dialect, IEntityProvider? entityProvider = null)
        {
            return SqlQuery<T>.For(dialect, entityProvider);
        }

        /// <summary>
        /// Creates a query for SQLite.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entityProvider">Optional entity provider for metadata.</param>
        /// <returns>An IQueryable for building SQLite queries.</returns>
        public static IQueryable<T> ForSqlite<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(IEntityProvider? entityProvider = null)
        {
            return SqlQuery<T>.ForSqlite(entityProvider);
        }

        /// <summary>
        /// Creates a query for SQL Server.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entityProvider">Optional entity provider for metadata.</param>
        /// <returns>An IQueryable for building SQL Server queries.</returns>
        public static IQueryable<T> ForSqlServer<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(IEntityProvider? entityProvider = null)
        {
            return SqlQuery<T>.ForSqlServer(entityProvider);
        }

        /// <summary>
        /// Creates a query for MySQL.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entityProvider">Optional entity provider for metadata.</param>
        /// <returns>An IQueryable for building MySQL queries.</returns>
        public static IQueryable<T> ForMySql<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(IEntityProvider? entityProvider = null)
        {
            return SqlQuery<T>.ForMySql(entityProvider);
        }

        /// <summary>
        /// Creates a query for PostgreSQL.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entityProvider">Optional entity provider for metadata.</param>
        /// <returns>An IQueryable for building PostgreSQL queries.</returns>
        public static IQueryable<T> ForPostgreSQL<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(IEntityProvider? entityProvider = null)
        {
            return SqlQuery<T>.ForPostgreSQL(entityProvider);
        }

        /// <summary>
        /// Creates a query for Oracle.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entityProvider">Optional entity provider for metadata.</param>
        /// <returns>An IQueryable for building Oracle queries.</returns>
        public static IQueryable<T> ForOracle<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(IEntityProvider? entityProvider = null)
        {
            return SqlQuery<T>.ForOracle(entityProvider);
        }

        /// <summary>
        /// Creates a query for DB2.
        /// </summary>
        /// <typeparam name="T">The entity type.</typeparam>
        /// <param name="entityProvider">Optional entity provider for metadata.</param>
        /// <returns>An IQueryable for building DB2 queries.</returns>
        public static IQueryable<T> ForDB2<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(IEntityProvider? entityProvider = null)
        {
            return SqlQuery<T>.ForDB2(entityProvider);
        }
    }
}
