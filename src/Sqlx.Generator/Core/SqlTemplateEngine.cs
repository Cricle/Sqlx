// -----------------------------------------------------------------------
// <copyright file="SqlTemplateEngine.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Sqlx.Generator.Core;

/// <summary>
/// SQL template processing engine implementation - 写一次、安全、高效、友好、多库可使用
/// 核心特性：
/// - 写一次(Write Once): 同一模板支持多种数据库
/// - 安全(Safety): 全面的SQL注入防护和参数验证
/// - 高效(Efficiency): 智能缓存和编译时优化
/// - 友好(User-friendly): 清晰的错误提示和智能建议
/// - 多库可使用(Multi-database): 通过SqlDefine支持所有主流数据库
/// </summary>
public class SqlTemplateEngine : ISqlTemplateEngine
{
    // 核心正则表达式 - 精简保留
    private static readonly Regex ParameterRegex = new(@"[@:$]\w+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex PlaceholderRegex = new(@"\{\{(\w+)(?::(\w+))?(?:\|([^}]+))?\}\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex SqlInjectionRegex = new(@"(?i)(union\s+select|drop\s+table|delete\s+from|insert\s+into|update\s+set|exec\s*\(|execute\s*\(|sp_|xp_|--|\*\/|\/\*)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

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
        result.ProcessedSql = processedSql;

        // 缓存功能已移至扩展类处理

        return result;
    }

    /// <inheritdoc/>
    public TemplateValidationResult ValidateTemplate(string templateSql)
    {
        var result = new TemplateValidationResult { IsValid = true };

        if (string.IsNullOrWhiteSpace(templateSql))
        {
            result.IsValid = false;
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
        var snakeTableName = ConvertToSnakeCase(tableName);
        return type == "quoted" ? dialect.WrapColumn(snakeTableName) : snakeTableName;
    }


    /// <summary>
    /// 处理列占位符 - 多数据库支持
    /// 自动应用正确的数据库列引用语法
    /// </summary>
    private string ProcessColumnsPlaceholder(string type, INamedTypeSymbol? entityType, SqlTemplateResult result, string options, SqlDefine dialect)
    {
        if (entityType == null)
        {
            result.Warnings.Add("Cannot infer columns without entity type");
            return "*";
        }

        var properties = GetFilteredProperties(entityType, options, null);
        return type == "quoted"
            ? string.Join(", ", properties.Select(p => dialect.WrapColumn(ConvertToSnakeCase(p.Name))))
            : string.Join(", ", properties.Select(p => ConvertToSnakeCase(p.Name)));
    }


    /// <summary>
    /// 处理值占位符 - 多数据库支持
    /// 自动应用正确的数据库参数语法
    /// </summary>
    private string ProcessValuesPlaceholder(string type, INamedTypeSymbol? entityType, IMethodSymbol method, string options, SqlDefine dialect)
    {
        if (entityType == null)
        {
            return method != null
                ? string.Join(", ", method.Parameters.Where(NonSystemParameterFilter).Select(p => $"{dialect.ParameterPrefix}{p.Name}"))
                : string.Empty;
        }

        var properties = GetFilteredProperties(entityType, options, "Id");
        return string.Join(", ", properties.Select(p => $"{dialect.ParameterPrefix}{p.Name}"));
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
                .Select(p => $"{ConvertToSnakeCase(p.Name)} = {dialect.ParameterPrefix}{p.Name}");
            return string.Join(", ", filteredParams);
        }

        var properties = GetFilteredProperties(entityType, options, "Id", requireSetter: true);
        return string.Join(", ", properties.Select(p => $"{ConvertToSnakeCase(p.Name)} = {dialect.ParameterPrefix}{p.Name}"));
    }


    private static readonly Dictionary<string, string> OrderByMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["id"] = "ORDER BY id ASC",
        ["name"] = "ORDER BY name ASC",
        ["created"] = "ORDER BY created_at DESC"
    };

    /// <summary>处理ORDER BY占位符 - 多数据库支持</summary>
    private static string ProcessOrderByPlaceholder(string type, INamedTypeSymbol? entityType, string options, SqlDefine dialect) =>
        OrderByMap.TryGetValue(type, out var orderBy) ? orderBy : $"ORDER BY {dialect.WrapColumn("id")} ASC";


    /// <summary>生成自动WHERE子句 - 多数据库支持</summary>
    private string GenerateAutoWhereClause(IMethodSymbol method, SqlDefine dialect) =>
        method?.Parameters.Any(NonSystemParameterFilter) == true
            ? string.Join(" AND ", method.Parameters.Where(NonSystemParameterFilter).Select(p => $"{ConvertToSnakeCase(p.Name)} = {dialect.ParameterPrefix}{p.Name}"))
            : "1=1";


    /// <summary>Converts C# property names to snake_case database column names.</summary>
    private static string ConvertToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        if (name.Contains("_")) return name.ToLowerInvariant();

