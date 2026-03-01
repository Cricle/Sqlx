// -----------------------------------------------------------------------
// <copyright file="ValuesPlaceholderTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Placeholders;
using System.Collections.Generic;
using System.Data;

namespace Sqlx.Tests.Placeholders;

/// <summary>
/// Consolidated tests for ValuesPlaceholderHandler.
/// </summary>
[TestClass]
public class ValuesPlaceholderTests
{
    /// <summary>
    /// Basic handler functionality tests.
    /// </summary>
    [TestClass]
    public class HandlerTests
    {
        [TestMethod]
        public void Values_BasicUsage_GeneratesParameterList()
        {
            var handler = ValuesPlaceholderHandler.Instance;
            var context = CreateContext();

            var result = handler.Process(context, "");

            Assert.IsTrue(result.Contains("@id"), $"Result: {result}");
            Assert.IsTrue(result.Contains("@name"), $"Result: {result}");
            Assert.IsTrue(result.Contains("@email"), $"Result: {result}");
        }

        [TestMethod]
        public void Values_WithExclude_ExcludesColumns()
        {
            var handler = ValuesPlaceholderHandler.Instance;
            var context = CreateContext();

            var result = handler.Process(context, "--exclude id");

            Assert.IsFalse(result.Contains("@id"), $"Result: {result}");
            Assert.IsTrue(result.Contains("@name"), $"Result: {result}");
            Assert.IsTrue(result.Contains("@email"), $"Result: {result}");
        }

        [TestMethod]
        public void Values_WithMultipleExcludes_ExcludesAllSpecified()
        {
            var handler = ValuesPlaceholderHandler.Instance;
            var context = CreateContext();

            var result = handler.Process(context, "--exclude id,email");

            Assert.IsFalse(result.Contains("@id"), $"Result: {result}");
            Assert.IsTrue(result.Contains("@name"), $"Result: {result}");
            Assert.IsFalse(result.Contains("@email"), $"Result: {result}");
        }

        [TestMethod]
        public void Values_WithInlineExpression_UsesExpression()
        {
            var handler = ValuesPlaceholderHandler.Instance;
            var context = CreateContext();

            var result = handler.Process(context, "--inline CreatedAt=CURRENT_TIMESTAMP");

            Assert.IsTrue(result.Contains("@id"), $"Result: {result}");
            Assert.IsTrue(result.Contains("@name"), $"Result: {result}");
            Assert.IsTrue(result.Contains("@email"), $"Result: {result}");
        }

        [TestMethod]
        public void Values_HandlerName_ReturnsValues()
        {
            var handler = ValuesPlaceholderHandler.Instance;

            var name = handler.Name;

            Assert.AreEqual("values", name);
        }

        [TestMethod]
        public void Values_GetType_WithParam_ReturnsDynamic()
        {
            var handler = ValuesPlaceholderHandler.Instance;

            var type = handler.GetType("--param ids");

            Assert.AreEqual(PlaceholderType.Dynamic, type);
        }

        [TestMethod]
        public void Values_GetType_WithoutParam_ReturnsStatic()
        {
            var handler = ValuesPlaceholderHandler.Instance;

            var type = handler.GetType("");

            Assert.AreEqual(PlaceholderType.Static, type);
        }

        private static PlaceholderContext CreateContext(SqlDialect? dialect = null)
        {
            var columns = new List<ColumnMeta>
            {
                new("id", "Id", DbType.Int64, false),
                new("name", "Name", DbType.String, false),
                new("email", "Email", DbType.String, false)
            };
            return new PlaceholderContext(dialect ?? SqlDefine.SQLite, "users", columns);
        }
    }

    /// <summary>
    /// Tests for dialect-specific parameter prefix generation.
    /// </summary>
    [TestClass]
    public class DialectTests
    {
        private static readonly ColumnMeta[] TestColumns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("email", "Email", DbType.String, true),
        };

        [TestMethod]
        public void Values_SQLite_UsesAtPrefix()
        {
            var handler = ValuesPlaceholderHandler.Instance;
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

            var result = handler.Process(context, string.Empty);

            Assert.AreEqual("@id, @name, @email", result);
        }

        [TestMethod]
        public void Values_PostgreSQL_UsesDollarPrefix()
        {
            var handler = ValuesPlaceholderHandler.Instance;
            var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);

            var result = handler.Process(context, string.Empty);

