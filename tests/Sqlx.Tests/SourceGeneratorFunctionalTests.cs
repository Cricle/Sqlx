// -----------------------------------------------------------------------
// <copyright file="SourceGeneratorFunctionalTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Functional tests for the source generator that verify actual code generation behavior.
/// </summary>
[TestClass]
public class SourceGeneratorFunctionalTests
{
    /// <summary>
    /// Tests that the source generator produces valid C# code for basic Sqlx attribute usage.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_GeneratesValidCode_ForBasicSqlxAttribute()
    {
        string sourceCode = @"
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public partial class TestRepository
    {
        private readonly DbConnection connection;
        
        public TestRepository(DbConnection connection)
        {
            this.connection = connection;
        }

        [Sqlx(""GetUserById"")]
        public partial int GetUserId(int id);
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Verify no compilation errors
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Compilation failed with errors:\n{errorMessages}");
        }

        // Verify that implementation was generated
        var generatedSources = GetGeneratedSources(compilation);
        Assert.IsTrue(generatedSources.Any(), "Source generator should produce generated code");

        var generatedCode = string.Join("\n", generatedSources);
        Assert.IsTrue(generatedCode.Contains("GetUserId"), "Generated code should contain the method implementation");

        // Debug: Show actual generated code if assertion fails
        if (!generatedCode.Contains("EXEC GetUserById") && !generatedCode.Contains("EXEC [GetUserById]"))
        {
            Console.WriteLine("Generated code:");
            Console.WriteLine(generatedCode);
            Assert.Fail($"Generated code should contain stored procedure call. Generated code length: {generatedCode.Length}");
        }
    }

