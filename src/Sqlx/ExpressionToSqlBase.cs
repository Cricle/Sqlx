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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Sqlx
{
    /// <summary>
    /// ExpressionToSql 类的抽象基类，包含公共的表达式解析和数据库方言适配功能。
    /// </summary>
    public abstract class ExpressionToSqlBase : IDisposable
    {
        internal readonly SqlDialect _dialect;
        internal readonly List<string> _whereConditions = new();
        internal readonly List<string> _orderByExpressions = new();
        internal readonly List<string> _groupByExpressions = new();
        internal readonly List<string> _havingConditions = new();
        internal readonly List<DbParameter> _parameters = new();
        internal int? _take;
        internal int? _skip;
        internal string? _tableName;

        private static readonly ConcurrentDictionary<MemberInfo, Func<object?, object?>> _compiledFunctionCache = new();

        /// <summary>
        /// 使用指定的 SQL 方言初始化新实例。
        /// </summary>
        protected ExpressionToSqlBase(SqlDialect dialect, Type entityType)
        {
            _dialect = dialect;
            _tableName = entityType.Name;
        }

        #region 公共查询方法

        /// <summary>
        /// 添加 GROUP BY 子句（内部使用）。
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
        /// 添加 GROUP BY 列名（内部使用）。
        /// </summary>
        internal void AddGroupByColumn(string columnName) => AddGroupBy(columnName);

        #endregion

        #region 内部辅助方法

        /// <summary>
        /// 获取 WHERE 条件（内部使用）。
        /// </summary>
        internal List<string> GetWhereConditions() => new List<string>(_whereConditions);

        /// <summary>
        /// 复制 WHERE 条件（内部使用）。
        /// </summary>
        internal void CopyWhereConditions(List<string> conditions) => _whereConditions.AddRange(conditions);

        /// <summary>
        /// 获取 HAVING 条件（内部使用）。
        /// </summary>
        internal List<string> GetHavingConditions() => new List<string>(_havingConditions);

        /// <summary>
        /// 复制 HAVING 条件（内部使用）。
        /// </summary>
        internal void CopyHavingConditions(List<string> conditions) => _havingConditions.AddRange(conditions);

        /// <summary>
        /// 添加 HAVING 条件（内部使用）。
        /// </summary>
        internal void AddHavingCondition(string condition) => _havingConditions.Add(condition);

        /// <summary>
        /// 设置表名
        /// </summary>
        public void SetTableName(string tableName) => _tableName = tableName;

        #endregion

        #region 表达式解析核心方法

        /// <summary>
        /// 增强的表达式解析，支持数学函数、字符串函数和嵌套表达式，性能优化。
        /// </summary>
        protected string ParseExpression(Expression expression, bool treatBoolAsComparison = true) => expression switch
        {
            BinaryExpression binary => ParseBinaryExpression(binary),
            MemberExpression member when treatBoolAsComparison && member.Type == typeof(bool) => $"{GetColumnName(member)} = 1",
            MemberExpression member when IsStringPropertyAccess(member) => ParseStringProperty(member),
            MemberExpression member when IsEntityProperty(member) => GetColumnName(member),
            MemberExpression member when member.Expression is MemberExpression baseMember && IsEntityProperty(baseMember) => GetColumnName(member),
            MemberExpression member => treatBoolAsComparison ? GetColumnName(member) : FormatConstantValue(GetMemberValueOptimized(member)),
            ConstantExpression constant => GetConstantValue(constant),
            UnaryExpression unary when unary.NodeType == ExpressionType.Not => ParseNotExpression(unary.Operand),
            UnaryExpression unary when unary.NodeType == ExpressionType.Convert => ParseExpression(unary.Operand, treatBoolAsComparison),
            MethodCallExpression method => ParseMethodCallExpression(method),
            ConditionalExpression conditional => ParseConditionalExpression(conditional),
            _ => "1=1",
        };

        /// <summary>
        /// 解析表达式但不对布尔成员进行特殊处理（用于二元表达式内部）
        /// </summary>
        protected string ParseExpressionRaw(Expression expression) => ParseExpression(expression, false);

        /// <summary>
        /// 解析条件表达式（三元运算符）
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
        /// 增强的方法调用表达式解析，支持更多嵌套场景。
        /// </summary>
        protected string ParseMethodCallExpression(MethodCallExpression method)
        {
            // 处理聚合函数中的嵌套方法调用
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
        /// 检查是否在聚合函数上下文中
        /// </summary>
        private bool IsAggregateContext(MethodCallExpression method) => method.Method.Name is "Count" or "Sum" or "Average" or "Avg" or "Max" or "Min";

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

            return binary.NodeType switch
            {
                // 比较运算符 - 只在复杂逻辑时添加括号
                ExpressionType.Equal => $"{left} = {right}",
                ExpressionType.NotEqual => $"{left} <> {right}",
                ExpressionType.GreaterThan => $"{left} > {right}",
                ExpressionType.GreaterThanOrEqual => $"{left} >= {right}",
                ExpressionType.LessThan => $"{left} < {right}",
                ExpressionType.LessThanOrEqual => $"{left} <= {right}",

                // 逻辑运算符 - 需要括号确保优先级
                ExpressionType.AndAlso => FormatLogicalExpression("AND", left, right, binary),
                ExpressionType.OrElse => FormatLogicalExpression("OR", left, right, binary),

                // 算术运算符 - 简单表达式不加括号，复杂时在外层加
                ExpressionType.Add => IsStringConcatenation(binary) ? GetConcatSyntax(left, right) : $"{left} + {right}",
                ExpressionType.Subtract => $"{left} - {right}",
                ExpressionType.Multiply => $"{left} * {right}",
                ExpressionType.Divide => $"{left} / {right}",
                ExpressionType.Modulo => GetOperatorFunction("%", left, right),
                ExpressionType.Power => GetDialectFunction("POWER", new[] { left, right }, new()
                {
                    ["MySQL"] = "POW({0}, {1})",
                    ["SQLite"] = "POW({0}, {1})"
                }),

                // 位运算符
                ExpressionType.And => $"({left} & {right})",
                ExpressionType.Or => $"({left} | {right})",
                ExpressionType.ExclusiveOr => GetOperatorFunction("^", left, right),

                // 空值合并
                ExpressionType.Coalesce => GetOperatorFunction("??", left, right),

                _ => $"{left} = {right}"
            };
        }

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
        /// 优化的成员值获取，统一缓存策略，遵循DRY原则
        /// </summary>
        protected object? GetMemberValueOptimized(MemberExpression member)
        {
            var expressionSimple = member.Expression == null || member.Expression is ConstantExpression;
            if (expressionSimple || member.Member is not FieldInfo or PropertyInfo) return Expression.Lambda(member).Compile().DynamicInvoke();

            return GetExpressionFunc(member)((member.Expression as ConstantExpression)?.Value);
        }

        private Func<object?, object?> GetExpressionFunc(MemberExpression expression)
        {
            return _compiledFunctionCache.GetOrAdd(expression.Member, x =>
            {
                var par = Expression.Parameter(typeof(object));
                var conv = expression.Expression == null ? null : Expression.Convert(par, x.DeclaringType);
                var exp = x switch
                {
                    FieldInfo field => Expression.Field(conv, field),
                    PropertyInfo property => Expression.Property(conv, property),
                    _ => null
                };
                return Expression.Lambda<Func<object?, object?>>(Expression.Convert(exp, typeof(object)), par).Compile();
            });
        }

        protected string FormatConstantValue(object? value)
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
                _ => value.ToString() ?? "NULL"
            };
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
                "Length" => GetDialectFunction("LENGTH", new[] { obj }, DialectMappings["Length"]),
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
        /// 获取数据库类型（通过列引用符号和参数前缀组合判断）
        /// </summary>
        protected string DatabaseType => _dialect.DatabaseType;

        // 静态方言映射，避免重复创建字典
        protected static readonly Dictionary<string, Dictionary<string, string>> DialectMappings = new()
        {
            ["Ceiling"] = new() { ["PostgreSql"] = "CEIL({0})", ["Oracle"] = "CEIL({0})", ["MySql"] = "CEILING({0})", ["SQLite"] = "CEIL({0})" },
            ["Min"] = new() { ["Oracle"] = "LEAST({0}, {1})" },
            ["Max"] = new() { ["Oracle"] = "GREATEST({0}, {1})" },
            ["Power"] = new() { ["MySQL"] = "POW({0}, {1})", ["SQLite"] = "POW({0}, {1})" },
            ["Substring1"] = new() { ["SqlServer"] = "SUBSTRING({0}, {1}, LEN({0}))", ["Oracle"] = "SUBSTR({0}, {1})", ["MySql"] = "SUBSTRING({0}, {1})", ["PostgreSql"] = "SUBSTRING({0}, {1})", ["SQLite"] = "SUBSTR({0}, {1})" },
            ["Substring2"] = new() { ["Oracle"] = "SUBSTR({0}, {1}, {2})", ["SQLite"] = "SUBSTR({0}, {1}, {2})" },
            ["Length"] = new() { ["SqlServer"] = "LEN({0})" },
            ["Modulo"] = new() { ["SqlServer"] = "({0} % {1})", ["PostgreSql"] = "({0} % {1})", ["MySql"] = "({0} % {1})", ["SQLite"] = "({0} % {1})", ["DB2"] = "MOD({0}, {1})" },
            ["Xor"] = new() { ["SqlServer"] = "({0} ^ {1})", ["PostgreSql"] = "({0} # {1})", ["Oracle"] = "BITXOR({0}, {1})", ["MySql"] = "({0} ^ {1})", ["SQLite"] = "({0} ^ {1})" },
            ["AddDays"] = new() { ["SqlServer"] = "DATEADD(DAY, {1}, {2})", ["PostgreSql"] = "{2} + INTERVAL '{1} days'", ["Oracle"] = "{2} + {1}", ["MySql"] = "DATE_ADD({2}, INTERVAL {1} DAY)", ["SQLite"] = "datetime({2}, '+{1} days')" },
            ["AddMonths"] = new() { ["SqlServer"] = "DATEADD(MONTH, {1}, {2})", ["PostgreSql"] = "{2} + INTERVAL '{1} months'", ["Oracle"] = "ADD_MONTHS({2}, {1})", ["MySql"] = "DATE_ADD({2}, INTERVAL {1} MONTH)", ["SQLite"] = "datetime({2}, '+{1} months')" },
            ["AddYears"] = new() { ["SqlServer"] = "DATEADD(YEAR, {1}, {2})", ["PostgreSql"] = "{2} + INTERVAL '{1} years'", ["Oracle"] = "ADD_MONTHS({2}, {1} * 12)", ["MySql"] = "DATE_ADD({2}, INTERVAL {1} YEAR)", ["SQLite"] = "datetime({2}, '+{1} years')" }
        };

        /// <summary>
        /// 通用的数据库方言函数适配器
        /// </summary>
        protected string GetDialectFunction(string defaultFunction, string[] args, Dictionary<string, string>? overrides = null) =>
            overrides?.TryGetValue(DatabaseType, out var custom) == true
                ? string.Format(custom, args.Cast<object>().ToArray())
                : $"{defaultFunction}({string.Join(", ", args)})";

        /// <summary>
        /// 获取字符串连接语法
        /// </summary>
        private string GetConcatSyntax(params string[] parts) => DatabaseType switch
        {
            "SqlServer" => string.Join(" + ", parts),
            "MySql" or "SQLite" => $"CONCAT({string.Join(", ", parts)})",
            "PostgreSql" or "Oracle" or "DB2" => string.Join(" || ", parts),
            _ => $"CONCAT({string.Join(", ", parts)})"
        };

        // 数学函数适配
        protected string ParseMathFunction(MethodCallExpression method, string methodName)
        {
            var args = method.Arguments.Select(ParseExpressionRaw).ToArray();

            return (methodName, args.Length) switch
            {
                ("Abs", 1) => $"ABS({args[0]})",
                ("Round", 1) => $"ROUND({args[0]})",
                ("Round", 2) => $"ROUND({args[0]}, {args[1]})",
                ("Floor", 1) => $"FLOOR({args[0]})",
                ("Ceiling", 1) => GetDialectFunction("CEILING", args, DialectMappings["Ceiling"]),
                ("Min", 2) => GetDialectFunction("MIN", args, DialectMappings["Min"]),
                ("Max", 2) => GetDialectFunction("MAX", args, DialectMappings["Max"]),
                ("Pow", 2) => GetDialectFunction("POWER", args, DialectMappings["Power"]),
                ("Sqrt", 1) => $"SQRT({args[0]})",
                _ => "1"
            };
        }

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
                ("Substring", 1) => GetDialectFunction("SUBSTRING", new[] { obj, args[0] }, DialectMappings["Substring1"]),
                ("Substring", 2) => GetDialectFunction("SUBSTRING", new[] { obj, args[0], args[1] }, DialectMappings["Substring2"]),
                ("Replace", 2) => $"REPLACE({obj}, {args[0]}, {args[1]})",
                ("Length", 0) => GetDialectFunction("LENGTH", new[] { obj }, DialectMappings["Length"]),
                _ => obj
            };
        }

        protected string ParseDateTimeFunction(MethodCallExpression method, string methodName)
        {
            var obj = method.Object != null ? ParseExpressionRaw(method.Object) : "";
            var args = method.Arguments.Select(ParseExpressionRaw).ToArray();

            return (methodName, args.Length) switch
            {
                ("AddDays", 1) => GetDialectFunction("DATEADD", new[] { "DAY", args[0], obj }, DialectMappings["AddDays"]),
                ("AddMonths", 1) => GetDialectFunction("DATEADD", new[] { "MONTH", args[0], obj }, DialectMappings["AddMonths"]),
                ("AddYears", 1) => GetDialectFunction("DATEADD", new[] { "YEAR", args[0], obj }, DialectMappings["AddYears"]),
                _ => obj
            };
        }

        // 统一的操作符处理函数
        protected string GetOperatorFunction(string op, string left, string right) => op switch
        {
            "%" => DatabaseType switch
            {
                "PostgreSql" or "MySQL" or "SQLite" => $"MOD({left}, {right})",
                _ => $"({left} % {right})"
            },
            "^" => DatabaseType switch
            {
                "MySQL" => $"({left} XOR {right})",
                "PostgreSql" => $"({left} # {right})",
                _ => $"({left} ^ {right})"
            },
            "??" => $"COALESCE({left}, {right})",
            _ => $"({left} {op} {right})"
        };

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
        /// 清理全局缓存（静态方法，谨慎使用）
        /// </summary>
        public static void ClearGlobalCache()
        {
            _compiledFunctionCache.Clear();
        }

        #endregion
    }
}