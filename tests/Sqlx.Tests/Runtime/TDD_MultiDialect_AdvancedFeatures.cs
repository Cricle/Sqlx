using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Annotations;

namespace Sqlx.Tests.Runtime;

/// <summary>
/// 多方言高级功能运行时测试
/// 目标：验证软删除、审计字段、乐观锁在不同数据库方言下的表现
/// </summary>
[TestClass]
public class TDD_MultiDialect_AdvancedFeatures
{
    // ==================== SQLite软删除测试 ====================

    [TestMethod]
    [Ignore("高级特性 - 软删除功能还未完全实现")]
    public async Task SQLite_SoftDelete_ShouldWork()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE products (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                price REAL NOT NULL,
                is_deleted INTEGER DEFAULT 0
            )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO products (name, price) VALUES ('Product1', 99.99)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "SELECT last_insert_rowid()";
        var productId = Convert.ToInt64(cmd.ExecuteScalar());

        var repo = new SQLiteSoftDeleteTestRepository(connection);

        // Act - 软删除
        var deleted = await repo.DeleteProductAsync(productId);

        // Assert
        Assert.AreEqual(1, deleted, "应该标记删除1条记录");

        // Verify is_deleted flag is set
        cmd.CommandText = "SELECT is_deleted FROM products WHERE id = @id";
        var param = cmd.CreateParameter();
        param.ParameterName = "@id";
        param.Value = productId;
        cmd.Parameters.Add(param);
        var isDeleted = Convert.ToInt32(cmd.ExecuteScalar());
        Assert.AreEqual(1, isDeleted, "is_deleted应该被设置为1");

        // Act - 查询（不包含已删除）
        var products = await repo.GetAllProductsAsync();

        // Assert
        Assert.AreEqual(0, products.Count, "查询结果不应包含已删除的产品");

        // Act - 查询（包含已删除）
        var allProducts = await repo.GetAllProductsIncludingDeletedAsync();

