# 软删除特性 - 实施进度

**当前状态**: 🟡 部分完成（3/5测试通过 - 60%）  
**用时**: ~2小时  
**Token使用**: 107k / 1M (10.7%)

---

## ✅ 已完成

### 1. 特性类创建
- ✅ `SoftDeleteAttribute.cs` - 软删除配置特性
- ✅ `IncludeDeletedAttribute.cs` - 绕过软删除过滤特性

### 2. SELECT查询自动过滤 ✅
**功能**: 自动为SELECT查询添加`WHERE is_deleted = false`

**实现位置**:
- `CodeGenerationService.cs` 第684-717行
- 在SQL处理后检测SELECT并添加WHERE子句

**测试通过** (3/5):
1. ✅ `SoftDelete_SELECT_Without_WHERE_Should_Add_Filter`
2. ✅ `SoftDelete_SELECT_With_WHERE_Should_Add_AND_Filter`
3. ✅ `IncludeDeleted_Should_Not_Filter`

**示例生成**:
```sql
-- 原始模板
SELECT * FROM {{table}}

-- 生成的SQL
SELECT * FROM user WHERE is_deleted = false
```

---

## ❌ 待修复

### DELETE转换为UPDATE (2个测试失败)

**问题**: DELETE语句没有被转换为UPDATE

**测试失败**:
1. ❌ `SoftDelete_DELETE_Should_Convert_To_UPDATE`
2. ❌ `SoftDelete_DELETE_With_Timestamp_Should_Set_DeletedAt`

**期望行为**:
```sql
-- 原始模板
DELETE FROM {{table}} WHERE id = @id

-- 期望生成
UPDATE user SET is_deleted = true WHERE id = @id

-- 实际生成（错误）
DELETE FROM user WHERE id = @id  -- 未转换！
```

**已实现的逻辑**:
- `ConvertDeleteToSoftDelete` 方法 (CodeGenerationService.cs 第1555-1595行)
- 调用位置: 第692-696行

**问题分析**:
`processedSql`在第692行被检测为包含"DELETE"，应该调用`ConvertDeleteToSoftDelete`，但转换后的SQL没有被应用到最终的`CommandText`。

**可能原因**:
1. ✅ `processedSql` 变量被正确修改
2. ❓ 但修改后的SQL没有传递到`GenerateCommandSetup`
3. ❓ 或者`GenerateCommandSetup`覆盖了修改

**调试发现**:
```csharp
// 第717行之前
processedSql = ConvertDeleteToSoftDelete(...);  // 应该修改为UPDATE

// 第719行
SharedCodeGenerationUtilities.GenerateCommandSetup(sb, processedSql, method, connectionName);
// 但生成的CommandText仍然是DELETE
```

---

## 🔍 问题定位

### 方案1: 检查ConvertDeleteToSoftDelete返回值
`ConvertDeleteToSoftDelete`方法可能没有正确返回修改后的SQL。

**需要验证**:
- 方法内的SQL修改逻辑
- 表名参数是否正确
- 返回值是否正确

### 方案2: 检查GenerateCommandSetup
`GenerateCommandSetup`可能重新处理了SQL或使用了原始模板。

**需要验证**:
- `SharedCodeGenerationUtilities.GenerateCommandSetup`的实现
- 是否使用了`processedSql`还是`templateResult.ProcessedSql`

---

## 📝 ConvertDeleteToSoftDelete详细分析

```csharp
private static string ConvertDeleteToSoftDelete(string sql, SoftDeleteConfig config, string dialect, string tableName)
{
    // 1. 检查是否包含DELETE（已通过）
    if (sql.IndexOf("DELETE", StringComparison.OrdinalIgnoreCase) < 0)
    {
        return sql;
    }

    // 2. 构建SET子句
    var flagColumn = SharedCodeGenerationUtilities.ConvertToSnakeCase(config.FlagColumn);
    var setClause = $"{flagColumn} = true";  // "is_deleted = true"

    // 3. 添加时间戳（如果配置了）
    if (!string.IsNullOrEmpty(config.TimestampColumn))
    {
        var timestampColumn = SharedCodeGenerationUtilities.ConvertToSnakeCase(config.TimestampColumn);
        var timestampSql = GetCurrentTimestampSql(dialect);  // PostgreSQL: "NOW()"
        setClause += $", {timestampColumn} = {timestampSql}";  // "is_deleted = true, deleted_at = NOW()"
    }

    // 4. 提取WHERE子句
    var deleteFromIndex = sql.IndexOf("DELETE FROM", StringComparison.OrdinalIgnoreCase);
    if (deleteFromIndex < 0)
    {
        deleteFromIndex = sql.IndexOf("DELETE", StringComparison.OrdinalIgnoreCase);
    }

    var whereIndex = sql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
    string whereClause = "";

    if (whereIndex > deleteFromIndex)
    {
        whereClause = sql.Substring(whereIndex);  // "WHERE id = @id"
    }
    else
    {
        whereClause = "WHERE 1=1";
    }

    // 5. 构建UPDATE语句
    var snakeTableName = SharedCodeGenerationUtilities.ConvertToSnakeCase(tableName);
    return $"UPDATE {snakeTableName} SET {setClause} {whereClause}";
    // 应该返回: "UPDATE user SET is_deleted = true WHERE id = @id"
}
```

