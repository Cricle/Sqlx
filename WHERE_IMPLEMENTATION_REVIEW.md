# WHERE Condition Implementation Review

## Overview
This document provides a comprehensive review of WHERE clause implementation in Sqlx, covering expression parsing, SQL generation, and code generation.

---

## 1. Core WHERE Implementation (`ExpressionToSql<T>`)

### 1.1 WHERE Method Entry Point
**File**: `src/Sqlx/ExpressionToSql.cs:119-124`

```csharp
/// <summary>Adds WHERE condition</summary>
public ExpressionToSql<T> Where(Expression<Func<T, bool>> predicate)
{
    if (predicate != null) _whereConditions.Add($"({ParseExpression(predicate.Body)})");
    return this;
}
```

**Key Points**:
- ✅ Accepts `Expression<Func<T, bool>>` for type-safe predicates
- ✅ Each condition wrapped in parentheses for safe AND combination
- ✅ Fluent API pattern (returns `this`)
- ✅ Null-safe (checks `predicate != null`)

**Usage Example**:
```csharp
var sql = ExpressionToSql<User>.ForSqlServer()
    .Where(u => u.Age > 18)
    .Where(u => u.IsActive)
    .ToSql();
// Generates: SELECT * FROM users WHERE (Age > 18) AND (IsActive = 1)
```

---

## 2. Expression Parsing (`ExpressionToSqlBase`)

### 2.1 ParseExpression Method
**File**: `src/Sqlx/ExpressionToSqlBase.cs:71-84`

```csharp
protected string ParseExpression(Expression expression, bool treatBoolAsComparison = true) => expression switch
{
    BinaryExpression binary => ParseBinaryExpression(binary),
    MemberExpression member when treatBoolAsComparison && member.Type == typeof(bool)
        => $"{GetColumnName(member)} = 1",
    MemberExpression member when IsStringPropertyAccess(member) => ParseStringProperty(member),
    MemberExpression member when IsEntityProperty(member) => GetColumnName(member),
    MemberExpression member => FormatConstantValue(GetMemberValueOptimized(member)),
    ConstantExpression constant => FormatConstantValue(constant.Value),
    UnaryExpression { NodeType: ExpressionType.Not } unary => ParseNotExpression(unary.Operand),
    UnaryExpression { NodeType: ExpressionType.Convert } unary => ParseExpression(unary.Operand),
    MethodCallExpression method => ParseMethodCallExpression(method),
    ConditionalExpression conditional => /* CASE WHEN ... */,
    _ => "1=1",
};
```

**Supported Expression Types**:

1. **Binary Expressions** (`Age > 18`, `Name == "John"`)
   - Comparison: `=`, `<>`, `>`, `>=`, `<`, `<=`
   - Logical: `AND`, `OR`
   - Arithmetic: `+`, `-`, `*`, `/`

2. **Member Access** (`u.IsActive`)
   - Boolean properties auto-converted to `= 1`
   - String properties support `.Length`
   - Entity properties mapped to column names

3. **Method Calls**
   - String methods: `Contains()`, `StartsWith()`, `EndsWith()`
   - Math methods: `Abs()`, `Round()`, `Ceiling()`, `Floor()`
   - DateTime methods: `AddDays()`, `AddMonths()`, etc.

4. **Unary Expressions** (`!u.IsDeleted`)
   - NOT operator supported
   - Type conversions handled

5. **Conditional** (`condition ? value1 : value2`)
   - Translated to `CASE WHEN ... THEN ... ELSE ... END`

---

## 3. WHERE Clause Building

### 3.1 GetWhereClause Method
**File**: `src/Sqlx/ExpressionToSql.cs:306`

```csharp
private string GetWhereClause() =>
    _whereConditions.Count > 0
        ? $" WHERE {string.Join(" AND ", _whereConditions.Select(RemoveOuterParentheses))}"
        : "";
```

**Key Points**:
- ✅ Multiple conditions combined with `AND`
- ✅ Outer parentheses removed to avoid: `WHERE ((Age > 18)) AND ((IsActive = 1))`
- ✅ Result: `WHERE Age > 18 AND IsActive = 1`
- ✅ Empty string if no conditions (no unnecessary WHERE clause)

### 3.2 RemoveOuterParentheses
**File**: `src/Sqlx/ExpressionToSqlBase.cs:245`

```csharp
protected static string RemoveOuterParentheses(string condition) =>
    condition.StartsWith("(") && condition.EndsWith(")")
        ? condition.Substring(1, condition.Length - 2)
        : condition;
```

**Performance Optimization**: Avoids regex, uses simple string operations.

---

## 4. Template Engine WHERE Placeholder

### 4.1 ProcessWherePlaceholder
**File**: `src/Sqlx.Generator/Core/SqlTemplateEngine.cs:376-384`

