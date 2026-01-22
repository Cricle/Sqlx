# API Reference

## Core Classes

### SqlQuery\<T\>

Generic static class for building type-safe SQL queries with cached entity metadata.

```csharp
public static class SqlQuery<T>
{
    // Properties
    public static IEntityProvider? EntityProvider { get; set; }  // Cached entity provider
    public static IResultReader<T>? ResultReader { get; set; }   // Cached result reader
    
    // Factory Methods
    public static IQueryable<T> For(SqlDialect dialect, IEntityProvider? entityProvider = null);
    public static IQueryable<T> ForSqlite(IEntityProvider? entityProvider = null);
    public static IQueryable<T> ForSqlServer(IEntityProvider? entityProvider = null);
    public static IQueryable<T> ForMySql(IEntityProvider? entityProvider = null);
    public static IQueryable<T> ForPostgreSQL(IEntityProvider? entityProvider = null);
    public static IQueryable<T> ForOracle(IEntityProvider? entityProvider = null);
    public static IQueryable<T> ForDB2(IEntityProvider? entityProvider = null);
    
    // Internal Methods
    internal static ColumnMeta? GetColumnByProperty(string propertyName);
}
```

**Example:**
```csharp
// Auto-registration (for partial classes with [SqlxEntity])
var query = SqlQuery<User>.ForSqlite()
    .Where(u => u.Age > 18)
    .OrderBy(u => u.Name);

// Manual registration
SqlQuery<User>.EntityProvider = UserEntityProvider.Default;
SqlQuery<User>.ResultReader = UserResultReader.Default;
var query = SqlQuery<User>.ForSqlServer();

// Aggregate functions use cached ColumnMeta for correct column names
var maxAge = await SqlQuery<User>.ForSqlite()
    .WithConnection(connection)
    .WithReader(UserResultReader.Default)
    .MaxAsync(u => u.Age);  // Uses column name from ColumnMeta

// Select projection with anonymous types (uses DynamicResultReader)
var results = await SqlQuery<User>.ForSqlite()
    .Where(u => u.Age >= 18)
    .Select(u => new { u.Id, u.Name, u.Email })
    .WithConnection(connection)
    .ToListAsync();  // Returns List<dynamic>, fully AOT compatible
```

### SqlTemplate

Represents a prepared SQL template with efficient rendering.

```csharp
public sealed class SqlTemplate
{
    // Properties
    public string Sql { get; }                    // Prepared SQL (static placeholders resolved)
    public bool HasDynamicPlaceholders { get; }   // Whether template has dynamic placeholders
    
    // Methods
    public static SqlTemplate Prepare(string template, PlaceholderContext context);
    public string Render(IReadOnlyDictionary<string, object?>? dynamicParameters);
}
```

**Example:**
```csharp
var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserEntityProvider.Default.Columns);
var template = SqlTemplate.Prepare(
    "SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}", 
    context);

var sql = template.Render(new Dictionary<string, object?> 
{ 
    ["predicate"] = "age > 18" 
});
```

### PlaceholderContext

Provides context for placeholder processing.

```csharp
public sealed class PlaceholderContext
{
    public PlaceholderContext(SqlDialect dialect, string tableName, IReadOnlyList<ColumnMeta> columns);
    
    public SqlDialect Dialect { get; }
    public string TableName { get; }
    public IReadOnlyList<ColumnMeta> Columns { get; }
}
```

### PlaceholderProcessor

Manages placeholder handlers.

```csharp
public static class PlaceholderProcessor
{
    public static void RegisterHandler(IPlaceholderHandler handler);
    public static void RegisterBlockClosingTag(string closingTag);
    public static bool TryGetHandler(string name, out IPlaceholderHandler handler);
    public static IReadOnlyList<string> ExtractParameters(string sql);
}
```

### ColumnMeta

Column metadata record.

```csharp
public record ColumnMeta(
    string Name,           // SQL column name
    string PropertyName,   // C# property name
    DbType DbType,         // ADO.NET database type
    bool IsNullable        // Whether column allows NULL
);
```

## Interfaces

### IPlaceholderHandler

Contract for placeholder handlers.

```csharp
public interface IPlaceholderHandler
{
    string Name { get; }
    PlaceholderType GetType(string options);
    string Process(PlaceholderContext context, string options);
    string Render(PlaceholderContext context, string options, IReadOnlyDictionary<string, object?>? parameters);
}
```

### IBlockPlaceholderHandler

Contract for block-level placeholder handlers that process content between opening and closing tags.

