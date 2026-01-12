# Multi-Database Guide

Learn how to write code that works across multiple databases.

## Overview

Sqlx allows you to write SQL templates once and run them on different databases. This is achieved through:

1. **Dialect-specific placeholders** - Automatically adapt to database syntax
2. **Multiple implementations** - One interface, multiple database-specific classes
3. **Compile-time generation** - Zero runtime overhead

## Quick Example

```csharp
// Define interface once
public interface IUserRepository
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = {{bool_true}}")]
    Task<List<User>> GetActiveUsersAsync();
}

// Implement for SQLite
[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("users")]
[RepositoryFor(typeof(IUserRepository))]
public partial class SqliteUserRepo(DbConnection conn) : IUserRepository { }

// Implement for PostgreSQL
[SqlDefine(SqlDefineTypes.PostgreSql)]
[TableName("users")]
[RepositoryFor(typeof(IUserRepository))]
public partial class PostgresUserRepo(DbConnection conn) : IUserRepository { }
```

**Generated SQL:**
- SQLite: `SELECT id, name FROM users WHERE is_active = 1`
- PostgreSQL: `SELECT "id", "name" FROM "users" WHERE is_active = true`

## Supported Databases

| Database   | Enum Value | Status |
|------------|------------|--------|
| SQLite     | `SqlDefineTypes.SQLite` | ✅ Production Ready |
| PostgreSQL | `SqlDefineTypes.PostgreSql` | ✅ Production Ready |
| MySQL      | `SqlDefineTypes.MySql` | ✅ Production Ready |
| SQL Server | `SqlDefineTypes.SqlServer` | ✅ Production Ready |

## Dialect Differences

### Identifier Quoting

Different databases use different quote characters:

| Database   | Quote Style | Example |
|------------|-------------|---------|
| SQLite     | `[...]` | `[users]`, `[id]` |
| PostgreSQL | `"..."` | `"users"`, `"id"` |
| MySQL      | `` `...` `` | `` `users` ``, `` `id` `` |
| SQL Server | `[...]` | `[users]`, `[id]` |

**Use `{{table}}` and `{{columns}}` placeholders** to handle this automatically.

---

### Boolean Literals

| Database   | True | False |
|------------|------|-------|
| SQLite     | `1` | `0` |
| PostgreSQL | `true` | `false` |
| MySQL      | `1` | `0` |
| SQL Server | `1` | `0` |

**Use `{{bool_true}}` and `{{bool_false}}` placeholders.**

---

### Current Timestamp

| Database   | Function |
|------------|----------|
| SQLite     | `CURRENT_TIMESTAMP` |
| PostgreSQL | `CURRENT_TIMESTAMP` |
| MySQL      | `NOW()` |
| SQL Server | `GETDATE()` |

**Use `{{current_timestamp}}` placeholder.**

---

### Returning Inserted ID

Different databases have different ways to return auto-generated IDs:

| Database   | Syntax |
|------------|--------|
| SQLite     | `SELECT last_insert_rowid()` (automatic) |
| PostgreSQL | `RETURNING id` |
| MySQL      | `SELECT LAST_INSERT_ID()` (automatic) |
| SQL Server | `OUTPUT INSERTED.id` |

**Use `[ReturnInsertedId]` attribute** - Sqlx handles this automatically.

---

### Parameter Prefixes

| Database   | Prefix | Example |
|------------|--------|---------|
| SQLite     | `@` | `@id`, `@name` |
| PostgreSQL | `@` or `$` | `@id`, `$1` |
| MySQL      | `@` | `@id`, `@name` |
| SQL Server | `@` | `@id`, `@name` |

**Use `@parameter` syntax** - Sqlx handles conversion automatically.

---

## Writing Portable SQL

### Pattern 1: Use Dialect Placeholders

Replace database-specific syntax with placeholders:

```csharp
// ❌ Not portable
[SqlTemplate("SELECT * FROM users WHERE is_active = 1")]

// ✅ Portable
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = {{bool_true}}")]
```

---

### Pattern 2: Avoid Database-Specific Functions

Some functions are database-specific:

```csharp
// ❌ Not portable (SQL Server specific)
[SqlTemplate("SELECT GETDATE()")]

// ✅ Portable
[SqlTemplate("SELECT {{current_timestamp}}")]
```

