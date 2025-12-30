// -----------------------------------------------------------------------
// <copyright file="TDD_CoalescePlaceholder.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using Sqlx.Generator;

namespace Sqlx.Tests.Placeholders.Core;

/// <summary>
/// TDD tests for {{coalesce}} placeholder - ensures correct syntax
/// Bug: {{coalesce email, 'default'}} generates invalid SQL
/// </summary>
[TestClass]
public class TDD_CoalescePlaceholder
{
    private SqlTemplateEngine _engine = null!;
    private Compilation _compilation = null!;
    private IMethodSymbol _method = null!;
    private INamedTypeSymbol _userType = null!;

    [TestInitialize]
    public void Initialize()
    {
        _engine = new SqlTemplateEngine();

        var code = @"
            using System.Threading.Tasks;
            
            public class User 
            { 
                public long Id { get; set; } 
                public string Name { get; set; } 
                public string Email { get; set; }
            }
            
            public interface IRepo 
            {
                Task<User?> GetUserAsync(long id);
            }";

        _compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(code));

        _userType = _compilation.GetTypeByMetadataName("User") as INamedTypeSymbol;
        _method = _compilation.GetTypeByMetadataName("IRepo")!
            .GetMembers("GetUserAsync").OfType<IMethodSymbol>().First();
    }

    [TestMethod]
    public void CoalescePlaceholder_WithSimpleFormat_ShouldGenerateCorrectSQL()
    {
        // Arrange
        var template = "SELECT id, name, {{coalesce email, 'no-email@example.com'}} as email FROM {{table}}";
        var dialect = Sqlx.Generator.SqlDefine.SQLite;

        // Act
        var result = _engine.ProcessTemplate(template, _method, _userType, "users", dialect);

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("COALESCE([email], 'no-email@example.com')"), 
            $"应该生成 'COALESCE([email], 'no-email@example.com')'。实际SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    public void CoalescePlaceholder_WithMultipleColumns_ShouldWork()
    {
        // Arrange
        var template = "SELECT {{coalesce email, phone, 'N/A'}} as contact FROM {{table}}";
        var dialect = Sqlx.Generator.SqlDefine.SQLite;

        // Act
        var result = _engine.ProcessTemplate(template, _method, _userType, "users", dialect);

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("COALESCE([email], [phone], 'N/A')"), 
            $"应该生成 'COALESCE([email], [phone], 'N/A')'。实际SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    public void CoalescePlaceholder_WithNumericDefault_ShouldWork()
    {
        // Arrange
        var template = "SELECT {{coalesce balance, 0}} as balance FROM {{table}}";
        var dialect = Sqlx.Generator.SqlDefine.SQLite;

        // Act
        var result = _engine.ProcessTemplate(template, _method, _userType, "users", dialect);

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("COALESCE([balance], 0)"), 
            $"应该生成 'COALESCE([balance], 0)'。实际SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    public void CoalescePlaceholder_PostgreSQL_ShouldUseDoubleQuotes()
    {
        // Arrange
        var template = "SELECT {{coalesce email, 'default'}} as email FROM {{table}}";
        var dialect = Sqlx.Generator.SqlDefine.PostgreSql;

        // Act
        var result = _engine.ProcessTemplate(template, _method, _userType, "users", dialect);

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("COALESCE(\"email\", 'default')"), 
            $"PostgreSQL应该使用双引号。实际SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    public void CoalescePlaceholder_MySQL_ShouldUseBackticks()
    {
        // Arrange
        var template = "SELECT {{coalesce email, 'default'}} as email FROM {{table}}";
        var dialect = Sqlx.Generator.SqlDefine.MySql;

        // Act
        var result = _engine.ProcessTemplate(template, _method, _userType, "users", dialect);

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("COALESCE(`email`, 'default')"), 
            $"MySQL应该使用反引号。实际SQL: {result.ProcessedSql}");
    }
}
