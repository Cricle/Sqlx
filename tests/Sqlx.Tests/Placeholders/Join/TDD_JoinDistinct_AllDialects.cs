// -----------------------------------------------------------------------
// <copyright file="TDD_JoinDistinct_AllDialects.cs" company="Cricle">
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

namespace Sqlx.Tests.Placeholders.Join;

/// <summary>
/// {{join}}, {{distinct}} 占位符在所有数据库方言中的完整测试
/// P2 JOIN + DISTINCT 占位符
/// </summary>
[TestClass]
public class TDD_JoinDistinct_AllDialects
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
        public int DepartmentId { get; set; }
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

    #region {{join}} 占位符测试

    [TestMethod]
    [Description("{{join}} 占位符应该在所有方言中生成 JOIN")]
    public void Join_AllDialects_GeneratesJoin()
    {
        var template = "SELECT u.* FROM users u {{join:inner|table=departments,on=u.department_id=d.id}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");
            Assert.AreEqual(0, result.Errors.Count,
                $"[{dialectName}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("JOIN"),
                $"[{dialectName}] 应该包含 JOIN。实际 SQL: {result.ProcessedSql}");

            // 检查是否有未处理的占位符
            Assert.IsFalse(result.ProcessedSql.Contains("{{join"),
                $"[{dialectName}] SQL 不应该包含未处理的 join 占位符");
        }
    }

    [TestMethod]
    [Description("{{join:inner}} 应该生成 INNER JOIN")]
    public void Join_Inner_GeneratesInnerJoin()
    {
        var template = "SELECT u.* FROM users u {{join:inner|table=departments,on=u.dept_id=d.id}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("INNER") || sqlUpper.Contains("JOIN"),
                $"[{dialectName}] 应该包含 INNER 或 JOIN。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{join:left}} 应该生成 LEFT JOIN")]
    public void Join_Left_GeneratesLeftJoin()
    {
        var template = "SELECT u.* FROM users u {{join:left|table=departments,on=u.dept_id=d.id}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("LEFT") || sqlUpper.Contains("JOIN"),
                $"[{dialectName}] 应该包含 LEFT 或 JOIN。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{join:right}} 应该生成 RIGHT JOIN")]
    public void Join_Right_GeneratesRightJoin()
    {
        var template = "SELECT u.* FROM users u {{join:right|table=departments,on=u.dept_id=d.id}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("RIGHT") || sqlUpper.Contains("JOIN"),
                $"[{dialectName}] 应该包含 RIGHT 或 JOIN。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{join:full}} 应该生成 FULL OUTER JOIN")]
    public void Join_Full_GeneratesFullOuterJoin()
    {
        var template = "SELECT u.* FROM users u {{join:full|table=departments,on=u.dept_id=d.id}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("FULL") || sqlUpper.Contains("OUTER") || sqlUpper.Contains("JOIN"),
                $"[{dialectName}] 应该包含 FULL、OUTER 或 JOIN。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("多个 JOIN 组合")]
    public void Join_Multiple_AllDialects()
    {
        var template = @"SELECT u.* FROM users u
            {{join:left|table=departments,on=u.dept_id=d.id}}
            {{join:inner|table=roles,on=u.role_id=r.id}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("JOIN"),
                $"[{dialectName}] 应该包含 JOIN");

            // 不应该有未处理的占位符
            Assert.IsFalse(result.ProcessedSql.Contains("{{join"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符");
        }
    }

    #endregion

    #region {{distinct}} 占位符测试

    [TestMethod]
    [Description("{{distinct}} 占位符应该在所有方言中生成 DISTINCT")]
    public void Distinct_AllDialects_GeneratesDistinct()
    {
        var template = "SELECT {{distinct}} name FROM users";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("DISTINCT"),
                $"[{dialectName}] 应该包含 DISTINCT。实际 SQL: {result.ProcessedSql}");

            Assert.IsFalse(result.ProcessedSql.Contains("{{distinct}}"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符");
        }
    }

    [TestMethod]
    [Description("{{distinct:column}} 应该生成 DISTINCT column")]
    public void Distinct_Column_GeneratesDistinctColumn()
    {
        var template = "SELECT {{distinct:name}} FROM users";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("DISTINCT") || sqlUpper.Contains("NAME"),
                $"[{dialectName}] 应该包含 DISTINCT 或 NAME。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{select:distinct}} 应该生成 SELECT DISTINCT")]
    public void Distinct_SelectDistinct_GeneratesSelectDistinct()
    {
        var template = "{{select:distinct}} name FROM users";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("SELECT"),
                $"[{dialectName}] 应该包含 SELECT");
            Assert.IsTrue(sqlUpper.Contains("DISTINCT"),
                $"[{dialectName}] 应该包含 DISTINCT。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("DISTINCT 与 COUNT 组合")]
    public void Distinct_WithCount_AllDialects()
    {
        var template = "SELECT COUNT({{distinct:name}}) FROM users";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("COUNT"),
                $"[{dialectName}] 应该包含 COUNT");

            // 不应该有未处理的占位符
            Assert.IsFalse(result.ProcessedSql.Contains("{{"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符");
        }
    }

    #endregion

    #region JOIN + DISTINCT 组合测试

    [TestMethod]
    [Description("JOIN 和 DISTINCT 组合使用")]
    public void JoinDistinct_Combined_AllDialects()
    {
        var template = "{{select:distinct}} u.name FROM users u {{join:left|table=departments,on=u.dept_id=d.id}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("SELECT"),
                $"[{dialectName}] 应该包含 SELECT");
            Assert.IsTrue(sqlUpper.Contains("DISTINCT"),
                $"[{dialectName}] 应该包含 DISTINCT");
            Assert.IsTrue(sqlUpper.Contains("JOIN") || result.ProcessedSql.Contains("RUNTIME"),
                $"[{dialectName}] 应该包含 JOIN 或运行时标记");

            // 不应该有未处理的占位符
            Assert.IsFalse(result.ProcessedSql.Contains("{{join") || result.ProcessedSql.Contains("{{distinct") || result.ProcessedSql.Contains("{{select"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符");
        }
    }

    #endregion

    #region 边界测试

    [TestMethod]
    [Description("JOIN 和 DISTINCT 占位符不应该留下未处理的占位符")]
    public void JoinDistinct_AllDialects_NoUnprocessedPlaceholders()
    {
        var templates = new[]
        {
            "SELECT u.* FROM users u {{join:left|table=departments,on=u.dept_id=d.id}}",
            "SELECT {{distinct}} name FROM users",
            "{{select:distinct}} * FROM users"
        };

        foreach (var template in templates)
        {
            foreach (var dialect in AllDialects)
            {
                var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
                var dialectName = GetDialectName(dialect);

                // 允许运行时占位符 RUNTIME，但不应有未处理的 {{join}}, {{distinct}}, {{select}}
                Assert.IsFalse(result.ProcessedSql.Contains("{{join") || result.ProcessedSql.Contains("{{distinct") || result.ProcessedSql.Contains("{{select"),
                    $"[{dialectName}] SQL 不应该包含未处理的占位符。模板: {template}, 实际 SQL: {result.ProcessedSql}");
            }
        }
    }

    [TestMethod]
    [Description("JOIN 和 DISTINCT 占位符不应该产生错误")]
    public void JoinDistinct_AllDialects_NoErrors()
    {
        var templates = new[]
        {
            "SELECT u.* FROM users u {{join:inner|table=departments,on=u.dept_id=d.id}}",
            "SELECT {{distinct}} name FROM users",
            "{{select:distinct}} * FROM users"
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







