// -----------------------------------------------------------------------
// <copyright file="ExpressionParser.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Sqlx.Expressions
{
    /// <summary>
    /// Parses LINQ expressions to SQL fragments.
    /// </summary>
    internal sealed class ExpressionParser
    {
        private readonly SqlDialect _dialect;
        private readonly Dictionary<string, object?> _parameters;
        private readonly bool _parameterized;
        private int _counter;

        public ExpressionParser(SqlDialect dialect, Dictionary<string, object?> parameters, bool parameterized)
        {
            _dialect = dialect;
            _parameters = parameters;
            _parameterized = parameterized;
        }

        public string DatabaseType => _dialect.DatabaseType;
        public SqlDialect Dialect => _dialect;

        public string Parse(Expression e, bool asBoolCmp = true) => e switch
        {
            BinaryExpression b => ParseBinary(b),
            MemberExpression m when asBoolCmp && m.Type == typeof(bool) => $"{Col(m)} = {BoolLit(true)}",
            MemberExpression m when ExpressionHelper.IsStringPropertyAccess(m) => StrLen(m),
            MemberExpression m when ExpressionHelper.IsEntityProperty(m) => Col(m),
            MemberExpression m => asBoolCmp ? Col(m) : Fmt(ExpressionHelper.GetMemberValueOptimized(m)),
            ConstantExpression c => Fmt(c.Value),
            UnaryExpression { NodeType: ExpressionType.Not } u => ParseNot(u.Operand),
            UnaryExpression { NodeType: ExpressionType.Convert } u => Parse(u.Operand, asBoolCmp),
            MethodCallExpression m => ParseMethod(m),
            ConditionalExpression c => $"CASE WHEN {Parse(c.Test)} THEN {Parse(c.IfTrue)} ELSE {Parse(c.IfFalse)} END",
            _ => "1=1",
        };

        public string ParseRaw(Expression e) => Parse(e, false);

        public string Col(Expression e) => e switch
        {
            UnaryExpression { NodeType: ExpressionType.Convert } u => Col(u.Operand),
            MemberExpression m => _dialect.WrapColumn(ExpressionHelper.ConvertToSnakeCase(m.Member.Name)),
            _ => ParseRaw(e)
        };

        public string GetColumnName(Expression e) => Col(e);

        public List<string> ExtractColumns(Expression e) => e switch
        {
            NewExpression n => n.Arguments.OfType<MemberExpression>().Select(Col).ToList(),
            MemberExpression m => new List<string> { Col(m) },
            UnaryExpression { NodeType: ExpressionType.Convert } u => ExtractColumns(u.Operand),
            _ => new List<string> { Col(e) }
        };

        public string ParseLambda(Expression e) => e switch
        {
            LambdaExpression l => ParseRaw(l.Body),
            UnaryExpression { NodeType: ExpressionType.Quote, Operand: LambdaExpression l } => ParseRaw(l.Body),
            _ => ParseRaw(e),
        };

        public string FormatConstantValue(object? v) => _parameterized ? CreateParameter(v) : ValueFormatter.FormatAsLiteral(_dialect, v);

        public string CreateParameter(object? v)
        {
            var n = $"{_dialect.ParameterPrefix}p{_parameters.Count}";
            _parameters[n] = v;
            return n;
        }

        private string Fmt(object? v) => FormatConstantValue(v);
        private string BoolLit(bool v) => ValueFormatter.GetBooleanLiteral(_dialect, v);

        private string StrLen(MemberExpression m) => m.Member.Name == "Length"
            ? (DatabaseType == "SqlServer" ? $"LEN({ParseRaw(m.Expression!)})" : $"LENGTH({ParseRaw(m.Expression!)})")
            : Col(m);

        private string ParseBinary(BinaryExpression b)
        {
            // Handle bool comparisons like x.IsActive == true
            if (b.NodeType is ExpressionType.Equal or ExpressionType.NotEqual)
            {
                var op = b.NodeType == ExpressionType.Equal ? "=" : "<>";
                if (ExpressionHelper.IsBooleanMember(b.Left) && b.Right is ConstantExpression { Value: bool rv })
                    return $"{Col(b.Left)} {op} {(rv ? "1" : "0")}";
                if (ExpressionHelper.IsBooleanMember(b.Right) && b.Left is ConstantExpression { Value: bool lv })
                    return $"{Col(b.Right)} {op} {(lv ? "1" : "0")}";
            }

            var left = ParseRaw(b.Left);
            var right = ParseRaw(b.Right);

            // Handle bool member on right side of AND/OR
            if (b.Right is MemberExpression { Type: var rt } rm && rt == typeof(bool) && right == Col(rm))
                right = $"{right} = 1";

            // Handle NULL comparisons
            if (left == "NULL" || right == "NULL")
            {
                var nonNull = left == "NULL" ? right : left;
                if (b.NodeType == ExpressionType.Equal) return $"{nonNull} IS NULL";
                if (b.NodeType == ExpressionType.NotEqual) return $"{nonNull} IS NOT NULL";
            }

            return b.NodeType switch
            {
                ExpressionType.Equal => $"{left} = {right}",
                ExpressionType.NotEqual => DatabaseType == "Oracle" ? $"{left} != {right}" : $"{left} <> {right}",
                ExpressionType.GreaterThan => $"{left} > {right}",
                ExpressionType.GreaterThanOrEqual => $"{left} >= {right}",
                ExpressionType.LessThan => $"{left} < {right}",
                ExpressionType.LessThanOrEqual => $"{left} <= {right}",
                ExpressionType.AndAlso or ExpressionType.OrElse => FormatLogical(b.NodeType == ExpressionType.AndAlso ? "AND" : "OR", left, right, b),
                ExpressionType.Add => ExpressionHelper.IsStringConcatenation(b) ? _dialect.Concat(left, right) : $"({left} + {right})",
                ExpressionType.Subtract => $"({left} - {right})",
                ExpressionType.Multiply => $"({left} * {right})",
                ExpressionType.Divide => $"({left} / {right})",
                ExpressionType.Modulo => DatabaseType == "Oracle" ? $"MOD({left}, {right})" : $"({left} % {right})",
                ExpressionType.Coalesce => $"COALESCE({left}, {right})",
                _ => throw new NotSupportedException($"Binary operator {b.NodeType} is not supported")
            };
        }

        private string FormatLogical(string op, string left, string right, BinaryExpression b)
        {
            if (b.Right is MemberExpression { Type: var rt } rm && rt == typeof(bool) && right == Col(rm))
                right = $"{right} = {BoolLit(true)}";
            if (b.Left is MemberExpression { Type: var lt } lm && lt == typeof(bool) && left == Col(lm))
                left = $"{left} = {BoolLit(true)}";
            return $"({left} {op} {right})";
        }

        private string ParseNot(Expression e) =>
            e is MemberExpression { Type: var t } m && t == typeof(bool) && ExpressionHelper.IsEntityProperty(m)
                ? $"{Col(m)} = {BoolLit(false)}"
                : $"NOT ({Parse(e)})";

        private string ParseMethod(MethodCallExpression m)
        {
            if (ExpressionHelper.IsAnyPlaceholder(m))
            {
                var pn = m.Arguments.Count > 0 && m.Arguments[0] is ConstantExpression { Value: string s } && !string.IsNullOrEmpty(s)
                    ? (s.StartsWith("@") ? s : "@" + s) : $"@p{_counter++}";
                _parameters[pn] = ExpressionHelper.GetDefaultValueForValueType(m.Method.ReturnType);
                return pn;
            }

            if (ExpressionHelper.IsAggregateContext(m)) return AggregateParser.Parse(this, m);

            if (m.Method.Name == "Contains" && m.Object != null && ExpressionHelper.IsCollectionType(m.Object.Type) && !ExpressionHelper.IsStringType(m.Object.Type))
            {
                var col = Parse(m.Arguments[0]);
                var coll = ExpressionHelper.EvaluateExpression(m.Object!) as IEnumerable;
                if (coll == null) return $"{col} IN (NULL)";
                var vals = coll.Cast<object?>().Select(x => x == null ? "NULL" : Fmt(x)).ToList();
                return vals.Count == 0 ? $"{col} IN (NULL)" : $"{col} IN ({string.Join(", ", vals)})";
            }

            return m.Method.DeclaringType switch
            {
                var t when t == typeof(Math) => MathFunctionParser.Parse(this, m),
                var t when t == typeof(string) => StringFunctionParser.Parse(this, m),
                var t when t == typeof(DateTime) => DateTimeFunctionParser.Parse(this, m),
                _ => m.Object != null ? ParseRaw(m.Object) : "1=1"
            };
        }
    }
}
