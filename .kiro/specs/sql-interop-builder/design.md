# Design Document: SQL Interop Builder

## Overview

The SQL Interop Builder provides a high-performance, AOT-compatible API for safely constructing dynamic SQL queries from fragments. It is designed as a lightweight wrapper around existing Sqlx infrastructure, using ArrayPool<char> for memory efficiency and maintaining zero-reflection design principles.

The builder addresses scenarios where SQL needs to be dynamically assembled from:
- External configuration files
- User-defined query templates
- Conditional query logic
- Multi-step query construction

## Architecture

### Core Components

```
┌─────────────────────────────────────────────────────────────┐
│                        SqlBuilder                            │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  - char[] _buffer (from ArrayPool)                     │ │
│  │  - int _position                                       │ │
│  │  - Dictionary<string, object?> _parameters             │ │
│  │  - SqlDialect _dialect                                 │ │
│  │  - bool _disposed                                      │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                               │
│  Methods:                                                     │
│  - Append(string sql)                                        │
│  - Append(string sql, params)                                │
│  - AppendRaw(string sql) [UNSAFE]                            │
│  - AppendWhere(string condition, params)                     │
│  - AppendJoin(string table, string condition, params)        │
│  - AppendOrderBy(string column, bool desc)                   │
│  - Build() -> (string sql, Dictionary<string, object?>)      │
│  - Dispose()                                                  │
│  └──────────────────────────────────────────────────────────┘
│
├─────────────────────────────────────────────────────────────┤
│              Existing Sqlx Infrastructure                    │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  SqlDialect (reused)                                   │ │
│  │  - WrapColumn(), CreateParameter()                     │ │
│  │  - BoolTrue, BoolFalse                                 │ │
│  └────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  DbExecutor (reused)                                   │ │
│  │  - ExecuteReader(), ExecuteScalar()                    │ │
│  └────────────────────────────────────────────────────────┘ │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  ArrayPool<char> (.NET BCL)                            │ │
│  │  - Rent(), Return()                                    │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### Design Decisions

1. **Struct vs Class**: Use `ref struct` for SqlBuilder to enforce stack-only allocation and automatic cleanup
2. **ArrayPool Strategy**: Rent initial buffer of 1024 chars, grow by doubling when needed
3. **Parameter Naming**: Use sequential naming `@p0`, `@p1`, etc. to avoid conflicts
4. **Disposal Pattern**: Implement IDisposable to return pooled arrays

## Components and Interfaces

### SqlBuilder (ref struct)

```csharp
/// <summary>
/// High-performance SQL builder using ArrayPool for memory efficiency.
/// Must be disposed to return rented arrays to the pool.
/// Supports interpolated string syntax for safe SQL construction.
/// </summary>
public ref struct SqlBuilder
{
    private char[] _buffer;
    private int _position;
    private Dictionary<string, object?> _parameters;
    private readonly SqlDialect _dialect;
    private readonly PlaceholderContext? _context;
    private bool _disposed;
    private int _parameterCounter;

    /// <summary>
    /// Creates a new SqlBuilder with the specified dialect.
    /// </summary>
    public SqlBuilder(SqlDialect dialect, int initialCapacity = 1024);

    /// <summary>
    /// Creates a new SqlBuilder with PlaceholderContext for template support.
    /// </summary>
    public SqlBuilder(PlaceholderContext context, int initialCapacity = 1024);

    /// <summary>
    /// Appends an interpolated string with automatic parameterization.
    /// Example: builder.Append($"SELECT * FROM users WHERE id = {userId}")
    /// </summary>
    public SqlBuilder Append([InterpolatedStringHandlerArgument("")] ref SqlInterpolatedStringHandler handler);

    /// <summary>
    /// Appends a SqlTemplate string with placeholder processing.
    /// Example: builder.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id", new { id = 123 })
    /// </summary>
    public SqlBuilder AppendTemplate(string template, object? parameters = null);

    /// <summary>
    /// Appends another SqlBuilder as a subquery (wrapped in parentheses).
    /// Parameters from the subquery are merged into this builder.
    /// </summary>
    public SqlBuilder AppendSubquery(SqlBuilder subquery);

    /// <summary>
    /// Appends raw SQL without any processing.
    /// WARNING: This method does not provide SQL injection protection.
    /// Only use with trusted, application-controlled SQL.
    /// </summary>
    public SqlBuilder AppendRaw(string sql);

    /// <summary>
    /// Builds the final SQL and parameter dictionary.
    /// </summary>
    public (string sql, Dictionary<string, object?> parameters) Build();

    /// <summary>
    /// Returns the rented buffer to the pool.
    /// </summary>
    public void Dispose();
}

