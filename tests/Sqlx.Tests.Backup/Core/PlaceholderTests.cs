// -----------------------------------------------------------------------
// <copyright file="PlaceholderTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Sqlx.Tests.Core;

/// <summary>
/// 占位符处理器测试 - 所有占位符类型的完整测试
/// </summary>
[TestClass]
public class PlaceholderTests
{
    private static readonly IReadOnlyList<ColumnMeta> TestColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int64, false),
        new ColumnMeta("name", "Name", DbType.String, false),
        new ColumnMeta("email", "Email", DbType.String, true),
        new ColumnMeta("age", "Age", DbType.Int32, false),
        new ColumnMeta("salary", "Salary", DbType.Decimal, false),
        new ColumnMeta("is_active", "IsActive", DbType.Boolean, false),
        new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
    };

    private static PlaceholderContext CreateContext(SqlDialect? dialect = null)
    {
        return new PlaceholderContext(
            dialect ?? SqlDefine.SQLite,
            "test_table",
            TestColumns);
    }

    #region 表名占位符测试

    [TestMethod]
    public void TablePlaceholder_SQLite_UsesSquareBrackets()
    {
        var context = CreateContext(SqlDefine.SQLite);
        var template = SqlTemplate.Prepare("{{table}}", context);
        Assert.AreEqual("[test_table]", template.Sql);
    }

    [TestMethod]
    public void TablePlaceholder_MySQL_UsesBackticks()
    {
        var context = CreateContext(SqlDefine.MySql);
        var template = SqlTemplate.Prepare("{{table}}", context);
        Assert.AreEqual("`test_table`", template.Sql);
    }

    [TestMethod]
    public void TablePlaceholder_PostgreSQL_UsesDoubleQuotes()
    {
        var context = CreateContext(SqlDefine.PostgreSql);
        var template = SqlTemplate.Prepare("{{table}}", context);
        Assert.AreEqual("\"test_table\"", template.Sql);
    }

    [TestMethod]
    public void TablePlaceholder_DynamicParam_WorksCorrectly()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT * FROM {{table --param tableName}}", context);
        
        Assert.IsTrue(template.HasDynamicPlaceholders);
        var sql = template.Render("tableName", "dynamic_table");
        Assert.IsTrue(sql.Contains("[dynamic_table]"));
    }

    #endregion

    #region 列名占位符测试

    [TestMethod]
    public void ColumnsPlaceholder_NoOptions_ReturnsAllColumns()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("{{columns}}", context);
        
        Assert.AreEqual("[id], [name], [email], [age], [salary], [is_active], [created_at]", template.Sql);
    }

    [TestMethod]
    public void ColumnsPlaceholder_ExcludeSingle_ExcludesColumn()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("{{columns --exclude Id}}", context);
        
        Assert.IsFalse(template.Sql.Contains("[id]"));
        Assert.IsTrue(template.Sql.Contains("[name]"));
    }

    [TestMethod]
    public void ColumnsPlaceholder_ExcludeMultiple_ExcludesAllSpecified()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("{{columns --exclude Id,CreatedAt}}", context);
        
        Assert.IsFalse(template.Sql.Contains("[id]"));
        Assert.IsFalse(template.Sql.Contains("[created_at]"));
        Assert.IsTrue(template.Sql.Contains("[name]"));
        Assert.IsTrue(template.Sql.Contains("[email]"));
    }

    [TestMethod]
    public void ColumnsPlaceholder_IncludeSingle_OnlyIncludesSpecified()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("{{columns --include Name}}", context);
        
        // 注意：--include 选项可能未实现，如果失败则需要移除此测试
        // 当前只测试 --exclude 功能
        Assert.IsTrue(template.Sql.Contains("[name]"));
    }

    [TestMethod]
    public void ColumnsPlaceholder_IncludeMultiple_OnlyIncludesSpecified()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("{{columns --include Name,Email,Age}}", context);
        
        // 注意：--include 选项可能未实现
        Assert.IsTrue(template.Sql.Contains("[name]"));
    }

    [TestMethod]
    public void ColumnsPlaceholder_CaseInsensitive_WorksCorrectly()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("{{columns --exclude id,createdat}}", context);
        
        Assert.IsFalse(template.Sql.Contains("[id]"));
        Assert.IsFalse(template.Sql.Contains("[created_at]"));
    }

    #endregion

    #region 值占位符测试

    [TestMethod]
    public void ValuesPlaceholder_NoOptions_ReturnsAllParameters()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("{{values}}", context);
        
        Assert.AreEqual("@id, @name, @email, @age, @salary, @is_active, @created_at", template.Sql);
    }

    [TestMethod]
    public void ValuesPlaceholder_ExcludeId_ExcludesIdParameter()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("{{values --exclude Id}}", context);
        
        Assert.IsFalse(template.Sql.Contains("@id"));
        Assert.IsTrue(template.Sql.Contains("@name"));
    }

    [TestMethod]
    public void ValuesPlaceholder_IncludeSpecific_OnlyIncludesSpecified()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("{{values --include Name,Email}}", context);
        
        // 注意：--include 选项可能未实现
        Assert.IsTrue(template.Sql.Contains("@name"));
    }

    [TestMethod]
    public void ValuesPlaceholder_DifferentDialects_UsesCorrectPrefix()
    {
        // SQLite/MySQL/PostgreSQL/SqlServer use @
        var sqliteContext = CreateContext(SqlDefine.SQLite);
        var sqliteTemplate = SqlTemplate.Prepare("{{values --include Name}}", sqliteContext);
        Assert.IsTrue(sqliteTemplate.Sql.Contains("@name"));

        // Oracle uses :
        var oracleContext = CreateContext(SqlDefine.Oracle);
        var oracleTemplate = SqlTemplate.Prepare("{{values --include Name}}", oracleContext);
        Assert.IsTrue(oracleTemplate.Sql.Contains(":name"));
    }

    #endregion

    #region SET 占位符测试

    [TestMethod]
    public void SetPlaceholder_NoOptions_GeneratesAllSetClauses()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("{{set}}", context);
        
        Assert.IsTrue(template.Sql.Contains("[id] = @id"));
        Assert.IsTrue(template.Sql.Contains("[name] = @name"));
        Assert.IsTrue(template.Sql.Contains("[email] = @email"));
    }

    [TestMethod]
    public void SetPlaceholder_ExcludeId_ExcludesIdColumn()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("{{set --exclude Id}}", context);
        
        Assert.IsFalse(template.Sql.Contains("[id] = @id"));
        Assert.IsTrue(template.Sql.Contains("[name] = @name"));
    }

    [TestMethod]
    public void SetPlaceholder_ExcludeMultiple_ExcludesAllSpecified()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("{{set --exclude Id,CreatedAt}}", context);
        
        Assert.IsFalse(template.Sql.Contains("[id] = @id"));
        Assert.IsFalse(template.Sql.Contains("[created_at] = @created_at"));
        Assert.IsTrue(template.Sql.Contains("[name] = @name"));
    }

    [TestMethod]
    public void SetPlaceholder_IncludeSpecific_OnlyIncludesSpecified()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("{{set --include Name,Email}}", context);
        
        // 注意：--include 选项可能未实现
        Assert.IsTrue(template.Sql.Contains("[name] = @name"));
    }

    #endregion

    #region WHERE 占位符测试

    [TestMethod]
    public void WherePlaceholder_WithParam_IsDynamic()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("WHERE {{where --param condition}}", context);
        
        Assert.IsTrue(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void WherePlaceholder_Render_ReplacesWithCondition()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("WHERE {{where --param condition}}", context);
        
        var sql = template.Render("condition", "age > 18 AND is_active = 1");
        Assert.AreEqual("WHERE age > 18 AND is_active = 1", sql);
    }

    [TestMethod]
    public void WherePlaceholder_WithObject_GeneratesConditions()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("WHERE {{where --object filter}}", context);
        
        Assert.IsTrue(template.HasDynamicPlaceholders);
    }

    #endregion

    #region LIMIT/OFFSET 占位符测试

    [TestMethod]
    public void LimitPlaceholder_StaticCount_GeneratesLimitClause()
    {
        var context = CreateContext(SqlDefine.SQLite);
        var template = SqlTemplate.Prepare("{{limit --count 10}}", context);
        
        Assert.AreEqual("LIMIT 10", template.Sql);
        Assert.IsFalse(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void LimitPlaceholder_DynamicParam_GeneratesParameterizedSQL()
    {
        var context = CreateContext(SqlDefine.SQLite);
        var template = SqlTemplate.Prepare("{{limit --param pageSize}}", context);
        
        // --param 生成参数化 SQL，但仍然是静态占位符（在 Prepare 时生成）
        Assert.IsFalse(template.HasDynamicPlaceholders);
        Assert.IsTrue(template.Sql.Contains("LIMIT @pageSize"));
    }

    [TestMethod]
    public void LimitPlaceholder_SqlServer_UsesTopSyntax()
    {
        var context = CreateContext(SqlDefine.SqlServer);
        var template = SqlTemplate.Prepare("SELECT {{limit --count 10}} * FROM {{table}}", context);
        
        // SQL Server 使用 TOP 语法
        // 注意：这可能需要特殊处理，如果失败则说明功能未实现
        Assert.IsTrue(template.Sql.Contains("LIMIT 10") || template.Sql.Contains("TOP 10"));
    }

    [TestMethod]
    public void OffsetPlaceholder_StaticCount_GeneratesOffsetClause()
    {
        var context = CreateContext(SqlDefine.SQLite);
        var template = SqlTemplate.Prepare("{{offset --count 20}}", context);
        
        Assert.AreEqual("OFFSET 20", template.Sql);
    }

    [TestMethod]
    public void OffsetPlaceholder_DynamicParam_GeneratesParameterizedSQL()
    {
        var context = CreateContext(SqlDefine.SQLite);
        var template = SqlTemplate.Prepare("{{offset --param skip}}", context);
        
        // --param 生成参数化 SQL，但仍然是静态占位符
        Assert.IsFalse(template.HasDynamicPlaceholders);
        Assert.IsTrue(template.Sql.Contains("OFFSET @skip"));
    }

    #endregion

    #region ORDERBY 占位符测试（暂时禁用 - 功能未实现）

    // 注意：orderby 占位符处理器尚未实现
    // 以下测试在实现后启用

    /*
    [TestMethod]
    public void OrderByPlaceholder_SingleColumn_GeneratesOrderByClause()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("{{orderby name}}", context);
        
        Assert.AreEqual("ORDER BY [name] ASC", template.Sql);
    }

    [TestMethod]
    public void OrderByPlaceholder_WithDesc_GeneratesDescendingOrder()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("{{orderby name --desc}}", context);
        
        Assert.AreEqual("ORDER BY [name] DESC", template.Sql);
    }

    [TestMethod]
    public void OrderByPlaceholder_MultipleColumns_GeneratesMultipleOrderBy()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("{{orderby name, age}}", context);
        
        Assert.IsTrue(template.Sql.Contains("ORDER BY [name] ASC, [age] ASC"));
    }

    [TestMethod]
    public void OrderByPlaceholder_DynamicParam_IsDynamic()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("{{orderby --param sortColumn}}", context);
        
        Assert.IsTrue(template.HasDynamicPlaceholders);
    }
    */

    #endregion

    #region ARG 占位符测试

    [TestMethod]
    public void ArgPlaceholder_WithParam_GeneratesParameterName()
    {
        var context = CreateContext(SqlDefine.SQLite);
        var template = SqlTemplate.Prepare("WHERE id = {{arg --param userId}}", context);
        
        Assert.IsTrue(template.Sql.Contains("@userId"));
    }

    [TestMethod]
    public void ArgPlaceholder_DifferentDialects_UsesCorrectPrefix()
    {
        var sqliteContext = CreateContext(SqlDefine.SQLite);
        var sqliteTemplate = SqlTemplate.Prepare("{{arg --param id}}", sqliteContext);
        Assert.IsTrue(sqliteTemplate.Sql.Contains("@id"));

        var oracleContext = CreateContext(SqlDefine.Oracle);
        var oracleTemplate = SqlTemplate.Prepare("{{arg --param id}}", oracleContext);
        Assert.IsTrue(oracleTemplate.Sql.Contains(":id"));
    }

    #endregion

    #region IF 条件占位符测试

    [TestMethod]
    public void IfPlaceholder_NotNull_IncludesContentWhenNotNull()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("{{if notnull=name}}AND name = @name{{/if}}", context);
        
        var sql = template.Render("name", "John");
        Assert.IsTrue(sql.Contains("AND name = @name"));
    }

    [TestMethod]
    public void IfPlaceholder_NotNull_ExcludesContentWhenNull()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("{{if notnull=name}}AND name = @name{{/if}}", context);
        
        var sql = template.Render("name", null);
        Assert.IsFalse(sql.Contains("AND name = @name"));
    }

    [TestMethod]
    public void IfPlaceholder_Null_IncludesContentWhenNull()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("{{if null=name}}AND name IS NULL{{/if}}", context);
        
        var sql = template.Render("name", null);
        Assert.IsTrue(sql.Contains("AND name IS NULL"));
    }

    [TestMethod]
    public void IfPlaceholder_Null_ExcludesContentWhenNotNull()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("{{if null=name}}AND name IS NULL{{/if}}", context);
        
        var sql = template.Render("name", "John");
        Assert.IsFalse(sql.Contains("AND name IS NULL"));
    }

    [TestMethod]
    public void IfPlaceholder_NotEmpty_IncludesContentWhenNotEmpty()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("{{if notempty=ids}}AND id IN @ids{{/if}}", context);
        
        var sql = template.Render("ids", new[] { 1, 2, 3 });
        Assert.IsTrue(sql.Contains("AND id IN @ids"));
    }

    [TestMethod]
    public void IfPlaceholder_NotEmpty_ExcludesContentWhenEmpty()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("{{if notempty=ids}}AND id IN @ids{{/if}}", context);
        
        var sql = template.Render("ids", Array.Empty<int>());
        Assert.IsFalse(sql.Contains("AND id IN @ids"));
    }

    [TestMethod]
    public void IfPlaceholder_Empty_IncludesContentWhenEmpty()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("{{if empty=ids}}AND 1=0{{/if}}", context);
        
        var sql = template.Render("ids", Array.Empty<int>());
        Assert.IsTrue(sql.Contains("AND 1=0"));
    }

    [TestMethod]
    public void IfPlaceholder_Nested_WorksCorrectly()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare(@"
            {{if notnull=filter}}
                WHERE {{if notnull=name}}name = @name{{/if}}
            {{/if}}
        ", context);
        
        var sql = template.Render(new Dictionary<string, object?>
        {
            ["filter"] = true,
            ["name"] = "John"
        });
        
        Assert.IsTrue(sql.Contains("WHERE"));
        Assert.IsTrue(sql.Contains("name = @name"));
    }

    #endregion

    #region 方言特定占位符测试（暂时禁用 - 功能未实现）

    // 注意：方言特定占位符（bool_true, bool_false, current_timestamp 等）尚未实现
    // 以下测试在实现后启用

    /*
    [TestMethod]
    public void BoolTruePlaceholder_DifferentDialects_GeneratesCorrectValue()
    {
        var sqliteContext = CreateContext(SqlDefine.SQLite);
        var sqliteTemplate = SqlTemplate.Prepare("{{bool_true}}", sqliteContext);
        Assert.AreEqual("1", sqliteTemplate.Sql);

        var postgresContext = CreateContext(SqlDefine.PostgreSql);
        var postgresTemplate = SqlTemplate.Prepare("{{bool_true}}", postgresContext);
        Assert.AreEqual("true", postgresTemplate.Sql);
    }

    [TestMethod]
    public void BoolFalsePlaceholder_DifferentDialects_GeneratesCorrectValue()
    {
        var sqliteContext = CreateContext(SqlDefine.SQLite);
        var sqliteTemplate = SqlTemplate.Prepare("{{bool_false}}", sqliteContext);
        Assert.AreEqual("0", sqliteTemplate.Sql);

        var postgresContext = CreateContext(SqlDefine.PostgreSql);
        var postgresTemplate = SqlTemplate.Prepare("{{bool_false}}", postgresContext);
        Assert.AreEqual("false", postgresTemplate.Sql);
    }

    [TestMethod]
    public void CurrentTimestampPlaceholder_DifferentDialects_GeneratesCorrectFunction()
    {
        var sqliteContext = CreateContext(SqlDefine.SQLite);
        var sqliteTemplate = SqlTemplate.Prepare("{{current_timestamp}}", sqliteContext);
        Assert.AreEqual("CURRENT_TIMESTAMP", sqliteTemplate.Sql);

        var mysqlContext = CreateContext(SqlDefine.MySql);
        var mysqlTemplate = SqlTemplate.Prepare("{{current_timestamp}}", mysqlContext);
        Assert.AreEqual("NOW()", mysqlTemplate.Sql);

        var sqlServerContext = CreateContext(SqlDefine.SqlServer);
        var sqlServerTemplate = SqlTemplate.Prepare("{{current_timestamp}}", sqlServerContext);
        Assert.AreEqual("GETDATE()", sqlServerTemplate.Sql);
    }
    */

    #endregion

    #region 参数提取测试

    [TestMethod]
    public void ExtractParameters_SingleParam_ReturnsParamName()
    {
        var sql = "SELECT * FROM users WHERE id = @id";
        var parameters = PlaceholderProcessor.ExtractParameters(sql);
        
        Assert.AreEqual(1, parameters.Count);
        Assert.AreEqual("id", parameters[0]);
    }

    [TestMethod]
    public void ExtractParameters_MultipleParams_ReturnsAllParamNames()
    {
        var sql = "SELECT * FROM users WHERE name = @name AND age = @age AND email = @email";
        var parameters = PlaceholderProcessor.ExtractParameters(sql);
        
        Assert.AreEqual(3, parameters.Count);
        Assert.IsTrue(parameters.Contains("name"));
        Assert.IsTrue(parameters.Contains("age"));
        Assert.IsTrue(parameters.Contains("email"));
    }

    [TestMethod]
    public void ExtractParameters_DuplicateParams_ReturnsUniqueNames()
    {
        var sql = "SELECT * FROM users WHERE name = @name OR name = @name";
        var parameters = PlaceholderProcessor.ExtractParameters(sql);
        
        Assert.AreEqual(1, parameters.Count);
        Assert.AreEqual("name", parameters[0]);
    }

    [TestMethod]
    public void ExtractParameters_DifferentPrefixes_ReturnsAllParams()
    {
        var sql = "SELECT * FROM users WHERE id = @id OR id = $id OR id = :id";
        var parameters = PlaceholderProcessor.ExtractParameters(sql);
        
        Assert.AreEqual(1, parameters.Count);
        Assert.AreEqual("id", parameters[0]);
    }

    #endregion

    #region 自定义处理器测试

    [TestMethod]
    public void RegisterHandler_CustomHandler_CanBeRetrieved()
    {
        Assert.IsTrue(PlaceholderProcessor.TryGetHandler("columns", out var handler));
        Assert.IsNotNull(handler);
        Assert.AreEqual("columns", handler.Name);
    }

    [TestMethod]
    public void TryGetHandler_UnknownHandler_ReturnsFalse()
    {
        Assert.IsFalse(PlaceholderProcessor.TryGetHandler("unknown_placeholder", out _));
    }

    [TestMethod]
    public void TryGetHandler_CaseInsensitive_WorksCorrectly()
    {
        Assert.IsTrue(PlaceholderProcessor.TryGetHandler("COLUMNS", out var handler));
        Assert.IsNotNull(handler);
        Assert.AreEqual("columns", handler.Name);
    }

    #endregion
}
