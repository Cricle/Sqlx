# SqlTemplate ADO.NET Integration

## Overview

SqlTemplate now provides high-performance ADO.NET extension methods that allow you to easily convert SqlTemplate instances into DbCommand objects and execute them directly. This integration combines the convenience of SqlTemplate with the full power and flexibility of ADO.NET.

## Key Features

- ⚡ **High Performance** - ValueTask<T>, method inlining, ConfigureAwait(false)
- 🔒 **Thread-Safe** - Immutable SqlTemplate, stateless extensions
- 🗑️ **Low GC Pressure** - Minimal allocations, optimized dictionary lookups
- 🐛 **Debugger-Friendly** - Clear error messages with parameter names
- 🔄 **Parameter Overrides** - Replace parameter values without creating new templates
- ⏱️ **Sync & Async** - Both synchronous and asynchronous versions available

## Extension Methods

### CreateCommand

Creates a DbCommand from a SqlTemplate with optional parameter overrides.

```csharp
public static DbCommand CreateCommand(
    this SqlTemplate template,
    DbConnection connection,
    DbTransaction? transaction = null,
    int? commandTimeout = null,
    IReadOnlyDictionary<string, object?>? parameterOverrides = null)
```

**Example:**
```csharp
var template = new SqlTemplate(
    "SELECT * FROM Users WHERE Age >= @minAge",
    new Dictionary<string, object?> { ["@minAge"] = 25 });

// Use default parameters
using var cmd1 = template.CreateCommand(connection);

// Override parameters
var overrides = new Dictionary<string, object?> { ["@minAge"] = 30 };
using var cmd2 = template.CreateCommand(connection, parameterOverrides: overrides);

// With transaction and timeout
using var transaction = connection.BeginTransaction();
using var cmd3 = template.CreateCommand(connection, transaction, commandTimeout: 30);
```

### CreateCommandAsync

Asynchronously creates a DbCommand, opening the connection if needed.

```csharp
public static async ValueTask<DbCommand> CreateCommandAsync(
    this SqlTemplate template,
    DbConnection connection,
    DbTransaction? transaction = null,
    int? commandTimeout = null,
    IReadOnlyDictionary<string, object?>? parameterOverrides = null,
    CancellationToken cancellationToken = default)
```

**Example:**
```csharp
using var cmd = await template.CreateCommandAsync(connection);
```

### ExecuteScalar / ExecuteScalarAsync

Executes the template and returns a scalar value with automatic type conversion.

```csharp
// Non-generic version
public static object? ExecuteScalar(
    this SqlTemplate template,
    DbConnection connection,
    DbTransaction? transaction = null,
    IReadOnlyDictionary<string, object?>? parameterOverrides = null)

// Generic version with type conversion
public static T? ExecuteScalar<T>(
    this SqlTemplate template,
    DbConnection connection,
    DbTransaction? transaction = null,
    IReadOnlyDictionary<string, object?>? parameterOverrides = null)
```

**Examples:**
```csharp
var countTemplate = new SqlTemplate("SELECT COUNT(*) FROM Users", new Dictionary<string, object?>());

// Non-generic - returns object?
object? result = await countTemplate.ExecuteScalarAsync(connection);

// Generic with automatic type conversion
int count = await countTemplate.ExecuteScalarAsync<int>(connection);
long countLong = await countTemplate.ExecuteScalarAsync<long>(connection);

// Nullable types
int? nullableCount = await countTemplate.ExecuteScalarAsync<int?>(connection);

// String type
var nameTemplate = new SqlTemplate(
    "SELECT Name FROM Users WHERE Id = @id",
    new Dictionary<string, object?> { ["@id"] = 1 });
string? name = await nameTemplate.ExecuteScalarAsync<string>(connection);

// Handles NULL values
var emailTemplate = new SqlTemplate(
    "SELECT Email FROM Users WHERE Id = @id",
    new Dictionary<string, object?> { ["@id"] = 3 });
string? email = await emailTemplate.ExecuteScalarAsync<string>(connection); // Returns null if DB value is NULL
```

