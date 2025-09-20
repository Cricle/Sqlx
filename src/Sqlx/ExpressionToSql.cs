using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text;

namespace Sqlx
{
    /// <summary>SQL operation types</summary>
    public enum SqlOperation
    {
        /// <summary>SELECT operation</summary>
        Select,
        /// <summary>INSERT operation</summary>
        Insert,
        /// <summary>UPDATE operation</summary>
        Update,
        /// <summary>DELETE operation</summary>
        Delete
    }

    /// <summary>Placeholder values for dynamic SQL generation</summary>
    public static class Any
    {
        /// <summary>Generic placeholder value</summary>
        public static TValue Value<TValue>() => default!;
        /// <summary>Named generic placeholder value</summary>
        public static TValue Value<TValue>(string parameterName) => default!;
        /// <summary>String placeholder</summary>
        public static string String() => default!;
        /// <summary>Named string placeholder</summary>
        public static string String(string parameterName) => default!;
        /// <summary>Integer placeholder</summary>
        public static int Int() => default;
        /// <summary>Named integer placeholder</summary>
        public static int Int(string parameterName) => default;
        /// <summary>Boolean placeholder</summary>
        public static bool Bool() => default;
        /// <summary>Named boolean placeholder</summary>
        public static bool Bool(string parameterName) => default;
        /// <summary>DateTime placeholder</summary>
        public static DateTime DateTime() => default;
        /// <summary>Named DateTime placeholder</summary>
        public static DateTime DateTime(string parameterName) => default;
        /// <summary>Guid placeholder</summary>
        public static Guid Guid() => default;
        /// <summary>Named Guid placeholder</summary>
        public static Guid Guid(string parameterName) => default;
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
        /// Initializes with specified SQL dialect
        /// </summary>
        private ExpressionToSql(SqlDialect dialect) : base(dialect, typeof(T))
        {
        }

        /// <summary>Sets custom SELECT columns</summary>
        public ExpressionToSql<T> Select(params string[] cols) =>
            ConfigureSelect(cols?.ToList() ?? new List<string>());

        /// <summary>Sets SELECT columns using expression</summary>
        public ExpressionToSql<T> Select<TResult>(Expression<Func<T, TResult>> selector) =>
            ConfigureSelect(ExtractColumnsFromSelector(selector));

        /// <summary>Sets SELECT columns using multiple expressions</summary>
        public ExpressionToSql<T> Select(params Expression<Func<T, object>>[] selectors) =>
            ConfigureSelect(ExtractColumnsFromSelectors(selectors));

        /// <summary>Configures SELECT columns</summary>
        private ExpressionToSql<T> ConfigureSelect(List<string> columns)
        {
            _custom = columns;
            return this;
        }

        /// <summary>Gets entity properties using generics (AOT-friendly)</summary>
        private static System.Reflection.PropertyInfo[] GetEntityProperties<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        TEntity>() => typeof(TEntity).GetProperties();

        /// <summary>
        /// 统一的列提取逻辑，避免重复代码
        /// </summary>
        private List<string> ExtractColumnsFromSelector<TResult>(Expression<Func<T, TResult>>? selector)
        {
            return selector != null ? ExtractColumns(selector.Body) : new List<string>();
        }

        private List<string> ExtractColumnsFromSelectors(Expression<Func<T, object>>[]? selectors) =>
            selectors?.Where(s => s != null).SelectMany(s => ExtractColumns(s.Body)).ToList() ?? new List<string>(0);

        /// <summary>Adds WHERE condition</summary>
        public ExpressionToSql<T> Where(Expression<Func<T, bool>> predicate)
        {
            if (predicate != null)
            {
                var conditionSql = ParseExpression(predicate.Body);
                _whereConditions.Add(NeedsParentheses(predicate.Body) ? $"({conditionSql})" : conditionSql);
            }
            return this;
        }

        /// <summary>Adds AND condition (alias for Where)</summary>
        public ExpressionToSql<T> And(Expression<Func<T, bool>> predicate) => Where(predicate);

