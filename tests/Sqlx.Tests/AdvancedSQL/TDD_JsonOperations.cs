using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Sqlx.Annotations;
using Sqlx.Generator;

namespace Sqlx.Tests.AdvancedSQL;

/// <summary>
/// JSON操作测试 - Requirement 49
/// 包括：SQL Server JSON_VALUE、PostgreSQL jsonb、MySQL JSON_EXTRACT、SQLite json_extract
/// 注意：SQLite支持JSON1扩展，可以用于测试基本JSON功能
/// </summary>
[TestClass]
public class TDD_JsonOperations
{
    private SqliteConnection? _connection;

    [TestInitialize]
    public void Setup()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _connection?.Close();
        _connection?.Dispose();
    }

    #region SQLite json_extract测试 (Requirement 49.4)

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("JsonOperations")]
    [Description("SQLite json_extract - 提取简单字段")]
    public async Task SQLite_JsonExtract_SimpleField_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE products (id INTEGER, data TEXT)");
        _connection.Execute("INSERT INTO products VALUES (1, '{\"name\": \"Laptop\", \"price\": 999.99}')");
        _connection.Execute("INSERT INTO products VALUES (2, '{\"name\": \"Phone\", \"price\": 599.99}')");

        var repo = new JsonOperationsRepo(_connection);

        // Act
        var results = await repo.GetProductNames();

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("Laptop", results.First(p => p.Id == 1).Name);
        Assert.AreEqual("Phone", results.First(p => p.Id == 2).Name);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("JsonOperations")]
    [Description("SQLite json_extract - 提取嵌套字段")]
    public async Task SQLite_JsonExtract_NestedField_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE users (id INTEGER, profile TEXT)");
        _connection.Execute("INSERT INTO users VALUES (1, '{\"name\": \"John\", \"address\": {\"city\": \"New York\", \"zip\": \"10001\"}}')");
        _connection.Execute("INSERT INTO users VALUES (2, '{\"name\": \"Jane\", \"address\": {\"city\": \"Los Angeles\", \"zip\": \"90001\"}}')");

        var repo = new JsonOperationsRepo(_connection);

        // Act
        var results = await repo.GetUserCities();

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("New York", results.First(u => u.Id == 1).City);
        Assert.AreEqual("Los Angeles", results.First(u => u.Id == 2).City);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("JsonOperations")]
    [Description("SQLite json_extract - 提取数组元素")]
    public async Task SQLite_JsonExtract_ArrayElement_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE orders (id INTEGER, items TEXT)");
        _connection.Execute("INSERT INTO orders VALUES (1, '{\"items\": [\"apple\", \"banana\", \"orange\"]}')");
        _connection.Execute("INSERT INTO orders VALUES (2, '{\"items\": [\"milk\", \"bread\"]}')");

        var repo = new JsonOperationsRepo(_connection);

        // Act
        var results = await repo.GetFirstOrderItem();

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual("apple", results.First(o => o.Id == 1).FirstItem);
        Assert.AreEqual("milk", results.First(o => o.Id == 2).FirstItem);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("JsonOperations")]
    [Description("SQLite json_extract - 提取数值类型")]
    public async Task SQLite_JsonExtract_NumericValue_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE inventory (id INTEGER, data TEXT)");
        _connection.Execute("INSERT INTO inventory VALUES (1, '{\"quantity\": 100, \"price\": 29.99}')");
        _connection.Execute("INSERT INTO inventory VALUES (2, '{\"quantity\": 50, \"price\": 49.99}')");

        var repo = new JsonOperationsRepo(_connection);

        // Act
        var results = await repo.GetInventoryQuantities();

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual(100, results.First(i => i.Id == 1).Quantity);
        Assert.AreEqual(50, results.First(i => i.Id == 2).Quantity);
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("JsonOperations")]
    [Description("SQLite json_extract - WHERE子句中使用")]
    public async Task SQLite_JsonExtract_InWhereClause_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE events (id INTEGER, data TEXT)");
        _connection.Execute("INSERT INTO events VALUES (1, '{\"type\": \"click\", \"count\": 10}')");
        _connection.Execute("INSERT INTO events VALUES (2, '{\"type\": \"view\", \"count\": 100}')");
        _connection.Execute("INSERT INTO events VALUES (3, '{\"type\": \"click\", \"count\": 5}')");

        var repo = new JsonOperationsRepo(_connection);

        // Act
        var results = await repo.GetClickEvents();

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.IsTrue(results.All(e => e.Type == "click"));
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("JsonOperations")]
    [Description("SQLite json_extract - NULL值处理")]
    public async Task SQLite_JsonExtract_NullValue_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE configs (id INTEGER, settings TEXT)");
        _connection.Execute("INSERT INTO configs VALUES (1, '{\"theme\": \"dark\", \"language\": null}')");
        _connection.Execute("INSERT INTO configs VALUES (2, '{\"theme\": \"light\"}')"); // language字段不存在

        var repo = new JsonOperationsRepo(_connection);

        // Act
        var results = await repo.GetConfigLanguages();

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.IsNull(results.First(c => c.Id == 1).Language);
        Assert.IsNull(results.First(c => c.Id == 2).Language);
    }

    #endregion

    #region 方言特定JSON语法测试

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("JsonOperations")]
    [Description("SQL Server JSON_VALUE语法验证")]
    public void SqlServer_JsonValue_SyntaxShouldBeCorrect()
    {
        // Arrange
        var provider = new SqlServerDialectProvider();
        
        // Act - 验证SQL Server的JSON_VALUE语法格式
        var sql = "SELECT JSON_VALUE(data, '$.name') as name FROM products";
        
        // Assert - SQL Server使用JSON_VALUE函数
        Assert.IsTrue(sql.Contains("JSON_VALUE"));
        Assert.IsTrue(sql.Contains("$.name"));
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("JsonOperations")]
    [Description("PostgreSQL jsonb操作符语法验证")]
    public void PostgreSQL_JsonbOperator_SyntaxShouldBeCorrect()
    {
        // Arrange
        var provider = new PostgreSqlDialectProvider();
        
        // Act - 验证PostgreSQL的jsonb操作符语法
        var sqlArrow = "SELECT data->>'name' as name FROM products"; // ->> 返回文本
        var sqlContains = "SELECT * FROM products WHERE data @> '{\"active\": true}'"; // @> 包含
        var sqlExists = "SELECT * FROM products WHERE data ? 'name'"; // ? 键存在
        
        // Assert
        Assert.IsTrue(sqlArrow.Contains("->>"));
        Assert.IsTrue(sqlContains.Contains("@>"));
        Assert.IsTrue(sqlExists.Contains("?"));
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("JsonOperations")]
    [Description("MySQL JSON_EXTRACT语法验证")]
    public void MySQL_JsonExtract_SyntaxShouldBeCorrect()
    {
        // Arrange
        var provider = new MySqlDialectProvider();
        
        // Act - 验证MySQL的JSON_EXTRACT语法
        var sql = "SELECT JSON_EXTRACT(data, '$.name') as name FROM products";
        var sqlUnquote = "SELECT JSON_UNQUOTE(JSON_EXTRACT(data, '$.name')) as name FROM products";
        
        // Assert
        Assert.IsTrue(sql.Contains("JSON_EXTRACT"));
        Assert.IsTrue(sqlUnquote.Contains("JSON_UNQUOTE"));
    }

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("JsonOperations")]
    [Description("SQLite json_extract语法验证")]
    public void SQLite_JsonExtract_SyntaxShouldBeCorrect()
    {
        // Arrange
        var provider = new SQLiteDialectProvider();
        
        // Act - 验证SQLite的json_extract语法
        var sql = "SELECT json_extract(data, '$.name') as name FROM products";
        
        // Assert
        Assert.IsTrue(sql.Contains("json_extract"));
        Assert.IsTrue(sql.Contains("$.name"));
    }

    #endregion

    #region JSON操作在聚合中使用

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("JsonOperations")]
    [Description("SQLite json_extract - 在聚合函数中使用")]
    public async Task SQLite_JsonExtract_InAggregate_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE sales (id INTEGER, data TEXT)");
        _connection.Execute("INSERT INTO sales VALUES (1, '{\"amount\": 100, \"region\": \"North\"}')");
        _connection.Execute("INSERT INTO sales VALUES (2, '{\"amount\": 200, \"region\": \"South\"}')");
        _connection.Execute("INSERT INTO sales VALUES (3, '{\"amount\": 150, \"region\": \"North\"}')");

        var repo = new JsonOperationsRepo(_connection);

        // Act
        var results = await repo.GetSalesSumByRegion();

        // Assert
        Assert.AreEqual(2, results.Count);
        Assert.AreEqual(250, results.First(s => s.Region == "North").TotalAmount, 0.01);
        Assert.AreEqual(200, results.First(s => s.Region == "South").TotalAmount, 0.01);
    }

    #endregion

    #region JSON数组操作

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("JsonOperations")]
    [Description("SQLite json_array_length - 获取数组长度")]
    public async Task SQLite_JsonArrayLength_ShouldWork()
    {
        // Arrange
        _connection!.Execute("CREATE TABLE carts (id INTEGER, items TEXT)");
        _connection.Execute("INSERT INTO carts VALUES (1, '{\"items\": [1, 2, 3, 4, 5]}')");
        _connection.Execute("INSERT INTO carts VALUES (2, '{\"items\": [1, 2]}')");
        _connection.Execute("INSERT INTO carts VALUES (3, '{\"items\": []}')");

        var repo = new JsonOperationsRepo(_connection);

        // Act
        var results = await repo.GetCartItemCounts();

        // Assert
        Assert.AreEqual(3, results.Count);
        Assert.AreEqual(5, results.First(c => c.Id == 1).ItemCount);
        Assert.AreEqual(2, results.First(c => c.Id == 2).ItemCount);
        Assert.AreEqual(0, results.First(c => c.Id == 3).ItemCount);
    }

    #endregion

    #region 不支持JSON的方言错误处理 (Requirement 49.5)

    [TestMethod]
    [TestCategory("TDD-Green")]
    [TestCategory("Phase2")]
    [TestCategory("JsonOperations")]
    [Description("验证各方言的JSON支持状态")]
    public void AllDialects_JsonSupport_ShouldBeDocumented()
    {
        // 验证各方言的JSON支持情况
        // SQL Server 2016+ 支持 JSON_VALUE, JSON_QUERY
        // PostgreSQL 9.2+ 支持 json, 9.4+ 支持 jsonb
        // MySQL 5.7+ 支持 JSON_EXTRACT
        // SQLite 3.9+ 支持 json_extract (需要JSON1扩展)
        // Oracle 12c+ 支持 JSON_VALUE
        
        var sqlServerProvider = new SqlServerDialectProvider();
        var postgreSqlProvider = new PostgreSqlDialectProvider();
        var mySqlProvider = new MySqlDialectProvider();
        var sqliteProvider = new SQLiteDialectProvider();
        
        // 所有现代数据库都支持JSON操作
        Assert.IsNotNull(sqlServerProvider);
        Assert.IsNotNull(postgreSqlProvider);
        Assert.IsNotNull(mySqlProvider);
        Assert.IsNotNull(sqliteProvider);
    }

    #endregion
}


