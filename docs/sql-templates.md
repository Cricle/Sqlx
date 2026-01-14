# SQL Templates

Sqlx uses a powerful template system for generating SQL statements. Templates support placeholders that are resolved at compile time (static) or runtime (dynamic).

## Placeholder Syntax

Placeholders use double curly braces: `{{name}}` or `{{name --option value}}`

## Built-in Placeholders

### {{columns}}

Generates a comma-separated list of quoted column names for SELECT statements.

```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}}")]
// Output: SELECT [id], [name], [email] FROM [users]
```

**Options:**
- `--exclude col1,col2` - Excludes specified columns

```csharp
[SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
// Output: INSERT INTO [users] ([name], [email]) VALUES (@name, @email)
```

### {{values}}

Generates parameter placeholders for INSERT statements.

```csharp
[SqlTemplate("INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})")]
// Output: INSERT INTO [users] ([id], [name], [email]) VALUES (@id, @name, @email)
```

**Options:**
- `--exclude col1,col2` - Excludes specified columns

### {{set}}

Generates SET clause for UPDATE statements.

```csharp
[SqlTemplate("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id")]
// Output: UPDATE [users] SET [name] = @name, [email] = @email WHERE id = @id
```

**Options:**
- `--exclude col1,col2` - Excludes specified columns

### {{table}}

Generates the quoted table name.

```csharp
[SqlTemplate("SELECT * FROM {{table}}")]
// SQLite:     SELECT * FROM [users]
// MySQL:      SELECT * FROM `users`
// PostgreSQL: SELECT * FROM "users"
```

### {{where}}

Generates dynamic WHERE clauses. Always requires `--param` option.

```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}")]
Task<List<User>> GetWhereAsync([ExpressionToSql] Expression<Func<User, bool>> predicate);

// Usage: repo.GetWhereAsync(u => u.Age > 18 && u.IsActive)
// Output: SELECT [id], [name], [email] FROM [users] WHERE [age] > 18 AND [is_active] = 1
```

**Options:**
- `--param name` - (Required) Parameter name containing the WHERE clause

### {{limit}}

Generates LIMIT clause. Can be static or dynamic.

```csharp
// Static limit
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{limit --count 10}}")]
// Output: SELECT [id], [name], [email] FROM [users] LIMIT 10

// Dynamic limit
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{limit --param pageSize}}")]
Task<List<User>> GetPagedAsync(int pageSize);
// Output: SELECT [id], [name], [email] FROM [users] LIMIT 20
```

**Options:**
- `--count n` - Static limit value (resolved at prepare time)
- `--param name` - Dynamic limit value (resolved at render time)

### {{offset}}

Generates OFFSET clause. Can be static or dynamic.

```csharp
// Dynamic pagination
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{limit --param pageSize}} {{offset --param offset}}")]
Task<List<User>> GetPagedAsync(int pageSize, int offset);
// Output: SELECT [id], [name], [email] FROM [users] LIMIT 20 OFFSET 40
```

**Options:**
- `--count n` - Static offset value
- `--param name` - Dynamic offset value

## Static vs Dynamic Placeholders

### Static Placeholders

Resolved once during `SqlTemplate.Prepare()`:
- `{{columns}}`
- `{{values}}`
- `{{set}}`
- `{{table}}`
- `{{limit --count n}}`
- `{{offset --count n}}`

### Dynamic Placeholders

Resolved each time during `SqlTemplate.Render()`:
- `{{where --param name}}`
- `{{limit --param name}}`
- `{{offset --param name}}`

## Template Processing Flow

```
Template String
      │
      ▼
┌─────────────────┐
│ SqlTemplate.    │  Static placeholders resolved
│ Prepare()       │  Dynamic positions recorded
└────────┬────────┘
         │
         ▼
   SqlTemplate
   (prepared)
         │
         ▼
┌─────────────────┐
│ SqlTemplate.    │  Dynamic placeholders rendered
│ Render()        │  with runtime parameters
└────────┬────────┘
         │
         ▼
   Final SQL String
```

## Custom Placeholder Handlers

You can create custom placeholder handlers by implementing `IPlaceholderHandler`:

