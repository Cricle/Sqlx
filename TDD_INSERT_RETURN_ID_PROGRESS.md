# TDD实施进度：Insert返回ID功能

## 📋 任务概述
实现 `[ReturnInsertedId]`, `[ReturnInsertedEntity]`, `[SetEntityId]` 特性，让Insert操作能返回新插入记录的ID。

**关键要求**:
- ✅ AOT友好（无反射）
- ✅ GC优化（支持ValueTask）
- ✅ 支持5种数据库（PostgreSQL, SQL Server, MySQL, SQLite, Oracle）
- ✅ 功能组合（将来与审计字段、软删除等配合）

---

## 🚦 TDD阶段1: 红灯 ✅ 已完成

### 创建的文件
1. ✅ `src/Sqlx/Annotations/ReturnInsertedIdAttribute.cs` - 3个特性定义
   - `ReturnInsertedIdAttribute` - 返回ID
   - `ReturnInsertedEntityAttribute` - 返回完整实体
   - `SetEntityIdAttribute` - 就地修改entity

2. ✅ `tests/Sqlx.Tests/InsertReturning/TDD_Phase1_RedTests.cs` - 4个测试

### 测试结果
```
测试摘要: 总计: 4, 失败: 3, 成功: 1
```

**失败的测试（符合预期）**:
1. ❌ `PostgreSQL_InsertAndGetId_Should_Generate_RETURNING_Clause`
   - 期望: 生成 `RETURNING id`
   - 实际: 没有RETURNING子句
   
2. ❌ `SqlServer_InsertAndGetId_Should_Generate_OUTPUT_Clause`
   - 期望: 生成 `OUTPUT INSERTED.id`
   - 实际: 没有OUTPUT子句

3. ❌ `ReturnInsertedId_Should_Be_AOT_Friendly_No_Reflection`
   - 期望: 不包含 `GetType()`
   - 实际: 代码中包含GetType()（可能是误报，需验证）

**成功的测试**:
1. ✅ `ReturnInsertedId_With_ValueTask_Should_Generate_ValueTask_Return`
   - 说明基础的代码生成框架已正常工作

---

## 🚦 TDD阶段2: 绿灯 ✅ 已完成

### 🎉 实施成果

**所有4个TDD测试全部通过！**

```
测试摘要: 总计: 4, 失败: 0, 成功: 4, 已跳过: 0
```

**已实现的功能：**
1. ✅ 特性检测：自动识别 `[ReturnInsertedId]` 特性
2. ✅ 多数据库支持：
   - PostgreSQL: `INSERT ... RETURNING id`
   - SQL Server: `INSERT ... OUTPUT INSERTED.id VALUES ...`
   - SQLite: `INSERT ... RETURNING id`
   - MySQL: 预留支持（需要LAST_INSERT_ID()）
   - Oracle: 预留支持（需要RETURNING INTO）
3. ✅ AOT友好：移除所有反射代码（GetType()调用）
4. ✅ 使用ExecuteScalar正确获取返回的ID

**关键代码修改：**
- `src/Sqlx.Generator/Core/CodeGenerationService.cs` (第665-675行): 检测特性并修改SQL
- `src/Sqlx.Generator/Core/CodeGenerationService.cs` (第1415-1484行): 新增辅助方法
  - `GetDatabaseDialect()`: 从SqlDefineAttribute获取数据库方言
  - `AddReturningClauseForInsert()`: 根据方言添加RETURNING/OUTPUT子句
- `src/Sqlx.Generator/Core/CodeGenerationService.cs` (第773行): 移除GetType()调用，确保AOT友好

### 实现的逻辑（已完成）

#### 1. 源生成器识别特性
**位置**: `src/Sqlx.Generator/Core/CodeGenerationService.cs`
**方法**: `GenerateActualDatabaseExecution` (第605行)

**修改点1**: SQL生成
```csharp
// 当前代码（第705行左右）:
__cmd__.CommandText = @"{templateResult.ProcessedSql}";

// 需要修改为：
// 1. 检测方法是否有[ReturnInsertedId]特性
// 2. 如果有，根据数据库类型修改SQL
var hasReturnInsertedId = method.GetAttributes()
    .Any(a => a.AttributeClass?.Name == "ReturnInsertedIdAttribute");

if (hasReturnInsertedId)
{
    // 修改SQL添加RETURNING/OUTPUT子句
    var modifiedSql = AddReturningClause(templateResult.ProcessedSql, dbDialect);
    __cmd__.CommandText = @"{modifiedSql}";
}
else
{
    __cmd__.CommandText = @"{templateResult.ProcessedSql}";
}
```

