// -----------------------------------------------------------------------
// <copyright file="SqlTemplateEngine.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Sqlx.Generator.Core;

/// <summary>
/// SQL template processing engine implementation.
/// This is the core engine that processes SQL templates with placeholders and generates appropriate code.
/// </summary>
public class SqlTemplateEngine : ISqlTemplateEngine
{
    private static readonly Regex ParameterRegex = new(@"[@:$]\w+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex PlaceholderRegex = new(@"\{\{(\w+)(?::(\w+))?(?:\|([^}]+))?\}\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex SqlInjectionRegex = new(@"(?i)(union\s+select|drop\s+table|delete\s+from|insert\s+into|update\s+set|exec\s*\(|execute\s*\(|sp_|xp_|--|\*\/|\/\*)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly HashSet<string> DangerousKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "DROP", "DELETE", "TRUNCATE", "ALTER", "CREATE", "INSERT", "UPDATE", "EXEC", "EXECUTE", "SP_", "XP_"
    };

    // 性能优化：缓存处理结果
    private readonly Dictionary<string, SqlTemplateResult> _templateCache = new();
    private readonly object _cacheLock = new();

    /// <inheritdoc/>
    public SqlTemplateResult ProcessTemplate(string templateSql, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName)
    {
        if (string.IsNullOrWhiteSpace(templateSql))
            return new SqlTemplateResult { ProcessedSql = "SELECT 1", Warnings = { "Empty SQL template provided" } };

        // 生成缓存键
        var cacheKey = GenerateCacheKey(templateSql, method.Name, entityType?.Name, tableName);

        // 尝试从缓存获取
        lock (_cacheLock)
        {
            if (_templateCache.TryGetValue(cacheKey, out var cachedResult))
            {
                cachedResult.Metadata["CacheHit"] = true;
                return cachedResult;
            }
        }

        var result = new SqlTemplateResult();

        // 增强安全验证
        if (!ValidateTemplateSecurity(templateSql, result))
        {
            return result; // 返回包含错误信息的结果
        }

        var processedSql = ProcessPlaceholders(templateSql, method, entityType, tableName, result);
        ProcessParameters(processedSql, method, result);
        result.HasDynamicFeatures = HasDynamicFeatures(processedSql);
        result.ProcessedSql = processedSql;

        // 添加处理元数据
        result.Metadata["ProcessedAt"] = DateTime.UtcNow;
        result.Metadata["TemplateHash"] = templateSql.GetHashCode();
        result.Metadata["CacheHit"] = false;

        // 缓存结果（仅缓存成功的结果）
        if (!result.Errors.Any())
        {
            lock (_cacheLock)
            {
                if (_templateCache.Count > 1000) // 限制缓存大小
                {
                    _templateCache.Clear();
                }
                _templateCache[cacheKey] = result;
            }
        }

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

        // 增强的安全验证
        var tempResult = new SqlTemplateResult();
        ValidateTemplateSecurity(templateSql, tempResult);
        result.Errors.AddRange(tempResult.Errors);
        result.Warnings.AddRange(tempResult.Warnings);

        // 验证SQL语法基本结构
        ValidateBasicSqlStructure(templateSql, result);

        // 验证占位符语法
        ValidatePlaceholderSyntax(templateSql, result);

        // 性能建议
        ProvidePerformanceSuggestions(templateSql, result);

        return result;
    }

    private void ValidateBasicSqlStructure(string templateSql, TemplateValidationResult result)
    {
        var upperSql = templateSql.ToUpperInvariant();

        // 检查SQL语句类型
        var isSelect = upperSql.TrimStart().StartsWith("SELECT");
        var isInsert = upperSql.TrimStart().StartsWith("INSERT");
        var isUpdate = upperSql.TrimStart().StartsWith("UPDATE");
        var isDelete = upperSql.TrimStart().StartsWith("DELETE");

        if (!isSelect && !isInsert && !isUpdate && !isDelete)
        {
            result.Warnings.Add("Template does not start with a recognized SQL statement type");
        }

        // 检查括号匹配
        var openParens = templateSql.Count(c => c == '(');
        var closeParens = templateSql.Count(c => c == ')');
        if (openParens != closeParens)
        {
            result.Errors.Add("Unmatched parentheses in SQL template");
            result.IsValid = false;
        }

        // 检查引号匹配
        var singleQuotes = templateSql.Count(c => c == '\'');
        if (singleQuotes % 2 != 0)
        {
            result.Warnings.Add("Unmatched single quotes in SQL template");
        }
    }

    private void ValidatePlaceholderSyntax(string templateSql, TemplateValidationResult result)
    {
        var placeholders = PlaceholderRegex.Matches(templateSql);
        foreach (Match placeholder in placeholders)
        {
            var placeholderName = placeholder.Groups[1].Value;
            var placeholderType = placeholder.Groups[2].Value;

            if (!IsValidPlaceholder(placeholderName, placeholderType))
            {
                result.Errors.Add($"Invalid placeholder: {placeholder.Value}");
                result.IsValid = false;
            }

            // 验证占位符选项
            if (placeholder.Groups.Count > 3)
            {
                var options = placeholder.Groups[3].Value;
                ValidatePlaceholderOptions(placeholderName, options, result);
            }
        }
    }

    private void ValidatePlaceholderOptions(string placeholderName, string options, TemplateValidationResult result)
    {
        if (string.IsNullOrEmpty(options)) return;

        var pairs = options.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var keyValue = pair.Split(new char[] { '=' }, 2);
            if (keyValue.Length != 2)
            {
                result.Warnings.Add($"Invalid option format in placeholder {placeholderName}: {pair}");
            }
        }
    }

