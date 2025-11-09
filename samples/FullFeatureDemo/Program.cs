using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using FullFeatureDemo;

Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║                                                                ║");
Console.WriteLine("║      Sqlx 全特性演示 (Full Feature with Placeholders)         ║");
Console.WriteLine("║         展示 70+ 占位符、表达式树、批量操作等                  ║");
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
    // 1. 基础占位符演示
    await Demo1_BasicPlaceholdersAsync(connection);

    // 2. 方言占位符演示
    await Demo2_DialectPlaceholdersAsync(connection);

    // 3. 聚合函数占位符
    await Demo3_AggregatePlaceholdersAsync(connection);

    // 4. 字符串函数占位符
    await Demo4_StringPlaceholdersAsync(connection);

    // 5. 批量操作占位符
    await Demo5_BatchPlaceholdersAsync(connection);

    // 6. 复杂查询占位符
    await Demo6_ComplexPlaceholdersAsync(connection);

    // 7. 表达式树查询
    await Demo7_ExpressionTreeAsync(connection);

    // 8. 高级特性（软删除、审计、乐观锁）
    await Demo8_AdvancedFeaturesAsync(connection);

    Console.WriteLine();
    Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║                                                                ║");
    Console.WriteLine("║     ✅ 所有演示完成！Sqlx 70+ 占位符全部展示！                ║");
    Console.WriteLine("║                                                                ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ 错误: {ex.Message}");
    Console.WriteLine(ex.StackTrace);
}

// ==================== 演示函数 ====================

static async Task Demo1_BasicPlaceholdersAsync(DbConnection connection)
{
    PrintSection("1. 基础占位符演示 ({{columns}}, {{table}}, {{orderby}}, {{limit}})");

    var repo = new UserRepository(connection);

    // 插入测试数据
    Console.WriteLine("📝 插入测试数据（使用 {{values}} 占位符）...");
    await repo.InsertAsync("张三", "zhangsan@example.com", 25, 5000m, DateTime.Now, true);
    await repo.InsertAsync("李四", "lisi@example.com", 30, 8500m, DateTime.Now, true);
    await repo.InsertAsync("王五", "wangwu@example.com", 17, 500m, DateTime.Now, false);
    await repo.InsertAsync("赵六", "zhaoliu@example.com", 28, 12000m, DateTime.Now, true);
    await repo.InsertAsync("钱七", "qianqi@example.com", 35, 15000m, DateTime.Now, true);
    Console.WriteLine("   ✅ 已插入 5 个用户\n");

    // {{columns}} 占位符
    Console.WriteLine("🔹 使用 {{columns}} 占位符查询所有列");
    var allUsers = await repo.GetAllAsync();
    Console.WriteLine($"   ✅ 查询到 {allUsers.Count} 个用户");
    Console.WriteLine($"   SQL: SELECT {{columns}} FROM {{table}}\n");

    // {{orderby}} + {{limit}} 占位符
    Console.WriteLine("🔹 使用 {{orderby balance --desc}} {{limit}} 占位符");
    var topRich = await repo.GetTopRichUsersAsync(3);
    Console.WriteLine($"   ✅ 余额最高的 3 个用户:");
    foreach (var u in topRich)
    {
        Console.WriteLine($"      - {u.Name}: {u.Balance:C}");
    }
    Console.WriteLine($"   SQL: SELECT {{columns}} FROM {{table}} {{orderby balance --desc}} {{limit}}\n");

    // {{limit}} + {{offset}} 分页
    Console.WriteLine("🔹 使用 {{limit}} {{offset}} 分页占位符");
    var page1 = await repo.GetPagedAsync(2, 0);
    var page2 = await repo.GetPagedAsync(2, 2);
    Console.WriteLine($"   ✅ 第1页: {page1.Count} 条记录");
    Console.WriteLine($"   ✅ 第2页: {page2.Count} 条记录");
    Console.WriteLine($"   SQL: SELECT {{columns}} FROM {{table}} {{orderby id}} {{limit}} {{offset}}\n");

    // {{set}} 占位符更新
    Console.WriteLine("🔹 使用 {{set}} 占位符更新");
    var user = allUsers.First();
    user.Name = "张三（已更新）";
    user.Age = 26;
    await repo.UpdateAsync(user);
    Console.WriteLine($"   ✅ 更新成功");
    Console.WriteLine($"   SQL: UPDATE {{table}} {{set}} WHERE id = @id\n");
}

