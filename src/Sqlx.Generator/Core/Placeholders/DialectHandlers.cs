// -----------------------------------------------------------------------
// <copyright file="DialectHandlers.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Linq;

namespace Sqlx.Generator.Placeholders;

/// <summary>{{bool_true}} - 布尔真值占位符</summary>
public sealed class BoolTrueHandler : IPlaceholderHandler
{
    public string Name => "bool_true";

    public string Process(PlaceholderContext context) =>
        context.Dialect.Equals(SqlDefine.PostgreSql) ? "true" : "1";
}

/// <summary>{{bool_false}} - 布尔假值占位符</summary>
public sealed class BoolFalseHandler : IPlaceholderHandler
{
    public string Name => "bool_false";

    public string Process(PlaceholderContext context) =>
        context.Dialect.Equals(SqlDefine.PostgreSql) ? "false" : "0";
}

/// <summary>{{current_timestamp}} - 当前时间戳占位符</summary>
public sealed class CurrentTimestampHandler : IPlaceholderHandler
{
    public string Name => "current_timestamp";

    public string Process(PlaceholderContext context)
    {
        // 使用标准的 CURRENT_TIMESTAMP，大多数数据库都支持
        if (context.Dialect.Equals(SqlDefine.SqlServer)) return "GETDATE()";
        if (context.Dialect.Equals(SqlDefine.Oracle)) return "SYSDATE";
        return "CURRENT_TIMESTAMP";
    }
}

/// <summary>{{random}} - 随机函数占位符</summary>
public sealed class RandomHandler : IPlaceholderHandler
{
    public string Name => "random";

    public string Process(PlaceholderContext context)
    {
        if (context.Dialect.Equals(SqlDefine.SqlServer)) return "RAND()";
        if (context.Dialect.Equals(SqlDefine.MySql)) return "RAND()";
        return "RANDOM()";
    }
}

/// <summary>{{coalesce}} - COALESCE函数占位符</summary>
public sealed class CoalesceHandler : PlaceholderHandlerBase
{
    public override string Name => "coalesce";

    public override string Process(PlaceholderContext context)
    {
        // 支持格式: {{coalesce col1, col2, 'default'}} 或 {{coalesce col, 0}}
        // context.Type 可能为空，内容在 context.Options 中
        var content = !string.IsNullOrEmpty(context.Type) ? context.Type : context.Options;
        var parts = content?.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToArray() ?? Array.Empty<string>();
        
        if (parts.Length == 0)
        {
            context.Result.Warnings.Add("{{coalesce}} requires at least one column name");
            return "COALESCE(NULL, 0)";
        }

        // 处理每个部分：列名需要包装，字符串字面量和数字保持原样
        var processedParts = parts.Select(p => WrapIfColumn(p, context.Dialect)).ToArray();
        
        return $"COALESCE({string.Join(", ", processedParts)})";
    }

    private static string WrapIfColumn(string value, SqlDefine dialect)
    {
        // 字符串字面量（以引号开头）保持原样
        if (value.StartsWith("'") || value.StartsWith("\""))
            return value;
        
        // 数字保持原样
        if (decimal.TryParse(value, out _))
            return value;
        
        // NULL 保持原样
        if (value.Equals("NULL", StringComparison.OrdinalIgnoreCase))
            return value;
        
        // 已经被包装的列名保持原样
        if (value.StartsWith("[") || value.StartsWith("`") || (value.StartsWith("\"") && value.EndsWith("\"")))
            return value;
        
        // 列名需要包装
        return dialect.WrapColumn(value);
    }
}

/// <summary>{{ifnull}} - IFNULL/ISNULL函数占位符</summary>
public sealed class IfNullHandler : PlaceholderHandlerBase
{
    public override string Name => "ifnull";

    public override string Process(PlaceholderContext context)
    {
        // context.Type 可能为空，内容在 context.Options 中
        var content = !string.IsNullOrEmpty(context.Type) ? context.Type : context.Options;
        var parts = content?.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToArray() ?? Array.Empty<string>();
        
        if (parts.Length < 2)
        {
            context.Result.Warnings.Add("{{ifnull}} requires column and default value");
            return "IFNULL(NULL, 0)";
        }

        var column = WrapIfColumn(parts[0], context.Dialect);
        var defaultValue = parts[1]; // 默认值保持原样

        // 不同数据库的IFNULL语法
        if (context.Dialect.Equals(SqlDefine.SqlServer))
            return $"ISNULL({column}, {defaultValue})";
        if (context.Dialect.Equals(SqlDefine.Oracle))
            return $"NVL({column}, {defaultValue})";
        if (context.Dialect.Equals(SqlDefine.PostgreSql))
            return $"COALESCE({column}, {defaultValue})";
        
        return $"IFNULL({column}, {defaultValue})";
    }

    private static string WrapIfColumn(string value, SqlDefine dialect)
    {
        if (value.StartsWith("'") || value.StartsWith("\""))
            return value;
        if (decimal.TryParse(value, out _))
            return value;
        if (value.Equals("NULL", StringComparison.OrdinalIgnoreCase))
            return value;
        if (value.StartsWith("[") || value.StartsWith("`") || (value.StartsWith("\"") && value.EndsWith("\"")))
            return value;
        return dialect.WrapColumn(value);
    }
}

/// <summary>聚合函数占位符 - 支持 COUNT, SUM, AVG, MAX, MIN</summary>
public sealed class AggregateHandler : PlaceholderHandlerBase
{
    private readonly string _function;

    public AggregateHandler(string function)
    {
        _function = function.ToUpperInvariant();
    }

    public override string Name => _function.ToLowerInvariant();

    public override string Process(PlaceholderContext context)
    {
        var options = ParseOptions(context);
        
        // 确定列名
        var column = options.Get("column", "");
        if (string.IsNullOrEmpty(column))
        {
            column = !string.IsNullOrEmpty(context.Type) ? context.Type : options.FirstArg;
        }

        // 处理特殊情况
        if (string.IsNullOrEmpty(column) || column == "*" || column == "all")
        {
            column = "*";
        }
        else if (column != "*" && !column.Contains("(") && !column.Contains("."))
        {
            column = ToSnakeCase(column);
        }

        // 检查DISTINCT
        var distinct = options.Has("distinct") || column == "distinct";
        if (column == "distinct") column = "*";

        // 检查COALESCE
        var useCoalesce = options.Has("coalesce") || !string.IsNullOrEmpty(options.Get("default"));
        var defaultValue = options.Get("default", "0");

        // 构建结果
        var result = distinct ? $"{_function}(DISTINCT {column})" : $"{_function}({column})";
        
        return useCoalesce ? $"COALESCE({result}, {defaultValue})" : result;
    }
}

/// <summary>{{batch_values}} - 批量INSERT值占位符</summary>
public sealed class BatchValuesHandler : PlaceholderHandlerBase
{
    public override string Name => "batch_values";

    public override string Process(PlaceholderContext context)
    {
        // 返回运行时标记，由代码生成器处理
        return "{{BATCH_VALUES_PLACEHOLDER}}";
    }
}
