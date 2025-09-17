# 🚀 Sqlx 完整功能演示项目

这是一个全面展示 Sqlx 源生成器所有功能的完整演示项目，包含了从基础到高级的各种特性演示。

## 📋 演示功能清单

### 🎯 核心功能
- ✅ **源生成Repository模式** - 自动实现接口方法
- ✅ **部分方法实现** - 为部分方法自动生成实现
- ✅ **多数据库方言支持** - SQL Server, MySQL, PostgreSQL, Oracle, DB2, SQLite
- ✅ **扩展方法源生成** - 为数据库连接添加自定义方法
- ✅ **Expression to SQL** - LINQ表达式转SQL查询

### 🛠️ 高级功能
- ✅ **完整CRUD操作** - Create, Read, Update, Delete 演示
- ✅ **复杂关系查询** - JOIN, 子查询, 聚合函数
- ✅ **事务处理** - 原子操作和回滚机制
- ✅ **审计日志系统** - 操作记录和历史追踪
- ✅ **数据分析查询** - 统计分析和报表查询
- ✅ **数据完整性检查** - 孤立记录检测
- ✅ **性能监控** - 基准测试和性能分析

### 🎛️ 开发者体验
- ✅ **诊断指导系统** - SQL质量检查和性能建议
- ✅ **类型安全** - 编译时类型检查
- ✅ **零反射** - 编译时代码生成，运行时高性能

## 🏗️ 项目结构

```
SqlxDemo/
├── Models/                      # 数据实体模型
│   ├── User.cs                 # 用户实体
│   ├── Department.cs           # 部门实体
│   └── Category.cs             # 产品分类、产品、订单等实体
├── Services/                   # 业务服务层
│   ├── IUserService.cs         # 用户服务接口
│   ├── ICategoryService.cs     # 分类服务接口
│   ├── UserService.cs          # 用户服务实现
│   ├── ComprehensiveServices.cs # 完整业务服务实现
│   └── MultiDatabaseServices.cs # 多数据库方言演示
├── Extensions/                 # 扩展方法
│   ├── DatabaseExtensions.cs   # 基础数据库扩展
│   └── AdvancedDatabaseExtensions.cs # 高级扩展功能
└── Program.cs                  # 主程序入口
```

## 🚀 快速开始

### 运行完整演示
```bash
cd samples/SqlxDemo
dotnet run
```

### 单独构建
```bash
dotnet build samples/SqlxDemo
```

## 💡 功能演示详解

### 1️⃣ 基础源生成Repository模式
```csharp
public interface IUserService
{
    [Sqlx("SELECT * FROM [user] WHERE [is_active] = 1")]
    Task<IList<User>> GetActiveUsersAsync();
}

public partial class UserService : IUserService
{
    // 源生成器自动实现接口方法
}
```

### 2️⃣ 复杂业务实体和关系
```csharp
// 支持复杂的JOIN查询
[Sqlx(@"SELECT p.*, c.name as category_name 
        FROM [product] p 
        INNER JOIN [product_category] c ON p.category_id = c.id 
        WHERE p.category_id = @category_id")]
Task<IList<dynamic>> GetProductsByCategoryAsync(int categoryId);
```

### 3️⃣ 完整CRUD操作
```csharp
// CREATE - 插入并返回ID
[Sqlx(@"INSERT INTO [product_category] (...) VALUES (...); 
        SELECT last_insert_rowid();")]
Task<int> CreateCategoryAsync(...);

// UPDATE - 更新记录
[Sqlx("UPDATE [product_category] SET ... WHERE [id] = @id")]
Task<int> UpdateCategoryAsync(...);

// DELETE - 软删除
[Sqlx("UPDATE [product_category] SET [is_active] = 0 WHERE [id] = @id")]
Task<int> SoftDeleteCategoryAsync(int id);
```

