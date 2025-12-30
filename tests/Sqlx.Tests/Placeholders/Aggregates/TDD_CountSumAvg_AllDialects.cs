// -----------------------------------------------------------------------
// <copyright file="TDD_CountSumAvg_AllDialects.cs" company="Cricle">
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

namespace Sqlx.Tests.Placeholders.Aggregates;

/// <summary>
/// {{count}}, {{sum}}, {{avg}} 聚合函数占位符在所有数据库方言中的完整测试
/// P1 聚合函数占位符
/// </summary>
[TestClass]
public class TDD_CountSumAvg_AllDialects
{
    private SqlTemplateEngine _engine = null!;
    private Compilation _compilation = null!;
    private IMethodSymbol _testMethod = null!;
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
        Task<int> CountAsync(CancellationToken ct = default);
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
        _testMethod = methodClass.GetMembers("CountAsync").OfType<IMethodSymbol>().First();
    }

    private static string GetDialectName(Sqlx.Generator.SqlDefine dialect)
    {
        return dialect.DatabaseType;
    }

    #region {{count}} 占位符测试

    [TestMethod]
    [Description("{{count}} 占位符应该在所有方言中生成 COUNT")]
    public void Count_AllDialects_GeneratesCount()
    {
        var template = "SELECT {{count}} FROM users";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");
            Assert.AreEqual(0, result.Errors.Count,
                $"[{dialectName}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("COUNT"),
                $"[{dialectName}] 应该包含 COUNT。实际 SQL: {result.ProcessedSql}");

            Assert.IsFalse(result.ProcessedSql.Contains("{{count}}"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符");
        }
    }

    [TestMethod]
    [Description("{{count}} 默认应该生成 COUNT 函数")]
    public void Count_Default_GeneratesCountFunction()
    {
        var template = "SELECT {{count}} FROM users";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("COUNT"),
                $"[{dialectName}] 应该包含 COUNT。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{count:id}} 应该生成 COUNT(id)")]
    public void Count_Column_GeneratesCountColumn()
    {
        var template = "SELECT {{count:id}} FROM users";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            var sqlLower = result.ProcessedSql.ToLowerInvariant();

            Assert.IsTrue(sqlUpper.Contains("COUNT"),
                $"[{dialectName}] 应该包含 COUNT");
            Assert.IsTrue(sqlLower.Contains("id"),
                $"[{dialectName}] 应该包含 id。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{count}} 在 WHERE 子句中应该正常工作")]
    public void Count_WithWhere_AllDialects()
    {
        var template = "SELECT {{count}} FROM users WHERE age >= 18";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("COUNT"),
                $"[{dialectName}] 应该包含 COUNT");
            Assert.IsTrue(sqlUpper.Contains("WHERE"),
                $"[{dialectName}] 应该包含 WHERE");
        }
    }

    #endregion

    #region {{sum}} 占位符测试

    [TestMethod]
    [Description("{{sum}} 占位符应该在所有方言中生成 SUM")]
    public void Sum_AllDialects_GeneratesSum()
    {
        var template = "SELECT {{sum:balance}} FROM users";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");
            Assert.AreEqual(0, result.Errors.Count,
                $"[{dialectName}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("SUM"),
                $"[{dialectName}] 应该包含 SUM。实际 SQL: {result.ProcessedSql}");

            Assert.IsFalse(result.ProcessedSql.Contains("{{sum}}"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符");
        }
    }

    [TestMethod]
    [Description("{{sum:balance}} 应该生成 SUM(balance)")]
    public void Sum_Column_GeneratesSumColumn()
    {
        var template = "SELECT {{sum:balance}} FROM users";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            var sqlLower = result.ProcessedSql.ToLowerInvariant();

            Assert.IsTrue(sqlUpper.Contains("SUM"),
                $"[{dialectName}] 应该包含 SUM");
            Assert.IsTrue(sqlLower.Contains("balance"),
                $"[{dialectName}] 应该包含 balance。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{sum}} 在 WHERE 子句中应该正常工作")]
    public void Sum_WithWhere_AllDialects()
    {
        var template = "SELECT {{sum:balance}} FROM users WHERE age >= 18";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("SUM"),
                $"[{dialectName}] 应该包含 SUM");
            Assert.IsTrue(sqlUpper.Contains("WHERE"),
                $"[{dialectName}] 应该包含 WHERE");
        }
    }

    [TestMethod]
    [Description("{{sum}} 在 GROUP BY 中应该正常工作")]
    public void Sum_WithGroupBy_AllDialects()
    {
        var template = "SELECT name, {{sum:balance}} FROM users GROUP BY name";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("SUM"),
                $"[{dialectName}] 应该包含 SUM");
            Assert.IsTrue(sqlUpper.Contains("GROUP BY"),
                $"[{dialectName}] 应该包含 GROUP BY");
        }
    }

    #endregion

    #region {{avg}} 占位符测试

    [TestMethod]
    [Description("{{avg}} 占位符应该在所有方言中生成 AVG")]
    public void Avg_AllDialects_GeneratesAvg()
    {
        var template = "SELECT {{avg:age}} FROM users";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");
            Assert.AreEqual(0, result.Errors.Count,
                $"[{dialectName}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("AVG"),
                $"[{dialectName}] 应该包含 AVG。实际 SQL: {result.ProcessedSql}");

            Assert.IsFalse(result.ProcessedSql.Contains("{{avg}}"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符");
        }
    }

    [TestMethod]
    [Description("{{avg:age}} 应该生成 AVG(age)")]
    public void Avg_Column_GeneratesAvgColumn()
    {
        var template = "SELECT {{avg:age}} FROM users";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            var sqlLower = result.ProcessedSql.ToLowerInvariant();

            Assert.IsTrue(sqlUpper.Contains("AVG"),
                $"[{dialectName}] 应该包含 AVG");
            Assert.IsTrue(sqlLower.Contains("age"),
                $"[{dialectName}] 应该包含 age。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{avg}} 在 WHERE 子句中应该正常工作")]
    public void Avg_WithWhere_AllDialects()
    {
        var template = "SELECT {{avg:balance}} FROM users WHERE age >= 18";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("AVG"),
                $"[{dialectName}] 应该包含 AVG");
            Assert.IsTrue(sqlUpper.Contains("WHERE"),
                $"[{dialectName}] 应该包含 WHERE");
        }
    }

    [TestMethod]
    [Description("{{avg}} 在 GROUP BY 中应该正常工作")]
    public void Avg_WithGroupBy_AllDialects()
    {
        var template = "SELECT name, {{avg:age}} FROM users GROUP BY name";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("AVG"),
                $"[{dialectName}] 应该包含 AVG");
            Assert.IsTrue(sqlUpper.Contains("GROUP BY"),
                $"[{dialectName}] 应该包含 GROUP BY");
        }
    }

    #endregion

    #region 聚合函数组合测试

    [TestMethod]
    [Description("{{count}} + {{sum}} + {{avg}} 组合查询")]
    public void Aggregates_Combined_AllDialects()
    {
        var template = "SELECT {{count}}, {{sum:balance}}, {{avg:age}} FROM users";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("COUNT"),
                $"[{dialectName}] 应该包含 COUNT");
            Assert.IsTrue(sqlUpper.Contains("SUM"),
                $"[{dialectName}] 应该包含 SUM");
            Assert.IsTrue(sqlUpper.Contains("AVG"),
                $"[{dialectName}] 应该包含 AVG");

            Assert.IsFalse(result.ProcessedSql.Contains("{{"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("聚合函数 + WHERE + GROUP BY + HAVING 完整查询")]
    public void Aggregates_CompleteQuery_AllDialects()
    {
        var template = "SELECT name, {{count}}, {{avg:age}} FROM users WHERE age >= 18 GROUP BY name HAVING COUNT(*) > 5";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("SELECT"),
                $"[{dialectName}] 应该包含 SELECT");
            Assert.IsTrue(sqlUpper.Contains("COUNT"),
                $"[{dialectName}] 应该包含 COUNT");
            Assert.IsTrue(sqlUpper.Contains("AVG"),
                $"[{dialectName}] 应该包含 AVG");
            Assert.IsTrue(sqlUpper.Contains("WHERE"),
                $"[{dialectName}] 应该包含 WHERE");
            Assert.IsTrue(sqlUpper.Contains("GROUP BY"),
                $"[{dialectName}] 应该包含 GROUP BY");
            Assert.IsTrue(sqlUpper.Contains("HAVING"),
                $"[{dialectName}] 应该包含 HAVING");
        }
    }

    #endregion

    #region 边界测试

    [TestMethod]
    [Description("聚合函数不应该留下未处理的占位符")]
    public void Aggregates_AllDialects_NoUnprocessedPlaceholders()
    {
        var templates = new[]
        {
            "SELECT {{count}} FROM users",
            "SELECT {{sum:balance}} FROM users",
            "SELECT {{avg:age}} FROM users"
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
    [Description("聚合函数不应该产生错误")]
    public void Aggregates_AllDialects_NoErrors()
    {
        var templates = new[]
        {
            "SELECT {{count}} FROM users",
            "SELECT {{sum:balance}} FROM users",
            "SELECT {{avg:age}} FROM users"
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