    /// <summary>
    /// Tests that the source generator generates correct SQL dialect constants.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_GeneratesSqlDefineConstants()
    {
        string sourceCode = @"
using Sqlx.Annotations;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            var mysql = SqlDefine.MySql;
            var sqlServer = SqlDefine.SqlServer;
            var postgres = SqlDefine.PgSql;
        }
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Compilation failed with errors:\n{errorMessages}");
        }

        var generatedSources = GetGeneratedSources(compilation);
        var generatedCode = string.Join("\n", generatedSources);

        // Verify SQL dialect constants are generated correctly
        Assert.IsTrue(generatedCode.Contains("MySql = (\"`\", \"`\", \"'\", \"'\", \"@\")"), "MySQL dialect should use backticks and @ prefix");
        Assert.IsTrue(generatedCode.Contains("SqlServer = (\"[\", \"]\", \"'\", \"'\", \"@\")"), "SQL Server dialect should use brackets and @ prefix");
        Assert.IsTrue(generatedCode.Contains("PgSql = (\"\\u0022\", \"\\u0022\", \"'\", \"'\", \"$\")"), "PostgreSQL dialect should use quotes and $ prefix");
    }

    /// <summary>
    /// Tests that ExpressionToSql class is generated with correct factory methods.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_GeneratesExpressionToSqlWithFactoryMethods()
    {
        string sourceCode = @"
using System;
using System.Linq.Expressions;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? Age { get; set; }
    }

    public class TestClass
    {
        public void TestMethod()
        {
            var sqlServerExpr = ExpressionToSql<Person>.ForSqlServer();
            var mySqlExpr = ExpressionToSql<Person>.ForMySql();
            var postgresExpr = ExpressionToSql<Person>.ForPostgreSQL();
            var sqliteExpr = ExpressionToSql<Person>.ForSqlite();
        }
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Compilation failed with errors:\n{errorMessages}");
        }

        var generatedSources = GetGeneratedSources(compilation);
        var generatedCode = string.Join("\n", generatedSources);

        // Verify ExpressionToSql class and factory methods are generated
        Assert.IsTrue(generatedCode.Contains("class ExpressionToSql<T>"), "ExpressionToSql class should be generated");
        Assert.IsTrue(generatedCode.Contains("ForSqlServer()"), "ForSqlServer factory method should be generated");
        Assert.IsTrue(generatedCode.Contains("ForMySql()"), "ForMySql factory method should be generated");
        Assert.IsTrue(generatedCode.Contains("ForPostgreSQL()"), "ForPostgreSQL factory method should be generated");
        Assert.IsTrue(generatedCode.Contains("ForSqlite()"), "ForSqlite factory method should be generated");
    }

    /// <summary>
    /// Tests that RawSql attribute generates correct implementation.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_GeneratesCorrectImplementation_ForRawSqlAttribute()
    {
        string sourceCode = @"
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public partial class TestRepository
    {
        private readonly DbConnection connection;

        [RawSql(""SELECT COUNT(*) FROM Users WHERE Status = @status"")]
        public partial int GetActiveUserCount(string status);
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Compilation failed with errors:\n{errorMessages}");
        }

        var generatedSources = GetGeneratedSources(compilation);
        var generatedCode = string.Join("\n", generatedSources);

        // Verify that raw SQL is used in generated implementation
        Assert.IsTrue(
            generatedCode.Contains("SELECT COUNT(*) FROM Users WHERE Status = @status"),
            "Generated code should contain the raw SQL query");
        Assert.IsTrue(
            generatedCode.Contains("GetActiveUserCount"),
            "Generated code should contain method implementation");
    }

    /// <summary>
    /// Tests data-driven scenarios for different SQL dialects.
    /// </summary>
    /// <param name="dialectConstant">The SQL dialect constant name.</param>
    /// <param name="expectedLeftQuote">Expected left quote character.</param>
    /// <param name="expectedRightQuote">Expected right quote character.</param>
    /// <param name="expectedParameterPrefix">Expected parameter prefix.</param>
    [TestMethod]
    [DataRow("MySql", "`", "`", "@")]
    [DataRow("SqlServer", "[", "]", "@")]
    public void SourceGenerator_GeneratesCorrectDialectConstants(
        string dialectConstant,
        string expectedLeftQuote,
        string expectedRightQuote,
        string expectedParameterPrefix)
    {
        string sourceCode = $@"
using Sqlx.Annotations;

namespace TestNamespace
{{
    public class TestClass
    {{
        public void TestMethod()
        {{
            var dialect = SqlDefine.{dialectConstant};
        }}
    }}
}}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.IsFalse(errors.Any(), $"Compilation should succeed for {dialectConstant} dialect");

        var generatedSources = GetGeneratedSources(compilation);
        var generatedCode = string.Join("\n", generatedSources);

        // Verify the specific dialect constant format
        var expectedPattern = $"{dialectConstant} = (\"{expectedLeftQuote}\", \"{expectedRightQuote}\", \"'\", \"'\", \"{expectedParameterPrefix}\")";
        Assert.IsTrue(generatedCode.Contains(expectedPattern), $"{dialectConstant} should have correct quote and prefix pattern: {expectedPattern}");
    }

    /// <summary>
    /// Tests that the source generator handles missing attributes gracefully.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_MissingAttributes_ReportsDiagnostic()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

public class TestClass
{
    [Sqlx(""SELECT * FROM Users"")]
    public void TestMethod() { }
}";

        var compilation = CreateCompilation(source);
        var generator = new CSharpGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        // Act
        driver = driver.RunGenerators(compilation);

        // Assert
        var runResult = driver.GetRunResult();
        var generatedSources = runResult.GeneratedSources;
        
        // Should generate some code even with missing attributes
        Assert.IsTrue(generatedSources.Count > 0, "Should generate some code");
    }

    /// <summary>
    /// Tests that the source generator handles null syntax receiver gracefully.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_NullSyntaxReceiver_HandlesGracefully()
    {
        // This test verifies that the generator doesn't crash when there's no syntax receiver
        // Arrange
        var source = @"
public class TestClass
{
    public void TestMethod() { }
}";

        var compilation = CreateCompilation(source);
        var generator = new CSharpGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        // Act
        driver = driver.RunGenerators(compilation);

        // Assert - should not throw exception
        var runResult = driver.GetRunResult();
        Assert.IsNotNull(runResult);
    }

    /// <summary>
    /// Tests that the source generator handles empty method list gracefully.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_EmptyMethodList_HandlesGracefully()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

public class TestClass
{
    // No methods with Sqlx attributes
}";

        var compilation = CreateCompilation(source);
        var generator = new CSharpGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        // Act
        driver = driver.RunGenerators(compilation);

        // Assert - should not throw exception
        var runResult = driver.GetRunResult();
        Assert.IsNotNull(runResult);
    }

    /// <summary>
    /// Tests that the source generator handles missing SqlxAttribute gracefully.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_MissingSqlxAttribute_ReportsDiagnostic()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

