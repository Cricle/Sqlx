// -----------------------------------------------------------------------
// <copyright file="VariableConflictRegressionTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests.Integration;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Regression tests for variable conflict resolution.
/// These tests ensure that the specific compilation issues found in the samples are fixed.
/// </summary>
[TestClass]
public class VariableConflictRegressionTests : CodeGenerationTestBase
{
    /// <summary>
    /// Tests the exact scenario that was failing in the ComprehensiveExample project.
    /// This reproduces the ModernSyntaxService compilation errors and verifies they are fixed.
    /// </summary>
    [TestMethod]
    public void ComprehensiveExample_ModernSyntaxService_CompilesWithoutErrors()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace ComprehensiveExample.Services
{
    // Modern C# Record type (C# 9+)
    public record UserRecord(int Id, string Name, string Email, DateTime CreatedAt);
    
    // Primary Constructor class (C# 12+)
    public class ModernUser(int id, string name, string email)
    {
        public int Id { get; init; } = id;
        public string Name { get; init; } = name;
        public string Email { get; init; } = email;
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    }

    public interface IModernSyntaxService
    {
        // Record operations
        UserRecord? GetUserRecord(int id);
        IList<UserRecord> GetAllUserRecords();
        int CreateUserRecord(UserRecord user);
        int UpdateUserRecord(UserRecord user);
        
        // Primary Constructor operations  
        ModernUser? GetModernUser(int id);
        IList<ModernUser> GetAllModernUsers();
        int CreateModernUser(ModernUser user);
        int UpdateModernUser(ModernUser user);
        
        // Mixed async operations
        Task<UserRecord?> GetUserRecordAsync(int id);
        Task<int> CreateUserRecordAsync(UserRecord user);
        Task<int> UpdateModernUserAsync(ModernUser user);
    }

    [RepositoryFor(typeof(IModernSyntaxService))]
    public partial class ModernSyntaxService : IModernSyntaxService
    {
        private readonly DbConnection connection;

        public ModernSyntaxService(DbConnection connection)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Check for the specific errors that were occurring
        var cs0136Errors = diagnostics.Where(d => d.Id == "CS0136").ToList(); // Variable redeclaration
        var cs0841Errors = diagnostics.Where(d => d.Id == "CS0841").ToList(); // Variable used before declaration

        Assert.AreEqual(0, cs0136Errors.Count, 
            $"Should not have CS0136 errors (variable redeclaration). Found: {string.Join(", ", cs0136Errors.Select(e => e.GetMessage()))}");
        
        Assert.AreEqual(0, cs0841Errors.Count, 
            $"Should not have CS0841 errors (variable used before declaration). Found: {string.Join(", ", cs0841Errors.Select(e => e.GetMessage()))}");

        // Verify no compilation errors at all
        var allErrors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (allErrors.Any())
        {
            var errorMessages = string.Join("\n", allErrors.Select(e => e.GetMessage()));
            Assert.Fail($"Generated code should compile without errors. Errors:\n{errorMessages}");
        }
    }

    /// <summary>
    /// Tests the UserService scenario that was failing with variable conflicts.
    /// </summary>
    [TestMethod]
    public void ComprehensiveExample_UserService_CompilesWithoutErrors()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace ComprehensiveExample.Services
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int? DepartmentId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public interface IUserService
    {
        // CRUD operations
        Task<IList<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task<int> CreateUserAsync(User user);
        Task<int> UpdateUserAsync(User user);
        Task<int> DeleteUserAsync(int id);
        
        // Complex queries
        Task<IList<User>> GetUsersByDepartmentAsync(int departmentId);
        Task<int> GetActiveUserCountAsync();
        Task<IList<User>> SearchUsersByNameAsync(string namePattern);
        
        // Batch operations
        Task<int> CreateUsersAsync(IList<User> users);
        Task<int> UpdateUsersAsync(IList<User> users);
    }

    [RepositoryFor(typeof(IUserService))]
    public partial class UserService : IUserService
    {
        private readonly DbConnection connection;

        public UserService(DbConnection connection)
        {
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Verify specific error types are not present
        var variableConflictErrors = diagnostics.Where(d => 
            d.Id == "CS0136" || // Variable redeclaration
            d.Id == "CS0841" || // Variable used before declaration
            d.GetMessage().Contains("__repoCmd__")).ToList();

        Assert.AreEqual(0, variableConflictErrors.Count, 
            $"Should not have variable conflict errors. Found: {string.Join(", ", variableConflictErrors.Select(e => e.GetMessage()))}");

        // Verify no compilation errors
        var allErrors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (allErrors.Any())
        {
            var errorMessages = string.Join("\n", allErrors.Select(e => e.GetMessage()));
            Assert.Fail($"UserService generation should compile without errors. Errors:\n{errorMessages}");
        }
    }

    /// <summary>
    /// Tests multiple service classes in the same compilation to ensure no cross-contamination.
    /// </summary>
    [TestMethod]
    public void MultipleServices_CompileWithoutVariableConflicts()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace ComprehensiveExample.Services
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class Customer
    {
        public int Id { get; set; }
        public string CompanyName { get; set; } = string.Empty;
    }

    // Multiple interfaces and implementations
    public interface IUserService
    {
        Task<int> CreateUserAsync(User user);
        Task<int> UpdateUserAsync(User user);
        Task<int> DeleteUserAsync(int id);
    }

    public interface IDepartmentService
    {
        Task<int> CreateDepartmentAsync(Department dept);
        Task<int> UpdateDepartmentAsync(Department dept);
        Task<int> DeleteDepartmentAsync(int id);
    }

    public interface ICustomerService
    {
        Task<int> CreateCustomerAsync(Customer customer);
        Task<int> UpdateCustomerAsync(Customer customer);
        Task<int> DeleteCustomerAsync(int id);
    }

    [RepositoryFor(typeof(IUserService))]
    public partial class UserService : IUserService
    {
        private readonly DbConnection connection;
        public UserService(DbConnection connection) => this.connection = connection;
    }

    [RepositoryFor(typeof(IDepartmentService))]
    public partial class DepartmentService : IDepartmentService
    {
        private readonly DbConnection connection;
        public DepartmentService(DbConnection connection) => this.connection = connection;
    }

    [RepositoryFor(typeof(ICustomerService))]
    public partial class CustomerService : ICustomerService
    {
        private readonly DbConnection connection;
        public CustomerService(DbConnection connection) => this.connection = connection;
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Verify no variable conflict errors across all generated classes
        var variableConflictErrors = diagnostics.Where(d => 
            d.Id == "CS0136" || 
            d.Id == "CS0841" || 
            d.GetMessage().Contains("__repoCmd__")).ToList();

        Assert.AreEqual(0, variableConflictErrors.Count, 
            $"Should not have variable conflict errors in multiple services. Found: {string.Join(", ", variableConflictErrors.Select(e => e.GetMessage()))}");

        // Verify overall compilation success
        var allErrors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.AreEqual(0, allErrors.Count, 
            $"Multiple services should compile without errors. Found: {string.Join(", ", allErrors.Select(e => e.GetMessage()))}");
    }

    /// <summary>
    /// Tests that the generated code doesn't contain the problematic pattern that was causing issues.
    /// </summary>
    [TestMethod]
    public void GeneratedCode_DoesNotContainProblematicPatterns()
    {
        string sourceCode = @"
using System.Data.Common;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public interface ITestService
    {
        int Create(TestEntity entity);
        int Update(TestEntity entity);
        int Delete(int id);
    }

    [RepositoryFor(typeof(ITestService))]
    public partial class TestService : ITestService
    {
        private readonly DbConnection connection;
        public TestService(DbConnection connection) => this.connection = connection;
    }
}";

        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode, "Should generate code");

        // Verify problematic patterns are not present
        Assert.IsFalse(generatedCode.Contains("var __repoCmd__ = connection.CreateCommand();"), 
            "Generated code should not contain 'var __repoCmd__' redeclarations");

        // Verify correct patterns are present
        Assert.IsTrue(generatedCode.Contains("System.Data.Common.DbCommand? __repoCmd__ = null;"), 
            "Generated code should contain proper __repoCmd__ declaration");
            
        Assert.IsTrue(generatedCode.Contains("__repoCmd__ = connection.CreateCommand();"), 
            "Generated code should contain proper __repoCmd__ assignment");

        // Verify the generated code would compile
        var syntaxTree = CSharpSyntaxTree.ParseText(generatedCode);
        var compilation = CSharpCompilation.Create("TestCompilation")
            .AddSyntaxTrees(syntaxTree);

        var diagnostics = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error && 
                       (d.Id == "CS0136" || d.Id == "CS0841"))
            .ToList();

        Assert.AreEqual(0, diagnostics.Count, 
            $"Generated code should not have variable conflict errors: {string.Join(", ", diagnostics.Select(d => d.GetMessage()))}");
    }

    /// <summary>
    /// Compiles source code with the Sqlx source generator and returns compilation and diagnostics.
    /// </summary>
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

    /// <summary>
    /// Gets basic references needed for compilation.
    /// </summary>
    private static List<MetadataReference> GetBasicReferences()
    {
        var references = new List<MetadataReference>();

        references.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Data.Common.DbConnection).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Linq.Expressions.Expression).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location));
        references.Add(MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location));

        var runtimeAssembly = System.AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "System.Runtime");
        if (runtimeAssembly != null)
        {
            references.Add(MetadataReference.CreateFromFile(runtimeAssembly.Location));
        }

        // Add reference to the Sqlx assembly
        references.Add(MetadataReference.CreateFromFile(typeof(CSharpGenerator).Assembly.Location));

        return references;
    }
}
