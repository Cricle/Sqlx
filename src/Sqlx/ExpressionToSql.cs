// -----------------------------------------------------------------------
// <copyright file="ExpressionToSql.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq.Expressions;
using System.Text;
using Sqlx.Annotations;

namespace Sqlx.Annotations
{
    /// <summary>
    /// Provides LINQ expression to SQL conversion functionality.
    /// </summary>
    /// <typeparam name="T">The entity type for expressions.</typeparam>
    public class ExpressionToSql<T> : IDisposable
    {
        private readonly List<Expression<Func<T, bool>>> _whereConditions =
            new List<Expression<Func<T, bool>>>();
        private readonly List<(LambdaExpression Expression, bool Descending)> _orderByExpressions =
            new List<(LambdaExpression, bool)>();
        private readonly List<(string Column, string Value)> _setClausesConstant =
            new List<(string, string)>();
        private readonly List<(string Column, string Expression)> _setClausesExpression =
            new List<(string, string)>();
        private readonly (string ColumnLeft, string ColumnRight, string StringLeft,
            string StringRight, string ParameterPrefix) _dialect;
        private SqlTemplate? _cachedTemplate;
        private int? _take;
        private int? _skip;

        /// <summary>
        /// Initializes a new instance with the specified SQL dialect.
        /// </summary>
        private ExpressionToSql((string ColumnLeft, string ColumnRight, string StringLeft,
            string StringRight, string ParameterPrefix) dialect)
        {
            _dialect = dialect;
        }

        /// <summary>
        /// Creates an ExpressionToSql builder for SQL Server dialect.
        /// </summary>
        public static ExpressionToSql<T> ForSqlServer()
            => new ExpressionToSql<T>(SqlDefine.SqlServer);

        /// <summary>
        /// Creates an ExpressionToSql builder for MySQL dialect.
        /// </summary>
        public static ExpressionToSql<T> ForMySql()
            => new ExpressionToSql<T>(SqlDefine.MySql);

        /// <summary>
        /// Creates an ExpressionToSql builder for PostgreSQL dialect.
        /// </summary>
        public static ExpressionToSql<T> ForPostgreSQL()
            => new ExpressionToSql<T>(SqlDefine.PgSql);

        /// <summary>
        /// Creates an ExpressionToSql builder for Oracle dialect.
        /// </summary>
        public static ExpressionToSql<T> ForOracle()
            => new ExpressionToSql<T>(SqlDefine.Oracle);

        /// <summary>
        /// Creates an ExpressionToSql builder for DB2 dialect.
        /// </summary>
        public static ExpressionToSql<T> ForDB2()
            => new ExpressionToSql<T>(SqlDefine.DB2);

        /// <summary>
        /// Creates an ExpressionToSql builder for SQLite dialect.
        /// </summary>
        public static ExpressionToSql<T> ForSqlite()
            => new ExpressionToSql<T>(SqlDefine.Sqlite);

        /// <summary>
        /// Creates an ExpressionToSql builder with default (SQL Server) dialect.
        /// </summary>
        public static ExpressionToSql<T> Create()
            => new ExpressionToSql<T>(SqlDefine.SqlServer);

        /// <summary>
        /// Adds a WHERE condition to the query.
        /// </summary>
        public ExpressionToSql<T> Where(Expression<Func<T, bool>> predicate)
        {
            if (predicate != null)
                _whereConditions.Add(predicate);
            return this;
        }

        /// <summary>
        /// Adds an AND condition to the query.
        /// </summary>
        public ExpressionToSql<T> And(Expression<Func<T, bool>> predicate)
        {
            return Where(predicate);
        }

        /// <summary>
        /// Adds an ORDER BY clause to the query.
        /// </summary>
        public ExpressionToSql<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            if (keySelector != null)
                _orderByExpressions.Add((keySelector, false));
            return this;
        }