    private void ProvidePerformanceSuggestions(string templateSql, TemplateValidationResult result)
    {
        var upperSql = templateSql.ToUpperInvariant();

        // 检查SELECT *
        if (upperSql.Contains("SELECT *"))
        {
            result.Suggestions.Add("Consider specifying explicit column names instead of SELECT *");
        }

        // 检查缺少WHERE子句的UPDATE/DELETE
        if ((upperSql.Contains("UPDATE ") || upperSql.Contains("DELETE ")) && !upperSql.Contains("WHERE"))
        {
            result.Warnings.Add("UPDATE/DELETE statements without WHERE clause may affect all rows");
        }

        // 检查可能的笛卡尔积
        var joinCount = System.Text.RegularExpressions.Regex.Matches(upperSql, @"\bJOIN\b").Count;
        var whereCount = System.Text.RegularExpressions.Regex.Matches(upperSql, @"\bWHERE\b").Count;
        if (joinCount > 0 && whereCount == 0)
        {
            result.Suggestions.Add("Consider adding WHERE clause to prevent Cartesian products in JOINs");
        }

        // 检查ORDER BY without LIMIT
        if (upperSql.Contains("ORDER BY") && !upperSql.Contains("TOP") && !upperSql.Contains("LIMIT"))
        {
            result.Suggestions.Add("Consider adding LIMIT/TOP clause with ORDER BY for better performance");
        }
    }

    private string ProcessPlaceholders(string sql, IMethodSymbol method, INamedTypeSymbol? entityType, string tableName, SqlTemplateResult result)
    {
        return PlaceholderRegex.Replace(sql, match =>
        {
            var placeholderName = match.Groups[1].Value.ToLowerInvariant();
            var placeholderType = match.Groups[2].Value.ToLowerInvariant();
            var placeholderOptions = match.Groups[3].Value; // 新增：选项支持

            return placeholderName switch
            {
                "table" => ProcessTablePlaceholder(tableName, placeholderType, entityType, placeholderOptions),
                "columns" => ProcessColumnsPlaceholder(placeholderType, entityType, result, placeholderOptions),
                "values" => ProcessValuesPlaceholder(placeholderType, entityType, method, placeholderOptions),
                "where" => ProcessWherePlaceholder(placeholderType, entityType, method, placeholderOptions),
                "set" => ProcessSetPlaceholder(placeholderType, entityType, method, placeholderOptions),
                "orderby" => ProcessOrderByPlaceholder(placeholderType, entityType, placeholderOptions),
                "limit" => ProcessLimitPlaceholder(placeholderType, method, placeholderOptions),
                "join" => ProcessJoinPlaceholder(placeholderType, entityType, placeholderOptions),
                "groupby" => ProcessGroupByPlaceholder(placeholderType, entityType, placeholderOptions),
                "having" => ProcessHavingPlaceholder(placeholderType, method, placeholderOptions),
                "if" => ProcessConditionalPlaceholder(placeholderType, method, placeholderOptions),
                _ => ProcessCustomPlaceholder(match.Value, placeholderName, placeholderType, placeholderOptions, result)
            };
        });
    }

