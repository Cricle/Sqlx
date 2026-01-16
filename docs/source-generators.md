# Source Generators

Sqlx uses Roslyn source generators to produce high-performance, AOT-compatible code at compile time.

## AOT Compatibility

**✅ Native AOT Ready:**
- Zero reflection at runtime
- All code generated at compile time
- Expression tree compilation (allowed in AOT)
- Static method caching for IDataRecord operations
- 1344 unit tests passing with AOT enabled

## Auto-Discovery

The source generator automatically discovers entity types from multiple sources:

### 1. [Sqlx] Attribute

Classes explicitly marked with `[Sqlx]` attribute:

```csharp
[Sqlx]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```

### 2. SqlQuery\<T\> Usage

Entity types used as generic arguments in `SqlQuery<T>`:

```csharp
// Product will be auto-discovered and have providers generated
var query = SqlQuery<Product>.ForSqlite()
    .Where(p => p.Price > 100);
```

### 3. [SqlTemplate] Method Types

Entity types from method return types and parameters:

```csharp
public interface IOrderRepository
{
    // Order discovered from Task<Order?> return type
    [SqlTemplate("SELECT * FROM orders WHERE id = @id")]
    Task<Order?> GetByIdAsync(int id);
    
    // OrderItem discovered from Task<List<OrderItem>> return type
    [SqlTemplate("SELECT * FROM order_items WHERE order_id = @orderId")]
    Task<List<OrderItem>> GetItemsAsync(int orderId);
    
    // FilterCriteria discovered from parameter type
    [SqlTemplate("INSERT INTO filters VALUES (@criteria)")]
    Task InsertFilterAsync(FilterCriteria criteria);
}
```

**Supported Return Type Patterns:**
- `Task<T>` → discovers T
- `Task<T?>` → discovers T
- `Task<List<T>>` → discovers T
- `Task<IEnumerable<T>>` → discovers T
- `ValueTask<T>` → discovers T
- Direct types (non-async)

### Deduplication

The generator automatically deduplicates types discovered from multiple sources. Each type is only generated once.

### Nested Class Naming

For nested classes, generated providers use `OuterClass_InnerClass` naming to avoid conflicts:

```csharp
public class Outer
{
    [Sqlx]
    public class Inner { public int Id { get; set; } }
}

// Generated: Outer_InnerEntityProvider, Outer_InnerResultReader, etc.

## Generated Components

### EntityProvider

Generated for classes marked with `[Sqlx]`. Provides column metadata without reflection.

```csharp
[Sqlx]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
}

// Generated: UserEntityProvider
public sealed class UserEntityProvider : IEntityProvider
{
    public static UserEntityProvider Default { get; } = new();
    public Type EntityType => typeof(User);
    public IReadOnlyList<ColumnMeta> Columns => _columns;
    
    private static readonly IReadOnlyList<ColumnMeta> _columns = new ColumnMeta[]
    {
        new ColumnMeta("id", "Id", DbType.Int32, false),
        new ColumnMeta("name", "Name", DbType.String, false),
    };
}

// Generated: SqlxInitializer (auto-registers all providers)
internal static class SqlxInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        SqlQuery<User>.EntityProvider = UserEntityProvider.Default;
        SqlQuery<User>.ResultReader = UserResultReader.Default;
        SqlQuery<User>.ParameterBinder = UserParameterBinder.Default;
    }
}
```

### ResultReader

Generated for classes marked with `[Sqlx]`. Reads entities from DbDataReader without reflection.

**Performance:** Uses ordinal-based access and static method caching for optimal performance.

```csharp
// Generated: UserResultReader
public sealed class UserResultReader : IResultReader<User>
{
    public static UserResultReader Default { get; } = new();
    
    // Static method cache (initialized once)
    private static readonly Func<IDataRecord, int, int> _getInt32 = 
        (Func<IDataRecord, int, int>)Delegate.CreateDelegate(
            typeof(Func<IDataRecord, int, int>), 
            typeof(IDataRecord).GetMethod("GetInt32")!);
    
    public IEnumerable<User> Read(DbDataReader reader)
    {
        var ord0 = reader.GetOrdinal("id");
        var ord1 = reader.GetOrdinal("name");
        
        while (reader.Read())
        {
            yield return new User
            {
                Id = _getInt32(reader, ord0),  // Uses cached method
                Name = reader.GetString(ord1),
            };
        }
    }
    
