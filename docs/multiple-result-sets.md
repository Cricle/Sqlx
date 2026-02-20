# Multiple Result Sets

Sqlx supports returning multiple scalar values from a single SQL query using tuple return types. This feature allows you to efficiently retrieve multiple related values in one database round-trip.

## Overview

Multiple result sets enable you to:
- Return multiple scalar values from multiple SELECT statements
- Get affected row counts along with query results
- Combine INSERT/UPDATE/DELETE operations with SELECT queries
- Mix output parameters with tuple returns (for databases that support output parameters)

## Basic Usage

### Simple Multiple Result Sets

Use tuple return types to return multiple values:

```csharp
public interface IUserRepository
{
    [SqlTemplate(@"
        INSERT INTO users (name) VALUES (@name);
        SELECT last_insert_rowid();
        SELECT COUNT(*) FROM users
    ")]
    [ResultSetMapping(0, "rowsAffected")]
    [ResultSetMapping(1, "userId")]
    [ResultSetMapping(2, "totalUsers")]
    Task<(int rowsAffected, long userId, int totalUsers)> InsertAndGetStatsAsync(string name);
}

// Usage
var (rows, userId, total) = await repo.InsertAndGetStatsAsync("Alice");
Console.WriteLine($"Inserted user {userId}, total users: {total}");
```

### Default Mapping

If you don't specify `[ResultSetMapping]` attributes, Sqlx uses default mapping:
- First tuple element → affected row count (from ExecuteNonQuery)
- Remaining elements → SELECT results in order

```csharp
[SqlTemplate(@"
    INSERT INTO users (name) VALUES (@name);
    SELECT last_insert_rowid();
    SELECT COUNT(*) FROM users
")]
Task<(int rowsAffected, long userId, int totalUsers)> InsertAndGetStatsAsync(string name);
```

## ResultSetMapping Attribute

The `[ResultSetMapping]` attribute explicitly maps result sets to tuple elements:

```csharp
[ResultSetMapping(index, "elementName")]
```

- `index`: Zero-based index of the result set
  - Index 0: Affected row count (INSERT/UPDATE/DELETE)
  - Index 1+: SELECT statement results in order
- `elementName`: Name of the tuple element (must match exactly)

### Example with Multiple SELECTs

```csharp
[SqlTemplate(@"
    SELECT COUNT(*) FROM users;
    SELECT MAX(id) FROM users;
    SELECT MIN(id) FROM users
")]
[ResultSetMapping(0, "totalUsers")]
[ResultSetMapping(1, "maxId")]
[ResultSetMapping(2, "minId")]
Task<(int totalUsers, long maxId, long minId)> GetUserStatsAsync();
```

## Synchronous Methods

Both sync and async methods are supported:

```csharp
// Async
[SqlTemplate("...")]
Task<(int rows, long id)> InsertAsync(string name);

// Sync
[SqlTemplate("...")]
(int rows, long id) Insert(string name);
```

## Mixed Scenarios

You can combine output parameters with tuple returns (for databases that support output parameters like SQL Server):

```csharp
[SqlTemplate(@"
    INSERT INTO users (name, created_at) VALUES (@name, @createdAt);
    SELECT last_insert_rowid();
    SELECT COUNT(*) FROM users
")]
[ResultSetMapping(0, "rowsAffected")]
[ResultSetMapping(1, "userId")]
[ResultSetMapping(2, "totalUsers")]
(int rowsAffected, long userId, int totalUsers) InsertAndGetStats(
    string name,
    [OutputParameter(DbType.DateTime)] ref DateTime createdAt);
```

**Note**: SQLite does not support output parameters. This feature is only available with databases like SQL Server, PostgreSQL, MySQL, etc.

## Type Conversion

Sqlx automatically converts database types to tuple element types:

```csharp
Task<(int count, long id, string name)> GetDataAsync();
```

- `int` ← INTEGER, BIGINT (with range check)
- `long` ← BIGINT, INTEGER
- `string` ← VARCHAR, TEXT, CHAR
- Other types are converted using `Convert.ChangeType()`

## Error Handling

### Runtime Errors

Sqlx throws `InvalidOperationException` in these cases:

1. **Empty Result Set**:
   ```
   No data returned for result set N
   ```

2. **Missing Result Set**:
   ```
   Expected N result sets, but only M were returned
   ```

3. **Type Conversion Failure**:
   ```
   Cannot convert result set N value to TargetType
   ```

### Best Practices

1. **Always use named tuples** for clarity:
   ```csharp
   // Good
   Task<(int rowsAffected, long userId)> InsertAsync(string name);
   
   // Avoid
   Task<(int, long)> InsertAsync(string name);
   ```

2. **Use ResultSetMapping for complex queries** to make the mapping explicit:
   ```csharp
   [ResultSetMapping(0, "rowsAffected")]
   [ResultSetMapping(1, "userId")]
   [ResultSetMapping(2, "totalUsers")]
   ```

3. **Handle nullable results** when queries might return no data:
   ```csharp
   Task<(int? count, long? maxId)> GetStatsAsync();
   ```

4. **Keep queries simple** - multiple result sets work best with scalar values

## Limitations

1. **Tuple Size**: Maximum 8 elements (ValueTuple limitation)
2. **Scalar Values Only**: Each result set should return a single scalar value (first row, first column)
3. **Result Set Order**: Must match SQL statement order
4. **No Nested Tuples**: Nested tuples are not supported
5. **Database Support**: Output parameters require database support (not available in SQLite)

## Performance

Multiple result sets provide excellent performance:
- **Single Database Round-Trip**: All values retrieved in one call
- **Zero Reflection**: All code generated at compile-time
- **Type Safe**: Compile-time type checking
- **AOT Compatible**: Fully supports Native AOT compilation

## See Also

- [Output Parameters](output-parameters.md) - Using stored procedure output parameters
- [SQL Templates](sql-templates.md) - SQL template syntax and features
- [Source Generators](source-generators.md) - How code generation works
