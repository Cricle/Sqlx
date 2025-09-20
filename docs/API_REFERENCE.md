# Sqlx 3.0 API Reference

This document provides detailed information about all public APIs in Sqlx 3.0.

## 🏗️ Core Architecture

```
Sqlx 3.0
├── ParameterizedSql        # Parameterized SQL execution instance
├── SqlTemplate            # Reusable SQL templates  
├── ExpressionToSql<T>      # Type-safe query builder
├── SqlDefine              # Database dialect definitions
└── Extensions             # Extension methods and utilities
```

## 📋 ParameterizedSql

Execution instance for parameterized SQL, representing SQL statements with parameters.

### Constructor
```csharp
public readonly record struct ParameterizedSql(string Sql, IReadOnlyDictionary<string, object?> Parameters)
```

### Static Methods
```csharp
// Create using anonymous object
public static ParameterizedSql Create(string sql, object? parameters)

// Create using dictionary  
public static ParameterizedSql CreateWithDictionary(string sql, Dictionary<string, object?> parameters)
```

### Instance Methods
```csharp
// Render final SQL (inline parameter values)
public string Render()
```

### Static Properties
```csharp
// Empty instance
public static ParameterizedSql Empty { get; }
```

### Usage Examples
```csharp
// Create with anonymous object
var sql1 = ParameterizedSql.Create(
    "SELECT * FROM Users WHERE Id = @id AND IsActive = @active",
    new { id = 123, active = true });

// Create with dictionary
var parameters = new Dictionary<string, object?> 
{
    ["id"] = 123,
    ["active"] = true
};
var sql2 = ParameterizedSql.CreateWithDictionary(
    "SELECT * FROM Users WHERE Id = @id AND IsActive = @active", 
    parameters);

// Render SQL
string finalSql = sql1.Render();
// Output: SELECT * FROM Users WHERE Id = 123 AND IsActive = 1
```

---

## 🎨 SqlTemplate

Reusable SQL template for executing the same SQL with different parameters.

### Constructor
```csharp
public readonly record struct SqlTemplate(string Sql)
```

### Static Methods
```csharp
// Parse SQL template
public static SqlTemplate Parse(string sql)
```

### Instance Methods
```csharp
// Execute with anonymous object
public ParameterizedSql Execute(object? parameters = null)

// Execute with dictionary
public ParameterizedSql Execute(Dictionary<string, object?> parameters)

// Start fluent parameter binding
public SqlTemplateBuilder Bind()
```

### Properties
```csharp
// Check if template has no parameters
public bool IsPureTemplate { get; }
```

### Usage Examples
```csharp
// Create template
var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Age > @age AND Department = @dept");

// Execute with different parameters
var youngEngineers = template.Execute(new { age = 20, dept = "Engineering" });
var seniorSales = template.Execute(new { age = 35, dept = "Sales" });

// Fluent binding
var customQuery = template.Bind()
    .Param("age", 25)
    .Param("dept", "Marketing")
    .Build();

// Check if pure template
bool isPure = SqlTemplate.Parse("SELECT * FROM Users").IsPureTemplate; // true
bool hasParams = SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id").IsPureTemplate; // false
```

---

## 🔧 SqlTemplateBuilder

Fluent interface for building parameterized SQL from templates.

### Methods
```csharp
// Add parameter
public SqlTemplateBuilder Param(string name, object? value)

// Build final parameterized SQL
public ParameterizedSql Build()
```

### Usage Example
```csharp
var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Age > @age AND Department = @dept");

var query = template.Bind()
    .Param("age", 25)
    .Param("dept", "IT")
    .Build();

string sql = query.Render();
```

---

## 🎯 ExpressionToSql<T>

Type-safe query builder for generating SQL from LINQ expressions.

### Static Factory
```csharp
public static ExpressionToSql<T> Create(SqlDefine sqlDefine)
```

### SELECT Methods
```csharp
// Select all columns
public ExpressionToSql<T> Select()

// Select specific columns by name
public ExpressionToSql<T> Select(params string[] columns)

// Select using expression
public ExpressionToSql<T> Select<TResult>(Expression<Func<T, TResult>> selector)
```

