# SqlxVar - Declarative Variable Providers

## Overview

SqlxVar enables declarative variable declaration with automatic code generation for runtime SQL template variables. Mark methods with `[SqlxVar("varName")]` and use `{{var --name varName}}` in SQL templates to inject runtime values.

**Key Features:**
- ✅ Zero reflection - all method calls resolved at compile time
- ✅ AOT compatible - fully supports Native AOT compilation
- ✅ Type safe - compile-time validation of variable names and method signatures
- ✅ Minimal overhead - static methods, O(1) dispatch, single instance cast

## Quick Start

### 1. Mark Methods with [SqlxVar]

```csharp
using Sqlx.Annotations;

public partial class UserRepository
{
    [SqlxVar("tenantId")]
    private string GetTenantId() => TenantContext.Current;
    
    [SqlxVar("userId")]
    private string GetUserId() => UserContext.CurrentUserId.ToString();
    
    [SqlxVar("timestamp")]
    private static string GetTimestamp() => DateTime.UtcNow.ToString("O");
}
```

### 2. Source Generator Creates GetVar and VarProvider

The source generator automatically creates:

```csharp
public partial class UserRepository
{
    public static string GetVar(object instance, string methodName)
    {
        var repo = (UserRepository)instance;
        return methodName switch
        {
            "tenantId" => repo.GetTenantId(),
            "userId" => repo.GetUserId(),
            "timestamp" => UserRepository.GetTimestamp(),
            _ => throw new ArgumentException(
                $"Unknown variable: {methodName}. Available variables: tenantId, userId, timestamp")
        };
    }
    
    public static readonly Func<object, string, string> VarProvider = GetVar;
}
```

### 3. Use {{var}} in SQL Templates

```csharp
var repo = new UserRepository(connection);
var context = new PlaceholderContext(
    SqlDefine.SQLite,
    "users",
    UserEntityProvider.Default.Columns,
    UserRepository.VarProvider,  // Pass VarProvider
    repo);                        // Pass repository instance

var template = SqlTemplate.Prepare(
    "SELECT {{columns}} FROM {{table}} WHERE tenant_id = {{var --name tenantId}}",
    context);

var sql = template.Render(null);
// Result: SELECT "id", "name", "email" FROM "users" WHERE tenant_id = tenant-123
```

## Basic Examples

### Single Variable in WHERE Clause

```csharp
public partial class UserRepository
{
    [SqlxVar("tenantId")]
    private string GetTenantId() => TenantContext.Current;
}

// Usage
var template = SqlTemplate.Prepare(
    "SELECT * FROM users WHERE tenant_id = {{var --name tenantId}}",
    new PlaceholderContext(dialect, "users", columns, UserRepository.VarProvider, repo));

var sql = template.Render(null);
// Result: SELECT * FROM users WHERE tenant_id = tenant-123
```

### Multiple Variables

```csharp
public partial class UserRepository
{
    [SqlxVar("tenantId")]
    private string GetTenantId() => TenantContext.Current;
    
    [SqlxVar("userId")]
    private string GetUserId() => UserContext.CurrentUserId.ToString();
}

// Usage
var template = SqlTemplate.Prepare(
    @"SELECT * FROM users 
      WHERE tenant_id = {{var --name tenantId}} 
      AND user_id = {{var --name userId}}",
    new PlaceholderContext(dialect, "users", columns, UserRepository.VarProvider, repo));

var sql = template.Render(null);
// Result: SELECT * FROM users WHERE tenant_id = tenant-123 AND user_id = user-456
```

### Static vs Instance Methods

```csharp
public partial class UserRepository
{
    // Instance method - accesses repository state
    [SqlxVar("tenantId")]
    private string GetTenantId() => _tenantContext.Current;
    
    // Static method - no instance needed
    [SqlxVar("timestamp")]
    private static string GetTimestamp() => DateTime.UtcNow.ToString("O");
}
```

## Multi-Tenant Pattern

### Tenant ID Filtering