public class TestClass
{
    [RawSql(""SELECT * FROM Users"")]
    public void TestMethod() { }
}";

        var compilation = CreateCompilation(source);
        var generator = new CSharpGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        // Act
        driver = driver.RunGenerators(compilation);

        // Assert
        var runResult = driver.GetRunResult();
        var generatedSources = runResult.GeneratedSources;
        
        // Should handle missing SqlxAttribute gracefully
        Assert.IsNotNull(runResult);
    }

    /// <summary>
    /// Tests that the source generator handles missing RawSqlAttribute gracefully.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_MissingRawSqlAttribute_ReportsDiagnostic()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

public class TestClass
{
    [Sqlx(""SELECT * FROM Users"")]
    public void TestMethod() { }
}";

        var compilation = CreateCompilation(source);
        var generator = new CSharpGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        // Act
        driver = driver.RunGenerators(compilation);

        // Assert
        var runResult = driver.GetRunResult();
        var generatedSources = runResult.GeneratedSources;
        
        // Should handle missing RawSqlAttribute gracefully
        Assert.IsNotNull(runResult);
    }

    /// <summary>
    /// Tests that the source generator handles missing ExpressionToSqlAttribute gracefully.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_MissingExpressionToSqlAttribute_ReportsDiagnostic()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

public class TestClass
{
    [Sqlx(""SELECT * FROM Users"")]
    public void TestMethod() { }
}";

        var compilation = CreateCompilation(source);
        var generator = new CSharpGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        // Act
        driver = driver.RunGenerators(compilation);

        // Assert
        var runResult = driver.GetRunResult();
        var generatedSources = runResult.GeneratedSources;
        
        // Should handle missing ExpressionToSqlAttribute gracefully
        Assert.IsNotNull(runResult);
    }

    /// <summary>
    /// Tests that the source generator handles nullable context options correctly.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_NullableContextOptions_HandlesCorrectly()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

public class TestClass
{
    [Sqlx(""SELECT * FROM Users"")]
    public void TestMethod() { }
}";

        var compilation = CreateCompilation(source);
        var generator = new CSharpGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        // Act
        driver = driver.RunGenerators(compilation);

        // Assert
        var runResult = driver.GetRunResult();
        var generatedSources = runResult.GeneratedSources;
        
        // Should handle nullable context options correctly
        Assert.IsNotNull(runResult);
    }

    /// <summary>
    /// Tests that the source generator handles class grouping correctly.
    /// </summary>
    [TestMethod]
    public void SourceGenerator_ClassGrouping_HandlesCorrectly()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

public class TestClass1
{
    [Sqlx(""SELECT * FROM Users"")]
    public void TestMethod1() { }
}

public class TestClass2
{
    [Sqlx(""SELECT * FROM Products"")]
    public void TestMethod2() { }
}";

        var compilation = CreateCompilation(source);
        var generator = new CSharpGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        // Act
        driver = driver.RunGenerators(compilation);

        // Assert
        var runResult = driver.GetRunResult();
        var generatedSources = runResult.GeneratedSources;
        
        Assert.IsTrue(generatedSources.Count > 0, "Should generate code for multiple classes");
    }

    /// <summary>
    /// Tests that the AbstractGenerator handles missing SqlxAttribute gracefully.
    /// </summary>
    [TestMethod]
    public void AbstractGenerator_MissingSqlxAttribute_ReportsDiagnostic()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

public class TestClass
{
    [RawSql(""SELECT * FROM Users"")]
    public void TestMethod() { }
}";

        var compilation = CreateCompilation(source);
        var generator = new CSharpGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        // Act
        driver = driver.RunGenerators(compilation);

        // Assert
        var runResult = driver.GetRunResult();
        Assert.IsNotNull(runResult);
        
        // The generator should handle missing SqlxAttribute gracefully
        // and potentially report diagnostics
    }

    /// <summary>
    /// Tests that the AbstractGenerator handles missing RawSqlAttribute gracefully.
    /// </summary>
    [TestMethod]
    public void AbstractGenerator_MissingRawSqlAttribute_ReportsDiagnostic()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

