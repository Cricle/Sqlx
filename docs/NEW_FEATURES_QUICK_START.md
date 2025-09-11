# 🆕 新功能快速入门

## ADO.NET BatchCommand 批量操作

### 支持的批处理类型

```csharp
public interface IProductService
{
    // 批量插入
    [SqlExecuteType(SqlExecuteTypes.BatchInsert, "products")]
    Task<int> BatchInsertAsync(IEnumerable<Product> products);
    
    // 批量更新
    [SqlExecuteType(SqlExecuteTypes.BatchUpdate, "products")]
    Task<int> BatchUpdateAsync(IEnumerable<Product> products);
    
    // 批量删除
    [SqlExecuteType(SqlExecuteTypes.BatchDelete, "products")]
    Task<int> BatchDeleteAsync(IEnumerable<Product> products);
    
    // 通用批处理（根据方法名自动推断操作类型）
    [SqlExecuteType(SqlExecuteTypes.BatchCommand, "products")]
    Task<int> BatchAddProductsAsync(IEnumerable<Product> products); // 推断为 INSERT
}

[RepositoryFor(typeof(IProductService))]
public partial class ProductRepository : IProductService
{
    private readonly DbConnection connection;
    public ProductRepository(DbConnection connection) => this.connection = connection;
}
```

### 使用示例

#### 基础用法

```csharp
var products = new[]
{
    new Product { Id = 1, Name = "产品1", Price = 10.99m },
    new Product { Id = 2, Name = "产品2", Price = 20.99m }
};

// 批量插入
var insertCount = await productService.BatchInsertAsync(products);
Console.WriteLine($"批量插入了 {insertCount} 个产品");

// 批量更新（使用默认主键 Id 作为 WHERE 条件）
products[0].Price = 15.99m;
var updateCount = await productService.BatchUpdateAsync(products);
Console.WriteLine($"批量更新了 {updateCount} 个产品");

// 批量删除
var deleteCount = await productService.BatchDeleteAsync(products);
Console.WriteLine($"批量删除了 {deleteCount} 个产品");
```

#### 🆕 精确控制 WHERE 和 SET 子句

```csharp
public class Product
{
    [Where] // 明确指定用于 WHERE 条件
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    [Set] // 明确指定用于 SET 子句
    public decimal Price { get; set; }
    
    [Set] // 明确指定用于 SET 子句  
    public int Stock { get; set; }
    
    public DateTime CreatedAt { get; set; } // 不参与更新
}

public class UserProfile
{
    [Where] // 基于用户名查找，而不是 ID
    public string UserName { get; set; } = string.Empty;
    
    [Set] // 只更新邮箱和登录时间
    public string Email { get; set; } = string.Empty;
    
    [Set]
    public DateTime LastLoginAt { get; set; }
    
    public int Id { get; set; } // 不参与更新
}

// 生成的 SQL:
// UPDATE products SET Price = @Price, Stock = @Stock WHERE Id = @Id
// UPDATE user_profiles SET Email = @Email, LastLoginAt = @LastLoginAt WHERE UserName = @UserName
```

#### 🔧 自定义 WHERE 操作符

```csharp
public class OrderStatus
{
    [Where] // 默认使用 = 操作符
    public string OrderNumber { get; set; } = string.Empty;
    
    [Where(">=")] // 自定义操作符
    public DateTime OrderDate { get; set; }
    
    [Set]
    public string Status { get; set; } = string.Empty;
}

// 生成的 SQL:
// UPDATE orders SET Status = @Status 
// WHERE OrderNumber = @OrderNumber AND OrderDate >= @OrderDate
```

### 特性

- ✅ 使用 ADO.NET 原生 `DbBatch`（如果数据库支持）
- ✅ 自动降级到单个命令（不支持 DbBatch 时）
- ✅ 支持插入、更新、删除操作
- 🆕 **精确控制特性**：
  - `[Where]` - 明确指定 WHERE 条件字段
  - `[Set]` - 明确指定 SET 子句字段
  - 自定义 WHERE 操作符支持
