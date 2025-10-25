# Sqlx业务优先改进计划 v2
## 目标：让用户更关注业务而不是SQL

**设计原则：**
1. ✅ **Expression表达式优先** - 用C#表达式代替SQL字符串
2. ✅ **EF Core风格特性** - 模仿EF Core的软删除、审计、并发控制
3. ✅ **显式特性标记** - 不自动推断，用户必须明确标记意图
4. ✅ **聚焦CRUD增强** - 不做CQRS、事件溯源等架构模式
5. ✅ **编译时优化** - 零运行时开销，所有逻辑在源生成器完成

---

## 📊 现状分析

### ✅ 已经做得很好
1. **ICrudRepository** - 8个标准CRUD方法零配置
2. **ExpressionToSql** - WHERE子句支持Expression
3. **多数据库支持** - 5种数据库自动适配
4. **编译时生成** - 零反射，AOT友好

### ❌ 仍需改进的痛点

#### 痛点1: Insert不返回ID 🔥🔥🔥🔥🔥
```csharp
// 现状：需要手写SQL获取ID
await repo.InsertAsync(todo);
using var cmd = conn.CreateCommand();
cmd.CommandText = "SELECT last_insert_rowid()";
var newId = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
```
**影响**: 严重 - 几乎每个Insert都需要ID

#### 痛点2: 集合参数支持不足 🔥🔥🔥🔥🔥
```csharp
// 痛点2.1：IN查询需要手动构造
var idsJson = $"[{string.Join(",", ids)}]";
await repo.UpdateWhereIdsAsync(idsJson, newValue);

// 痛点2.2：批量插入不够灵活
await repo.BatchInsertAsync(entities);  // 只支持List<T>，不支持IEnumerable<T>

// 痛点2.3：Expression中不支持Contains
await repo.GetWhereAsync(t => ids.Contains(t.Id));  // 不支持！

// 痛点2.4：批量操作可能超过数据库参数限制
var largeList = Enumerable.Range(1, 5000).ToList();
await repo.BatchInsertAsync(largeList);  // SQL Server限制2100个参数！
```
**影响**: 严重 - 集合操作是业务开发的常见场景

#### 痛点3: 条件查询仍需SQL模板 🔥🔥🔥🔥🔥
```csharp
// 现状：需要写SQL模板字符串
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE title LIKE @query")]
Task<List<Todo>> SearchAsync(string query);
```
**影响**: 严重 - 自定义查询时需要SQL知识

#### 痛点4: 缺少常见业务模式 🔥🔥🔥
- 软删除（is_deleted标记）
- 审计字段（created_at, updated_at, created_by, updated_by）
- 乐观锁（version/row_version字段）

---

## 🎯 改进计划

### 阶段1: 核心CRUD增强（高优先级）

#### 1.1 Insert返回新插入的ID/实体 ⭐⭐⭐⭐⭐

**目标**: 自动获取新插入记录的ID，无需手写SQL

**实现方案**

##### 方案A: 扩展ICrudRepository（推荐）
```csharp
public interface ICrudRepository<TEntity, TKey>
{
    // 现有方法保持不变
    Task<int> InsertAsync(TEntity entity);

    // 新增：返回新插入的ID
    [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
    [ReturnInsertedId]  // 新特性：标记返回ID
    Task<TKey> InsertAndGetIdAsync(TEntity entity);

    // 新增：返回完整实体（含ID）
    [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
    [ReturnInsertedEntity]  // 新特性：标记返回实体
    Task<TEntity> InsertAndReturnAsync(TEntity entity);
}
```

##### 方案B: 修改现有InsertAsync行为（可选，破坏性）
```csharp
public interface ICrudRepository<TEntity, TKey>
{
    // 修改：直接设置entity的Id属性
    [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
    [SetEntityId]  // 新特性：自动设置entity.Id
    Task InsertAsync(TEntity entity);  // entity.Id会被自动赋值
}
```

**多数据库支持**
```csharp
// 源生成器根据数据库方言生成不同代码：
// PostgreSQL/SQLite: RETURNING id
// SQL Server: OUTPUT INSERTED.id
// MySQL: LAST_INSERT_ID()
// Oracle: RETURNING id INTO :out_id
```

