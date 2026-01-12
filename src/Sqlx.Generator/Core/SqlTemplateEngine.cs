// -----------------------------------------------------------------------
// <copyright file="SqlTemplateEngine.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Sqlx.Generator.Placeholders;

namespace Sqlx.Generator;

/// <summary>
/// SQL template processing engine - backward compatible wrapper for SqlTemplateProcessor.
/// </summary>
public sealed class SqlTemplateEngine
{
    private readonly SqlTemplateProcessor _processor;

    public SqlTemplateEngine(SqlDefine? defaultDialect = null)
    {
        _processor = new SqlTemplateProcessor(defaultDialect);
    }

    public SqlTemplateResult ProcessTemplate(string templateSql, IMethodSymbol? method, INamedTypeSymbol? entityType, string tableName) =>
        _processor.Process(templateSql, method, entityType, tableName);

    public SqlTemplateResult ProcessTemplate(string templateSql, IMethodSymbol? method, INamedTypeSymbol? entityType, string tableName, SqlDefine dialect) =>
        _processor.Process(templateSql, method, entityType, tableName, dialect);
}
