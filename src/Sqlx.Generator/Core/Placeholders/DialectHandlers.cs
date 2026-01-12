// -----------------------------------------------------------------------
// <copyright file="DialectHandlers.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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
        if (context.Dialect.Equals(SqlDefine.MySql)) return "NOW()";
        if (context.Dialect.Equals(SqlDefine.SqlServer)) return "GETDATE()";
        if (context.Dialect.Equals(SqlDefine.Oracle)) return "SYSDATE";
        if (context.Dialect.Equals(SqlDefine.SQLite)) return "datetime('now')";
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
        var options = ParseOptions(context);
        
        // 支持格式: {{coalesce column, default}} 或 {{coalesce --column col --default 0}}
        var parts = context.Type?.Split(',') ?? new[] { "" };
        var column = parts.Length > 0 ? parts[0].Trim() : options.Get("column", "");
        var defaultValue = parts.Length > 1 ? parts[1].Trim() : options.Get("default", "0");

        if (string.IsNullOrEmpty(column))
        {
            // 可能是嵌套占位符的结果，直接使用options
            column = options.FirstArg;
        }

        if (string.IsNullOrEmpty(column))
        {
            context.Result.Warnings.Add("{{coalesce}} requires a column name");
            return "COALESCE(NULL, 0)";
        }

        return $"COALESCE({column}, {defaultValue})";
    }
}

/// <summary>{{ifnull}} - IFNULL/ISNULL函数占位符</summary>
public sealed class IfNullHandler : PlaceholderHandlerBase
{
    public override string Name => "ifnull";

    public override string Process(PlaceholderContext context)
    {
        var options = ParseOptions(context);
        var parts = context.Type?.Split(',') ?? new[] { "" };
        var column = parts.Length > 0 ? parts[0].Trim() : options.Get("column", "");
        var defaultValue = parts.Length > 1 ? parts[1].Trim() : options.Get("default", "0");

        if (string.IsNullOrEmpty(column))
        {
            column = options.FirstArg;
        }

        if (string.IsNullOrEmpty(column))
        {
            context.Result.Warnings.Add("{{ifnull}} requires a column name");
            return "IFNULL(NULL, 0)";
        }

        // 不同数据库的IFNULL语法
        if (context.Dialect.Equals(SqlDefine.SqlServer))
            return $"ISNULL({column}, {defaultValue})";
        if (context.Dialect.Equals(SqlDefine.Oracle))
            return $"NVL({column}, {defaultValue})";
        if (context.Dialect.Equals(SqlDefine.PostgreSql))
            return $"COALESCE({column}, {defaultValue})";
        
        return $"IFNULL({column}, {defaultValue})";
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