**实现要点**
- 源生成器识别 `[ReturnInsertedId]` 特性
- 自动生成获取ID的代码（多数据库适配）
- 支持自增ID和GUID主键
- 编译时检查实体是否有Id属性

---

#### 1.2 Expression参数支持 ⭐⭐⭐⭐⭐

**目标**: 用C#表达式代替SQL WHERE字符串

**实现方案**

##### 方案A: 扩展现有方法，支持Expression参数
```csharp
public interface ITodoRepository : ICrudRepository<Todo, long>
{
    // 方法1：Expression作为WHERE条件
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where}} {{orderby}} {{limit}}")]
    Task<List<Todo>> GetWhereAsync(
        Expression<Func<Todo, bool>> where,
        string? orderBy = null,
        int? limit = null);

    // 方法2：DELETE with Expression
    [SqlTemplate("DELETE FROM {{table}} WHERE {{where}}")]
    Task<int> DeleteWhereAsync(Expression<Func<Todo, bool>> where);

    // 方法3：UPDATE with Expression
    [SqlTemplate("UPDATE {{table}} SET {{set}} WHERE {{where}}")]
    Task<int> UpdateWhereAsync(
        Expression<Func<Todo, bool>> where,
        Expression<Func<Todo, Todo>> update);  // 新：支持SET表达式

    // 方法4：COUNT with Expression
    [SqlTemplate("SELECT COUNT(*) FROM {{table}} WHERE {{where}}")]
    Task<int> CountWhereAsync(Expression<Func<Todo, bool>> where);
}

// 使用示例
var results = await repo.GetWhereAsync(
    where: t => t.IsCompleted == false && t.Priority >= 3,
    orderBy: "priority DESC",
    limit: 10);

await repo.DeleteWhereAsync(t => t.CompletedAt < DateTime.Now.AddDays(-30));

await repo.UpdateWhereAsync(
    where: t => t.Priority == 1,
    update: t => new Todo { Priority = 2, UpdatedAt = DateTime.Now });
```

##### 方案B: 批量操作支持Expression和集合参数
```csharp
// 现状：需要手动构造JSON
var idsJson = $"[{string.Join(",", ids)}]";
await repo.BatchUpdateAsync(idsJson, ...);

// 改进1：直接接受集合
[SqlTemplate("UPDATE {{table}} SET priority = @priority WHERE id IN {{values @ids}}")]
Task<int> BatchUpdatePriorityAsync(IEnumerable<long> ids, int priority);
// 源生成器自动生成：WHERE id IN (@id_0, @id_1, @id_2, ...)

// 改进2：使用Expression
[SqlTemplate("UPDATE {{table}} SET priority = @priority WHERE {{where}}")]
Task<int> UpdatePriorityWhereAsync(
    Expression<Func<Todo, bool>> where,
    int priority);
```

**实现要点**
- 源生成器检测 `Expression<Func<T, bool>>` 参数
- 使用现有 `ExpressionToSql` 类解析表达式
- `{{where}}` 占位符自动绑定Expression参数
- 编译时验证表达式的属性名是否存在
- 支持AND、OR、比较、Contains、StartsWith等常用操作

---

#### 1.3 EF Core风格业务模式特性 ⭐⭐⭐⭐

**目标**: 模仿EF Core，提供软删除、审计、并发控制

##### 特性1: 软删除 `[SoftDelete]`

**定义**
```csharp
namespace Sqlx.Annotations;

/// <summary>
/// Marks entity for soft delete pattern
/// DELETE operations set flag instead of physical delete
/// Queries automatically filter deleted records
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class SoftDeleteAttribute : Attribute
{
    /// <summary>Soft delete column name (default: "IsDeleted")</summary>
    public string ColumnName { get; set; } = "IsDeleted";

    /// <summary>Deleted value (default: true)</summary>
    public object DeletedValue { get; set; } = true;

    /// <summary>Not deleted value (default: false)</summary>
    public object NotDeletedValue { get; set; } = false;
}
```