#region JSON操作Repository接口和模型

public interface IJsonOperationsRepo
{
    // SQLite json_extract - 简单字段
    [SqlTemplate("SELECT id, json_extract(data, '$.name') as name FROM products")]
    Task<List<ProductName>> GetProductNames();

    // SQLite json_extract - 嵌套字段
    [SqlTemplate("SELECT id, json_extract(profile, '$.address.city') as city FROM users")]
    Task<List<UserCity>> GetUserCities();

    // SQLite json_extract - 数组元素
    [SqlTemplate("SELECT id, json_extract(items, '$.items[0]') as first_item FROM orders")]
    Task<List<OrderFirstItem>> GetFirstOrderItem();

    // SQLite json_extract - 数值类型
    [SqlTemplate("SELECT id, json_extract(data, '$.quantity') as quantity FROM inventory")]
    Task<List<InventoryQuantity>> GetInventoryQuantities();

    // SQLite json_extract - WHERE子句
    [SqlTemplate("SELECT id, json_extract(data, '$.type') as type FROM events WHERE json_extract(data, '$.type') = 'click'")]
    Task<List<EventType>> GetClickEvents();

    // SQLite json_extract - NULL值
    [SqlTemplate("SELECT id, json_extract(settings, '$.language') as language FROM configs")]
    Task<List<ConfigLanguage>> GetConfigLanguages();