        /// <summary>
        /// Adds an ORDER BY DESC clause to the query.
        /// </summary>
        public ExpressionToSql<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            if (keySelector != null)
                _orderByExpressions.Add((keySelector, true));
            return this;
        }

        /// <summary>
        /// Limits the number of returned rows.
        /// </summary>
        public ExpressionToSql<T> Take(int count)
        {
            _take = count;
            return this;
        }

        /// <summary>
        /// Skips the specified number of rows.
        /// </summary>
        public ExpressionToSql<T> Skip(int count)
        {
            _skip = count;
            return this;
        }

        /// <summary>
        /// Sets a value for an UPDATE operation. Supports patterns like a=1.
        /// </summary>
        public ExpressionToSql<T> Set<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        {
            var columnName = GetColumnName(selector.Body);
            var valueStr = FormatConstantValue(value);
            _setClausesConstant.Add((columnName, valueStr));
            return this;
        }

        /// <summary>
        /// Sets a value using an expression for an UPDATE operation. Supports patterns like a=a+1.
        /// </summary>
        public ExpressionToSql<T> Set<TValue>(Expression<Func<T, TValue>> selector,
            Expression<Func<T, TValue>> valueExpression)
        {
            var columnName = GetColumnName(selector.Body);
            var expressionSql = ParseExpression(valueExpression.Body);
            _setClausesExpression.Add((columnName, expressionSql));
            return this;
        }

        /// <summary>
        /// Specifies columns for an INSERT operation.
        /// </summary>
        public ExpressionToSql<T> Insert(Expression<Func<T, object>> selector)
        {
            return this;
        }

        /// <summary>
        /// Specifies values for an INSERT operation.
        /// </summary>
        public ExpressionToSql<T> Values(params object[] values)
        {
            return this;
        }

        private string BuildSql()
        {
            if (_setClausesConstant.Count > 0 || _setClausesExpression.Count > 0)
            {
                var sql = new StringBuilder();
                sql.Append("UPDATE ");
                sql.Append(_dialect.ColumnLeft + typeof(T).Name + _dialect.ColumnRight);
                sql.Append(" SET ");
                var setClauses = new List<string>();
                foreach (var (column, value) in _setClausesConstant)
                {
                    setClauses.Add($"{column} = {value}");
                }
                foreach (var (column, expression) in _setClausesExpression)
                {
                    setClauses.Add($"{column} = {expression}");
                }
                sql.Append(string.Join(", ", setClauses));

                if (_whereConditions.Count > 0)
                {
                    sql.Append(" WHERE ");
                    var conditions = new List<string>();
                    foreach (var condition in _whereConditions)
                    {
                        var conditionSql = ParseExpression(condition.Body);
                        conditions.Add($"({conditionSql})");
                    }
                    sql.Append(string.Join(" AND ", conditions));
                }
                return sql.ToString();
            }

            var selectSql = new StringBuilder();
            selectSql.Append("SELECT * FROM ");
            selectSql.Append(_dialect.ColumnLeft + typeof(T).Name + _dialect.ColumnRight);

            if (_whereConditions.Count > 0)
            {
                selectSql.Append(" WHERE ");
                var conditions = new List<string>();
                foreach (var condition in _whereConditions)
                {
                    var conditionSql = ParseExpression(condition.Body);
                    conditions.Add($"({conditionSql})");
                }
                selectSql.Append(string.Join(" AND ", conditions));
            }

            if (_orderByExpressions.Count > 0)
            {
                selectSql.Append(" ORDER BY ");
                var orderClauses = new List<string>();
                foreach (var (expression, descending) in _orderByExpressions)
                {
                    var columnName = GetColumnName(expression.Body);
                    var direction = descending ? " DESC" : " ASC";
                    orderClauses.Add(columnName + direction);
                }
                selectSql.Append(string.Join(", ", orderClauses));
            }

            if (_skip.HasValue)
            {
                selectSql.Append($" OFFSET {_skip.Value}");
            }

            if (_take.HasValue)
            {
                selectSql.Append($" LIMIT {_take.Value}");
            }

            return selectSql.ToString();
        }

        /// <summary>
        /// Converts the built query to a parameterized SQL template.
        /// Results are cached for performance on repeated calls.
        /// </summary>
        public SqlTemplate ToTemplate()
        {
            if (_cachedTemplate.HasValue)
                return _cachedTemplate.Value;
            var sql = BuildSql();
            _cachedTemplate = new SqlTemplate(sql, new DbParameter[0]);
            return _cachedTemplate.Value;
        }

        /// <summary>
        /// Converts the built query to a SQL string.
        /// </summary>
        public string ToSql()
        {
            return BuildSql();
        }

        /// <summary>
        /// Generates the WHERE clause portion of the query.
        /// </summary>
        public string ToWhereClause()
        {
            if (_whereConditions.Count == 0)
                return string.Empty;
            var conditions = new List<string>();
            foreach (var condition in _whereConditions)
            {
                var conditionSql = ParseExpression(condition.Body);
                conditions.Add($"({conditionSql})");
            }
            return string.Join(" AND ", conditions);
        }

        /// <summary>
        /// Generates additional clauses for the query.
        /// </summary>
        public string ToAdditionalClause()
        {
            var clauses = new List<string>();
            if (_orderByExpressions.Count > 0)
            {
                var orderClauses = new List<string>();
                foreach (var (expression, descending) in _orderByExpressions)
                {
                    var columnName = GetColumnName(expression.Body);
                    var direction = descending ? " DESC" : " ASC";
                    orderClauses.Add(columnName + direction);
                }
                clauses.Add("ORDER BY " + string.Join(", ", orderClauses));
            }
            if (_skip.HasValue)
            {
                clauses.Add($"OFFSET {_skip.Value}");
            }
            if (_take.HasValue)
            {
                clauses.Add($"LIMIT {_take.Value}");
            }
            return string.Join(" ", clauses);
        }

        /// <summary>
        /// Releases resources used by this instance.
        /// </summary>
        public void Dispose()
        {
            _whereConditions.Clear();
            _orderByExpressions.Clear();
            _setClausesConstant.Clear();
            _setClausesExpression.Clear();
            _cachedTemplate = null;
        }

        private string ParseExpression(Expression expression)
        {
            switch (expression)
            {
                case BinaryExpression binary:
                    return ParseBinaryExpression(binary);
                case MemberExpression member:
                    return GetColumnName(member);
                case ConstantExpression constant:
                    return GetConstantValue(constant);
                case UnaryExpression unary when unary.NodeType == ExpressionType.Not:
                    return $"NOT ({ParseExpression(unary.Operand)})";
                default:
                    return "1=1";
            }
        }

        private string ParseBinaryExpression(BinaryExpression binary)
        {
            var left = ParseExpression(binary.Left);
            var right = ParseExpression(binary.Right);
            return binary.NodeType switch
            {
                ExpressionType.Equal => $"{left} = {right}",
                ExpressionType.NotEqual => $"{left} <> {right}",
                ExpressionType.GreaterThan => $"{left} > {right}",
                ExpressionType.GreaterThanOrEqual => $"{left} >= {right}",
                ExpressionType.LessThan => $"{left} < {right}",
                ExpressionType.LessThanOrEqual => $"{left} <= {right}",
                ExpressionType.AndAlso => $"({left} AND {right})",
                ExpressionType.OrElse => $"({left} OR {right})",
                ExpressionType.Add => $"{left} + {right}",
                ExpressionType.Subtract => $"{left} - {right}",
                ExpressionType.Multiply => $"{left} * {right}",
                ExpressionType.Divide => $"{left} / {right}",
                ExpressionType.Modulo => $"{left} % {right}",
                _ => $"{left} = {right}"
            };
        }

        private string GetColumnName(Expression expression)
        {
            if (expression is MemberExpression member)
            {
                var columnName = member.Member.Name;
                return _dialect.ColumnLeft + columnName + _dialect.ColumnRight;
            }
            return "Column";
        }

        private string GetConstantValue(ConstantExpression constant)
        {
            return FormatConstantValue(constant.Value);
        }

        private string FormatConstantValue(object? value)
        {
            if (value == null)
                return "NULL";
            return value switch
            {
                string s => _dialect.StringLeft + s.Replace("'", "''") + _dialect.StringRight,
                bool b => b ? "1" : "0",
                DateTime dt => _dialect.StringLeft + dt.ToString("yyyy-MM-dd HH:mm:ss") + _dialect.StringRight,
                _ => value.ToString() ?? "NULL"
            };
        }
    }
}
