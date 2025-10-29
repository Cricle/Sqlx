# 🚀 Sqlx Developer Cheat Sheet

> **开发者速查手册 - 一页纸掌握所有常用命令和技巧**

---

## 📦 快速安装

```bash
# 安装核心库
dotnet add package Sqlx
dotnet add package Sqlx.Generator

# 验证安装
dotnet list package | grep Sqlx
```

**VS Extension**: 从 [GitHub Releases](https://github.com/Cricle/Sqlx/releases) 下载 VSIX

---

## 🎯 基础用法

### 1. 定义实体

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
}
```

### 2. 定义Repository

```csharp
using Sqlx;
using Sqlx.Annotations;

[SqlDefine(SqlDialect.SQLite)]
public interface IUserRepository
{
    // 查询单个
    [SqlTemplate("SELECT * FROM users WHERE id = @id")]
    Task<User?> GetByIdAsync(int id);
    
    // 查询列表
    [SqlTemplate("SELECT * FROM users WHERE age >= @minAge")]
    Task<List<User>> GetAdultsAsync(int minAge);
    
    // 插入
    [SqlTemplate("INSERT INTO users (name, email, age) VALUES (@name, @email, @age)")]
    [ReturnInsertedId]
    Task<int> InsertAsync(string name, string email, int age);
    
    // 更新
    [SqlTemplate("UPDATE users SET name = @name WHERE id = @id")]
    Task<int> UpdateAsync(int id, string name);
    
    // 删除
    [SqlTemplate("DELETE FROM users WHERE id = @id")]
    Task<int> DeleteAsync(int id);
}
```

### 3. 使用Repository

```csharp
// 依赖注入
services.AddScoped<IDbConnection>(sp => 
    new SqliteConnection("Data Source=app.db"));
services.AddScoped<IUserRepository, UserRepository>();

// 使用
public class UserService
{
    private readonly IUserRepository _repo;
    
    public UserService(IUserRepository repo)
    {
        _repo = repo;
    }
    
    public async Task<User?> GetUserAsync(int id)
    {
        return await _repo.GetByIdAsync(id);
    }
}
```

---

## 🔧 占位符速查

| 占位符 | 用途 | 示例 |
|--------|------|------|
| `{{columns}}` | 实体所有列 | `SELECT {{columns}} FROM users` |
| `{{table}}` | 表名 | `SELECT * FROM {{table}}` |
| `{{values}}` | 插入值 | `INSERT INTO users {{values}}` |
| `{{where}}` | WHERE条件 | `SELECT * FROM users {{where}}` |
| `{{set}}` | SET赋值 | `UPDATE users {{set}} WHERE id = @id` |
| `{{orderby}}` | ORDER BY | `SELECT * FROM users {{orderby}}` |
| `{{limit}}` | LIMIT | `SELECT * FROM users {{limit}}` |
| `{{offset}}` | OFFSET | `SELECT * FROM users {{offset}}` |
| `{{batch_values}}` | 批量值 | `INSERT INTO users VALUES {{batch_values}}` |

### 占位符修饰符

```csharp
// 排除列
"SELECT {{columns--exclude:password,salt}} FROM users"

// 包含特定列
"SELECT {{columns--include:id,name,email}} FROM users"

// 带别名
"SELECT {{columns--prefix:u.}} FROM users u"

// 组合使用
"SELECT {{columns--exclude:password--prefix:u.}} FROM users u"
```

---

## 🎨 高级特性

### 1. 批量操作

```csharp
[SqlTemplate("INSERT INTO users (name, email) VALUES {{batch_values}}")]
[BatchOperation(MaxBatchSize = 1000)]
Task<int> BatchInsertAsync([BatchItems] List<User> users);
```

### 2. 返回插入ID

```csharp
[SqlTemplate("INSERT INTO users (name) VALUES (@name)")]
[ReturnInsertedId]
Task<int> InsertAndGetIdAsync(string name);
```

### 3. 返回插入实体

```csharp
[SqlTemplate("INSERT INTO users (name) VALUES (@name)")]
[ReturnInsertedEntity]
Task<User> InsertAndGetEntityAsync(string name);
```

### 4. 表达式转SQL

```csharp
[ExpressionToSql]
Task<List<User>> GetUsersAsync(Expression<Func<User, bool>> predicate);

// 使用
var users = await repo.GetUsersAsync(u => u.Age >= 18 && u.Name.Contains("John"));
// 生成: WHERE age >= 18 AND name LIKE '%John%'
```

### 5. 软删除

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    
    [SoftDelete]
    public bool IsDeleted { get; set; }
}
```

### 6. 审计字段

