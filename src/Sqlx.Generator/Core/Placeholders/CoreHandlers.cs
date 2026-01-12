// -----------------------------------------------------------------------
// <copyright file="CoreHandlers.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sqlx.Generator.Placeholders;

/// <summary>
/// 占位符处理器基类 - 提供通用功能
/// </summary>
public abstract class PlaceholderHandlerBase : IPlaceholderHandler
{
    public abstract string Name { get; }
    public abstract string Process(PlaceholderContext context);

    /// <summary>解析选项</summary>
    protected PlaceholderOptions ParseOptions(PlaceholderContext context) =>
        new(context.Options);

    /// <summary>获取过滤后的属性列表</summary>
    protected List<IPropertySymbol> GetFilteredProperties(
        INamedTypeSymbol entityType,
        PlaceholderOptions options,
        string? excludeProperty = null,
        bool requireSetter = false)
    {
        var excludeSet = options.GetExcludeSet();
        if (excludeProperty != null) excludeSet.Add(excludeProperty);

        var onlySet = options.GetOnlySet();

        return entityType.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.CanBeReferencedByName &&
                       p.GetMethod != null &&
                       p.Name != "EqualityContract" &&
                       !p.IsImplicitlyDeclared &&
                       (!requireSetter || p.SetMethod != null) &&
                       !excludeSet.Contains(p.Name) &&
                       (onlySet.Count == 0 || onlySet.Contains(p.Name)))
            .ToList();
    }

    /// <summary>转换为snake_case</summary>
    protected static string ToSnakeCase(string name) =>
        SharedCodeGenerationUtilities.ConvertToSnakeCase(name);
}

/// <summary>{{table}} - 表名占位符</summary>
public sealed class TableHandler : PlaceholderHandlerBase
{
    public override string Name => "table";

    public override string Process(PlaceholderContext context)
    {
        var options = ParseOptions(context);
        
        // 优先级: options > type > tableName
        var tableName = !string.IsNullOrEmpty(options.FirstArg) ? options.FirstArg :
                       !string.IsNullOrEmpty(context.Type) && context.Type != "quoted" ? context.Type :
                       context.TableName;

        var snakeTableName = ToSnakeCase(tableName);
        return context.Type == "quoted" ? context.Dialect.WrapColumn(snakeTableName) : snakeTableName;
    }
}

/// <summary>{{columns}} - 列名占位符</summary>
public sealed class ColumnsHandler : PlaceholderHandlerBase
{
    public override string Name => "columns";

    public override string Process(PlaceholderContext context)
    {
        if (context.EntityType == null)
        {
            context.Result.Warnings.Add("Cannot infer columns without entity type");
            return "*";
        }

        var options = ParseOptions(context);
        var properties = GetFilteredProperties(context.EntityType, options,
            context.Type == "auto" ? "Id" : null);

        // 支持 --regex 过滤
        var regexPattern = options.Get("regex");
        if (!string.IsNullOrEmpty(regexPattern))
        {
            try
            {
                var regex = new System.Text.RegularExpressions.Regex(regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                properties = properties.Where(p => regex.IsMatch(p.Name)).ToList();
            }
            catch
            {
                context.Result.Warnings.Add($"Invalid regex pattern: {regexPattern}");
            }
        }

        var shouldQuote = context.Type != "raw";
        var sb = new StringBuilder();

        context.Result.ColumnOrder.Clear();

        for (int i = 0; i < properties.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            var columnName = ToSnakeCase(properties[i].Name);
            sb.Append(shouldQuote ? context.Dialect.WrapColumn(columnName) : columnName);
            context.Result.ColumnOrder.Add(columnName);
        }

        return sb.ToString();
    }
}

/// <summary>{{values}} - 参数值占位符</summary>
public sealed class ValuesHandler : PlaceholderHandlerBase
{
    public override string Name => "values";