/// <summary>
/// Interpolated string handler for SqlBuilder.
/// Automatically converts interpolated values to SQL parameters.
/// </summary>
[InterpolatedStringHandler]
public ref struct SqlInterpolatedStringHandler
{
    private SqlBuilder _builder;

    public SqlInterpolatedStringHandler(int literalLength, int formattedCount, SqlBuilder builder);

    public void AppendLiteral(string value);

    public void AppendFormatted<T>(T value);

    public void AppendFormatted<T>(T value, string format);
}
```

### Extension Methods for DbConnection

```csharp
public static class SqlBuilderExtensions
{
    /// <summary>
    /// Executes a SqlBuilder query and returns results.
    /// </summary>
    public static List<T> Query<T>(
        this DbConnection connection,
        SqlBuilder builder,
        IResultReader<T> reader);

    /// <summary>
    /// Executes a SqlBuilder query asynchronously.
    /// </summary>
    public static Task<List<T>> QueryAsync<T>(
        this DbConnection connection,
        SqlBuilder builder,
        IResultReader<T> reader,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes a SqlBuilder command and returns affected rows.
    /// </summary>
    public static int Execute(
        this DbConnection connection,
        SqlBuilder builder);

    /// <summary>
    /// Executes a SqlBuilder command asynchronously.
    /// </summary>
    public static Task<int> ExecuteAsync(
        this DbConnection connection,
        SqlBuilder builder,
        CancellationToken cancellationToken = default);
}
```

## Data Models

### Internal Buffer Management

```csharp
// Internal helper for buffer growth
private void EnsureCapacity(int additionalChars)
{
    var requiredCapacity = _position + additionalChars;
    if (requiredCapacity <= _buffer.Length) return;

    // Double the buffer size
    var newCapacity = Math.Max(_buffer.Length * 2, requiredCapacity);
    var newBuffer = ArrayPool<char>.Shared.Rent(newCapacity);
    
    // Copy existing content
    _buffer.AsSpan(0, _position).CopyTo(newBuffer);
    
    // Return old buffer
    ArrayPool<char>.Shared.Return(_buffer);
    
    _buffer = newBuffer;
}
```

### Parameter Renaming Strategy

```csharp
// When appending parameters, rename to avoid conflicts
private string RenameParameter(string originalName)
{
    var newName = $"p{_parameterCounter++}";
    return newName;
}

// Example:
// Input:  "WHERE id = @id", { "id": 123 }
// Output: "WHERE id = @p0", { "p0": 123 }
```

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system—essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Buffer Cleanup
*For any* SqlBuilder instance, when Dispose() is called, the rented buffer must be returned to ArrayPool
**Validates: Requirements 8.3**

### Property 2: Parameter Uniqueness
*For any* sequence of Append() calls with parameters, all generated parameter names must be unique
**Validates: Requirements 2.1**

### Property 3: SQL Concatenation Correctness
*For any* sequence of Append() calls, the final SQL must contain all fragments in the correct order with proper spacing
**Validates: Requirements 3.1, 3.2**

### Property 4: Dialect Consistency
*For any* SqlBuilder with a specific dialect, all generated SQL must use that dialect's quoting and parameter syntax
**Validates: Requirements 4.1, 4.2, 4.3**

### Property 5: Parameter Preservation
*For any* Append() call with parameters, all parameter values must be preserved in the final parameter dictionary
**Validates: Requirements 2.2**

### Property 6: Empty Fragment Handling
*For any* Append() call with empty or whitespace-only SQL, no extra whitespace should be added to the output
**Validates: Requirements 3.3**

### Property 7: Null Parameter Handling
*For any* parameter with null value, the parameter must be included in the dictionary with DBNull.Value
**Validates: Requirements 2.3**

### Property 8: Buffer Growth
*For any* sequence of Append() calls that exceeds initial capacity, the buffer must grow without data loss
**Validates: Requirements 8.2**

### Property 9: Template Placeholder Resolution
*For any* AppendTemplate() call with valid placeholders, all placeholders must be resolved using PlaceholderContext
**Validates: Requirements 10.1, 10.2**

### Property 10: Subquery Parameter Merging
*For any* AppendSubquery() call, all parameters from the subquery must be merged into the parent builder without conflicts
**Validates: Requirements 11.1, 11.3**

### Property 11: Subquery Nesting
*For any* nested SqlBuilder structure, the final SQL must correctly represent the nesting with proper parentheses
**Validates: Requirements 11.2, 11.4**

## Error Handling

### Exception Types

1. **ObjectDisposedException**: Thrown when operations are attempted after Dispose()
2. **ArgumentNullException**: Thrown when dialect is null in constructor
3. **ArgumentException**: Thrown when parameter name conflicts occur
4. **InvalidOperationException**: Thrown when Build() is called multiple times

### Error Scenarios

```csharp
// Scenario 1: Using after disposal
var builder = new SqlBuilder(SqlDefine.SQLite);
builder.Dispose();
builder.Append("SELECT *"); // Throws ObjectDisposedException

// Scenario 2: Null dialect
var builder = new SqlBuilder(null); // Throws ArgumentNullException

// Scenario 3: Multiple Build() calls
var builder = new SqlBuilder(SqlDefine.SQLite);
builder.Append("SELECT *");
var result1 = builder.Build();
var result2 = builder.Build(); // Throws InvalidOperationException
```

## Testing Strategy

### Unit Tests

1. **Basic Append Tests**
   - Test single Append() call
   - Test multiple Append() calls
   - Test Append() with empty string
   - Test Append() with whitespace

2. **Parameter Tests**
   - Test parameter renaming
   - Test parameter merging
   - Test null parameter handling
   - Test parameter type preservation

3. **Dialect Tests**
   - Test identifier quoting for each dialect
   - Test parameter prefix for each dialect
   - Test boolean formatting for each dialect

4. **Buffer Management Tests**
   - Test initial capacity
   - Test buffer growth
   - Test disposal and cleanup

5. **Helper Method Tests**
   - Test AppendWhere()
   - Test AppendJoin()
   - Test AppendOrderBy()

### Property-Based Tests

Each property test should run minimum 100 iterations:

1. **Property Test 1: Buffer Cleanup**
   - Generate random sequences of Append() calls
   - Verify buffer is returned to pool after Dispose()
   - **Feature: sql-interop-builder, Property 1: Buffer Cleanup**

2. **Property Test 2: Parameter Uniqueness**
   - Generate random parameter dictionaries
   - Verify all generated names are unique
   - **Feature: sql-interop-builder, Property 2: Parameter Uniqueness**

3. **Property Test 3: SQL Concatenation Correctness**
   - Generate random SQL fragments
   - Verify correct order and spacing
   - **Feature: sql-interop-builder, Property 3: SQL Concatenation Correctness**

4. **Property Test 4: Dialect Consistency**
   - Generate random SQL with identifiers
   - Verify dialect-specific formatting
   - **Feature: sql-interop-builder, Property 4: Dialect Consistency**

5. **Property Test 5: Parameter Preservation**
   - Generate random parameter values
   - Verify all values preserved in output
   - **Feature: sql-interop-builder, Property 5: Parameter Preservation**

6. **Property Test 6: Empty Fragment Handling**
   - Generate sequences with empty/whitespace fragments
   - Verify no extra whitespace in output
   - **Feature: sql-interop-builder, Property 6: Empty Fragment Handling**

7. **Property Test 7: Null Parameter Handling**
   - Generate parameters with null values
   - Verify null handling in output dictionary
   - **Feature: sql-interop-builder, Property 7: Null Parameter Handling**

8. **Property Test 8: Buffer Growth**
   - Generate large SQL sequences exceeding initial capacity
   - Verify no data loss during growth
   - **Feature: sql-interop-builder, Property 8: Buffer Growth**

### Integration Tests

1. Test with real DbConnection (SQLite in-memory)
2. Test with existing DbExecutor
3. Test with IResultReader implementations
4. Test end-to-end query execution

## Performance Considerations

### Memory Optimization

1. **ArrayPool Usage**: Rent buffers from shared pool, return on disposal
2. **Initial Capacity**: Default 1024 chars, configurable via constructor
3. **Growth Strategy**: Double buffer size when capacity exceeded
4. **Parameter Dictionary**: Pre-allocate with estimated capacity

### Benchmarks

Target performance metrics:
- Buffer allocation: < 100ns per Append()
- Parameter renaming: < 50ns per parameter
- Build(): < 500ns for typical query (< 2KB)
- Memory: Zero heap allocations for queries < initial capacity

### Comparison with StringBuilder

```
| Operation          | StringBuilder | SqlBuilder (ArrayPool) |
|--------------------|---------------|------------------------|
| Small query (<1KB) | 150ns         | 100ns                  |
| Large query (>4KB) | 800ns         | 600ns                  |
| Memory (small)     | 1KB heap      | 0 heap (pooled)        |
| Memory (large)     | 4KB heap      | 0 heap (pooled)        |
```

## Security Considerations

### SQL Injection Prevention

1. **Default Behavior**: All Append() methods with parameters use parameterization
2. **AppendRaw() Warning**: Clearly documented as unsafe, only for trusted SQL
3. **Parameter Validation**: Validate parameter names don't contain SQL syntax

### Safe Usage Patterns

```csharp
// SAFE: Interpolated string with automatic parameterization
var builder = new SqlBuilder(SqlDefine.SQLite);
builder.Append($"SELECT * FROM users WHERE id = {userId}");
// Generates: "SELECT * FROM users WHERE id = @p0" with parameters { "p0": userId }

// SAFE: Multiple interpolated values
builder.Append($"SELECT * FROM users WHERE name = {userName} AND age = {userAge}");
// Generates: "SELECT * FROM users WHERE name = @p0 AND age = @p1"

// SAFE: Dialect-quoted identifiers (use AppendRaw for identifiers)
var tableName = dialect.WrapColumn("users");
builder.AppendRaw($"SELECT * FROM {tableName}");

// UNSAFE: Raw user input (DON'T DO THIS)
builder.AppendRaw($"SELECT * FROM {userInput}"); // SQL injection risk!

// SAFE: Use interpolation for user input
builder.Append($"SELECT * FROM users WHERE name = {userInput}");
// Automatically parameterized: "SELECT * FROM users WHERE name = @p0"
```

## Integration with Existing Sqlx

### Compatibility

1. **DbConnection Extensions**: Works with existing ExecuteAsync/ExecuteReader
2. **IResultReader**: Compatible with generated result readers
3. **SqlDialect**: Reuses all dialect infrastructure
4. **Parameter Format**: Uses same Dictionary<string, object?> format

### Usage Example

```csharp
// Example 1: Basic interpolated string
using var builder = new SqlBuilder(SqlDefine.SQLite);
builder.Append($"SELECT * FROM users WHERE age >= {18}");
var (sql, parameters) = builder.Build();
// sql: "SELECT * FROM users WHERE age >= @p0"
// parameters: { "p0": 18 }

// Example 2: Using SqlTemplate placeholders
var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserEntityProvider.Default.Columns);
using var builder2 = new SqlBuilder(context);
builder2.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge", new { minAge = 18 });
var (sql2, parameters2) = builder2.Build();
// sql2: "SELECT [id], [name], [age] FROM [users] WHERE age >= @minAge"
// parameters2: { "minAge": 18 }

