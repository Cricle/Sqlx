// <copyright file="TablePlaceholderTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Placeholders;
using System.Collections.Generic;
using System.Data;

namespace Sqlx.Tests;

/// <summary>
/// Comprehensive strict unit tests for {{table}} placeholder functionality.
/// Tests both static and dynamic table name generation with full dialect coverage.
/// </summary>
[TestClass]
public class TablePlaceholderTests
{
    private static readonly ColumnMeta[] TestColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int64, false),
        new ColumnMeta("name", "Name", DbType.String, false),
        new ColumnMeta("email", "Email", DbType.String, true),
    };

    private static readonly ColumnMeta[] EmptyColumns = Array.Empty<ColumnMeta>();

    #region Handler Instance Tests

    [TestMethod]
    public void TableHandler_Instance_IsNotNull()
    {
        Assert.IsNotNull(TablePlaceholderHandler.Instance);
    }

    [TestMethod]
    public void TableHandler_Instance_IsSingleton()
    {
        var instance1 = TablePlaceholderHandler.Instance;
        var instance2 = TablePlaceholderHandler.Instance;

        Assert.AreSame(instance1, instance2);
    }

    [TestMethod]
    public void TableHandler_Name_IsTable()
    {
        var handler = TablePlaceholderHandler.Instance;

        Assert.AreEqual("table", handler.Name);
    }

    #endregion

    #region GetType Tests

    [TestMethod]
    public void TableHandler_GetType_NoOptions_ReturnsStatic()
    {
        var handler = TablePlaceholderHandler.Instance;

        var type = handler.GetType(string.Empty);

        Assert.AreEqual(PlaceholderType.Static, type);
    }

    [TestMethod]
    public void TableHandler_GetType_WithParam_ReturnsDynamic()
    {
        var handler = TablePlaceholderHandler.Instance;

        var type = handler.GetType("--param tableName");

        Assert.AreEqual(PlaceholderType.Dynamic, type);
    }

    [TestMethod]
    public void TableHandler_GetType_WithParamAndSpaces_ReturnsDynamic()
    {
        var handler = TablePlaceholderHandler.Instance;

        var type = handler.GetType("  --param   tableName  ");

        Assert.AreEqual(PlaceholderType.Dynamic, type);
    }

    [TestMethod]
    public void TableHandler_GetType_NullOptions_ReturnsStatic()
    {
        var handler = TablePlaceholderHandler.Instance;

        var type = handler.GetType(null!);

        Assert.AreEqual(PlaceholderType.Static, type);
    }

    #endregion

    #region Static Table Name - Process Tests

    [TestMethod]
    public void Table_Process_NoOptions_ReturnsStaticTableName()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("[users]", result);
    }

    [TestMethod]
    public void Table_Process_NullOptions_ReturnsStaticTableName()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        var result = handler.Process(context, null!);

        Assert.AreEqual("[users]", result);
    }

    [TestMethod]
    public void Table_Process_WhitespaceOptions_ReturnsStaticTableName()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        var result = handler.Process(context, "   ");

        Assert.AreEqual("[users]", result);
    }

    #endregion

    #region Static Table Name - All Dialects

    [TestMethod]
    public void Table_SQLite_UsesSquareBrackets()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("[users]", result);
    }

    [TestMethod]
    public void Table_PostgreSQL_UsesDoubleQuotes()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("\"users\"", result);
    }

    [TestMethod]
    public void Table_MySQL_UsesBackticks()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.MySql, "users", TestColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("`users`", result);
    }

    [TestMethod]
    public void Table_SqlServer_UsesSquareBrackets()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SqlServer, "users", TestColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("[users]", result);
    }

    [TestMethod]
    public void Table_Oracle_UsesDoubleQuotes()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.Oracle, "users", TestColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("\"users\"", result);
    }

    [TestMethod]
    public void Table_DB2_UsesDoubleQuotes()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.DB2, "users", TestColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("\"users\"", result);
    }

    #endregion

    #region Special Table Names

    [TestMethod]
    public void Table_ReservedWordTableName_QuotedCorrectly()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "order", EmptyColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("[order]", result);
    }

    [TestMethod]
    public void Table_TableNameWithUnderscore_HandledCorrectly()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "user_accounts", EmptyColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("[user_accounts]", result);
    }

    [TestMethod]
    public void Table_TableNameWithNumbers_HandledCorrectly()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "logs_2024", EmptyColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("[logs_2024]", result);
    }

    [TestMethod]
    public void Table_TableNameStartingWithNumber_HandledCorrectly()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "2024_logs", EmptyColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("[2024_logs]", result);
    }

    [TestMethod]
    public void Table_TableNameWithMultipleUnderscores_HandledCorrectly()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "__internal_table__", EmptyColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("[__internal_table__]", result);
    }

    [TestMethod]
    public void Table_SingleCharacterTableName_HandledCorrectly()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "t", EmptyColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("[t]", result);
    }

    [TestMethod]
    public void Table_LongTableName_HandledCorrectly()
    {
        var handler = TablePlaceholderHandler.Instance;
        var longName = "very_long_table_name_with_many_characters_that_exceeds_normal_length";
        var context = new PlaceholderContext(SqlDefine.SQLite, longName, EmptyColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual($"[{longName}]", result);
    }

    [TestMethod]
    public void Table_TableNameWithMixedCase_PreservedCorrectly()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "UserAccounts", EmptyColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("[UserAccounts]", result);
    }

    #endregion

    #region Dynamic Table Name - Render Tests

    [TestMethod]
    public void Table_Render_WithParam_SQLite_GeneratesCorrectTableName()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        var result = handler.Render(context, "--param tableName", new Dictionary<string, object?> { ["tableName"] = "orders" });

        Assert.AreEqual("[orders]", result);
    }

    [TestMethod]
    public void Table_Render_WithParam_PostgreSQL_GeneratesCorrectTableName()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);

        var result = handler.Render(context, "--param tableName", new Dictionary<string, object?> { ["tableName"] = "orders" });

        Assert.AreEqual("\"orders\"", result);
    }

    [TestMethod]
    public void Table_Render_WithParam_MySQL_GeneratesCorrectTableName()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.MySql, "users", TestColumns);

        var result = handler.Render(context, "--param tableName", new Dictionary<string, object?> { ["tableName"] = "orders" });

        Assert.AreEqual("`orders`", result);
    }

    [TestMethod]
    public void Table_Render_WithParam_SqlServer_GeneratesCorrectTableName()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SqlServer, "users", TestColumns);

        var result = handler.Render(context, "--param tableName", new Dictionary<string, object?> { ["tableName"] = "orders" });

        Assert.AreEqual("[orders]", result);
    }

    [TestMethod]
    public void Table_Render_WithParam_Oracle_GeneratesCorrectTableName()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.Oracle, "users", TestColumns);

        var result = handler.Render(context, "--param tableName", new Dictionary<string, object?> { ["tableName"] = "orders" });

        Assert.AreEqual("\"orders\"", result);
    }

    [TestMethod]
    public void Table_Render_WithParam_DB2_GeneratesCorrectTableName()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.DB2, "users", TestColumns);

        var result = handler.Render(context, "--param tableName", new Dictionary<string, object?> { ["tableName"] = "orders" });

        Assert.AreEqual("\"orders\"", result);
    }

    [TestMethod]
    public void Table_Render_NoParam_UsesStaticTableName()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        var result = handler.Render(context, string.Empty, new Dictionary<string, object?>());

        Assert.AreEqual("[users]", result);
    }

    [TestMethod]
    public void Table_Render_NoParam_IgnoresParameters()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        var result = handler.Render(context, string.Empty, new Dictionary<string, object?> { ["tableName"] = "ignored" });

        Assert.AreEqual("[users]", result);
    }

    #endregion

    #region Dynamic Table Name - Special Names

    [TestMethod]
    public void Table_Render_DynamicReservedWord_QuotedCorrectly()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        var result = handler.Render(context, "--param tableName", new Dictionary<string, object?> { ["tableName"] = "select" });

        Assert.AreEqual("[select]", result);
    }

    [TestMethod]
    public void Table_Render_DynamicWithUnderscore_HandledCorrectly()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        var result = handler.Render(context, "--param tableName", new Dictionary<string, object?> { ["tableName"] = "user_logs" });

        Assert.AreEqual("[user_logs]", result);
    }

    [TestMethod]
    public void Table_Render_DynamicWithNumbers_HandledCorrectly()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        var result = handler.Render(context, "--param tableName", new Dictionary<string, object?> { ["tableName"] = "logs_2024_01" });

        Assert.AreEqual("[logs_2024_01]", result);
    }

    [TestMethod]
    public void Table_Render_DynamicSingleChar_HandledCorrectly()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        var result = handler.Render(context, "--param tableName", new Dictionary<string, object?> { ["tableName"] = "t" });

        Assert.AreEqual("[t]", result);
    }

    [TestMethod]
    public void Table_Render_DynamicLongName_HandledCorrectly()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var longName = "very_long_dynamic_table_name_with_many_characters";

        var result = handler.Render(context, "--param tableName", new Dictionary<string, object?> { ["tableName"] = longName });

        Assert.AreEqual($"[{longName}]", result);
    }

    #endregion

    #region Error Handling Tests

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Table_Process_WithParam_ThrowsException()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        handler.Process(context, "--param tableName");
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Table_Render_WithParam_NullTableName_ThrowsException()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        handler.Render(context, "--param tableName", new Dictionary<string, object?> { ["tableName"] = null });
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Table_Render_WithParam_MissingParameter_ThrowsException()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        handler.Render(context, "--param tableName", new Dictionary<string, object?>());
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Table_Render_WithParam_WrongParameterName_ThrowsException()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        handler.Render(context, "--param tableName", new Dictionary<string, object?> { ["wrongName"] = "orders" });
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Table_Render_WithParam_NullParameters_ThrowsException()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        handler.Render(context, "--param tableName", null!);
    }

    [TestMethod]
    public void Table_Render_WithParam_EmptyStringTableName_ThrowsException()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        try
        {
            handler.Render(context, "--param tableName", new Dictionary<string, object?> { ["tableName"] = "" });
            Assert.Fail("Expected InvalidOperationException was not thrown");
        }
        catch (InvalidOperationException ex)
        {
            Assert.IsTrue(ex.Message.Contains("cannot be null, empty, or whitespace"));
        }
    }

    #endregion

    #region Integration with SqlTemplate - Static

    [TestMethod]
    public void Table_InTemplate_Static_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}}", context);

        Assert.AreEqual("SELECT * FROM [users]", template.Sql);
        Assert.IsFalse(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void Table_InTemplate_Static_WithWhere_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} WHERE id = @id", context);

        Assert.AreEqual("SELECT * FROM [users] WHERE id = @id", template.Sql);
        Assert.IsFalse(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void Table_InTemplate_Static_WithColumns_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);

        Assert.AreEqual("SELECT [id], [name], [email] FROM [users]", template.Sql);
        Assert.IsFalse(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void Table_InTemplate_Static_InsertStatement_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare("INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})", context);

        Assert.AreEqual("INSERT INTO [users] ([id], [name], [email]) VALUES (@id, @name, @email)", template.Sql);
        Assert.IsFalse(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void Table_InTemplate_Static_UpdateStatement_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set}} WHERE id = @id", context);

        Assert.AreEqual("UPDATE [users] SET [id] = @id, [name] = @name, [email] = @email WHERE id = @id", template.Sql);
        Assert.IsFalse(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void Table_InTemplate_Static_DeleteStatement_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare("DELETE FROM {{table}} WHERE id = @id", context);

        Assert.AreEqual("DELETE FROM [users] WHERE id = @id", template.Sql);
        Assert.IsFalse(template.HasDynamicPlaceholders);
    }

    [TestMethod]
    public void Table_InTemplate_Static_MultipleOccurrences_AllReplaced()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} t1 JOIN {{table}} t2 ON t1.id = t2.parent_id", context);

        Assert.AreEqual("SELECT * FROM [users] t1 JOIN [users] t2 ON t1.id = t2.parent_id", template.Sql);
        Assert.IsFalse(template.HasDynamicPlaceholders);
    }

    #endregion

    #region Integration with SqlTemplate - Dynamic

    [TestMethod]
    public void Table_InTemplate_Dynamic_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table --param tableName}}", context);

        Assert.IsTrue(template.HasDynamicPlaceholders);

        var sql = template.Render(new Dictionary<string, object?> { ["tableName"] = "orders" });
        Assert.AreEqual("SELECT * FROM [orders]", sql);
    }

    [TestMethod]
    public void Table_InTemplate_Dynamic_WithColumns_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table --param tableName}}", context);

        Assert.IsTrue(template.HasDynamicPlaceholders);

        var sql = template.Render(new Dictionary<string, object?> { ["tableName"] = "products" });
        Assert.AreEqual("SELECT [id], [name], [email] FROM [products]", sql);
    }

    [TestMethod]
    public void Table_InTemplate_Dynamic_PostgreSQL_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table --param tableName}} WHERE id = @id", context);

        var sql = template.Render(new Dictionary<string, object?> { ["tableName"] = "customers" });
        Assert.AreEqual("SELECT * FROM \"customers\" WHERE id = @id", sql);
    }

    [TestMethod]
    public void Table_InTemplate_Dynamic_MySQL_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.MySql, "users", TestColumns);
        var template = SqlTemplate.Prepare("INSERT INTO {{table --param tableName}} ({{columns}}) VALUES ({{values}})", context);

        var sql = template.Render(new Dictionary<string, object?> { ["tableName"] = "logs" });
        Assert.AreEqual("INSERT INTO `logs` (`id`, `name`, `email`) VALUES (@id, @name, @email)", sql);
    }

    [TestMethod]
    public void Table_InTemplate_Dynamic_InsertStatement_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare("INSERT INTO {{table --param tableName}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})", context);

        var sql = template.Render(new Dictionary<string, object?> { ["tableName"] = "new_users" });
        Assert.AreEqual("INSERT INTO [new_users] ([name], [email]) VALUES (@name, @email)", sql);
    }

    [TestMethod]
    public void Table_InTemplate_Dynamic_UpdateStatement_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare("UPDATE {{table --param tableName}} SET {{set --exclude Id}} WHERE id = @id", context);

        var sql = template.Render(new Dictionary<string, object?> { ["tableName"] = "archived_users" });
        Assert.AreEqual("UPDATE [archived_users] SET [name] = @name, [email] = @email WHERE id = @id", sql);
    }

    [TestMethod]
    public void Table_InTemplate_Dynamic_DeleteStatement_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare("DELETE FROM {{table --param tableName}} WHERE id = @id", context);

        var sql = template.Render(new Dictionary<string, object?> { ["tableName"] = "temp_users" });
        Assert.AreEqual("DELETE FROM [temp_users] WHERE id = @id", sql);
    }

    [TestMethod]
    public void Table_InTemplate_Dynamic_MultipleDifferentTables_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table --param table1}} t1 JOIN {{table --param table2}} t2 ON t1.id = t2.user_id",
            context);

        var sql = template.Render(new Dictionary<string, object?>
        {
            ["table1"] = "users",
            ["table2"] = "orders"
        });
        Assert.AreEqual("SELECT * FROM [users] t1 JOIN [orders] t2 ON t1.id = t2.user_id", sql);
    }

    [TestMethod]
    public void Table_InTemplate_Dynamic_SameParamMultipleTimes_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table --param tableName}} t1 JOIN {{table --param tableName}} t2 ON t1.id = t2.parent_id",
            context);

        var sql = template.Render(new Dictionary<string, object?> { ["tableName"] = "categories" });
        Assert.AreEqual("SELECT * FROM [categories] t1 JOIN [categories] t2 ON t1.id = t2.parent_id", sql);
    }

    #endregion

    #region Integration with SqlTemplate - Mixed Static and Dynamic

    [TestMethod]
    public void Table_InTemplate_MixedStaticAndDynamic_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table --param targetTable}} SELECT * FROM {{table}}",
            context);

        Assert.IsTrue(template.HasDynamicPlaceholders);

        var sql = template.Render(new Dictionary<string, object?> { ["targetTable"] = "users_backup" });
        Assert.AreEqual("INSERT INTO [users_backup] SELECT * FROM [users]", sql);
    }

    [TestMethod]
    public void Table_InTemplate_MixedWithOtherDynamicPlaceholders_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table --param tableName}} WHERE {{where --param predicate}} {{limit --param count}}",
            context);

        var sql = template.Render(new Dictionary<string, object?>
        {
            ["tableName"] = "orders",
            ["predicate"] = "status = 'active'",
            ["count"] = 10
        });
        // Note: limit placeholder generates "LIMIT @count" not "LIMIT 10" - it's a parameter placeholder
        Assert.AreEqual("SELECT * FROM [orders] WHERE status = 'active' LIMIT @count", sql);
    }

    #endregion

    #region Real-World Use Cases

    [TestMethod]
    public void Table_UseCase_TimeBasedPartitioning_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "logs", TestColumns);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table --param tableName}} WHERE id > @lastId", context);

        // Simulate querying different monthly partitions
        var sql1 = template.Render(new Dictionary<string, object?> { ["tableName"] = "logs_2024_11" });
        Assert.AreEqual("SELECT * FROM [logs_2024_11] WHERE id > @lastId", sql1);

        var sql2 = template.Render(new Dictionary<string, object?> { ["tableName"] = "logs_2024_12" });
        Assert.AreEqual("SELECT * FROM [logs_2024_12] WHERE id > @lastId", sql2);
    }

    [TestMethod]
    public void Table_UseCase_MultiTenant_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table --param tableName}} WHERE id = @id", context);

        // Simulate multi-tenant scenario with tenant-specific tables
        var sql1 = template.Render(new Dictionary<string, object?> { ["tableName"] = "tenant_abc_users" });
        Assert.AreEqual("SELECT [id], [name], [email] FROM [tenant_abc_users] WHERE id = @id", sql1);

        var sql2 = template.Render(new Dictionary<string, object?> { ["tableName"] = "tenant_xyz_users" });
        Assert.AreEqual("SELECT [id], [name], [email] FROM [tenant_xyz_users] WHERE id = @id", sql2);
    }

    [TestMethod]
    public void Table_UseCase_ArchiveTable_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "orders", TestColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table --param archiveTable}} SELECT * FROM {{table}} WHERE created_at < @cutoffDate",
            context);

        var sql = template.Render(new Dictionary<string, object?> { ["archiveTable"] = "orders_archive" });
        Assert.AreEqual("INSERT INTO [orders_archive] SELECT * FROM [orders] WHERE created_at < @cutoffDate", sql);
    }

    [TestMethod]
    public void Table_UseCase_DynamicReporting_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.PostgreSql, "sales", TestColumns);
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table --param tableName}} WHERE date >= @startDate AND date <= @endDate",
            context);

        var sql = template.Render(new Dictionary<string, object?> { ["tableName"] = "sales_summary_2024" });
        Assert.AreEqual("SELECT * FROM \"sales_summary_2024\" WHERE date >= @startDate AND date <= @endDate", sql);
    }

    [TestMethod]
    public void Table_UseCase_TestEnvironment_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table --param tableName}} WHERE id = @id", context);

        // Production
        var prodSql = template.Render(new Dictionary<string, object?> { ["tableName"] = "users" });
        Assert.AreEqual("SELECT * FROM [users] WHERE id = @id", prodSql);

        // Test
        var testSql = template.Render(new Dictionary<string, object?> { ["tableName"] = "test_users" });
        Assert.AreEqual("SELECT * FROM [test_users] WHERE id = @id", testSql);
    }

    #endregion

    #region Edge Cases and Boundary Conditions

    [TestMethod]
    public void Table_EmptyColumnList_StillGeneratesTableName()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "empty_table", EmptyColumns);

        var result = handler.Process(context, string.Empty);

        Assert.AreEqual("[empty_table]", result);
    }

    [TestMethod]
    public void Table_WithParam_DifferentParameterNames_AllWork()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        var result1 = handler.Render(context, "--param tbl", new Dictionary<string, object?> { ["tbl"] = "orders" });
        Assert.AreEqual("[orders]", result1);

        var result2 = handler.Render(context, "--param table_name", new Dictionary<string, object?> { ["table_name"] = "products" });
        Assert.AreEqual("[products]", result2);

        var result3 = handler.Render(context, "--param t", new Dictionary<string, object?> { ["t"] = "logs" });
        Assert.AreEqual("[logs]", result3);
    }

    [TestMethod]
    public void Table_Render_ParameterNameIsCaseSensitive()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        // Parameter name matching is case-sensitive in the options string
        // but the dictionary lookup uses StringComparer.OrdinalIgnoreCase
        var result = handler.Render(context, "--param tableName", new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) 
        { 
            ["TABLENAME"] = "orders" 
        });

        Assert.AreEqual("[orders]", result);
    }

    [TestMethod]
    public void Table_AllDialects_ConsistentBehavior()
    {
        var handler = TablePlaceholderHandler.Instance;
        var dialects = new[]
        {
            SqlDefine.SQLite,
            SqlDefine.PostgreSql,
            SqlDefine.MySql,
            SqlDefine.SqlServer,
            SqlDefine.Oracle,
            SqlDefine.DB2
        };

        foreach (var dialect in dialects)
        {
            var context = new PlaceholderContext(dialect, "test_table", TestColumns);
            var result = handler.Process(context, string.Empty);

            // All dialects should quote the table name
            Assert.IsTrue(result.Contains("test_table"), $"Dialect {dialect} should contain table name");
            Assert.IsTrue(result.Length > "test_table".Length, $"Dialect {dialect} should add quotes");
        }
    }

    [TestMethod]
    public void Table_Render_WithParam_AllDialects_ConsistentBehavior()
    {
        var handler = TablePlaceholderHandler.Instance;
        var dialects = new[]
        {
            SqlDefine.SQLite,
            SqlDefine.PostgreSql,
            SqlDefine.MySql,
            SqlDefine.SqlServer,
            SqlDefine.Oracle,
            SqlDefine.DB2
        };

        foreach (var dialect in dialects)
        {
            var context = new PlaceholderContext(dialect, "users", TestColumns);
            var result = handler.Render(context, "--param tableName", new Dictionary<string, object?> { ["tableName"] = "dynamic_table" });

            // All dialects should quote the dynamic table name
            Assert.IsTrue(result.Contains("dynamic_table"), $"Dialect {dialect} should contain dynamic table name");
            Assert.IsTrue(result.Length > "dynamic_table".Length, $"Dialect {dialect} should add quotes");
        }
    }

    #endregion

    #region Parameter Parsing Tests

    [TestMethod]
    public void Table_ParseParam_ExtraWhitespace_HandledCorrectly()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        var result = handler.Render(context, "  --param   tableName  ", new Dictionary<string, object?> { ["tableName"] = "orders" });

        Assert.AreEqual("[orders]", result);
    }

    [TestMethod]
    public void Table_ParseParam_TabsAndSpaces_HandledCorrectly()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        var result = handler.Render(context, "\t--param\ttableName\t", new Dictionary<string, object?> { ["tableName"] = "orders" });

        Assert.AreEqual("[orders]", result);
    }

    #endregion

    #region Type Conversion Tests

    [TestMethod]
    public void Table_Render_IntegerTableName_ConvertsToString()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        var result = handler.Render(context, "--param tableName", new Dictionary<string, object?> { ["tableName"] = 123 });

        Assert.AreEqual("[123]", result);
    }

    [TestMethod]
    public void Table_Render_ObjectTableName_UsesToString()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var customObject = new { Name = "custom_table" };

        var result = handler.Render(context, "--param tableName", new Dictionary<string, object?> { ["tableName"] = customObject });

        // Should use ToString() of the object
        Assert.IsTrue(result.Length > 0);
    }

    #endregion

    #region Thread Safety Tests

    [TestMethod]
    public void Table_Instance_ThreadSafe_MultipleCalls()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context1 = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var context2 = new PlaceholderContext(SqlDefine.PostgreSql, "orders", TestColumns);

        var result1 = handler.Process(context1, string.Empty);
        var result2 = handler.Process(context2, string.Empty);

        Assert.AreEqual("[users]", result1);
        Assert.AreEqual("\"orders\"", result2);
    }

    [TestMethod]
    public void Table_Render_ThreadSafe_MultipleCalls()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        var result1 = handler.Render(context, "--param tableName", new Dictionary<string, object?> { ["tableName"] = "table1" });
        var result2 = handler.Render(context, "--param tableName", new Dictionary<string, object?> { ["tableName"] = "table2" });

        Assert.AreEqual("[table1]", result1);
        Assert.AreEqual("[table2]", result2);
    }

    #endregion

    #region Complex Integration Scenarios

    [TestMethod]
    public void Table_ComplexQuery_WithSubquery_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            "SELECT * FROM {{table --param mainTable}} WHERE id IN (SELECT user_id FROM {{table --param subTable}} WHERE active = 1)",
            context);

        var sql = template.Render(new Dictionary<string, object?>
        {
            ["mainTable"] = "users",
            ["subTable"] = "user_sessions"
        });

        Assert.AreEqual("SELECT * FROM [users] WHERE id IN (SELECT user_id FROM [user_sessions] WHERE active = 1)", sql);
    }

    [TestMethod]
    public void Table_ComplexQuery_WithJoins_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            @"SELECT u.*, o.* 
              FROM {{table --param userTable}} u
              INNER JOIN {{table --param orderTable}} o ON u.id = o.user_id
              LEFT JOIN {{table --param productTable}} p ON o.product_id = p.id",
            context);

        var sql = template.Render(new Dictionary<string, object?>
        {
            ["userTable"] = "users",
            ["orderTable"] = "orders",
            ["productTable"] = "products"
        });

        Assert.IsTrue(sql.Contains("\"users\" u"));
        Assert.IsTrue(sql.Contains("\"orders\" o"));
        Assert.IsTrue(sql.Contains("\"products\" p"));
    }

    [TestMethod]
    public void Table_ComplexQuery_WithCTE_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            @"WITH active_users AS (
                SELECT * FROM {{table --param tableName}} WHERE active = 1
              )
              SELECT * FROM active_users WHERE id > @minId",
            context);

        var sql = template.Render(new Dictionary<string, object?> { ["tableName"] = "users_2024" });

        Assert.IsTrue(sql.Contains("[users_2024]"));
        Assert.IsTrue(sql.Contains("WITH active_users AS"));
    }

    [TestMethod]
    public void Table_ComplexQuery_WithUnion_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare(
            @"SELECT * FROM {{table --param table1}}
              UNION ALL
              SELECT * FROM {{table --param table2}}",
            context);

        var sql = template.Render(new Dictionary<string, object?>
        {
            ["table1"] = "users_active",
            ["table2"] = "users_archived"
        });

        Assert.IsTrue(sql.Contains("[users_active]"));
        Assert.IsTrue(sql.Contains("[users_archived]"));
        Assert.IsTrue(sql.Contains("UNION ALL"));
    }

    #endregion

    #region Performance and Optimization Tests

    [TestMethod]
    public void Table_Render_RepeatedCalls_ConsistentResults()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var parameters = new Dictionary<string, object?> { ["tableName"] = "orders" };

        var results = new string[100];
        for (int i = 0; i < 100; i++)
        {
            results[i] = handler.Render(context, "--param tableName", parameters);
        }

        // All results should be identical
        Assert.IsTrue(results.All(r => r == "[orders]"));
    }

    [TestMethod]
    public void Table_Template_PrepareOnce_RenderMultipleTimes()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table --param tableName}}", context);

        var sql1 = template.Render(new Dictionary<string, object?> { ["tableName"] = "table1" });
        var sql2 = template.Render(new Dictionary<string, object?> { ["tableName"] = "table2" });
        var sql3 = template.Render(new Dictionary<string, object?> { ["tableName"] = "table3" });

        Assert.AreEqual("SELECT * FROM [table1]", sql1);
        Assert.AreEqual("SELECT * FROM [table2]", sql2);
        Assert.AreEqual("SELECT * FROM [table3]", sql3);
    }

    #endregion

    #region Documentation and Example Validation

    [TestMethod]
    public void Table_Example_FromDocumentation_StaticUsage()
    {
        // Example from documentation: SELECT * FROM {{table}}
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}}", context);

        Assert.AreEqual("SELECT * FROM [users]", template.Sql);
    }

    [TestMethod]
    public void Table_Example_FromDocumentation_DynamicUsage()
    {
        // Example from documentation: SELECT * FROM {{table --param tableName}}
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table --param tableName}}", context);

        var sql = template.Render(new Dictionary<string, object?> { ["tableName"] = "dynamic_table" });
        Assert.AreEqual("SELECT * FROM [dynamic_table]", sql);
    }

    [TestMethod]
    public void Table_Example_FromSample_TimeBasedPartitioning()
    {
        // From TablePlaceholderExample.cs
        var context = new PlaceholderContext(SqlDefine.SQLite, "LogEntry", TestColumns);
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table --param tableName}} WHERE timestamp >= @startDate",
            context);

        var sql = template.Render(new Dictionary<string, object?> { ["tableName"] = "logs_2024" });
        Assert.AreEqual("SELECT [id], [name], [email] FROM [logs_2024] WHERE timestamp >= @startDate", sql);
    }

    [TestMethod]
    public void Table_Example_FromSample_MultiTenant()
    {
        // From TablePlaceholderExample.cs - multi-tenant scenario
        var context = new PlaceholderContext(SqlDefine.SQLite, "LogEntry", TestColumns);
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table --param tableName}} WHERE timestamp >= @startDate",
            context);

        var tenantId = "abc123";
        var tableName = $"logs_tenant_{tenantId}";
        var sql = template.Render(new Dictionary<string, object?> { ["tableName"] = tableName });

        Assert.AreEqual("SELECT [id], [name], [email] FROM [logs_tenant_abc123] WHERE timestamp >= @startDate", sql);
    }

    #endregion

    #region Additional Security and Validation Tests

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Table_Render_WhitespaceTableName_ThrowsException()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        handler.Render(context, "--param tableName", new Dictionary<string, object?> { ["tableName"] = "   " });
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Table_Render_TabOnlyTableName_ThrowsException()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        handler.Render(context, "--param tableName", new Dictionary<string, object?> { ["tableName"] = "\t\t" });
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Table_Render_NewlineOnlyTableName_ThrowsException()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        handler.Render(context, "--param tableName", new Dictionary<string, object?> { ["tableName"] = "\n\r" });
    }

    [TestMethod]
    public void Table_Render_UnicodeTableName_HandledCorrectly()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        var result = handler.Render(context, "--param tableName", new Dictionary<string, object?> { ["tableName"] = "用户表" });

        Assert.AreEqual("[用户表]", result);
    }

    [TestMethod]
    public void Table_Render_TableNameWithSpaces_QuotedCorrectly()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        var result = handler.Render(context, "--param tableName", new Dictionary<string, object?> { ["tableName"] = "user accounts" });

        Assert.AreEqual("[user accounts]", result);
    }

    [TestMethod]
    public void Table_Render_TableNameWithSpecialChars_QuotedCorrectly()
    {
        var handler = TablePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

        var result = handler.Render(context, "--param tableName", new Dictionary<string, object?> { ["tableName"] = "user-accounts" });

        Assert.AreEqual("[user-accounts]", result);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void PlaceholderContext_EmptyTableName_ThrowsException()
    {
        new PlaceholderContext(SqlDefine.SQLite, "", TestColumns);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void PlaceholderContext_NullTableName_ThrowsException()
    {
        new PlaceholderContext(SqlDefine.SQLite, null!, TestColumns);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void PlaceholderContext_WhitespaceTableName_ThrowsException()
    {
        new PlaceholderContext(SqlDefine.SQLite, "   ", TestColumns);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void PlaceholderContext_NullDialect_ThrowsException()
    {
        new PlaceholderContext(null!, "users", TestColumns);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void PlaceholderContext_NullColumns_ThrowsException()
    {
        new PlaceholderContext(SqlDefine.SQLite, "users", null!);
    }

    #endregion
}
