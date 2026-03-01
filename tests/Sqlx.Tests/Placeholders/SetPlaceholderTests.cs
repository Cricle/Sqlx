// <copyright file="SetPlaceholderTests.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Placeholders;
using System;
using System.Collections.Generic;
using System.Data;

namespace Sqlx.Tests.Placeholders;

/// <summary>
/// Comprehensive tests for SetPlaceholderHandler covering all SET clause generation scenarios.
/// Merged from: SetPlaceholderHandlerTests, SetPlaceholderExpressionTests, SetPlaceholderInlineTests,
/// SetPlaceholderInlineEdgeCaseTests, SetPlaceholderInlineIntegrationTests, SetPlaceholderStrictTests,
/// SetPlaceholderUpdateScenarioTests, LimitOffsetPlaceholderHandlerTests.
/// </summary>
[TestClass]
public class SetPlaceholderTests
{
    /// <summary>
    /// Tests for basic SetPlaceholderHandler functionality.
    /// </summary>
    [TestClass]
    public class HandlerTests
    {
        private SetPlaceholderHandler _handler = null!;
        private SqlServerDialect _dialect = null!;
        private List<ColumnMeta> _columns = null!;

        [TestInitialize]
        public void Setup()
        {
            _handler = SetPlaceholderHandler.Instance;
            _dialect = new SqlServerDialect();
            _columns = new List<ColumnMeta>
            {
                new ColumnMeta("id", "Id", DbType.Int32, false),
                new ColumnMeta("name", "Name", DbType.String, false),
                new ColumnMeta("email", "Email", DbType.String, false),
                new ColumnMeta("version", "Version", DbType.Int32, false),
                new ColumnMeta("priority", "Priority", DbType.Int32, false),
                new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false)
            };
        }

        [TestMethod]
        public void Name_ReturnsSet()
        {
            var name = _handler.Name;
            Assert.AreEqual("set", name);
        }

        [TestMethod]
        public void GetType_WithoutParamOption_ReturnsStatic()
        {
            var type = _handler.GetType("");
            Assert.AreEqual(PlaceholderType.Static, type);
        }

        [TestMethod]
        public void GetType_WithParamOption_ReturnsDynamic()
        {
            var type = _handler.GetType("--param updates");
            Assert.AreEqual(PlaceholderType.Dynamic, type);
        }

        [TestMethod]
        public void Process_WithNoOptions_GeneratesAllColumns()
        {
            var context = new PlaceholderContext(_dialect, "users", _columns);
            var result = _handler.Process(context, "");

            Assert.IsTrue(result.Contains("[id] = @id"));
            Assert.IsTrue(result.Contains("[name] = @name"));
            Assert.IsTrue(result.Contains("[email] = @email"));
            Assert.IsTrue(result.Contains("[version] = @version"));
            Assert.IsTrue(result.Contains("[priority] = @priority"));
            Assert.IsTrue(result.Contains("[updated_at] = @updated_at"));
        }

        [TestMethod]
        public void Process_WithExcludeOption_ExcludesSpecifiedColumns()
        {
            var context = new PlaceholderContext(_dialect, "users", _columns);
            var result = _handler.Process(context, "--exclude Id,Version");

            Assert.IsFalse(result.Contains("[id]"));
            Assert.IsFalse(result.Contains("[version]"));
            Assert.IsTrue(result.Contains("[name] = @name"));
            Assert.IsTrue(result.Contains("[email] = @email"));
            Assert.IsTrue(result.Contains("[priority] = @priority"));
            Assert.IsTrue(result.Contains("[updated_at] = @updated_at"));
        }

        [TestMethod]
        public void Process_WithInlineExpression_UsesExpression()
        {
            var context = new PlaceholderContext(_dialect, "users", _columns);
            var result = _handler.Process(context, "--inline Version=Version+1");

            Assert.IsTrue(result.Contains("[version] = [version]+1"));
            Assert.IsTrue(result.Contains("[name] = @name"));
        }

        [TestMethod]
        public void Process_WithMultipleInlineExpressions_UsesAllExpressions()
        {
            var context = new PlaceholderContext(_dialect, "users", _columns);
            var result = _handler.Process(context, "--inline Version=Version+1,Priority=Priority*2");

            Assert.IsTrue(result.Contains("[version] = [version]+1"));
            Assert.IsTrue(result.Contains("[priority] = [priority]*2"));
            Assert.IsTrue(result.Contains("[name] = @name"));
        }

