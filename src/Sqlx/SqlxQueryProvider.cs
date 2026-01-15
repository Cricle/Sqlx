// -----------------------------------------------------------------------
// <copyright file="SqlxQueryProvider.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Sqlx
{
    /// <summary>
    /// IQueryProvider implementation for SQL generation (AOT-friendly, no reflection).
    /// </summary>
    public class SqlxQueryProvider : IQueryProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlxQueryProvider"/> class.
        /// </summary>
        /// <param name="dialect">The SQL dialect.</param>
        public SqlxQueryProvider(SqlDialect dialect)
        {
            Dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
        }

        /// <summary>
        /// Gets the SQL dialect.
        /// </summary>
        public SqlDialect Dialect { get; }

        /// <summary>
        /// Gets or sets the database connection for query execution.
        /// </summary>
        internal DbConnection? Connection { get; set; }

        /// <summary>
        /// Gets or sets the result reader (stored as object for non-generic provider).
        /// </summary>
        internal object? ResultReader { get; set; }

        /// <inheritdoc/>
        public IQueryable CreateQuery(Expression expression)
        {
            throw new NotSupportedException("Use CreateQuery<T> for AOT compatibility.");
        }

        /// <inheritdoc/>
        public IQueryable<TElement> CreateQuery<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
            TElement>(Expression expression)
        {
            var reader = ResultReader as IResultReader<TElement>;
            return new SqlxQueryable<TElement>(this, expression, Connection, reader);
        }

        /// <inheritdoc/>
        public object? Execute(Expression expression) => throw new NotSupportedException("Use Execute<TResult> for AOT compatibility.");

        /// <inheritdoc/>
        public TResult Execute<TResult>(Expression expression)
        {
            // This method is called by LINQ methods like First(), Single(), Count(), etc.
            if (Connection == null)
            {
                throw new InvalidOperationException("Connection is not set. Use WithConnection() before executing queries.");
            }

            if (ResultReader == null)
            {
                throw new InvalidOperationException("ResultReader is not set. Use WithReader() before executing queries.");
            }

            // Check if this is a method call expression
            if (expression is MethodCallExpression methodCall)
            {
                var methodName = methodCall.Method.Name;
                
                // Handle First/FirstOrDefault
                if (methodName == "First" || methodName == "FirstOrDefault")
                {
                    return ExecuteFirst<TResult>(expression, methodName == "FirstOrDefault");
                }
                
                // Handle Count/LongCount
                if (methodName == "Count" || methodName == "LongCount")
                {
                    return ExecuteCount<TResult>(expression);
                }
                
                // Handle ToList
                if (methodName == "ToList")
                {
                    return ExecuteToList<TResult>(expression);
                }
            }

            throw new NotSupportedException(
                $"Execute is not supported for expression type '{expression.NodeType}'. Supported methods: First, FirstOrDefault, Count, LongCount, ToList.");
        }

        private TResult ExecuteFirst<TResult>(Expression expression, bool orDefault)
        {
            // Generate SQL using visitor (which will add LIMIT 1 for First)
            var visitor = new SqlExpressionVisitor(Dialect, parameterized: true);
            var sql = visitor.GenerateSql(expression);
            var parameters = visitor.GetParameters();

            if (Connection!.State != ConnectionState.Open)
            {
                Connection.Open();
            }

            using var command = Connection.CreateCommand();
            command.CommandText = sql;
            
            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    var param = command.CreateParameter();
                    param.ParameterName = p.Key;
                    param.Value = p.Value ?? DBNull.Value;
                    command.Parameters.Add(param);
                }
            }
            
            using var reader = command.ExecuteReader();
            var resultReader = (IResultReader<TResult>)ResultReader!;
            var ordinals = resultReader.GetOrdinals(reader);
            
            if (reader.Read())
            {
                return resultReader.Read(reader, ordinals);
            }
            
            if (orDefault)
            {
                return default!;
            }
            
            throw new InvalidOperationException("Sequence contains no elements");
        }

        private TResult ExecuteCount<TResult>(Expression expression)
        {
            // Generate the base SQL (without Count wrapper)
            var visitor = new SqlExpressionVisitor(Dialect, parameterized: true);
            var baseSql = visitor.GenerateSql(expression);
            var parameters = visitor.GetParameters();
            
            // Wrap in COUNT query
            var sql = $"SELECT COUNT(*) FROM ({baseSql}) AS CountQuery";

            if (Connection!.State != ConnectionState.Open)
            {
                Connection.Open();
            }

            using var command = Connection.CreateCommand();
            command.CommandText = sql;
            
            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    var param = command.CreateParameter();
                    param.ParameterName = p.Key;
                    param.Value = p.Value ?? DBNull.Value;
                    command.Parameters.Add(param);
                }
            }
            
            var result = command.ExecuteScalar();
            return (TResult)Convert.ChangeType(result!, typeof(TResult));
        }

        private TResult ExecuteToList<TResult>(Expression expression)
        {
            // Generate SQL using visitor
            var visitor = new SqlExpressionVisitor(Dialect, parameterized: true);
            var sql = visitor.GenerateSql(expression);
            var parameters = visitor.GetParameters();
            
            // Get element type from List<T>
            var elementType = typeof(TResult).GetGenericArguments()[0];
            
            if (Connection!.State != ConnectionState.Open)
            {
                Connection.Open();
            }

            using var command = Connection.CreateCommand();
            command.CommandText = sql;
            
            if (parameters != null)
            {
                foreach (var p in parameters)
                {
                    var param = command.CreateParameter();
                    param.ParameterName = p.Key;
                    param.Value = p.Value ?? DBNull.Value;
                    command.Parameters.Add(param);
                }
            }
            
            using var reader = command.ExecuteReader();
            var list = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;
            
            // Get ordinals once
            var resultReaderType = ResultReader!.GetType();
            var getOrdinalsMethod = resultReaderType.GetMethod("GetOrdinals");
            var ordinals = (int[]?)getOrdinalsMethod!.Invoke(ResultReader, new object[] { reader });
            
            // Read all rows
            var readMethod = resultReaderType.GetMethod("Read", new[] { typeof(IDataRecord), typeof(int[]) });
            while (reader.Read())
            {
                var item = readMethod!.Invoke(ResultReader, new object?[] { reader, ordinals });
                list.Add(item);
            }
            
            return (TResult)list;
        }

        /// <summary>
        /// Generates SQL from the expression.
        /// </summary>
        /// <param name="expression">The expression tree.</param>
        /// <param name="parameterized">Whether to generate parameterized SQL.</param>
        /// <returns>The generated SQL string.</returns>
        public string ToSql(Expression expression, bool parameterized = false) => new SqlExpressionVisitor(Dialect, parameterized).GenerateSql(expression);

        /// <summary>
        /// Generates parameterized SQL and parameters from the expression.
        /// </summary>
        /// <param name="expression">The expression tree.</param>
        /// <returns>A tuple containing the SQL string and parameters.</returns>
        public (string Sql, IEnumerable<KeyValuePair<string, object?>> Parameters) ToSqlWithParameters(Expression expression)
        {
            var visitor = new SqlExpressionVisitor(Dialect, parameterized: true);
            return (visitor.GenerateSql(expression), visitor.GetParameters());
        }
    }
}
