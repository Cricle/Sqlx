# Sqlx

[![NuGet](https://img.shields.io/nuget/v/Sqlx)](https://www.nuget.org/packages/Sqlx/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.txt)
[![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-purple.svg)](#)

**Sqlx** is a compile-time source generator for building type-safe, high-performance database access layers in .NET. Write SQL templates once, run them on any database with zero runtime overhead.

## ✨ Key Features

- **🚀 Compile-Time Generation** - Zero runtime reflection, near-ADO.NET performance
- **🔒 Type-Safe SQL** - Catch SQL errors at compile time, not runtime
- **🌐 Multi-Database** - SQLite, PostgreSQL, MySQL, SQL Server with a single codebase
- **📝 Smart Templates** - 40+ placeholders that adapt to different database dialects
- **⚡ High Performance** - Direct ADO.NET calls, minimal allocations
- **🎯 AOT Compatible** - Full Native AOT support for modern .NET applications

## 🚀 Quick Start

### Installation

```bash
dotnet add package Sqlx
```

### Basic Example

```csharp
// 1. Define your entity
public class User
{
    public long Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}

// 2. Define repository interface with SQL templates
public interface IUserRepository
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(long id);

    [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
    [ReturnInsertedId]
    Task<long> InsertAsync(User user);
}

// 3. Implement repository (code generated automatically)
[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("users")]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository { }

// 4. Use it
await using var conn = new SqliteConnection("Data Source=app.db");
await conn.OpenAsync();

var repo = new UserRepository(conn);
var userId = await repo.InsertAsync(new User { Name = "Alice", Age = 25 });
var user = await repo.GetByIdAsync(userId);
```

## 📚 Core Concepts

### SQL Templates with Placeholders

Sqlx uses **placeholders** that automatically adapt to different database dialects:

```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = {{bool_true}}")]
Task<List<User>> GetActiveUsersAsync();
```

**Generated SQL by Database:**

| Database   | Generated SQL |
|------------|---------------|
| SQLite     | `SELECT id, name, age FROM users WHERE is_active = 1` |
| PostgreSQL | `SELECT "id", "name", "age" FROM "users" WHERE is_active = true` |
| MySQL      | ``SELECT `id`, `name`, `age` FROM `users` WHERE is_active = 1`` |
| SQL Server | `SELECT [id], [name], [age] FROM [users] WHERE is_active = 1` |

### Common Placeholders

| Placeholder | Description | Example Output |
|-------------|-------------|----------------|
| `{{table}}` | Table name with dialect-specific quoting | `"users"` (PostgreSQL) |
| `{{columns}}` | All column names | `id, name, age` |
| `{{columns --exclude Id}}` | Columns excluding specified ones | `name, age` |
| `{{values --exclude Id}}` | Parameter placeholders | `@name, @age` |
| `{{set --exclude Id}}` | SET clause for UPDATE | `name = @name, age = @age` |
| `{{where}}` | WHERE clause from expression | `WHERE age > @p0` |
| `{{orderby column}}` | ORDER BY clause | `ORDER BY column` |
| `{{limit --param count}}` | LIMIT clause | `LIMIT @count` |
| `{{bool_true}}` | Boolean true literal | `1` (SQLite), `true` (PostgreSQL) |
| `{{current_timestamp}}` | Current timestamp function | `CURRENT_TIMESTAMP`, `GETDATE()` |

[See full placeholder reference →](docs/PLACEHOLDER_REFERENCE.md)

## 🌐 Multi-Database Support

Write your repository interface once, implement it for multiple databases:

```csharp
// Define interface with dialect-agnostic templates
public interface ICrudRepository<TEntity, TKey>
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<TEntity?> GetByIdAsync(TKey id);
    
    [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
    [ReturnInsertedId]
    Task<long> InsertAsync(TEntity entity);
}

// SQLite implementation
[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("users")]
[RepositoryFor(typeof(ICrudRepository<User, long>))]
public partial class SqliteUserRepo(DbConnection conn) : ICrudRepository<User, long> { }

// PostgreSQL implementation
[SqlDefine(SqlDefineTypes.PostgreSql)]
[TableName("users")]
[RepositoryFor(typeof(ICrudRepository<User, long>))]
public partial class PostgresUserRepo(DbConnection conn) : ICrudRepository<User, long> { }
```

## 🎯 Advanced Features

### Expression-Based Queries

Build type-safe dynamic queries using LINQ expressions:

```csharp
public interface IUserRepository
{
    [SqlTemplate("SELECT {{columns}} FROM {{table}} {{where}}")]
    Task<List<User>> QueryAsync([ExpressionToSql] Expression<Func<User, bool>> predicate);
}

// Usage
var adults = await repo.QueryAsync(u => u.Age >= 18 && u.IsActive);
// Generated: SELECT id, name, age FROM users WHERE age >= @p0 AND is_active = @p1
```

### Batch Operations

Optimize bulk operations automatically:

```csharp
[SqlTemplate("INSERT INTO {{table}} (name, age) VALUES {{batch_values}}")]
[BatchOperation(MaxBatchSize = 500)]
Task<int> BatchInsertAsync(IEnumerable<User> users);
```

### SQL Debugging

Validate generated SQL without executing queries:

```csharp
[SqlxDebugger]
[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("users")]
[RepositoryFor(typeof(ICrudRepository<User, long>))]
public partial class UserRepository : ICrudRepository<User, long> { }

// Get generated SQL for debugging
var sql = repo.GetGetByIdAsyncSql(123);
// Returns: "SELECT id, name, age FROM users WHERE id = @id"
```

## 📖 Documentation

- **[Quick Start Guide](docs/QUICK_START.md)** - Get started in 5 minutes
- **[Placeholder Reference](docs/PLACEHOLDER_REFERENCE.md)** - Complete placeholder guide
- **[API Reference](docs/API_REFERENCE.md)** - Full API documentation
- **[Best Practices](docs/BEST_PRACTICES.md)** - Recommended patterns and tips
- **[Multi-Database Guide](docs/MULTI_DATABASE.md)** - Cross-database development
- **[Examples](samples/TodoWebApi/)** - Complete working examples

## 🗄️ Supported Databases

| Database   | Status | Dialect Enum |
|------------|--------|--------------|
| SQLite     | ✅ Production Ready | `SqlDefineTypes.SQLite` |
| PostgreSQL | ✅ Production Ready | `SqlDefineTypes.PostgreSql` |
| MySQL      | ✅ Production Ready | `SqlDefineTypes.MySql` |
| SQL Server | ✅ Production Ready | `SqlDefineTypes.SqlServer` |

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.

## 🙏 Acknowledgments

Built with ❤️ for the .NET community.
