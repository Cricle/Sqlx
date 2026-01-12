// -----------------------------------------------------------------------
// <copyright file="TDD_NullableLimitOffset_Extended.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator;
using System.Linq;

namespace Sqlx.Tests.Placeholders;

/// <summary>
/// 扩展的 TDD 测试 - 更全面的边界验证
/// </summary>
[TestClass]
public class TDD_NullableLimitOffset_Extended
{
    private SqlTemplateEngine _engine = null!;
    private INamedTypeSymbol _userType = null!;
    private IMethodSymbol _methodWithNullableLimit = null!;
    private IMethodSymbol _methodWithNonNullableLimit = null!;

    private static readonly Sqlx.Generator.SqlDefine[] AllDialects = new[]
    {
        Sqlx.Generator.SqlDefine.SQLite,
        Sqlx.Generator.SqlDefine.MySql,
        Sqlx.Generator.SqlDefine.PostgreSql,
        Sqlx.Generator.SqlDefine.SqlServer,
        Sqlx.Generator.SqlDefine.Oracle
    };

    [TestInitialize]
    public void Setup()
    {
        _engine = new SqlTemplateEngine();

        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            using System;
            using System.Threading.Tasks;
            using System.Collections.Generic;

            public class User
            {
                public long Id { get; set; }
                public string Name { get; set; }
                public int Age { get; set; }
            }

            public interface IUserRepository
            {
                Task<List<User>> GetTopUsersAsync(int? limit = 10);
                Task<List<User>> GetFixedLimitAsync(int limit);
            }
        ");

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create("TestAssembly")
            .AddSyntaxTrees(syntaxTree)
            .AddReferences(references);

        _userType = compilation.GetTypeByMetadataName("User")!;
        var interfaceType = compilation.GetTypeByMetadataName("IUserRepository")!;
        var methods = interfaceType.GetMembers().OfType<IMethodSymbol>().ToArray();
        _methodWithNullableLimit = methods.First(m => m.Name == "GetTopUsersAsync");
        _methodWithNonNullableLimit = methods.First(m => m.Name == "GetFixedLimitAsync");
    }

    private static string GetDialectName(Sqlx.Generator.SqlDefine dialect)
    {
        if (dialect.Equals(Sqlx.Generator.SqlDefine.SQLite)) return "SQLite";
        if (dialect.Equals(Sqlx.Generator.SqlDefine.MySql)) return "MySQL";
        if (dialect.Equals(Sqlx.Generator.SqlDefine.PostgreSql)) return "PostgreSQL";
        if (dialect.Equals(Sqlx.Generator.SqlDefine.SqlServer)) return "SqlServer";
        if (dialect.Equals(Sqlx.Generator.SqlDefine.Oracle)) return "Oracle";
        return "Unknown";
    }

    #region 参数前缀验证