**使用示例**
```csharp
[SoftDelete(ColumnName = "IsDeleted")]  // 显式标记
public class User
{
    public long Id { get; set; }
    public string Name { get; set; }
    public bool IsDeleted { get; set; }  // 软删除标记字段
}

// 使用：
await repo.DeleteAsync(id);
// 实际生成SQL：UPDATE users SET is_deleted = 1 WHERE id = @id

await repo.GetAllAsync();
// 实际生成SQL：SELECT * FROM users WHERE is_deleted = 0

// 如需真实删除，使用：
[SqlTemplate("DELETE FROM {{table}} WHERE id = @id")]
[IgnoreSoftDelete]  // 新特性：忽略软删除过滤
Task<int> HardDeleteAsync(long id);
```

**实现要点**
- 源生成器检测 `[SoftDelete]` 特性
- 自动修改所有CRUD操作的SQL
  - DELETE → UPDATE SET is_deleted = 1
  - SELECT → WHERE is_deleted = 0
  - COUNT → WHERE is_deleted = 0
- 可通过 `[IgnoreSoftDelete]` 方法特性临时禁用
- 编译时验证实体是否有对应的标记字段

##### 特性2: 审计字段 `[AuditFields]`

**定义**
```csharp
namespace Sqlx.Annotations;

/// <summary>
/// Auto-populate audit fields on Insert/Update
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class AuditFieldsAttribute : Attribute
{
    /// <summary>CreatedAt column name (default: "CreatedAt")</summary>
    public string CreatedAtColumn { get; set; } = "CreatedAt";

    /// <summary>UpdatedAt column name (default: "UpdatedAt")</summary>
    public string UpdatedAtColumn { get; set; } = "UpdatedAt";

    /// <summary>CreatedBy column name (default: "CreatedBy")</summary>
    public string? CreatedByColumn { get; set; } = "CreatedBy";

    /// <summary>UpdatedBy column name (default: "UpdatedBy")</summary>
    public string? UpdatedByColumn { get; set; } = "UpdatedBy";

    /// <summary>Use UTC time (default: true)</summary>
    public bool UseUtc { get; set; } = true;
}

/// <summary>
/// Provides current user for audit fields
/// </summary>
public interface IAuditProvider
{
    string GetCurrentUser();
}
```

**使用示例**
```csharp
[AuditFields(CreatedByColumn = "CreatedBy", UpdatedByColumn = "UpdatedBy")]
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }  // Insert时自动设置
    public DateTime UpdatedAt { get; set; }  // Insert/Update时自动设置
    public string? CreatedBy { get; set; }   // Insert时从IAuditProvider获取
    public string? UpdatedBy { get; set; }   // Update时从IAuditProvider获取
}

// 源生成代码会自动填充：
public partial class ProductRepository
{
    private readonly IAuditProvider? _auditProvider;

    public async Task InsertAsync(Product entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;
        if (_auditProvider != null)
        {
            entity.CreatedBy = _auditProvider.GetCurrentUser();
            entity.UpdatedBy = _auditProvider.GetCurrentUser();
        }
        // ... INSERT SQL
    }
}
```

**实现要点**
- 源生成器检测 `[AuditFields]` 特性
- 在Insert/Update前自动设置时间戳和用户
- 如果实体有对应字段，自动填充；否则跳过
- 支持注入 `IAuditProvider` 获取当前用户

##### 特性3: 乐观锁 `[ConcurrencyCheck]`

**定义**
```csharp
namespace Sqlx.Annotations;

/// <summary>
/// Marks property for optimistic concurrency control
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ConcurrencyCheckAttribute : Attribute
{
}

/// <summary>
/// Thrown when concurrency conflict detected
/// </summary>
public class DbConcurrencyException : Exception
{
    public object? EntityId { get; set; }
    public object? ExpectedVersion { get; set; }
    public DbConcurrencyException(string message) : base(message) { }
}
```

**使用示例**
```csharp
public class Order
{
    public long Id { get; set; }
    public decimal Amount { get; set; }

    [ConcurrencyCheck]  // 显式标记并发检查字段
    public int Version { get; set; }  // 版本号
    // 或者
    [ConcurrencyCheck]
    public byte[] RowVersion { get; set; }  // SQL Server的rowversion
}

// 使用：
var order = await repo.GetByIdAsync(1);
order.Amount = 100;
await repo.UpdateAsync(order);
// 实际生成SQL：
// UPDATE orders SET amount = @amount, version = @version + 1
// WHERE id = @id AND version = @version
// 如果version不匹配（其他用户已修改），返回0行，抛出DbConcurrencyException
```

