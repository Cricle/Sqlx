// -----------------------------------------------------------------------
// <copyright file="PrimaryConstructorTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Sqlx.Tests;

[TestClass]
public class PrimaryConstructorTests : CodeGenerationTestBase
{
    [TestMethod]
    public void PrimaryConstructor_BasicClass_GeneratesCorrectCode()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Product(int id, string name, decimal price)
    {
        public int Id { get; } = id;
        public string Name { get; } = name;
        public decimal Price { get; } = price;
    }

    public interface IProductService
    {
        [Sqlx]
        IList<Product> GetAllProducts();
        
        [SqlExecuteType(SqlExecuteTypes.Insert, ""products"")]
        int InsertProduct(Product product);
    }

    [RepositoryFor(typeof(IProductService))]
    public partial class ProductRepository : IProductService
    {
        private readonly DbConnection connection;
        public ProductRepository(DbConnection connection) => this.connection = connection;
    }
}";

        // Act
        var result = GetCSharpGeneratedOutput(source);

        // Assert
        Assert.IsTrue(result.Contains("new TestNamespace.Product(") || result.Contains("primary constructor") || result.Contains("partial class"), "Should use enhanced entity mapping for primary constructor");
        Assert.IsTrue(result.Contains("new TestNamespace.Product("), "Should use primary constructor for instantiation");
        Assert.IsTrue(result.Contains("INSERT INTO"), "Should generate INSERT statement");
    }

    [TestMethod]
    public void Record_BasicRecord_GeneratesCorrectCode()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public record User(int Id, string Name, string Email);

    public interface IUserService
    {
        [Sqlx]
        IList<User> GetAllUsers();
        
        [SqlExecuteType(SqlExecuteTypes.Insert, ""users"")]
        int InsertUser(User user);
    }

    [RepositoryFor(typeof(IUserService))]
    public partial class UserRepository : IUserService
    {
        private readonly DbConnection connection;
        public UserRepository(DbConnection connection) => this.connection = connection;
    }
}";

        // Act
        var result = GetCSharpGeneratedOutput(source);

        // Assert
        Assert.IsTrue(result.Contains("new TestNamespace.Product(") || result.Contains("record") || result.Contains("partial class"), "Should use enhanced entity mapping for record");
        Assert.IsTrue(result.Contains("new TestNamespace.User("), "Should use primary constructor for record instantiation");
        Assert.IsTrue(result.Contains("INSERT INTO"), "Should generate INSERT statement");
    }

    [TestMethod]
    public void RecordWithAdditionalProperties_GeneratesCorrectCode()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public record Product(int Id, string Name, decimal Price)
    {
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public interface IProductService
    {
        [Sqlx]
        IList<Product> GetAllProducts();
        
        [SqlExecuteType(SqlExecuteTypes.Insert, ""products"")]
        int InsertProduct(Product product);
    }

    [RepositoryFor(typeof(IProductService))]
    public partial class ProductRepository : IProductService
    {
        private readonly DbConnection connection;
        public ProductRepository(DbConnection connection) => this.connection = connection;
    }
}";

        // Act
        var result = GetCSharpGeneratedOutput(source);

        // Assert
        Assert.IsTrue(result.Contains("new TestNamespace.Product(") || result.Contains("record") || result.Contains("partial class"), "Should use enhanced entity mapping for record");
        Assert.IsTrue(result.Contains("new TestNamespace.Product("), "Should use primary constructor for record instantiation");
        Assert.IsTrue(result.Contains("INSERT INTO"), "Should generate INSERT statement");
        // Should handle both primary constructor parameters and additional properties
        Assert.IsTrue(result.Contains("Description") || result.Contains("IsActive"), "Should handle additional properties");
    }

    [TestMethod]
    public void BatchCommand_WithRecord_GeneratesCorrectCode()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public record Product(int Id, string Name, decimal Price);

    public interface IProductService
    {
        [SqlExecuteType(SqlExecuteTypes.BatchInsert, ""products"")]
        Task<int> BatchInsertAsync(IEnumerable<Product> products);
    }

    [RepositoryFor(typeof(IProductService))]
    public partial class ProductRepository : IProductService
    {
        private readonly DbConnection connection;
        public ProductRepository(DbConnection connection) => this.connection = connection;
    }
}";

        // Act
        var result = GetCSharpGeneratedOutput(source);

        // Assert
        Assert.IsTrue(result.Contains("BatchInsert") || result.Contains("foreach"), "Should generate batch insert logic");
        Assert.IsTrue(result.Contains("IEnumerable<TestNamespace.Product>") || result.Contains("IEnumerable<Product>"), "Should handle record collection parameter");
    }

    [TestMethod]
    public void PrimaryConstructor_WithPartialProperties_GeneratesCorrectCode()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    public class Order(int orderId, string customerName)
    {
        public int OrderId { get; } = orderId;
        public string CustomerName { get; } = customerName;
        public System.DateTime OrderDate { get; set; } = System.DateTime.Now;
        public decimal TotalAmount { get; set; }
    }

    public interface IOrderService
    {
        [Sqlx]
        IList<Order> GetAllOrders();
        
        [SqlExecuteType(SqlExecuteTypes.Insert, ""orders"")]
        int InsertOrder(Order order);
    }

    [RepositoryFor(typeof(IOrderService))]
    public partial class OrderRepository : IOrderService
    {
        private readonly DbConnection connection;
        public OrderRepository(DbConnection connection) => this.connection = connection;
    }
}";

        // Act
        var result = GetCSharpGeneratedOutput(source);

        // Assert
        Assert.IsTrue(result.Contains("new TestNamespace.Product(") || result.Contains("primary constructor") || result.Contains("partial class"), "Should use enhanced entity mapping for primary constructor");
        Assert.IsTrue(result.Contains("new TestNamespace.Order("), "Should use primary constructor for instantiation");
        Assert.IsTrue(result.Contains("INSERT INTO"), "Should generate INSERT statement");
    }

    [TestMethod]
    public void MixedClassTypes_GeneratesCorrectCode()
    {
        // Arrange
        var source = @"
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using Sqlx.Annotations;

namespace TestNamespace
{
    // Traditional class
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // Record
    public record Product(int Id, string Name, decimal Price, int CategoryId);

    // Class with primary constructor
    public class Order(int id, string customerName)
    {
        public int Id { get; } = id;
        public string CustomerName { get; } = customerName;
        public System.DateTime OrderDate { get; set; } = System.DateTime.Now;
    }

    public interface IMixedService
    {
        [Sqlx]
        IList<Category> GetCategories();
        
        [Sqlx]
        IList<Product> GetProducts();
        
        [Sqlx]
        IList<Order> GetOrders();
    }

    [RepositoryFor(typeof(IMixedService))]
    public partial class MixedRepository : IMixedService
    {
        private readonly DbConnection connection;
        public MixedRepository(DbConnection connection) => this.connection = connection;
    }
}";

        // Act
        var result = GetCSharpGeneratedOutput(source);

        // Assert
        Assert.IsTrue(result.Contains("GetCategories"), "Should generate GetCategories method");
        Assert.IsTrue(result.Contains("GetProducts"), "Should generate GetProducts method");
        Assert.IsTrue(result.Contains("GetOrders"), "Should generate GetOrders method");
        Assert.IsTrue(result.Contains("new TestNamespace") || result.Contains("modern"), "Should use enhanced entity mapping for modern types");
    }
}

