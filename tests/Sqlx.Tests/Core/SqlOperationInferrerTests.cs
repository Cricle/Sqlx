using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;
using System.Linq;

namespace Sqlx.Tests.Core;

[TestClass]
public class SqlOperationInferrerTests
{
    private Compilation _compilation = null!;

    [TestInitialize]
    public void Setup()
    {
        var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TestNamespace 
{
    public interface IUserService
    {
        Task<User> GetByIdAsync(int id);
        Task<List<User>> GetAllAsync();
        Task<int> CreateAsync(User user);
        Task<bool> UpdateAsync(User user);
        Task<bool> DeleteAsync(int id);
        Task<User> FindByNameAsync(string name);
        Task<List<User>> SearchByEmailAsync(string email);
        Task<int> InsertAsync(User user);
        Task<bool> RemoveAsync(int id);
        Task<User> SelectByIdAsync(int id);
        Task<List<User>> ListAllAsync();
        Task<bool> SaveAsync(User user);
        Task<bool> AddAsync(User user);
        Task<bool> EditAsync(User user);
        Task<bool> DestroyAsync(int id);
        Task<User> LoadByIdAsync(int id);
    }
    
    public class User 
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        _compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            new[] { 
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Threading.Tasks.Task).Assembly.Location)
            });
    }

    [TestMethod]
    public void InferOperation_WithGetMethod_ReturnsSelect()
    {
        // Arrange
        var method = GetMethod("GetByIdAsync");

        // Act
        var result = SqlOperationInferrer.InferOperation(method);

        // Assert
        Assert.AreEqual(SqlOperationType.Select, result);
    }

    [TestMethod]
    public void InferOperation_WithCreateMethod_ReturnsInsert()
    {
        // Arrange
        var method = GetMethod("CreateAsync");

        // Act
        var result = SqlOperationInferrer.InferOperation(method);

        // Assert
        Assert.AreEqual(SqlOperationType.Insert, result);
    }

    [TestMethod]
    public void InferOperation_WithUpdateMethod_ReturnsUpdate()
    {
        // Arrange
        var method = GetMethod("UpdateAsync");

        // Act
        var result = SqlOperationInferrer.InferOperation(method);

        // Assert
        Assert.AreEqual(SqlOperationType.Update, result);
    }

    [TestMethod]
    public void InferOperation_WithDeleteMethod_ReturnsDelete()
    {
        // Arrange
        var method = GetMethod("DeleteAsync");

        // Act
        var result = SqlOperationInferrer.InferOperation(method);

        // Assert
        Assert.AreEqual(SqlOperationType.Delete, result);
    }

    [TestMethod]
    public void InferOperation_WithFindMethod_ReturnsSelect()
    {
        // Arrange
        var method = GetMethod("FindByNameAsync");

        // Act
        var result = SqlOperationInferrer.InferOperation(method);

        // Assert
        Assert.AreEqual(SqlOperationType.Select, result);
    }

    [TestMethod]
    public void InferOperation_WithSearchMethod_ReturnsSelect()
    {
        // Arrange
        var method = GetMethod("SearchByEmailAsync");

        // Act
        var result = SqlOperationInferrer.InferOperation(method);

        // Assert
        Assert.AreEqual(SqlOperationType.Select, result);
    }

    [TestMethod]
    public void InferOperation_WithInsertMethod_ReturnsInsert()
    {
        // Arrange
        var method = GetMethod("InsertAsync");

        // Act
        var result = SqlOperationInferrer.InferOperation(method);

        // Assert
        Assert.AreEqual(SqlOperationType.Insert, result);
    }

    [TestMethod]
    public void InferOperation_WithRemoveMethod_ReturnsDelete()
    {
        // Arrange
        var method = GetMethod("RemoveAsync");

        // Act
        var result = SqlOperationInferrer.InferOperation(method);

        // Assert
        Assert.AreEqual(SqlOperationType.Delete, result);
    }

    [TestMethod]
    public void InferOperation_WithSelectMethod_ReturnsSelect()
    {
        // Arrange
        var method = GetMethod("SelectByIdAsync");

        // Act
        var result = SqlOperationInferrer.InferOperation(method);

        // Assert
        Assert.AreEqual(SqlOperationType.Select, result);
    }

    [TestMethod]
    public void InferOperation_WithListMethod_ReturnsSelect()
    {
        // Arrange
        var method = GetMethod("ListAllAsync");

        // Act
        var result = SqlOperationInferrer.InferOperation(method);

        // Assert
        Assert.AreEqual(SqlOperationType.Select, result);
    }

    [TestMethod]
    public void InferOperation_WithSaveMethod_ReturnsInsert()
    {
        // Arrange
        var method = GetMethod("SaveAsync");

        // Act
        var result = SqlOperationInferrer.InferOperation(method);

        // Assert
        Assert.AreEqual(SqlOperationType.Insert, result); // Save is typically used for creating new entities
    }

    [TestMethod]
    public void InferOperation_WithAddMethod_ReturnsInsert()
    {
        // Arrange
        var method = GetMethod("AddAsync");

        // Act
        var result = SqlOperationInferrer.InferOperation(method);

        // Assert
        Assert.AreEqual(SqlOperationType.Insert, result);
    }

    [TestMethod]
    public void InferOperation_WithEditMethod_ReturnsUpdate()
    {
        // Arrange
        var method = GetMethod("EditAsync");

        // Act
        var result = SqlOperationInferrer.InferOperation(method);

        // Assert
        Assert.AreEqual(SqlOperationType.Update, result);
    }

    [TestMethod]
    public void InferOperation_WithDestroyMethod_ReturnsDelete()
    {
        // Arrange
        var method = GetMethod("DestroyAsync");

        // Act
        var result = SqlOperationInferrer.InferOperation(method);

        // Assert
        Assert.AreEqual(SqlOperationType.Delete, result);
    }

    [TestMethod]
    public void InferOperation_WithLoadMethod_ReturnsSelect()
    {
        // Arrange
        var method = GetMethod("LoadByIdAsync");

        // Act
        var result = SqlOperationInferrer.InferOperation(method);

        // Assert
        Assert.AreEqual(SqlOperationType.Select, result);
    }

    [TestMethod]
    public void GenerateSqlTemplate_WithSelectOperation_ReturnsSelectSql()
    {
        // Arrange
        var userType = GetTypeSymbol("TestNamespace.User");

        // Act
        var result = SqlOperationInferrer.GenerateSqlTemplate(SqlOperationType.Select, "users", userType);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("SELECT"));
        Assert.IsTrue(result.Contains("users"));
    }

    [TestMethod]
    public void GenerateSqlTemplate_WithInsertOperation_ReturnsInsertSql()
    {
        // Arrange
        var userType = GetTypeSymbol("TestNamespace.User");

        // Act
        var result = SqlOperationInferrer.GenerateSqlTemplate(SqlOperationType.Insert, "users", userType);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("INSERT"));
        Assert.IsTrue(result.Contains("users"));
    }

    [TestMethod]
    public void GenerateSqlTemplate_WithUpdateOperation_ReturnsUpdateSql()
    {
        // Arrange
        var userType = GetTypeSymbol("TestNamespace.User");

        // Act
        var result = SqlOperationInferrer.GenerateSqlTemplate(SqlOperationType.Update, "users", userType);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("UPDATE"));
        Assert.IsTrue(result.Contains("users"));
    }

    [TestMethod]
    public void GenerateSqlTemplate_WithDeleteOperation_ReturnsDeleteSql()
    {
        // Arrange
        var userType = GetTypeSymbol("TestNamespace.User");

        // Act
        var result = SqlOperationInferrer.GenerateSqlTemplate(SqlOperationType.Delete, "users", userType);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("DELETE"));
        Assert.IsTrue(result.Contains("users"));
    }

    [TestMethod]
    public void GenerateSqlTemplate_WithNullEntityType_ReturnsBasicSql()
    {
        // Act
        var result = SqlOperationInferrer.GenerateSqlTemplate(SqlOperationType.Select, "users", null);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("SELECT"));
        Assert.IsTrue(result.Contains("users"));
    }

    [TestMethod]
    public void GenerateSqlTemplate_WithEmptyTableName_HandlesGracefully()
    {
        // Arrange
        var userType = GetTypeSymbol("TestNamespace.User");

        // Act
        var result = SqlOperationInferrer.GenerateSqlTemplate(SqlOperationType.Select, "", userType);

        // Assert
        Assert.IsNotNull(result);
        // Should handle empty table name gracefully
    }

    [TestMethod]
    public void InferOperation_WithNullMethod_ReturnsDefaultOperation()
    {
        // Act
        var result = SqlOperationInferrer.InferOperation(null!);

        // Assert
        Assert.AreEqual(SqlOperationType.Select, result); // Should default to Select
    }

    [TestMethod]
    public void InferOperation_CaseInsensitive_WorksCorrectly()
    {
        // Test that the inference works with different casing
        var sourceWithCasing = @"
namespace TestNamespace 
{
    public interface ITestService
    {
        Task getByIdAsync(int id);
        Task INSERT_async(User user);
        Task UPDATE_Async(User user);
        Task DELETE_async(int id);
    }
    public class User { }
}";

        var syntaxTree = CSharpSyntaxTree.ParseText(sourceWithCasing);
        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });

        var interfaceType = compilation.GetTypeByMetadataName("TestNamespace.ITestService") as INamedTypeSymbol;
        var methods = interfaceType?.GetMembers().OfType<IMethodSymbol>().ToArray();

        if (methods != null)
        {
            foreach (var method in methods)
            {
                var operation = SqlOperationInferrer.InferOperation(method);
                Assert.IsTrue(operation != SqlOperationType.Select || method.Name.ToLowerInvariant().Contains("get"));
            }
        }
    }

    #region Helper Methods

    private IMethodSymbol GetMethod(string methodName)
    {
        var interfaceType = _compilation.GetTypeByMetadataName("TestNamespace.IUserService") as INamedTypeSymbol;
        Assert.IsNotNull(interfaceType, "Could not find IUserService interface");

        var method = interfaceType.GetMembers(methodName).OfType<IMethodSymbol>().FirstOrDefault();
        Assert.IsNotNull(method, $"Could not find method: {methodName}");

        return method;
    }

    private INamedTypeSymbol GetTypeSymbol(string typeName)
    {
        var type = _compilation.GetTypeByMetadataName(typeName);
        Assert.IsNotNull(type, $"Could not find type: {typeName}");
        return type;
    }

    #endregion
}