**实现要点**
- 源生成器检测 `[ConcurrencyCheck]` 特性
- Update的WHERE子句自动包含version字段
- SET子句自动递增version（或生成新GUID）
- 如果受影响行数为0，抛出 `DbConcurrencyException`
- 支持int/long版本号和byte[]行版本

---

### 阶段2: 高级Expression支持（中优先级）

#### 2.1 编译时Expression分析和优化 ⭐⭐⭐

**目标**: 在编译时分析Expression，生成最优SQL

**实现方案**

##### 功能1: 常量折叠
```csharp
// 用户代码
int threshold = 10;
var results = await repo.GetWhereAsync(t => t.Priority >= threshold + 5);

// 源生成器编译时分析：
// threshold + 5 = 15 (常量表达式)
// 生成SQL: WHERE priority >= 15 (而不是参数化)
```

##### 功能2: 表达式预编译
```csharp
// 源生成器检测重复的Expression模式
// 自动生成静态缓存的SQL片段
private static readonly string _sql_HighPriority = "priority >= 3 AND is_completed = 0";

// 避免每次调用都解析Expression
```

##### 功能3: 不支持的Expression编译时警告
```csharp
// Roslyn分析器检测不支持的操作
await repo.GetWhereAsync(t => t.ToString() == "test");
// 编译时警告：SQLX006: ToString() is not supported in Expression, use string properties instead
```

**实现要点**
- Roslyn Source Generator分析Expression树
- 常量折叠和表达式简化
- 生成静态SQL缓存
- 新增分析器规则检测不支持的操作

---

#### 2.2 集合支持增强 ⭐⭐⭐⭐⭐

**目标**: 全面增强集合类型的支持，消除手动构造集合参数

**实现方案**

##### 功能1: IN查询的集合参数支持
```csharp
// 现状：需要手动构造JSON
var idsJson = $"[{string.Join(",", ids)}]";
[SqlTemplate("UPDATE {{table}} SET status = @status WHERE id IN (SELECT value FROM json_each(@idsJson))")]
Task<int> UpdateStatusAsync(string idsJson, string status);

// 改进1：直接接受集合参数
[SqlTemplate("UPDATE {{table}} SET status = @status WHERE id IN {{values @ids}}")]
Task<int> UpdateStatusAsync(IEnumerable<long> ids, string status);
// 源生成器识别{{values @paramName}}占位符
// 自动生成：WHERE id IN (@id_0, @id_1, @id_2, ...)

// 改进2：Expression中支持Contains
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where}}")]
Task<List<Todo>> GetWhereAsync(Expression<Func<Todo, bool>> where);

// 使用：
var ids = new[] { 1L, 2L, 3L };
var results = await repo.GetWhereAsync(t => ids.Contains(t.Id));
// 自动生成：WHERE id IN (@id_0, @id_1, @id_2)

// 支持的集合操作：
await repo.GetWhereAsync(t => ids.Contains(t.Id));  // IN
await repo.GetWhereAsync(t => !ids.Contains(t.Id)); // NOT IN
```

##### 功能2: 批量操作接受IEnumerable
```csharp
// 现状：只支持List<T>
Task<int> BatchInsertAsync(List<TEntity> entities);

// 改进：支持所有IEnumerable<T>
Task<int> BatchInsertAsync(IEnumerable<TEntity> entities);
Task<int> BatchUpdateAsync(IEnumerable<TEntity> entities);
Task<int> BatchDeleteAsync(IEnumerable<TKey> ids);

// 支持LINQ延迟执行
await repo.BatchInsertAsync(
    users.Where(u => u.IsActive).Select(u => new User { Name = u.Name }));
```

