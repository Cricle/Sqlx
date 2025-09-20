# Sqlx 3.0 Quick Start Guide

This guide will help you master the core usage of Sqlx in just 5 minutes.

## 📦 Installation

```bash
dotnet add package Sqlx
```

## 🎯 Three Core Patterns

Sqlx 3.0 focuses on three simple yet powerful usage patterns:

### 1️⃣ Direct Execution Pattern - Simplest

**Use case**: One-time queries, simple SQL execution

```csharp
using Sqlx;

// Create and execute parameterized SQL
var sql = ParameterizedSql.Create(
    "SELECT * FROM Users WHERE Age > @age AND IsActive = @active",
    new { age = 18, active = true });

// Render final SQL
string finalSql = sql.Render();
Console.WriteLine(finalSql);
// Output: SELECT * FROM Users WHERE Age > 18 AND IsActive = 1
```

### 2️⃣ Static Template Pattern - Reusable

**Use case**: SQL that needs to be reused with different parameters

```csharp
using Sqlx;

// Define reusable template
var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Age > @age AND IsActive = @active");

// Execute with different parameters
var youngUsers = template.Execute(new { age = 18, active = true });
var adultUsers = template.Execute(new { age = 25, active = true });

// Fluent parameter binding
var customQuery = template.Bind()
    .Param("age", 30)
    .Param("active", true)
    .Build();

Console.WriteLine(customQuery.Render());
```

### 3️⃣ Dynamic Template Pattern - Type-Safe

**Use case**: Complex conditional query building with type safety

```csharp
using Sqlx;

// Define entity
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsActive { get; set; }
}

// Build type-safe dynamic query
var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Where(u => u.Age > 25 && u.IsActive)
    .Select(u => new { u.Name, u.Id })
    .OrderBy(u => u.Name)
    .Take(10);

string sql = query.ToSql();
Console.WriteLine(sql);
// Output: SELECT [Name], [Id] FROM [User] 
//         WHERE ([Age] > 25 AND [IsActive] = 1) 
//         ORDER BY [Name] ASC 
//         OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY
```

## 🔧 Database Support

```csharp
// Choose your database dialect
var sqlServer = SqlDefine.SqlServer;   // [column] with @parameters
var mysql = SqlDefine.MySql;           // `column` with @parameters
var postgresql = SqlDefine.PostgreSql; // "column" with $parameters
var sqlite = SqlDefine.SQLite;         // [column] with $parameters
var oracle = SqlDefine.Oracle;         // "column" with :parameters

// Use with queries
var query = ExpressionToSql<User>.Create(mysql)
    .Where(u => u.IsActive)
    .Select(u => u.Name);
```

## 📚 Complete CRUD Examples

### SELECT Queries
```csharp
// Basic select
var users = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Select(u => new { u.Id, u.Name })
    .Where(u => u.Age > 18)
    .OrderBy(u => u.Name)
    .ToSql();

// Pagination
var pagedUsers = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Where(u => u.IsActive)
    .OrderBy(u => u.Name)
    .Skip(20)
    .Take(10)
    .ToSql();
```

### INSERT Operations
```csharp
// Explicit columns (AOT-friendly)
var insert = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .InsertInto(u => new { u.Name, u.Age })
    .Values("John", 25)
    .ToSql();

// INSERT with SELECT
var insertSelect = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .InsertInto(u => new { u.Name, u.Age })
    .InsertSelect("SELECT Name, Age FROM TempUsers")
    .ToSql();
```

### UPDATE Operations
```csharp
var update = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Update()
    .Set(u => u.Name, "New Name")
    .Set(u => u.Age, u => u.Age + 1)
    .Where(u => u.Id == 1)
    .ToSql();
```

### DELETE Operations
```csharp
var delete = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Delete()
    .Where(u => u.IsActive == false)
    .ToSql();

// One-step delete
var quickDelete = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Delete(u => u.Age < 18)
    .ToSql();
```

## 🎯 Template Reuse

Convert dynamic queries to reusable templates:

```csharp
// Build query
var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Where(u => u.IsActive)
    .Select(u => new { u.Name, u.Id });

// Convert to template
var template = query.ToTemplate();

// Reuse template
var result1 = template.Execute();
var result2 = template.Execute(new { extraParam = "value" });
```

## 🚀 Performance Tips

### Template Reuse
```csharp
// ✅ Good: Reuse templates
var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id");
var user1 = template.Execute(new { id = 1 });
var user2 = template.Execute(new { id = 2 });

// ❌ Avoid: Creating new templates each time
var user1 = SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id").Execute(new { id = 1 });
```

### AOT Optimization
```csharp
// ✅ AOT-friendly: Explicit columns
.InsertInto(u => new { u.Name, u.Age })

// ⚠️ Reflection-based: Use when necessary
.InsertIntoAll()
```

## 🏃‍♂️ Real-World Example

```csharp
using Sqlx;

public class UserService
{
    // Pattern 1: Direct execution
    public string GetActiveUsersCount()
    {
        return ParameterizedSql.Create(
            "SELECT COUNT(*) FROM Users WHERE IsActive = @active",
            new { active = true }).Render();
    }

    // Pattern 2: Static template
    private static readonly SqlTemplate UserByIdTemplate = 
        SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id");

    public string GetUser(int id)
    {
        return UserByIdTemplate.Execute(new { id }).Render();
    }

    // Pattern 3: Dynamic building
    public string SearchUsers(string? name, int? minAge, bool activeOnly = true)
    {
        var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
            .Select(u => new { u.Id, u.Name, u.Age });

        if (activeOnly)
            query = query.Where(u => u.IsActive);

        if (!string.IsNullOrEmpty(name))
            query = query.Where(u => u.Name.Contains(name));

        if (minAge.HasValue)
            query = query.Where(u => u.Age >= minAge.Value);

        return query.OrderBy(u => u.Name).ToSql();
    }
}
```

## 🎓 Next Steps

- [API Reference](API_REFERENCE.md) - Complete API documentation
- [Best Practices](BEST_PRACTICES.md) - Recommended patterns
- [Advanced Features](ADVANCED_FEATURES.md) - Deep dive into advanced functionality
- [Sample Projects](../samples/) - Real-world examples

## 🔗 Key Benefits

- **Zero Reflection**: Full AOT compatibility
- **Type Safety**: Compile-time validation
- **Simple API**: Minimal learning curve
- **High Performance**: Optimized for speed
- **Multi-Database**: Support for all major databases

---

**🚀 You're ready to start building with Sqlx 3.0!**

Choose the pattern that fits your needs and enjoy modern, efficient data access.