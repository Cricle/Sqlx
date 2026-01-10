using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Sqlx;
using Sqlx.Annotations;
using FullDemo.Models;

namespace FullDemo.Repositories;

// ==================== 用户仓储接口 ====================
/// <summary>
/// 用户仓储 - 继承 ICrudRepository 获得完整 CRUD 操作，并添加自定义方法
/// </summary>
public interface IUserRepository : ICrudRepository<User, long>
{
    // 继承自 ICrudRepository:
    // - GetByIdAsync(id)
    // - GetAllAsync(limit, orderBy)
    // - GetByIdsAsync(ids)
    // - GetTopAsync(limit, orderBy)
    // - GetRangeAsync(limit, offset, orderBy)
    // - GetPageAsync(pageNumber, pageSize, orderBy)
    // - GetWhereAsync(predicate)
    // - GetFirstWhereAsync(predicate)
    // - ExistsAsync(id)
    // - ExistsWhereAsync(predicate)
    // - GetRandomAsync(count)
    // - InsertAsync(entity)
    // - InsertAndGetIdAsync(entity)
    // - InsertAndGetEntityAsync(entity)
    // - UpdateAsync(entity)
    // - UpdatePartialAsync(id, updates)
    // - UpdateWhereAsync(predicate, updates)
    // - UpsertAsync(entity)
    // - DeleteAsync(id)
    // - DeleteWhereAsync(predicate)
    // - SoftDeleteAsync(id)
    // - RestoreAsync(id)
    // - CountAsync()

    // 自定义方法:

    /// <summary>按邮箱查找用户</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE email = @email")]
    Task<User?> GetByEmailAsync(string email);

    /// <summary>搜索用户（模糊匹配）</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE name {{like @pattern}} {{orderby name}}")]
    Task<List<User>> SearchAsync(string pattern);

    /// <summary>获取活跃用户</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = {{bool_true}} {{orderby created_at --desc}}")]
    Task<List<User>> GetActiveUsersAsync();

    /// <summary>获取非活跃用户</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = {{bool_false}}")]
    Task<List<User>> GetInactiveUsersAsync();

    /// <summary>获取余额最高的用户</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} {{orderby balance --desc}} {{limit --param count}}")]
    Task<List<User>> GetTopRichUsersAsync(int count);

    /// <summary>获取用户总余额</summary>
    [SqlTemplate("SELECT {{sum balance}} FROM {{table}}")]
    Task<decimal> GetTotalBalanceAsync();

    /// <summary>获取用户平均年龄</summary>
    [SqlTemplate("SELECT {{avg age}} FROM {{table}} WHERE is_active = {{bool_true}}")]
    Task<double> GetAverageAgeAsync();

    /// <summary>获取最大余额</summary>
    [SqlTemplate("SELECT {{max balance}} FROM {{table}}")]
    Task<decimal> GetMaxBalanceAsync();

    /// <summary>获取不同年龄列表</summary>
    [SqlTemplate("SELECT {{distinct age}} FROM {{table}} {{orderby age}}")]
    Task<List<int>> GetDistinctAgesAsync();

    /// <summary>按年龄范围查询</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE age BETWEEN @minAge AND @maxAge {{orderby age}}")]
    Task<List<User>> GetByAgeRangeAsync(int minAge, int maxAge);

    /// <summary>批量更新用户状态</summary>
    [SqlTemplate("UPDATE {{table}} SET is_active = @isActive WHERE id {{in @ids}}")]
    Task<int> BatchUpdateStatusAsync(long[] ids, bool isActive);
}

// ==================== 产品仓储接口（软删除） ====================
/// <summary>
/// 产品仓储 - 演示软删除功能
/// </summary>
public interface IProductRepository : ICrudRepository<Product, long>
{
    /// <summary>按分类获取产品</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE category = @category AND is_deleted = {{bool_false}} {{orderby price}}")]
    Task<List<Product>> GetByCategoryAsync(string category);

    /// <summary>按ID列表获取产品</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id {{in @ids}} AND is_deleted = {{bool_false}}")]
    Task<List<Product>> GetByIdsExcludeDeletedAsync(long[] ids);

    /// <summary>搜索产品</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE name {{like @pattern}} AND is_deleted = {{bool_false}}")]
    Task<List<Product>> SearchByNameAsync(string pattern);

