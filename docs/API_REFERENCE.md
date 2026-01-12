# API Reference

Complete API documentation for Sqlx.

## Attributes

### `[SqlDefine]`

Specifies the database dialect for SQL generation.

**Namespace:** `Sqlx.Annotations`

**Syntax:**
```csharp
[SqlDefine(SqlDefineTypes dialect)]
```

**Parameters:**
- `dialect` - The database dialect to use

**Available Dialects:**
- `SqlDefineTypes.SQLite` - SQLite database
- `SqlDefineTypes.PostgreSql` - PostgreSQL database
- `SqlDefineTypes.MySql` - MySQL/MariaDB database
- `SqlDefineTypes.SqlServer` - Microsoft SQL Server

**Example:**
```csharp
[SqlDefine(SqlDefineTypes.PostgreSql)]
public interface IUserRepository { }
```

---

### `[SqlTemplate]`

Defines an SQL template for a repository method.

**Namespace:** `Sqlx.Annotations`

**Syntax:**
```csharp
[SqlTemplate(string sql)]
```

**Parameters:**
- `sql` - SQL template string with placeholders

**Example:**
```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
Task<User?> GetByIdAsync(long id);
```

**Rules:**
- Method must be in an interface
- Parameters must match SQL template parameters
- Return type must be `Task<T>`, `Task<List<T>>`, or `Task<int>`

---

### `[RepositoryFor]`

Marks a class as implementing a repository interface.

**Namespace:** `Sqlx.Annotations`

**Syntax:**
```csharp
[RepositoryFor(Type interfaceType)]
[RepositoryFor(Type interfaceType, SqlDefineTypes Dialect = ..., string TableName = ...)]
```

**Parameters:**
- `interfaceType` - The repository interface to implement
- `Dialect` (optional) - Override dialect for this implementation
- `TableName` (optional) - Override table name for this implementation

**Example:**
```csharp
// Basic usage
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository { }

// With dialect and table name override
[RepositoryFor(typeof(IUserRepository), 
    Dialect = SqlDefineTypes.PostgreSql, 
    TableName = "users")]
public partial class PostgresUserRepository : IUserRepository { }
```

**Requirements:**
- Class must be marked as `partial`
- Class must implement the specified interface
- Class must have a constructor accepting `DbConnection`

---

### `[TableName]`

Specifies the database table name for an entity or repository.

**Namespace:** `Sqlx.Annotations`

**Syntax:**
```csharp
[TableName(string tableName)]
```

**Parameters:**
- `tableName` - The table name in the database

**Example:**
```csharp
[TableName("users")]
public class User { }

[TableName("user_profiles")]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository { }
```

---

### `[ReturnInsertedId]`

Indicates that an INSERT method should return the auto-generated ID.

**Namespace:** `Sqlx.Annotations`

**Syntax:**
```csharp
[ReturnInsertedId]
[ReturnInsertedId(string idColumnName = "id")]
```

**Parameters:**
- `idColumnName` (optional) - Name of the ID column (default: "id")

**Example:**
```csharp
[SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
[ReturnInsertedId]
Task<long> InsertAsync(User user);

// Custom ID column name
[SqlTemplate("INSERT INTO {{table}} ({{columns --exclude UserId}}) VALUES ({{values --exclude UserId}})")]
[ReturnInsertedId("user_id")]
Task<long> InsertAsync(User user);
```

**Return Types:**
- `Task<long>` - For 64-bit integer IDs
- `Task<int>` - For 32-bit integer IDs

---

### `[BatchOperation]`

Configures batch operation behavior.

**Namespace:** `Sqlx.Annotations`

**Syntax:**
```csharp
[BatchOperation(int MaxBatchSize = 1000, int MaxParametersPerBatch = 2000)]
```

**Parameters:**
- `MaxBatchSize` - Maximum number of items per batch
- `MaxParametersPerBatch` - Maximum number of parameters per batch

