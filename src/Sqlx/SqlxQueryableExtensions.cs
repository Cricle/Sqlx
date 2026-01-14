// -----------------------------------------------------------------------
// <copyright file="SqlxQueryableExtensions.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sqlx
{
    /// <summary>
    /// Extension methods for SqlxQueryable.
    /// </summary>
    public static class SqlxQueryableExtensions
    {
        /// <summary>Generates SQL from the query.</summary>
        public static string ToSql<T>(this IQueryable<T> query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            if (query.Provider is SqlxQueryProvider provider)
            {
                return provider.ToSql(query.Expression);
            }

            throw new InvalidOperationException("ToSql() can only be called on SqlxQueryable instances.");
        }

        /// <summary>Generates SQL with parameters from the query.</summary>
        public static (string Sql, Dictionary<string, object?> Parameters) ToSqlWithParameters<T>(this IQueryable<T> query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            if (query.Provider is SqlxQueryProvider provider)
            {
                return provider.ToSqlWithParameters(query.Expression);
            }

            throw new InvalidOperationException("ToSqlWithParameters() can only be called on SqlxQueryable instances.");
        }
    }
}