    /// <summary>按价格范围查询</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE price BETWEEN @minPrice AND @maxPrice AND is_deleted = {{bool_false}} {{orderby price}}")]
    Task<List<Product>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice);

    /// <summary>获取活跃产品</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_deleted = {{bool_false}} {{orderby created_at --desc}}")]
    Task<List<Product>> GetActiveProductsAsync();

    /// <summary>软删除产品</summary>
    [SqlTemplate("UPDATE {{table}} SET is_deleted = {{bool_true}} WHERE id = @id")]
    Task<int> SoftDeleteProductAsync(long id);

    /// <summary>恢复产品</summary>
    [SqlTemplate("UPDATE {{table}} SET is_deleted = {{bool_false}} WHERE id = @id")]
    Task<int> RestoreProductAsync(long id);

    /// <summary>获取已删除的产品</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_deleted = {{bool_true}}")]
    Task<List<Product>> GetDeletedProductsAsync();

    /// <summary>获取产品总价值（库存 × 价格）</summary>
    [SqlTemplate("SELECT COALESCE(SUM(price * stock), 0) FROM {{table}} WHERE is_deleted = {{bool_false}}")]
    Task<decimal> GetTotalInventoryValueAsync();
}

// ==================== 订单仓储接口（审计字段） ====================
/// <summary>
/// 订单仓储 - 演示审计字段功能
/// </summary>
public interface IOrderRepository : ICrudRepository<Order, long>
{
    /// <summary>按用户ID获取订单</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE user_id = @userId {{orderby created_at --desc}}")]
    Task<List<Order>> GetByUserIdAsync(long userId);

    /// <summary>按状态获取订单</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE status = @status {{orderby created_at --desc}}")]
    Task<List<Order>> GetByStatusAsync(string status);

    /// <summary>更新订单状态（带审计）</summary>
    [SqlTemplate("UPDATE {{table}} SET status = @status, updated_at = {{current_timestamp}}, updated_by = @updatedBy WHERE id = @id")]
    Task<int> UpdateStatusAsync(long id, string status, string updatedBy);

    /// <summary>获取用户订单总金额</summary>
    [SqlTemplate("SELECT COALESCE(SUM(total_amount), 0) FROM {{table}} WHERE user_id = @userId")]
    Task<decimal> GetUserTotalSpentAsync(long userId);

    /// <summary>按日期范围获取订单</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE created_at BETWEEN @startDate AND @endDate {{orderby created_at --desc}}")]
    Task<List<Order>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
}

// ==================== 账户仓储接口（乐观锁） ====================
/// <summary>
/// 账户仓储 - 演示乐观锁功能
/// </summary>
public interface IAccountRepository : ICrudRepository<Account, long>
{
    /// <summary>按账号查找</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE account_no = @accountNo")]
    Task<Account?> GetByAccountNoAsync(string accountNo);

    /// <summary>更新余额（带版本检查）</summary>
    [SqlTemplate("UPDATE {{table}} SET balance = @newBalance, version = version + 1 WHERE id = @id AND version = @version")]
    Task<int> UpdateBalanceWithVersionAsync(long id, decimal newBalance, long version);

    /// <summary>增加余额（带版本检查）</summary>
    [SqlTemplate("UPDATE {{table}} SET balance = balance + @amount, version = version + 1 WHERE id = @id AND version = @version")]
    Task<int> IncreaseBalanceAsync(long id, decimal amount, long version);

    /// <summary>减少余额（带版本检查）</summary>
    [SqlTemplate("UPDATE {{table}} SET balance = balance - @amount, version = version + 1 WHERE id = @id AND version = @version AND balance >= @amount")]
    Task<int> DecreaseBalanceAsync(long id, decimal amount, long version);
}

// ==================== 日志仓储接口（批量操作） ====================
/// <summary>
/// 日志仓储 - 演示批量操作功能
/// </summary>
public interface ILogRepository : ICrudRepository<Log, long>
{
    /// <summary>获取最近日志</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} {{orderby timestamp --desc}} {{limit --param count}}")]
    Task<List<Log>> GetRecentAsync(int count);

    /// <summary>按级别获取日志</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE level = @level {{orderby timestamp --desc}}")]
    Task<List<Log>> GetByLevelAsync(string level);

    /// <summary>删除旧日志</summary>
    [SqlTemplate("DELETE FROM {{table}} WHERE timestamp < @before")]
    Task<int> DeleteOldLogsAsync(DateTime before);

    /// <summary>按级别统计日志数量</summary>
    [SqlTemplate("SELECT {{count}} FROM {{table}} WHERE level = @level")]
    Task<long> CountByLevelAsync(string level);

    /// <summary>批量插入日志</summary>
    [SqlTemplate("INSERT INTO {{table}} (level, message, timestamp) VALUES {{batch_values --exclude Id}}")]
    [BatchOperation(MaxBatchSize = 1000)]
    Task<int> BatchInsertLogsAsync(List<Log> logs);

    /// <summary>清空日志表</summary>
    [SqlTemplate("DELETE FROM {{table}}")]
    Task<int> ClearAllAsync();
}

// ==================== 分类仓储接口 ====================
public interface ICategoryRepository : ICrudRepository<Category, long>
{
    /// <summary>按编码获取分类</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE code = @code")]
    Task<Category?> GetByCodeAsync(string code);

    /// <summary>获取活跃分类</summary>
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = {{bool_true}} {{orderby name}}")]
    Task<List<Category>> GetActiveCategoriesAsync();
}