```csharp
private string ProcessWherePlaceholder(string type, INamedTypeSymbol? entityType,
    IMethodSymbol method, string options, SqlDefine dialect)
{
    return type switch
    {
        "id" => $"id = {dialect.ParameterPrefix}id",
        "auto" => GenerateAutoWhereClause(method, dialect),
        _ => "1=1"
    };
}
```

**Supported Placeholder Types**:

1. **`{{where id}}`** → `id = @id` (SQL Server) / `id = ?` (MySQL)
2. **`{{where auto}}`** → Auto-generates from method parameters
3. **`{{where}}`** → Defaults to `1=1` (allows dynamic WHERE from ExpressionToSql)

**Usage in Templates**:
```csharp
[Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where id}}")]
Task<User?> GetByIdAsync(int id);

// Generates:
// SELECT id, name, email FROM users WHERE id = @id
```

---

## 5. Batch WHERE Merging

### 5.1 WhereFrom Method
**File**: `src/Sqlx/ExpressionToSqlBase.cs:408-418`

```csharp
public virtual ExpressionToSqlBase WhereFrom(ExpressionToSqlBase expression)
{
    if (expression == null)
        throw new ArgumentNullException(nameof(expression));

    // Merge WHERE conditions
    foreach (var condition in expression._whereConditions)
    {
        _whereConditions.Add(condition);
    }

    // Merge parameters
    foreach (var param in expression._parameters)
    {
        _parameters[param.Key] = param.Value;
    }

    return this;
}
```

**Use Case**: Batch operations with dynamic WHERE from `ExpressionToSql`

```csharp
var whereExpr = ExpressionToSql<User>.ForSqlServer()
    .Where(u => u.Age > 18)
    .Where(u => u.IsActive);

await repo.BatchDeleteAsync(whereExpr);
// DELETE FROM users WHERE Age > 18 AND IsActive = 1
```

---

## 6. Code Generation for WHERE

### 6.1 Generated Code with WHERE Parameter
When a method has `[ExpressionToSql]` parameter, the generator produces:

```csharp
// Source:
[Sqlx("DELETE FROM {{table}} WHERE {{where}}")]
Task<int> BatchDeleteAsync([ExpressionToSql] ExpressionToSqlBase<User> whereExpression);

// Generated:
public async Task<int> BatchDeleteAsync(ExpressionToSqlBase<User> whereExpression)
{
    using var command = _connection.CreateCommand();

    // Extract WHERE clause
    var whereClause = whereExpression.ToWhereClause();
    var sql = $"DELETE FROM users WHERE {whereClause}";

    command.CommandText = sql;

    // Add parameters
    foreach (var param in whereExpression.GetParameters())
    {
        var p = command.CreateParameter();
        p.ParameterName = param.Key;
        p.Value = param.Value ?? DBNull.Value;
        command.Parameters.Add(p);
    }

    return await command.ExecuteNonQueryAsync();
}
```

---

## 7. Special WHERE Features

### 7.1 Boolean Property Handling

**Input**: `Where(u => u.IsActive)`

**Different Dialects**:
- SQL Server: `IsActive = 1`
- MySQL: `IsActive = 1`
- PostgreSQL: `IsActive = true` (if using boolean type)

**File**: `src/Sqlx/ExpressionToSqlBase.cs:74`

### 7.2 String Operations

**Supported**:
```csharp
Where(u => u.Name.Contains("John"))     // LIKE '%John%'
Where(u => u.Name.StartsWith("J"))      // LIKE 'J%'
Where(u => u.Name.EndsWith("son"))      // LIKE '%son'
Where(u => u.Name.Length > 10)          // LEN(Name) > 10 (SQL Server)
```

### 7.3 Null Handling

```csharp
Where(u => u.Email == null)             // Email IS NULL
Where(u => u.Email != null)             // Email IS NOT NULL
Where(u => u.Email == "test@example.com" || u.Email == null)
// Email = 'test@example.com' OR Email IS NULL
```

**File**: `src/Sqlx/ExpressionToSqlBase.cs:194-198` (GetBinaryOperatorSql)

---

## 8. Performance Optimizations

### 8.1 Condition Storage
✅ Uses `List<string>` instead of rebuilding SQL each time
✅ Conditions cached in `_whereConditions` field

### 8.2 String Building
✅ `GetWhereClause()` only builds when needed
✅ Avoids unnecessary string allocations
✅ Uses `string.Join()` for efficient concatenation

### 8.3 Expression Parsing
✅ Pattern matching (`switch` expressions) for performance
✅ No reflection in hot path
✅ Static method caching where possible

---

## 9. Security Considerations

### 9.1 SQL Injection Prevention

**Inline Mode** (Default):
```csharp
Where(u => u.Age > 18)  // Age > 18 (safe, constant value)
```

**Parameterized Mode**:
```csharp
var sql = ExpressionToSql<User>.ForSqlServer()
    .UseParameterizedQueries()
    .Where(u => u.Age > Any.Int("age"))
    .ToTemplate();
// WHERE Age > @age (safe, parameterized)
```

### 9.2 Dynamic SQL with Validation

