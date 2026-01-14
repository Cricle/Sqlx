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
        /// <summary>Creates a new IQueryable for the specified entity type and dialect.</summary>
        public static IQueryable<T> For<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>(SqlDialect dialect)
        {
            var provider = new SqlxQueryProvider(dialect);
            return new SqlxQueryable<T>(provider);
        }

        /// <summary>Creates a new IQueryable for SQLite.</summary>
        public static IQueryable<T> ForSqlite<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>() => For<T>(SqlDefine.SQLite);

        /// <summary>Creates a new IQueryable for SQL Server.</summary>
        public static IQueryable<T> ForSqlServer<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>() => For<T>(SqlDefine.SqlServer);

        /// <summary>Creates a new IQueryable for MySQL.</summary>
        public static IQueryable<T> ForMySql<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>() => For<T>(SqlDefine.MySql);

        /// <summary>Creates a new IQueryable for PostgreSQL.</summary>
        public static IQueryable<T> ForPostgreSQL<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>() => For<T>(SqlDefine.PostgreSql);

        /// <summary>Creates a new IQueryable for Oracle.</summary>
        public static IQueryable<T> ForOracle<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>() => For<T>(SqlDefine.Oracle);

        /// <summary>Creates a new IQueryable for DB2.</summary>
        public static IQueryable<T> ForDB2<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T>() => For<T>(SqlDefine.DB2);
    }
}