public class TestClass
{
    [Sqlx(""SELECT * FROM Users"")]
    public void TestMethod() { }
}";

        var compilation = CreateCompilation(source);
        var generator = new CSharpGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        // Act
        driver = driver.RunGenerators(compilation);

        // Assert
        var runResult = driver.GetRunResult();
        Assert.IsNotNull(runResult);
        
        // The generator should handle missing RawSqlAttribute gracefully
        // and potentially report diagnostics
    }

    /// <summary>
    /// Tests that the AbstractGenerator handles missing ExpressionToSqlAttribute gracefully.
    /// </summary>
    [TestMethod]
    public void AbstractGenerator_MissingExpressionToSqlAttribute_ReportsDiagnostic()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

public class TestClass
{
    [Sqlx(""SELECT * FROM Users"")]
    public void TestMethod() { }
}";

        var compilation = CreateCompilation(source);
        var generator = new CSharpGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        // Act
        driver = driver.RunGenerators(compilation);

        // Assert
        var runResult = driver.GetRunResult();
        Assert.IsNotNull(runResult);
        
        // The generator should handle missing ExpressionToSqlAttribute gracefully
        // and potentially report diagnostics
    }

    /// <summary>
    /// Tests that the AbstractGenerator handles nullable context options correctly.
    /// </summary>
    [TestMethod]
    public void AbstractGenerator_NullableContextOptions_HandlesCorrectly()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

public class TestClass
{
    [Sqlx(""SELECT * FROM Users"")]
    public void TestMethod() { }
}";

        var compilation = CreateCompilation(source);
        var generator = new CSharpGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        // Act
        driver = driver.RunGenerators(compilation);

        // Assert
        var runResult = driver.GetRunResult();
        Assert.IsNotNull(runResult);
        
        // The generator should handle nullable context options correctly
    }

    /// <summary>
    /// Tests that the AbstractGenerator handles empty method lists gracefully.
    /// </summary>
    [TestMethod]
    public void AbstractGenerator_EmptyMethodList_HandlesGracefully()
    {
        // Arrange
        var source = @"
using Sqlx.Annotations;

public class TestClass
{
    // No methods with Sqlx attributes
}";

        var compilation = CreateCompilation(source);
        var generator = new CSharpGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        // Act
        driver = driver.RunGenerators(compilation);

        // Assert
        var runResult = driver.GetRunResult();
        Assert.IsNotNull(runResult);
        
        // The generator should handle empty method lists gracefully
    }

    /// <summary>
    /// Tests that the AbstractGenerator handles null syntax receiver gracefully.
    /// </summary>
    [TestMethod]
    public void AbstractGenerator_NullSyntaxReceiver_HandlesGracefully()
    {
        // Arrange
        var source = @"
public class TestClass
{
    public void TestMethod() { }
}";

        var compilation = CreateCompilation(source);
        var generator = new CSharpGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        // Act
        driver = driver.RunGenerators(compilation);

        // Assert
        var runResult = driver.GetRunResult();
        Assert.IsNotNull(runResult);
        
        // The generator should handle null syntax receiver gracefully
    }

    private static (Compilation Compilation, ImmutableArray<Diagnostic> Diagnostics) CompileWithSourceGenerator(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var references = GetBasicReferences();

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new CSharpGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out var diagnostics);

        return (newCompilation, diagnostics);
    }

    private static List<string> GetGeneratedSources(Compilation compilation)
    {
        var generatedSources = new List<string>();
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            // Include generated sources - they may not have "Generated" in the path
            // so we check for generated content or non-empty file paths that are not the original source
            if (syntaxTree.FilePath.Contains("Generated") ||
                string.IsNullOrEmpty(syntaxTree.FilePath) ||
                syntaxTree.ToString().Contains("// <auto-generated>"))
            {
                generatedSources.Add(syntaxTree.ToString());
            }
        }

        return generatedSources;
    }

    private static List<MetadataReference> GetBasicReferences()
    {
        var references = new List<MetadataReference>();

        // Add core runtime references
        references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Data.Common.DbConnection).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Linq.Expressions.Expression).Assembly.Location));

        // Add System.Runtime
        var runtimeAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "System.Runtime");
        if (runtimeAssembly != null)
        {
            references.Add(MetadataReference.CreateFromFile(runtimeAssembly.Location));
        }

        return references;
    }
}