        var result = new System.Text.StringBuilder(name.Length + 5);
        for (int i = 0; i < name.Length; i++)
        {
            char current = name[i];
            if (char.IsUpper(current))
            {
                if (i > 0 && !char.IsUpper(name[i - 1])) result.Append('_');
                result.Append(char.ToLowerInvariant(current));
            }
            else
            {
                result.Append(current);
            }
        }
        return result.ToString();
    }

    private void ProcessParameters(string sql, IMethodSymbol method, SqlTemplateResult result)
    {
        if (method == null) return; // 防护性检查
        var methodParams = method.Parameters.Where(NonSystemParameterFilter).ToList();

        foreach (Match match in ParameterRegex.Matches(sql))
        {
            var paramName = match.Value.Substring(1); // Remove @ : or $
            var methodParam = methodParams.FirstOrDefault(p => p.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase));

            if (methodParam != null)
            {
                result.Parameters.Add(new ParameterMapping
                {
                    Name = paramName,
                    Type = methodParam.Type.ToDisplayString(),
                    IsNullable = methodParam.Type.CanBeReferencedByName && methodParam.NullableAnnotation == NullableAnnotation.Annotated,
                    DbType = InferDbType(methodParam.Type)
                });
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
        var properties = entityType.GetMembers().OfType<IPropertySymbol>()
            .Where(AccessiblePropertyFilter)
            .Where(p => requireSetter ? p.SetMethod != null : true)
            .Where(p => excludeProperty == null || p.Name != excludeProperty);

        // 处理exclude选项
        var exclude = ExtractOption(options, "exclude", "");
        if (!string.IsNullOrEmpty(exclude))
        {
            var excludeList = exclude.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            properties = properties.Where(p => !excludeList.Contains(p.Name, StringComparer.OrdinalIgnoreCase));
        }

        return properties.ToList();
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

        return !result.Errors.Any();
    }


    /// <summary>
    /// 数据库特定安全验证
    /// </summary>
    private void ValidateDialectSpecificSecurity(string templateSql, SqlTemplateResult result, SqlDefine dialect)
    {
        var upper = templateSql.ToUpperInvariant();

        // PostgreSQL特定检查
        if (dialect.Equals(SqlDefine.PostgreSql))
        {
            if (upper.Contains("$$") && !upper.Contains("$BODY$"))
                result.Warnings.Add("PostgreSQL dollar-quoted strings detected, ensure they are safe");
        }

        // MySQL特定检查
        if (dialect.Equals(SqlDefine.MySql))
        {
            if (upper.Contains("LOAD_FILE") || upper.Contains("INTO OUTFILE"))
                result.Errors.Add("MySQL file operations detected, potential security risk");
        }

        // SQL Server特定检查
        if (dialect.Equals(SqlDefine.SqlServer))
        {
            if (upper.Contains("OPENROWSET") || upper.Contains("OPENDATASOURCE"))
                result.Errors.Add("SQL Server external data access detected, potential security risk");
        }
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

    /// <summary>
    /// 获取数据库方言名称 - 用于用户友好的错误提示
    /// </summary>
    private static string GetDialectName(SqlDefine dialect)
    {
        if (dialect.Equals(SqlDefine.MySql)) return "MySQL";
        if (dialect.Equals(SqlDefine.SqlServer)) return "SQL Server";
        if (dialect.Equals(SqlDefine.PostgreSql)) return "PostgreSQL";
        if (dialect.Equals(SqlDefine.SQLite)) return "SQLite";
        if (dialect.Equals(SqlDefine.Oracle)) return "Oracle";
        if (dialect.Equals(SqlDefine.DB2)) return "DB2";
        return "Unknown";
    }

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

    private static string ProcessLimitPlaceholder(string type, IMethodSymbol method, string options)
    {
        var defaultLimit = ExtractOption(options, "default", "100");
        return type == "sqlserver" ? $"TOP {defaultLimit}" : $"LIMIT {defaultLimit}";
    }




    #region 辅助生成方法

    /// <summary>从方法参数生成GROUP BY子句</summary>
    private static string GenerateGroupByFromMethod(IMethodSymbol method)
    {
        if (method == null) return "GROUP BY id";
        var parameters = method.Parameters.Where(NonSystemParameterFilter).ToList();
        if (!parameters.Any()) return "GROUP BY id";

        return $"GROUP BY {string.Join(", ", parameters.Select(p => ConvertToSnakeCase(p.Name)))}";
    }

    /// <summary>从方法参数生成HAVING子句</summary>
    private static string GenerateHavingFromMethod(IMethodSymbol method)
    {
        if (method == null) return "HAVING COUNT(*) > 0";
        var parameters = method.Parameters.Where(NonSystemParameterFilter).ToList();
        if (!parameters.Any()) return "HAVING COUNT(*) > 0";

        // 为聚合查询生成HAVING条件
        var conditions = parameters.Select(p =>
        {
            var columnName = ConvertToSnakeCase(p.Name);
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
        var snakeTableName = ConvertToSnakeCase(tableName);
        return type == "into" ? $"INSERT INTO {snakeTableName}" : $"INSERT INTO {snakeTableName}";
    }

    /// <summary>处理UPDATE占位符 - 多数据库支持</summary>
    private static string ProcessUpdatePlaceholder(string type, string tableName, string options, SqlDefine dialect)
    {
        var snakeTableName = ConvertToSnakeCase(tableName);
        return $"UPDATE {snakeTableName}";
    }

    /// <summary>处理DELETE占位符 - 多数据库支持</summary>
    private static string ProcessDeletePlaceholder(string type, string tableName, string options, SqlDefine dialect)
    {
        var snakeTableName = ConvertToSnakeCase(tableName);
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

    #endregion
}


