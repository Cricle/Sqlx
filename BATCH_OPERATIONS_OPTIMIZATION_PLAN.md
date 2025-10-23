# 批量操作性能优化方案

**日期**: 2024-10-23
**目标**: 优化批量插入、更新、删除性能，支持 ExpressionToSqlBase 参数

---

## 📊 当前实现分析

###问题：
1. ⚠️ **批量插入**：固定批次大小，不灵活
2. ⚠️ **批量更新**：缺少高效实现
3. ⚠️ **批量删除**：缺少条件表达式支持
4. ⚠️ **参数绑定**：不支持 `ExpressionToSqlBase`

### 当前实现：
```csharp
// BaseDialectProvider.cs
public virtual string GenerateBatchInsert(string tableName, string[] columns, int batchSize)
{
    // 生成固定批次的 INSERT
    var valuesClauses = new string[batchSize];
    for (int i = 0; i < batchSize; i++)
    {
        var parameters = columns.Select(c => $"@{c}I{i}").ToArray();
        valuesClauses[i] = $"({string.Join(", ", parameters)})";
    }
    return $"INSERT INTO {tableName} ({columns}) VALUES {string.Join(", ", valuesClauses)}";
}
```

---

## 🎯 优化方案

### 方案 1: 扩展 ExpressionToSqlBase

在 `ExpressionToSqlBase` 中添加批量操作支持：

```csharp
// 新增字段
internal List<Dictionary<string, object?>>? _batchParameters;
internal ExpressionToSqlBase? _whereExpression;

// 批量插入
public ExpressionToSqlBase<T> AddBatch(T entity) { }
public ExpressionToSqlBase<T> AddBatchRange(IEnumerable<T> entities) { }

// 批量更新
public ExpressionToSqlBase<T> SetBatch(Expression<Func<T, object>> selector, object value) { }

// 批量删除（使用另一个 ExpressionToSqlBase 的 WHERE）
public ExpressionToSqlBase<T> WhereFrom(ExpressionToSqlBase<T> expression) { }
```

### 方案 2: 生成优化的批量代码

```csharp
// 生成的代码示例
public async Task<int> BatchInsertAsync(IEnumerable<User> users)
{
    // 使用 StringBuilder 构建 SQL
    var sb = new StringBuilder("INSERT INTO users (name, email) VALUES ");
    var parameters = new List<DbParameter>();

    int index = 0;
    foreach (var user in users)
    {
        if (index > 0) sb.Append(", ");
        sb.Append($"(@name{index}, @email{index})");
        parameters.Add(new SqlParameter($"@name{index}", user.Name));
        parameters.Add(new SqlParameter($"@email{index}", user.Email));
        index++;

        // 每 1000 条执行一次
        if (index % 1000 == 0)
        {
            await ExecuteBatchAsync(sb.ToString(), parameters);
            sb.Clear();
            sb.Append("INSERT INTO users (name, email) VALUES ");
            parameters.Clear();
            index = 0;
        }
    }

    if (parameters.Count > 0)
    {
        await ExecuteBatchAsync(sb.ToString(), parameters);
    }
}
```

### 方案 3: 支持 ExpressionToSqlBase 作为参数

```csharp
// 用户代码
var whereExpr = new ExpressionToSql<User>(SqlDefineTypes.SqlServer)
    .Where(u => u.Age > 18 && u.Status == "Active");

// 批量删除
await repo.BatchDeleteAsync(whereExpr);
// 生成: DELETE FROM users WHERE Age > 18 AND Status = 'Active'

// 批量更新
await repo.BatchUpdateAsync(
    u => new { IsVerified = true },
    whereExpr);
// 生成: UPDATE users SET IsVerified = 1 WHERE Age > 18 AND Status = 'Active'
```

---

## 🚀 实施步骤

### Step 1: 扩展 ExpressionToSqlBase (高优先级)

**新增方法**:
```csharp
// src/Sqlx/ExpressionToSqlBase.cs

/// <summary>批量参数存储</summary>
internal List<Dictionary<string, object?>>? _batchParameters;

/// <summary>外部 WHERE 表达式</summary>
internal ExpressionToSqlBase? _whereExpression;

/// <summary>从另一个表达式复制 WHERE 条件</summary>
public ExpressionToSqlBase WhereFrom(ExpressionToSqlBase expression)
{
    _whereExpression = expression;
    return this;
}

/// <summary>获取合并后的 WHERE 条件</summary>
internal string GetMergedWhereConditions()
{
    var conditions = new List<string>(_whereConditions);
    if (_whereExpression != null)
    {
        conditions.AddRange(_whereExpression._whereConditions);
    }
    return conditions.Count > 0
        ? $"WHERE {string.Join(" AND ", conditions)}"
        : "";
}

/// <summary>获取合并后的参数</summary>
internal Dictionary<string, object?> GetMergedParameters()
{
    var merged = new Dictionary<string, object?>(_parameters);
    if (_whereExpression != null)
    {
        foreach (var kvp in _whereExpression._parameters)
        {
            merged[kvp.Key] = kvp.Value;
        }
    }
    return merged;
}
```

