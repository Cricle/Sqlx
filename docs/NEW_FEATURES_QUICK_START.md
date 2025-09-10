# 🆕 新功能快速入门

## ADO.NET BatchCommand 批量操作

### 基本用法

```csharp
public interface IProductService
{
    [SqlExecuteType(SqlExecuteTypes.BatchCommand, "products")]
    Task<int> BatchInsertAsync(IEnumerable<Product> products);
}

[RepositoryFor(typeof(IProductService))]
public partial class ProductRepository : IProductService
{
    private readonly DbConnection connection;
    public ProductRepository(DbConnection connection) => this.connection = connection;
}
```

### 使用示例

```csharp
var products = new[]
{
    new Product { Name = "产品1", Price = 10.99m },
    new Product { Name = "产品2", Price = 20.99m }
};

var count = await productService.BatchInsertAsync(products);
Console.WriteLine($"批量插入了 {count} 个产品");
```

### 特性

- ✅ 使用 ADO.NET 原生 `DbBatch`
- ✅ 自动参数绑定
- ✅ 支持同步/异步
- ✅ 事务支持
- ✅ 空值处理

---

## ExpressionToSql 模运算支持

### 基本用法

```csharp
// 查找偶数 ID 的记录
var evenRecords = ExpressionToSql<User>.ForSqlServer()
    .Where(u => u.Id % 2 == 0)
    .OrderBy(u => u.Name);

// 分页：每10个一组，取第一个
var groupLeaders = ExpressionToSql<User>.ForSqlServer()
    .Where(u => u.Id % 10 == 1)
    .OrderBy(u => u.Id);
```

### 支持的算术运算

| 运算符 | C# 表达式 | 生成的 SQL |
|--------|-----------|------------|
| 加法 | `a + b` | `a + b` |
| 减法 | `a - b` | `a - b` |
| 乘法 | `a * b` | `a * b` |
| 除法 | `a / b` | `a / b` |
| 🆕 模运算 | `a % b` | `a % b` |

### 实际应用场景

```csharp
public interface IUserService
{
    [Sqlx]
    IList<User> GetUsers([ExpressionToSql] ExpressionToSql<User> filter);
}

// 使用场景
var userService = new UserRepository(connection);

// 1. 分组查询
var groupedUsers = userService.GetUsers(
    ExpressionToSql<User>.ForSqlServer()
        .Where(u => u.Id % 5 == 0)  // 每5个一组
        .OrderBy(u => u.Id)
);

// 2. 奇偶筛选
var evenUsers = userService.GetUsers(
    ExpressionToSql<User>.ForSqlServer()
        .Where(u => u.Id % 2 == 0 && u.IsActive)
        .OrderBy(u => u.Name)
);

// 3. 复合运算
var complexFilter = userService.GetUsers(
    ExpressionToSql<User>.ForSqlServer()
        .Where(u => (u.Age + u.Experience) % 10 == 5)
        .Where(u => u.Salary > 50000)
);
```

---

## 最佳实践

### BatchCommand

1. **适用场景**：大批量数据插入/更新（>100条）
2. **性能优势**：比循环单条操作快 10-100 倍
3. **内存考虑**：大数据集建议分批处理

```csharp
// 推荐：分批处理大数据集
const int batchSize = 1000;
var batches = products.Chunk(batchSize);

foreach (var batch in batches)
{
    await productService.BatchInsertAsync(batch);
}
```

### ExpressionToSql 模运算

1. **数据库兼容性**：所有主流数据库都支持 `%` 运算符
2. **性能考虑**：模运算在大表上可能较慢，建议配合索引
3. **类型安全**：仅支持数值类型的模运算

```csharp
// 推荐：结合其他条件使用
.Where(u => u.IsActive && u.Id % 2 == 0)  // 先筛选活跃用户

// 避免：单独使用模运算在大表上
.Where(u => u.Id % 1000 == 1)  // 可能很慢
```

---

## 升级说明

这些功能向后兼容，无需修改现有代码。只需：

1. 更新到最新版本的 Sqlx
2. 开始使用新的 `SqlExecuteTypes.BatchCommand`
3. 在表达式中使用 `%` 运算符

```csharp
// 旧代码继续工作
[SqlExecuteType(SqlExecuteTypes.Insert, "products")]
int InsertProduct(Product product);

// 新功能可选使用
[SqlExecuteType(SqlExecuteTypes.BatchCommand, "products")]
Task<int> BatchInsertAsync(IEnumerable<Product> products);
```
