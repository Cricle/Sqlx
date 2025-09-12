// -----------------------------------------------------------------------
// <copyright file="SourceGeneratorIntegrationTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Sqlx;
using System.Linq;
using System.Text;

namespace Sqlx.Tests.Integration
{
    /// <summary>
    /// Integration tests for the complete source generation pipeline.
    /// </summary>
    [TestClass]
    public class SourceGeneratorIntegrationTests
    {
        [TestMethod]
        public void SourceGenerator_WithCompleteWorkflow_GeneratesExpectedCode()
        {
            // Arrange
            var sourceCode = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestProject
{
    public class UserService
    {
        [Sqlx(""GetUsers"")]
        public List<User> GetUsers() => null!;

        [RawSql(""SELECT * FROM Users WHERE Id = @id"")]
        public User? GetUserById(int id) => null;

        [SqlExecuteType(SqlExecuteTypes.Insert, ""Users"")]
        public void InsertUser(User user) { }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public record UserRecord(int Id, string Name, string Email);

    public class UserWithPrimaryConstructor(int id, string name)
    {
        public int Id { get; } = id;
        public string Name { get; } = name;
    }
}";

            // Act
            var result = RunSourceGenerator(sourceCode);

            // Assert
            Assert.IsTrue(result.Diagnostics.Length == 0 || 
                         result.Diagnostics.All(d => d.Severity != DiagnosticSeverity.Error),
                         $"No compilation errors expected. Found: {string.Join(", ", result.Diagnostics.Select(d => d.GetMessage()))}");
            
            Assert.IsTrue(result.GeneratedSources.Length > 0, "Should generate at least one source file");
            
            // Check that attributes were generated
            var attributeSource = result.GeneratedSources.FirstOrDefault(s => s.HintName.Contains("SqlxAttributes"));
            Assert.IsNotNull(attributeSource, "Should generate SqlxAttributes source");
            
            var attributeText = attributeSource.SourceText.ToString();
            Assert.IsTrue(attributeText.Contains("SqlxAttribute"), "Should contain SqlxAttribute");
            Assert.IsTrue(attributeText.Contains("RawSqlAttribute"), "Should contain RawSqlAttribute");
            Assert.IsTrue(attributeText.Contains("SqlExecuteTypeAttribute"), "Should contain SqlExecuteTypeAttribute");
        }

        [TestMethod]
        public void SourceGenerator_WithRepositoryPattern_GeneratesRepositoryCode()
        {
            // Arrange
            var sourceCode = @"
using System;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestProject
{
    public interface IUserService
    {
        List<User> GetUsers();
        User GetUserById(int id);
    }

    [RepositoryFor(typeof(IUserService))]
    [TableName(""Users"")]
    public class UserRepository
    {
        [Sqlx(""sp_GetAllUsers"")]
        public List<User> GetUsers() => null!;

        [Sqlx(""sp_GetUserById"")]
        public User GetUserById(int id) => null!;
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}";

            // Act
            var result = RunSourceGenerator(sourceCode);

            // Assert
            Assert.IsTrue(result.Diagnostics.All(d => d.Severity != DiagnosticSeverity.Error),
                         $"No compilation errors expected. Found: {string.Join(", ", result.Diagnostics.Select(d => d.GetMessage()))}");
            
            Assert.IsTrue(result.GeneratedSources.Length > 0, "Should generate source files");
        }

        [TestMethod]
        public void SourceGenerator_WithRecordTypes_HandlesCorrectly()
        {
            // Arrange
            var sourceCode = @"
using System;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestProject
{
    public record User(int Id, string Name, string Email);
    
    public record UserProfile(int UserId, string Bio, DateTime UpdatedAt);

    public class UserService
    {
        [Sqlx(""GetUsers"")]
        public List<User> GetUsers() => null!;

        [Sqlx(""GetUserProfile"")]
        public UserProfile? GetUserProfile(int userId) => null;
    }
}";

            // Act
            var result = RunSourceGenerator(sourceCode);

            // Assert
            Assert.IsTrue(result.Diagnostics.All(d => d.Severity != DiagnosticSeverity.Error),
                         $"No compilation errors expected. Found: {string.Join(", ", result.Diagnostics.Select(d => d.GetMessage()))}");
            
            Assert.IsTrue(result.GeneratedSources.Length > 0, "Should generate source files for records");
        }

        [TestMethod]
        public void SourceGenerator_WithPrimaryConstructors_HandlesCorrectly()
        {
            // Arrange
            var sourceCode = @"
using System;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestProject
{
    public class User(int id, string name, string email)
    {
        public int Id { get; } = id;
        public string Name { get; } = name;
        public string Email { get; } = email;
        public DateTime CreatedAt { get; set; }
    }

    public class UserService
    {
        [Sqlx(""GetUsers"")]
        public List<User> GetUsers() => null!;
    }
}";

            // Act
            var result = RunSourceGenerator(sourceCode);

            // Assert
            Assert.IsTrue(result.Diagnostics.All(d => d.Severity != DiagnosticSeverity.Error),
                         $"No compilation errors expected. Found: {string.Join(", ", result.Diagnostics.Select(d => d.GetMessage()))}");
            
            Assert.IsTrue(result.GeneratedSources.Length > 0, "Should generate source files for primary constructors");
        }

        [TestMethod]
        public void SourceGenerator_WithMultipleDialects_GeneratesCorrectly()
        {
            // Arrange
            var sourceCode = @"
using System;
using System.Collections.Generic;
using Sqlx.Annotations;

namespace TestProject
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class MySqlUserService
    {
        [SqlDefine(SqlDefineTypes.MySql)]
        [Sqlx(""GetUsers"")]
        public List<User> GetUsers() => null!;
    }

    public class SqlServerUserService
    {
        [SqlDefine(SqlDefineTypes.SqlServer)]
        [Sqlx(""GetUsers"")]
        public List<User> GetUsers() => null!;
    }

    public class PostgreSqlUserService
    {
        [SqlDefine(SqlDefineTypes.Postgresql)]
        [Sqlx(""GetUsers"")]
        public List<User> GetUsers() => null!;
    }
}";

            // Act
            var result = RunSourceGenerator(sourceCode);

            // Assert
            Assert.IsTrue(result.Diagnostics.All(d => d.Severity != DiagnosticSeverity.Error),
                         $"No compilation errors expected. Found: {string.Join(", ", result.Diagnostics.Select(d => d.GetMessage()))}");
            
            Assert.IsTrue(result.GeneratedSources.Length > 0, "Should generate source files for multiple dialects");
        }

        [TestMethod]
        public void SourceGenerator_WithExpressionToSql_GeneratesCorrectly()
        {
            // Arrange
            var sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Sqlx.Annotations;

namespace TestProject
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class UserService
    {
        [Sqlx(""GetUsers"")]
        public List<User> GetUsers([ExpressionToSql] Expression<Func<User, bool>> predicate) => null!;

        [RawSql(""SELECT * FROM Users"")]
        public List<User> GetUsersWithExpression([ExpressionToSql] Expression<Func<User, bool>> where) => null!;
    }
}";

            // Act
            var result = RunSourceGenerator(sourceCode);

            // Assert
            Assert.IsTrue(result.Diagnostics.All(d => d.Severity != DiagnosticSeverity.Error),
                         $"No compilation errors expected. Found: {string.Join(", ", result.Diagnostics.Select(d => d.GetMessage()))}");
            
            Assert.IsTrue(result.GeneratedSources.Length > 0, "Should generate source files for ExpressionToSql");
        }

