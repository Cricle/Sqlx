# Sqlx AI Assistant Guide

This document provides guidance for AI assistants working with the Sqlx library.

## Project Overview

Sqlx is a high-performance, AOT-friendly .NET database access library that uses source generators to generate code at compile time with zero runtime reflection.

### Key Features
- **Zero Reflection** - All code generated at compile time via source generators
- **AOT Ready** - Full Native AOT support with 3124+ passing tests
- **Type Safe** - Compile-time validation of SQL templates and expressions
- **Multi-Database** - SQLite, PostgreSQL, MySQL, SQL Server, Oracle, DB2
- **High Performance** - Optimized ResultReader, up to 5.8% faster than Dapper.AOT in some scenarios
- **LINQ Support** - IQueryable interface with Where/Select/OrderBy/Join support

## Core Concepts

### 1. Entity Definition

Entities can be defined as `class`, `record`, or `struct`:

```csharp
[Sqlx, TableName("users")]
public class User
{
    [Key] public long Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}

// Record type
[Sqlx, TableName("users")]
public record UserRecord(long Id, string Name, int Age);

// Struct type
[Sqlx, TableName("users")]
public struct UserStruct
{
    [Key] public long Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}
```

### 2. Repository Pattern

#### Interface Definition
```csharp
public interface IUserRepository : ICrudRepository<User, long>
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge")]
    Task<List<User>> GetAdultsAsync(int minAge);
    
    // Output parameters (sync methods only)
    [SqlTemplate("INSERT INTO {{table}} (name, age) VALUES (@name, @age)")]
    int InsertAndGetId(string name, int age, [OutputParameter(DbType.Int32)] out int id);
}
```

#### Implementation
```csharp
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository { }
```

**Important**: The `SqlDefine` attribute should be on the Repository implementation, NOT on SqlxContext.

### 3. SqlxContext (NEW)

SqlxContext provides unified transaction management across multiple repositories:

```csharp
[SqlxContext]
[IncludeRepository(typeof(UserRepository))]
[IncludeRepository(typeof(OrderRepository))]
public partial class AppDbContext : SqlxContext
{
    // Auto-generated properties:
    // public UserRepository Users { get; }
    // public OrderRepository Orders { get; }
}

// Usage with transactions
await using var context = new AppDbContext(connection, options, serviceProvider);
await using var transaction = await context.BeginTransactionAsync();
try
{
    await context.Users.InsertAsync(user);
    await context.Orders.InsertAsync(order);
    await transaction.CommitAsync();
}
catch
{
    // Transaction automatically rolled back on dispose
    throw;
}
```

**Key Points**:
- SqlxContext does NOT need `[SqlDefine]` attribute
- SQL dialect is configured on individual Repository classes
- Repositories are lazily resolved from IServiceProvider
- All repositories share the same transaction

### 4. SQL Templates

#### Placeholder System

Sqlx uses a powerful placeholder system for dynamic SQL generation:

```csharp
// {{columns}} - Generates column list
// {{table}} - Generates quoted table name
// {{values}} - Generates parameter placeholders for INSERT
// {{set}} - Generates SET clause for UPDATE
// {{where --param predicate}} - Dynamic WHERE clause from parameter
// {{where --object filter}} - WHERE clause from dictionary (AOT compatible)
// {{limit --count 10}} - Static LIMIT
// {{limit --param count}} - Dynamic LIMIT
// {{offset --param skip}} - Dynamic OFFSET
// {{if notnull=param}}...{{/if}} - Conditional blocks
```

#### Dictionary WHERE with NULL Support (NEW)

As of the latest version, dictionary WHERE conditions now support NULL values:

```csharp
// NULL values generate "IS NULL" conditions
var filter = new Dictionary<string, object?> 
{ 
    ["Name"] = "John",
    ["Age"] = null  // Generates: [age] IS NULL
};

// Template
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --object filter}}")]
Task<List<User>> SearchAsync(Dictionary<string, object?> filter);

// Generated SQL: SELECT [id], [name], [age] FROM [users] WHERE ([name] = @name AND [age] IS NULL)
```

**Previous Behavior**: NULL values were skipped
**New Behavior**: NULL values generate `IS NULL` conditions

### 5. Batch Operations

#### Batch Insert
```csharp
var users = new List<User> 
{ 
    new() { Name = "Alice", Age = 25 },
    new() { Name = "Bob", Age = 30 }
};

// Use BatchInsertAsync for efficient batch insertion
await context.Users.BatchInsertAsync(users, cancellationToken);
```

**Important**: Use `BatchInsertAsync` instead of looping with `InsertAsync` for better performance.

### 6. Output Parameters

