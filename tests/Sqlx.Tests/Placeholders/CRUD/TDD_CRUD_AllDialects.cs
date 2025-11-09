// -----------------------------------------------------------------------
// <copyright file="TDD_CRUD_AllDialects.cs" company="Cricle">
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

namespace Sqlx.Tests.Placeholders.CRUD;

/// <summary>
/// {{select}}, {{insert}}, {{update}}, {{delete}} CRUD 占位符在所有数据库方言中的完整测试
/// P2 CRUD 占位符
/// </summary>
[TestClass]
public class TDD_CRUD_AllDialects
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

    #region {{select}} 占位符测试

    [TestMethod]
    [Description("{{select}} 占位符应该在所有方言中生成 SELECT")]
    public void Select_AllDialects_GeneratesSelect()
    {
        var template = "{{select}} * FROM users";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");
            Assert.AreEqual(0, result.Errors.Count,
                $"[{dialectName}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("SELECT"),
                $"[{dialectName}] 应该包含 SELECT。实际 SQL: {result.ProcessedSql}");

            Assert.IsFalse(result.ProcessedSql.Contains("{{select}}"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符");
        }
    }

    [TestMethod]
    [Description("{{select:distinct}} 应该生成 SELECT DISTINCT")]
    public void Select_Distinct_GeneratesSelectDistinct()
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
    [Description("{{select}} 与 {{columns}}, {{table}} 组合")]
    public void Select_WithColumnsAndTable_AllDialects()
    {
        var template = "{{select}} {{columns}} FROM {{table}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("SELECT"),
                $"[{dialectName}] 应该包含 SELECT");
            Assert.IsTrue(sqlUpper.Contains("FROM"),
                $"[{dialectName}] 应该包含 FROM");

            Assert.IsFalse(result.ProcessedSql.Contains("{{"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符");
        }
    }

    #endregion

    #region {{insert}} 占位符测试

    [TestMethod]
    [Description("{{insert}} 占位符应该在所有方言中生成 INSERT")]
    public void Insert_AllDialects_GeneratesInsert()
    {
        var template = "{{insert}} INTO users (name, age) VALUES (@name, @age)";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");
            Assert.AreEqual(0, result.Errors.Count,
                $"[{dialectName}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("INSERT"),
                $"[{dialectName}] 应该包含 INSERT。实际 SQL: {result.ProcessedSql}");

            Assert.IsFalse(result.ProcessedSql.Contains("{{insert}}"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符");
        }
    }

    [TestMethod]
    [Description("{{insert:into}} 应该生成 INSERT INTO")]
    public void Insert_Into_GeneratesInsertInto()
    {
        var template = "{{insert:into}} users (name, age) VALUES (@name, @age)";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("INSERT"),
                $"[{dialectName}] 应该包含 INSERT");
            // INTO 可能在占位符中已经生成，或在模板中
            Assert.IsTrue(sqlUpper.Contains("INTO"),
                $"[{dialectName}] 应该包含 INTO。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{insert}} 与 {{table}}, {{values}} 组合")]
    public void Insert_WithTableAndValues_AllDialects()
    {
        var template = "{{insert}} INTO {{table}} (name, age) VALUES ({{values}})";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("INSERT"),
                $"[{dialectName}] 应该包含 INSERT");
            Assert.IsTrue(sqlUpper.Contains("INTO"),
                $"[{dialectName}] 应该包含 INTO");
            Assert.IsTrue(sqlUpper.Contains("VALUES"),
                $"[{dialectName}] 应该包含 VALUES");

            Assert.IsFalse(result.ProcessedSql.Contains("{{"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符");
        }
    }

    #endregion

    #region {{update}} 占位符测试

    [TestMethod]
    [Description("{{update}} 占位符应该在所有方言中生成 UPDATE")]
    public void Update_AllDialects_GeneratesUpdate()
    {
        var template = "{{update}} users SET name = @name WHERE id = @id";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");
            Assert.AreEqual(0, result.Errors.Count,
                $"[{dialectName}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("UPDATE"),
                $"[{dialectName}] 应该包含 UPDATE。实际 SQL: {result.ProcessedSql}");

            Assert.IsFalse(result.ProcessedSql.Contains("{{update}}"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符");
        }
    }

    [TestMethod]
    [Description("{{update}} 与 {{table}}, {{set}}, {{where}} 组合")]
    public void Update_WithTableSetWhere_AllDialects()
    {
        var template = "{{update}} {{table}} SET name = @name {{where:id}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("UPDATE"),
                $"[{dialectName}] 应该包含 UPDATE。实际 SQL: {result.ProcessedSql}");
            Assert.IsTrue(sqlUpper.Contains("SET"),
                $"[{dialectName}] 应该包含 SET。实际 SQL: {result.ProcessedSql}");

            Assert.IsFalse(result.ProcessedSql.Contains("{{"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符。实际 SQL: {result.ProcessedSql}");
        }
    }

    #endregion

    #region {{delete}} 占位符测试

    [TestMethod]
    [Description("{{delete}} 占位符应该在所有方言中生成 DELETE")]
    public void Delete_AllDialects_GeneratesDelete()
    {
        var template = "{{delete}} FROM users WHERE id = @id";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");
            Assert.AreEqual(0, result.Errors.Count,
                $"[{dialectName}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("DELETE"),
                $"[{dialectName}] 应该包含 DELETE。实际 SQL: {result.ProcessedSql}");

            Assert.IsFalse(result.ProcessedSql.Contains("{{delete}}"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符");
        }
    }

    [TestMethod]
    [Description("{{delete:from}} 应该生成 DELETE FROM")]
    public void Delete_From_GeneratesDeleteFrom()
    {
        var template = "{{delete:from}} users WHERE id = @id";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("DELETE"),
                $"[{dialectName}] 应该包含 DELETE");
            // FROM 可能在占位符中已经生成，或在模板中
            Assert.IsTrue(sqlUpper.Contains("FROM"),
                $"[{dialectName}] 应该包含 FROM。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{delete}} 与 {{table}}, {{where}} 组合")]
    public void Delete_WithTableAndWhere_AllDialects()
    {
        var template = "{{delete}} FROM {{table}} {{where:id}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("DELETE"),
                $"[{dialectName}] 应该包含 DELETE");
            Assert.IsTrue(sqlUpper.Contains("FROM"),
                $"[{dialectName}] 应该包含 FROM");

            Assert.IsFalse(result.ProcessedSql.Contains("{{"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符");
        }
    }

    #endregion

    #region CRUD 组合测试

    [TestMethod]
    [Description("完整 CRUD 操作测试")]
    public void CRUD_CompleteOperations_AllDialects()
    {
        var templates = new[]
        {
            ("{{select}} * FROM users WHERE id = @id", "SELECT"),
            ("{{insert}} INTO users (name) VALUES (@name)", "INSERT"),
            ("{{update}} users SET name = @name WHERE id = @id", "UPDATE"),
            ("{{delete}} FROM users WHERE id = @id", "DELETE")
        };

        foreach (var (template, keyword) in templates)
        {
            foreach (var dialect in AllDialects)
            {
                var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
                var dialectName = GetDialectName(dialect);

                var sqlUpper = result.ProcessedSql.ToUpperInvariant();
                Assert.IsTrue(sqlUpper.Contains(keyword),
                    $"[{dialectName}] 应该包含 {keyword}。模板: {template}, 实际 SQL: {result.ProcessedSql}");

                Assert.IsFalse(result.ProcessedSql.Contains("{{"),
                    $"[{dialectName}] SQL 不应该包含未处理的占位符。模板: {template}");
            }
        }
    }

    #endregion

    #region 边界测试

    [TestMethod]
    [Description("CRUD 占位符不应该留下未处理的占位符")]
    public void CRUD_AllDialects_NoUnprocessedPlaceholders()
    {
        var templates = new[]
        {
            "{{select}} * FROM users",
            "{{insert}} INTO users (name) VALUES (@name)",
            "{{update}} users SET name = @name",
            "{{delete}} FROM users WHERE id = @id"
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
    [Description("CRUD 占位符不应该产生错误")]
    public void CRUD_AllDialects_NoErrors()
    {
        var templates = new[]
        {
            "{{select}} * FROM users",
            "{{insert}} INTO users (name) VALUES (@name)",
            "{{update}} users SET name = @name",
            "{{delete}} FROM users WHERE id = @id"
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

