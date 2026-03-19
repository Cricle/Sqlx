# 🚀 Sqlx TODO Demo

A comprehensive TODO web application demonstrating **Sqlx** - a high-performance, source-generated ORM for .NET with full Native AOT support.

## ✨ 核心特性

### Sqlx 亮点
- **🎯 类型安全 SQL**: 编译时 SQL 验证，三种查询方式
- **⚡ 零反射**: 100% 源生成代码  
- **📦 AOT 原生支持**: 完整的 Native AOT 支持
- **🔥 高性能**: 接近原生 ADO.NET 速度
- **🎨 简洁 API**: 直观的仓储模式 + LINQ 支持
- **🔍 IQueryable**: 完整的 LINQ 查询构建器
- **🏗️ 高级类型支持**: 自动识别 class/record/struct 并生成最优代码

### 示例功能
- ✅ 完整的 CRUD 操作
- ✅ 三种查询方式（SqlTemplate、LINQ Expression、IQueryable）
- ✅ 批量操作（批量更新、批量删除、批量完成）
- ✅ 高级类型支持（record、mixed record、struct、struct record）
- ✅ 实时统计和分页
- ✅ 美观的响应式 UI
- ✅ 39 个 API 端点展示所有功能

## 🏃 快速开始

```bash
# 运行应用
cd samples/TodoWebApi
dotnet run

# 打开浏览器
http://localhost:5000
```

**注意：** 本示例使用 `WebApplication.CreateSlimBuilder()` 以支持 AOT 编译。CreateSlimBuilder 默认不加载完整的配置系统，因此：
- ✅ 支持 HTTP 端点配置
- ❌ 不支持 HTTPS 端点配置（需要额外调用 `UseKestrelHttpsConfiguration()`）
- 如需 HTTPS 支持，请改用 `WebApplication.CreateBuilder(args)`

## 📚 完整功能文档

查看 [FEATURES.md](FEATURES.md) 了解：
- 🎯 高级类型支持详解（class、record、mixed record、struct、struct record）
- 📝 三种查询方式完整示例
- 🔧 42 个内置 ICrudRepository 方法
- 📊 所有 39 个 API 端点说明
- 🚀 性能优化技巧
- 🎓 最佳实践指南

## 📝 三种查询方式速览

Sqlx provides three powerful ways to query your data:

### 1. SqlTemplate - Direct SQL with Smart Placeholders
```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_completed = @isCompleted")]
Task<List<Todo>> GetByCompletionStatusAsync(bool isCompleted);

// Usage
var completed = await repo.GetByCompletionStatusAsync(true);
```

### 2. LINQ Expression - Type-Safe Predicates
```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}")]
Task<List<Todo>> GetWhereAsync(Expression<Func<Todo, bool>> predicate);

// Usage
var highPriority = await repo.GetWhereAsync(t => t.Priority >= 3 && !t.IsCompleted);
```

### 3. IQueryable - Full LINQ Query Builder
```csharp
IQueryable<Todo> AsQueryable();

// Usage
var query = repo.AsQueryable()
    .Where(t => t.Priority >= 3 && !t.IsCompleted)
    .OrderByDescending(t => t.Priority)
    .ThenBy(t => t.DueDate)
    .Skip(10)
    .Take(10);

var todos = await query.ToListAsync();
var sql = query.ToSql(); // Get generated SQL for debugging
```

### 4. ExpressionBlockResult - Unified Expression Parsing (New!)
```csharp
// 高性能统一表达式解析 - 一次遍历同时获取 SQL 和参数
Task<int> DynamicUpdateWhereAsync(
    Expression<Func<Todo, Todo>> updateExpression,
    Expression<Func<Todo, bool>> whereExpression);

// Usage - 比传统方式快 2 倍
await repo.DynamicUpdateWhereAsync(
    t => new Todo { Priority = 5, UpdatedAt = DateTime.UtcNow },
    t => t.IsCompleted == false && t.Priority < 3
);

// 性能对比：
// 传统方式：ToSetClause() + GetSetParameters() + ToWhereClause() + GetParameters() = 4 次遍历
// ExpressionBlockResult：ParseUpdate() + Parse() = 2 次遍历（快 2 倍）
```

## 🎯 Complete Example

### 1. Define Entity
```csharp
[Sqlx, TableName("todos")]
public class Todo
{
    [Key] public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int Priority { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 2. Define Repository Interface
```csharp
public interface ITodoRepository : ICrudRepository<Todo, long>
{
    // Approach 1: SqlTemplate
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE priority >= @minPriority")]
    Task<List<Todo>> GetByPriorityAsync(int minPriority);
    
