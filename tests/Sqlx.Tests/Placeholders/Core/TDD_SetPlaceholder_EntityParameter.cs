// -----------------------------------------------------------------------
// <copyright file="TDD_SetPlaceholder_EntityParameter.cs" company="Cricle">
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
/// TDD测试：{{set}}占位符与实体参数的集成
/// 这是从FullFeatureDemo中发现的bug
/// </summary>
[TestClass]
public class TDD_SetPlaceholder_EntityParameter
{
    private SqlTemplateEngine _engine = null!;
    private Compilation _compilation = null!;
    private IMethodSymbol _updateMethod = null!;
    private INamedTypeSymbol _userType = null!;

    [TestInitialize]
    public void Initialize()
    {
        _engine = new SqlTemplateEngine();

        // 创建测试编译上下文
        var sourceCode = @"
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class User
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int Age { get; set; }
        public decimal Balance { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public interface IUserRepository
    {
        Task<int> UpdateAsync(User user, CancellationToken ct = default);
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
        var methodClass = _compilation.GetTypeByMetadataName("TestNamespace.IUserRepository")!;
        _updateMethod = methodClass.GetMembers("UpdateAsync").OfType<IMethodSymbol>().First();
    }

    [TestMethod]
    [Description("TDD Red: {{set}}占位符应该识别实体参数并生成正确的SET子句")]
    public void SetPlaceholder_WithEntityParameter_ShouldGenerateCorrectSetClause()
    {
        // Arrange
        var template = "UPDATE users SET {{set --exclude Id}} WHERE id = @id";

        // Act
        var result = _engine.ProcessTemplate(template, _updateMethod, _userType, "users", Sqlx.Generator.SqlDefine.SQLite);

        // Assert
        Assert.IsFalse(string.IsNullOrEmpty(result.ProcessedSql), "生成的SQL不应该为空");
        Assert.AreEqual(0, result.Errors.Count, $"不应该有错误。错误: {string.Join(", ", result.Errors)}");

        var sqlLower = result.ProcessedSql.ToLowerInvariant();
        
        // 应该包含SET子句
        Assert.IsTrue(sqlLower.Contains("set"), "应该包含SET关键字");
        
        // 应该包含列名（排除Id）
        Assert.IsTrue(sqlLower.Contains("name"), "应该包含name列");
        Assert.IsTrue(sqlLower.Contains("email"), "应该包含email列");
        Assert.IsTrue(sqlLower.Contains("age"), "应该包含age列");
        Assert.IsTrue(sqlLower.Contains("balance"), "应该包含balance列");
        
        // 不应该包含Id列（因为--exclude Id）
        var setClause = result.ProcessedSql.Substring(
            result.ProcessedSql.IndexOf("SET", StringComparison.OrdinalIgnoreCase),
            result.ProcessedSql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase) - 
            result.ProcessedSql.IndexOf("SET", StringComparison.OrdinalIgnoreCase));
        
        Assert.IsFalse(setClause.ToLowerInvariant().Contains("id ="), 
            "SET子句不应该包含id列（因为使用了--exclude Id）");
    }

    [TestMethod]
    [Description("TDD Red: {{set}}占位符应该为所有数据库方言生成正确的参数前缀")]
    public void SetPlaceholder_WithEntityParameter_AllDialects_ShouldUseCorrectParameterPrefix()
    {
        // Arrange
        var template = "UPDATE users SET {{set --exclude Id}} WHERE id = @id";
        var dialects = new[]
        {
            (Sqlx.Generator.SqlDefine.SQLite, "@"),
            (Sqlx.Generator.SqlDefine.MySql, "@"),
            (Sqlx.Generator.SqlDefine.SqlServer, "@"),
            (Sqlx.Generator.SqlDefine.PostgreSql, "@")
        };

        foreach (var (dialect, expectedPrefix) in dialects)
        {
            // Act
            var result = _engine.ProcessTemplate(template, _updateMethod, _userType, "users", dialect);

            // Assert
            Assert.AreEqual(0, result.Errors.Count, 
                $"[{dialect.DatabaseType}] 不应该有错误。错误: {string.Join(", ", result.Errors)}");
            
            // 应该包含正确的参数前缀
            var hasCorrectPrefix = result.ProcessedSql.Contains(expectedPrefix);
            Assert.IsTrue(hasCorrectPrefix, 
                $"[{dialect.DatabaseType}] 应该使用{expectedPrefix}作为参数前缀。实际SQL: {result.ProcessedSql}");
        }
    }
}
