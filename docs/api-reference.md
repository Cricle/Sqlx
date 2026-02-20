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
    public IReadOnlyDictionary<string, object?> Parameters { get; }  // Input parameters
    public IReadOnlyDictionary<string, DbType> OutputParameters { get; }  // Output parameters
    
    // Methods
    public static SqlTemplate Prepare(string template, PlaceholderContext context);
    public string Render(IReadOnlyDictionary<string, object?>? dynamicParameters);
    
    // Output Parameter Support
    public SqlTemplate AddOutputParameter(string name, DbType dbType);
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

// Adding output parameters
var builder = new SqlBuilder();
builder.Append($"INSERT INTO users (name) VALUES (@name)");
builder.AddParameter("name", "John");
var template = builder.Build();
template.AddOutputParameter("userId", DbType.Int32);

// Output parameters are stored in OutputParameters dictionary
Console.WriteLine($"Output parameters: {template.OutputParameters.Count}");
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

**Total Methods**: 46 (24 query + 22 command)

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

Command operations (22 methods).

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
    
    // Update Operations (10 methods)
    Task<int> UpdateAsync(TEntity entity, CancellationToken ct = default);
    int Update(TEntity entity);
    Task<int> UpdateWhereAsync(TEntity entity, Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    int UpdateWhere(TEntity entity, Expression<Func<TEntity, bool>> predicate);
    Task<int> BatchUpdateAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);
    int BatchUpdate(IEnumerable<TEntity> entities);
    
    // Dynamic Update Operations (4 methods) - NEW
    Task<int> DynamicUpdateAsync(TKey id, Expression<Func<TEntity, TEntity>> updateExpression, CancellationToken ct = default);
    int DynamicUpdate(TKey id, Expression<Func<TEntity, TEntity>> updateExpression);
    Task<int> DynamicUpdateWhereAsync(Expression<Func<TEntity, TEntity>> updateExpression, Expression<Func<TEntity, bool>> whereExpression, CancellationToken ct = default);
    int DynamicUpdateWhere(Expression<Func<TEntity, TEntity>> updateExpression, Expression<Func<TEntity, bool>> whereExpression);
    
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

### [OutputParameter]

Marks a method parameter as an output parameter for SQL execution.

**⚠️ Important**: C# does not allow `ref` and `out` parameters in async methods. Output parameters must be used with synchronous methods only.

```csharp
using System.Data;
using Sqlx.Annotations;

public interface IUserRepository
{
    // Out parameter (Output mode) - receives value from database
    [SqlTemplate("INSERT INTO {{table}} (name, age) VALUES (@name, @age); SELECT last_insert_rowid()")]
    int InsertAndGetId(
        string name, 
        int age, 
        [OutputParameter(DbType.Int32)] out int id);
    
    // Ref parameter (InputOutput mode) - sends and receives value
    [SqlTemplate("UPDATE counters SET value = value + 1 WHERE name = @name; SELECT value FROM counters WHERE name = @name")]
    int IncrementCounter(
        string name,
        [OutputParameter(DbType.Int32)] ref int currentValue);
    
    // Multiple output parameters
    [SqlTemplate("INSERT INTO orders (total, created_at) VALUES (@total, datetime('now')); SELECT last_insert_rowid(), datetime('now')")]
    int CreateOrder(
        decimal total,
        [OutputParameter(DbType.Int32)] out int orderId,
        [OutputParameter(DbType.String, Size = 50)] out string createdAt);
}
```

**Properties:**
- `DbType` (required) - The database type of the output parameter
- `Size` (optional) - The size for variable-length types (strings, binary data)

**Parameter Modes:**
- `out` parameter → `ParameterDirection.Output` - Only receives value from database
- `ref` parameter → `ParameterDirection.InputOutput` - Sends initial value and receives updated value

**Features:**
- ✅ Type-safe - Compile-time validation
- ✅ Dual mode - Supports both out and ref parameters
- ✅ Multiple parameters - Can have multiple output parameters in one method
- ✅ AOT compatible - Fully supports Native AOT
- ⚠️ Synchronous only - C# language limitation prevents use in async methods

