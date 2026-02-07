// -----------------------------------------------------------------------
// <copyright file="VarPlaceholderMethodTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Sqlx.Generator.Tests;

/// <summary>
/// Tests for methods using {{var}} placeholders with [SqlxVar] methods.
/// </summary>
[TestClass]
public class VarPlaceholderMethodTests
{
    [TestMethod]
    public void VarPlaceholder_WithSqlxVarMethod_GeneratesCorrectly()
    {
        var source = @"
using Sqlx;
using Sqlx.Annotations;

namespace TestNamespace
{
    [Sqlx]
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface IUserRepository : ICrudRepository<User, int>
    {
        [SqlTemplate(""SELECT {{var --name a}}"")]
        int AX(int a, double b, string c);
    }

    [RepositoryFor(typeof(IUserRepository))]
    [SqlDefine(SqlDefineTypes.SQLite)]
    public partial class UserRepository
    {
        [SqlxVar(Name = ""a"")]
        private string GetA() => ""test_value"";
    }
}";

        var generator = new RepositoryGenerator();
        var result = GeneratorTestHelper.RunGenerator(source, generator);

        // Should generate without errors
        Assert.IsFalse(result.Diagnostics.Any(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error),
            $"Should not have errors. Errors: {string.Join(", ", result.Diagnostics.Where(d => d.Severity == Microsoft.CodeAnalysis.DiagnosticSeverity.Error).Select(d => d.GetMessage()))}");

        var generatedSources = result.GetAllGeneratedSources().ToList();
        var repositorySource = generatedSources.FirstOrDefault(s => s.FileName.Contains("UserRepository.Repository.g.cs"));
        Assert.IsTrue(repositorySource != default, "Should generate UserRepository.Repository.g.cs");

        var code = repositorySource.Source;
        
        // Debug: print generated code
        System.Console.WriteLine("=== Generated Code ===");
        System.Console.WriteLine(code);
        System.Console.WriteLine("=== End Generated Code ===");

        // Should generate GetDynamicContext() method
        Assert.IsTrue(code.Contains("GetDynamicContext()"), "Should generate GetDynamicContext() method");
        
        // Should generate GetVarValue() method
        Assert.IsTrue(code.Contains("GetVarValue(object instance, string variableName)"), "Should generate GetVarValue() method");
        
        // Should generate AX() method
        Assert.IsTrue(code.Contains("AX(int a, double b, string c)"), "Should generate AX() method");
        
        // Should use dynamic context for template preparation
        Assert.IsTrue(code.Contains("GetDynamicContext()") || code.Contains("_dynamicContext"), 
            "Should use dynamic context for var placeholder");
    }
}
