// -----------------------------------------------------------------------
// <copyright file="ExpressionToSql.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Sqlx.Annotations;

namespace Sqlx
{
    /// <summary>
    /// 简单高效的 LINQ Expression 到 SQL 转换器，AOT 友好，无锁设计。
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    public partial class ExpressionToSql<T> : ExpressionToSqlBase
    {
        private readonly List<string> _setClausesConstant = new();
        private readonly List<string> _setClausesExpression = new();
        private readonly List<string> _insertColumns = new(); // INSERT列名
        private readonly List<List<string>> _insertValues = new(); // INSERT值（支持多行）
        private string? _insertSelectSql; // INSERT SELECT的SQL

        /// <summary>
        /// 使用指定的 SQL 方言初始化新实例。
        /// </summary>
        private ExpressionToSql(SqlDialect dialect) : base(dialect, typeof(T))
        {
        }

        /// <summary>
        /// 设置自定义的SELECT列。
        /// </summary>
        public ExpressionToSql<T> Select(params string[] columns)
        {
            _customSelectClause = columns?.ToList() ?? new List<string>();
            return this;
        }

        /// <summary>
        /// 添加 WHERE 条件到查询。
        /// </summary>
        public ExpressionToSql<T> Where(Expression<Func<T, bool>> predicate)
        {
            if (predicate != null)
            {
                var conditionSql = ParseExpression(predicate.Body);
                // 根据表达式复杂度决定是否添加括号
                if (NeedsParentheses(predicate.Body))
                {
                    _whereConditions.Add($"({conditionSql})");
                }
                else
                {
                    _whereConditions.Add(conditionSql);
                }
            }
            return this;
        }

        /// <summary>
        /// 添加 AND 条件到查询（等同于 Where）。
        /// </summary>
        public ExpressionToSql<T> And(Expression<Func<T, bool>> predicate) => Where(predicate);

        /// <summary>
        /// 添加 ORDER BY 子句。
        /// </summary>
        public ExpressionToSql<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            if (keySelector != null)
            {
                var columnName = GetColumnName(keySelector.Body);
                _orderByExpressions.Add(columnName + " ASC");
            }
            return this;
        }

