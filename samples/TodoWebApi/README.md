# 🚀 Sqlx TODO Demo

A comprehensive TODO web application demonstrating **Sqlx** - a high-performance, source-generated ORM for .NET with full IQueryable support.

## ✨ Features

### Sqlx Highlights
- **🎯 Type-Safe SQL**: Compile-time SQL validation with three query approaches
- **⚡ Zero Reflection**: 100% source-generated code  
- **📦 AOT Native**: Full Native AOT support
- **🔥 High Performance**: Near-raw ADO.NET speed
- **🎨 Clean API**: Intuitive repository pattern with LINQ support
- **🔍 IQueryable**: Full LINQ query builder with expression tree support

### Demo Functionality
- ✅ Create, Read, Update, Delete todos
- ✅ Toggle completion status
- ✅ Real-time statistics
- ✅ Beautiful, responsive UI
- ✅ Three query approaches: SqlTemplate, LINQ expressions, and IQueryable

## 🏃 Quick Start

```bash
# Run the application
cd samples/TodoWebApi
dotnet run

# Open browser
http://localhost:5000
```

## 📝 Three Query Approaches

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
SqlxQueryable<Todo> AsQueryable();

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
    SqlxQueryable<Todo> AsQueryable();
}
```

### 3. Implement Repository
```csharp
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(ITodoRepository))]
public partial class TodoRepository(SqliteConnection connection) : ITodoRepository
{
    private readonly SqliteConnection _connection = connection;
    
    public SqlxQueryable<Todo> AsQueryable()
    {
        return new SqlxQueryable<Todo>(_connection, SqlDefineTypes.SQLite);
    }
}
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
│   └── DatabaseService.cs      # Database initialization
├── wwwroot/
│   └── index.html              # Single-page UI
└── Program.cs                  # Minimal API setup
```

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
// Debug mode - returns SqlTemplate
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE title LIKE @query")]
SqlTemplate SearchSql(string query);

// Usage
var template = repo.SearchSql("%test%");
Console.WriteLine(template.Sql);  // SELECT * FROM todos WHERE title LIKE @query
Console.WriteLine(template.Parameters["@query"]);  // %test%
Console.WriteLine(template.Execute().Render());  // SELECT * FROM todos WHERE title LIKE '%test%'
```

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

**Total: 39 API endpoints** demonstrating comprehensive Sqlx features

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
