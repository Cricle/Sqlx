# Sqlx Documentation

Sqlx is a high-performance, AOT-compatible SQL generation library for .NET. It uses source generators to produce reflection-free code at compile time, making it ideal for Native AOT scenarios.

## Features

- **AOT Compatible**: All code is generated at compile time, no reflection required
- **High Performance**: Minimal allocations, pre-computed SQL templates
- **Multi-Dialect Support**: SQLite, MySQL, PostgreSQL, SQL Server, Oracle, DB2
- **Type Safe**: Compile-time validation of SQL templates
- **Dynamic SQL**: Conditional blocks (`{{if}}`) for flexible query building
- **Batch Execution**: Efficient batch insert/update/delete with `DbBatch` (.NET 6+) and fallback
- **Extensible**: Custom placeholder handlers and dialects

## Quick Start

### 1. Define Your Entity

```csharp
using Sqlx.Annotations;

[SqlxEntity]
[SqlxParameter]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}
```

### 2. Define Your Repository Interface

```csharp
public interface IUserRepository : ICrudRepository<User, int>
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE email = @email")]
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
}
```

### 3. Implement Your Repository

```csharp
[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("users")]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository
{
    private readonly DbConnection _connection;
    public DbTransaction? Transaction { get; set; }

    public UserRepository(DbConnection connection)
    {
        _connection = connection;
    }
}
```

### 4. Use Your Repository

```csharp
await using var connection = new SqliteConnection("Data Source=app.db");
await connection.OpenAsync();

var repo = new UserRepository(connection);
var user = await repo.GetByEmailAsync("test@example.com");
```

## Documentation

- [Getting Started](getting-started.md) - Installation and basic setup
- [SQL Templates](sql-templates.md) - Template syntax and placeholders
- [Dialects](dialects.md) - Database dialect support
- [Source Generators](source-generators.md) - How code generation works
- [API Reference](api-reference.md) - Complete API documentation

## Batch Execution

Efficiently execute batch operations using `DbBatch` on .NET 6+ with automatic fallback:

```csharp
var users = new List<User>
{
    new() { Name = "Alice", Email = "alice@test.com" },
    new() { Name = "Bob", Email = "bob@test.com" }
};

var sql = "INSERT INTO users (name, email) VALUES (@name, @email)";
var affected = await connection.ExecuteBatchAsync(
    sql, 
    users, 
    UserParameterBinder.Default,
    batchSize: 100);
```

## Benchmarks

Sqlx consistently outperforms Dapper.AOT in benchmarks:

| Operation | Sqlx | Dapper.AOT | Improvement |
|-----------|------|------------|-------------|
| SelectSingle | 7.73μs | 9.79μs | 27% faster |
| Insert | 56.82μs | 76.25μs | 34% faster |
| Update | 7.51μs | 9.95μs | 32% faster |

## License

MIT License - see [LICENSE](../License.txt) for details.