```csharp
public interface IBlockPlaceholderHandler : IPlaceholderHandler
{
    /// <summary>
    /// Gets the closing tag name (e.g., "/if" for "if" blocks).
    /// </summary>
    string ClosingTagName { get; }
    
    /// <summary>
    /// Processes the block content and returns the rendered result.
    /// </summary>
    /// <returns>
    /// The rendered content. Return empty string to exclude the block,
    /// or the processed content (possibly repeated multiple times for loops).
    /// </returns>
    string ProcessBlock(string options, string blockContent, IReadOnlyDictionary<string, object?>? parameters);
}
```

Block handlers can implement various behaviors:
- **Conditional blocks** (e.g., `{{if}}`): Return `blockContent` if condition is true, empty string otherwise
- **Loop blocks** (e.g., `{{foreach}}`): Render `blockContent` multiple times with different contexts
- **Transform blocks**: Modify and return transformed `blockContent`

**Built-in Implementation:**

`IfPlaceholderHandler` - Handles `{{if}}...{{/if}}` conditional blocks with conditions:
- `notnull=param` - Include when parameter is not null
- `null=param` - Include when parameter is null
- `notempty=param` - Include when parameter is not null and not empty
- `empty=param` - Include when parameter is null or empty

### IEntityProvider

Provides entity metadata.

```csharp
public interface IEntityProvider
{
    Type EntityType { get; }
    IReadOnlyList<ColumnMeta> Columns { get; }
}
```

### IResultReader\<TEntity\>

Reads entities from DbDataReader.

```csharp
public interface IResultReader<TEntity>
{
    IEnumerable<TEntity> Read(DbDataReader reader);
    
    // .NET 8+ only
    IAsyncEnumerable<TEntity> ReadAsync(DbDataReader reader, CancellationToken ct = default);
}
```

### DynamicResultReader\<T\>

High-performance result reader for anonymous types and dynamic projections. Automatically used by `Select()` queries.

```csharp
public sealed class DynamicResultReader<T> : IResultReader<T>
{
    public static DynamicResultReader<T> Create();
    public IEnumerable<T> Read(DbDataReader reader);
    public IAsyncEnumerable<T> ReadAsync(DbDataReader reader, CancellationToken ct = default);
}
```

**Features:**
- Fully AOT compatible (uses expression tree compilation)
- Static method caching (GetInt32, GetString, IsDBNull, etc.)
- Supports all basic types and nullable types
- Automatic type conversion
- Zero reflection at runtime

**Supported Types:**
- Primitives: Int32, Int64, Int16, Byte, Boolean, String, Decimal, Double, Float, Guid
- DateTime and DateTimeOffset
- Nullable versions of all above types
- Anonymous types (compiler-generated)

**Performance Optimizations:**
- Static constructor caches all IDataRecord methods once
- Expression tree compiled to native code
- Ordinal-based column access
- No boxing for value types

**Example:**
```csharp
// Automatically used for Select projections
var results = await SqlQuery<User>.ForSqlite()
    .Select(u => new { u.Id, u.Name, u.Email })
    .WithConnection(connection)
    .ToListAsync();  // Uses DynamicResultReader<dynamic>

// Manual usage (rarely needed)
var reader = DynamicResultReader<dynamic>.Create();
var query = SqlQuery<User>.ForSqlite()
    .Select(u => new { u.Id, u.Name })
    .WithConnection(connection)
    .WithReader(reader);

// Works with strongly-typed projections too
var typedReader = DynamicResultReader<UserDto>.Create();
var dtos = await SqlQuery<User>.ForSqlite()
    .Select(u => new UserDto { Id = u.Id, Name = u.Name })
    .WithConnection(connection)
    .WithReader(typedReader)
    .ToListAsync();
```

**AOT Compatibility:**
- ✅ Expression tree compilation is allowed in AOT
- ✅ No runtime reflection or dynamic code generation
- ✅ All type information resolved at compile time
- ✅ Tested with 51 comprehensive unit tests

### IParameterBinder\<TEntity\>

Binds entity parameters to DbCommand.

```csharp
public interface IParameterBinder<TEntity>
{
    void BindEntity(DbCommand command, TEntity entity, string parameterPrefix = "@");
    
#if NET6_0_OR_GREATER
    void BindEntity(DbBatchCommand command, TEntity entity, Func<DbParameter> parameterFactory, string parameterPrefix = "@");
#endif
}
```

### ICrudRepository\<TEntity, TKey\>

Standard CRUD repository interface combining query and command operations.

```csharp
public interface ICrudRepository<TEntity, TKey> : IQueryRepository<TEntity, TKey>, ICommandRepository<TEntity, TKey>
    where TEntity : class
{
}
```

**Total Methods**: 42 (24 query + 18 command)

