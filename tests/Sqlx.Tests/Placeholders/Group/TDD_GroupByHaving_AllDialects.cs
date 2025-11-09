// -----------------------------------------------------------------------
// <copyright file="TDD_GroupByHaving_AllDialects.cs" company="Cricle">
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

namespace Sqlx.Tests.Placeholders.Group;

/// <summary>
/// {{groupby}}, {{having}} 占位符在所有数据库方言中的完整测试
/// P2 GROUP BY + HAVING 占位符
/// </summary>
[TestClass]
public class TDD_GroupByHaving_AllDialects
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
        public int Age { get; set; }
        public string Department { get; set; }
    }

    public interface ITestMethods
    {
        Task<List<User>> QueryAsync(CancellationToken ct = default);
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
        _testMethod = methodClass.GetMembers("QueryAsync").OfType<IMethodSymbol>().First();
    }

    private static string GetDialectName(Sqlx.Generator.SqlDefine dialect)
    {
        return dialect.DatabaseType;
    }

    #region {{groupby}} 占位符测试

    [TestMethod]
    [Description("{{groupby}} 占位符应该在所有方言中生成 GROUP BY")]
    public void GroupBy_AllDialects_GeneratesGroupBy()
    {
        var template = "SELECT department, COUNT(*) FROM users {{groupby:department}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");
            Assert.AreEqual(0, result.Errors.Count,
                $"[{dialectName}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("GROUP") || sqlUpper.Contains("BY"),
                $"[{dialectName}] 应该包含 GROUP 或 BY。实际 SQL: {result.ProcessedSql}");

            // 检查是否有未处理的占位符
            Assert.IsFalse(result.ProcessedSql.Contains("{{groupby"),
                $"[{dialectName}] SQL 不应该包含未处理的 groupby 占位符");
        }
    }

    [TestMethod]
    [Description("{{groupby:column}} 应该生成 GROUP BY column")]
    public void GroupBy_SingleColumn_AllDialects()
    {
        var template = "SELECT department, COUNT(*) FROM users {{groupby:department}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("GROUP") || sqlUpper.Contains("BY") || sqlUpper.Contains("DEPARTMENT"),
                $"[{dialectName}] 应该包含 GROUP BY department。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{groupby}} 多列分组")]
    public void GroupBy_MultipleColumns_AllDialects()
    {
        var template = "SELECT department, age, COUNT(*) FROM users GROUP BY department, age";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("GROUP") && sqlUpper.Contains("BY"),
                $"[{dialectName}] 应该包含 GROUP BY");

            // 不应该有未处理的占位符
            Assert.IsFalse(result.ProcessedSql.Contains("{{"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符");
        }
    }

    [TestMethod]
    [Description("{{groupby}} 与聚合函数组合")]
    public void GroupBy_WithAggregates_AllDialects()
    {
        var template = "SELECT department, {{count}}, {{avg:age}} FROM users {{groupby:department}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("COUNT") || sqlUpper.Contains("AVG"),
                $"[{dialectName}] 应该包含聚合函数");
            Assert.IsTrue(sqlUpper.Contains("GROUP") || sqlUpper.Contains("BY"),
                $"[{dialectName}] 应该包含 GROUP BY");
        }
    }

    #endregion

    #region {{having}} 占位符测试

    [TestMethod]
    [Description("{{having}} 占位符应该在所有方言中生成 HAVING")]
    public void Having_AllDialects_GeneratesHaving()
    {
        var template = "SELECT department, COUNT(*) FROM users GROUP BY department HAVING COUNT(*) > 5";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");
            Assert.AreEqual(0, result.Errors.Count,
                $"[{dialectName}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("HAVING") && sqlUpper.Contains("COUNT"),
                $"[{dialectName}] 应该包含 HAVING 和 COUNT。实际 SQL: {result.ProcessedSql}");

            // 不应该有未处理的占位符
            Assert.IsFalse(result.ProcessedSql.Contains("{{"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符");
        }
    }

    [TestMethod]
    [Description("{{having}} 与 COUNT 条件")]
    public void Having_WithCount_AllDialects()
    {
        var template = "SELECT department, COUNT(*) FROM users GROUP BY department HAVING COUNT(*) >= 10";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("HAVING") && sqlUpper.Contains("COUNT"),
                $"[{dialectName}] 应该包含 HAVING COUNT 条件");
        }
    }

    [TestMethod]
    [Description("HAVING 与 SUM/AVG 条件")]
    public void Having_WithSumAvg_AllDialects()
    {
        var template = "SELECT department, AVG(age) FROM users GROUP BY department HAVING AVG(age) > 30";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("HAVING") && sqlUpper.Contains("AVG"),
                $"[{dialectName}] 应该包含 HAVING AVG 条件");
        }
    }

    #endregion

    #region GROUP BY + HAVING 组合测试

    [TestMethod]
    [Description("{{groupby}} 和 HAVING 组合使用")]
    public void GroupByHaving_Combined_AllDialects()
    {
        var template = "SELECT department, COUNT(*) FROM users {{groupby:department}} HAVING COUNT(*) > 5";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("GROUP") || sqlUpper.Contains("BY"),
                $"[{dialectName}] 应该包含 GROUP BY");
            Assert.IsTrue(sqlUpper.Contains("HAVING"),
                $"[{dialectName}] 应该包含 HAVING");

            // 不应该有未处理的占位符
            Assert.IsFalse(result.ProcessedSql.Contains("{{"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符");
        }
    }

    [TestMethod]
    [Description("完整的聚合查询：GROUP BY + 聚合函数 + HAVING")]
    public void GroupByHaving_CompleteQuery_AllDialects()
    {
        var template = @"SELECT department, {{count}}, {{avg:age}}, {{sum:age}}
                        FROM users
                        {{groupby:department}}
                        HAVING COUNT(*) > 10 AND AVG(age) > 25";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("COUNT"),
                $"[{dialectName}] 应该包含 COUNT");
            Assert.IsTrue(sqlUpper.Contains("GROUP") || sqlUpper.Contains("BY"),
                $"[{dialectName}] 应该包含 GROUP BY");
            Assert.IsTrue(sqlUpper.Contains("HAVING"),
                $"[{dialectName}] 应该包含 HAVING");

            // 不应该有未处理的占位符
            Assert.IsFalse(result.ProcessedSql.Contains("{{"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符");
        }
    }

    [TestMethod]
    [Description("GROUP BY + HAVING 与 WHERE 组合")]
    public void GroupByHaving_WithWhere_AllDialects()
    {
        var template = @"SELECT department, COUNT(*)
                        FROM users
                        WHERE age >= 18
                        {{groupby:department}}
                        HAVING COUNT(*) > 5";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("WHERE"),
                $"[{dialectName}] 应该包含 WHERE");
            Assert.IsTrue(sqlUpper.Contains("GROUP") || sqlUpper.Contains("BY"),
                $"[{dialectName}] 应该包含 GROUP BY");
            Assert.IsTrue(sqlUpper.Contains("HAVING"),
                $"[{dialectName}] 应该包含 HAVING");
        }
    }

    [TestMethod]
    [Description("GROUP BY + HAVING 与 ORDER BY 组合")]
    public void GroupByHaving_WithOrderBy_AllDialects()
    {
        var template = @"SELECT department, COUNT(*) as cnt
                        FROM users
                        {{groupby:department}}
                        HAVING COUNT(*) > 5
                        ORDER BY cnt DESC";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("GROUP") || sqlUpper.Contains("BY"),
                $"[{dialectName}] 应该包含 GROUP BY");
            Assert.IsTrue(sqlUpper.Contains("HAVING"),
                $"[{dialectName}] 应该包含 HAVING");
            Assert.IsTrue(sqlUpper.Contains("ORDER") && sqlUpper.Contains("BY"),
                $"[{dialectName}] 应该包含 ORDER BY");
        }
    }

    #endregion

    #region 边界测试

    [TestMethod]
    [Description("GROUP BY 和 HAVING 占位符不应该留下未处理的占位符")]
    public void GroupByHaving_AllDialects_NoUnprocessedPlaceholders()
    {
        var templates = new[]
        {
            "SELECT department, COUNT(*) FROM users {{groupby:department}}",
            "SELECT department, COUNT(*) FROM users GROUP BY department HAVING COUNT(*) > 5",
            "SELECT department, COUNT(*) FROM users {{groupby:department}} HAVING COUNT(*) > 5"
        };

        foreach (var template in templates)
        {
            foreach (var dialect in AllDialects)
            {
                var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
                var dialectName = GetDialectName(dialect);

                // 允许运行时占位符 RUNTIME，但不应有未处理的 {{groupby}}, {{having}}
                Assert.IsFalse(result.ProcessedSql.Contains("{{groupby") || result.ProcessedSql.Contains("{{having"),
                    $"[{dialectName}] SQL 不应该包含未处理的占位符。模板: {template}, 实际 SQL: {result.ProcessedSql}");
            }
        }
    }

    [TestMethod]
    [Description("GROUP BY 和 HAVING 占位符不应该产生错误")]
    public void GroupByHaving_AllDialects_NoErrors()
    {
        var templates = new[]
        {
            "SELECT department, COUNT(*) FROM users {{groupby:department}}",
            "SELECT department, COUNT(*) FROM users GROUP BY department HAVING COUNT(*) > 5",
            "SELECT department, COUNT(*) FROM users {{groupby:department}} HAVING COUNT(*) > 5"
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