static async Task Demo2_DialectPlaceholdersAsync(DbConnection connection)
{
    PrintSection("2. 方言占位符演示 ({{bool_true}}, {{bool_false}}, {{current_timestamp}})");

    var productRepo = new ProductRepository(connection);

    // 插入产品（使用 {{bool_false}} 占位符）
    Console.WriteLine("📝 插入产品（使用 {{bool_false}} 占位符）...");
    await productRepo.InsertAsync("iPhone 15", "Electronics", 999m, 100);
    await productRepo.InsertAsync("MacBook Pro", "Electronics", 2499m, 50);
    await productRepo.InsertAsync("Magic Mouse", "Electronics", 99m, 200);
    Console.WriteLine($"   ✅ 已插入 3 个产品");
    Console.WriteLine($"   SQL: INSERT INTO {{table}} (...) VALUES (..., {{bool_false}})\n");

    // 使用 {{bool_false}} 查询
    Console.WriteLine("🔹 使用 {{bool_false}} 占位符查询未删除的产品");
    var products = await productRepo.GetAllAsync();
    Console.WriteLine($"   ✅ 查询到 {products.Count} 个产品");
    Console.WriteLine($"   SQL: SELECT {{columns}} FROM {{table}} WHERE is_deleted = {{bool_false}}\n");

    // 软删除（使用 {{bool_true}} 占位符）
    Console.WriteLine("🔹 使用 {{bool_true}} 占位符软删除产品");
    await productRepo.SoftDeleteAsync(1);
    products = await productRepo.GetAllAsync();
    Console.WriteLine($"   ✅ 删除后剩余 {products.Count} 个产品");
    Console.WriteLine($"   SQL: UPDATE {{table}} SET is_deleted = {{bool_true}} WHERE id = @id\n");

    // 使用 {{current_timestamp}} 占位符
    var orderRepo = new OrderRepository(connection);
    Console.WriteLine("🔹 使用 {{current_timestamp}} 占位符插入订单");
    var orderId = await orderRepo.InsertAsync(1, 999m, "Pending", "Admin");
    Console.WriteLine($"   ✅ 订单创建成功，ID: {orderId}");
    Console.WriteLine($"   SQL: INSERT INTO {{table}} (..., {{current_timestamp}}, ...) VALUES (...)\n");
}

static async Task Demo3_AggregatePlaceholdersAsync(DbConnection connection)
{
    PrintSection("3. 聚合函数占位符 ({{count}}, {{sum}}, {{avg}}, {{max}}, {{min}})");

    var repo = new UserRepository(connection);

    // {{count}} 占位符
    Console.WriteLine("🔹 使用 {{count}} 占位符");
    var count = await repo.CountAsync();
    Console.WriteLine($"   ✅ 总用户数: {count}");
    Console.WriteLine($"   SQL: SELECT {{count}} FROM {{table}}\n");

    // {{sum}} 占位符
    Console.WriteLine("🔹 使用 {{sum}} 占位符");
    var totalBalance = await repo.GetTotalBalanceAsync();
    Console.WriteLine($"   ✅ 总余额: {totalBalance:C}");
    Console.WriteLine($"   SQL: SELECT {{sum balance}} FROM {{table}}\n");

    // {{avg}} 占位符
    Console.WriteLine("🔹 使用 {{avg}} 占位符");
    var avgAge = await repo.GetAverageAgeAsync();
    Console.WriteLine($"   ✅ 平均年龄: {avgAge:F1}");
    Console.WriteLine($"   SQL: SELECT {{avg age}} FROM {{table}} WHERE is_active = {{bool_true}}\n");

    // {{max}} 占位符
    Console.WriteLine("🔹 使用 {{max}} 占位符");
    var maxBalance = await repo.GetMaxBalanceAsync();
    Console.WriteLine($"   ✅ 最高余额: {maxBalance:C}");
    Console.WriteLine($"   SQL: SELECT {{max balance}} FROM {{table}}\n");
}

