// -----------------------------------------------------------------------
// <copyright file="VariableConflictResolutionTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.Tests.Core;

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for variable conflict resolution in code generation.
/// Ensures that generated code doesn't have variable redeclaration issues.
/// </summary>
[TestClass]
public class VariableConflictResolutionTests : CodeGenerationTestBase
{
    /// <summary>
    /// Tests that Insert operations don't generate duplicate variable declarations.
    /// This test specifically validates the fix for CS0136 error where __repoCmd__ was redeclared.
    /// </summary>
    [TestMethod]
    public void InsertOperation_DoesNotRedeclareRepoCmdVariable()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public interface IUserService
    {
        int CreateUser(User user);
        int InsertUser(User user);
    }

    [RepositoryFor(typeof(IUserService))]
    public partial class UserService : IUserService
    {
        private readonly DbConnection connection;
        
        public UserService(DbConnection connection)
        {
            this.connection = connection;
        }
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Verify no compilation errors
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Generated code should compile without errors. Errors:\n{errorMessages}");
        }

        // Get the generated code and verify it doesn't contain duplicate declarations
        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);
        
        // Verify that __repoCmd__ is declared only once per method
        var repoCmdDeclarations = CountStringOccurrences(generatedCode, "System.Data.Common.DbCommand? __repoCmd__ = null;");
        var repoCmdAssignments = CountStringOccurrences(generatedCode, "__repoCmd__ = connection.CreateCommand();");
        
        // Should have at least one declaration and assignments, but no "var __repoCmd__" redeclarations
        Assert.IsTrue(repoCmdDeclarations > 0, "Should have __repoCmd__ declarations");
        Assert.IsTrue(repoCmdAssignments > 0, "Should have __repoCmd__ assignments");
        
        // Most importantly, should not have any "var __repoCmd__" redeclarations
        var varRedeclarations = CountStringOccurrences(generatedCode, "var __repoCmd__ = connection.CreateCommand();");
        Assert.AreEqual(0, varRedeclarations, "Should not have any 'var __repoCmd__' redeclarations");
    }

    /// <summary>
    /// Tests that Update operations don't generate duplicate variable declarations.
    /// </summary>
    [TestMethod]
    public void UpdateOperation_DoesNotRedeclareRepoCmdVariable()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public interface IUserService
    {
        int UpdateUser(User user);
        int ModifyUser(User user);
    }

    [RepositoryFor(typeof(IUserService))]
    public partial class UserService : IUserService
    {
        private readonly DbConnection connection;
        
        public UserService(DbConnection connection)
        {
            this.connection = connection;
        }
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Verify no compilation errors
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Generated code should compile without errors. Errors:\n{errorMessages}");
        }

        // Get the generated code and verify variable usage
        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        Assert.IsNotNull(generatedCode);
        
        // Should not have any "var __repoCmd__" redeclarations
        var varRedeclarations = CountStringOccurrences(generatedCode, "var __repoCmd__ = connection.CreateCommand();");
        Assert.AreEqual(0, varRedeclarations, "Should not have any 'var __repoCmd__' redeclarations");
    }

    /// <summary>
    /// Tests that Delete operations don't generate duplicate variable declarations.
    /// </summary>
    [TestMethod]
    public void DeleteOperation_DoesNotRedeclareRepoCmdVariable()
    {
        string sourceCode = @"
using System.Data.Common;
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
        int DeleteUser(User user);
        int RemoveUser(int id);
    }

    [RepositoryFor(typeof(IUserService))]
    public partial class UserService : IUserService
    {
        private readonly DbConnection connection;
        
        public UserService(DbConnection connection)
        {
            this.connection = connection;
        }
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Verify no compilation errors
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Generated code should compile without errors. Errors:\n{errorMessages}");
        }

        // Verify no variable redeclarations
        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        var varRedeclarations = CountStringOccurrences(generatedCode, "var __repoCmd__ = connection.CreateCommand();");
        Assert.AreEqual(0, varRedeclarations, "Should not have any 'var __repoCmd__' redeclarations");
    }

    /// <summary>
    /// Tests that Custom SQL operations with Sqlx attribute don't generate duplicate variable declarations.
    /// </summary>
    [TestMethod]
    public void CustomSqlOperation_DoesNotRedeclareRepoCmdVariable()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public partial class UserService
    {
        private readonly DbConnection connection;
        
        public UserService(DbConnection connection)
        {
            this.connection = connection;
        }

        [Sqlx(""SELECT COUNT(*) FROM Users WHERE Name = @name"")]
        public partial int GetUserCountByName(string name);
        
        [Sqlx(""SELECT * FROM Users WHERE Email LIKE @pattern"")]
        public partial IList<User> FindUsersByEmailPattern(string pattern);
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Verify no compilation errors
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Generated code should compile without errors. Errors:\n{errorMessages}");
        }

        // Verify no variable redeclarations in custom SQL operations
        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        var varRedeclarations = CountStringOccurrences(generatedCode, "var __repoCmd__ = connection.CreateCommand();");
        Assert.AreEqual(0, varRedeclarations, "Should not have any 'var __repoCmd__' redeclarations");
    }

    /// <summary>
    /// Tests that mixed operations (Insert, Update, Delete, Select) in the same repository don't cause variable conflicts.
    /// </summary>
    [TestMethod]
    public void MixedOperations_DoNotCauseVariableConflicts()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public interface IUserService
    {
        // CRUD operations
        int CreateUser(User user);
        User? GetUserById(int id);
        IList<User> GetAllUsers();
        int UpdateUser(User user);
        int DeleteUser(int id);
        
        // Custom operations
        [Sqlx(""SELECT COUNT(*) FROM Users"")]
        int GetTotalUserCount();
    }

    [RepositoryFor(typeof(IUserService))]
    public partial class UserService : IUserService
    {
        private readonly DbConnection connection;
        
        public UserService(DbConnection connection)
        {
            this.connection = connection;
        }
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Verify no compilation errors
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Generated code with mixed operations should compile without errors. Errors:\n{errorMessages}");
        }

        // Verify proper variable handling
        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        
        // Should not have any variable redeclarations
        var varRedeclarations = CountStringOccurrences(generatedCode, "var __repoCmd__ = connection.CreateCommand();");
        Assert.AreEqual(0, varRedeclarations, "Should not have any 'var __repoCmd__' redeclarations");
        
        // Should have proper variable declarations and assignments
        var repoCmdDeclarations = CountStringOccurrences(generatedCode, "System.Data.Common.DbCommand? __repoCmd__ = null;");
        var repoCmdAssignments = CountStringOccurrences(generatedCode, "__repoCmd__ = connection.CreateCommand();");
        
        Assert.IsTrue(repoCmdDeclarations > 0, "Should have __repoCmd__ declarations");
        Assert.IsTrue(repoCmdAssignments > 0, "Should have __repoCmd__ assignments");
    }

    /// <summary>
    /// Tests that void operations don't cause variable declaration issues.
    /// This addresses the specific error where void methods could cause CS0841 errors.
    /// </summary>
    [TestMethod]
    public void VoidOperations_DoNotCauseVariableUsageBeforeDeclaration()
    {
        string sourceCode = @"
using System.Data.Common;
using System.Threading.Tasks;
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
        void ProcessUser(User user);
        Task ProcessUserAsync(User user);
        void ExecuteMaintenanceTask();
    }

    [RepositoryFor(typeof(IUserService))]
    public partial class UserService : IUserService
    {
        private readonly DbConnection connection;
        
        public UserService(DbConnection connection)
        {
            this.connection = connection;
        }
    }
}";

        var (compilation, diagnostics) = CompileWithSourceGenerator(sourceCode);

        // Verify no compilation errors, especially CS0841 (variable used before declaration)
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            var errorMessages = string.Join("\n", errors.Select(e => e.GetMessage()));
            Assert.Fail($"Generated code for void operations should compile without errors. Errors:\n{errorMessages}");
        }

        // Verify no variable redeclarations
        var generatedCode = GetCSharpGeneratedOutput(sourceCode);
        var varRedeclarations = CountStringOccurrences(generatedCode, "var __repoCmd__ = connection.CreateCommand();");
        Assert.AreEqual(0, varRedeclarations, "Should not have any 'var __repoCmd__' redeclarations");
    }

    /// <summary>
    /// Helper method to count occurrences of a string in text.
    /// </summary>
    private static int CountStringOccurrences(string text, string searchString)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(searchString))
            return 0;

        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(searchString, index)) != -1)
        {
            count++;
            index += searchString.Length;
        }
        return count;
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