**Example:**
```csharp
[SqlTemplate("INSERT INTO {{table}} (name, age) VALUES {{batch_values}}")]
[BatchOperation(MaxBatchSize = 500)]
Task<int> BatchInsertAsync(IEnumerable<User> users);
```

---

### `[ExpressionToSql]`

Marks a parameter as a LINQ expression to be converted to SQL.

**Namespace:** `Sqlx.Annotations`

**Syntax:**
```csharp
[ExpressionToSql]
```

**Example:**
```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{where}}")]
Task<List<User>> QueryAsync([ExpressionToSql] Expression<Func<User, bool>> predicate);
```

**Usage:**
```csharp
var adults = await repo.QueryAsync(u => u.Age >= 18 && u.IsActive);
```

**Supported Operators:**
- Comparison: `==`, `!=`, `>`, `>=`, `<`, `<=`
- Logical: `&&`, `||`, `!`
- String: `Contains()`, `StartsWith()`, `EndsWith()`
- Null checks: `== null`, `!= null`

---

### `[SqlxDebugger]`

Enables SQL debugging methods for a repository.

**Namespace:** `Sqlx.Annotations`

**Syntax:**
```csharp
[SqlxDebugger]
```

**Example:**
```csharp
[SqlxDebugger]
[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("users")]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository { }
```

**Generated Methods:**
For each repository method, generates a corresponding `Get{MethodName}Sql()` method:

```csharp
// Original method
Task<User?> GetByIdAsync(long id);

// Generated debug method
string GetGetByIdAsyncSql(long id);
```

**Usage:**
```csharp
var repo = new UserRepository(connection);
var sql = repo.GetGetByIdAsyncSql(123);
Console.WriteLine(sql);
// Output: SELECT id, name, age FROM users WHERE id = @id
```

---

## Interfaces

### `ICrudRepository<TEntity, TKey>`

Standard CRUD operations interface.

**Namespace:** `Sqlx`

**Type Parameters:**
- `TEntity` - The entity type
- `TKey` - The primary key type

**Methods:**

```csharp
Task<TEntity?> GetByIdAsync(TKey id);
Task<List<TEntity>> GetAllAsync();
Task<TKey> InsertAsync(TEntity entity);
Task<int> UpdateAsync(TEntity entity);
Task<int> DeleteAsync(TKey id);
Task<long> CountAsync();
```

**Example:**
```csharp
[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("users")]
[RepositoryFor(typeof(ICrudRepository<User, long>))]
public partial class UserRepository : ICrudRepository<User, long> { }
```

---

### `IAggregateRepository<TEntity, TKey>`

Repository with aggregate operations.

**Namespace:** `Sqlx`

**Type Parameters:**
- `TEntity` - The entity type
- `TKey` - The primary key type

**Methods:**

```csharp
Task<long> CountAsync();
Task<long> CountAsync(Expression<Func<TEntity, bool>> predicate);
Task<TResult> SumAsync<TResult>(Expression<Func<TEntity, TResult>> selector);
Task<TResult> AverageAsync<TResult>(Expression<Func<TEntity, TResult>> selector);
Task<TResult> MaxAsync<TResult>(Expression<Func<TEntity, TResult>> selector);
Task<TResult> MinAsync<TResult>(Expression<Func<TEntity, TResult>> selector);
```

---

### `IBatchRepository<TEntity, TKey>`

Repository with batch operations.

**Namespace:** `Sqlx`

**Type Parameters:**
- `TEntity` - The entity type
- `TKey` - The primary key type

**Methods:**

```csharp
Task<int> BatchInsertAsync(IEnumerable<TEntity> entities);
Task<int> BatchUpdateAsync(IEnumerable<TEntity> entities);
Task<int> BatchDeleteAsync(IEnumerable<TKey> ids);
```

---

## Classes

### `SqlTemplate`

Represents a compiled SQL template with parameters.

**Namespace:** `Sqlx`

**Properties:**

```csharp
public string Sql { get; }
public Dictionary<string, object?> Parameters { get; }
```

**Methods:**

