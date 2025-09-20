// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlBase.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Linq.Expressions;

namespace Sqlx
{
    /// <summary>
    /// Abstract base class for ExpressionToSql with common expression parsing and database dialect adaptation
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

        /// <summary>
        /// Whether to use parameterized query mode (default: false - inline constant values)
        /// </summary>
        protected bool _parameterized = false;

        /// <summary>
        /// Parameter counter for generating unique parameter names
        /// </summary>
        protected int _counter = 0;

        /// <summary>Initializes with SQL dialect and entity type</summary>
        protected ExpressionToSqlBase(SqlDialect dialect, Type entityType)
        {
            _dialect = dialect;
            _tableName = entityType.Name;
        }

        /// <summary>Adds GROUP BY column</summary>
        public virtual ExpressionToSqlBase AddGroupBy(string columnName)
        {
            if (!string.IsNullOrEmpty(columnName) && !_groupByExpressions.Contains(columnName))
                _groupByExpressions.Add(columnName);
            return this;
        }

        internal void AddGroupByColumn(string columnName) => AddGroupBy(columnName);

        internal List<string> GetWhereConditions() => new(_whereConditions);
        internal void CopyWhereConditions(List<string> conditions) => _whereConditions.AddRange(conditions);
        internal List<string> GetHavingConditions() => new(_havingConditions);
        internal void CopyHavingConditions(List<string> conditions) => _havingConditions.AddRange(conditions);
        internal void AddHavingCondition(string condition) => _havingConditions.Add(condition);
        /// <summary>Sets table name</summary>
        public void SetTableName(string tableName) => _tableName = tableName;

        /// <summary>
        /// Enhanced expression parsing with support for math functions, string functions, and nested expressions
        /// </summary>
        protected string ParseExpression(Expression expression, bool treatBoolAsComparison = true)
        {
            return expression switch
            {
                BinaryExpression binary => ParseBinaryExpression(binary),
                MemberExpression member when treatBoolAsComparison && member.Type == typeof(bool) => $"{GetColumnName(member)} = 1",
                MemberExpression member when IsStringPropertyAccess(member) => ParseStringProperty(member),
                MemberExpression member when IsEntityProperty(member) => GetColumnName(member),
                MemberExpression member => treatBoolAsComparison ? GetColumnName(member) : FormatConstantValue(GetMemberValueOptimized(member)),
                ConstantExpression constant => GetConstantValue(constant),
                UnaryExpression unary when unary.NodeType == ExpressionType.Not => ParseNotExpression(unary.Operand),
                UnaryExpression unary when unary.NodeType == ExpressionType.Convert => ParseExpression(unary.Operand, treatBoolAsComparison),
                MethodCallExpression method => ParseMethodCallExpression(method),
                ConditionalExpression conditional => ParseConditionalExpression(conditional),
                _ => "1=1",
            };
        }

        /// <summary>Parses expression as raw value</summary>
        protected string ParseExpressionRaw(Expression expression) => ParseExpression(expression, false);

        /// <summary>Parses conditional expression</summary>
        protected string ParseConditionalExpression(ConditionalExpression conditional)
        {
            var test = ParseExpression(conditional.Test);
            var ifTrue = ParseExpression(conditional.IfTrue);
            var ifFalse = ParseExpression(conditional.IfFalse);

            return DatabaseType switch
            {
                "SqlServer" => $"CASE WHEN {test} THEN {ifTrue} ELSE {ifFalse} END",
                "MySQL" or "PostgreSql" or "SQLite" or "Oracle" or "DB2" => $"CASE WHEN {test} THEN {ifTrue} ELSE {ifFalse} END",
                _ => $"CASE WHEN {test} THEN {ifTrue} ELSE {ifFalse} END"
            };
        }

        /// <summary>Parses method call expression</summary>
        protected string ParseMethodCallExpression(MethodCallExpression method)
        {
            // Handle Any placeholders
            if (IsAnyPlaceholder(method))
            {
                return CreateParameterForAnyPlaceholder(method);
            }

            // Handle nested method calls in aggregate functions
            if (IsAggregateContext(method)) return ParseAggregateMethodCall(method);

            return method.Method.DeclaringType switch
            {
                var t when t == typeof(Math) => ParseMathFunction(method, method.Method.Name),
                var t when t == typeof(string) => ParseStringFunction(method, method.Method.Name),
                var t when t == typeof(DateTime) => ParseDateTimeFunction(method, method.Method.Name),
                _ => method.Object != null ? ParseExpressionRaw(method.Object) : "1=1"
            };
        }

