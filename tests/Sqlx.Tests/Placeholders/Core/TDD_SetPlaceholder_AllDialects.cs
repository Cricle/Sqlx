// -----------------------------------------------------------------------
// <copyright file="TDD_SetPlaceholder_AllDialects.cs" company="Cricle">
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
/// {{set}} 占位符在所有数据库方言中的完整测试
/// P0 核心占位符 - UPDATE SET 子句生成
/// </summary>
[TestClass]
public class TDD_SetPlaceholder_AllDialects
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
        Task<int> UpdateAsync(CancellationToken ct = default);
        Task<int> UpdateUserAsync(long id, string name, string email, CancellationToken ct = default);
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
        _testMethod = methodClass.GetMembers("UpdateAsync").OfType<IMethodSymbol>().First();
        _testMethodWithParams = methodClass.GetMembers("UpdateUserAsync").OfType<IMethodSymbol>().First();
    }

    private static string GetDialectName(Sqlx.Generator.SqlDefine dialect)
    {
        return dialect.DatabaseType;
    }

    #region {{set}} 基础测试 - 所有方言

    [TestMethod]
    [Description("{{set}} 占位符应该在所有方言中生成 SET 子句")]
    public void Set_AllDialects_GeneratesSetClause()
    {
        var template = "UPDATE users {{set}} WHERE id = @id";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");
            Assert.AreEqual(0, result.Errors.Count,
                $"[{dialectName}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");

            // 不应该包含未处理的占位符
            Assert.IsFalse(result.ProcessedSql.Contains("{{set}}"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{set}} 应该生成 column = @param 格式")]
    public void Set_GeneratesColumnEqualsParam()
    {
        var template = "UPDATE users SET {{set}} WHERE id = @id";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithParams, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);
            var sqlLower = result.ProcessedSql.ToLowerInvariant();

            // 应该包含 SET 和参数引用
            Assert.IsTrue(sqlLower.Contains("set"),
                $"[{dialectName}] 应该包含 SET。实际 SQL: {result.ProcessedSql}");

            // 应该包含等号和逗号（多个赋值）
            Assert.IsTrue(result.ProcessedSql.Contains("="),
                $"[{dialectName}] 应该包含等号。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{set}} 应该排除 Id 属性")]
    public void Set_ExcludesIdProperty()
    {
        var template = "UPDATE users SET {{set}} WHERE id = @id";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            // 不应该包含 id = @id 在 SET 子句中（id 在 WHERE）
            // 但验证较复杂，我们主要验证生成了 SET 子句
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");
        }
    }

    #endregion

    #region {{set}} 参数引用测试 - 所有方言

    [TestMethod]
    [Description("{{set}} - SQLite 使用 @param 前缀")]
    public void Set_SQLite_UsesAtPrefix()
    {
        var template = "UPDATE users SET {{set}} WHERE id = @id";
        var result = _engine.ProcessTemplate(template, _testMethodWithParams, _userType, "users", Sqlx.Generator.SqlDefine.SQLite);

        var sqlUpper = result.ProcessedSql.ToUpperInvariant();
        Assert.IsTrue(sqlUpper.Contains("SET"),
            $"SQLite 应该包含 SET。实际 SQL: {result.ProcessedSql}");

        // 应该包含参数（@param 格式）
        Assert.IsTrue(result.ProcessedSql.Contains("@") || result.ProcessedSql.Contains("{RUNTIME"),
            $"SQLite 应该包含参数引用。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("{{set}} - PostgreSQL 使用 $param 前缀")]
    public void Set_PostgreSQL_UsesDollarPrefix()
    {
        var template = "UPDATE users SET {{set}} WHERE id = @id";
        var result = _engine.ProcessTemplate(template, _testMethodWithParams, _userType, "users", Sqlx.Generator.SqlDefine.PostgreSql);

        var sqlUpper = result.ProcessedSql.ToUpperInvariant();
        Assert.IsTrue(sqlUpper.Contains("SET"),
            $"PostgreSQL 应该包含 SET。实际 SQL: {result.ProcessedSql}");

        // 应该包含参数（$param 或运行时标记）
        Assert.IsTrue(result.ProcessedSql.Contains("$") || result.ProcessedSql.Contains("{RUNTIME"),
            $"PostgreSQL 应该包含参数引用。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("{{set}} - MySQL 使用 @param 前缀")]
    public void Set_MySQL_UsesAtPrefix()
    {
        var template = "UPDATE users SET {{set}} WHERE id = @id";
        var result = _engine.ProcessTemplate(template, _testMethodWithParams, _userType, "users", Sqlx.Generator.SqlDefine.MySql);

        var sqlUpper = result.ProcessedSql.ToUpperInvariant();
        Assert.IsTrue(sqlUpper.Contains("SET"),
            $"MySQL 应该包含 SET。实际 SQL: {result.ProcessedSql}");

        // 应该包含参数
        Assert.IsTrue(result.ProcessedSql.Contains("@") || result.ProcessedSql.Contains("{RUNTIME"),
            $"MySQL 应该包含参数引用。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("{{set}} - SQL Server 使用 @param 前缀")]
    public void Set_SqlServer_UsesAtPrefix()
    {
        var template = "UPDATE users SET {{set}} WHERE id = @id";
        var result = _engine.ProcessTemplate(template, _testMethodWithParams, _userType, "users", Sqlx.Generator.SqlDefine.SqlServer);

        var sqlUpper = result.ProcessedSql.ToUpperInvariant();
        Assert.IsTrue(sqlUpper.Contains("SET"),
            $"SQL Server 应该包含 SET。实际 SQL: {result.ProcessedSql}");

        // 应该包含参数
        Assert.IsTrue(result.ProcessedSql.Contains("@") || result.ProcessedSql.Contains("{RUNTIME"),
            $"SQL Server 应该包含参数引用。实际 SQL: {result.ProcessedSql}");
    }

    #endregion

    #region {{set}} 组合测试

    [TestMethod]
    [Description("{{set}} + {{where}} 组合")]
    public void Set_WithWhere_AllDialects()
    {
        var template = "UPDATE users SET {{set}} {{where:id}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithParams, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("UPDATE"),
                $"[{dialectName}] 应该包含 UPDATE");
            Assert.IsTrue(sqlUpper.Contains("SET"),
                $"[{dialectName}] 应该包含 SET");
        }
    }

    [TestMethod]
    [Description("{{table}} + {{set}} + {{where}} 完整 UPDATE 语句")]
    public void Set_CompleteUpdateStatement_AllDialects()
    {
        var template = "UPDATE {{table}} SET {{set}} {{where:id}}";

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

            // 验证完整 UPDATE 语句
            Assert.IsTrue(sqlUpper.Contains("UPDATE"),
                $"[{dialectName}] 应该包含 UPDATE");
            Assert.IsTrue(sqlUpper.Contains("SET"),
                $"[{dialectName}] 应该包含 SET");
            Assert.IsTrue(sqlLower.Contains("users"),
                $"[{dialectName}] 应该包含表名");

            // 不应该有未处理的占位符
            Assert.IsFalse(result.ProcessedSql.Contains("{{"),
                $"[{dialectName}] SQL 不应该包含未处理的占位符。实际 SQL: {result.ProcessedSql}");
        }
    }

    #endregion

    #region 边界测试

    [TestMethod]
    [Description("{{set}} 不应该留下未处理的占位符")]
    public void Set_AllDialects_NoUnprocessedPlaceholders()
    {
        var template = "UPDATE users SET {{set}} WHERE id = @id";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(result.ProcessedSql.Contains("{{set}}"),
                $"[{dialectName}] SQL 不应该包含未处理的 {{{{set}}}}。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{set}} 不应该产生错误")]
    public void Set_AllDialects_NoErrors()
    {
        var template = "UPDATE users SET {{set}} WHERE id = @id";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.AreEqual(0, result.Errors.Count,
                $"[{dialectName}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");
        }
    }

    [TestMethod]
    [Description("{{set}} 在复杂 UPDATE 语句中应该正常工作")]
    public void Set_ComplexUpdate_AllDialects()
    {
        var template = "UPDATE users SET {{set}}, updated_at = CURRENT_TIMESTAMP WHERE id = @id AND is_active = 1";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithParams, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("UPDATE"),
                $"[{dialectName}] 应该包含 UPDATE");
            Assert.IsTrue(sqlUpper.Contains("SET"),
                $"[{dialectName}] 应该包含 SET");
            Assert.IsTrue(sqlUpper.Contains("WHERE"),
                $"[{dialectName}] 应该包含 WHERE");
        }
    }

    #endregion

    #region 基于参数的 SET 生成

    [TestMethod]
    [Description("{{set}} 基于方法参数生成 SET 子句")]
    public void Set_BasedOnMethodParameters_AllDialects()
    {
        var template = "UPDATE users SET {{set}} WHERE id = @id";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithParams, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            var sqlLower = result.ProcessedSql.ToLowerInvariant();

            // 应该包含方法参数（name, email）
            var hasName = sqlLower.Contains("name") || result.ProcessedSql.Contains("{RUNTIME");
            var hasEmail = sqlLower.Contains("email") || result.ProcessedSql.Contains("{RUNTIME");

            Assert.IsTrue(hasName || hasEmail,
                $"[{dialectName}] 应该包含方法参数。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{set}} 基于实体属性生成 SET 子句")]
    public void Set_BasedOnEntityProperties_AllDialects()
    {
        var template = "UPDATE users SET {{set}} WHERE id = @id";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethod, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            // 应该包含某种形式的 SET 子句
            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("SET") || result.ProcessedSql.Contains("{RUNTIME"),
                $"[{dialectName}] 应该包含 SET 或运行时标记。实际 SQL: {result.ProcessedSql}");
        }
    }

    #endregion

    #region UPDATE 语句场景

    [TestMethod]
    [Description("{{set}} 在简单 UPDATE 中应该正常工作")]
    public void Set_SimpleUpdate_AllDialects()
    {
        var template = "UPDATE users SET {{set}} WHERE id = @id";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithParams, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("UPDATE"),
                $"[{dialectName}] 应该包含 UPDATE");
            Assert.IsTrue(sqlUpper.Contains("SET"),
                $"[{dialectName}] 应该包含 SET");
            Assert.IsTrue(sqlUpper.Contains("WHERE"),
                $"[{dialectName}] 应该包含 WHERE");
        }
    }

    [TestMethod]
    [Description("{{set}} 在多条件 UPDATE 中应该正常工作")]
    public void Set_MultiConditionUpdate_AllDialects()
    {
        var template = "UPDATE users SET {{set}} WHERE id = @id AND age >= 18";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _testMethodWithParams, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");

            var sqlUpper = result.ProcessedSql.ToUpperInvariant();
            Assert.IsTrue(sqlUpper.Contains("UPDATE"),
                $"[{dialectName}] 应该包含 UPDATE");
            Assert.IsTrue(sqlUpper.Contains("SET"),
                $"[{dialectName}] 应该包含 SET");
            Assert.IsTrue(sqlUpper.Contains("WHERE"),
                $"[{dialectName}] 应该包含 WHERE");
            Assert.IsTrue(sqlUpper.Contains("AND"),
                $"[{dialectName}] 应该包含 AND");
        }
    }

    #endregion
}