### ExecuteNonQuery / ExecuteNonQueryAsync

Executes the template and returns the number of affected rows.

```csharp
public static int ExecuteNonQuery(
    this SqlTemplate template,
    DbConnection connection,
    DbTransaction? transaction = null,
    IReadOnlyDictionary<string, object?>? parameterOverrides = null)

public static async ValueTask<int> ExecuteNonQueryAsync(
    this SqlTemplate template,
    DbConnection connection,
    DbTransaction? transaction = null,
    IReadOnlyDictionary<string, object?>? parameterOverrides = null,
    CancellationToken cancellationToken = default)
```

**Examples:**
```csharp
// INSERT with parameter overrides
var insertTemplate = new SqlTemplate(
    "INSERT INTO Users (Id, Name, Age) VALUES (@id, @name, @age)",
    new Dictionary<string, object?>
    {
        ["@id"] = 0,
        ["@name"] = "",
        ["@age"] = 0
    });

var overrides = new Dictionary<string, object?>
{
    ["@id"] = 100,
    ["@name"] = "Alice",
    ["@age"] = 25
};

int rowsInserted = await insertTemplate.ExecuteNonQueryAsync(connection, parameterOverrides: overrides);

// UPDATE
var updateTemplate = new SqlTemplate(
    "UPDATE Users SET Age = @age WHERE Id = @id",
    new Dictionary<string, object?> { ["@id"] = 100, ["@age"] = 26 });

int rowsUpdated = await updateTemplate.ExecuteNonQueryAsync(connection);
```

### ExecuteReader / ExecuteReaderAsync

Executes the template and returns a data reader.

```csharp
public static DbDataReader ExecuteReader(
    this SqlTemplate template,
    DbConnection connection,
    DbTransaction? transaction = null,
    IReadOnlyDictionary<string, object?>? parameterOverrides = null,
    CommandBehavior behavior = CommandBehavior.Default)

public static async ValueTask<DbDataReader> ExecuteReaderAsync(
    this SqlTemplate template,
    DbConnection connection,
    DbTransaction? transaction = null,
    IReadOnlyDictionary<string, object?>? parameterOverrides = null,
    CommandBehavior behavior = CommandBehavior.Default,
    CancellationToken cancellationToken = default)
```

**Example:**
```csharp
var template = new SqlTemplate(
    "SELECT Id, Name, Age FROM Users WHERE Age >= @minAge",
    new Dictionary<string, object?> { ["@minAge"] = 30 });

using var reader = await template.ExecuteReaderAsync(connection);

while (await reader.ReadAsync())
{
    var id = reader.GetInt32(0);
    var name = reader.GetString(1);
    var age = reader.GetInt32(2);
    
    Console.WriteLine($"{id}: {name}, Age {age}");
}
```

## Performance Optimizations

### 1. ValueTask<T> for Reduced Allocations

All async methods return `ValueTask<T>` instead of `Task<T>`, which eliminates heap allocations when operations complete synchronously.

```csharp
// Zero allocations when operation completes synchronously
var count = await template.ExecuteScalarAsync<int>(connection);
```

### 2. ConfigureAwait(false)

All async operations use `ConfigureAwait(false)` to avoid capturing the synchronization context, reducing overhead.

### 3. Optimized Dictionary Lookups

Parameter override logic uses `TryGetValue` instead of `ContainsKey` + indexer, reducing dictionary lookups by 50%.

```csharp
// Optimized: One dictionary lookup
if (parameterOverrides != null && parameterOverrides.TryGetValue(param.Key, out var overrideValue))
{
    value = overrideValue;
}

// Slower: Two dictionary lookups
if (parameterOverrides?.ContainsKey(param.Key) == true)
{
    value = parameterOverrides[param.Key];
}
```

### 4. Type Checking Before Conversion

