# Multi-Database Placeholder Support

Sqlx supports multiple database dialects out of the box. All SQL template placeholders automatically generate the correct syntax for your target database.

## Supported Databases

- **SQL Server** (SqlServer)
- **MySQL** (MySql)
- **PostgreSQL** (PostgreSql)
- **SQLite** (SQLite)
- **Oracle** (Oracle)

## Placeholder Multi-Database Support

### String Functions

| Placeholder | SQL Server | MySQL | PostgreSQL | SQLite | Oracle |
|------------|------------|-------|------------|---------|--------|
| `{{startswith}}` | `LIKE CONCAT(@param, '%')` | `LIKE CONCAT(@param, '%')` | `LIKE @param \|\| '%'` | `LIKE @param \|\| '%'` | `LIKE @param \|\| '%'` |
| `{{endswith}}` | `LIKE CONCAT('%', @param)` | `LIKE CONCAT('%', @param)` | `LIKE '%' \|\| @param` | `LIKE '%' \|\| @param` | `LIKE '%' \|\| @param` |
| `{{contains}}` | `LIKE '%' + @param + '%'` | `LIKE CONCAT('%', @param, '%')` | `ILIKE '%' \|\| @param \|\| '%'` | `LIKE '%' \|\| @param \|\| '%'` | `LIKE '%' \|\| @param \|\| '%'` |
| `{{concat}}` | `CONCAT(col1, col2)` | `CONCAT(col1, col2)` | `col1 \|\| col2` | `col1 \|\| col2` | `col1 \|\| col2` |
| `{{substring}}` | `SUBSTRING(col, start, len)` | `SUBSTR(col, start, len)` | `SUBSTRING(col, start, len)` | `SUBSTR(col, start, len)` | `SUBSTR(col, start, len)` |
| `{{length}}` | `LEN(col)` | `LENGTH(col)` | `LENGTH(col)` | `LENGTH(col)` | `LENGTH(col)` |
| `{{group_concat}}` | `STRING_AGG(col, ',')` | `GROUP_CONCAT(col SEPARATOR ',')` | `STRING_AGG(col, ',')` | `GROUP_CONCAT(col, ',')` | `GROUP_CONCAT(col, ',')` |

### Pagination

| Placeholder | SQL Server | MySQL | PostgreSQL | SQLite | Oracle |
|------------|------------|-------|------------|---------|--------|
| `{{limit}}` | `TOP n` or `OFFSET ... FETCH NEXT` | `LIMIT n` | `LIMIT n` | `LIMIT n` | `ROWNUM <= n` or `OFFSET ... FETCH NEXT` |
| `{{offset}}` | `OFFSET n ROWS` | `OFFSET n` | `OFFSET n` | `OFFSET n` | `OFFSET n ROWS` |

### Conditional Functions

| Placeholder | SQL Server | MySQL | PostgreSQL | SQLite | Oracle |
|------------|------------|-------|------------|---------|--------|
| `{{ifnull}}` | `ISNULL(col, default)` | `IFNULL(col, default)` | `IFNULL(col, default)` | `IFNULL(col, default)` | `NVL(col, default)` |
| `{{coalesce}}` | `COALESCE(col1, col2, ...)` | `COALESCE(col1, col2, ...)` | `COALESCE(col1, col2, ...)` | `COALESCE(col1, col2, ...)` | `COALESCE(col1, col2, ...)` |

### JSON Operations

| Placeholder | SQL Server | MySQL | PostgreSQL | SQLite | Oracle |
|------------|------------|-------|------------|---------|--------|
| `{{json_extract}}` | `JSON_VALUE(col, path)` | `JSON_EXTRACT(col, path)` | `col->>path` | `JSON_EXTRACT(col, path)` | `JSON_VALUE(col, path)` |
| `{{json_array}}` | `JSON_QUERY('[...]')` | `JSON_ARRAY(...)` | `JSON_BUILD_ARRAY(...)` | `JSON_ARRAY(...)` | `JSON_ARRAY(...)` |
| `{{json_object}}` | `JSON_OBJECT(key: value)` | `JSON_OBJECT(key, value)` | `JSON_BUILD_OBJECT(key, value)` | `JSON_OBJECT(key, value)` | `JSON_OBJECT(key VALUE value)` |

### UPSERT Operations

