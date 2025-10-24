// -----------------------------------------------------------------------
// <copyright file="SqlTemplateEngine.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Sqlx;

namespace Sqlx.Generator;

/// <summary>
/// SQL template processing engine implementation - 写一次、安全、高效、友好、多库可使用
/// 核心特性：
/// - 写一次(Write Once): 同一模板支持多种数据库
/// - 安全(Safety): 全面的SQL注入防护和参数验证
/// - 高效(Efficiency): 智能缓存和编译时优化
/// - 友好(User-friendly): 清晰的错误提示和智能建议
/// - 多库可使用(Multi-database): 通过SqlDefine支持所有主流数据库
/// </summary>
public class SqlTemplateEngine
{
    // 核心正则表达式 - 性能优化版本 (修复ExplicitCapture问题和占位符冲突)
    private static readonly Regex ParameterRegex = new(@"[@:$]\w+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    // 支持三种占位符格式（向后兼容）：
    // 1. 新格式（命令行风格）：{{columns --exclude Id}}, {{orderby created_at --desc}}
    // 2. 旧格式（冒号管道风格）：{{columns:auto|exclude=Id}}, {{limit:default|count=20}}
    // 3. 动态占位符（@ 前缀）：{{@tableName}}, {{@whereClause}}
    // 捕获组：(1)@前缀（动态标记）, (2)name, (3)type（旧格式）, (4)options（旧格式，管道后）, (5)options（新格式，空格后）
    private static readonly Regex PlaceholderRegex = new(@"\{\{(@)?(\w+)(?::(\w+))?(?:\|([^}\s]+))?(?:\s+([^}]+))?\}\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex SqlInjectionRegex = new(@"(?i)(union\s+select|drop\s+table|exec\s*\(|execute\s*\(|sp_|xp_|--|\*\/|\/\*)", RegexOptions.Compiled | RegexOptions.CultureInvariant);


    // 性能优化：通用过滤器
    private static readonly Func<IParameterSymbol, bool> NonSystemParameterFilter = p => p.Type.Name != "CancellationToken";
    private static readonly Func<IPropertySymbol, bool> AccessiblePropertyFilter = p =>
        p.CanBeReferencedByName &&
        p.GetMethod != null &&
        p.Name != "EqualityContract" &&  // Exclude record compiler-generated property
        !p.IsImplicitlyDeclared;          // Exclude all implicitly declared properties

    // 正则表达式缓存 - 避免重复编译
    private static readonly ConcurrentDictionary<string, Regex> RegexCache = new ConcurrentDictionary<string, Regex>();

    // 正则超时设置 - 防止ReDoS攻击
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

    // 安全性：敏感字段检测
    private static readonly HashSet<string> SensitiveFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password", "Pass", "Pwd", "Secret", "Token", "SecurityToken", "ApiKey", "Key",
        "CreditCard", "SSN", "SocialSecurityNumber", "BankAccount", "AuthToken", "RefreshToken",
        "PrivateKey", "Certificate", "Hash", "Salt", "Signature"
    };

    // 默认数据库方言 - 可通过构造函数或方法参数覆盖
    private readonly SqlDefine _defaultDialect;

    /// <summary>
    /// 初始化SQL模板引擎
    /// </summary>
    /// <param name="defaultDialect">默认数据库方言，如不指定则使用SqlServer</param>
    public SqlTemplateEngine(SqlDefine? defaultDialect = null)
    {
        _defaultDialect = defaultDialect ?? SqlDefine.SqlServer;
    }

    /// <inheritdoc/>
    public SqlTemplateResult ProcessTemplate(string templateSql, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName)
    {
        return ProcessTemplate(templateSql, method, entityType, tableName, _defaultDialect);
    }

    /// <summary>
    /// 处理SQL模板 - 多数据库支持版本
    /// 写一次，处处运行：同一个模板可以在不同数据库中使用
    /// </summary>
    /// <param name="templateSql">SQL模板字符串</param>
    /// <param name="method">方法符号</param>
    /// <param name="entityType">实体类型</param>
    /// <param name="tableName">表名</param>
    /// <param name="dialect">数据库方言</param>
    /// <returns>处理结果</returns>
    public SqlTemplateResult ProcessTemplate(string templateSql, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName, SqlDefine dialect)
    {
        if (templateSql == null)
            throw new ArgumentNullException(nameof(templateSql));
        if (tableName == null)
            throw new ArgumentNullException(nameof(tableName));

        if (string.IsNullOrWhiteSpace(templateSql))
            return new SqlTemplateResult { ProcessedSql = "SELECT 1", Warnings = { "Empty SQL template provided" } };
        if (string.IsNullOrWhiteSpace(tableName))
            return new SqlTemplateResult { ProcessedSql = "SELECT 1", Warnings = { "Empty table name provided" } };

        // 智能缓存功能已移至扩展类处理

        var result = new SqlTemplateResult();

        // 增强安全验证 - 基于数据库方言
        if (!ValidateTemplateSecurity(templateSql, result, dialect))
            return result;

        // 处理模板 - 传递数据库方言
        var processedSql = ProcessPlaceholders(templateSql, method!, entityType, tableName, result, dialect);
        ProcessParameters(processedSql, method!, result);

        // Return new instance with updated SQL
        return new SqlTemplateResult
        {
            ProcessedSql = processedSql,
            Parameters = result.Parameters,
            Warnings = result.Warnings,
            Errors = result.Errors,
            HasDynamicFeatures = result.HasDynamicFeatures,
            ColumnOrder = result.ColumnOrder  // 🚀 复制列顺序信息用于序号访问优化
        };
    }

    /// <inheritdoc/>
    public TemplateValidationResult ValidateTemplate(string templateSql)
    {
        var result = new TemplateValidationResult();

        if (string.IsNullOrWhiteSpace(templateSql))
        {
            result.Errors.Add("SQL template cannot be empty");
            return result;
        }

        // 基础验证
        var tempResult = new SqlTemplateResult();
        ValidateTemplateSecurity(templateSql, tempResult, _defaultDialect);
        result.Errors.AddRange(tempResult.Errors);
        result.Warnings.AddRange(tempResult.Warnings);

        // 简单的性能建议
        CheckBasicPerformance(templateSql, result);

        return result;
    }



    /// <summary>
    /// 处理占位符 - 多数据库支持版本
    /// 写一次模板，所有数据库都能使用
    /// </summary>
    private string ProcessPlaceholders(string sql, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName, SqlTemplateResult result, SqlDefine dialect)
    {
        return PlaceholderRegex.Replace(sql, match =>
        {
            // 检查是否是动态占位符（@ 前缀）
            var isDynamic = match.Groups[1].Value == "@";
            var placeholderNameOriginal = match.Groups[2].Value;
            var placeholderName = placeholderNameOriginal.ToLowerInvariant();

            // 如果是动态占位符，直接返回 C# 字符串插值格式
            if (isDynamic)
            {
                // {{@tableName}} -> {tableName}
                // 标记结果包含动态特性，用于后续生成验证代码
                result.HasDynamicFeatures = true;
                return $"{{{placeholderNameOriginal}}}"; // Preserve original case for dynamic placeholders
            }

            // 支持两种格式：
            // 旧格式：{{name:type|options}} -> Groups: (1)"", (2)name, (3)type, (4)options
            // 新格式：{{name --options}}    -> Groups: (1)"", (2)name, (3)"", (4)"", (5)options
            var placeholderType = match.Groups[3].Value; // 旧格式的type
            var oldFormatOptions = match.Groups[4].Value; // 旧格式的options（管道后）
            var newFormatOptions = match.Groups[5].Value; // 新格式的options（空格后）

            // 合并options：优先使用新格式，如果为空则使用旧格式
            var placeholderOptions = !string.IsNullOrEmpty(newFormatOptions) ? newFormatOptions : oldFormatOptions;

            // 验证占位符选项
            ValidatePlaceholderOptions(placeholderName, placeholderType, placeholderOptions, result);

            // 验证类型匹配
            ValidateTypeMismatch(placeholderName, placeholderType, placeholderOptions, entityType, result);

            return placeholderName switch
            {
                // 核心7个占位符（多数据库支持）
                "table" => ProcessTablePlaceholder(tableName, placeholderType, entityType, placeholderOptions, dialect),
                "columns" => ProcessColumnsPlaceholder(placeholderType, entityType, result, placeholderOptions, dialect),
                "values" => ProcessValuesPlaceholder(placeholderType, entityType, method, placeholderOptions, dialect, result),
                "where" => ProcessWherePlaceholder(placeholderType, entityType, method, placeholderOptions, dialect),
                "set" => ProcessSetPlaceholder(placeholderType, entityType, method, placeholderOptions, dialect, result),
                "orderby" => ProcessOrderByPlaceholder(placeholderType, entityType, placeholderOptions, dialect, method),
                "limit" => ProcessLimitPlaceholder(placeholderType, method, placeholderOptions, dialect),
                // 常用扩展占位符（多数据库支持）
                "join" => ProcessJoinPlaceholder(placeholderType, entityType, placeholderOptions, dialect, method),
                "groupby" => ProcessGroupByPlaceholder(placeholderType, entityType, method, placeholderOptions, dialect),
                "having" => ProcessHavingPlaceholder(placeholderType, method, placeholderOptions, dialect),
                "select" => ProcessSelectPlaceholder(placeholderType, entityType, placeholderOptions, dialect),
                "insert" => ProcessInsertPlaceholder(placeholderType, tableName, placeholderOptions, dialect),
                "update" => ProcessUpdatePlaceholder(placeholderType, tableName, placeholderOptions, dialect),
                "delete" => ProcessDeletePlaceholder(placeholderType, tableName, placeholderOptions, dialect),
                "count" => ProcessAggregateFunction("COUNT", placeholderType, placeholderOptions, dialect),
                "sum" => ProcessAggregateFunction("SUM", placeholderType, placeholderOptions, dialect),
                "avg" => ProcessAggregateFunction("AVG", placeholderType, placeholderOptions, dialect),
                "max" => ProcessAggregateFunction("MAX", placeholderType, placeholderOptions, dialect),
                "min" => ProcessAggregateFunction("MIN", placeholderType, placeholderOptions, dialect),
                "distinct" => ProcessDistinctPlaceholder(placeholderType, placeholderOptions, dialect),
                "union" => ProcessUnionPlaceholder(placeholderType, placeholderOptions, dialect),
                "top" => ProcessTopPlaceholder(placeholderType, placeholderOptions, dialect),
                "offset" => ProcessOffsetPlaceholder(placeholderType, placeholderOptions, dialect),
                // 增强的条件占位符
                "between" => ProcessBetweenPlaceholder(placeholderType, placeholderOptions, dialect),
                "like" => ProcessLikePlaceholder(placeholderType, placeholderOptions, dialect),
                "in" => ProcessInPlaceholder(placeholderType, placeholderOptions, dialect),
                "not_in" => ProcessInPlaceholder(placeholderType, placeholderOptions, dialect, true),
                "or" => ProcessOrPlaceholder(placeholderType, placeholderOptions, dialect),
                "isnull" => ProcessIsNullPlaceholder(placeholderType, placeholderOptions, dialect),
                "notnull" => ProcessIsNullPlaceholder(placeholderType, placeholderOptions, dialect, true),
                // 日期时间函数
                "today" => ProcessTodayPlaceholder(placeholderType, placeholderOptions, dialect),
                "week" => ProcessWeekPlaceholder(placeholderType, placeholderOptions, dialect),
                "month" => ProcessMonthPlaceholder(placeholderType, placeholderOptions, dialect),
                "year" => ProcessYearPlaceholder(placeholderType, placeholderOptions, dialect),
                "date_add" => ProcessDateAddPlaceholder(placeholderType, placeholderOptions, dialect),
                "date_diff" => ProcessDateDiffPlaceholder(placeholderType, placeholderOptions, dialect),
                // 字符串函数
                "contains" => ProcessContainsPlaceholder(placeholderType, placeholderOptions, dialect),
                "startswith" => ProcessStartsWithPlaceholder(placeholderType, placeholderOptions, dialect),
                "endswith" => ProcessEndsWithPlaceholder(placeholderType, placeholderOptions, dialect),
                "upper" => ProcessStringFunction("UPPER", placeholderType, placeholderOptions, dialect),
                "lower" => ProcessStringFunction("LOWER", placeholderType, placeholderOptions, dialect),
                "trim" => ProcessStringFunction("TRIM", placeholderType, placeholderOptions, dialect),
                // 数学函数
                "round" => ProcessMathFunction("ROUND", placeholderType, placeholderOptions, dialect),
                "abs" => ProcessMathFunction("ABS", placeholderType, placeholderOptions, dialect),
                "ceiling" => ProcessMathFunction("CEILING", placeholderType, placeholderOptions, dialect),
                "floor" => ProcessMathFunction("FLOOR", placeholderType, placeholderOptions, dialect),
                // 批量操作
                "batch_values" => ProcessBatchValuesPlaceholder(placeholderType, placeholderOptions, dialect),
                "upsert" => ProcessUpsertPlaceholder(placeholderType, tableName, placeholderOptions, dialect),
                // 子查询
                "exists" => ProcessExistsPlaceholder(placeholderType, placeholderOptions, dialect),
                "subquery" => ProcessSubqueryPlaceholder(placeholderType, placeholderOptions, dialect),
                // 分页增强
                "page" => ProcessPagePlaceholder(placeholderType, method, placeholderOptions, dialect),
                "pagination" => ProcessPaginationPlaceholder(placeholderType, method, placeholderOptions, dialect),
                // 条件表达式
                "case" => ProcessCasePlaceholder(placeholderType, placeholderOptions, dialect),
                "coalesce" => ProcessCoalescePlaceholder(placeholderType, placeholderOptions, dialect),
                "ifnull" => ProcessIfNullPlaceholder(placeholderType, placeholderOptions, dialect),
                // 类型转换
                "cast" => ProcessCastPlaceholder(placeholderType, placeholderOptions, dialect),
                "convert" => ProcessConvertPlaceholder(placeholderType, placeholderOptions, dialect),
                // JSON操作
                "json_extract" => ProcessJsonExtractPlaceholder(placeholderType, placeholderOptions, dialect),
                "json_array" => ProcessJsonArrayPlaceholder(placeholderType, placeholderOptions, dialect),
                "json_object" => ProcessJsonObjectPlaceholder(placeholderType, placeholderOptions, dialect),
                // 窗口函数
                "row_number" => ProcessRowNumberPlaceholder(placeholderType, placeholderOptions, dialect),
                "rank" => ProcessRankPlaceholder(placeholderType, placeholderOptions, dialect),
                "dense_rank" => ProcessDenseRankPlaceholder(placeholderType, placeholderOptions, dialect),
                "lag" => ProcessLagPlaceholder(placeholderType, placeholderOptions, dialect),
                "lead" => ProcessLeadPlaceholder(placeholderType, placeholderOptions, dialect),
                // 字符串高级函数
                "substring" => ProcessSubstringPlaceholder(placeholderType, placeholderOptions, dialect),
                "concat" => ProcessConcatPlaceholder(placeholderType, placeholderOptions, dialect),
                "group_concat" => ProcessGroupConcatPlaceholder(placeholderType, placeholderOptions, dialect),
                "replace" => ProcessReplacePlaceholder(placeholderType, placeholderOptions, dialect),
                "length" => ProcessLengthPlaceholder(placeholderType, placeholderOptions, dialect),
                // 数学高级函数
                "power" => ProcessPowerPlaceholder(placeholderType, placeholderOptions, dialect),
                "sqrt" => ProcessSqrtPlaceholder(placeholderType, placeholderOptions, dialect),
                "mod" => ProcessModPlaceholder(placeholderType, placeholderOptions, dialect),
                // 批量操作增强
                "batch_insert" => ProcessBatchInsertPlaceholder(placeholderType, tableName, entityType, placeholderOptions, dialect),
                "bulk_update" => ProcessBulkUpdatePlaceholder(placeholderType, tableName, placeholderOptions, dialect),
                _ => ProcessCustomPlaceholder(match.Value, placeholderName, placeholderType, placeholderOptions, result)
            };
        });
    }


    /// <summary>
    /// 处理表名占位符 - 多数据库支持
    /// 自动应用正确的数据库引用语法
    /// </summary>
    private static string ProcessTablePlaceholder(string tableName, string type, INamedTypeSymbol? entityType, string options, SqlDefine dialect)
    {
        var snakeTableName = SharedCodeGenerationUtilities.ConvertToSnakeCase(tableName);
        return type == "quoted" ? dialect.WrapColumn(snakeTableName) : snakeTableName;
    }


    /// <summary>
    /// 处理列占位符 - 多数据库支持 (性能优化版本)
    /// 自动应用正确的数据库列引用语法
    /// </summary>
    private string ProcessColumnsPlaceholder(string type, INamedTypeSymbol? entityType, SqlTemplateResult result, string options, SqlDefine dialect)
    {
        if (entityType == null)
        {
            result.Warnings.Add("Cannot infer columns without entity type");
            return "*";
        }

        var properties = GetFilteredProperties(entityType, options, type == "auto" ? "Id" : null, false, result);

        // 性能优化：预分配StringBuilder容量
        var capacity = properties.Count * 20; // 估算每个列名约20字符
        var sb = new StringBuilder(capacity);
        var isQuoted = type == "quoted";

        // 🚀 性能优化：记录列顺序以支持直接序号访问
        result.ColumnOrder.Clear(); // 清除之前的顺序

        for (int i = 0; i < properties.Count; i++)
        {
            if (i > 0) sb.Append(", ");

            var columnName = SharedCodeGenerationUtilities.ConvertToSnakeCase(properties[i].Name);
            sb.Append(isQuoted ? dialect.WrapColumn(columnName) : columnName);

            // 记录列名到ColumnOrder（用于序号访问优化）
            result.ColumnOrder.Add(columnName);
        }

        return sb.ToString();
    }


    /// <summary>
    /// 处理值占位符 - 多数据库支持 (性能优化版本)
    /// 自动应用正确的数据库参数语法
    /// </summary>
    private string ProcessValuesPlaceholder(string type, INamedTypeSymbol? entityType, IMethodSymbol method, string options, SqlDefine dialect, SqlTemplateResult result)
    {
        if (entityType == null)
        {
            if (method == null) return string.Empty;

            // 性能优化：预分配容量并使用StringBuilder，避免ToList()
            var filteredParams = method.Parameters.Where(NonSystemParameterFilter);
            var sb = new StringBuilder(80); // 预估容量

            bool first = true;
            foreach (var param in filteredParams)
            {
                if (!first) sb.Append(", ");
                first = false;
                sb.Append(dialect.ParameterPrefix).Append(param.Name);
            }

            return sb.ToString();
        }

        var properties = GetFilteredProperties(entityType, options, type == "auto" ? "Id" : null, false, result);

        // 性能优化：预分配StringBuilder容量
        var propertiesCapacity = properties.Count * 15; // 估算每个参数约15字符
        var propertiesSb = new StringBuilder(propertiesCapacity);

        for (int i = 0; i < properties.Count; i++)
        {
            if (i > 0) propertiesSb.Append(", ");
            var paramName = SharedCodeGenerationUtilities.ConvertToSnakeCase(properties[i].Name);
            propertiesSb.Append(dialect.ParameterPrefix).Append(paramName);
        }

        return propertiesSb.ToString();
    }


    /// <summary>
    /// Processes WHERE placeholder - supports multiple sources
    /// </summary>
    private string ProcessWherePlaceholder(string type, INamedTypeSymbol? entityType, IMethodSymbol method, string options, SqlDefine dialect)
    {
        // Check for ExpressionToSql parameter
        var exprParam = method.Parameters.FirstOrDefault(p =>
            p.GetAttributes().Any(a => a.AttributeClass?.Name == "ExpressionToSqlAttribute"));

        // Check for DynamicSql WHERE parameter
        var dynamicWhereParam = method.Parameters.FirstOrDefault(p =>
            p.GetAttributes().Any(a =>
                a.AttributeClass?.Name == "DynamicSqlAttribute" &&
                a.NamedArguments.Any(arg =>
                    arg.Key == "Type" &&
                    arg.Value.Value?.ToString() == "1"))); // Fragment = 1

        // Priority: @paramName > ExpressionToSql > auto > id > default
        if (!string.IsNullOrWhiteSpace(type) && type.StartsWith("@"))
        {
            // {{where @customWhere}} - use parameter as WHERE fragment
            var paramName = type.Substring(1);
            var param = method.Parameters.FirstOrDefault(p => p.Name == paramName);
            if (param != null)
            {
                return $"{{RUNTIME_WHERE_{paramName}}}"; // Marker for code generation
            }
        }

        if (exprParam != null)
        {
            // {{where}} with [ExpressionToSql] parameter - extract WHERE clause
            return $"{{RUNTIME_WHERE_EXPR_{exprParam.Name}}}"; // Marker for code generation
        }

        if (dynamicWhereParam != null)
        {
            // {{where}} with [DynamicSql(Type=Fragment)] parameter
            return $"{{RUNTIME_WHERE_DYNAMIC_{dynamicWhereParam.Name}}}"; // Marker for code generation
        }

        return type switch
        {
            "id" => $"id = {dialect.ParameterPrefix}id",
            "auto" => GenerateAutoWhereClause(method, dialect),
            _ => "1=1"
        };
    }


    /// <summary>Processes SET placeholder - supports dynamic runtime SET clauses</summary>
    private string ProcessSetPlaceholder(string type, INamedTypeSymbol? entityType, IMethodSymbol method, string options, SqlDefine dialect, SqlTemplateResult result)
    {
        // Check for dynamic SET parameter
        var dynamicSetParam = method.Parameters.FirstOrDefault(p =>
            p.GetAttributes().Any(a =>
                a.AttributeClass?.Name == "DynamicSqlAttribute" &&
                a.NamedArguments.Any(arg =>
                    arg.Key == "Type" &&
                    arg.Value.Value?.ToString() == "1"))); // Fragment = 1

        // Priority: @paramName > entity-based > parameter-based
        if (!string.IsNullOrWhiteSpace(type) && type.StartsWith("@"))
        {
            // {{set @customSet}} - use parameter as SET fragment
            var paramName = type.Substring(1);
            var param = method.Parameters.FirstOrDefault(p => p.Name == paramName);
            if (param != null)
            {
                return $"{{RUNTIME_SET_{paramName}}}"; // Marker for code generation
            }
        }

        if (dynamicSetParam != null)
        {
            // {{set}} with [DynamicSql(Type=Fragment)] parameter
            return $"{{RUNTIME_SET_DYNAMIC_{dynamicSetParam.Name}}}"; // Marker for code generation
        }

        // Static SET generation (existing behavior)
        if (entityType == null)
        {
            if (method == null) return string.Empty;
            var filteredParams = method.Parameters
                .Where(p => NonSystemParameterFilter(p) && !p.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                .Select(p => $"{SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name)} = {dialect.ParameterPrefix}{p.Name}");
            return string.Join(", ", filteredParams);
        }

        var properties = GetFilteredProperties(entityType, options, "Id", requireSetter: true, result);
        return string.Join(", ", properties.Select(p =>
        {
            var columnName = SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name);
            var paramName = columnName;
            return $"{columnName} = {dialect.ParameterPrefix}{paramName}";
        }));
    }


    /// <summary>Processes ORDERBY placeholder - supports dynamic runtime ordering</summary>
    private static string ProcessOrderByPlaceholder(string type, INamedTypeSymbol? entityType, string options, SqlDefine dialect, IMethodSymbol method)
    {
        // Check for dynamic ORDERBY parameter
        var dynamicOrderParam = method.Parameters.FirstOrDefault(p =>
            p.GetAttributes().Any(a =>
                a.AttributeClass?.Name == "DynamicSqlAttribute" &&
                a.NamedArguments.Any(arg =>
                    arg.Key == "Type" &&
                    arg.Value.Value?.ToString() == "1"))); // Fragment = 1

        // Priority: @paramName > dynamic param > options > type
        if (!string.IsNullOrWhiteSpace(type) && type.StartsWith("@"))
        {
            // {{orderby @customOrder}} - use parameter as ORDERBY fragment
            var paramName = type.Substring(1);
            var param = method.Parameters.FirstOrDefault(p => p.Name == paramName);
            if (param != null)
            {
                return $"{{RUNTIME_ORDERBY_{paramName}}}"; // Marker for code generation
            }
        }

        if (dynamicOrderParam != null)
        {
            // {{orderby}} with [DynamicSql(Type=Fragment)] parameter
            return $"{{RUNTIME_ORDERBY_DYNAMIC_{dynamicOrderParam.Name}}}"; // Marker for code generation
        }

        // Static ORDERBY generation (existing behavior)
        // 优先处理 options（新格式）：created_at --desc
        if (!string.IsNullOrWhiteSpace(options))
        {
            // 解析格式：column_name --asc/--desc
            var optionsParts = options.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (optionsParts.Length >= 1)
            {
                var columnName = optionsParts[0].Trim();
                var direction = "ASC"; // 默认升序

                // 查找方向选项
                for (int i = 1; i < optionsParts.Length; i++)
                {
                    var part = optionsParts[i].ToLowerInvariant();
                    if (part == "--desc")
                    {
                        direction = "DESC";
                        break;
                    }
                    else if (part == "--asc")
                    {
                        direction = "ASC";
                        break;
                    }
                }

                return $"ORDER BY {dialect.WrapColumn(columnName)} {direction}";
            }
        }

        // 兼容旧格式：处理 type 参数
        if (!string.IsNullOrWhiteSpace(type))
        {
            var orderBy = type.ToLowerInvariant() switch
            {
                "id" => "ORDER BY id ASC",
                "id_desc" => "ORDER BY id DESC",
                "name" => "ORDER BY name ASC",
                "name_desc" => "ORDER BY name DESC",
                "created" => "ORDER BY created_at DESC",
                "created_asc" => "ORDER BY created_at ASC",
                "updated" => "ORDER BY updated_at DESC",
                "updated_asc" => "ORDER BY updated_at ASC",
                "date" => "ORDER BY created_at DESC",
                "priority" => "ORDER BY priority DESC, created_at DESC",
                "random" => dialect.Equals(SqlDefine.SqlServer) ? "ORDER BY NEWID()" :
                           dialect.Equals(SqlDefine.MySql) ? "ORDER BY RAND()" :
                           dialect.Equals(SqlDefine.PostgreSql) ? "ORDER BY RANDOM()" :
                           dialect.Equals(SqlDefine.SQLite) ? "ORDER BY RANDOM()" :
                           "ORDER BY NEWID()",
                "rand" => dialect.Equals(SqlDefine.MySql) ? "ORDER BY RAND()" : "ORDER BY NEWID()",
                _ => null
            };

            if (orderBy != null) return orderBy;

            // 智能解析自定义排序 - 支持格式如 "field_asc", "field_desc"
            if (type.Contains('_'))
            {
                var parts = type.Split('_');
                if (parts.Length == 2)
                {
                    var field = parts[0];
                    var direction = parts[1].ToUpperInvariant();
                    if (direction is "ASC" or "DESC")
                    {
                        var columnName = SharedCodeGenerationUtilities.ConvertToSnakeCase(field);
                        return $"ORDER BY {dialect.WrapColumn(columnName)} {direction}";
                    }
                }
            }
        }

        // 默认排序
        return $"ORDER BY {dialect.WrapColumn("id")} ASC";
    }


    /// <summary>生成自动WHERE子句 - 多数据库支持</summary>
    private string GenerateAutoWhereClause(IMethodSymbol method, SqlDefine dialect) =>
        method?.Parameters.Any(NonSystemParameterFilter) == true
            ? string.Join(" AND ", method.Parameters.Where(NonSystemParameterFilter).Select(p => $"{SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name)} = {dialect.ParameterPrefix}{p.Name}"))
            : "1=1";



    private void ProcessParameters(string sql, IMethodSymbol method, SqlTemplateResult result)
    {
        if (method == null) return; // 防护性检查

        // 使用字典提高查找性能，避免多次遍历
        var methodParamDict = method.Parameters
            .Where(NonSystemParameterFilter)
            .ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

        // 从SQL中提取所有参数引用
        foreach (Match match in ParameterRegex.Matches(sql))
        {
            var fullParam = match.Value;  // @name, :name, $name
            var paramName = fullParam.Substring(1); // 移除前缀

            // 处理不同的参数名格式（移除前缀变种）
            var cleanParamName = paramName;
            if (paramName.StartsWith("@") || paramName.StartsWith(":") || paramName.StartsWith("$"))
            {
                cleanParamName = paramName.Substring(1);
            }

            // 尝试匹配方法参数（优先匹配清理后的名称，然后匹配原始名称）
            var matchedParam = methodParamDict.ContainsKey(cleanParamName) ? cleanParamName :
                             methodParamDict.ContainsKey(paramName) ? paramName : null;

            if (matchedParam != null)
            {
                if (!result.Parameters.ContainsKey(paramName))
                {
                    result.Parameters.Add(paramName, null); // 模板处理阶段只记录参数名
                }
            }
            else
            {
                // 为了测试兼容性，我们记录所有参数，即使在方法签名中找不到
                if (!result.Parameters.ContainsKey(paramName))
                {
                    result.Parameters.Add(paramName, null);
                }
            }
        }
    }

    private static bool HasDynamicFeatures(string sql) =>
        sql.Contains("IF") || sql.Contains("CASE") || sql.Contains("WHILE") || sql.Contains("{{");

    /// <summary>统一的属性过滤逻辑，减少重复代码</summary>
    private List<IPropertySymbol> GetFilteredProperties(INamedTypeSymbol entityType, string options, string? excludeProperty = null, bool requireSetter = false, SqlTemplateResult? result = null)
    {
        // 预构建排除集合，提高查找性能
        var excludeSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (excludeProperty != null) excludeSet.Add(excludeProperty);

        // 解析排除选项（支持两种格式）：
        // 新格式：--exclude Id CreatedAt
        // 旧格式：exclude=Id,CreatedAt 或 exclude=Id
        var newFormatExclude = ExtractCommandLineOption(options, "--exclude");
        var oldFormatExclude = ExtractOption(options, "exclude", "");
        var excludeOption = !string.IsNullOrEmpty(newFormatExclude) ? newFormatExclude : oldFormatExclude;

        if (!string.IsNullOrEmpty(excludeOption))
        {
            // 支持空格和逗号分隔的多个列名
            foreach (var item in excludeOption.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
                excludeSet.Add(item.Trim());
        }

        // 解析 --regex 选项（用于正则表达式过滤）
        var regexPattern = ExtractCommandLineOption(options, "--regex");
        Regex? compiledRegex = null;
        if (!string.IsNullOrEmpty(regexPattern))
        {
            try
            {
                // 从缓存获取或编译新正则表达式
                compiledRegex = RegexCache.GetOrAdd(regexPattern, pattern =>
                {
                    // 编译正则表达式，使用超时防止ReDoS
                    return new Regex(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant, RegexTimeout);
                });
            }
            catch (ArgumentException ex)
            {
                // 正则表达式语法错误
                throw new ArgumentException($"Invalid regex pattern '{regexPattern}': {ex.Message}", ex);
            }
        }

        // 解析 --only 选项（显式包含列）
        var onlyOption = ExtractCommandLineOption(options, "--only");
        var onlySet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(onlyOption))
        {
            foreach (var item in onlyOption.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries))
                onlySet.Add(item.Trim());
        }

        // 检查显式包含敏感字段的选项
        var includeOption = ExtractOption(options, "include", "");
        var includeSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrEmpty(includeOption))
        {
            foreach (var item in includeOption.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                includeSet.Add(item.Trim());
        }

        // 单次遍历过滤，避免多次枚举
        // 性能优化：预估容量减少重分配
        var properties = new List<IPropertySymbol>(16); // 大多数实体不会超过16个属性
        foreach (var member in entityType.GetMembers())
        {
            if (member is IPropertySymbol property &&
                AccessiblePropertyFilter(property) &&
                (!requireSetter || property.SetMethod != null) &&
                !excludeSet.Contains(property.Name))
            {
                // 转换为snake_case进行过滤（与实际列名一致）
                var columnName = SharedCodeGenerationUtilities.ConvertToSnakeCase(property.Name);

                // --only 过滤（最高优先级）
                if (onlySet.Count > 0 && !onlySet.Contains(property.Name) && !onlySet.Contains(columnName))
                {
                    continue;
                }

                // --regex 过滤（在 --only 之后应用）
                if (compiledRegex != null)
                {
                    try
                    {
                        // 对列名进行正则匹配（尝试Property名和snake_case列名）
                        if (!compiledRegex.IsMatch(property.Name) && !compiledRegex.IsMatch(columnName))
                        {
                            continue; // 不匹配，跳过
                        }
                    }
                    catch (RegexMatchTimeoutException)
                    {
                        // 正则匹配超时 - ReDoS 保护
                        throw new InvalidOperationException($"Regex pattern '{regexPattern}' caused timeout - possible ReDoS attack");
                    }
                }

                // 敏感字段安全检测
                bool isSensitive = SensitiveFieldNames.Contains(property.Name);

                if (isSensitive)
                {
                    // 敏感字段默认排除，除非显式包含
                    if (includeSet.Contains(property.Name))
                    {
                        result?.Warnings.Add($"Including sensitive field '{property.Name}' - ensure this is intentional and secure");
                        properties.Add(property);
                    }
                    // 否则默认排除敏感字段
                }
                else
                {
                    properties.Add(property);
                }
            }
        }

        // 如果使用了--regex但没有匹配到任何列，添加警告
        if (compiledRegex != null && properties.Count == 0)
        {
            result?.Warnings.Add($"No columns matched the regex pattern '{regexPattern}'");
        }

        return properties;
    }

    /// <summary>
    /// 增强安全验证 - 基于数据库方言的安全检查
    /// 针对不同数据库的特定安全威胁进行检测
    /// </summary>
    private bool ValidateTemplateSecurity(string templateSql, SqlTemplateResult result, SqlDefine dialect)
    {
        // 在验证SQL注入之前，先移除占位符（避免占位符选项中的 -- 被误判为SQL注释）
        var sqlWithoutPlaceholders = PlaceholderRegex.Replace(templateSql, "__PLACEHOLDER__");

        // 基础SQL注入检测（在移除占位符后的SQL上进行）
        if (SqlInjectionRegex.IsMatch(sqlWithoutPlaceholders))
        {
            result.Errors.Add("Template contains potential SQL injection patterns");
            return false;
        }

        // 数据库特定安全检查（使用原始模板）
        ValidateDialectSpecificSecurity(templateSql, result, dialect);

        // 参数安全检查 - 确保使用正确的参数前缀（使用原始模板）
        ValidateParameterSafety(templateSql, result, dialect);

        // 性能优化：使用Count检查集合是否为空，比Any()更直接
        return result.Errors.Count == 0;
    }


    /// <summary>数据库特定安全验证</summary>
    private void ValidateDialectSpecificSecurity(string templateSql, SqlTemplateResult result, SqlDefine dialect)
    {
        var upper = templateSql.ToUpperInvariant();

        // 使用模式匹配简化数据库特定检查
        if (dialect.Equals(SqlDefine.PostgreSql) && upper.Contains("$$") && !upper.Contains("$BODY$"))
            result.Warnings.Add("PostgreSQL dollar-quoted strings detected, ensure they are safe");
        else if (dialect.Equals(SqlDefine.MySql) && (upper.Contains("LOAD_FILE") || upper.Contains("INTO OUTFILE")))
            result.Errors.Add("MySQL file operations detected, potential security risk");
        else if (dialect.Equals(SqlDefine.SqlServer) && (upper.Contains("OPENROWSET") || upper.Contains("OPENDATASOURCE")))
            result.Errors.Add("SQL Server external data access detected, potential security risk");
    }

    /// <summary>
    /// 参数安全检查 - 确保使用正确的参数语法
    /// </summary>
    private void ValidateParameterSafety(string templateSql, SqlTemplateResult result, SqlDefine dialect)
    {
        var matches = ParameterRegex.Matches(templateSql);
        foreach (Match match in matches)
        {
            var paramText = match.Value;
            if (!paramText.StartsWith(dialect.ParameterPrefix))
            {
                result.Warnings.Add($"Parameter '{paramText}' doesn't use the correct prefix for {GetDialectName(dialect)} (expected '{dialect.ParameterPrefix}')");
            }
        }
    }

    // 性能优化：预构建方言名称映射字典
    private static readonly Dictionary<SqlDefine, string> DialectNameMap = new()
    {
        [SqlDefine.MySql] = "MySQL",
        [SqlDefine.SqlServer] = "SQL Server",
        [SqlDefine.PostgreSql] = "PostgreSQL",
        [SqlDefine.SQLite] = "SQLite",
        [SqlDefine.Oracle] = "Oracle",
        [SqlDefine.DB2] = "DB2"
    };

    /// <summary>获取数据库方言名称 - 用于用户友好的错误提示</summary>
    private static string GetDialectName(SqlDefine dialect) => DialectNameMap.TryGetValue(dialect, out var name) ? name : "Unknown";

    private static string ExtractOption(string options, string key, string defaultValue)
    {
        if (string.IsNullOrEmpty(options)) return defaultValue;

        foreach (var pair in options.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var keyValue = pair.Split(new char[] { '=' }, 2);
            if (keyValue.Length == 2 && keyValue[0].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
                return keyValue[1].Trim();
        }

        return defaultValue;
    }

    /// <summary>
    /// 提取命令行风格的选项（例如：--exclude Id CreatedAt）
    /// </summary>
    private static string ExtractCommandLineOption(string options, string flag)
    {
        if (string.IsNullOrEmpty(options)) return string.Empty;

        var optionsLower = options.ToLowerInvariant();
        var flagLower = flag.ToLowerInvariant();
        var index = optionsLower.IndexOf(flagLower);

        if (index == -1) return string.Empty;

        // 找到flag后面的内容
        var startIndex = index + flag.Length;
        if (startIndex >= options.Length) return string.Empty;

        // 找到下一个--或结束
        var nextFlagIndex = options.IndexOf(" --", startIndex);
        var endIndex = nextFlagIndex == -1 ? options.Length : nextFlagIndex;

        return options.Substring(startIndex, endIndex - startIndex).Trim();
    }

    /// <summary>验证占位符选项</summary>
    private static void ValidatePlaceholderOptions(string placeholderName, string placeholderType, string options, SqlTemplateResult result)
    {
        if (string.IsNullOrEmpty(options)) return;

        var parts = options.Split('|');
        foreach (var part in parts)
        {
            if (part.Contains('='))
            {
                var keyValue = part.Split('=');
                if (keyValue.Length == 2)
                {
                    var optionName = keyValue[0].Trim();
                    var optionValue = keyValue[1].Trim();

                    // 检查空值
                    if (string.IsNullOrEmpty(optionValue))
                    {
                        result.Warnings.Add($"Empty value for option '{optionName}' in placeholder '{placeholderName}'");
                        continue;
                    }

                    // 检查特定占位符的有效选项
                    if (!IsValidOptionForPlaceholder(placeholderName, optionName))
                    {
                        result.Warnings.Add($"Invalid option '{optionName}' for placeholder '{placeholderName}'. Template: SELECT * FROM {{{{table}}}} WHERE {{{{{placeholderName}:{placeholderType}|{optionName}={optionValue}}}}}");
                        continue;
                    }

                    // 检查数值类型选项
                    if (IsNumericOption(placeholderName, optionName) && !int.TryParse(optionValue, out _) && !decimal.TryParse(optionValue, out _))
                    {
                        result.Warnings.Add($"Invalid numeric value '{optionValue}' for option '{optionName}' in placeholder '{placeholderName}'");
                    }
                }
            }
            else if (!part.Contains('='))
            {
                // 检查类型是否有效 (对于没有=的部分，例如 {{limit:invalid_type|default=20}})
                if (placeholderName == "limit" && !IsValidLimitType(part.Trim()))
                {
                    result.Warnings.Add($"Invalid limit type '{part.Trim()}' for placeholder 'limit'");
                }
            }
        }

        // 额外检查: 对于 {{limit:type|options}} 格式，检查type部分
        if (placeholderName == "limit" && !string.IsNullOrEmpty(placeholderType) && !IsValidLimitType(placeholderType))
        {
            result.Warnings.Add($"Invalid limit type '{placeholderType}' for placeholder 'limit'");
        }
    }

    /// <summary>检查选项是否对指定占位符有效</summary>
    private static bool IsValidOptionForPlaceholder(string placeholderName, string optionName)
    {
        return placeholderName.ToLowerInvariant() switch
        {
            "between" => optionName is "min" or "max" or "column",
            "like" => optionName is "pattern" or "column" or "mode",
            "in" => optionName is "values" or "column",
            "or" => optionName is "conditions" or "columns",
            "round" => optionName is "decimals" or "column",
            "limit" => optionName is "default" or "offset",
            "columns" => optionName is "exclude" or "include",
            "values" => optionName is "exclude" or "include",
            "contains" => optionName is "text" or "column" or "value",
            "startswith" => optionName is "prefix" or "column",
            "endswith" => optionName is "suffix" or "column",
            _ => false
        };
    }

    /// <summary>检查选项是否为数值类型</summary>
    private static bool IsNumericOption(string placeholderName, string optionName)
    {
        return placeholderName.ToLowerInvariant() switch
        {
            "round" => optionName is "decimals",
            "limit" => optionName is "default" or "offset",
            _ => false
        };
    }

    /// <summary>检查limit类型是否有效</summary>
    private static bool IsValidLimitType(string limitType) =>
        limitType.ToLowerInvariant() is "small" or "medium" or "large" or "page" or "batch"
            or "sqlite" or "sqlserver" or "mysql" or "postgresql" or "oracle";

    /// <summary>验证类型不匹配</summary>
    private static void ValidateTypeMismatch(string placeholderName, string placeholderType, string options, INamedTypeSymbol? entityType, SqlTemplateResult result)
    {
        if (entityType == null) return;

        // 获取字段名称
        var fieldName = GetFieldNameForValidation(placeholderName, placeholderType, options);
        if (string.IsNullOrEmpty(fieldName)) return;

        // 查找属性 (支持snake_case到PascalCase转换)
        var property = entityType.GetMembers()
            .OfType<IPropertySymbol>()
            .FirstOrDefault(p => p.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase) ||
                                p.Name.Equals(ConvertToPascalCase(fieldName), StringComparison.OrdinalIgnoreCase));

        if (property == null) return;

        // 检查类型兼容性
        var typeName = property.Type.Name;
        var isNumeric = IsNumericType(typeName);
        var isString = IsStringType(typeName);
        var isBoolean = IsBooleanType(typeName);
        var isDateTime = IsDateTimeType(typeName);

        var incompatibleOperation = placeholderName switch
        {
            "sum" or "avg" or "max" or "min" when !isNumeric => $"Cannot apply {placeholderName.ToUpper()} to non-numeric field '{fieldName}' of type {typeName}",
            "round" or "abs" or "ceiling" or "floor" when !isNumeric => $"Cannot apply {placeholderName.ToUpper()} to non-numeric field '{fieldName}' of type {typeName}",
            "upper" or "lower" or "trim" or "contains" or "startswith" or "endswith" when !isString => $"Cannot apply string function {placeholderName.ToUpper()} to non-string field '{fieldName}' of type {typeName}",
            "today" or "week" or "month" or "year" when !isDateTime => $"Cannot apply date function {placeholderName.ToUpper()} to field '{fieldName}' of type {typeName}",
            _ => null
        };

        if (!string.IsNullOrEmpty(incompatibleOperation))
        {
            result.Warnings.Add(incompatibleOperation!);
        }
    }

    /// <summary>获取用于验证的字段名称</summary>
    private static string GetFieldNameForValidation(string placeholderName, string placeholderType, string options)
    {
        // 从options中提取字段名或使用type作为字段名
        var fieldName = ExtractOption(options, "column", placeholderType);
        if (string.IsNullOrEmpty(fieldName))
        {
            fieldName = ExtractOption(options, "field", placeholderType);
        }
        return fieldName;
    }

    /// <summary>检查是否为数值类型</summary>
    private static bool IsNumericType(string typeName) => typeName switch
    {
        "Int32" or "Int64" or "Int16" or "Decimal" or "Double" or "Single" or "Byte" or "SByte" or "UInt32" or "UInt64" or "UInt16" => true,
        _ => false
    };

    /// <summary>检查是否为字符串类型</summary>
    private static bool IsStringType(string typeName) => typeName == "String";

    /// <summary>检查是否为布尔类型</summary>
    private static bool IsBooleanType(string typeName) => typeName == "Boolean";

    /// <summary>检查是否为日期时间类型</summary>
    private static bool IsDateTimeType(string typeName) => typeName == "DateTime" || typeName == "DateTimeOffset" || typeName == "DateOnly" || typeName == "TimeOnly";

    /// <summary>将snake_case转换为PascalCase</summary>
    private static string ConvertToPascalCase(string snakeCase)
    {
        if (string.IsNullOrEmpty(snakeCase)) return snakeCase;

        var parts = snakeCase.Split('_');
        var result = new System.Text.StringBuilder();

        foreach (var part in parts)
        {
            if (!string.IsNullOrEmpty(part))
            {
                result.Append(char.ToUpper(part[0]));
                if (part.Length > 1)
                {
                    result.Append(part.Substring(1).ToLower());
                }
            }
        }

        return result.ToString();
    }





    #region 辅助生成方法

    /// <summary>从方法参数生成GROUP BY子句</summary>
    private static string GenerateGroupByFromMethod(IMethodSymbol method)
    {
        if (method == null) return "GROUP BY id";
        // 性能优化：避免ToList()，直接操作枚举
        var filteredParams = method.Parameters.Where(NonSystemParameterFilter);
        var groupByColumns = filteredParams.Select(p => SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name));
        var joinedColumns = string.Join(", ", groupByColumns);

        if (string.IsNullOrEmpty(joinedColumns)) return "GROUP BY id";

        return $"GROUP BY {joinedColumns}";
    }

    /// <summary>从方法参数生成HAVING子句</summary>
    private static string GenerateHavingFromMethod(IMethodSymbol method)
    {
        if (method == null) return "HAVING COUNT(*) > 0";

        // 性能优化：避免ToList()，直接操作枚举
        var filteredParams = method.Parameters.Where(NonSystemParameterFilter);

        // 为聚合查询生成HAVING条件
        var conditions = filteredParams.Select(p =>
        {
            var columnName = SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name);
            return p.Type.SpecialType == SpecialType.System_Int32 || p.Type.SpecialType == SpecialType.System_Int64
                ? $"COUNT({columnName}) > @{p.Name}"
                : $"{columnName} = @{p.Name}";
        });

        var joinedConditions = string.Join(" AND ", conditions);
        return string.IsNullOrEmpty(joinedConditions) ? "HAVING COUNT(*) > 0" : $"HAVING {joinedConditions}";
    }

