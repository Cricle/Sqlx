// -----------------------------------------------------------------------
// <copyright file="TDD_MinMax_AllDialects.cs" company="Cricle">
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
/// {{min}}, {{max}} 聚合函数占位符在所有数据库方言中的完整测试
/// P1 聚合函数占位符
/// </summary>
[TestClass]
public class TDD_MinMax_AllDialects
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
        public decimal Balance { get; set; }
    }

    public interface ITestMethods
    {
        Task<decimal> GetMinAsync(CancellationToken ct = default);
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
        _testMethod = methodClass.GetMembers("GetMinAsync").OfType<IMethodSymbol>().First();
    }

    private static string GetDialectName(Sqlx.Generator.SqlDefine dialect)
    {
        return dialect.DatabaseType;
    }

    #region {{min}} 占位符测试

    [TestMethod]
    [Description("{{min}} 占位符应该在所有方言中生成 MIN")]
    public void Min_AllDialects_GeneratesMin()
    {
        var template = "SELECT {{min:age}} FROM users";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");
            Assert.AreEqual(0, result.Errors.Count,
                $"[{dialectName}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("MIN"),
                $"[{dialectName}] 应该包含 MIN。实际 SQL: {result.ProcessedSql}");

            Assert.IsFalse(result.ProcessedSql.Contains("{{min}}"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符");
        }
    }

    [TestMethod]
    [Description("{{min:age}} 应该生成 MIN(age)")]
    public void Min_Column_GeneratesMinColumn()
    {
        var template = "SELECT {{min:age}} FROM users";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            var sqlLower = result.ProcessedSql.ToLowerInvariant();
            
            Assert.IsTrue(sqlUpper.Contains("MIN"),
                $"[{dialectName}] 应该包含 MIN");
            Assert.IsTrue(sqlLower.Contains("age"),
                $"[{dialectName}] 应该包含 age。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{min:balance}} 应该生成 MIN(balance)")]
    public void Min_Balance_GeneratesMinBalance()
    {
        var template = "SELECT {{min:balance}} FROM users";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            var sqlLower = result.ProcessedSql.ToLowerInvariant();
            
            Assert.IsTrue(sqlUpper.Contains("MIN"),
                $"[{dialectName}] 应该包含 MIN");
            Assert.IsTrue(sqlLower.Contains("balance"),
                $"[{dialectName}] 应该包含 balance。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{min}} 在 WHERE 子句中应该正常工作")]
    public void Min_WithWhere_AllDialects()
    {
        var template = "SELECT {{min:age}} FROM users WHERE age >= 18";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("MIN"),
                $"[{dialectName}] 应该包含 MIN");
            Assert.IsTrue(sqlUpper.Contains("WHERE"),
                $"[{dialectName}] 应该包含 WHERE");
        }
    }

    [TestMethod]
    [Description("{{min}} 在 GROUP BY 中应该正常工作")]
    public void Min_WithGroupBy_AllDialects()
    {
        var template = "SELECT name, {{min:age}} FROM users GROUP BY name";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("MIN"),
                $"[{dialectName}] 应该包含 MIN");
            Assert.IsTrue(sqlUpper.Contains("GROUP BY"),
                $"[{dialectName}] 应该包含 GROUP BY");
        }
    }

    #endregion

    #region {{max}} 占位符测试

    [TestMethod]
    [Description("{{max}} 占位符应该在所有方言中生成 MAX")]
    public void Max_AllDialects_GeneratesMax()
    {
        var template = "SELECT {{max:age}} FROM users";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");
            Assert.AreEqual(0, result.Errors.Count,
                $"[{dialectName}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("MAX"),
                $"[{dialectName}] 应该包含 MAX。实际 SQL: {result.ProcessedSql}");

            Assert.IsFalse(result.ProcessedSql.Contains("{{max}}"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符");
        }
    }

    [TestMethod]
    [Description("{{max:age}} 应该生成 MAX(age)")]
    public void Max_Column_GeneratesMaxColumn()
    {
        var template = "SELECT {{max:age}} FROM users";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            var sqlLower = result.ProcessedSql.ToLowerInvariant();
            
            Assert.IsTrue(sqlUpper.Contains("MAX"),
                $"[{dialectName}] 应该包含 MAX");
            Assert.IsTrue(sqlLower.Contains("age"),
                $"[{dialectName}] 应该包含 age。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{max:balance}} 应该生成 MAX(balance)")]
    public void Max_Balance_GeneratesMaxBalance()
    {
        var template = "SELECT {{max:balance}} FROM users";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            var sqlLower = result.ProcessedSql.ToLowerInvariant();
            
            Assert.IsTrue(sqlUpper.Contains("MAX"),
                $"[{dialectName}] 应该包含 MAX");
            Assert.IsTrue(sqlLower.Contains("balance"),
                $"[{dialectName}] 应该包含 balance。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{max}} 在 WHERE 子句中应该正常工作")]
    public void Max_WithWhere_AllDialects()
    {
        var template = "SELECT {{max:balance}} FROM users WHERE age >= 18";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("MAX"),
                $"[{dialectName}] 应该包含 MAX");
            Assert.IsTrue(sqlUpper.Contains("WHERE"),
                $"[{dialectName}] 应该包含 WHERE");
        }
    }

    [TestMethod]
    [Description("{{max}} 在 GROUP BY 中应该正常工作")]
    public void Max_WithGroupBy_AllDialects()
    {
        var template = "SELECT name, {{max:balance}} FROM users GROUP BY name";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("MAX"),
                $"[{dialectName}] 应该包含 MAX");
            Assert.IsTrue(sqlUpper.Contains("GROUP BY"),
                $"[{dialectName}] 应该包含 GROUP BY");
        }
    }

    #endregion

    #region {{min}} + {{max}} 组合测试

    [TestMethod]
    [Description("{{min}} + {{max}} 组合查询")]
    public void MinMax_Combined_AllDialects()
    {
        var template = "SELECT {{min:age}}, {{max:age}} FROM users";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("MIN"),
                $"[{dialectName}] 应该包含 MIN");
            Assert.IsTrue(sqlUpper.Contains("MAX"),
                $"[{dialectName}] 应该包含 MAX");

            Assert.IsFalse(result.ProcessedSql.Contains("{{"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符");
        }
    }

    [TestMethod]
    [Description("所有聚合函数组合查询")]
    public void AllAggregates_Combined_AllDialects()
    {
        var template = "SELECT {{count}}, {{min:age}}, {{max:age}}, {{avg:age}}, {{sum:balance}} FROM users";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("COUNT"),
                $"[{dialectName}] 应该包含 COUNT");
            Assert.IsTrue(sqlUpper.Contains("MIN"),
                $"[{dialectName}] 应该包含 MIN");
            Assert.IsTrue(sqlUpper.Contains("MAX"),
                $"[{dialectName}] 应该包含 MAX");
            Assert.IsTrue(sqlUpper.Contains("AVG"),
                $"[{dialectName}] 应该包含 AVG");
            Assert.IsTrue(sqlUpper.Contains("SUM"),
                $"[{dialectName}] 应该包含 SUM");

            Assert.IsFalse(result.ProcessedSql.Contains("{{"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符");
        }
    }

    [TestMethod]
    [Description("{{min}}/{{max}} + WHERE + GROUP BY + HAVING 完整查询")]
    public void MinMax_CompleteQuery_AllDialects()
    {
        var template = "SELECT name, {{min:age}}, {{max:balance}} FROM users WHERE age >= 18 GROUP BY name HAVING COUNT(*) > 5";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("SELECT"),
                $"[{dialectName}] 应该包含 SELECT");
            Assert.IsTrue(sqlUpper.Contains("MIN"),
                $"[{dialectName}] 应该包含 MIN");
            Assert.IsTrue(sqlUpper.Contains("MAX"),
                $"[{dialectName}] 应该包含 MAX");
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
    [Description("{{min}}/{{max}} 不应该留下未处理的占位符")]
    public void MinMax_AllDialects_NoUnprocessedPlaceholders()
    {
        var templates = new[]
        {
            "SELECT {{min:age}} FROM users",
            "SELECT {{max:balance}} FROM users"
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
    [Description("{{min}}/{{max}} 不应该产生错误")]
    public void MinMax_AllDialects_NoErrors()
    {
        var templates = new[]
        {
            "SELECT {{min:age}} FROM users",
            "SELECT {{max:balance}} FROM users"
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