    public override string Process(PlaceholderContext context)
    {
        var options = ParseOptions(context);
        var paramName = options.Get("param");

        // 检查 --param 选项用于 IN 子句
        if (!string.IsNullOrEmpty(paramName) && context.Method != null)
        {
            var param = context.Method.Parameters.FirstOrDefault(p => p.Name == paramName);
            if (param != null && SharedCodeGenerationUtilities.IsEnumerableParameter(param))
            {
                return $"({context.Dialect.ParameterPrefix}{paramName})";
            }
        }

        // 批量操作格式: {{values @paramName}}
        if (options.FirstArg.StartsWith("@") && context.Method != null)
        {
            var batchParamName = options.FirstArg.Substring(1);
            var param = context.Method.Parameters.FirstOrDefault(p => p.Name == batchParamName);
            if (param != null && SharedCodeGenerationUtilities.IsEnumerableParameter(param))
            {
                return $"__RUNTIME_BATCH_VALUES_{batchParamName}__";
            }
        }

        // 从实体类型生成
        if (context.EntityType == null)
        {
            if (context.Method == null) return string.Empty;
            return string.Join(", ", context.Method.Parameters
                .Where(p => p.Type.Name != "CancellationToken")
                .Select(p => $"{context.Dialect.ParameterPrefix}{p.Name}"));
        }

        var properties = GetFilteredProperties(context.EntityType, options,
            context.Type == "auto" ? "Id" : null);

        return string.Join(", ", properties.Select(p =>
            $"{context.Dialect.ParameterPrefix}{ToSnakeCase(p.Name)}"));
    }
}

/// <summary>{{set}} - UPDATE SET子句占位符</summary>
public sealed class SetHandler : PlaceholderHandlerBase
{
    public override string Name => "set";

    public override string Process(PlaceholderContext context)
    {
        var options = ParseOptions(context);

        // 动态SET参数
        if (context.Type?.StartsWith("@") == true && context.Method != null)
        {
            var paramName = context.Type.Substring(1);
            if (context.Method.Parameters.Any(p => p.Name == paramName))
            {
                return $"{{RUNTIME_SET_{paramName}}}";
            }
        }

        if (context.EntityType == null)
        {
            if (context.Method == null) return string.Empty;
            return string.Join(", ", context.Method.Parameters
                .Where(p => p.Type.Name != "CancellationToken" &&
                           !p.Name.Equals("id", StringComparison.OrdinalIgnoreCase))
                .Select(p => $"{context.Dialect.WrapColumn(ToSnakeCase(p.Name))} = {context.Dialect.ParameterPrefix}{p.Name}"));
        }

        var properties = GetFilteredProperties(context.EntityType, options, "Id", requireSetter: true);

        return string.Join(", ", properties.Select(p =>
        {
            var columnName = ToSnakeCase(p.Name);
            return $"{context.Dialect.WrapColumn(columnName)} = {context.Dialect.ParameterPrefix}{columnName}";
        }));
    }
}

/// <summary>{{where}} - WHERE子句占位符</summary>
public sealed class WhereHandler : PlaceholderHandlerBase
{
    public override string Name => "where";

    public override string Process(PlaceholderContext context)
    {
        var options = ParseOptions(context);
        var paramName = options.Get("param");

        // 支持 {{where --param predicate}} 语法
        if (!string.IsNullOrEmpty(paramName) && context.Method != null)
        {
            var param = context.Method.Parameters.FirstOrDefault(p => p.Name == paramName);
            if (param != null)
            {
                // 检查是否是 Expression<Func<T, bool>>
                if (IsExpressionParameter(param))
                {
                    return $"{{{{RUNTIME_WHERE_NATIVE_EXPR_{paramName}}}}}";
                }
                return $"{{{{RUNTIME_WHERE_EXPR_{paramName}}}}}";
            }
        }

        // 支持 {{where @paramName}} 语法
        var paramSource = context.Type?.StartsWith("@") == true ? context.Type :
                         options.FirstArg.StartsWith("@") ? options.FirstArg : null;

        if (paramSource != null && context.Method != null)
        {
            var pName = paramSource.Substring(1).Trim();
            var param = context.Method.Parameters.FirstOrDefault(p => p.Name == pName);
            if (param != null)
            {
                if (IsExpressionParameter(param))
                {
                    return $"{{{{RUNTIME_WHERE_NATIVE_EXPR_{pName}}}}}";
                }
                return $"{{{{RUNTIME_WHERE_{pName}}}}}";
            }
        }

        // 自动检测 ExpressionToSql 参数（向后兼容）
        if (context.Method != null && string.IsNullOrEmpty(paramName) && paramSource == null)
        {
            var expressionParam = context.Method.Parameters.FirstOrDefault(p => 
                HasExpressionToSqlAttribute(p) || IsExpressionParameter(p));
            if (expressionParam != null)
            {
                return $"{{{{RUNTIME_WHERE_EXPR_{expressionParam.Name}}}}}";
            }
        }

        // 默认WHERE生成
        return context.Type switch
        {
            "id" => $"{context.Dialect.WrapColumn("id")} = {context.Dialect.ParameterPrefix}id",
            "auto" => GenerateAutoWhere(context),
            _ => "1=1"
        };
    }