ExecuteScalar<T> checks if the result is already the target type before attempting conversion, avoiding unnecessary boxing/unboxing.

```csharp
if (result is T typedResult)
    return typedResult;  // Fast path - no conversion needed

return (T)Convert.ChangeType(result, typeof(T));  // Slow path - conversion required
```

### 5. Method Inlining

Critical methods like `CreateCommandAsync` use `[MethodImpl(MethodImplOptions.AggressiveInlining)]` to reduce method call overhead.

## Performance Benchmarks

Real benchmark results from BenchmarkDotNet comparing SqlTemplate extensions vs manual ADO.NET code:

**Test Environment:**
- CPU: AMD Ryzen 7 5800H with Radeon Graphics (8 cores, 16 logical processors)
- Runtime: .NET 9.0.8 (X64 RyuJIT AVX2)
- OS: Windows 10 (10.0.19045.6466/22H2/2022Update)
- Database: SQLite (in-memory)

| Method | Mean | Error | StdDev | Ratio | Allocated | Alloc Ratio |
|--------|------|-------|--------|-------|-----------|-------------|
| Manual CreateCommand | 354.1 ns | 81.95 ns | 21.28 ns | 1.00 | 392 B | 1.00 |
| SqlTemplate CreateCommand | 348.4 ns | 15.89 ns | 4.13 ns | 0.99 | 424 B | 1.08 |
| SqlTemplate CreateCommand + Override | 398.4 ns | 18.85 ns | 4.89 ns | 1.13 | 664 B | 1.69 |
| Manual ExecuteScalar | 49,819.7 ns | 1,982.67 ns | 514.89 ns | 141.10 | 944 B | 2.41 |
| SqlTemplate ExecuteScalar<int> | 51,308.6 ns | 8,246.07 ns | 2,141.48 ns | 145.32 | 1000 B | 2.55 |
| SqlTemplate ExecuteScalar<int> + Override | 49,604.2 ns | 3,712.74 ns | 964.19 ns | 140.49 | 1240 B | 3.16 |
| Manual ExecuteScalar String | 4,324.8 ns | 235.78 ns | 36.49 ns | 12.25 | 952 B | 2.43 |
| SqlTemplate ExecuteScalar<string> | 4,762.6 ns | 459.83 ns | 119.42 ns | 13.49 | 984 B | 2.51 |

**Key Findings:**
- **CreateCommand**: SqlTemplate is actually **1.6% faster** than manual ADO.NET (348.4 ns vs 354.1 ns)
- **CreateCommand + Override**: Only **12.5% overhead** (398.4 ns vs 354.1 ns) for parameter replacement
- **ExecuteScalar<int>**: Minimal **3.0% overhead** (51.3 μs vs 49.8 μs) - database I/O dominates
- **ExecuteScalar<int> + Override**: Actually **0.4% faster** than manual (49.6 μs vs 49.8 μs)
- **ExecuteScalar<string>**: **10.2% overhead** (4.76 μs vs 4.32 μs) due to type conversion
- **Memory allocations**: Comparable to manual ADO.NET, parameter overrides add 31-69% memory overhead
- **Overall**: Performance is nearly identical to manual ADO.NET for database operations

## Thread Safety

SqlTemplate ADO.NET extensions are completely thread-safe:

1. **Immutable SqlTemplate**: SqlTemplate is a `readonly record struct` - all fields are immutable
2. **Stateless Extensions**: Extension methods are pure functions with no shared state
3. **Read-Only Parameters**: Parameter overrides use `IReadOnlyDictionary<string, object?>`

**Example - Safe Concurrent Usage:**
```csharp
var template = new SqlTemplate(
    "SELECT * FROM Users WHERE Id = @id",
    new Dictionary<string, object?> { ["@id"] = 0 });

// Safe to share template across multiple threads
var tasks = Enumerable.Range(1, 100).Select(async id =>
{
    var overrides = new Dictionary<string, object?> { ["@id"] = id };
    
    // Each thread uses its own connection
    using var connection = new SqliteConnection(connectionString);
    using var reader = await template.ExecuteReaderAsync(
        connection, 
        parameterOverrides: overrides);
    
    // Process results...
});

await Task.WhenAll(tasks);
```

