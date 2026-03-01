# Sqlx Documentation

Sqlx 是一个高性能、AOT 友好的 SQL 生成库。使用源生成器在编译时生成代码，零运行时反射，完全支持 Native AOT。

## 核心特性

- **🚀 高性能** - 比 Dapper.AOT 快 19%，比 FreeSql 快 6.8 倍
- **⚡ 零反射** - 所有代码在编译时生成
- **🎯 类型安全** - SQL 模板的编译时验证
- **🌐 多数据库** - SQLite, MySQL, PostgreSQL, SQL Server, Oracle, DB2
- **📦 AOT 就绪** - 通过 3124 个单元测试，完全支持 Native AOT
- **🔧 LINQ 支持** - IQueryable 接口，支持 Where/Select/OrderBy/Join
- **💾 智能缓存** - SqlQuery\<T\> 泛型缓存，自动注册 EntityProvider
- **🔍 自动发现** - 源生成器自动发现 SqlQuery\<T\> 和 SqlTemplate 中的实体类型
- **🎨 动态投影** - Select 支持匿名类型，自动生成 DynamicResultReader
- **🔌 可扩展** - 自定义占位符处理器和数据库方言

## Quick Start

### 1. Define Your Entity

```csharp
using Sqlx.Annotations;

[Sqlx]
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
- [SqlxContext](sqlx-context.md) - Multi-repository management and transaction handling
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

## 性能对比

Sqlx 在 .NET 10 (LTS) 上的性能表现：

| 操作 | Sqlx | Dapper.AOT | FreeSql | Sqlx 优势 |
|------|------|------------|---------|-----------|
| 单条查询 | **8.70 μs** | 10.35 μs | 59.30 μs | 快 19% / 6.8x |
| 内存分配 | **1.41 KB** | 2.66 KB | 10.24 KB | 少 47% / 626% |

**测试环境:** .NET 10.0.2 (LTS), BenchmarkDotNet 0.15.7, SQLite 内存数据库

**AOT 兼容性:** ✅ 通过 3124 个单元测试，完全支持 Native AOT

**最新优化:**
- 泛型 SqlQuery\<T\> 缓存优化
- DynamicResultReader 静态方法缓存
- 移除 SQL 生成中的反射
- 支持 JOIN 查询，无性能损失
- 永远不生成 SELECT *，显式列出所有列
- 源生成器自动发现 SqlQuery\<T\> 和 SqlTemplate 中的实体类型
- `{{where --object}}` 支持字典生成 WHERE 条件

查看 [性能基准测试](benchmarks.md) 了解详细数据。

## License

MIT License - see [LICENSE](../License.txt) for details.
