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
    internal enum SqlOperation { Select, Insert, Update, Delete }

    /// <summary>
    /// Placeholder values for dynamic SQL generation.
    /// </summary>
    public static class Any
    {
        /// <summary>Gets a placeholder value of the specified type.</summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <returns>A default value placeholder.</returns>
        public static TValue Value<TValue>() => default!;

        /// <summary>Gets a placeholder value of the specified type with a parameter name.</summary>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="parameterName">The parameter name.</param>
        /// <returns>A default value placeholder.</returns>
        public static TValue Value<TValue>(string parameterName) => default!;

        /// <summary>Gets a string placeholder.</summary>
        /// <returns>A default string placeholder.</returns>
        public static string String() => default!;

        /// <summary>Gets a string placeholder with a parameter name.</summary>
        /// <param name="parameterName">The parameter name.</param>
        /// <returns>A default string placeholder.</returns>
        public static string String(string parameterName) => default!;

        /// <summary>Gets an integer placeholder.</summary>
        /// <returns>A default integer placeholder.</returns>
        public static int Int() => default;

        /// <summary>Gets an integer placeholder with a parameter name.</summary>
        /// <param name="parameterName">The parameter name.</param>
        /// <returns>A default integer placeholder.</returns>
        public static int Int(string parameterName) => default;

        /// <summary>Gets a boolean placeholder.</summary>
        /// <returns>A default boolean placeholder.</returns>
        public static bool Bool() => default;

        /// <summary>Gets a boolean placeholder with a parameter name.</summary>
        /// <param name="parameterName">The parameter name.</param>
        /// <returns>A default boolean placeholder.</returns>
        public static bool Bool(string parameterName) => default;

        /// <summary>Gets a DateTime placeholder.</summary>
        /// <returns>A default DateTime placeholder.</returns>
        public static DateTime DateTime() => default;

        /// <summary>Gets a DateTime placeholder with a parameter name.</summary>
        /// <param name="parameterName">The parameter name.</param>
        /// <returns>A default DateTime placeholder.</returns>
        public static DateTime DateTime(string parameterName) => default;

        /// <summary>Gets a Guid placeholder.</summary>
        /// <returns>A default Guid placeholder.</returns>
        public static Guid Guid() => default;

        /// <summary>Gets a Guid placeholder with a parameter name.</summary>
        /// <param name="parameterName">The parameter name.</param>
        /// <returns>A default Guid placeholder.</returns>
        public static Guid Guid(string parameterName) => default;
    }

    /// <summary>
    /// LINQ Expression to SQL converter (AOT-friendly).
    /// </summary>
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

        private ExpressionToSql(SqlDialect dialect) : base(dialect, typeof(T)) { }

        /// <summary>Creates a new instance with the specified SQL dialect.</summary>
        /// <param name="dialect">The SQL dialect to use.</param>
        /// <returns>A new ExpressionToSql instance.</returns>
        public static ExpressionToSql<T> Create(SqlDialect dialect) => new(dialect);

        /// <summary>Creates a new instance for SQL Server.</summary>
        /// <returns>A new ExpressionToSql instance configured for SQL Server.</returns>
        public static ExpressionToSql<T> ForSqlServer() => new(SqlDefine.SqlServer);

        /// <summary>Creates a new instance for MySQL.</summary>
        /// <returns>A new ExpressionToSql instance configured for MySQL.</returns>
        public static ExpressionToSql<T> ForMySql() => new(SqlDefine.MySql);

        /// <summary>Creates a new instance for PostgreSQL.</summary>
        /// <returns>A new ExpressionToSql instance configured for PostgreSQL.</returns>
        public static ExpressionToSql<T> ForPostgreSQL() => new(SqlDefine.PostgreSql);

        /// <summary>Creates a new instance for SQLite.</summary>
        /// <returns>A new ExpressionToSql instance configured for SQLite.</returns>
        public static ExpressionToSql<T> ForSqlite() => new(SqlDefine.SQLite);

        /// <summary>Creates a new instance for Oracle.</summary>
        /// <returns>A new ExpressionToSql instance configured for Oracle.</returns>
        public static ExpressionToSql<T> ForOracle() => new(SqlDefine.Oracle);

        /// <summary>Creates a new instance for DB2.</summary>
        /// <returns>A new ExpressionToSql instance configured for DB2.</returns>
        public static ExpressionToSql<T> ForDB2() => new(SqlDefine.DB2);

        /// <summary>Specifies columns to select by name.</summary>
        /// <param name="cols">Column names to select.</param>
        /// <returns>The current instance for method chaining.</returns>
        public ExpressionToSql<T> Select(params string[] cols) { _custom = cols?.Length > 0 ? new List<string>(cols) : new List<string>(); return this; }

        /// <summary>Specifies columns to select using a projection expression.</summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="selector">The projection expression.</param>
        /// <returns>A new ExpressionToSql instance with the specified projection.</returns>
        public ExpressionToSql<TResult> Select<TResult>(Expression<Func<T, TResult>> selector)
        {
            var result = ExpressionToSql<TResult>.Create(_dialect);
            result._custom = selector != null ? ExtractColumns(selector.Body) : new List<string>();
            result.SetTableName(_tableName!);
            result.CopyWhereConditions(new List<string>(_whereConditions));
            result._orderByExpressions.AddRange(_orderByExpressions);
            result._groupByExpressions.AddRange(_groupByExpressions);
            result._havingConditions.AddRange(_havingConditions);
            result._take = _take;
            result._skip = _skip;
            result._parameterized = _parameterized;
            return result;
        }

        /// <summary>Specifies columns to select using multiple expressions.</summary>
        /// <param name="selectors">The column selector expressions.</param>
        /// <returns>The current instance for method chaining.</returns>
        public ExpressionToSql<T> Select(params Expression<Func<T, object>>[] selectors)
        {
            if (selectors == null || selectors.Length == 0) { _custom = new List<string>(0); return this; }
            var result = new List<string>(selectors.Length * 2);
            foreach (var s in selectors) if (s != null) result.AddRange(ExtractColumns(s.Body));
            _custom = result;
            return this;
        }
        internal void SetCustomSelectClause(List<string> clause) => _custom = clause;

        /// <summary>Adds a WHERE condition using a predicate expression.</summary>
        /// <param name="predicate">The filter predicate.</param>
        /// <returns>The current instance for method chaining.</returns>
        public ExpressionToSql<T> Where(Expression<Func<T, bool>> predicate) { if (predicate != null) _whereConditions.Add($"({ParseExpression(predicate.Body)})"); return this; }

        /// <summary>Adds an additional AND condition to the WHERE clause.</summary>
        /// <param name="predicate">The filter predicate.</param>
        /// <returns>The current instance for method chaining.</returns>
        public ExpressionToSql<T> And(Expression<Func<T, bool>> predicate) => Where(predicate);

        /// <summary>Adds an ascending ORDER BY clause.</summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <param name="k">The key selector expression.</param>
        /// <returns>The current instance for method chaining.</returns>
        public ExpressionToSql<T> OrderBy<TKey>(Expression<Func<T, TKey>> k) { if (k != null) _orderByExpressions.Add($"{GetColumnName(k.Body)} ASC"); return this; }

        /// <summary>Adds a descending ORDER BY clause.</summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <param name="k">The key selector expression.</param>
        /// <returns>The current instance for method chaining.</returns>
        public ExpressionToSql<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> k) { if (k != null) _orderByExpressions.Add($"{GetColumnName(k.Body)} DESC"); return this; }

        /// <summary>Limits the number of rows returned.</summary>
        /// <param name="take">The maximum number of rows.</param>
        /// <returns>The current instance for method chaining.</returns>
        public ExpressionToSql<T> Take(int take) { _take = take; return this; }

        /// <summary>Skips a specified number of rows.</summary>
        /// <param name="skip">The number of rows to skip.</param>
        /// <returns>The current instance for method chaining.</returns>
        public ExpressionToSql<T> Skip(int skip) { _skip = skip; return this; }

        /// <summary>Sets the operation to UPDATE.</summary>
        /// <returns>The current instance for method chaining.</returns>
        public ExpressionToSql<T> Update() { _operation = SqlOperation.Update; return this; }

        /// <summary>Sets a column value for UPDATE using a constant value.</summary>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="selector">The column selector.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>The current instance for method chaining.</returns>
        public ExpressionToSql<T> Set<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        {
            _operation = SqlOperation.Update;
            if (selector != null) _sets.Add($"{GetColumnName(selector.Body)} = {FormatConstantValue(value)}");
            return this;
        }

        /// <summary>Sets a column value for UPDATE using an expression.</summary>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="selector">The column selector.</param>
        /// <param name="valueExpr">The value expression.</param>
        /// <returns>The current instance for method chaining.</returns>
        public ExpressionToSql<T> Set<TValue>(Expression<Func<T, TValue>> selector, Expression<Func<T, TValue>> valueExpr)
        {
            _operation = SqlOperation.Update;
            if (selector != null && valueExpr != null) _expressions.Add($"{GetColumnName(selector.Body)} = {ParseExpression(valueExpr.Body)}");
            return this;
        }

        /// <summary>Sets the operation to INSERT with optional column selection.</summary>
        /// <param name="selector">Optional column selector.</param>
        /// <returns>The current instance for method chaining.</returns>
        public ExpressionToSql<T> Insert(Expression<Func<T, object>>? selector = null)
        {
            _operation = SqlOperation.Insert;
            if (selector != null) { _columns.Clear(); _columns.AddRange(ExtractColumns(selector.Body)); }
            return this;
        }

        /// <summary>Sets the operation to INSERT with a SELECT statement.</summary>
        /// <param name="sql">The SELECT SQL statement.</param>
        /// <returns>The current instance for method chaining.</returns>
        public ExpressionToSql<T> InsertSelect(string sql) { _operation = SqlOperation.Insert; _selectSql = sql; return this; }

        /// <summary>Adds values for INSERT.</summary>
        /// <param name="values">The values to insert.</param>
        /// <returns>The current instance for method chaining.</returns>
        public ExpressionToSql<T> Values(params object[] values) => AddValues(values);

        /// <summary>Adds a row of values for INSERT.</summary>
        /// <param name="values">The values to insert.</param>
        /// <returns>The current instance for method chaining.</returns>
        public ExpressionToSql<T> AddValues(params object[] values)
        {
            if (values?.Length > 0) _values.Add(values.Select(v => FormatConstantValue(v)).ToList());
            return this;
        }

        /// <summary>Sets the operation to DELETE.</summary>
        /// <returns>The current instance for method chaining.</returns>
        public ExpressionToSql<T> Delete() { _operation = SqlOperation.Delete; return this; }

        /// <summary>Sets the operation to DELETE with a WHERE condition.</summary>
        /// <param name="predicate">The filter predicate.</param>
        /// <returns>The current instance for method chaining.</returns>
        public ExpressionToSql<T> Delete(Expression<Func<T, bool>> predicate) { _operation = SqlOperation.Delete; return predicate != null ? Where(predicate) : this; }

        /// <summary>Groups results by the specified key.</summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <param name="k">The key selector expression.</param>
        /// <returns>A GroupedExpressionToSql instance for aggregation.</returns>
        public GroupedExpressionToSql<T, TKey> GroupBy<TKey>(Expression<Func<T, TKey>> k)
        {
            if (k != null) _groupByExpressions.Add(GetColumnName(k.Body));
            return new GroupedExpressionToSql<T, TKey>(this, k!);
        }

        /// <summary>Adds a GROUP BY column.</summary>
        /// <param name="col">The column name.</param>
        /// <returns>The current instance for method chaining.</returns>
        public new ExpressionToSql<T> AddGroupBy(string col) { base.AddGroupBy(col); return this; }

        /// <summary>Adds a HAVING condition.</summary>
        /// <param name="predicate">The filter predicate.</param>
        /// <returns>The current instance for method chaining.</returns>
        public ExpressionToSql<T> Having(Expression<Func<T, bool>> predicate) { if (predicate != null) _havingConditions.Add($"({ParseExpression(predicate.Body)})"); return this; }

        /// <summary>Enables parameterized query generation.</summary>
        /// <returns>The current instance for method chaining.</returns>
        public ExpressionToSql<T> UseParameterizedQueries() { _parameterized = true; return this; }

        /// <summary>Generates the SQL statement.</summary>
        /// <returns>The generated SQL string.</returns>
        public override string ToSql() => BuildSql();

        /// <summary>Generates the SQL template.</summary>
        /// <returns>The generated SQL template string.</returns>
        public override string ToTemplate() => BuildSql();

        /// <summary>Gets the WHERE clause without the WHERE keyword.</summary>
        /// <returns>The WHERE conditions joined by AND.</returns>
        public override string ToWhereClause() => _whereConditions.Count == 0 ? string.Empty : string.Join(" AND ", _whereConditions);

        /// <summary>Gets the SET clause for UPDATE statements.</summary>
        /// <returns>The SET assignments joined by comma.</returns>
        public string ToSetClause() { var all = _sets.Concat(_expressions).ToList(); return all.Count == 0 ? string.Empty : string.Join(", ", all); }

        /// <summary>Gets additional clauses (GROUP BY, HAVING, ORDER BY, pagination).</summary>
        /// <returns>The additional SQL clauses.</returns>
        public string ToAdditionalClause()
        {
            var g = _groupByExpressions.Count > 0 ? $" GROUP BY {string.Join(", ", _groupByExpressions)}" : "";
            var h = _havingConditions.Count > 0 ? $" HAVING {string.Join(" AND ", _havingConditions)}" : "";
            var o = _orderByExpressions.Count > 0 ? $" ORDER BY {string.Join(", ", _orderByExpressions)}" : "";
            return $"{g}{h}{o}{GetPaginationClause()}".TrimStart();
        }

        private string BuildSql() => _operation switch
        {
            SqlOperation.Insert => BuildInsert(),
            SqlOperation.Update => BuildUpdate(),
            SqlOperation.Delete => BuildDelete(),
            _ => BuildSelect()
        };

        private string BuildSelect()
        {
            var sel = _custom?.Count > 0 ? $"SELECT {string.Join(", ", _custom)}" : "SELECT *";
            var frm = $"FROM {_dialect.WrapColumn(_tableName!)}";
            var whr = GetWhereClause();
            var grp = _groupByExpressions.Count > 0 ? $" GROUP BY {string.Join(", ", _groupByExpressions)}" : "";
            var hav = _havingConditions.Count > 0 ? $" HAVING {string.Join(" AND ", _havingConditions)}" : "";
            var ord = _orderByExpressions.Count > 0 ? $" ORDER BY {string.Join(", ", _orderByExpressions)}" : "";
            return $"{sel} {frm}{whr}{grp}{hav}{ord}{GetPaginationClause()}";
        }

        private string BuildInsert()
        {
            var tbl = _dialect.WrapColumn(_tableName!);
            var cols = _columns.Count > 0 ? $" ({string.Join(", ", _columns)})" : "";
            var vals = !string.IsNullOrEmpty(_selectSql) ? $" {_selectSql}" : GetValuesClause();
            return $"INSERT INTO {tbl}{cols}{vals}";
        }

        private string BuildUpdate()
        {
            var tbl = _dialect.WrapColumn(_tableName!);
            var set = string.Join(", ", _sets.Concat(_expressions));
            return $"UPDATE {tbl} SET {set}{GetWhereClause()}";
        }

        private string BuildDelete()
        {
            if (_whereConditions.Count == 0) throw new InvalidOperationException("DELETE requires WHERE clause.");
            return $"DELETE FROM {_dialect.WrapColumn(_tableName!)}{GetWhereClause()}";
        }

        private string GetWhereClause() => _whereConditions.Count > 0
            ? $" WHERE {string.Join(" AND ", _whereConditions.Select(RemoveOuterParentheses))}" : "";

        private string GetPaginationClause()
        {
            if (!_skip.HasValue && !_take.HasValue) return "";
            if (_dialect.DatabaseType == "SqlServer")
                return $" OFFSET {_skip ?? 0} ROWS{(_take.HasValue ? $" FETCH NEXT {_take.Value} ROWS ONLY" : "")}";
            return $"{(_take.HasValue ? $" LIMIT {_take.Value}" : "")}{(_skip.HasValue ? $" OFFSET {_skip.Value}" : "")}";
        }

        private string GetValuesClause()
        {
            if (_values.Count == 0) return "";
            var sb = new StringBuilder(" VALUES ");
            for (var i = 0; i < _values.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append('(').Append(string.Join(", ", _values[i])).Append(')');
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Grouped query with aggregation support.
    /// </summary>
    public class GroupedExpressionToSql<
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
    T, TKey> : ExpressionToSqlBase
    {
        private readonly ExpressionToSql<T> _baseQuery;
        private readonly string _keyCol;
        private readonly Expressions.ExpressionParser _parser;

        internal GroupedExpressionToSql(ExpressionToSql<T> baseQuery, Expression<Func<T, TKey>> keySelector)
            : base(baseQuery._dialect, typeof(T))
        {
            _baseQuery = baseQuery;
            _keyCol = keySelector != null ? GetColumnName(keySelector.Body) : "";
            _parser = new Expressions.ExpressionParser(_dialect, _parameters, _parameterized);
        }

        /// <summary>Projects the grouped results using a selector expression.</summary>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="selector">The projection expression.</param>
        /// <returns>A new ExpressionToSql instance with the projection.</returns>
        public ExpressionToSql<TResult> Select<
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
#endif
        TResult>(Expression<Func<IGrouping<TKey, T>, TResult>> selector)
        {
            var q = ExpressionToSql<TResult>.Create(_baseQuery._dialect);
            q.SetCustomSelectClause(BuildSelectClause(selector.Body));
            CopyTo(q);
            return q;
        }

        /// <summary>Adds a HAVING condition to the grouped query.</summary>
        /// <param name="predicate">The filter predicate.</param>
        /// <returns>The current instance for method chaining.</returns>
        public GroupedExpressionToSql<T, TKey> Having(Expression<Func<IGrouping<TKey, T>, bool>> predicate)
        {
            _baseQuery.AddHavingCondition(ParseHaving(predicate.Body));
            return this;
        }

        /// <summary>Generates the SQL statement.</summary>
        /// <returns>The generated SQL string.</returns>
        public override string ToSql() => _baseQuery.ToSql();

        /// <summary>Generates the SQL template.</summary>
        /// <returns>The generated SQL template string.</returns>
        public override string ToTemplate() => _baseQuery.ToTemplate();

        private List<string> BuildSelectClause(Expression e)
        {
            var list = new List<string>();
            switch (e)
            {
                case NewExpression n:
                    for (var i = 0; i < n.Arguments.Count; i++)
                        list.Add($"{ParseSelect(n.Arguments[i])} AS {n.Members?[i]?.Name ?? $"Column{i}"}");
                    break;
                case MemberInitExpression m:
                    foreach (var b in m.Bindings)
                        if (b is MemberAssignment a) list.Add($"{ParseSelect(a.Expression)} AS {a.Member.Name}");
                    break;
                default:
                    list.Add(ParseSelect(e));
                    break;
            }
            return list;
        }

        private string ParseSelect(Expression e) => e switch
        {
            MethodCallExpression m => ParseAggregate(m),
            MemberExpression { Expression: ParameterExpression { Name: "g" } } m => m.Member.Name == "Key" ? _keyCol : "NULL",
            BinaryExpression b => b.NodeType == ExpressionType.Coalesce
                ? $"COALESCE({ParseSelect(b.Left)}, {ParseSelect(b.Right)})"
                : $"({ParseSelect(b.Left)} {GetBinaryOperator(b.NodeType)} {ParseSelect(b.Right)})",
            ConstantExpression c => FormatConstantValue(c.Value),
            ConditionalExpression c => $"CASE WHEN {ParseSelect(c.Test)} THEN {ParseSelect(c.IfTrue)} ELSE {ParseSelect(c.IfFalse)} END",
            UnaryExpression { NodeType: ExpressionType.Convert } u => ParseSelect(u.Operand),
            _ => TryParse(e)
        };

        private string ParseAggregate(MethodCallExpression m) => (m.Method.Name, m.Arguments.Count) switch
        {
            ("Count", _) => "COUNT(*)",
            ("Sum", > 1) => $"SUM({ParseBody(m.Arguments[1])})",
            ("Average" or "Avg", > 1) => $"AVG({ParseBody(m.Arguments[1])})",
            ("Max", > 1) => $"MAX({ParseBody(m.Arguments[1])})",
            ("Min", > 1) => $"MIN({ParseBody(m.Arguments[1])})",
            _ => throw new NotSupportedException($"Aggregate {m.Method.Name} not supported")
        };

        private string ParseBody(Expression e) => e switch
        {
            LambdaExpression l => ParseAggBody(l.Body),
            UnaryExpression { NodeType: ExpressionType.Quote, Operand: LambdaExpression l } => ParseAggBody(l.Body),
            _ => ParseAggBody(e)
        };

        private string ParseAggBody(Expression e) => e switch
        {
            MemberExpression m when m.Member.Name == "Length" && m.Member.DeclaringType == typeof(string) =>
                DatabaseType == "SqlServer" ? $"LEN({ParseAggBody(m.Expression!)})" : $"LENGTH({ParseAggBody(m.Expression!)})",
            MemberExpression m => GetColumnName(m),
            BinaryExpression b => b.NodeType == ExpressionType.Coalesce
                ? $"COALESCE({ParseAggBody(b.Left)}, {ParseAggBody(b.Right)})"
                : $"({ParseAggBody(b.Left)} {GetBinaryOperator(b.NodeType)} {ParseAggBody(b.Right)})",
            MethodCallExpression m => ParseMethodInAgg(m),
            ConstantExpression c => FormatConstantValue(c.Value),
            ConditionalExpression c => $"CASE WHEN {ParseAggBody(c.Test)} THEN {ParseAggBody(c.IfTrue)} ELSE {ParseAggBody(c.IfFalse)} END",
            UnaryExpression { NodeType: ExpressionType.Convert } u => ParseAggBody(u.Operand),
            _ => TryGetCol(e)
        };

        private string ParseMethodInAgg(MethodCallExpression m)
        {
            // Delegate to shared FunctionParsers via ExpressionParser
            if (m.Method.DeclaringType == typeof(Math))
                return Expressions.MathFunctionParser.Parse(_parser, m);
            if (m.Method.DeclaringType == typeof(string) && m.Object != null)
                return Expressions.StringFunctionParser.Parse(_parser, m);
            return m.Object != null ? ParseAggBody(m.Object) : "NULL";
        }

        private string ParseHaving(Expression e) => e switch
        {
            BinaryExpression b => $"{ParseHavingPart(b.Left)} {GetBinaryOperator(b.NodeType)} {ParseHavingPart(b.Right)}",
            MethodCallExpression m => ParseAggregate(m),
            _ => e.ToString()
        };

        private string ParseHavingPart(Expression e) => e switch
        {
            MethodCallExpression m => ParseAggregate(m),
            ConstantExpression c => c.Value?.ToString() ?? "NULL",
            MemberExpression { Expression: ParameterExpression { Name: "g" }, Member.Name: "Key" } => _keyCol,
            _ => e.ToString()
        };

        private string TryParse(Expression e) { try { return ParseExpressionRaw(e); } catch { return "NULL"; } }
        private string TryGetCol(Expression e) { try { return GetColumnName(e); } catch { return "NULL"; } }

        private void CopyTo<TResult>(ExpressionToSql<TResult> q)
        {
            q.SetTableName(typeof(T).Name);
            q.CopyWhereConditions(new List<string>(_baseQuery._whereConditions));
            if (!string.IsNullOrEmpty(_keyCol)) q.AddGroupBy(_keyCol);
            q.CopyHavingConditions(new List<string>(_baseQuery._havingConditions));
        }
    }

    /// <summary>Grouping interface for expression tree parsing.</summary>
    /// <typeparam name="TKey">The type of the grouping key.</typeparam>
    /// <typeparam name="TElement">The type of the grouped elements.</typeparam>
    public interface IGrouping<out TKey, out TElement>
    {
        /// <summary>Gets the grouping key.</summary>
        TKey Key { get; }
    }

    /// <summary>Aggregation extensions for grouped queries (expression tree parsing only).</summary>
    public static class GroupingExtensions
    {
        /// <summary>Counts the elements in the group.</summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TElement">The element type.</typeparam>
        /// <param name="g">The grouping.</param>
        /// <returns>The count of elements.</returns>
        public static int Count<TKey, TElement>(this IGrouping<TKey, TElement> g) => default;

        /// <summary>Computes the sum of a numeric property in the group.</summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TElement">The element type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="g">The grouping.</param>
        /// <param name="s">The selector expression.</param>
        /// <returns>The sum of the selected values.</returns>
        public static TResult Sum<TKey, TElement, TResult>(this IGrouping<TKey, TElement> g, Expression<Func<TElement, TResult>> s) => default!;

        /// <summary>Computes the average of a double property in the group.</summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TElement">The element type.</typeparam>
        /// <param name="g">The grouping.</param>
        /// <param name="s">The selector expression.</param>
        /// <returns>The average of the selected values.</returns>
        public static double Average<TKey, TElement>(this IGrouping<TKey, TElement> g, Expression<Func<TElement, double>> s) => default;

        /// <summary>Computes the average of a decimal property in the group.</summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TElement">The element type.</typeparam>
        /// <param name="g">The grouping.</param>
        /// <param name="s">The selector expression.</param>
        /// <returns>The average of the selected values.</returns>
        public static double Average<TKey, TElement>(this IGrouping<TKey, TElement> g, Expression<Func<TElement, decimal>> s) => default;

        /// <summary>Gets the maximum value of a property in the group.</summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TElement">The element type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="g">The grouping.</param>
        /// <param name="s">The selector expression.</param>
        /// <returns>The maximum value.</returns>
        public static TResult Max<TKey, TElement, TResult>(this IGrouping<TKey, TElement> g, Expression<Func<TElement, TResult>> s) => default!;

        /// <summary>Gets the minimum value of a property in the group.</summary>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TElement">The element type.</typeparam>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <param name="g">The grouping.</param>
        /// <param name="s">The selector expression.</param>
        /// <returns>The minimum value.</returns>
        public static TResult Min<TKey, TElement, TResult>(this IGrouping<TKey, TElement> g, Expression<Func<TElement, TResult>> s) => default!;
    }
}