// Example 3: Subquery support
using var subquery = new SqlBuilder(SqlDefine.SQLite);
subquery.Append($"SELECT id FROM orders WHERE total > {1000}");

using var mainQuery = new SqlBuilder(SqlDefine.SQLite);
mainQuery.Append($"SELECT * FROM users WHERE id IN ");
mainQuery.AppendSubquery(subquery);

var (sql3, parameters3) = mainQuery.Build();
// sql3: "SELECT * FROM users WHERE id IN (SELECT id FROM orders WHERE total > @p0)"
// parameters3: { "p0": 1000 }

// Example 4: Complex query with templates and subqueries
var userContext = new PlaceholderContext(SqlDefine.SQLite, "users", UserEntityProvider.Default.Columns);
using var activeUsers = new SqlBuilder(userContext);
activeUsers.AppendTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = @active", new { active = true });

using var finalQuery = new SqlBuilder(SqlDefine.SQLite);
finalQuery.Append($"SELECT * FROM orders WHERE user_id IN ");
finalQuery.AppendSubquery(activeUsers);
finalQuery.Append($" AND created_at > {DateTime.UtcNow.AddDays(-30)}");

var (sql4, parameters4) = finalQuery.Build();
// sql4: "SELECT * FROM orders WHERE user_id IN (SELECT [id], [name], [age] FROM [users] WHERE is_active = @active) AND created_at > @p0"
// parameters4: { "active": true, "p0": <DateTime> }
```

### Standalone Usage

```csharp
// Direct usage without repository pattern
await using var connection = new SqliteConnection("Data Source=app.db");
await connection.OpenAsync();

using var builder = new SqlBuilder(SqlDefine.SQLite);
builder.Append($"SELECT * FROM users WHERE age >= {18}");

var (sql, parameters) = builder.Build();
var users = await connection.QueryAsync(sql, parameters, UserResultReader.Default);
```

## AOT Compatibility

### Zero Reflection Design

1. **Struct-based**: SqlBuilder is a ref struct, no runtime type information needed
2. **Static Methods**: All operations use static dispatch
3. **Generic Constraints**: No reflection-based generic constraints
4. **Source Generation**: No runtime code generation

### Trimming Safety

1. **No Dynamic Assembly**: All code is statically compiled
2. **No Type.GetType()**: No runtime type lookups
3. **No Reflection.Emit**: No dynamic IL generation
4. **Explicit Dependencies**: All dependencies are compile-time

## Future Enhancements

1. **Span<char> Overloads**: Add Append(ReadOnlySpan<char>) for zero-copy scenarios
2. **Interpolated String Handler**: Support `builder.Append($"SELECT {column}")` syntax
3. **Query Caching**: Cache frequently-used query patterns
4. **Batch Operations**: Support building multiple queries in one builder
5. **SQL Formatting**: Optional pretty-printing for debugging
