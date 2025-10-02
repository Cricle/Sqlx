# SqlxDemo - Sqlx 完整功能演示

## 📋 项目简介

SqlxDemo 是一个完整的 Sqlx 功能演示项目，展示了 Sqlx 的所有核心特性和高级功能。

## ✨ 功能清单

### 🎯 核心功能演示

#### 1. **CRUD 占位符**
- ✅ `{{insert}}` - INSERT 语句简化
- ✅ `{{update}}` - UPDATE 语句简化
- ✅ `{{delete}}` - DELETE 语句简化

```csharp
// INSERT 示例
[Sqlx("{{insert}} ({{columns:auto|exclude=Id}}) VALUES ({{values:auto}})")]
Task<int> CreateUserAsync(string name, string email, int age);

// UPDATE 示例
[Sqlx("{{update}} SET {{set:auto|exclude=Id}} WHERE {{where:id}}")]
Task<int> UpdateUserAsync(int id, string name, string email);

// DELETE 示例
[Sqlx("{{delete}} WHERE {{where:id}}")]
Task<int> DeleteUserAsync(int id);
```

#### 2. **7个核心占位符**

| 占位符 | 功能 | 示例 |
|--------|------|------|
| `{{table}}` | 表名自动推断 | `FROM {{table}}` |
| `{{columns:auto}}` | 自动生成列名列表 | `SELECT {{columns:auto}}` |
| `{{values:auto}}` | 自动生成参数占位符 | `VALUES ({{values:auto}})` |
| `{{where:id}}` | WHERE条件生成 | `WHERE {{where:id}}` |
| `{{set:auto}}` | SET子句生成 | `SET {{set:auto}}` |
| `{{orderby}}` | ORDER BY子句 | `{{orderby:created_desc}}` |
| `{{limit}}` | 分页限制 | `{{limit:sqlite\|offset=0\|rows=10}}` |

**演示文件**: `Services/SimpleTemplateDemo.cs`

#### 3. **22个扩展占位符**

**条件查询类**
- `{{between}}` - 范围查询
- `{{like}}` - 模糊匹配
- `{{in}}` - 多值匹配
- `{{not_in}}` - 排除匹配
- `{{isnull}}` - NULL检查
- `{{notnull}}` - 非NULL检查
- `{{or}}` - OR逻辑组合

**日期时间类**
- `{{today}}` - 今天
- `{{week}}` - 本周
- `{{month}}` - 本月
- `{{year}}` - 今年

**字符串函数类**
- `{{contains}}` - 包含文本
- `{{startswith}}` - 以...开始
- `{{endswith}}` - 以...结束

**聚合函数类**
- `{{count}}` - COUNT聚合
- `{{sum}}` - SUM聚合
- `{{avg}}` - AVG聚合
- `{{max}}` - MAX聚合
- `{{min}}` - MIN聚合
- `{{distinct}}` - 去重

**高级功能类**
- `{{join}}` - JOIN连接
- `{{groupby}}` - GROUP BY分组

**演示文件**: `Services/EnhancedPlaceholderDemo.cs`

### 🌐 多数据库支持

支持 6 种主流数据库：

| 数据库 | 列分隔符 | 参数前缀 | 示例 |
|--------|---------|---------|------|
| **SQL Server** | `[column]` | `@` | `SELECT [Id] FROM [User]` |
| **MySQL** | `` `column` `` | `@` | ``SELECT `Id` FROM `User` `` |
| **PostgreSQL** | `"column"` | `$` | `SELECT "Id" FROM "User"` |
| **SQLite** | `[column]` | `$` | `SELECT [Id] FROM [User]` |
| **Oracle** | `"column"` | `:` | `SELECT "Id" FROM "User"` |
| **DB2** | `"column"` | `?` | `SELECT "Id" FROM "User"` |

### ⚡ 源代码生成

**特性**:
- ✅ 编译时代码生成
- ✅ 零运行时反射
- ✅ AOT 原生支持
- ✅ 强类型安全

**演示代码**:
```csharp
[TableName("user")]
[RepositoryFor(typeof(IUserService))]
public partial class UserService : IUserService
{
    // 实现由 Sqlx 源生成器自动生成
}
```

### 🚀 高级特性

#### 1. **异步和取消支持**
```csharp
[Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{where:id}}")]
Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken);
```

#### 2. **表达式转SQL**
```csharp
var query = ExpressionToSql<User>.Create(SqlDefine.SQLite)
    .Where(u => u.Age > 25 && u.IsActive)
    .OrderBy(u => u.Name)
    .Take(10);
```

#### 3. **参数化SQL**
```csharp
var sql = ParameterizedSql.Create(
    "SELECT * FROM [user] WHERE [age] > @age",
    new { age = 25 });
```

## 📁 项目结构

```
SqlxDemo/
├── Models/
│   ├── User.cs              # 用户实体
│   └── Product.cs           # 产品实体
├── Services/
│   ├── SimpleTemplateDemo.cs        # 7个核心占位符演示
│   ├── EnhancedPlaceholderDemo.cs   # 22个扩展占位符演示
│   └── DemoUserRepository.cs        # 源代码生成演示
├── Program.cs               # 主程序入口
└── README.md               # 本文件
```

## 🏃 运行演示

### 前置条件
- .NET 9.0 SDK 或更高版本
- Visual Studio 2022 或 JetBrains Rider