---

### Pattern 3: Use Standard SQL When Possible

Stick to SQL features supported by all databases:

```csharp
// ✅ Standard SQL - works everywhere
[SqlTemplate(@"
    SELECT {{columns}} 
    FROM {{table}} 
    WHERE age >= @minAge 
    ORDER BY created_at DESC 
    LIMIT @limit
")]
```

---

## Implementation Patterns

### Pattern 1: Single Interface, Multiple Implementations

**Best for:** Applications that need to support multiple databases

```csharp
// Define interface once
public interface IUserRepository
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(long id);
    
    [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
    [ReturnInsertedId]
    Task<long> InsertAsync(User user);
}

// SQLite implementation
[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("users")]
[RepositoryFor(typeof(IUserRepository))]
public partial class SqliteUserRepository(DbConnection conn) : IUserRepository { }

// PostgreSQL implementation
[SqlDefine(SqlDefineTypes.PostgreSql)]
[TableName("users")]
[RepositoryFor(typeof(IUserRepository))]
public partial class PostgresUserRepository(DbConnection conn) : IUserRepository { }

// MySQL implementation
[SqlDefine(SqlDefineTypes.MySql)]
[TableName("users")]
[RepositoryFor(typeof(IUserRepository))]
public partial class MySqlUserRepository(DbConnection conn) : IUserRepository { }

// SQL Server implementation
[SqlDefine(SqlDefineTypes.SqlServer)]
[TableName("users")]
[RepositoryFor(typeof(IUserRepository))]
public partial class SqlServerUserRepository(DbConnection conn) : IUserRepository { }
```

**Usage with Dependency Injection:**

```csharp
// Startup configuration
services.AddScoped<IUserRepository>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var dbType = config["Database:Type"];
    var connectionString = config["Database:ConnectionString"];
    
    return dbType switch
    {
        "SQLite" => new SqliteUserRepository(new SqliteConnection(connectionString)),
        "PostgreSQL" => new PostgresUserRepository(new NpgsqlConnection(connectionString)),
        "MySQL" => new MySqlUserRepository(new MySqlConnection(connectionString)),
        "SqlServer" => new SqlServerUserRepository(new SqlConnection(connectionString)),
        _ => throw new NotSupportedException($"Database type {dbType} not supported")
    };
});
```

---

### Pattern 2: Generic Repository with Dialect Parameter

**Best for:** Shared repository logic across databases

```csharp
public interface ICrudRepository<TEntity, TKey>
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<TEntity?> GetByIdAsync(TKey id);
    
    [SqlTemplate("SELECT {{columns}} FROM {{table}}")]
    Task<List<TEntity>> GetAllAsync();
    
    [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
    [ReturnInsertedId]
    Task<long> InsertAsync(TEntity entity);
}

// Implement for each database
[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("users")]
[RepositoryFor(typeof(ICrudRepository<User, long>))]
public partial class SqliteUserCrudRepo(DbConnection conn) : ICrudRepository<User, long> { }

[SqlDefine(SqlDefineTypes.PostgreSql)]
[TableName("users")]
[RepositoryFor(typeof(ICrudRepository<User, long>))]
public partial class PostgresUserCrudRepo(DbConnection conn) : ICrudRepository<User, long> { }
```

---

### Pattern 3: Database-Specific Repositories

**Best for:** When you need database-specific optimizations

```csharp
// Common interface
public interface IUserRepository
{
    Task<User?> GetByIdAsync(long id);
    Task<long> InsertAsync(User user);
}

// SQLite-specific implementation with optimizations
[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("users")]
public interface ISqliteUserRepository : IUserRepository
{
    // SQLite-specific: Use FTS5 for full-text search
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{table}} MATCH @query")]
    Task<List<User>> FullTextSearchAsync(string query);
}

[RepositoryFor(typeof(ISqliteUserRepository))]
public partial class SqliteUserRepository(DbConnection conn) : ISqliteUserRepository { }

// PostgreSQL-specific implementation
[SqlDefine(SqlDefineTypes.PostgreSql)]
[TableName("users")]
public interface IPostgresUserRepository : IUserRepository
{
    // PostgreSQL-specific: Use tsvector for full-text search
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE to_tsvector('english', name) @@ to_tsquery(@query)")]
    Task<List<User>> FullTextSearchAsync(string query);
}

[RepositoryFor(typeof(IPostgresUserRepository))]
public partial class PostgresUserRepository(DbConnection conn) : IPostgresUserRepository { }
```

