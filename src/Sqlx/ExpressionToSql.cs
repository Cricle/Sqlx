using System;
using System.Collections.Generic;
using System.Linq;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif
using System.Linq.Expressions;

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
            _custom = cols?.Length > 0 ? new List<string>(cols) : new List<string>();
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
    private List<string> ExtractColumnsFromSelectors(Expression<Func<T, object>>[]? selectors)
    {
        if (selectors == null || selectors.Length == 0) return new List<string>(0);

        // 性能优化：预估容量避免重新分配
        var result = new List<string>(selectors.Length * 2);
        foreach (var selector in selectors)
        {
            if (selector != null)
                result.AddRange(ExtractColumns(selector.Body));
        }
        return result;
    }

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
        public ExpressionToSql<T> Take(int take)
        {
            _take = take;
            return this;
        }

        /// <summary>Skips specified number of records</summary>
        public ExpressionToSql<T> Skip(int skip)
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


        /// <summary>INSERT operation with specific columns (AOT-friendly)</summary>
        public ExpressionToSql<T> Insert(Expression<Func<T, object>>? selector = null)
        {
            _operation = SqlOperation.Insert;
            if (selector != null) SetInsertColumns(selector);
            return this;
        }

        /// <summary>INSERT all columns (uses reflection)</summary>
        public ExpressionToSql<T> InsertAll()
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

        /// <summary>Set INSERT columns</summary>
        private void SetInsertColumns(Expression<Func<T, object>>? selector)
        {
            _columns.Clear();
            if (selector != null) _columns.AddRange(ExtractColumns(selector.Body));
        }

        /// <summary>Specifies INSERT values</summary>
        public ExpressionToSql<T> Values(params object[] values) => AddValues(values);

        /// <summary>Adds multiple INSERT values</summary>
        public ExpressionToSql<T> AddValues(params object[] values)
        {
            // 性能优化：避免不必要的LINQ和ToList()调用
            if (values?.Length > 0)
            {
                var formattedValues = new List<string>(values.Length);
                foreach (var value in values)
                {
                    formattedValues.Add(FormatConstantValue(value));
                }
                _values.Add(formattedValues);
            }
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
            var select = _custom?.Count > 0 ? $"SELECT {string.Join(", ", _custom)}" : "SELECT *";
            var from = $"FROM {_dialect.WrapColumn(_tableName!)}";
            var where = GetWhereClause();
            var groupBy = _groupByExpressions.Count > 0 ? $" GROUP BY {string.Join(", ", _groupByExpressions)}" : "";
            var having = _havingConditions.Count > 0 ? $" HAVING {string.Join(" AND ", _havingConditions)}" : "";
            var orderBy = _orderByExpressions.Count > 0 ? $" ORDER BY {string.Join(", ", _orderByExpressions)}" : "";
            var pagination = GetPaginationClause();

            return $"{select} {from}{where}{groupBy}{having}{orderBy}{pagination}";
        }

        private string GetPaginationClause() =>
            !_skip.HasValue && !_take.HasValue ? "" :
            _dialect.DatabaseType == "SqlServer" ? $" OFFSET {_skip ?? 0} ROWS{(_take.HasValue ? $" FETCH NEXT {_take.Value} ROWS ONLY" : "")}" :
            $"{(_take.HasValue ? $" LIMIT {_take.Value}" : "")}{(_skip.HasValue ? $" OFFSET {_skip.Value}" : "")}";

        // 性能优化：缓存where条件的构建，避免重复的Select(RemoveOuterParentheses)调用
        private string GetWhereClause() => _whereConditions.Count > 0 ? $" WHERE {string.Join(" AND ", _whereConditions.Select(RemoveOuterParentheses))}" : "";

        // 性能优化：避免嵌套的string.Join调用，构建VALUES子句
        private string GetValuesClause()
        {
            if (_values.Count == 0) return "";

            // 预分配StringBuilder容量，避免重新分配（估算）
            var capacity = _values.Count * 30;
            var sb = new System.Text.StringBuilder(" VALUES ", capacity);

            for (int i = 0; i < _values.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append('(');
                var vals = _values[i];
                for (int j = 0; j < vals.Count; j++)
                {
                    if (j > 0) sb.Append(", ");
                    sb.Append(vals[j]);
                }
                sb.Append(')');
            }
            return sb.ToString();
        }

        private string BuildInsertSql() =>
            $"INSERT INTO {_dialect.WrapColumn(_tableName!)}" +
            (_columns.Count > 0 ? $" ({string.Join(", ", _columns)})" : "") +
            (!string.IsNullOrEmpty(_selectSql) ? $" {_selectSql}" : GetValuesClause());

        private string BuildUpdateSql() =>
            $"UPDATE {_dialect.WrapColumn(_tableName!)} SET {string.Join(", ", _sets.Concat(_expressions))}" +
            GetWhereClause();

        private string BuildDeleteSql()
        {
            if (_whereConditions.Count == 0)
                throw new InvalidOperationException("DELETE operation requires WHERE clause for safety. Use Delete(predicate) or call Where() before Delete().");
            return $"DELETE FROM {_dialect.WrapColumn(_tableName!)}{GetWhereClause()}";
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
            var groupBy = _groupByExpressions.Count > 0 ? $" GROUP BY {string.Join(", ", _groupByExpressions)}" : "";
            var having = _havingConditions.Count > 0 ? $" HAVING {string.Join(" AND ", _havingConditions)}" : "";
            var orderBy = _orderByExpressions.Count > 0 ? $" ORDER BY {string.Join(", ", _orderByExpressions)}" : "";
            var pagination = GetPaginationClause();
            return $"{groupBy}{having}{orderBy}{pagination}".TrimStart();
        }


        /// <summary>Create with SQL dialect</summary>
        public static ExpressionToSql<T> Create(SqlDialect dialect) => new(dialect);

        // 便利方法 - 保持向后兼容性
        /// <summary>Creates a new ExpressionToSql instance configured for SQL Server.</summary>
        /// <returns>A new ExpressionToSql instance with SQL Server dialect.</returns>
        public static ExpressionToSql<T> ForSqlServer() => new(SqlDefine.SqlServer);

        /// <summary>Creates a new ExpressionToSql instance configured for MySQL.</summary>
        /// <returns>A new ExpressionToSql instance with MySQL dialect.</returns>
        public static ExpressionToSql<T> ForMySql() => new(SqlDefine.MySql);

        /// <summary>Creates a new ExpressionToSql instance configured for PostgreSQL.</summary>
        /// <returns>A new ExpressionToSql instance with PostgreSQL dialect.</returns>
        public static ExpressionToSql<T> ForPostgreSQL() => new(SqlDefine.PostgreSql);

        /// <summary>Creates a new ExpressionToSql instance configured for SQLite.</summary>
        /// <returns>A new ExpressionToSql instance with SQLite dialect.</returns>
        public static ExpressionToSql<T> ForSqlite() => new(SqlDefine.SQLite);

        /// <summary>Creates a new ExpressionToSql instance configured for Oracle.</summary>
        /// <returns>A new ExpressionToSql instance with Oracle dialect.</returns>
        public static ExpressionToSql<T> ForOracle() => new(SqlDefine.Oracle);

        /// <summary>Creates a new ExpressionToSql instance configured for DB2.</summary>
        /// <returns>A new ExpressionToSql instance with DB2 dialect.</returns>
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
            var resultQuery = ExpressionToSql<TResult>.Create(_baseQuery._dialect);

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
            return binary.NodeType == ExpressionType.Coalesce ? $"COALESCE({left}, {right})" : $"({left} {GetBinaryOperator(binary.NodeType)} {right})";
        }

        private string ParseConditionalExpression(ConditionalExpression conditional) =>
            $"CASE WHEN {ParseSelectExpression(conditional.Test)} THEN {ParseSelectExpression(conditional.IfTrue)} ELSE {ParseSelectExpression(conditional.IfFalse)} END";

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
                // 性能优化：避免不必要的ToArray()调用，按需解析参数
                return (methodName, methodCall.Arguments.Count) switch
                {
                    ("Abs", 1) => $"ABS({ParseLambdaBody(methodCall.Arguments[0])})",
                    ("Round", 1) => $"ROUND({ParseLambdaBody(methodCall.Arguments[0])})",
                    ("Round", 2) => $"ROUND({ParseLambdaBody(methodCall.Arguments[0])}, {ParseLambdaBody(methodCall.Arguments[1])})",
                    ("Floor", 1) => $"FLOOR({ParseLambdaBody(methodCall.Arguments[0])})",
                    ("Ceiling", 1) => DatabaseType == "PostgreSql" ? $"CEIL({ParseLambdaBody(methodCall.Arguments[0])})" : $"CEILING({ParseLambdaBody(methodCall.Arguments[0])})",
                    ("Min", 2) => $"LEAST({ParseLambdaBody(methodCall.Arguments[0])}, {ParseLambdaBody(methodCall.Arguments[1])})",
                    ("Max", 2) => $"GREATEST({ParseLambdaBody(methodCall.Arguments[0])}, {ParseLambdaBody(methodCall.Arguments[1])})",
                    ("Pow", 2) => DatabaseType == "MySql" ? $"POW({ParseLambdaBody(methodCall.Arguments[0])}, {ParseLambdaBody(methodCall.Arguments[1])})" : $"POWER({ParseLambdaBody(methodCall.Arguments[0])}, {ParseLambdaBody(methodCall.Arguments[1])})",
                    ("Sqrt", 1) => $"SQRT({ParseLambdaBody(methodCall.Arguments[0])})",
                    _ => methodCall.Arguments.Count > 0 ? ParseLambdaBody(methodCall.Arguments[0]) : "NULL"
                };
            }

            // String functions
            if (declaringType == typeof(string) && methodCall.Object != null)
            {
                var obj = ParseLambdaBody(methodCall.Object);
                // 性能优化：避免不必要的ToArray()调用，按需解析参数
                return (methodName, methodCall.Arguments.Count) switch
                {
                    ("Length", 0) => DatabaseType == "SqlServer" ? $"LEN({obj})" : $"LENGTH({obj})",
                    ("ToUpper", 0) => $"UPPER({obj})",
                    ("ToLower", 0) => $"LOWER({obj})",
                    ("Trim", 0) => $"TRIM({obj})",
                    ("Substring", 1) => DatabaseType == "SQLite" ? $"SUBSTR({obj}, {ParseLambdaBody(methodCall.Arguments[0])})" : $"SUBSTRING({obj}, {ParseLambdaBody(methodCall.Arguments[0])})",
                    ("Substring", 2) => DatabaseType == "SQLite" ? $"SUBSTR({obj}, {ParseLambdaBody(methodCall.Arguments[0])}, {ParseLambdaBody(methodCall.Arguments[1])})" : $"SUBSTRING({obj}, {ParseLambdaBody(methodCall.Arguments[0])}, {ParseLambdaBody(methodCall.Arguments[1])})",
                    ("Replace", 2) => $"REPLACE({obj}, {ParseLambdaBody(methodCall.Arguments[0])}, {ParseLambdaBody(methodCall.Arguments[1])})",
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