### 运行步骤

1. **编译项目**
```bash
dotnet build SqlxDemo.csproj
```

2. **运行演示**
```bash
dotnet run --project SqlxDemo.csproj
```

3. **发布AOT版本**
```bash
dotnet publish -c Release
```

## 📊 演示输出

运行程序后，您将看到以下演示：

1. ✅ **ParameterizedSql 直接执行**
   - 基本参数化查询
   - 复杂条件构建

2. ✅ **SqlTemplate 静态模板**
   - 模板解析
   - 参数绑定

3. ✅ **ExpressionToSql 动态查询**
   - 类型安全的LINQ查询
   - INSERT/UPDATE/DELETE操作

4. ✅ **源代码生成**
   - 编译时代码生成演示
   - 生成代码执行

5. ✅ **简化模板引擎**
   - 7个核心占位符实际运行
   - 性能监控

6. ✅ **增强占位符**
   - 22个扩展占位符实际运行
   - 多场景应用

## 📝 关键代码示例

### 基础CRUD操作

```csharp
public interface IUserService
{
    // CREATE
    [Sqlx("{{insert}} ({{columns:auto|exclude=Id}}) VALUES ({{values:auto}})")]
    Task<int> CreateAsync(User user);
    
    // READ
    [Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{where:id}}")]
    Task<User?> GetByIdAsync(int id);
    
    // UPDATE
    [Sqlx("{{update}} SET {{set:auto|exclude=Id}} WHERE {{where:id}}")]
    Task<int> UpdateAsync(User user);
    
    // DELETE
    [Sqlx("{{delete}} WHERE {{where:id}}")]
    Task<int> DeleteAsync(int id);
}

[TableName("user")]
[RepositoryFor(typeof(IUserService))]
public partial class UserService(IDbConnection connection) : IUserService;
```

### 复杂查询

```csharp
// 分页查询
[Sqlx("SELECT {{columns:auto}} FROM {{table}} {{orderby:created_desc}} {{limit:sqlite|offset=@offset|rows=@rows}}")]
Task<List<User>> GetPagedAsync(int offset, int rows);

// 聚合查询
[Sqlx("SELECT {{count:all}}, {{avg:salary}}, {{max:salary}} FROM {{table}} WHERE is_active = 1")]
Task<Statistics> GetStatisticsAsync();

// 条件查询
[Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE age BETWEEN @minAge AND @maxAge AND {{like:name|pattern=@pattern}}")]
Task<List<User>> SearchAsync(int minAge, int maxAge, string pattern);
```

## 🎯 学习路径

1. **初学者** 👉 `SimpleTemplateDemo.cs`
   - 从7个核心占位符开始
   - 理解基本的CRUD操作

2. **进阶者** 👉 `EnhancedPlaceholderDemo.cs`
   - 学习22个扩展占位符
   - 掌握高级查询技巧

3. **高级用户** 👉 `DemoUserRepository.cs`
   - 理解源代码生成机制
   - 自定义Repository实现

## 📚 相关文档

- [CRUD操作完整指南](../../docs/CRUD_PLACEHOLDERS_COMPLETE_GUIDE.md)
- [扩展占位符指南](../../docs/EXTENDED_PLACEHOLDERS_GUIDE.md)
- [多数据库模板引擎](../../docs/MULTI_DATABASE_TEMPLATE_ENGINE.md)
- [API完整参考](../../docs/API_REFERENCE.md)

## 💡 最佳实践

1. **优先使用占位符**
   ```csharp
   // ✅ 推荐
   [Sqlx("{{insert}} ({{columns:auto}}) VALUES ({{values:auto}})")]
   
   // ❌ 不推荐
   [Sqlx("INSERT INTO users (name, email) VALUES (@name, @email)")]
   ```

2. **合理使用exclude选项**
   ```csharp
   // 插入时排除自增ID
   [Sqlx("{{insert}} ({{columns:auto|exclude=Id}}) VALUES ({{values:auto}})")]
   
   // 更新时排除不可变字段
   [Sqlx("{{update}} SET {{set:auto|exclude=Id,CreatedAt}} WHERE {{where:id}}")]
   ```

3. **利用类型推断**
   ```csharp
   // {{where:auto}} 自动根据参数推断WHERE条件
   [Sqlx("SELECT {{columns:auto}} FROM {{table}} WHERE {{where:auto}}")]
   Task<List<User>> GetByNameAsync(string name);
   ```

## ⚡ 性能特点

- **编译时处理**: 所有模板在编译时处理，零运行时开销
- **无反射**: 完全避免运行时反射，性能优异
- **AOT兼容**: 支持Native AOT编译，启动快、内存占用小
- **缓存优化**: 智能缓存模板处理结果

## 🎊 总结

SqlxDemo 展示了 Sqlx 的完整功能：

✅ **23个智能占位符** - 覆盖所有常用SQL场景  
✅ **多数据库支持** - 写一次，处处运行  
✅ **源代码生成** - 编译时验证，零运行时开销  
✅ **类型安全** - 强类型检查，避免运行时错误  
✅ **AOT原生支持** - 极致性能和启动速度  
✅ **简洁易用** - 简单的API，快速上手  

**立即开始使用 Sqlx，享受现代化的数据访问体验！** 🚀
