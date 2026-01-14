// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlBase.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Sqlx
{
    /// <summary>
    /// Abstract base class for ExpressionToSql with common expression parsing and database dialect adaptation.
    /// </summary>
    public abstract class ExpressionToSqlBase
    {
        internal readonly SqlDialect _dialect;
        internal readonly List<string> _whereConditions = new();
        internal readonly List<string> _orderByExpressions = new();
        internal readonly List<string> _groupByExpressions = new();
        internal readonly List<string> _havingConditions = new();
        internal readonly Dictionary<string, object?> _parameters = new();
        internal int? _take;
        internal int? _skip;
        internal string? _tableName;
        internal ExpressionToSqlBase? _whereExpression;
        internal List<Dictionary<string, object?>>? _batchParameters;

        /// <summary>Whether to use parameterized query mode.</summary>
        protected bool _parameterized;

        /// <summary>Parameter counter for generating unique parameter names.</summary>
        protected int _counter;

        /// <summary>Initializes with SQL dialect and entity type.</summary>
        protected ExpressionToSqlBase(SqlDialect dialect, Type entityType)
        {
            _dialect = dialect;
            _tableName = entityType.Name;
        }

        /// <summary>Gets the database type string.</summary>
        protected string DatabaseType => _dialect.DatabaseType;

        #region Public Methods

        /// <summary>Adds GROUP BY column.</summary>
        public virtual ExpressionToSqlBase AddGroupBy(string columnName)
        {
            if (!string.IsNullOrEmpty(columnName) && !_groupByExpressions.Contains(columnName))
            {
                _groupByExpressions.Add(columnName);
            }

            return this;
        }

        /// <summary>Sets table name.</summary>
        public void SetTableName(string tableName) => _tableName = tableName;

        /// <summary>Merges WHERE conditions from another ExpressionToSqlBase.</summary>
        public virtual ExpressionToSqlBase WhereFrom(ExpressionToSqlBase expression)
        {
            _whereExpression = expression ?? throw new ArgumentNullException(nameof(expression));
            return this;
        }

        /// <summary>Gets WHERE clause without WHERE keyword.</summary>
        public virtual string ToWhereClause()
        {
            if (_whereConditions.Count == 0)
            {
                return string.Empty;
            }

            return string.Join(" AND ", _whereConditions.Select(RemoveOuterParentheses));
        }

        /// <summary>Gets parameters dictionary for command binding.</summary>
        public virtual Dictionary<string, object?> GetParameters() => new(_parameters);

        /// <summary>Converts to SQL string.</summary>
        public abstract string ToSql();

        /// <summary>Converts to SQL template string.</summary>
        public abstract string ToTemplate();

        #endregion

        #region Internal Methods

        internal void CopyWhereConditions(List<string> conditions) => _whereConditions.AddRange(conditions);

        internal void CopyHavingConditions(List<string> conditions) => _havingConditions.AddRange(conditions);

        internal void AddHavingCondition(string condition) => _havingConditions.Add(condition);

        internal string GetMergedWhereConditions()
        {
            var conditions = new List<string>(_whereConditions);
            if (_whereExpression != null)
            {
                conditions.AddRange(_whereExpression._whereConditions);
            }

            return conditions.Count > 0 ? string.Join(" AND ", conditions) : string.Empty;
        }

        internal Dictionary<string, object?> GetMergedParameters()
        {
            var merged = new Dictionary<string, object?>(_parameters);
            if (_whereExpression == null)
            {
                return merged;
            }

            foreach (var kvp in _whereExpression._parameters)
            {
                var key = merged.ContainsKey(kvp.Key) ? $"__ext_{kvp.Key}" : kvp.Key;
                merged[key] = kvp.Value;
            }

            return merged;
        }

        #endregion

        #region Expression Parsing

        /// <summary>Parse expression to SQL.</summary>
        protected string ParseExpression(Expression expression, bool treatBoolAsComparison = true)
        {
            return expression switch
            {
                BinaryExpression binary => ParseBinaryExpression(binary),
                MemberExpression member when treatBoolAsComparison && member.Type == typeof(bool) =>
                    $"{GetColumnName(member)} = {GetBooleanLiteral(true)}",
                MemberExpression member when IsStringPropertyAccess(member) => ParseStringProperty(member),
                MemberExpression member when IsEntityProperty(member) => GetColumnName(member),
                MemberExpression member => treatBoolAsComparison
                    ? GetColumnName(member)
                    : FormatConstantValue(GetMemberValueOptimized(member)),
                ConstantExpression constant => FormatConstantValue(constant.Value),
                UnaryExpression { NodeType: ExpressionType.Not } unary => ParseNotExpression(unary.Operand),
                UnaryExpression { NodeType: ExpressionType.Convert } unary => ParseExpression(unary.Operand, treatBoolAsComparison),
                MethodCallExpression method => ParseMethodCallExpression(method),
                ConditionalExpression cond => $"CASE WHEN {ParseExpression(cond.Test)} THEN {ParseExpression(cond.IfTrue)} ELSE {ParseExpression(cond.IfFalse)} END",
                _ => "1=1",
            };
        }

        /// <summary>Parses expression as raw value.</summary>
        protected string ParseExpressionRaw(Expression expression) => ParseExpression(expression, false);

        /// <summary>Parse binary expression to SQL.</summary>
        protected string ParseBinaryExpression(BinaryExpression binary)
        {
            var boolResult = TryParseBooleanComparison(binary);
            if (boolResult != null)
            {
                return boolResult;
            }

            var left = ParseExpressionRaw(binary.Left);
            var right = ParseExpressionRaw(binary.Right);

            if (binary.Right is MemberExpression { Type: var rightType } rightMember &&
                rightType == typeof(bool) && right == GetColumnName(rightMember))
            {
                right = $"{right} = 1";
            }

            if (left == "NULL" || right == "NULL")
            {
                var nonNull = left == "NULL" ? right : left;
                return binary.NodeType == ExpressionType.Equal
                    ? $"{nonNull} IS NULL"
                    : binary.NodeType == ExpressionType.NotEqual
                        ? $"{nonNull} IS NOT NULL"
                        : GetBinaryOperatorSql(binary.NodeType, left, right, binary);
            }

            return GetBinaryOperatorSql(binary.NodeType, left, right, binary);
        }

        /// <summary>Parses method call expression.</summary>
        protected string ParseMethodCallExpression(MethodCallExpression method)
        {
            if (IsAnyPlaceholder(method))
            {
                return CreateParameterForAnyPlaceholder(method);
            }

            if (IsAggregateContext(method))
            {
                return ParseAggregateMethodCall(method);
            }

            if (method.Method.Name == "Contains" && method.Object != null &&
                IsCollectionType(method.Object.Type) && !IsStringType(method.Object.Type))
            {
                return ParseCollectionContains(method);
            }

            return method.Method.DeclaringType switch
            {
                var t when t == typeof(Math) => ParseMathFunction(method),
                var t when t == typeof(string) => ParseStringFunction(method),
                var t when t == typeof(DateTime) => ParseDateTimeFunction(method),
                _ => method.Object != null ? ParseExpressionRaw(method.Object) : "1=1"
            };
        }

        /// <summary>Parse aggregate function calls with dialect-specific syntax.</summary>
        protected string ParseAggregateMethodCall(MethodCallExpression method)
        {
            var name = method.Method.Name;
            var hasArg = method.Arguments.Count > 1;
            var arg = hasArg ? ParseLambdaExpression(method.Arguments[1]) : null;

            return name switch
            {
                "Count" => "COUNT(*)",
                "CountDistinct" when hasArg => $"COUNT(DISTINCT {arg})",
                "Sum" when hasArg => $"SUM({arg})",
                "Average" or "Avg" when hasArg => $"AVG({arg})",
                "Max" when hasArg => $"MAX({arg})",
                "Min" when hasArg => $"MIN({arg})",
                "StringAgg" when method.Arguments.Count > 2 => GetStringAggSyntax(arg!, ParseLambdaExpression(method.Arguments[2])),
                _ => throw new NotSupportedException($"Aggregate function {name} is not supported"),
            };
        }

        /// <summary>Gets dialect-specific string aggregation syntax.</summary>
        private string GetStringAggSyntax(string column, string separator)
        {
            return DatabaseType switch
            {
                "MySql" => $"GROUP_CONCAT({column} SEPARATOR {separator})",
                "SQLite" or "SqlServer" => $"GROUP_CONCAT({column}, {separator})",
                "Oracle" => $"LISTAGG({column}, {separator}) WITHIN GROUP (ORDER BY {column})",
                _ => $"STRING_AGG({column}, {separator})" // PostgreSQL, DB2
            };
        }

        /// <summary>Parse lambda expression.</summary>
        protected string ParseLambdaExpression(Expression expression)
        {
            return expression switch
            {
                LambdaExpression lambda => ParseExpression(lambda.Body, false),
                UnaryExpression { NodeType: ExpressionType.Quote } unary when unary.Operand is LambdaExpression quotedLambda =>
                    ParseExpression(quotedLambda.Body, false),
                _ => ParseExpression(expression, false),
            };
        }

        /// <summary>Get column name from expression.</summary>
        protected string GetColumnName(Expression expression)
        {
            return expression switch
            {
                UnaryExpression { NodeType: ExpressionType.Convert } unary => GetColumnName(unary.Operand),
                MemberExpression member => _dialect.WrapColumn(ConvertToSnakeCase(member.Member.Name)),
                _ => ParseExpressionRaw(expression)
            };
        }

        /// <summary>Extract column names from expression.</summary>
        protected List<string> ExtractColumns(Expression expression)
        {
            return expression switch
            {
                NewExpression newExpr => newExpr.Arguments.OfType<MemberExpression>().Select(GetColumnName).ToList(),
                MemberExpression member => new List<string> { GetColumnName(member) },
                UnaryExpression { NodeType: ExpressionType.Convert } unary => ExtractColumns(unary.Operand),
                _ => new List<string> { GetColumnName(expression) }
            };
        }

        #endregion

        #region Math Functions

        /// <summary>Parses math function.</summary>
        protected string ParseMathFunction(MethodCallExpression method)
        {
            var name = method.Method.Name;
            var argCount = method.Arguments.Count;

            return (name, argCount) switch
            {
                ("Abs", 1) => $"ABS({ParseExpressionRaw(method.Arguments[0])})",
                ("Round", 1) => $"ROUND({ParseExpressionRaw(method.Arguments[0])})",
                ("Round", 2) => $"ROUND({ParseExpressionRaw(method.Arguments[0])}, {ParseExpressionRaw(method.Arguments[1])})",
                ("Floor", 1) => $"FLOOR({ParseExpressionRaw(method.Arguments[0])})",
                ("Sqrt", 1) => $"SQRT({ParseExpressionRaw(method.Arguments[0])})",
                ("Ceiling", 1) => DatabaseType == "PostgreSql"
                    ? $"CEIL({ParseExpressionRaw(method.Arguments[0])})"
                    : $"CEILING({ParseExpressionRaw(method.Arguments[0])})",
                ("Min", 2) => $"LEAST({ParseExpressionRaw(method.Arguments[0])}, {ParseExpressionRaw(method.Arguments[1])})",
                ("Max", 2) => $"GREATEST({ParseExpressionRaw(method.Arguments[0])}, {ParseExpressionRaw(method.Arguments[1])})",
                ("Pow", 2) => DatabaseType == "MySql"
                    ? $"POW({ParseExpressionRaw(method.Arguments[0])}, {ParseExpressionRaw(method.Arguments[1])})"
                    : $"POWER({ParseExpressionRaw(method.Arguments[0])}, {ParseExpressionRaw(method.Arguments[1])})",
                _ => "1"
            };
        }

        #endregion

        #region String Functions

        /// <summary>Parses string function.</summary>
        protected string ParseStringFunction(MethodCallExpression method)
        {
            var obj = method.Object != null ? ParseExpressionRaw(method.Object) : string.Empty;
            var name = method.Method.Name;
            var argCount = method.Arguments.Count;

            return (name, argCount) switch
            {
                ("Contains", 1) => $"{obj} LIKE {GetConcatSyntax("'%'", ParseExpressionRaw(method.Arguments[0]), "'%'")}",
                ("StartsWith", 1) => $"{obj} LIKE {GetConcatSyntax(ParseExpressionRaw(method.Arguments[0]), "'%'")}",
                ("EndsWith", 1) => $"{obj} LIKE {GetConcatSyntax("'%'", ParseExpressionRaw(method.Arguments[0]))}",
                ("ToUpper", 0) => $"UPPER({obj})",
                ("ToLower", 0) => $"LOWER({obj})",
                ("Trim", 0) => $"TRIM({obj})",
                ("Replace", 2) => $"REPLACE({obj}, {ParseExpressionRaw(method.Arguments[0])}, {ParseExpressionRaw(method.Arguments[1])})",
                ("Substring", 1) => DatabaseType == "SQLite"
                    ? $"SUBSTR({obj}, {ParseExpressionRaw(method.Arguments[0])})"
                    : $"SUBSTRING({obj}, {ParseExpressionRaw(method.Arguments[0])})",
                ("Substring", 2) => DatabaseType == "SQLite"
                    ? $"SUBSTR({obj}, {ParseExpressionRaw(method.Arguments[0])}, {ParseExpressionRaw(method.Arguments[1])})"
                    : $"SUBSTRING({obj}, {ParseExpressionRaw(method.Arguments[0])}, {ParseExpressionRaw(method.Arguments[1])})",
                ("Length", 0) => DatabaseType == "SqlServer" ? $"LEN({obj})" : $"LENGTH({obj})",
                _ => obj
            };
        }

        /// <summary>Parses DateTime function.</summary>
        protected string ParseDateTimeFunction(MethodCallExpression method)
        {
            var obj = method.Object != null ? ParseExpressionRaw(method.Object) : string.Empty;
            if (DatabaseType == "SqlServer" && method.Method.Name.StartsWith("Add") && method.Arguments.Count == 1)
            {
                var arg = ParseExpressionRaw(method.Arguments[0]);
                return $"DATEADD({method.Method.Name.Substring(3).ToUpperInvariant()}, {arg}, {obj})";
            }

            return obj;
        }

        #endregion

        #region Value Formatting

        /// <summary>Formats constant value.</summary>
        protected string FormatConstantValue(object? value)
        {
            return _parameterized ? CreateParameter(value) : FormatValueAsLiteral(value);
        }

        /// <summary>Creates parameter.</summary>
        protected virtual string CreateParameter(object? value)
        {
            var paramName = $"{_dialect.ParameterPrefix}p{_parameters.Count}";
            _parameters[paramName] = value;
            return paramName;
        }

        private string FormatValueAsLiteral(object? value)
        {
            return value switch
            {
                null => "NULL",
                string s => _dialect.WrapString(s.Replace("'", "''")),
                bool b => GetBooleanLiteral(b),
                DateTime dt => _dialect.WrapString(dt.ToString("yyyy-MM-dd HH:mm:ss")),
                Guid g => _dialect.WrapString(g.ToString()),
                decimal or double or float => value.ToString()!,
                _ => value?.ToString() ?? "NULL"
            };
        }

        /// <summary>Gets the boolean literal based on database dialect.</summary>
        protected string GetBooleanLiteral(bool value)
        {
            if (DatabaseType == "PostgreSql")
            {
                return value ? "true" : "false";
            }

            return value ? "1" : "0";
        }

        #endregion

        #region Helper Methods

        /// <summary>Converts PascalCase/camelCase to snake_case.</summary>
        protected static string ConvertToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            var hasUpper = false;
            for (var i = 0; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]))
                {
                    hasUpper = true;
                    break;
                }
            }

            if (!hasUpper)
            {
                return name;
            }

            var result = new StringBuilder(name.Length + 4);
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (char.IsUpper(c))
                {
                    if (i > 0)
                    {
                        result.Append('_');
                    }

                    result.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        /// <summary>Removes outer parentheses from condition.</summary>
        protected static string RemoveOuterParentheses(string condition)
        {
            return condition.StartsWith("(") && condition.EndsWith(")")
                ? condition.Substring(1, condition.Length - 2)
                : condition;
        }

        /// <summary>Gets binary operator SQL string based on database dialect.</summary>
        protected string GetBinaryOperator(ExpressionType nodeType)
        {
            return nodeType switch
            {
                ExpressionType.Add => "+",
                ExpressionType.Subtract => "-",
                ExpressionType.Multiply => "*",
                ExpressionType.Divide => "/",
                ExpressionType.Modulo => DatabaseType == "Oracle" ? "MOD" : "%",
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => DatabaseType == "Oracle" ? "!=" : "<>",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.AndAlso => "AND",
                ExpressionType.OrElse => "OR",
                ExpressionType.Coalesce => "COALESCE",
                _ => throw new NotSupportedException($"Binary operator {nodeType} is not supported")
            };
        }

        /// <summary>Checks if member is entity property.</summary>
        protected static bool IsEntityProperty(MemberExpression member) => member.Expression is ParameterExpression;

        /// <summary>Gets optimized member value.</summary>
        protected static object? GetMemberValueOptimized(MemberExpression member) =>
            member.Type.IsValueType ? GetDefaultValueForValueType(member.Type) : null;

        /// <summary>Checks if expression is boolean member.</summary>
        protected static bool IsBooleanMember(Expression expression) =>
            expression is MemberExpression { Type: var type } && type == typeof(bool);

        /// <summary>Checks if expression is constant true.</summary>
        protected static bool IsConstantTrue(Expression expression) => expression is ConstantExpression { Value: true };

        /// <summary>Checks if expression is constant false.</summary>
        protected static bool IsConstantFalse(Expression expression) => expression is ConstantExpression { Value: false };

        /// <summary>Checks if member is string property access.</summary>
        protected static bool IsStringPropertyAccess(MemberExpression member) =>
            member.Member.Name == "Length" &&
            member.Expression is MemberExpression { Type: var type } stringMember &&
            type == typeof(string) && IsEntityProperty(stringMember);

        /// <summary>Checks if binary expression is string concatenation.</summary>
        protected static bool IsStringConcatenation(BinaryExpression binary) =>
            binary.Type == typeof(string) && binary.NodeType == ExpressionType.Add;

        private static bool IsCollectionType(Type type)
        {
            if (type.IsArray)
            {
                return true;
            }

            if (type.IsGenericType)
            {
                var genericDef = type.GetGenericTypeDefinition();
                return genericDef == typeof(List<>) ||
                       genericDef == typeof(IEnumerable<>) ||
                       genericDef == typeof(ICollection<>) ||
                       genericDef == typeof(IList<>);
            }

            return false;
        }

        private static bool IsStringType(Type type) => type == typeof(string);

        private static bool IsAggregateContext(MethodCallExpression method) =>
            method.Method.Name is "Count" or "CountDistinct" or "Sum" or "Average" or "Avg" or "Max" or "Min" or "StringAgg";

        private static bool IsAnyPlaceholder(MethodCallExpression method) =>
            method.Method.DeclaringType?.Name == "Any" && method.Method.DeclaringType?.Namespace == "Sqlx";

        private static object? GetDefaultValueForValueType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return null;
            }

            if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
            {
                return 0;
            }

            if (type == typeof(bool))
            {
                return false;
            }

            if (type == typeof(DateTime))
            {
                return DateTime.MinValue;
            }

            if (type == typeof(Guid))
            {
                return Guid.Empty;
            }

            if (type == typeof(decimal))
            {
                return 0m;
            }

            if (type == typeof(double))
            {
                return 0.0;
            }

            if (type == typeof(float))
            {
                return 0f;
            }

            return type.IsValueType ? (type.IsGenericType ? null : 0) : null;
        }

        #endregion

        #region Private Methods

        private string? TryParseBooleanComparison(BinaryExpression binary)
        {
            if (binary.NodeType is not (ExpressionType.Equal or ExpressionType.NotEqual))
            {
                return null;
            }

            var op = binary.NodeType == ExpressionType.Equal ? "=" : "<>";

            if (IsBooleanMember(binary.Left))
            {
                if (IsConstantTrue(binary.Right))
                {
                    return $"{GetColumnName(binary.Left)} {op} 1";
                }

                if (IsConstantFalse(binary.Right))
                {
                    return $"{GetColumnName(binary.Left)} {op} 0";
                }
            }

            if (IsBooleanMember(binary.Right))
            {
                if (IsConstantTrue(binary.Left))
                {
                    return $"{GetColumnName(binary.Right)} {op} 1";
                }

                if (IsConstantFalse(binary.Left))
                {
                    return $"{GetColumnName(binary.Right)} {op} 0";
                }
            }

            return null;
        }

        private string ParseNotExpression(Expression operand)
        {
            if (operand is MemberExpression { Type: var type } member && type == typeof(bool) && IsEntityProperty(member))
            {
                return $"{GetColumnName(member)} = {GetBooleanLiteral(false)}";
            }

            return $"NOT ({ParseExpression(operand)})";
        }

        private string ParseStringProperty(MemberExpression member)
        {
            if (member.Member.Name == "Length")
            {
                var expr = ParseExpressionRaw(member.Expression!);
                return DatabaseType == "SqlServer" ? $"LEN({expr})" : $"LENGTH({expr})";
            }

            return GetColumnName(member);
        }

        private string ParseCollectionContains(MethodCallExpression method)
        {
            if (method.Arguments.Count != 1)
            {
                throw new NotSupportedException("Collection Contains requires exactly one argument");
            }

            var columnSql = ParseExpression(method.Arguments[0]);
            var collection = EvaluateExpression(method.Object!);

            if (collection == null)
            {
                return $"{columnSql} IN (NULL)";
            }

            var values = new List<string>();
            foreach (var item in (System.Collections.IEnumerable)collection)
            {
                values.Add(item == null ? "NULL" : FormatConstantValue(item));
            }

            return values.Count == 0
                ? $"{columnSql} IN (NULL)"
                : $"{columnSql} IN ({string.Join(", ", values)})";
        }

        /// <summary>Evaluates expression to get runtime value.</summary>
        /// <param name="expression">The expression to evaluate.</param>
        /// <returns>The evaluated value.</returns>
        /// <exception cref="NotSupportedException">Thrown when member type is not supported.</exception>
        internal static object? EvaluateExpression(Expression expression)
        {
            return expression switch
            {
                ConstantExpression constant => constant.Value,
                MemberExpression member => EvaluateMemberExpression(member),
                MethodCallExpression method => EvaluateMethodCall(method),
                UnaryExpression { NodeType: ExpressionType.Convert } unary => EvaluateExpression(unary.Operand),
                NewExpression newExpr => EvaluateNewExpression(newExpr),
                NewArrayExpression arrayExpr => EvaluateNewArrayExpression(arrayExpr),
                _ => CompileAndInvoke(expression)
            };
        }

        private static object? EvaluateMemberExpression(MemberExpression member)
        {
            var obj = member.Expression != null ? EvaluateExpression(member.Expression) : null;
            return member.Member switch
            {
                System.Reflection.FieldInfo field => field.GetValue(obj),
                System.Reflection.PropertyInfo prop => prop.GetValue(obj),
                _ => throw new NotSupportedException($"Member type {member.Member.GetType()} is not supported")
            };
        }

        private static object? EvaluateMethodCall(MethodCallExpression method)
        {
            var obj = method.Object != null ? EvaluateExpression(method.Object) : null;
            var args = new object?[method.Arguments.Count];
            for (var i = 0; i < method.Arguments.Count; i++)
            {
                args[i] = EvaluateExpression(method.Arguments[i]);
            }

            return method.Method.Invoke(obj, args);
        }

        private static object? EvaluateNewExpression(NewExpression newExpr)
        {
            var args = new object?[newExpr.Arguments.Count];
            for (var i = 0; i < newExpr.Arguments.Count; i++)
            {
                args[i] = EvaluateExpression(newExpr.Arguments[i]);
            }

            return newExpr.Constructor?.Invoke(args);
        }

        private static object? EvaluateNewArrayExpression(NewArrayExpression arrayExpr)
        {
            var elementType = arrayExpr.Type.GetElementType()!;
            var array = Array.CreateInstance(elementType, arrayExpr.Expressions.Count);
            for (var i = 0; i < arrayExpr.Expressions.Count; i++)
            {
                array.SetValue(EvaluateExpression(arrayExpr.Expressions[i]), i);
            }

            return array;
        }

        private static object? CompileAndInvoke(Expression expression)
        {
            return Expression.Lambda<Func<object?>>(Expression.Convert(expression, typeof(object))).Compile()();
        }

        private string CreateParameterForAnyPlaceholder(MethodCallExpression method)
        {
            if (!_parameterized)
            {
                _parameterized = true;
            }

            var paramValue = GetDefaultValueForValueType(method.Method.ReturnType);
            var paramName = ExtractParameterName(method);
            _parameters[paramName] = paramValue;
            return paramName;
        }

        private string ExtractParameterName(MethodCallExpression method)
        {
            if (method.Arguments.Count > 0 &&
                method.Arguments[0] is ConstantExpression { Value: string userParamName } &&
                !string.IsNullOrEmpty(userParamName))
            {
                return userParamName.StartsWith("@") ? userParamName : "@" + userParamName;
            }

            return $"@p{_counter++}";
        }

        private string FormatLogicalExpression(string logicalOperator, string left, string right, BinaryExpression binary)
        {
            if (binary.Right is MemberExpression { Type: var rightType } rightMember &&
                rightType == typeof(bool) && right == GetColumnName(rightMember))
            {
                right = $"{right} = {GetBooleanLiteral(true)}";
            }

            if (binary.Left is MemberExpression { Type: var leftType } leftMember &&
                leftType == typeof(bool) && left == GetColumnName(leftMember))
            {
                left = $"{left} = {GetBooleanLiteral(true)}";
            }

            return $"({left} {logicalOperator} {right})";
        }

        private string GetBinaryOperatorSql(ExpressionType nodeType, string left, string right, BinaryExpression binary)
        {
            return nodeType switch
            {
                ExpressionType.Equal => $"{left} = {right}",
                ExpressionType.NotEqual => DatabaseType == "Oracle" ? $"{left} != {right}" : $"{left} <> {right}",
                ExpressionType.GreaterThan => $"{left} > {right}",
                ExpressionType.GreaterThanOrEqual => $"{left} >= {right}",
                ExpressionType.LessThan => $"{left} < {right}",
                ExpressionType.LessThanOrEqual => $"{left} <= {right}",
                ExpressionType.AndAlso => FormatLogicalExpression("AND", left, right, binary),
                ExpressionType.OrElse => FormatLogicalExpression("OR", left, right, binary),
                ExpressionType.Add => IsStringConcatenation(binary) ? GetConcatSyntax(left, right) : $"{left} + {right}",
                ExpressionType.Subtract => $"{left} - {right}",
                ExpressionType.Multiply => $"{left} * {right}",
                ExpressionType.Divide => $"{left} / {right}",
                ExpressionType.Modulo => DatabaseType == "Oracle" ? $"MOD({left}, {right})" : $"({left} % {right})",
                ExpressionType.Coalesce => $"COALESCE({left}, {right})",
                _ => throw new NotSupportedException($"Binary operator {nodeType} is not supported")
            };
        }

        private string GetConcatSyntax(params string[] parts) => _dialect.Concat(parts);

        #endregion
    }
}
