// -----------------------------------------------------------------------
// <copyright file="TDD_GroupConcatPlaceholder.cs" company="Cricle">
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
/// TDD tests for {{group_concat}} placeholder - ensures correct syntax
/// Bug: {{group_concat message, ', '}} generates invalid SQL
/// </summary>
[TestClass]
public class TDD_GroupConcatPlaceholder
{
    private SqlTemplateEngine _engine = null!;
    private Compilation _compilation = null!;
    private IMethodSymbol _method = null!;
    private INamedTypeSymbol _logType = null!;

    [TestInitialize]
    public void Initialize()
    {
        _engine = new SqlTemplateEngine();

        var code = @"
            using System.Threading.Tasks;
            using System.Collections.Generic;
            
            public class Log 
            { 
                public long Id { get; set; } 
                public string Level { get; set; } 
                public string Message { get; set; }
            }
            
            public interface IRepo 
            {
                Task<List<Dictionary<string, object?>>> GetSummaryAsync();
            }";

        _compilation = CSharpCompilation.Create("TestAssembly")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(code));

        _logType = _compilation.GetTypeByMetadataName("Log") as INamedTypeSymbol;
        _method = _compilation.GetTypeByMetadataName("IRepo")!
            .GetMembers("GetSummaryAsync").OfType<IMethodSymbol>().First();
    }

    [TestMethod]
    public void GroupConcatPlaceholder_WithSimpleFormat_ShouldGenerateCorrectSQL()
    {
        // Arrange
        var template = "SELECT level, {{group_concat message, ', '}} as messages FROM {{table}}";
        var dialect = Sqlx.Generator.SqlDefine.SQLite;

        // Act
        var result = _engine.ProcessTemplate(template, _method, _logType, "logs", dialect);

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("GROUP_CONCAT([message], ', ')"), 
            $"应该生成 'GROUP_CONCAT([message], ', ')'。实际SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    public void GroupConcatPlaceholder_WithDefaultSeparator_ShouldWork()
    {
        // Arrange
        var template = "SELECT level, {{group_concat message}} as messages FROM {{table}}";
        var dialect = Sqlx.Generator.SqlDefine.SQLite;

        // Act
        var result = _engine.ProcessTemplate(template, _method, _logType, "logs", dialect);

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("GROUP_CONCAT([message], ',')"), 
            $"应该生成 'GROUP_CONCAT([message], ',')'。实际SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    public void GroupConcatPlaceholder_PostgreSQL_ShouldUseStringAgg()
    {
        // Arrange
        var template = "SELECT level, {{group_concat message, ' | '}} as messages FROM {{table}}";
        var dialect = Sqlx.Generator.SqlDefine.PostgreSql;

        // Act
        var result = _engine.ProcessTemplate(template, _method, _logType, "logs", dialect);

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("STRING_AGG"), 
            $"PostgreSQL应该使用STRING_AGG。实际SQL: {result.ProcessedSql}");
        Assert.IsTrue(result.ProcessedSql.Contains("message") || result.ProcessedSql.Contains("\"message\""), 
            $"应该包含message列。实际SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    public void GroupConcatPlaceholder_MySQL_ShouldUseSeparatorKeyword()
    {
        // Arrange
        var template = "SELECT level, {{group_concat message, '; '}} as messages FROM {{table}}";
        var dialect = Sqlx.Generator.SqlDefine.MySql;

        // Act
        var result = _engine.ProcessTemplate(template, _method, _logType, "logs", dialect);

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("GROUP_CONCAT(`message` SEPARATOR '; ')"), 
            $"MySQL应该使用SEPARATOR关键字。实际SQL: {result.ProcessedSql}");
    }

    [TestMethod]
    public void GroupConcatPlaceholder_SqlServer_ShouldUseStringAgg()
    {
        // Arrange
        var template = "SELECT level, {{group_concat message, ', '}} as messages FROM {{table}}";
        var dialect = Sqlx.Generator.SqlDefine.SqlServer;

        // Act
        var result = _engine.ProcessTemplate(template, _method, _logType, "logs", dialect);

        // Assert
        Assert.IsTrue(result.ProcessedSql.Contains("STRING_AGG([message], ', ')"), 
            $"SQL Server应该使用STRING_AGG。实际SQL: {result.ProcessedSql}");
    }
}
