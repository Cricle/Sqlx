// -----------------------------------------------------------------------
// <copyright file="Sqlx.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;
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
        /// <summary>Creates a new IQueryable for the specified entity type and dialect.</summary>
        public static SqlxQueryable<T> For<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>(SqlDialect dialect)
        {
            var provider = new SqlxQueryProvider(dialect);
            return new SqlxQueryable<T>(provider);
        }

        /// <summary>Creates a new IQueryable for the specified entity type, dialect and connection.</summary>
        public static SqlxQueryable<T> For<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>(SqlDialect dialect, DbConnection connection)
        {
            var provider = new SqlxQueryProvider(dialect, connection);
            return new SqlxQueryable<T>(provider) { Connection = connection };
        }

        /// <summary>Creates a new IQueryable for SQLite.</summary>
        public static SqlxQueryable<T> ForSqlite<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>() => For<T>(SqlDefine.SQLite);

        /// <summary>Creates a new IQueryable for SQLite with connection.</summary>
        public static SqlxQueryable<T> ForSqlite<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>(DbConnection connection) => For<T>(SqlDefine.SQLite, connection);

        /// <summary>Creates a new IQueryable for SQL Server.</summary>
        public static SqlxQueryable<T> ForSqlServer<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>() => For<T>(SqlDefine.SqlServer);

        /// <summary>Creates a new IQueryable for SQL Server with connection.</summary>
        public static SqlxQueryable<T> ForSqlServer<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>(DbConnection connection) => For<T>(SqlDefine.SqlServer, connection);

        /// <summary>Creates a new IQueryable for MySQL.</summary>
        public static SqlxQueryable<T> ForMySql<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>() => For<T>(SqlDefine.MySql);

        /// <summary>Creates a new IQueryable for MySQL with connection.</summary>
        public static SqlxQueryable<T> ForMySql<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>(DbConnection connection) => For<T>(SqlDefine.MySql, connection);

        /// <summary>Creates a new IQueryable for PostgreSQL.</summary>
        public static SqlxQueryable<T> ForPostgreSQL<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>() => For<T>(SqlDefine.PostgreSql);

        /// <summary>Creates a new IQueryable for PostgreSQL with connection.</summary>
        public static SqlxQueryable<T> ForPostgreSQL<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>(DbConnection connection) => For<T>(SqlDefine.PostgreSql, connection);

        /// <summary>Creates a new IQueryable for Oracle.</summary>
        public static SqlxQueryable<T> ForOracle<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>() => For<T>(SqlDefine.Oracle);

        /// <summary>Creates a new IQueryable for Oracle with connection.</summary>
        public static SqlxQueryable<T> ForOracle<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>(DbConnection connection) => For<T>(SqlDefine.Oracle, connection);

        /// <summary>Creates a new IQueryable for DB2.</summary>
        public static SqlxQueryable<T> ForDB2<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>() => For<T>(SqlDefine.DB2);

        /// <summary>Creates a new IQueryable for DB2 with connection.</summary>
        public static SqlxQueryable<T> ForDB2<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>(DbConnection connection) => For<T>(SqlDefine.DB2, connection);
    }
}
