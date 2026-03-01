// <copyright file="SetPlaceholderUpdateScenarioTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests;

using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Comprehensive UPDATE scenario tests for {{set}} placeholder.
/// </summary>
[TestClass]
public class SetPlaceholderUpdateScenarioTests
{
    private static readonly ColumnMeta[] UserColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int64, false),
        new ColumnMeta("name", "Name", DbType.String, false),
        new ColumnMeta("email", "Email", DbType.String, false),
        new ColumnMeta("version", "Version", DbType.Int32, false),
        new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
        new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
    };

    #region Optimistic Locking Scenarios

    [TestMethod]
    public void UpdateWithOptimisticLocking_IncrementVersion_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id,CreatedAt --inline Version=Version+1,UpdatedAt=CURRENT_TIMESTAMP}} WHERE id = @id AND version = @version",
            context);

        var expected = "UPDATE [users] SET [name] = @name, [email] = @email, [version] = [version]+1, [updated_at] = CURRENT_TIMESTAMP WHERE id = @id AND version = @version";
        Assert.AreEqual(expected, template.Sql);
    }

    [TestMethod]
    public void UpdateWithOptimisticLocking_AllDialects_GeneratesCorrectSQL()
    {
        var dialects = new[]
        {
            SqlDefine.SQLite,
            SqlDefine.PostgreSql,
            SqlDefine.MySql,
            SqlDefine.SqlServer,
            SqlDefine.Oracle,
        };

        foreach (var dialect in dialects)
        {
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id,CreatedAt --inline Version=Version+1}} WHERE id = @id AND version = @version",
                context);

            var sql = template.Sql;
            
            // 验证 SET 子句包含版本递增表达式
            Assert.IsTrue(sql.Contains("SET"), $"Dialect {dialect} should contain SET clause");
            Assert.IsTrue(sql.Contains("WHERE"), $"Dialect {dialect} should contain WHERE clause");
            
            // 验证版本递增表达式存在（不同方言有不同的列包装符号）
            var hasVersionIncrement = sql.Contains("version") && sql.Contains("+1");
            Assert.IsTrue(hasVersionIncrement, $"Dialect {dialect} should contain version increment expression");
        }
    }

    #endregion

    #region Audit Trail Scenarios

    [TestMethod]
    public void UpdateWithAuditTrail_AutoTimestamp_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
            new ColumnMeta("updated_by", "UpdatedBy", DbType.String, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "documents", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline UpdatedAt=CURRENT_TIMESTAMP}} WHERE id = @id",
            context);

        var expected = "UPDATE [documents] SET [name] = @name, [updated_at] = CURRENT_TIMESTAMP, [updated_by] = @updated_by WHERE id = @id";
        Assert.AreEqual(expected, template.Sql);
    }

    [TestMethod]
    public void UpdateWithAuditTrail_MultipleFields_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("status", "Status", DbType.String, false),
            new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
            new ColumnMeta("updated_by", "UpdatedBy", DbType.String, false),
            new ColumnMeta("version", "Version", DbType.Int32, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "orders", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline UpdatedAt=CURRENT_TIMESTAMP,Version=Version+1}} WHERE id = @id",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("[status] = @status"));
        Assert.IsTrue(sql.Contains("[updated_at] = CURRENT_TIMESTAMP"));
        Assert.IsTrue(sql.Contains("[updated_by] = @updated_by"));
        Assert.IsTrue(sql.Contains("[version] = [version]+1"));
    }

    #endregion

    #region Conditional Update Scenarios

    [TestMethod]
    public void UpdateWithMultipleConditions_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id,CreatedAt,UpdatedAt}} WHERE id = @id AND email = @oldEmail AND version = @version",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("WHERE id = @id AND email = @oldEmail AND version = @version"));
        Assert.IsTrue(sql.Contains("[name] = @name"));
        Assert.IsTrue(sql.Contains("[email] = @email"));
        Assert.IsTrue(sql.Contains("[version] = @version"));
    }

    [TestMethod]
    public void UpdateWithInCondition_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id,CreatedAt --inline UpdatedAt=CURRENT_TIMESTAMP}} WHERE id IN (@id1, @id2, @id3)",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("WHERE id IN (@id1, @id2, @id3)"));
        Assert.IsTrue(sql.Contains("[updated_at] = CURRENT_TIMESTAMP"));
    }

    #endregion

    #region Partial Update Scenarios

    [TestMethod]
    public void PartialUpdate_OnlyNameAndEmail_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id,Version,CreatedAt,UpdatedAt}} WHERE id = @id",
            context);

        var expected = "UPDATE [users] SET [name] = @name, [email] = @email WHERE id = @id";
        Assert.AreEqual(expected, template.Sql);
    }

    [TestMethod]
    public void PartialUpdate_OnlyTimestamp_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id,Name,Email,Version,CreatedAt --inline UpdatedAt=CURRENT_TIMESTAMP}} WHERE id = @id",
            context);

        var expected = "UPDATE [users] SET [updated_at] = CURRENT_TIMESTAMP WHERE id = @id";
        Assert.AreEqual(expected, template.Sql);
    }

    #endregion

    #region Computed Field Scenarios

    [TestMethod]
    public void UpdateWithComputedField_CalculateTotal_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("quantity", "Quantity", DbType.Int32, false),
            new ColumnMeta("price", "Price", DbType.Decimal, false),
            new ColumnMeta("total", "Total", DbType.Decimal, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "order_items", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Total=Quantity*Price}} WHERE id = @id",
            context);

        var expected = "UPDATE [order_items] SET [quantity] = @quantity, [price] = @price, [total] = [quantity]*[price] WHERE id = @id";
        Assert.AreEqual(expected, template.Sql);
    }

    [TestMethod]
    public void UpdateWithComputedField_ComplexExpression_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("base_price", "BasePrice", DbType.Decimal, false),
            new ColumnMeta("discount", "Discount", DbType.Decimal, false),
            new ColumnMeta("tax_rate", "TaxRate", DbType.Decimal, false),
            new ColumnMeta("final_price", "FinalPrice", DbType.Decimal, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "products", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline FinalPrice=(BasePrice-Discount)*(1+TaxRate)}} WHERE id = @id",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("[final_price] = ([base_price]-[discount])*(1+[tax_rate])"));
    }

    #endregion

    #region Counter Update Scenarios

    [TestMethod]
    public void UpdateCounter_Increment_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("view_count", "ViewCount", DbType.Int32, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "posts", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline ViewCount=ViewCount+1}} WHERE id = @id",
            context);

        var expected = "UPDATE [posts] SET [view_count] = [view_count]+1 WHERE id = @id";
        Assert.AreEqual(expected, template.Sql);
    }

    [TestMethod]
    public void UpdateCounter_IncrementByParameter_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("points", "Points", DbType.Int32, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Points=Points+@increment}} WHERE id = @id",
            context);

        var expected = "UPDATE [users] SET [points] = [points]+@increment WHERE id = @id";
        Assert.AreEqual(expected, template.Sql);
    }

    [TestMethod]
    public void UpdateCounter_Decrement_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("stock", "Stock", DbType.Int32, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "inventory", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Stock=Stock-@quantity}} WHERE id = @id",
            context);

        var expected = "UPDATE [inventory] SET [stock] = [stock]-@quantity WHERE id = @id";
        Assert.AreEqual(expected, template.Sql);
    }

    #endregion

    #region String Manipulation Scenarios

    [TestMethod]
    public void UpdateWithStringFunction_Uppercase_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("code", "Code", DbType.String, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "products", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Code=UPPER(Code)}} WHERE id = @id",
            context);

        var expected = "UPDATE [products] SET [code] = UPPER([code]) WHERE id = @id";
        Assert.AreEqual(expected, template.Sql);
    }

    [TestMethod]
    public void UpdateWithStringFunction_Trim_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Name=TRIM(Name)}} WHERE id = @id",
            context);

        var expected = "UPDATE [users] SET [name] = TRIM([name]) WHERE id = @id";
        Assert.AreEqual(expected, template.Sql);
    }

    #endregion

    #region NULL Handling Scenarios

    [TestMethod]
    public void UpdateWithCoalesce_DefaultValue_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("status", "Status", DbType.String, true),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "orders", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Status=COALESCE(Status,'pending')}} WHERE id = @id",
            context);

        var expected = "UPDATE [orders] SET [status] = COALESCE([status],'pending') WHERE id = @id";
        Assert.AreEqual(expected, template.Sql);
    }

    #endregion

    #region Real-World Complex Scenarios

    [TestMethod]
    public void RealWorld_UserProfileUpdate_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("email", "Email", DbType.String, false),
            new ColumnMeta("version", "Version", DbType.Int32, false),
            new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
            new ColumnMeta("updated_by", "UpdatedBy", DbType.String, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Version=Version+1,UpdatedAt=CURRENT_TIMESTAMP}} WHERE id = @id AND version = @version",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("[name] = @name"));
        Assert.IsTrue(sql.Contains("[email] = @email"));
        Assert.IsTrue(sql.Contains("[version] = [version]+1"));
        Assert.IsTrue(sql.Contains("[updated_at] = CURRENT_TIMESTAMP"));
        Assert.IsTrue(sql.Contains("[updated_by] = @updated_by"));
        Assert.IsTrue(sql.Contains("WHERE id = @id AND version = @version"));
    }

    [TestMethod]
    public void RealWorld_OrderStatusUpdate_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("status", "Status", DbType.String, false),
            new ColumnMeta("status_changed_at", "StatusChangedAt", DbType.DateTime, false),
            new ColumnMeta("version", "Version", DbType.Int32, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "orders", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline StatusChangedAt=CURRENT_TIMESTAMP,Version=Version+1}} WHERE id = @id",
            context);

        var expected = "UPDATE [orders] SET [status] = @status, [status_changed_at] = CURRENT_TIMESTAMP, [version] = [version]+1 WHERE id = @id";
        Assert.AreEqual(expected, template.Sql);
    }

    [TestMethod]
    public void RealWorld_InventoryDeduction_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("stock", "Stock", DbType.Int32, false),
            new ColumnMeta("reserved", "Reserved", DbType.Int32, false),
            new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "products", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id --inline Stock=Stock-@quantity,Reserved=Reserved+@quantity,UpdatedAt=CURRENT_TIMESTAMP}} WHERE id = @id AND stock >= @quantity",
            context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("[stock] = [stock]-@quantity"));
        Assert.IsTrue(sql.Contains("[reserved] = [reserved]+@quantity"));
        Assert.IsTrue(sql.Contains("[updated_at] = CURRENT_TIMESTAMP"));
        Assert.IsTrue(sql.Contains("WHERE id = @id AND stock >= @quantity"));
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void EdgeCase_UpdateAllColumnsExceptId_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("col1", "Col1", DbType.String, false),
            new ColumnMeta("col2", "Col2", DbType.String, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "test", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id",
            context);

        var expected = "UPDATE [test] SET [col1] = @col1, [col2] = @col2 WHERE id = @id";
        Assert.AreEqual(expected, template.Sql);
    }

    [TestMethod]
    public void EdgeCase_UpdateWithNoWhere_GeneratesCorrectSQL()
    {
        var columns = new[]
        {
            new ColumnMeta("status", "Status", DbType.String, false),
            new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "tasks", columns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --inline UpdatedAt=CURRENT_TIMESTAMP}}",
            context);

        var expected = "UPDATE [tasks] SET [status] = @status, [updated_at] = CURRENT_TIMESTAMP";
        Assert.AreEqual(expected, template.Sql);
    }

    #endregion
}
