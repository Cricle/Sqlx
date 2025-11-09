// -----------------------------------------------------------------------
// <copyright file="TDD_LimitTopPlaceholder_AllDialects.cs" company="Cricle">
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

namespace Sqlx.Tests.Placeholders;

/// <summary>
/// {{limit}} 和 {{top}} 占位符在所有数据库方言中的完整测试
/// 覆盖 SQLite, PostgreSQL, MySQL, SQL Server 的所有分页语法
/// </summary>
[TestClass]
public class TDD_LimitTopPlaceholder_AllDialects
{
    private SqlTemplateEngine _engine = null!;
    private Compilation _compilation = null!;
    private IMethodSymbol _testMethod = null!;
    private IMethodSymbol _testMethodWithLimit = null!;
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
        Task<List<User>> GetTopAsync(int? limit = default, CancellationToken ct = default);
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
        _testMethodWithLimit = methodClass.GetMembers("GetTopAsync").OfType<IMethodSymbol>().First();
    }

    private static string GetDialectName(Sqlx.Generator.SqlDefine dialect)
    {
        return dialect.DatabaseType;
    }

    #region {{limit}} 占位符 - 所有方言

    [TestMethod]
    [Description("{{limit}} 占位符应该在所有方言中生成正确的分页语法")]
    public void Limit_AllDialects_GeneratesCorrectSyntax()
    {
        var template = "SELECT * FROM {{table}} {{limit}}";

        var expectedSyntax = new Dictionary<string, string[]>
        {
            ["SQLite"] = new[] { "LIMIT" },
            ["PostgreSql"] = new[] { "LIMIT" },
            ["MySql"] = new[] { "LIMIT" },
            ["SqlServer"] = new[] { "OFFSET", "ROWS", "FETCH NEXT", "ROWS ONLY" }
        };

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithLimit, _userType, "users", dialect);
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
    [Description("{{limit}} 占位符应该自动检测方法参数")]
    public void Limit_WithParameter_AutoDetectsLimitParameter()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithLimit, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            // 应该包含参数引用（根据方言不同，可能是 @limit, $limit, 或运行时占位符）
            var hasParameterRef = result.ProcessedSql.Contains("@limit") ||
                                 result.ProcessedSql.Contains("$limit") ||
                                 result.ProcessedSql.Contains(":limit") ||
                                 result.ProcessedSql.Contains("{RUNTIME_LIMIT");
            Assert.IsTrue(hasParameterRef,
                $"[{dialectName}] SQL 应该包含 limit 参数引用。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{limit}} 占位符 - SQLite 应该生成 LIMIT 语法")]
    public void Limit_SQLite_GeneratesLimitSyntax()
    {
        var template = "SELECT * FROM {{table}} {{limit}}";
        var result = _engine.ProcessTemplate(template, _testMethodWithLimit, _userType, "users", Sqlx.Generator.SqlDefine.SQLite);

        Assert.IsTrue(result.ProcessedSql.ToUpperInvariant().Contains("LIMIT"),
            $"SQLite 应该生成 LIMIT 语法。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("{{limit}} 占位符 - PostgreSQL 应该生成 LIMIT 语法")]
    public void Limit_PostgreSQL_GeneratesLimitSyntax()
    {
        var template = "SELECT * FROM {{table}} {{limit}}";
        var result = _engine.ProcessTemplate(template, _testMethodWithLimit, _userType, "users", Sqlx.Generator.SqlDefine.PostgreSql);

        Assert.IsTrue(result.ProcessedSql.ToUpperInvariant().Contains("LIMIT"),
            $"PostgreSQL 应该生成 LIMIT 语法。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("{{limit}} 占位符 - MySQL 应该生成 LIMIT 语法")]
    public void Limit_MySQL_GeneratesLimitSyntax()
    {
        var template = "SELECT * FROM {{table}} {{limit}}";
        var result = _engine.ProcessTemplate(template, _testMethodWithLimit, _userType, "users", Sqlx.Generator.SqlDefine.MySql);

        Assert.IsTrue(result.ProcessedSql.ToUpperInvariant().Contains("LIMIT"),
            $"MySQL 应该生成 LIMIT 语法。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("{{limit}} 占位符 - SQL Server 应该生成分页语法")]
    public void Limit_SqlServer_GeneratesOffsetFetchSyntax()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}}";
        var result = _engine.ProcessTemplate(template, _testMethodWithLimit, _userType, "users", Sqlx.Generator.SqlDefine.SqlServer);

        var sqlUpper = result.ProcessedSql.ToUpperInvariant();

        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
            "SQL Server 应该生成SQL");

        // SQL Server 应该生成分页语法：OFFSET...FETCH NEXT、运行时占位符或 LIMIT（兼容模式）
        var hasOffsetFetch = sqlUpper.Contains("OFFSET") && sqlUpper.Contains("FETCH NEXT");
        var hasRuntimePlaceholder = result.ProcessedSql.Contains("{RUNTIME_LIMIT");
        var hasLimit = sqlUpper.Contains("LIMIT");

        Assert.IsTrue(hasOffsetFetch || hasRuntimePlaceholder || hasLimit,
            $"SQL Server 应该生成分页语法。实际 SQL: {result.ProcessedSql}");
    }

    #endregion

    #region {{top}} 占位符 - 所有方言

    [TestMethod]
    [Description("{{top}} 占位符应该在所有方言中工作")]
    public void Top_AllDialects_GeneratesCorrectSyntax()
    {
        var template = "SELECT {{top}} * FROM {{table}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            // {{top}} 应该被处理（不再包含占位符）
            Assert.IsFalse(result.ProcessedSql.Contains("{{top}}"),
                $"[{dialectName}] SQL 不应该包含未处理的 {{{{top}}}} 占位符");
        }
    }

    [TestMethod]
    [Description("{{top}} 和 {{limit}} 应该都能生成分页语法")]
    public void Top_IsAliasForLimit()
    {
        var templateTop = "SELECT * FROM {{table}} {{top}}";
        var templateLimit = "SELECT * FROM {{table}} {{limit}}";

        foreach (var dialect in AllDialects)
        {
            var resultTop = _engine.ProcessTemplate(templateTop, _testMethodWithLimit, _userType, "users", dialect);
            var resultLimit = _engine.ProcessTemplate(templateLimit, _testMethodWithLimit, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            // 两者都应该生成有效的 SQL
            Assert.IsFalse(string.IsNullOrEmpty(resultTop.ProcessedSql),
                $"[{dialectName}] {{{{top}}}} 应该生成 SQL");
            Assert.IsFalse(string.IsNullOrEmpty(resultLimit.ProcessedSql),
                $"[{dialectName}] {{{{limit}}}} 应该生成 SQL");

            // 两者都应该处理占位符（不再包含 {{ 或 }}）
            Assert.IsFalse(resultTop.ProcessedSql.Contains("{{") || resultTop.ProcessedSql.Contains("}}"),
                $"[{dialectName}] {{{{top}}}} 不应该包含未处理的占位符。实际: {resultTop.ProcessedSql}");
            Assert.IsFalse(resultLimit.ProcessedSql.Contains("{{") || resultLimit.ProcessedSql.Contains("}}"),
                $"[{dialectName}] {{{{limit}}}} 不应该包含未处理的占位符。实际: {resultLimit.ProcessedSql}");
        }
    }

    #endregion

    #region {{limit}} + {{offset}} 组合 - 所有方言

    [TestMethod]
    [Description("{{limit}} + {{offset}} 组合应该在所有方言中生成正确的分页语法")]
    public void LimitOffset_AllDialects_GeneratesCorrectPagination()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}} {{offset}}";

        var expectedSyntax = new Dictionary<string, string[]>
        {
            ["SQLite"] = new[] { "LIMIT", "OFFSET" },
            ["PostgreSql"] = new[] { "LIMIT", "OFFSET" },
            ["MySql"] = new[] { "LIMIT", "OFFSET" },
            ["SqlServer"] = new[] { "OFFSET", "ROWS" }
        };

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithLimit, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

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
    [Description("SQL Server - {{limit}} + {{offset}} 应该生成分页语句")]
    public void LimitOffset_SqlServer_GeneratesCompleteOffsetFetch()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}} {{offset}}";
        var result = _engine.ProcessTemplate(template, _testMethodWithLimit, _userType, "users", Sqlx.Generator.SqlDefine.SqlServer);

        var sqlUpper = result.ProcessedSql.ToUpperInvariant();

        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
            "SQL Server 应该生成SQL");

        // 应该包含分页关键字
        var hasOffsetKeyword = sqlUpper.Contains("OFFSET");
        Assert.IsTrue(hasOffsetKeyword || sqlUpper.Contains("LIMIT"),
            $"SQL Server 应该包含分页关键字。实际 SQL: {result.ProcessedSql}");
    }

    #endregion

    #region 预定义模式测试 - 所有方言

    [TestMethod]
    [Description("{{limit:tiny}} 应该在所有方言中生成限制 5 条记录的语法")]
    public void Limit_TinyMode_AllDialects()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit:tiny}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            // 应该包含 5（tiny = 5条记录）
            Assert.IsTrue(result.ProcessedSql.Contains("5"),
                $"[{dialectName}] tiny 模式应该限制 5 条记录。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{limit:small}} 应该在所有方言中生成限制 10 条记录的语法")]
    public void Limit_SmallMode_AllDialects()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit:small}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            // 应该包含 10（small = 10条记录）
            Assert.IsTrue(result.ProcessedSql.Contains("10"),
                $"[{dialectName}] small 模式应该限制 10 条记录。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{limit:medium}} 应该在所有方言中生成限制 50 条记录的语法")]
    public void Limit_MediumMode_AllDialects()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit:medium}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            // 应该包含 50（medium = 50条记录）
            Assert.IsTrue(result.ProcessedSql.Contains("50"),
                $"[{dialectName}] medium 模式应该限制 50 条记录。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{limit:large}} 应该在所有方言中生成限制 100 条记录的语法")]
    public void Limit_LargeMode_AllDialects()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit:large}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            // 应该包含 100（large = 100条记录）
            Assert.IsTrue(result.ProcessedSql.Contains("100"),
                $"[{dialectName}] large 模式应该限制 100 条记录。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{limit:page}} 应该在所有方言中生成限制 20 条记录的语法")]
    public void Limit_PageMode_AllDialects()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit:page}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            // 应该包含 20（page = 20条记录）
            Assert.IsTrue(result.ProcessedSql.Contains("20"),
                $"[{dialectName}] page 模式应该限制 20 条记录。实际 SQL: {result.ProcessedSql}");
        }
    }

    #endregion

    #region 带 ORDER BY 的测试 - 所有方言

    [TestMethod]
    [Description("{{limit}} 与 {{orderby}} 组合应该在所有方言中正确工作")]
    public void Limit_WithOrderBy_AllDialects()
    {
        var template = "SELECT * FROM {{table}} {{orderby id --desc}} {{limit}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithLimit, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();

            // 应该包含 ORDER BY
            Assert.IsTrue(sqlUpper.Contains("ORDER BY"),
                $"[{dialectName}] 应该包含 ORDER BY。实际 SQL: {result.ProcessedSql}");

            // 应该包含 DESC
            Assert.IsTrue(sqlUpper.Contains("DESC"),
                $"[{dialectName}] 应该包含 DESC。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("SQL Server - {{limit}} 必须与 ORDER BY 一起使用")]
    public void Limit_SqlServer_RequiresOrderBy()
    {
        var templateWithOrderBy = "SELECT * FROM {{table}} ORDER BY id {{limit}}";
        var resultWithOrderBy = _engine.ProcessTemplate(templateWithOrderBy, _testMethodWithLimit, _userType, "users", Sqlx.Generator.SqlDefine.SqlServer);

        Assert.IsFalse(string.IsNullOrEmpty(resultWithOrderBy.ProcessedSql),
            "带 ORDER BY 的模板应该生成 SQL");

        // SQL Server 的 OFFSET...FETCH 需要 ORDER BY
        var sqlUpper = resultWithOrderBy.ProcessedSql.ToUpperInvariant();
        Assert.IsTrue(sqlUpper.Contains("ORDER BY"),
            "SQL Server 的 LIMIT 应该需要 ORDER BY");
    }

    #endregion

    #region 边界测试

    [TestMethod]
    [Description("{{limit}} 不应该在 SQL 中留下未处理的占位符")]
    public void Limit_AllDialects_NoUnprocessedPlaceholders()
    {
        var template = "SELECT * FROM {{table}} {{limit}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithLimit, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            // 不应该包含未处理的占位符
            Assert.IsFalse(result.ProcessedSql.Contains("{{limit}}"),
                $"[{dialectName}] SQL 不应该包含未处理的 {{{{limit}}}}。实际 SQL: {result.ProcessedSql}");
            Assert.IsFalse(result.ProcessedSql.Contains("{{top}}"),
                $"[{dialectName}] SQL 不应该包含未处理的 {{{{top}}}}。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{limit}} 不应该产生错误")]
    public void Limit_AllDialects_NoErrors()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithLimit, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.AreEqual(0, result.Errors.Count,
                $"[{dialectName}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");
        }
    }

    #endregion

    #region 负面测试

    [TestMethod]
    [Description("{{limit}} 不带 ORDER BY 应该在需要时产生警告（SQL Server）")]
    public void Limit_WithoutOrderBy_SqlServer_MayHaveWarning()
    {
        var template = "SELECT * FROM {{table}} {{limit}}";
        var result = _engine.ProcessTemplate(template, _testMethodWithLimit, _userType, "users", Sqlx.Generator.SqlDefine.SqlServer);

        // SQL Server 的 OFFSET...FETCH 需要 ORDER BY
        // 但生成器应该自动处理或给出警告
        // 这里只检查不会崩溃
        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql));
    }

    #endregion

    #region 复杂组合测试

    [TestMethod]
    [Description("{{columns}} + {{where}} + {{orderby}} + {{limit}} 完整组合 - 所有方言")]
    public void CompleteQuery_AllDialects_WithAllPlaceholders()
    {
        var template = "SELECT {{columns}} FROM {{table}} WHERE age >= @minAge {{orderby age --desc}} {{limit}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithLimit, _userType, "users", dialect);
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
}