    // Approach 2: LINQ Expression
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}")]
    Task<List<Todo>> GetWhereAsync(Expression<Func<Todo, bool>> predicate);
    
    // Approach 3: IQueryable
    IQueryable<Todo> AsQueryable();
}
```

### 3. Implement Repository
```csharp
[RepositoryFor(typeof(ITodoRepository))]
public partial class TodoRepository(SqliteConnection connection, SqlDialect dialect) : ITodoRepository
{
    // Connection Priority: Method Parameter > Field > Property > Primary Constructor
    
    // Option 1: Explicit field (recommended, highest priority after method parameter)
    // private readonly SqliteConnection _connection = connection;
    // public DbTransaction? Transaction { get; set; }
    
    // Option 2: Property (suitable for external access)
    // public SqliteConnection Connection { get; } = connection;
    // public DbTransaction? Transaction { get; set; }
    
    // Option 3: Primary constructor only (most concise, auto-generated)
    // Generator automatically creates:
    // private readonly SqliteConnection _connection = connection;
    // public DbTransaction? Transaction { get; set; }
    
    public IQueryable<Todo> AsQueryable()
    {
        return SqlQuery<Todo>.For(_placeholderContext.Dialect).WithConnection(_connection);
    }
}
```

**Connection Management**:
- **Method Parameter** (highest priority): Override connection for specific methods
- **Field** (recommended): Explicit, clear, easy to debug
- **Property**: Suitable when external access is needed
- **Primary Constructor** (lowest priority): Most concise, auto-generated by source generator
```

### 4. Use in API
```csharp
// Approach 1: SqlTemplate
app.MapGet("/api/todos/high-priority", async (ITodoRepository repo) =>
    await repo.GetByPriorityAsync(3));

// Approach 2: LINQ Expression
app.MapGet("/api/todos/pending", async (ITodoRepository repo) =>
    await repo.GetWhereAsync(t => !t.IsCompleted && t.DueDate < DateTime.UtcNow));

// Approach 3: IQueryable
app.MapGet("/api/todos/paged", async (ITodoRepository repo, int page = 1) =>
{
    var query = repo.AsQueryable()
        .Where(t => !t.IsCompleted)
        .OrderByDescending(t => t.Priority)
        .Skip((page - 1) * 10)
        .Take(10);
    
    return await query.ToListAsync();
});
```

## 🎨 Architecture

```
TodoWebApi/
├── Models/
│   └── Todo.cs                 # Entity definition
├── Services/
│   ├── TodoService.cs          # Repository interface with Sqlx attributes
│   ├── TodoDbContext.cs        # SqlxContext for unified repository management
│   └── DatabaseService.cs      # Database initialization
├── wwwroot/
│   └── index.html              # Single-page UI
├── Program.cs                  # Minimal API setup (using CreateSlimBuilder for AOT)
└── appsettings.json            # Configuration (HTTP only, no HTTPS)
```

### SqlxContext Integration

This sample demonstrates SqlxContext usage for unified repository management with automatic resolution:

```csharp
// 1. Define context with repository (auto-generated constructor)
[SqlxContext]
[IncludeRepository(typeof(TodoRepository))]
public partial class TodoDbContext : SqlxContext
{
    // Constructor auto-generated: (DbConnection, IServiceProvider)
    // Repositories resolved lazily from IServiceProvider
}

// 2. Register in DI (simplified with automatic resolution)
services.AddSingleton<TodoRepository>();
services.AddSqlxContext<TodoDbContext>(ServiceLifetime.Singleton);
services.AddSingleton<ITodoRepository>(sp => sp.GetRequiredService<TodoDbContext>().Todos);

// 3. Use in endpoints (via interface for backward compatibility)
app.MapGet("/api/todos", async (ITodoRepository repo) => 
    await repo.GetAllAsync(limit: 100));

// 4. Inject context directly for transaction management
app.MapPost("/api/todos/bulk", async (List<CreateTodoRequest> requests, TodoDbContext context) =>
{
    await using var transaction = await context.BeginTransactionAsync();
    try
    {
        var ids = new List<long>();
        foreach (var request in requests)
        {
            var todo = new Todo { /* ... */ };
            // All operations automatically participate in the transaction
            var newId = await context.Todos.InsertAndGetIdAsync(todo);
            ids.Add(newId);
        }
        
        // Commit all changes atomically
        await transaction.CommitAsync();
        return Results.Json(new { Count = ids.Count, Ids = ids });
    }
    catch
    {
        // Transaction automatically rolled back on dispose
        throw;
    }
});
```