        private bool IsAggregateContext(MethodCallExpression method) => method.Method.Name is "Count" or "Sum" or "Average" or "Avg" or "Max" or "Min";

        private bool IsAnyPlaceholder(MethodCallExpression method) =>
            method.Method.DeclaringType?.Name == "Any" &&
            method.Method.DeclaringType?.Namespace == "Sqlx" &&
            method.Method.Name is "Value" or "String" or "Int" or "Bool" or "DateTime" or "Guid";

        private string CreateParameterForAnyPlaceholder(MethodCallExpression method)
        {
            if (!_parameterized) _parameterized = true;

            var paramValue = GetDefaultValueForValueType(method.Method.ReturnType);
            var paramName = ExtractParameterName(method);
            _parameters[paramName] = paramValue;
            return paramName;
        }

        /// <summary>Extracts parameter name from method arguments</summary>
        private string ExtractParameterName(MethodCallExpression method) =>
            method.Arguments.Count > 0 &&
            method.Arguments[0] is ConstantExpression { Value: string userParamName } &&
            !string.IsNullOrEmpty(userParamName)
                ? (userParamName.StartsWith("@") ? userParamName : "@" + userParamName)
                : $"@p{_counter++}";

        /// <summary>
        /// Get default value for value types (AOT-friendly)
        /// </summary>
        private static object? GetDefaultValueForValueType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                return null;