## Transaction Support

All extension methods support transactions through an optional `DbTransaction` parameter:

```csharp
using var transaction = connection.BeginTransaction();

try
{
    // Execute multiple operations in a transaction
    var insertTemplate = new SqlTemplate(
        "INSERT INTO Users (Id, Name, Age) VALUES (@id, @name, @age)",
        new Dictionary<string, object?> { ["@id"] = 1, ["@name"] = "Alice", ["@age"] = 25 });
    
    await insertTemplate.ExecuteNonQueryAsync(connection, transaction);
    
    var updateTemplate = new SqlTemplate(
        "UPDATE Users SET Age = @age WHERE Id = @id",
        new Dictionary<string, object?> { ["@id"] = 1, ["@age"] = 26 });
    
    await updateTemplate.ExecuteNonQueryAsync(connection, transaction);
    
    // Commit transaction
    transaction.Commit();
}
catch (Exception)
{
    transaction.Rollback();
    throw;
}
```

## Best Practices

### 1. Reuse Templates

Create templates once and reuse them with parameter overrides:

```csharp
// Create template once
var template = new SqlTemplate(
    "INSERT INTO Users (Id, Name, Age) VALUES (@id, @name, @age)",
    new Dictionary<string, object?> { ["@id"] = 0, ["@name"] = "", ["@age"] = 0 });

// Reuse with different parameters
foreach (var user in users)
{
    var overrides = new Dictionary<string, object?>
    {
        ["@id"] = user.Id,
        ["@name"] = user.Name,
        ["@age"] = user.Age
    };
    
    await template.ExecuteNonQueryAsync(connection, parameterOverrides: overrides);
}
```

### 2. Use Generic ExecuteScalar When Possible

The generic version provides type safety and automatic conversion:

```csharp
// Preferred - type-safe
int count = await template.ExecuteScalarAsync<int>(connection);

// Avoid - requires manual casting
object? result = await template.ExecuteScalarAsync(connection);
int count = Convert.ToInt32(result);
```

### 3. Dispose Commands Properly

CreateCommand returns a DbCommand that must be disposed:

```csharp
// Good - using statement
using var cmd = template.CreateCommand(connection);

// Also good - explicit disposal
var cmd = template.CreateCommand(connection);
try
{
    // Use command
}
finally
{
    cmd.Dispose();
}
```

### 4. Use Async Methods for I/O Operations

Prefer async methods for database operations to avoid blocking threads:

```csharp
// Preferred - async
var count = await template.ExecuteScalarAsync<int>(connection);

// Avoid - blocks thread
var count = template.ExecuteScalar<int>(connection);
```

## Error Handling

All extension methods provide clear, debugger-friendly error messages:

```csharp
try
{
    var template = new SqlTemplate("SELECT * FROM Users", new Dictionary<string, object?>());
    using var reader = await template.ExecuteReaderAsync(null!); // Null connection
}
catch (ArgumentNullException ex)
{
    // Clear error message with parameter name
    Console.WriteLine($"Error: {ex.ParamName} - {ex.Message}");
    // Output: Error: connection - Database connection cannot be null
}
```

## Compatibility

- **Target Frameworks**: netstandard2.0, net8.0, net9.0
- **Dependencies**: 
  - System.Threading.Tasks.Extensions (for netstandard2.0 ValueTask support)
  - System.Data.Common (ADO.NET abstractions)

## See Also

- [SQL Template Return Type Documentation](SQL_TEMPLATE_RETURN_TYPE.md)
- [API Reference](API_REFERENCE.md)
- [Best Practices](BEST_PRACTICES.md)
