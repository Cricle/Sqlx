using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.DataTypes;

/// <summary>
/// TDD: 常用数据类型映射测试
/// 验证string, int, long, bool, DateTime, decimal等类型的正确映射
/// </summary>
[TestClass]
public class TDD_DataTypeMapping
{
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("DataTypes")]
    [TestCategory("Core")]
    public void DataType_Integer_ShouldMapCorrectly()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                stock INTEGER NOT NULL,
                price INTEGER NOT NULL
            )");
        
        connection.Execute("INSERT INTO products (stock, price) VALUES (100, 999)");
        
        var repo = new DataTypeTestRepository(connection);
        
        // Act
        var product = repo.GetProductByIdAsync(1).Result;
        
        // Assert
        Assert.IsNotNull(product);
        Assert.AreEqual(100, product.Stock);
        Assert.AreEqual(999, product.Price);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("DataTypes")]
    [TestCategory("Core")]
    public void DataType_Long_ShouldMapCorrectly()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE statistics (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                total_visits INTEGER NOT NULL,
                total_revenue INTEGER NOT NULL
            )");
        
        long largeNumber = 9999999999L; // 10 billion
        connection.Execute(
            "INSERT INTO statistics (total_visits, total_revenue) VALUES (@v, @r)",
            new { v = largeNumber, r = largeNumber * 2 });
        
        var repo = new DataTypeTestRepository(connection);
        
        // Act
        var stats = repo.GetStatisticsAsync(1).Result;
        
        // Assert
        Assert.IsNotNull(stats);
        Assert.AreEqual(largeNumber, stats.TotalVisits);
        Assert.AreEqual(largeNumber * 2, stats.TotalRevenue);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("DataTypes")]
    [TestCategory("Core")]
    public void DataType_Boolean_ShouldMapCorrectly()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE users (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                is_active INTEGER NOT NULL,
                is_admin INTEGER NOT NULL
            )");
        
        connection.Execute("INSERT INTO users (is_active, is_admin) VALUES (1, 0)");
        connection.Execute("INSERT INTO users (is_active, is_admin) VALUES (0, 1)");
        
        var repo = new DataTypeTestRepository(connection);
        
        // Act
        var user1 = repo.GetUserByIdAsync(1).Result;
        var user2 = repo.GetUserByIdAsync(2).Result;
        
        // Assert
        Assert.IsNotNull(user1);
        Assert.IsTrue(user1.IsActive);
        Assert.IsFalse(user1.IsAdmin);
        
        Assert.IsNotNull(user2);
        Assert.IsFalse(user2.IsActive);
        Assert.IsTrue(user2.IsAdmin);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("DataTypes")]
    [TestCategory("Core")]
    public void DataType_Decimal_ShouldMapCorrectly()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                amount REAL NOT NULL,
                tax REAL NOT NULL
            )");
        
        connection.Execute("INSERT INTO orders (amount, tax) VALUES (123.45, 12.35)");
        
        var repo = new DataTypeTestRepository(connection);
        
        // Act
        var order = repo.GetOrderByIdAsync(1).Result;
        
        // Assert
        Assert.IsNotNull(order);
        Assert.AreEqual(123.45m, order.Amount);
        Assert.AreEqual(12.35m, order.Tax);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("DataTypes")]
    [TestCategory("Nullable")]
    public void DataType_NullableInt_ShouldMapCorrectly()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                stock INTEGER,
                discount INTEGER
            )");
        
        connection.Execute("INSERT INTO products (stock, discount) VALUES (NULL, 10)");
        connection.Execute("INSERT INTO products (stock, discount) VALUES (50, NULL)");
        
        var repo = new DataTypeTestRepository(connection);
        
        // Act
        var product1 = repo.GetNullableProductAsync(1).Result;
        var product2 = repo.GetNullableProductAsync(2).Result;
        
        // Assert
        Assert.IsNotNull(product1);
        Assert.IsNull(product1.Stock);
        Assert.AreEqual(10, product1.Discount);
        
        Assert.IsNotNull(product2);
        Assert.AreEqual(50, product2.Stock);
        Assert.IsNull(product2.Discount);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("DataTypes")]
    [TestCategory("String")]
    public void DataType_String_ShouldHandleNullAndEmpty()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE messages (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                title TEXT,
                content TEXT NOT NULL
            )");
        
        connection.Execute("INSERT INTO messages (title, content) VALUES (NULL, 'content1')");
        connection.Execute("INSERT INTO messages (title, content) VALUES ('', 'content2')");
        connection.Execute("INSERT INTO messages (title, content) VALUES ('Title', 'content3')");
        
        var repo = new DataTypeTestRepository(connection);
        
        // Act
        var msg1 = repo.GetMessageAsync(1).Result;
        var msg2 = repo.GetMessageAsync(2).Result;
        var msg3 = repo.GetMessageAsync(3).Result;
        
        // Assert
        Assert.IsNull(msg1!.Title);
        Assert.AreEqual("", msg2!.Title);
        Assert.AreEqual("Title", msg3!.Title);
        
        connection.Dispose();
    }
    
    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("DataTypes")]
    [TestCategory("Multiple")]
    public void DataType_MultipleTypes_InSameEntity()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        connection.Execute(@"
            CREATE TABLE complex_entity (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                count INTEGER NOT NULL,
                amount REAL NOT NULL,
                is_enabled INTEGER NOT NULL,
                optional_value INTEGER
            )");
        
        connection.Execute(@"
            INSERT INTO complex_entity (name, count, amount, is_enabled, optional_value) 
            VALUES ('Test', 42, 99.99, 1, NULL)");
        
        var repo = new DataTypeTestRepository(connection);
        
        // Act
        var entity = repo.GetComplexEntityAsync(1).Result;
        
        // Assert
        Assert.IsNotNull(entity);
        Assert.AreEqual("Test", entity.Name);
        Assert.AreEqual(42, entity.Count);
        Assert.AreEqual(99.99m, entity.Amount);
        Assert.IsTrue(entity.IsEnabled);
        Assert.IsNull(entity.OptionalValue);
        
        connection.Dispose();
    }
}

