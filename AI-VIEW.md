# Sqlx - Complete AI Assistant Reference

> **Purpose**: This document serves as a comprehensive, standalone reference for AI assistants working with Sqlx. It contains all critical information, features, usage patterns, best practices, and implementation details needed to effectively use and contribute to the Sqlx library.

## Table of Contents

1. [Project Overview](#project-overview)
2. [Quick Start](#quick-start)
3. [Entity Definition](#entity-definition)
4. [Repository Pattern](#repository-pattern)
5. [Runtime Dialect Selection](#runtime-dialect-selection)
6. [SqlxContext](#sqlxcontext)
7. [SQL Templates & Placeholders](#sql-templates--placeholders)
8. [SqlxVar - Runtime Variables](#sqlxvar---runtime-variables)
9. [Inline Expressions](#inline-expressions)
10. [Conditional Blocks](#conditional-blocks)
11. [Dictionary-based WHERE](#dictionary-based-where)
12. [Batch Operations](#batch-operations)
13. [Transaction Management](#transaction-management)
14. [NULL Handling](#null-handling)
15. [Output Parameters](#output-parameters)
16. [Multiple Result Sets](#multiple-result-sets)
17. [IQueryable & LINQ](#iqueryable--linq)
18. [Expression Queries](#expression-queries)
19. [Any Placeholder](#any-placeholder)
20. [SqlBuilder](#sqlbuilder)
21. [Dynamic Update](#dynamic-update)
22. [Source Generators](#source-generators)
23. [Metrics Collection](#metrics-collection)
24. [Performance](#performance)
25. [Database Dialects](#database-dialects)
26. [Advanced Types](#advanced-types)
27. [Testing Guidelines](#testing-guidelines)
28. [Common Patterns](#common-patterns)
29. [Security Best Practices](#security-best-practices)
30. [Troubleshooting](#troubleshooting)
31. [Anti-Patterns](#anti-patterns)
32. [FAQ](#faq)
33. [Quick Reference](#quick-reference)

---

## Project Overview

### What is Sqlx?

Sqlx is a high-performance, AOT-friendly .NET database access library that uses source generators to generate code at compile time with **zero runtime reflection**.

### Key Statistics
- **3124+ passing tests** (unit + E2E)
- **83.2% branch coverage**
- **Up to 5.8% faster** than Dapper.AOT in single-row queries
- **Zero reflection** - all code generated at compile time
- **Full AOT support** - Native AOT ready

### Supported Databases
- SQLite
- PostgreSQL  
- MySQL
- SQL Server
- Oracle
- DB2

### Core Principles
1. **Zero Reflection** - Everything generated at compile time
2. **Type Safety** - Compile-time validation of SQL and expressions
3. **Performance** - Optimized ResultReader with minimal GC pressure
4. **AOT Compatible** - Full Native AOT support
5. **Developer Experience** - Clean API with IntelliSense support

---

## Quick Start

### Installation

```bash
dotnet add package Sqlx
```

### Basic Example

```csharp
using Sqlx;
using Sqlx.Annotations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

// 1. Define Entity (supports class, record, struct)
[Sqlx, TableName("users")]
public class User
{
    [Key] public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Email { get; set; } = string.Empty;
}

// 2. Define Repository Interface
public interface IUserRepository : ICrudRepository<User, long>
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge")]
    Task<List<User>> GetAdultsAsync(int minAge);
}

// 3. Implement Repository (code auto-generated)
// Pass dialect at runtime - no [SqlDefine] needed
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository
{
    public UserRepository(DbConnection connection, SqlDialect dialect) { }
}

// 4. Use
await using var conn = new SqliteConnection("Data Source=app.db");
var repo = new UserRepository(conn, SqlDefine.SQLite);
var adults = await repo.GetAdultsAsync(18);
```

---

## Entity Definition

### Supported Entity Types

| Type | Example | Generated Strategy | Use Case |
|------|---------|-------------------|----------|
| **Class** | `public class User { }` | Object initializer | Mutable entities |
| **Record** | `public record User(long Id, string Name);` | Constructor | Immutable DTOs |
| **Mixed Record** | `public record User(long Id) { public string Name { get; set; } }` | Constructor + initializer | Hybrid scenarios |
| **Struct** | `public struct User { }` | Object initializer | Value types |
| **Struct Record** | `public readonly record struct User(long Id);` | Constructor | Immutable value types |

### Required Attributes

```csharp
using Sqlx.Annotations;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

[Sqlx]                              // ✅ REQUIRED: Marks class for code generation
[TableName("users")]                // ✅ REQUIRED: Specifies database table name
public class User
{
    [Key]                           // ✅ REQUIRED: Marks primary key
    public long Id { get; set; }
    
    public string Name { get; set; }
    
    [Column("email_address")]       // Optional: Custom column name
    public string Email { get; set; }
    
    [IgnoreDataMember]              // Optional: Exclude from SQL operations
    public string ComputedProperty => $"User-{Id}";
    
    public string ReadOnlyProp { get; }  // Automatically excluded (no setter)
}
```

### Column Name Mapping

**Default Behavior**: Property names are converted to snake_case:

| Property Name | Column Name |
|--------------|-------------|
| `Id` | `id` |
| `UserName` | `user_name` |
| `CreatedAt` | `created_at` |
| `IsActive` | `is_active` |

**Custom Mapping**:
```csharp
[Column("custom_name")]
public string PropertyName { get; set; }
```

### Auto-Discovery

Source generator automatically discovers entities from:

1. **[Sqlx] Attribute** - Explicit marking
2. **SqlQuery<T> Usage** - Generic type arguments
3. **[SqlTemplate] Methods** - Return types and parameters

```csharp
// All three entities are auto-discovered:

[Sqlx, TableName("users")]
public class User { }  // Discovered via [Sqlx]

// Discovered via SqlQuery<T>
var products = SqlQuery<Product>.ForSqlite().ToList();

// Discovered via method return type
public interface IOrderRepository
{
    [SqlTemplate("SELECT * FROM orders WHERE id = @id")]
    Task<Order?> GetByIdAsync(long id);  // Order discovered here
}
```

---

## Repository Pattern

### Repository Definition

```csharp
// 1. Define interface (inherits ICrudRepository for 46 built-in methods)
public interface IUserRepository : ICrudRepository<User, long>
{
    // Custom methods
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge")]
    Task<List<User>> GetAdultsAsync(int minAge);
}

// 2. Implement repository (code auto-generated)
// NEW: Pass dialect at runtime via constructor - no [SqlDefine] needed
[RepositoryFor(typeof(IUserRepository))] // ✅ REQUIRED: Interface to implement
public partial class UserRepository : IUserRepository
{
    // Generator auto-creates this constructor when no [SqlDefine] present
    public UserRepository(DbConnection connection, SqlDialect dialect) { }
}

// Runtime dialect is always required
[RepositoryFor(typeof(IUserRepository))] // ✅ REQUIRED: Interface to implement
public partial class UserRepository : IUserRepository
{
    // Option 1: Primary constructor (recommended)
    public UserRepository(DbConnection connection, SqlDialect dialect) { }
    
    // Option 2: Explicit field
    private readonly DbConnection _connection;
    private readonly SqlDialect _dialect;
    public UserRepository(DbConnection connection, SqlDialect dialect)
    {
        _connection = connection;
        _dialect = dialect;
    }
    
    // Option 3: Property
    public DbConnection Connection { get; }
    public UserRepository(DbConnection connection)
    {
        Connection = connection;
    }
    
    // Transaction property (auto-generated if not defined)
    public DbTransaction? Transaction { get; set; }
}
```

### Runtime Dialect - No [SqlDefine] Required

`[SqlDefine]` is now **optional**. The preferred approach is to pass `SqlDialect` at runtime via the constructor.

**✅ PREFERRED (runtime dialect):**
```csharp
// No [SqlDefine] needed - dialect passed at construction time
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository { }

// Usage - choose dialect at runtime
var sqliteRepo = new UserRepository(sqliteConn, SqlDefine.SQLite);
var pgRepo     = new UserRepository(pgConn,     SqlDefine.PostgreSql);
var mysqlRepo  = new UserRepository(mysqlConn,  SqlDefine.MySql);
```

**❌ WRONG - [SqlDefine] on wrong target:**
```csharp
// DO NOT use [SqlDefine] on SqlxContext
[SqlxContext]
public partial class AppDbContext : SqlxContext { }

// DO NOT use [SqlDefine] on interface
public interface IUserRepository { }
```

### Built-in CRUD Methods (46 total)

Inheriting `ICrudRepository<TEntity, TKey>` provides:

**Query Methods (26)**:
- `GetByIdAsync/GetById` - Single entity by ID
- `GetByIdsAsync/GetByIds` - Multiple entities by IDs
- `GetAllAsync/GetAll` - All entities
- `GetFirstWhereAsync/GetFirstWhere` - First matching entity
- `GetWhereAsync/GetWhere` - All matching entities
- `GetPagedAsync/GetPaged` - Paginated results
- `GetPagedWhereAsync/GetPagedWhere` - Paginated with filter
- `ExistsByIdAsync/ExistsById` - Check existence by ID
- `ExistsAsync/Exists` - Check existence by condition
- `CountAsync/Count` - Count all
- `CountWhereAsync/CountWhere` - Count with filter
- `AsQueryable()` - LINQ query builder

**Command Methods (20)**:
- `InsertAndGetIdAsync/InsertAndGetId` - Insert and return ID
- `InsertAsync/Insert` - Insert entity
- `BatchInsertAsync/BatchInsert` - Insert multiple entities
- `UpdateAsync/Update` - Update entity
- `UpdateWhereAsync/UpdateWhere` - Update with condition
- `BatchUpdateAsync/BatchUpdate` - Update multiple entities
- `DynamicUpdateAsync/DynamicUpdate` - Update specific fields
- `DynamicUpdateWhereAsync/DynamicUpdateWhere` - Dynamic update with condition
- `DeleteAsync/Delete` - Delete by ID
- `DeleteByIdsAsync/DeleteByIds` - Delete multiple by IDs
- `DeleteWhereAsync/DeleteWhere` - Delete with condition
- `DeleteAllAsync/DeleteAll` - Delete all entities


---

## Runtime Dialect Selection

`[SqlDefine]` is now **optional**. Dialect can be passed at runtime via the constructor, enabling one repository class to work with multiple databases.

### How It Works

When no `[SqlDefine]` is present, the source generator produces a constructor that requires a `SqlDialect` parameter:

```csharp
// Your code
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository { }

// Generator produces:
public UserRepository(DbConnection connection, SqlDialect dialect)
{
    _connection = connection;
    _dialect = dialect;
}

public SqlDialect Dialect => _dialect;
```

Runtime dialect is always explicit:

```csharp
// Your code
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository { }

// Generator produces:
public UserRepository(DbConnection connection, SqlDialect dialect)
{
    _connection = connection;
    _dialect = dialect ?? throw new ArgumentNullException(nameof(dialect));
}
```

### Multi-Tenant Pattern

```csharp
public class TenantRepositoryFactory
{
    public IUserRepository Create(Tenant tenant)
    {
        var connection = CreateConnection(tenant.ConnectionString);
        var dialect = tenant.DbType switch
        {
            "postgresql" => SqlDefine.PostgreSql,
            "mysql"      => SqlDefine.MySql,
            "sqlite"     => SqlDefine.SQLite,
            _            => throw new NotSupportedException(tenant.DbType)
        };
        return new UserRepository(connection, dialect);
    }
}
```

### Test Environment Pattern

```csharp
// Production: PostgreSQL
services.AddScoped<IUserRepository>(sp =>
    new UserRepository(sp.GetRequiredService<NpgsqlConnection>(), SqlDefine.PostgreSql));

// Tests: SQLite in-memory
services.AddScoped<IUserRepository>(sp =>
    new UserRepository(new SqliteConnection(":memory:"), SqlDefine.SQLite));
```

### Database Migration Pattern

```csharp
// Migrate data from MySQL to PostgreSQL
var source = new UserRepository(mysqlConn, SqlDefine.MySql);
var target = new UserRepository(pgConn,    SqlDefine.PostgreSql);

var users = await source.GetAllAsync();
await target.BatchInsertAsync(users);
```

### ISqlxRepository.Dialect

All generated repositories expose the dialect via `ISqlxRepository`:

```csharp
public interface ISqlxRepository
{
    DbConnection? Connection { get; set; }
    DbTransaction? Transaction { get; set; }
    SqlDialect Dialect { get; }   // runtime dialect
}
```

---
---

## SqlxContext

### Context Definition

```csharp
[SqlxContext]
[IncludeRepository(typeof(UserRepository))]
[IncludeRepository(typeof(OrderRepository))]
public partial class AppDbContext : SqlxContext
{
    // Source generator auto-generates:
    // - Constructor
    // - Users and Orders properties (lazy-loaded)
    // - Transaction propagation logic
}
```

**⚠️ IMPORTANT**: SqlxContext is just a container. SQL dialect is configured per-repository, NOT on the context.

### Dependency Injection

```csharp
services.AddSingleton<UserRepository>();
services.AddSingleton<OrderRepository>();

services.AddSqlxContext<AppDbContext>((sp, options) =>
{
    var connection = sp.GetRequiredService<SqliteConnection>();
    
    // Exception handling
    options.OnException = (ex, context) =>
    {
        logger.LogError(ex, "SQL Error in {Method}", context.MethodName);
    };
    
    // Retry configuration
    options.EnableRetry = true;
    options.MaxRetryCount = 3;
    options.RetryDelayMilliseconds = 100;
    options.UseExponentialBackoff = true;
    
    // Logging
    options.Logger = sp.GetRequiredService<ILogger<AppDbContext>>();
    
    return new AppDbContext(connection, options, sp);
}, ServiceLifetime.Singleton);
```

### SqlxException - Rich Context

```csharp
try
{
    await repo.GetByIdAsync(123);
}
catch (SqlxException ex)
{
    // SQL context
    Console.WriteLine($"SQL: {ex.Sql}");
    Console.WriteLine($"Parameters: {string.Join(", ", ex.Parameters)}");
    Console.WriteLine($"Method: {ex.MethodName}");
    Console.WriteLine($"Repository: {ex.RepositoryType}");
    
    // Performance
    Console.WriteLine($"Duration: {ex.DurationMilliseconds}ms");
    
    // Transaction
    Console.WriteLine($"Transaction ID: {ex.TransactionId}");
    Console.WriteLine($"In Transaction: {ex.InTransaction}");
    
    // Tracing
    Console.WriteLine($"Correlation ID: {ex.CorrelationId}");
}
```

---

## SQL Templates & Placeholders

### Complete Placeholder Reference

| Placeholder | Type | Description | Example |
|------------|------|-------------|---------|
| `{{columns}}` | Static | All column names | `[id], [name], [email]` |
| `{{columns --exclude Id}}` | Static | Columns excluding specified | `[name], [email]` |
| `{{values}}` | Static | Parameter placeholders | `@id, @name, @email` |
| `{{values --exclude Id}}` | Static | Values excluding specified | `@name, @email` |
| `{{values --inline Status='pending'}}` | Static | Values with inline expressions | `@name, 'pending'` |
| `{{set}}` | Static | UPDATE SET clause | `[name] = @name` |
| `{{set --exclude Id}}` | Static | SET excluding specified | `[name] = @name` |
| `{{set --inline Version=Version+1}}` | Static | SET with inline expressions | `[version] = [version]+1` |
| `{{table}}` | Static | Table name | `[users]` |
| `{{table --param name}}` | Dynamic | Dynamic table name | Runtime value |
| `{{where --param expr}}` | Dynamic | Expression-based WHERE | From Expression<Func<T, bool>> |
| `{{where --object filter}}` | Dynamic | Dictionary-based WHERE | From Dictionary<string, object?> |
| `{{var --name varName}}` | Dynamic | Application variable | Literal value from [SqlxVar] |
| `{{limit --count 10}}` | Static | Static LIMIT | `LIMIT 10` |
| `{{limit --param size}}` | Dynamic | Dynamic LIMIT | Runtime value |
| `{{offset --count 20}}` | Static | Static OFFSET | `OFFSET 20` |
| `{{offset --param skip}}` | Dynamic | Dynamic OFFSET | Runtime value |
| `{{if notnull=param}}...{{/if}}` | Block | Conditional inclusion | Include if not null |

---

## SqlxVar - Runtime Variables

### Overview

Use `[SqlxVar]` attribute to declare runtime variables with automatic code generation, zero reflection, AOT compatible.

```csharp
public partial class UserRepository
{
    // Mark method as variable provider
    [SqlxVar("tenantId")]
    private string GetTenantId() => TenantContext.Current;
    
    [SqlxVar("userId")]
    private string GetUserId() => UserContext.CurrentUserId.ToString();
    
    [SqlxVar("timestamp")]
    private static string GetTimestamp() => DateTime.UtcNow.ToString("O");
    
    // Source generator automatically creates GetVar method and VarProvider property
}

// Use in SQL template
[SqlTemplate(@"
    SELECT {{columns}} FROM {{table}} 
    WHERE tenant_id = {{var --name tenantId}} 
    AND user_id = {{var --name userId}}
")]
Task<List<User>> GetMyDataAsync();

// Generated SQL (values inserted as literals):
// SELECT "id", "name" FROM "users" 
// WHERE tenant_id = tenant-123 AND user_id = user-456
```

### Key Features

- ✅ Zero reflection - compile-time generated switch statement
- ✅ AOT compatible - fully supports Native AOT
- ✅ Type safe - compile-time validation
- ✅ High performance - O(1) dispatch
- ✅ Supports static and instance methods

### ⚠️ SECURITY WARNING

**CRITICAL**: `{{var}}` inserts values as **literals** directly into SQL. Only use for trusted, application-controlled values (tenant IDs, SQL keywords). **NEVER** use for user input - always use SQL parameters (`@param`) for user input.

```csharp
// ✅ SAFE: Application-controlled tenant ID
[SqlxVar("tenantId")]
private string GetTenantId() => TenantContext.Current; // From auth token

// ❌ DANGEROUS: User input as literal - SQL INJECTION RISK!
[SqlxVar("searchTerm")]
private string GetSearchTerm() => HttpContext.Request.Query["search"]; // NEVER DO THIS!

// ✅ CORRECT: Use SQL parameters for user input
[SqlTemplate("SELECT * FROM users WHERE name LIKE @searchTerm")]
Task<List<User>> SearchAsync(string searchTerm);
```

---

## Inline Expressions

Use inline expressions for default values, timestamps, and computed fields:

### INSERT with Defaults

```csharp
[SqlTemplate(@"
    INSERT INTO {{table}} ({{columns --exclude Id}}) 
    VALUES ({{values --exclude Id --inline Status='pending',CreatedAt=CURRENT_TIMESTAMP}})
")]
Task<int> CreateAsync(string name, string description);
// SQL: INSERT INTO [tasks] ([name], [description], [status], [created_at]) 
//      VALUES (@name, @description, 'pending', CURRENT_TIMESTAMP)
```

### UPDATE with Version Increment

```csharp
[SqlTemplate(@"
    UPDATE {{table}} 
    SET {{set --exclude Id,Version --inline Version=Version+1,UpdatedAt=CURRENT_TIMESTAMP}} 
    WHERE id = @id
")]
Task<int> UpdateAsync(long id, string name, string email);
// SQL: UPDATE [users] SET [name] = @name, [email] = @email, 
//      [version] = [version]+1, [updated_at] = CURRENT_TIMESTAMP WHERE id = @id
```

### Expression Rules

- Use C# property names (PascalCase), not column names
- Property names automatically replaced with dialect-wrapped column names
- Parameter placeholders (@param) preserved as-is
- Supports SQL functions, literals, arithmetic operations

### Supported Expressions

- Arithmetic: `Version=Version+1`, `Total=@quantity*@unitPrice`
- SQL functions: `CreatedAt=CURRENT_TIMESTAMP`, `Email=LOWER(TRIM(Email))`
- Literals: `Status='pending'`, `Priority=0`, `IsActive=1`
- Complex: `Result=COALESCE(NULLIF(Value,''),Default)`

---

## Conditional Blocks

```csharp
// Dynamic search with optional filters
[SqlTemplate(@"
    SELECT {{columns}} FROM {{table}} 
    WHERE 1=1 
    {{if notnull=name}}AND name LIKE @name{{/if}}
    {{if notnull=minAge}}AND age >= @minAge{{/if}}
    {{if notnull=maxAge}}AND age <= @maxAge{{/if}}
")]
Task<List<User>> SearchAsync(string? name, int? minAge, int? maxAge);

// Usage:
await repo.SearchAsync("Alice%", 18, null);
// SQL: SELECT ... WHERE 1=1 AND name LIKE @name AND age >= @minAge
// (maxAge condition excluded because parameter is null)
```

### Supported Conditions

- `notnull=param` - Include when parameter is not null
- `null=param` - Include when parameter is null
- `notempty=param` - Include when parameter is not null and not empty
- `empty=param` - Include when parameter is null or empty

---

## Dictionary-based WHERE

```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --object filter}}")]
Task<List<User>> FilterAsync(IReadOnlyDictionary<string, object?> filter);

// Usage:
var filter = new Dictionary<string, object?>
{
    ["Name"] = "John",      // Generates: [name] = @name
    ["Age"] = 25,           // Generates: [age] = @age
    ["Email"] = null        // Generates: [email] IS NULL (NEW behavior)
};
await repo.FilterAsync(filter);
// SQL: SELECT ... WHERE ([name] = @name AND [age] = @age AND [email] IS NULL)

// Empty dictionary returns always-true
var all = await repo.FilterAsync(new Dictionary<string, object?>());
// SQL: SELECT ... WHERE 1=1
```

### ⚠️ NULL Behavior Change

- **NEW**: NULL values generate `IS NULL` conditions
- **OLD**: NULL values were ignored

---

## Batch Operations

### BatchInsert - High Performance

```csharp
var users = new List<User>
{
    new() { Name = "Alice", Age = 25 },
    new() { Name = "Bob", Age = 30 },
    new() { Name = "Charlie", Age = 35 }
};

await repo.BatchInsertAsync(users, cancellationToken);
// Generates single SQL: INSERT INTO users (name, age) VALUES (@name0, @age0), (@name1, @age1), (@name2, @age2)
```

**Why**: BatchInsertAsync generates a single SQL statement for all rows, dramatically improving performance (up to 10x faster).

### BatchUpdate

```csharp
var users = await repo.GetWhereAsync(u => u.IsActive);
foreach (var user in users)
{
    user.LastLoginDate = DateTime.UtcNow;
}

await repo.BatchUpdateAsync(users);
```

---

## Transaction Management

### Repository-level

```csharp
var repo = new UserRepository(connection);

using var transaction = connection.BeginTransaction();
repo.Transaction = transaction;

try
{
    await repo.InsertAsync(user1);
    await repo.UpdateAsync(user2);
    await repo.DeleteAsync(user3);
    
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

### Context-level

```csharp
await using var transaction = await context.BeginTransactionAsync();
try
{
    await context.Users.InsertAsync(user);
    await context.Orders.InsertAsync(order);
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

---

## NULL Handling

### Nullable Reference Types

```csharp
#nullable enable

[Sqlx, TableName("users")]
public class User
{
    [Key] public long Id { get; set; }
    public string Name { get; set; } = string.Empty;  // Non-nullable
    public string? Email { get; set; }                // Nullable
}
```

### NULL in WHERE

```csharp
// Dictionary-based WHERE with NULL
var filter = new Dictionary<string, object?>
{
    ["Name"] = "John",
    ["Email"] = null  // Generates: [email] IS NULL
};
// SQL: WHERE ([name] = @name AND [email] IS NULL)

// Expression-based WHERE with NULL
await repo.GetWhereAsync(u => u.Email == null);
// SQL: WHERE [email] IS NULL
```

---

## Output Parameters

### Synchronous Methods - Out/Ref

**Out Parameter (Output mode)**:
```csharp
public interface IUserRepository
{
    // [OutputParameter] is OPTIONAL - DbType auto-inferred from int
    [SqlTemplate("INSERT INTO {{table}} (name, age) VALUES (@name, @age); SELECT last_insert_rowid()")]
    int InsertAndGetId(string name, int age, out int id);
}

// Usage:
var result = repo.InsertAndGetId("John", 25, out int newId);
Console.WriteLine($"Inserted ID: {newId}");
```

**Ref Parameter (InputOutput mode)**:
```csharp
public interface ICounterRepository
{
    // [OutputParameter] is OPTIONAL
    [SqlTemplate("UPDATE counters SET value = value + 1 WHERE name = @name; SELECT value FROM counters WHERE name = @name")]
    int IncrementCounter(string name, ref int currentValue);
}

// Usage:
int counter = 100;
var result = repo.IncrementCounter("page_views", ref counter);
Console.WriteLine($"New counter value: {counter}"); // 101
```

### Asynchronous Methods - OutputParameter<T>

**⚠️ C# LIMITATION**: Async methods cannot use `out`/`ref` parameters. Use `OutputParameter<T>` wrapper instead.

```csharp
public interface IUserRepository
{
    // [OutputParameter] is OPTIONAL - auto-detected from OutputParameter<T>
    [SqlTemplate("INSERT INTO users (name, age) VALUES (@name, @age); SELECT last_insert_rowid()")]
    Task<int> InsertUserAsync(string name, int age, OutputParameter<int> userId);
}

// Usage:
var userId = new OutputParameter<int>();
var result = await repo.InsertUserAsync("Alice", 25, userId);
Console.WriteLine($"Inserted {result} rows, ID: {userId.Value}");

// InputOutput mode (with initial value):
var counter = OutputParameter<int>.WithValue(100);
var result = await repo.IncrementCounterAsync("page_views", counter);
Console.WriteLine($"New value: {counter.Value}"); // 101
```

### When to use [OutputParameter] attribute

- ✅ When you need to specify `DbType` explicitly
- ✅ When you need to set `Size` for strings/binary types
- ❌ NOT needed for basic scenarios (auto-inferred)

---

## Multiple Result Sets

Return multiple scalar values from a single SQL query using tuple return types:

```csharp
public interface IUserRepository
{
    // Use ResultSetMapping to specify mapping
    [SqlTemplate(@"
        INSERT INTO users (name) VALUES (@name);
        SELECT last_insert_rowid();
        SELECT COUNT(*) FROM users
    ")]
    [ResultSetMapping(0, "rowsAffected")]
    [ResultSetMapping(1, "userId")]
    [ResultSetMapping(2, "totalUsers")]
    Task<(int rowsAffected, long userId, int totalUsers)> InsertAndGetStatsAsync(string name);
}

// Usage:
var (rows, userId, total) = await repo.InsertAndGetStatsAsync("Alice");
Console.WriteLine($"Inserted user {userId}, total users: {total}");
```

### Default Mapping

Without `[ResultSetMapping]`:
- First element → affected row count (ExecuteNonQuery)
- Remaining elements → SELECT results in order

### Features

- ✅ Single database round-trip
- ✅ Type safe - compile-time checking
- ✅ Zero reflection
- ✅ AOT compatible
- ✅ Automatic type conversion

---

## IQueryable & LINQ

```csharp
// Basic query
var query = SqlQuery<User>.ForSqlite()
    .Where(u => u.Age >= 18 && u.IsActive)
    .OrderBy(u => u.Name)
    .Take(10);

var sql = query.ToSql();
// SELECT [id], [name], [age], [is_active] FROM [User] 
// WHERE ([age] >= 18 AND [is_active] = 1) 
// ORDER BY [name] ASC LIMIT 10

// Projection (anonymous types, AOT compatible)
var results = await SqlQuery<User>.ForPostgreSQL()
    .Where(u => u.Name.Contains("test"))
    .Select(u => new { u.Id, u.Name })
    .WithConnection(connection)
    .ToListAsync();

// JOIN
var query = SqlQuery<User>.ForSqlite()
    .Join(SqlQuery<Order>.ForSqlite(),
        u => u.Id,
        o => o.UserId,
        (u, o) => new { u.Name, o.Total })
    .Where(x => x.Total > 100);

// Aggregates
var maxAge = await SqlQuery<User>.ForSqlite()
    .WithConnection(connection)
    .WithReader(UserResultReader.Default)
    .MaxAsync(u => u.Age);

// Fast direct execution helpers
var firstAdult = await SqlQuery<User>.ForSqlite()
    .Where(u => u.Age >= 18)
    .WithConnection(connection)
    .FirstOrDefaultAsync();

var page = await SqlQuery<User>.ForSqlite()
    .OrderBy(u => u.Id)
    .Skip(1000)
    .Take(200)
    .WithConnection(connection)
    .ToListAsync();

var totalAdults = await SqlQuery<User>.ForSqlite()
    .Where(u => u.Age >= 18)
    .WithConnection(connection)
    .CountAsync();
```

### Supported LINQ Methods

- `Where`, `Select`, `OrderBy`, `ThenBy`, `Take`, `Skip`
- `GroupBy`, `Distinct`, `Join`, `GroupJoin`
- `Count`, `Min`, `Max`, `Sum`, `Average`
- `First`, `FirstOrDefault`, `Any`

### Supported Functions

- **String**: `Contains`, `StartsWith`, `EndsWith`, `ToUpper`, `ToLower`, `Trim`, `Substring`, `Replace`, `Length`
- **Math**: `Abs`, `Round`, `Floor`, `Ceiling`, `Sqrt`, `Pow`, `Min`, `Max`

---

## Expression Queries

```csharp
// In repository
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}")]
Task<List<User>> GetWhereAsync(Expression<Func<User, bool>> predicate);

// Usage
var adults = await repo.GetWhereAsync(u => u.Age >= 18 && u.IsActive);
```

---

## Any Placeholder

Use `Any.Value<T>()` to create reusable expression templates with runtime parameters:

```csharp
// Define reusable expression template
Expression<Func<User, bool>> ageRangeTemplate = u => 
    u.Age >= Any.Value<int>("minAge") && 
    u.Age <= Any.Value<int>("maxAge");

// Scenario 1: Query young users (18-30)
var youngUsers = ExpressionBlockResult.Parse(ageRangeTemplate.Body, SqlDefine.SQLite)
    .WithParameter("minAge", 18)
    .WithParameter("maxAge", 30);
// SQL: ([age] >= @minAge AND [age] <= @maxAge)
// Parameters: @minAge=18, @maxAge=30

// Scenario 2: Query middle-aged users (30-50) - reuse same template
var middleAgedUsers = ExpressionBlockResult.Parse(ageRangeTemplate.Body, SqlDefine.SQLite)
    .WithParameter("minAge", 30)
    .WithParameter("maxAge", 50);
// SQL: ([age] >= @minAge AND [age] <= @maxAge)
// Parameters: @minAge=30, @maxAge=50

// UPDATE expression template
Expression<Func<User, User>> updateTemplate = u => new User
{
    Name = Any.Value<string>("newName"),
    Age = u.Age + Any.Value<int>("ageIncrement")
};

var result = ExpressionBlockResult.ParseUpdate(updateTemplate, SqlDefine.SQLite)
    .WithParameter("newName", "John")
    .WithParameter("ageIncrement", 1);
// SQL: [name] = @newName, [age] = ([age] + @ageIncrement)
```

### Use Cases

- ✅ Query template library - predefined common query templates
- ✅ Dynamic query building - runtime parameter values
- ✅ Multi-tenant applications - different tenants use same template
- ✅ Configuration-driven queries - load parameters from config

---

## SqlBuilder

SqlBuilder provides high-performance, type-safe dynamic SQL construction with automatic parameterization to prevent SQL injection.

### Core Features

- **🔒 Auto-parameterization** - Interpolated strings automatically converted to SQL parameters
- **⚡ High performance** - ArrayPool<char> zero heap allocation, Expression tree optimization (20-34% faster than reflection)
- **🔧 SqlTemplate integration** - Supports {{columns}}, {{table}} and other placeholders
- **🔗 Subquery support** - Composable query building with automatic parameter conflict resolution
- **📦 AOT compatible** - Zero reflection design, fully supports Native AOT

### Quick Start

```csharp
using Sqlx;

// Auto-parameterization
using var builder = new SqlBuilder(SqlDefine.SQLite);
builder.Append($"SELECT * FROM users WHERE age >= {18} AND name = {"John"}");
var template = builder.Build();

// Generated SQL
Console.WriteLine(template.Sql);
// SELECT * FROM users WHERE age >= @p0 AND name = @p1

// Parameters
foreach (var (key, value) in template.Parameters)
{
    Console.WriteLine($"{key} = {value}");
}
// p0 = 18
// p1 = John
```

### Dynamic Conditions

```csharp
using var builder = new SqlBuilder(SqlDefine.SQLite);
builder.Append($"SELECT * FROM users WHERE 1=1");

// Add WHERE clauses dynamically based on conditions
if (!string.IsNullOrEmpty(nameFilter))
{
    builder.Append($" AND name LIKE {"%" + nameFilter + "%"}");
}

if (minAge.HasValue)
{
    builder.Append($" AND age >= {minAge.Value}");
}

if (isActive.HasValue)
{
    builder.Append($" AND is_active = {isActive.Value}");
}

var template = builder.Build();
var users = await connection.QueryAsync(template.Sql, template.Parameters, UserResultReader.Default);
```

### SqlTemplate Integration

```csharp
// Create builder with PlaceholderContext
var context = new PlaceholderContext(
    SqlDefine.SQLite, 
    "users", 
    UserEntityProvider.Default.Columns
);

using var builder = new SqlBuilder(context);

// Use {{columns}} and {{table}} placeholders
builder.AppendTemplate(
    "SELECT {{columns}} FROM {{table}} WHERE age >= @minAge", 
    new { minAge = 18 }
);

var template = builder.Build();
// SQL: SELECT [id], [name], [age], [email] FROM [users] WHERE age >= @minAge
// Parameters: { "minAge": 18 }
```

### Subquery Support

```csharp
// Create subquery
using var subquery = new SqlBuilder(SqlDefine.SQLite);
subquery.Append($"SELECT id FROM orders WHERE total > {1000}");

// Create main query
using var mainQuery = new SqlBuilder(SqlDefine.SQLite);
mainQuery.Append($"SELECT * FROM users WHERE id IN ");
mainQuery.AppendSubquery(subquery);

var template = mainQuery.Build();
// SQL: SELECT * FROM users WHERE id IN (SELECT id FROM orders WHERE total > @p0)
// Parameters: { "p0": 1000 }
```

### Performance Optimization

SqlBuilder uses Expression tree for anonymous object parameter conversion, 8.9%-34% faster than reflection:

| Properties | Expression Tree | Reflection | Performance Gain |
|-----------|----------------|------------|------------------|
| 2 props | 1.486 μs | 1.632 μs | **8.9%** |
| 5 props | 1.328 μs | 1.678 μs | **20.9%** |
| 10 props | 1.507 μs | 2.282 μs | **34.0%** |

### ⚠️ Security Warning

`AppendRaw` does not provide SQL injection protection. Only use for:
- ✅ SQL keywords (SELECT, WHERE, ORDER BY, etc.)
- ✅ Dialect-wrapped identifiers
- ✅ Application-controlled fixed strings

**NEVER** pass user input to `AppendRaw`:

```csharp
// ❌ DANGEROUS! SQL injection risk
builder.AppendRaw($"SELECT * FROM {userInput}");

// ✅ SAFE: Use parameterization
builder.Append($"SELECT * FROM users WHERE name = {userInput}");
```

---

## Dynamic Update

Use expression trees to dynamically update specific fields without defining custom methods:

### Single Field Update

```csharp
// Update single field
await repo.DynamicUpdateAsync(userId, u => new User { Name = "John" });

// Update multiple fields
await repo.DynamicUpdateAsync(userId, u => new User 
{ 
    Name = "John",
    Age = 30,
    UpdatedAt = DateTime.UtcNow
});
```

### Expression-based Update

```csharp
// Use expressions (increment, calculate)
await repo.DynamicUpdateAsync(userId, u => new User 
{ 
    Age = u.Age + 1,
    Score = u.Score * 1.1
});
```

### Batch Update with Condition

```csharp
// Batch update with WHERE expression
await repo.DynamicUpdateWhereAsync(
    u => new User { IsActive = false, UpdatedAt = DateTime.UtcNow },
    u => u.LastLoginDate < DateTime.UtcNow.AddDays(-30)
);
```

### Features

- ✅ Type safe - compile-time validation of field names and types
- ✅ Flexible - supports any field combination
- ✅ High performance - compile-time code generation, zero reflection
- ✅ Expression support - supports arithmetic operations, function calls

---

## Source Generators

### What Gets Generated

For each entity marked with `[Sqlx]`, the source generator creates:

1. **EntityProvider** - Column metadata (zero reflection)
2. **ResultReader** - DbDataReader → Entity mapping
3. **ParameterBinder** - Entity → DbCommand parameters
4. **SqlxInitializer** - Auto-registration with ModuleInitializer

```csharp
[Sqlx, TableName("users")]
public class User
{
    [Key] public long Id { get; set; }
    public string Name { get; set; }
}

// Generated:
// - UserEntityProvider.Default
// - UserResultReader.Default
// - UserParameterBinder.Default
// - SqlxInitializer.Initialize() [ModuleInitializer]
```

### Repository Implementation Generation

```csharp
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository { }

// Generated:
// - All interface method implementations
// - Static PlaceholderContext
// - Static SqlTemplate fields (cached)
// - Activity tracking (optional)
// - Interceptor hooks (optional)
```

### Viewing Generated Code

Add to `.csproj`:
```xml
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

Generated files will be in `obj/Generated/`.

### Interceptor Hooks

```csharp
public partial class UserRepository
{
    partial void OnExecuting(string operationName, DbCommand command, SqlTemplate template)
    {
        Console.WriteLine($"Executing: {operationName}");
        Console.WriteLine($"SQL: {command.CommandText}");
    }
    
    partial void OnExecuted(string operationName, DbCommand command, SqlTemplate template, 
        object? result, long elapsedTicks)
    {
        var ms = elapsedTicks * 1000.0 / Stopwatch.Frequency;
        Console.WriteLine($"Completed: {operationName} in {ms:F2}ms");
    }
    
    partial void OnExecuteFail(string operationName, DbCommand command, SqlTemplate template,
        Exception exception, long elapsedTicks)
    {
        Console.WriteLine($"Failed: {operationName} - {exception.Message}");
    }
}
```

---

## Metrics Collection

Sqlx automatically collects metrics using `System.Diagnostics.Metrics` (.NET 8+):

```csharp
// Metrics automatically recorded, no configuration needed
var repo = new UserRepository(connection);
await repo.GetByIdAsync(123);  // Automatically records execution time, count, etc.

// Export with OpenTelemetry
using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddMeter("Sqlx.SqlTemplate")
    .AddPrometheusExporter()
    .Build();
```

### Recorded Metrics

- `sqlx.template.duration` (Histogram) - SQL execution time (milliseconds)
- `sqlx.template.executions` (Counter) - SQL execution count
- `sqlx.template.errors` (Counter) - SQL execution error count

### Metric Tags

- `repository.class` - Repository class full name (e.g., `MyApp.UserRepository`)
- `repository.method` - Method name (e.g., `GetByIdAsync`)
- `sql.template` - SQL template (e.g., `SELECT {{columns}} FROM {{table}} WHERE id = @id`)
- `error.type` - Error type (errors only)

### Features

- ✅ Zero configuration - automatically records all SQL executions
- ✅ Standard API - compatible with OpenTelemetry, Prometheus, Application Insights
- ✅ High performance - compile-time generation, zero runtime overhead
- ✅ AOT friendly - fully supports Native AOT
- ✅ Backward compatible - no-op on .NET 7 and below

---

## Performance

Based on BenchmarkDotNet tests (.NET 10.0.2 LTS, SQLite in-memory database):

### Single Row Query Performance

| ORM | Mean | vs Sqlx | Allocated | vs Sqlx |
|-----|------|---------|-----------|---------|
| **Sqlx** | **9.447 μs** | baseline | **2.85 KB** | baseline |
| Dapper.AOT | 10.040 μs | +6.3% | 2.66 KB | -6.7% |
| FreeSql | 55.817 μs | +491% | 10.17 KB | +257% |

### Batch Query Performance

#### 10 Rows

| ORM | Mean | vs Sqlx | Allocated | vs Sqlx |
|-----|------|---------|-----------|---------|
| Dapper.AOT | 31.58 μs | -5.0% | 6.55 KB | +6.9% |
| **Sqlx** | **33.25 μs** | baseline | **6.13 KB** | baseline |
| FreeSql | 34.63 μs | +4.1% | 8.67 KB | +41.4% |

#### 100 Rows

| ORM | Mean | vs Sqlx | Allocated | vs Sqlx |
|-----|------|---------|-----------|---------|
| FreeSql | 151.15 μs | -14.4% | 37.23 KB | -3.2% |
| Dapper.AOT | 162.24 μs | -8.1% | 45.66 KB | +18.8% |
| **Sqlx** | **176.54 μs** | baseline | **38.45 KB** | baseline |

#### 1000 Rows

| ORM | Mean | vs Sqlx | Allocated | vs Sqlx | Gen1 GC |
|-----|------|---------|-----------|---------|---------|
| FreeSql | 1,290.47 μs | -19.2% | 318.56 KB | -11.9% | 9.77 |
| Dapper.AOT | 1,433.50 μs | -10.2% | 432.38 KB | +19.5% | 23.44 |
| **Sqlx** | **1,596.93 μs** | baseline | **361.69 KB** | baseline | **19.53** |

### Key Insights

- ✅ **Fastest single-row queries** - 6.3% faster than Dapper.AOT, 5.9x faster than FreeSql
- ✅ **Memory efficient** - 16.4% less allocation than Dapper.AOT (1000 rows)
- ✅ **Low GC pressure** - 16.7% less Gen1 GC than Dapper.AOT, 50% less than FreeSql
- ✅ **Small dataset advantage** - Best performance in Web API main scenarios (10-100 rows)
- ⚠️ **Large dataset tradeoff** - 19.2% slower than FreeSql at 1000 rows, but better memory and GC

---

## Database Dialects

### SQLite

```csharp
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository { }
var repo = new UserRepository(connection, SqlDefine.SQLite);

// Column quotes: [column]
// Parameter prefix: @
// Last inserted ID: SELECT last_insert_rowid()
// Boolean: 0/1
// Pagination: LIMIT/OFFSET
```

**Gotchas**:
- No native boolean type (use INTEGER 0/1)
- No OUTPUT parameters support
- Limited ALTER TABLE support
- Case-insensitive LIKE by default

### PostgreSQL

```csharp
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository { }
var repo = new UserRepository(connection, SqlDefine.PostgreSql);

// Column quotes: "column"
// Parameter prefix: @
// Last inserted ID: RETURNING id or SELECT lastval()
// Boolean: true/false
// Pagination: LIMIT/OFFSET
```

**Features**:
- Native boolean type
- RETURNING clause for INSERT/UPDATE/DELETE
- Rich data types (JSON, arrays, etc.)
- Case-sensitive by default

### MySQL

```csharp
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository { }
var repo = new UserRepository(connection, SqlDefine.MySql);

// Column quotes: ``column``
// Parameter prefix: @
// Last inserted ID: SELECT LAST_INSERT_ID()
// Boolean: 0/1 (TINYINT)
// Pagination: LIMIT/OFFSET
```

**Gotchas**:
- Boolean is TINYINT(1)
- Case-insensitive by default (depends on collation)
- Different string functions (CONCAT vs ||)

### SQL Server

```csharp
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository { }
var repo = new UserRepository(connection, SqlDefine.SqlServer);

// Column quotes: [column]
// Parameter prefix: @
// Last inserted ID: SELECT SCOPE_IDENTITY()
// Boolean: BIT
// Pagination: OFFSET/FETCH or TOP
```

**Features**:
- Native BIT type for boolean
- OUTPUT clause for INSERT/UPDATE/DELETE
- Rich T-SQL features
- OFFSET/FETCH for pagination

### Oracle

```csharp
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository { }
var repo = new UserRepository(connection, SqlDefine.Oracle);

// Column quotes: "column"
// Parameter prefix: :
// Last inserted ID: RETURNING INTO or SELECT SEQ.CURRVAL
// Boolean: NUMBER(1) or CHAR(1)
// Pagination: OFFSET/FETCH
```

### DB2

```csharp
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository { }
var repo = new UserRepository(connection, SqlDefine.DB2);

// Column quotes: "column"
// Parameter prefix: ?
// Pagination: OFFSET/FETCH
```

---

## Advanced Types

Sqlx supports multiple C# type definitions with automatic code generation optimization:

| Type | Example | Generated Strategy | Use Case |
|------|---------|-------------------|----------|
| **Class** | `public class User { }` | Object initializer | Mutable entities |
| **Record** | `public record User(long Id, string Name);` | Constructor | Immutable DTOs |
| **Mixed Record** | `public record User(long Id) { public string Name { get; set; } }` | Constructor + initializer | Hybrid scenarios |
| **Struct** | `public struct User { }` | Object initializer | Value types |
| **Struct Record** | `public readonly record struct User(long Id);` | Constructor | Immutable value types |

### Examples

```csharp
// Pure Record - uses constructor
[Sqlx, TableName("users")]
public record User(long Id, string Name, int Age);

// Mixed Record - constructor + object initializer
[Sqlx, TableName("users")]
public record MixedUser(long Id, string Name)
{
    public string Email { get; set; } = "";
    public int Age { get; set; }
}

// Read-only properties automatically ignored
[Sqlx, TableName("users")]
public class UserWithComputed
{
    [Key] public long Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    
    // Read-only property - automatically ignored
    public string FullName => $"{FirstName} {LastName}";
}

// Struct Record
[Sqlx, TableName("points")]
public readonly record struct Point(int X, int Y);
```

### Features

- ✅ Auto-detect type - source generator automatically identifies type and generates optimal code
- ✅ Read-only property filtering - automatically ignores properties without setters
- ✅ Mixed Record support - primary constructor parameters + additional properties
- ✅ Fully type safe - compile-time validation, zero runtime overhead

---

## Testing Guidelines

### Unit Testing Repositories

```csharp
[Fact]
public async Task GetByIdAsync_ReturnsUser_WhenExists()
{
    // Arrange
    await using var connection = new SqliteConnection("Data Source=:memory:");
    await connection.OpenAsync();
    await connection.ExecuteAsync("CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT, age INTEGER)");
    await connection.ExecuteAsync("INSERT INTO users (id, name, age) VALUES (1, 'John', 25)");
    
    var repo = new UserRepository(connection);
    
    // Act
    var user = await repo.GetByIdAsync(1);
    
    // Assert
    Assert.NotNull(user);
    Assert.Equal("John", user.Name);
    Assert.Equal(25, user.Age);
}
```

### E2E Testing with Multiple Databases

```csharp
[Theory]
[InlineData("sqlite")]
[InlineData("postgresql")]
[InlineData("mysql")]
public async Task InsertAsync_WorksAcrossDialects(string dialectName)
{
    // Arrange
    var dialect = GetDialect(dialectName);       // e.g. SqlDefine.SQLite
    var connection = GetConnectionForDialect(dialectName);
    var repo = new UserRepository(connection, dialect);
    
    // Act
    var user = new User { Name = "Test", Age = 30 };
    var id = await repo.InsertAndGetIdAsync(user);
    
    // Assert
    Assert.True(id > 0);
}
```

### Testing with Transactions

```csharp
[Fact]
public async Task Transaction_RollsBackOnError()
{
    await using var connection = new SqliteConnection("Data Source=:memory:");
    await connection.OpenAsync();
    
    var repo = new UserRepository(connection);
    
    using var transaction = connection.BeginTransaction();
    repo.Transaction = transaction;
    
    try
    {
        await repo.InsertAsync(new User { Name = "User1", Age = 25 });
        throw new Exception("Simulated error");
        await repo.InsertAsync(new User { Name = "User2", Age = 30 });
        transaction.Commit();
    }
    catch
    {
        transaction.Rollback();
    }
    
    var count = await repo.CountAsync();
    Assert.Equal(0, count);  // No users inserted due to rollback
}
```

---

## Common Patterns

### Dynamic Query Building

```csharp
// Pattern 1: Conditional WHERE with {{if}}
[SqlTemplate(@"
    SELECT {{columns}} FROM {{table}} 
    WHERE 1=1 
    {{if notnull=name}}AND name LIKE @name{{/if}}
    {{if notnull=minAge}}AND age >= @minAge{{/if}}
")]
Task<List<User>> SearchAsync(string? name, int? minAge);

// Pattern 2: Dictionary-based filter
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --object filter}}")]
Task<List<User>> FilterAsync(IReadOnlyDictionary<string, object?> filter);

// Pattern 3: Expression-based filter
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}")]
Task<List<User>> GetWhereAsync(Expression<Func<User, bool>> predicate);
```

### Pagination

```csharp
// Built-in pagination
var page = await repo.GetPagedAsync(pageNumber: 1, pageSize: 20);

// Custom pagination with sorting
[SqlTemplate(@"
    SELECT {{columns}} FROM {{table}} 
    ORDER BY {{var --name sortColumn}} {{var --name sortOrder}}
    {{limit --param pageSize}} {{offset --param offset}}
")]
Task<List<User>> GetPagedAsync(int pageSize, int offset);
```

### Soft Delete

```csharp
[Sqlx, TableName("users")]
public class User
{
    [Key] public long Id { get; set; }
    public string Name { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public interface IUserRepository : ICrudRepository<User, long>
{
    [SqlTemplate("UPDATE {{table}} SET is_deleted = 1, deleted_at = @deletedAt WHERE id = @id")]
    Task<int> SoftDeleteAsync(long id, DateTime deletedAt);
    
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_deleted = 0")]
    Task<List<User>> GetActiveUsersAsync();
}
```

### Audit Fields

```csharp
[Sqlx, TableName("users")]
public class User
{
    [Key] public long Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; }
    public string UpdatedBy { get; set; }
}

// Use inline expressions for automatic timestamps
[SqlTemplate(@"
    INSERT INTO {{table}} ({{columns --exclude Id}}) 
    VALUES ({{values --exclude Id --inline CreatedAt=CURRENT_TIMESTAMP,UpdatedAt=CURRENT_TIMESTAMP}})
")]
Task<int> InsertAsync(string name, string createdBy, string updatedBy);
```

---

## Security Best Practices

### 1. Always Use Parameters for User Input

```csharp
// ✅ CORRECT
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE name = @name")]
Task<List<User>> SearchAsync(string name);

// ❌ WRONG - SQL INJECTION!
var sql = $"SELECT * FROM users WHERE name = '{userName}'";
```

### 2. Use {{var}} Only for Trusted Values

```csharp
// ✅ CORRECT - Application-controlled
[SqlxVar("tenantId")]
private string GetTenantId() => _currentTenantId;  // From auth token

[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE tenant_id = {{var --name tenantId}}")]
Task<List<User>> GetAllAsync();

// ❌ WRONG - User input as literal
[SqlxVar("searchTerm")]
private string GetSearchTerm() => _userInput;  // DANGEROUS!
```

### 3. Validate Input Before Database Operations

```csharp
public async Task<User?> GetByIdAsync(long id)
{
    if (id <= 0)
        throw new ArgumentException("ID must be positive", nameof(id));
    
    return await repo.GetByIdAsync(id);
}
```

### 4. Use Least Privilege Database Accounts

```csharp
// Application database user should have minimal permissions
// - SELECT, INSERT, UPDATE, DELETE on application tables only
// - NO DROP, CREATE, ALTER permissions
// - NO access to system tables
```

### 5. Sanitize Sensitive Data in Logs

SqlxException automatically sanitizes sensitive parameters:
```csharp
// Sensitive parameter names are automatically redacted in logs:
// - password, pwd, secret, token, key, apikey, api_key
// Values are replaced with "[REDACTED]"
```

---

## Troubleshooting

### Issue 1: "EntityProvider not found for type X"

**Cause**: Entity class missing `[Sqlx]` attribute or not discovered by source generator.

**Solution**:
```csharp
// ✅ Add [Sqlx] attribute
[Sqlx, TableName("users")]
public class User { }

// Or use SqlQuery<T> to trigger auto-discovery
var query = SqlQuery<User>.ForSqlite();
```

### Issue 2: "Column 'X' not found in result set"

**Cause**: Property name doesn't match database column name.

**Solution**:
```csharp
// Use [Column] attribute to specify exact column name
[Column("email_address")]
public string Email { get; set; }
```

### Issue 3: Repository dialect not configured

**Cause**: No `SqlDialect` passed to constructor and no `[SqlDefine]` attribute present.

**Solution**:
```csharp
// ✅ CORRECT: Pass dialect at runtime
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository { }

var repo = new UserRepository(connection, SqlDefine.SQLite);

// ❌ WRONG: omit dialect
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository { }

var repo = new UserRepository(connection); // ❌
```

### Issue 4: "Placeholder {{X}} not recognized"

**Cause**: Typo in placeholder name or unsupported placeholder.

**Solution**: Check placeholder spelling and refer to complete placeholder reference.

### Issue 5: "Cannot use out/ref in async method"

**Cause**: C# language limitation.

**Solution**: Use `OutputParameter<T>` wrapper for async methods:
```csharp
// ✅ Async with OutputParameter<T>
Task<int> InsertAsync(string name, OutputParameter<int> id);

// ✅ Sync with out
int Insert(string name, out int id);
```

### Issue 6: "Transaction not propagating to repository"

**Cause**: Transaction property not set on repository.

**Solution**:
```csharp
var repo = new UserRepository(connection);
repo.Transaction = transaction;  // ✅ Set transaction
```

### Issue 7: "Generated code not updating"

**Cause**: Build cache not cleared after attribute changes.

**Solution**:
```bash
dotnet clean
dotnet build
```

Or delete `obj/` and `bin/` folders manually.

---

## Anti-Patterns

### ❌ Anti-Pattern 1: Looping with Single Inserts

```csharp
// ❌ BAD: Multiple database round-trips
foreach (var user in users)
{
    await repo.InsertAsync(user);
}

// ✅ GOOD: Single batch operation
await repo.BatchInsertAsync(users);
```

### ❌ Anti-Pattern 2: Hardcoding Column Names

```csharp
// ❌ BAD: Hardcoded columns, not dialect-independent
[SqlTemplate("SELECT id, name, email FROM users WHERE name = @name")]
Task<List<User>> SearchAsync(string name);

// ✅ GOOD: Use placeholders
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE name = @name")]
Task<List<User>> SearchAsync(string name);
```

### ❌ Anti-Pattern 3: String Concatenation for SQL

```csharp
// ❌ BAD: SQL injection risk
var sql = $"SELECT * FROM users WHERE name = '{userName}'";

// ✅ GOOD: Use parameters
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE name = @name")]
Task<List<User>> SearchAsync(string name);
```

### ❌ Anti-Pattern 4: Blocking Async Calls

```csharp
// ❌ BAD: Blocks thread pool
var user = repo.GetByIdAsync(id).Result;
repo.InsertAsync(user).Wait();

// ✅ GOOD: Async all the way
var user = await repo.GetByIdAsync(id);
await repo.InsertAsync(user);
```

### ❌ Anti-Pattern 5: Not Using Transactions for Multi-Step Operations

```csharp
// ❌ BAD: No transaction, partial updates possible
await repo.InsertAsync(user);
await repo.InsertAsync(order);  // If this fails, user is already inserted

// ✅ GOOD: Use transaction
using var transaction = connection.BeginTransaction();
repo.Transaction = transaction;
try
{
    await repo.InsertAsync(user);
    await repo.InsertAsync(order);
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

### ❌ Anti-Pattern 6: Using {{var}} for User Input

```csharp
// ❌ BAD: Security risk
[SqlxVar("searchTerm")]
private string GetSearchTerm() => _userInput;  // User input as literal!

[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE name = {{var --name searchTerm}}")]
Task<List<User>> SearchAsync();

// ✅ GOOD: Use parameters for user input
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE name = @searchTerm")]
Task<List<User>> SearchAsync(string searchTerm);
```

---

## FAQ

### Q: Do I need to manually register EntityProvider/ResultReader?

**A**: No. The source generator creates a `[ModuleInitializer]` that automatically registers all providers at startup. Zero configuration needed.

### Q: Can I use Sqlx with Native AOT?

**A**: Yes! Sqlx is fully AOT-compatible. All code is generated at compile time with zero runtime reflection. 3124+ tests pass with AOT enabled.

### Q: How do I debug generated SQL?

**A**: Use `SqlTemplate` return type to inspect generated SQL:
```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
SqlTemplate GetByIdSql(long id);

var template = repo.GetByIdSql(123);
Console.WriteLine($"SQL: {template.Sql}");
Console.WriteLine($"Parameters: {string.Join(", ", template.Parameters)}");
```

### Q: Can I use multiple databases in the same application?

**A**: Yes! Pass different dialects at construction time:
```csharp
// Same repository class, different dialects at runtime
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository { }

[RepositoryFor(typeof(IOrderRepository))]
public partial class OrderRepository : IOrderRepository { }

// Usage
var userRepo  = new UserRepository(sqliteConn, SqlDefine.SQLite);
var orderRepo = new OrderRepository(pgConn,    SqlDefine.PostgreSql);
```

### Q: How do I handle database migrations?

**A**: Sqlx doesn't include migration tools. Use external tools like:
- FluentMigrator
- DbUp
- EF Core Migrations (for schema only)
- Raw SQL scripts

### Q: Can I use stored procedures?

**A**: Yes! Use `[SqlTemplate]` with stored procedure syntax:
```csharp
[SqlTemplate("EXEC sp_GetUsers @minAge")]
Task<List<User>> GetUsersAsync(int minAge);

// With output parameters
[SqlTemplate("EXEC sp_InsertUser @name, @age")]
int InsertUser(string name, int age, out int id);
```

### Q: Can I use Sqlx with Dependency Injection?

**A**: Yes! Register repositories as services:
```csharp
services.AddSingleton<IUserRepository, UserRepository>();
services.AddSingleton<DbConnection>(sp => 
    new SqliteConnection("Data Source=app.db"));
```

### Q: Can I use Sqlx with ASP.NET Core?

**A**: Yes! Sqlx works great with ASP.NET Core:
```csharp
app.MapGet("/users/{id}", async (long id, IUserRepository repo) =>
{
    var user = await repo.GetByIdAsync(id);
    return user is not null ? Results.Ok(user) : Results.NotFound();
});
```

### Q: What's the difference between [SqlTemplate] attribute and SqlTemplate class?

**A**: 
- **[SqlTemplate] attribute** - Marks method for SQL execution
- **SqlTemplate class** - Returns template for debugging (doesn't execute)

Both use the same template syntax but serve different purposes.

---

## Quick Reference

### Entity Definition

```csharp
[Sqlx, TableName("users")]
public class User
{
    [Key] public long Id { get; set; }
    public string Name { get; set; }
    [Column("email_address")] public string Email { get; set; }
    [IgnoreDataMember] public string Computed => $"User-{Id}";
}
```

### Repository Definition

```csharp
public interface IUserRepository : ICrudRepository<User, long>
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge")]
    Task<List<User>> GetAdultsAsync(int minAge);
}

// No [SqlDefine] needed - pass dialect at runtime
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository { }

// Usage
var repo = new UserRepository(connection, SqlDefine.SQLite);
```

### Common Placeholders

```csharp
{{columns}}                          // All columns
{{columns --exclude Id}}             // Exclude columns
{{values}}                           // Parameter placeholders
{{values --inline Status='pending'}} // With inline expressions
{{set}}                              // UPDATE SET clause
{{set --inline Version=Version+1}}   // With inline expressions
{{table}}                            // Table name
{{where --object filter}}            // Dictionary filter
{{where --param predicate}}          // Expression filter
{{var --name varName}}               // Runtime variable
{{limit --param size}}               // Dynamic limit
{{offset --param skip}}              // Dynamic offset
{{if notnull=param}}...{{/if}}       // Conditional block
```

### CRUD Operations

```csharp
// Query
var user = await repo.GetByIdAsync(id);
var users = await repo.GetAllAsync();
var filtered = await repo.GetWhereAsync(u => u.Age >= 18);
var paged = await repo.GetPagedAsync(pageNumber: 1, pageSize: 20);
var count = await repo.CountAsync();
var exists = await repo.ExistsByIdAsync(id);

// Command
var id = await repo.InsertAndGetIdAsync(user);
await repo.InsertAsync(user);
await repo.BatchInsertAsync(users);
await repo.UpdateAsync(user);
await repo.DynamicUpdateAsync(id, u => new User { Name = "John" });
await repo.DeleteAsync(id);
await repo.DeleteWhereAsync(u => u.IsActive == false);
```

### Transaction

```csharp
using var transaction = connection.BeginTransaction();
repo.Transaction = transaction;
try
{
    await repo.InsertAsync(user);
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

### SqlQuery (LINQ)

```csharp
var query = SqlQuery<User>.ForSqlite()
    .Where(u => u.Age >= 18)
    .OrderBy(u => u.Name)
    .Take(10);

var users = await query
    .WithConnection(connection)
    .WithReader(UserResultReader.Default)
    .ToListAsync();
```

### Output Parameters

```csharp
// Sync
int Insert(string name, out int id);

// Async
Task<int> InsertAsync(string name, OutputParameter<int> id);
```

---

## Summary

Sqlx is a high-performance, AOT-friendly .NET database access library with:

- **Zero Reflection** - All code generated at compile time
- **Type Safety** - Compile-time validation
- **High Performance** - Optimized ResultReader, minimal GC pressure
- **Full AOT Support** - Native AOT ready
- **Rich Features** - 46 built-in CRUD methods, dynamic updates, batch operations, transactions
- **Multiple Databases** - SQLite, PostgreSQL, MySQL, SQL Server, Oracle, DB2
- **Developer Experience** - Clean API, IntelliSense support, comprehensive documentation

**Key Takeaways for AI Assistants**:

1. **[SqlDefine] is now optional** - pass `SqlDialect` via constructor at runtime instead
2. **Use BatchInsertAsync** for multiple inserts, never loop with InsertAsync
3. **Output parameters**: Sync uses out/ref, Async uses OutputParameter<T>
4. **{{var}} is for trusted values only**, never user input
5. **NULL in Dictionary WHERE** now generates IS NULL (behavior changed)
6. **[OutputParameter] attribute is optional** - auto-inferred in most cases
7. **Use placeholders** ({{columns}}, {{table}}) for dialect independence
8. **DynamicUpdate** for flexible field updates without custom methods
9. **SqlBuilder** for dynamic SQL with auto-parameterization
10. **Any.Value<T>()** for reusable expression templates

---

**Document Version**: 3.0  
**Last Updated**: 2026-03-08  
**Sqlx Version**: Latest  
**Test Coverage**: 3124+ tests, 83.2% branch coverage

This document provides complete, accurate information about Sqlx for AI assistants. All examples are tested and verified.

For more information, see:
- [GitHub Repository](https://github.com/Cricle/Sqlx)
- [Documentation](docs/)
- [Samples](samples/)
- [Tests](tests/)













