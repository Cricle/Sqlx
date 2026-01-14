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
        public static IQueryable<T> For<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>(SqlDialect dialect) => new SqlxQueryable<T>(new SqlxQueryProvider(dialect));

        public static IQueryable<T> ForSqlite<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>() => For<T>(SqlDefine.SQLite);

        public static IQueryable<T> ForSqlServer<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>() => For<T>(SqlDefine.SqlServer);

        public static IQueryable<T> ForMySql<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>() => For<T>(SqlDefine.MySql);

        public static IQueryable<T> ForPostgreSQL<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>() => For<T>(SqlDefine.PostgreSql);

        public static IQueryable<T> ForOracle<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>() => For<T>(SqlDefine.Oracle);

        public static IQueryable<T> ForDB2<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            T>() => For<T>(SqlDefine.DB2);
    }
}
