# 🚀 Sqlx 主构造函数和 Record 类型支持

Sqlx 现在完全支持 C# 12+ 的主构造函数（Primary Constructor）和 C# 9+ 的 Record 类型！这使得您可以使用现代 C# 语法来定义数据实体，同时享受 Sqlx 的强大功能。

## 📋 目录

- [功能概述](#功能概述)
- [主构造函数支持](#主构造函数支持)
- [Record 类型支持](#record-类型支持)
- [高级用法](#高级用法)
- [性能优化](#性能优化)
- [最佳实践](#最佳实践)
- [示例项目](#示例项目)

## 🎯 功能概述

### ✅ 新增功能

- **✅ 主构造函数支持**: 自动识别和处理 C# 12+ 主构造函数
- **✅ Record 类型支持**: 完全支持 record 类型的实体映射
- **✅ 混合类型支持**: 在同一项目中混合使用传统类、主构造函数类和 record
- **✅ 智能映射**: 自动选择最优的对象创建方式
- **✅ 批量操作**: 所有批量操作都支持新类型
- **✅ 向后兼容**: 不影响现有传统类的使用

### 🔧 技术特性

- **智能类型检测**: 自动识别 record 类型和主构造函数
- **优化的对象创建**: 使用主构造函数进行高效的对象实例化
- **增强的属性映射**: 支持主构造函数参数和常规属性的混合映射
- **性能优化**: 缓存反射信息，减少运行时开销

## 🏗️ 主构造函数支持

### 基础用法

```csharp
// C# 12+ 主构造函数语法
public class Product(int id, string name, decimal price)
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public decimal Price { get; } = price;
    
    // 额外的可变属性
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

// 服务接口
public interface IProductService
{
    [Sqlx]
    IList<Product> GetAllProducts();
    
    [SqlExecuteType(SqlExecuteTypes.Insert, "products")]
    Task<int> InsertProductAsync(Product product);
    
    [SqlExecuteType(SqlExecuteTypes.BatchInsert, "products")]
    Task<int> BatchInsertProductsAsync(IEnumerable<Product> products);
}

[RepositoryFor(typeof(IProductService))]
public partial class ProductRepository : IProductService
{
    private readonly DbConnection connection;
    public ProductRepository(DbConnection connection) => this.connection = connection;
}
```

### 生成的代码示例

Sqlx 会为主构造函数类生成优化的映射代码：

```csharp
// 自动生成的查询代码
var entity = new Product(
    reader.IsDBNull(__ordinal_Id) ? default : reader.GetInt32(__ordinal_Id),
    reader.IsDBNull(__ordinal_Name) ? string.Empty : reader.GetString(__ordinal_Name),
    reader.IsDBNull(__ordinal_Price) ? default : reader.GetDecimal(__ordinal_Price)
);

// 设置额外属性
entity.Description = reader.IsDBNull(__ordinal_Description) ? string.Empty : reader.GetString(__ordinal_Description);
entity.CreatedAt = reader.IsDBNull(__ordinal_CreatedAt) ? default : reader.GetDateTime(__ordinal_CreatedAt);
```

## 📝 Record 类型支持

### 基础 Record

```csharp
// 简单的 record 类型
public record User(int Id, string Name, string Email);

// 带有额外属性的 record
public record Product(int Id, string Name, decimal Price)
{
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
```

### 复杂 Record

```csharp
// 复杂的 record 类型，包含可变属性
public record Order(int Id, int UserId, decimal TotalAmount, DateTime OrderDate)
{
    public string Status { get; set; } = "Pending";
    public string Notes { get; set; } = string.Empty;
    public DateTime? ShippedAt { get; set; }
}

public interface IOrderService
{
    [Sqlx]
    IList<Order> GetAllOrders();
    
    [SqlExecuteType(SqlExecuteTypes.Insert, "orders")]
    Task<int> InsertOrderAsync(Order order);
    
    [SqlExecuteType(SqlExecuteTypes.BatchUpdate, "orders")]
    Task<int> BatchUpdateOrdersAsync(IEnumerable<Order> orders);
}
```

## 🎨 高级用法

### 混合类型项目

您可以在同一个项目中混合使用不同类型的实体：

```csharp
// 传统类
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// Record 类型
public record User(int Id, string Name, string Email);

// 主构造函数类
public class Product(int id, string name, decimal price, int categoryId)
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public decimal Price { get; } = price;
    public int CategoryId { get; } = categoryId;
    public bool IsActive { get; set; } = true;
}

// 统一的服务接口
public interface IMixedService
{
    [Sqlx] IList<Category> GetCategories();
    [Sqlx] IList<User> GetUsers();
    [Sqlx] IList<Product> GetProducts();
}
```

### 嵌套主构造函数

```csharp
public class OrderItem(int id, int orderId, int productId, int quantity, decimal unitPrice)
{
    public int Id { get; } = id;
    public int OrderId { get; } = orderId;
    public int ProductId { get; } = productId;
    public int Quantity { get; } = quantity;
    public decimal UnitPrice { get; } = unitPrice;
    
    // 计算属性
    public decimal TotalPrice => Quantity * UnitPrice;
}
```

## ⚡ 性能优化

### 智能对象创建

Sqlx 会根据类型自动选择最优的创建方式：

1. **Record 类型**: 使用主构造函数创建，然后设置额外属性
2. **主构造函数类**: 使用主构造函数创建，然后设置剩余属性
3. **传统类**: 使用对象初始化器语法

### 缓存机制

- **类型分析缓存**: 缓存类型的构造函数信息
- **属性映射缓存**: 缓存属性到参数的映射关系
- **GetOrdinal 缓存**: 缓存数据库列的序号信息

## 🎯 最佳实践

### 1. 选择合适的类型

```csharp
// ✅ 推荐：简单数据传输对象使用 record
public record UserDto(int Id, string Name, string Email);

// ✅ 推荐：复杂业务实体使用主构造函数类
public class Order(int id, string customerName, DateTime orderDate)
{
    public int Id { get; } = id;
    public string CustomerName { get; } = customerName;
    public DateTime OrderDate { get; } = orderDate;
    
    // 业务逻辑属性
    public List<OrderItem> Items { get; set; } = new();
    public decimal TotalAmount => Items.Sum(i => i.TotalPrice);
}

// ✅ 推荐：需要大量可变属性的实体使用传统类
public class ProductConfiguration
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    // ... 更多属性
}
```

### 2. 命名约定

```csharp
// ✅ 推荐：主构造函数参数使用 camelCase
public class Product(int id, string name, decimal price)
{
    // 属性使用 PascalCase
    public int Id { get; } = id;
    public string Name { get; } = name;
    public decimal Price { get; } = price;
}

// ✅ 推荐：Record 参数直接使用 PascalCase
public record User(int Id, string Name, string Email);
```

### 3. 批量操作优化

```csharp
// ✅ 推荐：使用具体的批量操作类型
[SqlExecuteType(SqlExecuteTypes.BatchInsert, "products")]
Task<int> BatchInsertAsync(IEnumerable<Product> products);

[SqlExecuteType(SqlExecuteTypes.BatchUpdate, "products")]
Task<int> BatchUpdateAsync(IEnumerable<Product> products);

// ✅ 推荐：分批处理大数据集
const int batchSize = 1000;
var batches = products.Chunk(batchSize);
foreach (var batch in batches)
{
    await productService.BatchInsertAsync(batch);
}
```

## 📦 示例项目

查看完整的示例项目：[samples/PrimaryConstructorExample/](../samples/PrimaryConstructorExample/)

该示例包含：

- **传统类示例**: Category
- **Record 类型示例**: User, Order
- **主构造函数示例**: Product, OrderItem
- **混合服务示例**: 展示如何在同一服务中使用多种类型
- **批量操作示例**: 演示所有批量操作的用法
- **性能测试**: 对比不同类型的性能表现

### 运行示例

```bash
cd samples/PrimaryConstructorExample
dotnet run
```

## 🔧 技术实现

### 类型检测

Sqlx 使用以下逻辑检测类型：

1. **Record 检测**: 检查 `INamedTypeSymbol.IsRecord` 属性
2. **主构造函数检测**: 分析构造函数参数与属性的对应关系
3. **智能映射**: 根据类型选择最优的映射策略

### 代码生成

1. **增强实体映射生成器**: 支持主构造函数和 record 的特殊处理
2. **主构造函数分析器**: 分析构造函数参数和属性的关系
3. **智能成员访问**: 统一处理属性和构造函数参数

## 📈 版本兼容性

- **C# 版本**: 支持 C# 9.0+ (Record) 和 C# 12.0+ (主构造函数)
- **.NET 版本**: 支持 .NET 6.0+
- **向后兼容**: 完全兼容现有的传统类实现

## 🤝 贡献

欢迎为主构造函数和 Record 支持功能贡献代码！请查看 [CONTRIBUTING.md](../CONTRIBUTING.md) 了解如何参与开发。

---

## 📞 支持

如果您在使用主构造函数或 Record 类型时遇到问题，请：

1. 查看[示例项目](../samples/PrimaryConstructorExample/)
2. 检查[测试用例](../tests/Sqlx.Tests/PrimaryConstructorTests.cs)
3. 提交 [Issue](https://github.com/your-repo/issues)

享受使用现代 C# 语法的数据访问体验！ 🚀

