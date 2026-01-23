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
- `--inline PropertyName=expression` - Specifies inline expressions using property names

**Inline Expressions:**

The `--inline` option allows you to use SQL expressions or literals instead of parameter placeholders. This is useful for default values, timestamps, and computed values.

```csharp
// Auto-generate timestamp
[SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline CreatedAt=CURRENT_TIMESTAMP}})")]
// Output: INSERT INTO [users] ([name], [email], [created_at]) VALUES (@name, @email, CURRENT_TIMESTAMP)

// Set default values
[SqlTemplate("INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Status='pending',Priority=0,CreatedAt=CURRENT_TIMESTAMP}})")]
// Output: INSERT INTO [tasks] ([id], [name], [status], [priority], [created_at]) VALUES (@id, @name, 'pending', 0, CURRENT_TIMESTAMP)

// Multiple inline expressions
[SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id --inline Version=1,CreatedAt=CURRENT_TIMESTAMP,UpdatedAt=CURRENT_TIMESTAMP}})")]
// Output: INSERT INTO [documents] ([title], [content], [version], [created_at], [updated_at]) VALUES (@title, @content, 1, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)

// Computed values
[SqlTemplate("INSERT INTO {{table}} ({{columns}}) VALUES ({{values --inline Total=@quantity*@unitPrice}})")]
Task<int> InsertOrderItemAsync(long id, int quantity, decimal unitPrice);
// Output: INSERT INTO [order_items] ([id], [quantity], [unit_price], [total]) VALUES (@id, @quantity, @unit_price, @quantity*@unitPrice)
```

**Expression Rules:**
- Use C# property names (PascalCase) in expressions, not column names
- Property names are automatically replaced with dialect-wrapped column names
- Parameter placeholders (@param, :param, $param) are preserved as-is
- Supports SQL functions, literals, and complex expressions

### {{set}}

Generates SET clause for UPDATE statements.

```csharp
[SqlTemplate("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id")]
// Output: UPDATE [users] SET [name] = @name, [email] = @email WHERE id = @id
```

**Options:**
- `--exclude col1,col2` - Excludes specified columns
- `--inline PropertyName=expression` - Specifies inline expressions using property names

**Inline Expressions:**

The `--inline` option allows you to use SQL expressions instead of parameter placeholders. Property names in expressions are automatically replaced with dialect-wrapped column names.

```csharp
// Increment version counter
[SqlTemplate("UPDATE {{table}} SET {{set --exclude Id --inline Version=Version+1}} WHERE id = @id")]
// Output: UPDATE [users] SET [name] = @name, [email] = @email, [version] = [version] + 1 WHERE id = @id

// Set timestamp to current time
[SqlTemplate("UPDATE {{table}} SET {{set --exclude Id --inline UpdatedAt=CURRENT_TIMESTAMP}} WHERE id = @id")]
// Output: UPDATE [users] SET [name] = @name, [email] = @email, [updated_at] = CURRENT_TIMESTAMP WHERE id = @id

// Multiple expressions (comma-separated)
[SqlTemplate("UPDATE {{table}} SET {{set --exclude Id --inline Version=Version+1,UpdatedAt=CURRENT_TIMESTAMP}} WHERE id = @id")]
// Output: UPDATE [users] SET [name] = @name, [email] = @email, [version] = [version] + 1, [updated_at] = CURRENT_TIMESTAMP WHERE id = @id

// Mix expressions with parameters
[SqlTemplate("UPDATE {{table}} SET {{set --exclude Id --inline Counter=Counter+@increment}} WHERE id = @id")]
Task<int> IncrementCounterAsync(long id, string name, int increment);
// Output: UPDATE [users] SET [name] = @name, [email] = @email, [counter] = [counter] + @increment WHERE id = @id
```

**Expression Rules:**
- Use C# property names (PascalCase) in expressions, not column names
- Property names are automatically replaced with dialect-wrapped column names
- Parameter placeholders (@param, :param, $param) are preserved as-is
- Expressions can contain spaces and complex SQL operations

### {{table}}

Generates the quoted table name. Can be static or dynamic.