```csharp
public class Article
{
    public int Id { get; set; }
    public string Title { get; set; }
    
    [AuditFields]
    public DateTime CreatedAt { get; set; }
    
    [AuditFields]
    public DateTime UpdatedAt { get; set; }
}
```

---

## 📚 Repository接口速查

### ICrudRepository<TEntity, TKey>
```csharp
Task<TEntity?> GetByIdAsync(TKey id);
Task<List<TEntity>> GetAllAsync();
Task<int> InsertAsync(TEntity entity);
Task<int> UpdateAsync(TEntity entity);
Task<int> DeleteAsync(TKey id);
Task<bool> ExistsAsync(TKey id);
Task<int> CountAsync();
Task<TEntity?> GetFirstAsync();
```

### IQueryRepository<TEntity>
```csharp
Task<List<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate);
Task<TEntity?> FindFirstAsync(Expression<Func<TEntity, bool>> predicate);
Task<int> CountAsync(Expression<Func<TEntity, bool>> predicate);
Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);
Task<List<TEntity>> GetPageAsync(int pageIndex, int pageSize);
Task<PagedResult<TEntity>> GetPagedAsync(int pageIndex, int pageSize);
```

### IBatchRepository<TEntity>
```csharp
Task<int> BatchInsertAsync(IEnumerable<TEntity> entities);
Task<int> BatchUpdateAsync(IEnumerable<TEntity> entities);
Task<int> BatchDeleteAsync(IEnumerable<TKey> ids);
```

---

## 🗄️ 数据库方言

```csharp
[SqlDefine(SqlDialect.SQLite)]      // SQLite
[SqlDefine(SqlDialect.MySql)]       // MySQL
[SqlDefine(SqlDialect.PostgreSql)]  // PostgreSQL
[SqlDefine(SqlDialect.SqlServer)]   // SQL Server
[SqlDefine(SqlDialect.Oracle)]      // Oracle
```

---

## 💻 VS Extension快捷键

| 快捷键 | 功能 |
|--------|------|
| `Ctrl+Alt+S, P` | 打开SQL Preview |
| `Ctrl+Alt+S, G` | 打开Generated Code |
| `Ctrl+Alt+S, T` | 打开Query Tester |
| `Ctrl+Alt+S, R` | 打开Repository Explorer |
| `Ctrl+Space` | 触发IntelliSense (在`{{`或`@`后) |

---

## 📝 代码片段

在VS中输入以下前缀并按Tab:

| 前缀 | 生成内容 |
|------|----------|
| `sqlrepo` | 基础Repository接口 |
| `sqlselect` | SELECT查询 |
| `sqlinsert` | INSERT语句 |
| `sqlupdate` | UPDATE语句 |
| `sqldelete` | DELETE语句 |
| `sqlbatch` | 批量插入 |
| `sqlcrud` | 完整CRUD接口 |
| `sqlpage` | 分页查询 |
| `sqlexpr` | 表达式查询 |
| `sqlentity` | 实体类 |

---

## 🔍 常用SQL模式

### 分页查询
```csharp
[SqlTemplate(@"
    SELECT {{columns}} 
    FROM {{table}} 
    WHERE age >= @minAge
    ORDER BY created_at DESC
    LIMIT @pageSize OFFSET @offset
")]
Task<List<User>> GetPageAsync(int minAge, int pageSize, int offset);
```

### JOIN查询
```csharp
[SqlTemplate(@"
    SELECT u.*, o.order_date, o.total
    FROM users u
    INNER JOIN orders o ON u.id = o.user_id
    WHERE u.id = @userId
")]
Task<List<UserWithOrder>> GetUserOrdersAsync(int userId);
```

### 聚合查询
```csharp
[SqlTemplate("SELECT COUNT(*) FROM users WHERE age >= @minAge")]
Task<int> CountAdultsAsync(int minAge);

[SqlTemplate("SELECT AVG(age) FROM users")]
Task<double> GetAverageAgeAsync();

[SqlTemplate("SELECT MAX(created_at) FROM users")]
Task<DateTime> GetLatestUserDateAsync();
```

### 事务处理
```csharp
using var transaction = connection.BeginTransaction();
try
{
    await repo.InsertAsync(user1);
    await repo.InsertAsync(user2);
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

---

## 🐛 调试技巧

### 1. 查看生成的代码

**VS Extension**: `Tools > Sqlx > Generated Code`

**手动**: 检查 `obj/Debug/net8.0/generated/Sqlx.Generator/`

### 2. 查看实际SQL

**VS Extension**: `Tools > Sqlx > SQL Preview`

**代码中**:
```csharp
var sql = repo.GetSqlPreview("MethodName", parameters);
Console.WriteLine(sql);
```

### 3. 性能分析

**VS Extension**: `Tools > Sqlx > Performance Analyzer`

**手动**:
```csharp
var sw = Stopwatch.StartNew();
var result = await repo.GetUsersAsync();
Console.WriteLine($"Took: {sw.ElapsedMilliseconds}ms");
```

---

## ⚡ 性能优化技巧

### 1. 使用批量操作
```csharp
// ❌ 慢 (1000次数据库调用)
foreach (var user in users) {
    await repo.InsertAsync(user);
}