    #endregion

    private static string ProcessCustomPlaceholder(string originalValue, string name, string type, string options, SqlTemplateResult result)
    {
        result.Warnings.Add($"Unknown placeholder '{name}'. Available placeholders: " +
            "Core: table, columns, values, where, set, orderby, limit | " +
            "Joins: join, groupby, having | " +
            "CRUD: select, insert, update, delete, upsert | " +
            "Aggregates: count, sum, avg, max, min, distinct, group_concat | " +
            "Conditions: between, like, in, not_in, or, isnull, notnull, exists, case, coalesce, ifnull | " +
            "Dates: today, week, month, year, date_add, date_diff, date_format | " +
            "Strings: contains, startswith, endswith, upper, lower, trim, substring, concat, replace, length | " +
            "Math: round, abs, ceiling, floor, power, sqrt, mod | " +
            "Window: row_number, rank, dense_rank, lag, lead | " +
            "JSON: json_extract, json_array, json_object | " +
            "Pagination: page, pagination, top, offset | " +
            "Conversions: cast, convert | " +
            "Batch: batch_values, batch_insert, bulk_update, subquery, union");
        return originalValue; // 保持原始值
    }

    /// <summary>简化的性能检查</summary>
    private static void CheckBasicPerformance(string template, TemplateValidationResult result)
    {
        var upper = template.ToUpperInvariant();

        if (upper.Contains("SELECT *"))
            result.Suggestions.Add("建议使用 {{columns:auto}} 替代 SELECT *");

        if (upper.Contains("ORDER BY") && !upper.Contains("LIMIT") && !upper.Contains("TOP"))
            result.Suggestions.Add("ORDER BY 建议添加 {{limit:auto}} 限制");

        if ((upper.Contains("UPDATE") || upper.Contains("DELETE")) && !upper.Contains("WHERE"))
            result.Warnings.Add("UPDATE/DELETE 语句建议添加 WHERE 条件");
    }

