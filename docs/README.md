# Sqlx 3.0 文档中心

欢迎来到Sqlx 3.0文档中心！这里提供完整的使用指南和参考资料。

## 📚 文档导航

### 🚀 入门指南
| 文档 | 描述 | 适合人群 |
|------|------|----------|
| [快速开始](QUICK_START_GUIDE.md) | 5分钟掌握核心用法 | 新用户 |
| [API参考](API_REFERENCE.md) | 完整的API文档 | 所有用户 |
| [最佳实践](BEST_PRACTICES.md) | 推荐的使用模式 | 进阶用户 |

### 🔧 深入学习  
| 文档 | 描述 | 适合人群 |
|------|------|----------|
| [高级功能](ADVANCED_FEATURES.md) | AOT优化、性能调优等 | 高级用户 |
| [迁移指南](MIGRATION_GUIDE.md) | 从旧版本迁移 | 升级用户 |
| [项目结构](PROJECT_STRUCTURE.md) | 代码组织和架构 | 贡献者 |

## 🎯 三种核心使用模式

Sqlx 3.0专注于三种简单而强大的使用模式：

### 1️⃣ 直接执行 - 最简单
```csharp
var sql = ParameterizedSql.Create(
    "SELECT * FROM Users WHERE Age > @age", 
    new { age = 18 });
string result = sql.Render();
```

### 2️⃣ 静态模板 - 可重用  
```csharp
var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Age > @age");
var young = template.Execute(new { age = 18 });
var senior = template.Execute(new { age = 65 });
```

### 3️⃣ 动态模板 - 类型安全
```csharp
var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Where(u => u.Age > 25 && u.IsActive)
    .Select(u => new { u.Name, u.Email })
    .OrderBy(u => u.Name)
    .Take(10);
string sql = query.ToSql();
```

## 🏗️ 核心组件

### ParameterizedSql
参数化SQL执行实例，表示带参数的SQL语句。
- `Create()` - 创建实例
- `Render()` - 渲染最终SQL

### SqlTemplate  
可重用的SQL模板，支持参数绑定。
- `Parse()` - 解析SQL为模板
- `Execute()` - 执行并绑定参数
- `Bind()` - 流式参数绑定

### ExpressionToSql<T>
类型安全的查询构建器，支持LINQ表达式。
- `Create()` - 创建构建器
- `Where()` - 添加条件
- `Select()` - 选择列
- `OrderBy()` - 排序
- `ToSql()` - 生成SQL

### SqlDefine
预定义的数据库方言。
- `SqlServer` - SQL Server方言
- `MySql` - MySQL方言  
- `PostgreSql` - PostgreSQL方言
- `SQLite` - SQLite方言

## 🚀 核心特性

### ✅ AOT兼容
- 零反射调用
- 编译时代码生成
- Native AOT支持

### ✅ 类型安全
- 编译时验证
- 强类型映射  
- LINQ表达式支持

### ✅ 高性能
- 模板重用机制
- 参数化查询
- 最小化内存分配

### ✅ 多数据库
- SQL Server
- MySQL
- PostgreSQL  
- SQLite
- Oracle (部分支持)

## 📋 快速参考

### 常用操作

#### SELECT查询
```csharp
// 简单查询
var users = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Where(u => u.IsActive)
    .ToSql();

// 复杂查询
var query = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Select(u => new { u.Name, u.Email })
    .Where(u => u.Age > 18)
    .OrderBy(u => u.Name)
    .Take(10);
```

#### INSERT操作
```csharp
// 指定列插入（推荐）
var insert = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .InsertInto(u => new { u.Name, u.Email })
    .Values("John", "john@example.com");

// 批量插入
var batchInsert = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .InsertInto(u => new { u.Name, u.Email })
    .Values("John", "john@example.com")
    .AddValues("Jane", "jane@example.com");
```

#### UPDATE操作
```csharp
var update = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Update()
    .Set(u => u.Name, "New Name")
    .Set(u => u.Age, u => u.Age + 1)
    .Where(u => u.Id == 1);
```

#### DELETE操作
```csharp
var delete = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Delete(u => u.IsActive == false);
```

### 模板使用

#### 基础模板
```csharp
var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id");
var user = template.Execute(new { id = 1 });
```

#### 流式绑定
```csharp
var result = template.Bind()
    .Param("id", 1)
    .Param("active", true)
    .Build();
```

#### 模板转换
```csharp
// 动态查询转模板
var template = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .UseParameterizedQueries()
    .Where(u => u.Age > 25)
    .ToTemplate();
```

## 🎯 选择指南

### 何时使用直接执行？
- 简单的一次性查询
- 固定的SQL语句
- 不需要重复使用

### 何时使用静态模板？
- 需要重复执行的SQL
- 参数会变化的查询
- 复杂的业务SQL

### 何时使用动态模板？
- 需要动态构建条件
- 搜索和筛选功能
- 类型安全要求高

## 📈 性能提示

### ✅ 推荐做法
```csharp
// 模板重用
var template = SqlTemplate.Parse(sql);
var result1 = template.Execute(params1);
var result2 = template.Execute(params2);

// 参数化查询
var query = ExpressionToSql<T>.Create(dialect)
    .UseParameterizedQueries()
    .Where(predicate);

// 显式列选择
.InsertInto(u => new { u.Name, u.Email })  // AOT友好
```

### ❌ 避免做法
```csharp
// 每次创建新实例
var sql1 = ParameterizedSql.Create(sql, params1);
var sql2 = ParameterizedSql.Create(sql, params2);

// 在AOT中使用反射
.InsertIntoAll()  // 使用反射，不推荐AOT

// 字符串拼接
var sql = $"SELECT * FROM Users WHERE Name = '{name}'";  // SQL注入风险
```

## 🛡️ 安全提醒

### 始终使用参数化查询
```csharp
// ✅ 安全
var query = ParameterizedSql.Create(
    "SELECT * FROM Users WHERE Name = @name", 
    new { name = userInput });

// ❌ 危险
var query = $"SELECT * FROM Users WHERE Name = '{userInput}'";
```

### 验证输入
```csharp
public static void ValidateId(int id)
{
    if (id <= 0)
        throw new ArgumentException("ID must be positive");
}
```

## 🔗 相关链接

- [GitHub 仓库](https://github.com/your-repo/sqlx)
- [NuGet 包](https://www.nuget.org/packages/Sqlx/)
- [问题反馈](https://github.com/your-repo/sqlx/issues)
- [讨论区](https://github.com/your-repo/sqlx/discussions)

## 📝 贡献

欢迎贡献代码和文档！请查看 [贡献指南](../CONTRIBUTING.md)。

---

**开始您的Sqlx 3.0之旅，体验极简现代的.NET数据访问！**