##### 功能3: 批量操作优化（参数数量限制）
```csharp
// 问题：数据库有参数数量限制（如SQL Server 2100个参数）
var largeList = Enumerable.Range(1, 5000).ToList();
await repo.BatchInsertAsync(largeList);  // 可能失败！

// 改进：自动分批处理
[SqlTemplate("INSERT INTO {{table}} ({{columns}}) VALUES {{batch_values}}")]
[BatchOperation(MaxBatchSize = 500)]  // 新参数：最大批次大小
Task<int> BatchInsertAsync(IEnumerable<TEntity> entities);

// 源生成器自动生成分批逻辑
public async Task<int> BatchInsertAsync(IEnumerable<Todo> entities)
{
    var totalAffected = 0;
    foreach (var batch in entities.Chunk(500))  // 自动分批
    {
        // ... 执行INSERT
        totalAffected += affected;
    }
    return totalAffected;
}
```

##### 功能4: Collection Expression支持（C# 12+）
```csharp
// C# 12 Collection Expression
await repo.UpdateStatusAsync([1, 2, 3], "completed");
await repo.BatchInsertAsync([
    new Todo { Title = "Task 1" },
    new Todo { Title = "Task 2" }
]);

// 支持spread operator
var existingIds = new[] { 1L, 2L };
await repo.GetWhereAsync(t => [..existingIds, 3L, 4L].Contains(t.Id));
```

**实现要点**
- 源生成器识别 `IEnumerable<T>` 参数类型
- `{{values @paramName}}` 占位符自动展开为参数列表
- Expression中的 `Contains()` 自动转换为 IN 查询
- 自动分批处理大集合（避免参数数量限制）
- 支持C# 12 Collection Expression语法

---

#### 2.3 动态列和动态值支持 ⭐⭐⭐

**目标**: 支持动态SELECT列和动态返回类型

**实现方案**

##### 功能1: Expression选择列
```csharp
// 现在：只能返回完整实体
Task<List<Todo>> GetAllAsync();

// 改进：Expression选择列
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where}}")]
Task<List<TResult>> SelectAsync<TResult>(
    Expression<Func<Todo, TResult>> selector,
    Expression<Func<Todo, bool>>? where = null);

// 使用：
var results = await repo.SelectAsync(
    selector: t => new { t.Id, t.Title, t.Priority },
    where: t => t.IsCompleted == false);
// 生成SQL: SELECT id, title, priority FROM todos WHERE is_completed = 0
```

##### 功能2: 正则筛选列（保留现有功能）
```csharp
// 现有功能保持，继续支持
[SqlTemplate("SELECT {{columns --regex ^(Id|Title|Priority)$}} FROM {{table}}")]
Task<List<Dictionary<string, object>>> GetProjectedAsync();
```

**实现要点**
- 源生成器分析 `Expression<Func<T, TResult>>` 中的属性访问
- 只生成选中列的SELECT语句
- 支持匿名类型和DTO
- 保持现有--regex功能向后兼容

---

### 阶段3: 批量操作和关联查询（低优先级）

#### 3.1 批量操作优化 ⭐⭐

**目标**: 简化批量操作的参数

**实现方案**

```csharp
// 现状：需要手动构造JSON
var idsJson = $"[{string.Join(",", ids)}]";
await repo.UpdatePriorityAsync(idsJson, priority);

// 改进：直接接受集合
[SqlTemplate("UPDATE {{table}} SET priority = @priority WHERE id IN {{values @ids}}")]
Task<int> BatchUpdatePriorityAsync(IEnumerable<long> ids, int priority);
// 源生成器识别{{values @paramName}}占位符
// 自动生成：WHERE id IN (@id_0, @id_1, @id_2, ...)

// 或者使用Expression
[SqlTemplate("UPDATE {{table}} SET priority = @priority WHERE {{where}}")]
Task<int> UpdatePriorityWhereAsync(Expression<Func<Todo, bool>> where, int priority);
```

---

## 🚀 实施优先级

### 第1周：核心改进（立即实施）

| 功能 | 优先级 | 预计时间 | 业务价值 | 技术难度 |
|------|--------|---------|---------|---------|
| Insert返回ID | ⭐⭐⭐⭐⭐ | 3天 | 极高 | 中 |
| Expression参数支持 | ⭐⭐⭐⭐⭐ | 2天 | 极高 | 低（已有ExpressionToSql） |
| 集合参数IN查询 | ⭐⭐⭐⭐⭐ | 1天 | 极高 | 低 |
| 软删除 [SoftDelete] | ⭐⭐⭐⭐ | 2天 | 高 | 低 |