```csharp
public partial class UserRepository
{
    [SqlxVar("tenantId")]
    private string GetTenantId() => TenantContext.Current;
    
    public async Task<List<User>> GetAllAsync()
    {
        var context = new PlaceholderContext(
            SqlDefine.SQLite, "users", UserEntityProvider.Default.Columns,
            VarProvider, this);
            
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM {{table}} WHERE tenant_id = {{var --name tenantId}}",
            context);
            
        return await connection.QueryAsync<User>(template.Render(null));
    }
}
```

### Tenant-Specific Table Names

```csharp
public partial class UserRepository
{
    [SqlxVar("tenantId")]
    private string GetTenantId() => TenantContext.Current;
    
    public async Task<List<User>> GetAllAsync()
    {
        var template = SqlTemplate.Prepare(
            "SELECT {{columns}} FROM tenant_{{var --name tenantId}}_users",
            new PlaceholderContext(dialect, "users", columns, VarProvider, this));
            
        return await connection.QueryAsync<User>(template.Render(null));
    }
}
```

### Multiple Context Variables

```csharp
public partial class AuditRepository
{
    [SqlxVar("tenantId")]
    private string GetTenantId() => TenantContext.Current;
    
    [SqlxVar("userId")]
    private string GetUserId() => UserContext.CurrentUserId.ToString();
    
    [SqlxVar("timestamp")]
    private static string GetTimestamp() => DateTime.UtcNow.ToString("O");
    
    public async Task<int> InsertAuditLogAsync(AuditLog log)
    {
        var template = SqlTemplate.Prepare(
            @"INSERT INTO {{table}} (tenant_id, user_id, created_at, {{columns --exclude Id TenantId UserId CreatedAt}})
              VALUES ({{var --name tenantId}}, {{var --name userId}}, {{var --name timestamp}}, {{values --exclude Id TenantId UserId CreatedAt}})",
            new PlaceholderContext(dialect, "audit_logs", columns, VarProvider, this));
            
        return await connection.ExecuteAsync(template.Render(null), log);
    }
}
```

## Security Considerations

### ⚠️ SQL Injection Warning

**IMPORTANT:** `{{var}}` placeholders insert values as **literals** directly into SQL. Only use for trusted, application-controlled values.

### ✅ Safe Uses

```csharp
// ✅ SAFE: Application-controlled tenant ID from authentication
[SqlxVar("tenantId")]
private string GetTenantId() => TenantContext.Current; // From auth token

// ✅ SAFE: Validated enum value
[SqlxVar("status")]
private string GetStatus() => OrderStatus.Pending.ToString(); // Enum

// ✅ SAFE: SQL function/keyword
[SqlxVar("timestamp")]
private static string GetTimestamp() => "CURRENT_TIMESTAMP"; // SQL keyword
```

### ❌ Unsafe Uses

```csharp
// ❌ DANGEROUS: User input as literal - SQL INJECTION RISK!
[SqlxVar("searchTerm")]
private string GetSearchTerm() => HttpContext.Request.Query["search"]; // NEVER DO THIS!

// ✅ CORRECT: Use SQL parameters for user input
var template = SqlTemplate.Prepare(
    "SELECT * FROM users WHERE name LIKE @searchTerm",
    context);
var sql = template.Render(null);
// Then bind @searchTerm as a parameter
```

### Input Validation

Always validate values before returning:

```csharp
[SqlxVar("tenantId")]
private string GetTenantId()
{
    var tenantId = TenantContext.Current;
    
    // Validate format (GUID, alphanumeric, etc.)
    if (!Regex.IsMatch(tenantId, @"^[a-zA-Z0-9\-]+$"))
        throw new SecurityException("Invalid tenant ID format");
    
    return tenantId;
}
```

### When to Use Variables vs Parameters

| Use `{{var}}` for | Use SQL Parameters for |
|-------------------|------------------------|
| Application-controlled identifiers (tenant IDs) | User input (search terms, filters) |
| SQL keywords and functions (`CURRENT_TIMESTAMP`) | Dynamic values that change frequently |
| Table/column name fragments | Any untrusted data |
| Validated enum values | Form data |