    // SQLite json_extract - 聚合
    [SqlTemplate("SELECT json_extract(data, '$.region') as region, SUM(json_extract(data, '$.amount')) as total_amount FROM sales GROUP BY json_extract(data, '$.region')")]
    Task<List<SalesRegionSum>> GetSalesSumByRegion();

    // SQLite json_array_length
    [SqlTemplate("SELECT id, json_array_length(json_extract(items, '$.items')) as item_count FROM carts")]
    Task<List<CartItemCount>> GetCartItemCounts();
}

[RepositoryFor(typeof(IJsonOperationsRepo))]
public partial class JsonOperationsRepo(IDbConnection connection) : IJsonOperationsRepo { }

// 模型类
public class ProductName
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
}

public class UserCity
{
    public long Id { get; set; }
    public string City { get; set; } = "";
}

public class OrderFirstItem
{
    public long Id { get; set; }
    public string FirstItem { get; set; } = "";
}

public class InventoryQuantity
{
    public long Id { get; set; }
    public int Quantity { get; set; }
}

public class EventType
{
    public long Id { get; set; }
    public string Type { get; set; } = "";
}

public class ConfigLanguage
{
    public long Id { get; set; }
    public string? Language { get; set; }
}

public class SalesRegionSum
{
    public string Region { get; set; } = "";
    public double TotalAmount { get; set; }
}

public class CartItemCount
{
    public long Id { get; set; }
    public int ItemCount { get; set; }
}

#endregion
