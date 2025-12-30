# 🚀 Sqlx TODO Demo

A minimal TODO web application demonstrating **Sqlx** - a high-performance, source-generated ORM for .NET.

## ✨ Features

### Sqlx Highlights
- **🎯 Type-Safe SQL**: Compile-time SQL validation
- **⚡ Zero Reflection**: 100% source-generated code  
- **📦 AOT Native**: Full Native AOT support
- **🔥 High Performance**: Near-raw ADO.NET speed
- **🎨 Clean API**: Intuitive repository pattern

### Demo Functionality
- ✅ Create, Read, Update, Delete todos
- ✅ Toggle completion status
- ✅ Real-time statistics
- ✅ Beautiful, responsive UI

## 🏃 Quick Start

```bash
# Run the application
cd samples/TodoWebApi
dotnet run

# Open browser
http://localhost:5000
```

## 📝 Sqlx Usage Example

### 1. Define Entity
```csharp
[Table("todos")]
public record Todo
{
    public long Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public bool IsCompleted { get; init; }
    public DateTime CreatedAt { get; init; }
}
```

### 2. Define Repository Interface
```csharp
[RepositoryFor(typeof(Todo), Dialect = DatabaseDialect.Sqlite)]
public interface ITodoRepository : ICrudRepository<Todo, long>
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE IsCompleted = @isCompleted LIMIT @limit")]
    Task<List<Todo>> GetByCompletionStatusAsync(bool isCompleted, int limit = 100);
    
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE Title LIKE @pattern")]
    Task<List<Todo>> SearchAsync(string pattern);
}
```

### 3. Use Repository
```csharp
// Inject generated repository
app.MapGet("/api/todos", async (ITodoRepository repo) =>
{
    var todos = await repo.GetAllAsync(limit: 100);
    return Results.Json(todos);
});

app.MapPost("/api/todos", async (Todo todo, ITodoRepository repo) =>
{
    await repo.InsertAsync(todo);
    return Results.Created($"/api/todos/{todo.Id}", todo);
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
    Task<List<TEntity>> GetAllAsync(int limit = 1000);
    Task InsertAsync(TEntity entity);
    Task<int> UpdateAsync(TEntity entity);
    Task<int> DeleteAsync(TKey id);
    Task<long> CountAsync();
}
```

### 2. **Custom SQL Templates**
Type-safe SQL with placeholders:
```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE IsCompleted = @isCompleted")]
Task<List<Todo>> GetByCompletionStatusAsync(bool isCompleted);
```

### 3. **Source Generation**
All code generated at compile-time - zero reflection at runtime!

### 4. **AOT Compatibility**
Full support for Native AOT compilation:
```bash
dotnet publish -c Release -r win-x64 --self-contained
```

## 📊 Performance

Sqlx achieves near-raw ADO.NET performance with a clean, type-safe API:

| Method | Mean | Allocated |
|--------|------|-----------|
| Raw ADO.NET | 100% | Baseline |
| **Sqlx** | **~102%** | **~Same** |
| Dapper | ~110% | +16 B |
| EF Core | ~450% | +8.4 KB |

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