**输入**:
- `sql` = "DELETE FROM user WHERE id = @id"
- `config.FlagColumn` = "IsDeleted"
- `dialect` = "PostgreSql" (或 "2")
- `tableName` = "User"

**期望输出**:
- "UPDATE user SET is_deleted = true WHERE id = @id"

**可能的问题**:
1. `tableName`参数可能不正确（当前使用`entityType?.Name ?? "table"`）
2. WHERE子句提取可能有问题
3. 字符串拼接逻辑可能有错误

---

## 🛠️ 修复计划

### 步骤1: 添加调试日志
在`ConvertDeleteToSoftDelete`中添加调试输出：
```csharp
// Debug: 记录输入和输出
Console.WriteLine($"[ConvertDeleteToSoftDelete] Input SQL: {sql}");
Console.WriteLine($"[ConvertDeleteToSoftDelete] TableName: {tableName}");
Console.WriteLine($"[ConvertDeleteToSoftDelete] Output SQL: {result}");
```

### 步骤2: 验证processedSql赋值
在`CodeGenerationService.cs`第696行之后添加：
```csharp
// Debug
sb.AppendLine($"// DEBUG: processedSql after soft delete = {processedSql}");
```

### 步骤3: 检查GenerateCommandSetup
查看`SharedCodeGenerationUtilities.GenerateCommandSetup`的实现，确认它使用了传入的`processedSql`参数。

### 步骤4: 单元测试ConvertDeleteToSoftDelete
创建单独的单元测试验证这个方法：
```csharp
[TestMethod]
public void Test_ConvertDeleteToSoftDelete()
{
    var config = new SoftDeleteConfig { FlagColumn = "IsDeleted" };
    var input = "DELETE FROM user WHERE id = @id";
    var output = ConvertDeleteToSoftDelete(input, config, "PostgreSql", "User");
    
    Assert.AreEqual("UPDATE user SET is_deleted = true WHERE id = @id", output);
}
```

---

## 📊 当前测试结果

| 测试 | 状态 | 说明 |
|------|------|------|
| SELECT without WHERE | ✅ PASS | 正确添加了WHERE is_deleted = false |
| SELECT with WHERE | ✅ PASS | 正确使用AND连接多个条件 |
| IncludeDeleted绕过 | ✅ PASS | [IncludeDeleted]正确绕过过滤 |
| DELETE转UPDATE | ❌ FAIL | DELETE未被转换为UPDATE |
| DELETE with Timestamp | ❌ FAIL | 时间戳未设置 |

**通过率**: 60% (3/5)

---

## ⏱️ 时间估算

**已用时**: ~2小时  
**剩余时间**: ~1小时

### 剩余任务
1. 调试DELETE转换逻辑 (30分钟)
2. 修复问题并通过测试 (20分钟)
3. 代码清理和文档 (10分钟)

---

## 🎯 成功标准

- [x] SELECT自动过滤已删除记录
- [x] `[IncludeDeleted]`正确绕过过滤
- [ ] DELETE自动转换为UPDATE
- [ ] 支持TimestampColumn
- [x] AOT友好（无反射）
- [x] 多数据库支持

**当前进度**: 4/6 完成 (67%)

---

## 📌 下次继续

1. 运行调试版本的ConvertDeleteToSoftDelete
2. 检查GenerateCommandSetup是否使用了修改后的SQL
3. 如果需要，调整SQL转换的时机或位置
4. 确保所有5个测试通过
5. 提交完整的软删除功能

**预计完成时间**: 1小时