---

## Testing Multi-Database Code

### Strategy 1: Test Against All Databases

```csharp
public class UserRepositoryTests
{
    [Theory]
    [InlineData("SQLite")]
    [InlineData("PostgreSQL")]
    [InlineData("MySQL")]
    [InlineData("SqlServer")]
    public async Task InsertAsync_ShouldReturnId(string dbType)
    {
        // Arrange
        var connection = CreateConnection(dbType);
        var repo = CreateRepository(dbType, connection);
        
        // Act
        var userId = await repo.InsertAsync(new User { Name = "Alice" });
        
        // Assert
        Assert.True(userId > 0);
    }
    
    private DbConnection CreateConnection(string dbType) => dbType switch
    {
        "SQLite" => new SqliteConnection("Data Source=:memory:"),
        "PostgreSQL" => new NpgsqlConnection(TestConfig.PostgresConnectionString),
        "MySQL" => new MySqlConnection(TestConfig.MySqlConnectionString),
        "SqlServer" => new SqlConnection(TestConfig.SqlServerConnectionString),
        _ => throw new NotSupportedException()
    };
    
    private IUserRepository CreateRepository(string dbType, DbConnection conn) => dbType switch
    {
        "SQLite" => new SqliteUserRepository(conn),
        "PostgreSQL" => new PostgresUserRepository(conn),
        "MySQL" => new MySqlUserRepository(conn),
        "SqlServer" => new SqlServerUserRepository(conn),
        _ => throw new NotSupportedException()
    };
}
```

---

### Strategy 2: Use SQLite for Fast Tests

```csharp
[Fact]
public async Task InsertAsync_ShouldReturnId()
{
    // Use SQLite in-memory for fast tests
    await using var connection = new SqliteConnection("Data Source=:memory:");
    await connection.OpenAsync();
    
    await connection.ExecuteAsync("CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT)");
    
    var repo = new SqliteUserRepository(connection);
    var userId = await repo.InsertAsync(new User { Name = "Alice" });
    
    Assert.True(userId > 0);
}
```

---

## Migration Strategies

### From Single Database to Multi-Database

**Step 1:** Extract interface from existing repository

```csharp
// Before
[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("users")]
public partial class UserRepository(DbConnection conn)
{
    [SqlTemplate("SELECT * FROM users WHERE id = @id")]
    public Task<User?> GetByIdAsync(long id);
}

// After
public interface IUserRepository
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(long id);
}

[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("users")]
[RepositoryFor(typeof(IUserRepository))]
public partial class SqliteUserRepository(DbConnection conn) : IUserRepository { }
```

**Step 2:** Replace hardcoded SQL with placeholders

```csharp
// Before
[SqlTemplate("SELECT id, name, email FROM users WHERE is_active = 1")]

// After
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = {{bool_true}}")]
```

**Step 3:** Add implementations for other databases

```csharp
[SqlDefine(SqlDefineTypes.PostgreSql)]
[TableName("users")]
[RepositoryFor(typeof(IUserRepository))]
public partial class PostgresUserRepository(DbConnection conn) : IUserRepository { }
```

---

## Common Pitfalls

### ❌ Hardcoding Database-Specific Syntax

```csharp
// ❌ Wrong - SQL Server specific
[SqlTemplate("SELECT GETDATE()")]

// ✅ Correct - portable
[SqlTemplate("SELECT {{current_timestamp}}")]
```

---

### ❌ Using Database-Specific Functions

```csharp
// ❌ Wrong - PostgreSQL specific
[SqlTemplate("SELECT * FROM users WHERE name ILIKE @pattern")]

// ✅ Correct - use standard LIKE
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE LOWER(name) LIKE LOWER(@pattern)")]
```

---

### ❌ Forgetting to Test on All Databases

Always test your code on all target databases before deploying.

---

## See Also

- [Quick Start Guide](QUICK_START.md)
- [Placeholder Reference](PLACEHOLDER_REFERENCE.md)
- [API Reference](API_REFERENCE.md)
- [Best Practices](BEST_PRACTICES.md)
