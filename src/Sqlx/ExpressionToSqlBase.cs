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
            _stringFunctionMap = InitializeStringFunctionMap();
        }

        /// <summary>Adds GROUP BY column</summary>
        public virtual ExpressionToSqlBase AddGroupBy(string columnName)
        {
            if (!string.IsNullOrEmpty(columnName) && !_groupByExpressions.Contains(columnName))
                _groupByExpressions.Add(columnName);
            return this;
        }

        internal void CopyWhereConditions(List<string> conditions) => _whereConditions.AddRange(conditions);
        internal void CopyHavingConditions(List<string> conditions) => _havingConditions.AddRange(conditions);
        internal void AddHavingCondition(string condition) => _havingConditions.Add(condition);
        /// <summary>Sets table name</summary>
        public void SetTableName(string tableName) => _tableName = tableName;

        /// <summary>Parse expression to SQL</summary>
        protected string ParseExpression(Expression expression, bool treatBoolAsComparison = true) => expression switch
        {
            BinaryExpression binary => ParseBinaryExpression(binary),
            MemberExpression member when treatBoolAsComparison && member.Type == typeof(bool) => $"{GetColumnName(member)} = 1",
            MemberExpression member when IsStringPropertyAccess(member) => ParseStringProperty(member),
            MemberExpression member when IsEntityProperty(member) => GetColumnName(member),
            MemberExpression member => treatBoolAsComparison ? GetColumnName(member) : FormatConstantValue(GetMemberValueOptimized(member)),
            ConstantExpression constant => FormatConstantValue(constant.Value),
            UnaryExpression { NodeType: ExpressionType.Not } unary => ParseNotExpression(unary.Operand),
            UnaryExpression { NodeType: ExpressionType.Convert } unary => ParseExpression(unary.Operand, treatBoolAsComparison),
            MethodCallExpression method => ParseMethodCallExpression(method),
            ConditionalExpression conditional => $"CASE WHEN {ParseExpression(conditional.Test)} THEN {ParseExpression(conditional.IfTrue)} ELSE {ParseExpression(conditional.IfFalse)} END",
            _ => "1=1",
        };

        /// <summary>Parses expression as raw value</summary>
        protected string ParseExpressionRaw(Expression expression) => ParseExpression(expression, false);

        /// <summary>Parses method call expression</summary>
        protected string ParseMethodCallExpression(MethodCallExpression method)
        {
            if (IsAnyPlaceholder(method)) return CreateParameterForAnyPlaceholder(method);
            if (IsAggregateContext(method)) return ParseAggregateMethodCall(method);

            return method.Method.DeclaringType switch
            {
                var t when t == typeof(Math) => ParseMathFunction(method),
                var t when t == typeof(string) => ParseStringFunction(method),
                var t when t == typeof(DateTime) => ParseDateTimeFunction(method),
                _ => method.Object != null ? ParseExpressionRaw(method.Object) : "1=1"
            };
        }

        private static bool IsAggregateContext(MethodCallExpression method) => method.Method.Name is "Count" or "Sum" or "Average" or "Avg" or "Max" or "Min";
        private static bool IsAnyPlaceholder(MethodCallExpression method) => method.Method.DeclaringType?.Name == "Any" && method.Method.DeclaringType?.Namespace == "Sqlx";

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

        /// <summary>Get default value for value types (AOT-friendly)</summary>
        private static object? GetDefaultValueForValueType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)) return null;
            if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte)) return 0;
            if (type == typeof(bool)) return false;
            if (type == typeof(DateTime)) return DateTime.MinValue;
            if (type == typeof(Guid)) return Guid.Empty;
            if (type == typeof(decimal)) return 0m;
            if (type == typeof(double)) return 0.0;
            if (type == typeof(float)) return 0f;
            return type.IsValueType ? (type.IsGenericType ? null : 0) : null;
        }

        /// <summary>Parse aggregate function calls</summary>
        protected string ParseAggregateMethodCall(MethodCallExpression method) => method.Method.Name switch
        {
            "Count" => "COUNT(*)",
            "Sum" when method.Arguments.Count > 1 => $"SUM({ParseLambdaExpression(method.Arguments[1])})",
            "Average" or "Avg" when method.Arguments.Count > 1 => $"AVG({ParseLambdaExpression(method.Arguments[1])})",
            "Max" when method.Arguments.Count > 1 => $"MAX({ParseLambdaExpression(method.Arguments[1])})",
            "Min" when method.Arguments.Count > 1 => $"MIN({ParseLambdaExpression(method.Arguments[1])})",
            _ => throw new NotSupportedException($"Aggregate function {method.Method.Name} is not supported"),
        };

        /// <summary>Parse lambda expression</summary>
        protected string ParseLambdaExpression(Expression expression) => expression switch
        {
            LambdaExpression lambda => ParseExpression(lambda.Body, false),
            UnaryExpression { NodeType: ExpressionType.Quote } unary when unary.Operand is LambdaExpression quotedLambda => ParseExpression(quotedLambda.Body, false),
            _ => ParseExpression(expression, false),
        };

        /// <summary>Try to parse boolean comparison</summary>
        private string? TryParseBooleanComparison(BinaryExpression binary)
        {
            if (binary.NodeType is not (ExpressionType.Equal or ExpressionType.NotEqual)) return null;

            var op = binary.NodeType == ExpressionType.Equal ? "=" : "<>";

            // 简化逻辑：直接检查左右两边的模式
            if (IsBooleanMember(binary.Left))
            {
                if (IsConstantTrue(binary.Right)) return $"{GetColumnName(binary.Left)} {op} 1";
                if (IsConstantFalse(binary.Right)) return $"{GetColumnName(binary.Left)} {op} 0";
            }

            if (IsBooleanMember(binary.Right))
            {
                if (IsConstantTrue(binary.Left)) return $"{GetColumnName(binary.Right)} {op} 1";
                if (IsConstantFalse(binary.Left)) return $"{GetColumnName(binary.Right)} {op} 0";
            }

            return null;
        }

        /// <summary>Parse binary expression to SQL</summary>
        protected string ParseBinaryExpression(BinaryExpression binary)
        {
            var boolResult = TryParseBooleanComparison(binary);
            if (boolResult != null) return boolResult;

            var left = ParseExpressionRaw(binary.Left);
            var right = ParseExpressionRaw(binary.Right);

            // Fix boolean member display
            if (binary.Right is MemberExpression { Type: var rightType } rightMember && rightType == typeof(bool) && right == GetColumnName(rightMember))
                right = $"{right} = 1";

            // Handle NULL comparison
            if (left == "NULL" || right == "NULL")
            {
                var nonNull = left == "NULL" ? right : left;
                return binary.NodeType == ExpressionType.Equal ? $"{nonNull} IS NULL" :
                       binary.NodeType == ExpressionType.NotEqual ? $"{nonNull} IS NOT NULL" :
                       GetBinaryOperatorSql(binary.NodeType, left, right, binary);
            }

            return GetBinaryOperatorSql(binary.NodeType, left, right, binary);
        }

        /// <summary>Get column name from expression</summary>
        protected string GetColumnName(Expression expression) =>
            expression is UnaryExpression { NodeType: ExpressionType.Convert } unary ? GetColumnName(unary.Operand) :
            expression is MemberExpression member ? _dialect.WrapColumn(member.Member.Name) :
            ParseExpressionRaw(expression);


        /// <summary>Checks if member is entity property</summary>
        protected static bool IsEntityProperty(MemberExpression member) => member.Expression is ParameterExpression;
        /// <summary>Gets member value optimized</summary>
        protected static object? GetMemberValueOptimized(MemberExpression member) => member.Type.IsValueType ? GetDefaultValueForValueType(member.Type) : null;

        /// <summary>Formats constant value</summary>
        protected string FormatConstantValue(object? value) => _parameterized ? CreateParameter(value) : FormatValueAsLiteral(value);

        // 性能优化：缓存常用的字面值
        private const string NullLiteral = "NULL";
        private static readonly string[] BooleanLiterals = { "0", "1" };

        private string FormatValueAsLiteral(object? value) => value switch
        {
            null => NullLiteral,
            string s => _dialect.WrapString(s.Replace("'", "''")),
            bool b => BooleanLiterals[b ? 1 : 0],
            DateTime dt => _dialect.WrapString(dt.ToString("yyyy-MM-dd HH:mm:ss")),
            Guid g => _dialect.WrapString(g.ToString()),
            decimal or double or float => value.ToString()!,
            _ => value?.ToString() ?? NullLiteral
        };

        /// <summary>Creates parameter</summary>
        protected virtual string CreateParameter(object? value)
        {
            var paramName = $"{_dialect.ParameterPrefix}p{_parameters.Count}";
            _parameters[paramName] = value;
            return paramName;
        }

        /// <summary>Removes outer parentheses from condition</summary>
        protected static string RemoveOuterParentheses(string condition) => condition.StartsWith("(") && condition.EndsWith(")") ? condition.Substring(1, condition.Length - 2) : condition;
        /// <summary>Helper methods for expression checking</summary>
        protected static bool IsBooleanMember(Expression expression) => expression is MemberExpression { Type: var type } && type == typeof(bool);
        /// <summary>Checks if the expression is a constant true value.</summary>
        /// <param name="expression">The expression to check.</param>
        /// <returns>True if the expression is a constant true value.</returns>
        protected static bool IsConstantTrue(Expression expression) => expression is ConstantExpression { Value: true };

        /// <summary>Checks if the expression is a constant false value.</summary>
        /// <param name="expression">The expression to check.</param>
        /// <returns>True if the expression is a constant false value.</returns>
        protected static bool IsConstantFalse(Expression expression) => expression is ConstantExpression { Value: false };

        /// <summary>Checks if the member expression represents a string property access (like string.Length).</summary>
        /// <param name="member">The member expression to check.</param>
        /// <returns>True if this is a string property access.</returns>
        protected static bool IsStringPropertyAccess(MemberExpression member) => member.Member.Name == "Length" && member.Expression is MemberExpression { Type: var type } stringMember && type == typeof(string) && IsEntityProperty(stringMember);

        /// <summary>Checks if the binary expression represents string concatenation.</summary>
        /// <param name="binary">The binary expression to check.</param>
        /// <returns>True if this is a string concatenation operation.</returns>
        protected static bool IsStringConcatenation(BinaryExpression binary) => binary.Type == typeof(string) && binary.NodeType == ExpressionType.Add;

        /// <summary>Parses string property</summary>
        protected string ParseStringProperty(MemberExpression member) =>
            member.Member.Name == "Length" ? (DatabaseType == "SqlServer" ? $"LEN({ParseExpressionRaw(member.Expression!)})" : $"LENGTH({ParseExpressionRaw(member.Expression!)})") : GetColumnName(member);

        /// <summary>Expression parsing helpers</summary>
        protected string ParseNotExpression(Expression operand) => operand is MemberExpression { Type: var type } member && type == typeof(bool) && IsEntityProperty(member) ? $"{GetColumnName(member)} = 0" : $"NOT ({ParseExpression(operand)})";
        /// <summary>Formats a logical expression (AND/OR) with proper boolean handling.</summary>
        /// <param name="logicalOperator">The logical operator (AND/OR).</param>
        /// <param name="left">The left side of the expression.</param>
        /// <param name="right">The right side of the expression.</param>
        /// <param name="binary">The original binary expression for context.</param>
        /// <returns>The formatted logical expression.</returns>
        protected string FormatLogicalExpression(string logicalOperator, string left, string right, BinaryExpression binary)
        {
            if (binary.Right is MemberExpression { Type: var rightType } rightMember && rightType == typeof(bool) && right == GetColumnName(rightMember)) right = $"{right} = 1";
            if (binary.Left is MemberExpression { Type: var leftType } leftMember && leftType == typeof(bool) && left == GetColumnName(leftMember)) left = $"{left} = 1";
            return $"({left} {logicalOperator} {right})";
        }

        /// <summary>Extract column names from expression</summary>
        protected List<string> ExtractColumns(Expression expression) => expression switch
        {
            NewExpression newExpr => newExpr.Arguments.OfType<MemberExpression>().Select(GetColumnName).ToList(),
            MemberExpression member => new List<string> { GetColumnName(member) },
            UnaryExpression { NodeType: ExpressionType.Convert } unary => ExtractColumns(unary.Operand),
            _ => new List<string> { GetColumnName(expression) }
        };

        /// <summary>Database type string</summary>
        protected string DatabaseType => _dialect.DatabaseType;
        private string GetConcatSyntax(params string[] parts) => _dialect.GetConcatFunction(parts);

        // 性能优化：预构建数学函数映射
        private static readonly Dictionary<(string Name, int ArgCount), Func<string[], string, string>> MathFunctionMap = new()
        {
            [("Abs", 1)] = (args, _) => $"ABS({args[0]})",
            [("Round", 1)] = (args, _) => $"ROUND({args[0]})",
            [("Round", 2)] = (args, _) => $"ROUND({args[0]}, {args[1]})",
            [("Floor", 1)] = (args, _) => $"FLOOR({args[0]})",
            [("Sqrt", 1)] = (args, _) => $"SQRT({args[0]})",
            [("Ceiling", 1)] = (args, dbType) => dbType == "PostgreSql" ? $"CEIL({args[0]})" : $"CEILING({args[0]})",
            [("Min", 2)] = (args, _) => $"LEAST({args[0]}, {args[1]})",
            [("Max", 2)] = (args, _) => $"GREATEST({args[0]}, {args[1]})",
            [("Pow", 2)] = (args, dbType) => $"{(dbType == "MySql" ? "POW" : "POWER")}({args[0]}, {args[1]})"
        };

        /// <summary>Parses math function</summary>
        protected string ParseMathFunction(MethodCallExpression method)
        {
            // 性能优化：避免不必要的ToArray()调用，按需创建参数数组
            var key = (method.Method.Name, method.Arguments.Count);
            if (MathFunctionMap.TryGetValue(key, out var func))
            {
                var args = new string[method.Arguments.Count];
                for (int i = 0; i < method.Arguments.Count; i++)
                {
                    args[i] = ParseExpressionRaw(method.Arguments[i]);
                }
                return func(args, DatabaseType);
            }
            return "1";
        }

        // 性能优化：预构建字符串函数映射，使用实例方法访问dialect
        private readonly Dictionary<(string Name, int ArgCount), Func<string, string[], string>> _stringFunctionMap;

        /// <summary>初始化字符串函数映射</summary>
        private Dictionary<(string Name, int ArgCount), Func<string, string[], string>> InitializeStringFunctionMap() => new()
        {
            [("Contains", 1)] = (obj, args) => $"{obj} LIKE {GetConcatSyntax("'%'", args[0], "'%'")}",
            [("StartsWith", 1)] = (obj, args) => $"{obj} LIKE {GetConcatSyntax(args[0], "'%'")}",
            [("EndsWith", 1)] = (obj, args) => $"{obj} LIKE {GetConcatSyntax("'%'", args[0])}",
            [("ToUpper", 0)] = (obj, _) => $"UPPER({obj})",
            [("ToLower", 0)] = (obj, _) => $"LOWER({obj})",
            [("Trim", 0)] = (obj, _) => $"TRIM({obj})",
            [("Replace", 2)] = (obj, args) => $"REPLACE({obj}, {args[0]}, {args[1]})",
            [("Substring", 1)] = (obj, args) => DatabaseType == "SQLite" ? $"SUBSTR({obj}, {args[0]})" : $"SUBSTRING({obj}, {args[0]})",
            [("Substring", 2)] = (obj, args) => DatabaseType == "SQLite" ? $"SUBSTR({obj}, {args[0]}, {args[1]})" : $"SUBSTRING({obj}, {args[0]}, {args[1]})",
            [("Length", 0)] = (obj, _) => DatabaseType == "SqlServer" ? $"LEN({obj})" : $"LENGTH({obj})"
        };

        /// <summary>Parses string function</summary>
        protected string ParseStringFunction(MethodCallExpression method)
        {
            var obj = method.Object != null ? ParseExpressionRaw(method.Object) : "";
            // 性能优化：避免不必要的ToArray()调用，按需创建参数数组
            var key = (method.Method.Name, method.Arguments.Count);
            if (_stringFunctionMap.TryGetValue(key, out var func))
            {
                var args = new string[method.Arguments.Count];
                for (int i = 0; i < method.Arguments.Count; i++)
                {
                    args[i] = ParseExpressionRaw(method.Arguments[i]);
                }
                return func(obj, args);
            }
            return obj;
        }

        /// <summary>Parses DateTime function</summary>
        protected string ParseDateTimeFunction(MethodCallExpression method)
        {
            var obj = method.Object != null ? ParseExpressionRaw(method.Object) : "";
            var args = method.Arguments.Select(ParseExpressionRaw).ToArray();
            return DatabaseType == "SqlServer" && method.Method.Name.StartsWith("Add") && args.Length == 1
                ? $"DATEADD({method.Method.Name.Substring(3).ToUpperInvariant()}, {args[0]}, {obj})"
                : obj;
        }

        /// <summary>Gets operator function</summary>
        protected static string GetOperatorFunction(string op, string left, string right) => op switch
        {
            "??" => $"COALESCE({left}, {right})",
            _ => $"({left} {op} {right})"
        };

        private string GetBinaryOperatorSql(ExpressionType nodeType, string left, string right, BinaryExpression binary) => nodeType switch
        {
            ExpressionType.Equal => $"{left} = {right}",
            ExpressionType.NotEqual => $"{left} <> {right}",
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
            ExpressionType.Modulo => GetOperatorFunction("%", left, right),
            ExpressionType.Coalesce => GetOperatorFunction("??", left, right),
            _ => $"{left} = {right}"
        };

        /// <summary>Converts to SQL string</summary>
        public abstract string ToSql();
        /// <summary>Converts to SQL template</summary>
        public abstract SqlTemplate ToTemplate();
    }
}