### WHERE Methods
```csharp
// Add WHERE condition
public ExpressionToSql<T> Where(Expression<Func<T, bool>> predicate)

// Add AND condition (alias for Where)
public ExpressionToSql<T> And(Expression<Func<T, bool>> predicate)
```

### ORDER BY Methods
```csharp
// Order by ascending
public ExpressionToSql<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)

// Order by descending
public ExpressionToSql<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
```

### PAGINATION Methods
```csharp
// Limit results
public ExpressionToSql<T> Take(int count)

// Skip results
public ExpressionToSql<T> Skip(int count)
```

### INSERT Methods
```csharp
// Start INSERT
public ExpressionToSql<T> Insert()

// INSERT into specific columns
public ExpressionToSql<T> InsertInto(Expression<Func<T, object>> selector)

// INSERT into all columns (uses reflection)
public ExpressionToSql<T> InsertIntoAll()

// Specify values
public ExpressionToSql<T> Values(params object[] values)

// INSERT SELECT
public ExpressionToSql<T> InsertSelect(string selectSql)
```

### UPDATE Methods
```csharp
// Start UPDATE
public ExpressionToSql<T> Update()

// Set column to value
public ExpressionToSql<T> Set<TValue>(Expression<Func<T, TValue>> selector, TValue value)

// Set column using expression
public ExpressionToSql<T> Set<TValue>(Expression<Func<T, TValue>> selector, Expression<Func<T, TValue>> valueExpression)
```

### DELETE Methods
```csharp
// Start DELETE
public ExpressionToSql<T> Delete()

// DELETE with condition
public ExpressionToSql<T> Delete(Expression<Func<T, bool>> predicate)
```

### GROUP BY Methods
```csharp
// Group by key
public GroupedExpressionToSql<T, TKey> GroupBy<TKey>(Expression<Func<T, TKey>> keySelector)

// Add HAVING condition
public ExpressionToSql<T> Having(Expression<Func<T, bool>> predicate)
```

### OUTPUT Methods
```csharp
// Generate SQL string
public string ToSql()

// Convert to reusable template
public SqlTemplate ToTemplate()
```

### Usage Examples
```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public string Department { get; set; } = string.Empty;
}

// SELECT example
var selectQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Select(u => new { u.Id, u.Name, u.Age })
    .Where(u => u.Age > 18 && u.IsActive)
    .OrderBy(u => u.Name)
    .Take(10);

string selectSql = selectQuery.ToSql();
// SELECT [Id], [Name], [Age] FROM [User] 
// WHERE ([Age] > 18 AND [IsActive] = 1) 
// ORDER BY [Name] ASC 
// OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY

// INSERT example
var insertQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .InsertInto(u => new { u.Name, u.Age, u.Department })
    .Values("John Doe", 30, "Engineering");

string insertSql = insertQuery.ToSql();
// INSERT INTO [User] ([Name], [Age], [Department]) VALUES ('John Doe', 30, 'Engineering')

// UPDATE example
var updateQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Update()
    .Set(u => u.Name, "Updated Name")
    .Set(u => u.Age, u => u.Age + 1)
    .Where(u => u.Id == 1);

string updateSql = updateQuery.ToSql();
// UPDATE [User] SET [Name] = 'Updated Name', [Age] = [Age] + 1 WHERE [Id] = 1

// DELETE example
var deleteQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Delete(u => u.IsActive == false);

string deleteSql = deleteQuery.ToSql();
// DELETE FROM [User] WHERE [IsActive] = 0
```

---

## 🌐 SqlDefine

Database dialect definitions for multi-database support.

### Static Properties
```csharp
public static SqlDefine SqlServer { get; }    // SQL Server: [column] with @param
public static SqlDefine MySql { get; }        // MySQL: `column` with @param
public static SqlDefine PostgreSql { get; }   // PostgreSQL: "column" with $param
public static SqlDefine SQLite { get; }       // SQLite: [column] with $param
public static SqlDefine Oracle { get; }       // Oracle: "column" with :param
```

