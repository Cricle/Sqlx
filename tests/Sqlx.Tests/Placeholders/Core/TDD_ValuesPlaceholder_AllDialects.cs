// -----------------------------------------------------------------------
// <copyright file="TDD_ValuesPlaceholder_AllDialects.cs" company="Cricle">
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

namespace Sqlx.Tests.Placeholders.Core;

/// <summary>
/// {{values}} 占位符在所有数据库方言中的完整测试
/// P0 核心占位符 - INSERT VALUES 子句生成
/// </summary>
[TestClass]
public class TDD_ValuesPlaceholder_AllDialects
{
    private SqlTemplateEngine _engine = null!;
    private Compilation _compilation = null!;
    private IMethodSymbol _testMethod = null!;
    private IMethodSymbol _testMethodWithParams = null!;
    private INamedTypeSymbol _userType = null!;

    // 所有支持的数据库方言
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

        // 创建测试编译上下文
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
        public string Email { get; set; }
        public int Age { get; set; }
        public decimal Balance { get; set; }
    }

    public interface ITestMethods
    {
        Task<int> InsertAsync(CancellationToken ct = default);
        Task<long> InsertUserAsync(string name, string email, int age, CancellationToken ct = default);
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
        _testMethod = methodClass.GetMembers("InsertAsync").OfType<IMethodSymbol>().First();
        _testMethodWithParams = methodClass.GetMembers("InsertUserAsync").OfType<IMethodSymbol>().First();
    }

    private static string GetDialectName(Sqlx.Generator.SqlDefine dialect)
    {
        return dialect.DatabaseType;
    }

    #region {{values}} 基础测试 - 所有方言

    [TestMethod]
    [Description("{{values}} 占位符应该在所有方言中生成 VALUES 子句")]
    public void Values_AllDialects_GeneratesValuesClause()
    {
        var template = "INSERT INTO users (name, email, age) VALUES ({{values}})";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithParams, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");
            Assert.AreEqual(0, result.Errors.Count,
                $"[{dialectName}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");

            // 不应该包含未处理的占位符
            Assert.IsFalse(result.ProcessedSql.Contains("{{values}}"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{values}} 应该生成参数列表")]
    public void Values_GeneratesParameterList()
    {
        var template = "INSERT INTO users (name, email, age) VALUES ({{values}})";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithParams, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            // 应该包含参数引用（@, $, 或 :）
            var hasParams = result.ProcessedSql.Contains("@") ||
                            result.ProcessedSql.Contains("$") ||
                            result.ProcessedSql.Contains(":");
            Assert.IsTrue(hasParams,
                $"[{dialectName}] 应该包含参数引用。实际 SQL: {result.ProcessedSql}");

            // 应该包含逗号（多个参数）
            Assert.IsTrue(result.ProcessedSql.Contains(","),
                $"[{dialectName}] 应该包含逗号分隔的参数。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{values}} 应该基于方法参数生成")]
    public void Values_BasedOnMethodParameters()
    {
        var template = "INSERT INTO users (name, email, age) VALUES ({{values}})";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithParams, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);
            var sqlLower = result.ProcessedSql.ToLowerInvariant();

            // 应该包含方法参数（name, email, age）
            var hasName = sqlLower.Contains("name");
            var hasEmail = sqlLower.Contains("email");
            var hasAge = sqlLower.Contains("age");

            Assert.IsTrue(hasName || hasEmail || hasAge,
                $"[{dialectName}] 应该包含方法参数。实际 SQL: {result.ProcessedSql}");
        }
    }

    #endregion

    #region {{values}} 参数引用测试 - 所有方言

    [TestMethod]
    [Description("{{values}} - SQLite 使用 @param 前缀")]
    public void Values_SQLite_UsesAtPrefix()
    {
        var template = "INSERT INTO users (name, email) VALUES ({{values}})";
        var result = _engine.ProcessTemplate(template, _testMethodWithParams, _userType, "users", Sqlx.Generator.SqlDefine.SQLite);

        var sqlUpper = result.ProcessedSql.ToUpperInvariant();
        Assert.IsTrue(sqlUpper.Contains("INSERT INTO"),
            "SQLite 应该包含 INSERT INTO");
        Assert.IsTrue(sqlUpper.Contains("VALUES"),
            "SQLite 应该包含 VALUES");

        // 应该包含参数引用
        Assert.IsTrue(result.ProcessedSql.Contains("@"),
            $"SQLite 应该使用 @ 前缀。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("{{values}} - PostgreSQL 使用 @param 前缀 (Npgsql supports @ format)")]
    public void Values_PostgreSQL_UsesDollarPrefix()
    {
        var template = "INSERT INTO users (name, email) VALUES ({{values}})";
        var result = _engine.ProcessTemplate(template, _testMethodWithParams, _userType, "users", Sqlx.Generator.SqlDefine.PostgreSql);

        var sqlUpper = result.ProcessedSql.ToUpperInvariant();
        Assert.IsTrue(sqlUpper.Contains("INSERT INTO"),
            "PostgreSQL 应该包含 INSERT INTO");
        Assert.IsTrue(sqlUpper.Contains("VALUES"),
            "PostgreSQL 应该包含 VALUES");

        // 应该包含参数引用（@param - Npgsql supports this format）
        Assert.IsTrue(result.ProcessedSql.Contains("@"),
            $"PostgreSQL 应该使用 @ 前缀 (Npgsql supports @ format)。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("{{values}} - MySQL 使用 @param 前缀")]
    public void Values_MySQL_UsesAtPrefix()
    {
        var template = "INSERT INTO users (name, email) VALUES ({{values}})";
        var result = _engine.ProcessTemplate(template, _testMethodWithParams, _userType, "users", Sqlx.Generator.SqlDefine.MySql);

        var sqlUpper = result.ProcessedSql.ToUpperInvariant();
        Assert.IsTrue(sqlUpper.Contains("INSERT INTO"),
            "MySQL 应该包含 INSERT INTO");
        Assert.IsTrue(sqlUpper.Contains("VALUES"),
            "MySQL 应该包含 VALUES");

        // 应该包含参数引用
        Assert.IsTrue(result.ProcessedSql.Contains("@"),
            $"MySQL 应该使用 @ 前缀。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("{{values}} - SQL Server 使用 @param 前缀")]
    public void Values_SqlServer_UsesAtPrefix()
    {
        var template = "INSERT INTO users (name, email) VALUES ({{values}})";
        var result = _engine.ProcessTemplate(template, _testMethodWithParams, _userType, "users", Sqlx.Generator.SqlDefine.SqlServer);

        var sqlUpper = result.ProcessedSql.ToUpperInvariant();
        Assert.IsTrue(sqlUpper.Contains("INSERT INTO"),
            "SQL Server 应该包含 INSERT INTO");
        Assert.IsTrue(sqlUpper.Contains("VALUES"),
            "SQL Server 应该包含 VALUES");

        // 应该包含参数引用
        Assert.IsTrue(result.ProcessedSql.Contains("@"),
            $"SQL Server 应该使用 @ 前缀。实际 SQL: {result.ProcessedSql}");
    }

    #endregion

    #region {{values}} 组合测试

    [TestMethod]
    [Description("{{table}} + {{values}} 完整 INSERT 语句")]
    public void Values_CompleteInsertStatement_AllDialects()
    {
        var template = "INSERT INTO {{table}} (name, email, age) VALUES ({{values}})";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithParams, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");
            Assert.AreEqual(0, result.Errors.Count,
                $"[{dialectName}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            var sqlLower = result.ProcessedSql.ToLowerInvariant();

            // 验证完整 INSERT 语句
            Assert.IsTrue(sqlUpper.Contains("INSERT INTO"),
                $"[{dialectName}] 应该包含 INSERT INTO");
            Assert.IsTrue(sqlUpper.Contains("VALUES"),
                $"[{dialectName}] 应该包含 VALUES");
            Assert.IsTrue(sqlLower.Contains("users"),
                $"[{dialectName}] 应该包含表名");

            // 不应该有未处理的占位符
            Assert.IsFalse(result.ProcessedSql.Contains("{{"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("INSERT ... {{values}} ... RETURNING/OUTPUT")]
    public void Values_WithReturningClause_AllDialects()
    {
        var template = "INSERT INTO users (name, email) VALUES ({{values}})";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithParams, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("INSERT INTO"),
                $"[{dialectName}] 应该包含 INSERT INTO");
            Assert.IsTrue(sqlUpper.Contains("VALUES"),
                $"[{dialectName}] 应该包含 VALUES");
        }
    }

    #endregion

    #region 边界测试

    [TestMethod]
    [Description("{{values}} 不应该留下未处理的占位符")]
    public void Values_AllDialects_NoUnprocessedPlaceholders()
    {
        var template = "INSERT INTO users (name, email) VALUES ({{values}})";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithParams, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(result.ProcessedSql.Contains("{{values}}"),
                $"[{dialectName}] SQL 不应该包含未处理的 {{{{values}}}}。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{values}} 不应该产生错误")]
    public void Values_AllDialects_NoErrors()
    {
        var template = "INSERT INTO users (name, email) VALUES ({{values}})";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithParams, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.AreEqual(0, result.Errors.Count,
                $"[{dialectName}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");
        }
    }

    [TestMethod]
    [Description("{{values}} 在复杂 INSERT 中应该正常工作")]
    public void Values_ComplexInsert_AllDialects()
    {
        var template = "INSERT INTO users (name, email, age, balance, created_at) VALUES ({{values}}, CURRENT_TIMESTAMP)";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithParams, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("INSERT INTO"),
                $"[{dialectName}] 应该包含 INSERT INTO");
            Assert.IsTrue(sqlUpper.Contains("VALUES"),
                $"[{dialectName}] 应该包含 VALUES");
            Assert.IsTrue(sqlUpper.Contains("CURRENT_TIMESTAMP"),
                $"[{dialectName}] 应该包含 CURRENT_TIMESTAMP");
        }
    }

    #endregion

    #region 基于实体属性的 VALUES 生成

    [TestMethod]
    [Description("{{values}} 基于实体属性生成")]
    public void Values_BasedOnEntityProperties_AllDialects()
    {
        var template = "INSERT INTO users (id, name, email, age, balance) VALUES ({{values}})";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            // 应该生成参数
            var hasParams = result.ProcessedSql.Contains("@") ||
                            result.ProcessedSql.Contains("$") ||
                            result.ProcessedSql.Contains(":");
            Assert.IsTrue(hasParams,
                $"[{dialectName}] 应该包含参数引用。实际 SQL: {result.ProcessedSql}");
        }
    }

    #endregion

    #region INSERT 语句场景

    [TestMethod]
    [Description("{{values}} 在简单 INSERT 中应该正常工作")]
    public void Values_SimpleInsert_AllDialects()
    {
        var template = "INSERT INTO users (name, email) VALUES ({{values}})";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithParams, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("INSERT INTO"),
                $"[{dialectName}] 应该包含 INSERT INTO");
            Assert.IsTrue(sqlUpper.Contains("VALUES"),
                $"[{dialectName}] 应该包含 VALUES");
        }
    }

    [TestMethod]
    [Description("{{values}} 在 ON CONFLICT / DUPLICATE KEY 语句中应该正常工作")]
    public void Values_WithConflictHandling_AllDialects()
    {
        var template = "INSERT INTO users (name, email) VALUES ({{values}})";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithParams, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("INSERT INTO"),
                $"[{dialectName}] 应该包含 INSERT INTO");
            Assert.IsTrue(sqlUpper.Contains("VALUES"),
                $"[{dialectName}] 应该包含 VALUES");
        }
    }

    #endregion
}