```csharp
// Static table name (from context)
[SqlTemplate("SELECT * FROM {{table}}")]
// SQLite:     SELECT * FROM [users]
// MySQL:      SELECT * FROM `users`
// PostgreSQL: SELECT * FROM "users"

// Dynamic table name (from parameter)
[SqlTemplate("SELECT * FROM {{table --param tableName}}")]
Task<List<User>> GetFromTableAsync(string tableName);

// Usage: repo.GetFromTableAsync("users_archive")
// Output: SELECT * FROM [users_archive]
```

**Options:**
- No options - Uses static table name from context
- `--param name` - Dynamic table name resolved at render time

### {{where}}

Generates dynamic WHERE clauses. Supports two modes:

**Mode 1: Expression-based (--param)**

```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}")]
Task<List<User>> GetWhereAsync([ExpressionToSql] Expression<Func<User, bool>> predicate);

// Usage: repo.GetWhereAsync(u => u.Age > 18 && u.IsActive)
// Output: SELECT [id], [name], [email] FROM [users] WHERE [age] > 18 AND [is_active] = 1
```

**Mode 2: Dictionary-based (--object)**

Generates WHERE conditions from a dictionary. Only non-null values are included. AOT compatible.

```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where --object filter}}")]
Task<List<User>> FilterAsync(IReadOnlyDictionary<string, object?> filter);

// Usage with multiple conditions:
var filter = new Dictionary<string, object?>
{
    ["Name"] = "John",      // Generates: [name] = @name
    ["Age"] = 25,           // Generates: [age] = @age
    ["Email"] = null        // Ignored (null value)
};
await repo.FilterAsync(filter);
// Output: SELECT ... WHERE ([name] = @name AND [age] = @age)

// Single condition (no parentheses):
var filter = new Dictionary<string, object?> { ["Name"] = "John" };
// Output: SELECT ... WHERE [name] = @name

// Empty dictionary returns always-true:
var filter = new Dictionary<string, object?>();
// Output: SELECT ... WHERE 1=1
```

**Dictionary Key Matching:**
- Matches by PropertyName (e.g., `"IsActive"`)
- Matches by ColumnName (e.g., `"is_active"`)
- Case-insensitive matching
- Unknown keys are ignored

**Options:**
- `--param name` - Parameter name containing the WHERE clause string or expression result
- `--object name` - Parameter name containing `IReadOnlyDictionary<string, object?>`

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

Create custom block handlers by implementing `IBlockPlaceholderHandler`. The `ProcessBlock` method gives you full control over how to handle the block content.

#### Simple Conditional Handler

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
    
    public string ProcessBlock(string options, string blockContent, 
        IReadOnlyDictionary<string, object?>? parameters)
    {
        // Inverse of "if notnull" - include when parameter IS null
        var paramName = options.Trim();
        var value = parameters?.TryGetValue(paramName, out var v) == true ? v : null;
        return value is null ? blockContent : string.Empty;
    }
}

// Register the handler
PlaceholderProcessor.RegisterHandler(UnlessPlaceholderHandler.Instance);
```

#### Loop Handler Example

The `ProcessBlock` method enables more complex scenarios like loops:

```csharp
public class ForeachPlaceholderHandler : PlaceholderHandlerBase, IBlockPlaceholderHandler
{
    public static ForeachPlaceholderHandler Instance { get; } = new();
    
    public override string Name => "foreach";
    public string ClosingTagName => "/foreach";
    
    public override PlaceholderType GetType(string options) => PlaceholderType.Dynamic;
    
    public override string Process(PlaceholderContext context, string options)
        => throw new InvalidOperationException("{{foreach}} is dynamic");
    
    public override string Render(PlaceholderContext context, string options, 
        IReadOnlyDictionary<string, object?>? parameters) => string.Empty;
    
    public string ProcessBlock(string options, string blockContent, 
        IReadOnlyDictionary<string, object?>? parameters)
    {
        var paramName = options.Trim();
        if (parameters?.TryGetValue(paramName, out var value) != true)
            return string.Empty;
            
        if (value is not IEnumerable<object> items)
            return string.Empty;
            
        var sb = new StringBuilder();
        foreach (var item in items)
        {
            // Render blockContent for each item
            // In a real implementation, you'd substitute placeholders in blockContent
            // with values from the current item
            sb.Append(blockContent);
        }
        return sb.ToString();
    }
}

// Register the handler
PlaceholderProcessor.RegisterHandler(ForeachPlaceholderHandler.Instance);
```
