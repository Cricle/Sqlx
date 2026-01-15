# Source Generators

Sqlx uses Roslyn source generators to produce high-performance, AOT-compatible code at compile time.

## AOT Compatibility

**✅ Native AOT Ready:**
- Zero reflection at runtime
- All code generated at compile time
- Expression tree compilation (allowed in AOT)
- Static method caching for IDataRecord operations
- 974 unit tests passing with AOT enabled

## Generated Components

### EntityProvider

Generated for classes marked with `[SqlxEntity]`. Provides column metadata without reflection.

**Auto-Registration:** If the entity class is marked as `partial`, a `ModuleInitializer` automatically registers the EntityProvider and ResultReader with `SqlQuery<T>` at startup.

```csharp
[SqlxEntity]
public partial class User  // 'partial' enables auto-registration
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

// Generated: ModuleInitializer (for partial classes)
internal static class UserModuleInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        SqlQuery<User>.EntityProvider = UserEntityProvider.Default;
        SqlQuery<User>.ResultReader = UserResultReader.Default;
    }
}
```

### ResultReader

Generated for classes marked with `[SqlxEntity]`. Reads entities from DbDataReader without reflection.

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

Generated for classes marked with `[SqlxParameter]`. Binds entity properties to DbCommand parameters.

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

[SqlxEntity]
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
| SQLX001 | Info | Entity should have [SqlxEntity] or [SqlxParameter] |
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
