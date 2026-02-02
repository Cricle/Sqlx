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
            MemberExpression m => asBoolCmp ? Col(m) : Fmt(_parameterized ? ExpressionHelper.EvaluateExpression(m) : ExpressionHelper.GetMemberValueOptimized(m)),
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
            MemberExpression m when m.Member.Name == "Key" && IsGroupingType(m.Expression?.Type) => _groupByColumn ?? "Key",
            MemberExpression m => _dialect.WrapColumn(ExpressionHelper.ConvertToSnakeCase(m.Member.Name)),
            _ => ParseRaw(e)
        };

        private static bool IsGroupingType(Type? type)
        {
            if (type == null) return false;
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IGrouping<,>);
        }

        private string? _groupByColumn;

        /// <summary>
        /// Gets the GROUP BY column for resolving Key property in Select after GroupBy.
        /// </summary>
        public string? GroupByColumn => _groupByColumn;

        /// <summary>
        /// Sets the GROUP BY column for resolving Key property in Select after GroupBy.
        /// </summary>
        public void SetGroupByColumn(string column) => _groupByColumn = column;

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
                var col = ParseRaw(n.Arguments[i]);
                var memberName = n.Members?[i]?.Name;
                
                // Add alias if member name differs from column expression
                if (memberName != null && !col.Equals(_dialect.WrapColumn(memberName), StringComparison.OrdinalIgnoreCase) 
                    && !col.Equals(_dialect.WrapColumn(ExpressionHelper.ConvertToSnakeCase(memberName)), StringComparison.OrdinalIgnoreCase))
                {
                    result.Add($"{col} AS {memberName}");
                }
                else
                {
                    result.Add(col);
                }
            }
            return result;
        }

        public string ParseLambda(Expression e) => e switch
        {
            LambdaExpression l => ParseRaw(l.Body),
            UnaryExpression { NodeType: ExpressionType.Quote, Operand: LambdaExpression l } => ParseRaw(l.Body),
            _ => ParseRaw(e),
        };

        /// <summary>
        /// Parses a lambda expression as a boolean condition (for aggregate predicates).
        /// </summary>
        public string ParseLambdaAsCondition(Expression e) => e switch
        {
            LambdaExpression l => Parse(l.Body),
            UnaryExpression { NodeType: ExpressionType.Quote, Operand: LambdaExpression l } => Parse(l.Body),
            _ => Parse(e),
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
            return _dialect.Length(inner);
        }

        private string BuildCaseWhen(ConditionalExpression c)
        {
            return _dialect.CaseWhen(Parse(c.Test), Parse(c.IfTrue), Parse(c.IfFalse));
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
                    ? _dialect.Mod(left, right)
                    : string.Concat("(", left, " % ", right, ")"),
                ExpressionType.Coalesce => _dialect.Coalesce(left, right),
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
            // Check if this is a SubQuery.For<T>().Aggregate() call
            if (IsSubQueryForMethod(m))
            {
                return ParseSubQueryForMethod(m);
            }

            // Check if this is a SubQuery.For<T>() chain without aggregate (e.g., ToList, ToArray, etc.)
            // In this case, generate the subquery SQL directly
            if (IsSubQueryChainWithoutAggregate(m))
            {
                return ParseSubQueryChain(m);
            }

            if (ExpressionHelper.IsAggregateContext(m))
            {
                return AggregateParser.Parse(this, m);
            }

            if (m.Method.Name == "Contains" && m.Object != null && ExpressionHelper.IsCollectionType(m.Object.Type) && !ExpressionHelper.IsStringType(m.Object.Type))
            {
                return ParseContains(m);
            }

            // If the object is a constant (e.g., "A".ToLower()), evaluate at compile time
            if (m.Object is ConstantExpression && m.Method.DeclaringType == typeof(string))
            {
                var result = ExpressionHelper.EvaluateExpression(m);
                return Fmt(result);
            }

            return m.Method.DeclaringType switch
            {
                var t when t == typeof(Math) => MathFunctionParser.Parse(this, m),
                var t when t == typeof(string) => StringFunctionParser.Parse(this, m),
                var t when t == typeof(DateTime) => DateTimeFunctionParser.Parse(this, m),
                _ => m.Object != null ? ParseRaw(m.Object) : "1=1"
            };
        }

        /// <summary>
        /// Checks if this is a SubQuery chain that ends with a non-aggregate method like ToList, ToArray, etc.
        /// </summary>
        private static bool IsSubQueryChainWithoutAggregate(MethodCallExpression m)
        {
            // Methods that indicate end of query chain but not aggregates
            if (m.Method.Name is not ("ToList" or "ToArray" or "AsEnumerable" or "AsQueryable"))
                return false;

            // Check if the source contains SubQuery.For<T>() call
            if (m.Arguments.Count > 0)
                return ContainsSubQueryFor(m.Arguments[0]);
            if (m.Object != null)
                return ContainsSubQueryFor(m.Object);
            return false;
        }

        /// <summary>
        /// Parses a SubQuery chain that doesn't end with an aggregate function.
        /// Generates the subquery SQL directly.
        /// </summary>
        private string ParseSubQueryChain(MethodCallExpression m)
        {
            // Get the source expression (before ToList/ToArray/etc.)
            var sourceExpr = m.Arguments.Count > 0 ? m.Arguments[0] : m.Object;
            if (sourceExpr == null)
                return "1=1";

            // Generate the subquery SQL
            var subQuerySql = GenerateSubQuerySql(sourceExpr);
            return $"({subQuerySql})";
        }

        private static bool IsSubQueryForMethod(MethodCallExpression m)
        {
            // Check if this is Queryable.Count/Sum/etc on a SubQuery.For<T>() chain
            if (m.Method.DeclaringType != typeof(System.Linq.Queryable))
                return false;

            if (m.Method.Name is not ("Count" or "LongCount" or "Sum" or "Average" or "Min" or "Max" or "Any" or "All" or "First" or "FirstOrDefault"))
                return false;

            // Check if the source contains SubQuery.For<T>() call
            return ContainsSubQueryFor(m.Arguments[0]);
        }

        private static bool ContainsSubQueryFor(Expression expr)
        {
            return expr switch
            {
                // SubQuery.For<T>() method call
                MethodCallExpression mc when mc.Method.DeclaringType == typeof(SubQuery) && mc.Method.Name == "For" => true,
                // Recurse into method chain (e.g., SubQuery.For<T>().Where(...))
                MethodCallExpression mc when mc.Arguments.Count > 0 => ContainsSubQueryFor(mc.Arguments[0]),
                _ => false
            };
        }

        private string ParseSubQueryForMethod(MethodCallExpression m)
        {
            // Generate the subquery SQL from the source - subquery parsing is normal query parsing
            var subQuerySql = GenerateSubQuerySql(m.Arguments[0]);

            var methodName = m.Method.Name;
            
            // Handle Count with predicate: Count(y => y.Id == x.Count())
            // This should generate: (SELECT COUNT(*) FROM (subquery) AS sq WHERE condition)
            if ((methodName == "Count" || methodName == "LongCount") && m.Arguments.Count > 1)
            {
                var predicateCondition = ParseLambdaAsCondition(m.Arguments[1]);
                return $"(SELECT COUNT(*) FROM ({subQuerySql}) AS sq WHERE {predicateCondition})";
            }
            
            // Handle Sum/Average/Min/Max with selector
            if (methodName is "Sum" or "Average" or "Min" or "Max" && m.Arguments.Count > 1)
            {
                var selectorColumn = ParseLambdaColumn(m.Arguments[1]);
                var aggregateFunc = methodName switch
                {
                    "Sum" => $"SUM({selectorColumn})",
                    "Average" => $"AVG({selectorColumn})",
                    "Min" => $"MIN({selectorColumn})",
                    "Max" => $"MAX({selectorColumn})",
                    _ => $"SUM({selectorColumn})"
                };
                return $"(SELECT {aggregateFunc} FROM ({subQuerySql}) AS sq)";
            }

            // Handle First/FirstOrDefault - return subquery with LIMIT 1
            if (methodName is "First" or "FirstOrDefault")
            {
                // If has predicate, add WHERE clause
                if (m.Arguments.Count > 1)
                {
                    var predicateCondition = ParseLambdaAsCondition(m.Arguments[1]);
                    return $"({subQuerySql} WHERE {predicateCondition} LIMIT 1)";
                }
                return $"({subQuerySql} LIMIT 1)";
            }

            // Handle Any - EXISTS check
            if (methodName == "Any")
            {
                if (m.Arguments.Count > 1)
                {
                    var predicateCondition = ParseLambdaAsCondition(m.Arguments[1]);
                    return $"(SELECT CASE WHEN EXISTS(SELECT 1 FROM ({subQuerySql}) AS sq WHERE {predicateCondition}) THEN 1 ELSE 0 END)";
                }
                return $"(SELECT CASE WHEN EXISTS({subQuerySql}) THEN 1 ELSE 0 END)";
            }

            // Handle All - NOT EXISTS with negated condition
            if (methodName == "All" && m.Arguments.Count > 1)
            {
                var predicateCondition = ParseLambdaAsCondition(m.Arguments[1]);
                return $"(SELECT CASE WHEN NOT EXISTS(SELECT 1 FROM ({subQuerySql}) AS sq WHERE NOT ({predicateCondition})) THEN 1 ELSE 0 END)";
            }
            
            // Default: Count
            return $"(SELECT COUNT(*) FROM ({subQuerySql}) AS sq)";
        }

        /// <summary>
        /// Generates SQL for a subquery expression. Subquery parsing is the same as normal query parsing.
        /// </summary>
        private string GenerateSubQuerySql(Expression expr)
        {
            // Get the entity provider for the subquery type from the global registry
            var elementType = GetSubQueryElementType(expr);
            var entityProvider = elementType != null ? EntityProviderRegistry.Get(elementType) : null;
            
            // Use SqlExpressionVisitor - subquery parsing is the same as normal query parsing
            // Pass the current groupByColumn so that references to outer scope (like x.Key) can be resolved
            var visitor = new SqlExpressionVisitor(_dialect, _parameterized, entityProvider, _groupByColumn);
            return visitor.GenerateSql(expr);
        }

        /// <summary>
        /// Extracts the element type from a SubQuery expression chain.
        /// </summary>
        private static Type? GetSubQueryElementType(Expression expr)
        {
            return expr switch
            {
                // Get element type from the return type IQueryable<T>
                MethodCallExpression mc when mc.Method.DeclaringType == typeof(SubQuery) && mc.Method.Name == "For" 
                    => mc.Type.IsGenericType ? mc.Type.GenericTypeArguments.FirstOrDefault() : null,
                MethodCallExpression mc when mc.Arguments.Count > 0 
                    => GetSubQueryElementType(mc.Arguments[0]),
                _ => null
            };
        }

        private string ParseLambdaColumn(Expression expr)
        {
            var body = GetLambdaBody(expr);
            return body != null ? Col(body) : "*";
        }

        private static Expression? GetLambdaBody(Expression expr) => expr switch
        {
            LambdaExpression l => l.Body,
            UnaryExpression { NodeType: ExpressionType.Quote, Operand: LambdaExpression l } => l.Body,
            _ => null
        };

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
