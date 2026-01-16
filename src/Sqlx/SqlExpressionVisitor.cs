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
        public string? SubQuery { get; set; }
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
        
        // Track property-to-alias mapping for multi-level JOINs
        // Key: property name from result selector (e.g., "User", "Dept")
        // Value: table alias (e.g., "t1", "t2")
        private readonly Dictionary<string, string> _propertyAliasMap = new(4);
        
        // Track property-to-column mapping for GroupBy/OrderBy after JOIN
        // Key: property name from result selector (e.g., "DeptName")
        // Value: original column expression (e.g., "[name]")
        private readonly Dictionary<string, string> _propertyColumnMap = new(4);
        
        private int? _take;
        private int? _skip;
        private string? _tableName;
        private string? _subQuery;
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
            // Check if the root expression's source has a subquery source
            var rootSource = GetRootSource(expression);
            if (rootSource is ConstantExpression { Value: ISqlxQueryable sqlxQueryable } &&
                sqlxQueryable.SubQuerySource != null)
            {
                // The source is a subquery, generate it first
                var subVisitor = new SqlExpressionVisitor(_dialect, _parameters.Count > 0, _entityProvider);
                _subQuery = subVisitor.GenerateSql(sqlxQueryable.SubQuerySource.Expression);
                foreach (var p in subVisitor.GetParameters())
                    _parameters[p.Key] = p.Value;
                
                // Set table name from the element type
                _tableName = sqlxQueryable.SubQuerySource.ElementType.Name;
            }
            
            Visit(expression);
            return BuildSql();
        }

        private static Expression? GetRootSource(Expression expression)
        {
            while (expression is MethodCallExpression methodCall && methodCall.Arguments.Count > 0)
            {
                expression = methodCall.Arguments[0];
            }
            return expression;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (node.Value is IQueryable queryable)
            {
                _tableName = queryable.ElementType.Name;
                // Check if this is a SqlxQueryable with an expression (subquery)
                if (queryable is ISqlxQueryable sqlxQueryable && sqlxQueryable.Expression != node)
                {
                    var subVisitor = new SqlExpressionVisitor(_dialect, _parameters.Count > 0, _entityProvider);
                    _subQuery = subVisitor.GenerateSql(sqlxQueryable.Expression);
                    foreach (var p in subVisitor.GetParameters())
                        _parameters[p.Key] = p.Value;
                }
            }
            return base.VisitConstant(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            // Handle SubQuery.For<T>() - get table name from generic type argument
            if (node.Method.DeclaringType == typeof(SubQuery) && node.Method.Name == "For")
            {
                var genericArgs = node.Method.GetGenericArguments();
                if (genericArgs.Length > 0)
                {
                    _tableName = genericArgs[0].Name;
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
                
                // Track column mappings from the Select projection for OrderBy resolution
                UpdateSelectColumnMapping(selector.Body);
            }
        }

        /// <summary>
        /// Updates the property-to-column mapping from a Select projection.
        /// This handles cases like Select(g => new { Count = g.Count() }) where OrderBy(x => x.Count) should resolve to COUNT(*).
        /// </summary>
        private void UpdateSelectColumnMapping(Expression expr)
        {
            if (expr is not NewExpression newExpr || newExpr.Members == null)
                return;

            for (var i = 0; i < newExpr.Arguments.Count; i++)
            {
                var memberName = newExpr.Members[i].Name;
                var arg = newExpr.Arguments[i];
                
                // Check if this is an aggregate method call (e.g., g.Count())
                if (arg is MethodCallExpression methodCall)
                {
                    var aggregateSql = _parser.ParseRaw(methodCall);
                    _propertyColumnMap[memberName] = aggregateSql;
                }
                else if (arg is MemberExpression memberArg)
                {
                    // Regular column access
                    var columnName = _parser.GetColumnName(memberArg);
                    _propertyColumnMap[memberName] = columnName;
                }
            }
        }

        private void VisitOrderBy(MethodCallExpression node, bool ascending)
        {
            var keySelector = GetLambda(node.Arguments.ElementAtOrDefault(1));
            if (keySelector != null)
            {
                var columnName = GetColumnNameWithMapping(keySelector.Body);
                _orderByExpressions.Add($"{columnName} {(ascending ? "ASC" : "DESC")}");
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
                _whereConditions.Add($"({_parser.Parse(predicate.Body)})");
        }

        private void VisitGroupBy(MethodCallExpression node)
        {
            var keySelector = GetLambda(node.Arguments.ElementAtOrDefault(1));
            if (keySelector != null)
            {
                var columnName = GetColumnNameWithMapping(keySelector.Body);
                _groupByExpressions.Add(columnName);
            }
        }

        /// <summary>
        /// Gets the column name, checking the property-to-column mapping first.
        /// This handles cases like GroupBy(x => x.DeptName) where DeptName is an alias from a result selector.
        /// </summary>
        private string GetColumnNameWithMapping(Expression expr)
        {
            // Check if this is a member access on a parameter (e.g., x.DeptName)
            if (expr is MemberExpression m && m.Expression is ParameterExpression)
            {
                var propName = m.Member.Name;
                if (_propertyColumnMap.TryGetValue(propName, out var columnExpr))
                {
                    return columnExpr;
                }
            }
            return _parser.GetColumnName(expr);
        }

        private void VisitJoin(MethodCallExpression node, JoinType joinType)
        {
            if (node.Arguments.Count < 5) return;

            var innerArg = node.Arguments[1];
            string? innerTableName = null;
            string? innerSubQuery = null;

            // Case 1: innerArg is a ConstantExpression containing an IQueryable
            if (innerArg is ConstantExpression { Value: IQueryable innerQueryable })
            {
                innerTableName = innerQueryable.ElementType.Name;
                // Check if this is a SqlxQueryable with a subquery source or non-trivial expression
                if (innerQueryable is ISqlxQueryable sqlxQueryable)
                {
                    if (sqlxQueryable.SubQuerySource != null)
                    {
                        // Has explicit subquery source - don't pass entityProvider, let subquery use its own
                        var subVisitor = new SqlExpressionVisitor(_dialect, _parameters.Count > 0, null);
                        innerSubQuery = subVisitor.GenerateSql(sqlxQueryable.SubQuerySource.Expression);
                    }
                    else if (sqlxQueryable.Expression is not ConstantExpression)
                    {
                        // Has non-trivial expression (e.g., Where, Select, etc.) - don't pass entityProvider
                        var subVisitor = new SqlExpressionVisitor(_dialect, _parameters.Count > 0, null);
                        innerSubQuery = subVisitor.GenerateSql(sqlxQueryable.Expression);
                    }
                }
            }
            // Case 2: innerArg is a MethodCallExpression (e.g., .Where(...))
            else if (innerArg is MethodCallExpression innerMethodCall)
            {
                // Get the element type from the method's return type
                var returnType = innerMethodCall.Type;
                if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(IQueryable<>))
                {
                    innerTableName = returnType.GetGenericArguments()[0].Name;
                }
                else
                {
                    innerTableName = innerMethodCall.Method.GetGenericArguments().FirstOrDefault()?.Name;
                }
                
                // Generate subquery SQL from the method call expression - don't pass entityProvider
                var subVisitor = new SqlExpressionVisitor(_dialect, _parameters.Count > 0, null);
                innerSubQuery = subVisitor.GenerateSql(innerArg);
            }
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
            
            // Generate unique alias for the joined table
            var alias = $"t{_joinClauses.Count + 2}";
            
            // Determine the outer alias based on the outer key selector
            // For nested access like x.Dept.CompanyId, we need to find which table "Dept" refers to
            var outerAlias = ResolveOuterAlias(outerKeySelector.Body);

            _joinClauses.Add(new JoinClause
            {
                JoinType = joinType,
                TableName = innerTableName,
                SubQuery = innerSubQuery,
                Alias = alias,
                OnCondition = $"{_dialect.WrapColumn(outerAlias)}.{outerColumn} = {_dialect.WrapColumn(alias)}.{innerColumn}"
            });

            var resultSelector = GetLambda(node.Arguments[4]);
            if (resultSelector != null)
            {
                _selectColumns.Clear();
                _selectColumns.AddRange(_parser.ExtractColumns(resultSelector.Body));
                
                // Update property-to-alias mapping from result selector
                UpdatePropertyAliasMap(resultSelector, alias);
            }
        }

        /// <summary>
        /// Resolves the outer table alias from the outer key selector expression.
        /// For nested access like x.Dept.CompanyId, finds which table "Dept" refers to.
        /// </summary>
        private string ResolveOuterAlias(Expression expr)
        {
            // Default alias for the main table or first JOIN
            var defaultAlias = _subQuery != null ? "sq" : (_joinClauses.Count == 0 ? "t1" : $"t{_joinClauses.Count + 1}");
            
            // For simple member access like u.DepartmentId, use the default alias
            if (expr is MemberExpression m)
            {
                // Check if this is a nested access like x.Dept.CompanyId
                if (m.Expression is MemberExpression parentMember && parentMember.Expression is ParameterExpression)
                {
                    // parentMember.Member.Name is the property name (e.g., "Dept")
                    var propertyName = parentMember.Member.Name;
                    if (_propertyAliasMap.TryGetValue(propertyName, out var alias))
                    {
                        return alias;
                    }
                }
            }
            
            return defaultAlias;
        }

        /// <summary>
        /// Updates the property-to-alias mapping from a result selector expression.
        /// For expressions like (u, d) => new { User = u, Dept = d }, maps "User" -> outer alias, "Dept" -> inner alias.
        /// Also updates property-to-column mapping for GroupBy/OrderBy resolution.
        /// </summary>
        private void UpdatePropertyAliasMap(LambdaExpression lambda, string innerAlias)
        {
            if (lambda.Body is not NewExpression newExpr || newExpr.Members == null)
                return;

            var outerAlias = _subQuery != null ? "sq" : (_joinClauses.Count == 1 ? "t1" : $"t{_joinClauses.Count}");
            
            // Get the parameter names - first is outer, second is inner
            var outerParam = lambda.Parameters.Count > 0 ? lambda.Parameters[0].Name : null;
            var innerParam = lambda.Parameters.Count > 1 ? lambda.Parameters[1].Name : null;
            
            for (var i = 0; i < newExpr.Arguments.Count; i++)
            {
                var memberName = newExpr.Members[i].Name;
                var arg = newExpr.Arguments[i];
                
                // Check if this argument is a parameter (outer or inner table)
                if (arg is ParameterExpression paramExpr)
                {
                    // Check if this is the outer or inner parameter
                    if (paramExpr.Name == innerParam)
                    {
                        _propertyAliasMap[memberName] = innerAlias;
                    }
                    else
                    {
                        _propertyAliasMap[memberName] = outerAlias;
                    }
                }
                else if (arg is MemberExpression memberArg)
                {
                    // Check if it's accessing a property from the outer result (e.g., x.User)
                    if (memberArg.Expression is ParameterExpression)
                    {
                        // This is accessing a property from the previous result selector
                        // Check if we have a mapping for it
                        var sourcePropName = memberArg.Member.Name;
                        if (_propertyAliasMap.TryGetValue(sourcePropName, out var sourceAlias))
                        {
                            _propertyAliasMap[memberName] = sourceAlias;
                        }
                        else
                        {
                            // First JOIN - the outer parameter refers to t1
                            _propertyAliasMap[memberName] = outerAlias;
                        }
                    }
                    else
                    {
                        // This might be the inner table parameter
                        _propertyAliasMap[memberName] = innerAlias;
                    }
                    
                    // Also track the column mapping for GroupBy/OrderBy
                    // e.g., DeptName = d.Name -> "DeptName" maps to "[name]"
                    var columnName = _parser.GetColumnName(memberArg);
                    _propertyColumnMap[memberName] = columnName;
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
            else
                sb.Append('*');

            // FROM
            if (_subQuery != null)
            {
                sb.Append(" FROM (").Append(_subQuery).Append(") AS ").Append(_dialect.WrapColumn("sq"));
            }
            else if (_joinClauses.Count > 0)
            {
                // When there are JOINs, add alias to main table
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

                // Support subquery in JOIN
                if (!string.IsNullOrEmpty(join.SubQuery))
                {
                    sb.Append(" (").Append(join.SubQuery).Append(") AS ").Append(_dialect.WrapColumn(join.Alias));
                }
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