    private static bool IsExpressionParameter(IParameterSymbol param)
    {
        var typeName = param.Type.ToDisplayString();
        return typeName.Contains("Expression<Func<") && typeName.Contains(", bool>>");
    }

    private static bool HasExpressionToSqlAttribute(IParameterSymbol param)
    {
        return param.GetAttributes().Any(a => 
            a.AttributeClass?.Name == "ExpressionToSqlAttribute" ||
            a.AttributeClass?.Name == "ExpressionToSql");
    }

    private string GenerateAutoWhere(PlaceholderContext context)
    {
        if (context.Method?.Parameters.Any(p => p.Type.Name != "CancellationToken") != true)
            return "1=1";

        return string.Join(" AND ", context.Method.Parameters
            .Where(p => p.Type.Name != "CancellationToken")
            .Select(p => $"{context.Dialect.WrapColumn(ToSnakeCase(p.Name))} = {context.Dialect.ParameterPrefix}{p.Name}"));
    }
}

/// <summary>{{limit}} - 分页LIMIT占位符</summary>
public sealed class LimitHandler : PlaceholderHandlerBase
{
    public override string Name => "limit";

    public override string Process(PlaceholderContext context)
    {
        var options = ParseOptions(context);

        // 预定义模式
        var preset = GetPresetLimit(context.Type);
        if (preset.HasValue)
        {
            return FormatLimit(context.Dialect, preset.Value);
        }

        // 参数化形式: {{limit --param count}}
        var paramName = options.Get("param");
        if (string.IsNullOrEmpty(paramName) && context.Method != null)
        {
            // 自动检测名为 "limit" 的参数
            var limitParam = context.Method.Parameters.FirstOrDefault(p =>
                p.Name.Equals("limit", StringComparison.OrdinalIgnoreCase));
            if (limitParam != null)
            {
                paramName = limitParam.Name;
            }
        }

        if (!string.IsNullOrEmpty(paramName) && context.Method != null)
        {
            var param = context.Method.Parameters.FirstOrDefault(p =>
                p.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase));

            if (param != null)
            {
                var isNullable = IsNullableType(param.Type);
                if (isNullable)
                {
                    return $"{{RUNTIME_NULLABLE_LIMIT_{paramName}}}";
                }
                
                // 非可空参数 - 直接生成带参数的SQL语法
                return FormatLimitWithParam(context.Dialect, paramName);
            }
        }

        // 静态值
        var count = options.Get("count", options.Get("limit", options.FirstArg));
        if (string.IsNullOrEmpty(count)) count = "20";

        return FormatLimit(context.Dialect, int.TryParse(count, out var n) ? n : 20);
    }

    private static int? GetPresetLimit(string? type) => type?.ToLowerInvariant() switch
    {
        "tiny" => 5,
        "small" => 10,
        "medium" => 50,
        "large" => 100,
        "page" or "default" => 20,
        _ => null
    };

    private static string FormatLimit(SqlDefine dialect, int count)
    {
        if (dialect.Equals(SqlDefine.SqlServer))
            return $"OFFSET 0 ROWS FETCH NEXT {count} ROWS ONLY";
        if (dialect.Equals(SqlDefine.Oracle))
            return $"FETCH FIRST {count} ROWS ONLY";
        return $"LIMIT {count}";
    }

    private static string FormatLimitWithParam(SqlDefine dialect, string paramName)
    {
        var prefix = dialect.ParameterPrefix;
        
        // SQL Server 需要特殊处理 - 使用运行时占位符
        if (dialect.Equals(SqlDefine.SqlServer))
            return $"{{RUNTIME_LIMIT_{paramName}}}";
        
        // Oracle 使用 FETCH FIRST ... ROWS ONLY 或 ROWNUM
        if (dialect.Equals(SqlDefine.Oracle))
            return $"FETCH FIRST {prefix}{paramName} ROWS ONLY";
        
        // MySQL, PostgreSQL, SQLite 使用 LIMIT
        return $"LIMIT {prefix}{paramName}";
    }

    private static bool IsNullableType(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType &&
            namedType.IsGenericType &&
            namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            return true;
        }
        return type.NullableAnnotation == NullableAnnotation.Annotated;
    }
}

