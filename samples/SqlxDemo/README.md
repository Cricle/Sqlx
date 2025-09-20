# Sqlx 3.0 Complete Feature Demo

This is a comprehensive feature demonstration program for the Sqlx ORM framework, using SQLite database to showcase all core features.

## 🚀 Feature Demonstrations

This demo program showcases all major features of Sqlx 3.0:

### 1. Direct Execution Pattern
- Simple parameterized SQL creation and execution
- Immediate SQL rendering
- One-time query execution

### 2. Static Template Pattern  
- Basic SQL template parsing and parameter binding
- Fluent parameter binding API
- Template reuse for performance
- Compile-time SQL safety validation

### 3. Dynamic Template Pattern
- Type-safe LINQ expression to SQL conversion
- Support for complex condition combinations
- Dynamic query building
- Expression method translation

### 4. INSERT Operations
- Explicit column specification (AOT-friendly)
- All-column insertion with reflection
- INSERT SELECT sub-queries
- Bulk insert operations

### 5. UPDATE Operations
- Conditional updates
- Multi-field updates
- Expression-based updates
- Safe parameter binding

### 6. DELETE Operations
- Conditional deletion
- Safe delete operations
- Type-safe WHERE clauses

### 7. Complex Queries
- Pagination queries (Skip/Take)
- Aggregation queries
- Sorting and filtering
- Advanced WHERE conditions

## 🔧 Technical Features

- **AOT Friendly**: Supports Ahead-of-Time compilation
- **Zero Reflection**: Compile-time code generation
- **Type Safe**: Strong-typed LINQ expressions  
- **SQLite Optimized**: Specifically optimized for SQLite dialect
- **High Performance**: Minimized runtime overhead
- **Modern C#**: Supports latest language features

## 🏃‍♂️ Running the Demo

```bash
cd samples/SqlxDemo
dotnet run
```

### Sample Output
The demo will show step-by-step execution of all three patterns:

1. **Direct Execution Examples**
   ```
   === Direct Execution Pattern ===
   Simple Query: SELECT COUNT(*) FROM user WHERE age > 25
   Result: 3 users found
   ```

2. **Static Template Examples**
   ```
   === Static Template Pattern ===
   Template: SELECT * FROM user WHERE name LIKE @pattern
   Execution 1: SELECT * FROM user WHERE name LIKE '%John%'
   Execution 2: SELECT * FROM user WHERE name LIKE '%Jane%'
   ```

3. **Dynamic Template Examples**
   ```
   === Dynamic Template Pattern ===
   Built Query: SELECT [name], [email] FROM [user] WHERE ([age] > 25 AND [isActive] = 1) ORDER BY [name] ASC
   ```

## 📊 Demo Data

The program uses an in-memory SQLite database with the following sample tables:

- **user**: User information table (name, email, age, salary, etc.)
- **product**: Product information table (name, price, status, etc.)

### Sample Data Structure
```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
    public decimal Salary { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

## 🎯 Core Advantages Demonstrated

1. **Clean Syntax**: Intuitive API design
2. **Compile-time Safety**: SQL errors caught at compile time
3. **High Performance**: Zero-allocation, zero-reflection execution paths
4. **Modern**: Supports latest C# features
5. **Maintainable**: Clear code structure and documentation

## 🔍 What You'll Learn

### Pattern Selection
- When to use Direct Execution vs Static Templates vs Dynamic Templates
- Performance implications of each pattern
- Best practices for each approach

### Type Safety
- How LINQ expressions translate to SQL
- Compile-time validation benefits
- Avoiding common SQL injection pitfalls

### Performance Optimization
- Template reuse strategies
- AOT compilation benefits
- Memory-efficient patterns

### Multi-Database Support
- How dialect definitions work
- Switching between database types
- Database-specific optimizations

## 📖 Code Examples

### Direct Execution
```csharp
var sql = ParameterizedSql.Create(
    "SELECT * FROM user WHERE age > @age AND isActive = @active",
    new { age = 25, active = true });

Console.WriteLine($"Generated SQL: {sql.Render()}");
```

### Static Templates
```csharp
var template = SqlTemplate.Parse("SELECT * FROM user WHERE name LIKE @pattern");

var result1 = template.Execute(new { pattern = "%John%" });
var result2 = template.Execute(new { pattern = "%Jane%" });
```

### Dynamic Templates
```csharp
var query = ExpressionToSql<User>.Create(SqlDefine.SQLite)
    .Where(u => u.Age > 25 && u.IsActive)
    .Select(u => new { u.Name, u.Email })
    .OrderBy(u => u.Name)
    .Take(10);

string sql = query.ToSql();
```

## 🚀 Running Instructions

1. **Prerequisites**
   - .NET 8.0 or later
   - No additional database setup required (uses in-memory SQLite)

2. **Build and Run**
   ```bash
   git clone <repository-url>
   cd Sqlx/samples/SqlxDemo
   dotnet restore
   dotnet build
   dotnet run
   ```

3. **Expected Output**
   The demo will run through all patterns sequentially, showing:
   - Generated SQL for each operation
   - Execution results
   - Performance characteristics
   - Pattern comparison

## 📚 Additional Resources

- [Sqlx Documentation](../../docs/)
- [Quick Start Guide](../../docs/QUICK_START_GUIDE.md)
- [API Reference](../../docs/API_REFERENCE.md)
- [Best Practices](../../docs/BEST_PRACTICES.md)
- [Advanced Features](../../docs/ADVANCED_FEATURES.md)

## 🎓 Learning Path

1. **Start Here**: Run this demo to see all features
2. **Deep Dive**: Read the Quick Start Guide
3. **Practice**: Try modifying the demo code
4. **Explore**: Check out the API Reference
5. **Optimize**: Review Best Practices guide

---

**🎯 This demo provides a complete introduction to Sqlx 3.0's capabilities - from simple queries to advanced features!**