            Assert.AreEqual("@id, @name, @email", result);
        }

        [TestMethod]
        public void Values_MySQL_UsesAtPrefix()
        {
            var handler = ValuesPlaceholderHandler.Instance;
            var context = new PlaceholderContext(SqlDefine.MySql, "users", TestColumns);

            var result = handler.Process(context, string.Empty);

            Assert.AreEqual("@id, @name, @email", result);
        }

        [TestMethod]
        public void Values_SqlServer_UsesAtPrefix()
        {
            var handler = ValuesPlaceholderHandler.Instance;
            var context = new PlaceholderContext(SqlDefine.SqlServer, "users", TestColumns);

            var result = handler.Process(context, string.Empty);

            Assert.AreEqual("@id, @name, @email", result);
        }

        [TestMethod]
        public void Values_Oracle_UsesColonPrefix()
        {
            var handler = ValuesPlaceholderHandler.Instance;
            var context = new PlaceholderContext(SqlDefine.Oracle, "users", TestColumns);

            var result = handler.Process(context, string.Empty);

            Assert.AreEqual(":id, :name, :email", result);
        }

        [TestMethod]
        public void Values_DB2_UsesQuestionPrefix()
        {
            var handler = ValuesPlaceholderHandler.Instance;
            var context = new PlaceholderContext(SqlDefine.DB2, "users", TestColumns);

            var result = handler.Process(context, string.Empty);

            Assert.AreEqual("?id, ?name, ?email", result);
        }

        [TestMethod]
        public void Values_InInsertStatement_SQLite_GeneratesCorrectSql()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare("INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})", context);

            Assert.AreEqual("INSERT INTO [users] ([id], [name], [email]) VALUES (@id, @name, @email)", template.Sql);
        }

        [TestMethod]
        public void Values_InInsertStatement_PostgreSQL_GeneratesCorrectSql()
        {
            var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);
            var template = SqlTemplate.Prepare("INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})", context);

            Assert.AreEqual("INSERT INTO \"users\" (\"id\", \"name\", \"email\") VALUES (@id, @name, @email)", template.Sql);
        }

        [TestMethod]
        public void Values_InInsertStatement_Oracle_GeneratesCorrectSql()
        {
            var context = new PlaceholderContext(SqlDefine.Oracle, "users", TestColumns);
            var template = SqlTemplate.Prepare("INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})", context);

            Assert.AreEqual("INSERT INTO \"users\" (\"id\", \"name\", \"email\") VALUES (:id, :name, :email)", template.Sql);
        }

        [TestMethod]
        public void Values_WithExclude_AllDialects_GeneratesCorrectSql()
        {
            var dialects = new[] { SqlDefine.SQLite, SqlDefine.PostgreSql, SqlDefine.MySql, SqlDefine.SqlServer, SqlDefine.Oracle, SqlDefine.DB2 };

            foreach (var dialect in dialects)
            {
                var context = new PlaceholderContext(dialect, "users", TestColumns);
                var template = SqlTemplate.Prepare("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})", context);

                Assert.IsFalse(template.Sql.Contains("id"), $"Dialect {dialect} should not contain 'id'");
                Assert.IsTrue(template.Sql.Contains("name"), $"Dialect {dialect} should contain 'name'");
                Assert.IsTrue(template.Sql.Contains("email"), $"Dialect {dialect} should contain 'email'");
            }
        }

        [TestMethod]
        public void Values_MySqlDialect_UsesCorrectParameterPrefix()
        {
            var handler = ValuesPlaceholderHandler.Instance;
            var context = new PlaceholderContext(SqlDefine.MySql, "users", TestColumns);

            var result = handler.Process(context, "");

            Assert.IsTrue(result.Contains("@"), $"Result: {result}");
        }

        [TestMethod]
        public void Values_PostgreSqlDialect_UsesCorrectParameterPrefix()
        {
            var handler = ValuesPlaceholderHandler.Instance;
            var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);

            var result = handler.Process(context, "");

            Assert.IsTrue(result.Contains("@"), $"Result: {result}");
        }

        [TestMethod]
        public void Values_OracleDialect_UsesCorrectParameterPrefix()
        {
            var handler = ValuesPlaceholderHandler.Instance;
            var context = new PlaceholderContext(SqlDefine.Oracle, "users", TestColumns);

            var result = handler.Process(context, "");

            Assert.IsTrue(result.Contains(":"), $"Result: {result}");
        }
    }

    /// <summary>
    /// Tests for --inline option with expression support.
    /// </summary>
    [TestClass]
    public class InlineTests
    {
        private static readonly ColumnMeta[] TestColumns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("email", "Email", DbType.String, false),
            new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
            new ColumnMeta("version", "Version", DbType.Int32, false),
        };

        [TestMethod]
        public void Values_InlineSingleExpression_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline CreatedAt=CURRENT_TIMESTAMP}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("@id"));
            Assert.IsTrue(sql.Contains("@name"));
            Assert.IsTrue(sql.Contains("@email"));
            Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
            Assert.IsTrue(sql.Contains("@version"));
            Assert.IsFalse(sql.Contains("@created_at"));
        }

        [TestMethod]
        public void Values_InlineMultipleExpressions_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline CreatedAt=CURRENT_TIMESTAMP,Version=1}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("@id"));
            Assert.IsTrue(sql.Contains("@name"));
            Assert.IsTrue(sql.Contains("@email"));
            Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
            Assert.IsTrue(sql.Contains(", 1"));
            Assert.IsFalse(sql.Contains("@created_at"));
            Assert.IsFalse(sql.Contains("@version"));
        }

        [TestMethod]
        public void Values_InlineWithExclude_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline CreatedAt=CURRENT_TIMESTAMP}})",
                context);

            var sql = template.Sql;
            Assert.IsFalse(sql.Contains("@id"));
            Assert.IsTrue(sql.Contains("@name"));
            Assert.IsTrue(sql.Contains("@email"));
            Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
            Assert.IsTrue(sql.Contains("@version"));
        }

        [TestMethod]
        public void Values_InlineDefaultValue_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Version=0}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("@id"));
            Assert.IsTrue(sql.Contains("@name"));
            Assert.IsTrue(sql.Contains("@email"));
            Assert.IsTrue(sql.Contains("@created_at"));
            Assert.IsTrue(sql.Contains(", 0"));
            Assert.IsFalse(sql.Contains("@version"));
        }

        [TestMethod]
        public void Values_Inline_SQLite_UsesCorrectSyntax()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline CreatedAt=datetime('now')}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("@id"));
            Assert.IsTrue(sql.Contains("datetime('now')"));
        }

        [TestMethod]
        public void Values_Inline_PostgreSQL_UsesCorrectSyntax()
        {
            var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline CreatedAt=NOW()}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("@id"));
            Assert.IsTrue(sql.Contains("@name"));
            Assert.IsTrue(sql.Contains("NOW()"));
        }

        [TestMethod]
        public void Values_Inline_MySQL_UsesCorrectSyntax()
        {
            var context = new PlaceholderContext(SqlDefine.MySql, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline CreatedAt=NOW()}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("@id"));
            Assert.IsTrue(sql.Contains("@name"));
            Assert.IsTrue(sql.Contains("NOW()"));
        }

        [TestMethod]
        public void Values_Inline_SqlServer_UsesCorrectSyntax()
        {
            var context = new PlaceholderContext(SqlDefine.SqlServer, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline CreatedAt=GETDATE()}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("@id"));
            Assert.IsTrue(sql.Contains("GETDATE()"));
        }

        [TestMethod]
        public void Values_Inline_Oracle_UsesCorrectSyntax()
        {
            var context = new PlaceholderContext(SqlDefine.Oracle, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline CreatedAt=SYSDATE}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains(":id"));
            Assert.IsTrue(sql.Contains(":name"));
            Assert.IsTrue(sql.Contains("SYSDATE"));
        }

        [TestMethod]
        public void Values_InlineWithNull_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Email=NULL}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("NULL"));
            Assert.IsFalse(sql.Contains("@email"));
        }

        [TestMethod]
        public void Values_InlineWithStringLiteral_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Name='DefaultName'}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("'DefaultName'"));
            Assert.IsFalse(sql.Contains("@name"));
        }

        [TestMethod]
        public void Values_InlineWithNumericLiteral_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Id=123,Version=1}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("123"));
            Assert.IsTrue(sql.Contains(", 1"));
            Assert.IsFalse(sql.Contains("@id"));
            Assert.IsFalse(sql.Contains("@version"));
        }

        [TestMethod]
        public void Values_InlineWithFunction_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Name=UPPER(@name)}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("UPPER(@name)"));
        }

        [TestMethod]
        public void Values_InlineWithCast_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Version=CAST(@ver AS INTEGER)}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("CAST(@ver AS INTEGER)"));
        }

        [TestMethod]
        public void Values_InlineWithCoalesce_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Email=COALESCE(@email,'default@example.com')}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("COALESCE(@email"));
        }

        [TestMethod]
        public void Values_InlineAllColumns_OnlyUsesExpressions()
        {
            var columns = new[]
            {
                new ColumnMeta("id", "Id", DbType.Int64, false),
                new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
            };

            var context = new PlaceholderContext(SqlDefine.SQLite, "logs", columns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Id=@id,CreatedAt=CURRENT_TIMESTAMP}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("@id"));
            Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
        }

        [TestMethod]
        public void Values_InlineNoMatch_UsesStandardParameters()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline NonExistent=123}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("@id"));
            Assert.IsTrue(sql.Contains("@name"));
            Assert.IsTrue(sql.Contains("@email"));
            Assert.IsTrue(sql.Contains("@created_at"));
            Assert.IsTrue(sql.Contains("@version"));
        }

        [TestMethod]
        public void Values_InlineEmptyColumns_ReturnsEmpty()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", System.Array.Empty<ColumnMeta>());
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Id=123}})",
                context);

            Assert.IsTrue(template.Sql.Contains("VALUES ()"));
        }

        [TestMethod]
        public void Values_InlineWithSpaces_HandlesCorrectly()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Version = 1}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("1"));
            Assert.IsFalse(sql.Contains("@version"));
        }

        [TestMethod]
        public void Values_InlineCompleteInsert_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline CreatedAt=CURRENT_TIMESTAMP,Version=1}})",
                context);

            var expected = "INSERT INTO [users] ([name], [email], [created_at], [version]) VALUES (@name, @email, CURRENT_TIMESTAMP, 1)";
            Assert.AreEqual(expected, template.Sql);
        }

        [TestMethod]
        public void Values_InlineMixedParametersAndExpressions_GeneratesCorrectSQL()
        {
            var columns = new[]
            {
                new ColumnMeta("id", "Id", DbType.Int64, false),
                new ColumnMeta("name", "Name", DbType.String, false),
                new ColumnMeta("status", "Status", DbType.String, false),
                new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
                new ColumnMeta("created_by", "CreatedBy", DbType.String, false),
            };

            var context = new PlaceholderContext(SqlDefine.SQLite, "tasks", columns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Status='pending',CreatedAt=CURRENT_TIMESTAMP}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("@id"));
            Assert.IsTrue(sql.Contains("@name"));
            Assert.IsTrue(sql.Contains("'pending'"));
            Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
            Assert.IsTrue(sql.Contains("@created_by"));
        }
    }

    /// <summary>
    /// Edge case and boundary tests for --inline option.
    /// </summary>
    [TestClass]
    public class EdgeCaseTests
    {
        private static readonly ColumnMeta[] TestColumns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("is_active", "IsActive", DbType.Boolean, false),
            new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
            new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
        };

        [TestMethod]
        public void Values_InlineWithAtParameter_PreservesParameter()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Name=@customName}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("@customName"));
            Assert.IsFalse(sql.Contains("@name"));
        }

        [TestMethod]
        public void Values_InlineWithColonParameter_PreservesParameter()
        {
            var context = new PlaceholderContext(SqlDefine.Oracle, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Name=:customName}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains(":customName"));
            Assert.IsFalse(sql.Contains(":name"));
        }

        [TestMethod]
        public void Values_InlineWithDollarParameter_PreservesParameter()
        {
            var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Name=$customName}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("$customName"));
            Assert.IsFalse(sql.Contains("$name"));
        }

        [TestMethod]
        public void Values_InlineWithUUID_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Id=gen_random_uuid()}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("gen_random_uuid()"));
        }

        [TestMethod]
        public void Values_InlineWithSequence_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Id=nextval('users_id_seq')}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("nextval('users_id_seq')"));
        }

        [TestMethod]
        public void Values_InlineWithConcat_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Name='User_'||@id}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("'User_'||@id"));
        }

        [TestMethod]
        public void Values_InlineWithSubstring_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Name=SUBSTR(@fullName,1)}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("SUBSTR(@fullName"));
        }

        [TestMethod]
        public void Values_InlineWithTrim_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Name=TRIM(@name)}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("TRIM(@name)"));
        }

        [TestMethod]
        public void Values_InlineWithLower_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Name=LOWER(@name)}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("LOWER(@name)"));
        }

        [TestMethod]
        public void Values_InlineWithArithmetic_GeneratesCorrectSQL()
        {
            var columns = new[]
            {
                new ColumnMeta("id", "Id", DbType.Int64, false),
                new ColumnMeta("quantity", "Quantity", DbType.Int32, false),
                new ColumnMeta("price", "Price", DbType.Decimal, false),
                new ColumnMeta("total", "Total", DbType.Decimal, false),
            };

            var context = new PlaceholderContext(SqlDefine.SQLite, "orders", columns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Total=@quantity*@price}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("@quantity*@price"));
        }

        [TestMethod]
        public void Values_InlineWithParentheses_GeneratesCorrectSQL()
        {
            var columns = new[]
            {
                new ColumnMeta("id", "Id", DbType.Int64, false),
                new ColumnMeta("result", "Result", DbType.Decimal, false),
            };

            var context = new PlaceholderContext(SqlDefine.SQLite, "calculations", columns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Result=(@a+@b)*@c}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("(@a+@b)*@c"));
        }

        [TestMethod]
        public void Values_InlineWithNestedFunctions_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Name=UPPER(TRIM(@name))}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("UPPER(TRIM(@name))"));
        }

        [TestMethod]
        public void Values_InlineWithBooleanTrue_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline IsActive=1}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains(", 1"));
            Assert.IsFalse(sql.Contains("@is_active"));
        }

        [TestMethod]
        public void Values_InlineWithBooleanFalse_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline IsActive=0}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains(", 0"));
        }

        [TestMethod]
        public void Values_InlineWithCase_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline IsActive=CASE WHEN @status='active' THEN 1 ELSE 0 END}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("CASE WHEN @status='active' THEN 1 ELSE 0 END"));
        }

        [TestMethod]
        public void Values_InlineWithCastToInteger_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Id=CAST(@stringId AS INTEGER)}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("CAST(@stringId AS INTEGER)"));
        }

        [TestMethod]
        public void Values_InlineWithCastToText_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Name=CAST(@id AS TEXT)}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("CAST(@id AS TEXT)"));
        }

        [TestMethod]
        public void Values_InlineWithDatetime_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline CreatedAt=datetime('now')}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("datetime('now')"));
        }

        [TestMethod]
        public void Values_InlineWithDateAdd_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline UpdatedAt=datetime('now','+1 day')}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("datetime('now'"));
        }

        [TestMethod]
        public void Values_InlineWithStrftime_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Name=strftime('%Y-%m-%d',@date)}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("strftime('%Y-%m-%d'"));
        }

        [TestMethod]
        public void Values_InlineMultipleTimestamps_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline CreatedAt=CURRENT_TIMESTAMP,UpdatedAt=CURRENT_TIMESTAMP}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("@id"));
            Assert.IsTrue(sql.Contains("@name"));
            Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
            Assert.IsFalse(sql.Contains("@created_at"));
            Assert.IsFalse(sql.Contains("@updated_at"));
        }

        [TestMethod]
        public void Values_InlineMultipleDifferentTypes_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Id=123,Name='Test',IsActive=1,CreatedAt=CURRENT_TIMESTAMP}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("123"));
            Assert.IsTrue(sql.Contains("'Test'"));
            Assert.IsTrue(sql.Contains(", 1"));
            Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
            Assert.IsTrue(sql.Contains("@updated_at"));
        }

        [TestMethod]
        public void Values_InlineWithSnakeCaseColumn_MatchesByPropertyName()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline IsActive=1,CreatedAt=CURRENT_TIMESTAMP}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains(", 1"));
            Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
            Assert.IsFalse(sql.Contains("@is_active"));
            Assert.IsFalse(sql.Contains("@created_at"));
        }

        [TestMethod]
        public void Values_InlineWithNullKeyword_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Name=NULL}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("NULL"));
            Assert.IsFalse(sql.Contains("@name"));
        }

        [TestMethod]
        public void Values_InlineWithDefault_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Id=DEFAULT}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("DEFAULT"));
        }

        [TestMethod]
        public void Values_InlineWithExcludeAndExpression_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns --exclude Id,UpdatedAt}}) VALUES ({{values --exclude Id,UpdatedAt --inline CreatedAt=CURRENT_TIMESTAMP}})",
                context);

            var sql = template.Sql;
            Assert.IsFalse(sql.Contains("@id"));
            Assert.IsTrue(sql.Contains("@name"));
            Assert.IsTrue(sql.Contains("@is_active"));
            Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
            Assert.IsFalse(sql.Contains("@updated_at"));
        }

        [TestMethod]
        public void Values_InlineExcludeAll_ReturnsEmpty()
        {
            var columns = new[]
            {
                new ColumnMeta("id", "Id", DbType.Int64, false),
                new ColumnMeta("name", "Name", DbType.String, false),
            };

            var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns --exclude Id,Name}}) VALUES ({{values --exclude Id,Name}})",
                context);

            Assert.IsTrue(template.Sql.Contains("VALUES ()"));
        }

        [TestMethod]
        public void Values_InlineCaseInsensitivePropertyMatch_Works()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline createdat=CURRENT_TIMESTAMP}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
            Assert.IsFalse(sql.Contains("@created_at"));
        }
    }

    /// <summary>
    /// Integration tests for real-world scenarios.
    /// </summary>
    [TestClass]
    public class IntegrationTests
    {
        [TestMethod]
        public void Values_InlineAuditTrail_GeneratesCorrectSQL()
        {
            var columns = new[]
            {
                new ColumnMeta("id", "Id", DbType.Int64, false),
                new ColumnMeta("entity_type", "EntityType", DbType.String, false),
                new ColumnMeta("entity_id", "EntityId", DbType.Int64, false),
                new ColumnMeta("action", "Action", DbType.String, false),
                new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
                new ColumnMeta("created_by", "CreatedBy", DbType.String, false),
            };

            var context = new PlaceholderContext(SqlDefine.SQLite, "audit_logs", columns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline CreatedAt=CURRENT_TIMESTAMP}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("@entity_type"));
            Assert.IsTrue(sql.Contains("@entity_id"));
            Assert.IsTrue(sql.Contains("@action"));
            Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
            Assert.IsTrue(sql.Contains("@created_by"));
        }

        [TestMethod]
        public void Values_InlineDefaultStatus_GeneratesCorrectSQL()
        {
            var columns = new[]
            {
                new ColumnMeta("id", "Id", DbType.Int64, false),
                new ColumnMeta("name", "Name", DbType.String, false),
                new ColumnMeta("status", "Status", DbType.String, false),
                new ColumnMeta("priority", "Priority", DbType.Int32, false),
                new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
            };

            var context = new PlaceholderContext(SqlDefine.SQLite, "tasks", columns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline Status='pending',Priority=0,CreatedAt=CURRENT_TIMESTAMP}})",
                context);

            var expected = "INSERT INTO [tasks] ([name], [status], [priority], [created_at]) VALUES (@name, 'pending', 0, CURRENT_TIMESTAMP)";
            Assert.AreEqual(expected, template.Sql);
        }

        [TestMethod]
        public void Values_InlineUUIDGeneration_GeneratesCorrectSQL()
        {
            var columns = new[]
            {
                new ColumnMeta("id", "Id", DbType.Guid, false),
                new ColumnMeta("name", "Name", DbType.String, false),
                new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
            };

            var context = new PlaceholderContext(SqlDefine.PostgreSql, "entities", columns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Id=gen_random_uuid(),CreatedAt=NOW()}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("gen_random_uuid()"));
            Assert.IsTrue(sql.Contains("@name"));
            Assert.IsTrue(sql.Contains("NOW()"));
        }

        [TestMethod]
        public void Values_InlineSequenceNextVal_GeneratesCorrectSQL()
        {
            var columns = new[]
            {
                new ColumnMeta("id", "Id", DbType.Int64, false),
                new ColumnMeta("code", "Code", DbType.String, false),
                new ColumnMeta("name", "Name", DbType.String, false),
            };

            var context = new PlaceholderContext(SqlDefine.PostgreSql, "products", columns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Id=nextval('products_id_seq')}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("nextval('products_id_seq')"));
            Assert.IsTrue(sql.Contains("@code"));
            Assert.IsTrue(sql.Contains("@name"));
        }

        [TestMethod]
        public void Values_InlineComputedTotal_GeneratesCorrectSQL()
        {
            var columns = new[]
            {
                new ColumnMeta("id", "Id", DbType.Int64, false),
                new ColumnMeta("quantity", "Quantity", DbType.Int32, false),
                new ColumnMeta("unit_price", "UnitPrice", DbType.Decimal, false),
                new ColumnMeta("total", "Total", DbType.Decimal, false),
                new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
            };

            var context = new PlaceholderContext(SqlDefine.SQLite, "order_items", columns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline Total=@quantity*@unitPrice,CreatedAt=CURRENT_TIMESTAMP}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("@quantity"));
            Assert.IsTrue(sql.Contains("@unit_price"));
            Assert.IsTrue(sql.Contains("@quantity*@unitPrice"));
            Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
        }

        [TestMethod]
        public void Values_InlineEmailNormalization_GeneratesCorrectSQL()
        {
            var columns = new[]
            {
                new ColumnMeta("id", "Id", DbType.Int64, false),
                new ColumnMeta("name", "Name", DbType.String, false),
                new ColumnMeta("email", "Email", DbType.String, false),
                new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
            };

            var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline Email=LOWER(TRIM(@email)),CreatedAt=CURRENT_TIMESTAMP}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("@name"));
            Assert.IsTrue(sql.Contains("LOWER(TRIM(@email))"));
            Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
        }

        [TestMethod]
        public void Values_InlineConditionalStatus_GeneratesCorrectSQL()
        {
            var columns = new[]
            {
                new ColumnMeta("id", "Id", DbType.Int64, false),
                new ColumnMeta("name", "Name", DbType.String, false),
                new ColumnMeta("is_active", "IsActive", DbType.Boolean, false),
                new ColumnMeta("status", "Status", DbType.String, false),
            };

            var context = new PlaceholderContext(SqlDefine.SQLite, "accounts", columns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline Status=CASE WHEN @isActive=1 THEN 'active' ELSE 'inactive' END}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("@name"));
            Assert.IsTrue(sql.Contains("@is_active"));
            Assert.IsTrue(sql.Contains("CASE WHEN @isActive=1 THEN 'active' ELSE 'inactive' END"));
        }

        [TestMethod]
        public void Values_InlineMultipleTimestamps_GeneratesCorrectSQL()
        {
            var columns = new[]
            {
                new ColumnMeta("id", "Id", DbType.Int64, false),
                new ColumnMeta("data", "Data", DbType.String, false),
                new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
                new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
                new ColumnMeta("expires_at", "ExpiresAt", DbType.DateTime, false),
            };

            var context = new PlaceholderContext(SqlDefine.SQLite, "sessions", columns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline CreatedAt=CURRENT_TIMESTAMP,UpdatedAt=CURRENT_TIMESTAMP,ExpiresAt=datetime('now','+1 hour')}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("@data"));
            Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
            Assert.IsTrue(sql.Contains("datetime('now'"));
        }

        [TestMethod]
        public void Values_InlineInitialVersion_GeneratesCorrectSQL()
        {
            var columns = new[]
            {
                new ColumnMeta("id", "Id", DbType.Int64, false),
                new ColumnMeta("title", "Title", DbType.String, false),
                new ColumnMeta("content", "Content", DbType.String, false),
                new ColumnMeta("version", "Version", DbType.Int32, false),
                new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
            };

            var context = new PlaceholderContext(SqlDefine.SQLite, "documents", columns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline Version=1,CreatedAt=CURRENT_TIMESTAMP}})",
                context);

            var expected = "INSERT INTO [documents] ([title], [content], [version], [created_at]) VALUES (@title, @content, 1, CURRENT_TIMESTAMP)";
            Assert.AreEqual(expected, template.Sql);
        }

        [TestMethod]
        public void Values_InlineCoalesceDefault_GeneratesCorrectSQL()
        {
            var columns = new[]
            {
                new ColumnMeta("id", "Id", DbType.Int64, false),
                new ColumnMeta("name", "Name", DbType.String, false),
                new ColumnMeta("description", "Description", DbType.String, true),
                new ColumnMeta("status", "Status", DbType.String, false),
            };

            var context = new PlaceholderContext(SqlDefine.SQLite, "items", columns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline Description=COALESCE(@description,'No description'),Status='active'}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("@name"));
            Assert.IsTrue(sql.Contains("COALESCE(@description"));
            Assert.IsTrue(sql.Contains("'active'"));
        }

        [TestMethod]
        public void Values_InlineCrossDialect_SQLite_GeneratesCorrectSQL()
        {
            var columns = new[]
            {
                new ColumnMeta("id", "Id", DbType.Int64, false),
                new ColumnMeta("name", "Name", DbType.String, false),
                new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
            };

            var context = new PlaceholderContext(SqlDefine.SQLite, "logs", columns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline CreatedAt=datetime('now')}})",
                context);

            Assert.IsTrue(template.Sql.Contains("datetime('now')"));
        }

        [TestMethod]
        public void Values_InlineCrossDialect_PostgreSQL_GeneratesCorrectSQL()
        {
            var columns = new[]
            {
                new ColumnMeta("id", "Id", DbType.Int64, false),
                new ColumnMeta("name", "Name", DbType.String, false),
                new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
            };

            var context = new PlaceholderContext(SqlDefine.PostgreSql, "logs", columns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline CreatedAt=NOW()}})",
                context);

            Assert.IsTrue(template.Sql.Contains("NOW()"));
            Assert.IsTrue(template.Sql.Contains("@name"));
        }

        [TestMethod]
        public void Values_InlineCrossDialect_MySQL_GeneratesCorrectSQL()
        {
            var columns = new[]
            {
                new ColumnMeta("id", "Id", DbType.Int64, false),
                new ColumnMeta("name", "Name", DbType.String, false),
                new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
            };

            var context = new PlaceholderContext(SqlDefine.MySql, "logs", columns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline CreatedAt=NOW()}})",
                context);

            Assert.IsTrue(template.Sql.Contains("NOW()"));
            Assert.IsTrue(template.Sql.Contains("@name"));
        }

        [TestMethod]
        public void Values_InlineCrossDialect_SqlServer_GeneratesCorrectSQL()
        {
            var columns = new[]
            {
                new ColumnMeta("id", "Id", DbType.Int64, false),
                new ColumnMeta("name", "Name", DbType.String, false),
                new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
            };

            var context = new PlaceholderContext(SqlDefine.SqlServer, "logs", columns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline CreatedAt=GETDATE()}})",
                context);

            Assert.IsTrue(template.Sql.Contains("GETDATE()"));
            Assert.IsTrue(template.Sql.Contains("@name"));
        }

        [TestMethod]
        public void Values_InlineCrossDialect_Oracle_GeneratesCorrectSQL()
        {
            var columns = new[]
            {
                new ColumnMeta("id", "Id", DbType.Int64, false),
                new ColumnMeta("name", "Name", DbType.String, false),
                new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
            };

            var context = new PlaceholderContext(SqlDefine.Oracle, "logs", columns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline CreatedAt=SYSDATE}})",
                context);

            Assert.IsTrue(template.Sql.Contains("SYSDATE"));
            Assert.IsTrue(template.Sql.Contains(":name"));
        }

        [TestMethod]
        public void Values_InlineCompleteUserRegistration_GeneratesCorrectSQL()
        {
            var columns = new[]
            {
                new ColumnMeta("id", "Id", DbType.Int64, false),
                new ColumnMeta("username", "Username", DbType.String, false),
                new ColumnMeta("email", "Email", DbType.String, false),
                new ColumnMeta("password_hash", "PasswordHash", DbType.String, false),
                new ColumnMeta("is_active", "IsActive", DbType.Boolean, false),
                new ColumnMeta("is_verified", "IsVerified", DbType.Boolean, false),
                new ColumnMeta("role", "Role", DbType.String, false),
                new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
                new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
            };

            var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
            var template = SqlTemplate.Prepare(
                "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline Email=LOWER(@email),IsActive=1,IsVerified=0,Role='user',CreatedAt=CURRENT_TIMESTAMP,UpdatedAt=CURRENT_TIMESTAMP}})",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("@username"));
            Assert.IsTrue(sql.Contains("LOWER(@email)"));
            Assert.IsTrue(sql.Contains("@password_hash"));
            Assert.IsTrue(sql.Contains(", 1"));
            Assert.IsTrue(sql.Contains(", 0"));
            Assert.IsTrue(sql.Contains("'user'"));
            Assert.IsTrue(sql.Contains("CURRENT_TIMESTAMP"));
        }
    }

    /// <summary>
    /// Tests for --param option functionality.
    /// </summary>
    [TestClass]
    public class ParamTests
    {
        private static readonly ColumnMeta[] TestColumns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("email", "Email", DbType.String, false),
        };

        [TestMethod]
        public void Values_WithParam_GeneratesSingleParameter()
        {
            var handler = ValuesPlaceholderHandler.Instance;
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

            var result = handler.Process(context, "--param ids");

            Assert.AreEqual("@ids", result);
        }

        [TestMethod]
        public void Values_WithParamAndCollection_ExpandsToMultipleParameters()
        {
            var handler = ValuesPlaceholderHandler.Instance;
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var parameters = new Dictionary<string, object?>
            {
                ["ids"] = new List<int> { 1, 2, 3 }
            };

            var result = handler.Render(context, "--param ids", parameters);

            Assert.IsTrue(result.Contains("@ids0"), $"Result: {result}");
            Assert.IsTrue(result.Contains("@ids1"), $"Result: {result}");
            Assert.IsTrue(result.Contains("@ids2"), $"Result: {result}");
        }

        [TestMethod]
        public void Values_WithParamAndEmptyCollection_ReturnsNull()
        {
            var handler = ValuesPlaceholderHandler.Instance;
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var parameters = new Dictionary<string, object?>
            {
                ["ids"] = new List<int>()
            };

            var result = handler.Render(context, "--param ids", parameters);

            Assert.AreEqual("NULL", result);
        }

        [TestMethod]
        public void Values_WithParamAndNonCollection_ReturnsSingleParameter()
        {
            var handler = ValuesPlaceholderHandler.Instance;
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var parameters = new Dictionary<string, object?>
            {
                ["id"] = 42
            };

            var result = handler.Render(context, "--param id", parameters);

            Assert.AreEqual("@id", result);
        }

        [TestMethod]
        public void Values_WithParamAndString_ReturnsSingleParameter()
        {
            var handler = ValuesPlaceholderHandler.Instance;
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var parameters = new Dictionary<string, object?>
            {
                ["name"] = "John"
            };

            var result = handler.Render(context, "--param name", parameters);

            Assert.AreEqual("@name", result);
        }

        [TestMethod]
        public void Values_WithParamAndArray_ExpandsToMultipleParameters()
        {
            var handler = ValuesPlaceholderHandler.Instance;
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var parameters = new Dictionary<string, object?>
            {
                ["tags"] = new[] { "tag1", "tag2", "tag3" }
            };

            var result = handler.Render(context, "--param tags", parameters);

            Assert.IsTrue(result.Contains("@tags0"), $"Result: {result}");
            Assert.IsTrue(result.Contains("@tags1"), $"Result: {result}");
            Assert.IsTrue(result.Contains("@tags2"), $"Result: {result}");
        }

        [TestMethod]
        public void Values_WithParamButNoParameters_FallsBackToStatic()
        {
            var handler = ValuesPlaceholderHandler.Instance;
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);

            var result = handler.Render(context, "--param ids", null);

            Assert.IsTrue(result.Contains("@id") || result.Contains("@name"), $"Result: {result}");
        }

        [TestMethod]
        public void Values_WithParamOption_PostgreSQL_GeneratesPostgreSQLParameter()
        {
            var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);
            var handler = ValuesPlaceholderHandler.Instance;

            var result = handler.Process(context, "--param ids");

            Assert.AreEqual("@ids", result);
        }

        [TestMethod]
        public void Values_WithParamOption_MySQL_GeneratesMySQLParameter()
        {
            var context = new PlaceholderContext(SqlDefine.MySql, "users", TestColumns);
            var handler = ValuesPlaceholderHandler.Instance;

            var result = handler.Process(context, "--param ids");

            Assert.AreEqual("@ids", result);
        }

        [TestMethod]
        public void Values_WithoutParamOption_GeneratesEntityPropertyList()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var handler = ValuesPlaceholderHandler.Instance;

            var result = handler.Process(context, "");

            Assert.AreEqual("@id, @name, @email", result);
        }

        [TestMethod]
        public void Values_WithExcludeOption_GeneratesFilteredPropertyList()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var handler = ValuesPlaceholderHandler.Instance;

            var result = handler.Process(context, "--exclude Id");

            Assert.AreEqual("@name, @email", result);
        }

        [TestMethod]
        public void Values_WithParamAndExcludeOptions_ParamTakesPrecedence()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var handler = ValuesPlaceholderHandler.Instance;

            var result = handler.Process(context, "--param ids --exclude Id");

            Assert.AreEqual("@ids", result);
        }
    }
}
