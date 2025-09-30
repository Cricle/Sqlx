# Sqlx API Reference

This document provides detailed information about all public APIs in Sqlx.

## 🏗️ Core Architecture

```
Sqlx
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
public readonly record struct ParameterizedSql(string Sql, IReadOnlyDictionary<string, object?>? Parameters)
```

### Static Methods
```csharp
// Create with parameters
public static ParameterizedSql Create(string sql, IReadOnlyDictionary<string, object?>? parameters = null)
```

### Instance Methods
```csharp
// Render final SQL (inline parameter values)
public string Render()
```

### Usage Examples
```csharp
// Create with dictionary
var parameters = new Dictionary<string, object?>
{
    ["@id"] = 123,
    ["@active"] = true
};
var sql = ParameterizedSql.Create(
    "SELECT * FROM Users WHERE Id = @id AND IsActive = @active",
    parameters);

// Render SQL
string finalSql = sql.Render();
// Output: SELECT * FROM Users WHERE Id = 123 AND IsActive = 1
```

---

## 🎨 SqlTemplate

Reusable SQL template for executing the same SQL with different parameters.

### Constructor
```csharp
public readonly record struct SqlTemplate(string Sql, IReadOnlyDictionary<string, object?> Parameters)
```

### Static Methods
```csharp
// Parse SQL template
public static SqlTemplate Parse(string sql)
```

### Instance Methods
```csharp
// Execute with parameters
public ParameterizedSql Execute(IReadOnlyDictionary<string, object?>? parameters = null)

// Start fluent parameter binding
public SqlTemplateBuilder Bind()
```

### Usage Examples
```csharp
// Create template
var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Age > @age AND Department = @dept");

// Execute with different parameters
var youngEngineers = template.Execute(new Dictionary<string, object?>
{
    ["@age"] = 20,
    ["@dept"] = "Engineering"
});
var seniorSales = template.Execute(new Dictionary<string, object?>
{
    ["@age"] = 35,
    ["@dept"] = "Sales"
});

// Fluent binding
var customQuery = template.Bind()
    .Param("@age", 25)
    .Param("@dept", "Marketing")
    .Build();
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
    .Param("@age", 25)
    .Param("@dept", "IT")
    .Build();

string sql = query.Render();
```

---

## 🎯 ExpressionToSql<T>

Type-safe query builder for generating SQL from LINQ expressions.

### Static Factory Methods
```csharp
public static ExpressionToSql<T> Create(SqlDialect dialect)
public static ExpressionToSql<T> ForSqlServer()
public static ExpressionToSql<T> ForMySql()
public static ExpressionToSql<T> ForPostgreSQL()
public static ExpressionToSql<T> ForSqlite()
public static ExpressionToSql<T> ForOracle()
public static ExpressionToSql<T> ForDB2()
```

### SELECT Methods
```csharp
// Select specific columns by name
public ExpressionToSql<T> Select(params string[] columns)

// Select using expression
public ExpressionToSql<T> Select<TResult>(Expression<Func<T, TResult>> selector)

// Select using multiple expressions
public ExpressionToSql<T> Select(params Expression<Func<T, object>>[] selectors)
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
// INSERT with column selector
public ExpressionToSql<T> Insert(Expression<Func<T, object>>? selector = null)

// INSERT all columns (uses reflection)
public ExpressionToSql<T> InsertAll()

// INSERT with SELECT
public ExpressionToSql<T> InsertSelect(string sql)

// Specify values
public ExpressionToSql<T> Values(params object[] values)

// Add values
public ExpressionToSql<T> AddValues(params object[] values)
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

### Other Methods
```csharp
// Enable parameterized queries
public ExpressionToSql<T> UseParameterizedQueries()

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
var selectQuery = ExpressionToSql<User>.ForSqlServer()
    .Select(u => new { u.Id, u.Name, u.Age })
    .Where(u => u.Age > 18 && u.IsActive)
    .OrderBy(u => u.Name)
    .Take(10);

string selectSql = selectQuery.ToSql();

// INSERT example
var insertQuery = ExpressionToSql<User>.ForSqlServer()
    .Insert(u => new { u.Name, u.Age, u.Department })
    .Values("John Doe", 30, "Engineering");