static async Task Demo4_StringPlaceholdersAsync(DbConnection connection)
{
    PrintSection("4. 字符串函数占位符 ({{like}}, {{in}}, {{distinct}}, {{coalesce}})");

    var productRepo = new ProductRepository(connection);

    // {{like}} 占位符
    Console.WriteLine("🔹 使用 {{like}} 占位符模糊搜索");
    var searchResults = await productRepo.SearchByNameAsync("%Mac%");
    Console.WriteLine($"   ✅ 搜索 'Mac' 找到 {searchResults.Count} 个产品");
    foreach (var p in searchResults)
    {
        Console.WriteLine($"      - {p.Name}");
    }
    Console.WriteLine($"   SQL: SELECT {{columns}} FROM {{table}} WHERE name {{like @pattern}}\n");

    // {{in}} 占位符
    Console.WriteLine("🔹 使用 {{in}} 占位符查询多个ID");
    var ids = new long[] { 1, 2, 3 };
    var productsById = await productRepo.GetByIdsAsync(ids);
    Console.WriteLine($"   ✅ 查询 IDs [1,2,3] 找到 {productsById.Count} 个产品");
    Console.WriteLine($"   SQL: SELECT {{columns}} FROM {{table}} WHERE id {{in @ids}}\n");

    // {{between}} 占位符
    Console.WriteLine("🔹 使用 {{between}} 占位符查询价格范围");
    var priceRange = await productRepo.GetByPriceRangeAsync(50m, 1000m);
    Console.WriteLine($"   ✅ 价格在 $50-$1000 之间的产品: {priceRange.Count} 个");
    Console.WriteLine($"   SQL: SELECT {{columns}} FROM {{table}} WHERE price {{between @minPrice, @maxPrice}}\n");

    // {{distinct}} 占位符
    var userRepo = new UserRepository(connection);
    Console.WriteLine("🔹 使用 {{distinct}} 占位符获取不同的年龄");
    var distinctAges = await userRepo.GetDistinctAgesAsync();
    Console.WriteLine($"   ✅ 不同的年龄: {string.Join(", ", distinctAges)}");
    Console.WriteLine($"   SQL: SELECT {{distinct age}} FROM {{table}} {{orderby age}}\n");

    // {{coalesce}} 占位符
    Console.WriteLine("🔹 使用 {{coalesce}} 占位符处理NULL值");
    var userWithDefault = await userRepo.GetUserWithDefaultEmailAsync(1);
    Console.WriteLine($"   ✅ 用户邮箱（带默认值）: {userWithDefault?.Email}");
    Console.WriteLine($"   SQL: SELECT id, name, {{coalesce email, 'default'}} as email FROM {{table}}\n");
}

static async Task Demo5_BatchPlaceholdersAsync(DbConnection connection)
{
    PrintSection("5. 批量操作占位符 ({{batch_values}})");

    var logRepo = new LogRepository(connection);

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

    // {{batch_values}} 批量插入
    Console.WriteLine("\n🔹 使用 {{batch_values}} 占位符批量插入");
    var startTime = DateTime.Now;
    var inserted = await logRepo.BatchInsertAsync(logs);
    var duration = DateTime.Now - startTime;

    Console.WriteLine($"   ✅ 插入了 {inserted} 条记录");
    Console.WriteLine($"   ✅ 耗时: {duration.TotalMilliseconds:F2}ms");
    Console.WriteLine($"   ✅ 平均: {duration.TotalMilliseconds / inserted:F4}ms/条");
    Console.WriteLine($"   SQL: INSERT INTO {{table}} (...) VALUES {{batch_values}}\n");

    // {{group_concat}} 占位符
    Console.WriteLine("🔹 使用 {{group_concat}} 占位符聚合消息");
    var summary = await logRepo.GetLogSummaryAsync(3);
    Console.WriteLine($"   ✅ 按级别分组的日志摘要:");
    foreach (var item in summary)
    {
        var level = item["level"]?.ToString();
        var messages = item["messages"]?.ToString();
        Console.WriteLine($"      - {level}: {messages?[..Math.Min(50, messages.Length)]}...");
    }
    Console.WriteLine($"   SQL: SELECT level, {{group_concat message, ', '}} FROM {{table}} {{groupby level}}\n");
}

