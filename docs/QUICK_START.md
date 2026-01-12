# Quick Start Guide

Get started with Sqlx in 5 minutes.

## Installation

Add Sqlx to your project:

```bash
dotnet add package Sqlx
```

## Step 1: Define Your Entity

Create a simple entity class:

```csharp
public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

## Step 2: Define Repository Interface

Create an interface with SQL templates:

```csharp
public interface IUserRepository
{
    // Get user by ID
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(long id);

    // Get all users
    [SqlTemplate("SELECT {{columns}} FROM {{table}}")]
    Task<List<User>> GetAllAsync();

    // Insert user and return ID
    [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
    [ReturnInsertedId]
    Task<long> InsertAsync(User user);

    // Update user
    [SqlTemplate("UPDATE {{table}} SET {{set --exclude Id CreatedAt}} WHERE id = @id")]
    Task<int> UpdateAsync(User user);

    // Delete user
    [SqlTemplate("DELETE FROM {{table}} WHERE id = @id")]
    Task<int> DeleteAsync(long id);

    // Count users
    [SqlTemplate("SELECT COUNT(*) FROM {{table}}")]
    Task<long> CountAsync();
}
```

## Step 3: Implement Repository

Create a partial class that implements the interface. Sqlx will generate the implementation:

```csharp
[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("users")]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository(DbConnection connection) : IUserRepository
{
}
```

**Attributes explained:**
- `[SqlDefine]` - Specifies the database dialect (SQLite, PostgreSQL, MySQL, SQL Server)
- `[TableName]` - Specifies the table name
- `[RepositoryFor]` - Tells the generator which interface to implement
- `partial` - Required for source generation

## Step 4: Use the Repository

```csharp
using Microsoft.Data.Sqlite;

// Create connection
await using var connection = new SqliteConnection("Data Source=app.db");
await connection.OpenAsync();

// Create repository
var repo = new UserRepository(connection);

// Insert a user
var userId = await repo.InsertAsync(new User
{
    Name = "Alice",
    Email = "alice@example.com",
    Age = 25,
    IsActive = true,
    CreatedAt = DateTime.UtcNow
});

Console.WriteLine($"Inserted user with ID: {userId}");

// Get user by ID
var user = await repo.GetByIdAsync(userId);
Console.WriteLine($"Found user: {user?.Name}");

// Update user
if (user != null)
{
    user.Age = 26;
    await repo.UpdateAsync(user);
}

// Get all users
var allUsers = await repo.GetAllAsync();
Console.WriteLine($"Total users: {allUsers.Count}");

// Count users
var count = await repo.CountAsync();
Console.WriteLine($"User count: {count}");

// Delete user
await repo.DeleteAsync(userId);
```

## Understanding Placeholders

Sqlx uses **placeholders** that automatically generate SQL based on your entity:

| Placeholder | What it does | Example Output |
|-------------|--------------|----------------|
| `{{table}}` | Inserts table name | `users` |
| `{{columns}}` | Lists all columns | `id, name, email, age, is_active, created_at` |
| `{{columns --exclude Id}}` | Lists columns except Id | `name, email, age, is_active, created_at` |
| `{{values --exclude Id}}` | Parameter placeholders | `@name, @email, @age, @is_active, @created_at` |
| `{{set --exclude Id}}` | SET clause for UPDATE | `name = @name, email = @email, ...` |

## Next Steps

- **[Placeholder Reference](PLACEHOLDER_REFERENCE.md)** - Learn all available placeholders
- **[Multi-Database Guide](MULTI_DATABASE.md)** - Support multiple databases
- **[Best Practices](BEST_PRACTICES.md)** - Write better code
- **[API Reference](API_REFERENCE.md)** - Complete API documentation

## Common Patterns

### Search with Conditions

```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge AND is_active = @isActive")]
Task<List<User>> SearchAsync(int minAge, bool isActive);
```

### Pagination

```csharp
[SqlTemplate(@"
    SELECT {{columns}} 
    FROM {{table}} 
    ORDER BY created_at DESC 
    LIMIT @pageSize 
    OFFSET @skip
")]
Task<List<User>> GetPagedAsync(int pageSize, int skip);
```

### Check Existence

```csharp
[SqlTemplate("SELECT EXISTS(SELECT 1 FROM {{table}} WHERE email = @email)")]
Task<bool> EmailExistsAsync(string email);
```

### Aggregate Functions

```csharp
[SqlTemplate("SELECT AVG(age) FROM {{table}} WHERE is_active = @isActive")]
Task<double> GetAverageAgeAsync(bool isActive);
```

## Troubleshooting

### Build Errors

If you see build errors:

1. Make sure the class is marked as `partial`
2. Check that all attributes are correctly applied
3. Verify SQL template syntax
4. Rebuild the project (`dotnet build`)

### Generated Code Location

Generated code is located at:
```
obj/Debug/net9.0/generated/Sqlx.Generator/Sqlx.Generator.CSharpGenerator/
```

You can inspect the generated code to see the actual SQL being executed.

### Common Mistakes

❌ **Forgetting `partial` keyword**
```csharp
public class UserRepository : IUserRepository { } // Wrong!
```

✅ **Correct**
```csharp
public partial class UserRepository : IUserRepository { } // Correct
```

❌ **Including Id in INSERT**
```csharp
[SqlTemplate("INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})")]
```

✅ **Correct - Exclude auto-increment Id**
```csharp
[SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
```

## Need Help?

- Check the [API Reference](API_REFERENCE.md) for detailed documentation
- See [Best Practices](BEST_PRACTICES.md) for recommended patterns
- Review the [TodoWebApi sample](../samples/TodoWebApi/) for a complete example
