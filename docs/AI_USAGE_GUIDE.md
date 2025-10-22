# Sqlx - AI 使用指南和原理说明

> 本文档专门为 AI 助手编写，帮助 AI 理解 Sqlx 框架的核心原理、设计决策、使用方式和常见陷阱。

---

## 📋 目录

1. [核心架构](#核心架构)
2. [关键设计决策](#关键设计决策)
3. [使用指南](#使用指南)
4. [常见陷阱和注意事项](#常见陷阱和注意事项)
5. [性能优化原理](#性能优化原理)
6. [代码生成流程](#代码生成流程)
7. [最佳实践](#最佳实践)

---

## 🏗️ 核心架构

### 1. 项目结构

```
Sqlx/
├── src/
│   ├── Sqlx/                    # 核心库 - 运行时组件
│   │   ├── SqlGen/              # SQL 生成引擎
│   │   ├── Annotations/         # 特性定义
│   │   └── (不包含拦截器)        # 已移除拦截器功能
│   │
│   └── Sqlx.Generator/          # 源生成器 - 编译时组件
│       ├── Core/                # 核心代码生成
│       ├── Analyzers/           # Roslyn 分析器
│       └── CSharpGenerator.cs   # 主入口
│
├── samples/
│   └── TodoWebApi/              # 唯一的示例项目
│
└── tests/
    ├── Sqlx.Tests/              # 单元测试 (617个测试)
    └── Sqlx.Benchmarks/         # 性能基准测试
```

### 2. 核心组件

#### 2.1 Sqlx (核心库)

**职责**: 运行时支持、SQL 生成引擎、特性定义

**关键类**:
- `SqlTemplate`: 纯模板类，不应实例化
- `ParameterizedSql`: SQL 和参数的容器
- `ExpressionToSql`: 表达式转 SQL（**非线程安全，短生命周期**）
- 各种 `Attribute`: `SqlxAttribute`, `RepositoryForAttribute`, `SqlDefineAttribute`

#### 2.2 Sqlx.Generator (源生成器)

**职责**: 编译时代码生成、分析、诊断

**关键类**:
- `CSharpGenerator`: 源生成器入口（继承 `ISourceGenerator`）
- `CodeGenerationService`: 核心代码生成服务
- `SharedCodeGenerationUtilities`: 共享的代码生成工具
- `PropertyOrderAnalyzer`: 属性顺序分析器 (SQLX001)

---

## 🎯 关键设计决策

### 1. **不要过度设计** ⚠️

Sqlx 的核心哲学是**简单、高效、直接**：
- ❌ 不要添加复杂的抽象层
- ❌ 不要引入反射
- ❌ 不要添加运行时缓存
- ✅ 编译时生成所有代码
- ✅ 直接使用 ADO.NET
- ✅ 零分配路径优化

### 2. **ExpressionToSql 是线程不安全的**

```csharp
// ❌ 错误：不要缓存或共享实例
private static ExpressionToSql _shared = new();

// ✅ 正确：每次使用时创建新实例
var converter = new ExpressionToSql(sqlDefine);
var sql = converter.ToSql(expression);
// 使用后立即丢弃，依赖 GC
```

**原因**: 
- 短生命周期对象，设计为即用即弃
- 内部状态不是线程安全的
- 不需要实现 `IDisposable`（没有非托管资源）

### 3. **强制启用追踪和指标**

从 2.0 版本开始，Sqlx **强制**启用 Activity 追踪和性能指标：

```csharp
// 生成的代码总是包含
using var activity = Activity.Current;
activity?.SetTag("sqlx.operation", "GetAllAsync");
activity?.SetTag("sqlx.sql", commandText);
// ... 执行 SQL ...
activity?.SetTag("sqlx.rows", result.Count);
```

**不要**尝试添加 `[DisableTracing]` 之类的特性，已被移除。

**性能影响**: 微小（< 1μs），可以忽略。

### 4. **硬编码序号访问是默认行为**

生成的代码使用硬编码索引访问列：

```csharp
// ✅ 生成的代码
while (reader.Read())
{
    result.Add(new User
    {
        Id = reader.GetInt32(0),      // 硬编码索引
        Name = reader.GetString(1),   // 硬编码索引
        Email = reader.GetString(2)   // 硬编码索引
    });
}
```

**要求**: 
- **Id 属性必须是第一个公共属性**
- C# 属性顺序必须与 SQL `SELECT {{columns}}` 列顺序一致
- 分析器 SQLX001 会在不符合时发出警告

### 5. **Partial 方法用于用户拦截**

生成的代码包含三个 partial 方法：

```csharp
// 用户可选实现
partial void OnExecuting(string operation, IDbCommand command);
partial void OnExecuted(string operation, IDbCommand command, object? result, long elapsedTicks);
partial void OnExecuteFail(string operation, IDbCommand command, Exception ex, long elapsedTicks);
```

**特点**:
- 未实现时，编译器会自动移除调用 → 零开销
- 允许用户完全控制拦截逻辑
- 不要在生成代码中"吞掉"异常

---

## 📖 使用指南

### 1. 基本用法

```csharp
// 1. 定义实体（Id 必须是第一个属性！）
[TableName("todos")]
public class Todo
{
    public int Id { get; set; }           // ⚠️ 必须第一个
    public string Title { get; set; }
    public bool IsCompleted { get; set; }
}

// 2. 定义接口
public interface ITodoRepository
{
    [Sqlx("SELECT {{columns}} FROM {{table}}")]
    Task<List<Todo>> GetAllAsync();
    
    [Sqlx("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values}})")]
    Task<int> CreateAsync(Todo todo);
}

// 3. 实现（源生成器自动生成）
[RepositoryFor(typeof(ITodoRepository))]
[SqlDefine(Dialect = SqlDialect.Sqlite, TableName = "todos")]
public partial class TodoRepository(IDbConnection connection) : ITodoRepository
{
    // 可选：实现 partial 方法
    partial void OnExecuting(string operation, IDbCommand command)
    {
        Console.WriteLine($"Executing: {command.CommandText}");
    }
}
```

### 2. 占位符系统

| 占位符 | 功能 | 示例输出 |
|--------|------|----------|
| `{{table}}` | 表名 | `todos` |
| `{{columns}}` | 所有列 | `id, title, is_completed` |
| `{{columns --exclude Id}}` | 排除列 | `title, is_completed` |
| `{{columns --only Title}}` | 仅包含列 | `title` |
| `{{values}}` | 参数占位符 | `@Title, @IsCompleted` |
| `{{set}}` | SET 子句 | `title=@Title, is_completed=@IsCompleted` |
| `{{set --exclude Id}}` | 排除 SET | `title=@Title, is_completed=@IsCompleted` |
| `{{orderby created_at --desc}}` | 排序 | `ORDER BY created_at DESC` |

**命名约定**:
- C# 属性名: `PascalCase` (如 `IsCompleted`)
- SQL 列名: `snake_case` (如 `is_completed`)
- 参数名: `@PascalCase` (如 `@IsCompleted`)

### 3. 多数据库支持

```csharp
[SqlDefine(Dialect = SqlDialect.Sqlite)]     // SQLite
[SqlDefine(Dialect = SqlDialect.SqlServer)]  // SQL Server
[SqlDefine(Dialect = SqlDialect.MySql)]      // MySQL
[SqlDefine(Dialect = SqlDialect.PostgreSql)] // PostgreSQL
[SqlDefine(Dialect = SqlDialect.Oracle)]     // Oracle
[SqlDefine(Dialect = SqlDialect.DB2)]        // DB2
```

**方言差异**自动处理：
- 列引号: SQLite `"col"`, MySQL `` `col` ``, SQL Server `[col]`
- 参数前缀: 大部分 `@param`, Oracle `:param`
- 分页语法: SQL Server `TOP/OFFSET-FETCH`, 其他 `LIMIT OFFSET`

---

## ⚠️ 常见陷阱和注意事项

### 1. ❌ Id 属性不是第一个

```csharp
// ❌ 错误
public class User
{
    public string Name { get; set; }
    public int Id { get; set; }      // SQLX001 警告
}

// ✅ 正确
public class User
{
    public int Id { get; set; }      // Id 必须第一个
    public string Name { get; set; }
}
```

**原因**: 硬编码序号访问，`Id` 总是假定在索引 0。

### 2. ❌ 属性顺序与 SQL 列顺序不一致

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

// ❌ 错误：列顺序不匹配
[Sqlx("SELECT id, email, name FROM users")]  // email 和 name 顺序错了
Task<List<User>> GetAllAsync();

// ✅ 正确：顺序一致
[Sqlx("SELECT id, name, email FROM users")]
Task<List<User>> GetAllAsync();

// ✅ 最佳：使用占位符
[Sqlx("SELECT {{columns}} FROM users")]
Task<List<User>> GetAllAsync();
```

### 3. ❌ 试图缓存或共享 ExpressionToSql

```csharp
// ❌ 错误：不要缓存
private static readonly ExpressionToSql _converter = new(...);

// ❌ 错误：不要共享
private ExpressionToSql _instance;

// ✅ 正确：每次创建新实例
var converter = new ExpressionToSql(sqlDefine);
var sql = converter.ToSql(expression);
```

### 4. ❌ 在生成代码中"吞掉"异常

```csharp
// ❌ 错误：不要吞异常
try
{
    // ... SQL 执行 ...
}
catch (Exception ex)
{
    // 吞掉异常，只记录
    Console.WriteLine(ex);
}

// ✅ 正确：让异常传播
try
{
    // ... SQL 执行 ...
}
catch (Exception ex)
{
    OnExecuteFail(operation, command, ex, elapsedTicks);
    throw;  // 重新抛出
}
finally
{
    command.Dispose();
}
```

### 5. ❌ 添加无意义的缓存

```csharp
// ❌ 错误：不要添加运行时缓存
private static readonly ConcurrentDictionary<Type, SqlCommand> _cache = new();

// ✅ 正确：让源生成器在编译时生成所有代码
```

**原因**: Sqlx 的整个设计就是为了**避免运行时开销**，所有逻辑都应该在编译时完成。

### 6. ❌ 尝试禁用追踪

```csharp
// ❌ 错误：这个特性不存在
[DisableTracing]
public partial class UserRepository { }

// ✅ 正确：接受追踪是默认行为
// 性能影响微乎其微（< 1μs）
```

---

## ⚡ 性能优化原理

### 1. 编译时代码生成

**零反射路径**:
```csharp
// 运行时不需要反射
while (reader.Read())
{
    result.Add(new User
    {
        Id = reader.GetInt32(0),      // 直接调用
        Name = reader.GetString(1),   // 不需要反射
        Email = reader.GetString(2)
    });
}
```

### 2. 硬编码序号访问

**避免 `GetOrdinal` 字符串查找**:
```csharp
// ❌ Dapper 等 ORM 的方式
var idOrd = reader.GetOrdinal("id");         // 字符串查找
var nameOrd = reader.GetOrdinal("name");
Id = reader.GetInt32(idOrd);

// ✅ Sqlx 的方式
Id = reader.GetInt32(0);  // 直接访问
Name = reader.GetString(1);
```

### 3. 智能 IsDBNull 检查

```csharp
// 只对 nullable 类型检查
Email = reader.IsDBNull(2) ? null : reader.GetString(2);

// 对非 nullable 类型直接读取
Id = reader.GetInt32(0);  // int 不会是 null
```

### 4. 命令自动释放

```csharp
try
{
    // ... 执行 SQL ...
}
finally
{
    command.Dispose();  // 确保释放
}
```

### 5. 性能数据

| 框架 | 延迟 | 内存分配 | 相对性能 |
|------|------|----------|----------|
| Raw ADO.NET | 6.434 μs | 1.17 KB | 100% (基准) |
| **Sqlx** | 7.371 μs | 1.21 KB | **比 Dapper 快 20%** |
| Dapper | 9.241 μs | 2.25 KB | 较慢 |

---

## 🔄 代码生成流程

### 1. 源生成器执行流程

```
用户代码 (Repository 接口和部分类)
    ↓
CSharpGenerator.Initialize()
    ↓
SyntaxReceiver 收集候选节点
    ↓
CSharpGenerator.Execute()
    ↓
AttributeHandler 识别特性
    ↓
CodeGenerationService 生成代码
    ↓
输出 .g.cs 文件
    ↓
编译到程序集
```

### 2. 生成的代码结构

```csharp
// UserRepository.g.cs
public partial class UserRepository
{
    // 实现接口方法
    public async Task<List<User>> GetAllAsync()
    {
        using var activity = Activity.Current;
        activity?.SetTag("sqlx.operation", "GetAllAsync");
        
        const string commandText = "SELECT id, name, email FROM users";
        activity?.SetTag("sqlx.sql", commandText);
        
        using var command = connection.CreateCommand();
        command.CommandText = commandText;
        
        var sw = Stopwatch.StartNew();
        try
        {
            OnExecuting("GetAllAsync", command);
            
            using var reader = await command.ExecuteReaderAsync();
            var result = new List<User>();
            
            while (await reader.ReadAsync())
            {
                result.Add(new User
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Email = reader.IsDBNull(2) ? null : reader.GetString(2)
                });
            }
            
            activity?.SetTag("sqlx.rows", result.Count);
            OnExecuted("GetAllAsync", command, result, sw.ElapsedTicks);
            
            return result;
        }
        catch (Exception ex)
        {
            OnExecuteFail("GetAllAsync", command, ex, sw.ElapsedTicks);
            throw;
        }
        finally
        {
            command.Dispose();
        }
    }
    
    // Partial 方法声明
    partial void OnExecuting(string operation, IDbCommand command);
    partial void OnExecuted(string operation, IDbCommand command, object? result, long elapsedTicks);
    partial void OnExecuteFail(string operation, IDbCommand command, Exception ex, long elapsedTicks);
}
```

---

## 💡 最佳实践

### 1. 实体设计

```csharp
// ✅ 最佳实践
[TableName("users")]
public class User
{
    public int Id { get; set; }              // 1. Id 第一个
    public string Name { get; set; }         // 2. 属性顺序与 SQL 一致
    public string? Email { get; set; }       // 3. 使用 nullable 引用类型
    public DateTime CreatedAt { get; set; }
}
```

### 2. Repository 设计

```csharp
// ✅ 最佳实践
public interface IUserRepository
{
    // 使用占位符，避免硬编码列名
    [Sqlx("SELECT {{columns}} FROM {{table}}")]
    Task<List<User>> GetAllAsync();
    
    // 排除自增 Id
    [Sqlx("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values}})")]
    Task<int> CreateAsync(User user);
    
    // 排除 Id 和时间戳
    [Sqlx("UPDATE {{table}} SET {{set --exclude Id CreatedAt}} WHERE id = @id")]
    Task<int> UpdateAsync(User user);
    
    // 清晰的参数命名
    [Sqlx("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(int id);
}
```

### 3. Partial 方法实现

```csharp
[RepositoryFor(typeof(IUserRepository))]
[SqlDefine(Dialect = SqlDialect.Sqlite, TableName = "users")]
public partial class UserRepository(IDbConnection connection) : IUserRepository
{
    // ✅ 日志记录
    partial void OnExecuting(string operation, IDbCommand command)
    {
        _logger.LogDebug("Executing {Operation}: {Sql}", operation, command.CommandText);
    }
    
    // ✅ 性能监控
    partial void OnExecuted(string operation, IDbCommand command, object? result, long elapsedTicks)
    {
        var ms = elapsedTicks * 1000.0 / Stopwatch.Frequency;
        if (ms > 1000) // 慢查询告警
        {
            _logger.LogWarning("Slow query {Operation}: {Ms}ms", operation, ms);
        }
    }
    
    // ✅ 错误处理
    partial void OnExecuteFail(string operation, IDbCommand command, Exception ex, long elapsedTicks)
    {
        _logger.LogError(ex, "Failed {Operation}: {Sql}", operation, command.CommandText);
    }
}
```

### 4. 连接管理

```csharp
// ✅ 使用 IDbConnection，不要使用具体类型
public partial class UserRepository(IDbConnection connection) : IUserRepository
{
    // 连接由调用者管理
}

// 使用时
using var connection = new SqliteConnection(connectionString);
await connection.OpenAsync();

using var transaction = connection.BeginTransaction();
try
{
    var repo = new UserRepository(connection);
    await repo.CreateAsync(user);
    await repo.UpdateAsync(user);
    transaction.Commit();
}
catch
{
    transaction.Rollback();
    throw;
}
```

---

## 🚫 不要做的事情清单

### 代码生成器相关

1. ❌ **不要**在源生成器中使用反射
2. ❌ **不要**在生成代码中添加复杂的抽象层
3. ❌ **不要**尝试添加拦截器框架（已移除）
4. ❌ **不要**添加特性来控制追踪（强制启用）
5. ❌ **不要**在生成代码中吞掉异常

### 运行时相关

6. ❌ **不要**缓存 `ExpressionToSql` 实例
7. ❌ **不要**共享 `ExpressionToSql` 实例
8. ❌ **不要**给 `ExpressionToSql` 添加 `IDisposable`
9. ❌ **不要**添加运行时缓存（如 `ConcurrentDictionary`）
10. ❌ **不要**使用 `ThreadStatic` 或 `AsyncLocal`

### 设计相关

11. ❌ **不要**过度设计
12. ❌ **不要**添加不必要的抽象
13. ❌ **不要**破坏硬编码序号访问的假设
14. ❌ **不要**忽略 SQLX001 警告

---

## 📊 测试覆盖率

当前测试状态：
- **总测试数**: 617
- **通过率**: 100%
- **测试时间**: ~18秒

主要测试模块：
- 代码生成: 200+ 测试
- 占位符系统: 80+ 测试
- 数据库方言: 85 测试
- Roslyn 分析器: 15 测试
- 源生成器核心: 43 测试

---

## 🎓 学习路径

对于 AI 助手，建议按以下顺序理解 Sqlx：

1. **先理解设计哲学**: 简单、高效、编译时
2. **再理解架构**: 源生成器 + 核心库
3. **然后理解占位符**: 如何自动生成 SQL
4. **最后理解优化**: 序号访问、零反射、Activity 追踪

---

## 📞 常见问题

### Q: 为什么不使用反射？
A: 反射有运行时开销，且不支持 AOT 编译。Sqlx 通过编译时代码生成实现零反射。

### Q: 为什么强制启用追踪？
A: 追踪开销极小（< 1μs），但提供了完整的可观测性。简化设计比微小的性能提升更重要。

### Q: 为什么 ExpressionToSql 不是线程安全的？
A: 它是短生命周期对象，设计为即用即弃。添加线程安全会增加复杂度和开销，违背设计原则。

### Q: 为什么要求 Id 是第一个属性？
A: 硬编码序号访问假定 Id 在索引 0，这是性能优化的关键。分析器会确保这个约定。

### Q: 能否添加更多的 ORM 特性（如延迟加载、变更追踪）？
A: **不能**。Sqlx 的定位是轻量级、高性能的数据访问层，不是全功能 ORM。如果需要这些特性，请使用 EF Core。

---

## 🎯 总结

Sqlx 的核心是：

1. **编译时代码生成** - 零反射、零运行时开销
2. **硬编码序号访问** - 最快的列访问方式
3. **简单直接** - 不过度设计、不添加不必要的抽象
4. **强制最佳实践** - Activity 追踪、Partial 方法、属性顺序
5. **多数据库支持** - 通过方言自动适配语法差异

**记住**: Sqlx 不是要替代所有 ORM，而是为需要**极致性能**和**编译时安全**的场景提供一个简单、高效的选择。

---

**文档版本**: 1.0  
**最后更新**: 2025-10-22  
**适用版本**: Sqlx 2.0+