### Step 2: 优化批量插入 (高优先级)

**代码生成优化**:
```csharp
// src/Sqlx.Generator/Core/CodeGenerationService.cs

private void GenerateBatchInsertMethod(IndentedStringBuilder sb, IMethodSymbol method)
{
    var entityType = GetEntityType(method);
    var columns = GetColumns(entityType);

    sb.AppendLine($"public async Task<int> {method.Name}(IEnumerable<{entityType}> entities)");
    sb.AppendLine("{");
    sb.PushIndent();

    // 常量
    sb.AppendLine("const int BatchSize = 1000;");
    sb.AppendLine();

    // StringBuilder 复用
    sb.AppendLine("var sb = global::System.Text.StringBuilder.Pool.Get();");
    sb.AppendLine("var parameters = new global::System.Collections.Generic.List<global::System.Data.Common.DbParameter>();");
    sb.AppendLine("int totalAffected = 0;");
    sb.AppendLine("int index = 0;");
    sb.AppendLine();

    // 初始化 SQL
    sb.AppendLine($"sb.Append(\"INSERT INTO {tableName} ({string.Join(", ", columns)}) VALUES \");");
    sb.AppendLine();

    // 遍历实体
    sb.AppendLine("foreach (var entity in entities)");
    sb.AppendLine("{");
    sb.PushIndent();

    sb.AppendLine("if (index > 0) sb.Append(\", \");");
    sb.AppendLine($"sb.Append(\"({string.Join(", ", columns.Select((c, i) => $"@{c}{{index}}"))})\");");
    sb.AppendLine();

    // 添加参数
    foreach (var column in columns)
    {
        sb.AppendLine($"parameters.Add(__CreateParameter($\"@{column}{{index}}\", entity.{column}));");
    }

    sb.AppendLine("index++;");
    sb.AppendLine();

    // 批次执行
    sb.AppendLine("if (index >= BatchSize)");
    sb.AppendLine("{");
    sb.PushIndent();
    sb.AppendLine("totalAffected += await __ExecuteBatchAsync(sb.ToString(), parameters);");
    sb.AppendLine("sb.Clear();");
    sb.AppendLine($"sb.Append(\"INSERT INTO {tableName} ({string.Join(", ", columns)}) VALUES \");");
    sb.AppendLine("parameters.Clear();");
    sb.AppendLine("index = 0;");
    sb.PopIndent();
    sb.AppendLine("}");

    sb.PopIndent();
    sb.AppendLine("}");
    sb.AppendLine();

    // 执行剩余
    sb.AppendLine("if (parameters.Count > 0)");
    sb.AppendLine("{");
    sb.PushIndent();
    sb.AppendLine("totalAffected += await __ExecuteBatchAsync(sb.ToString(), parameters);");
    sb.PopIndent();
    sb.AppendLine("}");
    sb.AppendLine();

    // 归还 StringBuilder
    sb.AppendLine("global::System.Text.StringBuilder.Pool.Return(sb);");
    sb.AppendLine("return totalAffected;");

    sb.PopIndent();
    sb.AppendLine("}");
}
```

### Step 3: 实现批量更新 (中优先级)

**生成代码**:
```csharp
// 用户接口
[Sqlx("UPDATE {{table}} SET {{set}} WHERE {{where}}")]
Task<int> BatchUpdateAsync(
    Expression<Func<User, object>> setExpression,
    [ExpressionToSql] ExpressionToSqlBase<User> whereExpression);

// 生成的代码
public async Task<int> BatchUpdateAsync(
    Expression<Func<User, object>> setExpression,
    ExpressionToSqlBase<User> whereExpression)
{
    // 解析 SET 子句
    var setClause = __ParseSetExpression(setExpression);

    // 获取 WHERE 子句
    var whereClause = whereExpression.GetMergedWhereConditions();
    var parameters = whereExpression.GetMergedParameters();

    var sql = $"UPDATE users SET {setClause} {whereClause}";

    using var cmd = __conn__.CreateCommand();
    cmd.CommandText = sql;

    foreach (var kvp in parameters)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = kvp.Key;
        param.Value = kvp.Value ?? DBNull.Value;
        cmd.Parameters.Add(param);
    }

    return await cmd.ExecuteNonQueryAsync();
}
```

### Step 4: 实现批量删除 (中优先级)

**生成代码**:
```csharp
// 用户接口
[Sqlx("DELETE FROM {{table}} WHERE {{where}}")]
Task<int> BatchDeleteAsync([ExpressionToSql] ExpressionToSqlBase<User> whereExpression);

// 生成的代码
public async Task<int> BatchDeleteAsync(ExpressionToSqlBase<User> whereExpression)
{
    var whereClause = whereExpression.GetMergedWhereConditions();
    var parameters = whereExpression.GetMergedParameters();

    if (string.IsNullOrEmpty(whereClause))
    {
        throw new InvalidOperationException("批量删除必须指定 WHERE 条件");
    }

    var sql = $"DELETE FROM users {whereClause}";

    using var cmd = __conn__.CreateCommand();
    cmd.CommandText = sql;

    foreach (var kvp in parameters)
    {
        var param = cmd.CreateParameter();
        param.ParameterName = kvp.Key;
        param.Value = kvp.Value ?? DBNull.Value;
        cmd.Parameters.Add(param);
    }

    return await cmd.ExecuteNonQueryAsync();
}
```

