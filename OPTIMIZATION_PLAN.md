# Sqlx Optimization Plan - Performance First

## 🎯 Goals
1. **Performance**: Extend string interpolation to all dynamic placeholders
2. **Correctness**: Add comprehensive source analyzers
3. **Usability**: Support transaction parameters
4. **Validation**: Complete benchmark suite

**Breaking Changes**: ✅ Allowed for better design

---

## Phase 1: String Interpolation Optimization (Week 1)

### 🎯 Objective
Eliminate ALL `string.Replace()` calls in generated code for dynamic placeholders.

### 📋 Tasks

#### 1.1 Optimize SET Placeholder
**File**: `src/Sqlx.Generator/Core/SqlTemplateEngine.cs`

**Current**:
```csharp
"{{set}}" → "name = @name, age = @age"
```

**Issue**: May generate Replace-based code if dynamic

**Solution**: Apply same pattern as WHERE
```csharp
// Generated code (optimized)
var __setClause_0__ = BuildSetClause(entity); // or from parameter
__cmd__.CommandText = $@"UPDATE users SET {__setClause_0__} WHERE id = @id";
```

**Changes**:
- Detect `[ExpressionToSql]` parameter for dynamic SET
- Support `{{set @customSet}}` with `[DynamicSql]`
- Generate compile-time split + interpolation code

---

#### 1.2 Optimize ORDERBY Placeholder
**Current**:
```csharp
"{{orderby created_at --desc}}" → "ORDER BY created_at DESC"
```

**Issue**: Static is fine, but dynamic ordering uses Replace

**Solution**: Support dynamic ordering
```csharp
// New syntax
[Sqlx("SELECT {{columns}} FROM {{table}} {{orderby @dynamicOrder}}")]
Task<List<User>> GetSortedAsync([DynamicSql] string dynamicOrder);

// Usage
await repo.GetSortedAsync("age DESC, name ASC");

// Generated (optimized)
var __orderClause_0__ = ValidateOrderBy(dynamicOrder);
__cmd__.CommandText = $@"SELECT * FROM users {__orderClause_0__}";
```

**Validation**: Whitelist approach
- Allow: column names (alphanumeric + underscore)
- Allow: ASC, DESC, NULLS FIRST, NULLS LAST
- Block: everything else

---

#### 1.3 Optimize JOIN Placeholder
**Current**:
```csharp
"{{join users on users.id = orders.user_id}}" → "JOIN users ON ..."
```

**Issue**: Complex, may use Replace

**Solution**: 
```csharp
// Support dynamic JOIN
[Sqlx(@"SELECT {{columns}} FROM {{table}} 
        {{join @joinClause}} 
        WHERE {{where}}")]
Task<List<Order>> GetWithJoinAsync(
    [DynamicSql(Type = Fragment)] string joinClause,
    [ExpressionToSql] ExpressionToSqlBase where);

// Generated
var __joinClause_0__ = ValidateJoin(joinClause);
var __whereClause_1__ = where?.ToWhereClause() ?? "1=1";
__cmd__.CommandText = $@"SELECT * FROM orders {__joinClause_0__ WHERE {__whereClause_1__}";
```

**Validation**: Relaxed but safe
- Allow: JOIN, INNER JOIN, LEFT JOIN, RIGHT JOIN, FULL JOIN, CROSS JOIN
- Allow: ON conditions with = operators
- Block: subqueries, UNION, dangerous keywords

---

#### 1.4 Optimize GROUPBY Placeholder
**Similar pattern to ORDERBY**

```csharp
{{groupby @columns}}
// Generated: $@"... GROUP BY {__groupByClause_0__}"
```

---

#### 1.5 Optimize LIMIT/OFFSET Placeholder
**Current**: May use Replace for dynamic pagination

**Solution**:
```csharp
// Support parameter-based limit
[Sqlx("SELECT {{columns}} FROM {{table}} LIMIT @limit OFFSET @offset")]
Task<List<User>> GetPageAsync(int limit, int offset);

// No need for special handling - already parameterized!
```

**Note**: This is already optimal if using parameters. Only optimize if template uses dynamic placeholders.

---

### 📊 Expected Results
- **Performance**: 30-50% fewer allocations for UPDATE/SELECT with dynamic clauses
- **Code Quality**: All generated SQL uses string interpolation
- **Consistency**: Same pattern across all placeholders

---

## Phase 2: Transaction Parameter Support (Week 1)

### 🎯 Objective
Allow passing `IDbTransaction` as method parameter for transactional operations.

### 📋 Implementation

