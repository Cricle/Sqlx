// <copyright file="SetPlaceholderStrictTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests;

using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Strict and comprehensive tests for {{set}} placeholder covering all edge cases.
/// </summary>
[TestClass]
public class SetPlaceholderStrictTests
{
    private static readonly ColumnMeta[] StandardColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int64, false),
        new ColumnMeta("name", "Name", DbType.String, false),
        new ColumnMeta("email", "Email", DbType.String, false),
        new ColumnMeta("age", "Age", DbType.Int32, false),
        new ColumnMeta("is_active", "IsActive", DbType.Boolean, false),
        new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
        new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
    };

    #region Basic SET Tests

    [TestMethod]
    public void Set_NoOptions_GeneratesAllColumns()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", StandardColumns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set}} WHERE id = @id", context);

        var expected = "UPDATE [users] SET [id] = @id, [name] = @name, [email] = @email, [age] = @age, [is_active] = @is_active, [created_at] = @created_at, [updated_at] = @updated_at WHERE id = @id";
        Assert.AreEqual(expected, template.Sql);
    }

    [TestMethod]
    public void Set_ExcludeSingleColumn_ExcludesCorrectly()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", StandardColumns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id", context);

        var sql = template.Sql;
        Assert.IsFalse(sql.Contains("[id] = @id,") || sql.Contains(", [id] = @id"));
        Assert.IsTrue(sql.Contains("[name] = @name"));
        Assert.IsTrue(sql.Contains("[email] = @email"));
    }

    [TestMethod]
    public void Set_ExcludeMultipleColumns_ExcludesAll()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", StandardColumns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id,CreatedAt,UpdatedAt}} WHERE id = @id", context);

        var sql = template.Sql;
        Assert.IsFalse(sql.Contains("[id] = @id"));
        Assert.IsFalse(sql.Contains("[created_at] = @created_at"));
        Assert.IsFalse(sql.Contains("[updated_at] = @updated_at"));
        Assert.IsTrue(sql.Contains("[name] = @name"));
        Assert.IsTrue(sql.Contains("[email] = @email"));
        Assert.IsTrue(sql.Contains("[age] = @age"));
        Assert.IsTrue(sql.Contains("[is_active] = @is_active"));
    }

    [TestMethod]
    public void Set_ExcludeByPropertyName_Works()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", StandardColumns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id,CreatedAt}} WHERE id = @id", context);

        var sql = template.Sql;
        Assert.IsFalse(sql.Contains("[id] = @id"));
        Assert.IsFalse(sql.Contains("[created_at] = @created_at"));
    }

    [TestMethod]
    public void Set_ExcludeByColumnName_Works()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", StandardColumns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude id,created_at}} WHERE id = @id", context);

        var sql = template.Sql;
        Assert.IsFalse(sql.Contains("[id] = @id"));
        Assert.IsFalse(sql.Contains("[created_at] = @created_at"));
    }

    [TestMethod]
    public void Set_ExcludeMixedCase_CaseInsensitive()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", StandardColumns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude ID,CREATEDAT}} WHERE id = @id", context);

        var sql = template.Sql;
        Assert.IsFalse(sql.Contains("[id] = @id"));
        Assert.IsFalse(sql.Contains("[created_at] = @created_at"));
    }

    #endregion

    #region Empty and Single Column Tests

    [TestMethod]
    public void Set_EmptyColumns_ReturnsEmpty()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", Array.Empty<ColumnMeta>());
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set}} WHERE id = @id", context);

        Assert.AreEqual("UPDATE [users] SET  WHERE id = @id", template.Sql);
    }

    [TestMethod]
    public void Set_SingleColumn_NoTrailingComma()
    {
        var columns = new[] { new ColumnMeta("name", "Name", DbType.String, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set}} WHERE id = @id", context);

        Assert.AreEqual("UPDATE [users] SET [name] = @name WHERE id = @id", template.Sql);
        Assert.IsFalse(template.Sql.Contains(",,"));
        Assert.IsFalse(template.Sql.Contains(", WHERE"));
    }

    [TestMethod]
    public void Set_ExcludeAllColumns_ReturnsEmpty()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id,Name}} WHERE id = @id", context);

        Assert.AreEqual("UPDATE [users] SET  WHERE id = @id", template.Sql);
    }

    #endregion

    #region Inline Expression Tests

    [TestMethod]
    public void Set_InlineSingleExpression_ReplacesCorrectColumn()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", StandardColumns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id --inline UpdatedAt=CURRENT_TIMESTAMP}} WHERE id = @id", context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("[name] = @name"));
        Assert.IsTrue(sql.Contains("[email] = @email"));
        Assert.IsTrue(sql.Contains("[updated_at] = CURRENT_TIMESTAMP"));
        Assert.IsFalse(sql.Contains("[updated_at] = @updated_at"));
    }

    [TestMethod]
    public void Set_InlineMultipleExpressions_ReplacesAllCorrectly()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("version", "Version", DbType.Int32, false),
            new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id --inline Version=Version+1,UpdatedAt=CURRENT_TIMESTAMP}} WHERE id = @id", context);

        var expected = "UPDATE [users] SET [name] = @name, [version] = [version]+1, [updated_at] = CURRENT_TIMESTAMP WHERE id = @id";
        Assert.AreEqual(expected, template.Sql);
    }

    [TestMethod]
    public void Set_InlineWithExclude_BothWork()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", StandardColumns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id,CreatedAt --inline UpdatedAt=CURRENT_TIMESTAMP}} WHERE id = @id", context);

        var sql = template.Sql;
        Assert.IsFalse(sql.Contains("[id] = @id"));
        Assert.IsFalse(sql.Contains("[created_at] = @created_at"));
        Assert.IsTrue(sql.Contains("[updated_at] = CURRENT_TIMESTAMP"));
        Assert.IsTrue(sql.Contains("[name] = @name"));
    }

    [TestMethod]
    public void Set_InlineNonExistentColumn_Ignored()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", StandardColumns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id --inline NonExistent=123}} WHERE id = @id", context);

        var sql = template.Sql;
        Assert.IsFalse(sql.Contains("NonExistent"));
        Assert.IsFalse(sql.Contains("123"));
        Assert.IsTrue(sql.Contains("[name] = @name"));
    }

    [TestMethod]
    public void Set_InlineOnlyExpressions_NoStandardParameters()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("version", "Version", DbType.Int32, false),
            new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id --inline Version=Version+1,UpdatedAt=CURRENT_TIMESTAMP}} WHERE id = @id", context);

        var expected = "UPDATE [users] SET [version] = [version]+1, [updated_at] = CURRENT_TIMESTAMP WHERE id = @id";
        Assert.AreEqual(expected, template.Sql);
    }

    #endregion

    #region Complex Expression Tests

    [TestMethod]
    public void Set_InlineArithmeticExpression_GeneratesCorrectly()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("counter", "Counter", DbType.Int32, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "stats", columns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id --inline Counter=Counter*2+1}} WHERE id = @id", context);

        Assert.AreEqual("UPDATE [stats] SET [counter] = [counter]*2+1 WHERE id = @id", template.Sql);
    }

    [TestMethod]
    public void Set_InlineWithParameters_PreservesParameters()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("counter", "Counter", DbType.Int32, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "stats", columns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id --inline Counter=Counter+@increment}} WHERE id = @id", context);

        Assert.AreEqual("UPDATE [stats] SET [counter] = [counter]+@increment WHERE id = @id", template.Sql);
    }

    [TestMethod]
    public void Set_InlineWithParentheses_PreservesStructure()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("result", "Result", DbType.Decimal, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "calculations", columns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id --inline Result=(Result+@a)*@b}} WHERE id = @id", context);

        Assert.AreEqual("UPDATE [calculations] SET [result] = ([result]+@a)*@b WHERE id = @id", template.Sql);
    }

    [TestMethod]
    public void Set_InlineWithSqlFunction_PreservesFunction()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id --inline Name=UPPER(Name)}} WHERE id = @id", context);

        Assert.AreEqual("UPDATE [users] SET [name] = UPPER([name]) WHERE id = @id", template.Sql);
    }

    #endregion

    #region Dialect-Specific Tests

    [TestMethod]
    public void Set_SQLite_UsesSquareBracketsAndAtPrefix()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", StandardColumns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id", context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("[name] = @name"));
        Assert.IsTrue(sql.Contains("[email] = @email"));
    }

    [TestMethod]
    public void Set_PostgreSQL_UsesDoubleQuotesAndDollarPrefix()
    {
        var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", StandardColumns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = $id", context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("\"name\" = $name"));
        Assert.IsTrue(sql.Contains("\"email\" = $email"));
    }

    [TestMethod]
    public void Set_MySQL_UsesBackticksAndAtPrefix()
    {
        var context = new PlaceholderContext(SqlDefine.MySql, "users", StandardColumns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id", context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("`name` = @name"));
        Assert.IsTrue(sql.Contains("`email` = @email"));
    }

    [TestMethod]
    public void Set_SqlServer_UsesSquareBracketsAndAtPrefix()
    {
        var context = new PlaceholderContext(SqlDefine.SqlServer, "users", StandardColumns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id", context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("[name] = @name"));
        Assert.IsTrue(sql.Contains("[email] = @email"));
    }

    [TestMethod]
    public void Set_Oracle_UsesDoubleQuotesAndColonPrefix()
    {
        var context = new PlaceholderContext(SqlDefine.Oracle, "users", StandardColumns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = :id", context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("\"name\" = :name"));
        Assert.IsTrue(sql.Contains("\"email\" = :email"));
    }

    [TestMethod]
    public void Set_DB2_UsesDoubleQuotesAndQuestionPrefix()
    {
        var context = new PlaceholderContext(SqlDefine.DB2, "users", StandardColumns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = ?", context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("\"name\" = ?"));
        Assert.IsTrue(sql.Contains("\"email\" = ?"));
    }

    #endregion

    #region Inline Expression Dialect Tests

    [TestMethod]
    public void Set_InlineSQLite_CorrectSyntax()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("version", "Version", DbType.Int32, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "docs", columns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id --inline Version=Version+1}} WHERE id = @id", context);

        Assert.AreEqual("UPDATE [docs] SET [version] = [version]+1 WHERE id = @id", template.Sql);
    }

    [TestMethod]
    public void Set_InlinePostgreSQL_CorrectSyntax()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("version", "Version", DbType.Int32, false),
        };

        var context = new PlaceholderContext(SqlDefine.PostgreSql, "docs", columns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id --inline Version=Version+1}} WHERE id = $id", context);

        Assert.AreEqual("UPDATE \"docs\" SET \"version\" = \"version\"+1 WHERE id = $id", template.Sql);
    }

    [TestMethod]
    public void Set_InlineMySQL_CorrectSyntax()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("version", "Version", DbType.Int32, false),
        };

        var context = new PlaceholderContext(SqlDefine.MySql, "docs", columns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id --inline Version=Version+1}} WHERE id = @id", context);

        Assert.AreEqual("UPDATE `docs` SET `version` = `version`+1 WHERE id = @id", template.Sql);
    }

    #endregion

    #region Column Order Tests

    [TestMethod]
    public void Set_MaintainsColumnOrder()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", StandardColumns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id", context);

        var sql = template.Sql;
        var nameIndex = sql.IndexOf("[name]");
        var emailIndex = sql.IndexOf("[email]");
        var ageIndex = sql.IndexOf("[age]");

        Assert.IsTrue(nameIndex < emailIndex, "Name should come before Email");
        Assert.IsTrue(emailIndex < ageIndex, "Email should come before Age");
    }

    #endregion

    #region Whitespace and Formatting Tests

    [TestMethod]
    public void Set_NoExtraWhitespace()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", StandardColumns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id", context);

        var sql = template.Sql;
        Assert.IsFalse(sql.Contains("  "), "Should not contain double spaces");
        Assert.IsFalse(sql.Contains(" ,"), "Should not have space before comma");
        Assert.IsFalse(sql.Contains(",,"), "Should not have double commas");
    }

    [TestMethod]
    public void Set_InlineWithSpaces_HandlesCorrectly()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("version", "Version", DbType.Int32, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "docs", columns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id --inline Version = Version + 1}} WHERE id = @id", context);

        // 表达式中的空格应该被保留
        Assert.IsTrue(template.Sql.Contains("[version] = [version] + 1"));
    }

    #endregion

    #region Special Characters in Column Names

    [TestMethod]
    public void Set_ColumnWithUnderscore_HandlesCorrectly()
    {
        var columns = new[]
        {
            new ColumnMeta("user_id", "UserId", DbType.Int64, false),
            new ColumnMeta("first_name", "FirstName", DbType.String, false),
            new ColumnMeta("last_name", "LastName", DbType.String, false),
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude UserId}} WHERE user_id = @userId", context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("[first_name] = @first_name"));
        Assert.IsTrue(sql.Contains("[last_name] = @last_name"));
        Assert.IsFalse(sql.Contains("[user_id] = @user_id"));
    }

    #endregion

    #region Nullable Column Tests

    [TestMethod]
    public void Set_NullableColumns_TreatedSameAsNonNullable()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("email", "Email", DbType.String, true),  // nullable
            new ColumnMeta("age", "Age", DbType.Int32, true),       // nullable
        };

        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id", context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("[name] = @name"));
        Assert.IsTrue(sql.Contains("[email] = @email"));
        Assert.IsTrue(sql.Contains("[age] = @age"));
    }

    #endregion

    #region Integration with WHERE Clause Tests

    [TestMethod]
    public void Set_WithSimpleWhere_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", StandardColumns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id,CreatedAt}} WHERE id = @id", context);

        var sql = template.Sql;
        Assert.IsTrue(sql.StartsWith("UPDATE [users] SET"));
        Assert.IsTrue(sql.EndsWith("WHERE id = @id"));
        Assert.IsTrue(sql.Contains("[name] = @name"));
    }

    [TestMethod]
    public void Set_WithComplexWhere_GeneratesCorrectSQL()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", StandardColumns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id,CreatedAt}} WHERE id = @id AND is_active = 1", context);

        var sql = template.Sql;
        Assert.IsTrue(sql.Contains("WHERE id = @id AND is_active = 1"));
        Assert.IsTrue(sql.Contains("[name] = @name"));
    }

    #endregion

    #region Performance and Large Column Set Tests

    [TestMethod]
    public void Set_ManyColumns_HandlesEfficiently()
    {
        var columns = new ColumnMeta[100];
        for (int i = 0; i < 100; i++)
        {
            columns[i] = new ColumnMeta($"col{i}", $"Col{i}", DbType.String, false);
        }

        var context = new PlaceholderContext(SqlDefine.SQLite, "large_table", columns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Col0}} WHERE col0 = @col0", context);

        var sql = template.Sql;
        Assert.IsFalse(sql.Contains("[col0] = @col0,") || sql.Contains(", [col0] = @col0"));
        Assert.IsTrue(sql.Contains("[col1] = @col1"));
        Assert.IsTrue(sql.Contains("[col99] = @col99"));
    }

    #endregion
}
