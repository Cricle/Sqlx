using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Sqlx;
using Sqlx.Annotations;

namespace FullFeatureDemo;

// ==================== 1. 基础CRUD仓储（使用占位符） ====================
public interface IUserRepository
{
    // ✨ 使用 {{columns}} 和 {{table}} 占位符
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(long id);

    // ✨ 自动生成列名
    [SqlTemplate("SELECT {{columns}} FROM {{table}}")]
    Task<List<User>> GetAllAsync();

    // ✨ 使用 {{where}} 占位符 + 表达式树
    [SqlTemplate("SELECT {{columns}} FROM {{table}} {{where}}")]
    Task<List<User>> QueryAsync([ExpressionToSql] Expression<Func<User, bool>> predicate);

    // ✨ 使用 {{orderby}} 和 {{limit}} {{offset}} 占位符
    [SqlTemplate("SELECT {{columns}} FROM {{table}} {{orderby id}} {{limit}} {{offset}}")]
    Task<List<User>> GetPagedAsync(int? limit = null, int? offset = null);

    // ✨ 使用 {{values}} 占位符插入
    [SqlTemplate("INSERT INTO {{table}} (name, email, age, balance, created_at, is_active) VALUES (@name, @email, @age, @balance, @createdAt, @isActive)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string name, string email, int age, decimal balance, DateTime createdAt, bool isActive = true);

    // ✨ 使用 {{set}} 占位符更新
    [SqlTemplate("UPDATE {{table}} SET {{set}} WHERE id = @id")]
    Task<int> UpdateAsync(User user);

    // ✨ 简单删除
    [SqlTemplate("DELETE FROM {{table}} WHERE id = @id")]
    Task<int> DeleteAsync(long id);

    // ✨ 使用 {{count}} 占位符
    [SqlTemplate("SELECT {{count}} FROM {{table}}")]
    Task<long> CountAsync();

    // ✨ 使用 {{sum}} 聚合函数占位符
    [SqlTemplate("SELECT {{sum balance}} FROM {{table}}")]
    Task<decimal> GetTotalBalanceAsync();

    // ✨ 使用 {{avg}} 聚合函数
    [SqlTemplate("SELECT {{avg age}} FROM {{table}} WHERE is_active = {{bool_true}}")]
    Task<double> GetAverageAgeAsync();

    // ✨ 使用 {{max}} 和 {{min}} 聚合函数
    [SqlTemplate("SELECT {{max balance}} FROM {{table}}")]
    Task<decimal> GetMaxBalanceAsync();

    // ✨ 使用 {{orderby --desc}} 降序排序
    [SqlTemplate("SELECT {{columns}} FROM {{table}} {{orderby balance --desc}} {{limit}}")]
    Task<List<User>> GetTopRichUsersAsync(int? limit = 10);

    // ✨ 使用 {{distinct}} 占位符
    [SqlTemplate("SELECT {{distinct age}} FROM {{table}} {{orderby age}}")]
    Task<List<int>> GetDistinctAgesAsync();

    // ✨ 使用 {{coalesce}} 处理NULL值
    [SqlTemplate("SELECT id, name, {{coalesce email, 'no-email@example.com'}} as email FROM {{table}} WHERE id = @id")]
    Task<User?> GetUserWithDefaultEmailAsync(long id);

    // ✨ 使用 {{current_timestamp}} 方言占位符
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE created_at > {{current_timestamp}} - INTERVAL '7 days'")]
    Task<List<User>> GetRecentUsersAsync();
}

// ==================== 2. 软删除仓储（使用占位符） ====================
public interface IProductRepository
{
    // ✨ 使用 {{bool_false}} 方言占位符
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_deleted = {{bool_false}} AND id = @id")]
    Task<Product?> GetByIdAsync(long id);

    // ✨ 软删除查询自动排除
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_deleted = {{bool_false}}")]
    Task<List<Product>> GetAllAsync();

    // ✨ 按分类查询 + 排序
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_deleted = {{bool_false}} AND category = @category {{orderby price}}")]
    Task<List<Product>> GetByCategoryAsync(string category);

    // ✨ 使用 {{in}} 占位符查询多个ID
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id {{in @ids}} AND is_deleted = {{bool_false}}")]
    Task<List<Product>> GetByIdsAsync(long[] ids);

    // ✨ 使用 {{like}} 占位符模糊搜索
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE name {{like @pattern}} AND is_deleted = {{bool_false}}")]
    Task<List<Product>> SearchByNameAsync(string pattern);

    // ✨ 插入（自动设置is_deleted=false）
    [SqlTemplate("INSERT INTO {{table}} (name, category, price, stock, is_deleted) VALUES (@name, @category, @price, @stock, {{bool_false}})")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string name, string category, decimal price, int stock);

    // ✨ 软删除（设置标志）
    [SqlTemplate("UPDATE {{table}} SET is_deleted = {{bool_true}} WHERE id = @id")]
    Task<int> SoftDeleteAsync(long id);

    // ✨ 恢复删除
    [SqlTemplate("UPDATE {{table}} SET is_deleted = {{bool_false}} WHERE id = @id")]
    Task<int> RestoreAsync(long id);

    // ✨ 包含已删除的查询
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    [IncludeDeleted]
    Task<Product?> GetByIdIncludingDeletedAsync(long id);

    // ✨ 使用 {{between}} 占位符价格范围查询
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE price {{between @minPrice, @maxPrice}} AND is_deleted = {{bool_false}}")]
    Task<List<Product>> GetByPriceRangeAsync(decimal minPrice, decimal maxPrice);
}

// ==================== 3. 审计字段仓储（使用占位符） ====================
public interface IOrderRepository
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<Order?> GetByIdAsync(long id);

    // ✨ 使用 {{orderby --desc}} 按创建时间降序
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE user_id = @userId {{orderby created_at --desc}}")]
    Task<List<Order>> GetByUserIdAsync(long userId);

    // ✨ 使用 {{current_timestamp}} 自动插入时间
    [SqlTemplate("INSERT INTO {{table}} (user_id, total_amount, status, created_at, created_by) VALUES (@userId, @totalAmount, @status, {{current_timestamp}}, @createdBy)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(long userId, decimal totalAmount, string status, string createdBy);

    // ✨ 使用 {{set}} 占位符 + 自动更新审计字段
    [SqlTemplate("UPDATE {{table}} SET status = @status, updated_at = {{current_timestamp}}, updated_by = @updatedBy WHERE id = @id")]
    Task<int> UpdateStatusAsync(long id, string status, string updatedBy);

    // ✨ 使用 {{date_diff}} 获取N天内的订单
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{date_diff day, created_at, {{current_timestamp}}}} <= @days")]
    Task<List<Order>> GetOrdersWithinDaysAsync(int days);

    // ✨ 使用 {{group_by}} 按状态统计
    [SqlTemplate("SELECT status, {{count}} as order_count, {{sum total_amount}} as total FROM {{table}} {{groupby status}}")]
    Task<List<Dictionary<string, object?>>> GetOrderStatsByStatusAsync();
}

// ==================== 4. 乐观锁仓储（使用占位符） ====================
public interface IAccountRepository
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<Account?> GetByIdAsync(long id);

    // ✨ 带乐观锁的更新（version自动递增）
    [SqlTemplate("UPDATE {{table}} SET balance = @balance, version = version + 1 WHERE id = @id AND version = @version")]
    Task<int> UpdateBalanceAsync(long id, decimal balance, long version);

    // ✨ 使用 {{values}} 插入并初始化version=0
    [SqlTemplate("INSERT INTO {{table}} (account_no, balance, version) VALUES (@accountNo, @balance, 0)")]
    [ReturnInsertedId]
    Task<long> InsertAsync(string accountNo, decimal balance);

    // ✨ 使用 {{cast}} 类型转换
    [SqlTemplate("SELECT id, account_no, {{cast balance, DECIMAL}} as balance, version FROM {{table}} WHERE id = @id")]
    Task<Account?> GetAccountWithCastAsync(long id);
}

// ==================== 5. 批量操作仓储（使用占位符） ====================
public interface ILogRepository
{
    // ✨ 使用 {{orderby --desc}} 和 {{limit}}
    [SqlTemplate("SELECT {{columns}} FROM {{table}} {{orderby timestamp --desc}} {{limit}}")]
    Task<List<Log>> GetRecentAsync(int? limit = 10);