#### 2.1 Add Transaction Parameter Detection
**File**: `src/Sqlx.Generator/MethodGenerationContext.cs`

```csharp
// Add to MethodGenerationContext
internal IParameterSymbol? TransactionParameter { get; }

// In constructor
TransactionParameter = GetParameter(methodSymbol, x => x.Type.Name == "IDbTransaction" || 
                                                       x.Type.AllInterfaces.Any(i => i.Name == "IDbTransaction"));
```

#### 2.2 Modify Command Creation
**File**: `src/Sqlx.Generator/Core/SharedCodeGenerationUtilities.cs`

```csharp
// Current
__cmd__ = connection.CreateCommand();

// New
if (transactionParam != null)
{
    __cmd__ = connection.CreateCommand();
    __cmd__.Transaction = transactionParam;
}
else
{
    __cmd__ = connection.CreateCommand();
}
```

#### 2.3 Usage Example
```csharp
public interface IUserRepository
{
    // Without transaction (auto-commit)
    [Sqlx("UPDATE {{table}} SET status = @status WHERE id = @id")]
    Task<int> UpdateStatusAsync(int id, string status);
    
    // With transaction parameter
    [Sqlx("UPDATE {{table}} SET status = @status WHERE id = @id")]
    Task<int> UpdateStatusAsync(int id, string status, IDbTransaction transaction);
    
    // Batch operation with transaction
    [Sqlx("DELETE FROM {{table}} WHERE {{where}}")]
    Task<int> BatchDeleteAsync([ExpressionToSql] ExpressionToSqlBase where, IDbTransaction transaction);
}

// Usage
using var transaction = connection.BeginTransaction();
try
{
    await repo.UpdateStatusAsync(1, "active", transaction);
    await repo.UpdateStatusAsync(2, "inactive", transaction);
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

#### 2.4 Breaking Change
**Old**: Transaction not supported in generated methods  
**New**: Can pass `IDbTransaction` parameter

**Migration**: No breaking change - this is purely additive.

---

## Phase 3: Source Analyzers (Week 2)

### 🎯 Objective
Add compile-time validation for common SQL template errors.

### 📋 Analyzers to Implement

#### 3.1 SQLX001: Unused Parameter Analyzer
**Detects**: Method parameters not referenced in SQL template

```csharp
// ❌ Warning SQLX001
[Sqlx("SELECT * FROM users WHERE id = @id")]
Task<User?> GetAsync(int id, string name); // 'name' not used

// ✅ OK
[Sqlx("SELECT * FROM users WHERE id = @id")]
Task<User?> GetAsync(int id);
```

**Implementation**:
1. Parse SQL template for `@paramName` references
2. Compare with method parameters
3. Report diagnostic for unused parameters
4. **Code Fix**: Remove unused parameter

---

#### 3.2 SQLX002: Missing Parameter Analyzer
**Detects**: SQL references parameter not in method signature

```csharp
// ❌ Error SQLX002
[Sqlx("SELECT * FROM users WHERE email = @email")]
Task<User?> GetAsync(int id); // Missing '@email' parameter

// ✅ OK
[Sqlx("SELECT * FROM users WHERE email = @email")]
Task<User?> GetAsync(int id, string email);
```

**Code Fix**: Add missing parameter with inferred type

---

#### 3.3 SQLX003: Parameter Type Mismatch Analyzer
**Detects**: SQL parameter type doesn't match method parameter type

```csharp
// ❌ Warning SQLX003
[Sqlx("SELECT * FROM users WHERE age > @age")]
Task<List<User>> GetByAgeAsync(string age); // age should be int/long

// Heuristics:
// - Numeric operators (>, <, >=, <=, +, -, *, /) → expect numeric type
// - LIKE operator → expect string type
// - IN (...) → expect collection type
```

**Implementation**:
- Parse SQL to detect operators around each parameter
- Suggest expected type based on operator
- Report warning if mismatch detected

---

#### 3.4 SQLX004: SQL Injection Risk Analyzer
**Detects**: String interpolation in SQL template (not parameterized)

```csharp
// ❌ Error SQLX004: SQL Injection Risk
[Sqlx($"SELECT * FROM users WHERE name = '{name}'")]
Task<User?> GetAsync(string name);

// ✅ OK
[Sqlx("SELECT * FROM users WHERE name = @name")]
Task<User?> GetAsync(string name);
```

**Note**: This should be ERROR, not warning.

---

#### 3.5 SQLX005: Invalid Placeholder Analyzer
**Detects**: Unknown or misspelled placeholders

```csharp
// ❌ Error SQLX005
[Sqlx("SELECT {{colums}} FROM {{table}}")] // Typo: colums → columns
Task<List<User>> GetAllAsync();

