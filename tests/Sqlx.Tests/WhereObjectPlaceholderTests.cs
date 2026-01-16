// <copyright file="WhereObjectPlaceholderTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using Sqlx.Placeholders;
using System.Data;

namespace Sqlx.Tests;

/// <summary>
/// Strict unit tests for {{where --object}} placeholder functionality.
/// Tests dictionary-based WHERE clause generation with AOT compatibility.
/// </summary>
[TestClass]
public class WhereObjectPlaceholderTests
{
    private static readonly ColumnMeta[] UserColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int64, false),
        new ColumnMeta("name", "Name", DbType.String, false),
        new ColumnMeta("email", "Email", DbType.String, true),
        new ColumnMeta("age", "Age", DbType.Int32, true),
        new ColumnMeta("is_active", "IsActive", DbType.Boolean, false),
    };

    #region Basic Functionality Tests

    [TestMethod]
    public void WhereObject_SingleProperty_GeneratesCorrectSql()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var filter = new Dictionary<string, object?> { ["Name"] = "John" };

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("[name] = @name", result);
    }

    [TestMethod]
    public void WhereObject_TwoProperties_GeneratesAndConditionWithParentheses()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var filter = new Dictionary<string, object?> { ["Name"] = "John", ["Age"] = 25 };

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("([name] = @name AND [age] = @age)", result);
    }

    [TestMethod]
    public void WhereObject_ThreeProperties_GeneratesMultipleAndConditions()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var filter = new Dictionary<string, object?>
        {
            ["Name"] = "John",
            ["Age"] = 25,
            ["IsActive"] = true
        };

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("([name] = @name AND [age] = @age AND [is_active] = @is_active)", result);
    }

    [TestMethod]
    public void WhereObject_AllFiveProperties_GeneratesAllConditions()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var filter = new Dictionary<string, object?>
        {
            ["Id"] = 1L,
            ["Name"] = "John",
            ["Email"] = "john@test.com",
            ["Age"] = 25,
            ["IsActive"] = true
        };

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("([id] = @id AND [name] = @name AND [email] = @email AND [age] = @age AND [is_active] = @is_active)", result);
    }

    #endregion

    #region Null Handling Tests

    [TestMethod]
    public void WhereObject_NullObject_Returns1Equals1()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = null });

        Assert.AreEqual("1=1", result);
    }

    [TestMethod]
    public void WhereObject_EmptyDictionary_Returns1Equals1()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var filter = new Dictionary<string, object?>();

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("1=1", result);
    }

    [TestMethod]
    public void WhereObject_AllNullValues_Returns1Equals1()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var filter = new Dictionary<string, object?>
        {
            ["Name"] = null,
            ["Age"] = null,
            ["Email"] = null
        };

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("1=1", result);
    }

    [TestMethod]
    public void WhereObject_MixedNullAndNonNull_OnlyIncludesNonNull()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var filter = new Dictionary<string, object?>
        {
            ["Name"] = "John",
            ["Age"] = null,
            ["Email"] = "john@test.com",
            ["IsActive"] = null
        };

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("([name] = @name AND [email] = @email)", result);
    }

    [TestMethod]
    public void WhereObject_OnlyOneNonNullValue_NoParentheses()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var filter = new Dictionary<string, object?>
        {
            ["Name"] = null,
            ["Age"] = 25,
            ["Email"] = null
        };

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("[age] = @age", result);
    }

    #endregion

    #region Property Name Matching Tests

    [TestMethod]
    public void WhereObject_MatchByPropertyName_Works()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var filter = new Dictionary<string, object?> { ["IsActive"] = true };

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("[is_active] = @is_active", result);
    }

    [TestMethod]
    public void WhereObject_MatchByColumnName_Works()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var filter = new Dictionary<string, object?> { ["is_active"] = true };

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("[is_active] = @is_active", result);
    }

    [TestMethod]
    public void WhereObject_CaseInsensitiveMatch_Works()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var filter = new Dictionary<string, object?> { ["NAME"] = "John", ["AGE"] = 25 };

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("([name] = @name AND [age] = @age)", result);
    }

    [TestMethod]
    public void WhereObject_UnknownProperty_Ignored()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var filter = new Dictionary<string, object?>
        {
            ["Name"] = "John",
            ["UnknownProperty"] = "ignored",
            ["AnotherUnknown"] = 123
        };

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("[name] = @name", result);
        Assert.IsFalse(result.Contains("unknown", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void WhereObject_AllUnknownProperties_Returns1Equals1()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var filter = new Dictionary<string, object?>
        {
            ["Unknown1"] = "value1",
            ["Unknown2"] = "value2"
        };

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("1=1", result);
    }

    #endregion

    #region Dialect Tests

    [TestMethod]
    public void WhereObject_SQLite_UsesSquareBrackets()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var filter = new Dictionary<string, object?> { ["Name"] = "John", ["Age"] = 25 };

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("([name] = @name AND [age] = @age)", result);
    }

    [TestMethod]
    public void WhereObject_PostgreSQL_UsesDoubleQuotesAndDollarPrefix()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", UserColumns);
        var filter = new Dictionary<string, object?> { ["Name"] = "John", ["Age"] = 25 };

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("(\"name\" = $name AND \"age\" = $age)", result);
    }

    [TestMethod]
    public void WhereObject_MySQL_UsesBackticks()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.MySql, "users", UserColumns);
        var filter = new Dictionary<string, object?> { ["Name"] = "John", ["Age"] = 25 };

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("(`name` = @name AND `age` = @age)", result);
    }

    [TestMethod]
    public void WhereObject_SqlServer_UsesSquareBrackets()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SqlServer, "users", UserColumns);
        var filter = new Dictionary<string, object?> { ["Name"] = "John", ["Age"] = 25 };

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("([name] = @name AND [age] = @age)", result);
    }

    [TestMethod]
    public void WhereObject_Oracle_UsesDoubleQuotes()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.Oracle, "users", UserColumns);
        var filter = new Dictionary<string, object?> { ["Name"] = "John", ["Age"] = 25 };

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("(\"name\" = :name AND \"age\" = :age)", result);
    }

    [TestMethod]
    public void WhereObject_DB2_UsesDoubleQuotes()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.DB2, "users", UserColumns);
        var filter = new Dictionary<string, object?> { ["Name"] = "John", ["Age"] = 25 };

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("(\"name\" = ?name AND \"age\" = ?age)", result);
    }

    #endregion

    #region Error Handling Tests

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void WhereObject_NonDictionaryObject_ThrowsException()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);

        handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = "not a dictionary" });
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void WhereObject_IntegerObject_ThrowsException()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);

        handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = 123 });
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void WhereObject_ListObject_ThrowsException()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);

        handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = new List<string> { "a", "b" } });
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void WhereObject_MissingParameter_ThrowsException()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);

        handler.Render(context, "--object filter", new Dictionary<string, object?> { ["wrongName"] = new Dictionary<string, object?>() });
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void WhereHandler_NoOption_ThrowsException()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);

        handler.Render(context, "", new Dictionary<string, object?>());
    }

    #endregion

    #region Integration with SqlTemplate Tests

    [TestMethod]
    public void WhereObject_InSqlTemplate_GeneratesCorrectSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} WHERE {{where --object filter}}", context);
        var filter = new Dictionary<string, object?> { ["Name"] = "John", ["IsActive"] = true };

        var sql = template.Render(new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("SELECT * FROM [users] WHERE ([name] = @name AND [is_active] = @is_active)", sql);
    }

    [TestMethod]
    public void WhereObject_InSqlTemplate_EmptyFilter_GeneratesAlwaysTrue()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} WHERE {{where --object filter}}", context);
        var filter = new Dictionary<string, object?>();

        var sql = template.Render(new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("SELECT * FROM [users] WHERE 1=1", sql);
    }

    [TestMethod]
    public void WhereObject_CombinedWithParamMode_BothWork()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        
        // Test --param mode still works
        var template1 = SqlTemplate.Prepare("SELECT * FROM {{table}} WHERE {{where --param predicate}}", context);
        var sql1 = template1.Render(new Dictionary<string, object?> { ["predicate"] = "id > 10" });
        Assert.AreEqual("SELECT * FROM [users] WHERE id > 10", sql1);

        // Test --object mode
        var template2 = SqlTemplate.Prepare("SELECT * FROM {{table}} WHERE {{where --object filter}}", context);
        var filter = new Dictionary<string, object?> { ["Name"] = "John" };
        var sql2 = template2.Render(new Dictionary<string, object?> { ["filter"] = filter });
        Assert.AreEqual("SELECT * FROM [users] WHERE [name] = @name", sql2);
    }

    #endregion

    #region Performance Optimization Tests

    [TestMethod]
    public void WhereObject_LargeColumnSet_UsesLookupOptimization()
    {
        // Create a context with more than 4 columns to trigger lookup optimization
        var manyColumns = new[]
        {
            new ColumnMeta("col1", "Col1", DbType.String, false),
            new ColumnMeta("col2", "Col2", DbType.String, false),
            new ColumnMeta("col3", "Col3", DbType.String, false),
            new ColumnMeta("col4", "Col4", DbType.String, false),
            new ColumnMeta("col5", "Col5", DbType.String, false),
            new ColumnMeta("col6", "Col6", DbType.String, false),
        };
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "test", manyColumns);
        var filter = new Dictionary<string, object?> { ["Col1"] = "a", ["Col6"] = "f" };

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("([col1] = @col1 AND [col6] = @col6)", result);
    }

    [TestMethod]
    public void WhereObject_SmallColumnSet_UsesLinearSearch()
    {
        // Create a context with 4 or fewer columns (no lookup optimization)
        var fewColumns = new[]
        {
            new ColumnMeta("col1", "Col1", DbType.String, false),
            new ColumnMeta("col2", "Col2", DbType.String, false),
        };
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "test", fewColumns);
        var filter = new Dictionary<string, object?> { ["Col1"] = "a", ["Col2"] = "b" };

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("([col1] = @col1 AND [col2] = @col2)", result);
    }

    #endregion

    #region Value Type Tests

    [TestMethod]
    public void WhereObject_IntegerValue_Works()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var filter = new Dictionary<string, object?> { ["Age"] = 25 };

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("[age] = @age", result);
    }

    [TestMethod]
    public void WhereObject_LongValue_Works()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var filter = new Dictionary<string, object?> { ["Id"] = 123456789L };

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("[id] = @id", result);
    }

    [TestMethod]
    public void WhereObject_BooleanValue_Works()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var filter = new Dictionary<string, object?> { ["IsActive"] = false };

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("[is_active] = @is_active", result);
    }

    [TestMethod]
    public void WhereObject_StringValue_Works()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var filter = new Dictionary<string, object?> { ["Email"] = "test@example.com" };

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = filter });

        Assert.AreEqual("[email] = @email", result);
    }

    [TestMethod]
    public void WhereObject_EmptyStringValue_IncludedInCondition()
    {
        var handler = WherePlaceholderHandler.Instance;
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var filter = new Dictionary<string, object?> { ["Name"] = "" };

        var result = handler.Render(context, "--object filter", new Dictionary<string, object?> { ["filter"] = filter });

        // Empty string is not null, so it should be included
        Assert.AreEqual("[name] = @name", result);
    }

    #endregion
}
