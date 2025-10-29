# Sqlx Visual Studio Extension - Complete Tutorial

> **从零开始，一步步掌握Sqlx VS Extension**

本教程将带您从安装到精通Sqlx Visual Studio Extension的所有功能。

---

## 📋 目录

- [第1课: 安装和设置](#第1课-安装和设置)
- [第2课: 第一个Repository](#第2课-第一个repository)
- [第3课: SQL语法着色](#第3课-sql语法着色)
- [第4课: 使用代码片段](#第4课-使用代码片段)
- [第5课: IntelliSense智能提示](#第5课-intellisense智能提示)
- [第6课: 工具窗口详解](#第6课-工具窗口详解)
- [第7课: 高级Repository功能](#第7课-高级repository功能)
- [第8课: 性能优化](#第8课-性能优化)
- [第9课: 调试技巧](#第9课-调试技巧)
- [第10课: 最佳实践](#第10课-最佳实践)

---

## 第1课: 安装和设置

### 系统要求

```
✅ Visual Studio 2022 (17.0+)
✅ Windows 10/11
✅ .NET 6.0+
✅ 100MB 磁盘空间
```

### 安装步骤

#### 步骤1: 安装Sqlx核心库

```bash
# 创建新项目
dotnet new console -n SqlxTutorial
cd SqlxTutorial

# 安装NuGet包
dotnet add package Sqlx
dotnet add package Sqlx.Generator
```

**验证安装:**
```xml
<!-- SqlxTutorial.csproj 应该包含: -->
<ItemGroup>
  <PackageReference Include="Sqlx" Version="0.4.0" />
  <PackageReference Include="Sqlx.Generator" Version="0.4.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

#### 步骤2: 安装VS Extension

**方法A: VSIX文件（推荐）**
```
1. 下载 Sqlx.Extension.vsix
2. 关闭所有Visual Studio实例
3. 双击 Sqlx.Extension.vsix
4. 按照安装向导操作
5. 重启Visual Studio
```

**方法B: Visual Studio Marketplace**
```
1. Visual Studio > Extensions > Manage Extensions
2. 搜索 "Sqlx"
3. 点击 Download
4. 重启Visual Studio完成安装
```

#### 步骤3: 验证安装

```
1. 打开 Visual Studio 2022
2. 打开 SqlxTutorial 项目
3. 检查: Tools 菜单应该有 "Sqlx" 选项
4. 点击 Tools > Sqlx > SQL Preview
5. 如果窗口打开，安装成功！✅
```

---

## 第2课: 第一个Repository

### 创建实体类

```csharp
// Models/User.cs
using System;

namespace SqlxTutorial.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
```

### 手动创建Repository

```csharp
// Repositories/IUserRepository.cs
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Sqlx;
using Sqlx.Annotations;
using SqlxTutorial.Models;

namespace SqlxTutorial.Repositories
{
    [RepositoryFor(typeof(User))]
    [SqlDefine(SqlDialect.SQLite)]
    public partial interface IUserRepository
    {
        [SqlTemplate("SELECT * FROM users WHERE id = @id")]
        Task<User?> GetByIdAsync(int id);

        [SqlTemplate("SELECT * FROM users")]
        Task<List<User>> GetAllAsync();

        [SqlTemplate("INSERT INTO users (name, email, created_at) VALUES (@name, @email, @createdAt)")]
        [ReturnInsertedId]
        Task<int> InsertAsync(string name, string email, DateTime createdAt);
    }
}
```

**注意观察:**
- `[SqlTemplate]` 中的SQL应该有颜色！
- 蓝色: SELECT, FROM, WHERE, INSERT, INTO, VALUES
- 橙色: （如果使用占位符）
- 青绿: @id, @name, @email

### 使用Quick Action自动生成

**更简单的方法:**

```csharp
// 1. 只写实体类 (User.cs)
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// 2. 将光标放在 "User" 类名上
// 3. 按 Ctrl + .
// 4. 选择 "Generate Repository for User"
// 5. 自动生成完整的Repository！✨
```

### 构建项目

```bash
dotnet build
```

**查看生成的代码:**
```
Tools > Sqlx > Generated Code
```

您应该看到完整的Repository实现！

---

## 第3课: SQL语法着色

### 颜色方案

Sqlx Extension提供5种颜色高亮：

```csharp
[SqlTemplate(@"
    SELECT id, name, email               -- SQL关键字 (蓝色)
    FROM users                           -- SQL关键字 (蓝色)
    WHERE name = @name                   -- @name 参数 (青绿色)
      AND created_at > '2024-01-01'      -- '...' 字符串 (棕色)
    -- 这是注释                          -- 注释 (绿色)
")]
Task<List<User>> SearchAsync(string name);
```

使用占位符时：

```csharp
[SqlTemplate(@"
    SELECT {{columns}}                   -- {{columns}} 占位符 (橙色)
    FROM {{table}}                       -- {{table}} 占位符 (橙色)
    WHERE {{where}}                      -- {{where}} 占位符 (橙色)
    LIMIT {{limit}}                      -- {{limit}} 占位符 (橙色)
")]
Task<List<User>> GetUsersAsync();
```

### 支持的格式

**单行字符串:**
```csharp
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
```

**多行字符串 (verbatim):**
```csharp
[SqlTemplate(@"
    SELECT * 
    FROM users 
    WHERE id = @id
")]
```

**字符串插值（C# 11+）:**
```csharp
[SqlTemplate($"""
    SELECT * 
    FROM users 
    WHERE id = @id
    """)]
```

**实时测试:**

打开示例文件查看效果：
```
File > Open > src/Sqlx.Extension/Examples/SyntaxColoringTestExample.cs
```

---

## 第4课: 使用代码片段

### 可用片段列表

| 片段 | 触发 | 生成内容 |
|------|------|----------|
| Repository接口 | `sqlx-repo` + Tab | 完整Repository定义 |
| 实体类 | `sqlx-entity` + Tab | 实体类模板 |
| SELECT查询 | `sqlx-select` + Tab | SELECT单个实体 |
| SELECT列表 | `sqlx-select-list` + Tab | SELECT多个实体 |
| INSERT | `sqlx-insert` + Tab | INSERT语句 |
| UPDATE | `sqlx-update` + Tab | UPDATE语句 |
| DELETE | `sqlx-delete` + Tab | DELETE语句 |
| 批量操作 | `sqlx-batch` + Tab | 批量INSERT |
| Expression查询 | `sqlx-expr` + Tab | 表达式查询 |
| COUNT | `sqlx-count` + Tab | COUNT查询 |
| EXISTS | `sqlx-exists` + Tab | EXISTS检查 |

### 使用示例

#### 示例1: 快速创建Repository

```csharp
// 1. 在Repositories文件夹创建新文件: IProductRepository.cs
// 2. 输入: sqlx-repo
// 3. 按 Tab
// 4. 填写占位符:
//    - RepositoryName: ProductRepository
//    - EntityType: Product
//    - KeyType: int
// 5. 完成！
```

生成的代码：

```csharp
[RepositoryFor(typeof(Product))]
[SqlDefine(SqlDialect.SQLite)]
public partial interface IProductRepository
{
    [SqlTemplate("SELECT * FROM products WHERE id = @id")]
    Task<Product?> GetByIdAsync(int id);
    
    [SqlTemplate("SELECT * FROM products")]
    Task<List<Product>> GetAllAsync();
}
```

#### 示例2: 添加CRUD方法

```csharp
// 在Repository接口内:
// 1. 输入: sqlx-insert
// 2. 按 Tab
// 3. 填写参数
```

生成：

```csharp
[SqlTemplate("INSERT INTO {table} ({columns}) VALUES ({values})")]
[ReturnInsertedId]
Task<int> InsertAsync(Product product);
```

#### 示例3: 创建实体类

```csharp
// 1. 输入: sqlx-entity
// 2. 按 Tab
// 3. 填写类名: Product
```

生成：

```csharp
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

---

## 第5课: IntelliSense智能提示

### 触发IntelliSense

#### 自动触发
```csharp
[SqlTemplate("SELECT {{ |")]  // 输入 {{ 后自动显示
                    ↑
```

#### 手动触发
```csharp
[SqlTemplate("SELECT |")]     // 按 Ctrl+Space
                    ↑
```

### IntelliSense项目

#### 1. 占位符 (9个)

```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}}")]
//                    ↑ 输入 {{ 会显示:
//                    - columns
//                    - table
//                    - values
//                    - set
//                    - where
//                    - limit
//                    - offset
//                    - orderby
//                    - batch_values
```

**完整示例:**

```csharp
[SqlTemplate(@"
    SELECT {{columns}}           -- 自动展开为所有列
    FROM {{table}}               -- 自动替换为表名
    WHERE {{where}}              -- 自动生成WHERE条件
    ORDER BY {{orderby}}         -- 自动生成排序
    LIMIT {{limit}}              -- 限制数量
    OFFSET {{offset}}            -- 偏移量
")]
Task<List<User>> GetUsersAsync(string? where = null, int? limit = null);
```

#### 2. 修饰符 (5个)

```csharp
[SqlTemplate("SELECT {{columns--exclude:password}}")]
//                            ↑ 输入 -- 会显示:
//                            - --exclude
//                            - --param
//                            - --value
//                            - --from
//                            - --desc
```

**使用示例:**

```csharp
// 排除列
[SqlTemplate("SELECT {{columns--exclude:password}} FROM users")]

// 参数化
[SqlTemplate("INSERT INTO users {{columns--param}} VALUES {{values--param}}")]

// 直接值
[SqlTemplate("INSERT INTO users ({{columns--value}}) VALUES ({{values--value}})")]

// 降序
[SqlTemplate("SELECT * FROM users ORDER BY {{orderby--desc}}")]
```

#### 3. SQL关键字 (30+)

```csharp
[SqlTemplate("SEL|")]  // 输入 SEL 会显示 SELECT
                ↑

// 完整列表:
// SELECT, INSERT, UPDATE, DELETE
// FROM, WHERE, JOIN, LEFT JOIN, RIGHT JOIN, INNER JOIN
// GROUP BY, ORDER BY, HAVING
// COUNT, SUM, AVG, MAX, MIN
// DISTINCT, UNION, CASE, WHEN, THEN, ELSE, END
// AS, ON, IN, NOT IN, EXISTS, NOT EXISTS
// AND, OR, NOT, LIKE, BETWEEN
```

#### 4. 参数提示

```csharp
[SqlTemplate("SELECT * FROM users WHERE name = @|")]
//                                              ↑ 输入 @ 会显示方法参数
Task<User?> GetByNameAsync(string name);  // 会建议: @name
```

### IntelliSense最佳实践

#### ✅ 好的用法

```csharp
// 1. 使用占位符让代码更简洁
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where}}")]
Task<List<User>> SearchAsync(string where);

// 2. 使用修饰符控制生成
[SqlTemplate("SELECT {{columns--exclude:password,salt}} FROM users")]
Task<List<User>> GetPublicUsersAsync();

// 3. 组合使用
[SqlTemplate(@"
    SELECT {{columns--exclude:password}}
    FROM {{table}}
    WHERE email = @email
    LIMIT {{limit}}
")]
Task<User?> FindByEmailAsync(string email, int limit = 1);
```

#### ❌ 避免的用法

```csharp
// 不要手写可以用占位符的内容
[SqlTemplate("SELECT id, name, email FROM users")]  // 应该用 {{columns}}

// 不要拼写错误占位符
[SqlTemplate("SELECT {{column}} FROM users")]  // 错误! 应该是 {{columns}}

// 不要忘记参数前缀
[SqlTemplate("SELECT * FROM users WHERE id = id")]  // 错误! 应该是 @id
```

---

## 第6课: 工具窗口详解

### 打开工具窗口

```
Tools > Sqlx > [选择窗口]
```

### 6.1 SQL Preview

**用途:** 实时查看生成的SQL

**使用步骤:**

```csharp
// 1. 写一个SqlTemplate方法
[SqlTemplate(@"
    SELECT {{columns--exclude:password}}
    FROM {{table}}
    WHERE email = @email
")]
Task<User?> GetByEmailAsync(string email);

// 2. 将光标放在方法上
// 3. 打开: Tools > Sqlx > SQL Preview
// 4. 查看生成的SQL:
```

**显示内容:**

```sql
-- 生成的SQL:
SELECT id, name, email, created_at
FROM users
WHERE email = @email

-- 参数:
@email (String)
```

**功能:**
- ✅ 实时预览
- ✅ 语法高亮
- ✅ 一键复制
- ✅ 刷新按钮

---

### 6.2 Generated Code Viewer

**用途:** 查看Roslyn生成的完整代码

**使用步骤:**

```csharp
// 1. 定义partial接口
public partial interface IUserRepository
{
    [SqlTemplate("SELECT * FROM users WHERE id = @id")]
    Task<User?> GetByIdAsync(int id);
}

// 2. Tools > Sqlx > Generated Code
// 3. 查看完整实现
```

**显示内容:**

```csharp
// Generated Code
public partial interface IUserRepository
{
    // 生成的实现类
    private class UserRepositoryImpl : IUserRepository
    {
        private readonly IDbConnection _connection;

        public UserRepositoryImpl(IDbConnection connection)
        {
            _connection = connection;
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            var sql = "SELECT * FROM users WHERE id = @id";
            // ... Dapper调用 ...
        }
    }
}
```

**功能:**
- ✅ 完整代码
- ✅ 语法高亮
- ✅ 可导航
- ✅ 复制代码

---

### 6.3 Query Tester

**用途:** 交互式测试SQL查询

**使用步骤:**

```
1. Tools > Sqlx > Query Tester
2. 填写连接字符串
3. 选择或输入SQL
4. 设置参数
5. 点击 "Execute"
6. 查看结果
```

**界面:**

```
┌─────────────────────────────────────┐
│ Connection String:                   │
│ [Data Source=test.db               ]│
├─────────────────────────────────────┤
│ SQL Template:                        │
│ [SELECT * FROM users WHERE id = @id]│
├─────────────────────────────────────┤
│ Parameters:                          │
│ @id (Int32): [1                    ]│
│ [+ Add Parameter]                   │
├─────────────────────────────────────┤
│ [ Execute ]  [ Clear ]              │
├─────────────────────────────────────┤
│ Results:                             │
│ ┌───┬──────┬─────────────────┐     │
│ │ Id│ Name │ Email           │     │
│ ├───┼──────┼─────────────────┤     │
│ │ 1 │ John │ john@example.com│     │
│ └───┴──────┴─────────────────┘     │
├─────────────────────────────────────┤
│ Execution Time: 23ms  Rows: 1      │
└─────────────────────────────────────┘
```

---

### 6.4 Repository Explorer

**用途:** 浏览和导航所有Repository

**界面:**

```
Repository Explorer
├── 📁 IUserRepository
│   ├── 🔍 GetByIdAsync(int id)
│   ├── 🔍 GetAllAsync()
│   ├── ➕ InsertAsync(User user)
│   ├── ✏️ UpdateAsync(User user)
│   └── ❌ DeleteAsync(int id)
├── 📁 IProductRepository
│   ├── 🔍 GetByIdAsync(int id)
│   └── 🔍 GetByCategoryAsync(string category)
└── 📁 IOrderRepository
    ├── 🔍 GetOrdersAsync()
    └── ➕ CreateOrderAsync(Order order)
```

**功能:**
- ✅ 树形视图
- ✅ 双击跳转到代码
- ✅ 显示方法签名
- ✅ 搜索过滤

---

### 6.5 SQL Execution Log

**用途:** 监控所有SQL执行

**界面:**

```
SQL Execution Log                    [ 🔴 Recording ]  [ Export CSV ]

Time     | Status | SQL                          | Duration | Rows
---------|--------|------------------------------|----------|------
10:23:45 |   ✅   | SELECT * FROM users WHERE... | 15ms     | 10
10:23:48 |   ✅   | INSERT INTO products...      | 8ms      | 1
10:23:50 |   ⚠️   | SELECT * FROM orders...      | 523ms    | 1000  ← 慢查询
10:23:52 |   ❌   | UPDATE users SET...          | ERROR    | 0

Statistics:
Total: 156  Success: 145  Warnings: 8  Errors: 3
Avg Duration: 45ms  Total Time: 7.2s
```

**功能:**
- ✅ 实时记录
- ✅ 颜色编码
- ✅ 慢查询高亮
- ✅ 详细信息面板
- ✅ 搜索过滤
- ✅ 导出CSV

---

### 6.6 Template Visualizer

**用途:** 可视化SQL模板设计器

**界面:**

```
┌──────────────┬────────────────────────────────┐
│ Components   │ Canvas                          │
├──────────────┤                                 │
│ 📊 SELECT    │   ┌─────────────────────┐      │
│ ➕ INSERT    │   │ SELECT Component     │      │
│ ✏️ UPDATE    │   │                      │      │
│ ❌ DELETE    │   │ Columns: [{{columns}}]     │
│ 🔍 WHERE     │   │ Table: [users       ]      │
│ 📋 ORDER BY  │   │                      │      │
│ 📏 LIMIT     │   └─────────────────────┘      │
│              │              ↓                  │
│              │   ┌─────────────────────┐      │
│              │   │ WHERE Component      │      │
│              │   │                      │      │
│              │   │ Condition: [id = @id]      │
│              │   └─────────────────────┘      │
└──────────────┴────────────────────────────────┘

Generated SQL:
SELECT {{columns}} FROM users WHERE id = @id
```

**使用:**
1. 从左侧拖拽组件
2. 配置参数
3. 查看实时预览
4. 复制生成的代码

---

### 6.7 Performance Analyzer

**用途:** 分析和优化查询性能

**界面:**

```
Performance Analyzer               [Time Range: Last 1 Hour ▼]

Metrics:
┌──────────────┬────────┬────────┬────────┬─────┐
│ Metric       │ Avg    │ Max    │ Min    │ QPS │
├──────────────┼────────┼────────┼────────┼─────┤
│ Query Time   │ 45ms   │ 523ms  │ 5ms    │ 12.3│
│ Rows Returned│ 25     │ 1000   │ 1      │  -  │
└──────────────┴────────┴────────┴────────┴─────┘

Slow Queries (>500ms):
┌─────────────────────────────┬──────────┬──────┐
│ SQL                         │ Duration │ Count│
├─────────────────────────────┼──────────┼──────┤
│ SELECT * FROM orders...     │ 523ms    │ 3    │
│ SELECT * FROM products...   │ 501ms    │ 1    │
└─────────────────────────────┴──────────┴──────┘

Performance Chart:
    ms
600 │                         ●
500 │                   ●     │
400 │             ●     │     │
300 │       ●     │     │     │
200 │ ●     │     │     │     │
100 │_│_____│_____│_____│_____│_____ time
    10:00  10:15  10:30  10:45  11:00
```

**优化建议:**
```
⚠️ Slow Query Detected!
SQL: SELECT * FROM orders WHERE user_id = @userId
Duration: 523ms
Suggestion:
  - Add index on orders.user_id
  - Use LIMIT to reduce result set
  - Consider caching
```

---

### 6.8 Entity Mapping Viewer

**用途:** 可视化实体与数据库映射

**界面:**

```
Entity Mapping Viewer

┌─────────────────┐         ┌──────────────────┐
│  C# Entity      │         │  Database Table  │
│  User           │         │  users           │
├─────────────────┤         ├──────────────────┤
│ 🔑 Id (int)     │────────→│ 🔑 id (INTEGER)  │
│ ✓ Name (string) │────────→│ ✓ name (TEXT)    │
│ ✓ Email (string)│────────→│ ✓ email (TEXT)   │
│ ✓ Age (int?)    │────────→│ ✓ age (INTEGER)  │
└─────────────────┘         └──────────────────┘

Mapping Details:
┌─────────┬──────────┬──────────┬─────────────┐
│ Property│ Column   │ Type     │ Nullable    │
├─────────┼──────────┼──────────┼─────────────┤
│ Id      │ id       │ int→INT  │ No          │
│ Name    │ name     │ str→TEXT │ No          │
│ Email   │ email    │ str→TEXT │ No          │
│ Age     │ age      │ int?→INT │ Yes         │
└─────────┴──────────┴──────────┴─────────────┘

✅ All mappings valid
```

---

### 6.9 SQL Breakpoints (Preview)

**用途:** SQL调试断点管理

**界面:**

```
SQL Breakpoints

┌──────────────────────────────────────────────────────┐
│ [+ Add Breakpoint]  [ Enable All ]  [ Clear All ]   │
├─────┬────────┬────────────────────────────┬─────────┤
│Enab │ Type   │ Location                    │ Hits    │
├─────┼────────┼────────────────────────────┼─────────┤
│ ☑   │ Line   │ UserRepo.GetByIdAsync:15   │ 5       │
│ ☑   │ Cond   │ UserRepo.InsertAsync:23    │ 2       │
│ ☐   │ HitCnt │ UserRepo.UpdateAsync:45    │ 0       │
└─────┴────────┴────────────────────────────┴─────────┘

Selected Breakpoint:
Type: Conditional
Location: IUserRepository.InsertAsync, Line 23
Condition: name == "admin"
Hit Count: 2
Enabled: ✅
```

---

### 6.10 SQL Watch (Preview)

**用途:** 监视SQL参数和结果

**界面:**

```
SQL Watch

┌──────────────────────────────────────────────────┐
│ [+ Add Watch]  [ Refresh ]                       │
├──────────┬─────────────┬───────────────────────────┤
│ Name     │ Type        │ Value                     │
├──────────┼─────────────┼───────────────────────────┤
│ @id      │ Parameter   │ 1                         │
│ @name    │ Parameter   │ "John"                    │
│ SQL      │ Generated   │ "SELECT * FROM users..." │
│ Result   │ Execution   │ { Id: 1, Name: "John" }  │
│ Duration │ Performance │ 15ms                      │
└──────────┴─────────────┴───────────────────────────┘
```

---

## 第7课: 高级Repository功能

### 7.1 使用新的Repository接口

从v0.5开始，Sqlx提供了10个专门的Repository接口：

```csharp
// 完整功能 (50+方法)
public partial interface IUserRepository : IRepository<User, int>
{
    // 自动继承所有方法!
}

// 只读 (查询+聚合)
public partial interface IUserQueryRepository : IReadOnlyRepository<User, int>
{
    // GetById, GetAll, GetWhere, Count, Sum等
}

// 批量操作
public partial interface IUserBulkRepository : IBulkRepository<User, int>
{
    // 查询 + 批量插入/更新/删除
}

// 只写 (CQRS)
public partial interface IUserCommandRepository : IWriteOnlyRepository<User, int>
{
    // Insert, Update, Delete + 批量操作
}
```

### 7.2 分页查询

```csharp
using Sqlx;

public partial interface IUserRepository : IQueryRepository<User, int>
{
    // 自动提供:
    // Task<PagedResult<User>> GetPageAsync(int pageIndex, int pageSize);
}

// 使用:
var page1 = await repository.GetPageAsync(pageIndex: 1, pageSize: 20);

Console.WriteLine($"Total: {page1.TotalCount}");  // 总数
Console.WriteLine($"Pages: {page1.TotalPages}");  // 总页数
Console.WriteLine($"Current: {page1.CurrentPage}");  // 当前页
Console.WriteLine($"Items: {page1.Items.Count}");  // 本页数量

foreach (var user in page1.Items)
{
    Console.WriteLine($"{user.Id}: {user.Name}");
}

// 下一页
if (page1.HasNextPage)
{
    var page2 = await repository.GetPageAsync(2, 20);
}
```

### 7.3 批量操作

```csharp
public partial interface IUserRepository : IBatchRepository<User, int>
{
    // 自动提供:
    // Task BatchInsertAsync(IEnumerable<User> entities);
    // Task<IEnumerable<int>> BatchInsertAndReturnIdsAsync(IEnumerable<User> entities);
    // Task BatchUpdateAsync(IEnumerable<User> entities);
    // Task BatchDeleteAsync(IEnumerable<int> ids);
}

// 使用:
var users = new List<User>
{
    new User { Name = "Alice", Email = "alice@example.com" },
    new User { Name = "Bob", Email = "bob@example.com" },
    new User { Name = "Charlie", Email = "charlie@example.com" }
};

// 批量插入 (快25倍!)
await repository.BatchInsertAsync(users);

// 批量插入并返回ID
var ids = await repository.BatchInsertAndReturnIdsAsync(users);
Console.WriteLine($"Inserted IDs: {string.Join(", ", ids)}");

// 批量删除
await repository.BatchDeleteAsync(new[] { 1, 2, 3 });
```

### 7.4 聚合查询

```csharp
public partial interface IUserRepository : IAggregateRepository<User, int>
{
    // 自动提供:
    // Task<int> CountAsync();
    // Task<int> CountWhereAsync(Expression<Func<User, bool>> predicate);
    // Task<TResult> SumAsync<TResult>(Expression<Func<User, TResult>> selector);
    // Task<TResult> AvgAsync<TResult>(Expression<Func<User, TResult>> selector);
    // Task<TResult> MaxAsync<TResult>(Expression<Func<User, TResult>> selector);
    // Task<TResult> MinAsync<TResult>(Expression<Func<User, TResult>> selector);
}

// 使用:
var totalUsers = await repository.CountAsync();
var activeUsers = await repository.CountWhereAsync(u => u.IsActive);
var avgAge = await repository.AvgAsync(u => u.Age);
var oldestUser = await repository.MaxAsync(u => u.Age);
```

### 7.5 高级查询 (Raw SQL)

```csharp
public partial interface IUserRepository : IAdvancedRepository<User, int>
{
    // 自动提供:
    // Task<IEnumerable<T>> ExecuteQueryAsync<T>(string sql, object parameters);
    // Task<int> ExecuteNonQueryAsync(string sql, object parameters);
    // Task TruncateTableAsync();
}

// 使用:
var results = await repository.ExecuteQueryAsync<User>(
    "SELECT * FROM users WHERE age > @minAge AND city = @city",
    new { minAge = 18, city = "Beijing" }
);

var affected = await repository.ExecuteNonQueryAsync(
    "DELETE FROM users WHERE inactive_days > @days",
    new { days = 365 }
);

// 清空表 (慎用!)
await repository.TruncateTableAsync();
```

---

## 第8课: 性能优化

### 8.1 使用Performance Analyzer

**步骤:**

```
1. Tools > Sqlx > Performance Analyzer
2. 选择时间范围
3. 查看慢查询
4. 点击查询查看详情
5. 按照建议优化
```

### 8.2 批量操作优化

**❌ 慢 (逐个插入):**

```csharp
// 1000条数据需要 ~5秒
foreach (var user in users)
{
    await repository.InsertAsync(user);  // 1000次数据库往返
}
```

**✅ 快 (批量插入):**

```csharp
// 1000条数据只需 ~200ms
await repository.BatchInsertAsync(users);  // 一次性插入
```

**性能对比:**
```
逐个: 5000ms
批量: 200ms
提升: 25倍! 🚀
```

### 8.3 使用索引

**查找慢查询:**

```
1. Performance Analyzer显示:
   SELECT * FROM users WHERE email = @email (523ms)

2. 添加索引:
   CREATE INDEX idx_users_email ON users(email);

3. 再次查询:
   SELECT * FROM users WHERE email = @email (5ms)

4. 提升: 100倍! 🚀
```

### 8.4 选择合适的Repository接口

**场景1: 只需要读取**

```csharp
// ❌ 使用完整接口 (包含不需要的写方法)
public partial interface IUserRepository : IRepository<User, int>
{
    // 50+方法，但你只用了5个
}

// ✅ 使用只读接口
public partial interface IUserRepository : IReadOnlyRepository<User, int>
{
    // 只有查询和聚合方法
}
```

**好处:**
- 更清晰的接口
- 更好的编译时检查
- 更容易测试

**场景2: 批量数据导入**

```csharp
// ✅ 使用批量接口
public partial interface IUserBulkRepository : IBulkRepository<User, int>
{
    // 查询 + 批量操作
}

// 使用:
var users = await LoadFromCSV("users.csv");  // 10万行
await bulkRepo.BatchInsertAsync(users);      // 秒级完成
```

### 8.5 连接池

**✅ 使用连接池:**

```csharp
// 依赖注入配置
services.AddScoped<IDbConnection>(sp => 
{
    var conn = new SqlConnection(connectionString);
    conn.Open();  // 预热连接
    return conn;
});

// 连接池自动管理
```

**❌ 不要每次都创建新连接:**

```csharp
// 每次调用都创建新连接 - 慢!
public async Task<User> GetUser(int id)
{
    using (var conn = new SqlConnection(connectionString))  // 每次创建
    {
        // ...
    }
}
```

---

## 第9课: 调试技巧

### 9.1 使用SQL Preview

**问题:** 生成的SQL不是我想要的

**解决:**

```csharp
// 1. 写SqlTemplate
[SqlTemplate(@"
    SELECT {{columns--exclude:password}}
    FROM {{table}}
    WHERE age > @minAge
    ORDER BY {{orderby--desc}}
")]
Task<List<User>> GetUsers(int minAge, string orderby = "id");

// 2. Tools > Sqlx > SQL Preview
// 3. 查看实际SQL:

/*
SELECT id, name, email, created_at
FROM users
WHERE age > @minAge
ORDER BY id DESC
*/

// 4. 调整直到满意
```

### 9.2 使用Query Tester

**问题:** 不确定SQL是否正确

**解决:**

```
1. Tools > Sqlx > Query Tester
2. 粘贴SQL
3. 设置测试参数
4. Execute
5. 查看结果
6. 修改直到正确
7. 复制回代码
```

### 9.3 使用SQL Execution Log

**问题:** 某个查询突然变慢了

**解决:**

```
1. Tools > Sqlx > SQL Execution Log
2. 开启Recording
3. 运行应用
4. 查找慢查询 (黄色/红色)
5. 点击查看详情
6. 分析SQL
7. 优化
```

### 9.4 使用Entity Mapping Viewer

**问题:** 类型转换错误

**解决:**

```
1. Tools > Sqlx > Entity Mapping Viewer
2. 选择有问题的实体
3. 查看映射
4. 检查类型匹配:
   - C# int → SQL INTEGER ✅
   - C# string → SQL TEXT ✅
   - C# DateTime → SQL TEXT ❌ (应该是 DATETIME)
5. 修正实体或表结构
```

### 9.5 查看Generated Code

**问题:** 想了解底层实现

**解决:**

```
1. Tools > Sqlx > Generated Code
2. 查看完整的生成代码
3. 理解工作原理
4. 必要时手动实现特殊逻辑
```

---

## 第10课: 最佳实践

### 10.1 Repository设计

**✅ 好的设计:**

```csharp
// 1. 使用合适的接口
public partial interface IUserRepository : IReadOnlyRepository<User, int>
{
    // 只读操作
}

// 2. 方法命名清晰
[SqlTemplate("SELECT * FROM users WHERE is_active = 1")]
Task<List<User>> GetActiveUsersAsync();  // 清晰

// 3. 参数验证
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
Task<User?> GetByIdAsync([Range(1, int.MaxValue)] int id);

// 4. 返回类型明确
Task<User?> GetByIdAsync(int id);  // 可能为null
Task<List<User>> GetAllAsync();    // 永不为null
```

**❌ 不好的设计:**

```csharp
// 1. 接口太大
public partial interface IUserRepository : IRepository<User, int>
{
    // 50+方法，但只用了5个
}

// 2. 方法命名模糊
Task<List<User>> Get();  // Get什么?

// 3. 没有参数验证
Task<User> GetUser(int id);  // id可以是-1吗?

// 4. 返回类型不明确
Task<User> GetUser(int id);  // 找不到时返回什么?
```

### 10.2 SQL模板编写

**✅ 好的模板:**

```csharp
// 1. 使用占位符
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where}}")]

// 2. 适当的缩进
[SqlTemplate(@"
    SELECT id, name, email
    FROM users
    WHERE is_active = 1
    ORDER BY created_at DESC
    LIMIT @limit
")]

// 3. 参数化
[SqlTemplate("WHERE email = @email")]  // ✅ 防SQL注入

// 4. 添加注释
[SqlTemplate(@"
    SELECT *
    FROM users
    WHERE created_at > @since  -- 只获取最近用户
      AND is_active = 1        -- 只要活跃用户
")]
```

**❌ 不好的模板:**

```csharp
// 1. 手写列名
[SqlTemplate("SELECT id, name, email, age, city, ... FROM users")]

// 2. 没有缩进
[SqlTemplate("SELECT * FROM users WHERE age > @age AND city = @city AND is_active = 1 ORDER BY id DESC LIMIT @limit")]

// 3. 字符串拼接 (危险!)
[SqlTemplate($"WHERE email = '{email}'")]  // ❌ SQL注入风险!

// 4. 没有注释
[SqlTemplate("WHERE DATEDIFF(NOW(), created_at) < 7 AND status = 1")]  // 什么意思?
```

### 10.3 性能优化

**✅ 最佳实践:**

```csharp
// 1. 使用批量操作
await repository.BatchInsertAsync(users);

// 2. 使用分页
var page = await repository.GetPageAsync(1, 20);

// 3. 只查询需要的列
[SqlTemplate("SELECT {{columns--exclude:password,salt}} FROM users")]

// 4. 使用适当的索引
// CREATE INDEX idx_users_email ON users(email);

// 5. 使用连接池
services.AddScoped<IDbConnection>(...);
```

### 10.4 错误处理

**✅ 好的错误处理:**

```csharp
public async Task<User?> GetUserSafelyAsync(int id)
{
    try
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id));

        return await repository.GetByIdAsync(id);
    }
    catch (SqlException ex)
    {
        _logger.LogError(ex, "Database error getting user {UserId}", id);
        return null;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error getting user {UserId}", id);
        throw;
    }
}
```

### 10.5 测试

**✅ 编写测试:**

```csharp
public class UserRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsUser()
    {
        // Arrange
        var repository = CreateTestRepository();
        var testUser = await repository.InsertAsync(new User 
        { 
            Name = "Test", 
            Email = "test@example.com" 
        });

        // Act
        var result = await repository.GetByIdAsync(testUser.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        var repository = CreateTestRepository();

        // Act
        var result = await repository.GetByIdAsync(-1);

        // Assert
        Assert.Null(result);
    }
}
```

---

## 🎓 恭喜完成教程！

您现在已经掌握了Sqlx Visual Studio Extension的所有核心功能：

### ✅ 您学会了:

1. 安装和设置Sqlx和Extension
2. 创建Repository (手动和自动)
3. 使用SQL语法着色
4. 使用代码片段加速开发
5. 使用IntelliSense (44+项)
6. 使用14个工具窗口
7. 使用高级Repository接口
8. 性能优化技巧
9. 调试和故障排除
10. 最佳实践

### 📚 下一步:

- **深入学习**: [API参考](docs/API_REFERENCE.md)
- **高级功能**: [高级特性](docs/ADVANCED_FEATURES.md)
- **最佳实践**: [最佳实践指南](docs/BEST_PRACTICES.md)
- **示例项目**: [samples/](samples/)

### 🆘 需要帮助？

- **文档**: https://cricle.github.io/Sqlx/
- **快速参考**: [QUICK_REFERENCE.md](QUICK_REFERENCE.md)
- **故障排除**: [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
- **Issues**: https://github.com/Cricle/Sqlx/issues

### 🌟 享受Sqlx！

**22倍开发效率提升等着您！** 🚀

---

**Happy Coding!** 😊


