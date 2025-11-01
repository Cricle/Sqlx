// -----------------------------------------------------------------------
// <copyright file="TemplateInheritanceResolverTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator;
using Sqlx.Generator.Core;
using System.Linq;

namespace Sqlx.Tests.Generator;

[TestClass]
public class TemplateInheritanceResolverTests
{
    private const string TestCode = @"
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
    }

    public interface IBaseRepository
    {
        [SqlTemplate(""SELECT * FROM {{table}} WHERE id = @id"")]
        Task<TestEntity?> GetByIdAsync(int id, CancellationToken ct);

        [SqlTemplate(""INSERT INTO {{table}} ({{columns}}) VALUES (@id, @name, @age) {{returning_id}}"")]
        Task<int> InsertAsync(TestEntity entity, CancellationToken ct);

        [SqlTemplate(""UPDATE {{table}} SET active = {{bool_true}} WHERE id = @id"")]
        Task UpdateActivateAsync(int id, CancellationToken ct);
    }
}
";

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ResolveInheritedTemplates_WithPlaceholders_ShouldReplace()
    {
        // Arrange
        var (compilation, interfaceSymbol, entityType) = CompileAndGetSymbols(TestCode, "IBaseRepository", "TestEntity");
        
        // Debug: check if symbols are null
        Assert.IsNotNull(interfaceSymbol, "Interface symbol should not be null");
        Assert.IsNotNull(entityType, "Entity type should not be null");
        
        var resolver = new TemplateInheritanceResolver();
        var dialectProvider = new PostgreSqlDialectProvider();

        // Act
        var templates = resolver.ResolveInheritedTemplates(
            interfaceSymbol,
            dialectProvider,
            tableName: "test_entities",
            entityType);

        // Debug: print compilation diagnostics if templates is empty
        if (templates.Count == 0)
        {
            var diagnostics = compilation.GetDiagnostics();
            foreach (var diag in diagnostics)
            {
                System.Console.WriteLine($"Diagnostic: {diag}");
            }
        }

        // Assert
        Assert.AreEqual(3, templates.Count, "Should find 3 templates");

        var getByIdTemplate = templates.FirstOrDefault(t => t.Method.Name == "GetByIdAsync");
        Assert.IsNotNull(getByIdTemplate, "GetByIdAsync template should exist");
        Assert.IsTrue(getByIdTemplate.ContainsPlaceholders);
        Assert.AreEqual(@"SELECT * FROM {{table}} WHERE id = @id", getByIdTemplate.OriginalSql);
        Assert.AreEqual(@"SELECT * FROM ""test_entities"" WHERE id = @id", getByIdTemplate.ProcessedSql);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ResolveInheritedTemplates_PostgreSQL_ShouldUsePostgreSQLSyntax()
    {
        // Arrange
        var (compilation, interfaceSymbol, entityType) = CompileAndGetSymbols(TestCode, "IBaseRepository", "TestEntity");
        var resolver = new TemplateInheritanceResolver();
        var dialectProvider = new PostgreSqlDialectProvider();

        // Act
        var templates = resolver.ResolveInheritedTemplates(
            interfaceSymbol,
            dialectProvider,
            tableName: "users",
            entityType);

        // Assert
        var insertTemplate = templates.First(t => t.Method.Name == "InsertAsync");
        Assert.IsTrue(insertTemplate.ProcessedSql.Contains(@"""users"""));
        Assert.IsTrue(insertTemplate.ProcessedSql.Contains("RETURNING id"));

        var updateTemplate = templates.First(t => t.Method.Name == "UpdateActivateAsync");
        Assert.IsTrue(updateTemplate.ProcessedSql.Contains("true"));
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ResolveInheritedTemplates_MySQL_ShouldUseMySQLSyntax()
    {
        // Arrange
        var (compilation, interfaceSymbol, entityType) = CompileAndGetSymbols(TestCode, "IBaseRepository", "TestEntity");
        var resolver = new TemplateInheritanceResolver();
        var dialectProvider = new MySqlDialectProvider();

        // Act
        var templates = resolver.ResolveInheritedTemplates(
            interfaceSymbol,
            dialectProvider,
            tableName: "users",
            entityType);

        // Assert
        var insertTemplate = templates.First(t => t.Method.Name == "InsertAsync");
        Assert.IsTrue(insertTemplate.ProcessedSql.Contains("`users`"));
        Assert.IsFalse(insertTemplate.ProcessedSql.Contains("RETURNING")); // MySQL doesn't use RETURNING

        var updateTemplate = templates.First(t => t.Method.Name == "UpdateActivateAsync");
        Assert.IsTrue(updateTemplate.ProcessedSql.Contains("1")); // MySQL uses 1 for true
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ResolveInheritedTemplates_SqlServer_ShouldUseSqlServerSyntax()
    {
        // Arrange
        var (compilation, interfaceSymbol, entityType) = CompileAndGetSymbols(TestCode, "IBaseRepository", "TestEntity");
        var resolver = new TemplateInheritanceResolver();
        var dialectProvider = new SqlServerDialectProvider();

        // Act
        var templates = resolver.ResolveInheritedTemplates(
            interfaceSymbol,
            dialectProvider,
            tableName: "users",
            entityType);

        // Assert
        var insertTemplate = templates.First(t => t.Method.Name == "InsertAsync");
        Assert.IsTrue(insertTemplate.ProcessedSql.Contains("[users]"));
        Assert.IsFalse(insertTemplate.ProcessedSql.Contains("RETURNING")); // SQL Server doesn't use RETURNING

        var updateTemplate = templates.First(t => t.Method.Name == "UpdateActivateAsync");
        Assert.IsTrue(updateTemplate.ProcessedSql.Contains("1")); // SQL Server uses 1 for true
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ResolveInheritedTemplates_NoPlaceholders_ShouldReturnOriginal()
    {
        // Arrange
        var code = @"
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class TestEntity
    {
        public int Id { get; set; }
    }

    public interface ISimpleRepository
    {
        [SqlTemplate(""SELECT * FROM users WHERE id = @id"")]
        Task<TestEntity?> GetByIdAsync(int id, CancellationToken ct);
    }
}
";
        var (compilation, interfaceSymbol, entityType) = CompileAndGetSymbols(code, "ISimpleRepository", "TestEntity");
        var resolver = new TemplateInheritanceResolver();
        var dialectProvider = new PostgreSqlDialectProvider();

        // Act
        var templates = resolver.ResolveInheritedTemplates(
            interfaceSymbol,
            dialectProvider,
            tableName: "users",
            entityType);

        // Assert
        Assert.AreEqual(1, templates.Count);
        var template = templates[0];
        Assert.IsFalse(template.ContainsPlaceholders);
        Assert.AreEqual("SELECT * FROM users WHERE id = @id", template.OriginalSql);
        Assert.AreEqual("SELECT * FROM users WHERE id = @id", template.ProcessedSql);
    }

    [TestMethod]
    [TestCategory(TestCategories.Unit)]
    public void ResolveInheritedTemplates_MultipleBaseInterfaces_ShouldCollectAll()
    {
        // Arrange
        var code = @"
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class TestEntity
    {
        public int Id { get; set; }
    }

    public interface IReadRepository
    {
        [SqlTemplate(""SELECT * FROM {{table}} WHERE id = @id"")]
        Task<TestEntity?> GetByIdAsync(int id, CancellationToken ct);
    }

    public interface IWriteRepository
    {
        [SqlTemplate(""INSERT INTO {{table}} VALUES (@id)"")]
        Task<int> InsertAsync(TestEntity entity, CancellationToken ct);
    }

    public interface ICombinedRepository : IReadRepository, IWriteRepository
    {
        [SqlTemplate(""DELETE FROM {{table}} WHERE id = @id"")]
        Task DeleteAsync(int id, CancellationToken ct);
    }
}
";
        var (compilation, interfaceSymbol, entityType) = CompileAndGetSymbols(code, "ICombinedRepository", "TestEntity");
        var resolver = new TemplateInheritanceResolver();
        var dialectProvider = new PostgreSqlDialectProvider();

        // Act
        var templates = resolver.ResolveInheritedTemplates(
            interfaceSymbol,
            dialectProvider,
            tableName: "users",
            entityType);

        // Assert
        Assert.AreEqual(3, templates.Count);
        Assert.IsTrue(templates.Any(t => t.Method.Name == "GetByIdAsync"));
        Assert.IsTrue(templates.Any(t => t.Method.Name == "InsertAsync"));
        Assert.IsTrue(templates.Any(t => t.Method.Name == "DeleteAsync"));
    }

    private (Compilation compilation, INamedTypeSymbol interfaceSymbol, INamedTypeSymbol entityType) CompileAndGetSymbols(
        string code,
        string interfaceName,
        string entityName)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        
        // Add reference to the basic runtime assemblies
        var refPaths = new[]
        {
            typeof(object).Assembly.Location,
            typeof(System.Threading.Tasks.Task).Assembly.Location,
            typeof(System.Linq.Enumerable).Assembly.Location,
            typeof(Sqlx.Annotations.SqlTemplateAttribute).Assembly.Location,
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Runtime.dll"),
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(object).Assembly.Location)!, "netstandard.dll")
        };

        var references = refPaths.Where(System.IO.File.Exists).Select(r => MetadataReference.CreateFromFile(r)).ToArray();

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var root = syntaxTree.GetRoot();

        var interfaceDeclaration = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.InterfaceDeclarationSyntax>()
            .First(i => i.Identifier.Text == interfaceName);

        var entityDeclaration = root.DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
            .First(c => c.Identifier.Text == entityName);

        var interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceDeclaration) as INamedTypeSymbol;
        var entitySymbol = semanticModel.GetDeclaredSymbol(entityDeclaration) as INamedTypeSymbol;

        return (compilation, interfaceSymbol!, entitySymbol!);
    }
}