```csharp
// Create ADO.NET command
DbCommand CreateCommand(DbConnection connection);

// Execute scalar query
Task<T> ExecuteScalarAsync<T>(DbConnection connection);

// Execute non-query (INSERT/UPDATE/DELETE)
Task<int> ExecuteNonQueryAsync(DbConnection connection);

// Execute reader
Task<DbDataReader> ExecuteReaderAsync(DbConnection connection);
```

**Example:**
```csharp
public interface IUserRepository
{
    [SqlTemplate("SELECT COUNT(*) FROM {{table}} WHERE age >= @minAge")]
    SqlTemplate GetCountSql(int minAge);
}

// Usage
var template = repo.GetCountSql(18);
int count = await template.ExecuteScalarAsync<int>(connection);
```

---

## Enums

### `SqlDefineTypes`

Database dialect enumeration.

**Namespace:** `Sqlx`

**Values:**

```csharp
public enum SqlDefineTypes
{
    MySql = 0,
    SqlServer = 1,
    PostgreSql = 2,
    Oracle = 3,
    DB2 = 4,
    SQLite = 5
}
```

---

## Extension Methods

### `ExecuteScalarAsync<T>`

Executes a SQL template and returns a scalar value.

**Namespace:** `Sqlx`

**Syntax:**
```csharp
Task<T> ExecuteScalarAsync<T>(
    this SqlTemplate template, 
    DbConnection connection,
    DbTransaction? transaction = null,
    Dictionary<string, object?>? parameterOverrides = null)
```

**Example:**
```csharp
var template = repo.GetCountSql(18);
int count = await template.ExecuteScalarAsync<int>(connection);
```

---

### `ExecuteNonQueryAsync`

Executes a SQL template that doesn't return data.

**Namespace:** `Sqlx`

**Syntax:**
```csharp
Task<int> ExecuteNonQueryAsync(
    this SqlTemplate template,
    DbConnection connection,
    DbTransaction? transaction = null,
    Dictionary<string, object?>? parameterOverrides = null)
```

**Returns:** Number of rows affected

**Example:**
```csharp
var template = repo.GetInsertSql(user);
int rowsAffected = await template.ExecuteNonQueryAsync(connection);
```

---

### `ExecuteReaderAsync`

Executes a SQL template and returns a data reader.

**Namespace:** `Sqlx`

**Syntax:**
```csharp
Task<DbDataReader> ExecuteReaderAsync(
    this SqlTemplate template,
    DbConnection connection,
    DbTransaction? transaction = null,
    Dictionary<string, object?>? parameterOverrides = null)
```

**Example:**
```csharp
var template = repo.GetUsersSql();
using var reader = await template.ExecuteReaderAsync(connection);
while (await reader.ReadAsync())
{
    var id = reader.GetInt64(0);
    var name = reader.GetString(1);
}
```

---

## Type Mappings

### C# to SQL Type Mappings

| C# Type | SQLite | PostgreSQL | MySQL | SQL Server |
|---------|--------|------------|-------|------------|
| `int` | `INTEGER` | `integer` | `INT` | `INT` |
| `long` | `INTEGER` | `bigint` | `BIGINT` | `BIGINT` |
| `string` | `TEXT` | `varchar` | `VARCHAR` | `NVARCHAR` |
| `bool` | `INTEGER` | `boolean` | `BOOLEAN` | `BIT` |
| `decimal` | `REAL` | `decimal` | `DECIMAL` | `DECIMAL` |
| `DateTime` | `TEXT` | `timestamp` | `DATETIME` | `DATETIME2` |
| `Guid` | `TEXT` | `uuid` | `CHAR(36)` | `UNIQUEIDENTIFIER` |
| `byte[]` | `BLOB` | `bytea` | `BLOB` | `VARBINARY` |

---

## See Also

- [Quick Start Guide](QUICK_START.md)
- [Placeholder Reference](PLACEHOLDER_REFERENCE.md)
- [Best Practices](BEST_PRACTICES.md)
- [Multi-Database Guide](MULTI_DATABASE.md)
