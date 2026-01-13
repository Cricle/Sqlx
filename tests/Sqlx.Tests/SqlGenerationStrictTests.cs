using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System.Data;

namespace Sqlx.Tests;

/// <summary>
/// Strict SQL generation tests that verify exact SQL output for all dialects.
/// These tests ensure SqlTemplate generates correct SQL for each database dialect.
/// </summary>
[TestClass]
public class SqlGenerationStrictTests
{
    private static readonly IReadOnlyList<ColumnMeta> UserColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int64, false),
        new ColumnMeta("user_name", "UserName", DbType.String, false),
        new ColumnMeta("email", "Email", DbType.String, true),
        new ColumnMeta("is_active", "IsActive", DbType.Boolean, false),
        new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
    };

    #region SQLite Strict Tests

    [TestMethod]
    public void SQLite_Select_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        Assert.AreEqual(
            "SELECT [id], [user_name], [email], [is_active], [created_at] FROM [users]",
            template.Sql);
    }

    [TestMethod]
    public void SQLite_SelectWithWhere_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE is_active = @isActive",
            context);
        
        Assert.AreEqual(
            "SELECT [id], [user_name], [email], [is_active], [created_at] FROM [users] WHERE is_active = @isActive",
            template.Sql);
    }

    [TestMethod]
    public void SQLite_Insert_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})",
            context);
        
        Assert.AreEqual(
            "INSERT INTO [users] ([user_name], [email], [is_active], [created_at]) VALUES (@user_name, @email, @is_active, @created_at)",
            template.Sql);
    }

    [TestMethod]
    public void SQLite_Update_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id",
            context);
        
        Assert.AreEqual(
            "UPDATE [users] SET [user_name] = @user_name, [email] = @email, [is_active] = @is_active, [created_at] = @created_at WHERE id = @id",
            template.Sql);
    }

    [TestMethod]
    public void SQLite_Delete_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "DELETE FROM {{table}} WHERE id = @id",
            context);
        
        Assert.AreEqual(
            "DELETE FROM [users] WHERE id = @id",
            template.Sql);
    }

    [TestMethod]
    public void SQLite_SelectWithStaticLimit_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} {{limit --count 10}}",
            context);
        
        Assert.AreEqual(
            "SELECT [id], [user_name], [email], [is_active], [created_at] FROM [users] LIMIT 10",
            template.Sql);
    }

    [TestMethod]
    public void SQLite_SelectWithDynamicLimitOffset_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} {{limit --param limit}} {{offset --param offset}}",
            context);
        
        var rendered = template.Render(new Dictionary<string, object?> { ["limit"] = 20, ["offset"] = 40 });
        
        Assert.AreEqual(
            "SELECT [id], [user_name], [email], [is_active], [created_at] FROM [users] LIMIT 20 OFFSET 40",
            rendered);
    }

    [TestMethod]
    public void SQLite_SelectWithDynamicWhere_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}",
            context);
        
        var rendered = template.Render(new Dictionary<string, object?> { ["predicate"] = "is_active = 1 AND email IS NOT NULL" });
        
        Assert.AreEqual(
            "SELECT [id], [user_name], [email], [is_active], [created_at] FROM [users] WHERE is_active = 1 AND email IS NOT NULL",
            rendered);
    }

    #endregion

    #region MySQL Strict Tests

    [TestMethod]
    public void MySQL_Select_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.MySql, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        Assert.AreEqual(
            "SELECT `id`, `user_name`, `email`, `is_active`, `created_at` FROM `users`",
            template.Sql);
    }

    [TestMethod]
    public void MySQL_Insert_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.MySql, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})",
            context);
        
        Assert.AreEqual(
            "INSERT INTO `users` (`user_name`, `email`, `is_active`, `created_at`) VALUES (@user_name, @email, @is_active, @created_at)",
            template.Sql);
    }

    [TestMethod]
    public void MySQL_Update_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.MySql, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id",
            context);
        
        Assert.AreEqual(
            "UPDATE `users` SET `user_name` = @user_name, `email` = @email, `is_active` = @is_active, `created_at` = @created_at WHERE id = @id",
            template.Sql);
    }

    [TestMethod]
    public void MySQL_SelectWithPagination_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.MySql, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} {{limit --param limit}} {{offset --param offset}}",
            context);
        
        var rendered = template.Render(new Dictionary<string, object?> { ["limit"] = 10, ["offset"] = 20 });
        
        Assert.AreEqual(
            "SELECT `id`, `user_name`, `email`, `is_active`, `created_at` FROM `users` LIMIT 10 OFFSET 20",
            rendered);
    }

    #endregion

    #region PostgreSQL Strict Tests

    [TestMethod]
    public void PostgreSQL_Select_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        Assert.AreEqual(
            "SELECT \"id\", \"user_name\", \"email\", \"is_active\", \"created_at\" FROM \"users\"",
            template.Sql);
    }

    [TestMethod]
    public void PostgreSQL_Insert_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})",
            context);
        
        Assert.AreEqual(
            "INSERT INTO \"users\" (\"user_name\", \"email\", \"is_active\", \"created_at\") VALUES (@user_name, @email, @is_active, @created_at)",
            template.Sql);
    }

    [TestMethod]
    public void PostgreSQL_Update_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id",
            context);
        
        Assert.AreEqual(
            "UPDATE \"users\" SET \"user_name\" = @user_name, \"email\" = @email, \"is_active\" = @is_active, \"created_at\" = @created_at WHERE id = @id",
            template.Sql);
    }

    [TestMethod]
    public void PostgreSQL_SelectWithPagination_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} {{limit --param limit}} {{offset --param offset}}",
            context);
        
        var rendered = template.Render(new Dictionary<string, object?> { ["limit"] = 15, ["offset"] = 30 });
        
        Assert.AreEqual(
            "SELECT \"id\", \"user_name\", \"email\", \"is_active\", \"created_at\" FROM \"users\" LIMIT 15 OFFSET 30",
            rendered);
    }

    #endregion

    #region SQL Server Strict Tests

    [TestMethod]
    public void SqlServer_Select_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.SqlServer, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        Assert.AreEqual(
            "SELECT [id], [user_name], [email], [is_active], [created_at] FROM [users]",
            template.Sql);
    }

    [TestMethod]
    public void SqlServer_Insert_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.SqlServer, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})",
            context);
        
        Assert.AreEqual(
            "INSERT INTO [users] ([user_name], [email], [is_active], [created_at]) VALUES (@user_name, @email, @is_active, @created_at)",
            template.Sql);
    }

    [TestMethod]
    public void SqlServer_Update_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.SqlServer, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id",
            context);
        
        Assert.AreEqual(
            "UPDATE [users] SET [user_name] = @user_name, [email] = @email, [is_active] = @is_active, [created_at] = @created_at WHERE id = @id",
            template.Sql);
    }

    [TestMethod]
    public void SqlServer_SelectWithPagination_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.SqlServer, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} {{limit --param limit}} {{offset --param offset}}",
            context);
        
        var rendered = template.Render(new Dictionary<string, object?> { ["limit"] = 10, ["offset"] = 20 });
        
        // SQL Server uses standard LIMIT/OFFSET in placeholder handlers
        Assert.AreEqual(
            "SELECT [id], [user_name], [email], [is_active], [created_at] FROM [users] LIMIT 10 OFFSET 20",
            rendered);
    }

    #endregion

    #region Oracle Strict Tests

    [TestMethod]
    public void Oracle_Select_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.Oracle, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        Assert.AreEqual(
            "SELECT \"id\", \"user_name\", \"email\", \"is_active\", \"created_at\" FROM \"users\"",
            template.Sql);
    }

    [TestMethod]
    public void Oracle_Insert_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.Oracle, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})",
            context);
        
        Assert.AreEqual(
            "INSERT INTO \"users\" (\"user_name\", \"email\", \"is_active\", \"created_at\") VALUES (@user_name, @email, @is_active, @created_at)",
            template.Sql);
    }

    [TestMethod]
    public void Oracle_Update_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.Oracle, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id",
            context);
        
        Assert.AreEqual(
            "UPDATE \"users\" SET \"user_name\" = @user_name, \"email\" = @email, \"is_active\" = @is_active, \"created_at\" = @created_at WHERE id = @id",
            template.Sql);
    }

    #endregion

    #region DB2 Strict Tests

    [TestMethod]
    public void DB2_Select_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.DB2, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        Assert.AreEqual(
            "SELECT \"id\", \"user_name\", \"email\", \"is_active\", \"created_at\" FROM \"users\"",
            template.Sql);
    }

    [TestMethod]
    public void DB2_Insert_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.DB2, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})",
            context);
        
        Assert.AreEqual(
            "INSERT INTO \"users\" (\"user_name\", \"email\", \"is_active\", \"created_at\") VALUES (@user_name, @email, @is_active, @created_at)",
            template.Sql);
    }

    [TestMethod]
    public void DB2_Update_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.DB2, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id",
            context);
        
        Assert.AreEqual(
            "UPDATE \"users\" SET \"user_name\" = @user_name, \"email\" = @email, \"is_active\" = @is_active, \"created_at\" = @created_at WHERE id = @id",
            template.Sql);
    }

    #endregion

    #region Complex Query Strict Tests

    [TestMethod]
    public void AllDialects_ComplexQuery_ExactSql()
    {
        var dialects = new[]
        {
            (SqlDefine.SQLite, "[", "]"),
            (SqlDefine.MySql, "`", "`"),
            (SqlDefine.PostgreSql, "\"", "\""),
            (SqlDefine.SqlServer, "[", "]"),
            (SqlDefine.Oracle, "\"", "\""),
            (SqlDefine.DB2, "\"", "\""),
        };

        foreach (var (dialect, left, right) in dialects)
        {
            var context = new PlaceholderContext(dialect, "orders", new[]
            {
                new ColumnMeta("id", "Id", DbType.Int64, false),
                new ColumnMeta("user_id", "UserId", DbType.Int64, false),
                new ColumnMeta("total", "Total", DbType.Decimal, false),
                new ColumnMeta("status", "Status", DbType.String, false),
            });

            var template = SqlTemplate.Prepare(
                "SELECT {{columns}} FROM {{table}} WHERE {{where --param filter}} ORDER BY id {{limit --param limit}}",
                context);

            var rendered = template.Render(new Dictionary<string, object?>
            {
                ["filter"] = "status = 'completed'",
                ["limit"] = 100
            });

            var expected = $"SELECT {left}id{right}, {left}user_id{right}, {left}total{right}, {left}status{right} FROM {left}orders{right} WHERE status = 'completed' ORDER BY id LIMIT 100";
            Assert.AreEqual(expected, rendered, $"Failed for dialect: {dialect.DatabaseType}");
        }
    }

    [TestMethod]
    public void AllDialects_InsertWithExclude_ExactSql()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
        };

        var testCases = new[]
        {
            (SqlDefine.SQLite, "INSERT INTO [items] ([name], [created_at]) VALUES (@name, @created_at)"),
            (SqlDefine.MySql, "INSERT INTO `items` (`name`, `created_at`) VALUES (@name, @created_at)"),
            (SqlDefine.PostgreSql, "INSERT INTO \"items\" (\"name\", \"created_at\") VALUES (@name, @created_at)"),
            (SqlDefine.SqlServer, "INSERT INTO [items] ([name], [created_at]) VALUES (@name, @created_at)"),
            (SqlDefine.Oracle, "INSERT INTO \"items\" (\"name\", \"created_at\") VALUES (@name, @created_at)"),
            (SqlDefine.DB2, "INSERT INTO \"items\" (\"name\", \"created_at\") VALUES (@name, @created_at)"),
        };

        foreach (var (dialect, expected) in testCases)
        {
            var context = new PlaceholderContext(dialect, "items", columns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})",
                context);

            Assert.AreEqual(expected, template.Sql, $"Failed for dialect: {dialect.DatabaseType}");
        }
    }

    [TestMethod]
    public void AllDialects_UpdateWithExclude_ExactSql()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
        };

        var testCases = new[]
        {
            (SqlDefine.SQLite, "UPDATE [items] SET [name] = @name, [updated_at] = @updated_at WHERE id = @id"),
            (SqlDefine.MySql, "UPDATE `items` SET `name` = @name, `updated_at` = @updated_at WHERE id = @id"),
            (SqlDefine.PostgreSql, "UPDATE \"items\" SET \"name\" = @name, \"updated_at\" = @updated_at WHERE id = @id"),
            (SqlDefine.SqlServer, "UPDATE [items] SET [name] = @name, [updated_at] = @updated_at WHERE id = @id"),
            (SqlDefine.Oracle, "UPDATE \"items\" SET \"name\" = @name, \"updated_at\" = @updated_at WHERE id = @id"),
            (SqlDefine.DB2, "UPDATE \"items\" SET \"name\" = @name, \"updated_at\" = @updated_at WHERE id = @id"),
        };

        foreach (var (dialect, expected) in testCases)
        {
            var context = new PlaceholderContext(dialect, "items", columns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id",
                context);

            Assert.AreEqual(expected, template.Sql, $"Failed for dialect: {dialect.DatabaseType}");
        }
    }

    #endregion

    #region Edge Cases Strict Tests

    [TestMethod]
    public void SingleColumn_Select_ExactSql()
    {
        var columns = new[] { new ColumnMeta("id", "Id", DbType.Int64, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "items", columns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        Assert.AreEqual("SELECT [id] FROM [items]", template.Sql);
    }

    [TestMethod]
    public void ExcludeAllButOne_Insert_ExactSql()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
        };
        var context = new PlaceholderContext(SqlDefine.SQLite, "items", columns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})",
            context);
        
        Assert.AreEqual("INSERT INTO [items] ([name]) VALUES (@name)", template.Sql);
    }

    [TestMethod]
    public void MultipleExclude_Update_ExactSql()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
            new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
        };
        var context = new PlaceholderContext(SqlDefine.SQLite, "items", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id,CreatedAt}} WHERE id = @id",
            context);
        
        Assert.AreEqual("UPDATE [items] SET [name] = @name, [updated_at] = @updated_at WHERE id = @id", template.Sql);
    }

    [TestMethod]
    public void ReservedWordTable_Select_ExactSql()
    {
        var columns = new[] { new ColumnMeta("id", "Id", DbType.Int64, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "order", columns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        Assert.AreEqual("SELECT [id] FROM [order]", template.Sql);
    }

    [TestMethod]
    public void ReservedWordColumn_Select_ExactSql()
    {
        var columns = new[]
        {
            new ColumnMeta("select", "Select", DbType.String, false),
            new ColumnMeta("from", "From", DbType.String, false),
        };
        var context = new PlaceholderContext(SqlDefine.SQLite, "keywords", columns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        Assert.AreEqual("SELECT [select], [from] FROM [keywords]", template.Sql);
    }

    #endregion
}
