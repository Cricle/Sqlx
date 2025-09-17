# SqlxDemo - Sqlx 源生成器完整功能演示

这个项目完整演示了 Sqlx 源生成器的所有4个核心特性，所有代码都由源生成器自动生成。

## 🎯 演示的核心特性

### 1️⃣ **RawSql/Sqlx 特性** - 手写SQL和存储过程
```csharp
[Sqlx("SELECT * FROM [user] WHERE [is_active] = 1")]
public partial Task<IList<User>> GetActiveUsersAsync();

[Sqlx("SELECT * FROM [user] WHERE [id] = @id")]
public partial Task<User?> GetUserByIdAsync(int id);
```

### 2️⃣ **SqlExecuteType 特性** - CRUD操作类型标注
```csharp
[SqlExecuteType(SqlOperation.Insert, "user")]
public partial Task<int> CreateUserAsync(string name, string email, int age, decimal salary, int departmentId);

[SqlExecuteType(SqlOperation.Update, "user")]
[Sqlx("UPDATE [user] SET [salary] = @salary WHERE [id] = @userId")]
public partial Task<int> UpdateUserSalaryAsync(int userId, decimal salary, decimal rating);
```

### 3️⃣ **RepositoryFor 特性** - 自动仓储模式生成
```csharp
[RepositoryFor(typeof(IUserRepository))]
public partial class UserRepository : IUserRepository
{
    // 所有接口方法的实现由源生成器自动生成
    [Sqlx("SELECT * FROM [user] WHERE [id] = @id")]
    public partial Task<User?> GetByIdAsync(int id);
    
    [Sqlx("SELECT * FROM [user] ORDER BY [name]")]
    public partial Task<IList<User>> GetAllAsync();
}
```

### 4️⃣ **ExpressionToSql 特性** - LINQ表达式转SQL
```csharp
[Sqlx("SELECT * FROM [user] WHERE {whereCondition} ORDER BY [name]")]
public partial Task<IList<User>> GetUsersByExpressionAsync(
    [ExpressionToSql] Expression<Func<User, bool>> whereCondition);

[Sqlx("SELECT * FROM [user] WHERE {whereCondition} AND [is_active] = 1 ORDER BY {orderBy}")]
public partial Task<IList<User>> GetActiveUsersByExpressionAsync(
    [ExpressionToSql] Expression<Func<User, bool>> whereCondition,
    [ExpressionToSql] Expression<Func<User, object>> orderBy);
```

## 🚀 快速开始

1. **构建项目**:
```bash
dotnet build
```

2. **运行演示**:
```bash
dotnet run
```

演示程序会创建一个内存SQLite数据库并展示所有Sqlx特性。

## 📁 项目结构

```
SqlxDemo/
├── Models/                          # 实体模型
│   ├── User.cs                     # 用户模型
│   ├── Department.cs               # 部门模型  
│   ├── Product.cs                  # 产品模型
│   └── Order.cs                    # 订单模型
├── Services/                        # 服务实现
│   ├── UserService.cs              # RawSql/Sqlx 特性演示
│   ├── ProductServices.cs          # 复杂查询演示
│   ├── AdvancedFeatureServices.cs  # 高级特性演示
│   └── RepositoryForDemo.cs        # RepositoryFor 特性演示
├── Extensions/                      # 扩展方法
│   └── DatabaseExtensions.cs       # 数据库扩展
├── CompleteSqlxDemo.cs             # 完整功能演示 (新增)
└── Program.cs                      # 主程序
```

## 💡 核心优势

- **🛡️ 类型安全**: 编译时SQL验证，避免运行时错误
- **⚡ 高性能**: 零反射，编译期代码生成
- **🔧 零配置**: 开箱即用，无需复杂配置
- **📝 自动生成**: 所有数据访问代码由源生成器自动生成
- **🎭 多数据库**: 支持SQL Server, MySQL, PostgreSQL等
- **🏗️ 架构清晰**: 仓储模式自动实现，代码更干净

## 🎯 演示场景

### 基础CRUD操作
```csharp
// 查询
var users = await userRepo.GetAllAsync();
var user = await userRepo.GetByIdAsync(1);

// 创建
var userId = await userRepo.CreateAsync(newUser);

// 更新  
await userRepo.UpdateAsync(user);

// 删除
await userRepo.DeleteAsync(userId);
```

### 复杂查询
```csharp
// 条件查询
var activeUsers = await userService.GetActiveUsersAsync();
var deptUsers = await userRepo.GetByDepartmentAsync(1);

// 搜索
var products = await productRepo.SearchAsync("手机");

// 统计
var count = await userService.GetUserCountByDepartmentAsync(1);
```

### 表达式查询
```csharp
// LINQ表达式自动转换为SQL
Expression<Func<User, bool>> condition = u => u.Age > 30 && u.Salary >= 80000;
var users = await advancedService.GetUsersByExpressionAsync(condition);
```

### 批量操作
```csharp
// 批量插入
await advancedService.CreateMultipleUsersAsync(...);

// 批量更新
await productRepo.UpdateStockAsync(productId, newStock);
```

## 🔍 查看生成的代码

构建项目后，可以在以下位置查看源生成器生成的代码:
```
obj/Debug/net9.0/generated/Sqlx.Generator/Sqlx.CSharpGenerator/
```

## 📖 更多文档

查看项目根目录下的文档：
- `docs/SQLX_COMPLETE_FEATURE_GUIDE.md` - 完整特性指南
- `docs/NEW_FEATURES_QUICK_START.md` - 快速入门指南

## 🎖️ 重要说明

**本演示项目中的所有数据访问代码都是由 Sqlx 源生成器自动生成的，开发者只需要定义接口和标注属性，无需手动编写任何数据访问实现代码！**