// ✅ OK
[Sqlx("SELECT {{columns}} FROM {{table}}")]
Task<List<User>> GetAllAsync();
```

**Implementation**:
- Maintain whitelist of valid placeholders
- Parse template and extract all `{{...}}`
- Report error for unknown placeholders
- **Code Fix**: Suggest correct spelling

---

#### 3.6 SQLX006: Performance Hint Analyzer
**Detects**: Opportunities for optimization

```csharp
// 💡 Info SQLX006: Consider using ordinal access for hot path
[Sqlx("SELECT id, name, email FROM users WHERE id = @id")]
Task<User?> GetByIdAsync(int id); // High frequency query

// Suggestion: Properties should match SQL column order for optimal performance
public class User
{
    public int Id { get; set; }      // ✅ Matches position 0
    public string Name { get; set; }  // ✅ Matches position 1
    public string Email { get; set; } // ✅ Matches position 2
}
```

**Severity**: Information (not warning)

---

### 📊 Expected Results
- **Catch 90% of common errors** at compile time
- **Reduce runtime errors** significantly
- **Improve developer experience** with actionable diagnostics

---

## Phase 4: Comprehensive Benchmark Suite (Week 2)

### 🎯 Objective
Validate all optimizations with real performance data.

### 📋 Benchmarks to Add

#### 4.1 WHERE Approaches Comparison
```csharp
[Benchmark]
public async Task Sqlx_Static_WHERE()
{
    await repo.GetByIdAsync(123);
}

[Benchmark]
public async Task Sqlx_Auto_WHERE()
{
    await repo.FindByNameAsync("Alice");
}

[Benchmark]
public async Task Sqlx_ExpressionToSql_WHERE_Simple()
{
    var where = ExpressionToSql<User>.ForSqlServer()
        .Where(u => u.Age > 18);
    await repo.SearchAsync(where);
}

[Benchmark]
public async Task Sqlx_ExpressionToSql_WHERE_Complex()
{
    var where = ExpressionToSql<User>.ForSqlServer()
        .Where(u => u.Age > 18)
        .Where(u => u.IsActive)
        .Where(u => u.Email.Contains("@example.com"));
    await repo.SearchAsync(where);
}

[Benchmark]
public async Task Sqlx_DynamicSql_WHERE()
{
    await repo.CustomSearchAsync("age > 18 AND is_active = 1");
}
```

---

#### 4.2 Code Generation Optimization Comparison
```csharp
// Compare old (Replace) vs new (Interpolation)
// This requires keeping old version or manually creating equivalent

[Benchmark]
public void Generated_WHERE_StringInterpolation()
{
    var whereClause = "age > 18";
    var sql = $@"SELECT * FROM users WHERE {whereClause}"; // New way
}

[Benchmark]
public void Generated_WHERE_StringReplace()
{
    var whereClause = "age > 18";
    var sql = @"SELECT * FROM users WHERE {MARKER}";
    sql = sql.Replace("{MARKER}", whereClause); // Old way
}
```

---

#### 4.3 Batch Operations
```csharp
[Benchmark]
public async Task Sqlx_BatchInsert_100()
{
    var users = GenerateUsers(100);
    await repo.BatchInsertAsync(users);
}

[Benchmark]
public async Task Sqlx_BatchInsert_1000()
{
    var users = GenerateUsers(1000);
    await repo.BatchInsertAsync(users);
}

[Benchmark]
public async Task Dapper_BatchInsert_1000()
{
    var users = GenerateUsers(1000);
    await connection.ExecuteAsync(
        "INSERT INTO users (name, age, email) VALUES (@Name, @Age, @Email)",
        users);
}
```

---

#### 4.4 Complex Queries
```csharp
[Benchmark]
public async Task Sqlx_JOIN_GroupBy_OrderBy()
{
    await repo.GetUserOrderStatsAsync();
}

