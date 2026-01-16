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
    /// Represents a JOIN clause in SQL.
    /// </summary>
    internal class JoinClause
    {
        public JoinType JoinType { get; set; }
        public string TableName { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;
        public string OnCondition { get; set; } = string.Empty;
    }

    /// <summary>
    /// Types of SQL JOINs.
    /// </summary>
    internal enum JoinType { Inner, Left, Right, Full }

    /// <summary>
    /// Visits LINQ expression trees and generates SQL.
    /// </summary>
    internal class SqlExpressionVisitor : ExpressionVisitor
    {
        [ThreadStatic]
        private static StringBuilder? _sharedBuilder;

        private readonly SqlDialect _dialect;
        private readonly Dictionary<string, object?> _parameters;
        private readonly ExpressionParser _parser;
        private readonly IEntityProvider? _entityProvider;

        private readonly List<string> _selectColumns = new(4);
        private readonly List<string> _whereConditions = new(4);
        private readonly List<string> _orderByExpressions = new(2);
        private readonly List<string> _groupByExpressions = new(2);
        private readonly List<JoinClause> _joinClauses = new(2);
        private int? _take;
        private int? _skip;
        private string? _tableName;
        private bool _isDistinct;

        public SqlExpressionVisitor(SqlDialect dialect, bool parameterized = false, IEntityProvider? entityProvider = null)
        {
            _dialect = dialect;
            _parameters = new Dictionary<string, object?>(4);
            _parser = new ExpressionParser(dialect, _parameters, parameterized);
            _entityProvider = entityProvider;
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
                _tableName = queryable.ElementType.Name;
            return base.VisitConstant(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Arguments.Count > 0)
                Visit(node.Arguments[0]);

            switch (node.Method.Name)
            {
                case "Where": VisitWhere(node); break;
                case "Select": VisitSelect(node); break;
                case "OrderBy": VisitOrderBy(node, true); break;
                case "OrderByDescending": VisitOrderBy(node, false); break;
                case "ThenBy": VisitOrderBy(node, true); break;
                case "ThenByDescending": VisitOrderBy(node, false); break;
                case "Take": VisitTake(node); break;
                case "Skip": VisitSkip(node); break;
                case "First" or "FirstOrDefault": VisitFirst(node); break;
                case "GroupBy": VisitGroupBy(node); break;
                case "Distinct": _isDistinct = true; break;
                case "Join": VisitJoin(node, JoinType.Inner); break;
                case "GroupJoin": VisitJoin(node, JoinType.Left); break;
            }
            return node;
        }

        private void VisitWhere(MethodCallExpression node)
        {
            var predicate = GetLambda(node.Arguments.ElementAtOrDefault(1));
            if (predicate != null)
                _whereConditions.Add($"({_parser.Parse(predicate.Body)})");
        }

        private void VisitSelect(MethodCallExpression node)
        {
            var selector = GetLambda(node.Arguments.ElementAtOrDefault(1));
            if (selector != null)
            {
                _selectColumns.Clear();
                if (_groupByExpressions.Count > 0)
                    _parser.SetGroupByColumn(_groupByExpressions[0]);
                _selectColumns.AddRange(_parser.ExtractColumns(selector.Body));
            }
        }

        private void VisitOrderBy(MethodCallExpression node, bool ascending)
        {
            var keySelector = GetLambda(node.Arguments.ElementAtOrDefault(1));
            if (keySelector != null)
                _orderByExpressions.Add($"{_parser.GetColumnName(keySelector.Body)} {(ascending ? "ASC" : "DESC")}");
        }

        private void VisitTake(MethodCallExpression node)
        {
            if (node.Arguments.ElementAtOrDefault(1) is ConstantExpression { Value: int take })
                _take = take;
        }

        private void VisitSkip(MethodCallExpression node)
        {
            if (node.Arguments.ElementAtOrDefault(1) is ConstantExpression { Value: int skip })
                _skip = skip;
        }

        private void VisitFirst(MethodCallExpression node)
        {
            _take = 1;
            var predicate = GetLambda(node.Arguments.ElementAtOrDefault(1));
            if (predicate != null)
                _whereConditions.Add($"({_parser.Parse(predicate.Body)})");
        }

        private void VisitGroupBy(MethodCallExpression node)
        {
            var keySelector = GetLambda(node.Arguments.ElementAtOrDefault(1));
            if (keySelector != null)
                _groupByExpressions.Add(_parser.GetColumnName(keySelector.Body));
        }

        private void VisitJoin(MethodCallExpression node, JoinType joinType)
        {
            if (node.Arguments.Count < 5) return;

            var innerArg = node.Arguments[1];
            string? innerTableName = null;

            if (innerArg is ConstantExpression { Value: IQueryable innerQueryable })
                innerTableName = innerQueryable.ElementType.Name;
            else if (innerArg is MethodCallExpression innerMethod)
            {
                Visit(innerArg);
                if (innerMethod.Method.IsGenericMethod)
                    innerTableName = innerMethod.Method.GetGenericArguments().FirstOrDefault()?.Name;
            }

            if (string.IsNullOrEmpty(innerTableName)) return;

            var outerKeySelector = GetLambda(node.Arguments[2]);
            var innerKeySelector = GetLambda(node.Arguments[3]);
            if (outerKeySelector == null || innerKeySelector == null) return;

            var outerColumn = _parser.GetColumnName(outerKeySelector.Body);
            var innerColumn = _parser.GetColumnName(innerKeySelector.Body);
            var alias = innerTableName.ToLower();

            _joinClauses.Add(new JoinClause
            {
                JoinType = joinType,
                TableName = innerTableName,
                Alias = alias,
                OnCondition = $"{_dialect.WrapColumn(_tableName ?? "t1")}.{outerColumn} = {_dialect.WrapColumn(alias)}.{innerColumn}"
            });

            var resultSelector = GetLambda(node.Arguments[4]);
            if (resultSelector != null)
            {
                _selectColumns.Clear();
                _selectColumns.AddRange(_parser.ExtractColumns(resultSelector.Body));
            }
        }

        private static LambdaExpression? GetLambda(Expression? expression) => expression switch
        {
            LambdaExpression lambda => lambda,
            UnaryExpression { NodeType: ExpressionType.Quote, Operand: LambdaExpression lambda } => lambda,
            _ => null
        };

        private string BuildSql()
        {
            var sb = _sharedBuilder ??= new StringBuilder(256);
            sb.Clear();

            // SELECT
            sb.Append("SELECT ");
            if (_isDistinct) sb.Append("DISTINCT ");

            if (_selectColumns.Count > 0)
                sb.AppendJoin(", ", _selectColumns);
            else if (_entityProvider?.Columns.Count > 0)
                sb.AppendJoin(", ", _entityProvider.Columns.Select(c => _dialect.WrapColumn(c.Name)));
            else
                sb.Append('*');

            // FROM
            sb.Append(" FROM ").Append(_dialect.WrapColumn(_tableName ?? "Unknown"));

            // JOIN
            foreach (var join in _joinClauses)
            {
                sb.Append(' ').Append(join.JoinType switch
                {
                    JoinType.Inner => "INNER JOIN",
                    JoinType.Left => "LEFT JOIN",
                    JoinType.Right => "RIGHT JOIN",
                    JoinType.Full => "FULL OUTER JOIN",
                    _ => "INNER JOIN"
                });
                sb.Append(' ').Append(_dialect.WrapColumn(join.TableName));
                if (!string.IsNullOrEmpty(join.Alias))
                    sb.Append(" AS ").Append(_dialect.WrapColumn(join.Alias));
                sb.Append(" ON ").Append(join.OnCondition);
            }

            // WHERE
            if (_whereConditions.Count > 0)
            {
                sb.Append(" WHERE ");
                sb.AppendJoin(" AND ", _whereConditions.Select(ExpressionHelper.RemoveOuterParentheses));
            }

            // GROUP BY
            if (_groupByExpressions.Count > 0)
            {
                sb.Append(" GROUP BY ");
                sb.AppendJoin(", ", _groupByExpressions);
            }

            // ORDER BY
            if (_orderByExpressions.Count > 0)
            {
                sb.Append(" ORDER BY ");
                sb.AppendJoin(", ", _orderByExpressions);
            }

            // PAGINATION
            AppendPagination(sb);

            return sb.ToString();
        }

        private void AppendPagination(StringBuilder sb)
        {
            if (!_skip.HasValue && !_take.HasValue) return;

            if (_dialect.DatabaseType is "SqlServer" or "Oracle" or "DB2")
            {
                sb.Append(" OFFSET ").Append(_skip ?? 0).Append(" ROWS");
                if (_take.HasValue)
                    sb.Append(" FETCH NEXT ").Append(_take.Value).Append(" ROWS ONLY");
            }
            else
            {
                if (_take.HasValue) sb.Append(" LIMIT ").Append(_take.Value);
                if (_skip.HasValue) sb.Append(" OFFSET ").Append(_skip.Value);
            }
        }
    }
}
