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
    internal record JoinClause(JoinType JoinType, string TableName, string? SubQuerySql, string Alias, string OnCondition);

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
        private readonly bool _parameterized;
        private readonly Dictionary<string, object?> _parameters;
        private readonly ExpressionParser _parser;
        private readonly IEntityProvider? _entityProvider;

        private List<string>? _selectColumns;
        private List<string>? _whereConditions;
        private List<string>? _orderByExpressions;
        private List<string>? _groupByExpressions;
        private List<JoinClause>? _joinClauses;
        
        // Track property mappings for multi-level JOINs
        private Dictionary<string, string>? _propertyAliasMap;
        private Dictionary<string, string>? _propertyColumnMap;
        
        private int? _take;
        private int? _skip;
        private string? _tableName;
        private Type? _elementType;
        private string? _fromSubQuerySql;
        private bool _isDistinct;
        private Dictionary<Expression, string>? _subQuerySqlCache;

        public SqlExpressionVisitor(
            SqlDialect dialect,
            bool parameterized = false,
            IEntityProvider? entityProvider = null,
            string? outerGroupByColumn = null,
            Dictionary<string, object?>? parameterStore = null)
        {
            _dialect = dialect;
            _parameterized = parameterized;
            _parameters = parameterStore ?? new Dictionary<string, object?>(4);
            _parser = new ExpressionParser(dialect, _parameters, parameterized, entityProvider: entityProvider);
            _entityProvider = entityProvider;
            
            // Set outer scope's groupByColumn for subquery references
            if (outerGroupByColumn != null)
                _parser.SetGroupByColumn(outerGroupByColumn);
        }

        public IReadOnlyDictionary<string, object?> GetParameters() => _parameters;
        
        public ExpressionParser Parser => _parser;

        public string GenerateSql(Expression expression)
        {
            Analyze(expression);
            return BuildSql();
        }

        public void Analyze(Expression expression)
        {
            Visit(expression);
        }

        public string BuildAnalyzedSql() => BuildSql();

        public bool CanBuildDirectAggregateQuery() =>
            !_isDistinct &&
            (_groupByExpressions?.Count ?? 0) == 0 &&
            !_skip.HasValue &&
            !_take.HasValue;

        public string BuildCountSql()
        {
            var sb = _sharedBuilder ??= new StringBuilder(256);
            sb.Clear();
            sb.Append("SELECT ").Append(_dialect.Count());
            AppendDirectAggregateBody(sb);
            return sb.ToString();
        }

        public string BuildExistsSql()
        {
            var sb = _sharedBuilder ??= new StringBuilder(256);
            sb.Clear();

            if (_dialect.DatabaseType == "SqlServer")
            {
                sb.Append("SELECT ").Append(_dialect.Limit("1")).Append(" 1");
                AppendDirectAggregateBody(sb);
            }
            else
            {
                sb.Append("SELECT 1");
                AppendDirectAggregateBody(sb);
                sb.Append(' ').Append(_dialect.Limit("1"));
            }

            return sb.ToString();
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is IQueryable queryable)
            {
                _tableName = TableNameResolver.Resolve(queryable.ElementType);
                _elementType = queryable.ElementType;
                
                // Check for FROM subquery
                if (queryable is ISqlxQueryable { SubQuerySource: { } source })
                    _fromSubQuerySql = GenerateSubQuery(source.Expression, source.ElementType);
                else if (queryable is ISqlxQueryable sqlxQueryable && sqlxQueryable.Expression != node)
                    _fromSubQuerySql = GenerateSubQuery(sqlxQueryable.Expression, queryable.ElementType);
            }
            return base.VisitConstant(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // Handle SubQuery.For<T>()
            if (node.Method.DeclaringType == typeof(SubQuery) && node.Method.Name == "For" && node.Type.IsGenericType)
            {
                var elementType = node.Type.GenericTypeArguments[0];
                _tableName = TableNameResolver.Resolve(elementType);
                _elementType = elementType;
                return node;
            }

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
                (_whereConditions ??= new List<string>(4)).Add(_parser.Parse(predicate.Body));
        }

        private void VisitSelect(MethodCallExpression node)
        {
            var selector = GetLambda(node.Arguments.ElementAtOrDefault(1));
            if (selector != null)
            {
                var selectColumns = _selectColumns ??= new List<string>(4);
                selectColumns.Clear();
                if ((_groupByExpressions?.Count ?? 0) > 0)
                    _parser.SetGroupByColumn(_groupByExpressions![0]);
                _parser.ExtractColumns(selector.Body, selectColumns);
                UpdateColumnMapping(selector.Body, selectColumns);
            }
        }

        private void VisitOrderBy(MethodCallExpression node, bool ascending)
        {
            var keySelector = GetLambda(node.Arguments.ElementAtOrDefault(1));
            if (keySelector != null)
                (_orderByExpressions ??= new List<string>(2)).Add($"{ResolveColumn(keySelector.Body)} {(ascending ? "ASC" : "DESC")}");
        }

        private void VisitTake(MethodCallExpression node) => _take = (node.Arguments.ElementAtOrDefault(1) as ConstantExpression)?.Value as int?;

        private void VisitSkip(MethodCallExpression node) => _skip = (node.Arguments.ElementAtOrDefault(1) as ConstantExpression)?.Value as int?;

        private void VisitFirst(MethodCallExpression node)
        {
            _take = 1;
            var predicate = GetLambda(node.Arguments.ElementAtOrDefault(1));
            if (predicate != null)
                (_whereConditions ??= new List<string>(4)).Add(_parser.Parse(predicate.Body));
        }

        private void VisitGroupBy(MethodCallExpression node)
        {
            var keySelector = GetLambda(node.Arguments.ElementAtOrDefault(1));
            if (keySelector != null)
                (_groupByExpressions ??= new List<string>(2)).Add(ResolveColumn(keySelector.Body));
        }

        private void VisitJoin(MethodCallExpression node, JoinType joinType)
        {
            if (node.Arguments.Count < 5) return;

            var (innerTableName, innerSubQuerySql) = ExtractInnerTableInfo(node.Arguments[1]);
            if (string.IsNullOrEmpty(innerTableName)) return;

            var outerKeySelector = GetLambda(node.Arguments[2]);
            var innerKeySelector = GetLambda(node.Arguments[3]);
            if (outerKeySelector == null || innerKeySelector == null) return;

            var outerColumn = _parser.GetColumnName(outerKeySelector.Body);
            var innerColumn = _parser.GetColumnName(innerKeySelector.Body);
            
            var joinClauses = _joinClauses ??= new List<JoinClause>(2);
            var alias = $"t{joinClauses.Count + 2}";
            var outerAlias = ResolveOuterAlias(outerKeySelector.Body);

            joinClauses.Add(new JoinClause(
                joinType,
                innerTableName,
                innerSubQuerySql,
                alias,
                $"{_dialect.WrapColumn(outerAlias)}.{outerColumn} = {_dialect.WrapColumn(alias)}.{innerColumn}"));

            var resultSelector = GetLambda(node.Arguments[4]);
            if (resultSelector != null)
            {
                var selectColumns = _selectColumns ??= new List<string>(4);
                selectColumns.Clear();
                _parser.ExtractColumns(resultSelector.Body, selectColumns);
                UpdateJoinPropertyMapping(resultSelector, alias);
            }
        }

        private (string? tableName, string? subQuerySql) ExtractInnerTableInfo(Expression innerArg)
        {
            Type? elementType = null;
            string? tableName = null;
            string? subQuerySql = null;

            if (innerArg is ConstantExpression { Value: IQueryable innerQueryable })
            {
                tableName = TableNameResolver.Resolve(innerQueryable.ElementType);
                elementType = innerQueryable.ElementType;
                
                if (innerQueryable is ISqlxQueryable sqlxQueryable)
                {
                    if (sqlxQueryable.SubQuerySource != null)
                        subQuerySql = GenerateSubQuery(sqlxQueryable.SubQuerySource.Expression, elementType);
                    else if (sqlxQueryable.Expression is not ConstantExpression)
                        subQuerySql = GenerateSubQuery(sqlxQueryable.Expression, elementType);
                }
            }
            else if (innerArg is MethodCallExpression { Type.IsGenericType: true } methodCall)
            {
                elementType = methodCall.Type.GenericTypeArguments.FirstOrDefault();
                tableName = elementType != null ? TableNameResolver.Resolve(elementType) : null;
                if (elementType != null)
                    subQuerySql = GenerateSubQuery(innerArg, elementType);
            }

            return (tableName, subQuerySql);
        }

        /// <summary>
        /// Generates SQL for a subquery. Subquery is just another query with the same logic.
        /// </summary>
        private string GenerateSubQuery(Expression expr, Type elementType)
        {
            if (_subQuerySqlCache != null && _subQuerySqlCache.TryGetValue(expr, out var cached))
            {
                return cached;
            }

            var entityProvider = EntityProviderResolver.ResolveOrCreate(elementType);
            var visitor = new SqlExpressionVisitor(
                _dialect,
                _parameterized,
                entityProvider,
                _parser.GroupByColumn,
                _parameters);

            var sql = visitor.GenerateSql(expr);
            (_subQuerySqlCache ??= new Dictionary<Expression, string>(ReferenceEqualityComparer<Expression>.Instance))[expr] = sql;
            return sql;
        }

        private string ResolveColumn(Expression expr)
        {
            if (expr is MemberExpression m && m.Expression is ParameterExpression)
            {
                if (_propertyColumnMap != null && _propertyColumnMap.TryGetValue(m.Member.Name, out var col))
                    return col;
            }
            return _parser.GetColumnName(expr);
        }

        private string GetCurrentAlias(int offset = 0) => _fromSubQuerySql != null ? "sq" : ((_joinClauses?.Count ?? 0) == 0 ? "t1" : $"t{_joinClauses!.Count + offset}");

        private string ResolveOuterAlias(Expression expr)
        {            
            if (expr is MemberExpression m && m.Expression is MemberExpression parent && parent.Expression is ParameterExpression)
            {
                if (_propertyAliasMap != null && _propertyAliasMap.TryGetValue(parent.Member.Name, out var alias))
                    return alias;
            }
            return GetCurrentAlias(1);
        }

        private void UpdateColumnMapping(Expression expr, IReadOnlyList<string> extractedColumns)
        {
            if (expr is not NewExpression newExpr || newExpr.Members == null) return;

            for (var i = 0; i < newExpr.Arguments.Count; i++)
            {
                var name = newExpr.Members[i].Name;
                var arg = newExpr.Arguments[i];
                var extractedColumn = i < extractedColumns.Count
                    ? RemoveProjectionAlias(extractedColumns[i], name)
                    : null;
                
                if (arg is MemberExpression ma)
                {
                    (_propertyColumnMap ??= new Dictionary<string, string>(4))[name] = _parser.GetColumnName(ma);
                }
                else if (!string.IsNullOrEmpty(extractedColumn))
                {
                    (_propertyColumnMap ??= new Dictionary<string, string>(4))[name] = extractedColumn;
                }
            }
        }

        private static string RemoveProjectionAlias(string columnExpression, string alias)
        {
            var aliasSuffix = $" AS {alias}";
            return columnExpression.EndsWith(aliasSuffix, StringComparison.Ordinal)
                ? columnExpression.Substring(0, columnExpression.Length - aliasSuffix.Length)
                : columnExpression;
        }

        private void UpdateJoinPropertyMapping(LambdaExpression lambda, string innerAlias)
        {
            if (lambda.Body is not NewExpression newExpr || newExpr.Members == null) return;

            var outerAlias = GetCurrentAlias();
            var innerParam = lambda.Parameters.Count > 1 ? lambda.Parameters[1].Name : null;
            
            for (var i = 0; i < newExpr.Arguments.Count; i++)
            {
                var name = newExpr.Members[i].Name;
                var arg = newExpr.Arguments[i];
                
                if (arg is ParameterExpression p)
                {
                    (_propertyAliasMap ??= new Dictionary<string, string>(4))[name] = p.Name == innerParam ? innerAlias : outerAlias;
                }
                else if (arg is MemberExpression ma)
                {
                    if (ma.Expression is ParameterExpression && _propertyAliasMap != null && _propertyAliasMap.TryGetValue(ma.Member.Name, out var src))
                        (_propertyAliasMap ??= new Dictionary<string, string>(4))[name] = src;
                    else
                        (_propertyAliasMap ??= new Dictionary<string, string>(4))[name] = outerAlias;
                    
                    (_propertyColumnMap ??= new Dictionary<string, string>(4))[name] = _parser.GetColumnName(ma);
                }
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

            if ((_selectColumns?.Count ?? 0) > 0)
            {
                AppendJoined(sb, _selectColumns!, ", ");
            }
            else
            {
                // Get columns from entity provider - no fallback to SELECT *
                var columns = GetEntityColumns();
                if (columns.Count == 0)
                    throw new InvalidOperationException($"No columns found for entity type '{_elementType?.Name ?? _tableName}'. Ensure the type exposes readable and writable properties, or use explicit Select().");
                AppendWrappedColumns(sb, columns);
            }

            // FROM
            AppendFromClause(sb);

            // JOIN
            AppendJoinClauses(sb);

            // WHERE
            if ((_whereConditions?.Count ?? 0) > 0)
            {
                sb.Append(" WHERE ");
                AppendJoined(sb, _whereConditions!, " AND ");
            }

            // GROUP BY
            if ((_groupByExpressions?.Count ?? 0) > 0)
            {
                sb.Append(" GROUP BY ");
                AppendJoined(sb, _groupByExpressions!, ", ");
            }

            // ORDER BY
            if ((_orderByExpressions?.Count ?? 0) > 0)
            {
                sb.Append(" ORDER BY ");
                AppendJoined(sb, _orderByExpressions!, ", ");
            }

            // PAGINATION
            AppendPagination(sb);

            return sb.ToString();
        }

        private void AppendDirectAggregateBody(StringBuilder sb)
        {
            AppendFromClause(sb);
            AppendJoinClauses(sb);

            if ((_whereConditions?.Count ?? 0) > 0)
            {
                sb.Append(" WHERE ");
                AppendJoined(sb, _whereConditions!, " AND ");
            }
        }

        private IReadOnlyList<ColumnMeta> GetEntityColumns()
        {
            if (_entityProvider?.Columns.Count > 0)
                return _entityProvider.Columns;
            
            if (_elementType != null)
            {
                var ep = EntityProviderResolver.ResolveOrCreate(_elementType);
                if (ep?.Columns.Count > 0)
                    return ep.Columns;
            }
            
            return Array.Empty<ColumnMeta>();
        }

        private void AppendFromClause(StringBuilder sb)
        {
            var tableName = _dialect.WrapColumn(_tableName ?? throw new InvalidOperationException("Table name is not set."));
            
            if (_fromSubQuerySql != null)
            {
                sb.Append(" FROM (").Append(_fromSubQuerySql).Append(") AS ").Append(_dialect.WrapColumn("sq"));
            }
            else if ((_joinClauses?.Count ?? 0) > 0)
            {
                sb.Append(" FROM ").Append(tableName).Append(" AS ").Append(_dialect.WrapColumn("t1"));
            }
            else
            {
                sb.Append(" FROM ").Append(tableName);
            }
        }

        private static readonly string[] JoinTypeNames = { "INNER JOIN", "LEFT JOIN", "RIGHT JOIN", "FULL OUTER JOIN" };

        private static void AppendJoined(StringBuilder sb, IReadOnlyList<string> values, string separator)
        {
            for (var i = 0; i < values.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(separator);
                }

                sb.Append(values[i]);
            }
        }

        private void AppendWrappedColumns(StringBuilder sb, IReadOnlyList<ColumnMeta> columns)
        {
            for (var i = 0; i < columns.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(_dialect.WrapColumn(columns[i].Name));
            }
        }

        private void AppendJoinClauses(StringBuilder sb)
        {
            if (_joinClauses == null)
            {
                return;
            }

            foreach (var join in _joinClauses)
            {
                sb.Append(' ').Append(JoinTypeNames[(int)join.JoinType]);

                if (!string.IsNullOrEmpty(join.SubQuerySql))
                {
                    sb.Append(" (").Append(join.SubQuerySql).Append(") AS ").Append(_dialect.WrapColumn(join.Alias));
                }
                else
                {
                    sb.Append(' ').Append(_dialect.WrapColumn(join.TableName));
                    sb.Append(" AS ").Append(_dialect.WrapColumn(join.Alias));
                }
                sb.Append(" ON ").Append(join.OnCondition);
            }
        }

        private void AppendPagination(StringBuilder sb)
        {
            if (_take.HasValue && _skip.HasValue)
                sb.Append(' ').Append(_dialect.Paginate(_take.Value.ToString(), _skip.Value.ToString()));
            else if (_take.HasValue)
                sb.Append(' ').Append(_dialect.Limit(_take.Value.ToString()));
            else if (_skip.HasValue)
                sb.Append(' ').Append(_dialect.Offset(_skip.Value.ToString()));
        }
    }
}