// ✅ 快 (1次数据库调用)
await repo.BatchInsertAsync(users);
```

### 2. 只查询需要的列
```csharp
// ❌ 查询所有列
[SqlTemplate("SELECT * FROM users")]

// ✅ 只查询需要的
[SqlTemplate("SELECT id, name, email FROM users")]
```

### 3. 使用分页
```csharp
// ❌ 可能OOM
var allUsers = await repo.GetAllAsync();

// ✅ 分页查询
var page = await repo.GetPageAsync(pageIndex: 1, pageSize: 100);
```

### 4. 使用连接池
```csharp
// ✅ 连接池
services.AddScoped<IDbConnection>(sp => 
    new SqlConnection(connectionString));

// ❌ 每次新建
var conn = new SqlConnection(connectionString);
```

### 5. 使用异步
```csharp
// ✅ 异步
var users = await repo.GetAllAsync();

// ❌ 同步（阻塞线程）
var users = repo.GetAll();
```

---

## 📊 性能对比

| 操作 | ADO.NET | Sqlx | EF Core |
|------|---------|------|---------|
| 单行查询 | 0.45ms | **0.43ms** ⚡ | 0.75ms |
| 1000行查询 | 12.5ms | **12.3ms** ⚡ | 21.8ms |
| 单行插入 | 0.85ms | **0.82ms** ⚡ | 1.45ms |
| 批量插入1000行 | 5200ms | **200ms** ⚡⚡⚡ | 8500ms |

---

## 🔗 快速链接

| 资源 | 链接 |
|------|------|
| 📑 完整文档索引 | [INDEX.md](INDEX.md) |
| 📖 README | [README.md](README.md) |
| 📦 安装指南 | [INSTALL.md](INSTALL.md) |
| 🎓 完整教程 | [TUTORIAL.md](TUTORIAL.md) |
| ⚡ 快速参考 | [QUICK_REFERENCE.md](QUICK_REFERENCE.md) |
| ❓ FAQ | [FAQ.md](FAQ.md) |
| 🔧 故障排除 | [TROUBLESHOOTING.md](TROUBLESHOOTING.md) |
| 📈 性能测试 | [PERFORMANCE.md](PERFORMANCE.md) |
| 🔄 迁移指南 | [MIGRATION_GUIDE.md](MIGRATION_GUIDE.md) |
| 🌐 GitHub | [github.com/Cricle/Sqlx](https://github.com/Cricle/Sqlx) |
| 📚 GitHub Pages | [cricle.github.io/Sqlx](https://cricle.github.io/Sqlx/) |

---

## 🆘 获取帮助

1. **查看FAQ**: [FAQ.md](FAQ.md) - 35+常见问题
2. **搜索Issues**: [GitHub Issues](https://github.com/Cricle/Sqlx/issues)
3. **提问**: [GitHub Discussions](https://github.com/Cricle/Sqlx/discussions)
4. **报告Bug**: [New Issue](https://github.com/Cricle/Sqlx/issues/new)

---

## 💡 最佳实践

1. ✅ 使用接口而不是具体类
2. ✅ 使用依赖注入
3. ✅ 使用异步方法
4. ✅ 使用批量操作处理大量数据
5. ✅ 使用占位符简化SQL
6. ✅ 使用事务保证一致性
7. ✅ 只查询需要的列
8. ✅ 使用分页避免大结果集
9. ✅ 使用参数化查询防SQL注入
10. ✅ 使用VS Extension提高效率

---

## 🚀 常用命令

```bash
# 项目管理
dotnet new console -n MyApp
dotnet add package Sqlx
dotnet add package Sqlx.Generator

# 构建和运行
dotnet restore
dotnet build
dotnet run

# 测试
dotnet test

# 清理
dotnet clean

# 发布
dotnet publish -c Release

# 健康检查
.\scripts\health-check.ps1

# 构建VSIX
cd src/Sqlx.Extension
.\build-vsix.ps1
```

---

**打印此页并放在桌上，随时查阅！** 📋

**Happy Coding!** 😊

---

**版本**: 1.0  
**更新**: 2025-10-29  
**适用于**: Sqlx v0.5.0-preview  