        /// <summary>
        /// 添加 ORDER BY DESC 子句。
        /// </summary>
        public ExpressionToSql<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            if (keySelector != null)
            {
                var columnName = GetColumnName(keySelector.Body);
                _orderByExpressions.Add(columnName + " DESC");
            }
            return this;
        }

        /// <summary>
        /// 限制返回行数。
        /// </summary>
        public ExpressionToSql<T> Take(int count)
        {
            _take = count;
            return this;
        }

        /// <summary>
        /// 跳过指定行数。
        /// </summary>
        public ExpressionToSql<T> Skip(int count)
        {
            _skip = count;
            return this;
        }

        /// <summary>
        /// 设置 UPDATE 操作的值。支持模式如 a=1。
        /// </summary>
        public ExpressionToSql<T> Set<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        {
            if (selector != null)
            {
                var columnName = GetColumnName(selector.Body);
                var valueStr = FormatConstantValue(value);
                _setClausesConstant.Add($"{columnName} = {valueStr}");
            }
            return this;
        }

        /// <summary>
        /// 使用表达式设置 UPDATE 操作的值。支持模式如 a=a+1。
        /// </summary>
        public ExpressionToSql<T> Set<TValue>(Expression<Func<T, TValue>> selector,
            Expression<Func<T, TValue>> valueExpression)
        {
            if (selector != null && valueExpression != null)
            {
                var columnName = GetColumnName(selector.Body);
                var expressionSql = ParseExpression(valueExpression.Body);
                _setClausesExpression.Add($"{columnName} = {expressionSql}");
            }
            return this;
        }

        /// <summary>
        /// 指定 INSERT 操作的列。
        /// </summary>
        public ExpressionToSql<T> Insert(Expression<Func<T, object>> selector)
        {
            if (selector != null)
            {
                _insertColumns.Clear();
                // 解析列名
                var columns = ExtractColumns(selector.Body);
                _insertColumns.AddRange(columns);
            }
            return this;
        }

        /// <summary>
        /// 指定 INSERT 操作的值。
        /// </summary>
        public ExpressionToSql<T> Values(params object[] values)
        {
            if (values != null && values.Length > 0)
            {
                var valueStrings = new List<string>();
                foreach (var value in values)
                {
                    valueStrings.Add(FormatConstantValue(value));
                }
                _insertValues.Add(valueStrings);
            }
            return this;
        }

        /// <summary>
        /// 添加多行INSERT值。
        /// </summary>
        public ExpressionToSql<T> AddValues(params object[] values)
        {
            return Values(values);
        }

        /// <summary>
        /// 指定INSERT INTO操作，自动推断所有列。
        /// </summary>
        public ExpressionToSql<T> InsertInto()
        {
            // 清除之前的列配置，准备插入所有列
            _insertColumns.Clear();
            
            // 获取类型的所有公共属性作为列
            var properties = typeof(T).GetProperties();
            foreach (var prop in properties)
            {
                var columnName = _dialect.WrapColumn(prop.Name);
                _insertColumns.Add(columnName);
            }
            return this;
        }

        /// <summary>
        /// 指定INSERT INTO操作，手动指定列。
        /// </summary>
        public ExpressionToSql<T> InsertInto(Expression<Func<T, object>> selector)
        {
            return Insert(selector);
        }

        /// <summary>
        /// 使用SELECT子查询进行INSERT操作。
        /// </summary>
        public ExpressionToSql<T> InsertSelect(string selectSql)
        {
            _insertSelectSql = selectSql;
            return this;
        }

        /// <summary>
        /// 使用另一个ExpressionToSql的查询进行INSERT操作。
        /// </summary>
        public ExpressionToSql<T> InsertSelect<TSource>(ExpressionToSql<TSource> selectQuery)
        {
            if (selectQuery != null)
            {
                _insertSelectSql = selectQuery.ToSql();
            }
            return this;
        }

        /// <summary>
        /// 添加 GROUP BY 子句，返回分组查询对象。
        /// </summary>
        public GroupedExpressionToSql<T, TKey> GroupBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            if (keySelector != null)
            {
                var columnName = GetColumnName(keySelector.Body);
                _groupByExpressions.Add(columnName);
            }
            return new GroupedExpressionToSql<T, TKey>(this, keySelector);
        }

        /// <summary>
        /// 添加 GROUP BY 子句，返回正确的类型以支持链式调用。
        /// </summary>
        public new ExpressionToSql<T> AddGroupBy(string columnName)
        {
            base.AddGroupBy(columnName);
            return this;
        }

        /// <summary>
        /// 添加 HAVING 条件。
        /// </summary>
        public ExpressionToSql<T> Having(Expression<Func<T, bool>> predicate)
        {
            if (predicate != null)
            {
                var conditionSql = ParseExpression(predicate.Body);
                _havingConditions.Add($"({conditionSql})"); // HAVING总是加括号保持一致性
            }
            return this;
        }

        /// <summary>
        /// 设置自定义 SELECT 子句（内部使用）。
        /// </summary>
        internal List<string>? _customSelectClause;

        internal void SetCustomSelectClause(List<string> selectClause)
        {
            _customSelectClause = selectClause;
        }


        /// <summary>
        /// 构建 SQL 语句，简单直接无缓存。
        /// </summary>
        private string BuildSql()
        {
            // 优先级：INSERT > UPDATE > SELECT
            if (_insertColumns.Count > 0 || !string.IsNullOrEmpty(_insertSelectSql))
            {
                return BuildInsertSql();
            }
            // 根据 SET 子句的存在判断是 UPDATE 还是 SELECT
            if (_setClausesConstant.Count > 0 || _setClausesExpression.Count > 0)
            {
                return BuildUpdateSql();
            }
            return BuildSelectSql();
        }

        private string BuildSelectSql()
        {
            using var sql = new ValueStringBuilder(512);
            
            // SELECT 子句
            sql.Append(_customSelectClause?.Count > 0 
                ? $"SELECT {string.Join(", ", _customSelectClause)} FROM " 
                : "SELECT * FROM ");
            
            // FROM 表名
            sql.Append(_dialect.WrapColumn(_tableName!));

            // 添加通用 SQL 子句 (内联以避免ref struct传递问题)
            // WHERE子句
            if (_whereConditions.Count > 0)
            {
                sql.Append(" WHERE ");
                var processedWhere = _whereConditions.Select(RemoveOuterParentheses);
                sql.Append(string.Join(" AND ", processedWhere));
            }

            // GROUP BY子句
            if (_groupByExpressions.Count > 0)
            {
                sql.Append(" GROUP BY ");
                sql.Append(string.Join(", ", _groupByExpressions));
            }

            // HAVING子句
            if (_havingConditions.Count > 0)
            {
                sql.Append(" HAVING ");
                sql.Append(string.Join(" AND ", _havingConditions));
            }

            // ORDER BY子句
            if (_orderByExpressions.Count > 0)
            {
                sql.Append(" ORDER BY ");
                sql.Append(string.Join(", ", _orderByExpressions));
            }

            // 分页子句
            if (_skip.HasValue || _take.HasValue)
            {
                // SQL Server和Oracle使用OFFSET/FETCH语法 - 其他数据库都使用LIMIT/OFFSET
                var dbType = DatabaseType;
                var useOffsetFetchSyntax = dbType == "SqlServer" || dbType == "Oracle";

                if (useOffsetFetchSyntax) // SQL Server 或 Oracle
                {
                    if (_skip.HasValue) sql.Append($" OFFSET {_skip.Value} ROWS");
                    if (_take.HasValue) sql.Append($" FETCH NEXT {_take.Value} ROWS ONLY");
                }
                else // MySQL, PostgreSQL, SQLite 等
                {
                    if (_take.HasValue) sql.Append($" LIMIT {_take.Value}");
                    if (_skip.HasValue) sql.Append($" OFFSET {_skip.Value}");
                }
            }
            
            return sql.ToString();
        }


        private string BuildInsertSql()
        {
            using var sql = new ValueStringBuilder(512);
            sql.Append($"INSERT INTO {_dialect.WrapColumn(_tableName!)}");

            // 添加列名
            if (_insertColumns.Count > 0)
            {
                sql.Append($" ({string.Join(", ", _insertColumns)})");
            }

            // 添加数据源
            if (!string.IsNullOrEmpty(_insertSelectSql))
            {
                sql.Append($" {_insertSelectSql}");
            }
            else if (_insertValues.Count > 0)
            {
                sql.Append(" VALUES ");
                sql.Append(string.Join(", ", _insertValues.Select(values => $"({string.Join(", ", values)})")));
            }

            return sql.ToString();
        }

        private string BuildUpdateSql()
        {
            using var sql = new ValueStringBuilder(256);
            sql.Append($"UPDATE {_dialect.WrapColumn(_tableName!)} SET ");

            // 合并所有 SET 子句
            var allSetClauses = _setClausesConstant.Concat(_setClausesExpression);
            sql.Append(string.Join(", ", allSetClauses));

            // 添加 WHERE 子句
            if (_whereConditions.Count > 0)
            {
                sql.Append(" WHERE ");
                // UPDATE语句保留括号以确保正确的逻辑分组
                if (_whereConditions.Count == 1)
                {
                    var condition = RemoveOuterParentheses(_whereConditions[0]);
                    sql.Append($"({condition})");
                }
                else
                {
                    var processedWhere = _whereConditions.Select(RemoveOuterParentheses);
                    sql.Append("(");
                    sql.Append(string.Join(" AND ", processedWhere));
                    sql.Append(")");
                }
            }

            return sql.ToString();
        }

        /// <summary>
        /// 转换为 SQL 模板（简化版，无缓存）。
        /// </summary>
        public override SqlTemplate ToTemplate()
        {
            var sql = BuildSql();
            return new SqlTemplate(sql, _parameters.ToArray());
        }

        /// <summary>
        /// 转换为 SQL 字符串。
        /// </summary>
        public override string ToSql() => BuildSql();

        /// <summary>
        /// 生成 WHERE 子句部分。
        /// </summary>
        public string ToWhereClause() => _whereConditions.Count == 0 ? string.Empty : string.Join(" AND ", _whereConditions);

        /// <summary>
        /// 生成额外的子句（GROUP BY, HAVING, ORDER BY, LIMIT, OFFSET）。
        /// </summary>
        public string ToAdditionalClause()
        {
            using var sql = new ValueStringBuilder(256);
            
            // GROUP BY子句
            if (_groupByExpressions.Count > 0)
            {
                sql.Append(" GROUP BY ");
                sql.Append(string.Join(", ", _groupByExpressions));
            }
            
            // HAVING子句
            if (_havingConditions.Count > 0)
            {
                sql.Append(" HAVING ");
                sql.Append(string.Join(" AND ", _havingConditions));
            }
            
            // ORDER BY子句
            if (_orderByExpressions.Count > 0)
            {
                sql.Append(" ORDER BY ");
                sql.Append(string.Join(", ", _orderByExpressions));
            }
            
            // 分页子句
            if (_skip.HasValue || _take.HasValue)
            {
                var dbType = DatabaseType;
                var useOffsetFetchSyntax = dbType == "SqlServer" || dbType == "Oracle";

                if (useOffsetFetchSyntax) // SQL Server 或 Oracle
                {
                    if (_skip.HasValue) sql.Append($" OFFSET {_skip.Value} ROWS");
                    if (_take.HasValue) sql.Append($" FETCH NEXT {_take.Value} ROWS ONLY");
                }
                else // MySQL, PostgreSQL, SQLite 等
                {
                    if (_take.HasValue) sql.Append($" LIMIT {_take.Value}");
                    if (_skip.HasValue) sql.Append($" OFFSET {_skip.Value}");
                }
            }
            
            return sql.ToString().TrimStart(); // 移除开头的空格
        }


        /// <summary>
        /// 释放资源（简化版）。
        /// </summary>
        public override void Dispose()
        {
            base.Dispose();
            _setClausesConstant.Clear();
            _setClausesExpression.Clear();
            _insertColumns.Clear();
            _insertValues.Clear();
            _insertSelectSql = null;
        }
    }

    /// <summary>
    /// 表示分组后的查询对象，支持聚合操作。
    /// </summary>
    public class GroupedExpressionToSql<T, TKey> : ExpressionToSqlBase
    {
        private readonly ExpressionToSql<T> _baseQuery;
        private readonly string _keyColumnName;

        internal GroupedExpressionToSql(ExpressionToSql<T> baseQuery, Expression<Func<T, TKey>> keySelector)
            : base(baseQuery._dialect, typeof(T))
        {
            _baseQuery = baseQuery;
            _keyColumnName = keySelector != null ? ExtractColumnName(keySelector.Body) : string.Empty;
        }

        /// <summary>
        /// 选择分组结果的投影。
        /// </summary>
        public ExpressionToSql<TResult> Select<TResult>(Expression<Func<IGrouping<TKey, T>, TResult>> selector)
        {
            // 创建新的查询对象，使用相同的方言
            var resultQuery = _baseQuery._dialect.DatabaseType switch
            {
                "SqlServer" => ExpressionToSql<TResult>.ForSqlServer(),
                "MySQL" => ExpressionToSql<TResult>.ForMySql(),
                "PostgreSql" => ExpressionToSql<TResult>.ForPostgreSQL(),
                "Oracle" => ExpressionToSql<TResult>.ForOracle(),
                "DB2" => ExpressionToSql<TResult>.ForDB2(),
                "SQLite" => ExpressionToSql<TResult>.ForSqlite(),
                _ => ExpressionToSql<TResult>.ForSqlServer()
            };
            
            // 构建 SELECT 子句
            var selectClause = BuildSelectClause(selector.Body);
            resultQuery.SetCustomSelectClause(selectClause);
            
            // 复制基础查询的信息
            CopyBaseQueryInfo(resultQuery);
            
            return resultQuery;
        }

        /// <summary>
        /// 添加 HAVING 条件。
        /// </summary>
        public GroupedExpressionToSql<T, TKey> Having(Expression<Func<IGrouping<TKey, T>, bool>> predicate)
        {
            // 解析 HAVING 条件中的聚合函数
            var havingClause = ParseHavingExpression(predicate.Body);
            _baseQuery.AddHavingCondition(havingClause);
            return this;
        }

        private List<string> BuildSelectClause(Expression expression)
        {
            var selectClause = new List<string>();
            
            switch (expression)
            {
                case NewExpression newExpr:
                    // 处理 new { Key = g.Key, Count = g.Count() } 形式
                    for (int i = 0; i < newExpr.Arguments.Count; i++)
                    {
                        var arg = newExpr.Arguments[i];
                        var memberName = newExpr.Members?[i]?.Name ?? $"Column{i}";
                        var selectExpression = ParseSelectExpression(arg);
                        selectClause.Add($"{selectExpression} AS {memberName}");
                    }
                    break;
                    
                case MemberInitExpression memberInit:
                    // 处理 new TestUserResult { Id = g.Key, Count = g.Count() } 形式
                    foreach (var binding in memberInit.Bindings)
                    {
                        if (binding is MemberAssignment assignment)
                        {
                            var memberName = assignment.Member.Name;
                            var selectExpression = ParseSelectExpression(assignment.Expression);
                            selectClause.Add($"{selectExpression} AS {memberName}");
                        }
                    }
                    break;
                    
                default:
                    // 单个表达式
                    var expr = ParseSelectExpression(expression);
                    selectClause.Add(expr);
                    break;
            }
            
            return selectClause;
        }

        private string ParseSelectExpression(Expression expression)
        {
            switch (expression)
            {
                case MethodCallExpression methodCall:
                    return ParseAggregateFunction(methodCall);
                    
                case MemberExpression member when member.Expression is ParameterExpression param && param.Name == "g":
                    // g.Key 访问
                    if (member.Member.Name == "Key")
                    {
                        return _keyColumnName;
                    }
                    return "NULL";
                    
                case BinaryExpression binary:
                    // 处理二元表达式，例如 g.Key ?? 0
                    var left = ParseSelectExpression(binary.Left);
                    var right = ParseSelectExpression(binary.Right);
                    var op = binary.NodeType switch
                    {
                        ExpressionType.Coalesce => "COALESCE",
                        _ => binary.NodeType.ToString()
                    };
                    return binary.NodeType == ExpressionType.Coalesce 
                        ? $"COALESCE({left}, {right})" 
                        : $"{left} {op} {right}";
                        
                case ConstantExpression constant:
                    return FormatConstantValue(constant.Value);
                    
                default:
                    // 对于无法处理的表达式，尝试作为普通表达式解析
                    try
                    {
                        return ParseExpressionRaw(expression);
                    }
                    catch
                    {
                        return "NULL";
                    }
            }
        }

        private string ParseAggregateFunction(MethodCallExpression methodCall)
        {
            var methodName = methodCall.Method.Name;
            
            return methodName switch
            {
                "Count" => "COUNT(*)",
                "Sum" when methodCall.Arguments.Count > 1 => $"SUM({ExtractColumnNameFromLambda(methodCall.Arguments[1])})",
                "Average" or "Avg" when methodCall.Arguments.Count > 1 => $"AVG({ExtractColumnNameFromLambda(methodCall.Arguments[1])})",
                "Max" when methodCall.Arguments.Count > 1 => $"MAX({ExtractColumnNameFromLambda(methodCall.Arguments[1])})",
                "Min" when methodCall.Arguments.Count > 1 => $"MIN({ExtractColumnNameFromLambda(methodCall.Arguments[1])})",
                _ => throw new NotSupportedException($"聚合函数 {methodName} 不受支持")
            };
        }

        private string ExtractColumnNameFromLambda(Expression expression)
        {
            // 处理 lambda 表达式，如 x => x.Salary
            if (expression is LambdaExpression lambda)
            {
                return ExtractColumnName(lambda.Body);
            }
            
            // 处理包装在 UnaryExpression 中的 lambda 表达式
            if (expression is UnaryExpression unary && unary.NodeType == ExpressionType.Quote)
            {
                if (unary.Operand is LambdaExpression quotedLambda)
                {
                    return ExtractColumnName(quotedLambda.Body);
                }
            }
            
            return ExtractColumnName(expression);
        }

        private string ExtractColumnName(Expression expression)
        {
            // 使用正确的数据库方言格式
            if (expression is MemberExpression member)
            {
                return _dialect.WrapColumn(member.Member.Name);
            }
            if (expression is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
            {
                return ExtractColumnName(unary.Operand);
            }
            return expression.ToString();
        }

        private string ParseHavingExpression(Expression expression)
        {
            // 解析 HAVING 子句中的条件表达式，支持聚合函数
            switch (expression)
            {
                case BinaryExpression binary:
                    return ParseHavingBinaryExpression(binary);
                case MethodCallExpression methodCall:
                    return ParseAggregateFunction(methodCall);
                default:
                    return expression.ToString();
            }
        }

        private string ParseHavingBinaryExpression(BinaryExpression binary)
        {
            var left = ParseHavingExpressionPart(binary.Left);
            var right = ParseHavingExpressionPart(binary.Right);
            var op = GetBinaryOperator(binary.NodeType);
            return $"{left} {op} {right}";
        }

        private string ParseHavingExpressionPart(Expression expression)
        {
            switch (expression)
            {
                case MethodCallExpression methodCall:
                    return ParseAggregateFunction(methodCall);
                case ConstantExpression constant:
                    return constant.Value?.ToString() ?? "NULL";
                case MemberExpression member when member.Expression is ParameterExpression param && param.Name == "g":
                    if (member.Member.Name == "Key")
                    {
                        return _keyColumnName;
                    }
                    break;
            }
            
            return expression.ToString();
        }

        private void CopyBaseQueryInfo<TResult>(ExpressionToSql<TResult> resultQuery)
        {
            // 复制表名 - 使用原始表名而不是结果类型名
            resultQuery.SetTableName(typeof(T).Name);
            
            // 复制 WHERE 条件
            resultQuery.CopyWhereConditions(_baseQuery.GetWhereConditions());
            
            // 确保包含 GROUP BY 子句
            if (!string.IsNullOrEmpty(_keyColumnName))
            {
                ((ExpressionToSqlBase)resultQuery).AddGroupByColumn(_keyColumnName);
            }
            
            // 复制 HAVING 条件
            resultQuery.CopyHavingConditions(_baseQuery.GetHavingConditions());
        }

        public override string ToSql()
        {
            return _baseQuery.ToSql();
        }

        public override SqlTemplate ToTemplate()
        {
            return _baseQuery.ToTemplate();
        }

        public override void Dispose()
        {
            base.Dispose();
            _baseQuery?.Dispose();
        }
    }

    /// <summary>
    /// 表示分组的接口，类似于 LINQ 的 IGrouping。
    /// </summary>
    public interface IGrouping<out TKey, out TElement>
    {
        TKey Key { get; }
    }

    /// <summary>
    /// 为 IGrouping 提供聚合扩展方法。
    /// </summary>
    public static class GroupingExtensions
    {
        public static int Count<TKey, TElement>(this IGrouping<TKey, TElement> grouping)
        {
            throw new NotImplementedException("此方法仅用于表达式树解析，不应被直接调用");
        }

        public static TResult Sum<TKey, TElement, TResult>(this IGrouping<TKey, TElement> grouping, Expression<Func<TElement, TResult>> selector)
        {
            throw new NotImplementedException("此方法仅用于表达式树解析，不应被直接调用");
        }

        public static double Average<TKey, TElement>(this IGrouping<TKey, TElement> grouping, Expression<Func<TElement, double>> selector)
        {
            throw new NotImplementedException("此方法仅用于表达式树解析，不应被直接调用");
        }

        public static double Average<TKey, TElement>(this IGrouping<TKey, TElement> grouping, Expression<Func<TElement, decimal>> selector)
        {
            throw new NotImplementedException("此方法仅用于表达式树解析，不应被直接调用");
        }

        public static TResult Max<TKey, TElement, TResult>(this IGrouping<TKey, TElement> grouping, Expression<Func<TElement, TResult>> selector)
        {
            throw new NotImplementedException("此方法仅用于表达式树解析，不应被直接调用");
        }

        public static TResult Min<TKey, TElement, TResult>(this IGrouping<TKey, TElement> grouping, Expression<Func<TElement, TResult>> selector)
        {
            throw new NotImplementedException("此方法仅用于表达式树解析，不应被直接调用");
        }
    }
}