| Database | Syntax |
|----------|--------|
| **PostgreSQL** | `INSERT ... ON CONFLICT (id) DO UPDATE SET ...` |
| **MySQL** | `INSERT ... ON DUPLICATE KEY UPDATE ...` |
| **SQLite** | `INSERT OR REPLACE INTO ...` |
| **SQL Server** | `MERGE ... WHEN MATCHED THEN UPDATE WHEN NOT MATCHED THEN INSERT` |
| **Oracle** | `MERGE INTO ... USING ... ON ... WHEN MATCHED THEN UPDATE WHEN NOT MATCHED THEN INSERT` |

### Window Functions

All standard window functions are supported across all databases (for modern versions):

- `{{row_number}}` - Row number within partition
- `{{rank}}` - Rank with gaps
- `{{dense_rank}}` - Rank without gaps
- `{{lag}}` - Access previous row
- `{{lead}}` - Access next row

**Note**: SQLite supports window functions from version 3.25.0 (2018) onwards.

## Usage Examples

### Startswith Placeholder

```csharp
[RepositoryFor(typeof(User))]
public interface IUserRepository
{
    // Automatically generates correct syntax for each database
    [Query("SELECT * FROM users WHERE name {{startswith --column=name|value=searchTerm}}")]
    Task<List<User>> FindByNamePrefix(string searchTerm);
}
```

**Generated SQL**:
- **SQL Server/MySQL**: `WHERE name LIKE CONCAT(@searchTerm, '%')`
- **PostgreSQL/SQLite/Oracle**: `WHERE name LIKE @searchTerm || '%'`

### Concat Placeholder with Separator

```csharp
[Query("SELECT {{concat --columns=first_name,last_name|separator= }} AS full_name FROM users")]
Task<List<string>> GetFullNames();
```

**Generated SQL**:
- **SQL Server/MySQL**: `SELECT CONCAT_WS(' ', first_name, last_name) AS full_name`
- **PostgreSQL**: `SELECT array_to_string(ARRAY[first_name, last_name], ' ') AS full_name`
- **SQLite/Oracle**: `SELECT first_name || ' ' || last_name AS full_name`

### UPSERT Placeholder

```csharp
[Execute("{{upsert --conflict=id}}")]
Task<int> UpsertUser(User user);
```

**Generated SQL**:
- **PostgreSQL**: `INSERT INTO users ... ON CONFLICT (id) DO UPDATE SET ...`
- **MySQL**: `INSERT INTO users ... ON DUPLICATE KEY UPDATE ...`
- **SQLite**: `INSERT OR REPLACE INTO users ...`
- **SQL Server**: `MERGE users AS target USING ... WHEN MATCHED THEN UPDATE ...`

### JSON Extract Placeholder

```csharp
[Query("SELECT {{json_extract --column=metadata|path=$.email}} FROM users")]
Task<List<string>> GetEmailsFromMetadata();
```

**Generated SQL**:
- **SQL Server**: `SELECT JSON_VALUE(metadata, '$.email') FROM users`
- **MySQL**: `SELECT JSON_EXTRACT(metadata, '$.email') FROM users`
- **PostgreSQL**: `SELECT metadata->>'email' FROM users`
- **SQLite**: `SELECT JSON_EXTRACT(metadata, '$.email') FROM users`

## Automatic Dialect Detection

Sqlx automatically detects your database dialect through the `SqlDialectAttribute`:

```csharp
[SqlDialect(SqlDefine.PostgreSql)]
[RepositoryFor(typeof(Product))]
public interface IProductRepository
{
    // All placeholders will generate PostgreSQL-specific syntax
}
```

## Column Wrapping

All column and table names are automatically wrapped with the correct identifier quotes:

- **SQL Server**: `[column_name]`
- **MySQL**: `` `column_name` ``
- **PostgreSQL**: `"column_name"`
- **SQLite**: `"column_name"`
- **Oracle**: `"column_name"`

## Benefits

1. **Write Once, Run Everywhere**: Define SQL templates once, deploy to any supported database
2. **No Conditional Logic**: No need to check `if (dialect == SqlServer)` in your code
3. **Type-Safe**: All placeholders are validated at compile-time by the source generator
4. **Performance**: Zero runtime overhead - SQL is generated at compile-time
5. **Maintainable**: Change database without touching your repository code

## Migration Between Databases

Switching databases is as simple as changing the dialect attribute:

```csharp
// Development: SQLite
[SqlDialect(SqlDefine.SQLite)]
[RepositoryFor(typeof(Order))]
public interface IOrderRepository { }

// Production: PostgreSQL
[SqlDialect(SqlDefine.PostgreSql)]
[RepositoryFor(typeof(Order))]
public interface IOrderRepository { }
```

All placeholder-generated SQL will automatically adapt to the new dialect.