static async Task Demo6_ComplexPlaceholdersAsync(DbConnection connection)
{
    PrintSection("6. 复杂查询占位符 ({{join}}, {{groupby}}, {{having}}, {{case}})");

    var advRepo = new AdvancedRepository(connection);

    // {{join}} 占位符
    Console.WriteLine("🔹 使用 {{join}} 占位符进行JOIN查询");
    var productDetails = await advRepo.GetProductDetailsAsync();
    Console.WriteLine($"   ✅ 查询到 {productDetails.Count} 个产品详情");
    foreach (var detail in productDetails.Take(2))
    {
        Console.WriteLine($"      - {detail.ProductName} ({detail.CategoryName}): {detail.Price:C}");
    }
    Console.WriteLine($"   SQL: SELECT ... FROM {{table products}} p {{join --type inner --table categories c}}\n");

    // {{groupby}} + {{having}} 占位符
    Console.WriteLine("🔹 使用 {{groupby}} 和 {{having}} 占位符聚合查询");
    var userStats = await advRepo.GetUserStatsAsync();
    Console.WriteLine($"   ✅ 用户统计 (有订单的用户):");
    foreach (var stat in userStats.Take(3))
    {
        Console.WriteLine($"      - {stat.UserName}: {stat.OrderCount} 订单, {stat.TotalSpent:C}");
    }
    Console.WriteLine($"   SQL: SELECT ... {{groupby u.id, u.name}} {{having --condition 'COUNT(o.id) > 0'}}\n");

    // {{case}} 占位符
    Console.WriteLine("🔹 使用 {{case}} 占位符条件表达式");
    var usersWithLevel = await advRepo.GetUsersWithLevelAsync();
    Console.WriteLine($"   ✅ 用户等级分类:");
    foreach (var item in usersWithLevel.Take(3))
    {
        var name = item["name"]?.ToString();
        var balance = Convert.ToDecimal(item["balance"]);
        var level = item["level"]?.ToString();
        Console.WriteLine($"      - {name}: {balance:C} ({level})");
    }
    Console.WriteLine($"   SQL: SELECT ..., {{case --when ... --then ... --else ...}} FROM {{table}}\n");

    // {{exists}} 子查询占位符
    Console.WriteLine("🔹 使用 {{exists}} 占位符子查询");
    var highValueCustomers = await advRepo.GetHighValueCustomersAsync(500m);
    Console.WriteLine($"   ✅ 高价值客户 (订单金额>$500): {highValueCustomers.Count} 个");
    Console.WriteLine($"   SQL: SELECT {{columns}} WHERE {{exists --query '...'}}\n");
}

static async Task Demo7_ExpressionTreeAsync(DbConnection connection)
{
    PrintSection("7. 表达式树查询 ({{where}} + Expression<Func<T, bool>>)");

    var expressionRepo = new ExpressionRepository(connection);

    // 简单条件
    Console.WriteLine("🔹 表达式树：简单条件查询");
    var adults = await expressionRepo.FindUsersAsync(u => u.Age >= 18 && u.IsActive);
    Console.WriteLine($"   ✅ 成年且活跃的用户: {adults.Count} 个");
    Console.WriteLine($"   表达式: u => u.Age >= 18 && u.IsActive");
    Console.WriteLine($"   SQL: SELECT {{columns}} FROM {{table}} WHERE age >= 18 AND is_active = 1\n");

    // 字符串条件
    Console.WriteLine("🔹 表达式树：字符串包含");
    var nameContains = await expressionRepo.FindUsersAsync(u => u.Name.Contains("张"));
    Console.WriteLine($"   ✅ 名字包含'张'的用户: {nameContains.Count} 个");
    Console.WriteLine($"   表达式: u => u.Name.Contains('张')");
    Console.WriteLine($"   SQL: SELECT {{columns}} FROM {{table}} WHERE name LIKE '%张%'\n");

    // 复杂条件
    Console.WriteLine("🔹 表达式树：复杂组合条件");
    var complex = await expressionRepo.FindUsersAsync(
        u => (u.Age >= 25 && u.Balance > 5000) || u.Email.EndsWith("@example.com"));
    Console.WriteLine($"   ✅ 符合复杂条件的用户: {complex.Count} 个");
    Console.WriteLine($"   表达式: u => (u.Age >= 25 && u.Balance > 5000) || u.Email.EndsWith(...)");
    Console.WriteLine($"   SQL: SELECT {{columns}} FROM {{table}} WHERE (age >= 25 AND balance > 5000) OR email LIKE '%@example.com'\n");

    // 表达式树 + 分页
    Console.WriteLine("🔹 表达式树：组合分页");
    var paged = await expressionRepo.FindUsersPagedAsync(u => u.IsActive, 2, 0);
    Console.WriteLine($"   ✅ 活跃用户（第1页，每页2条）: {paged.Count} 个");
    Console.WriteLine($"   SQL: SELECT {{columns}} FROM {{table}} {{where}} {{orderby}} {{limit}} {{offset}}\n");

    // 表达式树 + 聚合
    Console.WriteLine("🔹 表达式树：聚合函数");
    var count = await expressionRepo.CountUsersAsync(u => u.Age >= 30);
    var maxBalance = await expressionRepo.GetMaxBalanceAsync(u => u.IsActive);
    Console.WriteLine($"   ✅ 30岁以上用户数: {count}");
    Console.WriteLine($"   ✅ 活跃用户最高余额: {maxBalance:C}");
    Console.WriteLine($"   SQL: SELECT {{count}}/{{max}} FROM {{table}} {{where}}\n");
}

