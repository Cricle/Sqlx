// TDD: Test for RepositoryFor with generic interface parameters
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Annotations;
using System.Collections.Generic;

namespace Sqlx.Tests.RepositoryFor
{
    #region Test Entities

    [TableName("users")]
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    [TableName("products")]
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
    }

    #endregion

    #region Custom Generic Interfaces

    /// <summary>Custom generic read-only repository</summary>
    public interface IReadRepository<T> where T : class
    {
        [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
        Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

        [SqlTemplate("SELECT {{columns}} FROM {{table}} {{limit --param limit}}")]
        Task<List<T>> GetAllAsync(int limit = 100, CancellationToken cancellationToken = default);
    }

    #endregion

    #region Test Repositories

    // Test 1: Non-generic syntax with ICrudRepository<T, TKey>
    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(ICrudRepository<User, int>))]
    public partial class UserCrudRepository(DbConnection connection) 
        : ICrudRepository<User, int>
    {
    }

    // Test 2: Generic syntax with ICrudRepository<T, TKey>
    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor<ICrudRepository<Product, int>>]
    public partial class ProductCrudRepository(DbConnection connection) 
        : ICrudRepository<Product, int>
    {
    }

    // Test 3: Custom generic interface with non-generic syntax
    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor(typeof(IReadRepository<User>))]
    public partial class UserReadRepository(DbConnection connection) 
        : IReadRepository<User>
    {
    }

    // Test 4: Custom generic interface with generic syntax
    [SqlDefine(SqlDefineTypes.SQLite)]
    [RepositoryFor<IReadRepository<Product>>]
    public partial class ProductReadRepository(DbConnection connection) 
        : IReadRepository<Product>
    {
    }

    #endregion

    [TestClass]
    public class GenericInterfaceParameterTest
    {
        [TestMethod]
        public void RepositoryFor_NonGenericSyntax_WithICrudRepository_ShouldCompile()
        {
            // Arrange & Act - compilation is the test
            // If this compiles, the source generator worked correctly
            
            // Assert
            Assert.IsTrue(true, "UserCrudRepository compiled successfully with non-generic RepositoryFor syntax");
        }

        [TestMethod]
        public void RepositoryFor_GenericSyntax_WithICrudRepository_ShouldCompile()
        {
            // Arrange & Act - compilation is the test
            
            // Assert
            Assert.IsTrue(true, "ProductCrudRepository compiled successfully with generic RepositoryFor syntax");
        }

        [TestMethod]
        public void RepositoryFor_NonGenericSyntax_WithCustomInterface_ShouldCompile()
        {
            // Arrange & Act - compilation is the test
            
            // Assert
            Assert.IsTrue(true, "UserReadRepository compiled successfully with custom generic interface");
        }

        [TestMethod]
        public void RepositoryFor_GenericSyntax_WithCustomInterface_ShouldCompile()
        {
            // Arrange & Act - compilation is the test
            
            // Assert
            Assert.IsTrue(true, "ProductReadRepository compiled successfully with custom generic interface");
        }

        [TestMethod]
        public async Task RepositoryFor_ICrudRepository_GeneratedMethods_ShouldWorkCorrectly()
        {
            // Arrange
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            
            // Create table
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT, email TEXT)";
            await cmd.ExecuteNonQueryAsync();

            var repository = new UserCrudRepository(connection);

            // Act & Assert - verify generated methods exist and can be called
            // We're not testing the actual functionality here, just that the methods were generated
            var count = await repository.CountAsync();
            Assert.AreEqual(0, count, "Count should be 0 for empty table");
        }

        [TestMethod]
        public async Task RepositoryFor_CustomGenericInterface_GeneratedMethods_ShouldWorkCorrectly()
        {
            // Arrange
            using var connection = new Microsoft.Data.Sqlite.SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            
            // Create table
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT, email TEXT)";
            await cmd.ExecuteNonQueryAsync();

            var repository = new UserReadRepository(connection);

            // Act & Assert - verify generated methods exist
            var users = await repository.GetAllAsync(10);
            Assert.IsNotNull(users, "GetAllAsync should return a list");
            Assert.AreEqual(0, users.Count, "List should be empty for empty table");
        }
    }
}

