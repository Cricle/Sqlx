# Getting Started with Sqlx

## Installation

Add the Sqlx NuGet package to your project:

```bash
dotnet add package Sqlx
```

## Prerequisites

- .NET 8.0 or later (recommended: .NET 10 LTS for best performance and long-term support)
- A currently supported database: SQLite, MySQL, PostgreSQL, or SQL Server
- Oracle and DB2 dialect/API entry points remain in the package for compatibility, but are not part of the actively validated support matrix for this release
- **Native AOT Ready**: Sqlx is fully compatible with Native AOT deployment and validated by a large test suite

## Basic Setup

### Step 1: Define Your Entity

Create a class representing your database table and mark it with `[Sqlx]` attribute:

```csharp
using System.ComponentModel.DataAnnotations.Schema;
using Sqlx.Annotations;

[Sqlx]
public class User
{
    public int Id { get; set; }
    
    [Column("user_name")]  // Optional: customize column name
    public string Name { get; set; } = string.Empty;
    
    public string Email { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
}
```

The source generator will create:
- `UserEntityProvider` - Provides column metadata
- `UserResultReader` - Reads entities from DbDataReader
- `UserParameterBinder` - Binds entity properties to DbCommand parameters
- `SqlxInitializer` - ModuleInitializer that auto-registers all providers with `SqlQuery<T>`

You can control what gets generated using attribute properties:
```csharp
[Sqlx(GenerateEntityProvider = true, GenerateResultReader = true, GenerateParameterBinder = false)]
public class User { }
```

### Step 2: Use The Lightweight API

If you do not want to create a repository interface and partial class, you can query directly from `DbConnection`:

```csharp
using Microsoft.Data.Sqlite;
using Sqlx;
using Sqlx.Annotations;

[Sqlx, TableName("users")]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

await using var connection = new SqliteConnection("Data Source=app.db");
await connection.OpenAsync();

var users = await connection.SqlxQueryAsync<User>(
    "SELECT {{columns}} FROM {{table}} WHERE id >= @minId ORDER BY id",
    SqlDefine.SQLite,
    new { minId = 10 });

var total = await connection.SqlxScalarAsync<long, User>(
    "SELECT COUNT(*) FROM {{table}}",
    SqlDefine.SQLite);

await connection.SqlxExecuteAsync<User>(
    "UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id",
    SqlDefine.SQLite,
    new User { Id = 10, Name = "Updated", Email = "u@test.com", CreatedAt = DateTime.UtcNow });
```

This path still uses the existing Sqlx template engine, placeholder processing, entity metadata, and result readers. It only removes the repository boilerplate.

For LINQ-style execution, prefer the direct helpers on `SqlxQueryable`:

```csharp
var page = await SqlQuery<User>.ForSqlite()
    .Where(u => u.CreatedAt >= DateTime.UtcNow.AddDays(-30))
    .OrderBy(u => u.Id)
    .Take(100)
    .WithConnection(connection)
    .ToListAsync();

var firstUser = await SqlQuery<User>.ForSqlite()
    .Where(u => u.Email == "john@example.com")
    .WithConnection(connection)
    .FirstOrDefaultAsync();

var totalUsers = await SqlQuery<User>.ForSqlite()
    .WithConnection(connection)
    .CountAsync();
```

These helpers avoid the extra overhead of routing through a generic async-enumerable adapter and are the recommended path for both small and large result sets.

### Step 3: Define Your Repository Interface

Create an interface extending `ICrudRepository<TEntity, TKey>` or define custom methods with `[SqlTemplate]`:

```csharp
using Sqlx;
using Sqlx.Annotations;

public interface IUserRepository : ICrudRepository<User, int>
{
    // Custom query method
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE email = @email")]
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    
    // Custom query with dynamic WHERE
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}} {{limit --param limit}}")]
    Task<List<User>> SearchAsync(
        [ExpressionToSql] Expression<Func<User, bool>> predicate,
        int limit = 100,
        CancellationToken ct = default);
}
```

