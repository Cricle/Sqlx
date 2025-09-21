#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
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
        public ExpressionToSql<T> Select(params string[] cols)
        {
            _custom = cols?.ToList() ?? new List<string>();
            return this;
        }
        /// <summary>Sets SELECT columns using expression</summary>
        public ExpressionToSql<T> Select<TResult>(Expression<Func<T, TResult>> selector)
        {
            _custom = ExtractColumnsFromSelector(selector);
            return this;
        }
        /// <summary>Sets SELECT columns using multiple expressions</summary>
        public ExpressionToSql<T> Select(params Expression<Func<T, object>>[] selectors)
        {
            _custom = ExtractColumnsFromSelectors(selectors);
            return this;
        }

        /// <summary>Gets entity properties using generics (AOT-friendly)</summary>
        private static System.Reflection.PropertyInfo[] GetEntityProperties<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        TEntity>() => typeof(TEntity).GetProperties();

        /// <summary>Extract columns from selector</summary>
        private List<string> ExtractColumnsFromSelector<TResult>(Expression<Func<T, TResult>>? selector) => selector != null ? ExtractColumns(selector.Body) : new List<string>();
        private List<string> ExtractColumnsFromSelectors(Expression<Func<T, object>>[]? selectors) => selectors?.Where(s => s != null).SelectMany(s => ExtractColumns(s.Body)).ToList() ?? new List<string>(0);

        /// <summary>Adds WHERE condition</summary>
        public ExpressionToSql<T> Where(Expression<Func<T, bool>> predicate)
        {
            if (predicate != null) _whereConditions.Add($"({ParseExpression(predicate.Body)})");
            return this;
        }

        /// <summary>Adds AND condition (alias for Where)</summary>
        public ExpressionToSql<T> And(Expression<Func<T, bool>> predicate) => Where(predicate);

        /// <summary>Adds ORDER BY ascending</summary>
        public ExpressionToSql<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            if (keySelector != null)
                _orderByExpressions.Add($"{GetColumnName(keySelector.Body)} ASC");
            return this;
        }
        /// <summary>Adds ORDER BY descending</summary>
        public ExpressionToSql<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            if (keySelector != null)
                _orderByExpressions.Add($"{GetColumnName(keySelector.Body)} DESC");
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
        public ExpressionToSql<T> Set<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        {
            _operation = SqlOperation.Update;
            if (selector != null) _sets.Add($"{GetColumnName(selector.Body)} = {FormatConstantValue(value)}");
            return this;
        }
        /// <summary>Sets column value using expression for UPDATE</summary>
        public ExpressionToSql<T> Set<TValue>(Expression<Func<T, TValue>> selector, Expression<Func<T, TValue>> valueExpression)
        {
            _operation = SqlOperation.Update;
            if (selector != null && valueExpression != null) _expressions.Add($"{GetColumnName(selector.Body)} = {ParseExpression(valueExpression.Body)}");
            return this;
        }


        #region As INSERT Methods - Consolidated INSERT operations with 'as' prefix

        /// <summary>Sets as INSERT operation</summary>
        public ExpressionToSql<T> AsInsert()
        {
            _operation = SqlOperation.Insert;
            return this;
        }
        /// <summary>Sets as INSERT operation with specific columns</summary>
        public ExpressionToSql<T> AsInsert(Expression<Func<T, object>> selector)
        {
            _operation = SqlOperation.Insert;
            if (selector != null)
                SetInsertColumns(selector);
            return this;
        }
        /// <summary>Sets as INSERT INTO with explicit columns (AOT-friendly)</summary>
        public ExpressionToSql<T> AsInsertInto(Expression<Func<T, object>> selector)
        {
            _operation = SqlOperation.Insert;
            if (selector != null)
                SetInsertColumns(selector);
            return this;
        }
        /// <summary>Sets as INSERT INTO all columns</summary>
        public ExpressionToSql<T> AsInsertIntoAll()
        {
            _operation = SqlOperation.Insert;
            _columns.Clear();
            _columns.AddRange(GetEntityProperties<T>().Select(prop => _dialect.WrapColumn(prop.Name)));
            return this;
        }
        /// <summary>Sets as INSERT using SELECT subquery</summary>
        public ExpressionToSql<T> AsInsertSelect(string sql)
        {
            _operation = SqlOperation.Insert;
            _selectSql = sql;
            return this;
        }
        /// <summary>Sets as INSERT using another query</summary>
        public ExpressionToSql<T> AsInsertSelect<TSource>(ExpressionToSql<TSource> query)
        {
            _operation = SqlOperation.Insert;
            if (query != null)
                _selectSql = query.ToSql();
            return this;
        }

        #endregion

        #region INSERT Methods - Legacy methods for backward compatibility

        /// <summary>Creates INSERT operation</summary>
        public ExpressionToSql<T> Insert()
        {
            _operation = SqlOperation.Insert;
            return this;
        }
        /// <summary>Sets INSERT columns using expression</summary>
        public ExpressionToSql<T> Insert(Expression<Func<T, object>> selector)
        {
            _operation = SqlOperation.Insert;
            if (selector != null)
                SetInsertColumns(selector);
            return this;
        }
        /// <summary>INSERT INTO with explicit columns (AOT-friendly)</summary>
        public ExpressionToSql<T> InsertInto(Expression<Func<T, object>> selector)
        {
            _operation = SqlOperation.Insert;
            if (selector != null)
                SetInsertColumns(selector);
            return this;
        }
        /// <summary>INSERT INTO all columns (uses reflection)</summary>
        public ExpressionToSql<T> InsertIntoAll()
        {
            _operation = SqlOperation.Insert;
            _columns.Clear();
            _columns.AddRange(GetEntityProperties<T>().Select(prop => _dialect.WrapColumn(prop.Name)));
            return this;
        }
        /// <summary>INSERT using SELECT subquery</summary>
        public ExpressionToSql<T> InsertSelect(string sql)
        {
            _operation = SqlOperation.Insert;
            _selectSql = sql;
            return this;
        }

        /// <summary>INSERT using another query</summary>
        public ExpressionToSql<T> InsertSelect<TSource>(ExpressionToSql<TSource> query)
        {
            _operation = SqlOperation.Insert;
            if (query != null)
                _selectSql = query.ToSql();
            return this;
        }

        #endregion

        #region Private INSERT Configuration Methods


        #endregion

        /// <summary>Set INSERT columns</summary>
        private void SetInsertColumns(Expression<Func<T, object>>? selector)
        {
            _columns.Clear();
            if (selector != null) _columns.AddRange(ExtractColumns(selector.Body));
        }

        /// <summary>Specifies INSERT values</summary>
        public ExpressionToSql<T> Values(params object[] values)
        {
            if (values?.Length > 0)
                _values.Add(values.Select(FormatConstantValue).ToList());
            return this;
        }
        /// <summary>Adds multiple INSERT values</summary>
        public ExpressionToSql<T> AddValues(params object[] values)
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
        public new ExpressionToSql<T> AddGroupBy(string columnName)
        {
            base.AddGroupBy(columnName);
            return this;
        }
        /// <summary>Adds HAVING condition</summary>
        public ExpressionToSql<T> Having(Expression<Func<T, bool>> predicate)
        {
            if (predicate != null)
                _havingConditions.Add($"({ParseExpression(predicate.Body)})");
            return this;
        }

        /// <summary>Creates DELETE statement</summary>
        public ExpressionToSql<T> Delete()
        {
            _operation = SqlOperation.Delete;
            return this;
        }
        /// <summary>Creates DELETE statement with WHERE condition</summary>
        public ExpressionToSql<T> Delete(Expression<Func<T, bool>> predicate)
        {
            _operation = SqlOperation.Delete;
            return predicate != null ? Where(predicate) : this;
        }
        /// <summary>Creates UPDATE statement</summary>
        public ExpressionToSql<T> Update()
        {
            _operation = SqlOperation.Update;
            return this;
        }

        /// <summary>Custom SELECT clause (internal use)</summary>
        internal List<string>? _custom;
        internal void SetCustomSelectClause(List<string> clause) => _custom = clause;


        /// <summary>Build SQL statement</summary>
        private string BuildSql()
        {
            return _operation switch
            {
                SqlOperation.Insert => BuildInsertSql(),
                SqlOperation.Update => BuildUpdateSql(),
                SqlOperation.Delete => BuildDeleteSql(),
                _ => BuildSelectSql()
            };
        }

        private string BuildSelectSql()
        {
            var sql = new System.Text.StringBuilder(512);
            sql.Append(_custom?.Count > 0 ? $"SELECT {string.Join(", ", _custom)} FROM " : "SELECT * FROM ");
            sql.Append(_dialect.WrapColumn(_tableName!));
            AppendWhereClause(sql, false);
            if (_groupByExpressions.Count > 0)
                sql.Append($" GROUP BY {string.Join(", ", _groupByExpressions)}");
            if (_havingConditions.Count > 0)
                sql.Append($" HAVING {string.Join(" AND ", _havingConditions)}");
            if (_orderByExpressions.Count > 0)
                sql.Append($" ORDER BY {string.Join(", ", _orderByExpressions)}");
            AppendPaginationClause(sql);
            return sql.ToString();
        }

        private void AppendPaginationClause(StringBuilder sql)
        {
            if (!_skip.HasValue && !_take.HasValue) return;
            if (_dialect.DatabaseType == "SqlServer")
            {
                if (_skip.HasValue)
                {
                    sql.Append($" OFFSET {_skip.Value} ROWS");
                    if (_take.HasValue)
                        sql.Append($" FETCH NEXT {_take.Value} ROWS ONLY");
                }
                else if (_take.HasValue)
                {
                    sql.Append($" OFFSET 0 ROWS FETCH NEXT {_take.Value} ROWS ONLY");
                }
            }
            else
            {
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

        private void AppendWhereClause(StringBuilder sql, bool useParentheses)
        {
            if (_whereConditions.Count == 0) return;
            sql.Append(" WHERE ");
            var conditions = _whereConditions.Select(RemoveOuterParentheses);
            if (useParentheses) sql.Append($"({string.Join(" AND ", conditions)})");
            else sql.Append(string.Join(" AND ", conditions));
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

            AppendWhereClause(sql, true);

            return sql.ToString();
        }

        /// <summary>Build DELETE SQL statement</summary>
        private string BuildDeleteSql()
        {
            if (_whereConditions.Count == 0)
                throw new InvalidOperationException("DELETE operation requires WHERE clause for safety. Use Delete(predicate) or call Where() before Delete().");

            var sql = new StringBuilder(256);
            sql.Append($"DELETE FROM {_dialect.WrapColumn(_tableName!)}");
            AppendWhereClause(sql, false);
            return sql.ToString();
        }

        /// <summary>Enable parameterized query mode for SqlTemplate generation</summary>
        public ExpressionToSql<T> UseParameterizedQueries()
        {
            _parameterized = true;
            return this;
        }

        /// <summary>Convert to SQL template</summary>
        public override SqlTemplate ToTemplate() => new(BuildSql(), _parameters);
        /// <summary>Convert to SQL string</summary>
        public override string ToSql() => BuildSql();
        /// <summary>Generate WHERE clause part</summary>
        public string ToWhereClause() => _whereConditions.Count == 0 ? string.Empty : string.Join(" AND ", _whereConditions);

        /// <summary>Generate additional clauses (GROUP BY, HAVING, ORDER BY, LIMIT, OFFSET)</summary>
        public string ToAdditionalClause()
        {
            var sql = new StringBuilder(256);
            if (_groupByExpressions.Count > 0) sql.Append($" GROUP BY {string.Join(", ", _groupByExpressions)}");
            if (_havingConditions.Count > 0) sql.Append($" HAVING {string.Join(" AND ", _havingConditions)}");
            if (_orderByExpressions.Count > 0) sql.Append($" ORDER BY {string.Join(", ", _orderByExpressions)}");

            if (_skip.HasValue || _take.HasValue)
            {
                var useOffsetFetch = DatabaseType is "SqlServer" or "Oracle";
                if (useOffsetFetch)
                {
                    if (_skip.HasValue) sql.Append($" OFFSET {_skip.Value} ROWS");
                    if (_take.HasValue) sql.Append($" FETCH NEXT {_take.Value} ROWS ONLY");
                }
                else
                {
                    if (_take.HasValue) sql.Append($" LIMIT {_take.Value}");
                    if (_skip.HasValue) sql.Append($" OFFSET {_skip.Value}");
                }
            }
            return sql.ToString().TrimStart();
        }


        /// <summary>SQL Server dialect</summary>
        public static ExpressionToSql<T> ForSqlServer() => new(SqlDefine.SqlServer);
        /// <summary>MySQL dialect</summary>
        public static ExpressionToSql<T> ForMySql() => new(SqlDefine.MySql);
        /// <summary>PostgreSQL dialect</summary>
        public static ExpressionToSql<T> ForPostgreSQL() => new(SqlDefine.PostgreSql);
        /// <summary>SQLite dialect</summary>
        public static ExpressionToSql<T> ForSqlite() => new(SqlDefine.SQLite);
        /// <summary>Oracle dialect</summary>
        public static ExpressionToSql<T> ForOracle() => new(SqlDefine.Oracle);
        /// <summary>DB2 dialect</summary>
        public static ExpressionToSql<T> ForDB2() => new(SqlDefine.DB2);
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

        private string ParseSelectExpression(Expression expression) => expression switch
        {
            MethodCallExpression methodCall => ParseAggregateFunction(methodCall),
            MemberExpression member when member.Expression is ParameterExpression { Name: "g" } =>
                member.Member.Name == "Key" ? _keyColumnName : "NULL",
            BinaryExpression binary => ParseBinarySelectExpression(binary),
            ConstantExpression constant => FormatConstantValue(constant.Value),
            ConditionalExpression conditional => ParseConditionalExpression(conditional),
            UnaryExpression { NodeType: ExpressionType.Convert } unary => ParseSelectExpression(unary.Operand),
            _ => ParseFallbackExpression(expression)
        };

        private string ParseBinarySelectExpression(BinaryExpression binary)
        {
            var left = ParseSelectExpression(binary.Left);
            var right = ParseSelectExpression(binary.Right);
            var op = GetBinaryOperator(binary.NodeType);

            return binary.NodeType == ExpressionType.Coalesce
                ? $"COALESCE({left}, {right})"
                : $"({left} {op} {right})";
        }

        private string ParseConditionalExpression(ConditionalExpression conditional)
        {
            var test = ParseSelectExpression(conditional.Test);
            var ifTrue = ParseSelectExpression(conditional.IfTrue);
            var ifFalse = ParseSelectExpression(conditional.IfFalse);
            return $"CASE WHEN {test} THEN {ifTrue} ELSE {ifFalse} END";
        }

        private string ParseFallbackExpression(Expression expression)
        {
            try
            {
                return ParseExpressionRaw(expression);
            }
            catch
            {
                return "NULL";
            }
        }

        private static string GetBinaryOperator(ExpressionType nodeType) => nodeType switch
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
            _ => nodeType.ToString()
        };

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
            resultQuery.CopyWhereConditions(new List<string>(_baseQuery._whereConditions));

            // Ensure GROUP BY clause is included
            if (!string.IsNullOrEmpty(_keyColumnName))
            {
                resultQuery.AddGroupBy(_keyColumnName);
            }

            // Copy HAVING conditions
            resultQuery.CopyHavingConditions(new List<string>(_baseQuery._havingConditions));
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