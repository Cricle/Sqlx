// -----------------------------------------------------------------------
// <copyright file="TestRepositories.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Sqlx;
using Sqlx.Annotations;

namespace Sqlx.Tests.TestModels;

// ==================== 1. 基础CRUD仓储（使用占位符） ====================
public interface IUserRepository
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(long id);

    [SqlTemplate("SELECT {{columns}} FROM {{table}}")]
    Task<List<User>> GetAllAsync();

    [SqlTemplate("SELECT {{columns}} FROM {{table}} {{where}}")]
    Task<List<User>> QueryAsync([ExpressionToSql] Expression<Func<User, bool>> predicate);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} {{orderby id}} {{limit}} {{offset}}")]
    Task<List<User>> GetPagedAsync(int? limit = null, int? offset = null);

    [SqlTemplate("INSERT INTO {{table}} (name, email, age, balance, created_at, is_active) VALUES (@name, @email, @age, @balance, @createdAt, @isActive)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string name, string email, int age, decimal balance, DateTime createdAt, bool isActive = true);

    [SqlTemplate("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id")]
    Task<int> UpdateAsync(User user);

    [SqlTemplate("DELETE FROM {{table}} WHERE id = @id")]
    Task<int> DeleteAsync(long id);

    [SqlTemplate("SELECT {{count}} FROM {{table}}")]
    Task<long> CountAsync();

    [SqlTemplate("SELECT {{sum balance}} FROM {{table}}")]
    Task<decimal> GetTotalBalanceAsync();

    [SqlTemplate("SELECT {{avg age}} FROM {{table}} WHERE is_active = {{bool_true}}")]
    Task<double> GetAverageAgeAsync();

    [SqlTemplate("SELECT {{max balance}} FROM {{table}}")]
    Task<decimal> GetMaxBalanceAsync();

    [SqlTemplate("SELECT {{columns}} FROM {{table}} {{orderby balance --desc}} {{limit}}")]
    Task<List<User>> GetTopRichUsersAsync(int? limit = 10);

    [SqlTemplate("SELECT {{distinct age}} FROM {{table}} {{orderby age}}")]
    Task<List<int>> GetDistinctAgesAsync();

    [SqlTemplate("SELECT id, name, {{coalesce email, 'no-email@example.com'}} as email, age, balance, created_at, is_active FROM {{table}} WHERE id = @id")]
    Task<User?> GetUserWithDefaultEmailAsync(long id);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = {{bool_true}}")]
    Task<List<User>> GetActiveUsersAsync();

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = {{bool_false}}")]
    Task<List<User>> GetInactiveUsersAsync();
}

// ==================== 2. 软删除仓储（使用占位符） ====================
public interface IProductRepository
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_deleted = {{bool_false}} AND id = @id")]
    Task<Product?> GetByIdAsync(long id);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_deleted = {{bool_false}}")]
    Task<List<Product>> GetAllAsync();

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_deleted = {{bool_false}} AND category = @category {{orderby price}}")]
    Task<List<Product>> GetByCategoryAsync(string category);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id {{in @ids}} AND is_deleted = {{bool_false}}")]
    Task<List<Product>> GetByIdsAsync(long[] ids);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE name {{like @pattern}} AND is_deleted = {{bool_false}}")]
    Task<List<Product>> SearchByNameAsync(string pattern);

    [SqlTemplate("INSERT INTO {{table}} (name, category, price, stock, is_deleted) VALUES (@name, @category, @price, @stock, {{bool_false}})")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string name, string category, decimal price, int stock);

    [SqlTemplate("UPDATE {{table}} SET is_deleted = {{bool_true}} WHERE id = @id")]
    Task<int> SoftDeleteAsync(long id);

    [SqlTemplate("UPDATE {{table}} SET is_deleted = {{bool_false}} WHERE id = @id")]
    Task<int> RestoreAsync(long id);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    [IncludeDeleted]
    Task<Product?> GetByIdIncludingDeletedAsync(long id);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE price {{between @minPrice, @maxPrice}} AND is_deleted = {{bool_false}}")]
    Task<List<Product>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_deleted = {{bool_false}}")]
    Task<List<Product>> GetActiveProductsAsync();
}

