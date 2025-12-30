// -----------------------------------------------------------------------
// <copyright file="TDD_DialectSpecific_AllDialects.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using Sqlx.Generator;

namespace Sqlx.Tests.Placeholders.Dialect;

/// <summary>
/// {{bool_true}}, {{bool_false}}, {{current_timestamp}} 方言特定占位符的完整测试
/// P1 方言占位符
/// </summary>
[TestClass]
public class TDD_DialectSpecific_AllDialects
{
    private SqlTemplateEngine _engine = null!;
    private Compilation _compilation = null!;
    private IMethodSymbol _testMethod = null!;
    private INamedTypeSymbol _userType = null!;

    private static readonly Sqlx.Generator.SqlDefine[] AllDialects = new[]
    {
        Sqlx.Generator.SqlDefine.SQLite,
        Sqlx.Generator.SqlDefine.PostgreSql,
        Sqlx.Generator.SqlDefine.MySql,
        Sqlx.Generator.SqlDefine.SqlServer
    };

    [TestInitialize]
    public void Initialize()
    {
        _engine = new SqlTemplateEngine();

        var sourceCode = @"
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public interface ITestMethods
    {
        Task<List<User>> GetActiveAsync(CancellationToken ct = default);
    }
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToList();

        _compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        _userType = _compilation.GetTypeByMetadataName("TestNamespace.User")!;
        var methodClass = _compilation.GetTypeByMetadataName("TestNamespace.ITestMethods")!;
        _testMethod = methodClass.GetMembers("GetActiveAsync").OfType<IMethodSymbol>().First();
    }

    private static string GetDialectName(Sqlx.Generator.SqlDefine dialect)
    {
        return dialect.DatabaseType;
    }

    #region {{bool_true}} 占位符测试

    [TestMethod]
    [Description("{{bool_true}} 占位符应该在所有方言中生成正确的值")]
    public void BoolTrue_AllDialects_GeneratesCorrectValue()
    {
        var template = "SELECT * FROM users WHERE is_active = {{bool_true}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");
            Assert.AreEqual(0, result.Errors.Count,
                $"[{dialectName}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");

            Assert.IsFalse(result.ProcessedSql.Contains("{{bool_true}}"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符");
        }
    }

