# 🚀 Sqlx 全面功能演示

> **最全面的 Sqlx 功能展示** - 从基础 CRUD 到高级特性，一站式体验现代 .NET 数据访问层

<div align="center">

**交互式演示 · 实时性能测试 · 全特性覆盖**

[![.NET](https://img.shields.io/badge/.NET-8.0+-512BD4?style=for-the-badge)](https://dotnet.microsoft.com/)
[![Sqlx](https://img.shields.io/badge/Sqlx-v2.0.1+-007ACC?style=for-the-badge)](../../)
[![Demo](https://img.shields.io/badge/Status-Ready-green?style=for-the-badge)]()

</div>

---

## ✨ 演示项目亮点

<table>
<tr>
<td width="50%">

### 🚀 核心特性全覆盖
- ⚡ **零反射高性能** - 编译时代码生成
- 🛡️ **类型安全** - 编译时错误检查  
- 🎯 **智能推断** - 自动识别 SQL 操作
- 📊 **原生 DbBatch** - 10-100x 批量性能

### 🎨 Expression to SQL
- 🔍 **动态查询构建** - Lambda 表达式转 SQL
- 🌐 **多数据库方言** - SQL Server、MySQL、PostgreSQL、SQLite
- 📝 **类型安全过滤** - 编译时验证查询条件
- ⚡ **实时性能优化** - 智能 SQL 生成

</td>
<td width="50%">

### 🏗️ 现代 C# 语法
- 📦 **Record 类型** (C# 9+) - 不可变数据模型
- 🔧 **Primary Constructor** (C# 12+) - 业界首创支持
- 🎭 **混合使用** - 传统类、Record、主构造函数
- ✨ **零学习成本** - 无需额外配置

### 📈 性能基准测试
- 🔬 **内存使用分析** - GC 压力测试
- 🚄 **吞吐量测试** - ops/sec 性能指标
- 📊 **批量 vs 单条对比** - 实测性能提升
- 🔄 **并发性能** - 多线程安全验证

</td>
</tr>
</table>

## 🎯 交互式演示菜单

运行程序后，您将看到专业的演示菜单：

```
🎯 Sqlx 全面功能演示菜单
============================================================
1️⃣  基础 CRUD 操作演示
2️⃣  🆕 智能 UPDATE 操作演示 (优化体验)
3️⃣  Expression to SQL 动态查询演示
4️⃣  DbBatch 批量操作演示
5️⃣  多数据库方言支持演示
6️⃣  现代 C# 语法支持演示
7️⃣  复杂查询和分析演示
8️⃣  性能基准测试对比
9️⃣  全部演示 (推荐)
0️⃣  退出演示
============================================================
```

## 🏗️ 项目架构

```
ComprehensiveExample/
├── 📂 Models/                      # 实体模型层
│   └── User.cs                    # 🎭 多种语法演示 (传统类/Record/Primary Constructor)
├── 📂 Services/                   # 服务接口层  
│   ├── IUserService.cs           # 👥 用户服务接口
│   ├── IExpressionToSqlService.cs # 🎨 动态查询接口
│   ├── IBatchOperationService.cs  # ⚡ 批量操作接口
│   └── UserService.cs            # 🚀 自动生成实现
├── 📂 Demonstrations/             # 演示模块
│   ├── ExpressionToSqlDemo.cs    # 🎨 动态查询演示
│   ├── BatchOperationDemo.cs     # ⚡ 批量操作演示
│   └── MultiDatabaseDemo.cs      # 🌐 多数据库演示
├── 📂 Data/
│   └── DatabaseSetup.cs          # 🗄️ 数据库初始化
├── Program.cs                     # 🎮 主程序入口
├── PerformanceTest.cs            # 📊 性能测试套件
└── README.md                     # 📖 项目文档
```

## 🚀 快速开始

### 📦 运行演示

```bash
# 1. 克隆项目 (如果还没有)
git clone https://github.com/Cricle/Sqlx.git
cd Sqlx/samples/ComprehensiveExample

# 2. 运行演示项目
dotnet run

# 3. 选择演示项目或直接运行全部演示
# 推荐选择: 8️⃣ 全部演示 (约5-10分钟)
```

### 🎮 演示体验

```csharp
// 🎯 智能 CRUD - 零配置自动生成
var user = await userService.GetUserByIdAsync(1);
await userService.CreateUserAsync(newUser);

// 🎨 动态查询 - 类型安全的条件构建
var query = ExpressionToSql<User>.ForSqlite()
    .Where(u => u.IsActive && u.CreatedAt > DateTime.Now.AddDays(-30))
    .OrderBy(u => u.Name)
    .Take(10);
var results = await expressionService.QueryUsersAsync(query);

// ⚡ 批量操作 - 10-100x 性能提升
await batchService.BatchCreateUsersAsync(thousandUsers);
```

## 📋 演示内容详解

### 1️⃣ 基础 CRUD 操作演示
- 🎯 **智能推断** - 方法名自动识别 SQL 操作类型
- 📝 **参数化查询** - 自动防止 SQL 注入
- 🔍 **复杂查询** - 关联查询、聚合查询、标量查询
- ✅ **实时验证** - 每步操作都有结果验证

### 2️⃣ 🆕 智能 UPDATE 操作演示
- 🎯 **部分更新** - 只更新指定字段，减少数据传输
- 📦 **批量条件更新** - 基于条件批量修改记录
- ⚡ **增量更新** - 原子性数值字段增减操作
- 🔒 **乐观锁更新** - 基于版本字段的并发安全更新
- 🚀 **批量字段更新** - 高性能批量更新指定字段
- 🎨 **类型安全** - 编译时验证，运行时高效

### 3️⃣ Expression to SQL 动态查询
- 🔧 **条件组合** - AND、OR、NOT 逻辑操作
- 📊 **排序分页** - OrderBy、Skip、Take 支持
- 🔄 **动态构建** - 根据用户输入动态添加条件
- 🎭 **多实体支持** - User、Customer、Product 等不同类型

### 4️⃣ DbBatch 批量操作
- ⚡ **性能对比** - 批量 vs 单条插入实测
- 📈 **吞吐量测试** - 实时显示 ops/sec
- 🚀 **原生 DbBatch** - .NET 6+ 高性能批处理
- 📊 **内存分析** - GC 压力和内存使用监控

### 5️⃣ 多数据库方言支持
- 🔷 **SQL Server** - `[列名]` 标识符
- 🟢 **MySQL** - `` `列名` `` 标识符  
- 🐘 **PostgreSQL** - `"列名"` 标识符
- 📊 **SQLite** - 简洁无标识符模式
- 🔄 **迁移友好** - 一套代码适配多数据库

### 6️⃣ 现代 C# 语法支持
- 📦 **Record 类型** - `Product`, `InventoryItem` 等不可变模型
- 🔧 **Primary Constructor** - `Customer` 类演示
- 🎭 **组合语法** - `AuditLog` Record + Primary Constructor
- ✨ **自动适配** - 无需额外配置自动支持

### 7️⃣ 复杂查询和分析
- 📊 **统计分析** - 客户价值分析、订单统计
- 🔗 **多表关联** - JOIN 查询、视图查询
- 📂 **层次结构** - 分类父子关系查询
- 📝 **审计日志** - 操作历史记录和查询

### 8️⃣ 性能基准测试
- ⚡ **标量查询** - 10,000 次 COUNT 查询性能
- 📋 **实体查询** - 1,000 次列表查询性能
- 🚀 **批量操作** - 1,000 条记录批量插入性能
- 🔄 **并发测试** - 10 线程并发查询性能
- 🗑️ **内存分析** - GC 回收次数和内存使用

## 🎯 实体模型设计

### 传统类示例
```csharp
[TableName("users")]
public class User  // 标准 C# 类
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    // ... 更多属性
}
```

### Record 类型示例
```csharp
[TableName("products")]
public record Product(int Id, string Name, decimal Price, int CategoryId)  // C# 9+ Record
{
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;
}
```

### Primary Constructor 示例
```csharp
[TableName("customers")]  
public class Customer(int id, string name, string email, DateTime birthDate)  // C# 12+
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
    // ... 更多属性
}
```

### 组合语法示例
```csharp
[TableName("audit_logs")]
public record AuditLog(string Action, string EntityType, string EntityId, string UserId)  // Record + Primary Constructor
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    // ... 更多属性
}
```

## 📊 性能测试结果示例

演示程序会显示详细的性能测试结果：

```
📊 标量查询性能测试
   - 迭代次数: 10,000
   - 总耗时: 1,245 ms
   - 平均耗时: 0.125 ms/次
   - 吞吐量: 8,032 ops/sec

⚡ 批量操作性能测试
   - 批量插入 1,000 条记录
   - 耗时: 156 ms
   - 吞吐量: 6,410 条/秒
   - 单条插入速度: 645 条/秒
   - 🚀 性能提升: 9.9x 倍

🗑️ 内存使用测试
   - 执行了 5,000 次查询
   - Gen 0 回收: 12
   - Gen 1 回收: 3
   - Gen 2 回收: 0
   - 内存变化: 23.4 KB
   - 平均内存/查询: 4.8 bytes
```

## 🌐 数据库支持矩阵

| 数据库 | 连接示例 | 标识符 | 分页语法 | 自增主键 |
|--------|----------|--------|----------|----------|
| SQL Server | `Data Source=...` | `[列名]` | `OFFSET/FETCH` | `IDENTITY` |
| MySQL | `Server=...` | `` `列名` `` | `LIMIT offset, count` | `AUTO_INCREMENT` |
| PostgreSQL | `Host=...` | `"列名"` | `LIMIT/OFFSET` | `SERIAL` |
| SQLite | `Data Source=...` | 无引用 | `LIMIT/OFFSET` | `INTEGER PRIMARY KEY` |
| Oracle | `Data Source=...` | `"列名"` | `ROWNUM/OFFSET` | `SEQUENCE` |
| DB2 | `Server=...` | `"列名"` | `OFFSET/FETCH` | `GENERATED` |

## 💡 最佳实践演示

### 动态查询构建
```csharp
// 根据用户输入动态构建查询
var query = ExpressionToSql<Customer>.ForSqlite();

if (!string.IsNullOrEmpty(searchName))
    query = query.Where(c => c.Name.Contains(searchName));

if (isVip.HasValue)
    query = query.Where(c => c.IsVip == isVip.Value);

if (minSpent.HasValue)
    query = query.Where(c => c.TotalSpent >= minSpent.Value);

var results = await customerService.SearchCustomersAsync(
    query.OrderBy(c => c.Name).Take(50)
);
```

### 高性能批量操作
```csharp
// 传统方式 (慢)
foreach (var user in users)
{
    await userService.CreateUserAsync(user);  // N 次数据库往返
}

// Sqlx 批量方式 (快 10-100 倍)
await batchService.BatchCreateUsersAsync(users);  // 1 次批量操作
```

### 类型安全的复杂查询
```csharp
// 编译时验证的复杂查询
var analyticsQuery = ExpressionToSql<Customer>.ForSqlite()
    .Where(c => c.IsVip && c.Status == CustomerStatus.Active)
    .Where(c => c.TotalSpent > 10000 && c.CreatedAt > DateTime.Now.AddYears(-1))
    .OrderByDescending(c => c.TotalSpent)
    .ThenBy(c => c.Name)
    .Take(100);
```

## 🚀 技术优势

### 🎯 开发效率
- ✅ **零配置启动** - 无需复杂映射文件
- ✅ **智能代码生成** - 编译时自动生成实现
- ✅ **类型安全** - 编译时捕获错误
- ✅ **现代语法支持** - 最新 C# 特性

### ⚡ 运行性能  
- ✅ **零反射** - 编译时确定类型
- ✅ **原生 DbBatch** - 批量操作性能
- ✅ **智能优化** - 自动选择最优实现
- ✅ **内存友好** - 最小化 GC 压力

### 🛡️ 安全可靠
- ✅ **SQL 注入防护** - 自动参数化
- ✅ **编译时验证** - 避免运行时错误
- ✅ **详细诊断** - 友好的错误信息
- ✅ **99.1% 测试覆盖** - 高质量保证

## 🔧 技术要求

- **.NET 8.0+** - 现代 .NET 平台
- **C# 12.0+** - 最新语言特性
- **支持的数据库** - SQL Server、MySQL、PostgreSQL、SQLite、Oracle、DB2

## 🎉 开始体验

```bash
cd samples/ComprehensiveExample
dotnet run
# 选择 8️⃣ 全部演示，享受完整体验！
```

---

<div align="center">

**🚀 这是 Sqlx 能力的全面展示** 

**从简单的 CRUD 到复杂的企业级应用场景，Sqlx 都能完美胜任**

**体验现代 .NET 数据访问层的强大能力！**

</div>