// -----------------------------------------------------------------------
// <copyright file="IPlaceholderHandler.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;

namespace Sqlx.Generator.Placeholders;

/// <summary>
/// 占位符处理上下文 - 包含处理占位符所需的所有信息
/// </summary>
public sealed class PlaceholderContext
{
    public string Name { get; init; } = "";
    public string Type { get; init; } = "";
    public string Options { get; init; } = "";
    public IMethodSymbol? Method { get; init; }
    public INamedTypeSymbol? EntityType { get; init; }
    public string TableName { get; init; } = "";
    public SqlDefine Dialect { get; init; } = SqlDefine.SqlServer;
    public SqlTemplateResult Result { get; init; } = new();
}

/// <summary>
/// 占位符处理器接口 - 所有占位符处理器的统一抽象
/// </summary>
public interface IPlaceholderHandler
{
    /// <summary>
    /// 占位符名称（小写）
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 处理占位符并返回SQL片段
    /// </summary>
    string Process(PlaceholderContext context);
}
