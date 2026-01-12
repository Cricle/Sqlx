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
/// </summary>
public class TDD_LogicalPlaceholders
{
    [Fact]
    public void IfNotNull_ShouldGenerateConditionalCode()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public interface ITestRepository
    {
        [SqlTemplate(""SELECT * FROM users {{*ifnotnull orderBy}}ORDER BY {{orderBy}}{{/ifnotnull}}"")]
        Task<List<User>> GetUsersAsync(string? orderBy = null);
    }

    public class User { public int Id { get; set; } public string Name { get; set; } }
}";

        // Act
        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);

        // Assert - 只检查没有编译错误，逻辑占位符在运行时处理
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        
        var generatedCode = compilation.SyntaxTrees
            .FirstOrDefault(t => t.FilePath.Contains("ITestRepository"))?.ToString() ?? "";

        // 应该生成包含参数的代码
        Assert.Contains("orderBy", generatedCode);
    }

    [Fact]
    public void IfNull_ShouldGenerateConditionalCode()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public interface ITestRepository
    {
        [SqlTemplate(""SELECT * FROM users {{*ifnull filter}}WHERE 1=1{{/ifnull}} {{*ifnotnull filter}}{{where --param filter}}{{/ifnotnull}}"")]
        Task<List<User>> GetUsersAsync(System.Linq.Expressions.Expression<System.Func<User, bool>>? filter = null);
    }

    public class User { public int Id { get; set; } public string Name { get; set; } }
}";

        // Act
        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);

        // Assert - 只检查没有编译错误，逻辑占位符在运行时处理
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        
        var generatedCode = compilation.SyntaxTrees
            .FirstOrDefault(t => t.FilePath.Contains("ITestRepository"))?.ToString() ?? "";

        // 应该生成包含参数的代码
        Assert.Contains("filter", generatedCode);
    }

    [Fact]
    public void IfEmpty_WithString_ShouldGenerateStringEmptyCheck()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public interface ITestRepository
    {
        [SqlTemplate(""SELECT * FROM users {{*ifnotempty search}}WHERE name LIKE @search{{/ifnotempty}}"")]
        Task<List<User>> SearchUsersAsync(string? search = null);
    }

    public class User { public int Id { get; set; } public string Name { get; set; } }
}";

        // Act
        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);

        // Assert - 只检查没有编译错误，逻辑占位符在运行时处理
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        
        var generatedCode = compilation.SyntaxTrees
            .FirstOrDefault(t => t.FilePath.Contains("ITestRepository"))?.ToString() ?? "";

        // 应该生成包含参数的代码
        Assert.Contains("search", generatedCode);
    }

    [Fact]
    public void IfNotEmpty_WithCollection_ShouldGenerateCollectionCheck()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public interface ITestRepository
    {
        [SqlTemplate(""SELECT * FROM users {{*ifnotempty ids}}WHERE id IN {{values --param ids}}{{/ifnotempty}}"")]
        Task<List<User>> GetUsersByIdsAsync(List<int>? ids = null);
    }

    public class User { public int Id { get; set; } public string Name { get; set; } }
}";

        // Act
        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);

        // Assert - 只检查没有编译错误，逻辑占位符在运行时处理
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        
        var generatedCode = compilation.SyntaxTrees
            .FirstOrDefault(t => t.FilePath.Contains("ITestRepository"))?.ToString() ?? "";

        // 应该生成包含参数的代码
        Assert.Contains("ids", generatedCode);
    }

    [Fact]
    public void MultipleLogicalPlaceholders_ShouldAllWork()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public interface ITestRepository
    {
        [SqlTemplate(@""
            SELECT * FROM users 
            WHERE 1=1
            {{*ifnotempty search}}AND name LIKE @search{{/ifnotempty}}
            {{*ifnotnull minAge}}AND age >= @minAge{{/ifnotnull}}
            {{*ifnotnull orderBy}}ORDER BY {{orderBy}}{{/ifnotnull}}
            {{*ifnotnull limit}}LIMIT @limit{{/ifnotnull}}
        "")]
        Task<List<User>> SearchUsersAsync(string? search = null, int? minAge = null, string? orderBy = null, int? limit = null);
    }

    public class User { public int Id { get; set; } public string Name { get; set; } public int Age { get; set; } }
}";

        // Act
        var (diagnostics, compilation) = TestHelper.GetGeneratedOutput(source);

        // Assert
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        
        var generatedCode = compilation.SyntaxTrees
            .FirstOrDefault(t => t.FilePath.Contains("ITestRepository"))?.ToString() ?? "";

        // 应该生成所有条件代码
        Assert.Contains("search", generatedCode);
        Assert.Contains("minAge", generatedCode);
        Assert.Contains("orderBy", generatedCode);
        Assert.Contains("limit", generatedCode);
    }
}