    #region 多数据库支持的扩展占位符方法

    /// <summary>处理LIMIT占位符 - 多数据库支持</summary>
    private static string ProcessLimitPlaceholder(string type, IMethodSymbol method, string options, SqlDefine dialect)
    {
        return SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport.ProcessLimitPlaceholder(type, options, dialect);
    }

    /// <summary>处理聚合函数占位符 - 多数据库支持</summary>
    private static string ProcessAggregateFunction(string function, string type, string options, SqlDefine dialect)
    {
        return SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport.ProcessAggregateFunction(function, type, options, dialect);
    }

    /// <summary>Processes JOIN placeholder - supports dynamic runtime joins</summary>
    private static string ProcessJoinPlaceholder(string type, INamedTypeSymbol? entityType, string options, SqlDefine dialect, IMethodSymbol method)
    {
        // DEBUG: Log inputs
        // System.Diagnostics.Debug.WriteLine($"ProcessJoinPlaceholder: type='{type}', options='{options}'");

        // Auto-detect: Check for DynamicSql Fragment parameter
        var dynamicJoinParam = method.Parameters.FirstOrDefault(p =>
            p.GetAttributes().Any(a =>
                a.AttributeClass?.Name == "DynamicSqlAttribute" &&
                a.NamedArguments.Any(arg =>
                    arg.Key == "Type" &&
                    arg.Value.Value?.ToString() == "1"))); // Fragment = 1

        // Priority 1: Explicit parameter reference {{join @paramName}}
        var paramSource = !string.IsNullOrWhiteSpace(options) && options.StartsWith("@") ? options :
                         !string.IsNullOrWhiteSpace(type) && type.StartsWith("@") ? type : null;

        if (paramSource != null)
        {
            var paramName = paramSource.Substring(1).Trim();
            // DEBUG: Check parameter matching
            // System.Diagnostics.Debug.WriteLine($"  Looking for param: '{paramName}'");
            // System.Diagnostics.Debug.WriteLine($"  Available params: {string.Join(", ", method.Parameters.Select(p => p.Name))}");
            var param = method.Parameters.FirstOrDefault(p => p.Name.Equals(paramName, StringComparison.Ordinal));
            if (param != null)
            {
                // DEBUG:
                // System.Diagnostics.Debug.WriteLine($"  FOUND! Returning RUNTIME_JOIN_{paramName}");
                return $"{{RUNTIME_JOIN_{paramName}}}";
            }
            // DEBUG:
            // System.Diagnostics.Debug.WriteLine($"  NOT FOUND - will try auto-detect");
        }

        // Priority 2: Auto-detect DynamicSql Fragment parameter (like SET/WHERE/ORDERBY)
        if (dynamicJoinParam != null)
        {
            // DEBUG:
            // System.Diagnostics.Debug.WriteLine($"  Auto-detected: {dynamicJoinParam.Name}");
            return $"{{RUNTIME_JOIN_DYNAMIC_{dynamicJoinParam.Name}}}";
        }

        // Static JOIN generation (existing behavior)
        // DEBUG:
        // System.Diagnostics.Debug.WriteLine($"  Falling back to static JOIN");
        return SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport.ProcessGenericPlaceholder("join", type, options, dialect);
    }

