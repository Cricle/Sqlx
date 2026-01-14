// -----------------------------------------------------------------------
// <copyright file="SqlExpressionVisitor.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
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
        private readonly SqlDialect _dialect;
        private readonly bool _parameterized;
        private readonly Dictionary<string, object?> _parameters = new();
        private readonly ExpressionParser _parser;

        private readonly List<string> _selectColumns = new();
        private readonly List<string> _whereConditions = new();
        private readonly List<string> _orderByExpressions = new();
        private readonly List<string> _groupByExpressions = new();
        private readonly List<string> _havingConditions = new();
        private int? _take;
        private int? _skip;
        private string? _tableName;
        private bool _isDistinct;

        public SqlExpressionVisitor(SqlDialect dialect, bool parameterized = false)
        {
            _dialect = dialect;
            _parameterized = parameterized;
            _parser = new ExpressionParser(dialect, _parameters, parameterized);
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
                    VisitOrderBy(node, ascending: true, thenBy: false);
                    break;
                case "OrderByDescending":
                    VisitOrderBy(node, ascending: false, thenBy: false);
                    break;
                case "ThenBy":
                    VisitOrderBy(node, ascending: true, thenBy: true);
                    break;
                case "ThenByDescending":
                    VisitOrderBy(node, ascending: false, thenBy: true);
                    break;
                case "Take":
                    VisitTake(node);
                    break;
                case "Skip":
                    VisitSkip(node);
                    break;
                case "GroupBy":
                    VisitGroupBy(node);
                    break;
                case "Distinct":
                    _isDistinct = true;
                    break;
                default:
                    // Unknown method - ignore for now
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
                    _whereConditions.Add($"({condition})");
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
                    _selectColumns.Clear();
                    _selectColumns.AddRange(_parser.ExtractColumns(selector.Body));
                }
            }
        }

        private void VisitOrderBy(MethodCallExpression node, bool ascending, bool thenBy)
        {
            if (node.Arguments.Count > 1)
            {
                var keySelector = GetLambda(node.Arguments[1]);
                if (keySelector != null)
                {
                    var column = _parser.GetColumnName(keySelector.Body);
                    var direction = ascending ? "ASC" : "DESC";
                    _orderByExpressions.Add($"{column} {direction}");
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
            var sb = new StringBuilder();

            // SELECT
            sb.Append("SELECT ");
            if (_isDistinct) sb.Append("DISTINCT ");
            sb.Append(_selectColumns.Count > 0 ? string.Join(", ", _selectColumns) : "*");

            // FROM
            sb.Append(" FROM ");
            sb.Append(_dialect.WrapColumn(_tableName ?? "Unknown"));

            // WHERE
            if (_whereConditions.Count > 0)
            {
                sb.Append(" WHERE ");
                sb.Append(string.Join(" AND ", _whereConditions.Select(ExpressionHelper.RemoveOuterParentheses)));
            }

            // GROUP BY
            if (_groupByExpressions.Count > 0)
            {
                sb.Append(" GROUP BY ");
                sb.Append(string.Join(", ", _groupByExpressions));
            }

            // HAVING
            if (_havingConditions.Count > 0)
            {
                sb.Append(" HAVING ");
                sb.Append(string.Join(" AND ", _havingConditions));
            }

            // ORDER BY
            if (_orderByExpressions.Count > 0)
            {
                sb.Append(" ORDER BY ");
                sb.Append(string.Join(", ", _orderByExpressions));
            }

            // PAGINATION
            sb.Append(GetPaginationClause());

            return sb.ToString();
        }

        private string GetPaginationClause()
        {
            if (!_skip.HasValue && !_take.HasValue) return "";

            if (_dialect.DatabaseType == "SqlServer")
            {
                var offset = $" OFFSET {_skip ?? 0} ROWS";
                var fetch = _take.HasValue ? $" FETCH NEXT {_take.Value} ROWS ONLY" : "";
                return offset + fetch;
            }

            var limit = _take.HasValue ? $" LIMIT {_take.Value}" : "";
            var offsetClause = _skip.HasValue ? $" OFFSET {_skip.Value}" : "";
            return limit + offsetClause;
        }
    }
}
