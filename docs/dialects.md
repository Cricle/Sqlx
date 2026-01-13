# Database Dialects

Sqlx supports multiple database dialects, each providing database-specific SQL generation.

## Supported Dialects

| Dialect | Enum Value | Column Quotes | Parameter Prefix |
|---------|------------|---------------|------------------|
| SQLite | `SqlDefineTypes.SQLite` | `[column]` | `@` |
| MySQL | `SqlDefineTypes.MySql` | `` `column` `` | `@` |
| PostgreSQL | `SqlDefineTypes.PostgreSql` | `"column"` | `$` |
| SQL Server | `SqlDefineTypes.SqlServer` | `[column]` | `@` |
| Oracle | `SqlDefineTypes.Oracle` | `"column"` | `:` |
| DB2 | `SqlDefineTypes.DB2` | `"column"` | `?` |

## Using Dialects

### In Repository Attributes

```csharp
[SqlDefine(SqlDefineTypes.SQLite)]
[TableName("users")]
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository { }
```

### Programmatically

```csharp
// Access predefined dialects
var sqlite = SqlDefine.SQLite;
var mysql = SqlDefine.MySql;
var postgres = SqlDefine.PostgreSql;

// Get dialect by enum
var dialect = SqlDefine.GetDialect(SqlDefineTypes.SQLite);

// Use dialect methods
var quotedColumn = dialect.WrapColumn("user_name");  // [user_name]
var param = dialect.CreateParameter("id");           // @id
```

## Dialect Features

### Identifier Quoting

```csharp
// SQLite/SQL Server
dialect.WrapColumn("name")  // [name]

// MySQL
dialect.WrapColumn("name")  // `name`

// PostgreSQL/Oracle/DB2
dialect.WrapColumn("name")  // "name"
```

### String Functions

```csharp
// Concatenation
SqlDefine.SqlServer.Concat("a", "b")  // a + b
SqlDefine.MySql.Concat("a", "b")      // CONCAT(a, b)
SqlDefine.PostgreSql.Concat("a", "b") // a || b

// Length
SqlDefine.SqlServer.Length("col")     // LEN(col)
SqlDefine.MySql.Length("col")         // CHAR_LENGTH(col)
SqlDefine.SQLite.Length("col")        // LENGTH(col)
```

### Date/Time Functions

```csharp
// Current timestamp
SqlDefine.SqlServer.CurrentTimestamp  // GETDATE()
SqlDefine.MySql.CurrentTimestamp      // NOW()
SqlDefine.PostgreSql.CurrentTimestamp // CURRENT_TIMESTAMP
SqlDefine.SQLite.CurrentTimestamp     // CURRENT_TIMESTAMP
SqlDefine.Oracle.CurrentTimestamp     // SYSTIMESTAMP

// Date arithmetic
SqlDefine.SqlServer.DateAdd("DAY", "7", "created_at")
// DATEADD(DAY, 7, created_at)

SqlDefine.MySql.DateAdd("DAY", "7", "created_at")
// DATE_ADD(created_at, INTERVAL 7 DAY)

SqlDefine.PostgreSql.DateAdd("DAY", "7", "created_at")
// (created_at + INTERVAL '7 DAY')
```

### Pagination

```csharp
// Standard LIMIT/OFFSET (SQLite, MySQL, PostgreSQL)
SqlDefine.SQLite.Limit("10")           // LIMIT 10
SqlDefine.SQLite.Offset("20")          // OFFSET 20
SqlDefine.SQLite.Paginate("10", "20")  // LIMIT 10 OFFSET 20

// SQL Server (uses TOP or OFFSET FETCH)
SqlDefine.SqlServer.Limit("10")        // TOP 10
SqlDefine.SqlServer.Paginate("10", "20")
// OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY

// Oracle/DB2
SqlDefine.Oracle.Paginate("10", "20")
// OFFSET 20 ROWS FETCH NEXT 10 ROWS ONLY
```

### Null Handling

```csharp
// IFNULL/ISNULL/NVL/COALESCE
SqlDefine.SqlServer.IfNull("col", "'default'")  // ISNULL(col, 'default')
SqlDefine.MySql.IfNull("col", "'default'")      // IFNULL(col, 'default')
SqlDefine.Oracle.IfNull("col", "'default'")     // NVL(col, 'default')
SqlDefine.PostgreSql.IfNull("col", "'default'") // COALESCE(col, 'default')
```

### Last Inserted ID

```csharp
SqlDefine.SQLite.LastInsertedId     // SELECT last_insert_rowid()
SqlDefine.MySql.LastInsertedId      // SELECT LAST_INSERT_ID()
SqlDefine.SqlServer.LastInsertedId  // SELECT SCOPE_IDENTITY()
SqlDefine.PostgreSql.LastInsertedId // SELECT lastval()
SqlDefine.Oracle.LastInsertedId     // SELECT SEQ.CURRVAL FROM DUAL
```

## Creating Custom Dialects

Extend `SqlDialect` to create a custom dialect:

```csharp
public class CustomDialect : SqlDialect
{
    public override string DatabaseType => "Custom";
    public override Annotations.SqlDefineTypes DbType => SqlDefineTypes.SQLite;
    public override string ColumnLeft => "`";
    public override string ColumnRight => "`";
    public override string ParameterPrefix => "?";
    
    public override string Concat(params string[] parts) 
        => $"CONCAT({string.Join(", ", parts)})";
    
    public override string CurrentTimestamp => "NOW()";
    public override string CurrentDate => "CURDATE()";
    public override string CurrentTime => "CURTIME()";
    
    public override string DatePart(string part, string expression) 
        => $"EXTRACT({part} FROM {expression})";
    
    public override string DateAdd(string interval, string number, string expression) 
        => $"DATE_ADD({expression}, INTERVAL {number} {interval})";
    
    public override string DateDiff(string interval, string startDate, string endDate) 
        => $"TIMESTAMPDIFF({interval}, {startDate}, {endDate})";
    
    public override string LastInsertedId => "SELECT LAST_INSERT_ID()";
}
```

## Best Practices

1. **Choose the right dialect**: Match your database exactly
2. **Use dialect methods**: Don't hardcode SQL syntax
3. **Test across dialects**: If supporting multiple databases, test each one
4. **Handle NULL carefully**: Different databases handle NULL differently
