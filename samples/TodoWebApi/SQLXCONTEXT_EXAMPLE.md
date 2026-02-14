# SqlxContext Integration Example

This document explains how the TodoWebApi sample demonstrates SqlxContext usage with lazy repository resolution.

## What is SqlxContext?

SqlxContext is a lightweight context manager for Sqlx that provides:
- EF Core-style API for accessing repositories
- Lazy repository resolution from DI container (only created when first accessed)
- Unified transaction management across multiple repositories
- Zero reflection and full AOT compatibility
- Minimal overhead

## Implementation

### 1. Define the Context

See `Services/TodoDbContext.cs`:

```csharp
/// <summary>
/// Database context for the Todo application.
/// Demonstrates SqlxContext usage with lazy repository resolution via IServiceProvider.
/// </summary>
[SqlxContext]
[SqlDefine(SqlDefineTypes.SQLite)]
[IncludeRepository(typeof(TodoRepository))]
public partial class TodoDbContext : SqlxContext
{
    // ===== AUTO-GENERATED CODE =====
    // The source generator creates the following members:
    //
    // 1. Service Provider Field:
    //    private readonly System.IServiceProvider _serviceProvider;
    //
    // 2. Constructor:
    //    public TodoDbContext(DbConnection connection, IServiceProvider serviceProvider)
    //        : base(connection, ownsConnection: false)
    //    { _serviceProvider = serviceProvider; }
    //
    // 3. Repository Property (lazy-resolved on first access):
    //    private TodoRepository? _todos;
    //    public TodoRepository Todos { get { if (_todos == null) { ... } return _todos; } }
    //
    // 4. Transaction Management:
    //    protected override void PropagateTransactionToRepositories() { ... }
    //    protected override void ClearRepositoryTransactions() { ... }
}
```

The source generator automatically creates:
- **Service Provider Field** - stores IServiceProvider for lazy resolution
- **Constructor** accepting `DbConnection` and `IServiceProvider`
- **Todos property** returning `TodoRepository` (with lazy resolution on first access)
- **Transaction propagation logic** - automatically sets Transaction on all repositories
- **Transaction cleanup logic** - automatically clears Transaction on all repositories

**Generated Code Preview:**
```csharp
// Auto-generated in: obj/Debug/net10.0/generated/Sqlx.Generator/TodoDbContext.Context.g.cs
public partial class TodoDbContext
{
    private TodoRepository? _todos;
    private readonly System.IServiceProvider _serviceProvider;

    public TodoDbContext(DbConnection connection, IServiceProvider serviceProvider) 
        : base(connection, ownsConnection: false)
    {
        _serviceProvider = serviceProvider;
    }

    public TodoRepository Todos
    {
        get
        {
            if (_todos == null)
            {
                _todos = _serviceProvider.GetRequiredService<TodoRepository>();
                ((ISqlxRepository)_todos).Connection = Connection;
                _todos.Transaction = Transaction;
            }
            return _todos;
        }
    }

    protected override void PropagateTransactionToRepositories()
    {
        if (_todos != null) _todos.Transaction = Transaction;
    }

    protected override void ClearRepositoryTransactions()
    {
        if (_todos != null) _todos.Transaction = null;
    }
}
```

**Key Features:**
- **Lazy Initialization**: Repository is only created when `Todos` property is first accessed
- **Single Instance**: Once created, the same instance is returned for all subsequent accesses
- **Automatic Configuration**: Connection and Transaction are set automatically when repository is created
- **Performance**: Unused repositories are never instantiated, saving memory and startup time

### 2. Register in DI

See `Program.cs`:

```csharp
// Register connection
var sqliteConnection = new SqliteConnection("Data Source=todos.db;Cache=Shared;Foreign Keys=true");
sqliteConnection.Open();
builder.Services.AddSingleton(_ => sqliteConnection);

// Register repository
builder.Services.AddSingleton<TodoRepository>();

// Register context (automatically resolves repositories)
builder.Services.AddSingleton<TodoDbContext>();

// Register interface for backward compatibility
builder.Services.AddSingleton<ITodoRepository>(sp => 
    sp.GetRequiredService<TodoDbContext>().Todos);
```