    /// <summary>Processes GROUPBY placeholder - supports dynamic runtime grouping</summary>
    private static string ProcessGroupByPlaceholder(string type, INamedTypeSymbol? entityType, IMethodSymbol method, string options, SqlDefine dialect)
    {
        // Auto-detect: Check for DynamicSql Fragment parameter
        var dynamicGroupParam = method.Parameters.FirstOrDefault(p =>
            p.GetAttributes().Any(a =>
                a.AttributeClass?.Name == "DynamicSqlAttribute" &&
                a.NamedArguments.Any(arg =>
                    arg.Key == "Type" &&
                    arg.Value.Value?.ToString() == "1"))); // Fragment = 1

        // Priority 1: Explicit parameter reference {{groupby @paramName}}
        var paramSource = !string.IsNullOrWhiteSpace(options) && options.StartsWith("@") ? options :
                         !string.IsNullOrWhiteSpace(type) && type.StartsWith("@") ? type : null;

        if (paramSource != null)
        {
            var paramName = paramSource.Substring(1).Trim();
            var param = method.Parameters.FirstOrDefault(p => p.Name == paramName);
            if (param != null)
            {
                return $"{{RUNTIME_GROUPBY_{paramName}}}";
            }
        }

        // Priority 2: Auto-detect DynamicSql Fragment parameter (like SET/WHERE/ORDERBY)
        if (dynamicGroupParam != null)
        {
            return $"{{RUNTIME_GROUPBY_DYNAMIC_{dynamicGroupParam.Name}}}";
        }

        // Static GROUPBY generation (existing behavior)
        return SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport.ProcessGenericPlaceholder("groupby", type, options, dialect);
    }