    // ✨ 使用 {{batch_values}} 批量插入
    [BatchOperation(MaxBatchSize = 1000)]
    [SqlTemplate("INSERT INTO {{table}} (level, message, timestamp) VALUES {{batch_values}}")]
    Task<int> BatchInsertAsync(IEnumerable<Log> logs);

    // ✨ 批量删除
    [SqlTemplate("DELETE FROM {{table}} WHERE timestamp < @before")]
    Task<int> DeleteOldLogsAsync(DateTime before);

    // ✨ 使用 {{group_concat}} 聚合字符串
    [SqlTemplate("SELECT level, {{group_concat message, ', '}} as messages FROM {{table}} {{groupby level}} {{limit}}")]
    Task<List<Dictionary<string, object?>>> GetLogSummaryAsync(int? limit = 5);

    // ✨ 使用 {{count}} + WHERE
    [SqlTemplate("SELECT {{count}} FROM {{table}} WHERE level = @level")]
    Task<long> CountByLevelAsync(string level);
}

// ==================== 6. 复杂查询仓储（使用占位符） ====================
public interface IAdvancedRepository
{
    // ✨ 使用 {{join}} 占位符
    [SqlTemplate(@"
        SELECT p.id as product_id, p.name as product_name, p.price, c.name as category_name
        FROM {{table products}} p
        {{join --type inner --table categories c --on p.category = c.code}}
        WHERE p.is_deleted = {{bool_false}}
    ")]
    Task<List<ProductDetail>> GetProductDetailsAsync();

    // ✨ 使用 {{groupby}} 和 {{having}} 占位符
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

    // ✨ 使用 {{exists}} 子查询
    [SqlTemplate(@"
        SELECT {{columns}} FROM {{table users}}
        WHERE {{exists --query 'SELECT 1 FROM orders WHERE orders.user_id = users.id AND total_amount > @minAmount'}}
    ")]
    Task<List<User>> GetHighValueCustomersAsync(decimal minAmount);

    // ✨ 使用 {{orderby --desc}} + {{limit}}
    [SqlTemplate("SELECT {{columns}} FROM {{table users}} {{orderby balance --desc}} {{limit}}")]
    Task<List<User>> GetTopRichUsersAsync(int? limit = 10);

    // ✨ 使用 {{union}} 合并查询
    [SqlTemplate(@"
        SELECT name, 'user' as type FROM {{table users}} WHERE balance > @minBalance
        {{union}}
        SELECT name, 'product' as type FROM {{table products}} WHERE price > @minPrice
    ")]
    Task<List<Dictionary<string, object?>>> GetHighValueEntitiesAsync(decimal minBalance, decimal minPrice);

    // ✨ 使用 {{case}} 条件表达式
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

    // ✨ 使用 {{row_number}} 窗口函数
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

// ==================== 7. 表达式树查询仓储 ====================
public interface IExpressionRepository
{
    // ✨ 表达式树：简单条件
    [SqlTemplate("SELECT {{columns}} FROM {{table users}} {{where}}")]
    Task<List<User>> FindUsersAsync([ExpressionToSql] Expression<Func<User, bool>> predicate);

    // ✨ 表达式树：组合排序和分页
    [SqlTemplate("SELECT {{columns}} FROM {{table users}} {{where}} {{orderby balance --desc}} {{limit}} {{offset}}")]
    Task<List<User>> FindUsersPagedAsync(
        [ExpressionToSql] Expression<Func<User, bool>> predicate,
        int? limit = null,
        int? offset = null);

    // ✨ 表达式树：计数
    [SqlTemplate("SELECT {{count}} FROM {{table users}} {{where}}")]
    Task<long> CountUsersAsync([ExpressionToSql] Expression<Func<User, bool>> predicate);

    // ✨ 表达式树：获取最大值
    [SqlTemplate("SELECT {{max balance}} FROM {{table users}} {{where}}")]
    Task<decimal?> GetMaxBalanceAsync([ExpressionToSql] Expression<Func<User, bool>> predicate);
}

// ==================== 仓储实现 ====================
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository 
{
    // ✨ 可选：添加日志拦截器
    partial void OnExecuting(string operationName, DbCommand command)
    {
        Console.WriteLine($"[SQL] {operationName}: {command.CommandText}");
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

[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IExpressionRepository))]
public partial class ExpressionRepository(DbConnection connection) : IExpressionRepository { }
