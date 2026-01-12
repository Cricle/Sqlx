# Sqlx Documentation

Welcome to the Sqlx documentation!

## Getting Started

- **[Quick Start Guide](QUICK_START.md)** - Get up and running in 5 minutes
- **[Placeholder Reference](PLACEHOLDER_REFERENCE.md)** - Complete guide to all placeholders
- **[API Reference](API_REFERENCE.md)** - Full API documentation

## Guides

- **[Best Practices](BEST_PRACTICES.md)** - Recommended patterns and tips
- **[Multi-Database Guide](MULTI_DATABASE.md)** - Write code that works across databases

## What is Sqlx?

Sqlx is a compile-time source generator for building type-safe, high-performance database access layers in .NET. It generates ADO.NET code at compile time, providing:

- **Zero runtime overhead** - No reflection, no dynamic code
- **Type safety** - Catch SQL errors at compile time
- **Multi-database support** - Write once, run on SQLite, PostgreSQL, MySQL, SQL Server
- **Smart templates** - 40+ placeholders that adapt to different databases

## Quick Example

```csharp
// 1. Define entity
public class User
{
    public long Id { get; set; }
    public string Name { get; set; }
    public int Age { get; set; }
}

// 2. Define repository interface
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
var repo = new UserRepository(connection);
var userId = await repo.InsertAsync(new User { Name = "Alice", Age = 25 });
var user = await repo.GetByIdAsync(userId);
```

## Key Features

### Compile-Time Generation

Sqlx generates code at compile time, not runtime. This means:
- No reflection overhead
- No runtime code generation
- Full AOT compatibility
- Near-ADO.NET performance

### Type-Safe SQL

SQL templates are validated at compile time:
- Parameter mismatches caught early
- Return type validation
- Column name validation

### Multi-Database Support

Write SQL templates once, run on any database:
- SQLite
- PostgreSQL
- MySQL
- SQL Server

### Smart Placeholders

40+ placeholders that automatically adapt to different databases:
- `{{table}}` - Table name with proper quoting
- `{{columns}}` - All column names
- `{{bool_true}}` - Boolean true literal (1, true, etc.)
- `{{current_timestamp}}` - Current timestamp function
- And many more...

## Documentation Structure

### For Beginners

1. Start with [Quick Start Guide](QUICK_START.md)
2. Learn about [Placeholders](PLACEHOLDER_REFERENCE.md)
3. Review [Best Practices](BEST_PRACTICES.md)

### For Advanced Users

1. Explore [API Reference](API_REFERENCE.md)
2. Learn [Multi-Database](MULTI_DATABASE.md) patterns
3. Check out the [TodoWebApi sample](../samples/TodoWebApi/)

## Need Help?

- **Issues:** [GitHub Issues](https://github.com/your-repo/Sqlx/issues)
- **Discussions:** [GitHub Discussions](https://github.com/your-repo/Sqlx/discussions)
- **Examples:** [samples/TodoWebApi](../samples/TodoWebApi/)

## Contributing

Contributions are welcome! Please see our contributing guidelines.

## License

Sqlx is licensed under the MIT License.