    [TestMethod]
    [Description("{{bool_true}} - PostgreSQL 使用 true")]
    public void BoolTrue_PostgreSQL_UsesTrue()
    {
        var template = "SELECT * FROM users WHERE is_active = {{bool_true}}";
        var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", Sqlx.Generator.SqlDefine.PostgreSql);

        Assert.IsTrue(result.ProcessedSql.Contains("true"),
            $"PostgreSQL 应该使用 'true'。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("{{bool_true}} - SQLite 使用 1")]
    public void BoolTrue_SQLite_UsesOne()
    {
        var template = "SELECT * FROM users WHERE is_active = {{bool_true}}";
        var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", Sqlx.Generator.SqlDefine.SQLite);

        Assert.IsTrue(result.ProcessedSql.Contains("1"),
            $"SQLite 应该使用 '1'。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("{{bool_true}} - MySQL 使用 1")]
    public void BoolTrue_MySQL_UsesOne()
    {
        var template = "SELECT * FROM users WHERE is_active = {{bool_true}}";
        var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", Sqlx.Generator.SqlDefine.MySql);

        Assert.IsTrue(result.ProcessedSql.Contains("1"),
            $"MySQL 应该使用 '1'。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("{{bool_true}} - SQL Server 使用 1")]
    public void BoolTrue_SqlServer_UsesOne()
    {
        var template = "SELECT * FROM users WHERE is_active = {{bool_true}}";
        var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", Sqlx.Generator.SqlDefine.SqlServer);

        Assert.IsTrue(result.ProcessedSql.Contains("1"),
            $"SQL Server 应该使用 '1'。实际 SQL: {result.ProcessedSql}");
    }

    #endregion

    #region {{bool_false}} 占位符测试

    [TestMethod]
    [Description("{{bool_false}} 占位符应该在所有方言中生成正确的值")]
    public void BoolFalse_AllDialects_GeneratesCorrectValue()
    {
        var template = "SELECT * FROM users WHERE is_active = {{bool_false}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");
            Assert.AreEqual(0, result.Errors.Count,
                $"[{dialectName}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");

            Assert.IsFalse(result.ProcessedSql.Contains("{{bool_false}}"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符");
        }
    }

    [TestMethod]
    [Description("{{bool_false}} - PostgreSQL 使用 false")]
    public void BoolFalse_PostgreSQL_UsesFalse()
    {
        var template = "SELECT * FROM users WHERE is_active = {{bool_false}}";
        var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", Sqlx.Generator.SqlDefine.PostgreSql);

        Assert.IsTrue(result.ProcessedSql.Contains("false"),
            $"PostgreSQL 应该使用 'false'。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("{{bool_false}} - SQLite 使用 0")]
    public void BoolFalse_SQLite_UsesZero()
    {
        var template = "SELECT * FROM users WHERE is_active = {{bool_false}}";
        var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", Sqlx.Generator.SqlDefine.SQLite);

        Assert.IsTrue(result.ProcessedSql.Contains("0"),
            $"SQLite 应该使用 '0'。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("{{bool_false}} - MySQL 使用 0")]
    public void BoolFalse_MySQL_UsesZero()
    {
        var template = "SELECT * FROM users WHERE is_active = {{bool_false}}";
        var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", Sqlx.Generator.SqlDefine.MySql);

        Assert.IsTrue(result.ProcessedSql.Contains("0"),
            $"MySQL 应该使用 '0'。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("{{bool_false}} - SQL Server 使用 0")]
    public void BoolFalse_SqlServer_UsesZero()
    {
        var template = "SELECT * FROM users WHERE is_active = {{bool_false}}";
        var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", Sqlx.Generator.SqlDefine.SqlServer);

        Assert.IsTrue(result.ProcessedSql.Contains("0"),
            $"SQL Server 应该使用 '0'。实际 SQL: {result.ProcessedSql}");
    }

    #endregion

    #region {{current_timestamp}} 占位符测试

    [TestMethod]
    [Description("{{current_timestamp}} 占位符应该在所有方言中生成正确的值")]
    public void CurrentTimestamp_AllDialects_GeneratesCorrectValue()
    {
        var template = "INSERT INTO users (name, created_at) VALUES (@name, {{current_timestamp}})";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");
            Assert.AreEqual(0, result.Errors.Count,
                $"[{dialectName}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");

            Assert.IsFalse(result.ProcessedSql.Contains("{{current_timestamp}}"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符");
        }
    }

    [TestMethod]
    [Description("{{current_timestamp}} - PostgreSQL 使用 CURRENT_TIMESTAMP")]
    public void CurrentTimestamp_PostgreSQL_UsesCurrentTimestamp()
    {
        var template = "INSERT INTO users (name, created_at) VALUES (@name, {{current_timestamp}})";
        var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", Sqlx.Generator.SqlDefine.PostgreSql);

        Assert.IsTrue(result.ProcessedSql.ToUpperInvariant().Contains("CURRENT_TIMESTAMP"),
            $"PostgreSQL 应该使用 'CURRENT_TIMESTAMP'。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("{{current_timestamp}} - SQLite 使用 CURRENT_TIMESTAMP")]
    public void CurrentTimestamp_SQLite_UsesCurrentTimestamp()
    {
        var template = "INSERT INTO users (name, created_at) VALUES (@name, {{current_timestamp}})";
        var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", Sqlx.Generator.SqlDefine.SQLite);

        Assert.IsTrue(result.ProcessedSql.ToUpperInvariant().Contains("CURRENT_TIMESTAMP"),
            $"SQLite 应该使用 'CURRENT_TIMESTAMP'。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("{{current_timestamp}} - MySQL 使用 CURRENT_TIMESTAMP")]
    public void CurrentTimestamp_MySQL_UsesCurrentTimestamp()
    {
        var template = "INSERT INTO users (name, created_at) VALUES (@name, {{current_timestamp}})";
        var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", Sqlx.Generator.SqlDefine.MySql);

        Assert.IsTrue(result.ProcessedSql.ToUpperInvariant().Contains("CURRENT_TIMESTAMP"),
            $"MySQL 应该使用 'CURRENT_TIMESTAMP'。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("{{current_timestamp}} - SQL Server 使用 GETDATE() 或 CURRENT_TIMESTAMP")]
    public void CurrentTimestamp_SqlServer_UsesGetDateOrCurrentTimestamp()
    {
        var template = "INSERT INTO users (name, created_at) VALUES (@name, {{current_timestamp}})";
        var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", Sqlx.Generator.SqlDefine.SqlServer);

        var sqlUpper = result.ProcessedSql.ToUpperInvariant();
        Assert.IsTrue(sqlUpper.Contains("GETDATE") || sqlUpper.Contains("CURRENT_TIMESTAMP"),
            $"SQL Server 应该使用 'GETDATE()' 或 'CURRENT_TIMESTAMP'。实际 SQL: {result.ProcessedSql}");
    }

    #endregion

    #region 方言占位符组合测试

    [TestMethod]
    [Description("{{bool_true}} + {{bool_false}} 组合查询")]
    public void BoolTrueFalse_Combined_AllDialects()
    {
        var template = "UPDATE users SET is_active = {{bool_true}} WHERE is_deleted = {{bool_false}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            Assert.IsFalse(result.ProcessedSql.Contains("{{"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("所有方言占位符组合查询")]
    public void AllDialectPlaceholders_Combined_AllDialects()
    {
        var template = "INSERT INTO users (name, is_active, created_at) VALUES (@name, {{bool_true}}, {{current_timestamp}})";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");
            Assert.AreEqual(0, result.Errors.Count,
                $"[{dialectName}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("INSERT INTO"),
                $"[{dialectName}] 应该包含 INSERT INTO");
            Assert.IsTrue(sqlUpper.Contains("VALUES"),
                $"[{dialectName}] 应该包含 VALUES");

            Assert.IsFalse(result.ProcessedSql.Contains("{{"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("方言占位符在 WHERE 子句中正常工作")]
    public void DialectPlaceholders_InWhereClause_AllDialects()
    {
        var template = "SELECT * FROM users WHERE is_active = {{bool_true}} AND created_at > {{current_timestamp}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("SELECT"),
                $"[{dialectName}] 应该包含 SELECT");
            Assert.IsTrue(sqlUpper.Contains("WHERE"),
                $"[{dialectName}] 应该包含 WHERE");

            Assert.IsFalse(result.ProcessedSql.Contains("{{"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符");
        }
    }

    #endregion

    #region 边界测试

    [TestMethod]
    [Description("方言占位符不应该留下未处理的占位符")]
    public void DialectPlaceholders_AllDialects_NoUnprocessedPlaceholders()
    {
        var templates = new[]
        {
            "SELECT * FROM users WHERE is_active = {{bool_true}}",
            "SELECT * FROM users WHERE is_active = {{bool_false}}",
            "INSERT INTO users (created_at) VALUES ({{current_timestamp}})"
        };

        foreach (var template in templates)
        {
            foreach (var dialect in AllDialects)
            {
                var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
                var dialectName = GetDialectName(dialect);

                Assert.IsFalse(result.ProcessedSql.Contains("{{"),
                    $"[{dialectName}] SQL 不应该包含未处理的占位符。模板: {template}, 实际 SQL: {result.ProcessedSql}");
            }
        }
    }

    [TestMethod]
    [Description("方言占位符不应该产生错误")]
    public void DialectPlaceholders_AllDialects_NoErrors()
    {
        var templates = new[]
        {
            "SELECT * FROM users WHERE is_active = {{bool_true}}",
            "SELECT * FROM users WHERE is_active = {{bool_false}}",
            "INSERT INTO users (created_at) VALUES ({{current_timestamp}})"
        };

        foreach (var template in templates)
        {
            foreach (var dialect in AllDialects)
            {
                var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
                var dialectName = GetDialectName(dialect);

                Assert.AreEqual(0, result.Errors.Count,
                    $"[{dialectName}] 不应该有错误。模板: {template}, 错误: {string.Join(", ", result.Errors)}");
            }
        }
    }

    #endregion
}

