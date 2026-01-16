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
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        T>
    {
        private static IEntityProvider? _cachedEntityProvider;
        private static IResultReader<T>? _cachedResultReader;
        private static IParameterBinder<T>? _cachedParameterBinder;
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
        /// Gets or sets the cached result reader for type T.
        /// Used for Select projections and query results.
        /// </summary>
        public static IResultReader<T>? ResultReader
        {
            get => _cachedResultReader;
            set
            {
                lock (_lock)
                {
                    _cachedResultReader ??= value;  // Only set once
                }
            }
        }

        /// <summary>
        /// Gets or sets the cached parameter binder for type T.
        /// Used for binding entity properties to command parameters.
        /// </summary>
        public static IParameterBinder<T>? ParameterBinder
        {
            get => _cachedParameterBinder;
            set
            {
                lock (_lock)
                {
                    _cachedParameterBinder ??= value;  // Only set once
                }
            }
        }

        /// <summary>
        /// Creates a query for the specified SQL dialect.
        /// </summary>
        /// <param name="dialect">The SQL dialect.</param>
        /// <returns>An IQueryable for building SQL queries.</returns>
        public static IQueryable<T> For(SqlDialect dialect)
        {
            var provider = new SqlxQueryProvider<T>(dialect, EntityProvider)
            {
                ResultReader = ResultReader
            };
            return new SqlxQueryable<T>(provider);
        }

        /// <summary>
        /// Creates a query from a subquery (FROM subquery).
        /// </summary>
        /// <param name="dialect">The SQL dialect.</param>
        /// <param name="subQuery">The subquery to use as the FROM source.</param>
        /// <returns>An IQueryable for building SQL queries with subquery as source.</returns>
        public static IQueryable<T> For(SqlDialect dialect, IQueryable<T> subQuery)
        {
            var provider = new SqlxQueryProvider<T>(dialect, EntityProvider)
            {
                ResultReader = ResultReader
            };
            // Create a SqlxQueryable with subquery source marker
            return new SqlxQueryable<T>(provider, subQuery);
        }

        /// <summary>
        /// Creates a query for the specified SQL dialect.
        /// </summary>
        /// <param name="dialect">The SQL dialect.</param>
        /// <param name="entityProvider">Optional entity provider for metadata.</param>
        /// <returns>An IQueryable for building SQL queries.</returns>
        public static IQueryable<T> For(SqlDialect dialect, IEntityProvider? entityProvider)
        {
            var provider = new SqlxQueryProvider<T>(dialect, entityProvider ?? EntityProvider)
            {
                ResultReader = ResultReader
            };
            return new SqlxQueryable<T>(provider);
        }

        /// <summary>
        /// Creates a query for SQLite.
        /// </summary>
        /// <returns>An IQueryable for building SQLite queries.</returns>
        public static IQueryable<T> ForSqlite() => For(SqlDefine.SQLite);

        /// <summary>
        /// Creates a query for SQLite.
        /// </summary>
        /// <param name="entityProvider">Optional entity provider for metadata.</param>
        /// <returns>An IQueryable for building SQLite queries.</returns>
        public static IQueryable<T> ForSqlite(IEntityProvider? entityProvider) => For(SqlDefine.SQLite, entityProvider);

        /// <summary>
        /// Creates a query for SQL Server.
        /// </summary>
        /// <returns>An IQueryable for building SQL Server queries.</returns>
        public static IQueryable<T> ForSqlServer() => For(SqlDefine.SqlServer);

        /// <summary>
        /// Creates a query for SQL Server.
        /// </summary>
        /// <param name="entityProvider">Optional entity provider for metadata.</param>
        /// <returns>An IQueryable for building SQL Server queries.</returns>
        public static IQueryable<T> ForSqlServer(IEntityProvider? entityProvider) => For(SqlDefine.SqlServer, entityProvider);

        /// <summary>
        /// Creates a query for MySQL.
        /// </summary>
        /// <returns>An IQueryable for building MySQL queries.</returns>
        public static IQueryable<T> ForMySql() => For(SqlDefine.MySql);

        /// <summary>
        /// Creates a query for MySQL.
        /// </summary>
        /// <param name="entityProvider">Optional entity provider for metadata.</param>
        /// <returns>An IQueryable for building MySQL queries.</returns>
        public static IQueryable<T> ForMySql(IEntityProvider? entityProvider) => For(SqlDefine.MySql, entityProvider);

        /// <summary>
        /// Creates a query for PostgreSQL.
        /// </summary>
        /// <returns>An IQueryable for building PostgreSQL queries.</returns>
        public static IQueryable<T> ForPostgreSQL() => For(SqlDefine.PostgreSql);

        /// <summary>
        /// Creates a query for PostgreSQL.
        /// </summary>
        /// <param name="entityProvider">Optional entity provider for metadata.</param>
        /// <returns>An IQueryable for building PostgreSQL queries.</returns>
        public static IQueryable<T> ForPostgreSQL(IEntityProvider? entityProvider) => For(SqlDefine.PostgreSql, entityProvider);

        /// <summary>
        /// Creates a query for Oracle.
        /// </summary>
        /// <returns>An IQueryable for building Oracle queries.</returns>
        public static IQueryable<T> ForOracle() => For(SqlDefine.Oracle);

        /// <summary>
        /// Creates a query for Oracle.
        /// </summary>
        /// <param name="entityProvider">Optional entity provider for metadata.</param>
        /// <returns>An IQueryable for building Oracle queries.</returns>
        public static IQueryable<T> ForOracle(IEntityProvider? entityProvider) => For(SqlDefine.Oracle, entityProvider);

        /// <summary>
        /// Creates a query for DB2.
        /// </summary>
        /// <returns>An IQueryable for building DB2 queries.</returns>
        public static IQueryable<T> ForDB2() => For(SqlDefine.DB2);

        /// <summary>
        /// Creates a query for DB2.
        /// </summary>
        /// <param name="entityProvider">Optional entity provider for metadata.</param>
        /// <returns>An IQueryable for building DB2 queries.</returns>
        public static IQueryable<T> ForDB2(IEntityProvider? entityProvider) => For(SqlDefine.DB2, entityProvider);
    }
}
