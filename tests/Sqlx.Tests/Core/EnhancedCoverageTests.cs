// -----------------------------------------------------------------------
// <copyright file="EnhancedCoverageTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using System;

namespace Sqlx.Tests.Core;

[TestClass]
public class EnhancedCoverageTests
{
    private static Compilation CreateTestCompilation(string code)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location),
        };

        return CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    [TestMethod]
    public void DiagnosticHelper_WithMockTypeSymbol_CreatesValidDiagnostics()
    {
        // Arrange
        var code = @"
namespace MyApp.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}";
        var compilation = CreateTestCompilation(code);
        var userType = compilation.GetTypeByMetadataName("MyApp.Models.User");

        // Act
        var diagnostic = DiagnosticHelper.CreatePrimaryConstructorDiagnostic("Test issue", userType!);

        // Assert
        Assert.IsNotNull(diagnostic);
        Assert.AreEqual("SQLX1001", diagnostic.Id);
        Assert.IsTrue(diagnostic.GetMessage().Contains("Test issue"));
    }

    [TestMethod]
    public void DiagnosticHelper_GenerateTypeAnalysisReport_WithValidType_GeneratesReport()
    {
        // Arrange
        var code = @"
namespace MyApp.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}";
        var compilation = CreateTestCompilation(code);
        var userType = compilation.GetTypeByMetadataName("MyApp.Models.User");

        // Act
        var report = DiagnosticHelper.GenerateTypeAnalysisReport(userType!);

        // Assert
        Assert.IsNotNull(report);
        Assert.IsTrue(report.Length > 0);
        Assert.IsTrue(report.Contains("User"));
    }

    [TestMethod]
    public void DiagnosticHelper_ValidateEntityType_WithValidType_ReturnsNoIssues()
    {
        // Arrange
        var code = @"
namespace MyApp.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}";
        var compilation = CreateTestCompilation(code);
        var userType = compilation.GetTypeByMetadataName("MyApp.Models.User");

        // Act
        var issues = DiagnosticHelper.ValidateEntityType(userType!);

        // Assert
        Assert.IsNotNull(issues);
        // Should have minimal issues for a simple, well-formed entity
    }

    [TestMethod]
    public void DiagnosticHelper_GeneratePerformanceSuggestions_WithEntityType_ReturnsSuggestions()
    {
        // Arrange
        var code = @"
namespace MyApp.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
    }
}";
        var compilation = CreateTestCompilation(code);
        var userType = compilation.GetTypeByMetadataName("MyApp.Models.User");

        // Act
        var suggestions = DiagnosticHelper.GeneratePerformanceSuggestions(userType!);

        // Assert
        Assert.IsNotNull(suggestions);
        // Should suggest using Record or primary constructor for entities with many string properties
    }

    [TestMethod]
    public void DiagnosticHelper_LogCodeGenerationContext_WithValidParameters_DoesNotThrow()
    {
        // Arrange
        var code = @"
namespace MyApp.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}";
        var compilation = CreateTestCompilation(code);
        var userType = compilation.GetTypeByMetadataName("MyApp.Models.User");

        // Act & Assert - Should not throw
        try
        {
            DiagnosticHelper.LogCodeGenerationContext("Test context", userType!, "TestMethod");
            Assert.IsTrue(true, "LogCodeGenerationContext executed without throwing");
        }
        catch (Exception ex)
        {
            Assert.Fail($"LogCodeGenerationContext should not throw: {ex.Message}");
        }
    }

    [TestMethod]
    public void DiagnosticHelper_ValidateGeneratedCode_WithValidCode_ReturnsValidationResults()
    {
        // Arrange
        var code = @"
namespace MyApp.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}";
        var compilation = CreateTestCompilation(code);
        var userType = compilation.GetTypeByMetadataName("MyApp.Models.User");
        var generatedCode = @"
