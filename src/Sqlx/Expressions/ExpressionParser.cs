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
using System.Runtime.CompilerServices;
using System.Text;

namespace Sqlx.Expressions
{
    /// <summary>
    /// Parses LINQ expressions to SQL fragments.
    /// </summary>
    internal sealed class ExpressionParser
    {
        // Thread-local StringBuilder for reducing allocations
        [ThreadStatic]
        private static StringBuilder? _sharedBuilder;

        private readonly SqlDialect _dialect;
        private readonly Dictionary<string, object?> _parameters;
        private readonly bool _parameterized;

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
            MemberExpression m when asBoolCmp && m.Type == typeof(bool) => string.Concat(Col(m), " = ", BoolLit(true)),
            MemberExpression m when ExpressionHelper.IsStringPropertyAccess(m) => StrLen(m),
            MemberExpression m when ExpressionHelper.IsEntityProperty(m) => Col(m),
            MemberExpression m => asBoolCmp ? Col(m) : Fmt(ExpressionHelper.GetMemberValueOptimized(m)),
            ConstantExpression c => Fmt(c.Value),
            UnaryExpression { NodeType: ExpressionType.Not } u => ParseNot(u.Operand),
            UnaryExpression { NodeType: ExpressionType.Convert } u => Parse(u.Operand, asBoolCmp),
            MethodCallExpression m => ParseMethod(m),
            ConditionalExpression c => BuildCaseWhen(c),
            _ => "1=1",
        };