[Benchmark]
public async Task Dapper_JOIN_GroupBy_OrderBy()
{
    await connection.QueryAsync<UserOrderStats>(@"
        SELECT u.id, u.name, COUNT(o.id) as OrderCount
        FROM users u
        LEFT JOIN orders o ON u.id = o.user_id
        GROUP BY u.id, u.name
        ORDER BY OrderCount DESC");
}

[Benchmark]
public async Task RawADO_JOIN_GroupBy_OrderBy()
{
    // Hand-written ADO.NET code
}
```

---

#### 4.5 Memory Allocation Tracking
```csharp
[MemoryDiagnoser]
public class MemoryBenchmarks
{
    [Benchmark]
    public async Task<User> Sqlx_GetById()
    {
        return await repo.GetByIdAsync(123);
    }
    
    [Benchmark]
    public async Task<User> Dapper_GetById()
    {
        return await connection.QuerySingleAsync<User>(
            "SELECT * FROM users WHERE id = @id", 
            new { id = 123 });
    }
}
```

---

### 📊 Benchmark Report Format
```
BenchmarkDotNet v0.13.x

|                    Method |      Mean |    Error |   StdDev | Allocated |
|-------------------------- |----------:|---------:|---------:|----------:|
|        Sqlx_Static_WHERE  |  12.34 us | 0.45 us  | 0.12 us  |   1.2 KB  |
|          Sqlx_Auto_WHERE  |  13.56 us | 0.52 us  | 0.15 us  |   1.3 KB  |
| Sqlx_ExpressionToSql_WHR  |  15.78 us | 0.61 us  | 0.18 us  |   1.8 KB  |
|    Sqlx_DynamicSql_WHERE  |  14.23 us | 0.48 us  | 0.14 us  |   1.5 KB  |
|              Dapper_Same  |  13.89 us | 0.55 us  | 0.16 us  |   1.4 KB  |
|         RawADONET_Same    |  11.45 us | 0.42 us  | 0.11 us  |   0.9 KB  |

// Goal: Sqlx should be within 10-20% of raw ADO.NET
```

---

## Implementation Order

### Week 1 (Days 1-3): String Interpolation
- [x] Day 1: SET placeholder optimization
- [x] Day 2: ORDERBY + GROUPBY optimization
- [x] Day 3: JOIN optimization + testing

### Week 1 (Days 4-5): Transaction Support
- [x] Day 4: Transaction parameter detection + generation
- [x] Day 5: Documentation + examples

### Week 2 (Days 1-3): Source Analyzers
- [x] Day 1: SQLX001 (Unused Param) + SQLX002 (Missing Param)
- [x] Day 2: SQLX003 (Type Mismatch) + SQLX004 (SQL Injection)
- [x] Day 3: SQLX005 (Invalid Placeholder) + SQLX006 (Perf Hint)

### Week 2 (Days 4-5): Benchmarks
- [x] Day 4: Implement all benchmark scenarios
- [x] Day 5: Run benchmarks, analyze results, generate report

---

## Breaking Changes Summary

### 1. Code Generation Changes
**Breaking**: Generated code structure changes (string interpolation)  
**Impact**: Low - users don't typically inspect generated code  
**Migration**: Automatic (just recompile)

### 2. Parameter Validation (Analyzers)
**Breaking**: New compile errors for invalid SQL templates  
**Impact**: Medium - existing bad code will fail to compile  
**Migration**: Fix reported errors (mostly quick fixes available)

### 3. New Features (Transaction Parameters)
**Breaking**: None - purely additive  
**Impact**: Zero  
**Migration**: Not needed

---

## Success Criteria

### Performance Goals
- [ ] Sqlx within **15%** of raw ADO.NET performance
- [ ] Sqlx **faster than** Dapper for simple queries
- [ ] Sqlx **competitive with** Dapper for complex queries
- [ ] **Zero** Replace() calls in generated code
- [ ] **<50%** allocations of old Replace-based approach

### Quality Goals
- [ ] **90%+** of common errors caught by analyzers
- [ ] **100%** test coverage for new optimizations
- [ ] **Zero** performance regressions
- [ ] All 724+ existing tests pass

### Documentation Goals
- [ ] Update all docs with new features
- [ ] Add benchmark results to README
- [ ] Create migration guide for breaking changes
- [ ] Update GitHub Pages with performance charts

---

## Risk Mitigation

### Risk 1: Breaking Changes Impact
**Mitigation**: 
- Keep old code as deprecated (one release cycle)
- Provide automatic code fixes where possible
- Clear migration guide

### Risk 2: Performance Not Meeting Goals
**Mitigation**:
- Benchmark early and often
- Profile generated code
- Iterate on hot paths

### Risk 3: Analyzer False Positives
**Mitigation**:
- Conservative rules initially
- Allow suppression via attributes
- Gather user feedback before making strict

---

## Next Steps

**Immediate Action**: 
1. ✅ Commit this plan
2. ✅ Start with SET placeholder optimization (highest impact)
3. ✅ Add benchmarks as we go (validate each optimization)

**Question for User**:
- 开始执行第一阶段（SET/ORDERBY/JOIN 优化）？
- 还是想先看某个具体优化的详细设计？