        // Assert
        Assert.AreEqual(1, allProducts.Count, "包含已删除的查询应该返回1个产品");
        Assert.IsTrue(allProducts[0].IsDeleted, "产品应该被标记为已删除");
    }

    [TestMethod]
    [Ignore("高级特性 - 软删除功能还未完全实现")]
    public async Task SQLite_SoftDelete_WithTimestamp_ShouldWork()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE documents (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                title TEXT NOT NULL,
                deleted_at TEXT NULL
            )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO documents (title) VALUES ('Doc1')";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "SELECT last_insert_rowid()";
        var docId = Convert.ToInt64(cmd.ExecuteScalar());

        var repo = new SQLiteSoftDeleteTestRepository(connection);

        // Act
        var deletedBefore = DateTime.Now;
        var deleted = await repo.DeleteDocumentAsync(docId);
        var deletedAfter = DateTime.Now;

        // Assert
        Assert.AreEqual(1, deleted);

        cmd.CommandText = "SELECT deleted_at FROM documents WHERE id = @id";
        var param = cmd.CreateParameter();
        param.ParameterName = "@id";
        param.Value = docId;
        cmd.Parameters.Add(param);
        var deletedAtStr = cmd.ExecuteScalar() as string;
        Assert.IsNotNull(deletedAtStr, "deleted_at应该被设置");

        // Parse and verify timestamp is within reasonable range
        if (DateTime.TryParse(deletedAtStr, out var deletedAt))
        {
            Assert.IsTrue(deletedAt >= deletedBefore.AddSeconds(-1) &&
                         deletedAt <= deletedAfter.AddSeconds(1),
                         "deleted_at时间戳应该在合理范围内");
        }
    }

    // ==================== SQLite审计字段测试 ====================

    [TestMethod]
    [Ignore("高级特性 - 审计字段功能还未完全实现")]
    public async Task SQLite_AuditFields_Insert_ShouldSetCreatedAt()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                product_name TEXT NOT NULL,
                quantity INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NULL
            )";
        cmd.ExecuteNonQuery();

        var repo = new SQLiteAuditFieldsTestRepository(connection);

        // Act
        var beforeInsert = DateTime.Now;
        var orderId = await repo.InsertOrderAsync("Product1", 5);
        var afterInsert = DateTime.Now;

        // Assert
        Assert.IsTrue(orderId > 0);

        cmd.CommandText = "SELECT created_at FROM orders WHERE id = @id";
        var param = cmd.CreateParameter();
        param.ParameterName = "@id";
        param.Value = orderId;
        cmd.Parameters.Add(param);
        var createdAtStr = cmd.ExecuteScalar() as string;
        Assert.IsNotNull(createdAtStr, "created_at应该被自动设置");

        if (DateTime.TryParse(createdAtStr, out var createdAt))
        {
            Assert.IsTrue(createdAt >= beforeInsert.AddSeconds(-1) &&
                         createdAt <= afterInsert.AddSeconds(1),
                         "created_at应该在合理范围内");
        }
    }

    [TestMethod]
    [Ignore("高级特性 - 审计字段功能还未完全实现")]
    public async Task SQLite_AuditFields_Update_ShouldSetUpdatedAt()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE orders (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                product_name TEXT NOT NULL,
                quantity INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NULL
            )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = @"INSERT INTO orders (product_name, quantity, created_at)
                           VALUES ('Product1', 5, datetime('now'))";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "SELECT last_insert_rowid()";
        var orderId = Convert.ToInt64(cmd.ExecuteScalar());

        var repo = new SQLiteAuditFieldsTestRepository(connection);

        // Wait a bit to ensure updated_at is different
        await Task.Delay(10);

        // Act
        var beforeUpdate = DateTime.Now;
        var updated = await repo.UpdateOrderQuantityAsync(orderId, 10);
        var afterUpdate = DateTime.Now;

        // Assert
        Assert.AreEqual(1, updated);

        cmd.CommandText = "SELECT updated_at FROM orders WHERE id = @id";
        var param = cmd.CreateParameter();
        param.ParameterName = "@id";
        param.Value = orderId;
        cmd.Parameters.Add(param);
        var updatedAtStr = cmd.ExecuteScalar() as string;
        Assert.IsNotNull(updatedAtStr, "updated_at应该被自动设置");

        if (DateTime.TryParse(updatedAtStr, out var updatedAt))
        {
            Assert.IsTrue(updatedAt >= beforeUpdate.AddSeconds(-1) &&
                         updatedAt <= afterUpdate.AddSeconds(1),
                         "updated_at应该在合理范围内");
        }
    }

    // ==================== SQLite乐观锁测试 ====================

    [TestMethod]
    [Ignore("高级特性 - 乐观锁功能还未完全实现")]
    public async Task SQLite_ConcurrencyCheck_ShouldIncrementVersion()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE accounts (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                balance REAL NOT NULL,
                version INTEGER DEFAULT 0
            )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO accounts (name, balance, version) VALUES ('Account1', 1000.0, 0)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "SELECT last_insert_rowid()";
        var accountId = Convert.ToInt64(cmd.ExecuteScalar());

        var repo = new SQLiteConcurrencyTestRepository(connection);

        // Act
        var updated = await repo.UpdateBalanceAsync(accountId, 1500.0m, 0);

        // Assert
        Assert.AreEqual(1, updated, "应该更新1条记录");

        // Verify version is incremented
        cmd.CommandText = "SELECT version FROM accounts WHERE id = @id";
        var param = cmd.CreateParameter();
        param.ParameterName = "@id";
        param.Value = accountId;
        cmd.Parameters.Add(param);
        var version = Convert.ToInt32(cmd.ExecuteScalar());
        Assert.AreEqual(1, version, "version应该增加到1");
    }

    [TestMethod]
    [Ignore("高级特性 - 乐观锁功能还未完全实现")]
    public async Task SQLite_ConcurrencyCheck_WithOldVersion_ShouldFail()
    {
        // Arrange
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE accounts (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                balance REAL NOT NULL,
                version INTEGER DEFAULT 0
            )";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO accounts (name, balance, version) VALUES ('Account1', 1000.0, 0)";
        cmd.ExecuteNonQuery();
        cmd.CommandText = "SELECT last_insert_rowid()";
        var accountId = Convert.ToInt64(cmd.ExecuteScalar());

        var repo = new SQLiteConcurrencyTestRepository(connection);

        // First update
        await repo.UpdateBalanceAsync(accountId, 1500.0m, 0);

        // Act - Try to update with old version
        var updated = await repo.UpdateBalanceAsync(accountId, 2000.0m, 0);

        // Assert
        Assert.AreEqual(0, updated, "使用旧版本号不应该更新任何记录");

        // Verify balance is still 1500
        cmd.CommandText = "SELECT balance FROM accounts WHERE id = @id";
        var param = cmd.CreateParameter();
        param.ParameterName = "@id";
        param.Value = accountId;
        cmd.Parameters.Add(param);
        var balance = Convert.ToDecimal(cmd.ExecuteScalar());
        Assert.AreEqual(1500.0m, balance, "余额不应该被更新");
    }

    // ==================== 代码生成测试 ====================

    [TestMethod]
    [Ignore("代码生成验证测试 - 需要独立仓储文件才能触发源生成")]
    public void MySQL_SoftDelete_GeneratesCorrectSQL()
    {
        var generatedCode = GetGeneratedCode<MySqlSoftDeleteTestRepository>();

        // DELETE应该转换为UPDATE
        Assert.IsTrue(generatedCode.Contains("UPDATE") &&
                     generatedCode.Contains("SET is_deleted") ||
                     generatedCode.Contains("SET deleted_at"),
            "MySQL软删除应该生成UPDATE语句");
        Assert.IsFalse(generatedCode.Contains("DELETE FROM"),
            "不应该有真正的DELETE语句");
    }

    [TestMethod]
    [Ignore("代码生成验证测试 - 需要独立仓储文件才能触发源生成")]
    public void PostgreSQL_SoftDelete_GeneratesCorrectSQL()
    {
        var generatedCode = GetGeneratedCode<PostgreSqlSoftDeleteTestRepository>();

        Assert.IsTrue(generatedCode.Contains("UPDATE") &&
                     (generatedCode.Contains("SET is_deleted") ||
                      generatedCode.Contains("SET deleted_at")),
            "PostgreSQL软删除应该生成UPDATE语句");
    }

    [TestMethod]
    [Ignore("代码生成验证测试 - 需要独立仓储文件才能触发源生成")]
    public void SqlServer_SoftDelete_GeneratesCorrectSQL()
    {
        var generatedCode = GetGeneratedCode<SqlServerSoftDeleteTestRepository>();

        Assert.IsTrue(generatedCode.Contains("UPDATE") &&
                     (generatedCode.Contains("SET is_deleted") ||
                      generatedCode.Contains("SET deleted_at")),
            "SQL Server软删除应该生成UPDATE语句");
    }

    [TestMethod]
    [Ignore("代码生成验证测试 - 需要独立仓储文件才能触发源生成")]
    public void MySQL_AuditFields_GeneratesCorrectSQL()
    {
        var generatedCode = GetGeneratedCode<MySqlAuditFieldsTestRepository>();

        // INSERT应该包含created_at
        Assert.IsTrue(generatedCode.Contains("created_at") ||
                     generatedCode.Contains("NOW()") ||
                     generatedCode.Contains("CURRENT_TIMESTAMP"),
            "MySQL INSERT应该设置created_at");

        // UPDATE应该包含updated_at
        Assert.IsTrue(generatedCode.Contains("updated_at") ||
                     generatedCode.Contains("NOW()") ||
                     generatedCode.Contains("CURRENT_TIMESTAMP"),
            "MySQL UPDATE应该设置updated_at");
    }

    [TestMethod]
    [Ignore("代码生成验证测试 - 需要独立仓储文件才能触发源生成")]
    public void PostgreSQL_ConcurrencyCheck_GeneratesCorrectSQL()
    {
        var generatedCode = GetGeneratedCode<PostgreSqlConcurrencyTestRepository>();

        // UPDATE应该包含version检查和递增
        Assert.IsTrue(generatedCode.Contains("version = version + 1") ||
                     generatedCode.Contains("version = @version + 1"),
            "PostgreSQL UPDATE应该递增version");
        Assert.IsTrue(generatedCode.Contains("WHERE") &&
                     generatedCode.Contains("version"),
            "PostgreSQL UPDATE应该在WHERE中检查version");
    }

    // ==================== 辅助方法 ====================

    private string GetGeneratedCode<T>()
    {
        var directory = "obj/Debug/net9.0/Sqlx.Generator/Sqlx.CSharpGenerator/";
        if (System.IO.Directory.Exists(directory))
        {
            var files = System.IO.Directory.GetFiles(directory, "*.Repository.g.cs");
            foreach (var file in files)
            {
                var content = System.IO.File.ReadAllText(file);
                if (content.Contains(typeof(T).Name))
                {
                    return content;
                }
            }
        }
        return string.Empty;
    }
}