        public string ParseRaw(Expression e) => Parse(e, false);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string Col(Expression e) => e switch
        {
            UnaryExpression { NodeType: ExpressionType.Convert } u => Col(u.Operand),
            MemberExpression m => _dialect.WrapColumn(ExpressionHelper.ConvertToSnakeCase(m.Member.Name)),
            _ => ParseRaw(e)
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string GetColumnName(Expression e) => Col(e);

        public List<string> ExtractColumns(Expression e) => e switch
        {
            NewExpression n => ExtractFromNewExpression(n),
            MemberExpression m when ExpressionHelper.IsStringPropertyAccess(m) => new List<string>(1) { ParseRaw(m) },
            MemberExpression m when ExpressionHelper.IsEntityProperty(m) => new List<string>(1) { Col(m) },
            MemberExpression m => new List<string>(1) { ParseRaw(m) },
            MethodCallExpression mc => new List<string>(1) { ParseRaw(mc) },
            BinaryExpression b => new List<string>(1) { ParseRaw(b) },
            ConditionalExpression c => new List<string>(1) { ParseRaw(c) },
            UnaryExpression { NodeType: ExpressionType.Convert } u => ExtractColumns(u.Operand),
            _ => new List<string>(1) { ParseRaw(e) }
        };

        private List<string> ExtractFromNewExpression(NewExpression n)
        {
            var result = new List<string>(n.Arguments.Count);
            for (var i = 0; i < n.Arguments.Count; i++)
            {
                result.Add(ParseRaw(n.Arguments[i]));
            }

            return result;
        }

        public string ParseLambda(Expression e) => e switch
        {
            LambdaExpression l => ParseRaw(l.Body),
            UnaryExpression { NodeType: ExpressionType.Quote, Operand: LambdaExpression l } => ParseRaw(l.Body),
            _ => ParseRaw(e),
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string FormatConstantValue(object? v) => _parameterized ? CreateParameter(v) : ValueFormatter.FormatAsLiteral(_dialect, v);

        public string CreateParameter(object? v)
        {
            var n = string.Concat(_dialect.ParameterPrefix, "p", _parameters.Count.ToString());
            _parameters[n] = v;
            return n;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string Fmt(object? v) => FormatConstantValue(v);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private string BoolLit(bool v) => ValueFormatter.GetBooleanLiteral(_dialect, v);

        private string StrLen(MemberExpression m)
        {
            if (m.Member.Name != "Length")
            {
                return Col(m);
            }

            var inner = ParseRaw(m.Expression!);
            return DatabaseType == "SqlServer"
                ? string.Concat("LEN(", inner, ")")
                : string.Concat("LENGTH(", inner, ")");
        }

        private string BuildCaseWhen(ConditionalExpression c)
        {
            var sb = GetBuilder();
            sb.Append("CASE WHEN ");
            sb.Append(Parse(c.Test));
            sb.Append(" THEN ");
            sb.Append(Parse(c.IfTrue));
            sb.Append(" ELSE ");
            sb.Append(Parse(c.IfFalse));
            sb.Append(" END");
            return sb.ToString();
        }

        private string ParseBinary(BinaryExpression b)
        {
            // Handle bool comparisons like x.IsActive == true
            if (b.NodeType is ExpressionType.Equal or ExpressionType.NotEqual)
            {
                var op = b.NodeType == ExpressionType.Equal ? " = " : " <> ";
                if (ExpressionHelper.IsBooleanMember(b.Left) && b.Right is ConstantExpression { Value: bool rv })
                {
                    return string.Concat(Col(b.Left), op, BoolLit(rv));
                }

                if (ExpressionHelper.IsBooleanMember(b.Right) && b.Left is ConstantExpression { Value: bool lv })
                {
                    return string.Concat(Col(b.Right), op, BoolLit(lv));
                }
            }

            var left = ParseRaw(b.Left);
            var right = ParseRaw(b.Right);

            // Handle bool member on right side of AND/OR
            if (b.Right is MemberExpression { Type: var rt } rm && rt == typeof(bool) && right == Col(rm))
            {
                right = string.Concat(right, " = ", BoolLit(true));
            }

            // Handle NULL comparisons
            if (left == "NULL" || right == "NULL")
            {
                var nonNull = left == "NULL" ? right : left;
                if (b.NodeType == ExpressionType.Equal)
                {
                    return string.Concat(nonNull, " IS NULL");
                }

                if (b.NodeType == ExpressionType.NotEqual)
                {
                    return string.Concat(nonNull, " IS NOT NULL");
                }
            }

            return b.NodeType switch
            {
                ExpressionType.Equal => string.Concat(left, " = ", right),
                ExpressionType.NotEqual => DatabaseType == "Oracle"
                    ? string.Concat(left, " != ", right)
                    : string.Concat(left, " <> ", right),
                ExpressionType.GreaterThan => string.Concat(left, " > ", right),
                ExpressionType.GreaterThanOrEqual => string.Concat(left, " >= ", right),
                ExpressionType.LessThan => string.Concat(left, " < ", right),
                ExpressionType.LessThanOrEqual => string.Concat(left, " <= ", right),
                ExpressionType.AndAlso or ExpressionType.OrElse => FormatLogical(b.NodeType == ExpressionType.AndAlso ? "AND" : "OR", left, right, b),
                ExpressionType.Add => ExpressionHelper.IsStringConcatenation(b) ? _dialect.Concat(left, right) : string.Concat("(", left, " + ", right, ")"),
                ExpressionType.Subtract => string.Concat("(", left, " - ", right, ")"),
                ExpressionType.Multiply => string.Concat("(", left, " * ", right, ")"),
                ExpressionType.Divide => string.Concat("(", left, " / ", right, ")"),
                ExpressionType.Modulo => DatabaseType == "Oracle"
                    ? string.Concat("MOD(", left, ", ", right, ")")
                    : string.Concat("(", left, " % ", right, ")"),
                ExpressionType.Coalesce => string.Concat("COALESCE(", left, ", ", right, ")"),
                _ => throw new NotSupportedException($"Binary operator {b.NodeType} is not supported")
            };
        }

        private string FormatLogical(string op, string left, string right, BinaryExpression b)
        {
            if (b.Right is MemberExpression { Type: var rt } rm && rt == typeof(bool) && right == Col(rm))
            {
                right = string.Concat(right, " = ", BoolLit(true));
            }

            if (b.Left is MemberExpression { Type: var lt } lm && lt == typeof(bool) && left == Col(lm))
            {
                left = string.Concat(left, " = ", BoolLit(true));
            }

            return string.Concat("(", left, " ", op, " ", right, ")");
        }

        private string ParseNot(Expression e)
        {
            if (e is MemberExpression { Type: var t } m && t == typeof(bool) && ExpressionHelper.IsEntityProperty(m))
            {
                return string.Concat(Col(m), " = ", BoolLit(false));
            }

            return string.Concat("NOT (", Parse(e), ")");
        }

        private string ParseMethod(MethodCallExpression m)
        {
            if (ExpressionHelper.IsAggregateContext(m))
            {
                return AggregateParser.Parse(this, m);
            }

            if (m.Method.Name == "Contains" && m.Object != null && ExpressionHelper.IsCollectionType(m.Object.Type) && !ExpressionHelper.IsStringType(m.Object.Type))
            {
                return ParseContains(m);
            }

            return m.Method.DeclaringType switch
            {
                var t when t == typeof(Math) => MathFunctionParser.Parse(this, m),
                var t when t == typeof(string) => StringFunctionParser.Parse(this, m),
                var t when t == typeof(DateTime) => DateTimeFunctionParser.Parse(this, m),
                _ => m.Object != null ? ParseRaw(m.Object) : "1=1"
            };
        }

        private string ParseContains(MethodCallExpression m)
        {
            var col = Parse(m.Arguments[0]);
            var coll = ExpressionHelper.EvaluateExpression(m.Object!) as IEnumerable;
            if (coll == null)
            {
                return string.Concat(col, " IN (NULL)");
            }

            var sb = GetBuilder();
            sb.Append(col);
            sb.Append(" IN (");

            var first = true;
            foreach (var item in coll)
            {
                if (!first)
                {
                    sb.Append(", ");
                }

                sb.Append(item == null ? "NULL" : Fmt(item));
                first = false;
            }

            if (first)
            {
                sb.Append("NULL");
            }

            sb.Append(')');
            return sb.ToString();
        }

        private static StringBuilder GetBuilder()
        {
            var sb = _sharedBuilder ??= new StringBuilder(64);
            sb.Clear();
            return sb;
        }
    }
}