```csharp
public class OrderByPlaceholderHandler : PlaceholderHandlerBase
{
    public static OrderByPlaceholderHandler Instance { get; } = new();
    
    public override string Name => "orderby";
    
    public override PlaceholderType GetType(string options) => PlaceholderType.Dynamic;
    
    public override string Process(PlaceholderContext context, string options)
        => throw new InvalidOperationException("{{orderby}} is dynamic");
    
    public override string Render(PlaceholderContext context, string options, 
        IReadOnlyDictionary<string, object?>? parameters)
    {
        var paramName = ParseParam(options) 
            ?? throw new InvalidOperationException("{{orderby}} requires --param");
        var value = GetParam(parameters, paramName);
        return $"ORDER BY {value}";
    }
}

// Register the handler
PlaceholderProcessor.RegisterHandler(OrderByPlaceholderHandler.Instance);
```

## Best Practices

1. **Use --exclude for auto-generated columns**: Exclude `Id` columns in INSERT statements
2. **Prefer static placeholders**: They're resolved once and cached
3. **Use expressions for type-safe WHERE clauses**: `[ExpressionToSql]` provides compile-time safety
4. **Keep templates simple**: Complex logic should be in your code, not templates

## Block Placeholders

Block placeholders have opening and closing tags that control content inclusion based on runtime conditions.

### {{if}} Conditional Blocks

Conditionally include SQL fragments based on parameter values.

**Syntax:**
```
{{if condition=paramName}}SQL content{{/if}}
```

**Supported Conditions:**

| Condition | Description |
|-----------|-------------|
| `notnull=param` | Include when parameter is not null |
| `null=param` | Include when parameter is null |
| `notempty=param` | Include when parameter is not null and not empty (for collections/strings) |
| `empty=param` | Include when parameter is null or empty |

**Examples:**

```csharp
// Dynamic search with optional filters
[SqlTemplate(@"
    SELECT {{columns}} FROM {{table}} 
    WHERE 1=1 
    {{if notnull=name}}AND name LIKE @name{{/if}}
    {{if notnull=minAge}}AND age >= @minAge{{/if}}
    {{if notnull=maxAge}}AND age <= @maxAge{{/if}}
")]
Task<List<User>> SearchAsync(string? name, int? minAge, int? maxAge);

// Usage:
await repo.SearchAsync("Alice%", 18, null);
// Output: SELECT ... WHERE 1=1 AND name LIKE @name AND age >= @minAge
// (maxAge condition excluded because parameter is null)
```

```csharp
// Optional JOIN
[SqlTemplate(@"
    SELECT u.* FROM users u
    {{if notnull=includeOrders}}LEFT JOIN orders o ON o.user_id = u.id{{/if}}
    WHERE u.id = @id
")]
Task<User?> GetUserAsync(long id, bool? includeOrders);
```

```csharp
// Collection-based conditions
[SqlTemplate(@"
    SELECT {{columns}} FROM {{table}} 
    WHERE 1=1
    {{if notempty=ids}}AND id IN ({{values --param ids}}){{/if}}
")]
Task<List<User>> GetByIdsAsync(List<long>? ids);
```

**Multiple Conditions:**

You can use multiple `{{if}}` blocks in the same template:

```csharp
[SqlTemplate(@"
    SELECT {{columns}} FROM {{table}} 
    WHERE is_active = {{bool_true}}
    {{if notnull=department}}AND department = @department{{/if}}
    {{if notnull=role}}AND role = @role{{/if}}
    {{if notnull=minSalary}}AND salary >= @minSalary{{/if}}
    ORDER BY name
")]
Task<List<Employee>> FilterEmployeesAsync(string? department, string? role, decimal? minSalary);
```

### Custom Block Placeholder Handlers

Create custom block handlers by implementing `IBlockPlaceholderHandler`:

```csharp
public class UnlessPlaceholderHandler : PlaceholderHandlerBase, IBlockPlaceholderHandler
{
    public static UnlessPlaceholderHandler Instance { get; } = new();
    
    public override string Name => "unless";
    public string ClosingTagName => "/unless";
    
    public override PlaceholderType GetType(string options) => PlaceholderType.Dynamic;
    
    public override string Process(PlaceholderContext context, string options)
        => throw new InvalidOperationException("{{unless}} is dynamic");
    
    public override string Render(PlaceholderContext context, string options, 
        IReadOnlyDictionary<string, object?>? parameters) => string.Empty;
    
    public bool ShouldInclude(string options, IReadOnlyDictionary<string, object?>? parameters)
    {
        // Inverse of "if notnull" - include when parameter IS null
        var paramName = options.Trim();
        var value = parameters?.TryGetValue(paramName, out var v) == true ? v : null;
        return value is null;
    }
}

// Register the handler
PlaceholderProcessor.RegisterHandler(UnlessPlaceholderHandler.Instance);
PlaceholderProcessor.RegisterBlockClosingTag("/unless");
```