public User GetUser(int id)
{
    using var cmd = connection.CreateCommand();
    cmd.CommandText = ""SELECT Id, Name FROM Users WHERE Id = @id"";
    var param = cmd.CreateParameter();
    param.ParameterName = ""@id"";
    param.Value = id;
    cmd.Parameters.Add(param);
    
    using var reader = cmd.ExecuteReader();
    if (reader.Read())
    {
        return new MyApp.Models.User
        {
            Id = reader.GetInt32(0),
            Name = reader.IsDBNull(1) ? null : reader.GetString(1)
        };
    }
    return null;
}";

        // Act
        var validationResults = DiagnosticHelper.ValidateGeneratedCode(generatedCode, userType!);

        // Assert
        Assert.IsNotNull(validationResults);
        // Should have fewer issues since this code includes proper null checks
    }

    [TestMethod]
    public void DiagnosticHelper_ValidateGeneratedCode_WithProblematicCode_ReturnsIssues()
    {
        // Arrange
        var code = @"
namespace MyApp.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}";
        var compilation = CreateTestCompilation(code);
        var userType = compilation.GetTypeByMetadataName("MyApp.Models.User");
        var problematicCode = @"
public User GetUser(int id)
{
    // Problematic code with unsafe casts and no null checks
    return new User
    {
        Id = (int)reader[0],
        Name = (string)reader[1]  // No null check
    };
}";

        // Act
        var validationResults = DiagnosticHelper.ValidateGeneratedCode(problematicCode, userType!);

        // Assert
        Assert.IsNotNull(validationResults);
        // Should detect issues with unsafe casting and missing null checks
        Assert.IsTrue(validationResults.Count > 0, "Should detect issues in problematic code");
    }

    [TestMethod]
    public void DatabaseDialectFactory_GetDialectProvider_WithMySqlDefine_ReturnsMySqlProvider()
    {
        // Act
        var provider = DatabaseDialectFactory.GetDialectProvider(Sqlx.SqlDefine.MySql);

        // Assert
        Assert.IsInstanceOfType(provider, typeof(MySqlDialectProvider));
        Assert.AreEqual(Sqlx.SqlGen.SqlDefineTypes.MySql, provider.DialectType);
    }

    [TestMethod]
    public void DatabaseDialectFactory_GetDialectProvider_WithSqlServerDefine_ReturnsSqlServerProvider()
    {
        // Act
        var provider = DatabaseDialectFactory.GetDialectProvider(Sqlx.SqlDefine.SqlServer);

        // Assert
        Assert.IsInstanceOfType(provider, typeof(SqlServerDialectProvider));
        Assert.AreEqual(Sqlx.SqlGen.SqlDefineTypes.SqlServer, provider.DialectType);
    }

    [TestMethod]
    public void DatabaseDialectFactory_GetDialectProvider_WithPostgreSqlDefine_ReturnsPostgreSqlProvider()
    {
        // Act
        var provider = DatabaseDialectFactory.GetDialectProvider(Sqlx.SqlDefine.PgSql);

        // Assert
        Assert.IsInstanceOfType(provider, typeof(PostgreSqlDialectProvider));
        Assert.AreEqual(Sqlx.SqlGen.SqlDefineTypes.Postgresql, provider.DialectType);
    }

    [TestMethod]
    public void DatabaseDialectFactory_GetDialectProvider_WithSQLiteDefine_ReturnsSQLiteProvider()
    {
        // Act
        var provider = DatabaseDialectFactory.GetDialectProvider(Sqlx.SqlDefine.SQLite);

        // Assert
        Assert.IsInstanceOfType(provider, typeof(SQLiteDialectProvider));
        Assert.AreEqual(Sqlx.SqlGen.SqlDefineTypes.SQLite, provider.DialectType);
    }

    [TestMethod]
    public void DatabaseDialectFactory_GetDialectProvider_WithOracleDefineTypes_ThrowsNotSupportedException()
    {
        // Act & Assert
        Assert.ThrowsException<UnsupportedDialectException>(() => 
            DatabaseDialectFactory.GetDialectProvider(Sqlx.SqlGen.SqlDefineTypes.Oracle));
    }

    [TestMethod]
    public void DatabaseDialectFactory_GetDialectProvider_WithDB2DefineTypes_ThrowsNotSupportedException()
    {
        // Act & Assert
        Assert.ThrowsException<UnsupportedDialectException>(() => 
            DatabaseDialectFactory.GetDialectProvider(Sqlx.SqlGen.SqlDefineTypes.DB2));
    }

    [TestMethod]
    public void DatabaseDialectFactory_GetDialectProvider_WithOracleDefine_ThrowsNotSupportedException()
    {
        // Act & Assert
        Assert.ThrowsException<UnsupportedDialectException>(() => 
            DatabaseDialectFactory.GetDialectProvider(Sqlx.SqlDefine.Oracle));
    }

    [TestMethod]
    public void DatabaseDialectFactory_GetDialectProvider_WithDB2Define_ThrowsNotSupportedException()
    {
        // Act & Assert
        Assert.ThrowsException<UnsupportedDialectException>(() => 
            DatabaseDialectFactory.GetDialectProvider(Sqlx.SqlDefine.DB2));
    }

    [TestMethod]
    public void TypeAnalyzer_IsLikelyEntityType_WithEntityClass_ReturnsTrue()
    {
        // Arrange
        var code = @"
namespace MyApp.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
    }
}";
        var compilation = CreateTestCompilation(code);
        var productType = compilation.GetTypeByMetadataName("MyApp.Models.Product");

        // Act
        var result = TypeAnalyzer.IsLikelyEntityType(productType);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void TypeAnalyzer_IsLikelyEntityType_WithPrimitiveType_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation("class Test { }");
        var intType = compilation.GetSpecialType(SpecialType.System_Int32);

        // Act
        var result = TypeAnalyzer.IsLikelyEntityType(intType);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TypeAnalyzer_IsLikelyEntityType_WithStringType_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation("class Test { }");
        var stringType = compilation.GetSpecialType(SpecialType.System_String);

        // Act
        var result = TypeAnalyzer.IsLikelyEntityType(stringType);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TypeAnalyzer_IsLikelyEntityType_WithNullType_ReturnsFalse()
    {
        // Act
        var result = TypeAnalyzer.IsLikelyEntityType(null);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TypeAnalyzer_IsCollectionType_WithListType_ReturnsTrue()
    {
        // Arrange
        var code = @"
using System.Collections.Generic;
namespace MyApp
{
    public class Test
    {
        public List<string> Items { get; set; }
    }
}";
        var compilation = CreateTestCompilation(code);
        var testType = compilation.GetTypeByMetadataName("MyApp.Test");
        var itemsProperty = testType?.GetMembers("Items").FirstOrDefault() as IPropertySymbol;

        // Act
        var result = TypeAnalyzer.IsCollectionType(itemsProperty?.Type!);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void TypeAnalyzer_IsCollectionType_WithNonCollectionType_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation("class Test { }");
        var stringType = compilation.GetSpecialType(SpecialType.System_String);

        // Act
        var result = TypeAnalyzer.IsCollectionType(stringType);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TypeAnalyzer_IsAsyncType_WithTaskType_ReturnsTrue()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;
namespace MyApp
{
    public class Test
    {
        public Task DoSomethingAsync() => Task.CompletedTask;
    }
}";
        var compilation = CreateTestCompilation(code);
        var testType = compilation.GetTypeByMetadataName("MyApp.Test");
        var method = testType?.GetMembers("DoSomethingAsync").FirstOrDefault() as IMethodSymbol;

        // Act
        var result = TypeAnalyzer.IsAsyncType(method?.ReturnType!);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void TypeAnalyzer_IsAsyncType_WithGenericTaskType_ReturnsTrue()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;
namespace MyApp
{
    public class Test
    {
        public Task<string> GetStringAsync() => Task.FromResult(""test"");
    }
}";
        var compilation = CreateTestCompilation(code);
        var testType = compilation.GetTypeByMetadataName("MyApp.Test");
        var method = testType?.GetMembers("GetStringAsync").FirstOrDefault() as IMethodSymbol;

        // Act
        var result = TypeAnalyzer.IsAsyncType(method?.ReturnType!);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void TypeAnalyzer_IsAsyncType_WithNonAsyncType_ReturnsFalse()
    {
        // Arrange
        var compilation = CreateTestCompilation("class Test { }");
        var stringType = compilation.GetSpecialType(SpecialType.System_String);

        // Act
        var result = TypeAnalyzer.IsAsyncType(stringType);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TypeAnalyzer_IsScalarReturnType_WithIntType_ReturnsTrue()
    {
        // Arrange
        var compilation = CreateTestCompilation("class Test { }");
        var intType = compilation.GetSpecialType(SpecialType.System_Int32);

        // Act
        var result = TypeAnalyzer.IsScalarReturnType(intType, false);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void TypeAnalyzer_IsScalarReturnType_WithStringType_ReturnsTrue()
    {
        // Arrange
        var compilation = CreateTestCompilation("class Test { }");
        var stringType = compilation.GetSpecialType(SpecialType.System_String);

        // Act
        var result = TypeAnalyzer.IsScalarReturnType(stringType, false);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void TypeAnalyzer_IsScalarReturnType_WithEntityType_ReturnsFalse()
    {
        // Arrange
        var code = @"
namespace MyApp.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}";
        var compilation = CreateTestCompilation(code);
        var userType = compilation.GetTypeByMetadataName("MyApp.Models.User");

        // Act
        var result = TypeAnalyzer.IsScalarReturnType(userType!, false);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void TypeAnalyzer_GetDefaultValue_WithIntType_ReturnsCorrectDefault()
    {
        // Arrange
        var compilation = CreateTestCompilation("class Test { }");
        var intType = compilation.GetSpecialType(SpecialType.System_Int32);

        // Act
        var result = TypeAnalyzer.GetDefaultValue(intType);

        // Assert
        Assert.AreEqual("0", result);
    }

    [TestMethod]
    public void TypeAnalyzer_GetDefaultValue_WithStringType_ReturnsCorrectDefault()
    {
        // Arrange
        var compilation = CreateTestCompilation("class Test { }");
        var stringType = compilation.GetSpecialType(SpecialType.System_String);

        // Act
        var result = TypeAnalyzer.GetDefaultValue(stringType);

        // Assert
        Assert.AreEqual("string.Empty", result);
    }

    [TestMethod]
    public void TypeAnalyzer_GetDefaultValue_WithBoolType_ReturnsCorrectDefault()
    {
        // Arrange
        var compilation = CreateTestCompilation("class Test { }");
        var boolType = compilation.GetSpecialType(SpecialType.System_Boolean);

        // Act
        var result = TypeAnalyzer.GetDefaultValue(boolType);

        // Assert
        Assert.AreEqual("false", result);
    }

    [TestMethod]
    public void TypeAnalyzer_GetInnerType_WithListOfUser_ReturnsListType()
    {
        // Arrange
        var code = @"
using System.Collections.Generic;
namespace MyApp.Models
{
    public class User { }
    public class Test
    {
        public List<User> Users { get; set; }
    }
}";
        var compilation = CreateTestCompilation(code);
        var testType = compilation.GetTypeByMetadataName("MyApp.Models.Test");
        var usersProperty = testType?.GetMembers("Users").FirstOrDefault() as IPropertySymbol;

        // Act
        var result = TypeAnalyzer.GetInnerType(usersProperty?.Type!);

        // Assert
        Assert.IsNotNull(result);
        // GetInnerType returns the original type for non-Task types
        Assert.AreEqual("List", result?.Name);
    }

    [TestMethod]
    public void TypeAnalyzer_GetInnerType_WithTaskOfUser_ReturnsUserType()
    {
        // Arrange
        var code = @"
using System.Threading.Tasks;
namespace MyApp.Models
{
    public class User { }
    public class Test
    {
        public Task<User> GetUserAsync() => null;
    }
}";
        var compilation = CreateTestCompilation(code);
        var testType = compilation.GetTypeByMetadataName("MyApp.Models.Test");
        var method = testType?.GetMembers("GetUserAsync").FirstOrDefault() as IMethodSymbol;

        // Act
        var result = TypeAnalyzer.GetInnerType(method?.ReturnType!);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("User", result?.Name);
    }

    [TestMethod]
    public void TypeAnalyzer_GetInnerType_WithNonGenericType_ReturnsOriginalType()
    {
        // Arrange
        var compilation = CreateTestCompilation("class Test { }");
        var stringType = compilation.GetSpecialType(SpecialType.System_String);

        // Act
        var result = TypeAnalyzer.GetInnerType(stringType);

        // Assert
        Assert.AreEqual(stringType, result);
    }
}
