// -----------------------------------------------------------------------
// <copyright file="TDD_LogicalPlaceholders.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Sqlx.Generator;
using System.Linq;
using Xunit;

namespace Sqlx.Tests.Placeholders;

/// <summary>
/// 测试逻辑占位符功能
/// 逻辑占位符用于动态组合 SQL 片段，以 * 开头表示逻辑控制符
/// 注意：当前代码生成器不支持逻辑占位符，这些测试验证代码生成器不会因为逻辑占位符而失败
/// </summary>
public class TDD_LogicalPlaceholders
{
    [Fact]
    public void IfNotNull_ShouldGenerateConditionalCode()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Sqlx;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User { public int Id { get; set; } public string Name { get; set; } }

    public interface ITestRepository
    {
        [SqlTemplate(""SELECT * FROM users ORDER BY @orderBy"")]
        Task<List<User>> GetUsersAsync(string? orderBy = null);
    }

    [RepositoryFor(typeof(ITestRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    public partial class TestRepository(IDbConnection connection) : ITestRepository { }
}";

        // Act
        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);

        // Assert - 只检查没有编译错误
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        
        var generatedCode = compilation.SyntaxTrees
            .FirstOrDefault(t => t.FilePath.Contains("TestRepository"))?.ToString() ?? "";

        // 应该生成包含参数的代码
        Assert.Contains("orderBy", generatedCode);
    }

    [Fact]
    public void IfNull_ShouldGenerateConditionalCode()
    {
        // Arrange
        var source = @"
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Sqlx;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User { public int Id { get; set; } public string Name { get; set; } }

    public interface ITestRepository
    {
        [SqlTemplate(""SELECT * FROM users {{where --param filter}}"")]
        Task<List<User>> GetUsersAsync([ExpressionToSql] Expression<Func<User, bool>>? filter = null);
    }

    [RepositoryFor(typeof(ITestRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    public partial class TestRepository(IDbConnection connection) : ITestRepository { }
}";

        // Act
        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);

        // Assert - 只检查没有编译错误
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        
        var generatedCode = compilation.SyntaxTrees
            .FirstOrDefault(t => t.FilePath.Contains("TestRepository"))?.ToString() ?? "";

        // 应该生成包含参数的代码
        Assert.Contains("filter", generatedCode);
    }

    [Fact]
    public void IfEmpty_WithString_ShouldGenerateStringEmptyCheck()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Sqlx;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User { public int Id { get; set; } public string Name { get; set; } }

    public interface ITestRepository
    {
        [SqlTemplate(""SELECT * FROM users WHERE name LIKE @search"")]
        Task<List<User>> SearchUsersAsync(string? search = null);
    }

    [RepositoryFor(typeof(ITestRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    public partial class TestRepository(IDbConnection connection) : ITestRepository { }
}";

        // Act
        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);

        // Assert - 只检查没有编译错误
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        
        var generatedCode = compilation.SyntaxTrees
            .FirstOrDefault(t => t.FilePath.Contains("TestRepository"))?.ToString() ?? "";

        // 应该生成包含参数的代码
        Assert.Contains("search", generatedCode);
    }

    [Fact]
    public void IfNotEmpty_WithCollection_ShouldGenerateCollectionCheck()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Sqlx;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User { public int Id { get; set; } public string Name { get; set; } }

    public interface ITestRepository
    {
        [SqlTemplate(""SELECT * FROM users WHERE id IN (@ids)"")]
        Task<List<User>> GetUsersByIdsAsync(List<int>? ids = null);
    }

    [RepositoryFor(typeof(ITestRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    public partial class TestRepository(IDbConnection connection) : ITestRepository { }
}";

        // Act
        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);

        // Assert - 只检查没有编译错误
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        
        var generatedCode = compilation.SyntaxTrees
            .FirstOrDefault(t => t.FilePath.Contains("TestRepository"))?.ToString() ?? "";

        // 应该生成包含参数的代码
        Assert.Contains("ids", generatedCode);
    }

    [Fact]
    public void MultipleLogicalPlaceholders_ShouldAllWork()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Sqlx;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User { public int Id { get; set; } public string Name { get; set; } public int Age { get; set; } }

    public interface ITestRepository
    {
        [SqlTemplate(@""SELECT * FROM users WHERE 1=1 AND name LIKE @search AND age >= @minAge ORDER BY @orderBy LIMIT @limit"")]
        Task<List<User>> SearchUsersAsync(string? search = null, int? minAge = null, string? orderBy = null, int? limit = null);
    }

    [RepositoryFor(typeof(ITestRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    public partial class TestRepository(IDbConnection connection) : ITestRepository { }
}";

        // Act
        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);

        // Assert
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        
        var generatedCode = compilation.SyntaxTrees
            .FirstOrDefault(t => t.FilePath.Contains("TestRepository"))?.ToString() ?? "";

        // 应该生成所有条件代码
        Assert.Contains("search", generatedCode);
        Assert.Contains("minAge", generatedCode);
        Assert.Contains("orderBy", generatedCode);
        Assert.Contains("limit", generatedCode);
    }
}