    /// <summary>处理HAVING占位符 - 多数据库支持</summary>
    private static string ProcessHavingPlaceholder(string type, IMethodSymbol method, string options, SqlDefine dialect)
    {
        return SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport.ProcessGenericPlaceholder("having", type, options, dialect);
    }

    /// <summary>处理SELECT占位符 - 多数据库支持</summary>
    private static string ProcessSelectPlaceholder(string type, INamedTypeSymbol? entityType, string options, SqlDefine dialect)
    {
        return SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport.ProcessGenericPlaceholder("select", type, options, dialect);
    }

    /// <summary>
    /// 处理INSERT占位符 - 支持自动生成完整INSERT语句
    /// 用法：
    /// - {{insert}} 或 {{insert:auto}} - 生成完整的 INSERT INTO table (columns) VALUES (values)
    /// - {{insert:into}} - 仅生成 INSERT INTO table
    /// - {{insert:auto|exclude=Id,CreatedAt}} - 排除指定列
    /// </summary>
    private static string ProcessInsertPlaceholder(string type, string tableName, string options, SqlDefine dialect)
    {
        var snakeTableName = SharedCodeGenerationUtilities.ConvertToSnakeCase(tableName);

        // {{insert:into}} - 只返回 INSERT INTO table_name
        if (type == "into")
        {
            return $"INSERT INTO {snakeTableName}";
        }

        // {{insert}} 或 {{insert:auto}} - 生成完整INSERT语句（不含VALUES）
        // 返回 INSERT INTO table_name，columns和values由单独的占位符处理
        return $"INSERT INTO {snakeTableName}";
    }

    /// <summary>处理UPDATE占位符 - 多数据库支持</summary>
    private static string ProcessUpdatePlaceholder(string type, string tableName, string options, SqlDefine dialect)
    {
        var snakeTableName = SharedCodeGenerationUtilities.ConvertToSnakeCase(tableName);
        return $"UPDATE {snakeTableName}";
    }

    /// <summary>处理DELETE占位符 - 多数据库支持</summary>
    private static string ProcessDeletePlaceholder(string type, string tableName, string options, SqlDefine dialect)
    {
        var snakeTableName = SharedCodeGenerationUtilities.ConvertToSnakeCase(tableName);
        return type == "from" ? $"DELETE FROM {snakeTableName}" : $"DELETE FROM {snakeTableName}";
    }

    /// <summary>处理DISTINCT占位符 - 多数据库支持</summary>
    private static string ProcessDistinctPlaceholder(string type, string options, SqlDefine dialect)
    {
        return SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport.ProcessGenericPlaceholder("distinct", type, options, dialect);
    }