        /// <summary>Adds ORDER BY ascending</summary>
        public ExpressionToSql<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector) => AddOrderBy(keySelector, "ASC");
        /// <summary>Adds ORDER BY descending</summary>
        public ExpressionToSql<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector) => AddOrderBy(keySelector, "DESC");

        private ExpressionToSql<T> AddOrderBy<TKey>(Expression<Func<T, TKey>>? keySelector, string direction)
        {
            if (keySelector != null)
            {
                var columnName = GetColumnName(keySelector.Body);
                _orderByExpressions.Add($"{columnName} {direction}");
            }
            return this;
        }

        /// <summary>Limits result count</summary>
        public ExpressionToSql<T> Take(int? take)
        {
            _take = take;
            return this;
        }

        /// <summary>Skips specified number of records</summary>
        public ExpressionToSql<T> Skip(int? skip)
        {
            _skip = skip;
            return this;
        }

        /// <summary>Sets column value for UPDATE</summary>
        public ExpressionToSql<T> Set<TValue>(Expression<Func<T, TValue>> selector, TValue value) =>
            ConfigureSet(selector, value, null);

        /// <summary>Sets column value using expression for UPDATE</summary>
        public ExpressionToSql<T> Set<TValue>(Expression<Func<T, TValue>> selector, Expression<Func<T, TValue>> valueExpression) =>
            ConfigureSet(selector, default, valueExpression);

        /// <summary>Configures UPDATE SET clause</summary>
        private ExpressionToSql<T> ConfigureSet<TValue>(Expression<Func<T, TValue>>? selector, TValue? value, Expression<Func<T, TValue>>? valueExpression)
        {
            EnsureUpdateMode();
            if (selector != null)
            {
                var column = GetColumnName(selector.Body);
                var assignment = valueExpression != null
                    ? $"{column} = {ParseExpression(valueExpression.Body)}"
                    : $"{column} = {FormatConstantValue(value)}";

                if (valueExpression != null)
                    _expressions.Add(assignment);
                else
                    _sets.Add(assignment);
            }
            return this;
        }

        private void EnsureUpdateMode() => _operation = SqlOperation.Update;
        private void EnsureInsertMode() => _operation = SqlOperation.Insert;
        private void EnsureDeleteMode() => _operation = SqlOperation.Delete;

        #region As INSERT Methods - Consolidated INSERT operations with 'as' prefix

        /// <summary>Sets as INSERT operation</summary>
        public ExpressionToSql<T> AsInsert() => ConfigureInsert();

        /// <summary>Sets as INSERT operation with specific columns</summary>
        public ExpressionToSql<T> AsInsert(Expression<Func<T, object>> selector) => ConfigureInsert(selector);

        /// <summary>Sets as INSERT INTO with explicit columns (AOT-friendly)</summary>
        public ExpressionToSql<T> AsInsertInto(Expression<Func<T, object>> selector) => ConfigureInsert(selector);

        /// <summary>Sets as INSERT INTO all columns</summary>
        public ExpressionToSql<T> AsInsertIntoAll() => ConfigureInsertAll();

        /// <summary>Sets as INSERT using SELECT subquery</summary>
        public ExpressionToSql<T> AsInsertSelect(string sql) => ConfigureInsertSelect(sql);

        /// <summary>Sets as INSERT using another query</summary>
        public ExpressionToSql<T> AsInsertSelect<TSource>(ExpressionToSql<TSource> query) => ConfigureInsertSelect(query);

        #endregion

        #region INSERT Methods - Legacy methods for backward compatibility

        /// <summary>Creates INSERT operation</summary>
        public ExpressionToSql<T> Insert() => ConfigureInsert();

        /// <summary>Sets INSERT columns using expression</summary>
        public ExpressionToSql<T> Insert(Expression<Func<T, object>> selector) => ConfigureInsert(selector);

        /// <summary>INSERT INTO with explicit columns (AOT-friendly)</summary>
        public ExpressionToSql<T> InsertInto(Expression<Func<T, object>> selector) => ConfigureInsert(selector);

        /// <summary>INSERT INTO all columns (uses reflection)</summary>
        public ExpressionToSql<T> InsertIntoAll() => ConfigureInsertAll();

        /// <summary>INSERT using SELECT subquery</summary>
        public ExpressionToSql<T> InsertSelect(string sql) => ConfigureInsertSelect(sql);

        /// <summary>INSERT using another query</summary>
        public ExpressionToSql<T> InsertSelect<TSource>(ExpressionToSql<TSource> query) => ConfigureInsertSelect(query);

        #endregion

        #region Private INSERT Configuration Methods

        /// <summary>Configures INSERT operation with optional columns</summary>
        private ExpressionToSql<T> ConfigureInsert(Expression<Func<T, object>>? selector = null)
        {
            EnsureInsertMode();
            if (selector != null) SetInsertColumns(selector);
            return this;
        }

        /// <summary>Configures INSERT with all entity columns</summary>
        private ExpressionToSql<T> ConfigureInsertAll()
        {
            EnsureInsertMode();
            _columns.Clear();
            _columns.AddRange(GetEntityProperties<T>().Select(prop => _dialect.WrapColumn(prop.Name)));
            return this;
        }

        /// <summary>Configures INSERT using SELECT subquery</summary>
        private ExpressionToSql<T> ConfigureInsertSelect(string sql)
        {
            EnsureInsertMode();
            _selectSql = sql;
            return this;
        }

        /// <summary>Configures INSERT using another query</summary>
        private ExpressionToSql<T> ConfigureInsertSelect<TSource>(ExpressionToSql<TSource> query)
        {
            EnsureInsertMode();
            if (query != null) _selectSql = query.ToSql();
            return this;
        }

        #endregion

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

        /// <summary>Specifies INSERT values</summary>
        public ExpressionToSql<T> Values(params object[] values) => ConfigureValues(values);

        /// <summary>Adds multiple INSERT values</summary>
        public ExpressionToSql<T> AddValues(params object[] values) => ConfigureValues(values);

        /// <summary>Configures INSERT values</summary>
        private ExpressionToSql<T> ConfigureValues(object[]? values)
        {
            if (values?.Length > 0)
                _values.Add(values.Select(FormatConstantValue).ToList());
            return this;
        }


        /// <summary>Adds GROUP BY clause, returns grouped query</summary>
        public GroupedExpressionToSql<T, TKey> GroupBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            if (keySelector != null)
            {
                var columnName = GetColumnName(keySelector.Body);
                _groupByExpressions.Add(columnName);
            }
            return new GroupedExpressionToSql<T, TKey>(this, keySelector!);
        }

        /// <summary>Adds GROUP BY column</summary>
        public new ExpressionToSql<T> AddGroupBy(string columnName) =>
            ConfigureGroupBy(columnName);

        /// <summary>Adds HAVING condition</summary>
        public ExpressionToSql<T> Having(Expression<Func<T, bool>> predicate) =>
            ConfigureHaving(predicate);

        /// <summary>Configures GROUP BY</summary>
        private ExpressionToSql<T> ConfigureGroupBy(string columnName)
        {
            base.AddGroupBy(columnName);
            return this;
        }

        /// <summary>Configures HAVING condition</summary>
        private ExpressionToSql<T> ConfigureHaving(Expression<Func<T, bool>>? predicate)
        {
            if (predicate != null)
            {
                var conditionSql = ParseExpression(predicate.Body);
                _havingConditions.Add($"({conditionSql})");
            }
            return this;
        }

        /// <summary>Creates DELETE statement</summary>
        public ExpressionToSql<T> Delete() => ConfigureDelete();

        /// <summary>Creates DELETE statement with WHERE condition</summary>
        public ExpressionToSql<T> Delete(Expression<Func<T, bool>> predicate) => ConfigureDelete(predicate);

        /// <summary>Creates UPDATE statement</summary>
        public ExpressionToSql<T> Update() => ConfigureUpdate();

        /// <summary>Configures DELETE operation</summary>
        private ExpressionToSql<T> ConfigureDelete(Expression<Func<T, bool>>? predicate = null)
        {
            EnsureDeleteMode();
            return predicate != null ? Where(predicate) : this;
        }

        /// <summary>Configures UPDATE operation</summary>
        private ExpressionToSql<T> ConfigureUpdate()
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
            var sql = new StringBuilder(512);
            sql.Append($"INSERT INTO {_dialect.WrapColumn(_tableName!)}");

            if (_columns.Count > 0)
            {
                sql.Append($" ({string.Join(", ", _columns)})");
            }
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
            var sql = new StringBuilder(256);
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
            var sql = new StringBuilder(256);

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
            var sql = new StringBuilder(256);

            if (_groupByExpressions.Count > 0)
            {
                sql.Append(" GROUP BY ");
                sql.Append(string.Join(", ", _groupByExpressions));
            }

            if (_havingConditions.Count > 0)
            {
                sql.Append(" HAVING ");
                sql.Append(string.Join(" AND ", _havingConditions));
            }

            if (_orderByExpressions.Count > 0)
            {
                sql.Append(" ORDER BY ");
                sql.Append(string.Join(", ", _orderByExpressions));
            }

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
    /// Grouped query object supporting aggregation operations
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
        /// Selects grouped result projection
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
    }

    /// <summary>Grouping interface similar to LINQ IGrouping</summary>
    public interface IGrouping<out TKey, out TElement>
    {
        /// <summary>Gets the grouping key</summary>
        TKey Key { get; }
    }

    /// <summary>Extensions for grouping operations (expression tree parsing only)</summary>
    public static class GroupingExtensions
    {
        /// <summary>Count aggregation</summary>
        public static int Count<TKey, TElement>(this IGrouping<TKey, TElement> grouping) => default;
        /// <summary>Sum aggregation</summary>
        public static TResult Sum<TKey, TElement, TResult>(this IGrouping<TKey, TElement> grouping, Expression<Func<TElement, TResult>> selector) => default!;
        /// <summary>Average aggregation for double</summary>
        public static double Average<TKey, TElement>(this IGrouping<TKey, TElement> grouping, Expression<Func<TElement, double>> selector) => default;
        /// <summary>Average aggregation for decimal</summary>
        public static double Average<TKey, TElement>(this IGrouping<TKey, TElement> grouping, Expression<Func<TElement, decimal>> selector) => default;
        /// <summary>Max aggregation</summary>
        public static TResult Max<TKey, TElement, TResult>(this IGrouping<TKey, TElement> grouping, Expression<Func<TElement, TResult>> selector) => default!;
        /// <summary>Min aggregation</summary>
        public static TResult Min<TKey, TElement, TResult>(this IGrouping<TKey, TElement> grouping, Expression<Func<TElement, TResult>> selector) => default!;
    }
}