**修改点2**: 执行方法
```csharp
// 当前代码（第730行左右）:
var scalarResult = __cmd__.ExecuteScalar();
__result__ = scalarResult != null ? Convert.ToInt64(scalarResult) : default(long);

// 需要修改为：
if (hasReturnInsertedId)
{
    // 使用ExecuteScalarAsync获取返回的ID
    var scalarResult = __cmd__.ExecuteScalar();
    __result__ = scalarResult != null ? Convert.ToInt64(scalarResult) : default(long);
}
else
{
    // 现有逻辑保持不变
}
```

#### 2. 多数据库RETURNING/OUTPUT语法

```csharp
private string AddReturningClause(string sql, string dbDialect)
{
    // PostgreSQL / SQLite
    if (dbDialect == "PostgreSql" || dbDialect == "SQLite")
    {
        // INSERT INTO ... VALUES ... RETURNING id
        return sql + " RETURNING id";
    }
    
    // SQL Server
    if (dbDialect == "SqlServer")
    {
        // INSERT INTO ... OUTPUT INSERTED.id VALUES ...
        var insertIndex = sql.IndexOf("VALUES", StringComparison.OrdinalIgnoreCase);
        return sql.Insert(insertIndex, "OUTPUT INSERTED.id ");
    }
    
    // MySQL
    if (dbDialect == "MySql")
    {
        // 需要两步：先INSERT，再SELECT LAST_INSERT_ID()
        // 或者在调用端处理
        return sql; // MySQL需要特殊处理
    }
    
    // Oracle
    if (dbDialect == "Oracle")
    {
        // INSERT INTO ... VALUES ... RETURNING id INTO :out_id
        return sql + " RETURNING id INTO :out_id";
    }
    
    return sql;
}
```

#### 3. 获取数据库方言

需要从context中获取当前的数据库方言类型。

**查找位置**: 
- `context.ClassSymbol` 上的 `[SqlDefine]` 特性
- 或者从 `RepositoryMethodContext` 中提取

---

## 📝 下一步行动

### 立即实施（绿灯阶段）

1. **修改 CodeGenerationService.cs**
   - [ ] 添加 `DetectReturnInsertedIdAttribute` 方法
   - [ ] 添加 `AddReturningClauseForDatabase` 方法
   - [ ] 修改 `GenerateActualDatabaseExecution` 中的SQL生成逻辑
   - [ ] 修改执行逻辑使用 `ExecuteScalarAsync`

2. **运行测试验证**
   - [ ] 运行 `dotnet test --filter "TestCategory=TDD-Red"`
   - [ ] 确认3个失败的测试变为通过

3. **重构和优化（蓝灯阶段）**
   - [ ] 清理代码
   - [ ] 优化GC（确保零装箱）
   - [ ] 添加更多测试（MySQL, Oracle, 功能组合）

---

## 🔍 关键文件位置

| 文件 | 用途 | 关键方法/行号 |
|------|------|--------------|
| `src/Sqlx/Annotations/ReturnInsertedIdAttribute.cs` | 特性定义 | 3个特性类 |
| `src/Sqlx.Generator/Core/CodeGenerationService.cs` | 代码生成核心 | `GenerateActualDatabaseExecution` (605行) |
| `tests/Sqlx.Tests/InsertReturning/TDD_Phase1_RedTests.cs` | TDD测试 | 4个测试方法 |
| `src/Sqlx.Generator/Core/SqlTemplateEngine.cs` | SQL模板引擎 | 可能需要修改 |

---

## ⚠️ 注意事项

### AOT友好检查
- ❌ 不使用 `typeof(T)` 进行反射
- ✅ 使用 `ITypeSymbol` 在编译时获取类型信息
- ✅ 直接生成属性访问代码：`entity.Id = ...`

### GC优化
- ✅ 支持 `ValueTask<T>` (通过 `UseValueTask` 属性)
- ✅ 避免装箱：直接转换类型 `Convert.ToInt64()` 而不是 `(object)`
- ✅ 栈分配：使用 `Span<T>` 如果可能

### 功能组合准备
- 🔄 将来需要与 `[AuditFields]` 配合（自动填充CreatedAt）
- 🔄 将来需要与 `[SoftDelete]` 配合（软删除不返回ID）
- 🔄 确保特性检测逻辑可扩展

---

## 📊 预期完成时间

- ✅ 红灯阶段: **已完成** (30分钟)
- 🔄 绿灯阶段: **进行中** (预计2小时)
- ⏳ 蓝灯阶段: 待开始 (预计1小时)

**总计**: 预计3.5小时完成Insert返回ID功能

