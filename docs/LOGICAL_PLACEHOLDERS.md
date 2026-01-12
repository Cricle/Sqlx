# Logical Placeholders

Logical placeholders allow you to conditionally include SQL fragments based on parameter values. They start with `*` to indicate they are control flow operators, not SQL generators.

## Supported Logical Placeholders

### `{{*ifnull paramName}}...{{/ifnull}}`
Includes the content if the parameter is `null`.

```csharp
[SqlTemplate("SELECT * FROM users {{*ifnull filter}}WHERE 1=1{{/ifnull}}")]
Task<List<User>> GetUsersAsync(Expression<Func<User, bool>>? filter = null);
```

### `{{*ifnotnull paramName}}...{{/ifnotnull}}`
Includes the content if the parameter is NOT `null`.

```csharp
[SqlTemplate("SELECT * FROM users {{*ifnotnull orderBy}}ORDER BY {{orderBy}}{{/ifnotnull}}")]
Task<List<User>> GetUsersAsync(string? orderBy = null);
```

### `{{*ifempty paramName}}...{{/ifempty}}`
Includes the content if the parameter is empty (null, empty string, or empty collection).

```csharp
[SqlTemplate("SELECT * FROM users {{*ifempty search}}WHERE 1=1{{/ifempty}}")]
Task<List<User>> SearchAsync(string? search = null);
```

### `{{*ifnotempty paramName}}...{{/ifnotempty}}`
Includes the content if the parameter is NOT empty.

```csharp
[SqlTemplate("SELECT * FROM users {{*ifnotempty ids}}WHERE id IN {{values --param ids}}{{/ifnotempty}}")]
Task<List<User>> GetByIdsAsync(List<int>? ids = null);
```

## Common Use Cases

### Dynamic ORDER BY

```csharp
public interface IUserRepository
{
    [SqlTemplate(@"
        SELECT * FROM users 
        WHERE is_active = true
        {{*ifnotnull orderBy}}ORDER BY {{orderBy}}{{/ifnotnull}}
        {{*ifnull orderBy}}ORDER BY created_at DESC{{/ifnull}}
    ")]
    Task<List<User>> GetActiveUsersAsync(string? orderBy = null);
}
```

### Optional Search Filter

```csharp
public interface IProductRepository
{
    [SqlTemplate(@"
        SELECT * FROM products 
        WHERE 1=1
        {{*ifnotempty search}}AND name LIKE @search{{/ifnotempty}}
        {{*ifnotnull minPrice}}AND price >= @minPrice{{/ifnotnull}}
        {{*ifnotnull maxPrice}}AND price <= @maxPrice{{/ifnotnull}}
    ")]
    Task<List<Product>> SearchAsync(
        string? search = null, 
        decimal? minPrice = null, 
        decimal? maxPrice = null);
}
```

### Conditional LIMIT

```csharp
public interface ILogRepository
{
    [SqlTemplate(@"
        SELECT * FROM logs 
        WHERE level = @level
        ORDER BY created_at DESC
        {{*ifnotnull limit}}LIMIT @limit{{/ifnotnull}}
    ")]
    Task<List<Log>> GetLogsAsync(string level, int? limit = null);
}
```

### Complex Dynamic Query

```csharp
public interface IOrderRepository
{
    [SqlTemplate(@"
        SELECT * FROM orders 
        WHERE 1=1
        {{*ifnotnull customerId}}AND customer_id = @customerId{{/ifnotnull}}
        {{*ifnotnull status}}AND status = @status{{/ifnotnull}}
        {{*ifnotnull startDate}}AND created_at >= @startDate{{/ifnotnull}}
        {{*ifnotnull endDate}}AND created_at <= @endDate{{/ifnotnull}}
        {{*ifnotnull orderBy}}ORDER BY {{orderBy}}{{/ifnotnull}}
        {{*ifnull orderBy}}ORDER BY created_at DESC{{/ifnull}}
        {{*ifnotnull limit}}LIMIT @limit{{/ifnotnull}}
        {{*ifnotnull offset}}OFFSET @offset{{/ifnotnull}}
    ")]
    Task<List<Order>> SearchOrdersAsync(
        int? customerId = null,
        string? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? orderBy = null,
        int? limit = null,
        int? offset = null);
}
```

## How It Works

1. **Compile-Time Processing**: Logical placeholders are processed during code generation
2. **Runtime Evaluation**: Generated code includes conditional checks (e.g., `if (param != null)`)
3. **Zero Overhead**: No string replacement at runtime - conditions are evaluated once
4. **Type-Aware**: Different checks for strings (empty), collections (Any()), and nullable types

## Extensibility

The logical placeholder system is designed to be extensible. New logical conditions can be added by:

1. Adding a new entry to the `logicalGenerators` dictionary in `SharedCodeGenerationUtilities.cs`
2. Implementing a generator function that returns the appropriate condition code
3. Updating the regex pattern to recognize the new placeholder type

Example of adding a custom condition:

```csharp
// In SharedCodeGenerationUtilities.cs
var logicalGenerators = new Dictionary<string, Func<string, IMethodSymbol, IndentedStringBuilder, string>>
{
    ["IFNULL"] = GenerateIfNullCondition,
    ["IFNOTNULL"] = GenerateIfNotNullCondition,
    ["IFEMPTY"] = GenerateIfEmptyCondition,
    ["IFNOTEMPTY"] = GenerateIfNotEmptyCondition,
    // Add your custom condition here
    ["IFPOSITIVE"] = GenerateIfPositiveCondition  // Example: check if number > 0
};
```

## Best Practices

1. **Use Descriptive Parameter Names**: Makes templates more readable
2. **Provide Sensible Defaults**: Use `{{*ifnull}}` blocks for default behavior
3. **Keep Logic Simple**: Complex conditions should be in C# code, not SQL templates
4. **Test Edge Cases**: Ensure your queries work with all combinations of null/non-null parameters
5. **Document Complex Queries**: Add XML comments explaining the dynamic behavior

## Performance

- **No Runtime Overhead**: Conditions are evaluated once during SQL building
- **Efficient String Building**: Uses `StringBuilder` and string interpolation
- **Minimal Allocations**: Reuses variables where possible
- **Compile-Time Optimization**: SQL structure is determined at code generation time