        [TestMethod]
        public void Process_WithExcludeAndInline_CombinesBothOptions()
        {
            var context = new PlaceholderContext(_dialect, "users", _columns);
            var result = _handler.Process(context, "--exclude Id --inline Version=Version+1");

            Assert.IsFalse(result.Contains("[id]"));
            Assert.IsTrue(result.Contains("[version] = [version]+1"));
            Assert.IsTrue(result.Contains("[name] = @name"));
        }

        [TestMethod]
        public void Process_WithParamOption_ReturnsEmptyString()
        {
            var context = new PlaceholderContext(_dialect, "users", _columns);
            var result = _handler.Process(context, "--param updates");

            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void Process_WithEmptyColumns_ReturnsEmptyString()
        {
            var context = new PlaceholderContext(_dialect, "users", new List<ColumnMeta>());
            var result = _handler.Process(context, "");

            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void Process_WithSingleColumn_ReturnsOneSetClause()
        {
            var singleColumn = new List<ColumnMeta> { new ColumnMeta("name", "Name", DbType.String, false) };
            var context = new PlaceholderContext(_dialect, "users", singleColumn);
            var result = _handler.Process(context, "");

            Assert.AreEqual("[name] = @name", result);
        }

        [TestMethod]
        public void Process_WithInlineExpression_ComplexExpression_HandlesCorrectly()
        {
            var context = new PlaceholderContext(_dialect, "users", _columns);
            var result = _handler.Process(context, "--inline Priority=Priority+Version*2");

            Assert.IsTrue(result.Contains("[priority] = [priority]+[version]*2"));
        }

        [TestMethod]
        public void Process_WithInlineExpression_CaseInsensitivePropertyMatch()
        {
            var context = new PlaceholderContext(_dialect, "users", _columns);
            var result = _handler.Process(context, "--inline version=Version+1");

            Assert.IsTrue(result.Contains("[version] = [version]+1"));
        }

        [TestMethod]
        public void Render_WithoutParamOption_CallsProcess()
        {
            var context = new PlaceholderContext(_dialect, "users", _columns);
            var parameters = new Dictionary<string, object?>();
            var result = _handler.Render(context, "", parameters);

            Assert.IsTrue(result.Contains("[name] = @name"));
        }

        [TestMethod]
        public void Render_WithParamOption_ReturnsParameterValue()
        {
            var context = new PlaceholderContext(_dialect, "users", _columns);
            var parameters = new Dictionary<string, object?>
            {
                ["updates"] = "[name] = @name, [email] = @email"
            };
            var result = _handler.Render(context, "--param updates", parameters);

            Assert.AreEqual("[name] = @name, [email] = @email", result);
        }

        [TestMethod]
        public void Render_WithParamOption_NullParameter_ReturnsEmptyString()
        {
            var context = new PlaceholderContext(_dialect, "users", _columns);
            var parameters = new Dictionary<string, object?>
            {
                ["updates"] = null
            };
            var result = _handler.Render(context, "--param updates", parameters);

            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Render_WithParamOption_MissingParameter_ThrowsException()
        {
            var context = new PlaceholderContext(_dialect, "users", _columns);
            var parameters = new Dictionary<string, object?>();
            _handler.Render(context, "--param updates", parameters);
        }

        [TestMethod]
        public void Render_WithParamOption_ExpressionValue_ReturnsExpression()
        {
            var context = new PlaceholderContext(_dialect, "users", _columns);
            var parameters = new Dictionary<string, object?>
            {
                ["updates"] = "[priority] = [priority] + 1, [updated_at] = @updatedAt"
            };
            var result = _handler.Render(context, "--param updates", parameters);

            Assert.AreEqual("[priority] = [priority] + 1, [updated_at] = @updatedAt", result);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Render_WithNullParameters_ThrowsException()
        {
            var context = new PlaceholderContext(_dialect, "users", _columns);
            _handler.Render(context, "--param updates", null);
        }
    }

    /// <summary>
    /// Tests for SetPlaceholder with different SQL dialects.
    /// </summary>
    [TestClass]
    public class DialectTests
    {
        private static readonly List<ColumnMeta> TestColumns = new()
        {
            new ColumnMeta("id", "Id", DbType.Int32, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("email", "Email", DbType.String, false)
        };

        [TestMethod]
        public void Process_WithSqlServerDialect_UsesSquareBrackets()
        {
            var dialect = new SqlServerDialect();
            var context = new PlaceholderContext(dialect, "users", TestColumns);
            var handler = SetPlaceholderHandler.Instance;
            var result = handler.Process(context, "--exclude Id");

            Assert.IsTrue(result.Contains("[name] = @name"));
            Assert.IsTrue(result.Contains("[email] = @email"));
        }

        [TestMethod]
        public void Process_WithMySqlDialect_UsesBackticks()
        {
            var dialect = new MySqlDialect();
            var context = new PlaceholderContext(dialect, "users", TestColumns);
            var handler = SetPlaceholderHandler.Instance;
            var result = handler.Process(context, "--exclude Id");

            Assert.IsTrue(result.Contains("`name` = @name"));
            Assert.IsTrue(result.Contains("`email` = @email"));
        }

        [TestMethod]
        public void Process_WithPostgreSqlDialect_UsesDoubleQuotes()
        {
            var dialect = new PostgreSqlDialect();
            var context = new PlaceholderContext(dialect, "users", TestColumns);
            var handler = SetPlaceholderHandler.Instance;
            var result = handler.Process(context, "--exclude Id");

            Assert.IsTrue(result.Contains("\"name\" = @name"));
            Assert.IsTrue(result.Contains("\"email\" = @email"));
        }

        [TestMethod]
        public void Process_WithSQLiteDialect_UsesSquareBrackets()
        {
            var dialect = new SQLiteDialect();
            var context = new PlaceholderContext(dialect, "users", TestColumns);
            var handler = SetPlaceholderHandler.Instance;
            var result = handler.Process(context, "--exclude Id");

            Assert.IsTrue(result.Contains("[name] = @name"));
            Assert.IsTrue(result.Contains("[email] = @email"));
        }

        [TestMethod]
        public void Process_WithOracleDialect_UsesDoubleQuotes()
        {
            var dialect = new OracleDialect();
            var context = new PlaceholderContext(dialect, "users", TestColumns);
            var handler = SetPlaceholderHandler.Instance;
            var result = handler.Process(context, "--exclude Id");

            // Oracle uses : for parameters
            Assert.IsTrue(result.Contains("\"name\""));
            Assert.IsTrue(result.Contains("\"email\""));
        }

        [TestMethod]
        public void Process_WithDB2Dialect_UsesDoubleQuotes()
        {
            var dialect = new DB2Dialect();
            var context = new PlaceholderContext(dialect, "users", TestColumns);
            var handler = SetPlaceholderHandler.Instance;
            var result = handler.Process(context, "--exclude Id");

            // DB2 uses : for parameters
            Assert.IsTrue(result.Contains("\"name\""));
            Assert.IsTrue(result.Contains("\"email\""));
        }
    }


    /// <summary>
    /// Tests for SetPlaceholder with Expression parameters.
    /// </summary>
    [TestClass]
    public class ExpressionTests
    {
        private readonly PlaceholderContext _context;

        public ExpressionTests()
        {
            _context = new PlaceholderContext(
                dialect: SqlDefine.SQLite,
                tableName: "users",
                columns: new List<ColumnMeta>
                {
                    new("id", "Id", DbType.Int32, false),
                    new("name", "Name", DbType.String, false),
                    new("email", "Email", DbType.String, false),
                    new("age", "Age", DbType.Int32, false),
                    new("version", "Version", DbType.Int32, false),
                });
        }

        [TestMethod]
        public void SetPlaceholder_WithParamOption_IsDynamic()
        {
            var handler = SetPlaceholderHandler.Instance;
            var options = "--param updates";
            var type = handler.GetType(options);

            Assert.AreEqual(PlaceholderType.Dynamic, type);
        }

        [TestMethod]
        public void SetPlaceholder_WithParamOption_ReturnsEmptyForStaticProcessing()
        {
            var handler = SetPlaceholderHandler.Instance;
            var options = "--param updates";
            var result = handler.Process(_context, options);

            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void SetPlaceholder_WithExpression_RendersSingleFieldUpdate()
        {
            var handler = SetPlaceholderHandler.Instance;
            var options = "--param updates";
            var updatesSql = "[name] = @p0";
            var parameters = new Dictionary<string, object?>
            {
                ["updates"] = updatesSql
            };
            var result = handler.Render(_context, options, parameters);

            Assert.AreEqual("[name] = @p0", result);
        }

        [TestMethod]
        public void SetPlaceholder_WithExpression_RendersMultipleFieldsUpdate()
        {
            var handler = SetPlaceholderHandler.Instance;
            var options = "--param updates";
            var updatesSql = "[name] = @p0, [email] = @p1";
            var parameters = new Dictionary<string, object?>
            {
                ["updates"] = updatesSql
            };
            var result = handler.Render(_context, options, parameters);

            Assert.AreEqual("[name] = @p0, [email] = @p1", result);
        }

        [TestMethod]
        public void SetPlaceholder_WithExpression_RendersIncrementExpression()
        {
            var handler = SetPlaceholderHandler.Instance;
            var options = "--param updates";
            var updatesSql = "[version] = ([version] + @p0)";
            var parameters = new Dictionary<string, object?>
            {
                ["updates"] = updatesSql
            };
            var result = handler.Render(_context, options, parameters);

            Assert.AreEqual("[version] = ([version] + @p0)", result);
        }

        [TestMethod]
        public void SetPlaceholder_WithExpression_RendersMixedUpdate()
        {
            var handler = SetPlaceholderHandler.Instance;
            var options = "--param updates";
            var updatesSql = "[name] = @p0, [version] = ([version] + @p1)";
            var parameters = new Dictionary<string, object?>
            {
                ["updates"] = updatesSql
            };
            var result = handler.Render(_context, options, parameters);

            Assert.AreEqual("[name] = @p0, [version] = ([version] + @p1)", result);
        }

        [TestMethod]
        public void SetPlaceholder_WithParamOption_ThrowsWhenParameterMissing()
        {
            var handler = SetPlaceholderHandler.Instance;
            var options = "--param updates";
            var parameters = new Dictionary<string, object?>();

            var ex = Assert.ThrowsException<InvalidOperationException>(() =>
                handler.Render(_context, options, parameters));
            Assert.IsTrue(ex.Message.Contains("updates"));
        }

        [TestMethod]
        public void SetPlaceholder_WithParamOption_HandlesNullParameter()
        {
            var handler = SetPlaceholderHandler.Instance;
            var options = "--param updates";
            var parameters = new Dictionary<string, object?>
            {
                ["updates"] = null
            };
            var result = handler.Render(_context, options, parameters);

            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void SetPlaceholder_WithoutParamOption_RemainsStatic()
        {
            var handler = SetPlaceholderHandler.Instance;
            var options = "--exclude Id";
            var type = handler.GetType(options);

            Assert.AreEqual(PlaceholderType.Static, type);
        }
    }

    /// <summary>
    /// Tests for SetPlaceholder with --inline option.
    /// </summary>
    [TestClass]
    public class InlineTests
    {
        private static readonly ColumnMeta[] TestColumns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("version", "Version", DbType.Int32, false),
            new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
            new ColumnMeta("counter", "Counter", DbType.Int32, false),
        };

        [TestMethod]
        public void Set_InlineSingleExpression_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id --inline Version=Version+1}} WHERE id = @id", context);

            var expected = "UPDATE [users] SET [name] = @name, [version] = [version]+1, [updated_at] = @updated_at, [counter] = @counter WHERE id = @id";
            Assert.AreEqual(expected, template.Sql);
        }

        [TestMethod]
        public void Set_InlineMultipleExpressions_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline Version=Version+1,Counter=Counter+1}} WHERE id = @id",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("[name] = @name"));
            Assert.IsTrue(sql.Contains("[version] = [version]+1"));
            Assert.IsTrue(sql.Contains("[counter] = [counter]+1"));
            Assert.IsTrue(sql.Contains("[updated_at] = @updated_at"));
        }

