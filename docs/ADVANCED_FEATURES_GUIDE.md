# 🚀 Sqlx 高级特性指南

本指南详细介绍 Sqlx 的高级特性，包括 Primary Constructor、Record 类型支持以及性能优化技巧。

## 📋 目录

- [Primary Constructor 支持](#primary-constructor-支持)
- [Record 类型支持](#record-类型支持)
- [混合实体类型](#混合实体类型)
- [性能优化](#性能优化)
- [错误处理与诊断](#错误处理与诊断)
- [最佳实践](#最佳实践)

## Primary Constructor 支持

### 基本用法

Sqlx 完全支持 C# 12+ 的 Primary Constructor 语法：

```csharp
// 定义 Primary Constructor 实体
public class User(int id, string email)
{
    public int Id { get; } = id;
    public string Email { get; } = email;
    public string? Name { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

// Repository 接口
public interface IUserService
{
    IList<User> GetActiveUsers();
    User? GetUserById(int id);
    int CreateUser(User user);
}

// Repository 实现
[RepositoryFor(typeof(IUserService))]
public partial class UserRepository : IUserService
{
    private readonly IDbConnection connection;

    public UserRepository(IDbConnection connection)
    {
        this.connection = connection;
    }

    [Sqlx("SELECT * FROM Users WHERE IsActive = 1")]
    public partial IList<User> GetActiveUsers();

    [Sqlx("SELECT * FROM Users WHERE Id = @id")]
    public partial User? GetUserById(int id);

    [Sqlx("INSERT INTO Users (Email, Name, CreatedAt, IsActive) VALUES (@Email, @Name, @CreatedAt, @IsActive); SELECT last_insert_rowid();")]
    public partial int CreateUser(User user);
}
```

### 生成的代码示例

Sqlx 为 Primary Constructor 生成的优化代码：

```csharp
// 自动生成的实体映射代码
public IList<User> GetActiveUsers()
{
    // ... 连接和命令设置 ...
    
    while (reader.Read())
    {
        // 使用 Primary Constructor 创建实体
        var entity = new User(
            reader.IsDBNull(__ordinal_Id) ? default : reader.GetInt32(__ordinal_Id),
            reader.IsDBNull(__ordinal_Email) ? string.Empty : reader.GetString(__ordinal_Email)
        );
        
        // 设置附加属性
        entity.Name = reader.IsDBNull(__ordinal_Name) ? null : reader.GetString(__ordinal_Name);
        entity.CreatedAt = reader.GetDateTime(__ordinal_CreatedAt);
        entity.IsActive = reader.GetBoolean(__ordinal_IsActive);
        
        results.Add(entity);
    }
    
    return results;
}
```

### 高级 Primary Constructor 场景

#### 复杂业务实体

```csharp
public class Order(int orderId, string customerId, DateTime orderDate)
{
    public int OrderId { get; } = orderId;
    public string CustomerId { get; } = customerId;
    public DateTime OrderDate { get; } = orderDate;
    
    // 计算属性
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    
    // 导航属性
    public List<OrderItem> Items { get; set; } = new();
    public Customer? Customer { get; set; }
    
    // 业务方法
    public void AddItem(OrderItem item) => Items.Add(item);
    public decimal CalculateTotal() => Items.Sum(i => i.Price * i.Quantity);
}

public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}
```

#### 验证和约束

```csharp
public class Product(int id, string name, decimal price)
{
    private readonly int _id = id > 0 ? id : throw new ArgumentException("ID must be positive");
    private readonly string _name = !string.IsNullOrWhiteSpace(name) ? name : throw new ArgumentException("Name is required");
    private readonly decimal _price = price >= 0 ? price : throw new ArgumentException("Price cannot be negative");

    public int Id => _id;
    public string Name => _name;
    public decimal Price => _price;
    
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public bool IsActive { get; set; } = true;
}
```

## Record 类型支持

### 基本 Record 定义

```csharp
// 简单 Record
public record Category(int Id, string Name, string Description);

// 带附加属性的 Record
public record Product(int Id, string Name, decimal Price, int CategoryId)
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

// 继承的 Record
public record BaseEntity(int Id, DateTime CreatedAt);
public record AuditableEntity(int Id, DateTime CreatedAt, DateTime? UpdatedAt) : BaseEntity(Id, CreatedAt);
```

### Record 的优势

1. **不可变性**: Record 的主构造函数参数默认是只读的
2. **值语义**: 基于值的相等性比较
3. **解构支持**: 自动支持解构语法
4. **简洁语法**: 减少样板代码

```csharp
// 使用示例
var product = new Product(1, "iPhone 15", 999.99m, 1);

// 解构
var (id, name, price, categoryId) = product;

// with 表达式创建副本
var updatedProduct = product with { Price = 899.99m };

// 值相等性
var product2 = new Product(1, "iPhone 15", 999.99m, 1);
Console.WriteLine(product == product2); // True
```

### 生成的代码优化

```csharp
// 为 Record 生成的优化代码
public IList<Product> GetProducts()
{
    while (reader.Read())
    {
        // 直接使用 Record 构造函数
        var entity = new Product(
            reader.GetInt32(__ordinal_Id),
            reader.GetString(__ordinal_Name),
            reader.GetDecimal(__ordinal_Price),
            reader.GetInt32(__ordinal_CategoryId)
        )
        {
            // 设置可变属性
            CreatedAt = reader.GetDateTime(__ordinal_CreatedAt),
            IsActive = reader.GetBoolean(__ordinal_IsActive),
            Description = reader.IsDBNull(__ordinal_Description) ? null : reader.GetString(__ordinal_Description)
        };
        
        results.Add(entity);
    }
}
```

## 混合实体类型

Sqlx 支持在同一个项目中混合使用不同类型的实体：

```csharp
// 传统类
public class LegacyUser
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// Primary Constructor
public class ModernUser(int id, string email)
{
    public int Id { get; } = id;
    public string Email { get; } = email;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Record
public record UserProfile(int UserId, string DisplayName, string Avatar);

// 混合服务接口
public interface IMixedUserService
{
    IList<LegacyUser> GetLegacyUsers();        // 传统类
    IList<ModernUser> GetModernUsers();        // Primary Constructor
    IList<UserProfile> GetUserProfiles();     // Record
}

[RepositoryFor(typeof(IMixedUserService))]
public partial class MixedUserRepository : IMixedUserService
{
    // Sqlx 会为每个方法正确推断实体类型并生成相应的映射代码
}
```

## 性能优化

### 类型安全的数据读取

Sqlx 自动选择最优的 DataReader 方法：

```csharp
// 优化前（不安全）
var value = (DateTime)reader.GetValue(ordinal);

// 优化后（类型安全）
var value = reader.GetDateTime(ordinal);
```

### 内存优化

1. **Primary Constructor**: 减少对象初始化开销
2. **Record 类型**: 更好的内存布局和缓存局部性
3. **直接构造**: 避免反射和中间对象创建

### 性能基准测试

运行性能测试来验证优化效果：

```bash
dotnet test tests/Sqlx.PerformanceTests --configuration Release
```

预期结果：
- Primary Constructor: ~20% 性能提升
- Record 类型: ~15% 内存优化
- 类型安全读取: ~10% 速度提升

## 错误处理与诊断

### 编译时诊断

Sqlx 提供详细的编译时诊断信息：

```
SQLX1001: Primary Constructor Issue - Primary Constructor analysis failed for type 'User': Missing required parameter
SQLX1002: Record Type Issue - Record type analysis failed for type 'Product': EqualityContract property conflicts
SQLX1003: Entity Type Inference Issue - Entity type inference failed for method 'GetUsers': Multiple possible entity types
```

### 运行时诊断

启用详细日志记录：

```csharp
// 在 Repository 中实现拦截器
public partial class UserRepository
{
    partial void OnExecuting(string methodName, DbCommand command)
    {
        Console.WriteLine($"Executing {methodName}: {command.CommandText}");
    }

    partial void OnExecuted(string methodName, DbCommand command, object? result, long elapsed)
    {
        Console.WriteLine($"Completed {methodName} in {elapsed}ms");
    }

    partial void OnExecuteFail(string methodName, DbCommand? command, Exception exception, long elapsed)
    {
        Console.WriteLine($"Failed {methodName} after {elapsed}ms: {exception.Message}");
    }
}
```

### 代码质量验证

Sqlx 自动验证生成的代码质量：

- 检测不安全的类型转换
- 验证实体类型使用正确性
- 确保适当的 null 检查

## 最佳实践

### 1. 实体设计原则

```csharp
// ✅ 好的设计
public record User(int Id, string Email)  // 不可变核心数据
{
    public string? Name { get; set; }      // 可变的可选数据
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

// ❌ 避免的设计
public record BadUser(int Id, string Email, string? Name, DateTime CreatedAt, bool IsActive);
// 太多参数，降低可读性
```

### 2. 构造函数参数选择

**Primary Constructor 参数应该是：**
- 实体的核心标识符
- 不可变的业务关键数据
- 创建实体时必需的数据

**属性应该是：**
- 可选的元数据
- 可变的状态信息
- 计算属性或导航属性

### 3. 性能考虑

```csharp
// ✅ 高性能设计
public class OptimizedOrder(int id, string customerId)
{
    public int Id { get; } = id;
    public string CustomerId { get; } = customerId;
    
    // 使用具体类型而不是 object
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
}

// ❌ 性能较差的设计
public class SlowOrder
{
    public object Id { get; set; }  // 装箱/拆箱开销
    public object CustomerId { get; set; }
    // 所有属性都可变，增加内存开销
}
```

### 4. 数据库映射

```csharp
// 使用 TableName 属性明确指定表名
[TableName("Users")]
public record User(int Id, string Email);

// 复杂映射场景
public class OrderService
{
    [Sqlx(@"
        SELECT o.Id, o.CustomerId, o.OrderDate, o.TotalAmount,
               c.Name as CustomerName, c.Email as CustomerEmail
        FROM Orders o
        INNER JOIN Customers c ON o.CustomerId = c.Id
        WHERE o.OrderDate >= @startDate")]
    public IList<OrderWithCustomer> GetOrdersWithCustomers(DateTime startDate);
}

public record OrderWithCustomer(int Id, string CustomerId, DateTime OrderDate, decimal TotalAmount)
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
}
```

### 5. 测试策略

```csharp
[TestClass]
public class UserRepositoryTests
{
    [TestMethod]
    public void PrimaryConstructor_CreatesCorrectEntity()
    {
        var user = new User(1, "test@example.com");
        
        Assert.AreEqual(1, user.Id);
        Assert.AreEqual("test@example.com", user.Email);
        Assert.IsTrue(user.IsActive);
        Assert.IsTrue(user.CreatedAt <= DateTime.UtcNow);
    }

    [TestMethod]
    public void Record_SupportsValueSemantics()
    {
        var user1 = new UserProfile(1, "John Doe", "avatar.jpg");
        var user2 = new UserProfile(1, "John Doe", "avatar.jpg");
        
        Assert.AreEqual(user1, user2);  // 值相等性
        Assert.AreEqual(user1.GetHashCode(), user2.GetHashCode());
    }
}
```

## 🎯 总结

Sqlx 的现代 C# 支持为开发者提供了：

1. **类型安全**: 编译时验证和运行时安全
2. **高性能**: 优化的代码生成和内存使用
3. **现代语法**: 支持最新的 C# 特性
4. **向后兼容**: 不影响现有代码
5. **详细诊断**: 丰富的错误信息和性能建议

通过合理使用这些特性，您可以构建更安全、更高效、更易维护的数据访问层。
