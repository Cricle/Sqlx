# API Reference

## Core Classes

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

### IParameterBinder\<TEntity\>

Binds entity parameters to DbCommand.

```csharp
public interface IParameterBinder<TEntity>
{
    void BindEntity(DbCommand command, TEntity entity, string parameterPrefix = "@");
}
```

### ICrudRepository\<TEntity, TKey\>

Standard CRUD repository interface.

```csharp
public interface ICrudRepository<TEntity, TKey> : IQueryRepository<TEntity, TKey>, ICommandRepository<TEntity, TKey>
    where TEntity : class
{
}
```

### IQueryRepository\<TEntity, TKey\>

Query operations.

```csharp
public interface IQueryRepository<TEntity, TKey> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default);
    Task<List<TEntity>> GetByIdsAsync(List<TKey> ids, CancellationToken ct = default);
    Task<List<TEntity>> GetAllAsync(int limit = 1000, CancellationToken ct = default);
    Task<List<TEntity>> GetWhereAsync(Expression<Func<TEntity, bool>> predicate, int limit = 1000, CancellationToken ct = default);
    Task<TEntity?> GetFirstWhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    Task<List<TEntity>> GetPagedAsync(int pageSize = 20, int offset = 0, CancellationToken ct = default);
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    Task<long> CountAsync(CancellationToken ct = default);
    Task<long> CountWhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
}
```

### ICommandRepository\<TEntity, TKey\>

Command operations.

```csharp
public interface ICommandRepository<TEntity, TKey> where TEntity : class
{
    Task<TKey> InsertAndGetIdAsync(TEntity entity, CancellationToken ct = default);
    Task<int> UpdateAsync(TEntity entity, CancellationToken ct = default);
    Task<int> UpdateWhereAsync(Expression<Func<TEntity, bool>> predicate, ExpressionToSql<TEntity> setter, CancellationToken ct = default);
    Task<int> DeleteAsync(TKey id, CancellationToken ct = default);
    Task<int> DeleteWhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
}
```

## Attributes

### [SqlxEntity]

Marks a class for EntityProvider and ResultReader generation.

```csharp
[SqlxEntity]
public class User { }
```

### [SqlxParameter]

Marks a class for ParameterBinder generation.

```csharp
[SqlxParameter]
public class User { }
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