- ✅ 智能默认行为（主键自动识别）
- ✅ 支持同步/异步
- ✅ 事务支持
- ✅ 完善的错误处理和验证

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

1. **适用场景**：大批量数据操作（>100条）
2. **性能优势**：使用原生 DbBatch 比单条操作快 10-100 倍
3. **内存考虑**：大数据集建议分批处理
4. **数据库支持**：
   - ✅ SQL Server 2012+（原生 DbBatch）
   - ✅ PostgreSQL 3.0+（原生 DbBatch）
   - ✅ MySQL 8.0+（原生 DbBatch）
   - ⚠️ SQLite（降级到单个命令）
   - ⚠️ 其他数据库（自动降级）

```csharp
// 推荐：分批处理大数据集
const int batchSize = 1000;
var batches = products.Chunk(batchSize);

foreach (var batch in batches)
{
    // 使用具体的批处理类型获得最佳性能
    await productService.BatchInsertAsync(batch);
}

// 支持不同操作类型
await userService.BatchUpdateUsersAsync(usersToUpdate);
await logService.BatchDeleteLogsAsync(oldLogs);

// 🆕 使用新特性进行精确控制
public class ProductUpdate
{
    [Where] public int Id { get; set; }           // WHERE 条件
    [Set] public decimal Price { get; set; }      // SET 子句
    [Set] public int Stock { get; set; }          // SET 子句
    public DateTime CreatedAt { get; set; }       // 忽略
}
```

### 🆕 新特性最佳实践

#### 1. **明确的意图表达**

```csharp
// ✅ 推荐：明确指定 WHERE 和 SET 字段
public class UserUpdate
{
    [Where] public string UserName { get; set; }    // 基于用户名更新
    [Set] public string Email { get; set; }         // 只更新邮箱
    [Set] public DateTime LastLoginAt { get; set; }  // 和登录时间
}

// ❌ 避免：依赖自动推断可能不准确
public class UserUpdate
{
    public int Id { get; set; }        // 可能不是想要的 WHERE 条件
    public string UserName { get; set; }
    public string Email { get; set; }
}
```

#### 2. **复合 WHERE 条件**

```csharp
// ✅ 支持多字段 WHERE 条件
public class InventoryUpdate
{
    [Where] public int ProductId { get; set; }
    [Where] public int WarehouseId { get; set; }
    [Set] public int Quantity { get; set; }
}
// 生成: WHERE ProductId = @ProductId AND WarehouseId = @WarehouseId
```

#### 3. **自定义操作符**

```csharp
// ✅ 支持不同的 WHERE 操作符
public class PriceUpdate
{
    [Where(">=")] public decimal MinPrice { get; set; }  // 价格范围
    [Where("<=")] public decimal MaxPrice { get; set; }
    [Set] public decimal NewPrice { get; set; }
}
// 生成: WHERE MinPrice >= @MinPrice AND MaxPrice <= @MaxPrice
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

// 新功能可选使用 - 具体批处理类型
[SqlExecuteType(SqlExecuteTypes.BatchInsert, "products")]
Task<int> BatchInsertAsync(IEnumerable<Product> products);

[SqlExecuteType(SqlExecuteTypes.BatchUpdate, "products")]
Task<int> BatchUpdateAsync(IEnumerable<Product> products);

[SqlExecuteType(SqlExecuteTypes.BatchDelete, "products")]
Task<int> BatchDeleteAsync(IEnumerable<Product> products);

// 或使用通用批处理（根据方法名推断）
[SqlExecuteType(SqlExecuteTypes.BatchCommand, "products")]
Task<int> BatchAddProductsAsync(IEnumerable<Product> products);

// 🆕 使用新特性进行精确控制
public class ProductForUpdate
{
    [Where] public int Id { get; set; }
    [Set] public decimal Price { get; set; }
    [Set] public int Stock { get; set; }
}

[SqlExecuteType(SqlExecuteTypes.BatchUpdate, "products")]
Task<int> BatchUpdateWithControlAsync(IEnumerable<ProductForUpdate> products);
```
