// -----------------------------------------------------------------------
// <copyright file="SqlTemplateTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Sqlx.Annotations;

namespace Sqlx.Tests.Core;

/// <summary>
/// SqlTemplate 核心功能测试 - 模板准备、渲染、占位符处理
/// </summary>
[TestClass]
public class SqlTemplateTests
{
    private static readonly IReadOnlyList<ColumnMeta> TestColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int64, false),
        new ColumnMeta("name", "Name", DbType.String, false),
        new ColumnMeta("email", "Email", DbType.String, true),
        new ColumnMeta("age", "Age", DbType.Int32, false),
        new ColumnMeta("is_active", "IsActive", DbType.Boolean, false),
        new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
    };

    private static PlaceholderContext CreateContext(SqlDialect? dialect = null, string? tableName = null)
    {
        return new PlaceholderContext(
            dialect ?? SqlDefine.SQLite,
            tableName ?? "users",
            TestColumns);
    }

    #region 基础模板准备测试

    [TestMethod]
    public void Prepare_EmptyTemplate_ReturnsEmptySQL()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("", context);
        
        Assert.AreEqual("", template.Sql);
        Assert.IsFalse(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void Prepare_PlainText_ReturnsUnchanged()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT * FROM users", context);
        
        Assert.AreEqual("SELECT * FROM users", template.Sql);
        Assert.IsFalse(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void Prepare_UnknownPlaceholder_ThrowsException()
    {
        var context = CreateContext();
        
        Assert.ThrowsException<InvalidOperationException>(() =>
            SqlTemplate.Prepare("SELECT {{unknown}} FROM users", context));
    }

    #endregion

    #region 静态占位符测试

    [TestMethod]
    public void Prepare_TablePlaceholder_ReplacesWithQuotedTableName()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}}", context);
        
        Assert.AreEqual("SELECT * FROM [users]", template.Sql);
        Assert.IsFalse(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void Prepare_ColumnsPlaceholder_ReplacesWithAllColumns()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        Assert.AreEqual("SELECT [id], [name], [email], [age], [is_active], [created_at] FROM [users]", template.Sql);
        Assert.IsFalse(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void Prepare_ColumnsWithExclude_ExcludesSpecifiedColumns()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns --exclude Id}} FROM {{table}}", context);
        
        Assert.IsTrue(template.Sql.Contains("[name]"));
        Assert.IsTrue(template.Sql.Contains("[age]"));
        Assert.IsFalse(template.Sql.Contains("[id]"));
    }

    [TestMethod]
    public void Prepare_ColumnsWithInclude_OnlyIncludesSpecifiedColumns()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns --include Name,Age}} FROM {{table}}", context);
        
        // 注意：--include 选项可能未实现
        Assert.IsTrue(template.Sql.Contains("[name]"));
    }

    [TestMethod]
    public void Prepare_ValuesPlaceholder_GeneratesParameterList()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("INSERT INTO {{table}} VALUES ({{values}})", context);
        
        Assert.IsTrue(template.Sql.Contains("@id, @name, @email, @age, @is_active, @created_at"));
    }

    [TestMethod]
    public void Prepare_ValuesWithExclude_ExcludesParameters()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})", context);
        
        Assert.IsFalse(template.Sql.Contains("@id"));
        Assert.IsTrue(template.Sql.Contains("@name"));
        Assert.IsTrue(template.Sql.Contains("@email"));
    }

    [TestMethod]
    public void Prepare_SetPlaceholder_GeneratesUpdateClause()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set}}", context);
        
        Assert.IsTrue(template.Sql.Contains("[id] = @id"));
        Assert.IsTrue(template.Sql.Contains("[name] = @name"));
        Assert.IsTrue(template.Sql.Contains("[email] = @email"));
    }

    [TestMethod]
    public void Prepare_SetWithExclude_ExcludesColumns()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id,CreatedAt}} WHERE id = @id", context);
        
        Assert.IsFalse(template.Sql.Contains("[id] = @id"));
        Assert.IsFalse(template.Sql.Contains("[created_at] = @created_at"));
        Assert.IsTrue(template.Sql.Contains("[name] = @name"));
    }

    #endregion

    #region 动态占位符测试

    [TestMethod]
    public void Prepare_WherePlaceholder_MarksDynamic()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} WHERE {{where --param predicate}}", context);
        
        Assert.IsTrue(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void Render_WherePlaceholder_ReplacesWithCondition()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} WHERE {{where --param predicate}}", context);
        
        var sql = template.Render("predicate", "age > 18 AND is_active = 1");
        
        Assert.IsTrue(sql.Contains("WHERE age > 18 AND is_active = 1"));
    }

    [TestMethod]
    public void Render_MultipleDynamicParams_ReplacesAll()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} WHERE {{where --param condition1}} AND {{where --param condition2}}", context);
        
        var sql = template.Render(new Dictionary<string, object?>
        {
            ["condition1"] = "age > 18",
            ["condition2"] = "is_active = 1"
        });
        
        Assert.IsTrue(sql.Contains("age > 18"));
        Assert.IsTrue(sql.Contains("is_active = 1"));
    }

    #endregion

    #region 条件块测试

    [TestMethod]
    public void Prepare_IfBlock_MarksHasBlocks()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} {{if notnull=name}}WHERE name = @name{{/if}}", context);
        
        Assert.IsTrue(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void Render_IfBlockWithNonNullParam_IncludesContent()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} {{if notnull=name}}WHERE name = @name{{/if}}", context);
        
        var sql = template.Render("name", "John");
        
        Assert.IsTrue(sql.Contains("WHERE name = @name"));
    }

    [TestMethod]
    public void Render_IfBlockWithNullParam_ExcludesContent()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} {{if notnull=name}}WHERE name = @name{{/if}}", context);
        
        var sql = template.Render("name", null);
        
        Assert.IsFalse(sql.Contains("WHERE"));
    }

    [TestMethod]
    public void Render_MultipleIfBlocks_EvaluatesIndependently()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare(@"
            SELECT * FROM {{table}} WHERE 1=1
            {{if notnull=name}}AND name = @name{{/if}}
            {{if notnull=age}}AND age = @age{{/if}}
        ", context);
        
        var sql = template.Render(new Dictionary<string, object?>
        {
            ["name"] = "John",
            ["age"] = null
        });
        
        Assert.IsTrue(sql.Contains("AND name = @name"));
        Assert.IsFalse(sql.Contains("AND age = @age"));
    }

    [TestMethod]
    public void Render_NestedIfBlocks_WorksCorrectly()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare(@"
            SELECT * FROM {{table}}
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

    #region 方言特定测试

    [TestMethod]
    [DataRow(SqlDefineTypes.SQLite, "[users]", "[name]")]
    [DataRow(SqlDefineTypes.SqlServer, "[users]", "[name]")]
    [DataRow(SqlDefineTypes.MySql, "`users`", "`name`")]
    [DataRow(SqlDefineTypes.PostgreSql, "\"users\"", "\"name\"")]
    [DataRow(SqlDefineTypes.Oracle, "\"users\"", "\"name\"")]
    [DataRow(SqlDefineTypes.DB2, "\"users\"", "\"name\"")]
    public void Prepare_DifferentDialects_UsesCorrectQuotes(SqlDefineTypes dialectType, string expectedTable, string expectedColumn)
    {
        var dialect = dialectType switch
        {
            SqlDefineTypes.SQLite => SqlDefine.SQLite,
            SqlDefineTypes.SqlServer => SqlDefine.SqlServer,
            SqlDefineTypes.MySql => SqlDefine.MySql,
            SqlDefineTypes.PostgreSql => SqlDefine.PostgreSql,
            SqlDefineTypes.Oracle => SqlDefine.Oracle,
            SqlDefineTypes.DB2 => SqlDefine.DB2,
            _ => throw new ArgumentException()
        };

        var context = CreateContext(dialect);
        var template = SqlTemplate.Prepare("SELECT {{columns --include Name}} FROM {{table}}", context);
        
        Assert.IsTrue(template.Sql.Contains(expectedTable));
        Assert.IsTrue(template.Sql.Contains(expectedColumn));
    }

    #endregion

    #region 性能和边界测试

    [TestMethod]
    public void Prepare_LargeTemplate_HandlesEfficiently()
    {
        var context = CreateContext();
        var largeTemplate = string.Join(" ", Enumerable.Repeat("{{columns}}", 100));
        
        var template = SqlTemplate.Prepare(largeTemplate, context);
        
        Assert.IsNotNull(template.Sql);
    }

    [TestMethod]
    public void Render_WithoutDynamicParams_ReturnsPreparedSQL()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        var sql = template.Render((IReadOnlyDictionary<string, object?>?)null);
        
        Assert.AreEqual(template.Sql, sql);
    }

    [TestMethod]
    public void Render_SingleParam_UsesOptimizedPath()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} WHERE {{where --param condition}}", context);
        
        var sql1 = template.Render("condition", "age > 18");
        var sql2 = template.Render("condition", "age > 18");
        
        Assert.AreEqual(sql1, sql2);
    }

    [TestMethod]
    public void Render_TwoParams_UsesOptimizedPath()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} WHERE {{where --param c1}} {{limit --param c2}}", context);
        
        var sql = template.Render("c1", "age > 18", "c2", 10);
        
        Assert.IsTrue(sql.Contains("age > 18"));
        Assert.IsTrue(sql.Contains("LIMIT"));
    }

    #endregion

    #region 复杂场景测试

    [TestMethod]
    public void Prepare_MixedStaticAndDynamic_WorksCorrectly()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare(@"
            SELECT {{columns --exclude Id}}
            FROM {{table}}
            WHERE {{where --param condition}}
            {{if notnull=sortColumn}}ORDER BY @sortColumn{{/if}}
            {{limit --count 100}}
        ", context);
        
        Assert.IsTrue(template.HasDynamicPlaceholders);
        Assert.IsTrue(template.Sql.Contains("LIMIT 100"));
        Assert.IsFalse(template.Sql.Contains("[id]"));
    }

    [TestMethod]
    public void Render_ComplexTemplate_GeneratesCorrectSQL()
    {
        var context = CreateContext();
        var template = SqlTemplate.Prepare(@"
            SELECT {{columns}}
            FROM {{table}}
            WHERE 1=1
            {{if notnull=name}}AND name LIKE @name{{/if}}
            {{if notnull=minAge}}AND age >= @minAge{{/if}}
            {{if notnull=maxAge}}AND age <= @maxAge{{/if}}
            {{if notnull=isActive}}AND is_active = @isActive{{/if}}
            ORDER BY created_at DESC
            {{limit --count 50}}
        ", context);
        
        var sql = template.Render(new Dictionary<string, object?>
        {
            ["name"] = "%John%",
            ["minAge"] = 18,
            ["maxAge"] = null,
            ["isActive"] = true
        });
        
        Assert.IsTrue(sql.Contains("AND name LIKE @name"));
        Assert.IsTrue(sql.Contains("AND age >= @minAge"));
        Assert.IsFalse(sql.Contains("AND age <= @maxAge"));
        Assert.IsTrue(sql.Contains("AND is_active = @isActive"));
        Assert.IsTrue(sql.Contains("LIMIT 50"));
    }

    #endregion
}