**Key Benefits:**
- ✅ **Automatic Repository Resolution** - No manual injection needed
- ✅ **Lazy Loading** - Repositories resolved only when accessed
- ✅ **Unified Transaction Management** - All operations in one transaction
- ✅ **Automatic Rollback** - Transaction rolled back on error
- ✅ **Type-Safe** - Direct property access with IntelliSense

For more details on SqlxContext, see the [SqlxContext documentation](../../docs/sqlx-context.md).

### CreateSlimBuilder vs CreateBuilder

本示例使用 `WebApplication.CreateSlimBuilder()` 以支持 Native AOT 编译：

| 特性 | CreateSlimBuilder | CreateBuilder |
|------|-------------------|---------------|
| AOT 支持 | ✅ 完全支持 | ❌ 不支持 |
| 配置系统 | 最小化 | 完整 |
| HTTPS 配置 | ❌ 需要额外配置 | ✅ 自动支持 |
| 启动速度 | 更快 | 较慢 |
| 内存占用 | 更小 | 较大 |

**HTTPS 配置说明：**
- `CreateSlimBuilder` 不自动加载 Kestrel HTTPS 配置
- 如果 `appsettings.json` 中包含 HTTPS 端点，会抛出异常：
  ```
  System.InvalidOperationException: Call UseKestrelHttpsConfiguration() 
  on IWebHostBuilder to enable loading HTTPS settings from configuration.
  ```
- **解决方案1**（推荐）：只使用 HTTP 端点（演示项目足够）
- **解决方案2**：改用 `WebApplication.CreateBuilder(args)`（失去 AOT 支持）
- **解决方案3**：手动配置 HTTPS（需要额外代码）

## 🔧 Key Sqlx Features Demonstrated

### 1. **ICrudRepository Interface**
Pre-defined CRUD operations without writing SQL (42 methods total):

**Query Methods (24)**:
```csharp
// Single Entity (4)
Task<TEntity?> GetByIdAsync(TKey id);
TEntity? GetById(TKey id);
Task<TEntity?> GetFirstWhereAsync(Expression<Func<TEntity, bool>> predicate);
TEntity? GetFirstWhere(Expression<Func<TEntity, bool>> predicate);

// List Queries (6)
Task<List<TEntity>> GetByIdsAsync(List<TKey> ids);
List<TEntity> GetByIds(List<TKey> ids);
Task<List<TEntity>> GetAllAsync(int limit = 1000);
List<TEntity> GetAll(int limit = 1000);
Task<List<TEntity>> GetWhereAsync(Expression<Func<TEntity, bool>> predicate, int limit = 1000);
List<TEntity> GetWhere(Expression<Func<TEntity, bool>> predicate, int limit = 1000);

// Pagination (4)
Task<List<TEntity>> GetPagedAsync(int pageSize = 20, int offset = 0);
List<TEntity> GetPaged(int pageSize = 20, int offset = 0);
Task<List<TEntity>> GetPagedWhereAsync(Expression<Func<TEntity, bool>> predicate, int pageSize = 20, int offset = 0);
List<TEntity> GetPagedWhere(Expression<Func<TEntity, bool>> predicate, int pageSize = 20, int offset = 0);

// Existence & Count (10)
Task<bool> ExistsByIdAsync(TKey id);
bool ExistsById(TKey id);
Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate);
bool Exists(Expression<Func<TEntity, bool>> predicate);
Task<long> CountAsync();
long Count();
Task<long> CountWhereAsync(Expression<Func<TEntity, bool>> predicate);
long CountWhere(Expression<Func<TEntity, bool>> predicate);
```