For dynamic WHERE clauses (e.g., user input):
```csharp
// BAD - Potential SQL injection
var tableName = userInput;
[Sqlx($"SELECT * FROM {tableName} WHERE id = @id")]

// GOOD - Use validation
[Sqlx("SELECT * FROM {{@tableName}} WHERE id = @id")]
Task<User?> GetAsync([DynamicSql(Type = DynamicSqlType.Identifier)] string tableName, int id);
```

**File**: `src/Sqlx/Validation/SqlValidator.cs` provides runtime validation.

---

## 10. Issues and Recommendations

### ✅ Strengths

1. **Type Safety**: LINQ expressions provide compile-time type checking
2. **Dialect Support**: WHERE generation adapts to SQL dialect
3. **Fluent API**: Easy to chain multiple conditions
4. **Null Safety**: Proper NULL handling in comparisons
5. **Performance**: Optimized parsing with minimal allocations

### ⚠️ Potential Issues

1. **Complex OR Conditions**:
   ```csharp
   // Current: Requires multiple Where() calls combined with AND
   Where(u => u.Age > 18).Where(u => u.IsActive)  // AND

   // Missing: No built-in OR() method
   // Workaround: Use single Where() with || operator
   Where(u => u.Age > 18 || u.Country == "US")  // Works, but less fluent
   ```

2. **Sub-query Support**:
   ```csharp
   // Not supported:
   Where(u => someList.Contains(u.Id))  // Would require IN (SELECT ...)

   // Current workaround: Use string interpolation or custom SQL
   ```

3. **Dynamic Column Names**:
   ```csharp
   // Not supported:
   var columnName = "Age";
   Where(/* dynamically access columnName */)  // Requires reflection
   ```

4. **`{{where}}` Placeholder Limitation**:
   - **File**: `src/Sqlx.Generator/Core/SqlTemplateEngine.cs:376-384`
   - Currently only supports: `{{where id}}`, `{{where auto}}`, `{{where}}`
   - **Issue**: No way to specify custom WHERE template like `{{where @whereClause}}`
   - **Recommendation**: Add support for `[DynamicSql]` WHERE parameter injection

### 💡 Recommendations

1. **Add OR() Method**:
   ```csharp
   public ExpressionToSql<T> Or(Expression<Func<T, bool>> predicate)
   {
       if (_whereConditions.Count > 0)
       {
           var lastCondition = _whereConditions[^1];
           _whereConditions[^1] = $"({lastCondition} OR {ParseExpression(predicate.Body)})";
       }
       else
       {
           Where(predicate);
       }
       return this;
   }
   ```

2. **Add WhereIn() Method**:
   ```csharp
   public ExpressionToSql<T> WhereIn<TValue>(
       Expression<Func<T, TValue>> selector,
       IEnumerable<TValue> values)
   {
       var column = GetColumnName(selector.Body);
       var valuesList = string.Join(", ", values.Select(FormatConstantValue));
       _whereConditions.Add($"{column} IN ({valuesList})");
       return this;
   }
   ```

3. **Enhance `{{where}}` Placeholder**:
   ```csharp
   // Support dynamic WHERE from parameter
   [Sqlx("SELECT {{columns}} FROM {{table}} WHERE {{where @customWhere}}")]
   Task<List<User>> SearchAsync([DynamicSql(Type = DynamicSqlType.Fragment)] string customWhere);
   ```

4. **Add WHERE Condition Builder Helper**:
   ```csharp
   // For complex dynamic queries
   public class WhereBuilder<T>
   {
       public WhereBuilder<T> Add(string column, string op, object value) { /*...*/ }
       public WhereBuilder<T> AddIf(bool condition, /* ... */) { /*...*/ }
       public string Build() { /*...*/ }
   }
   ```

---

## 11. Testing Coverage

### Current Tests (Estimated):
- ✅ Basic WHERE conditions: `Age > 18`, `Name == "John"`
- ✅ Boolean properties: `IsActive`
- ✅ String methods: `Contains()`, `StartsWith()`, `EndsWith()`
- ✅ Null comparisons: `== null`, `!= null`
- ✅ AND combinations
- ✅ Dialect-specific SQL generation

### Missing Tests:
- ⚠️ Complex nested OR/AND: `(A || B) && (C || D)`
- ⚠️ Very long WHERE chains (100+ conditions)
- ⚠️ WHERE with all supported method calls
- ⚠️ Batch WHERE merging with parameter conflicts
- ⚠️ Unicode/special characters in WHERE values

---

## 12. Conclusion

### Overall Assessment: ⭐⭐⭐⭐½ (4.5/5)

**Strengths**:
- Robust expression parsing
- Excellent type safety
- Good performance optimizations
- Multi-dialect support
- Clean, maintainable code

**Areas for Improvement**:
- Add OR() fluent method
- Enhance `{{where}}` placeholder flexibility
- Add WhereIn() for collections
- Improve complex sub-query support

The WHERE implementation is **production-ready** for most use cases, with clear extension points for advanced scenarios.