### IQueryRepository\<TEntity, TKey\>

Query operations (24 methods).

```csharp
public interface IQueryRepository<TEntity, TKey> where TEntity : class
{
    // Single Entity Queries (4 methods)
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);
    TEntity? GetById(TKey id);
    Task<TEntity?> GetFirstWhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    TEntity? GetFirstWhere(Expression<Func<TEntity, bool>> predicate);
    
    // List Queries (6 methods)
    Task<List<TEntity>> GetByIdsAsync(List<TKey> ids, CancellationToken ct = default);
    List<TEntity> GetByIds(List<TKey> ids);
    Task<List<TEntity>> GetAllAsync(int limit = 1000, CancellationToken ct = default);
    List<TEntity> GetAll(int limit = 1000);
    Task<List<TEntity>> GetWhereAsync(Expression<Func<TEntity, bool>> predicate, int limit = 1000, CancellationToken ct = default);
    List<TEntity> GetWhere(Expression<Func<TEntity, bool>> predicate, int limit = 1000);
    
    // Pagination (4 methods)
    Task<List<TEntity>> GetPagedAsync(int pageSize = 20, int offset = 0, CancellationToken ct = default);
    List<TEntity> GetPaged(int pageSize = 20, int offset = 0);
    Task<List<TEntity>> GetPagedWhereAsync(Expression<Func<TEntity, bool>> predicate, int pageSize = 20, int offset = 0, CancellationToken ct = default);
    List<TEntity> GetPagedWhere(Expression<Func<TEntity, bool>> predicate, int pageSize = 20, int offset = 0);
    
    // Existence & Count (10 methods)
    Task<bool> ExistsByIdAsync(TKey id, CancellationToken ct = default);
    bool ExistsById(TKey id);
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    bool Exists(Expression<Func<TEntity, bool>> predicate);
    Task<long> CountAsync(CancellationToken ct = default);
    long Count();
    Task<long> CountWhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    long CountWhere(Expression<Func<TEntity, bool>> predicate);
}
```

### ICommandRepository\<TEntity, TKey\>

Command operations (18 methods).

```csharp
public interface ICommandRepository<TEntity, TKey> where TEntity : class
{
    // Insert Operations (6 methods)
    Task<TKey> InsertAndGetIdAsync(TEntity entity, CancellationToken ct = default);
    TKey InsertAndGetId(TEntity entity);
    Task<int> InsertAsync(TEntity entity, CancellationToken ct = default);
    int Insert(TEntity entity);
    Task<int> BatchInsertAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);
    int BatchInsert(IEnumerable<TEntity> entities);
    
    // Update Operations (6 methods)
    Task<int> UpdateAsync(TEntity entity, CancellationToken ct = default);
    int Update(TEntity entity);
    Task<int> UpdateWhereAsync(TEntity entity, Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    int UpdateWhere(TEntity entity, Expression<Func<TEntity, bool>> predicate);
    Task<int> BatchUpdateAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);
    int BatchUpdate(IEnumerable<TEntity> entities);
    
    // Delete Operations (6 methods)
    Task<int> DeleteAsync(TKey id, CancellationToken ct = default);
    int Delete(TKey id);
    Task<int> DeleteByIdsAsync(List<TKey> ids, CancellationToken ct = default);
    int DeleteByIds(List<TKey> ids);
    Task<int> DeleteWhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    int DeleteWhere(Expression<Func<TEntity, bool>> predicate);
    Task<int> DeleteAllAsync(CancellationToken ct = default);
    int DeleteAll();
}
```

## Attributes

### [Sqlx]

Marks a class for EntityProvider, ResultReader, and ParameterBinder generation.

```csharp
[Sqlx]
public class User { }

// Control what gets generated
[Sqlx(GenerateEntityProvider = true, GenerateResultReader = true, GenerateParameterBinder = false)]
public class User { }

// Generate for external/sealed types
[Sqlx(typeof(ExternalEntity))]
public class MyClass { }
```

### [SqlDefine]

Specifies the SQL dialect.

```csharp
[SqlDefine(SqlDefineTypes.SQLite)]
public partial class UserRepository { }
```

### [TableName]

Specifies the table name.

```csharp
[TableName("users")]
public partial class UserRepository { }
```

### [RepositoryFor]

Marks a class as repository implementation.

```csharp
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository { }
```

### [SqlTemplate]

Defines SQL template for a method.

```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
Task<User?> GetByIdAsync(int id, CancellationToken ct = default);
```

### [ReturnInsertedId]

Marks Insert method to return the generated ID.

```csharp
[SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
[ReturnInsertedId]
Task<int> InsertAndGetIdAsync(User entity, CancellationToken ct = default);
```