## Method Requirements

### Return Type

Methods marked with `[SqlxVar]` **must** return `string`:

```csharp
// ✅ Valid
[SqlxVar("tenantId")]
private string GetTenantId() => "tenant-123";

// ✅ Any return type (user handles conversion)\n[SqlxVar("count")]\nprivate string GetCount() => 42.ToString();
```

### Parameters

Methods marked with `[SqlxVar]` **must** have zero parameters:

```csharp
// ✅ Valid
[SqlxVar("tenantId")]
private string GetTenantId() => TenantContext.Current;

// ❌ Invalid - compile error SQLX1003
[SqlxVar("tenantId")]
private string GetTenantId(string param) => param;
```

### Variable Names

Variable names must be unique within a class:

```csharp
// ❌ Invalid - compile error SQLX1001
[SqlxVar("tenantId")]
private string GetTenantId() => "tenant-123";

[SqlxVar("tenantId")]  // Duplicate!
private string GetTenantIdAgain() => "tenant-456";
```

## Best Practices

### 1. Keep Methods Simple

Variable provider methods should be fast and simple:

```csharp
// ✅ Good: Simple, fast
[SqlxVar("tenantId")]
private string GetTenantId() => _tenantContext.Current;

// ❌ Bad: Complex logic, I/O operations
[SqlxVar("tenantId")]
private async Task<string> GetTenantIdAsync()
{
    return await _database.GetTenantIdAsync(); // Don't do this!
}
```

### 2. Use Descriptive Names

```csharp
// ✅ Good: Clear, descriptive
[SqlxVar("tenantId")]
private string GetTenantId() => ...;

// ❌ Bad: Unclear, abbreviated
[SqlxVar("t")]
private string GetT() => ...;
```

### 3. Use Static Methods for Global Values

```csharp
// Static for global values
[SqlxVar("timestamp")]
private static string GetTimestamp() => DateTime.UtcNow.ToString("O");

// Instance for context-dependent values
[SqlxVar("tenantId")]
private string GetTenantId() => _tenantContext.Current;
```

### 4. Cache Values If Needed

```csharp
private string? _cachedTenantId;

[SqlxVar("tenantId")]
private string GetTenantId()
{
    return _cachedTenantId ??= TenantContext.Current;
}
```

## Error Handling

### Unknown Variable Name

```csharp
var ex = Assert.ThrowsException<ArgumentException>(
    () => template.Render(null));
// Message: "Unknown variable: unknownVar. Available variables: tenantId, userId, timestamp"
```

### Missing VarProvider

```csharp
var context = new PlaceholderContext(dialect, "users", columns, null, null);
var ex = Assert.ThrowsException<InvalidOperationException>(
    () => template.Render(null));
// Message: "VarProvider not configured for variable: tenantId"
```

### Exception from Variable Method

Exceptions from variable methods are propagated:

```csharp
[SqlxVar("tenantId")]
private string GetTenantId()
{
    if (TenantContext.Current == null)
        throw new InvalidOperationException("Tenant context not set");
    return TenantContext.Current;
}

// Exception propagates with original message
```

## Testing

### Unit Testing Variable Methods

```csharp
[Test]
public void GetVar_ReturnsCurrentTenantId()
{
    // Arrange
    TenantContext.Current = "test-tenant";
    var repo = new UserRepository(connection);
    
    // Act
    var result = UserRepository.GetVar(repo, "tenantId");
    
    // Assert
    Assert.AreEqual("test-tenant", result);
}
```

### Integration Testing

```csharp
[Test]
public async Task GetAllForTenantAsync_FiltersByTenant()
{
    // Arrange
    TenantContext.Current = "tenant-1";
    var repo = new UserRepository(connection);
    
    // Act
    var users = await repo.GetAllForTenantAsync();
    
    // Assert
    Assert.All(users, u => Assert.Equal("tenant-1", u.TenantId));
}
```

### Mocking in Tests

