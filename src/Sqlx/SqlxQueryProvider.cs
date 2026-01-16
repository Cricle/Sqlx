// -----------------------------------------------------------------------
// <copyright file="SqlxQueryProvider.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;

#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Sqlx
{
    /// <summary>
    /// IQueryProvider implementation for SQL generation (AOT-friendly).
    /// </summary>
    public class SqlxQueryProvider<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        T> : IQueryProvider
    {
        /// <summary>Initializes a new instance.</summary>
        public SqlxQueryProvider(SqlDialect dialect, IEntityProvider? entityProvider = null)
        {
            Dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
            EntityProvider = entityProvider;
        }

        /// <summary>Gets the SQL dialect.</summary>
        public SqlDialect Dialect { get; }
        internal DbConnection? Connection { get; set; }
        internal object? ResultReader { get; set; }
        internal IEntityProvider? EntityProvider { get; }

        /// <inheritdoc/>
        public IQueryable CreateQuery(Expression expression) => throw new NotSupportedException("Use CreateQuery<T>.");

        /// <inheritdoc/>
        public IQueryable<TElement> CreateQuery<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        TElement>(Expression expression)
        {
            if (typeof(TElement) == typeof(T))
                return new SqlxQueryable<TElement>(
                    (this as SqlxQueryProvider<TElement>)!, 
                    expression, 
                    Connection, 
                    ResultReader as IResultReader<TElement>);

            // Get cached reader or create dynamic reader for anonymous/projected types
            var reader = SqlQuery<TElement>.ResultReader;
            if (reader == null && DynamicReaderCache<TElement>.ShouldCreate)
            {
                reader = new DynamicResultReader<TElement>();
                SqlQuery<TElement>.ResultReader = reader;
            }

            var provider = new SqlxQueryProvider<TElement>(Dialect, EntityProvider)
            {
                Connection = Connection,
                ResultReader = reader
            };
            return new SqlxQueryable<TElement>(provider, expression);
        }

        private static class DynamicReaderCache<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        TElement>
        {
            public static readonly bool ShouldCreate = ComputeShouldCreate();

            private static bool ComputeShouldCreate()
            {
                var type = typeof(TElement);
                if (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) ||
                    type == typeof(DateTime) || type == typeof(Guid) || type == typeof(TimeSpan) ||
                    (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IGrouping<,>)))
                    return false;
                return true;
            }
        }

        /// <inheritdoc/>
        public object? Execute(Expression expression) => throw new NotSupportedException("Use Execute<TResult>.");

        /// <inheritdoc/>
        public TResult Execute<TResult>(Expression expression)
        {
            if (Connection == null)
                throw new InvalidOperationException("Connection is not set.");

            if (expression is not MethodCallExpression methodCall)
                throw new NotSupportedException($"Unsupported expression: {expression.NodeType}");

            return methodCall.Method.Name switch
            {
                "First" => ExecuteFirst<TResult>(methodCall, throwIfEmpty: true),
                "FirstOrDefault" => ExecuteFirst<TResult>(methodCall, throwIfEmpty: false),
                "Count" or "LongCount" => ExecuteCount<TResult>(methodCall),
                "Min" or "Max" or "Sum" or "Average" => ExecuteAggregate<TResult>(methodCall),
                _ => throw new NotSupportedException($"Method '{methodCall.Method.Name}' is not supported.")
            };
        }

        private TResult ExecuteFirst<TResult>(MethodCallExpression methodCall, bool throwIfEmpty)
        {
            if (ResultReader == null)
                throw new InvalidOperationException("ResultReader is not set.");

            var (sql, parameters) = ToSqlWithParameters(methodCall);
            var result = DbExecutor.ExecuteReader(Connection!, sql, parameters, (IResultReader<TResult>)ResultReader);
            return throwIfEmpty ? result.First() : result.FirstOrDefault()!;
        }

        private TResult ExecuteCount<TResult>(MethodCallExpression methodCall)
        {
            var (sql, parameters) = ToSqlWithParameters(methodCall.Arguments[0]);
            var countSql = $"SELECT COUNT(*) FROM ({sql}) AS q";
            var result = DbExecutor.ExecuteScalar(Connection!, countSql, parameters);
            return (TResult)Convert.ChangeType(result!, typeof(TResult));
        }

        private TResult ExecuteAggregate<TResult>(MethodCallExpression methodCall)
        {
            var (sql, parameters) = ToSqlWithParameters(methodCall.Arguments[0]);
            var column = ExtractColumnExpression(methodCall);
            var func = GetAggregateFunction(methodCall.Method.Name, column);
            var aggSql = $"SELECT {func} FROM ({sql}) AS q";
            var result = DbExecutor.ExecuteScalar(Connection!, aggSql, parameters);
            return (TResult)Convert.ChangeType(result!, typeof(TResult));
        }

        private string ExtractColumnExpression(MethodCallExpression methodCall)
        {
            if (methodCall.Arguments.Count <= 1)
                return "*";

            var arg = methodCall.Arguments[1];
            if (arg is UnaryExpression { NodeType: ExpressionType.Quote } unary)
                arg = unary.Operand;

            if (arg is LambdaExpression { Body: MemberExpression member })
            {
                var propName = member.Member.Name;
                var colName = EntityProvider?.Columns.FirstOrDefault(c => c.PropertyName == propName)?.Name ?? propName;
                return Dialect.WrapColumn(colName);
            }
            return "*";
        }

        private string GetAggregateFunction(string methodName, string column) => methodName switch
        {
            "Min" => Dialect.Min(column),
            "Max" => Dialect.Max(column),
            "Sum" => Dialect.Sum(column),
            "Average" => Dialect.Avg(column),
            _ => throw new NotSupportedException($"Aggregate '{methodName}' is not supported.")
        };

        /// <summary>Generates SQL from the expression.</summary>
        public string ToSql(Expression expression, bool parameterized = false) 
            => new SqlExpressionVisitor(Dialect, parameterized, EntityProvider).GenerateSql(expression);

        /// <summary>Generates parameterized SQL and parameters.</summary>
        public (string Sql, IEnumerable<KeyValuePair<string, object?>> Parameters) ToSqlWithParameters(Expression expression)
        {
            var visitor = new SqlExpressionVisitor(Dialect, parameterized: true, EntityProvider);
            return (visitor.GenerateSql(expression), visitor.GetParameters());
        }
    }
}