**Example Usage:**
```csharp
// Out parameter
var result = repo.InsertAndGetId("John", 25, out int newId);
Console.WriteLine($"Inserted ID: {newId}");

// Ref parameter
int counter = 100;
repo.IncrementCounter("page_views", ref counter);
Console.WriteLine($"New value: {counter}"); // 101

// Multiple output parameters
repo.CreateOrder(99.99m, out int orderId, out string timestamp);
Console.WriteLine($"Order {orderId} created at {timestamp}");
```

### [ResultSetMapping]

Specifies the mapping between result sets and tuple elements in method return values. Use this attribute to explicitly define which result set corresponds to which tuple element when returning multiple scalar values.

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class ResultSetMappingAttribute : Attribute
{
    public ResultSetMappingAttribute(int index, string name);
    
    public int Index { get; }    // Zero-based index of the result set
    public string Name { get; }  // Name of the tuple element
}
```

**Parameters:**
- `index` - Zero-based index of the result set
  - Index 0: Affected row count (INSERT/UPDATE/DELETE)
  - Index 1+: SELECT statement results in order
- `name` - Name of the tuple element (must match exactly)

**Example:**

```csharp
public interface IUserRepository
{
    // Explicit mapping with ResultSetMapping
    [SqlTemplate(@"
        INSERT INTO users (name) VALUES (@name);
        SELECT last_insert_rowid();
        SELECT COUNT(*) FROM users
    ")]
    [ResultSetMapping(0, "rowsAffected")]
    [ResultSetMapping(1, "userId")]
    [ResultSetMapping(2, "totalUsers")]
    Task<(int rowsAffected, long userId, int totalUsers)> InsertAndGetStatsAsync(string name);
    
    // Default mapping (no ResultSetMapping attributes)
    // First element = affected rows, remaining = SELECT results in order
    [SqlTemplate(@"
        INSERT INTO users (name) VALUES (@name);
        SELECT last_insert_rowid()
    ")]
    Task<(int rowsAffected, long userId)> InsertAndGetIdAsync(string name);
    
    // Multiple SELECT statements
    [SqlTemplate(@"
        SELECT COUNT(*) FROM users;
        SELECT MAX(id) FROM users;
        SELECT MIN(id) FROM users
    ")]
    [ResultSetMapping(0, "totalUsers")]
    [ResultSetMapping(1, "maxId")]
    [ResultSetMapping(2, "minId")]
    Task<(int totalUsers, long maxId, long minId)> GetStatsAsync();
}
```

**Usage:**

```csharp
// With explicit mapping
var (rows, userId, total) = await repo.InsertAndGetStatsAsync("Alice");
Console.WriteLine($"Inserted user {userId}, total users: {total}");

// With default mapping
var (rows2, userId2) = await repo.InsertAndGetIdAsync("Bob");

// Multiple SELECTs
var (total, maxId, minId) = await repo.GetStatsAsync();
```

**Features:**
- ✅ Single database round-trip - All values retrieved in one call
- ✅ Type-safe - Compile-time type checking
- ✅ Zero reflection - All code generated at compile-time
- ✅ AOT compatible - Fully supports Native AOT
- ✅ Automatic conversion - Handles type conversion automatically
- ✅ Sync and async - Both synchronous and asynchronous methods supported

**Limitations:**
- Maximum 8 tuple elements (ValueTuple limitation)
- Each result set should return a single scalar value (first row, first column)
- Result sets must be in the same order as SQL statements
- Nested tuples are not supported

**See also:** [Multiple Result Sets Documentation](multiple-result-sets.md)

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

## ExpressionBlockResult - Unified Expression Parsing

`ExpressionBlockResult` is a high-performance expression parsing class that provides a unified way to parse WHERE and UPDATE expressions, avoiding duplicate parsing and improving performance.

### Core Features

- **Unified Parsing** - Get SQL and parameters in one pass
- **High Performance** - Avoid repeated expression tree traversal (2x faster)
- **AOT Friendly** - Zero reflection, pure expression tree parsing
- **Thread Safe** - No shared state
- **Multi-Dialect** - Supports all database dialects

### API Interface

```csharp
namespace Sqlx.Expressions;

public sealed class ExpressionBlockResult
{
    // Generated SQL fragment
    public string Sql { get; }
    
    // Extracted parameter dictionary
    public Dictionary<string, object?> Parameters { get; }
    
    // Placeholder tracking (for Any.Value<T>)
    private Dictionary<string, Type> Placeholders { get; }
    
    // Parse WHERE expression
    public static ExpressionBlockResult Parse(
        Expression? expression, 
        SqlDialect dialect);
    
    // Parse UPDATE expression
    public static ExpressionBlockResult ParseUpdate<T>(
        Expression<Func<T, T>>? updateExpression, 
        SqlDialect dialect);
    
    // Fill single placeholder
    public ExpressionBlockResult WithParameter(string name, object? value);
    
    // Fill multiple placeholders
    public ExpressionBlockResult WithParameters(IReadOnlyDictionary<string, object?> parameters);
    
    // Get all placeholder names
    public IReadOnlyList<string> GetPlaceholderNames();
    
    // Check if all placeholders are filled
    public bool AreAllPlaceholdersFilled();
    
    // Empty result
    public static ExpressionBlockResult Empty { get; }
}
```

### Usage Examples

#### 1. WHERE Expression Parsing

```csharp
using Sqlx.Expressions;

// Simple condition
var minAge = 18;
Expression<Func<User, bool>> predicate = u => u.Age > minAge;
var result = ExpressionBlockResult.Parse(predicate.Body, SqlDefine.SQLite);

Console.WriteLine(result.Sql);        // "[age] > @p0"
Console.WriteLine(result.Parameters["@p0"]);  // 18

// Complex condition
var name = "John";
Expression<Func<User, bool>> complexPredicate = 
    u => u.Age > minAge && u.Name == name && u.IsActive;
var result2 = ExpressionBlockResult.Parse(complexPredicate.Body, SqlDefine.SQLite);

Console.WriteLine(result2.Sql);
// "[age] > @p0 AND [name] = @p1 AND [is_active] = @p2"
Console.WriteLine(result2.Parameters.Count);  // 3
```

#### 2. UPDATE Expression Parsing

```csharp
// Simple update
Expression<Func<User, User>> updateExpr = u => new User 
{ 
    Name = "Jane", 
    Age = 25 
};
var result = ExpressionBlockResult.ParseUpdate(updateExpr, SqlDefine.SQLite);

Console.WriteLine(result.Sql);
// "[name] = @p0, [age] = @p1"
Console.WriteLine(result.Parameters["@p0"]);  // "Jane"
Console.WriteLine(result.Parameters["@p1"]);  // 25

// Increment update
Expression<Func<User, User>> incrementExpr = u => new User 
{ 
    Age = u.Age + 1,
    Version = u.Version + 1
};
var result2 = ExpressionBlockResult.ParseUpdate(incrementExpr, SqlDefine.SQLite);

Console.WriteLine(result2.Sql);
// "[age] = [age] + @p0, [version] = [version] + @p1"

// String functions
Expression<Func<User, User>> funcExpr = u => new User 
{ 
    Name = u.Name.Trim().ToLower()
};
var result3 = ExpressionBlockResult.ParseUpdate(funcExpr, SqlDefine.SQLite);

Console.WriteLine(result3.Sql);
// "[name] = LOWER(TRIM([name]))"
```

#### 3. Any Placeholder Support (NEW)

Use `Any.Value<T>(name)` to create reusable expression templates:

```csharp
using Sqlx.Expressions;

// Define reusable increment template
var incrementTemplate = ExpressionBlockResult.ParseUpdate<Todo>(
    t => new Todo 
    { 
        Priority = t.Priority + Any.Value<int>("increment"),
        Version = t.Version + 1
    },
    SqlDefine.SQLite
);

Console.WriteLine(incrementTemplate.Sql);
// "[priority] = [priority] + @increment, [version] = [version] + @p0"

Console.WriteLine(string.Join(", ", incrementTemplate.GetPlaceholderNames()));
// "increment"

// Fill placeholder with different values
var result1 = incrementTemplate.WithParameter("increment", 1);
Console.WriteLine(result1.Parameters["@increment"]);  // 1

var result2 = incrementTemplate.WithParameter("increment", 5);
Console.WriteLine(result2.Parameters["@increment"]);  // 5

// Batch operations with template
var batchTemplate = ExpressionBlockResult.ParseUpdate<Todo>(
    t => new Todo 
    { 
        Priority = Any.Value<int>("newPriority"),
        UpdatedAt = DateTime.UtcNow
    },
    SqlDefine.SQLite
);

foreach (var batch in batches)
{
    var result = batchTemplate.WithParameter("newPriority", batch.Priority);
    // Execute update with result.Sql and result.Parameters
}
```

#### 4. Practical Application

```csharp
// Build complete UPDATE statement
public async Task<int> UpdateUsersAsync(
    Expression<Func<User, User>> updateExpr,
    Expression<Func<User, bool>> whereExpr)
{
    var dialect = SqlDefine.SQLite;
    
    // Parse UPDATE and WHERE expressions
    var updateResult = ExpressionBlockResult.ParseUpdate(updateExpr, dialect);
    var whereResult = ExpressionBlockResult.Parse(whereExpr.Body, dialect);
    
    // Merge parameters
    var parameters = new Dictionary<string, object?>(updateResult.Parameters);
    foreach (var param in whereResult.Parameters)
    {
        parameters[param.Key] = param.Value;
    }
    
    // Build complete SQL
    var sql = $"UPDATE [users] SET {updateResult.Sql} WHERE {whereResult.Sql}";
    
    // Execute SQL
    using var cmd = connection.CreateCommand();
    cmd.CommandText = sql;
    foreach (var param in parameters)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = param.Key;
        p.Value = param.Value ?? DBNull.Value;
        cmd.Parameters.Add(p);
    }
    
    return await cmd.ExecuteNonQueryAsync();
}

// Usage
await UpdateUsersAsync(
    u => new User { Name = "Updated", Age = u.Age + 1 },
    u => u.Age > 18 && u.IsActive
);
```

### Performance Advantage

Compared to calling `ToSetClause()` + `GetSetParameters()` or `ToWhereClause()` + `GetParameters()` separately:

| Method | Expression Traversals | Performance |
|--------|----------------------|-------------|
| Traditional | 2 times (SQL + params) | Baseline |
| ExpressionBlockResult | 1 time (both at once) | **2x faster** |

```csharp
// ❌ Traditional way - traverse 2 times
var sql = updateExpr.ToSetClause();
var parameters = updateExpr.GetSetParameters();

// ✅ New way - traverse 1 time
var result = ExpressionBlockResult.ParseUpdate(updateExpr, dialect);
// result.Sql and result.Parameters available simultaneously
```

### Supported Expressions

#### WHERE Expressions

- Comparison: `>`, `<`, `>=`, `<=`, `==`, `!=`
- Logical: `&&`, `||`, `!`
- String functions: `ToLower()`, `ToUpper()`, `Trim()`, `Contains()`, `StartsWith()`, `EndsWith()`
- Math functions: `Abs()`, `Round()`, `Floor()`, `Ceiling()`, `Sqrt()`, `Pow()`
- Null checks: `== null`, `!= null`
- Boolean properties: `u.IsActive` (auto-converts to `u.IsActive = true`)

#### UPDATE Expressions

- Constant assignment: `Name = "John"`
- Field reference: `Age = u.Age + 1`
- String functions: `Name = u.Name.Trim().ToLower()`
- Math functions: `Age = Math.Abs(u.Age)`
- Arithmetic: `+`, `-`, `*`, `/`
- Null values: `Email = null`
- Placeholders: `Priority = Any.Value<int>("newPriority")`

### Notes

1. **Parameter Naming**: Parameters include dialect prefix, e.g., `@p0` (SQLite), `$1` (PostgreSQL)
2. **Parameter Order**: Parameters numbered by parse order, starting from 0
3. **Null Handling**: Null values parameterized as `@p0 = null`
4. **Thread Safety**: Each call creates new instance, no shared state
5. **AOT Compatible**: Fully supports Native AOT, no reflection
6. **Placeholder Tracking**: Placeholders created by `Any.Value<T>()` are tracked separately

### Test Coverage

`ExpressionBlockResult` includes **77 comprehensive tests**:

- ✅ WHERE expression parsing (simple, complex, nested)
- ✅ UPDATE expression parsing (constant, increment, functions)
- ✅ Multi-database dialect support
- ✅ Null value handling
- ✅ String and math functions
- ✅ Any placeholder support (16 tests)
- ✅ Advanced placeholder scenarios (31 tests)
- ✅ DynamicUpdate integration (15 tests)
- ✅ Edge cases and error handling

**Test Files:**
- `tests/Sqlx.Tests/ExpressionBlockResultTests.cs` - Basic tests (19)
- `tests/Sqlx.Tests/ExpressionBlockResultAnyPlaceholderTests.cs` - Placeholder tests (16)
- `tests/Sqlx.Tests/ExpressionBlockResultAnyPlaceholderAdvancedTests.cs` - Advanced tests (31)
- `tests/Sqlx.Tests/DynamicUpdateWithAnyPlaceholderTests.cs` - Integration tests (15)

---

## Dynamic Update Operations

### DynamicUpdateAsync / DynamicUpdate

Update specific fields of a single entity by ID using type-safe expressions.

```csharp
// Interface (inherited from ICommandRepository)
Task<int> DynamicUpdateAsync(TKey id, Expression<Func<TEntity, TEntity>> updateExpression, CancellationToken ct = default);
int DynamicUpdate(TKey id, Expression<Func<TEntity, TEntity>> updateExpression);
```

**Parameters:**
- `id` - Primary key of the entity to update
- `updateExpression` - Expression defining which fields to update and their values
- `ct` - Cancellation token (optional)

**Returns:** Number of rows affected (0 or 1)

**Examples:**

```csharp
// Update single field
await repo.DynamicUpdateAsync(userId, u => new User { Priority = 5 });
// SQL: UPDATE [users] SET [priority] = @p0 WHERE [id] = @id

// Update multiple fields
await repo.DynamicUpdateAsync(userId, u => new User 
{ 
    Name = "John",
    Priority = 5,
    UpdatedAt = DateTime.UtcNow
});
// SQL: UPDATE [users] SET [name] = @p0, [priority] = @p1, [updated_at] = @p2 WHERE [id] = @id

// Increment field
await repo.DynamicUpdateAsync(userId, u => new User { Version = u.Version + 1 });
// SQL: UPDATE [users] SET [version] = [version] + @p0 WHERE [id] = @id

// String functions
await repo.DynamicUpdateAsync(userId, u => new User { Name = u.Name.Trim().ToUpper() });
// SQL: UPDATE [users] SET [name] = UPPER(TRIM([name])) WHERE [id] = @id
```

### DynamicUpdateWhereAsync / DynamicUpdateWhere

Update specific fields of multiple entities matching a WHERE condition.

```csharp
// Interface (inherited from ICommandRepository)
Task<int> DynamicUpdateWhereAsync(
    Expression<Func<TEntity, TEntity>> updateExpression, 
    Expression<Func<TEntity, bool>> whereExpression, 
    CancellationToken ct = default);
int DynamicUpdateWhere(
    Expression<Func<TEntity, TEntity>> updateExpression, 
    Expression<Func<TEntity, bool>> whereExpression);
```

**Parameters:**
- `updateExpression` - Expression defining which fields to update and their values
- `whereExpression` - Expression defining which entities to update
- `ct` - Cancellation token (optional)

**Returns:** Number of rows affected

**Examples:**

```csharp
// Batch update with condition
await repo.DynamicUpdateWhereAsync(
    u => new User { IsActive = false, UpdatedAt = DateTime.UtcNow },
    u => u.Age < 18
);
// SQL: UPDATE [users] SET [is_active] = @p0, [updated_at] = @p1 WHERE [age] < @p2

// Complex condition
await repo.DynamicUpdateWhereAsync(
    u => new User { Priority = 5 },
    u => u.Priority >= 3 && !u.IsCompleted && u.DueDate < DateTime.Now
);
// SQL: UPDATE [users] SET [priority] = @p0 
//      WHERE [priority] >= @p1 AND [is_completed] = @p2 AND [due_date] < @p3

// Increment for matching records
await repo.DynamicUpdateWhereAsync(
    u => new User { ViewCount = u.ViewCount + 1 },
    u => u.IsActive
);
// SQL: UPDATE [users] SET [view_count] = [view_count] + @p0 WHERE [is_active] = @p1
```

### Advantages

| Feature | Traditional Update | DynamicUpdate |
|---------|-------------------|---------------|
| Compile-time safety | ✅ Yes | ✅ Yes |
| IDE support | ✅ Intellisense | ✅ Intellisense + Refactoring |
| Flexibility | ⚠️ Fixed fields | ✅ Any field combination |
| Partial updates | ❌ No | ✅ Yes |
| Increment operations | ❌ No | ✅ Yes |
| Function support | ❌ No | ✅ Yes (string, math) |
| SQL injection safe | ✅ Yes | ✅ Yes (auto-parameterized) |

### Supported Functions

| Category | C# Function | SQL Output |
|----------|-------------|------------|
| **String** | `ToLower()` | `LOWER(column)` |
| | `ToUpper()` | `UPPER(column)` |
| | `Trim()` | `TRIM(column)` |
| | `Substring(start, length)` | `SUBSTR(column, start, length)` |
| | `Replace(old, new)` | `REPLACE(column, old, new)` |
| | `+ (concat)` | `column \|\| value` (dialect-specific) |
| **Math** | `Math.Abs(x)` | `ABS(x)` |
| | `Math.Round(x)` | `ROUND(x)` |
| | `Math.Ceiling(x)` | `CEIL(x)` |
| | `Math.Floor(x)` | `FLOOR(x)` |
| | `Math.Pow(x, y)` | `POWER(x, y)` |
| | `Math.Sqrt(x)` | `SQRT(x)` |
| | `Math.Max(a, b)` | `GREATEST(a, b)` (dialect-specific) |
| | `Math.Min(a, b)` | `LEAST(a, b)` (dialect-specific) |
| **Arithmetic** | `+`, `-`, `*`, `/` | `+`, `-`, `*`, `/` |

### Notes

1. **Expression must be MemberInitExpression**: `u => new User { Field = value }`
2. **Cannot return parameter directly**: `u => u` will throw ArgumentException
3. **Parameters auto-numbered**: `@p0`, `@p1`, `@p2`...
4. **Column names auto-converted**: PascalCase → snake_case with dialect wrapping
5. **Performance**: Uses `ExpressionBlockResult` for 2x faster parsing

---

## Any Placeholder Support

### Any.Value\<T\>(name)

Create placeholders in expressions that can be filled later, enabling template reuse.

```csharp
namespace Sqlx.Expressions;

public static class Any
{
    public static T Value<T>(string name);
}
```

**Parameters:**
- `name` - Placeholder name (must be unique within expression)

**Returns:** Default value of type T (not used, only for type inference)

**Usage:**

```csharp
using Sqlx.Expressions;

// Define template with placeholder
var template = ExpressionBlockResult.ParseUpdate<User>(
    u => new User 
    { 
        Priority = Any.Value<int>("newPriority"),
        UpdatedAt = DateTime.UtcNow
    },
    SqlDefine.SQLite
);

Console.WriteLine(template.Sql);
// "[priority] = @newPriority, [updated_at] = @p0"

// Fill placeholder
var result = template.WithParameter("newPriority", 5);
Console.WriteLine(result.Parameters["@newPriority"]);  // 5

// Reuse template with different values
var result2 = template.WithParameter("newPriority", 10);
Console.WriteLine(result2.Parameters["@newPriority"]);  // 10
```

### Use Cases

#### 1. Template Reuse

```csharp
// Define once
var updateTemplate = ExpressionBlockResult.ParseUpdate<Product>(
    p => new Product 
    { 
        Price = Any.Value<decimal>("newPrice"),
        UpdatedAt = DateTime.UtcNow
    },
    SqlDefine.SQLite
);

// Use multiple times
foreach (var product in products)
{
    var result = updateTemplate.WithParameter("newPrice", product.NewPrice);
    // Execute update...
}
```

#### 2. Batch Operations

```csharp
// Batch update template
var batchTemplate = ExpressionBlockResult.ParseUpdate<Order>(
    o => new Order 
    { 
        Status = Any.Value<string>("status"),
        ProcessedAt = DateTime.UtcNow
    },
    SqlDefine.SQLite
);

// Process different batches
var pendingResult = batchTemplate.WithParameter("status", "pending");
var completedResult = batchTemplate.WithParameter("status", "completed");
```

#### 3. Dynamic Forms

```csharp
// Form template
var formTemplate = ExpressionBlockResult.ParseUpdate<User>(
    u => new User 
    { 
        Name = Any.Value<string>("name"),
        Email = Any.Value<string>("email"),
        Age = Any.Value<int>("age")
    },
    SqlDefine.SQLite
);

// Fill from form data
var result = formTemplate
    .WithParameter("name", formData.Name)
    .WithParameter("email", formData.Email)
    .WithParameter("age", formData.Age);
```

#### 4. Conditional Updates

```csharp
// Template with optional fields
var template = ExpressionBlockResult.ParseUpdate<Task>(
    t => new Task 
    { 
        Priority = Any.Value<int>("priority"),
        DueDate = Any.Value<DateTime?>("dueDate")
    },
    SqlDefine.SQLite
);

// Check which placeholders need filling
var placeholders = template.GetPlaceholderNames();
// ["priority", "dueDate"]

// Fill based on conditions
var result = template
    .WithParameter("priority", updatePriority ? newPriority : currentPriority)
    .WithParameter("dueDate", updateDueDate ? newDueDate : currentDueDate);
```

### Methods

#### WithParameter

Fill a single placeholder.

```csharp
public ExpressionBlockResult WithParameter(string name, object? value);
```

**Returns:** New `ExpressionBlockResult` with placeholder filled

#### WithParameters

Fill multiple placeholders at once.

```csharp
public ExpressionBlockResult WithParameters(IReadOnlyDictionary<string, object?> parameters);
```

**Returns:** New `ExpressionBlockResult` with all placeholders filled

**Example:**

```csharp
var result = template.WithParameters(new Dictionary<string, object?>
{
    ["priority"] = 5,
    ["status"] = "active",
    ["updatedAt"] = DateTime.UtcNow
});
```

#### GetPlaceholderNames

Get all placeholder names in the expression.

```csharp
public IReadOnlyList<string> GetPlaceholderNames();
```

**Returns:** List of placeholder names

#### AreAllPlaceholdersFilled

Check if all placeholders have been filled.

```csharp
public bool AreAllPlaceholdersFilled();
```

**Returns:** `true` if all placeholders filled, `false` otherwise

### Notes

1. **Placeholder names are case-sensitive**
2. **Placeholders must be filled before execution**
3. **Unfilled placeholders will have `null` values**
4. **Type safety**: `Any.Value<T>()` ensures correct parameter type
5. **Immutable**: `WithParameter()` returns new instance, original unchanged

### Test Coverage

Any placeholder support includes **62 comprehensive tests**:

- ✅ Basic placeholder creation and filling (16 tests)
- ✅ Complex expressions with placeholders (31 tests)
- ✅ DynamicUpdate integration (15 tests)
- ✅ Multiple placeholders
- ✅ Type safety validation
- ✅ Error handling

**Test Files:**
- `tests/Sqlx.Tests/ExpressionBlockResultAnyPlaceholderTests.cs`
- `tests/Sqlx.Tests/ExpressionBlockResultAnyPlaceholderAdvancedTests.cs`
- `tests/Sqlx.Tests/DynamicUpdateWithAnyPlaceholderTests.cs`

---

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




## Diagnostics

### SqlTemplateMetrics

Provides metrics instrumentation for SQL template execution using `System.Diagnostics.Metrics` (.NET 8+).

```csharp
namespace Sqlx.Diagnostics;

public static class SqlTemplateMetrics
{
    // Record successful execution
    public static void RecordExecution(
        string repositoryClass,
        string methodName,
        string sqlTemplateField,
        long elapsedTicks);
    
    // Record failed execution
    public static void RecordError(
        string repositoryClass,
        string methodName,
        string sqlTemplateField,
        long elapsedTicks,
        Exception exception);
}
```

**Metrics Exposed:**

| Metric Name | Type | Unit | Description |
|-------------|------|------|-------------|
| `sqlx.template.duration` | Histogram | ms | SQL execution duration in milliseconds |
| `sqlx.template.executions` | Counter | {execution} | Total number of SQL executions |
| `sqlx.template.errors` | Counter | {error} | Total number of SQL execution errors |

**Tags (Dimensions):**

| Tag Name | Description | Example |
|----------|-------------|---------|
| `repository.class` | Full repository class name | `MyApp.Repositories.UserRepository` |
| `repository.method` | Method name | `GetByIdAsync` |
| `sql.template` | SQL template string | `SELECT {{columns}} FROM {{table}} WHERE id = @id` |
| `error.type` | Exception type name (errors only) | `SqliteException` |

**Usage:**

```csharp
// Metrics are automatically recorded by generated repository code
var repo = new UserRepository(connection);
await repo.GetByIdAsync(123);  // Metrics recorded automatically

// Consume metrics with OpenTelemetry
using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddMeter("Sqlx.SqlTemplate")
    .AddConsoleExporter()
    .Build();

// Or with MeterListener (for testing/debugging)
var listener = new MeterListener
{
    InstrumentPublished = (instrument, listener) =>
    {
        if (instrument.Meter.Name == "Sqlx.SqlTemplate")
        {
            listener.EnableMeasurementEvents(instrument);
        }
    }
};

listener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
{
    Console.WriteLine($"{instrument.Name}: {measurement}ms");
    foreach (var tag in tags)
    {
        Console.WriteLine($"  {tag.Key}: {tag.Value}");
    }
});

listener.Start();
```

**Features:**
- ✅ Zero configuration - automatically enabled
- ✅ Standard .NET Metrics API - works with OpenTelemetry, Prometheus, Application Insights
- ✅ High performance - compile-time code generation, zero runtime overhead
- ✅ AOT friendly - fully supports Native AOT
- ✅ Backward compatible - no-op on .NET 7 and earlier

**Example Output (Console Exporter):**

```
sqlx.template.duration: 2.45ms
  repository.class: MyApp.UserRepository
  repository.method: GetByIdAsync
  sql.template: SELECT {{columns}} FROM {{table}} WHERE id = @id

sqlx.template.executions: 1
  repository.class: MyApp.UserRepository
  repository.method: GetByIdAsync
  sql.template: SELECT {{columns}} FROM {{table}} WHERE id = @id
```

**Integration with Monitoring Systems:**

```csharp
// Prometheus
using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddMeter("Sqlx.SqlTemplate")
    .AddPrometheusExporter()
    .Build();

// Application Insights
using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddMeter("Sqlx.SqlTemplate")
    .AddAzureMonitorMetricExporter(options =>
    {
        options.ConnectionString = "InstrumentationKey=...";
    })
    .Build();

// OTLP (OpenTelemetry Protocol)
using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .AddMeter("Sqlx.SqlTemplate")
    .AddOtlpExporter()
    .Build();
```

See [Metrics Documentation](metrics.md) for more details.