---

## 🔧 新增特性

### 1. `[ExpressionToSql]` 特性

```csharp
// src/Sqlx/Attributes/ExpressionToSqlAttribute.cs
namespace Sqlx;

/// <summary>
/// 标记参数为 ExpressionToSqlBase 类型，用于批量操作的 WHERE 条件
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class ExpressionToSqlAttribute : Attribute
{
}
```

### 2. `[BatchOperation]` 特性

```csharp
// src/Sqlx/Attributes/BatchOperationAttribute.cs
namespace Sqlx;

/// <summary>
/// 标记方法为批量操作，自动优化性能
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class BatchOperationAttribute : Attribute
{
    /// <summary>
    /// 批次大小（默认 1000）
    /// </summary>
    public int BatchSize { get; set; } = 1000;
}
```

---

## 📊 性能目标

| 操作 | 当前性能 | 目标性能 | 提升 |
|------|---------|---------|------|
| **批量插入 1000 条** | ~50ms | ~15ms | **3.3x** |
| **批量更新 1000 条** | N/A | ~20ms | **新功能** |
| **批量删除 1000 条** | N/A | ~10ms | **新功能** |
| **GC 分配** | 高 | 低 (复用 StringBuilder) | **50%↓** |

---

## 🧪 测试计划

### 功能测试
- ✅ 批量插入（空集合、1条、100条、1000条、10000条）
- ✅ 批量更新（使用 ExpressionToSqlBase）
- ✅ 批量删除（使用 ExpressionToSqlBase）
- ✅ 事务中的批量操作
- ✅ 异常情况（连接断开、参数错误）

### 性能测试
- ✅ 批量插入 vs 单条插入
- ✅ 批量更新 vs 单条更新
- ✅ 批量删除 vs 单条删除
- ✅ GC 压力测试
- ✅ 并发批量操作

---

## 📝 使用示例

### 示例 1: 批量插入

```csharp
public interface IUserRepository
{
    [Sqlx("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES {{batch_values}}")]
    [BatchOperation(BatchSize = 1000)]
    Task<int> BatchInsertAsync(IEnumerable<User> users);
}

// 使用
var users = GetUsers(); // 10000 users
var affected = await repo.BatchInsertAsync(users);
// 自动分批执行：10 批 x 1000 条
```

### 示例 2: 批量更新（使用表达式）

```csharp
public interface IUserRepository
{
    [Sqlx("UPDATE {{table}} SET {{set}} WHERE {{where}}")]
    Task<int> BatchUpdateAsync(
        Expression<Func<User, object>> setExpression,
        [ExpressionToSql] ExpressionToSqlBase<User> whereExpression);
}

// 使用
var whereExpr = new ExpressionToSql<User>(SqlDefineTypes.SqlServer)
    .Where(u => u.Age > 18 && u.IsActive);

var affected = await repo.BatchUpdateAsync(
    u => new { LastLoginAt = DateTime.Now, LoginCount = u.LoginCount + 1 },
    whereExpr);
// 生成: UPDATE users SET LastLoginAt = @p0, LoginCount = LoginCount + 1
//       WHERE Age > 18 AND IsActive = 1
```

### 示例 3: 批量删除（使用表达式）

```csharp
public interface IUserRepository
{
    [Sqlx("DELETE FROM {{table}} WHERE {{where}}")]
    Task<int> BatchDeleteAsync([ExpressionToSql] ExpressionToSqlBase<User> whereExpression);
}

// 使用
var whereExpr = new ExpressionToSql<User>(SqlDefineTypes.SqlServer)
    .Where(u => u.IsDeleted && u.DeletedAt < DateTime.Now.AddDays(-30));

var affected = await repo.BatchDeleteAsync(whereExpr);
// 生成: DELETE FROM users WHERE IsDeleted = 1 AND DeletedAt < @p0
```

---

## 🎯 实施优先级

### 高优先级 (本周)
1. ✅ 扩展 `ExpressionToSqlBase`（WhereFrom, GetMergedWhereConditions）
2. ✅ 优化批量插入代码生成
3. ✅ 添加 `[ExpressionToSql]` 特性

### 中优先级 (下周)
4. ✅ 实现批量更新
5. ✅ 实现批量删除
6. ✅ 添加性能测试

### 低优先级 (按需)
7. 添加 `[BatchOperation]` 特性
8. 支持自定义批次大小
9. 支持批量操作的进度回调

---

**下一步**: 开始实施 Step 1 - 扩展 ExpressionToSqlBase

