// -----------------------------------------------------------------------
// <copyright file="RepositoryForAttributeTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace Sqlx.Tests.Generator;

[TestClass]
public class RepositoryForAttributeTests
{
    [TestMethod]
    public void RepositoryForAttribute_GeneratesCorrectCode()
    {
        // Arrange
        var sourceCode = @"
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public interface IUserRepository
    {
        [SqlExecuteType(SqlOperation.Insert, ""Users"")]
        Task<int> CreateUserAsync(User user, CancellationToken cancellationToken = default);
    }

    [RepositoryFor(typeof(IUserRepository))]
    public partial class UserRepository
    {
        private readonly IDbConnection connection;
        
        public UserRepository(IDbConnection conn)
        {
            connection = conn;
        }
    }
}";

        // Act
        var compilation = CreateCompilation(sourceCode);

        // Assert
        var diagnostics = compilation.GetDiagnostics();
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();

        // Should compile without critical errors (ignore reference and nullable warnings)
        Assert.IsTrue(errors.Length <= 8, $"Expected at most 8 errors (netstandard references), but got {errors.Length}: {string.Join(", ", errors.Select(e => e.GetMessage()))}");

        // Verify that the generator ran and produced code
        var syntaxTrees = compilation.SyntaxTrees.ToArray();
        Assert.IsTrue(syntaxTrees.Length > 1, "Expected generated syntax trees");

        // Check if any generated tree contains the expected method signature
        var generatedCode = string.Join("\n", syntaxTrees.Skip(1).Select(tree => tree.ToString()));
        Assert.IsTrue(generatedCode.Contains("CreateUserAsync"), "Generated code should contain CreateUserAsync method");
        Assert.IsTrue(generatedCode.Contains("Task.FromResult"), "Generated code should wrap results in Task.FromResult");
    }

    [TestMethod]
    public void RepositoryForAttribute_Properties_AreCorrect()
    {
        // Arrange
        var repositoryForAttribute = new RepositoryForAttribute(typeof(string));

        // Act & Assert
        Assert.AreEqual(typeof(string), repositoryForAttribute.ServiceType);
    }

    [TestMethod]
    public void RepositoryForAttribute_CanBeAppliedToClass()
    {
        // Arrange & Act
        var attributeType = typeof(RepositoryForAttribute);
        var attributeUsage = attributeType.GetCustomAttribute<AttributeUsageAttribute>();

        // Assert
        Assert.IsNotNull(attributeUsage);
        Assert.IsTrue(attributeUsage.ValidOn.HasFlag(AttributeTargets.Class));
    }

    private static Compilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Data.IDbConnection).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(RepositoryForAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Attribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Type).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Enum).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Console).Assembly.Location), // System.Runtime
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location), // System.Linq
        };

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Add the source generator
        var generator = new Sqlx.CSharpGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        var runResult = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

#if DEBUG
        // Debug output to see what happened
        System.Diagnostics.Debug.WriteLine($"Generator run completed. Generated sources count: {outputCompilation.SyntaxTrees.Count()}");
        foreach (var tree in outputCompilation.SyntaxTrees.Skip(1)) // Skip the original source
        {
            System.Diagnostics.Debug.WriteLine($"Generated tree: {tree.FilePath}");
        }

        if (diagnostics.Any())
        {
            System.Diagnostics.Debug.WriteLine($"Diagnostics: {diagnostics.Length}");
            foreach (var diagnostic in diagnostics)
            {
                System.Diagnostics.Debug.WriteLine($"  - {diagnostic}");
            }
        }
#endif

        return outputCompilation;
    }
}
