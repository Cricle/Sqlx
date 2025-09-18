// -----------------------------------------------------------------------
// <copyright file="ExpressionToSql.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Sqlx.Annotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Linq;

namespace Sqlx
{
    /// <summary>
    /// SQL operation type enumeration
    /// </summary>
    public enum SqlOperation
    {
        /// <summary>SELECT query</summary>
        Select,
        /// <summary>INSERT operation</summary>
        Insert,
        /// <summary>UPDATE operation</summary>
        Update,
        /// <summary>DELETE operation</summary>
        Delete
    }

    /// <summary>
    /// Any placeholder class for SqlTemplate
    /// </summary>
    public static class Any
    {
        /// <summary>
        /// Placeholder for any value type with auto-generated parameter name
        /// </summary>
        /// <typeparam name="TValue">Placeholder type</typeparam>
        /// <returns>Placeholder value</returns>
        public static TValue Value<TValue>() => default!;

        /// <summary>
        /// Placeholder for any value type with custom parameter name
        /// </summary>
        /// <typeparam name="TValue">Placeholder type</typeparam>
        public static TValue Value<TValue>(string parameterName) => default!;

        /// <summary>
        /// String placeholder with auto-generated parameter name
        /// </summary>
        /// <returns>String placeholder</returns>
        public static string String() => default!;

        /// <summary>
        /// String placeholder with custom parameter name
        /// </summary>
        /// <param name="parameterName">Custom parameter name</param>
        /// <returns>String placeholder</returns>
        public static string String(string parameterName) => default!;

        /// <summary>
        /// 整数占位符（自动生成参数名）
        /// </summary>
        /// <returns>整数占位符</returns>
        public static int Int() => default;

        /// <summary>
        /// 整数占位符（指定参数名）
        /// </summary>
        /// <returns>整数占位符</returns>
        public static int Int(string parameterName) => default;

        /// <summary>
        /// 布尔占位符（自动生成参数名）
        /// </summary>
        /// <returns>布尔占位符</returns>
        public static bool Bool() => default(bool);

        /// <summary>
        /// 布尔占位符（指定参数名）
        /// </summary>
        /// <returns>布尔占位符</returns>
        public static bool Bool(string parameterName) => default(bool);

        /// <summary>
        /// 日期时间占位符（自动生成参数名）
        /// </summary>
        /// <returns>日期时间占位符</returns>
        public static DateTime DateTime() => default(DateTime);

        /// <summary>
        /// 日期时间占位符（指定参数名）
        /// </summary>
        /// <returns>日期时间占位符</returns>
        public static DateTime DateTime(string parameterName) => default(DateTime);

        /// <summary>
        /// Guid占位符（自动生成参数名）
        /// </summary>
        /// <returns>Guid占位符</returns>
        public static Guid Guid() => default(Guid);

        /// <summary>
        /// Guid占位符（指定参数名）
        /// </summary>
        /// <returns>Guid占位符</returns>
        public static Guid Guid(string parameterName) => default(Guid);
    }