// ==================== 测试实体 ====================

public class SoftDeleteProduct
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsDeleted { get; set; }
}

public class SoftDeleteDocument
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime? DeletedAt { get; set; }
}

public class AuditOrder
{
    public long Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ConcurrencyAccount
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Balance { get; set; }

    [ConcurrencyCheck]
    public int Version { get; set; }
}

// ==================== SQLite仓储 ====================

public interface ISQLiteSoftDeleteTestRepository
{
    [SqlTemplate("DELETE FROM products WHERE id = @id")]
    Task<int> DeleteProductAsync(long id);

    [SqlTemplate("SELECT id, name, price, is_deleted FROM products WHERE is_deleted = 0")]
    Task<List<SoftDeleteProduct>> GetAllProductsAsync();

    [IncludeDeleted]
    [SqlTemplate("SELECT id, name, price, is_deleted FROM products")]
    Task<List<SoftDeleteProduct>> GetAllProductsIncludingDeletedAsync();

    [SqlTemplate("DELETE FROM documents WHERE id = @id")]
    Task<int> DeleteDocumentAsync(long id);
}

[TableName("products")]
[SoftDelete(FlagColumn = "is_deleted", TimestampColumn = "deleted_at")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ISQLiteSoftDeleteTestRepository))]
public partial class SQLiteSoftDeleteTestRepository(System.Data.Common.DbConnection connection)
    : ISQLiteSoftDeleteTestRepository { }

