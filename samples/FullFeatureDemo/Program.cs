using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using FullFeatureDemo;

Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║                                                                ║");
Console.WriteLine("║          Sqlx 全功能演示 (Full Feature Demo)                  ║");
Console.WriteLine("║                                                                ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
Console.WriteLine();

// 创建数据库连接
using DbConnection connection = new SqliteConnection("Data Source=:memory:");
connection.Open();

// 初始化数据库
await InitializeDatabaseAsync(connection);

try
{
    // 1. 基础CRUD操作
    await Demo1_BasicCrudAsync(connection);

    // 2. 软删除功能
    await Demo2_SoftDeleteAsync(connection);

    // 3. 审计字段
    await Demo3_AuditFieldsAsync(connection);

    // 4. 乐观锁
    await Demo4_OptimisticLockingAsync(connection);

    // 5. 批量操作
    await Demo5_BatchOperationsAsync(connection);

    // 6. 复杂查询
    await Demo6_AdvancedQueriesAsync(connection);

    // 7. 事务支持
    await Demo7_TransactionsAsync(connection);

    Console.WriteLine();
    Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║                                                                ║");
    Console.WriteLine("║          ✅ 所有演示完成！Sqlx 功能强大且易用！               ║");
    Console.WriteLine("║                                                                ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ 错误: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}

// ==================== 演示函数 ====================

static async Task Demo1_BasicCrudAsync(DbConnection connection)
{
    PrintSection("1. 基础 CRUD 操作");

    var repo = new UserRepository(connection);

    // 插入并返回ID
    Console.WriteLine("📝 插入用户...");
    var userId1 = await repo.InsertAsync("张三", "zhangsan@example.com", 25, 1000.50m, DateTime.Now);
    Console.WriteLine($"   ✅ 插入成功，ID: {userId1}");

    var userId2 = await repo.InsertAsync("李四", "lisi@example.com", 30, 2500.75m, DateTime.Now);
    Console.WriteLine($"   ✅ 插入成功，ID: {userId2}");

    var userId3 = await repo.InsertAsync("王五", "wangwu@example.com", 17, 500.00m, DateTime.Now);
    Console.WriteLine($"   ✅ 插入成功，ID: {userId3}");

    var userId4 = await repo.InsertAsync("赵六", "zhaoliu@example.com", 28, 1800.00m, DateTime.Now);
    Console.WriteLine($"   ✅ 插入成功，ID: {userId4}");

    // 查询单个
    Console.WriteLine("\n🔍 查询单个用户...");
    var user = await repo.GetByIdAsync(userId1);
    Console.WriteLine($"   ✅ 查询结果: {user?.Name}, 年龄: {user?.Age}, 余额: {user?.Balance:C}");

    // 查询所有
    Console.WriteLine("\n🔍 查询所有用户...");
    var allUsers = await repo.GetAllAsync();
    Console.WriteLine($"   ✅ 共 {allUsers.Count} 个用户");
    foreach (var u in allUsers)
    {
        Console.WriteLine($"      - {u.Name} ({u.Age}岁), 余额: {u.Balance:C}");
    }

    // 条件查询
    Console.WriteLine("\n🔍 查询成年用户 (age >= 18)...");
    var adults = await repo.GetAdultsAsync(18);
    Console.WriteLine($"   ✅ 共 {adults.Count} 个成年用户");

    // 分页查询
    Console.WriteLine("\n📄 分页查询 (limit 2, offset 1)...");
    var pagedUsers = await repo.GetPagedAsync(2, 1);
    Console.WriteLine($"   ✅ 返回 {pagedUsers.Count} 条记录");
    foreach (var u in pagedUsers)
    {
        Console.WriteLine($"      - {u.Name}");
    }

    // 更新
    Console.WriteLine("\n✏️ 更新用户...");
    var updated = await repo.UpdateAsync(userId1, "张三（已更新）", 26);
    Console.WriteLine($"   ✅ 更新了 {updated} 条记录");

    // 聚合查询
    Console.WriteLine("\n📊 聚合查询...");
    var count = await repo.CountAsync();
    var totalBalance = await repo.GetTotalBalanceAsync();
    Console.WriteLine($"   ✅ 总用户数: {count}");
    Console.WriteLine($"   ✅ 总余额: {totalBalance:C}");

    // 删除
    Console.WriteLine("\n🗑️ 删除用户...");
    var deleted = await repo.DeleteAsync(userId3);
    Console.WriteLine($"   ✅ 删除了 {deleted} 条记录");
}

static async Task Demo2_SoftDeleteAsync(DbConnection connection)
{
    PrintSection("2. 软删除功能");

    var repo = new ProductRepository(connection);

    // 插入产品
    Console.WriteLine("📝 插入产品...");
    var productId1 = await repo.InsertAsync("iPhone 15", "Electronics", 999.99m, 100);
    var productId2 = await repo.InsertAsync("MacBook Pro", "Electronics", 2499.99m, 50);
    var productId3 = await repo.InsertAsync("AirPods Pro", "Electronics", 249.99m, 200);
    Console.WriteLine($"   ✅ 插入了 3 个产品");

    // 查询所有
    Console.WriteLine("\n🔍 查询所有产品...");
    var products = await repo.GetAllAsync();
    Console.WriteLine($"   ✅ 共 {products.Count} 个产品");
    foreach (var p in products)
    {
        Console.WriteLine($"      - {p.Name}: {p.Price:C} (库存: {p.Stock})");
    }

    // 软删除
    Console.WriteLine($"\n🗑️ 软删除产品 (ID: {productId2})...");
    await repo.SoftDeleteAsync(productId2);
    Console.WriteLine("   ✅ 已软删除");

    // 再次查询（应该看不到已删除的）
    Console.WriteLine("\n🔍 再次查询所有产品（已删除的不显示）...");
    products = await repo.GetAllAsync();
    Console.WriteLine($"   ✅ 现在有 {products.Count} 个产品");

    // 包含已删除的查询
    Console.WriteLine($"\n🔍 查询包含已删除的产品 (ID: {productId2})...");
    var deletedProduct = await repo.GetByIdIncludingDeletedAsync(productId2);
    Console.WriteLine($"   ✅ 找到: {deletedProduct?.Name}, IsDeleted: {deletedProduct?.IsDeleted}");

    // 恢复
    Console.WriteLine($"\n♻️ 恢复已删除的产品 (ID: {productId2})...");
    await repo.RestoreAsync(productId2);
    Console.WriteLine("   ✅ 已恢复");

    // 验证恢复
    products = await repo.GetAllAsync();
    Console.WriteLine($"   ✅ 恢复后有 {products.Count} 个产品");
}

static async Task Demo3_AuditFieldsAsync(DbConnection connection)
{
    PrintSection("3. 审计字段");

    var repo = new OrderRepository(connection);

    // 创建订单
    Console.WriteLine("📝 创建订单...");
    var orderId = await repo.InsertAsync(1, 1999.98m, "Pending", DateTime.Now, "Admin");
    Console.WriteLine($"   ✅ 订单创建成功，ID: {orderId}");

    // 查询订单
    var order = await repo.GetByIdAsync(orderId);
    Console.WriteLine($"   ✅ 创建时间: {order?.CreatedAt:yyyy-MM-dd HH:mm:ss}");
    Console.WriteLine($"   ✅ 创建人: {order?.CreatedBy}");

    // 更新订单状态
    Console.WriteLine("\n✏️ 更新订单状态...");
    await Task.Delay(1000); // 模拟时间流逝
    await repo.UpdateStatusAsync(orderId, "Shipped", DateTime.Now, "System");

    // 再次查询
    order = await repo.GetByIdAsync(orderId);
    Console.WriteLine($"   ✅ 更新时间: {order?.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
    Console.WriteLine($"   ✅ 更新人: {order?.UpdatedBy}");
    Console.WriteLine($"   ✅ 新状态: {order?.Status}");
}

static async Task Demo4_OptimisticLockingAsync(DbConnection connection)
{
    PrintSection("4. 乐观锁（Optimistic Locking）");

    var repo = new AccountRepository(connection);

    // 创建账户
    Console.WriteLine("📝 创建账户...");
    var accountId = await repo.InsertAsync("ACC001", 10000.00m);
    Console.WriteLine($"   ✅ 账户创建成功，ID: {accountId}");

    // 查询账户
    var account = await repo.GetByIdAsync(accountId);
    Console.WriteLine($"   ✅ 初始余额: {account?.Balance:C}, 版本: {account?.Version}");

    // 模拟并发更新 - 第一个事务
    Console.WriteLine("\n💰 事务1: 扣款 1000元...");
    var version1 = account!.Version;
    var newBalance1 = account.Balance - 1000;
    var updated1 = await repo.UpdateBalanceAsync(accountId, newBalance1, version1);
    Console.WriteLine($"   ✅ 事务1成功，更新了 {updated1} 条记录");

    // 查询最新状态
    account = await repo.GetByIdAsync(accountId);
    Console.WriteLine($"   ✅ 当前余额: {account?.Balance:C}, 版本: {account?.Version}");

    // 模拟并发更新 - 第二个事务（使用旧版本号，应该失败）
    Console.WriteLine("\n💰 事务2: 尝试使用旧版本号扣款 500元...");
    var newBalance2 = 10000 - 500; // 使用原始余额
    var updated2 = await repo.UpdateBalanceAsync(accountId, newBalance2, version1); // 使用旧版本号

    if (updated2 == 0)
    {
        Console.WriteLine("   ❌ 事务2失败！版本冲突，更新了 0 条记录");
        Console.WriteLine("   ✅ 乐观锁保护成功，防止了数据覆盖");
    }
    else
    {
        Console.WriteLine($"   ⚠️ 事务2意外成功，更新了 {updated2} 条记录");
    }

    // 最终状态
    account = await repo.GetByIdAsync(accountId);
    Console.WriteLine($"\n   ✅ 最终余额: {account?.Balance:C}, 版本: {account?.Version}");
}

static async Task Demo5_BatchOperationsAsync(DbConnection connection)
{
    PrintSection("5. 批量操作");

    var repo = new LogRepository(connection);

    // 生成1000条日志
    Console.WriteLine("📝 生成1000条日志记录...");
    var logs = Enumerable.Range(1, 1000)
        .Select(i => new Log
        {
            Level = i % 3 == 0 ? "ERROR" : (i % 2 == 0 ? "WARN" : "INFO"),
            Message = $"日志消息 #{i}",
            Timestamp = DateTime.Now.AddSeconds(-i)
        })
        .ToList();

    Console.WriteLine("   ✅ 日志生成完成");

    // 批量插入
    Console.WriteLine("\n⚡ 批量插入日志（自动分批）...");
    var startTime = DateTime.Now;
    var inserted = await repo.BatchInsertAsync(logs);
    var duration = DateTime.Now - startTime;

    Console.WriteLine($"   ✅ 插入了 {inserted} 条记录");
    Console.WriteLine($"   ✅ 耗时: {duration.TotalMilliseconds:F2}ms");
    Console.WriteLine($"   ✅ 平均: {duration.TotalMilliseconds / inserted:F4}ms/条");

    // 查询最近的日志
    Console.WriteLine("\n🔍 查询最近10条日志...");
    var recentLogs = await repo.GetRecentAsync(10);
    foreach (var log in recentLogs.Take(5))
    {
        Console.WriteLine($"   [{log.Level}] {log.Message} - {log.Timestamp:HH:mm:ss}");
    }
    Console.WriteLine($"   ... 还有 {recentLogs.Count - 5} 条");

    // 批量删除旧日志
    Console.WriteLine("\n🗑️ 删除1分钟前的日志...");
    var before = DateTime.Now.AddMinutes(-1);
    var deletedCount = await repo.DeleteOldLogsAsync(before);
    Console.WriteLine($"   ✅ 删除了 {deletedCount} 条旧日志");
}

static async Task Demo6_AdvancedQueriesAsync(DbConnection connection)
{
    PrintSection("6. 复杂查询");

    var repo = new AdvancedRepository(connection);

    // JOIN查询
    Console.WriteLine("🔗 JOIN查询 - 产品详情...");
    var productDetails = await repo.GetProductDetailsAsync();
    Console.WriteLine($"   ✅ 找到 {productDetails.Count} 个产品详情");
    foreach (var detail in productDetails.Take(3))
    {
        Console.WriteLine($"      - {detail.ProductName} ({detail.CategoryName}): {detail.Price:C}");
    }

    // 聚合查询
    Console.WriteLine("\n📊 聚合查询 - 用户统计...");
    var userStats = await repo.GetUserStatsAsync();
    Console.WriteLine($"   ✅ 找到 {userStats.Count} 个用户统计");
    foreach (var stat in userStats)
    {
        Console.WriteLine($"      - {stat.UserName}: {stat.OrderCount} 个订单, 总消费 {stat.TotalSpent:C}");
    }

    // 子查询
    Console.WriteLine("\n🔍 子查询 - 高价值客户 (总订单金额 > 1000)...");
    var highValueCustomers = await repo.GetHighValueCustomersAsync(1000);
    Console.WriteLine($"   ✅ 找到 {highValueCustomers.Count} 个高价值客户");

    // TOP查询
    Console.WriteLine("\n🏆 TOP查询 - 余额最高的3个用户...");
    var topRichUsers = await repo.GetTopRichUsersAsync(3);
    foreach (var user in topRichUsers)
    {
        Console.WriteLine($"      - {user.Name}: {user.Balance:C}");
    }
}

static async Task Demo7_TransactionsAsync(DbConnection connection)
{
    PrintSection("7. 事务支持");

    var userRepo = new UserRepository(connection);
    var orderRepo = new OrderRepository(connection);

    Console.WriteLine("💳 场景：用户下单，扣除余额");

    // 查询用户余额
    var user = await userRepo.GetByIdAsync(1);
    Console.WriteLine($"   初始余额: {user?.Balance:C}");

    // 开始事务
    Console.WriteLine("\n⚡ 开始事务...");
    using var transaction = connection.BeginTransaction() as System.Data.Common.DbTransaction
        ?? throw new InvalidOperationException("Connection must support DbTransaction for async operations.");

    try
    {
        // 设置事务
        userRepo.Transaction = transaction;
        orderRepo.Transaction = transaction;

        // 创建订单
        Console.WriteLine("   1️⃣ 创建订单...");
        var orderId = await orderRepo.InsertAsync(1, 500.00m, "Pending", DateTime.Now, "User");
        Console.WriteLine($"      ✅ 订单ID: {orderId}");

        // 扣除余额
        Console.WriteLine("   2️⃣ 扣除余额...");
        var newBalance = user!.Balance - 500;
        await userRepo.UpdateAsync(1, user.Name, user.Age);
        Console.WriteLine($"      ✅ 新余额: {newBalance:C}");

        // 提交事务
        transaction.Commit();
        Console.WriteLine("\n   ✅ 事务提交成功！");
    }
    catch (Exception ex)
    {
        transaction.Rollback();
        Console.WriteLine($"\n   ❌ 事务回滚: {ex.Message}");
    }
}


static Task InitializeDatabaseAsync(DbConnection connection)
{
    Console.WriteLine("🔧 初始化数据库...");

    using var cmd = connection.CreateCommand();

    // 创建表
    cmd.CommandText = @"
        CREATE TABLE users (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            email TEXT NOT NULL,
            age INTEGER NOT NULL,
            balance REAL NOT NULL,
            created_at TEXT NOT NULL
        );

        CREATE TABLE products (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            category TEXT NOT NULL,
            price REAL NOT NULL,
            stock INTEGER NOT NULL,
            is_deleted INTEGER NOT NULL DEFAULT 0
        );

        CREATE TABLE categories (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            code TEXT NOT NULL UNIQUE,
            name TEXT NOT NULL
        );

        INSERT INTO categories (code, name) VALUES ('Electronics', '电子产品');
        INSERT INTO categories (code, name) VALUES ('Books', '图书');
        INSERT INTO categories (code, name) VALUES ('Clothing', '服装');

        CREATE TABLE orders (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            user_id INTEGER NOT NULL,
            total_amount REAL NOT NULL,
            status TEXT NOT NULL,
            created_at TEXT NOT NULL,
            created_by TEXT NOT NULL,
            updated_at TEXT,
            updated_by TEXT
        );

        CREATE TABLE accounts (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            account_no TEXT NOT NULL,
            balance REAL NOT NULL,
            version INTEGER NOT NULL DEFAULT 0
        );

        CREATE TABLE logs (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            level TEXT NOT NULL,
            message TEXT NOT NULL,
            timestamp TEXT NOT NULL
        );
    ";

    cmd.ExecuteNonQuery();

    Console.WriteLine("   ✅ 数据库初始化完成");
    Console.WriteLine();

    return Task.CompletedTask;
}

static void PrintSection(string title)
{
    Console.WriteLine();
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine($"  {title}");
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
}