**第1周目标：解决90%的SQL手写场景和集合操作痛点**

### 第2周：业务模式和集合增强

| 功能 | 优先级 | 预计时间 | 业务价值 | 技术难度 |
|------|--------|---------|---------|---------|
| 审计字段 [AuditFields] | ⭐⭐⭐⭐ | 2天 | 高 | 低 |
| 乐观锁 [ConcurrencyCheck] | ⭐⭐⭐ | 2天 | 中 | 中 |
| 批量操作IEnumerable支持 | ⭐⭐⭐⭐ | 1天 | 高 | 低 |
| UpdateWhereAsync(Expression) | ⭐⭐⭐ | 2天 | 中 | 中 |

### 第3-4周：高级功能

| 功能 | 优先级 | 预计时间 | 业务价值 | 技术难度 |
|------|--------|---------|---------|---------|
| 编译时Expression分析 | ⭐⭐⭐ | 3天 | 中 | 高 |
| Expression选择列 | ⭐⭐⭐ | 2天 | 中 | 中 |
| 批量操作自动分批 | ⭐⭐⭐ | 2天 | 中 | 低 |
| Collection Expression支持 | ⭐⭐ | 1天 | 低 | 低 |

---

## 📋 完整功能列表

### ✅ 已实现
- ICrudRepository 8个标准方法
- ExpressionToSql WHERE支持
- 多数据库方言
- SQL模板占位符40+
- 编译时源生成

### 🎯 计划实现（按阶段）

**阶段1（第1周）- 核心CRUD和集合基础**
- [ ] `[ReturnInsertedId]` - Insert返回ID
- [ ] `[ReturnInsertedEntity]` - Insert返回完整实体
- [ ] `Expression<Func<T, bool>>` 参数支持
- [ ] `GetWhereAsync(Expression)` 方法
- [ ] `DeleteWhereAsync(Expression)` 方法
- [ ] `{{values @paramName}}` 占位符 - 集合参数IN查询
- [ ] Expression中 `Contains()` 支持 - 自动转IN查询
- [ ] `[SoftDelete]` - 软删除特性
- [ ] `[IgnoreSoftDelete]` - 忽略软删除过滤

**阶段2（第2周）- 业务模式和集合增强**
- [ ] `[AuditFields]` - 审计字段自动填充
- [ ] `IAuditProvider` 接口 - 获取当前用户
- [ ] `[ConcurrencyCheck]` - 乐观锁特性
- [ ] `DbConcurrencyException` 异常 - 并发冲突
- [ ] 批量操作接受 `IEnumerable<T>` 而非 `List<T>`
- [ ] `UpdateWhereAsync(Expression, Expression)` - SET支持Expression

**阶段3（第3-4周）- 高级优化**
- [ ] 编译时Expression常量折叠
- [ ] Expression分析器警告 - 不支持的操作
- [ ] `SelectAsync<TResult>(Expression<Func<T, TResult>>)` - 选择列
- [ ] `[BatchOperation(MaxBatchSize)]` - 自动分批
- [ ] Collection Expression支持（C# 12+）

---

## 🎯 成功指标

**当前状态**
- ✅ 标准CRUD：0% SQL知识
- ❌ 自定义查询：80% SQL知识
- ❌ 批量操作：60% SQL知识
- ❌ Insert+GetId：100% SQL知识

**阶段1完成（第1周）**
- ✅ 标准CRUD：0% SQL知识
- ✅ 自定义查询：20% SQL知识（Expression代替）
- ✅ 批量操作：30% SQL知识
- ✅ Insert+GetId：0% SQL知识

**最终目标（4周后）**
- ✅ 标准CRUD：0% SQL知识
- ✅ 自定义查询：5% SQL知识
- ✅ 批量操作：10% SQL知识
- ✅ 审计/并发：0% SQL知识

---

## 💡 示例：改进前 vs 改进后

### 场景1：Insert并获取ID

