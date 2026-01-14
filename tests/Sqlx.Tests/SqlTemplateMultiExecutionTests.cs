using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System.Data;

namespace Sqlx.Tests;

/// <summary>
/// Tests for SqlTemplate multi-SQL execution scenarios.
/// </summary>
[TestClass]
public class SqlTemplateMultiExecutionTests
{
    private static readonly IReadOnlyList<ColumnMeta> UserColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int64, false),
        new ColumnMeta("user_name", "UserName", DbType.String, false),
        new ColumnMeta("email", "Email", DbType.String, false),
        new ColumnMeta("is_active", "IsActive", DbType.Boolean, false),
        new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
        new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, true),
    };

    private static PlaceholderContext CreateUserContext(SqlDialect? dialect = null)
    {
        return new PlaceholderContext(dialect ?? SqlDefine.SQLite, "users", UserColumns);
    }

    #region Basic Multi-Placeholder Tests

    [TestMethod]
    public void Prepare_SelectWithAllPlaceholders_GeneratesCorrectSql()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE id = @id",
            context);

        Assert.AreEqual("SELECT [id], [user_name], [email], [is_active], [created_at], [updated_at] FROM [users] WHERE id = @id", template.Sql);
        Assert.IsFalse(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void Prepare_InsertWithExclude_GeneratesCorrectSql()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})",
            context);

        Assert.IsTrue(template.Sql.Contains("[user_name]"));
        Assert.IsTrue(template.Sql.Contains("@user_name"));
        Assert.IsFalse(template.Sql.Contains("[id]"));
        Assert.IsFalse(template.Sql.Contains("@id"));
    }

    [TestMethod]
    public void Prepare_UpdateWithExclude_GeneratesCorrectSql()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id,CreatedAt}} WHERE id = @id",
            context);

        Assert.IsTrue(template.Sql.Contains("[user_name] = @user_name"));
        Assert.IsTrue(template.Sql.Contains("[email] = @email"));
        Assert.IsFalse(template.Sql.Contains("[id] = @id"));
        Assert.IsFalse(template.Sql.Contains("[created_at] = @created_at"));
    }

    #endregion

    #region Dynamic Placeholder Tests

    [TestMethod]
    public void Render_WithLimitAndOffset_GeneratesCorrectSql()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} {{limit --param limit}} {{offset --param offset}}",
            context);

        var rendered = template.Render(new Dictionary<string, object?>
        {
            ["limit"] = 10,
            ["offset"] = 20
        });

        Assert.IsTrue(rendered.Contains("LIMIT 10"));
        Assert.IsTrue(rendered.Contains("OFFSET 20"));
    }

    [TestMethod]
    public void Render_WithWhereClause_GeneratesCorrectSql()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}",
            context);

        var rendered = template.Render(new Dictionary<string, object?>
        {
            ["predicate"] = "[is_active] = 1"
        });

        Assert.IsTrue(rendered.Contains("WHERE [is_active] = 1"));
    }

    [TestMethod]
    public void Render_WithNullDynamicParam_GeneratesEmptyClause()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} {{limit --param limit}}",
            context);

        var rendered = template.Render(new Dictionary<string, object?>
        {
            ["limit"] = null
        });

        Assert.IsFalse(rendered.Contains("LIMIT"));
    }

    #endregion

    #region Conditional Block Tests

    [TestMethod]
    public void Render_ConditionalBlock_IncludedWhenNotNull()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE 1=1 {{if notnull=filter}}AND [is_active] = @isActive{{/if}}",
            context);

        var rendered = template.Render(new Dictionary<string, object?>
        {
            ["filter"] = true
        });

        Assert.IsTrue(rendered.Contains("AND [is_active] = @isActive"));
    }

    [TestMethod]
    public void Render_ConditionalBlock_ExcludedWhenNull()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE 1=1 {{if notnull=filter}}AND [is_active] = @isActive{{/if}}",
            context);

        var rendered = template.Render(new Dictionary<string, object?>
        {
            ["filter"] = null
        });

        Assert.IsFalse(rendered.Contains("AND [is_active] = @isActive"));
    }

    [TestMethod]
    public void Render_NestedConditionalBlocks_WorksCorrectly()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE 1=1 {{if notnull=outer}}{{if notnull=inner}}AND [is_active] = 1{{/if}}{{/if}}",
            context);

        var rendered1 = template.Render(new Dictionary<string, object?>
        {
            ["outer"] = true,
            ["inner"] = true
        });
        Assert.IsTrue(rendered1.Contains("AND [is_active] = 1"));

        var rendered2 = template.Render(new Dictionary<string, object?>
        {
            ["outer"] = true,
            ["inner"] = null
        });
        Assert.IsFalse(rendered2.Contains("AND [is_active] = 1"));

        var rendered3 = template.Render(new Dictionary<string, object?>
        {
            ["outer"] = null,
            ["inner"] = true
        });
        Assert.IsFalse(rendered3.Contains("AND [is_active] = 1"));
    }

    [TestMethod]
    public void Render_MultipleConditionalBlocks_WorksIndependently()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE 1=1 {{if notnull=filterActive}}AND [is_active] = 1{{/if}} {{if notnull=filterEmail}}AND [email] IS NOT NULL{{/if}}",
            context);

        var rendered = template.Render(new Dictionary<string, object?>
        {
            ["filterActive"] = true,
            ["filterEmail"] = null
        });

        Assert.IsTrue(rendered.Contains("AND [is_active] = 1"));
        Assert.IsFalse(rendered.Contains("AND [email] IS NOT NULL"));
    }

    #endregion

    #region Complex Query Tests

    [TestMethod]
    public void Prepare_ComplexSelectWithJoin_GeneratesCorrectSql()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare(
            "SELECT u.{{columns}}, COUNT(o.id) as order_count FROM {{table}} u LEFT JOIN orders o ON u.id = o.user_id GROUP BY u.id",
            context);

        // Note: {{columns}} generates column names without table alias prefix
        Assert.IsTrue(template.Sql.Contains("[id], [user_name], [email]"));
        Assert.IsTrue(template.Sql.Contains("GROUP BY u.id"));
    }

    [TestMethod]
    public void Prepare_SubqueryWithPlaceholders_GeneratesCorrectSql()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE id IN (SELECT user_id FROM orders WHERE status = @status)",
            context);

        Assert.IsTrue(template.Sql.Contains("SELECT [id], [user_name]"));
        Assert.IsTrue(template.Sql.Contains("WHERE id IN"));
    }

    [TestMethod]
    public void Render_ComplexDynamicQuery_GeneratesCorrectSql()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE 1=1 {{if notempty=name}}AND [user_name] LIKE @name{{/if}} {{if notnull=email}}AND [email] = @email{{/if}} {{limit --param limit}} {{offset --param offset}}",
            context);

        var rendered = template.Render(new Dictionary<string, object?>
        {
            ["name"] = "%test%",
            ["email"] = null,
            ["limit"] = 10,
            ["offset"] = 0
        });

        Assert.IsTrue(rendered.Contains("AND [user_name] LIKE @name"));
        Assert.IsFalse(rendered.Contains("AND [email] = @email"));
        Assert.IsTrue(rendered.Contains("LIMIT 10"));
    }

    #endregion

    #region Multi-Dialect Tests

    [TestMethod]
    public void Prepare_MySqlDialect_UsesBackticks()
    {
        var context = CreateUserContext(SqlDefine.MySql);
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE id = @id",
            context);

        Assert.IsTrue(template.Sql.Contains("`id`"));
        Assert.IsTrue(template.Sql.Contains("`users`"));
    }

    [TestMethod]
    public void Prepare_SqlServerDialect_UsesBrackets()
    {
        var context = CreateUserContext(SqlDefine.SqlServer);
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE id = @id",
            context);

        Assert.IsTrue(template.Sql.Contains("[id]"));
        Assert.IsTrue(template.Sql.Contains("[users]"));
    }

    [TestMethod]
    public void Prepare_PostgreSqlDialect_UsesDoubleQuotes()
    {
        var context = CreateUserContext(SqlDefine.PostgreSql);
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE id = @id",
            context);

        Assert.IsTrue(template.Sql.Contains("\"id\""));
        Assert.IsTrue(template.Sql.Contains("\"users\""));
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void Prepare_EmptyTemplate_ReturnsEmptySql()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare("", context);

        Assert.AreEqual("", template.Sql);
        Assert.IsFalse(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void Prepare_NoPlaceholders_ReturnsSameString()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare("SELECT * FROM users WHERE id = 1", context);

        Assert.AreEqual("SELECT * FROM users WHERE id = 1", template.Sql);
        Assert.IsFalse(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Prepare_UnknownPlaceholder_ThrowsException()
    {
        var context = CreateUserContext();
        SqlTemplate.Prepare("SELECT {{unknown}} FROM {{table}}", context);
    }

    #endregion

    #region Performance Optimization Tests

    [TestMethod]
    public void Prepare_StaticTemplate_HasDynamicPlaceholdersIsFalse()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE id = @id",
            context);

        Assert.IsFalse(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void Prepare_DynamicTemplate_HasDynamicPlaceholdersIsTrue()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} {{limit --param limit}}",
            context);

        Assert.IsTrue(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void Prepare_ConditionalTemplate_HasDynamicPlaceholdersIsTrue()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} {{if notnull=filter}}WHERE [is_active] = 1{{/if}}",
            context);

        Assert.IsTrue(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void Render_StaticTemplate_ReturnsSameSqlInstance()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}}",
            context);

        var rendered1 = template.Render(null);
        var rendered2 = template.Render(null);

        Assert.AreEqual(template.Sql, rendered1);
        Assert.AreEqual(template.Sql, rendered2);
    }

    #endregion

    #region Batch Operation Tests

    [TestMethod]
    public void Prepare_BatchInsert_GeneratesCorrectSql()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})",
            context);

        Assert.IsTrue(template.Sql.Contains("INSERT INTO [users]"));
        Assert.IsTrue(template.Sql.Contains("[user_name]"));
        Assert.IsTrue(template.Sql.Contains("@user_name"));
    }

    [TestMethod]
    public void Prepare_BatchUpdate_GeneratesCorrectSql()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id CreatedAt}} WHERE id = @id",
            context);

        Assert.IsTrue(template.Sql.Contains("UPDATE [users] SET"));
        Assert.IsTrue(template.Sql.Contains("[user_name] = @user_name"));
        Assert.IsTrue(template.Sql.Contains("WHERE id = @id"));
    }

    [TestMethod]
    public void Prepare_BatchDelete_GeneratesCorrectSql()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare(
            "DELETE FROM {{table}} WHERE id = @id",
            context);

        Assert.AreEqual("DELETE FROM [users] WHERE id = @id", template.Sql);
    }

    #endregion

    #region Real-World Scenario Tests

    [TestMethod]
    public void Render_SearchWithPagination_GeneratesCorrectSql()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE 1=1 {{if notempty=keyword}}AND ([user_name] LIKE @keyword OR [email] LIKE @keyword){{/if}} {{if notnull=isActive}}AND [is_active] = @isActive{{/if}} ORDER BY [created_at] DESC {{limit --param pageSize}} {{offset --param offset}}",
            context);

        var rendered = template.Render(new Dictionary<string, object?>
        {
            ["keyword"] = "%john%",
            ["isActive"] = true,
            ["pageSize"] = 20,
            ["offset"] = 40
        });

        Assert.IsTrue(rendered.Contains("AND ([user_name] LIKE @keyword OR [email] LIKE @keyword)"));
        Assert.IsTrue(rendered.Contains("AND [is_active] = @isActive"));
        Assert.IsTrue(rendered.Contains("LIMIT 20"));
        Assert.IsTrue(rendered.Contains("OFFSET 40"));
    }

    [TestMethod]
    public void Render_SearchWithoutFilters_GeneratesMinimalSql()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE 1=1 {{if notempty=keyword}}AND [user_name] LIKE @keyword{{/if}} {{if notnull=isActive}}AND [is_active] = @isActive{{/if}}",
            context);

        var rendered = template.Render(new Dictionary<string, object?>
        {
            ["keyword"] = "",
            ["isActive"] = null
        });

        Assert.IsFalse(rendered.Contains("AND [user_name]"));
        Assert.IsFalse(rendered.Contains("AND [is_active]"));
    }

    [TestMethod]
    public void Render_ConditionalJoin_GeneratesCorrectSql()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare(
            "SELECT u.{{columns}} FROM {{table}} u {{if notnull=includeOrders}}LEFT JOIN orders o ON u.id = o.user_id{{/if}} WHERE 1=1",
            context);

        var withJoin = template.Render(new Dictionary<string, object?> { ["includeOrders"] = true });
        Assert.IsTrue(withJoin.Contains("LEFT JOIN orders"));

        var withoutJoin = template.Render(new Dictionary<string, object?> { ["includeOrders"] = null });
        Assert.IsFalse(withoutJoin.Contains("LEFT JOIN orders"));
    }

    [TestMethod]
    public void Render_ComplexBusinessLogic_GeneratesCorrectSql()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare(
            @"SELECT {{columns}} FROM {{table}} 
              WHERE 1=1 
              {{if notempty=searchTerm}}AND ([user_name] LIKE @searchTerm OR [email] LIKE @searchTerm){{/if}}
              {{if notnull=isActive}}AND [is_active] = @isActive{{/if}}
              {{if notnull=createdAfter}}AND [created_at] >= @createdAfter{{/if}}
              {{if notnull=createdBefore}}AND [created_at] <= @createdBefore{{/if}}
              ORDER BY [created_at] DESC
              {{limit --param limit}}",
            context);

        var fullSearch = template.Render(new Dictionary<string, object?>
        {
            ["searchTerm"] = "%admin%",
            ["isActive"] = true,
            ["createdAfter"] = DateTime.Now.AddDays(-30),
            ["createdBefore"] = DateTime.Now,
            ["limit"] = 50
        });

        Assert.IsTrue(fullSearch.Contains("AND ([user_name] LIKE @searchTerm OR [email] LIKE @searchTerm)"));
        Assert.IsTrue(fullSearch.Contains("AND [is_active] = @isActive"));
        Assert.IsTrue(fullSearch.Contains("AND [created_at] >= @createdAfter"));
        Assert.IsTrue(fullSearch.Contains("AND [created_at] <= @createdBefore"));
        Assert.IsTrue(fullSearch.Contains("LIMIT 50"));
    }

    #endregion

    #region Stress Tests

    [TestMethod]
    public void Render_ManyConditionalBlocks_PerformsWell()
    {
        var context = CreateUserContext();
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE 1=1 " +
            "{{if notnull=f1}}AND f1=@f1{{/if}} " +
            "{{if notnull=f2}}AND f2=@f2{{/if}} " +
            "{{if notnull=f3}}AND f3=@f3{{/if}} " +
            "{{if notnull=f4}}AND f4=@f4{{/if}} " +
            "{{if notnull=f5}}AND f5=@f5{{/if}}",
            context);

        for (int i = 0; i < 1000; i++)
        {
            var rendered = template.Render(new Dictionary<string, object?>
            {
                ["f1"] = i % 2 == 0 ? "v1" : null,
                ["f2"] = i % 3 == 0 ? "v2" : null,
                ["f3"] = i % 5 == 0 ? "v3" : null,
                ["f4"] = null,
                ["f5"] = "always",
            });

            Assert.IsTrue(rendered.Contains("AND f5=@f5"));
        }
    }

    #endregion
}
