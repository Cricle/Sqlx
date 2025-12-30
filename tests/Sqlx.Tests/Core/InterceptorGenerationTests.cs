// -----------------------------------------------------------------------
// <copyright file="InterceptorGenerationTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Generator;
using System.Linq;

namespace Sqlx.Tests.Core;

/// <summary>
/// Tests for interceptor method generation functionality.
/// </summary>
[TestClass]
public class InterceptorGenerationTests
{
    private CodeGenerationService _codeGenerationService = null!;
    private Compilation _compilation = null!;
    private INamedTypeSymbol _repositoryType = null!;

    [TestInitialize]
    public void Initialize()
    {
        _codeGenerationService = new CodeGenerationService();

        // Create a test compilation with a repository class
        var sourceCode = @"
using System;
using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public interface IUserRepository
    {
        Task<User?> GetUserByIdAsync(int id);
        Task<List<User>> GetAllUsersAsync();
        Task<int> CreateUserAsync(User user);
        Task<int> UpdateUserAsync(User user);
        Task<int> DeleteUserAsync(int id);
    }

    public partial class UserRepository : IUserRepository
    {
        private readonly IDbConnection _connection;

        public UserRepository(IDbConnection connection)
        {
            _connection = connection;
        }
    }
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Data.IDbConnection).Assembly.Location)
        };

        _compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        _repositoryType = _compilation.GetTypeByMetadataName("TestNamespace.UserRepository")!;
    }

    [TestMethod]
    public void GenerateInterceptorMethods_GeneratesOnExecutingMethod()
    {
        // Arrange
        var sb = new IndentedStringBuilder(string.Empty);

        // Act
        _codeGenerationService.GenerateInterceptorMethods(sb, _repositoryType);
        var result = sb.ToString();

        // Assert
        Assert.IsTrue(result.Contains("partial void OnExecuting(string operationName, global::System.Data.Common.DbCommand command)"),
            "Should generate OnExecuting method");
        Assert.IsTrue(result.Contains("Called before executing a repository operation"),
            "Should include documentation for OnExecuting");
    }

    [TestMethod]
    public void GenerateInterceptorMethods_GeneratesOnExecutedMethod()
    {
        // Arrange
        var sb = new IndentedStringBuilder(string.Empty);

        // Act
        _codeGenerationService.GenerateInterceptorMethods(sb, _repositoryType);
        var result = sb.ToString();

        // Assert
        Assert.IsTrue(result.Contains("partial void OnExecuted(string operationName, global::System.Data.Common.DbCommand command, object? result, long elapsedTicks)"),
            "Should generate OnExecuted method");
        Assert.IsTrue(result.Contains("Called after successfully executing a repository operation"),
            "Should include documentation for OnExecuted");
    }

    [TestMethod]
    public void GenerateInterceptorMethods_GeneratesOnExecuteFailMethod()
    {
        // Arrange
        var sb = new IndentedStringBuilder(string.Empty);

        // Act
        _codeGenerationService.GenerateInterceptorMethods(sb, _repositoryType);
        var result = sb.ToString();

        // Assert
        Assert.IsTrue(result.Contains("partial void OnExecuteFail(string operationName, global::System.Data.Common.DbCommand command, global::System.Exception exception, long elapsedTicks)"),
            "Should generate OnExecuteFail method");
        Assert.IsTrue(result.Contains("Called when a repository operation fails with an exception"),
            "Should include documentation for OnExecuteFail");
    }

    [TestMethod]
    public void GenerateInterceptorMethods_GeneratesAllThreeInterceptors()
    {
        // Arrange
        var sb = new IndentedStringBuilder(string.Empty);

        // Act
        _codeGenerationService.GenerateInterceptorMethods(sb, _repositoryType);
        var result = sb.ToString();

        // Assert
        Assert.IsTrue(result.Contains("OnExecuting"), "Should contain OnExecuting");
        Assert.IsTrue(result.Contains("OnExecuted"), "Should contain OnExecuted");
        Assert.IsTrue(result.Contains("OnExecuteFail"), "Should contain OnExecuteFail");

        // Verify all are partial void methods
        Assert.IsTrue(result.Contains("partial void OnExecuting"), "OnExecuting should be partial void");
        Assert.IsTrue(result.Contains("partial void OnExecuted"), "OnExecuted should be partial void");
        Assert.IsTrue(result.Contains("partial void OnExecuteFail"), "OnExecuteFail should be partial void");
    }

    [TestMethod]
    public void GenerateInterceptorMethods_IncludesProperParameterDocumentation()
    {
        // Arrange
        var sb = new IndentedStringBuilder(string.Empty);

        // Act
        _codeGenerationService.GenerateInterceptorMethods(sb, _repositoryType);
        var result = sb.ToString();

        // Assert
        // Check parameter documentation for each method
        Assert.IsTrue(result.Contains("operationName\">The name of the operation"),
            "Should document operationName parameter");
        Assert.IsTrue(result.Contains("command\">The database command"),
            "Should document command parameter");
        Assert.IsTrue(result.Contains("result\">The result of the operation"),
            "Should document result parameter");
        Assert.IsTrue(result.Contains("elapsedTicks\">The elapsed time"),
            "Should document elapsedTicks parameter");
        Assert.IsTrue(result.Contains("exception\">The exception that occurred"),
            "Should document exception parameter");
    }

    [TestMethod]
    public void GenerateInterceptorMethods_UsesGlobalNamespaces()
    {
        // Arrange
        var sb = new IndentedStringBuilder(string.Empty);

        // Act
        _codeGenerationService.GenerateInterceptorMethods(sb, _repositoryType);
        var result = sb.ToString();

        // Assert
        Assert.IsTrue(result.Contains("global::System.Data.Common.DbCommand"),
            "Should use global namespace for DbCommand");
        Assert.IsTrue(result.Contains("global::System.Exception"),
            "Should use global namespace for Exception");
    }

    [TestMethod]
    public void GenerateInterceptorMethods_ProducesValidCSharpCode()
    {
        // Arrange
        var sb = new IndentedStringBuilder(string.Empty);

        // Act
        _codeGenerationService.GenerateInterceptorMethods(sb, _repositoryType);
        var result = sb.ToString();

        // Assert
        // Verify that generated code is syntactically correct by parsing it
        var wrappedCode = $@"
using System;
using System.Data;

namespace Test
{{
    public partial class TestClass
    {{
{result}
    }}
}}";

        var syntaxTree = CSharpSyntaxTree.ParseText(wrappedCode);
        var diagnostics = syntaxTree.GetDiagnostics();
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();

        Assert.IsFalse(errors.Any(),
            $"Generated code should be syntactically valid. Errors: {string.Join(", ", errors.Select(e => e.GetMessage()))}");
    }

    [TestMethod]
    public void GenerateInterceptorMethods_GeneratesCleanOutput()
    {
        // Arrange
        var sb = new IndentedStringBuilder(string.Empty);

        // Act
        _codeGenerationService.GenerateInterceptorMethods(sb, _repositoryType);
        var result = sb.ToString();

        // Assert
        // Verify clean formatting
        Assert.IsFalse(result.Contains("    ;"), "Should not have trailing semicolons with spaces");
        Assert.IsTrue(result.Contains("/// <summary>"), "Should include XML documentation");
        Assert.IsTrue(result.Contains("/// </summary>"), "Should close XML documentation");

        // Verify proper method signatures
        Assert.IsTrue(result.Contains("partial void OnExecuting(string operationName, global::System.Data.Common.DbCommand command);"));
        Assert.IsTrue(result.Contains("partial void OnExecuted(string operationName, global::System.Data.Common.DbCommand command, object? result, long elapsedTicks);"));
        Assert.IsTrue(result.Contains("partial void OnExecuteFail(string operationName, global::System.Data.Common.DbCommand command, global::System.Exception exception, long elapsedTicks);"));
    }
}
