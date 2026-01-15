// -----------------------------------------------------------------------
// <copyright file="SqlExpressionVisitor.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Sqlx.Expressions;

namespace Sqlx
{
    /// <summary>
    /// Visits LINQ expression trees and generates SQL.
    /// </summary>
    internal class SqlExpressionVisitor : ExpressionVisitor
    {
        // Shared StringBuilder pool for reducing allocations
        [ThreadStatic]
        private static StringBuilder? _sharedBuilder;

        private readonly SqlDialect _dialect;
        private readonly Dictionary<string, object?> _parameters;
        private readonly ExpressionParser _parser;

        private readonly List<string> _selectColumns;
        private readonly List<string> _whereConditions;
        private readonly List<string> _orderByExpressions;
        private readonly List<string> _groupByExpressions;
        private readonly List<string> _havingConditions;
        private int? _take;
        private int? _skip;
        private string? _tableName;
        private Type? _entityType;
        private bool _isDistinct;

        public SqlExpressionVisitor(SqlDialect dialect, bool parameterized = false)
        {
            _dialect = dialect;
            _parameters = new Dictionary<string, object?>(4);
            _parser = new ExpressionParser(dialect, _parameters, parameterized);

            // Pre-allocate with small capacity
            _selectColumns = new List<string>(4);
            _whereConditions = new List<string>(4);
            _orderByExpressions = new List<string>(2);
            _groupByExpressions = new List<string>(2);
            _havingConditions = new List<string>(2);
        }

        public Dictionary<string, object?> GetParameters() => new(_parameters);

        public string GenerateSql(Expression expression)
        {
            Visit(expression);
            return BuildSql();
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is IQueryable queryable)
            {
                _tableName = queryable.ElementType.Name;
                _entityType = queryable.ElementType;
            }

            return base.VisitConstant(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // Process source first (for chained calls)
            if (node.Arguments.Count > 0)
            {
                Visit(node.Arguments[0]);
            }

            switch (node.Method.Name)
            {
                case "Where":
                    VisitWhere(node);
                    break;
                case "Select":
                    VisitSelect(node);
                    break;
                case "OrderBy":
                    VisitOrderBy(node, ascending: true);
                    break;
                case "OrderByDescending":
                    VisitOrderBy(node, ascending: false);
                    break;
                case "ThenBy":
                    VisitOrderBy(node, ascending: true);
                    break;
                case "ThenByDescending":
                    VisitOrderBy(node, ascending: false);
                    break;
                case "Take":
                    VisitTake(node);
                    break;
                case "Skip":
                    VisitSkip(node);
                    break;
                case "First":
                case "FirstOrDefault":
                    VisitFirst(node);
                    break;
                case "Count":
                case "LongCount":
                    // Count is handled in BuildSql by wrapping the query
                    break;
                case "ToList":
                    // ToList doesn't modify the SQL, just materializes results
                    break;
                case "GroupBy":
                    VisitGroupBy(node);
                    break;
                case "Distinct":
                    _isDistinct = true;
                    break;
            }

            return node;
        }

        private void VisitWhere(MethodCallExpression node)
        {
            if (node.Arguments.Count > 1)
            {
                var predicate = GetLambda(node.Arguments[1]);
                if (predicate != null)
                {
                    var condition = _parser.Parse(predicate.Body);
                    _whereConditions.Add(string.Concat("(", condition, ")"));
                }
            }
        }

        private void VisitSelect(MethodCallExpression node)
        {
            if (node.Arguments.Count > 1)
            {
                var selector = GetLambda(node.Arguments[1]);
                if (selector != null)
                {
                    // When we have GROUP BY, the Select after GroupBy needs special handling
                    // The selector body should contain aggregate functions and/or grouping keys
                    _selectColumns.Clear();
                    
                    // Parse the selector body to extract columns/aggregates
                    // This will handle both simple property access and aggregate function calls
                    var columns = _parser.ExtractColumns(selector.Body);
                    _selectColumns.AddRange(columns);
                }
            }
        }

        private void VisitOrderBy(MethodCallExpression node, bool ascending)
        {
            if (node.Arguments.Count > 1)
            {
                var keySelector = GetLambda(node.Arguments[1]);
                if (keySelector != null)
                {
                    var column = _parser.GetColumnName(keySelector.Body);
                    _orderByExpressions.Add(string.Concat(column, ascending ? " ASC" : " DESC"));
                }
            }
        }

        private void VisitTake(MethodCallExpression node)
        {
            if (node.Arguments.Count > 1 && node.Arguments[1] is ConstantExpression c && c.Value is int take)
            {
                _take = take;
            }
        }

        private void VisitSkip(MethodCallExpression node)
        {
            if (node.Arguments.Count > 1 && node.Arguments[1] is ConstantExpression c && c.Value is int skip)
            {
                _skip = skip;
            }
        }