        [TestMethod]
        public void SourceGenerator_WithComplexScenario_HandlesAllFeatures()
        {
            // Arrange - This test combines multiple features
            var sourceCode = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq.Expressions;
using Sqlx.Annotations;

namespace TestProject
{
    // Record type
    public record UserSummary(int Id, string Name, int PostCount);

    // Primary constructor class
    public class User(int id, string name)
    {
        public int Id { get; } = id;
        public string Name { get; } = name;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    // Traditional class
    public class Post
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    // Service interface
    public interface IUserService
    {
        Task<List<User>> GetActiveUsersAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task<int> CreateUserAsync(User user);
    }

    // Repository with multiple features
    [RepositoryFor(typeof(IUserService))]
    [TableName(""Users"")]
    [SqlDefine(SqlDefineTypes.SqlServer)]
    public class UserRepository
    {
        [Sqlx(""sp_GetActiveUsers"")]
        public async Task<List<User>> GetActiveUsersAsync() => null!;

        [RawSql(""SELECT * FROM Users WHERE Id = @id AND IsActive = 1"")]
        public async Task<User?> GetUserByIdAsync(int id) => null;

        [SqlExecuteType(SqlExecuteTypes.Insert, ""Users"")]
        public async Task<int> CreateUserAsync(User user) => 0;

        [Sqlx(""sp_GetUserSummaries"")]
        public List<UserSummary> GetUserSummaries() => null!;

        [RawSql(""SELECT * FROM Posts WHERE UserId = @userId"")]
        public List<Post> GetUserPosts(int userId) => null!;

        [Sqlx(""sp_SearchUsers"")]
        public List<User> SearchUsers([ExpressionToSql] Expression<Func<User, bool>> criteria) => null!;

        [SqlExecuteType(SqlExecuteTypes.BatchInsert, ""Posts"")]
        public void InsertPosts(List<Post> posts) { }

        [SqlExecuteType(SqlExecuteTypes.Update, ""Users"")]
        public void UpdateUser(User user) { }

        [SqlExecuteType(SqlExecuteTypes.Delete, ""Users"")]
        public void DeleteUser(int id) { }
    }
}";

            // Act
            var result = RunSourceGenerator(sourceCode);

            // Assert
            Assert.IsTrue(result.Diagnostics.All(d => d.Severity != DiagnosticSeverity.Error),
                         $"No compilation errors expected. Found: {string.Join(", ", result.Diagnostics.Select(d => d.GetMessage()))}");
            
            Assert.IsTrue(result.GeneratedSources.Length > 0, "Should generate source files for complex scenario");
            
            // Verify that the attributes source was generated
            var attributeSource = result.GeneratedSources.FirstOrDefault(s => s.HintName.Contains("SqlxAttributes"));
            Assert.IsNotNull(attributeSource, "Should generate SqlxAttributes source");
            
            var attributeText = attributeSource.SourceText.ToString();
            Assert.IsTrue(attributeText.Contains("ExpressionToSql<T>"), "Should contain ExpressionToSql class");
            Assert.IsTrue(attributeText.Contains("SqlExecuteTypes"), "Should contain SqlExecuteTypes enum");
            Assert.IsTrue(attributeText.Contains("SqlDefineTypes"), "Should contain SqlDefineTypes enum");
        }

        private (Diagnostic[] Diagnostics, GeneratedSourceResult[] GeneratedSources) RunSourceGenerator(string sourceCode)
        {
            // Create compilation
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                new[]
                {
                    MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
                    MetadataReference.CreateFromFile(typeof(System.Linq.Expressions.Expression).Assembly.Location)
                },
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            // Create and run generator
            var generator = new CSharpGenerator();
            var driver = CSharpGeneratorDriver.Create(generator);
            driver = (CSharpGeneratorDriver)driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

            // Get results
            var runResult = driver.GetRunResult();
            var generatorResult = runResult.Results.FirstOrDefault();

            return (
                diagnostics.ToArray(),
                generatorResult.GeneratedSources.ToArray()
            );
        }
    }
}