    /// <summary>处理UNION占位符 - 多数据库支持</summary>
    private static string ProcessUnionPlaceholder(string type, string options, SqlDefine dialect)
    {
        return SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport.ProcessGenericPlaceholder("union", type, options, dialect);
    }

    /// <summary>处理TOP占位符 - 多数据库支持</summary>
    private static string ProcessTopPlaceholder(string type, string options, SqlDefine dialect)
    {
        return SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport.ProcessGenericPlaceholder("top", type, options, dialect);
    }

    /// <summary>处理OFFSET占位符 - 多数据库支持</summary>
    private static string ProcessOffsetPlaceholder(string type, string options, SqlDefine dialect)
    {
        return SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport.ProcessGenericPlaceholder("offset", type, options, dialect);
    }

    #region 增强的条件占位符处理

    /// <summary>处理BETWEEN占位符 - 范围查询</summary>
    private static string ProcessBetweenPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        var min = ExtractOption(options, "min", "minValue");
        var max = ExtractOption(options, "max", "maxValue");

        // 避免重复添加参数前缀
        var minParam = min.StartsWith("@") || min.StartsWith(":") || min.StartsWith("$") ? min : $"{dialect.ParameterPrefix}{min}";
        var maxParam = max.StartsWith("@") || max.StartsWith(":") || max.StartsWith("$") ? max : $"{dialect.ParameterPrefix}{max}";