### 3. Use in Endpoints

#### Option A: Inject Repository Interface (Current Approach)

```csharp
app.MapGet("/api/todos", async (ITodoRepository repo) => 
    await repo.GetAllAsync(limit: 100));
```

This approach:
- Maintains backward compatibility with existing code
- Works with or without SqlxContext
- Suitable for single-repository operations

#### Option B: Inject Context Directly (For Transactions)

The sample includes a real transactional bulk insert endpoint demonstrating SqlxContext:

```csharp
app.MapPost("/api/todos/bulk", async (List<CreateTodoRequest> requests, TodoDbContext context) =>
{
    if (requests == null || requests.Count == 0)
    {
        return Results.BadRequest(new { Error = "Request list cannot be empty" });
    }

    await using var transaction = await context.BeginTransactionAsync();
    try
    {
        var ids = new List<long>();
        var now = DateTime.UtcNow;
        
        foreach (var request in requests)
        {
            var todo = new Todo { /* ... */ };
            
            // All operations automatically participate in the transaction
            var newId = await context.Todos.InsertAndGetIdAsync(todo, default);
            ids.Add(newId);
        }
        
        // Commit all changes atomically
        await transaction.CommitAsync();
        
        return Results.Json(new BulkCreateResult(
            true, 
            ids.Count, 
            ids, 
            $"Successfully created {ids.Count} todos in a single transaction"
        ), TodoJsonContext.Default.BulkCreateResult);
    }
    catch (Exception ex)
    {
        // Transaction automatically rolled back on dispose
        return Results.Json(new BulkCreateResult(
            false, 
            0, 
            new List<long>(), 
            $"Transaction rolled back: {ex.Message}"
        ), TodoJsonContext.Default.BulkCreateResult, statusCode: 500);
    }
});
```

**Try it yourself:**
```bash
# Create multiple todos in a single transaction
curl -X POST http://localhost:5000/api/todos/bulk \
  -H "Content-Type: application/json" \
  -d '[
    {"title": "Task 1", "priority": 3},
    {"title": "Task 2", "priority": 4},
    {"title": "Task 3", "priority": 5}
  ]'

# Response:
# {
#   "success": true,
#   "count": 3,
#   "ids": [1, 2, 3],
#   "message": "Successfully created 3 todos in a single transaction"
# }
```

This approach:
- Provides unified transaction management
- Ensures all operations participate in the same transaction
- Automatically rolls back on errors
- Returns detailed success/failure information

## How Lazy Resolution Works

When you access `context.Todos` for the first time:

1. The generated property checks if `_todos` is null
2. If null, it calls `_serviceProvider.GetRequiredService<TodoRepository>()`
3. It automatically sets `Connection` via `ISqlxRepository` interface
4. It automatically sets `Transaction` property
5. The repository is cached in `_todos` for subsequent access

This provides:
- **Lazy Loading**: Repositories only created when first accessed
- **Single Instance**: Each repository is created only once
- **Automatic Configuration**: Connection and Transaction set automatically
- **Performance**: Unused repositories are never instantiated
- **No Overhead**: Cached after first access

## Benefits

1. **Simpler DI Registration**: No need to manually inject repositories
2. **Cleaner Code**: Auto-generated constructors reduce boilerplate
3. **Performance**: Lazy initialization - only create what you use
4. **Backward Compatible**: Existing code using `ITodoRepository` continues to work
5. **Transaction Support**: Easy to add transactional operations when needed
6. **Type-Safe**: Direct property access with IntelliSense support
7. **Zero Overhead**: Thin wrapper with no runtime reflection
8. **AOT Compatible**: Fully supports Native AOT compilation

## When to Use SqlxContext

### Use SqlxContext When:
- You have multiple repositories that need to work together
- You need unified transaction management
- You want an EF Core-style API
- You want lazy repository resolution

### Use Direct Repository Injection When:
- You only have one repository
- You don't need transaction coordination
- You want maximum simplicity

## Learn More

- [SqlxContext Documentation](../../docs/sqlx-context.md)
- [SqlxContext Tests](../../tests/Sqlx.Tests/SqlxContextTests.cs)
- [API Reference](../../docs/api-reference.md)