    private string ProcessTablePlaceholder(string tableName, string type, INamedTypeSymbol? entityType, string options)
    {
        // Convert table name to snake_case
        var snakeTableName = ConvertToSnakeCase(tableName);

        // 处理选项参数
        var schema = ExtractOption(options, "schema", "dbo");
        var alias = ExtractOption(options, "alias", "");

        var result = type switch
        {
            "quoted" => $"[{snakeTableName}]",
            "schema" => $"{schema}.{snakeTableName}",
            "full" => $"[{schema}].[{snakeTableName}]",
            _ => snakeTableName
        };

        return !string.IsNullOrEmpty(alias) ? $"{result} AS {alias}" : result;
    }

    private string ProcessColumnsPlaceholder(string type, INamedTypeSymbol? entityType, SqlTemplateResult result, string options)
    {
        if (entityType == null)
        {
            result.Warnings.Add("Cannot infer columns without entity type");
            return "*";
        }

        var properties = entityType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName && p.GetMethod != null)
            .ToList();

        // 处理选项：排除字段、包含字段、前缀
        var exclude = ExtractOption(options, "exclude", "").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        var include = ExtractOption(options, "include", "").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        var prefix = ExtractOption(options, "prefix", "");

        // 应用包含/排除逻辑
        if (include.Any())
        {
            properties = properties.Where(p => include.Contains(p.Name, StringComparer.OrdinalIgnoreCase)).ToList();
        }
        if (exclude.Any())
        {
            properties = properties.Where(p => !exclude.Contains(p.Name, StringComparer.OrdinalIgnoreCase)).ToList();
        }

        Func<string, string> columnFormatter = type switch
        {
            "auto" => (string name) => ConvertToSnakeCase(name),
            "quoted" => (string name) => $"[{ConvertToSnakeCase(name)}]",
            "prefixed" => (string name) => $"{prefix}.{ConvertToSnakeCase(name)}",
            "aliased" => (string name) => $"{ConvertToSnakeCase(name)} AS {name}",
            _ => (string name) => ConvertToSnakeCase(name)
        };