**改进前** ❌
```csharp
var todo = new Todo { Title = "Learn Sqlx" };
await repo.InsertAsync(todo);  // 返回rows affected

// 需要手写SQL获取ID
using var cmd = conn.CreateCommand();
cmd.CommandText = "SELECT last_insert_rowid()";
var newId = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
```

**改进后** ✅
```csharp
var todo = new Todo { Title = "Learn Sqlx" };
var newId = await repo.InsertAndGetIdAsync(todo);  // 直接返回ID

// 或者
var created = await repo.InsertAndReturnAsync(todo);  // 返回完整实体（含ID）
```

---

### 场景2：条件查询

**改进前** ❌
```csharp
// 需要写SQL模板
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_completed = @isCompleted AND priority >= @minPriority ORDER BY priority DESC")]
Task<List<Todo>> GetHighPriorityAsync(bool isCompleted, int minPriority);
```

**改进后** ✅
```csharp
// 使用C#表达式
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE {{where}} ORDER BY priority DESC")]
Task<List<Todo>> GetWhereAsync(Expression<Func<Todo, bool>> where);

// 调用
var results = await repo.GetWhereAsync(
    t => t.IsCompleted == false && t.Priority >= 3);
```

---

### 场景3：软删除

**改进前** ❌
```csharp
// 需要手动实现软删除逻辑
[SqlTemplate("UPDATE {{table}} SET is_deleted = 1 WHERE id = @id")]
Task<int> SoftDeleteAsync(long id);

[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_deleted = 0")]
Task<List<User>> GetActiveAsync();
```

**改进后** ✅
```csharp
[SoftDelete]  // 一个特性搞定
public class User
{
    public long Id { get; set; }
    public string Name { get; set; }
    public bool IsDeleted { get; set; }
}

// 所有CRUD方法自动支持软删除
await repo.DeleteAsync(id);  // 自动变为UPDATE is_deleted = 1
await repo.GetAllAsync();    // 自动过滤is_deleted = 0
```

---

### 场景4：审计字段

**改进前** ❌
```csharp
var product = new Product
{
    Name = "iPhone",
    CreatedAt = DateTime.UtcNow,  // 手动设置
    UpdatedAt = DateTime.UtcNow,  // 手动设置
    CreatedBy = GetCurrentUser(), // 手动设置
};
await repo.InsertAsync(product);
```

**改进后** ✅
```csharp
[AuditFields]  // 自动填充
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }  // 自动填充
    public DateTime UpdatedAt { get; set; }  // 自动填充
    public string CreatedBy { get; set; }    // 自动填充
}

var product = new Product { Name = "iPhone" };
await repo.InsertAsync(product);  // 时间戳和用户自动填充
```

---

### 场景5：集合操作

**改进前** ❌
```csharp
// 痛点1：IN查询需要手动构造JSON
var idsJson = $"[{string.Join(",", ids)}]";
[SqlTemplate("UPDATE todos SET status = @status WHERE id IN (SELECT value FROM json_each(@idsJson))")]
Task<int> UpdateStatusAsync(string idsJson, string status);

await repo.UpdateStatusAsync(idsJson, "completed");

// 痛点2：Expression不支持Contains
// 无法写：await repo.GetWhereAsync(t => ids.Contains(t.Id));

// 痛点3：批量插入只支持List
var entities = GetEntitiesFromSomewhere();  // returns IEnumerable<T>
await repo.BatchInsertAsync(entities.ToList());  // 需要ToList()

// 痛点4：大批量操作可能失败
var largeList = Enumerable.Range(1, 5000).ToList();
await repo.BatchInsertAsync(largeList);  // SQL Server限制2100个参数！
```

**改进后** ✅
```csharp
// ✅ 1. IN查询直接用集合
[SqlTemplate("UPDATE {{table}} SET status = @status WHERE id IN {{values @ids}}")]
Task<int> UpdateStatusAsync(IEnumerable<long> ids, string status);

await repo.UpdateStatusAsync(ids, "completed");  // 直接传集合

// ✅ 2. Expression支持Contains
var results = await repo.GetWhereAsync(t => ids.Contains(t.Id));
// 自动生成：WHERE id IN (@id_0, @id_1, @id_2)

// ✅ 3. 批量操作接受IEnumerable
await repo.BatchInsertAsync(entities);  // 无需ToList()

// ✅ 4. 大批量自动分批
[BatchOperation(MaxBatchSize = 500)]
Task<int> BatchInsertAsync(IEnumerable<Todo> entities);
// 自动分批，避免参数限制
```