    [TestMethod]
    [Description("MySQL 非可空 limit 应该使用 @ 参数前缀")]
    public void MySQL_NonNullableLimit_ShouldUseAtPrefix()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}}";
        var result = _engine.ProcessTemplate(template, _methodWithNonNullableLimit, _userType, "users", Sqlx.Generator.SqlDefine.MySql);

        Assert.IsTrue(result.ProcessedSql.Contains("LIMIT @limit"),
            $"MySQL 非可空 limit 应该使用 @ 参数前缀。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("PostgreSQL 非可空 limit 应该使用 $ 参数前缀")]
    public void PostgreSQL_NonNullableLimit_ShouldUseDollarPrefix()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}}";
        var result = _engine.ProcessTemplate(template, _methodWithNonNullableLimit, _userType, "users", Sqlx.Generator.SqlDefine.PostgreSql);

        Assert.IsTrue(result.ProcessedSql.Contains("LIMIT $limit"),
            $"PostgreSQL 非可空 limit 应该使用 $ 参数前缀。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("SQLite 非可空 limit 应该使用 @ 参数前缀")]
    public void SQLite_NonNullableLimit_ShouldUseAtPrefix()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}}";
        var result = _engine.ProcessTemplate(template, _methodWithNonNullableLimit, _userType, "users", Sqlx.Generator.SqlDefine.SQLite);

        Assert.IsTrue(result.ProcessedSql.Contains("LIMIT @limit"),
            $"SQLite 非可空 limit 应该使用 @ 参数前缀。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("Oracle 非可空 limit 应该使用 : 参数前缀")]
    public void Oracle_NonNullableLimit_ShouldUseColonPrefix()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}}";
        var result = _engine.ProcessTemplate(template, _methodWithNonNullableLimit, _userType, "users", Sqlx.Generator.SqlDefine.Oracle);

        Assert.IsTrue(result.ProcessedSql.Contains(":limit"),
            $"Oracle 非可空 limit 应该使用 : 参数前缀。实际 SQL: {result.ProcessedSql}");
    }

    #endregion

    #region 特殊参数类型

    [TestMethod]
    [Description("short? 类型的 limit 参数应该被正确识别为可空")]
    public void ShortNullableLimit_ShouldBeRecognizedAsNullable()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            using System.Threading.Tasks;
            using System.Collections.Generic;
            public class User { public long Id { get; set; } }
            public interface IRepo { Task<List<User>> GetAsync(short? limit = null); }
        ");

        var compilation = CreateCompilation(syntaxTree);
        var (userType, method) = GetTypeAndMethod(compilation);

        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, method, userType, "users", dialect);
            Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_LIMIT_limit}"),
                $"[{GetDialectName(dialect)}] short? 应该生成 RUNTIME_NULLABLE_LIMIT");
        }
    }

    [TestMethod]
    [Description("uint? 类型的 limit 参数应该被正确识别为可空")]
    public void UIntNullableLimit_ShouldBeRecognizedAsNullable()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            using System.Threading.Tasks;
            using System.Collections.Generic;
            public class User { public long Id { get; set; } }
            public interface IRepo { Task<List<User>> GetAsync(uint? limit = null); }
        ");

        var compilation = CreateCompilation(syntaxTree);
        var (userType, method) = GetTypeAndMethod(compilation);

        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, method, userType, "users", dialect);
            Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_LIMIT_limit}"),
                $"[{GetDialectName(dialect)}] uint? 应该生成 RUNTIME_NULLABLE_LIMIT");
        }
    }

    #endregion

    #region 参数名称大小写

    [TestMethod]
    [Description("大写参数名 LIMIT 应该被正确识别")]
    public void UppercaseParamName_ShouldBeRecognized()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            using System.Threading.Tasks;
            using System.Collections.Generic;
            public class User { public long Id { get; set; } }
            public interface IRepo { Task<List<User>> GetAsync(int? LIMIT = null); }
        ");

        var compilation = CreateCompilation(syntaxTree);
        var (userType, method) = GetTypeAndMethod(compilation);

        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, method, userType, "users", dialect);
            Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_LIMIT_LIMIT}"),
                $"[{GetDialectName(dialect)}] 大写参数名应该被识别。实际: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("混合大小写参数名 Limit 应该被正确识别")]
    public void MixedCaseParamName_ShouldBeRecognized()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            using System.Threading.Tasks;
            using System.Collections.Generic;
            public class User { public long Id { get; set; } }
            public interface IRepo { Task<List<User>> GetAsync(int? Limit = null); }
        ");

        var compilation = CreateCompilation(syntaxTree);
        var (userType, method) = GetTypeAndMethod(compilation);

        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, method, userType, "users", dialect);
            Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_LIMIT_Limit}"),
                $"[{GetDialectName(dialect)}] 混合大小写参数名应该被识别。实际: {result.ProcessedSql}");
        }
    }

    #endregion

    #region 组合占位符

    [TestMethod]
    [Description("{{limit}} 和 {{offset}} 顺序颠倒应该正常工作")]
    public void OffsetBeforeLimit_ShouldWork()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            using System.Threading.Tasks;
            using System.Collections.Generic;
            public class User { public long Id { get; set; } }
            public interface IRepo { Task<List<User>> GetAsync(int? limit = null, int? offset = null); }
        ");

        var compilation = CreateCompilation(syntaxTree);
        var (userType, method) = GetTypeAndMethod(compilation);

        var template = "SELECT * FROM {{table}} ORDER BY id {{offset}} {{limit}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, method, userType, "users", dialect);
            Assert.IsTrue(result.IsValid, $"[{GetDialectName(dialect)}] offset 在 limit 之前不应该有错误");
            Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_LIMIT_limit}"),
                $"[{GetDialectName(dialect)}] 应该包含 RUNTIME_NULLABLE_LIMIT");
            Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_OFFSET_offset}"),
                $"[{GetDialectName(dialect)}] 应该包含 RUNTIME_NULLABLE_OFFSET");
        }
    }

    #endregion

    #region 默认值处理

    [TestMethod]
    [Description("有默认值的可空参数应该生成 RUNTIME_NULLABLE 占位符")]
    public void NullableWithDefaultValue_ShouldGenerateRuntimePlaceholder()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            using System.Threading.Tasks;
            using System.Collections.Generic;
            public class User { public long Id { get; set; } }
            public interface IRepo { Task<List<User>> GetAsync(int? limit = 100); }
        ");

        var compilation = CreateCompilation(syntaxTree);
        var (userType, method) = GetTypeAndMethod(compilation);

        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, method, userType, "users", dialect);
            Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_LIMIT_limit}"),
                $"[{GetDialectName(dialect)}] 有默认值的可空参数应该生成 RUNTIME_NULLABLE_LIMIT");
        }
    }

    [TestMethod]
    [Description("有默认值的非可空参数应该直接生成 LIMIT 语法")]
    public void NonNullableWithDefaultValue_ShouldGenerateDirectSyntax()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            using System.Threading.Tasks;
            using System.Collections.Generic;
            public class User { public long Id { get; set; } }
            public interface IRepo { Task<List<User>> GetAsync(int limit = 100); }
        ");

        var compilation = CreateCompilation(syntaxTree);
        var (userType, method) = GetTypeAndMethod(compilation);

        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}}";

        var directLimitDialects = new[] { Sqlx.Generator.SqlDefine.SQLite, Sqlx.Generator.SqlDefine.MySql, Sqlx.Generator.SqlDefine.PostgreSql };

        foreach (var dialect in directLimitDialects)
        {
            var result = _engine.ProcessTemplate(template, method, userType, "users", dialect);
            Assert.IsTrue(result.ProcessedSql.Contains("LIMIT"),
                $"[{GetDialectName(dialect)}] 非可空参数应该直接生成 LIMIT 语法");
            Assert.IsFalse(result.ProcessedSql.Contains("RUNTIME_NULLABLE"),
                $"[{GetDialectName(dialect)}] 非可空参数不应该生成 RUNTIME_NULLABLE");
        }
    }

    #endregion

    #region 无参数方法

    [TestMethod]
    [Description("无参数方法使用 {{limit}} 应该使用默认值或生成 LIMIT 关键字")]
    public void NoParameterMethod_ShouldUseDefaultLimit()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            using System.Threading.Tasks;
            using System.Collections.Generic;
            public class User { public long Id { get; set; } }
            public interface IRepo { Task<List<User>> GetAllAsync(); }
        ");

        var compilation = CreateCompilation(syntaxTree);
        var (userType, method) = GetTypeAndMethod(compilation);

        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, method, userType, "users", dialect);
            Assert.IsTrue(result.IsValid, $"[{GetDialectName(dialect)}] 无参数方法不应该有错误");
            // 无参数方法应该生成某种形式的 LIMIT 语法
            var hasLimitSyntax = result.ProcessedSql.Contains("LIMIT") || 
                                 result.ProcessedSql.Contains("TOP") ||
                                 result.ProcessedSql.Contains("FETCH") ||
                                 result.ProcessedSql.Contains("ROWNUM");
            Assert.IsTrue(hasLimitSyntax,
                $"[{GetDialectName(dialect)}] 无参数方法应该生成分页语法。实际: {result.ProcessedSql}");
        }
    }

    #endregion

    #region 特殊 SQL 场景

    [TestMethod]
    [Description("复杂查询中的 {{limit}} 应该正确处理")]
    public void ComplexQuery_WithLimit_ShouldWork()
    {
        // 注意：UNION SELECT 会被 SQL 注入检测拦截（这是预期行为）
        // 使用子查询来测试复杂 SQL 场景
        var template = "SELECT * FROM {{table}} WHERE id IN (SELECT id FROM other_table) ORDER BY id {{limit}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _methodWithNullableLimit, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);
            
            // 检查是否生成了 SQL（可能有警告但不应该完全失败）
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 复杂查询应该生成 SQL。错误: {string.Join(", ", result.Errors)}");
            Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_LIMIT_limit}"),
                $"[{dialectName}] 复杂查询应该包含 RUNTIME_NULLABLE_LIMIT。实际: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("CTE 查询中的 {{limit}} 应该正确处理")]
    public void CTEQuery_WithLimit_ShouldWork()
    {
        var template = "WITH cte AS (SELECT * FROM {{table}}) SELECT * FROM cte ORDER BY id {{limit}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _methodWithNullableLimit, _userType, "users", dialect);
            Assert.IsTrue(result.IsValid, $"[{GetDialectName(dialect)}] CTE 查询不应该有错误");
            Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_LIMIT_limit}"),
                $"[{GetDialectName(dialect)}] CTE 查询应该包含 RUNTIME_NULLABLE_LIMIT");
        }
    }

    #endregion

    #region 数据库方言区分

    [TestMethod]
    [Description("验证 SQLite 和 SQL Server 的正确区分")]
    public void SQLiteVsSqlServer_ShouldBeDistinguished()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}}";

        var sqliteResult = _engine.ProcessTemplate(template, _methodWithNonNullableLimit, _userType, "users", Sqlx.Generator.SqlDefine.SQLite);
        Assert.IsTrue(sqliteResult.ProcessedSql.Contains("LIMIT @limit"),
            $"SQLite 应该生成 LIMIT @limit。实际: {sqliteResult.ProcessedSql}");

        var sqlServerResult = _engine.ProcessTemplate(template, _methodWithNonNullableLimit, _userType, "users", Sqlx.Generator.SqlDefine.SqlServer);
        Assert.IsTrue(sqlServerResult.ProcessedSql.Contains("{RUNTIME_LIMIT_limit}"),
            $"SQL Server 应该生成 RUNTIME_LIMIT 占位符。实际: {sqlServerResult.ProcessedSql}");
    }

    [TestMethod]
    [Description("验证 DatabaseType 属性正确返回数据库名称")]
    public void DatabaseType_ShouldReturnCorrectName()
    {
        Assert.AreEqual("SQLite", Sqlx.Generator.SqlDefine.SQLite.DatabaseType);
        Assert.AreEqual("SqlServer", Sqlx.Generator.SqlDefine.SqlServer.DatabaseType);
        Assert.AreEqual("MySql", Sqlx.Generator.SqlDefine.MySql.DatabaseType);
        Assert.AreEqual("PostgreSql", Sqlx.Generator.SqlDefine.PostgreSql.DatabaseType);
        Assert.AreEqual("Oracle", Sqlx.Generator.SqlDefine.Oracle.DatabaseType);
    }

    #endregion

    #region 错误处理

    [TestMethod]
    [Description("无效的预定义模式应该回退到默认行为")]
    public void InvalidPredefinedMode_ShouldFallbackToDefault()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit:invalid_mode}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _methodWithNullableLimit, _userType, "users", dialect);
            Assert.IsTrue(result.IsValid, $"[{GetDialectName(dialect)}] 无效的预定义模式不应该产生错误");
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{GetDialectName(dialect)}] 应该生成有效的 SQL");
        }
    }

    #endregion

    #region OFFSET 参数前缀验证

    [TestMethod]
    [Description("MySQL 非可空 offset 应该使用 @ 参数前缀")]
    public void MySQL_NonNullableOffset_ShouldUseAtPrefix()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            using System.Threading.Tasks;
            using System.Collections.Generic;
            public class User { public long Id { get; set; } }
            public interface IRepo { Task<List<User>> GetAsync(int offset); }
        ");

        var compilation = CreateCompilation(syntaxTree);
        var (userType, method) = GetTypeAndMethod(compilation);

        var template = "SELECT * FROM {{table}} ORDER BY id {{offset}}";
        var result = _engine.ProcessTemplate(template, method, userType, "users", Sqlx.Generator.SqlDefine.MySql);

        Assert.IsTrue(result.ProcessedSql.Contains("OFFSET @offset"),
            $"MySQL 非可空 offset 应该使用 @ 参数前缀。实际: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("PostgreSQL 非可空 offset 应该使用 $ 参数前缀")]
    public void PostgreSQL_NonNullableOffset_ShouldUseDollarPrefix()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            using System.Threading.Tasks;
            using System.Collections.Generic;
            public class User { public long Id { get; set; } }
            public interface IRepo { Task<List<User>> GetAsync(int offset); }
        ");

        var compilation = CreateCompilation(syntaxTree);
        var (userType, method) = GetTypeAndMethod(compilation);

        var template = "SELECT * FROM {{table}} ORDER BY id {{offset}}";
        var result = _engine.ProcessTemplate(template, method, userType, "users", Sqlx.Generator.SqlDefine.PostgreSql);

        Assert.IsTrue(result.ProcessedSql.Contains("OFFSET $offset"),
            $"PostgreSQL 非可空 offset 应该使用 $ 参数前缀。实际: {result.ProcessedSql}");
    }

    #endregion

    #region 辅助方法

    private static CSharpCompilation CreateCompilation(SyntaxTree syntaxTree)
    {
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
        };

        return CSharpCompilation.Create("TestAssembly")
            .AddSyntaxTrees(syntaxTree)
            .AddReferences(references);
    }

    private static (INamedTypeSymbol userType, IMethodSymbol method) GetTypeAndMethod(CSharpCompilation compilation)
    {
        var userType = compilation.GetTypeByMetadataName("User")!;
        var interfaceType = compilation.GetTypeByMetadataName("IRepo")!;
        var method = interfaceType.GetMembers().OfType<IMethodSymbol>().First();
        return (userType, method);
    }

    #endregion
}