### Properties
```csharp
public string ColumnWrapper { get; }          // Column name wrapper
public string ParameterPrefix { get; }        // Parameter prefix
public string TableWrapper { get; }           // Table name wrapper
public string StringQuote { get; }           // String literal quote
```

### Usage Examples
```csharp
// SQL Server
var sqlServerQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Where(u => u.Name == "John")
    .ToSql();
// WHERE [Name] = 'John'

// MySQL
var mysqlQuery = ExpressionToSql<User>.Create(SqlDefine.MySql)
    .Where(u => u.Name == "John")
    .ToSql();
// WHERE `Name` = 'John'

// PostgreSQL
var postgresQuery = ExpressionToSql<User>.Create(SqlDefine.PostgreSql)
    .Where(u => u.Name == "John")
    .ToSql();
// WHERE "Name" = 'John'
```

---

## 🔍 GroupedExpressionToSql<T, TKey>

Specialized query builder for GROUP BY operations.

### Methods
```csharp
// Select with grouping
public ExpressionToSql<TResult> Select<TResult>(Expression<Func<IGrouping<TKey, T>, TResult>> selector)

// Add HAVING condition
public GroupedExpressionToSql<T, TKey> Having(Expression<Func<IGrouping<TKey, T>, bool>> predicate)

// Convert to regular builder
public ExpressionToSql<T> AsNonGrouped()
```

### Usage Example
```csharp
var groupQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .GroupBy(u => u.Department)
    .Select(g => new 
    {
        Department = g.Key,
        Count = g.Count(),
        AvgAge = g.Average(u => u.Age),
        MaxAge = g.Max(u => u.Age)
    })
    .Having(g => g.Count() > 5);

string sql = groupQuery.ToSql();
// SELECT [Department], COUNT(*), AVG([Age]), MAX([Age])
// FROM [User] 
// GROUP BY [Department] 
// HAVING COUNT(*) > 5
```

---

## 🛠️ Extension Methods

Additional utility methods for enhanced functionality.

### String Extensions
```csharp
// Safe SQL identifier escaping
public static string ToSafeIdentifier(this string value)

// Parameter value conversion
public static string ToSqlValue(this object? value)
```

### Type Extensions
```csharp
// Check if type is nullable
public static bool IsNullableType(this Type type)

// Get underlying type from nullable
public static Type GetUnderlyingType(this Type type)
```

---

## 🎯 Best Practices

### 1. Choose the Right API
```csharp
// Simple one-time queries → ParameterizedSql
var simple = ParameterizedSql.Create("SELECT COUNT(*) FROM Users", null);

// Reusable queries → SqlTemplate
var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id");

// Complex type-safe building → ExpressionToSql<T>
var complex = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Where(u => u.IsActive && u.Age > 18);
```

### 2. Performance Optimization
```csharp
// ✅ Reuse templates
var userTemplate = SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id");
var user1 = userTemplate.Execute(new { id = 1 });
var user2 = userTemplate.Execute(new { id = 2 });

// ✅ Convert to template for reuse
var baseQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Where(u => u.IsActive);
var template = baseQuery.ToTemplate();
```

### 3. AOT Compatibility
```csharp
// ✅ AOT-friendly: Explicit column specification
.InsertInto(u => new { u.Name, u.Email })

// ⚠️ Reflection-based: Use only when necessary
.InsertIntoAll()
```

---

## 📊 Type Safety Features

Sqlx 3.0 provides compile-time safety through:

1. **Expression Validation**: LINQ expressions are validated at compile time
2. **Type Checking**: Parameter types are enforced
3. **SQL Generation**: SQL is generated safely without injection risks
4. **Null Safety**: Proper handling of nullable types

---

This completes the API reference for Sqlx 3.0. For more examples and usage patterns, see the [Quick Start Guide](QUICK_START_GUIDE.md) and [Best Practices](BEST_PRACTICES.md).