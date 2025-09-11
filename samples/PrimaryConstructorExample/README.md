# Sqlx Primary Constructor & Record 支持演示

本示例展示了 Sqlx 对现代 C# 特性的完整支持，包括：

## 🚀 支持的 C# 特性

### 1. 传统类 (Traditional Classes)
```csharp
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
```

### 2. Record 类型 (C# 9+)
```csharp
public record Product(int Id, string Name, decimal Price, int CategoryId)
{
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;
}
```

### 3. 主构造函数类 (C# 12+)
```csharp
public class Order(int id, string customerName)
{
    public int Id { get; } = id;
    public string CustomerName { get; } = customerName;
    public DateTime OrderDate { get; set; } = DateTime.Now;
    public decimal TotalAmount { get; set; }
}
```

## 🔧 技术特性

### 自动代码生成
- ✅ **实体映射**: 自动生成高效的 DataReader 到实体的映射代码
- ✅ **主构造函数支持**: 正确识别并使用主构造函数参数
- ✅ **Record 支持**: 完整支持 Record 类型的构造和属性设置
- ✅ **类型安全**: 使用强类型的 DataReader 方法 (如 `GetDateTime`, `GetDecimal`)

### 生成的代码示例

对于 Record 类型：
```csharp
var entity = new TestNamespace.Product(
    reader.IsDBNull(__ordinal_Id) ? default : reader.GetInt32(__ordinal_Id),
    reader.IsDBNull(__ordinal_Name) ? string.Empty : reader.GetString(__ordinal_Name),
    reader.IsDBNull(__ordinal_Price) ? default : reader.GetDecimal(__ordinal_Price),
    reader.IsDBNull(__ordinal_CategoryId) ? default : reader.GetInt32(__ordinal_CategoryId)
);
```

对于主构造函数类：
```csharp
var entity = new TestNamespace.Order(
    reader.IsDBNull(__ordinal_Id) ? default : reader.GetInt32(__ordinal_Id),
    reader.IsDBNull(__ordinal_CustomerName) ? string.Empty : reader.GetString(__ordinal_CustomerName)
);
entity.OrderDate = reader.GetDateTime(__ordinal_OrderDate);
```

## 🏃 运行示例

```bash
cd samples/PrimaryConstructorExample
dotnet run
```

## 📊 预期输出

```
🚀 Sqlx Primary Constructor & Record 支持演示
==================================================

📁 1. 测试传统类 (Category)
添加分类: 电子产品
分类ID: 1
总分类数: 1
  - 电子产品: 各种电子设备和配件

📦 2. 测试 Record 类型 (Product)
添加产品: iPhone 15 - $999.99
产品ID: 1
总产品数: 1
  - iPhone 15: $999.99 (分类: 1)
    创建时间: 2024-01-01 12:00:00, 激活: True

🛒 3. 测试主构造函数类 (Order)
添加订单: 客户 张三, 金额 $999.99
订单ID: 1
总订单数: 1
  - 订单 #1: 张三
    日期: 2024-01-01 12:00:00, 金额: $999.99

✅ 所有测试完成！Primary Constructor 和 Record 支持正常工作。
```

## 🎯 关键改进

1. **智能实体类型推断**: 每个方法根据其返回类型自动推断正确的实体类型
2. **高性能代码生成**: 生成的代码使用最优的 DataReader 方法
3. **完整的 C# 支持**: 支持从传统类到最新的主构造函数和 Record
4. **编译时安全**: 所有映射在编译时生成和验证

## 🔍 技术细节

- **实体分析器**: `PrimaryConstructorAnalyzer` 识别主构造函数和 Record
- **映射生成器**: `EnhancedEntityMappingGenerator` 生成优化的实体映射
- **类型识别**: 改进的类型系统正确处理 DateTime 等特殊类型
- **命名空间处理**: 完整的全限定名称支持