### Step 4: Implement Your Repository

Create a partial class with the required attributes:

```csharp
using System.Data.Common;
using Sqlx.Annotations;

[TableName("users")]                 // Specify the table name
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository
{
    // Connection Priority: Method Parameter > Field > Property > Primary Constructor
    
    // Option 1: Explicit field (recommended)
    private readonly SqlDialect _dialect;
    private readonly DbConnection _connection;
    public DbTransaction? Transaction { get; set; }

    public UserRepository(DbConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }
    
    // Option 2: Property (suitable for external access)
    // public DbConnection Connection { get; }
    // public DbTransaction? Transaction { get; set; }
    // public UserRepository(DbConnection connection, SqlDialect dialect)
    // {
    //     Connection = connection;
    //     _dialect = dialect;
    // }
    
    // Option 3: Primary constructor (most concise, auto-generated)
    // public partial class UserRepository(DbConnection connection, SqlDialect dialect) : IUserRepository
    // {
    //     // Generator automatically creates:
    //     // private readonly DbConnection _connection = connection;
    //     // private readonly SqlDialect _dialect = dialect;
    //     // public DbTransaction? Transaction { get; set; }
    // }
}
```

**Connection Management**:
- **Method Parameter** (highest priority): Pass connection to specific methods
- **Field** (recommended): Explicit, clear, easy to debug  
- **Property**: Suitable when external access is needed
- **Primary Constructor** (lowest priority): Most concise, auto-generated

The source generator will implement all interface methods automatically.

### Step 5: Use Your Repository

```csharp
using Microsoft.Data.Sqlite;

// Create and open connection
await using var connection = new SqliteConnection("Data Source=app.db");
await connection.OpenAsync();

// Create repository
var repo = new UserRepository(connection, SqlDefine.SQLite);

// Use CRUD operations
var user = new User { Name = "John", Email = "john@example.com" };
var id = await repo.InsertAndGetIdAsync(user);

var retrieved = await repo.GetByIdAsync(id);
Console.WriteLine($"User: {retrieved?.Name}");

// Use custom methods
var byEmail = await repo.GetByEmailAsync("john@example.com");

// Use expression-based queries
var activeUsers = await repo.SearchAsync(u => u.CreatedAt > DateTime.Now.AddDays(-30), limit: 10);
```

## Using Transactions

```csharp
await using var transaction = await connection.BeginTransactionAsync();
repo.Transaction = transaction;

try
{
    await repo.InsertAndGetIdAsync(new User { Name = "User1", Email = "u1@test.com" });
    await repo.InsertAndGetIdAsync(new User { Name = "User2", Email = "u2@test.com" });
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
finally
{
    repo.Transaction = null;
}
```

## Batch Execution

For bulk insert/update/delete operations, use `ExecuteBatchAsync`:

```csharp
var users = new List<User>
{
    new() { Name = "Alice", Email = "alice@test.com" },
    new() { Name = "Bob", Email = "bob@test.com" },
    new() { Name = "Charlie", Email = "charlie@test.com" }
};

// Batch insert
var sql = "INSERT INTO users (name, email) VALUES (@name, @email)";
var affected = await connection.ExecuteBatchAsync(
    sql,
    users,
    UserParameterBinder.Default,  // Generated by [SqlxParameter]
    batchSize: 100,               // Split into batches of 100
    commandTimeout: 60);          // 60 second timeout

// With transaction
await using var transaction = await connection.BeginTransactionAsync();
await connection.ExecuteBatchAsync(
    sql,
    users,
    UserParameterBinder.Default,
    transaction: transaction);
await transaction.CommitAsync();
```

**Features:**
- Uses `DbBatch` on .NET 6+ for optimal performance
- Automatic fallback to loop execution on netstandard2.0
- Configurable batch size for large datasets
- Transaction and timeout support

## Native AOT Deployment

Sqlx is fully compatible with Native AOT. To enable AOT in your project:

```xml
<PropertyGroup>
    <PublishAot>true</PublishAot>
    <InvariantGlobalization>false</InvariantGlobalization>
</PropertyGroup>
```

**AOT Compatibility:**
- ✅ Source-generated hot paths avoid runtime reflection
- ✅ All hot paths (parameter binding, SQL rendering, type conversion, result reading) are reflection-free at runtime
- ✅ Plain POCO fallback paths use reflection only once at initialization to build expression trees; subsequent calls are zero-reflection
- ✅ Expression tree compilation (allowed in AOT)
- ✅ Static method caching
- ✅ validated by a large automated test suite

**Publish AOT:**
```bash
dotnet publish -c Release -r win-x64
dotnet publish -c Release -r linux-x64
dotnet publish -c Release -r osx-arm64
```

## Next Steps

- Learn about [SQL Templates](sql-templates.md) and placeholder syntax
- Explore [Dialect Support](dialects.md) for your database
- Check the [API Reference](api-reference.md) for detailed documentation
- Review [Performance Benchmarks](benchmarks.md) for optimization tips


## Exception Handling

Sqlx provides comprehensive exception handling with automatic logging, retry, and context enrichment.

### Basic Exception Information

All database exceptions are wrapped in `SqlxException` with rich context:

```csharp
try
{
    var user = await repository.GetByIdAsync(userId);
}
catch (SqlxException ex)
{
    // Exception includes:
    Console.WriteLine($"SQL: {ex.Sql}");                    // The SQL that failed
    Console.WriteLine($"Parameters: {ex.Parameters}");      // Parameter values (sanitized)
    Console.WriteLine($"Method: {ex.MethodName}");          // Repository method name
    Console.WriteLine($"Duration: {ex.Duration}");          // How long it took
    Console.WriteLine($"Correlation: {ex.CorrelationId}"); // Distributed tracing ID
    Console.WriteLine($"Original: {ex.InnerException}");   // Original database exception
}
```

### Configuring Exception Handling with SqlxContext

Use `SqlxContextOptions` to configure global exception handling:

```csharp
var options = new SqlxContextOptions
{
    // Automatic logging
    Logger = loggerFactory.CreateLogger<AppDbContext>(),
    
    // Automatic retry for transient failures
    EnableRetry = true,
    MaxRetryCount = 3,
    InitialRetryDelay = TimeSpan.FromMilliseconds(100),
    
    // Custom exception callback
    OnException = async (ex) =>
    {
        await telemetry.RecordException(ex);
    }
};

var context = new AppDbContext(connection, options, serviceProvider);
```

### Automatic Retry

Enable automatic retry for transient database errors:

```csharp
var options = new SqlxContextOptions
{
    EnableRetry = true,
    MaxRetryCount = 3,
    Logger = logger
};

// Automatically retries on:
// - Connection timeouts
// - Deadlocks  
// - Azure SQL transient errors
// Uses exponential backoff: 100ms, 200ms, 400ms
```

### Dependency Injection with Exception Handling

```csharp
services.AddSqlxContext<AppDbContext>((sp, options) =>
{
    var connection = sp.GetRequiredService<DbConnection>();
    var logger = sp.GetRequiredService<ILogger<AppDbContext>>();
    
    options.Logger = logger;
    options.EnableRetry = true;
    options.OnException = async (ex) =>
    {
        var telemetry = sp.GetRequiredService<ITelemetryService>();
        await telemetry.RecordException(ex);
    };
    
    return new AppDbContext(connection, options, sp);
});
```

### Sensitive Data Protection

Sensitive parameters are automatically redacted:

```csharp
// These parameter names are automatically redacted:
// password, pwd, secret, token, apikey, api_key

await repository.AuthenticateAsync(
    username: "user@example.com",
    password: "secret123"  // Will show as "***REDACTED***" in logs
);
```

For more details, see the [SqlxContext Exception Handling documentation](sqlx-context.md#exception-handling).