### [ExpressionToSql]

Marks a parameter as expression to be converted to SQL.

```csharp
Task<List<User>> GetWhereAsync([ExpressionToSql] Expression<Func<User, bool>> predicate);
```

## Dialects

### SqlDefine

Static dialect instances.

```csharp
public static class SqlDefine
{
    public static readonly SqlDialect SQLite;
    public static readonly SqlDialect MySql;
    public static readonly SqlDialect PostgreSql;
    public static readonly SqlDialect SqlServer;
    public static readonly SqlDialect Oracle;
    public static readonly SqlDialect DB2;
    
    public static SqlDialect GetDialect(SqlDefineTypes type);
}
```

### SqlDialect

Base class for dialect implementations.

```csharp
public abstract class SqlDialect
{
    // Identification
    public abstract string DatabaseType { get; }
    public abstract SqlDefineTypes DbType { get; }
    
    // Quoting
    public abstract string ColumnLeft { get; }
    public abstract string ColumnRight { get; }
    public abstract string ParameterPrefix { get; }
    public virtual string WrapColumn(string name);
    public virtual string CreateParameter(string name);
    
    // Functions
    public abstract string Concat(params string[] parts);
    public virtual string Upper(string expression);
    public virtual string Lower(string expression);
    public virtual string Length(string expression);
    
    // Date/Time
    public abstract string CurrentTimestamp { get; }
    public abstract string CurrentDate { get; }
    public abstract string CurrentTime { get; }
    public abstract string DatePart(string part, string expression);
    public abstract string DateAdd(string interval, string number, string expression);
    public abstract string DateDiff(string interval, string startDate, string endDate);
    
    // Pagination
    public virtual string Limit(string count);
    public virtual string Offset(string count);
    public virtual string Paginate(string limit, string offset);
    
    // Null handling
    public virtual string IfNull(string expression, string defaultValue);
    public virtual string Coalesce(params string[] expressions);
    
    // Last inserted ID
    public abstract string LastInsertedId { get; }
}
```

## Enums

### SqlDefineTypes

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

### PlaceholderType

```csharp
public enum PlaceholderType
{
    Static,   // Resolved at prepare time
    Dynamic   // Resolved at render time
}
```

## Batch Execution

### DbBatchExecutor

Batch executor for multiple entities. Uses `DbBatch` on .NET 6+ with fallback to loop execution on older frameworks.

```csharp
public static class DbBatchExecutor
{
    public const int DefaultBatchSize = 1000;
    
    public static Task<int> ExecuteAsync<TEntity>(
        DbConnection connection,
        DbTransaction? transaction,
        string sql,
        List<TEntity> entities,
        IParameterBinder<TEntity> binder,
        string parameterPrefix = "@",
        int batchSize = DefaultBatchSize,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default);
}
```

**Parameters:**
- `connection` - The database connection
- `transaction` - Optional transaction
- `sql` - The SQL command text (INSERT, UPDATE, DELETE)
- `entities` - List of entities to process
- `binder` - The parameter binder (generated by `[SqlxParameter]`)
- `parameterPrefix` - Parameter prefix (default: "@")
- `batchSize` - Max commands per batch (default: 1000)
- `commandTimeout` - Command timeout in seconds (null = use default)
- `cancellationToken` - Cancellation token

**Returns:** Total rows affected.

**Example:**
```csharp
var users = new List<User>
{
    new() { Name = "Alice", Email = "alice@test.com" },
    new() { Name = "Bob", Email = "bob@test.com" }
};

var sql = "INSERT INTO users (name, email) VALUES (@name, @email)";
var affected = await DbBatchExecutor.ExecuteAsync(
    connection, 
    transaction: null, 
    sql, 
    users, 
    UserParameterBinder.Default,
    batchSize: 100);
```

### DbConnectionExtensions

Extension methods for convenient batch execution.

```csharp
public static class DbConnectionExtensions
{
    public static Task<int> ExecuteBatchAsync<TEntity>(
        this DbConnection connection,
        string sql,
        List<TEntity> entities,
        IParameterBinder<TEntity> binder,
        DbTransaction? transaction = null,
        string parameterPrefix = "@",
        int batchSize = DbBatchExecutor.DefaultBatchSize,
        int? commandTimeout = null,
        CancellationToken cancellationToken = default);
}
```

**Example:**
```csharp
// Using extension method
var affected = await connection.ExecuteBatchAsync(
    "INSERT INTO users (name, email) VALUES (@name, @email)",
    users,
    UserParameterBinder.Default,
    batchSize: 100,
    commandTimeout: 60);
```


