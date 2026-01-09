// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlBase.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
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
        /// External WHERE expression for batch operations
        /// </summary>
        internal ExpressionToSqlBase? _whereExpression;

        /// <summary>
        /// Batch parameters storage for batch insert operations
        /// </summary>
        internal List<Dictionary<string, object?>>? _batchParameters = null;

        /// <summary>
        /// Parameter counter for generating unique parameter names
        /// </summary>
        protected int _counter;

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
            MemberExpression member when treatBoolAsComparison && member.Type == typeof(bool) => $"{GetColumnName(member)} = {GetBooleanTrueLiteral()}",
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

            // Check for collection Contains (IN clause) before string Contains
            if (method.Method.Name == "Contains" && method.Object != null)
            {
                // Collection.Contains(item) → item IN (collection values)
                var objectType = method.Object.Type;
                if (IsCollectionType(objectType) && !IsStringType(objectType))
                {
                    return ParseCollectionContains(method);
                }
            }

            return method.Method.DeclaringType switch
            {
                var t when t == typeof(Math) => ParseMathFunction(method),
                var t when t == typeof(string) => ParseStringFunction(method),
                var t when t == typeof(DateTime) => ParseDateTimeFunction(method),
                _ => method.Object != null ? ParseExpressionRaw(method.Object) : "1=1"
            };
        }

        /// <summary>Checks if type is a collection type (IEnumerable, List, Array, etc.)</summary>
        private static bool IsCollectionType(Type type)
        {
            if (type.IsArray) return true;
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

        /// <summary>Checks if type is string</summary>
        private static bool IsStringType(Type type) => type == typeof(string);

        /// <summary>Parses collection Contains to IN clause</summary>
        protected string ParseCollectionContains(MethodCallExpression method)
        {
            // ids.Contains(x.Id) → x.Id IN (1, 2, 3)
            if (method.Arguments.Count != 1) return "1=1";

            var collectionExpr = method.Object;
            var itemExpr = method.Arguments[0];

            // Get the property/column being checked
            var columnSql = ParseExpression(itemExpr);

            // Evaluate the collection to get its values
            try
            {
                var collection = Expression.Lambda(collectionExpr!).Compile().DynamicInvoke();
                if (collection == null) return $"{columnSql} IN (NULL)";

                // Convert collection to array of values
                var values = new List<string>();
                foreach (var item in (System.Collections.IEnumerable)collection)
                {
                    if (item == null)
                    {
                        values.Add("NULL");
                    }
                    else
                    {
                        values.Add(FormatConstantValue(item));
                    }
                }

                if (values.Count == 0)
                {
                    return $"{columnSql} IN (NULL)";
                }

                return $"{columnSql} IN ({string.Join(", ", values)})";
            }
            catch
            {
                // If we can't evaluate the collection, fall back to 1=1
                return "1=1";
            }
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
            expression is MemberExpression member ? _dialect.WrapColumn(ConvertToSnakeCase(member.Member.Name)) :
            ParseExpressionRaw(expression);

        /// <summary>Converts PascalCase/camelCase to snake_case for database column names</summary>
        protected static string ConvertToSnakeCase(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            
            // Fast path for already lowercase names
            bool hasUpper = false;
            for (int i = 0; i < name.Length; i++)
            {
                if (char.IsUpper(name[i]))
                {
                    hasUpper = true;
                    break;
                }
            }
            if (!hasUpper) return name;
            
            // Convert PascalCase/camelCase to snake_case
            var result = new System.Text.StringBuilder(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (char.IsUpper(c))
                {
                    if (i > 0) result.Append('_');
                    result.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    result.Append(c);
                }
            }
            return result.ToString();
        }

        /// <summary>Checks if member is entity property</summary>
        protected static bool IsEntityProperty(MemberExpression member) => member.Expression is ParameterExpression;
        /// <summary>Gets member value optimized</summary>
        protected static object? GetMemberValueOptimized(MemberExpression member) => member.Type.IsValueType ? GetDefaultValueForValueType(member.Type) : null;

        /// <summary>Formats constant value</summary>
        protected string FormatConstantValue(object? value) => _parameterized ? CreateParameter(value) : FormatValueAsLiteral(value);

        /// <summary>Gets the boolean TRUE literal based on database dialect</summary>
        /// <remarks>
        /// PostgreSQL uses true/false keywords, while SQLite/SQL Server use 1/0
        /// </remarks>
        protected string GetBooleanTrueLiteral() => _dialect.DatabaseType switch
        {
            "PostgreSql" => "true",
            "Oracle" => "1",  // Oracle uses 1/0
            _ => "1"          // SQLite, SQL Server, MySQL use 1/0
        };

        /// <summary>Gets the boolean FALSE literal based on database dialect</summary>
        protected string GetBooleanFalseLiteral() => _dialect.DatabaseType switch
        {
            "PostgreSql" => "false",
            "Oracle" => "0",
            _ => "0"
        };

        private const string NullLiteral = "NULL";

        private string FormatValueAsLiteral(object? value) => value switch
        {
            null => NullLiteral,
            string s => _dialect.WrapString(s.Replace("'", "''")),
            bool b => b ? GetBooleanTrueLiteral() : GetBooleanFalseLiteral(),
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
        protected string ParseNotExpression(Expression operand) => operand is MemberExpression { Type: var type } member && type == typeof(bool) && IsEntityProperty(member) ? $"{GetColumnName(member)} = {GetBooleanFalseLiteral()}" : $"NOT ({ParseExpression(operand)})";
        /// <summary>Formats a logical expression (AND/OR) with proper boolean handling.</summary>
        /// <param name="logicalOperator">The logical operator (AND/OR).</param>
        /// <param name="left">The left side of the expression.</param>
        /// <param name="right">The right side of the expression.</param>
        /// <param name="binary">The original binary expression for context.</param>
        /// <returns>The formatted logical expression.</returns>
        protected string FormatLogicalExpression(string logicalOperator, string left, string right, BinaryExpression binary)
        {
            if (binary.Right is MemberExpression { Type: var rightType } rightMember && rightType == typeof(bool) && right == GetColumnName(rightMember)) right = $"{right} = {GetBooleanTrueLiteral()}";
            if (binary.Left is MemberExpression { Type: var leftType } leftMember && leftType == typeof(bool) && left == GetColumnName(leftMember)) left = $"{left} = {GetBooleanTrueLiteral()}";
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

        /// <summary>
        /// Merges WHERE conditions from another ExpressionToSqlBase (for batch operations)
        /// </summary>
        /// <param name="expression">The expression to merge from</param>
        /// <returns>This instance for chaining</returns>
        public virtual ExpressionToSqlBase WhereFrom(ExpressionToSqlBase expression)
        {
            if (expression == null)
                throw new ArgumentNullException(nameof(expression));

            _whereExpression = expression;
            return this;
        }

        /// <summary>
        /// Gets merged WHERE conditions (including from external expression)
        /// </summary>
        /// <returns>WHERE clause string (without "WHERE" keyword)</returns>
        internal string GetMergedWhereConditions()
        {
            var conditions = new List<string>(_whereConditions);

            if (_whereExpression != null)
            {
                conditions.AddRange(_whereExpression._whereConditions);
            }

            return conditions.Count > 0
                ? string.Join(" AND ", conditions)
                : "";
        }

        /// <summary>
        /// Gets merged parameters (including from external expression)
        /// </summary>
        /// <returns>Merged parameter dictionary</returns>
        internal Dictionary<string, object?> GetMergedParameters()
        {
            var merged = new Dictionary<string, object?>(_parameters);

            if (_whereExpression != null)
            {
                foreach (var kvp in _whereExpression._parameters)
                {
                    // Avoid duplicate keys by prefixing
                    var key = kvp.Key;
                    if (merged.ContainsKey(key))
                    {
                        key = $"__ext_{key}";
                    }
                    merged[key] = kvp.Value;
                }
            }

            return merged;
        }

        /// <summary>Gets WHERE clause without WHERE keyword</summary>
        public virtual string ToWhereClause() => 
            _whereConditions.Count == 0 
                ? string.Empty 
                : string.Join(" AND ", _whereConditions.Select(c => 
                    c.StartsWith("(") && c.EndsWith(")") 
                        ? c.Substring(1, c.Length - 2) 
                        : c));

        /// <summary>Gets parameters dictionary for command binding</summary>
        public virtual Dictionary<string, object?> GetParameters() => new Dictionary<string, object?>(_parameters);

        /// <summary>Converts to SQL string</summary>
        public abstract string ToSql();
        /// <summary>Converts to SQL template</summary>
        public abstract SqlTemplate ToTemplate();
    }
}
