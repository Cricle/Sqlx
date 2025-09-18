// -----------------------------------------------------------------------
// <copyright file="ExpressionToSqlBase.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Sqlx.Annotations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Sqlx
{
    /// <summary>
    /// Abstract base class for ExpressionToSql with common expression parsing and database dialect adaptation
    /// </summary>
    public abstract class ExpressionToSqlBase : IDisposable
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

        // Removed compiled function cache for AOT compatibility

        /// <summary>
        /// Initializes new instance with specified SQL dialect
        /// </summary>
        protected ExpressionToSqlBase(SqlDialect dialect, Type entityType)
        {
            _dialect = dialect;
            _tableName = entityType.Name;
        }

        #region Public Query Methods

        /// <summary>
        /// Adds GROUP BY clause (internal use)
        /// </summary>
        public virtual ExpressionToSqlBase AddGroupBy(string columnName)
        {
            if (!string.IsNullOrEmpty(columnName) && !_groupByExpressions.Contains(columnName))
            {
                _groupByExpressions.Add(columnName);
            }
            return this;
        }

        /// <summary>
        /// Adds GROUP BY column name (internal use)
        /// </summary>
        internal void AddGroupByColumn(string columnName) => AddGroupBy(columnName);

        #endregion

        #region Internal Helper Methods

        /// <summary>
        /// Gets WHERE conditions (internal use)
        /// </summary>
        internal List<string> GetWhereConditions() => new List<string>(_whereConditions);

        /// <summary>
        /// Copies WHERE conditions (internal use)
        /// </summary>
        internal void CopyWhereConditions(List<string> conditions) => _whereConditions.AddRange(conditions);

        /// <summary>
        /// Gets HAVING conditions (internal use)
        /// </summary>
        internal List<string> GetHavingConditions() => new List<string>(_havingConditions);

        /// <summary>
        /// Copies HAVING conditions (internal use)
        /// </summary>
        internal void CopyHavingConditions(List<string> conditions) => _havingConditions.AddRange(conditions);

        /// <summary>
        /// Adds HAVING condition (internal use)
        /// </summary>
        internal void AddHavingCondition(string condition) => _havingConditions.Add(condition);

        /// <summary>
        /// Sets table name
        /// </summary>
        public void SetTableName(string tableName) => _tableName = tableName;

        #endregion

        #region Core Expression Parsing Methods

        /// <summary>
        /// Enhanced expression parsing with support for math functions, string functions, and nested expressions (performance optimized)
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

        /// <summary>
        /// Parses expression without special boolean member handling (for use within binary expressions)
        /// </summary>
        protected string ParseExpressionRaw(Expression expression) => ParseExpression(expression, false);

        /// <summary>
        /// Parses conditional expression (ternary operator)
        /// </summary>
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

        /// <summary>
        /// Enhanced method call expression parsing with support for more nested scenarios
        /// </summary>
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

        /// <summary>
        /// Checks if in aggregate function context
        /// </summary>
        private bool IsAggregateContext(MethodCallExpression method) => method.Method.Name is "Count" or "Sum" or "Average" or "Avg" or "Max" or "Min";

        /// <summary>
        /// Checks if method call is an Any placeholder
        /// </summary>
        private bool IsAnyPlaceholder(MethodCallExpression method)
        {
            // Check if it's a static method call from Sqlx.Any class
            if (method.Method.DeclaringType?.Name != "Any") return false;
            if (method.Method.DeclaringType?.Namespace != "Sqlx") return false;

            return method.Method.Name switch
            {
                "Value" or "String" or "Int" or "Bool" or "DateTime" or "Guid" => true,
                _ => false
            };
        }

        /// <summary>
        /// Creates parameter for Any placeholder
        /// </summary>
        private string CreateParameterForAnyPlaceholder(MethodCallExpression method)
        {
            // Auto-enable parameterized query mode when Any placeholder is detected
            if (!_parameterized)
            {
                _parameterized = true;
            }

            // 获取返回类型
            var returnType = method.Method.ReturnType;

            // 确定参数的默认值
            object? paramValue = returnType switch
            {
                var t when t == typeof(string) => null,
                var t when t == typeof(int) => 0,
                var t when t == typeof(bool) => false,
                var t when t == typeof(DateTime) => DateTime.MinValue,
                var t when t == typeof(Guid) => Guid.Empty,
                var t when t == typeof(decimal) => 0m,
                var t when t == typeof(double) => 0.0,
                var t when t == typeof(float) => 0f,
                var t when t == typeof(long) => 0L,
                var t when t.IsValueType => GetDefaultValueForValueType(t),
                _ => null // 引用类型默认值为null
            };

            // 尝试获取用户提供的参数名
            string paramName;
            if (method.Arguments.Count > 0 && method.Arguments[0] is ConstantExpression paramNameExpr && paramNameExpr.Value is string userParamName && !string.IsNullOrEmpty(userParamName))
            {
                // 使用用户提供的参数名，确保以@开头
                paramName = userParamName.StartsWith("@") ? userParamName : "@" + userParamName;
            }
            else
            {
                // 自动生成参数名
                paramName = $"@p{_counter++}";
            }

            // 创建参数并添加到字典
            _parameters[paramName] = paramValue;

            return paramName;
        }

        /// <summary>
        /// 获取类型的默认值（AOT友好）
        /// </summary>
        private static object? GetDefaultValue(Type type)
        {
            if (type.IsValueType)
            {
                return GetDefaultValueForValueType(type);
            }
            return null;
        }

        /// <summary>
        /// 为值类型获取默认值（AOT友好）
        /// </summary>
        private static object? GetDefaultValueForValueType(Type type)
        {
            // 处理可空类型
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return null;
            }

            // 处理常见的值类型（AOT友好）
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
                _ => null // 对于其他类型，返回null避免复杂反射
            };
        }

        /// <summary>
        /// 安全地创建类型的默认值（AOT友好）
        /// </summary>
        private static object? CreateDefaultValueSafely(Type type)
        {
            if (type.IsValueType)
            {
                return GetDefaultValueForValueType(type);
            }

            return null; // 引用类型的默认值是null
        }

        /// <summary>
        /// 解析聚合函数中的方法调用，支持嵌套函数
        /// </summary>
        protected string ParseAggregateMethodCall(MethodCallExpression method) => method.Method.Name switch
        {
            "Count" => "COUNT(*)",
            "Sum" when method.Arguments.Count > 1 => $"SUM({ParseLambdaExpression(method.Arguments[1])})",
            "Average" or "Avg" when method.Arguments.Count > 1 => $"AVG({ParseLambdaExpression(method.Arguments[1])})",
            "Max" when method.Arguments.Count > 1 => $"MAX({ParseLambdaExpression(method.Arguments[1])})",
            "Min" when method.Arguments.Count > 1 => $"MIN({ParseLambdaExpression(method.Arguments[1])})",
            _ => throw new NotSupportedException($"聚合函数 {method.Method.Name} 不受支持"),
        };

        /// <summary>
        /// 增强的Lambda表达式解析，支持复杂的嵌套函数
        /// </summary>
        protected string ParseLambdaExpression(Expression expression) => expression switch
        {
            LambdaExpression lambda => ParseExpression(lambda.Body, false),
            UnaryExpression { NodeType: ExpressionType.Quote } unary when unary.Operand is LambdaExpression quotedLambda => ParseExpression(quotedLambda.Body, false),
            _ => ParseExpression(expression, false),
        };

        /// <summary>
        /// 尝试解析布尔比较，返回 null 如果不是布尔比较
        /// </summary>
        private string? TryParseBooleanComparison(BinaryExpression binary)
        {
            // 只处理 Equal 和 NotEqual
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
        /// 解析二元表达式为SQL字符串
        /// </summary>
        protected string ParseBinaryExpression(BinaryExpression binary)
        {
            // 先处理特殊情况，再解析表达式

            // 处理布尔类型与常量的比较
            var boolResult = TryParseBooleanComparison(binary);
            if (boolResult != null) return boolResult;

            var left = ParseExpressionRaw(binary.Left);
            var right = ParseExpressionRaw(binary.Right);

            // 特殊处理：如果右侧是布尔成员且尚未正确转换，强制添加 = 1
            if (binary.Right is MemberExpression rightMember && rightMember.Type == typeof(bool) && right == GetColumnName(rightMember))
            {
                right = $"{right} = 1";
            }

            // 处理 NULL 比较
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
        /// 获取表达式对应的列名
        /// </summary>
        protected string GetColumnName(Expression expression)
        {
            // 处理类型转换表达式
            if (expression is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
            {
                expression = unary.Operand;
            }

            // For complex expressions, try to parse them as SQL expressions
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
        /// 获取常量表达式的值
        /// </summary>
        protected string GetConstantValue(ConstantExpression constant) => FormatConstantValue(constant.Value);

        /// <summary>
        /// 检查成员表达式是否是实体属性（而不是闭包变量）
        /// </summary>
        protected bool IsEntityProperty(MemberExpression member)
        {
            // 如果表达式是参数表达式（如 e.Property），则是实体属性
            // 如果表达式是常量表达式或其他类型，则不是实体属性
            return member.Expression is ParameterExpression;
        }

        /// <summary>
        /// AOT 友好的成员值获取，避免反射
        /// </summary>
        protected object? GetMemberValueOptimized(MemberExpression member)
        {
            // 对于 AOT 兼容性，我们不使用反射获取值
            // 而是返回类型的默认值，让参数化查询处理实际值
            return GetSimpleDefaultValue(member.Type);
        }

        /// <summary>
        /// 获取简单的默认值（不使用复杂反射）
        /// </summary>
        private static object? GetSimpleDefaultValue(Type type)
        {
            if (!type.IsValueType)
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
                var t when t == typeof(char) => '\0',
                _ => null // 对于其他类型，返回null避免复杂反射
            };
        }

        /// <summary>
        /// 获取成员类型的默认值（AOT友好）
        /// </summary>
        private static object? GetDefaultValueForMemberType(Type memberType)
        {
            if (!memberType.IsValueType)
                return null;

            return GetDefaultValueForValueType(memberType);
        }

        // Removed complex reflection methods for AOT compatibility

        /// <summary>
        /// 格式化常量值为SQL字符串（泛型版本）。
        /// </summary>
        /// <typeparam name="T">值的类型。</typeparam>
        protected string FormatConstantValue<T>(T? value)
        {
            // 如果启用参数化查询，创建参数
            if (_parameterized)
            {
                return CreateParameter(value);
            }

            // 否则内联常量值
            return FormatValueAsLiteral(value);
        }

        /// <summary>
        /// 格式化常量值为SQL字符串。
        /// </summary>
        protected string FormatConstantValue(object? value)
        {
            // 如果启用参数化查询，创建参数
            if (_parameterized)
            {
                return CreateParameter(value);
            }

            // 否则内联常量值
            return FormatValueAsLiteral(value);
        }

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

        /// <summary>
        /// 创建数据库参数并返回参数占位符（泛型版本）
        /// </summary>
        protected virtual string CreateParameter<T>(T? value)
        {
            var paramName = $"{_dialect.ParameterPrefix}p{_parameters.Count}";
            _parameters[paramName] = value;
            return paramName;
        }

        /// <summary>
        /// 创建数据库参数并返回参数占位符（兼容版本）
        /// </summary>
        protected virtual string CreateParameter(object? value)
        {
            var paramName = $"{_dialect.ParameterPrefix}p{_parameters.Count}";
            _parameters[paramName] = value;
            return paramName;
        }

        #endregion

        #region 表达式辅助方法

        /// <summary>判断表达式是否需要括号包围</summary>
        protected static bool NeedsParentheses(Expression expression) => true;

        /// <summary>移除字符串外层的括号</summary>
        protected static string RemoveOuterParentheses(string condition) =>
            condition.StartsWith("(") && condition.EndsWith(")")
                ? condition.Substring(1, condition.Length - 2)
                : condition;

        /// <summary>检查表达式是否是布尔成员</summary>
        protected static bool IsBooleanMember(Expression expression) =>
            expression is MemberExpression member && member.Type == typeof(bool);

        /// <summary>检查表达式是否是常量 true</summary>
        protected static bool IsConstantTrue(Expression expression) =>
            expression is ConstantExpression { Value: true };

        /// <summary>检查表达式是否是常量 false</summary>
        protected static bool IsConstantFalse(Expression expression) =>
            expression is ConstantExpression { Value: false };

        /// <summary>检查是否是字符串属性访问</summary>
        protected bool IsStringPropertyAccess(MemberExpression member)
        {
            // 检查是否是 .Length 属性
            if (member.Member.Name != "Length") return false;

            // 检查父表达式是否是字符串类型的实体属性
            if (member.Expression is MemberExpression stringMember &&
                stringMember.Type == typeof(string) &&
                IsEntityProperty(stringMember))
            {
                return true;
            }

            return false;
        }

        /// <summary>检查是否是字符串连接操作</summary>
        protected bool IsStringConcatenation(BinaryExpression binary) =>
            binary.Type == typeof(string) && binary.NodeType == ExpressionType.Add;

        /// <summary>解析字符串属性访问</summary>
        protected string ParseStringProperty(MemberExpression member)
        {
            var obj = ParseExpressionRaw(member.Expression!);
            return member.Member.Name switch
            {
                "Length" => DatabaseType == "SqlServer" ? $"LEN({obj})" : $"LENGTH({obj})",
                _ => GetColumnName(member)
            };
        }

        /// <summary>
        /// 解析 NOT 表达式，正确处理布尔类型
        /// </summary>
        protected string ParseNotExpression(Expression operand)
        {
            // 如果操作数是布尔成员，生成更简洁的 [Column] = 0
            if (operand is MemberExpression member && member.Type == typeof(bool) && IsEntityProperty(member))
            {
                var columnName = GetColumnName(member);
                return $"{columnName} = 0";
            }

            // 其他情况使用标准解析
            return $"NOT ({ParseExpression(operand)})";
        }

        /// <summary>
        /// 格式化逻辑表达式（AND/OR），确保布尔成员正确处理
        /// </summary>
        protected string FormatLogicalExpression(string logicalOperator, string left, string right, BinaryExpression binary)
        {
            // 检查右侧是否是布尔成员表达式
            if (binary.Right is MemberExpression rightMember && rightMember.Type == typeof(bool))
            {
                // 获取期望的列名格式
                var expectedColumnName = GetColumnName(rightMember);
                // 如果right只是列名，没有= 1，则添加
                if (right == expectedColumnName)
                {
                    right = $"{right} = 1";
                }
            }

            // 检查左侧是否也是布尔成员（虽然不太常见，但为了完整性）
            if (binary.Left is MemberExpression leftMember && leftMember.Type == typeof(bool))
            {
                var expectedColumnName = GetColumnName(leftMember);
                if (left == expectedColumnName)
                {
                    left = $"{left} = 1";
                }
            }

            return $"({left} {logicalOperator} {right})";
        }

        /// <summary>
        /// 从表达式中提取列名列表
        /// </summary>
        protected List<string> ExtractColumns(Expression expression)
        {
            var columns = new List<string>();

            switch (expression)
            {
                case NewExpression newExpr:
                    // 处理 new { Id, Name } 形式
                    foreach (var arg in newExpr.Arguments)
                    {
                        if (arg is MemberExpression member)
                        {
                            columns.Add(GetColumnName(member));
                        }
                    }
                    break;

                case MemberExpression member:
                    // 处理单个属性
                    columns.Add(GetColumnName(member));
                    break;

                case UnaryExpression { NodeType: ExpressionType.Convert } unary:
                    // 处理类型转换
                    return ExtractColumns(unary.Operand);

                default:
                    // 对于其他表达式类型，尝试作为单个成员处理
                    try
                    {
                        columns.Add(GetColumnName(expression));
                    }
                    catch
                    {
                        // 如果无法解析，忽略
                    }
                    break;
            }

            return columns;
        }

        #endregion

        #region 数据库方言适配函数

        /// <summary>
        /// Gets database type (type-safe enum version)
        /// </summary>
        protected DatabaseType DbType => _dialect.DbType;

        /// <summary>
        /// Gets database type (string version for backward compatibility)
        /// </summary>
        protected string DatabaseType => _dialect.DatabaseType;


        /// <summary>
        /// 获取字符串连接语法
        /// </summary>
        private string GetConcatSyntax(params string[] parts) => _dialect.GetConcatFunction(parts);

        // 数学函数适配
        /// <summary>
        /// 解析数学函数调用表达式。
        /// </summary>
        /// <returns>SQL函数字符串。</returns>
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

        /// <summary>
        /// 解析字符串函数调用表达式。
        /// </summary>
        /// <returns>SQL函数字符串。</returns>
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

        /// <summary>
        /// 解析日期时间函数调用表达式（简化版本）
        /// </summary>
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

        /// <summary>
        /// 简化的操作符处理
        /// </summary>
        protected string GetOperatorFunction(string op, string left, string right) => op switch
        {
            "%" => $"({left} % {right})",
            "^" => $"({left} ^ {right})",
            "??" => $"COALESCE({left}, {right})",
            _ => $"({left} {op} {right})"
        };

        /// <summary>
        /// 获取二元操作的SQL
        /// </summary>
        private string GetBinaryOperatorSql(ExpressionType nodeType, string left, string right, BinaryExpression binary)
        {
            return nodeType switch
            {
                // 比较运算符
                ExpressionType.Equal => $"{left} = {right}",
                ExpressionType.NotEqual => $"{left} <> {right}",
                ExpressionType.GreaterThan => $"{left} > {right}",
                ExpressionType.GreaterThanOrEqual => $"{left} >= {right}",
                ExpressionType.LessThan => $"{left} < {right}",
                ExpressionType.LessThanOrEqual => $"{left} <= {right}",

                // 逻辑运算符
                ExpressionType.AndAlso => FormatLogicalExpression("AND", left, right, binary),
                ExpressionType.OrElse => FormatLogicalExpression("OR", left, right, binary),

                // 算术运算符
                ExpressionType.Add => IsStringConcatenation(binary) ? GetConcatSyntax(left, right) : $"{left} + {right}",
                ExpressionType.Subtract => $"{left} - {right}",
                ExpressionType.Multiply => $"{left} * {right}",
                ExpressionType.Divide => $"{left} / {right}",
                ExpressionType.Modulo => GetOperatorFunction("%", left, right),

                // 空值合并
                ExpressionType.Coalesce => GetOperatorFunction("??", left, right),

                _ => $"{left} = {right}"
            };
        }

        /// <summary>
        /// 获取二元操作的SQL操作符
        /// </summary>
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

        #endregion

        #region 抽象方法

        /// <summary>
        /// 转换为 SQL 字符串。子类必须实现此方法。
        /// </summary>
        public abstract string ToSql();

        /// <summary>
        /// 转换为 SQL 模板。子类必须实现此方法。
        /// </summary>
        public abstract SqlTemplate ToTemplate();

        #endregion

        #region 资源释放

        /// <summary>释放资源。</summary>
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// 清理全局缓存。
        /// </summary>
        public static void ClearGlobalCache()
        {
            // No-op method for compatibility
        }

        #endregion
    }
}