/// <summary>{{offset}} - 分页OFFSET占位符</summary>
public sealed class OffsetHandler : PlaceholderHandlerBase
{
    public override string Name => "offset";

    public override string Process(PlaceholderContext context)
    {
        var options = ParseOptions(context);
        var paramName = options.Get("param");

        // 自动检测名为 "offset" 的参数
        if (string.IsNullOrEmpty(paramName) && context.Method != null)
        {
            var offsetParam = context.Method.Parameters.FirstOrDefault(p =>
                p.Name.Equals("offset", StringComparison.OrdinalIgnoreCase));
            if (offsetParam != null)
            {
                paramName = offsetParam.Name;
            }
        }

        if (!string.IsNullOrEmpty(paramName) && context.Method != null)
        {
            var param = context.Method.Parameters.FirstOrDefault(p =>
                p.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase));

            if (param != null)
            {
                var isNullable = param.Type is INamedTypeSymbol nt &&
                                nt.IsGenericType &&
                                nt.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T;
                if (isNullable)
                {
                    return $"{{RUNTIME_NULLABLE_OFFSET_{paramName}}}";
                }
                
                // 非可空参数 - 直接生成带参数的SQL语法
                return FormatOffsetWithParam(context.Dialect, paramName);
            }
        }

        // 静态值
        var offset = options.Get("offset", options.Get("skip", options.FirstArg));
        if (string.IsNullOrEmpty(offset)) offset = "0";

        if (context.Dialect.Equals(SqlDefine.SqlServer) || context.Dialect.Equals(SqlDefine.Oracle))
            return $"OFFSET {offset} ROWS";
        return $"OFFSET {offset}";
    }

    private static string FormatOffsetWithParam(SqlDefine dialect, string paramName)
    {
        var prefix = dialect.ParameterPrefix;
        
        // SQL Server 和 Oracle 使用 OFFSET ... ROWS 语法
        if (dialect.Equals(SqlDefine.SqlServer) || dialect.Equals(SqlDefine.Oracle))
            return $"OFFSET {prefix}{paramName} ROWS";
        
        // MySQL, PostgreSQL, SQLite 使用 OFFSET
        return $"OFFSET {prefix}{paramName}";
    }
}

/// <summary>{{wrap column}} - 标识符包装占位符</summary>
public sealed class WrapHandler : PlaceholderHandlerBase
{
    public override string Name => "wrap";

    public override string Process(PlaceholderContext context)
    {
        var options = ParseOptions(context);
        var column = !string.IsNullOrEmpty(context.Type) ? context.Type :
                    !string.IsNullOrEmpty(options.FirstArg) ? options.FirstArg : "";

        if (string.IsNullOrEmpty(column))
        {
            context.Result.Warnings.Add("{{wrap}} requires a column name");
            return "";
        }

        return context.Dialect.WrapColumn(ToSnakeCase(column));
    }
}

/// <summary>{{orderby column}} - ORDER BY子句占位符</summary>
public sealed class OrderByHandler : PlaceholderHandlerBase
{
    public override string Name => "orderby";

    public override string Process(PlaceholderContext context)
    {
        var options = ParseOptions(context);
        
        // 动态ORDER BY参数: {{orderby --param orderBy}}
        var paramName = options.Get("param");
        if (!string.IsNullOrEmpty(paramName) && context.Method != null)
        {
            var param = context.Method.Parameters.FirstOrDefault(p => p.Name == paramName);
            if (param != null)
            {
                return $"{{RUNTIME_ORDERBY_{paramName}}}";
            }
        }

        // 静态列名: {{orderby column}} 或 {{orderby column --desc}}
        var column = !string.IsNullOrEmpty(context.Type) ? context.Type : options.FirstArg;
        
        if (string.IsNullOrEmpty(column))
        {
            context.Result.Warnings.Add("{{orderby}} requires a column name or --param option");
            return "";
        }

        var isDesc = options.Has("desc");
        var isAsc = options.Has("asc");
        var direction = isDesc ? " DESC" : (isAsc ? " ASC" : "");

        // 处理多列: {{orderby col1, col2}}
        var columns = column.Split(',')
            .Select(c => c.Trim())
            .Where(c => !string.IsNullOrEmpty(c))
            .Select(c => context.Dialect.WrapColumn(ToSnakeCase(c)));

        return $"ORDER BY {string.Join(", ", columns)}{direction}";
    }
}