// Test models
public class DataTypeProduct
{
    public long Id { get; set; }
    public int Stock { get; set; }
    public int Price { get; set; }
}

public class DataTypeStatistics
{
    public long Id { get; set; }
    public long TotalVisits { get; set; }
    public long TotalRevenue { get; set; }
}

public class DataTypeUser
{
    public long Id { get; set; }
    public bool IsActive { get; set; }
    public bool IsAdmin { get; set; }
}

public class DataTypeOrder
{
    public long Id { get; set; }
    public decimal Amount { get; set; }
    public decimal Tax { get; set; }
}

public class DataTypeNullableProduct
{
    public long Id { get; set; }
    public int? Stock { get; set; }
    public int? Discount { get; set; }
}

public class DataTypeMessage
{
    public long Id { get; set; }
    public string? Title { get; set; }
    public string Content { get; set; } = "";
}

public class DataTypeComplexEntity
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int Count { get; set; }
    public decimal Amount { get; set; }
    public bool IsEnabled { get; set; }
    public int? OptionalValue { get; set; }
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IDataTypeTestRepository))]
public partial class DataTypeTestRepository(IDbConnection connection) : IDataTypeTestRepository { }

public interface IDataTypeTestRepository
{
    [SqlTemplate("SELECT * FROM products WHERE id = @id")]
    Task<DataTypeProduct?> GetProductByIdAsync(long id);
    
    [SqlTemplate("SELECT * FROM statistics WHERE id = @id")]
    Task<DataTypeStatistics?> GetStatisticsAsync(long id);
    
    [SqlTemplate("SELECT * FROM users WHERE id = @id")]
    Task<DataTypeUser?> GetUserByIdAsync(long id);
    
    [SqlTemplate("SELECT * FROM orders WHERE id = @id")]
    Task<DataTypeOrder?> GetOrderByIdAsync(long id);
    
    [SqlTemplate("SELECT id, stock, discount FROM products WHERE id = @id")]
    Task<DataTypeNullableProduct?> GetNullableProductAsync(long id);
    
    [SqlTemplate("SELECT * FROM messages WHERE id = @id")]
    Task<DataTypeMessage?> GetMessageAsync(long id);
    
    [SqlTemplate("SELECT * FROM complex_entity WHERE id = @id")]
    Task<DataTypeComplexEntity?> GetComplexEntityAsync(long id);
}

// Helper extension
public static class DataTypeConnectionExtensions
{
    public static void Execute(this IDbConnection connection, string sql, object? param = null)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = sql;
        
        if (param != null)
        {
            foreach (var prop in param.GetType().GetProperties())
            {
                var p = cmd.CreateParameter();
                p.ParameterName = "@" + prop.Name;
                p.Value = prop.GetValue(param) ?? DBNull.Value;
                cmd.Parameters.Add(p);
            }
        }
        
        cmd.ExecuteNonQuery();
    }
}

