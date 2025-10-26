using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Sqlx;
using Sqlx.Annotations;

namespace FullFeatureDemo;

// ==================== 1. 基础CRUD仓储 ====================
public interface IUserRepository
{
    // 基础查询
    [SqlTemplate("SELECT * FROM users WHERE id = @id")]
    Task<User?> GetByIdAsync(long id);

    [SqlTemplate("SELECT * FROM users")]
    Task<List<User>> GetAllAsync();

    // 条件查询
    [SqlTemplate("SELECT * FROM users WHERE age >= @minAge")]
    Task<List<User>> GetAdultsAsync(int minAge);

    // 分页查询
    [SqlTemplate("SELECT * FROM users ORDER BY id LIMIT @limit OFFSET @offset")]
    Task<List<User>> GetPagedAsync(int limit, int offset);

    // 插入并返回ID
    [SqlTemplate("INSERT INTO users (name, email, age, balance, created_at) VALUES (@name, @email, @age, @balance, @createdAt)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string name, string email, int age, decimal balance, DateTime createdAt);

    // 更新
    [SqlTemplate("UPDATE users SET name = @name, age = @age WHERE id = @id")]
    Task<int> UpdateAsync(long id, string name, int age);

    // 删除
    [SqlTemplate("DELETE FROM users WHERE id = @id")]
    Task<int> DeleteAsync(long id);

    // 聚合查询
    [SqlTemplate("SELECT COUNT(*) FROM users")]
    Task<int> CountAsync();

    [SqlTemplate("SELECT SUM(balance) FROM users")]
    Task<decimal> GetTotalBalanceAsync();

    // 动态WHERE条件
    [SqlTemplate("SELECT * FROM users WHERE balance > @minBalance")]
    Task<List<User>> GetRichUsersAsync(decimal minBalance);
}

// ==================== 2. 软删除仓储 ====================
public interface IProductRepository
{
    // 查询（自动排除已删除）
    [SqlTemplate("SELECT * FROM products WHERE is_deleted = 0 AND id = @id")]
    Task<Product?> GetByIdAsync(long id);

    [SqlTemplate("SELECT * FROM products WHERE is_deleted = 0")]
    Task<List<Product>> GetAllAsync();

    // 按分类查询
    [SqlTemplate("SELECT * FROM products WHERE is_deleted = 0 AND category = @category")]
    Task<List<Product>> GetByCategoryAsync(string category);

    // 插入
    [SqlTemplate("INSERT INTO products (name, category, price, stock, is_deleted) VALUES (@name, @category, @price, @stock, 0)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string name, string category, decimal price, int stock);

    // 软删除
    [SqlTemplate("UPDATE products SET is_deleted = 1 WHERE id = @id")]
    Task<int> SoftDeleteAsync(long id);

    // 恢复删除
    [SqlTemplate("UPDATE products SET is_deleted = 0 WHERE id = @id")]
    Task<int> RestoreAsync(long id);

    // 包含已删除的查询
    [SqlTemplate("SELECT * FROM products WHERE id = @id")]
    Task<Product?> GetByIdIncludingDeletedAsync(long id);
}

// ==================== 3. 审计字段仓储 ====================
public interface IOrderRepository
{
    [SqlTemplate("SELECT * FROM orders WHERE id = @id")]
    Task<Order?> GetByIdAsync(long id);

    [SqlTemplate("SELECT * FROM orders WHERE user_id = @userId ORDER BY created_at DESC")]
    Task<List<Order>> GetByUserIdAsync(long userId);

    // 插入（自动填充审计字段）
    [SqlTemplate("INSERT INTO orders (user_id, total_amount, status, created_at, created_by) VALUES (@userId, @totalAmount, @status, @createdAt, @createdBy)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(long userId, decimal totalAmount, string status, DateTime createdAt, string createdBy);

    // 更新（自动更新审计字段）
    [SqlTemplate("UPDATE orders SET status = @status, updated_at = @updatedAt, updated_by = @updatedBy WHERE id = @id")]
    Task<int> UpdateStatusAsync(long id, string status, DateTime updatedAt, string updatedBy);
}

// ==================== 4. 乐观锁仓储 ====================
public interface IAccountRepository
{
    [SqlTemplate("SELECT * FROM accounts WHERE id = @id")]
    Task<Account?> GetByIdAsync(long id);

    // 带乐观锁的更新
    [SqlTemplate("UPDATE accounts SET balance = @balance, version = version + 1 WHERE id = @id AND version = @version")]
    Task<int> UpdateBalanceAsync(long id, decimal balance, long version);

    [SqlTemplate("INSERT INTO accounts (account_no, balance, version) VALUES (@accountNo, @balance, 0)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string accountNo, decimal balance);
}

// ==================== 5. 批量操作仓储 ====================
public interface ILogRepository
{
    [SqlTemplate("SELECT * FROM logs ORDER BY timestamp DESC LIMIT @limit")]
    Task<List<Log>> GetRecentAsync(int limit);

    // 批量插入
    [BatchOperation(MaxBatchSize = 1000)]
    [SqlTemplate("INSERT INTO logs (level, message, timestamp) VALUES {{batch_values}}")]
    Task<int> BatchInsertAsync(IEnumerable<Log> logs);

    // 批量删除
    [SqlTemplate("DELETE FROM logs WHERE timestamp < @before")]
    Task<int> DeleteOldLogsAsync(DateTime before);
}

// ==================== 6. 复杂查询仓储 ====================
public interface IAdvancedRepository
{
    // JOIN查询
    [SqlTemplate(@"SELECT p.id as product_id, p.name as product_name, p.price, c.name as category_name
                   FROM products p
                   INNER JOIN categories c ON p.category = c.code
                   WHERE p.is_deleted = 0")]
    Task<List<ProductDetail>> GetProductDetailsAsync();

    // 聚合查询
    [SqlTemplate(@"SELECT u.id as user_id, u.name as user_name, COUNT(o.id) as order_count, COALESCE(SUM(o.total_amount), 0) as total_spent
                   FROM users u
                   LEFT JOIN orders o ON u.id = o.user_id
                   GROUP BY u.id, u.name
                   HAVING COUNT(o.id) > 0")]
    Task<List<UserStats>> GetUserStatsAsync();

    // 子查询
    [SqlTemplate(@"SELECT * FROM users WHERE id IN (SELECT DISTINCT user_id FROM orders WHERE total_amount > @minAmount)")]
    Task<List<User>> GetHighValueCustomersAsync(decimal minAmount);

    // TOP查询
    [SqlTemplate(@"SELECT id, name, email, age, balance, created_at FROM users ORDER BY balance DESC LIMIT @limit")]
    Task<List<User>> GetTopRichUsersAsync(int limit);
}

// ==================== 仓储实现 ====================
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository { }

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IProductRepository))]
public partial class ProductRepository(DbConnection connection) : IProductRepository { }

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IOrderRepository))]
public partial class OrderRepository(DbConnection connection) : IOrderRepository { }

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IAccountRepository))]
public partial class AccountRepository(DbConnection connection) : IAccountRepository { }

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ILogRepository))]
public partial class LogRepository(DbConnection connection) : ILogRepository { }

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IAdvancedRepository))]
public partial class AdvancedRepository(DbConnection connection) : IAdvancedRepository { }