        return $"{dialect.WrapColumn(column)} BETWEEN {minParam} AND {maxParam}";
    }

    /// <summary>处理LIKE占位符 - 模糊搜索</summary>
    private static string ProcessLikePlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        var pattern = ExtractOption(options, "pattern", "pattern");
        var mode = ExtractOption(options, "mode", "contains"); // contains, starts, ends, exact

        return mode switch
        {
            "starts" => $"{dialect.WrapColumn(column)} LIKE CONCAT({dialect.ParameterPrefix}{pattern}, '%')",
            "ends" => $"{dialect.WrapColumn(column)} LIKE CONCAT('%', {dialect.ParameterPrefix}{pattern})",
            "exact" => $"{dialect.WrapColumn(column)} LIKE {dialect.ParameterPrefix}{pattern}",
            _ => $"{dialect.WrapColumn(column)} LIKE CONCAT('%', {dialect.ParameterPrefix}{pattern}, '%')" // contains
        };
    }

    /// <summary>处理IN/NOT IN占位符 - 优化合并版本</summary>
    private static string ProcessInPlaceholder(string type, string options, SqlDefine dialect, bool isNotIn = false)
    {
        var column = ExtractOption(options, "column", type);
        var values = ExtractOption(options, "values", "values");
        var operation = isNotIn ? "NOT IN" : "IN";

        return $"{dialect.WrapColumn(column)} {operation} ({dialect.ParameterPrefix}{values})";
    }

    /// <summary>处理OR占位符 - 多条件OR组合</summary>
    private static string ProcessOrPlaceholder(string type, string options, SqlDefine dialect)
    {
        var conditions = ExtractOption(options, "conditions", type);
        var columns = ExtractOption(options, "columns", "");

        if (!string.IsNullOrEmpty(columns))
        {
            // 多列OR: column1 = @value OR column2 = @value
            var columnList = columns.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var orConditions = columnList.Select(col =>
                $"{dialect.WrapColumn(col.Trim())} = {dialect.ParameterPrefix}{conditions}");
            return $"({string.Join(" OR ", orConditions)})";
        }

        // 条件OR: (condition1 OR condition2)
        return $"({conditions.Replace(",", " OR ")})";
    }

    /// <summary>处理IS NULL/IS NOT NULL占位符 - 优化合并版本</summary>
    private static string ProcessIsNullPlaceholder(string type, string options, SqlDefine dialect, bool isNotNull = false)
    {
        var column = ExtractOption(options, "column", type);
        var operation = isNotNull ? "IS NOT NULL" : "IS NULL";
        return $"{dialect.WrapColumn(column)} {operation}";
    }

    #endregion

    #region 日期时间函数占位符

    /// <summary>处理TODAY占位符 - 当前日期</summary>
    private static string ProcessTodayPlaceholder(string type, string options, SqlDefine dialect)
    {
        var format = ExtractOption(options, "format", "date"); // date, datetime, timestamp
        var field = ExtractOption(options, "field", type); // 使用type作为字段名，如果没有明确指定field选项

        var dateFunction = format switch
        {
            "datetime" => GetCurrentDateTimeFunction(dialect),
            "timestamp" => GetCurrentTimestampFunction(dialect),
            _ => GetCurrentDateFunction(dialect)
        };

        // 如果有字段名，生成比较条件；否则只返回日期函数
        return !string.IsNullOrEmpty(field) && field != "today"
            ? $"{field} = {dateFunction}"
            : dateFunction;
    }

    /// <summary>处理WEEK占位符 - 周相关函数</summary>
    private static string ProcessWeekPlaceholder(string type, string options, SqlDefine dialect)
    {
        var operation = ExtractOption(options, "op", "number"); // start, end, number
        var field = ExtractOption(options, "field", type); // 使用type作为字段名

        var weekFunction = operation switch
        {
            "start" => GetWeekStartFunction(dialect),
            "end" => GetWeekEndFunction(dialect),
            "number" => GetWeekNumberFunction(dialect),
            _ => GetWeekNumberFunction(dialect)
        };

        // 如果有字段名，生成比较条件；否则只返回周函数
        return !string.IsNullOrEmpty(field) && field != "week"
            ? $"{field} = {weekFunction}"
            : weekFunction;
    }

    /// <summary>处理MONTH占位符 - 月份相关函数</summary>
    private static string ProcessMonthPlaceholder(string type, string options, SqlDefine dialect)
    {
        var operation = ExtractOption(options, "op", "number"); // start, end, name, number
        var field = ExtractOption(options, "field", type); // 使用type作为字段名

        var monthFunction = operation switch
        {
            "start" => GetMonthStartFunction(dialect),
            "end" => GetMonthEndFunction(dialect),
            "name" => GetMonthNameFunction(dialect),
            "number" => GetMonthNumberFunction(dialect),
            _ => GetMonthNumberFunction(dialect)
        };

        // 如果有字段名，生成比较条件；否则只返回月份函数
        return !string.IsNullOrEmpty(field) && field != "month"
            ? $"{field} = {monthFunction}"
            : monthFunction;
    }

    /// <summary>处理YEAR占位符 - 年份函数</summary>
    private static string ProcessYearPlaceholder(string type, string options, SqlDefine dialect)
    {
        var field = ExtractOption(options, "field", type); // 使用type作为字段名
        var column = ExtractOption(options, "column", type != "year" ? type : "created_at");

        var yearFunction = dialect.Equals(SqlDefine.SqlServer) ? $"YEAR({dialect.WrapColumn(column)})" :
                          dialect.Equals(SqlDefine.MySql) ? $"YEAR({dialect.WrapColumn(column)})" :
                          dialect.Equals(SqlDefine.PostgreSql) ? $"EXTRACT(YEAR FROM {dialect.WrapColumn(column)})" :
                          dialect.Equals(SqlDefine.Oracle) ? $"EXTRACT(YEAR FROM {dialect.WrapColumn(column)})" :
                          $"strftime('%Y', {dialect.WrapColumn(column)})"; // SQLite

        // 如果有字段名且与column不同，生成比较条件；否则只返回年份函数
        return !string.IsNullOrEmpty(field) && field != "year" && field != column
            ? $"{field} = {yearFunction}"
            : yearFunction;
    }

    /// <summary>处理DATE_ADD占位符 - 日期加法</summary>
    private static string ProcessDateAddPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", "date_column");
        var interval = ExtractOption(options, "interval", "1");
        var unit = ExtractOption(options, "unit", "DAY"); // DAY, WEEK, MONTH, YEAR

        return dialect.Equals(SqlDefine.SqlServer)
            ? $"DATEADD({unit}, {interval}, {dialect.WrapColumn(column)})"
            : dialect.Equals(SqlDefine.MySql)
                ? $"DATE_ADD({dialect.WrapColumn(column)}, INTERVAL {interval} {unit})"
                : $"({dialect.WrapColumn(column)} + INTERVAL {interval} {unit})"; // PostgreSQL, SQLite
    }

    /// <summary>处理DATE_DIFF占位符 - 日期差值</summary>
    private static string ProcessDateDiffPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column1 = ExtractOption(options, "column1", "end_date");
        var column2 = ExtractOption(options, "column2", "start_date");
        var unit = ExtractOption(options, "unit", "DAY");

        return dialect.Equals(SqlDefine.SqlServer)
            ? $"DATEDIFF({unit}, {dialect.WrapColumn(column2)}, {dialect.WrapColumn(column1)})"
            : dialect.Equals(SqlDefine.MySql)
                ? $"DATEDIFF({dialect.WrapColumn(column1)}, {dialect.WrapColumn(column2)})"
                : $"({dialect.WrapColumn(column1)} - {dialect.WrapColumn(column2)})"; // PostgreSQL, SQLite
    }

    // 数据库特定日期时间函数
    private static string GetCurrentDateFunction(SqlDefine dialect) =>
        dialect.Equals(SqlDefine.SqlServer) ? "CAST(GETDATE() AS DATE)" :
        dialect.Equals(SqlDefine.MySql) ? "CURDATE()" :
        dialect.Equals(SqlDefine.PostgreSql) ? "CURRENT_DATE" :
        dialect.Equals(SqlDefine.Oracle) ? "TRUNC(SYSDATE)" :
        "date('now')"; // SQLite

    private static string GetCurrentDateTimeFunction(SqlDefine dialect) =>
        dialect.Equals(SqlDefine.SqlServer) ? "GETDATE()" :
        dialect.Equals(SqlDefine.MySql) ? "NOW()" :
        dialect.Equals(SqlDefine.PostgreSql) ? "NOW()" :
        dialect.Equals(SqlDefine.Oracle) ? "SYSDATE" :
        "datetime('now')"; // SQLite

    private static string GetCurrentTimestampFunction(SqlDefine dialect) =>
        dialect.Equals(SqlDefine.SqlServer) ? "GETUTCDATE()" :
        dialect.Equals(SqlDefine.MySql) ? "UTC_TIMESTAMP()" :
        dialect.Equals(SqlDefine.PostgreSql) ? "NOW() AT TIME ZONE 'UTC'" :
        dialect.Equals(SqlDefine.Oracle) ? "SYS_EXTRACT_UTC(SYSTIMESTAMP)" :
        "datetime('now', 'utc')"; // SQLite

    private static string GetWeekStartFunction(SqlDefine dialect) =>
        dialect.Equals(SqlDefine.SqlServer) ? "DATEADD(week, DATEDIFF(week, 0, GETDATE()), 0)" :
        dialect.Equals(SqlDefine.MySql) ? "DATE_SUB(CURDATE(), INTERVAL WEEKDAY(CURDATE()) DAY)" :
        dialect.Equals(SqlDefine.PostgreSql) ? "date_trunc('week', CURRENT_DATE)" :
        "date('now', 'weekday 0', '-6 days')"; // SQLite

    private static string GetWeekEndFunction(SqlDefine dialect) =>
        dialect.Equals(SqlDefine.SqlServer) ? "DATEADD(week, DATEDIFF(week, 0, GETDATE()), 6)" :
        dialect.Equals(SqlDefine.MySql) ? "DATE_ADD(CURDATE(), INTERVAL (6 - WEEKDAY(CURDATE())) DAY)" :
        dialect.Equals(SqlDefine.PostgreSql) ? "date_trunc('week', CURRENT_DATE) + INTERVAL '6 days'" :
        "date('now', 'weekday 0')"; // SQLite

    private static string GetWeekNumberFunction(SqlDefine dialect) =>
        dialect.Equals(SqlDefine.SqlServer) ? "DATEPART(week, GETDATE())" :
        dialect.Equals(SqlDefine.MySql) ? "WEEK(CURDATE())" :
        dialect.Equals(SqlDefine.PostgreSql) ? "EXTRACT(week FROM CURRENT_DATE)" :
        "strftime('%W', 'now')"; // SQLite

    private static string GetMonthStartFunction(SqlDefine dialect) =>
        dialect.Equals(SqlDefine.SqlServer) ? "DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1)" :
        dialect.Equals(SqlDefine.MySql) ? "DATE_FORMAT(CURDATE(), '%Y-%m-01')" :
        dialect.Equals(SqlDefine.PostgreSql) ? "date_trunc('month', CURRENT_DATE)" :
        "date('now', 'start of month')"; // SQLite

    private static string GetMonthEndFunction(SqlDefine dialect) =>
        dialect.Equals(SqlDefine.SqlServer) ? "EOMONTH(GETDATE())" :
        dialect.Equals(SqlDefine.MySql) ? "LAST_DAY(CURDATE())" :
        dialect.Equals(SqlDefine.PostgreSql) ? "date_trunc('month', CURRENT_DATE) + INTERVAL '1 month' - INTERVAL '1 day'" :
        "date('now', 'start of month', '+1 month', '-1 day')"; // SQLite

    private static string GetMonthNameFunction(SqlDefine dialect) =>
        dialect.Equals(SqlDefine.SqlServer) ? "DATENAME(month, GETDATE())" :
        dialect.Equals(SqlDefine.MySql) ? "MONTHNAME(CURDATE())" :
        dialect.Equals(SqlDefine.PostgreSql) ? "to_char(CURRENT_DATE, 'Month')" :
        "strftime('%B', 'now')"; // SQLite

    private static string GetMonthNumberFunction(SqlDefine dialect) =>
        dialect.Equals(SqlDefine.SqlServer) ? "MONTH(GETDATE())" :
        dialect.Equals(SqlDefine.MySql) ? "MONTH(CURDATE())" :
        dialect.Equals(SqlDefine.PostgreSql) ? "EXTRACT(month FROM CURRENT_DATE)" :
        "strftime('%m', 'now')"; // SQLite

    #endregion

    #region 字符串函数占位符

    /// <summary>处理CONTAINS占位符 - 包含检查</summary>
    private static string ProcessContainsPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        var textValue = ExtractOption(options, "text", "");
        var value = !string.IsNullOrEmpty(textValue) ? textValue : ExtractOption(options, "value", "searchValue");

        // 如果value以@开头，说明是参数引用，直接使用；否则加上参数前缀
        var parameterValue = value.StartsWith("@") ? value.Substring(1) : value;

        return dialect.Equals(SqlDefine.SqlServer)
            ? $"{dialect.WrapColumn(column)} LIKE '%' + {dialect.ParameterPrefix}{parameterValue} + '%'"
            : dialect.Equals(SqlDefine.PostgreSql)
                ? $"{dialect.WrapColumn(column)} ILIKE '%' || {dialect.ParameterPrefix}{parameterValue} || '%'"
                : dialect.Equals(SqlDefine.MySql)
                    ? $"{dialect.WrapColumn(column)} LIKE CONCAT('%', {dialect.ParameterPrefix}{parameterValue}, '%')"
                    : $"{dialect.WrapColumn(column)} LIKE '%' || {dialect.ParameterPrefix}{parameterValue} || '%'"; // SQLite, Oracle, DB2
    }

    /// <summary>处理STARTSWITH占位符</summary>
    private static string ProcessStartsWithPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        var value = ExtractOption(options, "value", "prefix");

        return $"{dialect.WrapColumn(column)} LIKE CONCAT({dialect.ParameterPrefix}{value}, '%')";
    }

    /// <summary>处理ENDSWITH占位符</summary>
    private static string ProcessEndsWithPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        var value = ExtractOption(options, "value", "suffix");

        return $"{dialect.WrapColumn(column)} LIKE CONCAT('%', {dialect.ParameterPrefix}{value})";
    }

    /// <summary>处理字符串函数占位符 - 统一优化版本</summary>
    private static string ProcessStringFunction(string function, string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);

        return function.ToUpper() switch
        {
            "UPPER" => $"UPPER({dialect.WrapColumn(column)})",
            "LOWER" => $"LOWER({dialect.WrapColumn(column)})",
            "TRIM" => ExtractOption(options, "mode", "both") switch
            {
                "leading" => $"LTRIM({dialect.WrapColumn(column)})",
                "trailing" => $"RTRIM({dialect.WrapColumn(column)})",
                _ => $"TRIM({dialect.WrapColumn(column)})"
            },
            _ => $"{function.ToUpper()}({dialect.WrapColumn(column)})"
        };
    }

    #endregion

    #region 数学函数占位符

    /// <summary>处理数学函数占位符 - 统一优化版本</summary>
    private static string ProcessMathFunction(string function, string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);

        return function.ToUpper() switch
        {
            "ROUND" => $"ROUND({dialect.WrapColumn(column)}, {ExtractOption(options, "precision", "2")})",
            "ABS" => $"ABS({dialect.WrapColumn(column)})",
            "CEILING" => dialect.Equals(SqlDefine.SqlServer)
                ? $"CEILING({dialect.WrapColumn(column)})"
                : $"CEIL({dialect.WrapColumn(column)})",
            "FLOOR" => $"FLOOR({dialect.WrapColumn(column)})",
            _ => $"{function.ToUpper()}({dialect.WrapColumn(column)})"
        };
    }

    #endregion

    #region 批量操作和子查询占位符

    /// <summary>处理BATCH_VALUES占位符 - 批量插入值</summary>
    private static string ProcessBatchValuesPlaceholder(string type, string options, SqlDefine dialect)
    {
        var count = ExtractOption(options, "count", "1");
        var columns = ExtractOption(options, "columns", "");

        if (string.IsNullOrEmpty(columns))
            return $"VALUES {dialect.ParameterPrefix}batchValues";

        return $"VALUES ({string.Join(", ", columns.Split(',').Select(c => $"{dialect.ParameterPrefix}{c.Trim()}"))})";
    }

    /// <summary>处理UPSERT占位符 - 插入或更新</summary>
    private static string ProcessUpsertPlaceholder(string type, string tableName, string options, SqlDefine dialect)
    {
        var snakeTableName = SharedCodeGenerationUtilities.ConvertToSnakeCase(tableName);
        var conflictColumn = ExtractOption(options, "conflict", "id");

        return dialect.Equals(SqlDefine.PostgreSql)
            ? $"INSERT INTO {snakeTableName} {{{{values}}}} ON CONFLICT ({conflictColumn}) DO UPDATE SET {{{{set:auto}}}}"
            : dialect.Equals(SqlDefine.MySql)
                ? $"INSERT INTO {snakeTableName} {{{{values}}}} ON DUPLICATE KEY UPDATE {{{{set:auto}}}}"
                : dialect.Equals(SqlDefine.SQLite)
                    ? $"INSERT OR REPLACE INTO {snakeTableName} {{{{values}}}}"
                    : $"MERGE {snakeTableName} USING (VALUES {{{{values}}}}) AS src ON {conflictColumn} = src.{conflictColumn}"; // SQL Server
    }

    /// <summary>处理EXISTS占位符 - 存在性检查</summary>
    private static string ProcessExistsPlaceholder(string type, string options, SqlDefine dialect)
    {
        var subquery = ExtractOption(options, "query", "SELECT 1 FROM table WHERE condition");
        var negation = ExtractOption(options, "not", "false") == "true";

        return negation ? $"NOT EXISTS ({subquery})" : $"EXISTS ({subquery})";
    }

    /// <summary>处理SUBQUERY占位符 - 子查询</summary>
    private static string ProcessSubqueryPlaceholder(string type, string options, SqlDefine dialect)
    {
        var query = ExtractOption(options, "query", "SELECT column FROM table");
        var alias = ExtractOption(options, "alias", "");

        return string.IsNullOrEmpty(alias)
            ? $"({query})"
            : $"({query}) AS {dialect.WrapColumn(alias)}";
    }

    #endregion

    #region 新增实用占位符 - 分页、条件、JSON、窗口函数等

    /// <summary>处理PAGE占位符 - 智能分页（自动计算OFFSET和LIMIT）</summary>
    private static string ProcessPagePlaceholder(string type, IMethodSymbol method, string options, SqlDefine dialect)
    {
        var pageParam = ExtractOption(options, "page", "page");
        var sizeParam = ExtractOption(options, "size", "pageSize");
        var defaultSize = ExtractOption(options, "default", "20");

        // 生成分页逻辑：LIMIT @pageSize OFFSET (@page - 1) * @pageSize
        return dialect.Equals(SqlDefine.SqlServer)
            ? $"OFFSET (({dialect.ParameterPrefix}{pageParam} - 1) * {dialect.ParameterPrefix}{sizeParam}) ROWS FETCH NEXT {dialect.ParameterPrefix}{sizeParam} ROWS ONLY"
            : dialect.Equals(SqlDefine.Oracle)
                ? $"OFFSET (({dialect.ParameterPrefix}{pageParam} - 1) * {dialect.ParameterPrefix}{sizeParam}) ROWS FETCH NEXT {dialect.ParameterPrefix}{sizeParam} ROWS ONLY"
                : $"LIMIT {dialect.ParameterPrefix}{sizeParam} OFFSET (({dialect.ParameterPrefix}{pageParam} - 1) * {dialect.ParameterPrefix}{sizeParam})";
    }

    /// <summary>处理PAGINATION占位符 - 完整的分页信息（包含总数）</summary>
    private static string ProcessPaginationPlaceholder(string type, IMethodSymbol method, string options, SqlDefine dialect)
    {
        // 用于CTE或子查询中的分页
        var pageParam = ExtractOption(options, "page", "page");
        var sizeParam = ExtractOption(options, "size", "pageSize");

        return dialect.Equals(SqlDefine.SqlServer)
            ? $"ROW_NUMBER() OVER (ORDER BY id) AS RowNum"
            : $"LIMIT {dialect.ParameterPrefix}{sizeParam} OFFSET (({dialect.ParameterPrefix}{pageParam} - 1) * {dialect.ParameterPrefix}{sizeParam})";
    }

    /// <summary>处理CASE占位符 - CASE WHEN表达式</summary>
    private static string ProcessCasePlaceholder(string type, string options, SqlDefine dialect)
    {
        // 格式：{{case --when status=1 --then 'Active' --when status=0 --then 'Inactive' --else 'Unknown'}}
        // 简化格式：{{case:status|1=Active,0=Inactive,default=Unknown}}

        var column = ExtractOption(options, "column", type);
        var whenClause = ExtractCommandLineOption(options, "--when");
        var thenClause = ExtractCommandLineOption(options, "--then");
        var elseClause = ExtractCommandLineOption(options, "--else");

        if (!string.IsNullOrEmpty(whenClause))
        {
            // 命令行格式
            return $"CASE WHEN {whenClause} THEN {thenClause} ELSE {elseClause} END";
        }

        // 简化格式处理
        return $"CASE {dialect.WrapColumn(column)} END";
    }

    /// <summary>处理COALESCE占位符 - NULL合并</summary>
    private static string ProcessCoalescePlaceholder(string type, string options, SqlDefine dialect)
    {
        var columns = ExtractOption(options, "columns", type);
        var defaultValue = ExtractOption(options, "default", "NULL");

        var columnList = columns.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var wrappedColumns = string.Join(", ", columnList.Select(c => dialect.WrapColumn(c.Trim())));

        return $"COALESCE({wrappedColumns}, {defaultValue})";
    }

    /// <summary>处理IFNULL占位符 - NULL检查替换</summary>
    private static string ProcessIfNullPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        var defaultValue = ExtractOption(options, "default", "0");

        return dialect.Equals(SqlDefine.SqlServer)
            ? $"ISNULL({dialect.WrapColumn(column)}, {defaultValue})"
            : dialect.Equals(SqlDefine.Oracle)
                ? $"NVL({dialect.WrapColumn(column)}, {defaultValue})"
                : $"IFNULL({dialect.WrapColumn(column)}, {defaultValue})";
    }

    /// <summary>处理CAST占位符 - 类型转换</summary>
    private static string ProcessCastPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        var targetType = ExtractOption(options, "as", "VARCHAR");

        return $"CAST({dialect.WrapColumn(column)} AS {targetType})";
    }

    /// <summary>处理CONVERT占位符 - 类型转换（SQL Server风格）</summary>
    private static string ProcessConvertPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        var targetType = ExtractOption(options, "to", "VARCHAR");
        var style = ExtractOption(options, "style", "");

        if (dialect.Equals(SqlDefine.SqlServer) && !string.IsNullOrEmpty(style))
        {
            return $"CONVERT({targetType}, {dialect.WrapColumn(column)}, {style})";
        }

        return $"CAST({dialect.WrapColumn(column)} AS {targetType})";
    }

    /// <summary>处理JSON_EXTRACT占位符 - 提取JSON字段</summary>
    private static string ProcessJsonExtractPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        var path = ExtractOption(options, "path", "$.value");

        return dialect.Equals(SqlDefine.SqlServer)
            ? $"JSON_VALUE({dialect.WrapColumn(column)}, '{path}')"
            : dialect.Equals(SqlDefine.MySql)
                ? $"JSON_EXTRACT({dialect.WrapColumn(column)}, '{path}')"
                : dialect.Equals(SqlDefine.PostgreSql)
                    ? $"{dialect.WrapColumn(column)}->'{path}'"
                    : $"JSON_EXTRACT({dialect.WrapColumn(column)}, '{path}')";
    }

    /// <summary>处理JSON_ARRAY占位符 - 创建JSON数组</summary>
    private static string ProcessJsonArrayPlaceholder(string type, string options, SqlDefine dialect)
    {
        var values = ExtractOption(options, "values", "value1, value2");

        return dialect.Equals(SqlDefine.SqlServer)
            ? $"JSON_QUERY('[{values}]')"
            : dialect.Equals(SqlDefine.MySql)
                ? $"JSON_ARRAY({values})"
                : dialect.Equals(SqlDefine.PostgreSql)
                    ? $"JSON_BUILD_ARRAY({values})"
                    : $"JSON_ARRAY({values})";
    }

    /// <summary>处理JSON_OBJECT占位符 - 创建JSON对象</summary>
    private static string ProcessJsonObjectPlaceholder(string type, string options, SqlDefine dialect)
    {
        var keys = ExtractOption(options, "keys", "key");
        var values = ExtractOption(options, "values", "value");

        return dialect.Equals(SqlDefine.PostgreSql)
            ? $"JSON_BUILD_OBJECT('{keys}', {values})"
            : $"JSON_OBJECT('{keys}', {values})";
    }

    /// <summary>处理ROW_NUMBER占位符 - 行号窗口函数</summary>
    private static string ProcessRowNumberPlaceholder(string type, string options, SqlDefine dialect)
    {
        var orderBy = ExtractOption(options, "orderby", "id");
        var partitionBy = ExtractOption(options, "partition", "");

        var partitionClause = !string.IsNullOrEmpty(partitionBy) ? $"PARTITION BY {partitionBy} " : "";
        return $"ROW_NUMBER() OVER ({partitionClause}ORDER BY {orderBy})";
    }

    /// <summary>处理RANK占位符 - 排名窗口函数</summary>
    private static string ProcessRankPlaceholder(string type, string options, SqlDefine dialect)
    {
        var orderBy = ExtractOption(options, "orderby", "id");
        var partitionBy = ExtractOption(options, "partition", "");

        var partitionClause = !string.IsNullOrEmpty(partitionBy) ? $"PARTITION BY {partitionBy} " : "";
        return $"RANK() OVER ({partitionClause}ORDER BY {orderBy})";
    }

    /// <summary>处理DENSE_RANK占位符 - 密集排名窗口函数</summary>
    private static string ProcessDenseRankPlaceholder(string type, string options, SqlDefine dialect)
    {
        var orderBy = ExtractOption(options, "orderby", "id");
        var partitionBy = ExtractOption(options, "partition", "");

        var partitionClause = !string.IsNullOrEmpty(partitionBy) ? $"PARTITION BY {partitionBy} " : "";
        return $"DENSE_RANK() OVER ({partitionClause}ORDER BY {orderBy})";
    }

    /// <summary>处理LAG占位符 - 前一行值</summary>
    private static string ProcessLagPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        var offset = ExtractOption(options, "offset", "1");
        var defaultValue = ExtractOption(options, "default", "NULL");
        var orderBy = ExtractOption(options, "orderby", "id");

        return $"LAG({dialect.WrapColumn(column)}, {offset}, {defaultValue}) OVER (ORDER BY {orderBy})";
    }

    /// <summary>处理LEAD占位符 - 后一行值</summary>
    private static string ProcessLeadPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        var offset = ExtractOption(options, "offset", "1");
        var defaultValue = ExtractOption(options, "default", "NULL");
        var orderBy = ExtractOption(options, "orderby", "id");

        return $"LEAD({dialect.WrapColumn(column)}, {offset}, {defaultValue}) OVER (ORDER BY {orderBy})";
    }

    /// <summary>处理SUBSTRING占位符 - 子字符串</summary>
    private static string ProcessSubstringPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        var start = ExtractOption(options, "start", "1");
        var length = ExtractOption(options, "length", "10");

        return dialect.Equals(SqlDefine.SqlServer) || dialect.Equals(SqlDefine.PostgreSql)
            ? $"SUBSTRING({dialect.WrapColumn(column)}, {start}, {length})"
            : $"SUBSTR({dialect.WrapColumn(column)}, {start}, {length})";
    }

    /// <summary>处理CONCAT占位符 - 字符串连接</summary>
    private static string ProcessConcatPlaceholder(string type, string options, SqlDefine dialect)
    {
        var columns = ExtractOption(options, "columns", type);
        var separator = ExtractOption(options, "separator", "");

        var columnList = columns.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (!string.IsNullOrEmpty(separator))
        {
            return dialect.Equals(SqlDefine.SqlServer)
                ? $"CONCAT_WS('{separator}', {string.Join(", ", columnList.Select(c => dialect.WrapColumn(c.Trim())))})"
                : $"CONCAT_WS('{separator}', {string.Join(", ", columnList.Select(c => dialect.WrapColumn(c.Trim())))})";
        }

        return $"CONCAT({string.Join(", ", columnList.Select(c => dialect.WrapColumn(c.Trim())))})";
    }

    /// <summary>处理GROUP_CONCAT占位符 - 分组字符串聚合</summary>
    private static string ProcessGroupConcatPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        var separator = ExtractOption(options, "separator", ",");
        var orderBy = ExtractOption(options, "orderby", "");

        var orderClause = !string.IsNullOrEmpty(orderBy) ? $" ORDER BY {orderBy}" : "";

        return dialect.Equals(SqlDefine.SqlServer)
            ? $"STRING_AGG({dialect.WrapColumn(column)}, '{separator}'){orderClause}"
            : dialect.Equals(SqlDefine.PostgreSql)
                ? $"STRING_AGG({dialect.WrapColumn(column)}, '{separator}'{orderClause})"
                : dialect.Equals(SqlDefine.MySql)
                    ? $"GROUP_CONCAT({dialect.WrapColumn(column)}{orderClause} SEPARATOR '{separator}')"
                    : $"GROUP_CONCAT({dialect.WrapColumn(column)}, '{separator}')";
    }

    /// <summary>处理REPLACE占位符 - 字符串替换</summary>
    private static string ProcessReplacePlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        var oldValue = ExtractOption(options, "old", "oldValue");
        var newValue = ExtractOption(options, "new", "newValue");

        return $"REPLACE({dialect.WrapColumn(column)}, '{oldValue}', '{newValue}')";
    }

    /// <summary>处理LENGTH占位符 - 字符串长度</summary>
    private static string ProcessLengthPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);

        return dialect.Equals(SqlDefine.SqlServer)
            ? $"LEN({dialect.WrapColumn(column)})"
            : dialect.Equals(SqlDefine.Oracle)
                ? $"LENGTH({dialect.WrapColumn(column)})"
                : $"LENGTH({dialect.WrapColumn(column)})";
    }

    /// <summary>处理POWER占位符 - 幂运算</summary>
    private static string ProcessPowerPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        var exponent = ExtractOption(options, "exp", "2");

        return $"POWER({dialect.WrapColumn(column)}, {exponent})";
    }

    /// <summary>处理SQRT占位符 - 平方根</summary>
    private static string ProcessSqrtPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        return $"SQRT({dialect.WrapColumn(column)})";
    }

    /// <summary>处理MOD占位符 - 取模运算</summary>
    private static string ProcessModPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        var divisor = ExtractOption(options, "by", "2");

        return dialect.Equals(SqlDefine.SqlServer)
            ? $"({dialect.WrapColumn(column)} % {divisor})"
            : $"MOD({dialect.WrapColumn(column)}, {divisor})";
    }

    /// <summary>处理BATCH_INSERT占位符 - 批量插入简化</summary>
    private static string ProcessBatchInsertPlaceholder(string type, string tableName, INamedTypeSymbol? entityType, string options, SqlDefine dialect)
    {
        var snakeTableName = SharedCodeGenerationUtilities.ConvertToSnakeCase(tableName);
        var batchSize = ExtractOption(options, "size", "100");

        // 返回批量插入的模板
        return $"INSERT INTO {snakeTableName} ({{{{columns --exclude Id}}}}) VALUES {{{{batch_values}}}}";
    }

    /// <summary>处理BULK_UPDATE占位符 - 批量更新</summary>
    private static string ProcessBulkUpdatePlaceholder(string type, string tableName, string options, SqlDefine dialect)
    {
        var snakeTableName = SharedCodeGenerationUtilities.ConvertToSnakeCase(tableName);
        var keyColumn = ExtractOption(options, "key", "id");

        return dialect.Equals(SqlDefine.SqlServer)
            ? $"UPDATE {snakeTableName} SET {{{{set}}}} FROM {snakeTableName} INNER JOIN @values"
            : $"UPDATE {snakeTableName} SET {{{{set}}}} WHERE {keyColumn} IN ({{{{values}}}})";
    }

    #endregion

    #endregion
}


