# Migration Guide to Sqlx

> **从其他ORM迁移到Sqlx的完整指南**

---

## 📋 目录

- [从Entity Framework Core迁移](#从entity-framework-core迁移)
- [从Dapper迁移](#从dapper迁移)
- [从ADO.NET迁移](#从adonet迁移)
- [混合使用策略](#混合使用策略)
- [迁移检查清单](#迁移检查清单)
- [常见陷阱](#常见陷阱)

---

## 从Entity Framework Core迁移

### 为什么迁移？

**从EF Core迁移到Sqlx的理由:**
```
✅ 性能提升 75% (60% → 105%)
✅ 完全SQL控制
✅ 编译时安全
✅ 零运行时开销
✅ 更简单的代码库
```

---

### 基础CRUD操作对比

#### 查询单个实体

**EF Core:**
```csharp
var user = await _dbContext.Users
    .FirstOrDefaultAsync(u => u.Id == id);
```

**Sqlx:**
```csharp
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
Task<User?> GetByIdAsync(int id);

// 使用:
var user = await _userRepo.GetByIdAsync(id);
```

**或使用预定义方法:**
```csharp
public partial interface IUserRepository : IQueryRepository<User, int>
{
    // 自动提供:
    // Task<User?> GetByIdAsync(int id);
}

var user = await _userRepo.GetByIdAsync(id);
```

---

#### 查询列表

**EF Core:**
```csharp
var activeUsers = await _dbContext.Users
    .Where(u => u.IsActive)
    .ToListAsync();
```

**Sqlx:**
```csharp
[SqlTemplate("SELECT * FROM users WHERE is_active = 1")]
Task<List<User>> GetActiveUsersAsync();

// 使用:
var activeUsers = await _userRepo.GetActiveUsersAsync();
```

**使用预定义方法:**
```csharp
public partial interface IUserRepository : IQueryRepository<User, int>
{
    // Task<List<User>> GetWhereAsync(string condition, object parameters);
}

var activeUsers = await _userRepo.GetWhereAsync(
    "is_active = @active", 
    new { active = true }
);
```

---

#### 插入

**EF Core:**
```csharp
_dbContext.Users.Add(newUser);
await _dbContext.SaveChangesAsync();
```

**Sqlx:**
```csharp
[SqlTemplate("INSERT INTO users (name, email) VALUES (@name, @email)")]
[ReturnInsertedId]
Task<int> InsertAsync(string name, string email);

// 使用:
var id = await _userRepo.InsertAsync(user.Name, user.Email);
```

**或使用实体对象:**
```csharp
public partial interface IUserRepository : ICommandRepository<User, int>
{
    // Task<int> InsertAsync(User entity);
}

var id = await _userRepo.InsertAsync(newUser);
```

---

#### 更新

**EF Core:**
```csharp
var user = await _dbContext.Users.FindAsync(id);
user.Name = "New Name";
await _dbContext.SaveChangesAsync();
```

**Sqlx:**
```csharp
[SqlTemplate("UPDATE users SET name = @name WHERE id = @id")]
Task<int> UpdateNameAsync(int id, string name);

// 使用:
await _userRepo.UpdateNameAsync(id, "New Name");
```

**或更新整个实体:**
```csharp
public partial interface IUserRepository : ICommandRepository<User, int>
{
    // Task<int> UpdateAsync(User entity);
}

user.Name = "New Name";
await _userRepo.UpdateAsync(user);
```

---

#### 删除

**EF Core:**
```csharp
var user = await _dbContext.Users.FindAsync(id);
_dbContext.Users.Remove(user);
await _dbContext.SaveChangesAsync();
```

**Sqlx:**
```csharp
[SqlTemplate("DELETE FROM users WHERE id = @id")]
Task<int> DeleteAsync(int id);

// 使用:
await _userRepo.DeleteAsync(id);
```

**使用预定义方法:**
```csharp
public partial interface IUserRepository : ICommandRepository<User, int>
{
    // Task<int> DeleteAsync(int id);
}

await _userRepo.DeleteAsync(id);
```

---

### 高级查询对比

#### Include / Join

**EF Core:**
```csharp
var orders = await _dbContext.Orders
    .Include(o => o.Customer)
    .Include(o => o.Items)
    .ToListAsync();
```

**Sqlx:**
```csharp
[SqlTemplate(@"
    SELECT 
        o.*, 
        c.name as customer_name,
        c.email as customer_email
    FROM orders o
    INNER JOIN customers c ON o.customer_id = c.id
    WHERE o.created_at > @since
")]
Task<List<OrderWithCustomer>> GetOrdersWithCustomerAsync(DateTime since);

// 或使用多次查询
var orders = await _orderRepo.GetAllAsync();
var customerIds = orders.Select(o => o.CustomerId).Distinct();
var customers = await _customerRepo.GetByIdsAsync(customerIds);
```

**注意:** Sqlx不支持自动导航属性，但你可以：
1. 使用SQL JOIN
2. 手动进行多次查询
3. 创建视图模型

---

#### 分页

**EF Core:**
```csharp
var users = await _dbContext.Users
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();

var total = await _dbContext.Users.CountAsync();
```

**Sqlx:**
```csharp
public partial interface IUserRepository : IQueryRepository<User, int>
{
    // Task<PagedResult<User>> GetPageAsync(int pageIndex, int pageSize);
}

// 使用:
var page = await _userRepo.GetPageAsync(pageIndex: 1, pageSize: 20);

Console.WriteLine($"Total: {page.TotalCount}");
Console.WriteLine($"Pages: {page.TotalPages}");
Console.WriteLine($"Items: {page.Items.Count}");
```

---

#### 聚合

**EF Core:**
```csharp
var count = await _dbContext.Users.CountAsync();
var avgAge = await _dbContext.Users.AverageAsync(u => u.Age);
var maxAge = await _dbContext.Users.MaxAsync(u => u.Age);
```

**Sqlx:**
```csharp
public partial interface IUserRepository : IAggregateRepository<User, int>
{
    // Task<int> CountAsync();
    // Task<double> AvgAsync<TResult>(Expression<Func<User, TResult>> selector);
    // Task<TResult> MaxAsync<TResult>(Expression<Func<User, TResult>> selector);
}

var count = await _userRepo.CountAsync();
var avgAge = await _userRepo.AvgAsync(u => u.Age);
var maxAge = await _userRepo.MaxAsync(u => u.Age);
```

---

### 事务

**EF Core:**
```csharp
using (var transaction = await _dbContext.Database.BeginTransactionAsync())
{
    try
    {
        _dbContext.Users.Add(user);
        _dbContext.Orders.Add(order);
        await _dbContext.SaveChangesAsync();
        
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

**Sqlx:**
```csharp
using (var connection = new SqlConnection(connectionString))
{
    await connection.OpenAsync();
    using (var transaction = connection.BeginTransaction())
    {
        try
        {
            await _userRepo.InsertAsync(user, transaction);
            await _orderRepo.InsertAsync(order, transaction);
            
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
```

---

### Change Tracking

**EF Core有，Sqlx没有！**

**EF Core:**
```csharp
var user = await _dbContext.Users.FindAsync(id);
user.Name = "New Name";  // EF追踪变化
await _dbContext.SaveChangesAsync();  // 自动生成UPDATE
```

**Sqlx:**
```csharp
// 必须显式更新
var user = await _userRepo.GetByIdAsync(id);
user.Name = "New Name";
await _userRepo.UpdateAsync(user);  // 显式调用UPDATE
```

**原因:** Change Tracking有性能开销，Sqlx追求极致性能。

**替代方案:**
1. 使用部分更新
2. 实现自己的变更跟踪（如果需要）
3. 使用乐观并发（`[ConcurrencyCheck]`）

---

### 迁移策略

#### 策略1: 逐步迁移（推荐）

```csharp
// 1. 保留EF Core用于复杂查询
public class OrderService
{
    private readonly AppDbContext _dbContext;
    private readonly IOrderRepository _orderRepo;  // Sqlx

    public async Task<Order> GetOrderDetailsAsync(int id)
    {
        // 复杂导航属性 - 用EF Core
        return await _dbContext.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<List<Order>> GetRecentOrdersAsync(int days)
    {
        // 简单高性能查询 - 用Sqlx
        return await _orderRepo.GetRecentOrdersAsync(days);
    }
}
```

**优点:**
- ✅ 降低风险
- ✅ 保留EF Core优势
- ✅ 获得Sqlx性能

**适用:**
- 大型现有项目
- 混合需求
- 渐进式迁移

---

#### 策略2: 完全迁移

```csharp
// 1. 创建Sqlx Repository
public partial interface IUserRepository : IRepository<User, int>
{
    [SqlTemplate("SELECT * FROM users WHERE department_id = @deptId")]
    Task<List<User>> GetByDepartmentAsync(int deptId);
}

// 2. 替换EF Core调用
// Before:
var users = await _dbContext.Users
    .Where(u => u.DepartmentId == deptId)
    .ToListAsync();

// After:
var users = await _userRepo.GetByDepartmentAsync(deptId);

// 3. 移除DbContext依赖
```

**优点:**
- ✅ 最大性能提升
- ✅ 简化代码库
- ✅ 完全SQL控制

**适用:**
- 新项目
- 小型项目
- 性能关键项目

---

### 不支持的EF Core功能

#### ❌ 导航属性
```csharp
// EF Core:
public class Order
{
    public Customer Customer { get; set; }  // 自动加载
    public List<OrderItem> Items { get; set; }
}

// Sqlx: 手动加载或使用JOIN
public class OrderWithCustomer
{
    // Order properties
    public int Id { get; set; }
    // Customer properties
    public string CustomerName { get; set; }
}
```

#### ❌ 自动迁移
```csharp
// EF Core:
dotnet ef migrations add AddUser
dotnet ef database update

// Sqlx: 手动管理或使用工具
// - FluentMigrator
// - DbUp
// - 直接SQL脚本
```

#### ❌ Change Tracking
```csharp
// EF Core: 自动跟踪
user.Name = "New";
await _dbContext.SaveChangesAsync();

// Sqlx: 显式更新
user.Name = "New";
await _repo.UpdateAsync(user);
```

#### ❌ Lazy Loading
```csharp
// EF Core:
var order = await _dbContext.Orders.FindAsync(id);
var customer = order.Customer;  // 延迟加载

// Sqlx: 显式查询
var order = await _orderRepo.GetByIdAsync(id);
var customer = await _customerRepo.GetByIdAsync(order.CustomerId);
```

---

## 从Dapper迁移

### 为什么迁移？

**从Dapper迁移到Sqlx的理由:**
```
✅ 编译时类型安全
✅ 自动代码生成
✅ IntelliSense支持
✅ 占位符系统
✅ VS工具链
✅ 相同性能
```

---

### 基本查询对比

#### 查询单个

**Dapper:**
```csharp
using (var conn = new SqlConnection(connectionString))
{
    var sql = "SELECT * FROM users WHERE id = @id";
    var user = await conn.QueryFirstOrDefaultAsync<User>(sql, new { id });
    return user;
}
```

**Sqlx:**
```csharp
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
Task<User?> GetByIdAsync(int id);

// 自动生成上面的Dapper代码！
```

**优势:**
- ✅ 编译时检查SQL参数
- ✅ 自动类型推断
- ✅ 无需手写Dapper代码

---

#### 查询列表

**Dapper:**
```csharp
using (var conn = new SqlConnection(connectionString))
{
    var sql = "SELECT * FROM users WHERE is_active = @active";
    var users = await conn.QueryAsync<User>(sql, new { active = true });
    return users.ToList();
}
```

**Sqlx:**
```csharp
[SqlTemplate("SELECT * FROM users WHERE is_active = @active")]
Task<List<User>> GetActiveUsersAsync(bool active);

// 自动生成，更简洁！
```

---

#### 插入并返回ID

**Dapper:**
```csharp
using (var conn = new SqlConnection(connectionString))
{
    var sql = @"
        INSERT INTO users (name, email) VALUES (@name, @email);
        SELECT CAST(SCOPE_IDENTITY() as int)";
    
    var id = await conn.QuerySingleAsync<int>(sql, new { name, email });
    return id;
}
```

**Sqlx:**
```csharp
[SqlTemplate("INSERT INTO users (name, email) VALUES (@name, @email)")]
[ReturnInsertedId]
Task<int> InsertAsync(string name, string email);

// 自动处理ID返回！
```

---

### 高级功能对比

#### 批量操作

**Dapper (手动):**
```csharp
using (var conn = new SqlConnection(connectionString))
{
    await conn.OpenAsync();
    using (var transaction = conn.BeginTransaction())
    {
        foreach (var user in users)
        {
            await conn.ExecuteAsync(
                "INSERT INTO users (name, email) VALUES (@name, @email)",
                user,
                transaction
            );
        }
        transaction.Commit();
    }
}
```

**Sqlx (内置):**
```csharp
public partial interface IUserRepository : IBatchRepository<User, int>
{
    // Task BatchInsertAsync(IEnumerable<User> entities);
}

await _userRepo.BatchInsertAsync(users);  // 25倍快！
```

---

#### 动态SQL

**Dapper:**
```csharp
var sql = "SELECT * FROM users WHERE 1=1";
var parameters = new DynamicParameters();

if (!string.IsNullOrEmpty(name))
{
    sql += " AND name = @name";
    parameters.Add("name", name);
}

if (minAge.HasValue)
{
    sql += " AND age >= @minAge";
    parameters.Add("minAge", minAge);
}

var users = await conn.QueryAsync<User>(sql, parameters);
```

**Sqlx:**
```csharp
[SqlTemplate(@"
    SELECT * FROM users 
    WHERE 1=1
    {{where}}
")]
Task<List<User>> SearchAsync(string? where = null);

// 使用:
await _repo.SearchAsync("AND name = 'John' AND age >= 18");

// 或使用占位符:
[SqlTemplate("SELECT * FROM users WHERE {{where}}")]
Task<List<User>> SearchAsync(string where);
```

---

### 迁移步骤

#### 步骤1: 识别Repository模式

**Dapper常见模式:**
```csharp
public class UserRepository
{
    private readonly string _connectionString;

    public async Task<User> GetByIdAsync(int id)
    {
        using (var conn = new SqlConnection(_connectionString))
        {
            return await conn.QueryFirstOrDefaultAsync<User>(
                "SELECT * FROM users WHERE id = @id",
                new { id }
            );
        }
    }
}
```

**转换为Sqlx:**
```csharp
[RepositoryFor(typeof(User))]
[SqlDefine(SqlDialect.SqlServer)]
public partial interface IUserRepository
{
    [SqlTemplate("SELECT * FROM users WHERE id = @id")]
    Task<User?> GetByIdAsync(int id);
}

// 实现自动生成！
```

---

#### 步骤2: 替换手写查询

**Dapper:**
```csharp
public class UserService
{
    private readonly UserRepository _repo;

    public async Task<User> GetUserAsync(int id)
    {
        return await _repo.GetByIdAsync(id);  // 手写的Dapper代码
    }
}
```

**Sqlx:**
```csharp
public class UserService
{
    private readonly IUserRepository _repo;

    public async Task<User> GetUserAsync(int id)
    {
        return await _repo.GetByIdAsync(id);  // 自动生成的代码！
    }
}
```

---

#### 步骤3: 利用新功能

**使用预定义方法:**
```csharp
// Dapper: 所有方法都要手写
public class UserRepository
{
    public Task<User> GetByIdAsync(int id) { ... }
    public Task<List<User>> GetAllAsync() { ... }
    public Task<List<User>> GetPageAsync(int page, int size) { ... }
    public Task<int> CountAsync() { ... }
    // ... 50+个方法要手写
}

// Sqlx: 自动提供50+方法！
public partial interface IUserRepository : IRepository<User, int>
{
    // 自动提供:
    // - GetByIdAsync
    // - GetAllAsync
    // - GetPageAsync
    // - CountAsync
    // - BatchInsertAsync
    // - ... 50+方法
}
```

---

## 从ADO.NET迁移

### 基本查询对比

**ADO.NET:**
```csharp
using (var conn = new SqlConnection(connectionString))
{
    await conn.OpenAsync();
    using (var cmd = new SqlCommand("SELECT * FROM users WHERE id = @id", conn))
    {
        cmd.Parameters.AddWithValue("@id", id);
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                return new User
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Email = reader.GetString(2),
                    // ... 手动映射
                };
            }
        }
    }
}
```

**Sqlx:**
```csharp
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
Task<User?> GetByIdAsync(int id);

// 自动映射！
```

**代码减少:** 95%！

---

## 混合使用策略

### 场景1: Sqlx + EF Core

```csharp
public class OrderService
{
    private readonly AppDbContext _dbContext;        // EF Core
    private readonly IOrderRepository _orderRepo;    // Sqlx

    // 复杂查询用EF Core
    public async Task<OrderDetailsDto> GetOrderDetailsAsync(int id)
    {
        return await _dbContext.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Select(o => new OrderDetailsDto { ... })
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    // 高性能查询用Sqlx
    public async Task<List<OrderSummary>> GetDashboardDataAsync()
    {
        return await _orderRepo.GetDashboardDataAsync();
    }

    // 批量操作用Sqlx
    public async Task ImportOrdersAsync(List<Order> orders)
    {
        await _orderRepo.BatchInsertAsync(orders);  // 25倍快
    }
}
```

**最佳实践:**
- ✅ 复杂导航 → EF Core
- ✅ 高性能查询 → Sqlx
- ✅ 批量操作 → Sqlx
- ✅ 报表统计 → Sqlx

---

### 场景2: Sqlx + Dapper

```csharp
public class DataService
{
    private readonly IUserRepository _userRepo;  // Sqlx
    private readonly IDbConnection _connection;  // Dapper

    // 常规查询用Sqlx (类型安全)
    public async Task<User> GetUserAsync(int id)
    {
        return await _userRepo.GetByIdAsync(id);
    }

    // 极度动态的查询用Dapper
    public async Task<IEnumerable<dynamic>> ExecuteDynamicQueryAsync(string sql)
    {
        return await _connection.QueryAsync(sql);
    }
}
```

---

## 迁移检查清单

### 准备阶段
```
☐ 阅读Sqlx文档
☐ 理解差异
☐ 评估项目复杂度
☐ 确定迁移策略 (全部/部分)
☐ 设置测试环境
```

### 迁移阶段
```
☐ 安装Sqlx和Sqlx.Generator
☐ 创建第一个Repository接口
☐ 测试基本CRUD
☐ 迁移简单查询
☐ 迁移复杂查询
☐ 迁移批量操作
☐ 处理特殊情况
☐ 性能测试
```

### 验证阶段
```
☐ 单元测试通过
☐ 集成测试通过
☐ 性能基准测试
☐ 代码审查
☐ 文档更新
```

### 清理阶段
```
☐ 移除旧ORM依赖 (如果完全迁移)
☐ 清理未使用代码
☐ 更新CI/CD
☐ 团队培训
```

---

## 常见陷阱

### ❌ 陷阱1: 期望自动导航属性

```csharp
// ❌ 错误期望
public class Order
{
    public Customer Customer { get; set; }  // Sqlx不会自动加载
}

var order = await _repo.GetByIdAsync(1);
var customerName = order.Customer.Name;  // NullReferenceException!

// ✅ 正确做法
[SqlTemplate(@"
    SELECT o.*, c.name as customer_name
    FROM orders o
    JOIN customers c ON o.customer_id = c.id
    WHERE o.id = @id
")]
Task<OrderWithCustomer> GetOrderWithCustomerAsync(int id);
```

---

### ❌ 陷阱2: 忘记处理NULL

```csharp
// ❌ 可能出错
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
Task<User> GetByIdAsync(int id);  // 找不到时怎么办？

var user = await _repo.GetByIdAsync(999);
var name = user.Name;  // NullReferenceException!

// ✅ 正确
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
Task<User?> GetByIdAsync(int id);  // 可空

var user = await _repo.GetByIdAsync(999);
if (user != null)
{
    var name = user.Name;
}
```

---

### ❌ 陷阱3: SQL注入

```csharp
// ❌ 危险！
[SqlTemplate($"SELECT * FROM users WHERE name = '{name}'")]  // SQL注入!

// ✅ 安全
[SqlTemplate("SELECT * FROM users WHERE name = @name")]
Task<User?> GetByNameAsync(string name);
```

---

### ❌ 陷阱4: 连接泄漏

```csharp
// ❌ 泄漏
var conn = new SqlConnection(connectionString);
var repo = new UserRepository(conn);
// 使用repo...
// 忘记释放conn

// ✅ 正确
using (var conn = new SqlConnection(connectionString))
{
    var repo = new UserRepository(conn);
    // 使用repo...
}  // 自动释放

// ✅ 更好: 依赖注入
services.AddScoped<IDbConnection>(sp => 
    new SqlConnection(connectionString));
```

---

## 迁移示例项目

### 完整示例: 从EF Core迁移

**Before (EF Core):**
```csharp
public class UserService
{
    private readonly AppDbContext _dbContext;

    public async Task<User> GetUserAsync(int id)
    {
        return await _dbContext.Users.FindAsync(id);
    }

    public async Task<List<User>> GetActiveUsersAsync()
    {
        return await _dbContext.Users
            .Where(u => u.IsActive)
            .ToListAsync();
    }

    public async Task<int> CreateUserAsync(User user)
    {
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        return user.Id;
    }
}
```

**After (Sqlx):**
```csharp
// Repository定义
[RepositoryFor(typeof(User))]
[SqlDefine(SqlDialect.SqlServer)]
public partial interface IUserRepository : IRepository<User, int>
{
    [SqlTemplate("SELECT * FROM users WHERE is_active = 1")]
    Task<List<User>> GetActiveUsersAsync();
}

// Service使用
public class UserService
{
    private readonly IUserRepository _userRepo;

    public async Task<User> GetUserAsync(int id)
    {
        return await _userRepo.GetByIdAsync(id);  // 预定义方法
    }

    public async Task<List<User>> GetActiveUsersAsync()
    {
        return await _userRepo.GetActiveUsersAsync();
    }

    public async Task<int> CreateUserAsync(User user)
    {
        return await _userRepo.InsertAsync(user);  // 预定义方法
    }
}
```

**结果:**
- ✅ 代码更简洁
- ✅ 性能提升 75%
- ✅ 编译时安全
- ✅ 完全SQL控制

---

## 📚 更多资源

- **完整教程**: [TUTORIAL.md](TUTORIAL.md)
- **API参考**: [docs/API_REFERENCE.md](docs/API_REFERENCE.md)
- **最佳实践**: [docs/BEST_PRACTICES.md](docs/BEST_PRACTICES.md)
- **FAQ**: [FAQ.md](FAQ.md)
- **故障排除**: [TROUBLESHOOTING.md](TROUBLESHOOTING.md)

---

## 🆘 需要帮助？

**有迁移问题？**
- 📝 查看FAQ: [FAQ.md](FAQ.md)
- 🐛 报告问题: [GitHub Issues](https://github.com/Cricle/Sqlx/issues)
- 💬 讨论: [GitHub Discussions](https://github.com/Cricle/Sqlx/discussions)

---

**准备好迁移了吗？** 🚀

**开始享受Sqlx的性能和简洁！** 😊


