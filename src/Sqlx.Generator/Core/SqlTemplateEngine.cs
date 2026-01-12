// -----------------------------------------------------------------------
// <copyright file="SqlTemplateEngine.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Sqlx.Generator.Placeholders;

namespace Sqlx.Generator;

/// <summary>
/// SQL模板处理引擎 - 简化版本
/// 内部委托给 SqlTemplateProcessor 处理
/// 保留此类以保持向后兼容性
/// </summary>
public class SqlTemplateEngine
{
    private readonly SqlTemplateProcessor _processor;
    private readonly SqlDefine _defaultDialect;

    /// <summary>
    /// 初始化SQL模板引擎
    /// </summary>
    /// <param name="defaultDialect">默认数据库方言，如不指定则使用SqlServer</param>
    public SqlTemplateEngine(SqlDefine? defaultDialect = null)
    {
        _defaultDialect = defaultDialect ?? SqlDefine.SqlServer;
        _processor = new SqlTemplateProcessor(_defaultDialect);
    }

    /// <summary>
    /// 处理SQL模板
    /// </summary>
    public SqlTemplateResult ProcessTemplate(string templateSql, IMethodSymbol? method, INamedTypeSymbol? entityType, string tableName)
    {
        return ProcessTemplate(templateSql, method, entityType, tableName, _defaultDialect);
    }

    /// <summary>
    /// 处理SQL模板 - 多数据库支持版本
    /// </summary>
    public SqlTemplateResult ProcessTemplate(string templateSql, IMethodSymbol? method, INamedTypeSymbol? entityType, string tableName, SqlDefine dialect)
    {
        return _processor.Process(templateSql, method, entityType, tableName, dialect);
    }
}