---

## 🔑 关键设计决策

### 1. 为什么必须用特性标记？
- ✅ **明确意图** - 用户清楚知道启用了什么功能
- ✅ **避免意外** - 不会因为命名巧合触发意外行为
- ✅ **可维护性** - 容易搜索和理解代码
- ✅ **向后兼容** - 不影响现有代码

### 2. 为什么不做查询构建器（Fluent API）？
- ❌ **运行时开销** - 需要Expression树构建和解析
- ❌ **复杂性** - 实现成本高，维护困难
- ❌ **与Sqlx理念不符** - 编译时优于运行时
- ✅ **Expression参数更简单** - 直接用C#表达式，编译时生成

### 3. 为什么不做CQRS/事件溯源？
- ❌ **超出范围** - 这是架构模式，不是ORM功能
- ❌ **增加复杂度** - 对大多数用户不适用
- ✅ **聚焦CRUD** - 专注做好数据访问

### 4. Expression vs SQL模板如何选择？
```csharp
// 简单条件：用Expression
Task<List<T>> GetWhereAsync(Expression<Func<T, bool>> where);

// 复杂SQL：仍用模板（保留灵活性）
[SqlTemplate(@"
    SELECT t.*, COUNT(c.id) as comment_count
    FROM {{table}} t
    LEFT JOIN comments c ON t.id = c.todo_id
    GROUP BY t.id
    HAVING COUNT(c.id) > @minComments
")]
Task<List<TodoWithCommentCount>> GetWithManyCommentsAsync(int minComments);
```

---

## 📝 下一步行动

### 推荐：立即实施阶段1（1周见效）

**Day 1-3: Insert返回ID**
1. 设计 `[ReturnInsertedId]` 和 `[ReturnInsertedEntity]` 特性
2. 实现多数据库RETURNING语法
3. 源生成器实现
4. 单元测试（5种数据库）

**Day 4-5: Expression参数支持**
1. 扩展占位符解析，识别Expression参数
2. 实现 `GetWhereAsync(Expression)` 生成逻辑
3. 添加 `DeleteWhereAsync(Expression)`
4. 单元测试

**Day 6: 集合参数IN查询**
1. 实现 `{{values @paramName}}` 占位符
2. Expression中 `Contains()` 方法转IN查询
3. 自动参数展开（@id_0, @id_1, ...）
4. 单元测试

**Day 7-8: 软删除**
1. 实现 `[SoftDelete]` 特性
2. 修改CRUD生成逻辑（DELETE→UPDATE, 查询自动过滤）
3. 实现 `[IgnoreSoftDelete]` 方法特性
4. 单元测试和文档

---

**要开始实施吗？**
- **A. 立即开始阶段1** （Insert返回ID + Expression参数 + 集合支持 + 软删除，8天完成）
- **B. 先实施前3项** （Insert返回ID + Expression参数 + 集合支持，6天快速见效）
- **C. 我想调整优先级或补充需求**

---

**总结**:
- ✅ 去除了查询构建器（Fluent API）
- ✅ 增强Expression参数支持（WHERE条件、Contains等）
- ✅ 全面增强集合支持（IN查询、JSON列、批量操作）
- ✅ 模仿EF Core特性（软删除、审计、并发）
- ✅ 强制用户写特性（明确意图）
- ✅ 聚焦CRUD，不做CQRS等架构模式
- ✅ 保持编译时生成、零运行时开销

**集合支持亮点**:
- `{{values @ids}}` - 集合参数自动展开为IN查询
- `ids.Contains(t.Id)` - Expression中自动转换为IN
- `IEnumerable<T>` - 批量操作不再强制List
- `[BatchOperation(MaxBatchSize)]` - 自动分批处理，避免参数数量限制
- Collection Expression - 支持C# 12最新语法