            return type switch
            {
                var t when t == typeof(int) => 0,
                var t when t == typeof(bool) => false,
                var t when t == typeof(DateTime) => DateTime.MinValue,
                var t when t == typeof(Guid) => Guid.Empty,
                var t when t == typeof(decimal) => 0m,
                var t when t == typeof(double) => 0.0,
                var t when t == typeof(float) => 0f,
                var t when t == typeof(long) => 0L,
                var t when t == typeof(short) => (short)0,
                var t when t == typeof(byte) => (byte)0,
                var t when t == typeof(sbyte) => (sbyte)0,
                var t when t == typeof(uint) => 0u,
                var t when t == typeof(ushort) => (ushort)0,
                var t when t == typeof(ulong) => 0ul,
                var t when t == typeof(char) => '\0',
                _ => null
            };
        }

        /// <summary>
        /// Parse method calls in aggregate functions with nested function support
        /// </summary>
        protected string ParseAggregateMethodCall(MethodCallExpression method) => method.Method.Name switch
        {
            "Count" => "COUNT(*)",
            "Sum" when method.Arguments.Count > 1 => $"SUM({ParseLambdaExpression(method.Arguments[1])})",
            "Average" or "Avg" when method.Arguments.Count > 1 => $"AVG({ParseLambdaExpression(method.Arguments[1])})",
            "Max" when method.Arguments.Count > 1 => $"MAX({ParseLambdaExpression(method.Arguments[1])})",
            "Min" when method.Arguments.Count > 1 => $"MIN({ParseLambdaExpression(method.Arguments[1])})",
            _ => throw new NotSupportedException($"Aggregate function {method.Method.Name} is not supported"),
        };

        /// <summary>
        /// Enhanced Lambda expression parsing with complex nested function support
        /// </summary>
        protected string ParseLambdaExpression(Expression expression) => expression switch
        {
            LambdaExpression lambda => ParseExpression(lambda.Body, false),
            UnaryExpression { NodeType: ExpressionType.Quote } unary when unary.Operand is LambdaExpression quotedLambda => ParseExpression(quotedLambda.Body, false),
            _ => ParseExpression(expression, false),
        };

        /// <summary>
        /// Try to parse boolean comparison, return null if not a boolean comparison
        /// </summary>
        private string? TryParseBooleanComparison(BinaryExpression binary)
        {
            // Only handle Equal and NotEqual
            if (binary.NodeType != ExpressionType.Equal && binary.NodeType != ExpressionType.NotEqual)
                return null;

            var op = binary.NodeType == ExpressionType.Equal ? "=" : "<>";

            return (IsBooleanMember(binary.Left), IsBooleanMember(binary.Right), IsConstantTrue(binary.Right), IsConstantFalse(binary.Right), IsConstantTrue(binary.Left), IsConstantFalse(binary.Left)) switch
            {
                (true, false, true, false, false, false) => $"{GetColumnName(binary.Left)} {op} 1",
                (false, true, false, false, true, false) => $"{GetColumnName(binary.Right)} {op} 1",
                (true, false, false, true, false, false) => $"{GetColumnName(binary.Left)} {op} 0",
                (false, true, false, false, false, true) => $"{GetColumnName(binary.Right)} {op} 0",
                _ => null
            };
        }

        /// <summary>
        /// Parse binary expression to SQL string
        /// </summary>
        protected string ParseBinaryExpression(BinaryExpression binary)
        {
            // Handle special cases first, then parse expressions

            // Handle boolean type comparison with constants
            var boolResult = TryParseBooleanComparison(binary);
            if (boolResult != null) return boolResult;

            var left = ParseExpressionRaw(binary.Left);
            var right = ParseExpressionRaw(binary.Right);

            // Special handling: if right side is boolean member and not correctly converted, force add = 1
            if (binary.Right is MemberExpression rightMember && rightMember.Type == typeof(bool) && right == GetColumnName(rightMember))
            {
                right = $"{right} = 1";
            }

            // Handle NULL comparison
            if (binary.NodeType == ExpressionType.Equal && (left == "NULL" || right == "NULL"))
            {
                return left == "NULL" ? $"{right} IS NULL" : $"{left} IS NULL";
            }
            if (binary.NodeType == ExpressionType.NotEqual && (left == "NULL" || right == "NULL"))
            {
                return left == "NULL" ? $"{right} IS NOT NULL" : $"{left} IS NOT NULL";
            }

            return GetBinaryOperatorSql(binary.NodeType, left, right, binary);
        }

        /// <summary>
        /// Get column name corresponding to expression
        /// </summary>
        protected string GetColumnName(Expression expression)
        {
            if (expression is UnaryExpression unary && unary.NodeType == ExpressionType.Convert) expression = unary.Operand;

            if (expression is not MemberExpression)
            {
                try
                {
                    return ParseExpressionRaw(expression);
                }
                catch
                {
                    throw new ArgumentException($"{expression} is not member expression and cannot be parsed as complex expression");
                }
            }

            var member = (MemberExpression)expression;
            return _dialect.WrapColumn(member.Member.Name);
        }

        /// <summary>
        /// Get value of constant expression
        /// </summary>
        protected string GetConstantValue(ConstantExpression constant) => FormatConstantValue(constant.Value);

        /// <summary>Checks if member is entity property</summary>
        protected bool IsEntityProperty(MemberExpression member) =>
            member.Expression is ParameterExpression;

        /// <summary>Gets member value optimized</summary>
        protected static object? GetMemberValueOptimized(MemberExpression member) =>
            GetSimpleDefaultValue(member.Type);

        /// <summary>
        /// Get simple default value (without complex reflection)
        /// </summary>
        private static object? GetSimpleDefaultValue(Type type) => 
            type.IsValueType ? GetDefaultValueForValueType(type) : null;

        /// <summary>Formats constant value</summary>
        protected string FormatConstantValue<T>(T? value) =>
            _parameterized ? CreateParameter(value) : FormatValueAsLiteral(value);

        /// <summary>Formats constant value</summary>
        protected string FormatConstantValue(object? value) =>
            _parameterized ? CreateParameter(value) : FormatValueAsLiteral(value);

        private string FormatValueAsLiteral<T>(T? value)
        {
            return value switch
            {
                null => "NULL",
                string s => _dialect.WrapString(s.Replace("'", "''")),
                bool b => b ? "1" : "0",
                DateTime dt => _dialect.WrapString(dt.ToString("yyyy-MM-dd HH:mm:ss")),
                Guid g => _dialect.WrapString(g.ToString()),
                decimal d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
                double d => d.ToString(System.Globalization.CultureInfo.InvariantCulture),
                float f => f.ToString(System.Globalization.CultureInfo.InvariantCulture),
                _ => value?.ToString() ?? "NULL"
            };
        }

        /// <summary>Creates parameter</summary>
        protected virtual string CreateParameter<T>(T? value)
        {
            var paramName = $"{_dialect.ParameterPrefix}p{_parameters.Count}";
            _parameters[paramName] = value;
            return paramName;
        }

        /// <summary>Creates parameter</summary>
        protected virtual string CreateParameter(object? value)
        {
            var paramName = $"{_dialect.ParameterPrefix}p{_parameters.Count}";
            _parameters[paramName] = value;
            return paramName;
        }

        /// <summary>Checks if expression needs parentheses</summary>
        protected static bool NeedsParentheses(Expression expression) => true;
        /// <summary>Removes outer parentheses from condition</summary>
        protected static string RemoveOuterParentheses(string condition) =>
            condition.StartsWith("(") && condition.EndsWith(")")
                ? condition.Substring(1, condition.Length - 2)
                : condition;
        /// <summary>Checks if expression is boolean member</summary>
        protected static bool IsBooleanMember(Expression expression) =>
            expression is MemberExpression member && member.Type == typeof(bool);
        /// <summary>Checks if expression is constant true</summary>
        protected static bool IsConstantTrue(Expression expression) =>
            expression is ConstantExpression { Value: true };
        /// <summary>Checks if expression is constant false</summary>
        protected static bool IsConstantFalse(Expression expression) =>
            expression is ConstantExpression { Value: false };

        /// <summary>Checks if member is string property access</summary>
        protected bool IsStringPropertyAccess(MemberExpression member) =>
            member.Member.Name == "Length" &&
            member.Expression is MemberExpression stringMember &&
            stringMember.Type == typeof(string) &&
            IsEntityProperty(stringMember);

        /// <summary>Checks if binary is string concatenation</summary>
        protected bool IsStringConcatenation(BinaryExpression binary) =>
            binary.Type == typeof(string) && binary.NodeType == ExpressionType.Add;

        /// <summary>Parses string property</summary>
        protected string ParseStringProperty(MemberExpression member)
        {
            var obj = ParseExpressionRaw(member.Expression!);
            return member.Member.Name switch
            {
                "Length" => DatabaseType == "SqlServer" ? $"LEN({obj})" : $"LENGTH({obj})",
                _ => GetColumnName(member)
            };
        }

        /// <summary>Parses NOT expression</summary>
        protected string ParseNotExpression(Expression operand) =>
            operand is MemberExpression member && member.Type == typeof(bool) && IsEntityProperty(member)
                ? $"{GetColumnName(member)} = 0"
                : $"NOT ({ParseExpression(operand)})";

        /// <summary>Formats logical expression</summary>
        protected string FormatLogicalExpression(string logicalOperator, string left, string right, BinaryExpression binary)
        {
            if (binary.Right is MemberExpression rightMember && rightMember.Type == typeof(bool))
            {
                var expectedColumnName = GetColumnName(rightMember);
                if (right == expectedColumnName)
                    right = $"{right} = 1";
            }

            if (binary.Left is MemberExpression leftMember && leftMember.Type == typeof(bool))
            {
                var expectedColumnName = GetColumnName(leftMember);
                if (left == expectedColumnName)
                    left = $"{left} = 1";
            }

            return $"({left} {logicalOperator} {right})";
        }

        /// <summary>
        /// Extract column name list from expression
        /// </summary>
        protected List<string> ExtractColumns(Expression expression)
        {
            var columns = new List<string>();

            switch (expression)
            {
                case NewExpression newExpr:
                    // Handle new { Id, Name } format
                    foreach (var arg in newExpr.Arguments)
                    {
                        if (arg is MemberExpression member)
                        {
                            columns.Add(GetColumnName(member));
                        }
                    }
                    break;

                case MemberExpression member:
                    // Handle single property
                    columns.Add(GetColumnName(member));
                    break;

                case UnaryExpression { NodeType: ExpressionType.Convert } unary:
                    // Handle type conversion
                    return ExtractColumns(unary.Operand);

                default:
                    // For other expression types, try to handle as single member
                    try
                    {
                        columns.Add(GetColumnName(expression));
                    }
                    catch
                    {
                        // If cannot parse, ignore
                    }
                    break;
            }

            return columns;
        }

        /// <summary>Database type string</summary>
        protected string DatabaseType => _dialect.DatabaseType;
        private string GetConcatSyntax(params string[] parts) => _dialect.GetConcatFunction(parts);

        /// <summary>Parses math function</summary>
        protected string ParseMathFunction(MethodCallExpression method, string methodName)
        {
            var args = method.Arguments.Select(ParseExpressionRaw).ToArray();

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
                _ => "1"
            };
        }

        /// <summary>Parses string function</summary>
        protected string ParseStringFunction(MethodCallExpression method, string methodName)
        {
            var obj = method.Object != null ? ParseExpressionRaw(method.Object) : "";
            var args = method.Arguments.Select(ParseExpressionRaw).ToArray();

            return (methodName, args.Length) switch
            {
                ("Contains", 1) => $"{obj} LIKE {GetConcatSyntax("'%'", args[0], "'%'")}",
                ("StartsWith", 1) => $"{obj} LIKE {GetConcatSyntax(args[0], "'%'")}",
                ("EndsWith", 1) => $"{obj} LIKE {GetConcatSyntax("'%'", args[0])}",
                ("ToUpper", 0) => $"UPPER({obj})",
                ("ToLower", 0) => $"LOWER({obj})",
                ("Trim", 0) => $"TRIM({obj})",
                ("Substring", 1) => DatabaseType == "SQLite" ? $"SUBSTR({obj}, {args[0]})" : $"SUBSTRING({obj}, {args[0]})",
                ("Substring", 2) => DatabaseType == "SQLite" ? $"SUBSTR({obj}, {args[0]}, {args[1]})" : $"SUBSTRING({obj}, {args[0]}, {args[1]})",
                ("Replace", 2) => $"REPLACE({obj}, {args[0]}, {args[1]})",
                ("Length", 0) => DatabaseType == "SqlServer" ? $"LEN({obj})" : $"LENGTH({obj})",
                _ => obj
            };
        }

        /// <summary>Parses DateTime function</summary>
        protected string ParseDateTimeFunction(MethodCallExpression method, string methodName)
        {
            var obj = method.Object != null ? ParseExpressionRaw(method.Object) : "";
            var args = method.Arguments.Select(ParseExpressionRaw).ToArray();

            return (methodName, args.Length) switch
            {
                ("AddDays", 1) => DatabaseType == "SqlServer" ? $"DATEADD(DAY, {args[0]}, {obj})" : obj,
                ("AddMonths", 1) => DatabaseType == "SqlServer" ? $"DATEADD(MONTH, {args[0]}, {obj})" : obj,
                ("AddYears", 1) => DatabaseType == "SqlServer" ? $"DATEADD(YEAR, {args[0]}, {obj})" : obj,
                _ => obj
            };
        }

        /// <summary>Gets operator function</summary>
        protected string GetOperatorFunction(string op, string left, string right) => op switch
        {
            "%" => $"({left} % {right})",
            "^" => $"({left} ^ {right})",
            "??" => $"COALESCE({left}, {right})",
            _ => $"({left} {op} {right})"
        };

        private string GetBinaryOperatorSql(ExpressionType nodeType, string left, string right, BinaryExpression binary)
        {
            return nodeType switch
            {
                // Comparison operators
                ExpressionType.Equal => $"{left} = {right}",
                ExpressionType.NotEqual => $"{left} <> {right}",
                ExpressionType.GreaterThan => $"{left} > {right}",
                ExpressionType.GreaterThanOrEqual => $"{left} >= {right}",
                ExpressionType.LessThan => $"{left} < {right}",
                ExpressionType.LessThanOrEqual => $"{left} <= {right}",

                // Logical operators
                ExpressionType.AndAlso => FormatLogicalExpression("AND", left, right, binary),
                ExpressionType.OrElse => FormatLogicalExpression("OR", left, right, binary),

                // Arithmetic operators
                ExpressionType.Add => IsStringConcatenation(binary) ? GetConcatSyntax(left, right) : $"{left} + {right}",
                ExpressionType.Subtract => $"{left} - {right}",
                ExpressionType.Multiply => $"{left} * {right}",
                ExpressionType.Divide => $"{left} / {right}",
                ExpressionType.Modulo => GetOperatorFunction("%", left, right),

                // Null coalescing
                ExpressionType.Coalesce => GetOperatorFunction("??", left, right),

                _ => $"{left} = {right}"
            };
        }

        /// <summary>Gets binary operator</summary>
        protected static string GetBinaryOperator(ExpressionType nodeType) => nodeType switch
        {
            ExpressionType.Equal => "=",
            ExpressionType.NotEqual => "!=",
            ExpressionType.GreaterThan => ">",
            ExpressionType.GreaterThanOrEqual => ">=",
            ExpressionType.LessThan => "<",
            ExpressionType.LessThanOrEqual => "<=",
            ExpressionType.AndAlso => "AND",
            ExpressionType.OrElse => "OR",
            ExpressionType.Add => "+",
            ExpressionType.Subtract => "-",
            ExpressionType.Multiply => "*",
            ExpressionType.Divide => "/",
            _ => nodeType.ToString()
        };

        /// <summary>Converts to SQL string</summary>
        public abstract string ToSql();
        /// <summary>Converts to SQL template</summary>
        public abstract SqlTemplate ToTemplate();
    }
}