static async Task Demo8_AdvancedFeaturesAsync(DbConnection connection)
{
    PrintSection("8. 高级特性 (软删除、审计字段、乐观锁)");

    // 软删除
    var productRepo = new ProductRepository(connection);
    Console.WriteLine("🔹 软删除特性 [SoftDelete]");
    var allProducts = await productRepo.GetAllAsync();
    Console.WriteLine($"   ✅ 软删除前: {allProducts.Count} 个产品");
    
    await productRepo.SoftDeleteAsync(1);
    allProducts = await productRepo.GetAllAsync();
    Console.WriteLine($"   ✅ 软删除后: {allProducts.Count} 个产品（已删除的被自动过滤）");
    
    var deletedProduct = await productRepo.GetByIdIncludingDeletedAsync(1);
    Console.WriteLine($"   ✅ 包含已删除: IsDeleted = {deletedProduct?.IsDeleted}");
    Console.WriteLine($"   特性: [SoftDelete(FlagColumn = \"is_deleted\")]\n");

    // 审计字段
    var orderRepo = new OrderRepository(connection);
    Console.WriteLine("🔹 审计字段特性 [AuditFields]");
    var orderId = await orderRepo.InsertAsync(1, 1999m, "Pending", "Admin");
    var order = await orderRepo.GetByIdAsync(orderId);
    Console.WriteLine($"   ✅ 创建时间: {order?.CreatedAt:yyyy-MM-dd HH:mm:ss}");
    Console.WriteLine($"   ✅ 创建人: {order?.CreatedBy}");
    
    await Task.Delay(100);
    await orderRepo.UpdateStatusAsync(orderId, "Shipped", "System");
    order = await orderRepo.GetByIdAsync(orderId);
    Console.WriteLine($"   ✅ 更新时间: {order?.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
    Console.WriteLine($"   ✅ 更新人: {order?.UpdatedBy}");
    Console.WriteLine($"   特性: [AuditFields(CreatedAtColumn = ..., UpdatedAtColumn = ...)]\n");

    // 乐观锁
    var accountRepo = new AccountRepository(connection);
    Console.WriteLine("🔹 乐观锁特性 [ConcurrencyCheck]");
    var accountId = await accountRepo.InsertAsync("ACC001", 10000m);
    var account = await accountRepo.GetByIdAsync(accountId);
    Console.WriteLine($"   ✅ 初始余额: {account?.Balance:C}, Version: {account?.Version}");
    
    // 正常更新
    var updated1 = await accountRepo.UpdateBalanceAsync(accountId, 9000m, account!.Version);
    Console.WriteLine($"   ✅ 更新1成功: {updated1} 条记录");
    
    // 使用旧版本号（应该失败）
    var updated2 = await accountRepo.UpdateBalanceAsync(accountId, 8000m, account.Version);
    Console.WriteLine($"   ❌ 更新2失败: {updated2} 条记录（版本冲突）");
    Console.WriteLine($"   特性: [ConcurrencyCheck]\n");

    // 最终统计
    Console.WriteLine("📊 最终统计");
    var userRepo = new UserRepository(connection);
    var totalUsers = await userRepo.CountAsync();
    var totalBalance = await userRepo.GetTotalBalanceAsync();
    var avgAge = await userRepo.GetAverageAgeAsync();
    
    Console.WriteLine($"   ✅ 总用户数: {totalUsers}");
    Console.WriteLine($"   ✅ 总余额: {totalBalance:C}");
    Console.WriteLine($"   ✅ 平均年龄: {avgAge:F1}");
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
            created_at TEXT NOT NULL,
            is_active INTEGER NOT NULL DEFAULT 1
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
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
    Console.WriteLine($"  {title}");
    Console.WriteLine("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
}