        private void VisitFirst(MethodCallExpression node)
        {
            // First() and FirstOrDefault() are equivalent to Take(1)
            _take = 1;
            
            // If there's a predicate argument, treat it as a Where clause
            if (node.Arguments.Count > 1)
            {
                var predicate = GetLambda(node.Arguments[1]);
                if (predicate != null)
                {
                    var condition = _parser.Parse(predicate.Body);
                    _whereConditions.Add(string.Concat("(", condition, ")"));
                }
            }
        }

        private void VisitGroupBy(MethodCallExpression node)
        {
            if (node.Arguments.Count > 1)
            {
                var keySelector = GetLambda(node.Arguments[1]);
                if (keySelector != null)
                {
                    var column = _parser.GetColumnName(keySelector.Body);
                    _groupByExpressions.Add(column);
                }
            }
        }

        private static LambdaExpression? GetLambda(Expression expression)
        {
            return expression switch
            {
                LambdaExpression lambda => lambda,
                UnaryExpression { NodeType: ExpressionType.Quote, Operand: LambdaExpression lambda } => lambda,
                _ => null
            };
        }

        private string BuildSql()
        {
            // Reuse thread-local StringBuilder
            var sb = _sharedBuilder ??= new StringBuilder(256);
            sb.Clear();

            // SELECT
            sb.Append("SELECT ");
            if (_isDistinct)
            {
                sb.Append("DISTINCT ");
            }

            if (_selectColumns.Count > 0)
            {
                AppendJoined(sb, _selectColumns, ", ");
            }
            else
            {
                // Never generate SELECT *, always list all columns explicitly
                // Get all columns from the entity provider
                var entityProvider = GetEntityProvider();
                if (entityProvider != null && entityProvider.Columns.Count > 0)
                {
                    for (var i = 0; i < entityProvider.Columns.Count; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(", ");
                        }
                        sb.Append(_dialect.WrapColumn(entityProvider.Columns[i].Name));
                    }
                }
                else
                {
                    // Fallback: if no entity provider, use * (should not happen in normal usage)
                    sb.Append('*');
                }
            }

            // FROM
            sb.Append(" FROM ");
            sb.Append(_dialect.WrapColumn(_tableName ?? "Unknown"));

            // WHERE
            if (_whereConditions.Count > 0)
            {
                sb.Append(" WHERE ");
                AppendJoinedWithTransform(sb, _whereConditions, " AND ", ExpressionHelper.RemoveOuterParentheses);
            }

            // GROUP BY
            if (_groupByExpressions.Count > 0)
            {
                sb.Append(" GROUP BY ");
                AppendJoined(sb, _groupByExpressions, ", ");
            }

            // HAVING
            if (_havingConditions.Count > 0)
            {
                sb.Append(" HAVING ");
                AppendJoined(sb, _havingConditions, " AND ");
            }

            // ORDER BY
            if (_orderByExpressions.Count > 0)
            {
                sb.Append(" ORDER BY ");
                AppendJoined(sb, _orderByExpressions, ", ");
            }

            // PAGINATION
            AppendPaginationClause(sb);

            return sb.ToString();
        }

        private static void AppendJoined(StringBuilder sb, List<string> items, string separator)
        {
            for (var i = 0; i < items.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(separator);
                }

                sb.Append(items[i]);
            }
        }

        private static void AppendJoinedWithTransform(StringBuilder sb, List<string> items, string separator, Func<string, string> transform)
        {
            for (var i = 0; i < items.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(separator);
                }

                sb.Append(transform(items[i]));
            }
        }

        private void AppendPaginationClause(StringBuilder sb)
        {
            if (!_skip.HasValue && !_take.HasValue)
            {
                return;
            }

            if (_dialect.DatabaseType == "SqlServer")
            {
                sb.Append(" OFFSET ");
                sb.Append(_skip ?? 0);
                sb.Append(" ROWS");
                if (_take.HasValue)
                {
                    sb.Append(" FETCH NEXT ");
                    sb.Append(_take.Value);
                    sb.Append(" ROWS ONLY");
                }
            }
            else
            {
                if (_take.HasValue)
                {
                    sb.Append(" LIMIT ");
                    sb.Append(_take.Value);
                }

                if (_skip.HasValue)
                {
                    sb.Append(" OFFSET ");
                    sb.Append(_skip.Value);
                }
            }
        }

        /// <summary>
        /// Gets the entity provider for the current entity type using reflection.
        /// This is only called when no explicit Select is specified.
        /// </summary>
        private IEntityProvider? GetEntityProvider()
        {
            if (_entityType == null)
            {
                return null;
            }

            try
            {
                // Try to get the cached EntityProvider from SqlQuery<T>
                var sqlQueryType = typeof(SqlQuery<>).MakeGenericType(_entityType);
                var entityProviderProperty = sqlQueryType.GetProperty("EntityProvider", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                
                if (entityProviderProperty != null)
                {
                    return entityProviderProperty.GetValue(null) as IEntityProvider;
                }
            }
            catch
            {
                // If reflection fails, return null and fall back to SELECT *
            }

            return null;
        }
    }
}