Output parameters are supported in **synchronous methods only** (C# limitation):

```csharp
public interface IUserRepository
{
    // ✅ Correct - Sync method with out parameter
    [SqlTemplate("INSERT INTO {{table}} (name) VALUES (@name); SELECT LAST_INSERT_ROWID()")]
    int InsertAndGetId(string name, [OutputParameter(DbType.Int64)] out long id);
    
    // ❌ Wrong - Async methods cannot use out/ref parameters
    Task<int> InsertAndGetIdAsync(string name, [OutputParameter] out long id);
}
```

## Common Patterns

### 1. CRUD Operations

```csharp
// Create
var id = await repo.InsertAndGetIdAsync(user);

// Read
var user = await repo.GetByIdAsync(id);
var users = await repo.GetAllAsync(limit: 100);

// Update
await repo.UpdateAsync(user);
await repo.DynamicUpdateAsync(id, u => new User { Age = 26 });

// Delete
await repo.DeleteAsync(id);
await repo.DeleteByIdsAsync(new[] { 1L, 2L, 3L });
```

### 2. Dynamic Queries

```csharp
// Using SqlQuery<T>
var query = SqlQuery<User>.ForSqlite()
    .Where(u => u.Age >= 18)
    .OrderBy(u => u.Name)
    .Take(10);

var sql = query.ToSql();
var users = await query.ToListAsync(connection);
```

### 3. Transaction Management

```csharp
// Option 1: Using SqlxContext (Recommended)
await using var context = new AppDbContext(connection, options, serviceProvider);
await using var transaction = await context.BeginTransactionAsync();
try
{
    await context.Users.InsertAsync(user);
    await context.Orders.InsertAsync(order);
    await transaction.CommitAsync();
}
catch
{
    // Auto rollback on dispose
    throw;
}

// Option 2: Manual transaction
await using var transaction = await connection.BeginTransactionAsync();
try
{
    repo.Transaction = transaction;
    await repo.InsertAsync(user);
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

## Testing Guidelines

### Unit Tests
- Test specific examples and edge cases
- Focus on integration points between components
- Verify error conditions

### Property-Based Tests
- Test universal properties across all inputs
- Use minimum 100 iterations per test
- Tag tests with feature and property references

Example:
```csharp
[TestMethod]
public void Property_RoundTrip_ParseThenPrint()
{
    // Feature: parser, Property 1: Round trip consistency
    var property = Prop.ForAll<ValidAst>(ast =>
    {
        var printed = Printer.Print(ast);
        var parsed = Parser.Parse(printed);
        return ast.Equals(parsed);
    });
    
    property.QuickCheckThrowOnFailure();
}
```

## Best Practices

### 1. Attribute Usage
- ✅ Use `[SqlDefine]` on Repository implementations
- ❌ Do NOT use `[SqlDefine]` on SqlxContext
- ✅ Use `[SqlxContext]` and `[IncludeRepository]` for context classes

### 2. Batch Operations
- ✅ Use `BatchInsertAsync` for multiple inserts
- ❌ Avoid looping with `InsertAsync`

### 3. NULL Handling
- ✅ Dictionary WHERE now supports NULL values (generates `IS NULL`)
- ✅ Use NULL in filters to search for NULL columns
- ❌ Don't skip NULL values in filters (old behavior)

### 4. Output Parameters
- ✅ Use output parameters in synchronous methods
- ❌ Don't use output parameters in async methods (C# limitation)

### 5. SQL Writing
- ✅ Use Sqlx's ORM methods and placeholders
- ❌ Don't write raw SQL strings manually
- ✅ Use `{{where --object filter}}` for dynamic WHERE clauses

## Recent Changes

### Version History

#### Latest (2024)
- **Dictionary WHERE NULL Support**: NULL values now generate `IS NULL` conditions instead of being skipped
- **SqlxContext Cleanup**: Removed unnecessary `[SqlDefine]` attribute from SqlxContext
- **Batch Insert**: Improved bulk insert endpoint to use `BatchInsertAsync`

## File Structure

```
Sqlx/
├── src/
│   ├── Sqlx/                          # Core library
│   │   ├── Annotations/               # Attributes
│   │   ├── Placeholders/              # SQL placeholder handlers
│   │   └── ...
│   └── Sqlx.Generator/                # Source generator
├── tests/
│   └── Sqlx.Tests/                    # Unit and E2E tests
├── samples/
│   └── TodoWebApi/                    # Example application
└── docs/                              # Documentation
```

## Troubleshooting

### Common Issues

1. **"SqlDefine on SqlxContext"**
   - Remove `[SqlDefine]` from SqlxContext
   - Add it to Repository implementations instead

2. **"Batch insert not working"**
   - Use `BatchInsertAsync` instead of looping
   - Ensure you're passing a List or IEnumerable

3. **"NULL values ignored in WHERE"**
   - Update to latest version
   - NULL values now generate `IS NULL` conditions

4. **"Output parameter in async method"**
   - C# doesn't support out/ref in async methods
   - Use synchronous methods for output parameters

## Resources

- [Getting Started](docs/getting-started.md)
- [SQL Templates](docs/sql-templates.md)
- [SqlxContext Documentation](docs/sqlx-context.md)
- [Source Generators](docs/source-generators.md)
- [Benchmarks](docs/benchmarks.md)
- [API Reference](docs/api-reference.md)
- [Sample Project](samples/TodoWebApi/)

## Contributing

When contributing to Sqlx:
1. Follow existing code patterns
2. Add unit tests for new features
3. Update documentation
4. Ensure all tests pass
5. Follow C# coding conventions

## Support

For issues, questions, or contributions, please visit the GitHub repository.