### 4️⃣ 多数据库方言支持
```csharp
// MySQL方言 - 使用 `column` 和 @param
[SqlDefine(SqlDefineTypes.MySql)]
[Sqlx("SELECT * FROM `user` WHERE `is_active` = 1")]

// SQL Server方言 - 使用 [column] 和 @param  
[SqlDefine(SqlDefineTypes.SqlServer)]
[Sqlx("SELECT * FROM [user] WHERE [is_active] = 1")]

// PostgreSQL方言 - 使用 "column" 和 $param
[SqlDefine(SqlDefineTypes.PostgreSql)]
[Sqlx("SELECT * FROM \"user\" WHERE \"is_active\" = 1")]
```

### 5️⃣ 高级扩展方法
```csharp
// 扩展方法自动生成
[Sqlx("SELECT COUNT(*) FROM [user] WHERE [is_active] = 1")]
public static partial Task<int> GetActiveUserCountAsync(this SqliteConnection connection);

// 复杂分析查询
[Sqlx(@"SELECT category_id, COUNT(*) as product_count, 
        SUM(price * stock_quantity) as total_value
        FROM [product] GROUP BY category_id")]
public static partial Task<IList<dynamic>> GetCategoryAnalysisAsync(this SqliteConnection connection);
```

### 6️⃣ Expression to SQL
```csharp
// LINQ表达式转SQL
var query = ExpressionToSql<User>.Create()
    .Where(u => u.is_active && u.age > 25)
    .OrderBy(u => u.salary)
    .Take(10);

Console.WriteLine(query.ToSql());
// 输出: SELECT * FROM [User] WHERE ([is_active] = 1 AND [age] > 25) ORDER BY [salary] ASC LIMIT 10
```

### 7️⃣ 事务处理
```csharp
public async Task<int> CreateOrderWithItemsAsync(Order order, IList<OrderItem> items)
{
    using var transaction = _connection.BeginTransaction();
    try
    {
        var orderId = await CreateOrderAsync(...);
        foreach (var item in items)
        {
            await AddOrderItemAsync(orderId, ...);
        }
        await transaction.CommitAsync();
        return orderId;
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### 8️⃣ 审计日志系统
```csharp
[Sqlx(@"INSERT INTO [audit_log] (table_name, operation, record_id, ...) 
        VALUES (@table_name, @operation, @record_id, ...)")]
Task<int> LogAsync(string tableName, string operation, int? recordId, ...);
```

## 🎯 数据库Schema

项目使用SQLite内存数据库，包含以下表结构：

- **user** - 用户表 (8个用户)
- **department** - 部门表 (5个部门)  
- **product_category** - 产品分类表 (8个分类，支持层级结构)
- **product** - 产品表 (10个产品)
- **order** - 订单表 (5个订单)
- **order_item** - 订单项表 (9个订单项)
- **audit_log** - 审计日志表 (3条审计记录)

## 🔍 诊断指导系统

Sqlx包含强大的诊断系统，在编译时提供：

- **SQL质量检查** - 检测SELECT *、复杂JOIN、子查询等
- **性能优化建议** - 索引建议、查询优化提示
- **最佳实践指导** - 命名规范、方法约定
- **安全警告** - SQL注入风险提示

编译时会看到如下诊断信息：
```
warning SQLX3002: SQL quality issue in 'SELECT * FROM [product]': 
避免使用 SELECT *，明确指定需要的列可以提高性能和维护性
```

## 📊 性能特性

- **零反射** - 编译时代码生成，运行时无反射开销
- **高性能** - 直接SQL执行，无ORM映射损耗
- **内存效率** - 最小化对象分配
- **类型安全** - 编译时类型检查，避免运行时错误

### 性能基准
演示项目包含性能测试：
- 基础查询：100次调用通常 < 10ms
- 复杂查询：50次搜索查询通常 < 20ms

## 🤝 开发建议

1. **使用明确的列名** - 避免SELECT *
2. **合理使用索引** - 在WHERE子句中的列上创建索引
3. **参数化查询** - 始终使用参数而非字符串拼接
4. **事务管理** - 对多表操作使用事务
5. **错误处理** - 实现适当的异常处理和重试机制

## 🔗 相关链接

- [Sqlx 源码](../../src/)
- [测试项目](../../tests/)
- [诊断指导文档](../../docs/DiagnosticGuidance.md)

---

💡 **这个演示项目展示了Sqlx的完整功能，是学习和理解Sqlx最好的起点！**