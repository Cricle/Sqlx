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
        public static TValue Value<TValue>() => default!;

        /// <summary>Gets a placeholder value of the specified type with a parameter name.</summary>
        public static TValue Value<TValue>(string parameterName) => default!;

        /// <summary>Gets a string placeholder.</summary>
        public static string String() => default!;

        /// <summary>Gets a string placeholder with a parameter name.</summary>
        public static string String(string parameterName) => default!;

        /// <summary>Gets an integer placeholder.</summary>
        public static int Int() => default;

        /// <summary>Gets an integer placeholder with a parameter name.</summary>
        public static int Int(string parameterName) => default;

        /// <summary>Gets a boolean placeholder.</summary>
        public static bool Bool() => default;

        /// <summary>Gets a boolean placeholder with a parameter name.</summary>
        public static bool Bool(string parameterName) => default;

        /// <summary>Gets a DateTime placeholder.</summary>
        public static DateTime DateTime() => default;

        /// <summary>Gets a DateTime placeholder with a parameter name.</summary>
        public static DateTime DateTime(string parameterName) => default;

        /// <summary>Gets a Guid placeholder.</summary>
        public static Guid Guid() => default;

        /// <summary>Gets a Guid placeholder with a parameter name.</summary>
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

        #region Factory Methods

        /// <summary>Creates a new instance with the specified SQL dialect.</summary>
        public static ExpressionToSql<T> Create(SqlDialect dialect) => new(dialect);

        /// <summary>Creates a new instance for SQL Server.</summary>
        public static ExpressionToSql<T> ForSqlServer() => new(SqlDefine.SqlServer);

        /// <summary>Creates a new instance for MySQL.</summary>
        public static ExpressionToSql<T> ForMySql() => new(SqlDefine.MySql);

        /// <summary>Creates a new instance for PostgreSQL.</summary>
        public static ExpressionToSql<T> ForPostgreSQL() => new(SqlDefine.PostgreSql);

        /// <summary>Creates a new instance for SQLite.</summary>
        public static ExpressionToSql<T> ForSqlite() => new(SqlDefine.SQLite);

        /// <summary>Creates a new instance for Oracle.</summary>
        public static ExpressionToSql<T> ForOracle() => new(SqlDefine.Oracle);

        /// <summary>Creates a new instance for DB2.</summary>
        public static ExpressionToSql<T> ForDB2() => new(SqlDefine.DB2);

        #endregion

        #region SELECT

        /// <summary>Specifies columns to select by name.</summary>
        public ExpressionToSql<T> Select(params string[] cols)
        {
            _custom = cols?.Length > 0 ? new List<string>(cols) : new List<string>();
            return this;
        }

        /// <summary>Specifies columns to select using a projection expression.</summary>
        public ExpressionToSql<TResult> Select<TResult>(Expression<Func<T, TResult>> selector)
        {
            var result = ExpressionToSql<TResult>.Create(_dialect);
            result._custom = selector != null ? ExtractColumns(selector.Body) : new List<string>();
            result.SetTableName(_tableName!);
            CopyStateTo(result);
            return result;
        }

        /// <summary>Specifies columns to select using multiple expressions.</summary>
        public ExpressionToSql<T> Select(params Expression<Func<T, object>>[] selectors)
        {
            if (selectors == null || selectors.Length == 0) { _custom = new List<string>(0); return this; }
            var result = new List<string>(selectors.Length * 2);
            foreach (var s in selectors) if (s != null) result.AddRange(ExtractColumns(s.Body));
            _custom = result;
            return this;
        }

        internal void SetCustomSelectClause(List<string> clause) => _custom = clause;

        #endregion

        #region WHERE / AND

        /// <summary>Adds a WHERE condition using a predicate expression.</summary>
        public ExpressionToSql<T> Where(Expression<Func<T, bool>> predicate)
        {
            if (predicate != null) _whereConditions.Add($"({ParseExpression(predicate.Body)})");
            return this;
        }

        /// <summary>Adds an additional AND condition to the WHERE clause.</summary>
        public ExpressionToSql<T> And(Expression<Func<T, bool>> predicate) => Where(predicate);

        #endregion

        #region ORDER BY

        /// <summary>Adds an ascending ORDER BY clause.</summary>
        public ExpressionToSql<T> OrderBy<TKey>(Expression<Func<T, TKey>> k)
        {
            if (k != null) _orderByExpressions.Add($"{GetColumnName(k.Body)} ASC");
            return this;
        }

        /// <summary>Adds a descending ORDER BY clause.</summary>
        public ExpressionToSql<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> k)
        {
            if (k != null) _orderByExpressions.Add($"{GetColumnName(k.Body)} DESC");
            return this;
        }

        #endregion

        #region Pagination

        /// <summary>Limits the number of rows returned.</summary>
        public ExpressionToSql<T> Take(int take) { _take = take; return this; }

        /// <summary>Skips a specified number of rows.</summary>
        public ExpressionToSql<T> Skip(int skip) { _skip = skip; return this; }

        #endregion

        #region UPDATE / SET

        /// <summary>Sets the operation to UPDATE.</summary>
        public ExpressionToSql<T> Update() { _operation = SqlOperation.Update; return this; }

        /// <summary>Sets a column value for UPDATE using a constant value.</summary>
        public ExpressionToSql<T> Set<TValue>(Expression<Func<T, TValue>> selector, TValue value)
        {
            _operation = SqlOperation.Update;
            if (selector != null) _sets.Add($"{GetColumnName(selector.Body)} = {FormatConstantValue(value)}");
            return this;
        }

        /// <summary>Sets a column value for UPDATE using an expression.</summary>
        public ExpressionToSql<T> Set<TValue>(Expression<Func<T, TValue>> selector, Expression<Func<T, TValue>> valueExpr)
        {
            _operation = SqlOperation.Update;
            if (selector != null && valueExpr != null)
                _expressions.Add($"{GetColumnName(selector.Body)} = {ParseExpression(valueExpr.Body)}");
            return this;
        }

        #endregion

        #region INSERT

        /// <summary>Sets the operation to INSERT with optional column selection.</summary>
        public ExpressionToSql<T> Insert(Expression<Func<T, object>>? selector = null)
        {
            _operation = SqlOperation.Insert;
            if (selector != null) { _columns.Clear(); _columns.AddRange(ExtractColumns(selector.Body)); }
            return this;
        }

        /// <summary>Sets the operation to INSERT with a SELECT statement.</summary>
        public ExpressionToSql<T> InsertSelect(string sql) { _operation = SqlOperation.Insert; _selectSql = sql; return this; }

        /// <summary>Adds values for INSERT.</summary>
        public ExpressionToSql<T> Values(params object[] values) => AddValues(values);

        /// <summary>Adds a row of values for INSERT.</summary>
        public ExpressionToSql<T> AddValues(params object[] values)
        {
            if (values?.Length > 0) _values.Add(values.Select(v => FormatConstantValue(v)).ToList());
            return this;
        }

        #endregion

        #region DELETE

        /// <summary>Sets the operation to DELETE.</summary>
        public ExpressionToSql<T> Delete() { _operation = SqlOperation.Delete; return this; }

        /// <summary>Sets the operation to DELETE with a WHERE condition.</summary>
        public ExpressionToSql<T> Delete(Expression<Func<T, bool>> predicate)
        {
            _operation = SqlOperation.Delete;
            return predicate != null ? Where(predicate) : this;
        }

        #endregion

        #region GROUP BY / HAVING

        /// <summary>Groups results by the specified key.</summary>
        public GroupedExpressionToSql<T, TKey> GroupBy<TKey>(Expression<Func<T, TKey>> k)
        {
            if (k != null) _groupByExpressions.Add(GetColumnName(k.Body));
            return new GroupedExpressionToSql<T, TKey>(this, k!);
        }

        /// <summary>Adds a GROUP BY column.</summary>
        public new ExpressionToSql<T> AddGroupBy(string col) { base.AddGroupBy(col); return this; }

        /// <summary>Adds a HAVING condition.</summary>
        public ExpressionToSql<T> Having(Expression<Func<T, bool>> predicate)
        {
            if (predicate != null) _havingConditions.Add($"({ParseExpression(predicate.Body)})");
            return this;
        }

        #endregion

        #region Options

        /// <summary>Enables parameterized query generation.</summary>
        public ExpressionToSql<T> UseParameterizedQueries() { _parameterized = true; return this; }

        #endregion

        #region Output Methods

        /// <summary>Generates the SQL statement.</summary>
        public override string ToSql() => BuildSql();

        /// <summary>Generates the SQL template.</summary>
        public override string ToTemplate() => BuildSql();

        /// <summary>Gets the WHERE clause without the WHERE keyword.</summary>
        public override string ToWhereClause() =>
            _whereConditions.Count == 0 ? string.Empty : string.Join(" AND ", _whereConditions);

        /// <summary>Gets the SET clause for UPDATE statements.</summary>
        public string ToSetClause()
        {
            var all = _sets.Concat(_expressions).ToList();
            return all.Count == 0 ? string.Empty : string.Join(", ", all);
        }

        /// <summary>Gets additional clauses (GROUP BY, HAVING, ORDER BY, pagination).</summary>
        public string ToAdditionalClause()
        {
            var g = _groupByExpressions.Count > 0 ? $" GROUP BY {string.Join(", ", _groupByExpressions)}" : "";
            var h = _havingConditions.Count > 0 ? $" HAVING {string.Join(" AND ", _havingConditions)}" : "";
            var o = _orderByExpressions.Count > 0 ? $" ORDER BY {string.Join(", ", _orderByExpressions)}" : "";
            return $"{g}{h}{o}{GetPaginationClause()}".TrimStart();
        }

        #endregion

        #region Private Methods

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

        private void CopyStateTo<TResult>(ExpressionToSql<TResult> target)
        {
            target.CopyWhereConditions(new List<string>(_whereConditions));
            target._orderByExpressions.AddRange(_orderByExpressions);
            target._groupByExpressions.AddRange(_groupByExpressions);
            target._havingConditions.AddRange(_havingConditions);
            target._take = _take;
            target._skip = _skip;
            target._parameterized = _parameterized;
        }

        #endregion
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

        /// <summary>Projects the grouped results.</summary>
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

        /// <summary>Adds a HAVING condition.</summary>
        public GroupedExpressionToSql<T, TKey> Having(Expression<Func<IGrouping<TKey, T>, bool>> predicate)
        {
            _baseQuery.AddHavingCondition(ParseHaving(predicate.Body));
            return this;
        }

        /// <summary>Generates the SQL statement.</summary>
        public override string ToSql() => _baseQuery.ToSql();

        /// <summary>Generates the SQL template.</summary>
        public override string ToTemplate() => _baseQuery.ToTemplate();

        #region Private Methods

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

        #endregion
    }
}