**Command Methods (18)**:
```csharp
// Insert (6)
Task<TKey> InsertAndGetIdAsync(TEntity entity);
TKey InsertAndGetId(TEntity entity);
Task<int> InsertAsync(TEntity entity);
int Insert(TEntity entity);
Task<int> BatchInsertAsync(IEnumerable<TEntity> entities);
int BatchInsert(IEnumerable<TEntity> entities);

// Update (6)
Task<int> UpdateAsync(TEntity entity);
int Update(TEntity entity);
Task<int> UpdateWhereAsync(TEntity entity, Expression<Func<TEntity, bool>> predicate);
int UpdateWhere(TEntity entity, Expression<Func<TEntity, bool>> predicate);
Task<int> BatchUpdateAsync(IEnumerable<TEntity> entities);
int BatchUpdate(IEnumerable<TEntity> entities);

// Delete (6)
Task<int> DeleteAsync(TKey id);
int Delete(TKey id);
Task<int> DeleteByIdsAsync(List<TKey> ids);
int DeleteByIds(List<TKey> ids);
Task<int> DeleteWhereAsync(Expression<Func<TEntity, bool>> predicate);
int DeleteWhere(Expression<Func<TEntity, bool>> predicate);
Task<int> DeleteAllAsync();
int DeleteAll();
```

### 2. **SqlTemplate - Direct SQL with Placeholders**
Type-safe SQL with smart placeholders:
```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_completed = @isCompleted")]
Task<List<Todo>> GetByCompletionStatusAsync(bool isCompleted);

// Generates: SELECT id, title, is_completed, ... FROM todos WHERE is_completed = @isCompleted
```

### 3. **LINQ Expression Support**
Use C# expressions for type-safe queries:
```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}")]
Task<List<Todo>> GetWhereAsync(Expression<Func<Todo, bool>> predicate);

// Usage
var todos = await repo.GetWhereAsync(t => t.Priority >= 3 && !t.IsCompleted);
// Generates: SELECT * FROM todos WHERE priority >= 3 AND is_completed = 0
```

### 4. **Dictionary-Based WHERE (AOT Compatible)**
Use dictionaries for dynamic filtering without reflection:
```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --object filter}}")]
Task<List<Todo>> FilterAsync(IReadOnlyDictionary<string, object?> filter);

// Usage - only non-null values generate conditions
var filter = new Dictionary<string, object?>
{
    ["Priority"] = 3,           // Generates: [priority] = @priority
    ["IsCompleted"] = false,    // Generates: [is_completed] = @is_completed
    ["DueDate"] = null          // Ignored (null value)
};
var todos = await repo.FilterAsync(filter);
// Generates: SELECT * FROM todos WHERE ([priority] = @priority AND [is_completed] = @is_completed)
```

### 5. **IQueryable Query Builder**
Full LINQ support with deferred execution:
```csharp
var query = repo.AsQueryable()
    .Where(t => t.Priority >= 3)
    .Where(t => !t.IsCompleted)
    .OrderByDescending(t => t.Priority)
    .ThenBy(t => t.DueDate)
    .Skip(10)
    .Take(10);

// Inspect generated SQL before execution
var sql = query.ToSql();
Console.WriteLine(sql);

// Execute query
var todos = await query.ToListAsync();
```

**Supported LINQ Methods:**
- `Where` - Filtering with complex expressions
- `Select` - Projection (columns selection)
- `OrderBy` / `OrderByDescending` / `ThenBy` / `ThenByDescending` - Sorting
- `Take` / `Skip` - Pagination
- `First` / `FirstOrDefault` / `Single` / `SingleOrDefault` - Single result
- `Any` / `Count` - Aggregation
- `Distinct` - Remove duplicates

**Supported Functions:**
- String: `Contains`, `StartsWith`, `EndsWith`, `ToUpper`, `ToLower`, `Trim`, `Substring`, `Replace`, `Length`
- Math: `Abs`, `Round`, `Floor`, `Ceiling`, `Sqrt`, `Pow`, `Min`, `Max`
- DateTime: Property access (`Year`, `Month`, `Day`, etc.)

### 6. **Source Generation**
All code generated at compile-time - zero reflection at runtime!

### 7. **SqlTemplate Return Type (Debugging)**
Get generated SQL without executing queries:
```csharp
using Sqlx;                    // SqlTemplate class
using Sqlx.Annotations;        // [SqlTemplate] attribute

// Debug mode - returns SqlTemplate class (not executing the query)
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE title LIKE @query")]
SqlTemplate SearchSql(string query);  // Return type is Sqlx.SqlTemplate class

// Usage
var template = repo.SearchSql("%test%");
Console.WriteLine(template.Sql);  // SELECT * FROM todos WHERE title LIKE @query
Console.WriteLine(template.Parameters["@query"]);  // %test%
Console.WriteLine(template.Execute().Render());  // SELECT * FROM todos WHERE title LIKE '%test%'
```

