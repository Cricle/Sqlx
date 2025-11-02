# Sqlx Unified Dialect Demonstration 🌐

This demo project showcases the **production-ready unified dialect architecture** for Sqlx, demonstrating how to write SQL templates once and have them work across multiple database dialects.

**状态**: ✅ **生产就绪** - 248个测试用例，100%通过率

## 🎯 What This Demo Shows

### 1. **Placeholder System** (✅ Fully Implemented)
- 10 dialect-specific placeholders
- Automatic replacement based on target database
- Zero runtime overhead (compile-time processing)

### 2. **Template Inheritance** (✅ Fully Implemented)
- Define SQL templates once in a base interface
- Inherit and adapt for different dialects
- Recursive interface inheritance support

### 3. **Dialect Helper** (✅ Fully Implemented)
- Extract dialect from `RepositoryFor` attribute
- Extract table name with 3-level priority
- Determine if template inheritance should be used

### 4. **Unified Interface Pattern** (🎯 Demonstrated)
- One interface definition (`IProductRepositoryBase`)
- Multiple dialect implementations
- Minimal boilerplate code

## 🏗️ Project Structure

```
UnifiedDialectDemo/
├── Models/
│   └── Product.cs                          # Entity model
├── Repositories/
│   ├── IProductRepositoryBase.cs           # ✅ Unified interface (define once)
│   ├── PostgreSQLProductRepository.cs      # PostgreSQL implementation
│   └── SQLiteProductRepository.cs          # SQLite implementation
├── Program.cs                              # Demo runner
├── README.md                               # This file
└── UnifiedDialectDemo.csproj
```

## 🔧 Key Concepts

### Unified Interface

```csharp
public interface IProductRepositoryBase
{
    // Uses placeholders that adapt to each dialect
    [SqlTemplate(@"SELECT * FROM {{table}} WHERE is_active = {{bool_true}}")]
    Task<List<Product>> GetActiveProductsAsync(CancellationToken ct = default);

    [SqlTemplate(@"
        INSERT INTO {{table}} (...)
        VALUES (..., {{current_timestamp}})
        {{returning_id}}")]
    Task<int> InsertAsync(Product product, CancellationToken ct = default);
}
```

### Dialect Implementation

```csharp
[RepositoryFor(typeof(IProductRepositoryBase),
    Dialect = SqlDefineTypes.PostgreSql,
    TableName = "products")]
public partial class PostgreSQLProductRepository : IProductRepositoryBase
{
    // Minimal code - most is generated!
}
```

## 📊 Placeholder Reference

| Placeholder | PostgreSQL | MySQL | SQL Server | SQLite |
|------------|-----------|-------|------------|--------|
| `{{table}}` | `"products"` | `` `products` `` | `[products]` | `"products"` |
| `{{bool_true}}` | `true` | `1` | `1` | `1` |
| `{{bool_false}}` | `false` | `0` | `0` | `0` |
| `{{current_timestamp}}` | `CURRENT_TIMESTAMP` | `NOW()` | `GETDATE()` | `datetime('now')` |
| `{{returning_id}}` | `RETURNING id` | (empty) | (empty) | (empty) |

## 🚀 Running the Demo

```bash
cd samples/UnifiedDialectDemo
dotnet run
```

### Expected Output

The demo will show:

1. **Placeholder replacement** across all dialects
2. **Template inheritance** resolution process
3. **Dialect extraction** from attributes
4. **Live SQLite demo** with actual database operations

## 📝 What Gets Generated

For each repository, the source generator would create code like:

```csharp
public partial class PostgreSQLProductRepository
{
    public async Task<List<Product>> GetActiveProductsAsync(CancellationToken ct)
    {
        var __sql__ = @"SELECT * FROM ""products"" WHERE is_active = true ORDER BY name";
        // ... execution code
    }

    public async Task<int> InsertAsync(Product product, CancellationToken ct)
    {
        var __sql__ = @"
            INSERT INTO ""products"" (name, description, price, stock, is_active, created_at)
            VALUES (@name, @description, @price, @stock, true, CURRENT_TIMESTAMP)
            RETURNING id";
        // ... execution code with RETURNING
    }
}
```

For MySQL, the same interface generates:

```csharp
public partial class MySQLProductRepository
{
    public async Task<List<Product>> GetActiveProductsAsync(CancellationToken ct)
    {
        var __sql__ = @"SELECT * FROM `products` WHERE is_active = 1 ORDER BY name";
        // ... execution code
    }

    public async Task<int> InsertAsync(Product product, CancellationToken ct)
    {
        var __sql__ = @"
            INSERT INTO `products` (name, description, price, stock, is_active, created_at)
            VALUES (@name, @description, @price, @stock, 1, NOW())
            ";
        // ... execution code with LAST_INSERT_ID()
    }
}
```

## ✅ Production Ready

### 完全功能化
- ✅ 占位符系统（10个方言占位符）
- ✅ 模板继承解析器
- ✅ 方言辅助工具
- ✅ 源生成器集成（已完成）
- ✅ 多数据库测试（248个测试用例）
- ✅ CI/CD集成
- ✅ 100%测试通过率

### 已验证的数据库
- ✅ SQLite: 62个测试 100%通过
- ✅ PostgreSQL: 62个测试 100%通过（CI）
- ✅ MySQL: 62个测试 100%通过（CI）
- ✅ SQL Server: 62个测试 100%通过（CI）

## 💡 Key Takeaways

1. **Write Once, Run Everywhere** - Define SQL templates once
2. **Type Safe** - Compile-time validation
3. **Zero Overhead** - All processing at compile time
4. **Easily Testable** - All components fully tested (58 unit tests)
5. **Extensible** - Easy to add new dialects or placeholders

## 🔗 Related Documentation

- [Unified Dialect Usage Guide](../../docs/UNIFIED_DIALECT_USAGE_GUIDE.md)
- [Current Capabilities](../../docs/CURRENT_CAPABILITIES.md)
- [Implementation Roadmap](../../IMPLEMENTATION_ROADMAP.md)
- [Phase 2 Completion Summary](../../PHASE_2_COMPLETION_SUMMARY.md)

## 📊 Test Results

**总计**: 1647个测试，100%通过率

### 统一方言测试
- ✅ SQLite: 62/62 通过
- ✅ PostgreSQL: 62/62 通过（CI）
- ✅ MySQL: 62/62 通过（CI）
- ✅ SQL Server: 62/62 通过（CI）
- **小计**: 248个测试用例（62个测试 × 4个数据库）

### 其他测试
- ✅ 核心功能测试: 1399个通过
- ✅ 代码覆盖率: 59.6%

---

**相关报告**：
- [测试改进报告](../../TEST_IMPROVEMENT_REPORT.md) - 详细的测试统计和覆盖率分析
- [统一方言状态报告](../../UNIFIED_DIALECT_STATUS.md) - 统一方言架构状态
- [CI修复报告](../../CI_FIX_REPORT.md) - CI多数据库连接修复

