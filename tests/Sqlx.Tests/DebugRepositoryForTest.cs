// -----------------------------------------------------------------------
// <copyright file="DebugRepositoryForTest.cs" company="Cricle">
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
/// Debug test to understand what's happening with RepositoryFor generator.
/// </summary>
[TestClass]
public class DebugRepositoryForTest
{
    /// <summary>
    /// Debug test to see what code is actually generated.
    /// </summary>
    [TestMethod]
    public void Debug_RepositoryFor_ShowGeneratedCode()
    {
        string sourceCode = @"
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface IUserService
    {
        IList<User> GetAll();
        User? GetById(int id);
        int Create(User user);
    }

    [RepositoryFor(typeof(IUserService))]
    public partial class UserRepository
    {
        // Should be auto-implemented
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Show all diagnostics
        Console.WriteLine("=== DIAGNOSTICS ===");
        foreach (var diagnostic in diagnostics)
        {
            Console.WriteLine($"{diagnostic.Severity}: {diagnostic.GetMessage()}");
        }

        // Show all generated sources
        var debugInfo = "\n=== ALL SYNTAX TREES ===\n";
        int i = 0;
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            debugInfo += $"\n--- Syntax Tree {i++} ---\n";
            debugInfo += $"File Path: {syntaxTree.FilePath ?? "No Path"}\n";
            debugInfo += $"Length: {syntaxTree.ToString().Length}\n";
            if (syntaxTree.ToString().Length < 5000) // Only show small files
            {
                debugInfo += "Content:\n";
                debugInfo += syntaxTree.ToString() + "\n";
            }
            else
            {
                debugInfo += "Content (first 1000 chars):\n";
                debugInfo += syntaxTree.ToString().Substring(0, 1000) + "...\n";
            }
        }

        // Show diagnostics info
        var diagnosticsInfo = "\n=== DIAGNOSTICS ===\n";
        foreach (var diagnostic in diagnostics)
        {
            diagnosticsInfo += $"{diagnostic.Severity}: {diagnostic.GetMessage()}\n";
        }

        // Use failure to show debug info - will show in test output
        if (compilation.SyntaxTrees.Count() <= 1)
        {
            Assert.Fail($"No generated code found!\n{diagnosticsInfo}\n{debugInfo}");
        }

        // Test passes if we have generated code
        Assert.IsTrue(true, "Debug test passes - generated code found");
    }

    private static (Compilation Compilation, ImmutableArray<Diagnostic> Diagnostics) CompileWithSourceGenerator(string sourceCode)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var references = GetBasicReferences();

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable));

        var generator = new CSharpGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out var diagnostics);

        return (newCompilation, diagnostics);
    }

    private static List<MetadataReference> GetBasicReferences()
    {
        var references = new List<MetadataReference>();

        references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Data.Common.DbConnection).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Linq.Expressions.Expression).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location));

        var runtimeAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "System.Runtime");
        if (runtimeAssembly != null)
        {
            references.Add(MetadataReference.CreateFromFile(runtimeAssembly.Location));
        }

        references.Add(MetadataReference.CreateFromFile(typeof(CSharpGenerator).Assembly.Location));

        return references;
    }
}
