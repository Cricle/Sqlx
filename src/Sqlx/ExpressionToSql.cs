// -----------------------------------------------------------------------
// <copyright file="ExpressionToSql.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Sqlx
{
    /// <summary>
    /// SQL operation types for internal use.
    /// </summary>
    internal enum SqlOperation
    {
        /// <summary>SELECT operation.</summary>
        Select,

        /// <summary>INSERT operation.</summary>
        Insert,

        /// <summary>UPDATE operation.</summary>
        Update,

        /// <summary>DELETE operation.</summary>
        Delete
    }

    /// <summary>
    /// Placeholder values for dynamic SQL generation.
    /// </summary>
    public static class Any
    {
        /// <summary>Generic placeholder value.</summary>
        public static TValue Value<TValue>() => default!;

        /// <summary>Named generic placeholder value.</summary>
        public static TValue Value<TValue>(string parameterName) => default!;

        /// <summary>String placeholder.</summary>
        public static string String() => default!;

        /// <summary>Named string placeholder.</summary>
        public static string String(string parameterName) => default!;

        /// <summary>Integer placeholder.</summary>
        public static int Int() => default;

        /// <summary>Named integer placeholder.</summary>
        public static int Int(string parameterName) => default;

        /// <summary>Boolean placeholder.</summary>
        public static bool Bool() => default;

        /// <summary>Named boolean placeholder.</summary>
        public static bool Bool(string parameterName) => default;

        /// <summary>DateTime placeholder.</summary>
        public static DateTime DateTime() => default;

        /// <summary>Named DateTime placeholder.</summary>
        public static DateTime DateTime(string parameterName) => default;

        /// <summary>Guid placeholder.</summary>
        public static Guid Guid() => default;

        /// <summary>Named Guid placeholder.</summary>
        public static Guid Guid(string parameterName) => default;
    }

    /// <summary>
    /// Simple and efficient LINQ Expression to SQL converter (AOT-friendly, lock-free design).
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    public partial class ExpressionToSql<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
    T> : ExpressionToSqlBase
    {
        private readonly List<string> _sets = new();
        private readonly List<string> _expressions = new();
        private readonly List<string> _columns = new();
        private readonly List<List<string>> _values = new();
        private string? _selectSql;
        private SqlOperation _operation = SqlOperation.Select;
        internal List<string>? _custom;

        private ExpressionToSql(SqlDialect dialect)
            : base(dialect, typeof(T))
        {
        }

        #region Static Factory Methods

        /// <summary>Create with SQL dialect.</summary>
        public static ExpressionToSql<T> Create(SqlDialect dialect) => new(dialect);

        /// <summary>Creates instance for SQL Server.</summary>
        public static ExpressionToSql<T> ForSqlServer() => new(SqlDefine.SqlServer);

        /// <summary>Creates instance for MySQL.</summary>
        public static ExpressionToSql<T> ForMySql() => new(SqlDefine.MySql);

        /// <summary>Creates instance for PostgreSQL.</summary>
        public static ExpressionToSql<T> ForPostgreSQL() => new(SqlDefine.PostgreSql);

        /// <summary>Creates instance for SQLite.</summary>
        public static ExpressionToSql<T> ForSqlite() => new(SqlDefine.SQLite);

        /// <summary>Creates instance for Oracle.</summary>
        public static ExpressionToSql<T> ForOracle() => new(SqlDefine.Oracle);

        /// <summary>Creates instance for DB2.</summary>
        public static ExpressionToSql<T> ForDB2() => new(SqlDefine.DB2);

        #endregion

        #region SELECT Methods

        /// <summary>Sets custom SELECT columns.</summary>
        public ExpressionToSql<T> Select(params string[] cols)
        {
            _custom = cols?.Length > 0 ? new List<string>(cols) : new List<string>();
            return this;
        }

        /// <summary>Sets SELECT columns using expression.</summary>
        public ExpressionToSql<T> Select<TResult>(Expression<Func<T, TResult>> selector)
        {
            _custom = selector != null ? ExtractColumns(selector.Body) : new List<string>();
            return this;
        }

        /// <summary>Sets SELECT columns using multiple expressions.</summary>
        public ExpressionToSql<T> Select(params Expression<Func<T, object>>[] selectors)
        {
            if (selectors == null || selectors.Length == 0)
            {
                _custom = new List<string>(0);
                return this;
            }

            var result = new List<string>(selectors.Length * 2);
            foreach (var selector in selectors)
            {
                if (selector != null)
                {
                    result.AddRange(ExtractColumns(selector.Body));
                }
            }

            _custom = result;
            return this;
        }

        internal void SetCustomSelectClause(List<string> clause) => _custom = clause;

        #endregion

        #region WHERE Methods

        /// <summary>Adds WHERE condition.</summary>
        public ExpressionToSql<T> Where(Expression<Func<T, bool>> predicate)
        {
            if (predicate != null)
            {
                _whereConditions.Add($"({ParseExpression(predicate.Body)})");
            }

            return this;
        }

        /// <summary>Adds AND condition (alias for Where).</summary>
        public ExpressionToSql<T> And(Expression<Func<T, bool>> predicate) => Where(predicate);

        #endregion

        #region ORDER BY Methods

        /// <summary>Adds ORDER BY ascending.</summary>
        public ExpressionToSql<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            if (keySelector != null)
            {
                _orderByExpressions.Add($"{GetColumnName(keySelector.Body)} ASC");
            }

            return this;
        }

        /// <summary>Adds ORDER BY descending.</summary>
        public ExpressionToSql<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            if (keySelector != null)
            {
                _orderByExpressions.Add($"{GetColumnName(keySelector.Body)} DESC");
            }

            return this;
        }

        #endregion

        #region Pagination Methods

        /// <summary>Limits result count.</summary>
        public ExpressionToSql<T> Take(int take)
        {
            _take = take;
            return this;
        }

        /// <summary>Skips specified number of records.</summary>
        public ExpressionToSql<T> Skip(int skip)
        {
            _skip = skip;
            return this;
        }

        #endregion

        #region UPDATE Methods

        /// <summary>Creates UPDATE statement.</summary>
        public ExpressionToSql<T> Update()
        {
            _operation = SqlOperation.Update;
            return this;
        }

        /// <summary>Sets column value for UPDATE.</summary>
        public ExpressionToSql<T> Set<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        {
            _operation = SqlOperation.Update;
            if (selector != null)
            {
                _sets.Add($"{GetColumnName(selector.Body)} = {FormatConstantValue(value)}");
            }

            return this;
        }

        /// <summary>Sets column value using expression for UPDATE.</summary>
        public ExpressionToSql<T> Set<TValue>(Expression<Func<T, TValue>> selector, Expression<Func<T, TValue>> valueExpression)
        {
            _operation = SqlOperation.Update;
            if (selector != null && valueExpression != null)
            {
                _expressions.Add($"{GetColumnName(selector.Body)} = {ParseExpression(valueExpression.Body)}");
            }

            return this;
        }

        #endregion

        #region INSERT Methods

        /// <summary>INSERT operation with specific columns (AOT-friendly).</summary>
        public ExpressionToSql<T> Insert(Expression<Func<T, object>>? selector = null)
        {
            _operation = SqlOperation.Insert;
            if (selector != null)
            {
                _columns.Clear();
                _columns.AddRange(ExtractColumns(selector.Body));
            }

            return this;
        }

        /// <summary>INSERT all columns (uses reflection).</summary>
        public ExpressionToSql<T> InsertAll()
        {
            _operation = SqlOperation.Insert;
            _columns.Clear();
            _columns.AddRange(typeof(T).GetProperties().Select(prop => _dialect.WrapColumn(prop.Name)));
            return this;
        }

        /// <summary>INSERT using SELECT subquery.</summary>
        public ExpressionToSql<T> InsertSelect(string sql)
        {
            _operation = SqlOperation.Insert;
            _selectSql = sql;
            return this;
        }

        /// <summary>Specifies INSERT values.</summary>
        public ExpressionToSql<T> Values(params object[] values) => AddValues(values);

        /// <summary>Adds multiple INSERT values.</summary>
        public ExpressionToSql<T> AddValues(params object[] values)
        {
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

        #endregion

        #region DELETE Methods

        /// <summary>Creates DELETE statement.</summary>
        public ExpressionToSql<T> Delete()
        {
            _operation = SqlOperation.Delete;
            return this;
        }

        /// <summary>Creates DELETE statement with WHERE condition.</summary>
        public ExpressionToSql<T> Delete(Expression<Func<T, bool>> predicate)
        {
            _operation = SqlOperation.Delete;
            return predicate != null ? Where(predicate) : this;
        }

        #endregion

        #region GROUP BY / HAVING Methods

        /// <summary>Adds GROUP BY clause, returns grouped query.</summary>
        public GroupedExpressionToSql<T, TKey> GroupBy<TKey>(Expression<Func<T, TKey>> keySelector)
        {
            if (keySelector != null)
            {
                _groupByExpressions.Add(GetColumnName(keySelector.Body));
            }

            return new GroupedExpressionToSql<T, TKey>(this, keySelector!);
        }

        /// <summary>Adds GROUP BY column.</summary>
        public new ExpressionToSql<T> AddGroupBy(string columnName)
        {
            base.AddGroupBy(columnName);
            return this;
        }

        /// <summary>Adds HAVING condition.</summary>
        public ExpressionToSql<T> Having(Expression<Func<T, bool>> predicate)
        {
            if (predicate != null)
            {
                _havingConditions.Add($"({ParseExpression(predicate.Body)})");
            }

            return this;
        }

        #endregion

        #region Parameterized Query

        /// <summary>Enable parameterized query mode for SqlTemplate generation.</summary>
        public ExpressionToSql<T> UseParameterizedQueries()
        {
            _parameterized = true;
            return this;
        }

        #endregion

        #region SQL Generation

        /// <summary>Convert to SQL string.</summary>
        public override string ToSql() => BuildSql();

        /// <summary>Convert to SQL template.</summary>
        public override SqlTemplate ToTemplate() => new(BuildSql(), _parameters);

        /// <summary>Generate WHERE clause part.</summary>
        public override string ToWhereClause() =>
            _whereConditions.Count == 0 ? string.Empty : string.Join(" AND ", _whereConditions);

        /// <summary>Generate additional clauses (GROUP BY, HAVING, ORDER BY, LIMIT, OFFSET).</summary>
        public string ToAdditionalClause()
        {
            var groupBy = _groupByExpressions.Count > 0 ? $" GROUP BY {string.Join(", ", _groupByExpressions)}" : string.Empty;
            var having = _havingConditions.Count > 0 ? $" HAVING {string.Join(" AND ", _havingConditions)}" : string.Empty;
            var orderBy = _orderByExpressions.Count > 0 ? $" ORDER BY {string.Join(", ", _orderByExpressions)}" : string.Empty;
            var pagination = GetPaginationClause();
            return $"{groupBy}{having}{orderBy}{pagination}".TrimStart();
        }

        private string BuildSql() => _operation switch
        {
            SqlOperation.Insert => BuildInsertSql(),
            SqlOperation.Update => BuildUpdateSql(),
            SqlOperation.Delete => BuildDeleteSql(),
            _ => BuildSelectSql()
        };

        private string BuildSelectSql()
        {
            var select = _custom?.Count > 0 ? $"SELECT {string.Join(", ", _custom)}" : "SELECT *";
            var from = $"FROM {_dialect.WrapColumn(_tableName!)}";
            var where = GetWhereClause();
            var groupBy = _groupByExpressions.Count > 0 ? $" GROUP BY {string.Join(", ", _groupByExpressions)}" : string.Empty;
            var having = _havingConditions.Count > 0 ? $" HAVING {string.Join(" AND ", _havingConditions)}" : string.Empty;
            var orderBy = _orderByExpressions.Count > 0 ? $" ORDER BY {string.Join(", ", _orderByExpressions)}" : string.Empty;
            var pagination = GetPaginationClause();

            return $"{select} {from}{where}{groupBy}{having}{orderBy}{pagination}";
        }

        private string BuildInsertSql()
        {
            var table = _dialect.WrapColumn(_tableName!);
            var columns = _columns.Count > 0 ? $" ({string.Join(", ", _columns)})" : string.Empty;
            var values = !string.IsNullOrEmpty(_selectSql) ? $" {_selectSql}" : GetValuesClause();
            return $"INSERT INTO {table}{columns}{values}";
        }

        private string BuildUpdateSql()
        {
            var table = _dialect.WrapColumn(_tableName!);
            var setClause = string.Join(", ", _sets.Concat(_expressions));
            return $"UPDATE {table} SET {setClause}{GetWhereClause()}";
        }

        private string BuildDeleteSql()
        {
            if (_whereConditions.Count == 0)
            {
                throw new InvalidOperationException("DELETE operation requires WHERE clause for safety.");
            }

            return $"DELETE FROM {_dialect.WrapColumn(_tableName!)}{GetWhereClause()}";
        }

        private string GetWhereClause() =>
            _whereConditions.Count > 0
                ? $" WHERE {string.Join(" AND ", _whereConditions.Select(RemoveOuterParentheses))}"
                : string.Empty;

        private string GetPaginationClause()
        {
            if (!_skip.HasValue && !_take.HasValue)
            {
                return string.Empty;
            }

            if (_dialect.DatabaseType == "SqlServer")
            {
                var offset = $" OFFSET {_skip ?? 0} ROWS";
                var fetch = _take.HasValue ? $" FETCH NEXT {_take.Value} ROWS ONLY" : string.Empty;
                return $"{offset}{fetch}";
            }

            var limit = _take.HasValue ? $" LIMIT {_take.Value}" : string.Empty;
            var skip = _skip.HasValue ? $" OFFSET {_skip.Value}" : string.Empty;
            return $"{limit}{skip}";
        }

        private string GetValuesClause()
        {
            if (_values.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder(" VALUES ", _values.Count * 30);
            for (var i = 0; i < _values.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }

                sb.Append('(');
                var vals = _values[i];
                for (var j = 0; j < vals.Count; j++)
                {
                    if (j > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(vals[j]);
                }

                sb.Append(')');
            }

            return sb.ToString();
        }

        #endregion
    }


    /// <summary>
    /// Grouped query object supporting aggregation operations.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <typeparam name="TKey">Grouping key type.</typeparam>
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
            _keyColumnName = keySelector != null ? GetColumnName(keySelector.Body) : string.Empty;
        }

        /// <summary>Selects grouped result projection.</summary>
        public ExpressionToSql<TResult> Select<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        TResult>(Expression<Func<IGrouping<TKey, T>, TResult>> selector)
        {
            var resultQuery = ExpressionToSql<TResult>.Create(_baseQuery._dialect);
            resultQuery.SetCustomSelectClause(BuildSelectClause(selector.Body));
            CopyBaseQueryInfo(resultQuery);
            return resultQuery;
        }

        /// <summary>Add HAVING conditions.</summary>
        public GroupedExpressionToSql<T, TKey> Having(Expression<Func<IGrouping<TKey, T>, bool>> predicate)
        {
            _baseQuery.AddHavingCondition(ParseHavingExpression(predicate.Body));
            return this;
        }

        /// <summary>Convert to SQL query string.</summary>
        public override string ToSql() => _baseQuery.ToSql();

        /// <summary>Convert to SQL template.</summary>
        public override SqlTemplate ToTemplate() => _baseQuery.ToTemplate();

        #region SELECT Clause Building

        private List<string> BuildSelectClause(Expression expression)
        {
            var selectClause = new List<string>();

            switch (expression)
            {
                case NewExpression newExpr:
                    for (var i = 0; i < newExpr.Arguments.Count; i++)
                    {
                        var memberName = newExpr.Members?[i]?.Name ?? $"Column{i}";
                        selectClause.Add($"{ParseSelectExpression(newExpr.Arguments[i])} AS {memberName}");
                    }

                    break;

                case MemberInitExpression memberInit:
                    foreach (var binding in memberInit.Bindings)
                    {
                        if (binding is MemberAssignment assignment)
                        {
                            selectClause.Add($"{ParseSelectExpression(assignment.Expression)} AS {assignment.Member.Name}");
                        }
                    }

                    break;

                default:
                    selectClause.Add(ParseSelectExpression(expression));
                    break;
            }

            return selectClause;
        }

        private string ParseSelectExpression(Expression expression) => expression switch
        {
            MethodCallExpression methodCall => ParseAggregateFunction(methodCall),
            MemberExpression { Expression: ParameterExpression { Name: "g" } } member =>
                member.Member.Name == "Key" ? _keyColumnName : "NULL",
            BinaryExpression binary => ParseBinarySelectExpression(binary),
            ConstantExpression constant => FormatConstantValue(constant.Value),
            ConditionalExpression cond => $"CASE WHEN {ParseSelectExpression(cond.Test)} THEN {ParseSelectExpression(cond.IfTrue)} ELSE {ParseSelectExpression(cond.IfFalse)} END",
            UnaryExpression { NodeType: ExpressionType.Convert } unary => ParseSelectExpression(unary.Operand),
            _ => TryParseExpression(expression)
        };

        private string ParseBinarySelectExpression(BinaryExpression binary)
        {
            var left = ParseSelectExpression(binary.Left);
            var right = ParseSelectExpression(binary.Right);
            return binary.NodeType == ExpressionType.Coalesce
                ? $"COALESCE({left}, {right})"
                : $"({left} {GetBinaryOperator(binary.NodeType)} {right})";
        }

        private string TryParseExpression(Expression expression)
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

        #endregion

        #region Aggregate Functions

        private string ParseAggregateFunction(MethodCallExpression methodCall)
        {
            var name = methodCall.Method.Name;
            var argCount = methodCall.Arguments.Count;

            return (name, argCount) switch
            {
                ("Count", _) => "COUNT(*)",
                ("Sum", > 1) => $"SUM({ParseLambdaBodyEnhanced(methodCall.Arguments[1])})",
                ("Average" or "Avg", > 1) => $"AVG({ParseLambdaBodyEnhanced(methodCall.Arguments[1])})",
                ("Max", > 1) => $"MAX({ParseLambdaBodyEnhanced(methodCall.Arguments[1])})",
                ("Min", > 1) => $"MIN({ParseLambdaBodyEnhanced(methodCall.Arguments[1])})",
                _ => throw new NotSupportedException($"Aggregate function {name} is not supported")
            };
        }

        private string ParseLambdaBodyEnhanced(Expression expression) => expression switch
        {
            LambdaExpression lambda => ParseAggregateBody(lambda.Body),
            UnaryExpression { NodeType: ExpressionType.Quote, Operand: LambdaExpression quotedLambda } =>
                ParseAggregateBody(quotedLambda.Body),
            _ => ParseAggregateBody(expression)
        };

        private string ParseAggregateBody(Expression body) => body switch
        {
            MemberExpression member when member.Member.Name == "Length" && member.Member.DeclaringType == typeof(string) =>
                DatabaseType == "SqlServer" ? $"LEN({ParseAggregateBody(member.Expression!)})" : $"LENGTH({ParseAggregateBody(member.Expression!)})",
            MemberExpression member => GetColumnName(member),
            BinaryExpression binary => ParseAggregateBinary(binary),
            MethodCallExpression methodCall => ParseMethodInAggregate(methodCall),
            ConstantExpression constant => FormatConstantValue(constant.Value),
            ConditionalExpression cond => $"CASE WHEN {ParseAggregateBody(cond.Test)} THEN {ParseAggregateBody(cond.IfTrue)} ELSE {ParseAggregateBody(cond.IfFalse)} END",
            UnaryExpression { NodeType: ExpressionType.Convert } unary => ParseAggregateBody(unary.Operand),
            _ => TryGetColumnName(body)
        };

        private string ParseAggregateBinary(BinaryExpression binary)
        {
            var left = ParseAggregateBody(binary.Left);
            var right = ParseAggregateBody(binary.Right);

            return binary.NodeType == ExpressionType.Coalesce
                ? $"COALESCE({left}, {right})"
                : $"({left} {GetBinaryOperator(binary.NodeType)} {right})";
        }

        private string ParseMethodInAggregate(MethodCallExpression methodCall)
        {
            var declaringType = methodCall.Method.DeclaringType;

            if (declaringType == typeof(Math))
            {
                return ParseMathInAggregate(methodCall);
            }

            if (declaringType == typeof(string) && methodCall.Object != null)
            {
                return ParseStringInAggregate(methodCall);
            }

            return methodCall.Object != null ? ParseAggregateBody(methodCall.Object) : "NULL";
        }

        private string ParseMathInAggregate(MethodCallExpression methodCall)
        {
            var name = methodCall.Method.Name;
            var args = methodCall.Arguments;

            return (name, args.Count) switch
            {
                ("Abs", 1) => $"ABS({ParseAggregateBody(args[0])})",
                ("Round", 1) => $"ROUND({ParseAggregateBody(args[0])})",
                ("Round", 2) => $"ROUND({ParseAggregateBody(args[0])}, {ParseAggregateBody(args[1])})",
                ("Floor", 1) => $"FLOOR({ParseAggregateBody(args[0])})",
                ("Ceiling", 1) => DatabaseType == "PostgreSql" ? $"CEIL({ParseAggregateBody(args[0])})" : $"CEILING({ParseAggregateBody(args[0])})",
                ("Min", 2) => $"LEAST({ParseAggregateBody(args[0])}, {ParseAggregateBody(args[1])})",
                ("Max", 2) => $"GREATEST({ParseAggregateBody(args[0])}, {ParseAggregateBody(args[1])})",
                ("Pow", 2) => DatabaseType == "MySql"
                    ? $"POW({ParseAggregateBody(args[0])}, {ParseAggregateBody(args[1])})"
                    : $"POWER({ParseAggregateBody(args[0])}, {ParseAggregateBody(args[1])})",
                ("Sqrt", 1) => $"SQRT({ParseAggregateBody(args[0])})",
                _ => args.Count > 0 ? ParseAggregateBody(args[0]) : "NULL"
            };
        }

        private string ParseStringInAggregate(MethodCallExpression methodCall)
        {
            var obj = ParseAggregateBody(methodCall.Object!);
            var name = methodCall.Method.Name;
            var args = methodCall.Arguments;

            return (name, args.Count) switch
            {
                ("Length", 0) => DatabaseType == "SqlServer" ? $"LEN({obj})" : $"LENGTH({obj})",
                ("ToUpper", 0) => $"UPPER({obj})",
                ("ToLower", 0) => $"LOWER({obj})",
                ("Trim", 0) => $"TRIM({obj})",
                ("Substring", 1) => DatabaseType == "SQLite"
                    ? $"SUBSTR({obj}, {ParseAggregateBody(args[0])})"
                    : $"SUBSTRING({obj}, {ParseAggregateBody(args[0])})",
                ("Substring", 2) => DatabaseType == "SQLite"
                    ? $"SUBSTR({obj}, {ParseAggregateBody(args[0])}, {ParseAggregateBody(args[1])})"
                    : $"SUBSTRING({obj}, {ParseAggregateBody(args[0])}, {ParseAggregateBody(args[1])})",
                ("Replace", 2) => $"REPLACE({obj}, {ParseAggregateBody(args[0])}, {ParseAggregateBody(args[1])})",
                _ => obj
            };
        }

        private string TryGetColumnName(Expression expression)
        {
            try
            {
                return GetColumnName(expression);
            }
            catch
            {
                return "NULL";
            }
        }

        #endregion

        #region HAVING Clause

        private string ParseHavingExpression(Expression expression) => expression switch
        {
            BinaryExpression binary => ParseHavingBinary(binary),
            MethodCallExpression methodCall => ParseAggregateFunction(methodCall),
            _ => expression.ToString()
        };

        private string ParseHavingBinary(BinaryExpression binary)
        {
            var left = ParseHavingPart(binary.Left);
            var right = ParseHavingPart(binary.Right);
            return $"{left} {GetBinaryOperator(binary.NodeType)} {right}";
        }

        private string ParseHavingPart(Expression expression) => expression switch
        {
            MethodCallExpression methodCall => ParseAggregateFunction(methodCall),
            ConstantExpression constant => constant.Value?.ToString() ?? "NULL",
            MemberExpression { Expression: ParameterExpression { Name: "g" }, Member.Name: "Key" } => _keyColumnName,
            _ => expression.ToString()
        };

        #endregion

        #region Helper Methods

        private void CopyBaseQueryInfo<TResult>(ExpressionToSql<TResult> resultQuery)
        {
            resultQuery.SetTableName(typeof(T).Name);
            resultQuery.CopyWhereConditions(new List<string>(_baseQuery._whereConditions));

            if (!string.IsNullOrEmpty(_keyColumnName))
            {
                resultQuery.AddGroupBy(_keyColumnName);
            }

            resultQuery.CopyHavingConditions(new List<string>(_baseQuery._havingConditions));
        }

        #endregion
    }

    /// <summary>
    /// Grouping interface similar to LINQ IGrouping.
    /// </summary>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <typeparam name="TElement">Element type.</typeparam>
    public interface IGrouping<out TKey, out TElement>
    {
        /// <summary>Gets the grouping key.</summary>
        TKey Key { get; }
    }

    /// <summary>
    /// Extensions for grouping operations (expression tree parsing only).
    /// </summary>
    public static class GroupingExtensions
    {
        /// <summary>Count aggregation.</summary>
        public static int Count<TKey, TElement>(this IGrouping<TKey, TElement> grouping) => default;

        /// <summary>Sum aggregation.</summary>
        public static TResult Sum<TKey, TElement, TResult>(this IGrouping<TKey, TElement> grouping, Expression<Func<TElement, TResult>> selector) => default!;

        /// <summary>Average aggregation for double.</summary>
        public static double Average<TKey, TElement>(this IGrouping<TKey, TElement> grouping, Expression<Func<TElement, double>> selector) => default;

        /// <summary>Average aggregation for decimal.</summary>
        public static double Average<TKey, TElement>(this IGrouping<TKey, TElement> grouping, Expression<Func<TElement, decimal>> selector) => default;

        /// <summary>Max aggregation.</summary>
        public static TResult Max<TKey, TElement, TResult>(this IGrouping<TKey, TElement> grouping, Expression<Func<TElement, TResult>> selector) => default!;

        /// <summary>Min aggregation.</summary>
        public static TResult Min<TKey, TElement, TResult>(this IGrouping<TKey, TElement> grouping, Expression<Func<TElement, TResult>> selector) => default!;
    }
}
