// -----------------------------------------------------------------------
// <copyright file="TDD_NullableLimitOffset_AllDialects.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator;
using System.Collections.Immutable;
using System.Linq;

namespace Sqlx.Tests.Placeholders;

/// <summary>
/// TDD tests for nullable LIMIT/OFFSET parameters across all database dialects.
/// 验证可空参数 (int? limit = null, int? offset = null) 在所有数据库方言中的正确处理。
/// </summary>
[TestClass]
public class TDD_NullableLimitOffset_AllDialects
{
    private SqlTemplateEngine _engine = null!;
    private INamedTypeSymbol _userType = null!;
    private IMethodSymbol _methodWithNullableLimit = null!;
    private IMethodSymbol _methodWithNullableLimitOffset = null!;
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

        // Create compilation with test types
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            using System;
            using System.Threading.Tasks;
            using System.Collections.Generic;

            public class User
            {
                public long Id { get; set; }
                public string Name { get; set; }
                public int Age { get; set; }
                public decimal Balance { get; set; }
            }

            public interface IUserRepository
            {
                // 可空 limit 参数
                Task<List<User>> GetTopUsersAsync(int? limit = 10);
                
                // 可空 limit 和 offset 参数
                Task<List<User>> GetPagedAsync(int? limit = null, int? offset = null);
                
                // 非可空 limit 参数
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

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        _userType = compilation.GetTypeByMetadataName("User")!;

        var interfaceType = compilation.GetTypeByMetadataName("IUserRepository")!;
        var methods = interfaceType.GetMembers().OfType<IMethodSymbol>().ToArray();
        _methodWithNullableLimit = methods.First(m => m.Name == "GetTopUsersAsync");
        _methodWithNullableLimitOffset = methods.First(m => m.Name == "GetPagedAsync");
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

