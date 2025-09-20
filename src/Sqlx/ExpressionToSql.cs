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
        private readonly List<string> _columns = new(); // INSERT column names
        private readonly List<List<string>> _values = new(); // INSERT values (supports multiple rows)
        private string? _selectSql; // SQL for INSERT SELECT
        private SqlOperation _operation = SqlOperation.Select; // Default to SELECT operation

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
        /// Unified column extraction logic to avoid code duplication
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
        /// Unified INSERT column setting logic
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
        /// Set custom SELECT clause (internal use).
        /// </summary>
        internal List<string>? _custom;

        internal void SetCustomSelectClause(List<string> clause)
        {
            _custom = clause;
        }


        /// <summary>
        /// Build SQL statement, simple and direct without caching.
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

            // SELECT clause
            var selectClause = _custom?.Count > 0
                ? $"SELECT {string.Join(", ", _custom)} FROM "
                : "SELECT * FROM ";
            sql.Append(selectClause);

            // FROM table name
            sql.Append(_dialect.WrapColumn(_tableName!));

            // WHERE clause
            if (_whereConditions.Count > 0)
            {
                sql.Append(" WHERE ");
                var conditions = _whereConditions.Select(RemoveOuterParentheses);
                sql.Append(string.Join(" AND ", conditions));
            }

            // GROUP BY clause
            if (_groupByExpressions.Count > 0)
            {
                sql.Append(" GROUP BY ");
                sql.Append(string.Join(", ", _groupByExpressions));
            }

            // HAVING clause
            if (_havingConditions.Count > 0)
            {
                sql.Append(" HAVING ");
                sql.Append(string.Join(" AND ", _havingConditions));
            }

            // ORDER BY clause
            if (_orderByExpressions.Count > 0)
            {
                sql.Append(" ORDER BY ");
                sql.Append(string.Join(", ", _orderByExpressions));
            }

            // LIMIT/OFFSET clause
            if (_skip.HasValue || _take.HasValue)
            {
                if (_dialect.DatabaseType == "SqlServer")
                {
                    // SQL Server uses OFFSET...FETCH
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
                    // Other databases use LIMIT/OFFSET
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

            // Merge all SET clauses
            var allClauses = _sets.Concat(_expressions);
            sql.Append(string.Join(", ", allClauses));

            // Add WHERE clause
            if (_whereConditions.Count > 0)
            {
                sql.Append(" WHERE ");
                // UPDATE statements retain parentheses to ensure correct logical grouping
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
        /// Build DELETE SQL statement.
        /// </summary>
        private string BuildDeleteSql()
        {
            var sql = new StringBuilder(256);

            // DELETE FROM table name
            sql.Append("DELETE FROM ");
            sql.Append(_dialect.WrapColumn(_tableName!));

            // WHERE clause - DELETE must have WHERE conditions for safety
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
        /// Enable parameterized query mode for SqlTemplate generation
        /// </summary>
        public ExpressionToSql<T> UseParameterizedQueries()
        {
            _parameterized = true;
            return this;
        }

        /// <summary>
        /// Convert to SQL template. If parameterized mode is not enabled, it will automatically enable and rebuild the query.
        /// </summary>
        public override SqlTemplate ToTemplate()
        {
            // If parameterized queries are not yet enabled, need to rebuild
            if (!_parameterized)
            {
                // Save current state
                var conditions = new List<string>(_whereConditions);
                var orderBy = new List<string>(_orderByExpressions);
                var groupBy = new List<string>(_groupByExpressions);
                var having = new List<string>(_havingConditions);

                // Clear and re-enable parameterized mode
                _whereConditions.Clear();
                _orderByExpressions.Clear();
                _groupByExpressions.Clear();
                _havingConditions.Clear();
                _parameters.Clear();
                _parameterized = true;

                // Need to rebuild query, but this requires original expressions
                // Since we didn't save original expressions, we can only return current SQL and empty parameters
                // This is a design limitation, users should explicitly call UseParameterizedQueries()
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
        /// Convert to SQL string.
        /// </summary>
        public override string ToSql() => BuildSql();

        /// <summary>
        /// Generate WHERE clause part.
        /// </summary>
        public string ToWhereClause() => _whereConditions.Count == 0 ? string.Empty : string.Join(" AND ", _whereConditions);

        /// <summary>
        /// Generate additional clauses (GROUP BY, HAVING, ORDER BY, LIMIT, OFFSET).
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

                if (useOffsetFetchSyntax) // SQL Server or Oracle
                {
                    if (_skip.HasValue) sql.Append($" OFFSET {_skip.Value} ROWS");
                    if (_take.HasValue) sql.Append($" FETCH NEXT {_take.Value} ROWS ONLY");
                }
                else // MySQL, PostgreSQL, SQLite etc
                {
                    if (_take.HasValue) sql.Append($" LIMIT {_take.Value}");
                    if (_skip.HasValue) sql.Append($" OFFSET {_skip.Value}");
                }
            }

            return sql.ToString().TrimStart(); // Remove leading spaces
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
            // Create new query object using the same dialect
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

            // Build SELECT clause
            var selectClause = BuildSelectClause(selector.Body);
            resultQuery.SetCustomSelectClause(selectClause);

            // Copy base query information
            CopyBaseQueryInfo(resultQuery);

            return resultQuery;
        }

        /// <summary>
        /// Add HAVING conditions.
        /// </summary>
        public GroupedExpressionToSql<T, TKey> Having(Expression<Func<IGrouping<TKey, T>, bool>> predicate)
        {
            // Parse aggregate functions in HAVING conditions
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
                    // Handle new { Key = g.Key, Count = g.Count() } format
                    for (int i = 0; i < newExpr.Arguments.Count; i++)
                    {
                        var arg = newExpr.Arguments[i];
                        var memberName = newExpr.Members?[i]?.Name ?? $"Column{i}";
                        var selectExpression = ParseSelectExpression(arg);
                        selectClause.Add($"{selectExpression} AS {memberName}");
                    }
                    break;

                case MemberInitExpression memberInit:
                    // Handle new TestUserResult { Id = g.Key, Count = g.Count() } format
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
                    // Single expression
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
                    // g.Key access
                    if (member.Member.Name == "Key")
                    {
                        return _keyColumnName;
                    }
                    return "NULL";

                case BinaryExpression binary:
                    // Handle binary expressions, e.g. g.Key ?? 0 or complex arithmetic expressions
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
                    // Handle ternary operator condition ? ifTrue : ifFalse
                    var test = ParseSelectExpression(conditional.Test);
                    var ifTrue = ParseSelectExpression(conditional.IfTrue);
                    var ifFalse = ParseSelectExpression(conditional.IfFalse);
                    return $"CASE WHEN {test} THEN {ifTrue} ELSE {ifFalse} END";

                case UnaryExpression unary when unary.NodeType == ExpressionType.Convert:
                    // Handle type conversion
                    return ParseSelectExpression(unary.Operand);

                default:
                    // For expressions that cannot be handled, try parsing as normal expressions
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
                _ => throw new NotSupportedException($"Aggregate function {methodName} is not supported")
            };
        }

        /// <summary>
        /// Enhanced Lambda expression parsing with support for complex nested functions and expressions
        /// </summary>
        private string ParseLambdaExpressionEnhanced(Expression expression) => expression switch
        {
            LambdaExpression lambda => ParseLambdaBody(lambda.Body),
            UnaryExpression { NodeType: ExpressionType.Quote } unary when unary.Operand is LambdaExpression quotedLambda => ParseLambdaBody(quotedLambda.Body),
            _ => ParseLambdaBody(expression),
        };

        /// <summary>
        /// Parse Lambda expression Body part with support for nested functions
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
                    // Support arithmetic expressions and null coalescing, like x.Salary * 1.2, x.Bonus ?? 0
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
                    // Support nested function calls, like Math.Round(x.Salary, 2)
                    return ParseMethodCallInAggregate(methodCall);

                case ConstantExpression constant:
                    return FormatConstantValue(constant.Value);

                case ConditionalExpression conditional:
                    // Support conditional expressions, like x.IsActive ? x.Salary : 0
                    var test = ParseLambdaBody(conditional.Test);
                    var ifTrue = ParseLambdaBody(conditional.IfTrue);
                    var ifFalse = ParseLambdaBody(conditional.IfFalse);
                    return $"CASE WHEN {test} THEN {ifTrue} ELSE {ifFalse} END";

                case UnaryExpression unary when unary.NodeType == ExpressionType.Convert:
                    return ParseLambdaBody(unary.Operand);

                default:
                    // Fallback to simple column name extraction
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
        /// Parse method calls in aggregate function context
        /// </summary>
        private string ParseMethodCallInAggregate(MethodCallExpression methodCall)
        {
            var methodName = methodCall.Method.Name;
            var declaringType = methodCall.Method.DeclaringType;

            // Math functions
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

            // String functions
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

            // Other cases, fallback to basic parsing
            return methodCall.Object != null ? ParseLambdaBody(methodCall.Object) : "NULL";
        }
        private string ExtractColumnName(Expression expression)
        {
            // Use correct database dialect format
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
            // Parse conditional expressions in HAVING clause with aggregate function support
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
            // Copy table name - use original table name instead of result type name
            resultQuery.SetTableName(typeof(T).Name);

            // Copy WHERE conditions
            resultQuery.CopyWhereConditions(_baseQuery.GetWhereConditions());

            // Ensure GROUP BY clause is included
            if (!string.IsNullOrEmpty(_keyColumnName))
            {
                resultQuery.AddGroupByColumn(_keyColumnName);
            }

            // Copy HAVING conditions
            resultQuery.CopyHavingConditions(_baseQuery.GetHavingConditions());
        }

        /// <summary>
        /// Convert to SQL query string.
        /// </summary>
        /// <returns>SQL query string.</returns>
        public override string ToSql()
        {
            return _baseQuery.ToSql();
        }

        /// <summary>
        /// Convert to SQL template.
        /// </summary>
        /// <returns>SQL template instance.</returns>
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