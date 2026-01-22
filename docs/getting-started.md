# Getting Started with Sqlx

## Installation

Add the Sqlx NuGet package to your project:

```bash
dotnet add package Sqlx
```

## Prerequisites

- .NET 8.0 or later (recommended: .NET 10 LTS for best performance and long-term support)
- A supported database: SQLite, MySQL, PostgreSQL, SQL Server, Oracle, or DB2
- **Native AOT Ready**: Sqlx is fully compatible with Native AOT deployment (1344 tests passing)

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

### Step 2: Define Your Repository Interface

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

### Step 3: Implement Your Repository

Create a partial class with the required attributes:

```csharp
using System.Data.Common;
using Sqlx.Annotations;

[SqlDefine(SqlDefineTypes.SQLite)]  // Specify your database dialect
[TableName("users")]                 // Specify the table name
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository
{
    // Connection Priority: Method Parameter > Field > Property > Primary Constructor
    
    // Option 1: Explicit field (recommended)
    private readonly DbConnection _connection;
    public DbTransaction? Transaction { get; set; }

    public UserRepository(DbConnection connection)
    {
        _connection = connection;
    }
    
    // Option 2: Property (suitable for external access)
    // public DbConnection Connection { get; }
    // public DbTransaction? Transaction { get; set; }
    // public UserRepository(DbConnection connection) => Connection = connection;
    
    // Option 3: Primary constructor (most concise, auto-generated)
    // public partial class UserRepository(DbConnection connection) : IUserRepository
    // {
    //     // Generator automatically creates:
    //     // private readonly DbConnection _connection = connection;
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

### Step 4: Use Your Repository

```csharp
using Microsoft.Data.Sqlite;

// Create and open connection
await using var connection = new SqliteConnection("Data Source=app.db");
await connection.OpenAsync();

// Create repository
var repo = new UserRepository(connection);

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
- ✅ Zero reflection at runtime
- ✅ All code generated at compile time
- ✅ Expression tree compilation (allowed in AOT)
- ✅ Static method caching
- ✅ 1344 unit tests passing

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