    /// <summary>
    /// Simple and efficient LINQ Expression to SQL converter (AOT-friendly, lock-free design)
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public partial class ExpressionToSql<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] 
#endif
        T> : ExpressionToSqlBase
    {
        private readonly List<string> _sets = new();
        private readonly List<string> _expressions = new();
        private readonly List<string> _columns = new(); // INSERT列名
        private readonly List<List<string>> _values = new(); // INSERT值（支持多行）
        private string? _selectSql; // INSERT SELECT的SQL
        private SqlOperation _operation = SqlOperation.Select; // 默认为SELECT操作

        /// <summary>
        /// 使用指定的 SQL 方言初始化新实例。
        /// </summary>
        private ExpressionToSql(SqlDialect dialect) : base(dialect, typeof(T))
        {
        }

        /// <summary>
        /// 设置自定义的SELECT列。
        /// </summary>
        public ExpressionToSql<T> Select(params string[] cols)
        {
            _custom = cols?.ToList() ?? new List<string>();
            return this;
        }

        /// <summary>
        /// 使用表达式设置SELECT列。
        /// </summary>
        public ExpressionToSql<T> Select<TResult>(Expression<Func<T, TResult>> selector)
        {
            _custom = ExtractColumnsFromSelector(selector);
            return this;
        }

        /// <summary>
        /// 使用多个表达式设置SELECT列。
        /// </summary>
        public ExpressionToSql<T> Select(params Expression<Func<T, object>>[] selectors)
        {
            _custom = ExtractColumnsFromSelectors(selectors);
            return this;
        }

        /// <summary>
        /// 统一的列提取逻辑，避免重复代码
        /// </summary>
        private List<string> ExtractColumnsFromSelector<TResult>(Expression<Func<T, TResult>>? selector)
        {
            return selector != null ? ExtractColumns(selector.Body) : new List<string>();
        }

        /// <summary>
        /// 统一的多选择器列提取逻辑
        /// </summary>
        private List<string> ExtractColumnsFromSelectors(Expression<Func<T, object>>[]? selectors)
        {
            if (selectors == null || selectors.Length == 0) return new List<string>(0);

            return selectors.Where(s => s != null).SelectMany(s => ExtractColumns(s.Body)).ToList();
        }

        /// <summary>
        /// 添加 WHERE 条件到查询。
        /// </summary>
        public ExpressionToSql<T> Where(Expression<Func<T, bool>> predicate)
        {
            if (predicate != null)
            {
                var conditionSql = ParseExpression(predicate.Body);
                _whereConditions.Add(NeedsParentheses(predicate.Body) ? $"({conditionSql})" : conditionSql);
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
        public ExpressionToSql<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector) => AddOrderBy(keySelector, "ASC");

        /// <summary>
        /// 添加 ORDER BY DESC 子句。
        /// </summary>
        public ExpressionToSql<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector) => AddOrderBy(keySelector, "DESC");

        /// <summary>
        /// 添加排序表达式的通用方法。
        /// </summary>
        private ExpressionToSql<T> AddOrderBy<TKey>(Expression<Func<T, TKey>>? keySelector, string direction)
        {
            if (keySelector != null)
            {
                var columnName = GetColumnName(keySelector.Body);
                _orderByExpressions.Add($"{columnName} {direction}");
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
            EnsureUpdateMode();
            if (selector != null)
            {
                var column = GetColumnName(selector.Body);
                var valueStr = FormatConstantValue(value);
                _sets.Add($"{column} = {valueStr}");
            }
            return this;
        }

        /// <summary>
        /// 使用表达式设置 UPDATE 操作的值。支持模式如 a=a+1。
        /// </summary>
        public ExpressionToSql<T> Set<TValue>(Expression<Func<T, TValue>> selector,
            Expression<Func<T, TValue>> valueExpression)
        {
            EnsureUpdateMode();
            if (selector != null && valueExpression != null)
            {
                var column = GetColumnName(selector.Body);
                var sql = ParseExpression(valueExpression.Body);
                _expressions.Add($"{column} = {sql}");
            }
            return this;
        }

        /// <summary>
        /// 统一的操作类型设置，避免重复代码
        /// </summary>
        private void SetOperationType(SqlOperation type) => _operation = type;

        private void EnsureUpdateMode() => SetOperationType(SqlOperation.Update);
        private void EnsureInsertMode() => SetOperationType(SqlOperation.Insert);
        private void EnsureDeleteMode() => SetOperationType(SqlOperation.Delete);

        /// <summary>
        /// 创建 INSERT 操作。
        /// </summary>
        public ExpressionToSql<T> Insert()
        {
            EnsureInsertMode();
            return this;
        }

        /// <summary>
        /// 指定 INSERT 操作的列。
        /// </summary>
        public ExpressionToSql<T> Insert(Expression<Func<T, object>> selector)
        {
            EnsureInsertMode();
            SetInsertColumns(selector);
            return this;
        }

        /// <summary>
        /// 统一的INSERT列设置逻辑
        /// </summary>
        private void SetInsertColumns(Expression<Func<T, object>>? selector)
        {
            _columns.Clear();
            if (selector != null)
            {
                _columns.AddRange(ExtractColumns(selector.Body));
            }
        }

        /// <summary>
        /// 指定 INSERT 操作的值。
        /// </summary>
        public ExpressionToSql<T> Values(params object[] values)
        {
            AddFormattedValues(values);
            return this;
        }

        /// <summary>
        /// 添加多行INSERT值。
        /// </summary>
        public ExpressionToSql<T> AddValues(params object[] values) => Values(values);

        /// <summary>
        /// 统一的值格式化和添加逻辑
        /// </summary>
        private void AddFormattedValues(object[]? values)
        {
            if (values?.Length > 0)
            {
                var strings = values.Select(FormatConstantValue).ToList();
                _values.Add(strings);
            }
        }

        /// <summary>
        /// 指定INSERT INTO操作，需要显式指定列（AOT 友好）。
        /// </summary>
        /// <param name="selector">列选择表达式</param>
        public ExpressionToSql<T> InsertInto(Expression<Func<T, object>> selector)
        {
            EnsureInsertMode();
            SetInsertColumns(selector);
            return this;
        }

        /// <summary>
        /// 指定INSERT INTO操作，自动推断所有列（使用反射，不推荐在 AOT 中使用）。
        /// </summary>
        public ExpressionToSql<T> InsertIntoAll()
        {
            EnsureInsertMode();
            _columns.Clear();
            _columns.AddRange(typeof(T).GetProperties().Select(prop => _dialect.WrapColumn(prop.Name)));
            return this;
        }


        /// <summary>
        /// 使用SELECT子查询进行INSERT操作。
        /// </summary>
        public ExpressionToSql<T> InsertSelect(string sql)
        {
            _selectSql = sql;
            return this;
        }

        /// <summary>
        /// 使用另一个ExpressionToSql的查询进行INSERT操作。
        /// </summary>
        public ExpressionToSql<T> InsertSelect<TSource>(ExpressionToSql<TSource> query)
        {
            if (query != null)
            {
                _selectSql = query.ToSql();
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
            return new GroupedExpressionToSql<T, TKey>(this, keySelector!);
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
        /// 创建DELETE语句。必须配合WHERE使用以确保安全。
        /// </summary>
        public ExpressionToSql<T> Delete()
        {
            EnsureDeleteMode();
            return this;
        }

        /// <summary>
        /// 创建DELETE语句并添加WHERE条件。
        /// </summary>
        public ExpressionToSql<T> Delete(Expression<Func<T, bool>> predicate)
        {
            EnsureDeleteMode();
            return Where(predicate);
        }

        /// <summary>
        /// 创建UPDATE语句。
        /// </summary>
        public ExpressionToSql<T> Update()
        {
            EnsureUpdateMode();
            return this;
        }

        /// <summary>
        /// 设置自定义 SELECT 子句（内部使用）。
        /// </summary>
        internal List<string>? _custom;

        internal void SetCustomSelectClause(List<string> clause)
        {
            _custom = clause;
        }


        /// <summary>
        /// 构建 SQL 语句，简单直接无缓存。
        /// </summary>
        private string BuildSql()
        {
            return _operation switch
            {
                SqlOperation.Insert => BuildInsertSql(),
                SqlOperation.Update => BuildUpdateSql(),
                SqlOperation.Delete => BuildDeleteSql(),
                SqlOperation.Select => BuildSelectSql(),
                _ => BuildSelectSql()
            };
        }

        private string BuildSelectSql()
        {
            var sql = new System.Text.StringBuilder(512);

            // SELECT 子句
            var selectClause = _custom?.Count > 0
                ? $"SELECT {string.Join(", ", _custom)} FROM "
                : "SELECT * FROM ";
            sql.Append(selectClause);

            // FROM 表名
            sql.Append(_dialect.WrapColumn(_tableName!));

            // WHERE 子句
            if (_whereConditions.Count > 0)
            {
                sql.Append(" WHERE ");
                var conditions = _whereConditions.Select(RemoveOuterParentheses);
                sql.Append(string.Join(" AND ", conditions));
            }

            // GROUP BY 子句
            if (_groupByExpressions.Count > 0)
            {
                sql.Append(" GROUP BY ");
                sql.Append(string.Join(", ", _groupByExpressions));
            }

            // HAVING 子句
            if (_havingConditions.Count > 0)
            {
                sql.Append(" HAVING ");
                sql.Append(string.Join(" AND ", _havingConditions));
            }

            // ORDER BY 子句
            if (_orderByExpressions.Count > 0)
            {
                sql.Append(" ORDER BY ");
                sql.Append(string.Join(", ", _orderByExpressions));
            }

            // LIMIT/OFFSET 子句
            if (_skip.HasValue || _take.HasValue)
            {
                if (_dialect.DatabaseType == "SqlServer")
                {
                    // SQL Server 使用 OFFSET...FETCH
                    if (_skip.HasValue)
                    {
                        sql.Append($" OFFSET {_skip.Value} ROWS");
                        if (_take.HasValue)
                        {
                            sql.Append($" FETCH NEXT {_take.Value} ROWS ONLY");
                        }
                    }
                    else if (_take.HasValue)
                    {
                        sql.Append($" OFFSET 0 ROWS FETCH NEXT {_take.Value} ROWS ONLY");
                    }
                }
                else
                {
                    // 其他数据库使用 LIMIT/OFFSET
                    if (_take.HasValue)
                    {
                        sql.Append($" LIMIT {_take.Value}");
                    }
                    if (_skip.HasValue)
                    {
                        sql.Append($" OFFSET {_skip.Value}");
                    }
                }
            }

            return sql.ToString();
        }



        private string BuildInsertSql()
        {
            using var sql = new ValueStringBuilder(512);
            sql.Append($"INSERT INTO {_dialect.WrapColumn(_tableName!)}");

            // 添加列名
            if (_columns.Count > 0)
            {
                sql.Append($" ({string.Join(", ", _columns)})");
            }

            // 添加数据源
            if (!string.IsNullOrEmpty(_selectSql))
            {
                sql.Append($" {_selectSql}");
            }
            else if (_values.Count > 0)
            {
                sql.Append(" VALUES ");
                sql.Append(string.Join(", ", _values.Select(vals => $"({string.Join(", ", vals)})")));
            }

            return sql.ToString();
        }

        private string BuildUpdateSql()
        {
            using var sql = new ValueStringBuilder(256);
            sql.Append($"UPDATE {_dialect.WrapColumn(_tableName!)} SET ");

            // 合并所有 SET 子句
            var allClauses = _sets.Concat(_expressions);
            sql.Append(string.Join(", ", allClauses));

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
        /// 构建DELETE SQL语句。
        /// </summary>
        private string BuildDeleteSql()
        {
            using var sql = new ValueStringBuilder(256);

            // DELETE FROM 表名
            sql.Append("DELETE FROM ");
            sql.Append(_dialect.WrapColumn(_tableName!));

            // WHERE子句 - DELETE必须有WHERE条件以确保安全
            if (_whereConditions.Count > 0)
            {
                sql.Append(" WHERE ");
                var processedWhere = _whereConditions.Select(RemoveOuterParentheses);
                sql.Append(string.Join(" AND ", processedWhere));
            }
            else
            {
                throw new InvalidOperationException("DELETE operation requires WHERE clause for safety. Use Delete(predicate) or call Where() before Delete().");
            }

            return sql.ToString();
        }

        /// <summary>
        /// 启用参数化查询模式，用于生成SqlTemplate
        /// </summary>
        public ExpressionToSql<T> UseParameterizedQueries()
        {
            _parameterized = true;
            return this;
        }

        /// <summary>
        /// 转换为 SQL 模板。如果未启用参数化模式，将自动启用并重新构建查询。
        /// </summary>
        public override SqlTemplate ToTemplate()
        {
            // 如果还没有启用参数化查询，需要重新构建
            if (!_parameterized)
            {
                // 保存当前状态
                var conditions = new List<string>(_whereConditions);
                var orderBy = new List<string>(_orderByExpressions);
                var groupBy = new List<string>(_groupByExpressions);
                var having = new List<string>(_havingConditions);

                // 清空并重新启用参数化模式
                _whereConditions.Clear();
                _orderByExpressions.Clear();
                _groupByExpressions.Clear();
                _havingConditions.Clear();
                _parameters.Clear();
                _parameterized = true;

                // 需要重新构建查询，但这需要原始表达式
                // 由于我们没有保存原始表达式，我们只能返回当前的SQL和空参数
                // 这是一个设计限制，建议用户显式调用UseParameterizedQueries()
                _parameterized = false;
                _whereConditions.AddRange(conditions);
                _orderByExpressions.AddRange(orderBy);
                _groupByExpressions.AddRange(groupBy);
                _havingConditions.AddRange(having);
            }

            var sql = BuildSql();
            return new SqlTemplate(sql, _parameters);
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
    }

    /// <summary>
    /// 表示分组后的查询对象，支持聚合操作。
    /// </summary>
    public class GroupedExpressionToSql<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] 
#endif
        T, TKey> : ExpressionToSqlBase
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
        public ExpressionToSql<TResult> Select<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] 
#endif
            TResult>(Expression<Func<IGrouping<TKey, T>, TResult>> selector)
        {
            // 创建新的查询对象，使用相同的方言
            var resultQuery = _baseQuery._dialect.DatabaseType switch
            {
                "SqlServer" => ExpressionToSql<TResult>.ForSqlServer(),
                "MySql" => ExpressionToSql<TResult>.ForMySql(),
                "PostgreSql" => ExpressionToSql<TResult>.ForPostgreSQL(),
                "SQLite" => ExpressionToSql<TResult>.ForSqlite(),
                "Oracle" => ExpressionToSql<TResult>.ForOracle(),
                "DB2" => ExpressionToSql<TResult>.ForDB2(),
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
                    // 处理二元表达式，例如 g.Key ?? 0 或复杂的算术表达式
                    var left = ParseSelectExpression(binary.Left);
                    var right = ParseSelectExpression(binary.Right);
                    var op = binary.NodeType switch
                    {
                        ExpressionType.Coalesce => "COALESCE",
                        ExpressionType.Add => "+",
                        ExpressionType.Subtract => "-",
                        ExpressionType.Multiply => "*",
                        ExpressionType.Divide => "/",
                        ExpressionType.Equal => "=",
                        ExpressionType.NotEqual => "<>",
                        ExpressionType.GreaterThan => ">",
                        ExpressionType.GreaterThanOrEqual => ">=",
                        ExpressionType.LessThan => "<",
                        ExpressionType.LessThanOrEqual => "<=",
                        _ => binary.NodeType.ToString()
                    };
                    return binary.NodeType == ExpressionType.Coalesce
                        ? $"COALESCE({left}, {right})"
                        : $"({left} {op} {right})";

                case ConstantExpression constant:
                    return FormatConstantValue(constant.Value);

                case ConditionalExpression conditional:
                    // 处理三元运算符 condition ? ifTrue : ifFalse
                    var test = ParseSelectExpression(conditional.Test);
                    var ifTrue = ParseSelectExpression(conditional.IfTrue);
                    var ifFalse = ParseSelectExpression(conditional.IfFalse);
                    return $"CASE WHEN {test} THEN {ifTrue} ELSE {ifFalse} END";

                case UnaryExpression unary when unary.NodeType == ExpressionType.Convert:
                    // 处理类型转换
                    return ParseSelectExpression(unary.Operand);

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
                "Sum" when methodCall.Arguments.Count > 1 => $"SUM({ParseLambdaExpressionEnhanced(methodCall.Arguments[1])})",
                "Average" or "Avg" when methodCall.Arguments.Count > 1 => $"AVG({ParseLambdaExpressionEnhanced(methodCall.Arguments[1])})",
                "Max" when methodCall.Arguments.Count > 1 => $"MAX({ParseLambdaExpressionEnhanced(methodCall.Arguments[1])})",
                "Min" when methodCall.Arguments.Count > 1 => $"MIN({ParseLambdaExpressionEnhanced(methodCall.Arguments[1])})",
                _ => throw new NotSupportedException($"聚合函数 {methodName} 不受支持")
            };
        }

        /// <summary>
        /// 增强的Lambda表达式解析，支持复杂的嵌套函数和表达式
        /// </summary>
        private string ParseLambdaExpressionEnhanced(Expression expression) => expression switch
        {
            LambdaExpression lambda => ParseLambdaBody(lambda.Body),
            UnaryExpression { NodeType: ExpressionType.Quote } unary when unary.Operand is LambdaExpression quotedLambda => ParseLambdaBody(quotedLambda.Body),
            _ => ParseLambdaBody(expression),
        };

        /// <summary>
        /// 解析Lambda表达式的Body部分，支持嵌套函数
        /// </summary>
        private string ParseLambdaBody(Expression body)
        {
            switch (body)
            {
                case MemberExpression member:
                    // Handle special properties like string.Length
                    if (member.Member.Name == "Length" && member.Member.DeclaringType == typeof(string))
                    {
                        var obj = ParseLambdaBody(member.Expression!);
                        return DatabaseType == "SqlServer" ? $"LEN({obj})" : $"LENGTH({obj})";
                    }
                    return ExtractColumnName(member);

                case BinaryExpression binary:
                    // 支持算术表达式和空值合并，如 x.Salary * 1.2, x.Bonus ?? 0
                    var left = ParseLambdaBody(binary.Left);
                    var right = ParseLambdaBody(binary.Right);
                    var op = binary.NodeType switch
                    {
                        ExpressionType.Add => "+",
                        ExpressionType.Subtract => "-",
                        ExpressionType.Multiply => "*",
                        ExpressionType.Divide => "/",
                        ExpressionType.Modulo => "%",
                        ExpressionType.Coalesce => "COALESCE",
                        ExpressionType.GreaterThan => ">",
                        ExpressionType.GreaterThanOrEqual => ">=",
                        ExpressionType.LessThan => "<",
                        ExpressionType.LessThanOrEqual => "<=",
                        ExpressionType.Equal => "=",
                        ExpressionType.NotEqual => "!=",
                        _ => "+"
                    };
                    return binary.NodeType == ExpressionType.Coalesce
                        ? $"COALESCE({left}, {right})"
                        : $"({left} {op} {right})";

                case MethodCallExpression methodCall:
                    // 支持嵌套函数调用，如 Math.Round(x.Salary, 2)
                    return ParseMethodCallInAggregate(methodCall);

                case ConstantExpression constant:
                    return FormatConstantValue(constant.Value);

                case ConditionalExpression conditional:
                    // 支持条件表达式，如 x.IsActive ? x.Salary : 0
                    var test = ParseLambdaBody(conditional.Test);
                    var ifTrue = ParseLambdaBody(conditional.IfTrue);
                    var ifFalse = ParseLambdaBody(conditional.IfFalse);
                    return $"CASE WHEN {test} THEN {ifTrue} ELSE {ifFalse} END";

                case UnaryExpression unary when unary.NodeType == ExpressionType.Convert:
                    return ParseLambdaBody(unary.Operand);

                default:
                    // 回退到简单的列名提取
                    try
                    {
                        return ExtractColumnName(body);
                    }
                    catch
                    {
                        return "NULL";
                    }
            }
        }

        /// <summary>
        /// 在聚合函数上下文中解析方法调用
        /// </summary>
        private string ParseMethodCallInAggregate(MethodCallExpression methodCall)
        {
            var methodName = methodCall.Method.Name;
            var declaringType = methodCall.Method.DeclaringType;

            // 数学函数
            if (declaringType == typeof(Math))
            {
                var args = methodCall.Arguments.Select(ParseLambdaBody).ToArray();
                return (methodName, args.Length) switch
                {
                    ("Abs", 1) => $"ABS({args[0]})",
                    ("Round", 1) => $"ROUND({args[0]})",
                    ("Round", 2) => $"ROUND({args[0]}, {args[1]})",
                    ("Floor", 1) => $"FLOOR({args[0]})",
                    ("Ceiling", 1) => DatabaseType == "PostgreSql" ? $"CEIL({args[0]})" : $"CEILING({args[0]})",
                    ("Min", 2) => $"LEAST({args[0]}, {args[1]})",
                    ("Max", 2) => $"GREATEST({args[0]}, {args[1]})",
                    ("Pow", 2) => DatabaseType == "MySql" ? $"POW({args[0]}, {args[1]})" : $"POWER({args[0]}, {args[1]})",
                    ("Sqrt", 1) => $"SQRT({args[0]})",
                    _ => args.Length > 0 ? args[0] : "NULL"
                };
            }

            // 字符串函数
            if (declaringType == typeof(string) && methodCall.Object != null)
            {
                var obj = ParseLambdaBody(methodCall.Object);
                var args = methodCall.Arguments.Select(ParseLambdaBody).ToArray();
                return (methodName, args.Length) switch
                {
                    ("Length", 0) => DatabaseType == "SqlServer" ? $"LEN({obj})" : $"LENGTH({obj})",
                    ("ToUpper", 0) => $"UPPER({obj})",
                    ("ToLower", 0) => $"LOWER({obj})",
                    ("Trim", 0) => $"TRIM({obj})",
                    ("Substring", 1) => DatabaseType == "SQLite" ? $"SUBSTR({obj}, {args[0]})" : $"SUBSTRING({obj}, {args[0]})",
                    ("Substring", 2) => DatabaseType == "SQLite" ? $"SUBSTR({obj}, {args[0]}, {args[1]})" : $"SUBSTRING({obj}, {args[0]}, {args[1]})",
                    ("Replace", 2) => $"REPLACE({obj}, {args[0]}, {args[1]})",
                    _ => obj
                };
            }

            // 其他情况，回退到基础解析
            return methodCall.Object != null ? ParseLambdaBody(methodCall.Object) : "NULL";
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
                resultQuery.AddGroupByColumn(_keyColumnName);
            }

            // 复制 HAVING 条件
            resultQuery.CopyHavingConditions(_baseQuery.GetHavingConditions());
        }

        /// <summary>
        /// 转换为SQL查询字符串。
        /// </summary>
        /// <returns>SQL查询字符串。</returns>
        public override string ToSql()
        {
            return _baseQuery.ToSql();
        }

        /// <summary>
        /// 转换为SQL模板。
        /// </summary>
        /// <returns>SQL模板实例。</returns>
        public override SqlTemplate ToTemplate()
        {
            return _baseQuery.ToTemplate();
        }

        /// <summary>
        /// 释放资源，清理内部查询对象。
        /// </summary>
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
        /// <summary>
        /// 获取分组的键值。
        /// </summary>
        TKey Key { get; }
    }

    /// <summary>
    /// 为 IGrouping 提供聚合扩展方法。
    /// </summary>
    public static class GroupingExtensions
    {
        /// <summary>
        /// 计算分组中元素的数量。
        /// </summary>
        /// <typeparam name="TKey">分组键的类型。</typeparam>
        /// <typeparam name="TElement">元素的类型。</typeparam>
        /// <param name="grouping">分组对象。</param>
        /// <returns>元素数量。</returns>
        public static int Count<TKey, TElement>(this IGrouping<TKey, TElement> grouping)
        {
            throw new NotImplementedException("此方法仅用于表达式树解析，不应被直接调用");
        }

        /// <summary>
        /// 计算分组中元素的总和。
        /// </summary>
        /// <typeparam name="TKey">分组键的类型。</typeparam>
        /// <typeparam name="TElement">元素的类型。</typeparam>
        /// <typeparam name="TResult">结果的类型。</typeparam>
        /// <param name="grouping">分组对象。</param>
        /// <param name="selector">选择器表达式。</param>
        /// <returns>总和值。</returns>
        public static TResult Sum<TKey, TElement, TResult>(this IGrouping<TKey, TElement> grouping, Expression<Func<TElement, TResult>> selector)
        {
            throw new NotImplementedException("此方法仅用于表达式树解析，不应被直接调用");
        }

        /// <summary>
        /// 计算分组中元素的平均值（double类型）。
        /// </summary>
        /// <typeparam name="TKey">分组键的类型。</typeparam>
        /// <typeparam name="TElement">元素的类型。</typeparam>
        /// <param name="grouping">分组对象。</param>
        /// <param name="selector">选择器表达式。</param>
        /// <returns>平均值。</returns>
        public static double Average<TKey, TElement>(this IGrouping<TKey, TElement> grouping, Expression<Func<TElement, double>> selector)
        {
            throw new NotImplementedException("此方法仅用于表达式树解析，不应被直接调用");
        }

        /// <summary>
        /// 计算分组中元素的平均值（decimal类型）。
        /// </summary>
        /// <typeparam name="TKey">分组键的类型。</typeparam>
        /// <typeparam name="TElement">元素的类型。</typeparam>
        /// <param name="grouping">分组对象。</param>
        /// <param name="selector">选择器表达式。</param>
        /// <returns>平均值。</returns>
        public static double Average<TKey, TElement>(this IGrouping<TKey, TElement> grouping, Expression<Func<TElement, decimal>> selector)
        {
            throw new NotImplementedException("此方法仅用于表达式树解析，不应被直接调用");
        }

        /// <summary>
        /// 获取分组中元素的最大值。
        /// </summary>
        /// <typeparam name="TKey">分组键的类型。</typeparam>
        /// <typeparam name="TElement">元素的类型。</typeparam>
        /// <typeparam name="TResult">结果的类型。</typeparam>
        /// <param name="grouping">分组对象。</param>
        /// <param name="selector">选择器表达式。</param>
        /// <returns>最大值。</returns>
        public static TResult Max<TKey, TElement, TResult>(this IGrouping<TKey, TElement> grouping, Expression<Func<TElement, TResult>> selector)
        {
            throw new NotImplementedException("此方法仅用于表达式树解析，不应被直接调用");
        }

        /// <summary>
        /// 获取分组中元素的最小值。
        /// </summary>
        /// <typeparam name="TKey">分组键的类型。</typeparam>
        /// <typeparam name="TElement">元素的类型。</typeparam>
        /// <typeparam name="TResult">结果的类型。</typeparam>
        /// <param name="grouping">分组对象。</param>
        /// <param name="selector">选择器表达式。</param>
        /// <returns>最小值。</returns>
        public static TResult Min<TKey, TElement, TResult>(this IGrouping<TKey, TElement> grouping, Expression<Func<TElement, TResult>> selector)
        {
            throw new NotImplementedException("此方法仅用于表达式树解析，不应被直接调用");
        }
    }
}