        return string.Join(", ", properties.Select(p => columnFormatter(p.Name)));
    }

    private string ProcessValuesPlaceholder(string type, INamedTypeSymbol? entityType, IMethodSymbol method, string options)
    {
        if (entityType == null)
        {
            // Use method parameters
            var parameters = method.Parameters.Where(p => p.Type.Name != "CancellationToken").ToList();
            return string.Join(", ", parameters.Select(p => $"@{p.Name}"));
        }

        var properties = entityType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName && p.GetMethod != null && p.Name != "Id")
            .ToList();

        // 处理选项：排除字段
        var exclude = ExtractOption(options, "exclude", "").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        if (exclude.Any())
        {
            properties = properties.Where(p => !exclude.Contains(p.Name, StringComparer.OrdinalIgnoreCase)).ToList();
        }

        return string.Join(", ", properties.Select(p => $"@{p.Name}"));
    }

    private string ProcessWherePlaceholder(string type, INamedTypeSymbol? entityType, IMethodSymbol method, string options)
    {
        var customCondition = ExtractOption(options, "condition", "");
        if (!string.IsNullOrEmpty(customCondition))
        {
            return customCondition;
        }

        return type switch
        {
            "id" => $"{ConvertToSnakeCase("Id")} = @id",
            "auto" => GenerateAutoWhereClause(method),
            "soft" => $"deleted_at IS NULL", // 软删除支持
            _ => "1=1"
        };
    }

    private string ProcessSetPlaceholder(string type, INamedTypeSymbol? entityType, IMethodSymbol method, string options)
    {
        if (entityType == null)
        {
            var parameters = method.Parameters.Where(p =>
                p.Type.Name != "CancellationToken" &&
                !p.Name.Equals("id", StringComparison.OrdinalIgnoreCase)).ToList();
            return string.Join(", ", parameters.Select(p => $"{p.Name} = @{p.Name}"));
        }

        var properties = entityType.GetMembers().OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName && p.GetMethod != null && p.SetMethod != null && p.Name != "Id")
            .ToList();

        // 处理选项：排除字段、包含审计字段
        var exclude = ExtractOption(options, "exclude", "").Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        var includeAudit = ExtractOption(options, "audit", "false").Equals("true", StringComparison.OrdinalIgnoreCase);

        if (exclude.Any())
        {
            properties = properties.Where(p => !exclude.Contains(p.Name, StringComparer.OrdinalIgnoreCase)).ToList();
        }

        var setClauses = properties.Select(p => $"{ConvertToSnakeCase(p.Name)} = @{p.Name}").ToList();

        // 添加审计字段
        if (includeAudit)
        {
            setClauses.Add("updated_at = GETUTCDATE()");
        }

        return string.Join(", ", setClauses);
    }

    private string ProcessOrderByPlaceholder(string type, INamedTypeSymbol? entityType, string options)
    {
        var columns = ExtractOption(options, "columns", "");
        var direction = ExtractOption(options, "dir", "ASC");

        if (!string.IsNullOrEmpty(columns))
        {
            return $"{columns} {direction}";
        }

        return type switch
        {
            "id" => $"Id {direction}",
            "name" => $"Name {direction}",
            "created" => $"created_at {direction}",
            _ => $"Id {direction}"
        };
    }

    private string GenerateAutoWhereClause(IMethodSymbol method)
    {
        var parameters = method.Parameters.Where(p => p.Type.Name != "CancellationToken").ToList();
        if (!parameters.Any())
            return "1=1";

        return string.Join(" AND ", parameters.Select(p => $"{InferColumnName(p.Name)} = @{p.Name}"));
    }

    private string InferColumnName(string parameterName)
    {
        // Convert to snake_case for database column names
        return ConvertToSnakeCase(parameterName);
    }

    /// <summary>
    /// Converts C# property names to snake_case database column names.
    /// </summary>
    private string ConvertToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        // If already contains underscores and is all lowercase, return as-is
        if (name.Contains("_") && name.All(c => char.IsLower(c) || c == '_' || char.IsDigit(c)))
        {
            return name;
        }

        // If already contains underscores and is all caps, convert to lowercase
        if (name.Contains("_") && name.All(c => char.IsUpper(c) || c == '_' || char.IsDigit(c)))
        {
            return name.ToLower();
        }

        // Convert PascalCase/camelCase to snake_case
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            char current = name[i];

            if (char.IsUpper(current))
            {
                // Add underscore before uppercase letters (except at the beginning)
                if (i > 0 && !char.IsUpper(name[i - 1]))
                {
                    result.Append('_');
                }
                result.Append(char.ToLower(current));
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
        var parameterMatches = ParameterRegex.Matches(sql);
        var methodParams = method.Parameters.Where(p => p.Type.Name != "CancellationToken").ToList();

        foreach (Match match in parameterMatches)
        {
            var paramName = match.Value.Substring(1); // Remove @ : or $
            var methodParam = methodParams.FirstOrDefault(p =>
                p.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase));

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

    private string InferDbType(ITypeSymbol type)
    {
        return type.SpecialType switch
        {
            SpecialType.System_String => "String",
            SpecialType.System_Int32 => "Int32",
            SpecialType.System_Int64 => "Int64",
            SpecialType.System_Boolean => "Boolean",
            SpecialType.System_DateTime => "DateTime",
            SpecialType.System_Decimal => "Decimal",
            SpecialType.System_Double => "Double",
            SpecialType.System_Single => "Single",
            SpecialType.System_Byte => "Byte",
            SpecialType.System_Int16 => "Int16",
            _ => "Object"
        };
    }

    private bool HasDynamicFeatures(string sql)
    {
        // Check for conditional logic, loops, or other dynamic features
        return sql.Contains("IF") || sql.Contains("CASE") || sql.Contains("WHILE") ||
               sql.Contains("{{") || sql.Contains("}}");
    }

    private bool IsValidPlaceholder(string name, string type)
    {
        var validPlaceholders = new[] { "table", "columns", "values", "where", "set", "orderby", "limit", "join", "groupby", "having", "if" };
        return validPlaceholders.Contains(name.ToLowerInvariant());
    }

    // 新增的辅助方法
    private string GenerateCacheKey(string templateSql, string methodName, string? entityTypeName, string tableName)
    {
        return $"{templateSql.GetHashCode()}:{methodName}:{entityTypeName}:{tableName}";
    }

    private bool ValidateTemplateSecurity(string templateSql, SqlTemplateResult result)
    {
        // 增强的SQL注入检测
        if (SqlInjectionRegex.IsMatch(templateSql))
        {
            result.Errors.Add("Template contains potential SQL injection patterns");
            return false;
        }

        // 检查危险关键字
        var upperSql = templateSql.ToUpperInvariant();
        foreach (var keyword in DangerousKeywords)
        {
            if (upperSql.Contains(keyword))
            {
                result.Warnings.Add($"Template contains potentially dangerous keyword: {keyword}");
            }
        }

        // 检查嵌套查询深度
        var nestedLevel = CountNestedQueries(templateSql);
        if (nestedLevel > 3)
        {
            result.Warnings.Add($"Template has deep nesting level ({nestedLevel}), consider simplifying");
        }

        return true;
    }

    private int CountNestedQueries(string sql)
    {
        var level = 0;
        var maxLevel = 0;

        foreach (char c in sql)
        {
            if (c == '(') level++;
            else if (c == ')') level--;
            maxLevel = Math.Max(maxLevel, level);
        }

        return maxLevel;
    }

    private string ExtractOption(string options, string key, string defaultValue)
    {
        if (string.IsNullOrEmpty(options)) return defaultValue;

        var pairs = options.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var keyValue = pair.Split(new char[] { '=' }, 2);
            if (keyValue.Length == 2 && keyValue[0].Trim().Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return keyValue[1].Trim();
            }
        }

        return defaultValue;
    }

    // 新增的占位符处理方法
    private string ProcessLimitPlaceholder(string type, IMethodSymbol method, string options)
    {
        var defaultLimit = ExtractOption(options, "default", "100");
        var maxLimit = ExtractOption(options, "max", "1000");

        return type switch
        {
            "auto" => $"TOP {defaultLimit}",
            "param" => "TOP @limit",
            "mysql" => $"LIMIT {defaultLimit}",
            "postgres" => $"LIMIT {defaultLimit}",
            _ => $"TOP {defaultLimit}"
        };
    }

    private string ProcessJoinPlaceholder(string type, INamedTypeSymbol? entityType, string options)
    {
        var joinTable = ExtractOption(options, "table", "");
        var joinKey = ExtractOption(options, "key", "Id");
        var foreignKey = ExtractOption(options, "fkey", $"{entityType?.Name}Id");

        return type switch
        {
            "inner" => $"INNER JOIN {joinTable} ON {ConvertToSnakeCase(joinKey)} = {ConvertToSnakeCase(foreignKey)}",
            "left" => $"LEFT JOIN {joinTable} ON {ConvertToSnakeCase(joinKey)} = {ConvertToSnakeCase(foreignKey)}",
            "right" => $"RIGHT JOIN {joinTable} ON {ConvertToSnakeCase(joinKey)} = {ConvertToSnakeCase(foreignKey)}",
            _ => $"INNER JOIN {joinTable} ON {ConvertToSnakeCase(joinKey)} = {ConvertToSnakeCase(foreignKey)}"
        };
    }

    private string ProcessGroupByPlaceholder(string type, INamedTypeSymbol? entityType, string options)
    {
        var columns = ExtractOption(options, "columns", "");
        return string.IsNullOrEmpty(columns) ? "Id" : columns;
    }

    private string ProcessHavingPlaceholder(string type, IMethodSymbol method, string options)
    {
        var condition = ExtractOption(options, "condition", "COUNT(*) > 0");
        return condition;
    }

    private string ProcessConditionalPlaceholder(string type, IMethodSymbol method, string options)
    {
        var condition = ExtractOption(options, "condition", "");
        var thenClause = ExtractOption(options, "then", "");
        var elseClause = ExtractOption(options, "else", "");

        return type switch
        {
            "exists" => $"IF EXISTS ({condition}) {thenClause} ELSE {elseClause}",
            "null" => $"IF {condition} IS NULL {thenClause} ELSE {elseClause}",
            "param" => $"IF @{condition} = 1 {thenClause} ELSE {elseClause}",
            _ => $"IF {condition} {thenClause} ELSE {elseClause}"
        };
    }

    private string ProcessCustomPlaceholder(string originalValue, string name, string type, string options, SqlTemplateResult result)
    {
        result.Warnings.Add($"Unknown placeholder: {name}");
        return originalValue; // 保持原始值
    }
}