public interface ISQLiteAuditFieldsTestRepository
{
    [ReturnInsertedId]
    [SqlTemplate("INSERT INTO orders (product_name, quantity) VALUES (@productName, @quantity)")]
    Task<long> InsertOrderAsync(string productName, int quantity);

    [SqlTemplate("UPDATE orders SET quantity = @quantity WHERE id = @id")]
    Task<int> UpdateOrderQuantityAsync(long id, int quantity);
}

[AuditFields(CreatedAtColumn = "created_at", UpdatedAtColumn = "updated_at")]
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ISQLiteAuditFieldsTestRepository))]
public partial class SQLiteAuditFieldsTestRepository(System.Data.Common.DbConnection connection)
    : ISQLiteAuditFieldsTestRepository { }

public interface ISQLiteConcurrencyTestRepository
{
    [SqlTemplate("UPDATE accounts SET balance = @balance WHERE id = @id AND version = @version")]
    Task<int> UpdateBalanceAsync(long id, decimal balance, int version);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ISQLiteConcurrencyTestRepository))]
public partial class SQLiteConcurrencyTestRepository(System.Data.Common.DbConnection connection)
    : ISQLiteConcurrencyTestRepository { }

// ==================== MySQL仓储 (代码生成测试) ====================

public interface IMySqlSoftDeleteTestRepository
{
    [SqlTemplate("DELETE FROM products WHERE id = @id")]
    Task<int> DeleteProductAsync(long id);
}

[TableName("products")]
[SoftDelete(FlagColumn = "is_deleted")]
[SqlDefine(SqlDefineTypes.MySql)]
[RepositoryFor(typeof(IMySqlSoftDeleteTestRepository))]
public partial class MySqlSoftDeleteTestRepository(System.Data.Common.DbConnection connection)
    : IMySqlSoftDeleteTestRepository { }

public interface IMySqlAuditFieldsTestRepository
{
    [SqlTemplate("INSERT INTO orders (product_name, quantity) VALUES (@productName, @quantity)")]
    Task<int> InsertOrderAsync(string productName, int quantity);

    [SqlTemplate("UPDATE orders SET quantity = @quantity WHERE id = @id")]
    Task<int> UpdateOrderQuantityAsync(long id, int quantity);
}

[AuditFields(CreatedAtColumn = "created_at", UpdatedAtColumn = "updated_at")]
[SqlDefine(SqlDefineTypes.MySql)]
[RepositoryFor(typeof(IMySqlAuditFieldsTestRepository))]
public partial class MySqlAuditFieldsTestRepository(System.Data.Common.DbConnection connection)
    : IMySqlAuditFieldsTestRepository { }

// ==================== PostgreSQL仓储 (代码生成测试) ====================

public interface IPostgreSqlSoftDeleteTestRepository
{
    [SqlTemplate("DELETE FROM products WHERE id = @id")]
    Task<int> DeleteProductAsync(long id);
}

[TableName("products")]
[SoftDelete(FlagColumn = "is_deleted")]
[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IPostgreSqlSoftDeleteTestRepository))]
public partial class PostgreSqlSoftDeleteTestRepository(System.Data.Common.DbConnection connection)
    : IPostgreSqlSoftDeleteTestRepository { }

public interface IPostgreSqlConcurrencyTestRepository
{
    [SqlTemplate("UPDATE accounts SET balance = @balance WHERE id = @id AND version = @version")]
    Task<int> UpdateBalanceAsync(long id, decimal balance, int version);
}

[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IPostgreSqlConcurrencyTestRepository))]
public partial class PostgreSqlConcurrencyTestRepository(System.Data.Common.DbConnection connection)
    : IPostgreSqlConcurrencyTestRepository { }

// ==================== SQL Server仓储 (代码生成测试) ====================

public interface ISqlServerSoftDeleteTestRepository
{
    [SqlTemplate("DELETE FROM products WHERE id = @id")]
    Task<int> DeleteProductAsync(long id);
}

[TableName("products")]
[SoftDelete(FlagColumn = "is_deleted")]
[SqlDefine(SqlDefineTypes.SqlServer)]
[RepositoryFor(typeof(ISqlServerSoftDeleteTestRepository))]
public partial class SqlServerSoftDeleteTestRepository(System.Data.Common.DbConnection connection)
    : ISqlServerSoftDeleteTestRepository { }