```csharp
[Test]
public void TestWithMockedContext()
{
    // Mock the context
    var mockContext = new Mock<ITenantContext>();
    mockContext.Setup(x => x.Current).Returns("test-tenant");
    
    // Inject mock
    var repo = new UserRepository(connection, mockContext.Object);
    
    // Test
    var result = UserRepository.GetVar(repo, "tenantId");
    Assert.Equal("test-tenant", result);
}
```

## Performance

### Overhead

SqlxVar has minimal performance overhead:

| Operation | Time | Notes |
|-----------|------|-------|
| GetVar dispatch | < 10 ns | Switch statement on string |
| Method invocation | Varies | User-defined logic |
| Value insertion | < 20 ns | String concatenation |
| **Total overhead** | **< 50 ns** | Negligible vs SQL execution |

### Optimization

The generated code is highly optimized:

```csharp
// Single instance cast (not per variable)
var repo = (UserRepository)instance;

// O(1) or O(log n) switch dispatch
return methodName switch
{
    "tenantId" => repo.GetTenantId(),  // Direct method call
    "userId" => repo.GetUserId(),
    _ => throw new ArgumentException(...)
};
```

## Troubleshooting

### Compile Error: SQLX1001

**Problem:** Duplicate variable name

```csharp
[SqlxVar("tenantId")]
private string GetTenantId() => "tenant-123";

[SqlxVar("tenantId")]  // Error: Variable name 'tenantId' is already defined
private string GetTenantIdAgain() => "tenant-456";
```

**Solution:** Use unique variable names

### Compile Error: SQLX1003

**Problem:** Method has parameters

```csharp
[SqlxVar("tenantId")]
private string GetTenantId(string param) => param;  // Error: Must have zero parameters
```

**Solution:** Remove parameters, use instance fields instead

```csharp
private readonly string _tenantId;

[SqlxVar("tenantId")]
private string GetTenantId() => _tenantId;
```

### Runtime Error: VarProvider not configured

**Problem:** PlaceholderContext created without VarProvider

```csharp
var context = new PlaceholderContext(dialect, "users", columns);  // Missing VarProvider
```

**Solution:** Pass VarProvider and instance

```csharp
var context = new PlaceholderContext(
    dialect, "users", columns,
    UserRepository.VarProvider,  // Add VarProvider
    repo);                        // Add instance
```

## See Also

- [SQL Templates](sql-templates.md) - Complete SQL template guide
- [Placeholders](placeholders.md) - All available placeholders
- [Source Generators](source-generators.md) - How source generation works
- [Multi-Tenancy Patterns](multi-tenancy.md) - Multi-tenant architecture patterns

## Limitations

### ICrudRepository Methods

**Important:** Built-in `ICrudRepository` methods (like `GetAllAsync`, `GetByIdAsync`, etc.) do **not** support `{{var}}` placeholders because they use static SQL templates generated at compile time.

**Workaround 1: Define Custom Methods**

Instead of using `GetAllAsync()`, define a custom method:

```csharp
public interface IUserRepository : ICrudRepository<User, long>
{
    // Custom method with {{var}} support
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE tenant_id = {{var --name tenantId}}")]
    Task<List<User>> GetAllForTenantAsync(CancellationToken ct = default);
}
```

**Workaround 2: Use SqlQuery with Manual Context**

```csharp
public async Task<List<User>> GetAllForTenantAsync()
{
    var context = new PlaceholderContext(
        SqlDefine.SQLite,
        "users",
        UserEntityProvider.Default.Columns,
        VarProvider,
        this);
    
    var template = SqlTemplate.Prepare(
        "SELECT {{columns}} FROM {{table}} WHERE tenant_id = {{var --name tenantId}}",
        context);
    
    var sql = template.Render(null);
    return await _connection.QueryAsync<User>(sql, UserResultReader.Default);
}
```

**Why This Limitation Exists:**

ICrudRepository methods are generated with static `PlaceholderContext` for performance. Adding VarProvider support would require:
- Runtime context creation (performance overhead)
- Breaking change to existing code
- Complexity in source generator

For most use cases, defining custom methods with `[SqlTemplate]` is the recommended approach.