    // Also generates ReadAsync for .NET 8+
}
```

### ParameterBinder

Generated for classes marked with `[Sqlx]`. Binds entity properties to DbCommand parameters.

```csharp
// Generated: UserParameterBinder
public sealed class UserParameterBinder : IParameterBinder<User>
{
    public static UserParameterBinder Default { get; } = new();
    
    public void BindEntity(DbCommand command, User entity, string parameterPrefix = "@")
    {
        {
            var p = command.CreateParameter();
            p.ParameterName = parameterPrefix + "id";
            p.Value = entity.Id;
            command.Parameters.Add(p);
        }
        {
            var p = command.CreateParameter();
            p.ParameterName = parameterPrefix + "name";
            p.Value = entity.Name ?? (object)DBNull.Value;
            command.Parameters.Add(p);
        }
    }
}
```

### Repository Implementation

Generated for classes marked with `[RepositoryFor]`. Implements all interface methods.

```csharp
[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("users")]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository
{
    private readonly DbConnection _connection;
    public DbTransaction? Transaction { get; set; }
}

// Generated implementation includes:
// - Static PlaceholderContext
// - Static SqlTemplate fields for each method
// - Full method implementations with parameter binding
// - Activity tracking (optional)
// - Interceptor hooks (optional)
```

## Column Name Mapping

By default, property names are converted to snake_case:

| Property | Column |
|----------|--------|
| `Id` | `id` |
| `UserName` | `user_name` |
| `CreatedAt` | `created_at` |

Use `[Column]` attribute to customize:

```csharp
[Column("custom_column_name")]
public string PropertyName { get; set; }
```

## Excluding Properties

Use `[IgnoreDataMember]` to exclude properties from generation:

```csharp
using System.Runtime.Serialization;

[Sqlx]
public class User
{
    public int Id { get; set; }
    
    [IgnoreDataMember]
    public string ComputedProperty => $"User-{Id}";
}
```

## Analyzer Diagnostics

Sqlx includes analyzers to help catch issues at compile time:

| Code | Severity | Description |
|------|----------|-------------|
| SQLX001 | Info | Entity should have [Sqlx] attribute |
| SQLX002 | Error | Unknown placeholder in SqlTemplate |
| SQLX003 | Info | Consider adding [Column] attribute |

## Interceptors

Generated repositories support optional interceptors for logging, metrics, etc.:

```csharp
public partial class UserRepository
{
    partial void OnExecuting(string operationName, DbCommand command, SqlTemplate template)
    {
        Console.WriteLine($"Executing: {operationName}");
        Console.WriteLine($"SQL: {command.CommandText}");
    }
    
    partial void OnExecuted(string operationName, DbCommand command, SqlTemplate template, 
        object? result, long elapsedTicks)
    {
        var ms = elapsedTicks * 1000.0 / Stopwatch.Frequency;
        Console.WriteLine($"Completed: {operationName} in {ms:F2}ms");
    }
    
    partial void OnExecuteFail(string operationName, DbCommand command, SqlTemplate template,
        Exception exception, long elapsedTicks)
    {
        Console.WriteLine($"Failed: {operationName} - {exception.Message}");
    }
}
```

Disable interceptors with `#define SQLX_DISABLE_INTERCEPTOR`.

## Activity Tracking

Generated code includes OpenTelemetry-compatible activity tracking:

```csharp
// Activities include tags:
// - db.system: Database type
// - db.operation: "sqlx.execute"
// - db.statement: Final SQL
// - db.rows_affected: Result count
// - db.duration_ms: Execution time
```

Disable with `#define SQLX_DISABLE_ACTIVITY`.

## Build Configuration

### Viewing Generated Code

In your `.csproj`:

```xml
<PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

Generated files will be in `obj/Generated/`.

### Troubleshooting

1. **Generator not running**: Ensure `Sqlx` package is referenced correctly
2. **Missing generated code**: Check for analyzer errors in build output
3. **Wrong column names**: Verify `[Column]` attributes are correct
4. **Type mismatches**: Ensure property types match database column types