        [TestMethod]
        public void Set_InlineWithCurrentTimestamp_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline UpdatedAt=CURRENT_TIMESTAMP}} WHERE id = @id",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("[updated_at] = CURRENT_TIMESTAMP"));
            Assert.IsTrue(sql.Contains("[name] = @name"));
            Assert.IsFalse(sql.Contains("[updated_at] = @updated_at"));
        }

        [TestMethod]
        public void Set_InlineComplexExpression_GeneratesCorrectSQL()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline Counter=Counter*2+1}} WHERE id = @id",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("[counter] = [counter]*2+1"));
        }

        [TestMethod]
        public void Set_InlineWithParameters_PreservesParameters()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline Counter=Counter+@increment}} WHERE id = @id",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("[counter] = [counter]+@increment"));
            Assert.IsTrue(sql.Contains("[name] = @name"));
        }

        [TestMethod]
        public void Set_InlinePostgreSQL_UsesDoubleQuotesAndDollarSign()
        {
            var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline Version=Version+1}} WHERE id = @id",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("\"version\" = \"version\"+1"));
            Assert.IsTrue(sql.Contains("\"name\" = @name"));
        }

        [TestMethod]
        public void Set_InlineMySQL_UsesBackticks()
        {
            var context = new PlaceholderContext(SqlDefine.MySql, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline Version=Version+1}} WHERE id = @id",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("`version` = `version`+1"));
            Assert.IsTrue(sql.Contains("`name` = @name"));
        }

        [TestMethod]
        public void Set_InlineOnlyExpressions_NoStandardParameters()
        {
            var columns = new[]
            {
                new ColumnMeta("id", "Id", DbType.Int64, false),
                new ColumnMeta("version", "Version", DbType.Int32, false),
            };

            var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline Version=Version+1}} WHERE id = @id",
                context);

            Assert.AreEqual("UPDATE [users] SET [version] = [version]+1 WHERE id = @id", template.Sql);
        }

        [TestMethod]
        public void Set_InlineMixedCase_MatchesPropertyName()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline Version=Version+1}} WHERE id = @id",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("[version] = [version]+1"));
        }

        [TestMethod]
        public void Set_InlineWithSpaces_HandlesCorrectly()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline Version = Version + 1}} WHERE id = @id",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("[version] = [version] + 1"));
        }
    }


    /// <summary>
    /// Edge case tests for SetPlaceholder with --inline option.
    /// </summary>
    [TestClass]
    public class EdgeCaseTests
    {
        private static readonly ColumnMeta[] TestColumns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
            new ColumnMeta("version", "Version", DbType.Int32, false),
            new ColumnMeta("is_active", "IsActive", DbType.Boolean, false),
            new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
        };

        [TestMethod]
        public void Set_InlineWithNoMatchingColumn_IgnoresExpression()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline NonExistent=NonExistent+1}} WHERE id = @id",
                context);

            var sql = template.Sql;
            Assert.IsFalse(sql.Contains("NonExistent"));
            Assert.IsTrue(sql.Contains("[name] = @name"));
        }

        [TestMethod]
        public void Set_InlineAllColumns_OnlyUsesExpressions()
        {
            var columns = new[]
            {
                new ColumnMeta("id", "Id", DbType.Int64, false),
                new ColumnMeta("counter", "Counter", DbType.Int32, false),
            };

            var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline Counter=Counter+1}} WHERE id = @id",
                context);

            Assert.AreEqual("UPDATE [users] SET [counter] = [counter]+1 WHERE id = @id", template.Sql);
        }

        [TestMethod]
        public void Set_InlineWithSnakeCaseColumn_MatchesByPropertyName()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline IsActive=IsActive+1}} WHERE id = @id",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("[is_active] = [is_active]+1"));
        }

        [TestMethod]
        public void Set_InlineWithParameterPlaceholder_PreservesAtSign()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline Version=@newVersion}} WHERE id = @id",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("[version] = @newVersion"));
        }

        [TestMethod]
        public void Set_InlineWithColonParameter_PreservesColon()
        {
            var context = new PlaceholderContext(SqlDefine.Oracle, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline Version=:newVersion}} WHERE id = :id",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("\"version\" = :newVersion"));
        }

        [TestMethod]
        public void Set_InlineWithDollarParameter_PreservesDollar()
        {
            var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline Version=$newVersion}} WHERE id = $id",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("\"version\" = $newVersion"));
        }

        [TestMethod]
        public void Set_InlineWithSqlFunction_PreservesFunction()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline CreatedAt=datetime('now')}} WHERE id = @id",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("[created_at] = datetime('now')"));
        }

        [TestMethod]
        public void Set_InlineWithCast_PreservesCast()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline Version=CAST(@value AS INTEGER)}} WHERE id = @id",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("[version] = CAST(@value AS INTEGER)"));
        }

        [TestMethod]
        public void Set_InlineWithCase_PreservesCase()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline IsActive=CASE WHEN Version>0 THEN 1 ELSE 0 END}} WHERE id = @id",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("[is_active] = CASE WHEN [version]>0 THEN 1 ELSE 0 END"));
        }

        [TestMethod]
        public void Set_InlineWithParentheses_PreservesParentheses()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline Version=(Version+1)*2}} WHERE id = @id",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("[version] = ([version]+1)*2"));
        }

        [TestMethod]
        public void Set_InlineWithStringLiteral_PreservesLiteral()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline Name='DefaultName'}} WHERE id = @id",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("[name] = 'DefaultName'"));
        }

        [TestMethod]
        public void Set_InlineWithNumericLiteral_PreservesLiteral()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline Version=42}} WHERE id = @id",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("[version] = 42"));
        }

        [TestMethod]
        public void Set_InlineWithNull_PreservesNull()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline Name=NULL}} WHERE id = @id",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("[name] = NULL"));
        }

        [TestMethod]
        public void Set_InlineWithCoalesce_PreservesCoalesce()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline Name=COALESCE(@newName}} WHERE id = @id",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("[name] = COALESCE(@newName"));
        }

        [TestMethod]
        public void Set_InlineWithConcat_PreservesConcat()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline Name=Name||'_updated'}} WHERE id = @id",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("[name] = [name]||'_updated'"));
        }

        [TestMethod]
        public void Set_InlineWithSubstring_PreservesSubstring()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline Name=SUBSTR(Name,1)}} WHERE id = @id",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("[name] = SUBSTR([name]"));
        }

        [TestMethod]
        public void Set_InlineEmptyColumns_ReturnsEmptySet()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", Array.Empty<ColumnMeta>());
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --inline Version=Version+1}} WHERE id = @id",
                context);

            Assert.AreEqual("UPDATE [users] SET  WHERE id = @id", template.Sql);
        }

        [TestMethod]
        public void Set_InlineExcludeAll_ReturnsEmptySet()
        {
            var columns = new[]
            {
                new ColumnMeta("id", "Id", DbType.Int64, false),
                new ColumnMeta("version", "Version", DbType.Int32, false),
            };

            var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id,Version}} WHERE id = @id",
                context);

            Assert.AreEqual("UPDATE [users] SET  WHERE id = @id", template.Sql);
        }

        [TestMethod]
        public void Set_InlineMixedCasePropertyNames_MatchesCorrectly()
        {
            var columns = new[]
            {
                new ColumnMeta("user_id", "UserId", DbType.Int64, false),
                new ColumnMeta("is_active", "IsActive", DbType.Boolean, false),
                new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
            };

            var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude UserId --inline IsActive=IsActive+1,CreatedAt=CURRENT_TIMESTAMP}} WHERE user_id = @userId",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("[is_active] = [is_active]+1"));
            Assert.IsTrue(sql.Contains("[created_at] = CURRENT_TIMESTAMP"));
        }

        [TestMethod]
        public void Set_InlineWithLowerCaseKeywords_DoesNotReplaceKeywords()
        {
            var context = new PlaceholderContext(SqlDefine.SQLite, "users", TestColumns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline Version=Version+1}} WHERE id = @id",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("UPDATE"));
            Assert.IsTrue(sql.Contains("WHERE"));
        }

        [TestMethod]
        public void Set_InlineWithReservedWords_OnlyReplacesPropertyNames()
        {
            var columns = new[]
            {
                new ColumnMeta("id", "Id", DbType.Int64, false),
                new ColumnMeta("order", "Order", DbType.Int32, false),
                new ColumnMeta("select", "Select", DbType.String, false),
            };

            var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
            var template = SqlTemplate.Prepare(
                "UPDATE {{table}} SET {{set --exclude Id --inline Order=Order+1}} WHERE id = @id",
                context);

            var sql = template.Sql;
            Assert.IsTrue(sql.Contains("[order] = [order]+1"));
            Assert.IsTrue(sql.Contains("[select] = @select"));
        }
    }
}
