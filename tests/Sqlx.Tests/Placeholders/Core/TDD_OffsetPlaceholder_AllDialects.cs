// -----------------------------------------------------------------------
// <copyright file="TDD_OffsetPlaceholder_AllDialects.cs" company="Cricle">
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
/// {{offset}} 占位符在所有数据库方言中的完整测试
/// P0 核心占位符 - 分页偏移量
/// </summary>
[TestClass]
public class TDD_OffsetPlaceholder_AllDialects
{
    private SqlTemplateEngine _engine = null!;
    private Compilation _compilation = null!;
    private IMethodSymbol _testMethod = null!;
    private IMethodSymbol _testMethodWithOffset = null!;
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
        public int Age { get; set; }
        public decimal Balance { get; set; }
    }

    public interface ITestMethods
    {
        Task<List<User>> GetAllAsync(CancellationToken ct = default);
        Task<List<User>> GetPagedAsync(int? offset = default, CancellationToken ct = default);
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
        _testMethod = methodClass.GetMembers("GetAllAsync").OfType<IMethodSymbol>().First();
        _testMethodWithOffset = methodClass.GetMembers("GetPagedAsync").OfType<IMethodSymbol>().First();
    }

    private static string GetDialectName(Sqlx.Generator.SqlDefine dialect)
    {
        return dialect.DatabaseType;
    }

    #region {{offset}} 占位符 - 所有方言

    [TestMethod]
    [Description("{{offset}} 占位符应该在所有方言中生成正确的 OFFSET 语法")]
    public void Offset_AllDialects_GeneratesCorrectSyntax()
    {
        var template = "SELECT * FROM {{table}} LIMIT 10 {{offset}}";

        var expectedSyntax = new Dictionary<string, string[]>
        {
            ["SQLite"] = new[] { "OFFSET" },
            ["PostgreSql"] = new[] { "OFFSET" },
            ["MySql"] = new[] { "OFFSET" },
            ["SqlServer"] = new[] { "OFFSET" } // 可能是 "OFFSET ... ROWS"
        };

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithOffset, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");
            Assert.AreEqual(0, result.Errors.Count,
                $"[{dialectName}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");

            var expected = expectedSyntax[dialectName];
            var sqlUpper = result.ProcessedSql.ToUpperInvariant();

            foreach (var keyword in expected)
            {
                Assert.IsTrue(sqlUpper.Contains(keyword),
                    $"[{dialectName}] SQL 应该包含关键字 '{keyword}'。实际 SQL: {result.ProcessedSql}");
            }
        }
    }

    [TestMethod]
    [Description("{{offset}} 占位符应该生成 OFFSET 关键字")]
    public void Offset_WithParameter_GeneratesOffset()
    {
        var template = "SELECT * FROM {{table}} LIMIT 10 {{offset}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithOffset, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            // 应该包含 OFFSET 关键字
            Assert.IsTrue(result.ProcessedSql.ToUpperInvariant().Contains("OFFSET"),
                $"[{dialectName}] SQL 应该包含 OFFSET 关键字。实际 SQL: {result.ProcessedSql}");

            // 不应该有未处理的占位符
            Assert.IsFalse(result.ProcessedSql.Contains("{{offset}}"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{offset}} 占位符 - SQLite 应该生成 OFFSET 语法")]
    public void Offset_SQLite_GeneratesOffsetSyntax()
    {
        var template = "SELECT * FROM {{table}} LIMIT 10 {{offset}}";
        var result = _engine.ProcessTemplate(template, _testMethodWithOffset, _userType, "users", Sqlx.Generator.SqlDefine.SQLite);

        Assert.IsTrue(result.ProcessedSql.ToUpperInvariant().Contains("OFFSET"),
            $"SQLite 应该生成 OFFSET 语法。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("{{offset}} 占位符 - PostgreSQL 应该生成 OFFSET 语法")]
    public void Offset_PostgreSQL_GeneratesOffsetSyntax()
    {
        var template = "SELECT * FROM {{table}} LIMIT 10 {{offset}}";
        var result = _engine.ProcessTemplate(template, _testMethodWithOffset, _userType, "users", Sqlx.Generator.SqlDefine.PostgreSql);

        Assert.IsTrue(result.ProcessedSql.ToUpperInvariant().Contains("OFFSET"),
            $"PostgreSQL 应该生成 OFFSET 语法。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("{{offset}} 占位符 - MySQL 应该生成 OFFSET 语法")]
    public void Offset_MySQL_GeneratesOffsetSyntax()
    {
        var template = "SELECT * FROM {{table}} LIMIT 10 {{offset}}";
        var result = _engine.ProcessTemplate(template, _testMethodWithOffset, _userType, "users", Sqlx.Generator.SqlDefine.MySql);

        Assert.IsTrue(result.ProcessedSql.ToUpperInvariant().Contains("OFFSET"),
            $"MySQL 应该生成 OFFSET 语法。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("{{offset}} 占位符 - SQL Server 应该生成 OFFSET 语法")]
    public void Offset_SqlServer_GeneratesOffsetSyntax()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id LIMIT 10 {{offset}}";
        var result = _engine.ProcessTemplate(template, _testMethodWithOffset, _userType, "users", Sqlx.Generator.SqlDefine.SqlServer);

        Assert.IsTrue(result.ProcessedSql.ToUpperInvariant().Contains("OFFSET"),
            $"SQL Server 应该生成 OFFSET 语法。实际 SQL: {result.ProcessedSql}");
    }

    #endregion

    #region {{offset}} 零值测试

    [TestMethod]
    [Description("{{offset}} 零值应该在所有方言中正常工作")]
    public void Offset_ZeroValue_AllDialects()
    {
        var template = "SELECT * FROM {{table}} LIMIT 10 OFFSET 0";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");
            Assert.AreEqual(0, result.Errors.Count,
                $"[{dialectName}] 零值 OFFSET 不应该产生错误。错误: {string.Join(", ", result.Errors)}");
        }
    }

    #endregion

    #region {{offset}} 与其他占位符组合

    [TestMethod]
    [Description("{{offset}} 与 {{limit}} 组合应该在所有方言中正常工作")]
    public void Offset_WithLimit_AllDialects()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}} {{offset}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithOffset, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            // 应该包含 OFFSET 关键字
            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            var hasOffset = sqlUpper.Contains("OFFSET");

            Assert.IsTrue(hasOffset,
                $"[{dialectName}] SQL 应该包含 OFFSET 关键字。实际 SQL: {result.ProcessedSql}");

            // 不应该有未处理的占位符
            Assert.IsFalse(result.ProcessedSql.Contains("{{"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{offset}} 与 {{orderby}} 组合应该在所有方言中正常工作")]
    public void Offset_WithOrderBy_AllDialects()
    {
        var template = "SELECT * FROM {{table}} {{orderby id}} LIMIT 10 {{offset}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithOffset, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();

            // 应该包含 ORDER BY
            Assert.IsTrue(sqlUpper.Contains("ORDER BY"),
                $"[{dialectName}] 应该包含 ORDER BY。实际 SQL: {result.ProcessedSql}");

            // 应该包含 OFFSET
            Assert.IsTrue(sqlUpper.Contains("OFFSET"),
                $"[{dialectName}] 应该包含 OFFSET。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("完整分页查询：{{columns}} + {{where}} + {{orderby}} + {{limit}} + {{offset}}")]
    public void Offset_CompleteQuery_AllDialects()
    {
        var template = "SELECT {{columns}} FROM {{table}} WHERE age >= @minAge {{orderby age --desc}} {{limit}} {{offset}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithOffset, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");
            Assert.AreEqual(0, result.Errors.Count,
                $"[{dialectName}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();

            // 验证所有占位符都被处理
            Assert.IsTrue(sqlUpper.Contains("SELECT"),
                $"[{dialectName}] 应该包含 SELECT");
            Assert.IsTrue(sqlUpper.Contains("FROM"),
                $"[{dialectName}] 应该包含 FROM");
            Assert.IsTrue(sqlUpper.Contains("WHERE"),
                $"[{dialectName}] 应该包含 WHERE");
            Assert.IsTrue(sqlUpper.Contains("ORDER BY"),
                $"[{dialectName}] 应该包含 ORDER BY");

            // 不应该有未处理的占位符
            Assert.IsFalse(result.ProcessedSql.Contains("{{"),
                $"[{dialectName}] 不应该包含未处理的占位符。实际 SQL: {result.ProcessedSql}");
        }
    }

    #endregion

    #region 边界测试

    [TestMethod]
    [Description("{{offset}} 不应该在 SQL 中留下未处理的占位符")]
    public void Offset_AllDialects_NoUnprocessedPlaceholders()
    {
        var template = "SELECT * FROM {{table}} LIMIT 10 {{offset}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithOffset, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            // 不应该包含未处理的占位符
            Assert.IsFalse(result.ProcessedSql.Contains("{{offset}}"),
                $"[{dialectName}] SQL 不应该包含未处理的 {{{{offset}}}}。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{offset}} 不应该产生错误")]
    public void Offset_AllDialects_NoErrors()
    {
        var template = "SELECT * FROM {{table}} LIMIT 10 {{offset}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithOffset, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.AreEqual(0, result.Errors.Count,
                $"[{dialectName}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");
        }
    }

    #endregion

    #region SQL Server 特殊测试

    [TestMethod]
    [Description("SQL Server - {{offset}} 需要 ORDER BY")]
    public void Offset_SqlServer_RequiresOrderBy()
    {
        var templateWithOrderBy = "SELECT * FROM {{table}} ORDER BY id {{offset}}";
        var resultWithOrderBy = _engine.ProcessTemplate(templateWithOrderBy, _testMethodWithOffset, _userType, "users", Sqlx.Generator.SqlDefine.SqlServer);

        Assert.IsFalse(string.IsNullOrEmpty(resultWithOrderBy.ProcessedSql),
            "带 ORDER BY 的模板应该生成 SQL");

        // SQL Server 的 OFFSET 需要 ORDER BY
        var sqlUpper = resultWithOrderBy.ProcessedSql.ToUpperInvariant();
        Assert.IsTrue(sqlUpper.Contains("ORDER BY"),
            "SQL Server 的 OFFSET 应该需要 ORDER BY");
    }

    #endregion
}

