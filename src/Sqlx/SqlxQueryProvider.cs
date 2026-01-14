// -----------------------------------------------------------------------
// <copyright file="SqlxQueryProvider.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Sqlx
{
    /// <summary>
    /// IQueryProvider implementation for SQL generation.
    /// </summary>
    public class SqlxQueryProvider : IQueryProvider
    {
        private readonly SqlDialect _dialect;

        /// <summary>Creates a new SqlxQueryProvider with the specified dialect.</summary>
        public SqlxQueryProvider(SqlDialect dialect)
        {
            _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
        }

        /// <summary>Gets the SQL dialect.</summary>
        public SqlDialect Dialect => _dialect;

        /// <summary>Creates a new query with the specified expression.</summary>
        public IQueryable CreateQuery(Expression expression)
        {
            var elementType = GetElementType(expression.Type);
            var queryableType = typeof(SqlxQueryable<>).MakeGenericType(elementType);
            return (IQueryable)Activator.CreateInstance(queryableType, this, expression)!;
        }

        /// <summary>Creates a new typed query with the specified expression.</summary>
        public IQueryable<TElement> CreateQuery<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        TElement>(Expression expression)
        {
            return new SqlxQueryable<TElement>(this, expression);
        }

        /// <summary>Executes the query - not supported for SQL generation.</summary>
        public object? Execute(Expression expression)
        {
            throw new NotSupportedException("SqlxQueryProvider is for SQL generation only. Use ToSql() extension method.");
        }

        /// <summary>Executes the typed query - not supported for SQL generation.</summary>
        public TResult Execute<TResult>(Expression expression)
        {
            throw new NotSupportedException("SqlxQueryProvider is for SQL generation only. Use ToSql() extension method.");
        }

        /// <summary>Generates SQL from the expression tree.</summary>
        public string ToSql(Expression expression)
        {
            var visitor = new SqlExpressionVisitor(_dialect);
            return visitor.GenerateSql(expression);
        }

        /// <summary>Generates SQL with parameters from the expression tree.</summary>
        public (string Sql, Dictionary<string, object?> Parameters) ToSqlWithParameters(Expression expression)
        {
            var visitor = new SqlExpressionVisitor(_dialect, parameterized: true);
            var sql = visitor.GenerateSql(expression);
            return (sql, visitor.GetParameters());
        }

        private static Type GetElementType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>))
                return type.GetGenericArguments()[0];
            
            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IQueryable<>))
                    return iface.GetGenericArguments()[0];
            }
            
            return type;
        }
    }
}