string insertSql = insertQuery.ToSql();

// UPDATE example
var updateQuery = ExpressionToSql<User>.ForSqlServer()
    .Update()
    .Set(u => u.Name, "Updated Name")
    .Set(u => u.Age, u => u.Age + 1)
    .Where(u => u.Id == 1);

string updateSql = updateQuery.ToSql();

// DELETE example
var deleteQuery = ExpressionToSql<User>.ForSqlServer()
    .Delete(u => u.IsActive == false);

string deleteSql = deleteQuery.ToSql();
```

---

## 🌐 SqlDefine

Database dialect definitions for multi-database support.

### Static Properties
```csharp
public static readonly SqlDialect SqlServer     // SQL Server: [column] with @param
public static readonly SqlDialect MySql         // MySQL: `column` with @param
public static readonly SqlDialect PostgreSql    // PostgreSQL: "column" with $param
public static readonly SqlDialect SQLite        // SQLite: [column] with $param
public static readonly SqlDialect Oracle        // Oracle: "column" with :param
public static readonly SqlDialect DB2           // DB2: "column" with ?param

// Aliases for backward compatibility
public static readonly SqlDialect PgSql         // Alias for PostgreSql
public static readonly SqlDialect Sqlite        // Alias for SQLite
```

## 🎯 SqlDialect

SQL dialect configuration for database-specific syntax.

### Constructor
```csharp
public readonly record struct SqlDialect(
    string ColumnLeft,
    string ColumnRight,
    string StringLeft,
    string StringRight,
    string ParameterPrefix)
```

### Properties
```csharp
public string DatabaseType { get; }            // Database type name
public Annotations.SqlDefineTypes DbType { get; } // Database type enum
```

### Methods
```csharp
// Wrap column name with dialect-specific delimiters
public string WrapColumn(string columnName)

// Wrap string value with dialect-specific delimiters
public string WrapString(string value)

// Create parameter with dialect-specific prefix
public string CreateParameter(string name)

// Get concatenation syntax for database
public string GetConcatFunction(params string[] parts)
```

### Usage Examples
```csharp
// SQL Server
var sqlServerQuery = ExpressionToSql<User>.ForSqlServer()
    .Where(u => u.Name == "John")
    .ToSql();

// MySQL
var mysqlQuery = ExpressionToSql<User>.ForMySql()
    .Where(u => u.Name == "John")
    .ToSql();

// PostgreSQL
var postgresQuery = ExpressionToSql<User>.ForPostgreSQL()
    .Where(u => u.Name == "John")
    .ToSql();

// SQLite
var sqliteQuery = ExpressionToSql<User>.ForSqlite()
    .Where(u => u.Name == "John")
    .ToSql();
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
var complex = ExpressionToSql<User>.ForSqlServer()
    .Where(u => u.IsActive && u.Age > 18);
```

### 2. Performance Optimization
```csharp
// ✅ Reuse templates
var userTemplate = SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id");
var user1 = userTemplate.Execute(new Dictionary<string, object?> { ["@id"] = 1 });
var user2 = userTemplate.Execute(new Dictionary<string, object?> { ["@id"] = 2 });

// ✅ Convert to template for reuse
var baseQuery = ExpressionToSql<User>.ForSqlServer()
    .Where(u => u.IsActive);
var template = baseQuery.ToTemplate();
```

### 3. AOT Compatibility
```csharp
// ✅ AOT-friendly: Explicit column specification
.Insert(u => new { u.Name, u.Email })

// ⚠️ Reflection-based: Use only when necessary
.InsertAll()
```

---

## 📊 Type Safety Features

Sqlx provides compile-time safety through:

1. **Expression Validation**: LINQ expressions are validated at compile time
2. **Type Checking**: Parameter types are enforced
3. **SQL Generation**: SQL is generated safely without injection risks
4. **Null Safety**: Proper handling of nullable types

---

This completes the API reference for Sqlx. For more examples and usage patterns, see the [Quick Start Guide](QUICK_START_GUIDE.md) and [Best Practices](BEST_PRACTICES.md).
