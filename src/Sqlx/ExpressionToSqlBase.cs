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
        private static object? GetDefaultValueForValueType(Type type) =>
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) ? null :
            type == typeof(int) ? 0 :
            type == typeof(bool) ? false :
            type == typeof(DateTime) ? DateTime.MinValue :
            type == typeof(Guid) ? Guid.Empty :
            type == typeof(decimal) ? 0m :
            type == typeof(double) ? 0.0 :
            type == typeof(float) ? 0f :
            type == typeof(long) ? 0L :
            type == typeof(short) ? (short)0 :
            type == typeof(byte) ? (byte)0 :
            type.IsValueType ? GetValueTypeDefault(type) : null;

        /// <summary>AOT-safe value type default creation</summary>
        private static object? GetValueTypeDefault(Type type)
        {
            // For unknown value types, return 0 as a safe default
            return type.IsGenericType ? null : 0;
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
            var (leftBool, rightBool, rightTrue, rightFalse, leftTrue, leftFalse) = (
                IsBooleanMember(binary.Left), IsBooleanMember(binary.Right),
                IsConstantTrue(binary.Right), IsConstantFalse(binary.Right),
                IsConstantTrue(binary.Left), IsConstantFalse(binary.Left));

            return (leftBool, rightBool, rightTrue || leftTrue, rightFalse || leftFalse) switch
            {
                (true, false, true, false) => $"{GetColumnName(binary.Left)} {op} 1",
                (false, true, true, false) => $"{GetColumnName(binary.Right)} {op} 1",
                (true, false, false, true) => $"{GetColumnName(binary.Left)} {op} 0",
                (false, true, false, true) => $"{GetColumnName(binary.Right)} {op} 0",
                _ => null
            };
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

        private string FormatValueAsLiteral(object? value) => value switch
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
        protected static bool IsConstantTrue(Expression expression) => expression is ConstantExpression { Value: true };
        protected static bool IsConstantFalse(Expression expression) => expression is ConstantExpression { Value: false };

        /// <summary>String and entity property checks</summary>
        protected static bool IsStringPropertyAccess(MemberExpression member) => member.Member.Name == "Length" && member.Expression is MemberExpression { Type: var type } stringMember && type == typeof(string) && IsEntityProperty(stringMember);
        protected static bool IsStringConcatenation(BinaryExpression binary) => binary.Type == typeof(string) && binary.NodeType == ExpressionType.Add;

        /// <summary>Parses string property</summary>
        protected string ParseStringProperty(MemberExpression member) =>
            member.Member.Name == "Length" ? (DatabaseType == "SqlServer" ? $"LEN({ParseExpressionRaw(member.Expression!)})" : $"LENGTH({ParseExpressionRaw(member.Expression!)})") : GetColumnName(member);

        /// <summary>Expression parsing helpers</summary>
        protected string ParseNotExpression(Expression operand) => operand is MemberExpression { Type: var type } member && type == typeof(bool) && IsEntityProperty(member) ? $"{GetColumnName(member)} = 0" : $"NOT ({ParseExpression(operand)})";
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
            _ => TryGetColumnName(expression)
        };

        private List<string> TryGetColumnName(Expression expression) => new List<string> { GetColumnName(expression) };

        /// <summary>Database type string</summary>
        protected string DatabaseType => _dialect.DatabaseType;
        private string GetConcatSyntax(params string[] parts) => _dialect.GetConcatFunction(parts);

        /// <summary>Parses math function</summary>
        protected string ParseMathFunction(MethodCallExpression method)
        {
            var args = method.Arguments.Select(ParseExpressionRaw).ToArray();
            return (method.Method.Name, args.Length) switch
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
        protected string ParseStringFunction(MethodCallExpression method)
        {
            var obj = method.Object != null ? ParseExpressionRaw(method.Object) : "";
            var args = method.Arguments.Select(ParseExpressionRaw).ToArray();
            return (method.Method.Name, args.Length) switch
            {
                ("Contains", 1) => $"{obj} LIKE {GetConcatSyntax("'%'", args[0], "'%'")}",
                ("StartsWith", 1) => $"{obj} LIKE {GetConcatSyntax(args[0], "'%'")}",
                ("EndsWith", 1) => $"{obj} LIKE {GetConcatSyntax("'%'", args[0])}",
                ("ToUpper", 0) => $"UPPER({obj})",
                ("ToLower", 0) => $"LOWER({obj})",
                ("Trim", 0) => $"TRIM({obj})",
                ("Replace", 2) => $"REPLACE({obj}, {args[0]}, {args[1]})",
                ("Substring", 1) => DatabaseType == "SQLite" ? $"SUBSTR({obj}, {args[0]})" : $"SUBSTRING({obj}, {args[0]})",
                ("Substring", 2) => DatabaseType == "SQLite" ? $"SUBSTR({obj}, {args[0]}, {args[1]})" : $"SUBSTRING({obj}, {args[0]}, {args[1]})",
                ("Length", 0) => DatabaseType == "SqlServer" ? $"LEN({obj})" : $"LENGTH({obj})",
                _ => obj
            };
        }

        /// <summary>Parses DateTime function</summary>
        protected string ParseDateTimeFunction(MethodCallExpression method)
        {
            var obj = method.Object != null ? ParseExpressionRaw(method.Object) : "";
            var args = method.Arguments.Select(ParseExpressionRaw).ToArray();
            return (method.Method.Name, args.Length) switch
            {
                ("AddDays", 1) => DatabaseType == "SqlServer" ? $"DATEADD(DAY, {args[0]}, {obj})" : obj,
                ("AddMonths", 1) => DatabaseType == "SqlServer" ? $"DATEADD(MONTH, {args[0]}, {obj})" : obj,
                ("AddYears", 1) => DatabaseType == "SqlServer" ? $"DATEADD(YEAR, {args[0]}, {obj})" : obj,
                _ => obj
            };
        }

        /// <summary>Gets operator function</summary>
        protected static string GetOperatorFunction(string op, string left, string right) => op switch
        {
            "%" => $"({left} % {right})",
            "^" => $"({left} ^ {right})",
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