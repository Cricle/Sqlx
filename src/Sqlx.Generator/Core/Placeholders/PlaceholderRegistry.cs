// -----------------------------------------------------------------------
// <copyright file="PlaceholderRegistry.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Sqlx.Generator.Placeholders;

/// <summary>
/// 占位符注册表 - 管理所有占位符处理器
/// 支持运行时注册自定义占位符处理器
/// </summary>
public sealed class PlaceholderRegistry
{
    private readonly Dictionary<string, IPlaceholderHandler> _handlers = new(StringComparer.OrdinalIgnoreCase);
    private static PlaceholderRegistry? _default;

    /// <summary>默认注册表实例（包含所有内置占位符）</summary>
    public static PlaceholderRegistry Default => _default ??= CreateDefault();

    /// <summary>注册占位符处理器</summary>
    public PlaceholderRegistry Register(IPlaceholderHandler handler)
    {
        _handlers[handler.Name] = handler;
        return this;
    }

    /// <summary>注册多个占位符处理器</summary>
    public PlaceholderRegistry Register(params IPlaceholderHandler[] handlers)
    {
        foreach (var handler in handlers)
        {
            _handlers[handler.Name] = handler;
        }
        return this;
    }

    /// <summary>尝试获取占位符处理器</summary>
    public bool TryGet(string name, out IPlaceholderHandler? handler) =>
        _handlers.TryGetValue(name, out handler);

    /// <summary>处理占位符</summary>
    public string Process(PlaceholderContext context)
    {
        if (_handlers.TryGetValue(context.Name, out var handler))
        {
            return handler.Process(context);
        }

        // 未知占位符 - 添加警告并保留原始值
        context.Result.Warnings.Add($"Unknown placeholder '{context.Name}'");
        return BuildOriginalPlaceholder(context);
    }

    /// <summary>检查占位符是否已注册</summary>
    public bool Contains(string name) => _handlers.ContainsKey(name);

    /// <summary>获取所有已注册的占位符名称</summary>
    public IEnumerable<string> GetRegisteredNames() => _handlers.Keys;

    private static string BuildOriginalPlaceholder(PlaceholderContext context)
    {
        var result = context.Name;
        if (!string.IsNullOrEmpty(context.Type))
            result += $":{context.Type}";
        if (!string.IsNullOrEmpty(context.Options))
            result += context.Options.StartsWith("--") ? $" {context.Options}" : $"|{context.Options}";
        return $"{{{{{result}}}}}";
    }

    private static PlaceholderRegistry CreateDefault()
    {
        var registry = new PlaceholderRegistry();
        
        // 核心占位符
        registry.Register(
            new TableHandler(),
            new ColumnsHandler(),
            new ValuesHandler(),
            new SetHandler(),
            new WhereHandler(),
            new LimitHandler(),
            new OffsetHandler(),
            new WrapHandler(),
            new OrderByHandler()
        );

        // 数据库特定占位符
        registry.Register(
            new BoolTrueHandler(),
            new BoolFalseHandler(),
            new CurrentTimestampHandler(),
            new RandomHandler()
        );

        // 聚合函数
        registry.Register(
            new AggregateHandler("count"),
            new AggregateHandler("sum"),
            new AggregateHandler("avg"),
            new AggregateHandler("max"),
            new AggregateHandler("min")
        );

        // 条件函数
        registry.Register(
            new CoalesceHandler(),
            new IfNullHandler()
        );

        // 批量操作
        registry.Register(new BatchValuesHandler());

        return registry;
    }
}