    [TestMethod]
    [Description("可空 limit 参数应该生成 RUNTIME_NULLABLE_LIMIT 占位符")]
    public void NullableLimit_ShouldGenerateRuntimeNullablePlaceholder()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _methodWithNullableLimit, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_LIMIT_limit}"),
                $"[{dialectName}] 可空 limit 参数应该生成 RUNTIME_NULLABLE_LIMIT 占位符。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("可空 offset 参数应该生成 RUNTIME_NULLABLE_OFFSET 占位符")]
    public void NullableOffset_ShouldGenerateRuntimeNullablePlaceholder()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}} {{offset}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _methodWithNullableLimitOffset, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_OFFSET_offset}"),
                $"[{dialectName}] 可空 offset 参数应该生成 RUNTIME_NULLABLE_OFFSET 占位符。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("非可空 limit 参数应该直接生成 LIMIT 语法（非 SQL Server）")]
    public void NonNullableLimit_ShouldGenerateDirectLimitSyntax()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}}";

        // SQLite, MySQL, PostgreSQL 应该直接生成 LIMIT @limit
        var directLimitDialects = new[] { Sqlx.Generator.SqlDefine.SQLite, Sqlx.Generator.SqlDefine.MySql, Sqlx.Generator.SqlDefine.PostgreSql };

        foreach (var dialect in directLimitDialects)
        {
            var result = _engine.ProcessTemplate(template, _methodWithNonNullableLimit, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsTrue(result.ProcessedSql.Contains("LIMIT"),
                $"[{dialectName}] 非可空 limit 参数应该直接生成 LIMIT 语法。实际 SQL: {result.ProcessedSql}");
            Assert.IsFalse(result.ProcessedSql.Contains("RUNTIME_NULLABLE"),
                $"[{dialectName}] 非可空参数不应该生成 RUNTIME_NULLABLE 占位符。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("SQL Server 非可空 limit 应该生成运行时占位符")]
    public void SqlServer_NonNullableLimit_ShouldGenerateRuntimePlaceholder()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}}";
        var result = _engine.ProcessTemplate(template, _methodWithNonNullableLimit, _userType, "users", Sqlx.Generator.SqlDefine.SqlServer);

        Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_LIMIT_limit}"),
            $"[SqlServer] 非可空 limit 参数应该生成 RUNTIME_LIMIT 占位符。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("可空 limit 和 offset 组合应该正确生成")]
    public void NullableLimitAndOffset_ShouldGenerateBothPlaceholders()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}} {{offset}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _methodWithNullableLimitOffset, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_LIMIT_limit}"),
                $"[{dialectName}] 应该包含 RUNTIME_NULLABLE_LIMIT 占位符。实际 SQL: {result.ProcessedSql}");
            Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_OFFSET_offset}"),
                $"[{dialectName}] 应该包含 RUNTIME_NULLABLE_OFFSET 占位符。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("所有方言的可空参数处理不应该产生错误")]
    public void NullableParameters_AllDialects_NoErrors()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}} {{offset}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _methodWithNullableLimitOffset, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsTrue(result.IsValid,
                $"[{dialectName}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");
            Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql),
                $"[{dialectName}] 生成的 SQL 不应该为空");
        }
    }

    [TestMethod]
    [Description("验证参数类型检测 - int? 应该被识别为可空类型")]
    public void ParameterTypeDetection_NullableInt_ShouldBeDetected()
    {
        // 验证 _methodWithNullableLimit 的参数是可空类型
        var limitParam = _methodWithNullableLimit.Parameters.FirstOrDefault(p => p.Name == "limit");
        Assert.IsNotNull(limitParam, "应该找到 limit 参数");

        // 检查是否是 Nullable<int>
        var isNullable = limitParam.Type is INamedTypeSymbol namedType &&
                        namedType.IsGenericType &&
                        namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T;
        Assert.IsTrue(isNullable, "limit 参数应该是 Nullable<int> 类型");
    }

    [TestMethod]
    [Description("验证参数类型检测 - int 应该被识别为非可空类型")]
    public void ParameterTypeDetection_NonNullableInt_ShouldBeDetected()
    {
        // 验证 _methodWithNonNullableLimit 的参数是非可空类型
        var limitParam = _methodWithNonNullableLimit.Parameters.FirstOrDefault(p => p.Name == "limit");
        Assert.IsNotNull(limitParam, "应该找到 limit 参数");

        // 检查是否不是 Nullable<int>
        var isNullable = limitParam.Type is INamedTypeSymbol namedType &&
                        namedType.IsGenericType &&
                        namedType.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T;
        Assert.IsFalse(isNullable, "limit 参数不应该是 Nullable<int> 类型");
    }

    [TestMethod]
    [Description("SQLite 可空 limit 应该在运行时生成 LIMIT 语法")]
    public void SQLite_NullableLimit_RuntimeShouldGenerateLimitSyntax()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}}";
        var result = _engine.ProcessTemplate(template, _methodWithNullableLimit, _userType, "users", Sqlx.Generator.SqlDefine.SQLite);

        // 模板处理阶段应该生成 RUNTIME_NULLABLE_LIMIT 占位符
        Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_LIMIT_limit}"),
            $"SQLite 应该生成 RUNTIME_NULLABLE_LIMIT 占位符。实际 SQL: {result.ProcessedSql}");

        // 注意：实际的 LIMIT 语法是在代码生成阶段根据方言生成的
        // 这里只验证模板处理阶段的输出
    }

    [TestMethod]
    [Description("PostgreSQL 可空 limit 应该在运行时生成 LIMIT 语法")]
    public void PostgreSQL_NullableLimit_RuntimeShouldGenerateLimitSyntax()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}}";
        var result = _engine.ProcessTemplate(template, _methodWithNullableLimit, _userType, "users", Sqlx.Generator.SqlDefine.PostgreSql);

        Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_LIMIT_limit}"),
            $"PostgreSQL 应该生成 RUNTIME_NULLABLE_LIMIT 占位符。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("MySQL 可空 limit 应该在运行时生成 LIMIT 语法")]
    public void MySQL_NullableLimit_RuntimeShouldGenerateLimitSyntax()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}}";
        var result = _engine.ProcessTemplate(template, _methodWithNullableLimit, _userType, "users", Sqlx.Generator.SqlDefine.MySql);

        Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_LIMIT_limit}"),
            $"MySQL 应该生成 RUNTIME_NULLABLE_LIMIT 占位符。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("SQL Server 可空 limit 应该在运行时生成 FETCH 语法")]
    public void SqlServer_NullableLimit_RuntimeShouldGenerateFetchSyntax()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}}";
        var result = _engine.ProcessTemplate(template, _methodWithNullableLimit, _userType, "users", Sqlx.Generator.SqlDefine.SqlServer);

        Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_LIMIT_limit}"),
            $"SQL Server 应该生成 RUNTIME_NULLABLE_LIMIT 占位符。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("Oracle 可空 limit 应该在运行时生成正确语法")]
    public void Oracle_NullableLimit_RuntimeShouldGenerateCorrectSyntax()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}}";
        var result = _engine.ProcessTemplate(template, _methodWithNullableLimit, _userType, "users", Sqlx.Generator.SqlDefine.Oracle);

        Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_LIMIT_limit}"),
            $"Oracle 应该生成 RUNTIME_NULLABLE_LIMIT 占位符。实际 SQL: {result.ProcessedSql}");
    }

    #region 边界测试 - 预定义模式

    [TestMethod]
    [Description("{{limit:tiny}} 预定义模式应该在所有方言中生成固定值 5")]
    public void LimitTinyMode_AllDialects_ShouldGenerateFixedValue()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit:tiny}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _methodWithNullableLimit, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsTrue(result.ProcessedSql.Contains("5"),
                $"[{dialectName}] {{{{limit:tiny}}}} 应该生成固定值 5。实际 SQL: {result.ProcessedSql}");
            Assert.IsFalse(result.ProcessedSql.Contains("RUNTIME_NULLABLE"),
                $"[{dialectName}] 预定义模式不应该生成 RUNTIME_NULLABLE 占位符。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{limit:small}} 预定义模式应该在所有方言中生成固定值 10")]
    public void LimitSmallMode_AllDialects_ShouldGenerateFixedValue()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit:small}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _methodWithNullableLimit, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsTrue(result.ProcessedSql.Contains("10"),
                $"[{dialectName}] {{{{limit:small}}}} 应该生成固定值 10。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{limit:medium}} 预定义模式应该在所有方言中生成固定值 50")]
    public void LimitMediumMode_AllDialects_ShouldGenerateFixedValue()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit:medium}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _methodWithNullableLimit, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsTrue(result.ProcessedSql.Contains("50"),
                $"[{dialectName}] {{{{limit:medium}}}} 应该生成固定值 50。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{limit:large}} 预定义模式应该在所有方言中生成固定值 100")]
    public void LimitLargeMode_AllDialects_ShouldGenerateFixedValue()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit:large}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _methodWithNullableLimit, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsTrue(result.ProcessedSql.Contains("100"),
                $"[{dialectName}] {{{{limit:large}}}} 应该生成固定值 100。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("{{limit:page}} 预定义模式应该在所有方言中生成固定值 20")]
    public void LimitPageMode_AllDialects_ShouldGenerateFixedValue()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit:page}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _methodWithNullableLimit, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsTrue(result.ProcessedSql.Contains("20"),
                $"[{dialectName}] {{{{limit:page}}}} 应该生成固定值 20。实际 SQL: {result.ProcessedSql}");
        }
    }

    #endregion

    #region 边界测试 - 参数名称变体

    [TestMethod]
    [Description("自定义参数名 --param customLimit 应该正确处理")]
    public void CustomParamName_ShouldBeHandledCorrectly()
    {
        // 创建带有自定义参数名的方法
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            using System.Threading.Tasks;
            using System.Collections.Generic;

            public class User { public long Id { get; set; } }

            public interface IRepo
            {
                Task<List<User>> GetAsync(int? customLimit = null);
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

        var userType = compilation.GetTypeByMetadataName("User")!;
        var interfaceType = compilation.GetTypeByMetadataName("IRepo")!;
        var method = interfaceType.GetMembers().OfType<IMethodSymbol>().First();

        var template = "SELECT * FROM {{table}} ORDER BY id {{limit --param customLimit}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, method, userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_LIMIT_customLimit}"),
                $"[{dialectName}] 自定义参数名应该生成 RUNTIME_NULLABLE_LIMIT_customLimit。实际 SQL: {result.ProcessedSql}");
        }
    }

    #endregion

    #region 边界测试 - 复杂 SQL 模板

    [TestMethod]
    [Description("复杂查询模板中的可空 LIMIT/OFFSET 应该正确处理")]
    public void ComplexQuery_WithNullableLimitOffset_ShouldWork()
    {
        var template = "SELECT {{columns}} FROM {{table}} WHERE age > @minAge {{orderby id --desc}} {{limit}} {{offset}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _methodWithNullableLimitOffset, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsTrue(result.IsValid,
                $"[{dialectName}] 复杂查询不应该有错误。错误: {string.Join(", ", result.Errors)}");
            Assert.IsTrue(result.ProcessedSql.Contains("SELECT"),
                $"[{dialectName}] 应该包含 SELECT");
            Assert.IsTrue(result.ProcessedSql.Contains("WHERE"),
                $"[{dialectName}] 应该包含 WHERE");
            Assert.IsTrue(result.ProcessedSql.Contains("ORDER BY"),
                $"[{dialectName}] 应该包含 ORDER BY");
            Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_LIMIT_limit}"),
                $"[{dialectName}] 应该包含 RUNTIME_NULLABLE_LIMIT。实际 SQL: {result.ProcessedSql}");
            Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_OFFSET_offset}"),
                $"[{dialectName}] 应该包含 RUNTIME_NULLABLE_OFFSET。实际 SQL: {result.ProcessedSql}");
        }
    }

    [TestMethod]
    [Description("子查询中的可空 LIMIT 应该正确处理")]
    public void SubQuery_WithNullableLimit_ShouldWork()
    {
        var template = "SELECT * FROM {{table}} WHERE id IN (SELECT id FROM {{table}} ORDER BY balance DESC {{limit}})";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _methodWithNullableLimit, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsTrue(result.IsValid,
                $"[{dialectName}] 子查询不应该有错误。错误: {string.Join(", ", result.Errors)}");
            Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_LIMIT_limit}"),
                $"[{dialectName}] 子查询中应该包含 RUNTIME_NULLABLE_LIMIT。实际 SQL: {result.ProcessedSql}");
        }
    }

    #endregion

    #region 边界测试 - 只有 OFFSET 没有 LIMIT

    [TestMethod]
    [Description("只有 {{offset}} 没有 {{limit}} 应该正确处理")]
    public void OnlyOffset_WithoutLimit_ShouldWork()
    {
        // 创建只有 offset 参数的方法
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            using System.Threading.Tasks;
            using System.Collections.Generic;

            public class User { public long Id { get; set; } }

            public interface IRepo
            {
                Task<List<User>> GetAsync(int? offset = null);
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

        var userType = compilation.GetTypeByMetadataName("User")!;
        var interfaceType = compilation.GetTypeByMetadataName("IRepo")!;
        var method = interfaceType.GetMembers().OfType<IMethodSymbol>().First();

        var template = "SELECT * FROM {{table}} ORDER BY id {{offset}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, method, userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsTrue(result.IsValid,
                $"[{dialectName}] 只有 offset 不应该有错误。错误: {string.Join(", ", result.Errors)}");
            Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_OFFSET_offset}"),
                $"[{dialectName}] 应该包含 RUNTIME_NULLABLE_OFFSET。实际 SQL: {result.ProcessedSql}");
        }
    }

    #endregion

    #region 边界测试 - 混合可空和非可空参数

    [TestMethod]
    [Description("混合可空 limit 和非可空 offset 应该正确处理")]
    public void MixedNullability_NullableLimitNonNullableOffset_ShouldWork()
    {
        // 创建混合可空性的方法
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            using System.Threading.Tasks;
            using System.Collections.Generic;

            public class User { public long Id { get; set; } }

            public interface IRepo
            {
                Task<List<User>> GetAsync(int? limit = null, int offset = 0);
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

        var userType = compilation.GetTypeByMetadataName("User")!;
        var interfaceType = compilation.GetTypeByMetadataName("IRepo")!;
        var method = interfaceType.GetMembers().OfType<IMethodSymbol>().First();

        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}} {{offset}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, method, userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsTrue(result.IsValid,
                $"[{dialectName}] 混合可空性不应该有错误。错误: {string.Join(", ", result.Errors)}");
            
            // limit 是可空的，应该生成 RUNTIME_NULLABLE_LIMIT
            Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_LIMIT_limit}"),
                $"[{dialectName}] 可空 limit 应该生成 RUNTIME_NULLABLE_LIMIT。实际 SQL: {result.ProcessedSql}");
            
            // offset 是非可空的，应该直接生成 OFFSET 语法
            Assert.IsFalse(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_OFFSET_offset}"),
                $"[{dialectName}] 非可空 offset 不应该生成 RUNTIME_NULLABLE_OFFSET。实际 SQL: {result.ProcessedSql}");
        }
    }

    #endregion

    #region 边界测试 - 数据库特定语法验证

    [TestMethod]
    [Description("SQL Server 非可空 offset 应该生成 OFFSET...ROWS 语法")]
    public void SqlServer_NonNullableOffset_ShouldGenerateOffsetRowsSyntax()
    {
        // 创建非可空 offset 的方法
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            using System.Threading.Tasks;
            using System.Collections.Generic;

            public class User { public long Id { get; set; } }

            public interface IRepo
            {
                Task<List<User>> GetAsync(int offset);
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

        var userType = compilation.GetTypeByMetadataName("User")!;
        var interfaceType = compilation.GetTypeByMetadataName("IRepo")!;
        var method = interfaceType.GetMembers().OfType<IMethodSymbol>().First();

        var template = "SELECT * FROM {{table}} ORDER BY id {{offset}}";
        var result = _engine.ProcessTemplate(template, method, userType, "users", Sqlx.Generator.SqlDefine.SqlServer);

        Assert.IsTrue(result.ProcessedSql.Contains("OFFSET") && result.ProcessedSql.Contains("ROWS"),
            $"SQL Server 非可空 offset 应该生成 OFFSET...ROWS 语法。实际 SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    [Description("Oracle 非可空 limit 应该生成 ROWNUM 语法")]
    public void Oracle_NonNullableLimit_ShouldGenerateRownumSyntax()
    {
        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}}";
        var result = _engine.ProcessTemplate(template, _methodWithNonNullableLimit, _userType, "users", Sqlx.Generator.SqlDefine.Oracle);

        Assert.IsTrue(result.ProcessedSql.Contains("ROWNUM"),
            $"Oracle 非可空 limit 应该生成 ROWNUM 语法。实际 SQL: {result.ProcessedSql}");
    }

    #endregion

    #region 边界测试 - 空模板和边界情况

    [TestMethod]
    [Description("没有 limit/offset 占位符的模板应该正常处理")]
    public void NoLimitOffsetPlaceholder_ShouldWorkNormally()
    {
        var template = "SELECT * FROM {{table}} WHERE id = @id";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _methodWithNullableLimitOffset, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsTrue(result.IsValid,
                $"[{dialectName}] 没有 limit/offset 的模板不应该有错误");
            Assert.IsFalse(result.ProcessedSql.Contains("RUNTIME_NULLABLE"),
                $"[{dialectName}] 没有 limit/offset 占位符不应该生成 RUNTIME_NULLABLE");
        }
    }

    [TestMethod]
    [Description("多个 {{limit}} 占位符应该都被正确处理")]
    public void MultipleLimitPlaceholders_ShouldAllBeProcessed()
    {
        var template = "SELECT * FROM {{table}} WHERE id IN (SELECT id FROM other_table {{limit:tiny}}) {{limit}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, _methodWithNullableLimit, _userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsTrue(result.IsValid,
                $"[{dialectName}] 多个 limit 占位符不应该有错误");
            
            // 第一个是预定义模式，应该生成固定值 5
            Assert.IsTrue(result.ProcessedSql.Contains("5"),
                $"[{dialectName}] 第一个 limit:tiny 应该生成固定值 5。实际 SQL: {result.ProcessedSql}");
            
            // 第二个是参数化的，应该生成 RUNTIME_NULLABLE_LIMIT
            Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_LIMIT_limit}"),
                $"[{dialectName}] 第二个 limit 应该生成 RUNTIME_NULLABLE_LIMIT。实际 SQL: {result.ProcessedSql}");
        }
    }

    #endregion

    #region 边界测试 - long? 类型参数

    [TestMethod]
    [Description("long? 类型的 limit 参数应该被正确识别为可空")]
    public void LongNullableLimit_ShouldBeRecognizedAsNullable()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(@"
            using System.Threading.Tasks;
            using System.Collections.Generic;

            public class User { public long Id { get; set; } }

            public interface IRepo
            {
                Task<List<User>> GetAsync(long? limit = null);
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

        var userType = compilation.GetTypeByMetadataName("User")!;
        var interfaceType = compilation.GetTypeByMetadataName("IRepo")!;
        var method = interfaceType.GetMembers().OfType<IMethodSymbol>().First();

        var template = "SELECT * FROM {{table}} ORDER BY id {{limit}}";

        foreach (var dialect in AllDialects)
        {
            var result = _engine.ProcessTemplate(template, method, userType, "users", dialect);
            var dialectName = GetDialectName(dialect);

            Assert.IsTrue(result.ProcessedSql.Contains("{RUNTIME_NULLABLE_LIMIT_limit}"),
                $"[{dialectName}] long? 类型的 limit 应该生成 RUNTIME_NULLABLE_LIMIT。实际 SQL: {result.ProcessedSql}");
        }
    }

    #endregion
}
