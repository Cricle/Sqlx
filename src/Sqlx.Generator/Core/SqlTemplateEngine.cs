// -----------------------------------------------------------------------
// <copyright file="SqlTemplateEngine.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Text;
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
    // 核心正则表达式 - 性能优化版本 (修复ExplicitCapture问题)
    private static readonly Regex ParameterRegex = new(@"[@:$]\w+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex PlaceholderRegex = new(@"\{\{(\w+)(?::(\w+))?(?:\|([^}]+))?\}\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex SqlInjectionRegex = new(@"(?i)(union\s+select|drop\s+table|exec\s*\(|execute\s*\(|sp_|xp_|--|\*\/|\/\*)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // 性能优化：缓存常用字符串
    private static readonly Dictionary<string, string> CommonPlaceholderCache = new(StringComparer.OrdinalIgnoreCase)
    {
        ["*"] = "*",
        ["1=1"] = "1=1",
        ["COUNT(*)"] = "COUNT(*)",
        ["SELECT *"] = "SELECT *",
        ["ORDER BY id ASC"] = "ORDER BY id ASC",
        ["LIMIT 20"] = "LIMIT 20",
        ["TOP 20"] = "TOP 20"
    };

    // 性能优化：通用过滤器
    private static readonly Func<IParameterSymbol, bool> NonSystemParameterFilter = p => p.Type.Name != "CancellationToken";
    private static readonly Func<IPropertySymbol, bool> AccessiblePropertyFilter = p => p.CanBeReferencedByName && p.GetMethod != null;

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
        if (string.IsNullOrWhiteSpace(templateSql))
            return new SqlTemplateResult { ProcessedSql = "SELECT 1", Warnings = { "Empty SQL template provided" } };

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
            HasDynamicFeatures = result.HasDynamicFeatures
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
            var placeholderName = match.Groups[1].Value.ToLowerInvariant();
            var placeholderType = match.Groups[2].Value.ToLowerInvariant();
            var placeholderOptions = match.Groups[3].Value; // 新增：选项支持

            return placeholderName switch
            {
                // 核心7个占位符（多数据库支持）
                "table" => ProcessTablePlaceholder(tableName, placeholderType, entityType, placeholderOptions, dialect),
                "columns" => ProcessColumnsPlaceholder(placeholderType, entityType, result, placeholderOptions, dialect),
                "values" => ProcessValuesPlaceholder(placeholderType, entityType, method, placeholderOptions, dialect),
                "where" => ProcessWherePlaceholder(placeholderType, entityType, method, placeholderOptions, dialect),
                "set" => ProcessSetPlaceholder(placeholderType, entityType, method, placeholderOptions, dialect),
                "orderby" => ProcessOrderByPlaceholder(placeholderType, entityType, placeholderOptions, dialect),
                "limit" => ProcessLimitPlaceholder(placeholderType, method, placeholderOptions, dialect),
                // 常用扩展占位符（多数据库支持）
                "join" => ProcessJoinPlaceholder(placeholderType, entityType, placeholderOptions, dialect),
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
                "not_in" => ProcessNotInPlaceholder(placeholderType, placeholderOptions, dialect),
                "isnull" => ProcessIsNullPlaceholder(placeholderType, placeholderOptions, dialect),
                "notnull" => ProcessNotNullPlaceholder(placeholderType, placeholderOptions, dialect),
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
                "upper" => ProcessUpperPlaceholder(placeholderType, placeholderOptions, dialect),
                "lower" => ProcessLowerPlaceholder(placeholderType, placeholderOptions, dialect),
                "trim" => ProcessTrimPlaceholder(placeholderType, placeholderOptions, dialect),
                // 数学函数
                "round" => ProcessRoundPlaceholder(placeholderType, placeholderOptions, dialect),
                "abs" => ProcessAbsPlaceholder(placeholderType, placeholderOptions, dialect),
                "ceiling" => ProcessCeilingPlaceholder(placeholderType, placeholderOptions, dialect),
                "floor" => ProcessFloorPlaceholder(placeholderType, placeholderOptions, dialect),
                // 批量操作
                "batch_values" => ProcessBatchValuesPlaceholder(placeholderType, placeholderOptions, dialect),
                "upsert" => ProcessUpsertPlaceholder(placeholderType, tableName, placeholderOptions, dialect),
                // 子查询
                "exists" => ProcessExistsPlaceholder(placeholderType, placeholderOptions, dialect),
                "subquery" => ProcessSubqueryPlaceholder(placeholderType, placeholderOptions, dialect),
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
            return CommonPlaceholderCache["*"];
        }

        var properties = GetFilteredProperties(entityType, options, type == "auto" ? "Id" : null);

        // 性能优化：预分配StringBuilder容量
        var capacity = properties.Count * 20; // 估算每个列名约20字符
        var sb = new StringBuilder(capacity);
        var isQuoted = type == "quoted";

        for (int i = 0; i < properties.Count; i++)
        {
            if (i > 0) sb.Append(", ");

            var columnName = SharedCodeGenerationUtilities.ConvertToSnakeCase(properties[i].Name);
            sb.Append(isQuoted ? dialect.WrapColumn(columnName) : columnName);
        }

        return sb.ToString();
    }


    /// <summary>
    /// 处理值占位符 - 多数据库支持 (性能优化版本)
    /// 自动应用正确的数据库参数语法
    /// </summary>
    private string ProcessValuesPlaceholder(string type, INamedTypeSymbol? entityType, IMethodSymbol method, string options, SqlDefine dialect)
    {
        if (entityType == null)
        {
            if (method == null) return string.Empty;

            // 性能优化：预分配容量并使用StringBuilder
            var methodParams = method.Parameters.Where(NonSystemParameterFilter).ToList();
            var capacity = methodParams.Count * 15; // 估算每个参数约15字符
            var sb = new StringBuilder(capacity);

            for (int i = 0; i < methodParams.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(dialect.ParameterPrefix).Append(methodParams[i].Name);
            }

            return sb.ToString();
        }

        var properties = GetFilteredProperties(entityType, options, type == "auto" ? "Id" : null);

        // 性能优化：预分配StringBuilder容量
        var propertiesCapacity = properties.Count * 15; // 估算每个参数约15字符
        var propertiesSb = new StringBuilder(propertiesCapacity);

        for (int i = 0; i < properties.Count; i++)
        {
            if (i > 0) propertiesSb.Append(", ");
            propertiesSb.Append(dialect.ParameterPrefix).Append(properties[i].Name);
        }

        return propertiesSb.ToString();
    }


    /// <summary>
    /// 处理WHERE占位符 - 多数据库支持
    /// </summary>
    private string ProcessWherePlaceholder(string type, INamedTypeSymbol? entityType, IMethodSymbol method, string options, SqlDefine dialect)
    {
        return type switch
        {
            "id" => $"id = {dialect.ParameterPrefix}id",
            "auto" => GenerateAutoWhereClause(method, dialect),
            _ => "1=1"
        };
    }


    /// <summary>处理SET占位符 - 多数据库支持</summary>
    private string ProcessSetPlaceholder(string type, INamedTypeSymbol? entityType, IMethodSymbol method, string options, SqlDefine dialect)
    {
        if (entityType == null)
        {
            if (method == null) return string.Empty;
            var filteredParams = method.Parameters
                .Where(p => NonSystemParameterFilter(p) && !p.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                .Select(p => $"{SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name)} = {dialect.ParameterPrefix}{p.Name}");
            return string.Join(", ", filteredParams);
        }

        var properties = GetFilteredProperties(entityType, options, "Id", requireSetter: true);
        return string.Join(", ", properties.Select(p => $"{SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name)} = {dialect.ParameterPrefix}{p.Name}"));
    }


    // 性能优化：扩展的ORDER BY映射表，支持更多常用排序模式
    private static readonly Dictionary<string, string> OrderByMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "ORDER BY id ASC",
        ["id_desc"] = "ORDER BY id DESC",
        ["name"] = "ORDER BY name ASC",
        ["name_desc"] = "ORDER BY name DESC",
        ["created"] = "ORDER BY created_at DESC",
        ["created_asc"] = "ORDER BY created_at ASC",
        ["updated"] = "ORDER BY updated_at DESC",
        ["updated_asc"] = "ORDER BY updated_at ASC",
        ["date"] = "ORDER BY created_at DESC",
        ["random"] = "ORDER BY NEWID()", // SQL Server
        ["rand"] = "ORDER BY RAND()", // MySQL
        ["priority"] = "ORDER BY priority DESC, created_at DESC"
    };

    /// <summary>处理ORDER BY占位符 - 多数据库支持 (增强版本)</summary>
    private static string ProcessOrderByPlaceholder(string type, INamedTypeSymbol? entityType, string options, SqlDefine dialect)
    {
        // 优先检查缓存的映射
        if (OrderByMap.TryGetValue(type, out var orderBy))
        {
            // 针对随机排序进行数据库特定优化
            if (type == "random")
            {
                return dialect.Equals(SqlDefine.SqlServer) ? "ORDER BY NEWID()" :
                       dialect.Equals(SqlDefine.MySql) ? "ORDER BY RAND()" :
                       dialect.Equals(SqlDefine.PostgreSql) ? "ORDER BY RANDOM()" :
                       dialect.Equals(SqlDefine.SQLite) ? "ORDER BY RANDOM()" :
                       "ORDER BY NEWID()";
            }

            return orderBy;
        }

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

        foreach (Match match in ParameterRegex.Matches(sql))
        {
            var paramName = match.Value.Substring(1); // Remove @ : or $

            if (methodParamDict.TryGetValue(paramName, out var methodParam))
            {
                if (!result.Parameters.ContainsKey(paramName))
                {
                    result.Parameters.Add(paramName, null); // 模板处理阶段只记录参数名
                }
            }
            else
            {
                result.Warnings.Add($"Parameter '{paramName}' not found in method signature");
            }
        }
    }

    private static readonly Dictionary<SpecialType, string> DbTypeMap = new()
    {
        [SpecialType.System_String] = "String",
        [SpecialType.System_Int32] = "Int32",
        [SpecialType.System_Int64] = "Int64",
        [SpecialType.System_Boolean] = "Boolean",
        [SpecialType.System_DateTime] = "DateTime",
        [SpecialType.System_Decimal] = "Decimal",
        [SpecialType.System_Double] = "Double",
        [SpecialType.System_Single] = "Single",
        [SpecialType.System_Byte] = "Byte",
        [SpecialType.System_Int16] = "Int16"
    };

    private static string InferDbType(ITypeSymbol type) => DbTypeMap.TryGetValue(type.SpecialType, out var dbType) ? dbType : "Object";

    private static bool HasDynamicFeatures(string sql) =>
        sql.Contains("IF") || sql.Contains("CASE") || sql.Contains("WHILE") || sql.Contains("{{");

    private static readonly HashSet<string> ValidPlaceholders = new(StringComparer.OrdinalIgnoreCase)
    {
        // 核心7个占位符（保持不变）
        "table", "columns", "values", "where", "set", "orderby", "limit",
        // 常用扩展占位符
        "join", "groupby", "having", "select", "insert", "update", "delete",
        "count", "sum", "avg", "max", "min", "distinct", "union", "top", "offset"
    };

    private static bool IsValidPlaceholder(string name, string type) => ValidPlaceholders.Contains(name);


    /// <summary>统一的属性过滤逻辑，减少重复代码</summary>
    private List<IPropertySymbol> GetFilteredProperties(INamedTypeSymbol entityType, string options, string? excludeProperty = null, bool requireSetter = false)
    {
        // 预构建排除集合，提高查找性能
        var excludeSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (excludeProperty != null) excludeSet.Add(excludeProperty);

        var excludeOption = ExtractOption(options, "exclude", "");
        if (!string.IsNullOrEmpty(excludeOption))
        {
            foreach (var item in excludeOption.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                excludeSet.Add(item.Trim());
        }

        // 单次遍历过滤，避免多次枚举
        // 性能优化：预估容量减少重分配
        var result = new List<IPropertySymbol>(16); // 大多数实体不会超过16个属性
        foreach (var member in entityType.GetMembers())
        {
            if (member is IPropertySymbol property &&
                AccessiblePropertyFilter(property) &&
                (!requireSetter || property.SetMethod != null) &&
                !excludeSet.Contains(property.Name))
            {
                result.Add(property);
            }
        }

        return result;
    }

    /// <summary>
    /// 增强安全验证 - 基于数据库方言的安全检查
    /// 针对不同数据库的特定安全威胁进行检测
    /// </summary>
    private bool ValidateTemplateSecurity(string templateSql, SqlTemplateResult result, SqlDefine dialect)
    {
        // 基础SQL注入检测
        if (SqlInjectionRegex.IsMatch(templateSql))
        {
            result.Errors.Add("Template contains potential SQL injection patterns");
            return false;
        }

        // 数据库特定安全检查
        ValidateDialectSpecificSecurity(templateSql, result, dialect);

        // 参数安全检查 - 确保使用正确的参数前缀
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





    #region 辅助生成方法

    /// <summary>从方法参数生成GROUP BY子句</summary>
    private static string GenerateGroupByFromMethod(IMethodSymbol method)
    {
        if (method == null) return "GROUP BY id";
        var parameters = method.Parameters.Where(NonSystemParameterFilter).ToList();
        // 性能优化：使用Count检查集合是否为空，比Any()更直接
        if (parameters.Count == 0) return "GROUP BY id";

        return $"GROUP BY {string.Join(", ", parameters.Select(p => SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name)))}";
    }

    /// <summary>从方法参数生成HAVING子句</summary>
    private static string GenerateHavingFromMethod(IMethodSymbol method)
    {
        if (method == null) return "HAVING COUNT(*) > 0";
        var parameters = method.Parameters.Where(NonSystemParameterFilter).ToList();
        // 性能优化：使用Count检查集合是否为空，比Any()更直接
        if (parameters.Count == 0) return "HAVING COUNT(*) > 0";

        // 为聚合查询生成HAVING条件
        var conditions = parameters.Select(p =>
        {
            var columnName = SharedCodeGenerationUtilities.ConvertToSnakeCase(p.Name);
            return p.Type.SpecialType == SpecialType.System_Int32 || p.Type.SpecialType == SpecialType.System_Int64
                ? $"COUNT({columnName}) > @{p.Name}"
                : $"{columnName} = @{p.Name}";
        });

        return $"HAVING {string.Join(" AND ", conditions)}";
    }

    #endregion

    private static string ProcessCustomPlaceholder(string originalValue, string name, string type, string options, SqlTemplateResult result)
    {
        result.Warnings.Add($"Unknown placeholder '{name}'. Available: table, columns, values, where, set, orderby, limit, join, groupby, having, select, insert, update, delete, count, sum, avg, max, min, distinct, union, top, offset");
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

    /// <summary>处理JOIN占位符 - 多数据库支持</summary>
    private static string ProcessJoinPlaceholder(string type, INamedTypeSymbol? entityType, string options, SqlDefine dialect)
    {
        return SqlTemplateEngineExtensions.MultiDatabasePlaceholderSupport.ProcessGenericPlaceholder("join", type, options, dialect);
    }

    /// <summary>处理GROUP BY占位符 - 多数据库支持</summary>
    private static string ProcessGroupByPlaceholder(string type, INamedTypeSymbol? entityType, IMethodSymbol method, string options, SqlDefine dialect)
    {
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

    /// <summary>处理INSERT占位符 - 多数据库支持</summary>
    private static string ProcessInsertPlaceholder(string type, string tableName, string options, SqlDefine dialect)
    {
        var snakeTableName = SharedCodeGenerationUtilities.ConvertToSnakeCase(tableName);
        return type == "into" ? $"INSERT INTO {snakeTableName}" : $"INSERT INTO {snakeTableName}";
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

        return $"{dialect.WrapColumn(column)} BETWEEN {dialect.ParameterPrefix}{min} AND {dialect.ParameterPrefix}{max}";
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

    /// <summary>处理IN占位符 - IN操作</summary>
    private static string ProcessInPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        var values = ExtractOption(options, "values", "values");

        return $"{dialect.WrapColumn(column)} IN ({dialect.ParameterPrefix}{values})";
    }

    /// <summary>处理NOT IN占位符</summary>
    private static string ProcessNotInPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        var values = ExtractOption(options, "values", "values");

        return $"{dialect.WrapColumn(column)} NOT IN ({dialect.ParameterPrefix}{values})";
    }

    /// <summary>处理IS NULL占位符</summary>
    private static string ProcessIsNullPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        return $"{dialect.WrapColumn(column)} IS NULL";
    }

    /// <summary>处理IS NOT NULL占位符</summary>
    private static string ProcessNotNullPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        return $"{dialect.WrapColumn(column)} IS NOT NULL";
    }

    #endregion

    #region 日期时间函数占位符

    /// <summary>处理TODAY占位符 - 当前日期</summary>
    private static string ProcessTodayPlaceholder(string type, string options, SqlDefine dialect)
    {
        var format = ExtractOption(options, "format", "date"); // date, datetime, timestamp

        return format switch
        {
            "datetime" => GetCurrentDateTimeFunction(dialect),
            "timestamp" => GetCurrentTimestampFunction(dialect),
            _ => GetCurrentDateFunction(dialect)
        };
    }

    /// <summary>处理WEEK占位符 - 周相关函数</summary>
    private static string ProcessWeekPlaceholder(string type, string options, SqlDefine dialect)
    {
        var operation = ExtractOption(options, "op", type); // start, end, number

        return operation switch
        {
            "start" => GetWeekStartFunction(dialect),
            "end" => GetWeekEndFunction(dialect),
            "number" => GetWeekNumberFunction(dialect),
            _ => GetWeekNumberFunction(dialect)
        };
    }

    /// <summary>处理MONTH占位符 - 月份相关函数</summary>
    private static string ProcessMonthPlaceholder(string type, string options, SqlDefine dialect)
    {
        var operation = ExtractOption(options, "op", type); // start, end, name, number

        return operation switch
        {
            "start" => GetMonthStartFunction(dialect),
            "end" => GetMonthEndFunction(dialect),
            "name" => GetMonthNameFunction(dialect),
            "number" => GetMonthNumberFunction(dialect),
            _ => GetMonthNumberFunction(dialect)
        };
    }

    /// <summary>处理YEAR占位符 - 年份函数</summary>
    private static string ProcessYearPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", "created_at");
        return $"YEAR({dialect.WrapColumn(column)})";
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
        var value = ExtractOption(options, "value", "searchValue");

        return dialect.Equals(SqlDefine.SqlServer)
            ? $"CHARINDEX({dialect.ParameterPrefix}{value}, {dialect.WrapColumn(column)}) > 0"
            : $"{dialect.WrapColumn(column)} LIKE CONCAT('%', {dialect.ParameterPrefix}{value}, '%')";
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

    /// <summary>处理UPPER占位符 - 大写转换</summary>
    private static string ProcessUpperPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        return $"UPPER({dialect.WrapColumn(column)})";
    }

    /// <summary>处理LOWER占位符 - 小写转换</summary>
    private static string ProcessLowerPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        return $"LOWER({dialect.WrapColumn(column)})";
    }

    /// <summary>处理TRIM占位符 - 去除空白</summary>
    private static string ProcessTrimPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        var mode = ExtractOption(options, "mode", "both"); // both, leading, trailing

        return mode switch
        {
            "leading" => $"LTRIM({dialect.WrapColumn(column)})",
            "trailing" => $"RTRIM({dialect.WrapColumn(column)})",
            _ => $"TRIM({dialect.WrapColumn(column)})"
        };
    }

    #endregion

    #region 数学函数占位符

    /// <summary>处理ROUND占位符 - 四舍五入</summary>
    private static string ProcessRoundPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        var precision = ExtractOption(options, "precision", "2");

        return $"ROUND({dialect.WrapColumn(column)}, {precision})";
    }

    /// <summary>处理ABS占位符 - 绝对值</summary>
    private static string ProcessAbsPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        return $"ABS({dialect.WrapColumn(column)})";
    }

    /// <summary>处理CEILING占位符 - 向上取整</summary>
    private static string ProcessCeilingPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        return dialect.Equals(SqlDefine.SqlServer)
            ? $"CEILING({dialect.WrapColumn(column)})"
            : $"CEIL({dialect.WrapColumn(column)})";
    }

    /// <summary>处理FLOOR占位符 - 向下取整</summary>
    private static string ProcessFloorPlaceholder(string type, string options, SqlDefine dialect)
    {
        var column = ExtractOption(options, "column", type);
        return $"FLOOR({dialect.WrapColumn(column)})";
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

    #endregion
}


