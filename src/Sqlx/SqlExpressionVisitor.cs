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
        public string? SubQuerySql { get; set; }
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
        
        // Track property mappings for multi-level JOINs
        private readonly Dictionary<string, string> _propertyAliasMap = new(4);
        private readonly Dictionary<string, string> _propertyColumnMap = new(4);
        
        private int? _take;
        private int? _skip;
        private string? _tableName;
        private Type? _elementType;
        private string? _fromSubQuerySql;
        private bool _isDistinct;

        public SqlExpressionVisitor(SqlDialect dialect, bool parameterized = false, IEntityProvider? entityProvider = null, string? outerGroupByColumn = null)
        {
            _dialect = dialect;
            _parameters = new Dictionary<string, object?>(4);
            _parser = new ExpressionParser(dialect, _parameters, parameterized);
            _entityProvider = entityProvider;
            
            // Set outer scope's groupByColumn for subquery references
            if (outerGroupByColumn != null)
                _parser.SetGroupByColumn(outerGroupByColumn);
        }

        public Dictionary<string, object?> GetParameters() => new(_parameters);
        
        public ExpressionParser Parser => _parser;

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
                _elementType = queryable.ElementType;
                
                // Check for FROM subquery
                if (queryable is ISqlxQueryable sqlxQueryable)
                {
                    if (sqlxQueryable.SubQuerySource != null)
                    {
                        _fromSubQuerySql = GenerateSubQuery(sqlxQueryable.SubQuerySource.Expression, sqlxQueryable.SubQuerySource.ElementType);
                    }
                    else if (sqlxQueryable.Expression != node)
                    {
                        _fromSubQuerySql = GenerateSubQuery(sqlxQueryable.Expression, queryable.ElementType);
                    }
                }
            }
            return base.VisitConstant(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // Handle SubQuery.For<T>()
            if (node.Method.DeclaringType == typeof(SubQuery) && node.Method.Name == "For")
            {
                var genericArgs = node.Method.GetGenericArguments();
                if (genericArgs.Length > 0)
                {
                    _tableName = genericArgs[0].Name;
                    _elementType = genericArgs[0];
                }
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
                _whereConditions.Add(_parser.Parse(predicate.Body));
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
                UpdateColumnMapping(selector.Body);
            }
        }

        private void VisitOrderBy(MethodCallExpression node, bool ascending)
        {
            var keySelector = GetLambda(node.Arguments.ElementAtOrDefault(1));
            if (keySelector != null)
            {
                var col = ResolveColumn(keySelector.Body);
                _orderByExpressions.Add($"{col} {(ascending ? "ASC" : "DESC")}");
            }
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
                _whereConditions.Add(_parser.Parse(predicate.Body));
        }

        private void VisitGroupBy(MethodCallExpression node)
        {
            var keySelector = GetLambda(node.Arguments.ElementAtOrDefault(1));
            if (keySelector != null)
                _groupByExpressions.Add(ResolveColumn(keySelector.Body));
        }

        private void VisitJoin(MethodCallExpression node, JoinType joinType)
        {
            if (node.Arguments.Count < 5) return;

            var innerArg = node.Arguments[1];
            string? innerTableName = null;
            string? innerSubQuerySql = null;
            Type? innerElementType = null;

            // Extract inner table info
            if (innerArg is ConstantExpression { Value: IQueryable innerQueryable })
            {
                innerTableName = innerQueryable.ElementType.Name;
                innerElementType = innerQueryable.ElementType;
                
                if (innerQueryable is ISqlxQueryable sqlxQueryable)
                {
                    if (sqlxQueryable.SubQuerySource != null)
                        innerSubQuerySql = GenerateSubQuery(sqlxQueryable.SubQuerySource.Expression, innerElementType);
                    else if (sqlxQueryable.Expression is not ConstantExpression)
                        innerSubQuerySql = GenerateSubQuery(sqlxQueryable.Expression, innerElementType);
                }
            }
            else if (innerArg is MethodCallExpression innerMethodCall)
            {
                var returnType = innerMethodCall.Type;
                if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(IQueryable<>))
                    innerElementType = returnType.GetGenericArguments()[0];
                else
                    innerElementType = innerMethodCall.Method.GetGenericArguments().FirstOrDefault();
                
                innerTableName = innerElementType?.Name;
                if (innerElementType != null)
                    innerSubQuerySql = GenerateSubQuery(innerArg, innerElementType);
            }

            if (string.IsNullOrEmpty(innerTableName)) return;

            var outerKeySelector = GetLambda(node.Arguments[2]);
            var innerKeySelector = GetLambda(node.Arguments[3]);
            if (outerKeySelector == null || innerKeySelector == null) return;

            var outerColumn = _parser.GetColumnName(outerKeySelector.Body);
            var innerColumn = _parser.GetColumnName(innerKeySelector.Body);
            
            var alias = $"t{_joinClauses.Count + 2}";
            var outerAlias = ResolveOuterAlias(outerKeySelector.Body);

            _joinClauses.Add(new JoinClause
            {
                JoinType = joinType,
                TableName = innerTableName,
                SubQuerySql = innerSubQuerySql,
                Alias = alias,
                OnCondition = $"{_dialect.WrapColumn(outerAlias)}.{outerColumn} = {_dialect.WrapColumn(alias)}.{innerColumn}"
            });

            var resultSelector = GetLambda(node.Arguments[4]);
            if (resultSelector != null)
            {
                _selectColumns.Clear();
                _selectColumns.AddRange(_parser.ExtractColumns(resultSelector.Body));
                UpdateJoinPropertyMapping(resultSelector, alias);
            }
        }

        /// <summary>
        /// Generates SQL for a subquery. Subquery is just another query with the same logic.
        /// </summary>
        private string GenerateSubQuery(Expression expr, Type elementType)
        {
            var entityProvider = EntityProviderRegistry.Get(elementType);
            var visitor = new SqlExpressionVisitor(_dialect, _parameters.Count > 0, entityProvider);
            
            // Pass groupByColumn for outer scope references
            if (_parser.GroupByColumn != null)
                visitor._parser.SetGroupByColumn(_parser.GroupByColumn);
            
            var sql = visitor.GenerateSql(expr);
            
            // Merge parameters
            foreach (var p in visitor.GetParameters())
                _parameters[p.Key] = p.Value;
            
            return sql;
        }

        private string ResolveColumn(Expression expr)
        {
            if (expr is MemberExpression m && m.Expression is ParameterExpression)
            {
                if (_propertyColumnMap.TryGetValue(m.Member.Name, out var col))
                    return col;
            }
            return _parser.GetColumnName(expr);
        }

        private string ResolveOuterAlias(Expression expr)
        {
            var defaultAlias = _fromSubQuerySql != null ? "sq" : (_joinClauses.Count == 0 ? "t1" : $"t{_joinClauses.Count + 1}");
            
            if (expr is MemberExpression m && m.Expression is MemberExpression parent && parent.Expression is ParameterExpression)
            {
                if (_propertyAliasMap.TryGetValue(parent.Member.Name, out var alias))
                    return alias;
            }
            return defaultAlias;
        }

        private void UpdateColumnMapping(Expression expr)
        {
            if (expr is not NewExpression newExpr || newExpr.Members == null) return;

            for (var i = 0; i < newExpr.Arguments.Count; i++)
            {
                var name = newExpr.Members[i].Name;
                var arg = newExpr.Arguments[i];
                
                if (arg is MethodCallExpression mc)
                    _propertyColumnMap[name] = _parser.ParseRaw(mc);
                else if (arg is MemberExpression ma)
                    _propertyColumnMap[name] = _parser.GetColumnName(ma);
            }
        }

        private void UpdateJoinPropertyMapping(LambdaExpression lambda, string innerAlias)
        {
            if (lambda.Body is not NewExpression newExpr || newExpr.Members == null) return;

            var outerAlias = _fromSubQuerySql != null ? "sq" : (_joinClauses.Count == 1 ? "t1" : $"t{_joinClauses.Count}");
            var innerParam = lambda.Parameters.Count > 1 ? lambda.Parameters[1].Name : null;
            
            for (var i = 0; i < newExpr.Arguments.Count; i++)
            {
                var name = newExpr.Members[i].Name;
                var arg = newExpr.Arguments[i];
                
                if (arg is ParameterExpression p)
                {
                    _propertyAliasMap[name] = p.Name == innerParam ? innerAlias : outerAlias;
                }
                else if (arg is MemberExpression ma)
                {
                    if (ma.Expression is ParameterExpression && _propertyAliasMap.TryGetValue(ma.Member.Name, out var src))
                        _propertyAliasMap[name] = src;
                    else
                        _propertyAliasMap[name] = outerAlias;
                    
                    _propertyColumnMap[name] = _parser.GetColumnName(ma);
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

            if (_selectColumns.Count > 0)
                sb.AppendJoin(", ", _selectColumns);
            else if (_entityProvider?.Columns.Count > 0)
                sb.AppendJoin(", ", _entityProvider.Columns.Select(c => _dialect.WrapColumn(c.Name)));
            else if (_elementType != null)
            {
                var ep = EntityProviderRegistry.Get(_elementType);
                if (ep?.Columns.Count > 0)
                    sb.AppendJoin(", ", ep.Columns.Select(c => _dialect.WrapColumn(c.Name)));
                else
                    sb.Append('*');
            }
            else
                sb.Append('*');

            // FROM - always use alias for subquery
            if (_fromSubQuerySql != null)
            {
                sb.Append(" FROM (").Append(_fromSubQuerySql).Append(") AS ").Append(_dialect.WrapColumn("sq"));
            }
            else if (_joinClauses.Count > 0)
            {
                sb.Append(" FROM ").Append(_dialect.WrapColumn(_tableName ?? "Unknown"))
                  .Append(" AS ").Append(_dialect.WrapColumn("t1"));
            }
            else
            {
                sb.Append(" FROM ").Append(_dialect.WrapColumn(_tableName ?? "Unknown"));
            }

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

                if (!string.IsNullOrEmpty(join.SubQuerySql))
                    sb.Append(" (").Append(join.SubQuerySql).Append(") AS ").Append(_dialect.WrapColumn(join.Alias));
                else
                {
                    sb.Append(' ').Append(_dialect.WrapColumn(join.TableName));
                    if (!string.IsNullOrEmpty(join.Alias))
                        sb.Append(" AS ").Append(_dialect.WrapColumn(join.Alias));
                }
                sb.Append(" ON ").Append(join.OnCondition);
            }

            // WHERE
            if (_whereConditions.Count > 0)
            {
                sb.Append(" WHERE ");
                sb.AppendJoin(" AND ", _whereConditions);
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