// ==================== 3. 审计字段仓储（使用占位符） ====================
public interface IOrderRepository
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<Order?> GetByIdAsync(long id);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE user_id = @userId {{orderby created_at --desc}}")]
    Task<List<Order>> GetByUserIdAsync(long userId);

    [SqlTemplate("INSERT INTO {{table}} (user_id, total_amount, status, created_at, created_by) VALUES (@userId, @totalAmount, @status, {{current_timestamp}}, @createdBy)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(long userId, decimal totalAmount, string status, string createdBy);

    [SqlTemplate("UPDATE {{table}} SET status = @status, updated_at = {{current_timestamp}}, updated_by = @updatedBy WHERE id = @id")]
    Task<int> UpdateStatusAsync(long id, string status, string updatedBy);

    [SqlTemplate("SELECT status, {{count}} as order_count, {{sum total_amount}} as total FROM {{table}} {{groupby status}}")]
    Task<List<Dictionary<string, object?>>> GetOrderStatsByStatusAsync();
}

// ==================== 4. 乐观锁仓储（使用占位符） ====================
public interface IAccountRepository
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<Account?> GetByIdAsync(long id);

    [SqlTemplate("UPDATE {{table}} SET balance = @balance, version = version + 1 WHERE id = @id AND version = @version")]
    Task<int> UpdateBalanceAsync(long id, decimal balance, long version);

    [SqlTemplate("INSERT INTO {{table}} (account_no, balance, version) VALUES (@accountNo, @balance, 0)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string accountNo, decimal balance);
}

// ==================== 5. 批量操作仓储（使用占位符） ====================
public interface ILogRepository
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} {{orderby timestamp --desc}} {{limit}}")]
    Task<List<Log>> GetRecentAsync(int? limit = 10);

    [BatchOperation(MaxBatchSize = 1000)]
    [SqlTemplate("INSERT INTO {{table}} (level, message, timestamp) VALUES {{batch_values}}")]
    Task<int> BatchInsertAsync(IEnumerable<Log> logs);

    [SqlTemplate("DELETE FROM {{table}} WHERE timestamp < @before")]
    Task<int> DeleteOldLogsAsync(DateTime before);

    [SqlTemplate("SELECT level, {{group_concat message, ', '}} as messages FROM {{table}} {{groupby level}} {{limit}}")]
    Task<List<Dictionary<string, object?>>> GetLogSummaryAsync(int? limit = 5);

    [SqlTemplate("SELECT {{count}} FROM {{table}} WHERE level = @level")]
    Task<long> CountByLevelAsync(string level);
}

// ==================== 6. 高级查询仓储（占位符未实现） ====================
public interface IAdvancedRepository
{
    // 注意: 以下方法使用的高级占位符尚未实现,测试会失败
    
    [SqlTemplate(@"
        SELECT p.id as product_id, p.name as product_name, p.price, c.name as category_name
        FROM {{table products}} p
        {{join --type inner --table categories c --on p.category = c.code}}
        WHERE p.is_deleted = {{bool_false}}
    ")]
    Task<List<ProductDetail>> GetProductDetailsAsync();

    [SqlTemplate(@"
        SELECT u.id as user_id, u.name as user_name, 
               {{count o.id}} as order_count, 
               {{coalesce {{sum o.total_amount}}, 0}} as total_spent
        FROM {{table users}} u
        {{join --type left --table orders o --on u.id = o.user_id}}
        {{groupby u.id, u.name}}
        {{having --condition 'COUNT(o.id) > 0'}}
    ")]
    Task<List<UserStats>> GetUserStatsAsync();

    [SqlTemplate(@"
        SELECT {{columns}} FROM {{table users}}
        WHERE {{exists --query 'SELECT 1 FROM orders WHERE orders.user_id = users.id AND total_amount > @minAmount'}}
    ")]
    Task<List<User>> GetHighValueCustomersAsync(decimal minAmount);

    [SqlTemplate("SELECT {{columns}} FROM {{table users}} {{orderby balance --desc}} {{limit}}")]
    Task<List<User>> GetTopRichUsersAsync(int? limit = 10);

    [SqlTemplate(@"
        SELECT name, 'user' as type FROM {{table users}} WHERE balance > @minBalance
        {{union}}
        SELECT name, 'product' as type FROM {{table products}} WHERE price > @minPrice
    ")]
    Task<List<Dictionary<string, object?>>> GetHighValueEntitiesAsync(decimal minBalance, decimal minPrice);

    [SqlTemplate(@"
        SELECT id, name, balance,
        {{case 
            --when 'balance > 10000' --then 'VIP'
            --when 'balance > 5000' --then 'Premium'
            --else 'Regular'
        }} as level
        FROM {{table users}}
    ")]
    Task<List<Dictionary<string, object?>>> GetUsersWithLevelAsync();

    [SqlTemplate(@"
        SELECT * FROM (
            SELECT {{columns}}, 
                   {{row_number --partition_by category --order_by price DESC}} as rank
            FROM {{table products}}
            WHERE is_deleted = {{bool_false}}
        )
        WHERE rank <= @topN
    ")]
    Task<List<Product>> GetTopProductsByCategory(int topN);
}

// ==================== 仓储实现 ====================
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository
{
    partial void OnExecuting(string operationName, DbCommand command)
    {
        // Optional: Add logging
    }
}

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
