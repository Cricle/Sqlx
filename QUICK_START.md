# Sqlx 3.0 Quick Start Guide

Welcome to Sqlx 3.0! This guide will get you up and running with the modern minimal .NET ORM framework in just a few minutes.

## 🚀 Installation

### Package Installation
```bash
dotnet add package Sqlx
```

Or via PackageReference:
```xml
<PackageReference Include="Sqlx" Version="3.0.0" />
```

## ✨ Basic Usage - Three Patterns

Sqlx 3.0 provides three distinct patterns for different use cases. Choose the one that best fits your needs.

### Pattern 1: Direct Execution 🎯
Perfect for simple, one-time queries.

```csharp
using Sqlx;

// Create and execute parameterized SQL directly
var sql = ParameterizedSql.Create(
    "SELECT * FROM Users WHERE Age > @age AND IsActive = @active",
    new { age = 18, active = true });

string renderedSql = sql.Render();
Console.WriteLine(renderedSql);
// Output: SELECT * FROM Users WHERE Age > 18 AND IsActive = 1
```

### Pattern 2: Static Templates ♻️
Ideal for reusable SQL with different parameters.

```csharp
using Sqlx;

// Define a reusable template
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

### Pattern 3: Dynamic Templates 🔧
Best for complex, type-safe query building.

```csharp
using Sqlx;

// Define your entity
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Build type-safe queries
var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Where(u => u.Age > 25 && u.IsActive)
    .Select(u => new { u.Name, u.Email, u.Age })
    .OrderBy(u => u.Name)
    .Take(10);

string sql = query.ToSql();
Console.WriteLine(sql);
// Output: SELECT [Name], [Email], [Age] FROM [User] 
//         WHERE ([Age] > 25 AND [IsActive] = 1) 
//         ORDER BY [Name] ASC 
//         OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY
```

## 🔧 Database Configuration

Sqlx supports multiple databases with predefined dialects:

```csharp
// Choose your database dialect
var sqlServer = SqlDefine.SqlServer;   // [column] with @parameters
var mysql = SqlDefine.MySql;           // `column` with @parameters
var postgresql = SqlDefine.PostgreSql; // "column" with $parameters
var sqlite = SqlDefine.SQLite;         // [column] with $parameters
var oracle = SqlDefine.Oracle;         // "column" with :parameters

// Use with ExpressionToSql
var query = ExpressionToSql<User>.Create(mysql)
    .Where(u => u.IsActive)
    .Select(u => u.Name);
```

## 📊 Complete CRUD Examples

### SELECT Operations
```csharp
// Simple select
var selectQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Select(u => new { u.Id, u.Name, u.Email })
    .Where(u => u.Age > 18)
    .OrderBy(u => u.Name);

// With joins (using navigation properties)
var joinQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Select(u => new { u.Name, u.Department.Name })
    .Where(u => u.Department.IsActive);

// Pagination
var pagedQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Where(u => u.IsActive)
    .OrderBy(u => u.CreatedAt)
    .Skip(20)
    .Take(10);
```

### INSERT Operations
```csharp
// Explicit column insert (recommended for AOT)
var insertQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .InsertInto(u => new { u.Name, u.Email, u.Age })
    .Values("John Doe", "john@example.com", 30);

// Bulk insert with SELECT
var bulkInsert = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .InsertInto(u => new { u.Name, u.Email })
    .InsertSelect("SELECT Name, Email FROM TempUsers WHERE IsValid = 1");
```

### UPDATE Operations
```csharp
// Simple update
var updateQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Update()
    .Set(u => u.Name, "Updated Name")
    .Set(u => u.Age, u => u.Age + 1)
    .Where(u => u.Id == 1);

// Conditional update
var conditionalUpdate = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Update()
    .Set(u => u.IsActive, false)
    .Where(u => u.CreatedAt < DateTime.Now.AddYears(-1));
```

### DELETE Operations
```csharp
// Simple delete
var deleteQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Delete()
    .Where(u => u.IsActive == false);

