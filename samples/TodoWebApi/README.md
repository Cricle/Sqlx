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
[SqlxEntity, SqlxParameter, TableName("todos")]
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
Pre-defined CRUD operations without writing SQL:
```csharp
public interface ICrudRepository<TEntity, TKey>
{
    Task<TEntity?> GetByIdAsync(TKey id);
    Task<List<TEntity>> GetAllAsync(int limit = 1000, int offset = 0);
    Task<int> InsertAsync(TEntity entity);
    Task<long> InsertAndGetIdAsync(TEntity entity);
    Task<int> UpdateAsync(TEntity entity);
    Task<int> DeleteAsync(TKey id);
    Task<long> CountAsync();
    Task<bool> ExistsAsync(TKey id);
    Task<int> BatchInsertAsync(IEnumerable<TEntity> entities);
}
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

### 4. **IQueryable Query Builder**
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

### 5. **Source Generation**
All code generated at compile-time - zero reflection at runtime!

### 6. **SqlTemplate Return Type (Debugging)**
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

### 7. **AOT Compatibility**
Full support for Native AOT compilation:
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## 📊 API Endpoints

### Basic CRUD
- `GET /api/todos` - Get all todos
- `GET /api/todos/{id}` - Get todo by ID
- `POST /api/todos` - Create new todo
- `PUT /api/todos/{id}` - Update todo
- `DELETE /api/todos/{id}` - Delete todo

### SqlTemplate Examples
- `GET /api/todos/search?q={keyword}` - Search todos
- `GET /api/todos/completed` - Get completed todos
- `GET /api/todos/high-priority` - Get high priority todos
- `GET /api/todos/due-soon` - Get todos due in next 7 days

### LINQ Expression Examples
- `GET /api/todos/linq/high-priority-pending` - High priority pending todos
- `GET /api/todos/linq/count-overdue` - Count overdue todos

### IQueryable Examples
- `GET /api/todos/queryable/priority-paged?page=1&pageSize=10` - Paginated high priority todos
- `GET /api/todos/queryable/search-advanced?keyword={text}` - Advanced search with LINQ
- `GET /api/todos/queryable/stats` - Get statistics using aggregation

## 📊 Performance

Sqlx achieves near-raw ADO.NET performance with a clean, type-safe API:

| Method | Mean | Allocated |
|--------|------|-----------|
| Raw ADO.NET | 100% | Baseline |
| **Sqlx** | **~102%** | **~Same** |
| Dapper.AOT | ~116% | +73% |
| EF Core | ~450% | +8.4 KB |

*Based on BenchmarkDotNet tests - see [benchmarks](../../docs/benchmarks.md) for details*

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
