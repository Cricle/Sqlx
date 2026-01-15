# Sqlx Documentation

Sqlx is a high-performance, AOT-compatible SQL generation library for .NET. It uses source generators to produce reflection-free code at compile time, making it ideal for Native AOT scenarios.

## Features

- **完全 AOT 兼容**: 所有代码在编译时生成，无需反射（✅ 817 个单元测试通过）
- **高性能**: 最小内存分配，预编译 SQL 模板，静态方法缓存
- **多数据库支持**: SQLite, MySQL, PostgreSQL, SQL Server, Oracle, DB2
- **类型安全**: SQL 模板的编译时验证
- **动态 SQL**: 条件块 (`{{if}}`) 实现灵活的查询构建
- **批量执行**: 使用 `DbBatch` (.NET 6+) 高效批量插入/更新/删除
- **动态投影**: Select 支持匿名类型，自动生成 DynamicResultReader
- **可扩展**: 自定义占位符处理器和数据库方言

## Quick Start

### 1. Define Your Entity

```csharp
using Sqlx.Annotations;

[SqlxEntity]
[SqlxParameter]
public partial class User  // 'partial' enables auto-registration
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
- [Performance Benchmarks](benchmarks.md) - Detailed performance comparison
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

Sqlx consistently outperforms other ORMs in .NET 10 (LTS):

| Operation | Sqlx | Dapper.AOT | FreeSql | Sqlx Advantage |
|-----------|------|------------|---------|----------------|
| SelectSingle | **9.06μs** | 10.14μs | 63.87μs | 12% faster than Dapper.AOT, 7.1x faster than FreeSql |
| Memory | **1.79KB** | 2.96KB | 11.48KB | Lowest allocation (65% less than Dapper.AOT) |

**Test Environment:** .NET 10.0.2 (LTS), BenchmarkDotNet 0.15.7, SQLite in-memory

**AOT Compatibility:** ✅ 817 unit tests passing, fully Native AOT ready

See [Performance Benchmarks](benchmarks.md) for detailed results.

## License

MIT License - see [LICENSE](../License.txt) for details.
