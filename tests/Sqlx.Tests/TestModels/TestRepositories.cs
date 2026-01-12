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

    [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY id {{limit}} {{offset}}")]
    Task<List<User>> GetPagedAsync(int? limit = null, int? offset = null);

    [SqlTemplate("INSERT INTO {{table}} (name, email, age, balance, created_at, is_active) VALUES (@name, @email, @age, @balance, @createdAt, @isActive)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string name, string email, int age, decimal balance, DateTime createdAt, bool isActive = true);

    [SqlTemplate("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id")]
    Task<int> UpdateAsync(User user);

    [SqlTemplate("DELETE FROM {{table}} WHERE id = @id")]
    Task<int> DeleteAsync(long id);

    [SqlTemplate("SELECT COUNT(*) FROM {{table}}")]
    Task<long> CountAsync();

    [SqlTemplate("SELECT SUM(balance) FROM {{table}}")]
    Task<decimal> GetTotalBalanceAsync();

    [SqlTemplate("SELECT AVG(age) FROM {{table}} WHERE is_active = {{bool_true}}")]
    Task<double> GetAverageAgeAsync();

    [SqlTemplate("SELECT MAX(balance) FROM {{table}}")]
    Task<decimal> GetMaxBalanceAsync();

    [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY balance DESC {{limit}}")]
    Task<List<User>> GetTopRichUsersAsync(int? limit = 10);

    [SqlTemplate("SELECT DISTINCT age FROM {{table}} ORDER BY age")]
    Task<List<int>> GetDistinctAgesAsync();

    [SqlTemplate("SELECT id, name, {{coalesce --columns email --default 'no-email@example.com'}} as email, age, balance, created_at, is_active FROM {{table}} WHERE id = @id")]
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

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_deleted = {{bool_false}} AND category = @category ORDER BY price")]
    Task<List<Product>> GetByCategoryAsync(string category);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id IN @ids AND is_deleted = {{bool_false}}")]
    Task<List<Product>> GetByIdsAsync(long[] ids);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE name LIKE @pattern AND is_deleted = {{bool_false}}")]
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

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE price BETWEEN @minPrice AND @maxPrice AND is_deleted = {{bool_false}}")]
    Task<List<Product>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_deleted = {{bool_false}}")]
    Task<List<Product>> GetActiveProductsAsync();
}

// ==================== 3. 审计字段仓储（使用占位符） ====================
public interface IOrderRepository
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<Order?> GetByIdAsync(long id);

    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE user_id = @userId ORDER BY created_at DESC")]
    Task<List<Order>> GetByUserIdAsync(long userId);

    [SqlTemplate("INSERT INTO {{table}} (user_id, total_amount, status, created_at, created_by) VALUES (@userId, @totalAmount, @status, {{current_timestamp}}, @createdBy)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(long userId, decimal totalAmount, string status, string createdBy);

    [SqlTemplate("UPDATE {{table}} SET status = @status, updated_at = {{current_timestamp}}, updated_by = @updatedBy WHERE id = @id")]
    Task<int> UpdateStatusAsync(long id, string status, string updatedBy);

    [SqlTemplate("SELECT status, COUNT(*) as order_count, SUM(total_amount) as total FROM {{table}} GROUP BY status")]
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
    [SqlTemplate("SELECT {{columns}} FROM {{table}} ORDER BY timestamp DESC {{limit}}")]
    Task<List<Log>> GetRecentAsync(int? limit = 10);

    [BatchOperation(MaxBatchSize = 1000)]
    [SqlTemplate("INSERT INTO {{table}} (level, message, timestamp) VALUES {{batch_values}}")]
    Task<int> BatchInsertAsync(IEnumerable<Log> logs);

    [SqlTemplate("DELETE FROM {{table}} WHERE timestamp < @before")]
    Task<int> DeleteOldLogsAsync(DateTime before);

    [SqlTemplate("SELECT level, COUNT(*) as count FROM {{table}} GROUP BY level {{limit}}")]
    Task<List<Dictionary<string, object?>>> GetLogSummaryAsync(int? limit = 5);

    [SqlTemplate("SELECT COUNT(*) FROM {{table}} WHERE level = @level")]
    Task<long> CountByLevelAsync(string level);
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