**Important:** There are two different `SqlTemplate` in Sqlx:
- **`[SqlTemplate]` attribute** (`Sqlx.Annotations`) - Used to annotate interface methods, defines SQL templates
- **`SqlTemplate` class** (`Sqlx`) - Runtime class representing parsed SQL template, used for debugging

### 8. **AOT Compatibility**
Full support for Native AOT compilation:
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## 📊 API Endpoints

### Basic CRUD Operations
- `GET /api/todos` - Get all todos (limit 100)
- `GET /api/todos/{id}` - Get todo by ID
- `POST /api/todos` - Create new todo
- `PUT /api/todos/{id}` - Update todo
- `DELETE /api/todos/{id}` - Delete todo by ID
- `PUT /api/todos/{id}/complete` - Mark todo as completed
- `PUT /api/todos/{id}/actual-minutes` - Update actual work time

### Query & Filter Operations
- `GET /api/todos/search?q={keyword}` - Search todos by title/description
- `GET /api/todos/completed` - Get completed todos
- `GET /api/todos/high-priority` - Get high priority todos (priority >= 3)
- `GET /api/todos/priority/{priority}` - Get todos by specific priority
- `GET /api/todos/overdue` - Get overdue incomplete todos
- `GET /api/todos/due-soon` - Get todos due in next 7 days
- `GET /api/todos/paged?page={page}&pageSize={size}` - Get paginated todos
- `GET /api/todos/{id}/exists` - Check if todo exists
- `POST /api/todos/by-ids` - Get multiple todos by IDs

### Count & Statistics
- `GET /api/todos/count` - Get total todo count
- `GET /api/todos/count/pending` - Get pending todo count
- `GET /api/todos/linq/count-overdue` - Count overdue todos (LINQ)
- `GET /api/todos/queryable/stats` - Get comprehensive statistics

### Batch Operations
- `POST /api/todos/bulk` - **Transactional bulk insert** (SqlxContext example)
- `PUT /api/todos/batch/priority` - Batch update priority
- `PUT /api/todos/batch/complete` - Batch complete todos
- `DELETE /api/todos/batch` - Batch delete todos
- `DELETE /api/todos/completed` - Delete all completed todos

### LINQ Expression Examples
- `GET /api/todos/linq/high-priority-pending` - High priority pending todos using LINQ expressions

### IQueryable Examples
- `GET /api/todos/queryable/priority-paged?page={page}&pageSize={size}` - Paginated high priority todos
- `GET /api/todos/queryable/titles` - Get only titles and priorities (projection)
- `GET /api/todos/queryable/search-advanced?keyword={text}` - Advanced search with LINQ

**Total: 40 API endpoints** demonstrating comprehensive Sqlx features (including SqlxContext transaction management)

## 📊 Performance

Sqlx achieves excellent performance with minimal GC pressure:

### Small Dataset Performance (10-100 rows) - Typical Web API Scenario

| Framework | 10 rows | 100 rows | Memory (100 rows) | Gen1 GC (1000 rows) |
|-----------|---------|----------|-------------------|---------------------|
| **Sqlx** | **42.19 μs** 🥇 | **230.35 μs** 🥇 | **37 KB** 🥇 | **1.95** 🥇 |
| Dapper.AOT | 43.42 μs | 233.76 μs | 45.66 KB | 3.91 |
| FreeSql | 49.64 μs | 237.38 μs | 37.23 KB | 25.39 |

**Key Advantages:**
- ✅ **Fastest** for small datasets (10-100 rows) - typical Web API queries
- ✅ **Lowest GC pressure** - Gen1 GC is 13x lower than FreeSql
- ✅ **Best memory efficiency** - 23-40% less memory than Dapper.AOT
- ✅ **Most stable** - ideal for long-running web applications

*Based on BenchmarkDotNet tests (.NET 10 LTS) - see [benchmarks](../../docs/benchmarks.md) and [AOT results](../../AOT_PERFORMANCE_RESULTS.md) for details*

## 🌟 Why Sqlx?

1. **Performance First**: Minimal overhead, maximum speed
2. **Type Safety**: Catch SQL errors at compile time
3. **AOT Ready**: Works perfectly with Native AOT
4. **Developer Friendly**: Clean, intuitive API
5. **Zero Magic**: Transparent source-generated code

## 📚 Learn More

- [Sqlx Documentation](../../docs/README.md)
- [GitHub Repository](https://github.com/yourusername/Sqlx)
- [Performance Benchmarks](../../tests/Sqlx.Benchmarks)

## 📄 License

MIT License - See [LICENSE](../../LICENSE) for details