// One-step delete
var quickDelete = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Delete(u => u.Age < 18);
```

## 🎯 Template Conversion and Reuse

Convert dynamic queries to reusable templates:

```csharp
// Build a query
var baseQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Where(u => u.IsActive)
    .Select(u => new { u.Name, u.Email });

// Convert to template for reuse
var template = baseQuery.ToTemplate();

// Use template multiple times
var execution1 = template.Execute();
var execution2 = template.Execute(new { additionalParam = "value" });
```

## 🔥 Performance Tips

### 1. Template Reuse
```csharp
// ✅ Good: Reuse templates
var userTemplate = SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id");
var user1 = userTemplate.Execute(new { id = 1 });
var user2 = userTemplate.Execute(new { id = 2 });

// ❌ Avoid: Creating new templates each time
var user1 = SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id").Execute(new { id = 1 });
var user2 = SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id").Execute(new { id = 2 });
```

### 2. AOT-Friendly Patterns
```csharp
// ✅ AOT-friendly: Explicit column specification
.InsertInto(u => new { u.Name, u.Email, u.Age })

// ⚠️ Reflection-based: Use only when necessary
.InsertIntoAll()  // Uses reflection to get all properties
```

### 3. Parameterized Queries
```csharp
// ✅ Use parameterized mode for better performance
var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .UseParameterizedQueries()
    .Where(u => u.Status == "Active");
```

## 🏃‍♂️ Real-World Example

Here's a complete example showing all three patterns in action:

```csharp
using System;
using System.Collections.Generic;
using Sqlx;

public class UserService
{
    // Pattern 1: Direct execution for simple queries
    public string GetActiveUsersCount()
    {
        var sql = ParameterizedSql.Create(
            "SELECT COUNT(*) FROM Users WHERE IsActive = @active",
            new { active = true });
        return sql.Render();
    }

    // Pattern 2: Template for reusable queries
    private static readonly SqlTemplate UserByIdTemplate = 
        SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id");

    public string GetUserById(int id)
    {
        return UserByIdTemplate.Execute(new { id }).Render();
    }

    // Pattern 3: Dynamic building for complex queries
    public string SearchUsers(string? name, int? minAge, bool includeInactive = false)
    {
        var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
            .Select(u => new { u.Id, u.Name, u.Email, u.Age });

        if (!includeInactive)
            query = query.Where(u => u.IsActive);

        if (!string.IsNullOrEmpty(name))
            query = query.Where(u => u.Name.Contains(name));

        if (minAge.HasValue)
            query = query.Where(u => u.Age >= minAge.Value);

        return query.OrderBy(u => u.Name).ToSql();
    }
}

// Usage example
var service = new UserService();

Console.WriteLine("=== Pattern 1: Direct Execution ===");
Console.WriteLine(service.GetActiveUsersCount());

Console.WriteLine("\n=== Pattern 2: Static Template ===");
Console.WriteLine(service.GetUserById(123));

Console.WriteLine("\n=== Pattern 3: Dynamic Template ===");
Console.WriteLine(service.SearchUsers("John", 25, false));
```

## 📁 Sample Projects

Explore comprehensive examples in the repository:

- **[SqlxDemo](samples/SqlxDemo/)** - Complete showcase of all three patterns
- **[IntegrationShowcase](samples/IntegrationShowcase/)** - Real-world integration examples

To run the samples:
```bash
cd samples/SqlxDemo
dotnet run
```

## 📚 Next Steps

Now that you've got the basics down, explore more advanced features:

- [API Reference](docs/API_REFERENCE.md) - Complete API documentation
- [Best Practices](docs/BEST_PRACTICES.md) - Recommended usage patterns  
- [Advanced Features](docs/ADVANCED_FEATURES.md) - Deep dive into advanced functionality
- [Migration Guide](docs/MIGRATION_GUIDE.md) - Upgrading from previous versions

## 🤝 Need Help?

- Check the [documentation](docs/) for detailed guides
- Look at [sample projects](samples/) for real-world examples
- Review [best practices](docs/BEST_PRACTICES.md) for optimal usage

---

**🚀 You're now ready to build efficient, type-safe data access with Sqlx 3.0!**

Choose your pattern and start building amazing applications with minimal, modern .NET data access.