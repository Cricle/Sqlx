# 软删除特性 - 实施计划

**优先级**: ⭐⭐⭐ 高  
**预计用时**: 2-3小时  
**用户价值**: 高（常见业务需求）

---

## 🎯 目标

实现`[SoftDelete]`特性，自动为实体添加软删除支持：
- 查询自动过滤已删除记录
- DELETE操作自动转换为UPDATE（设置删除标记）
- 提供恢复方法
- 支持"包含已删除"查询选项

---

## 📋 功能需求

### 1. 特性定义

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SoftDeleteAttribute : Attribute
{
    /// <summary>
    /// 软删除标记字段名（默认: "IsDeleted"）
    /// </summary>
    public string FlagColumn { get; set; } = "IsDeleted";
    
    /// <summary>
    /// 删除时间字段名（可选，默认: null）
    /// </summary>
    public string? TimestampColumn { get; set; }
    
    /// <summary>
    /// 删除用户字段名（可选，默认: null）
    /// </summary>
    public string? DeletedByColumn { get; set; }
}
```

### 2. 使用示例

```csharp
[SoftDelete(FlagColumn = "IsDeleted", TimestampColumn = "DeletedAt")]
public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public interface IUserRepository
{
    // 查询 - 自动添加 WHERE is_deleted = false
    Task<List<User>> GetAllAsync();
    
    // 删除 - 转换为 UPDATE users SET is_deleted = true, deleted_at = NOW()
    Task<int> DeleteAsync(long id);
    
    // 软删除方法 - 显式生成
    [SqlTemplate("UPDATE {{table}} SET {{set --include IsDeleted,DeletedAt}} WHERE id = @id")]
    Task<int> SoftDeleteAsync(long id, DateTime? deletedAt);
    
    // 恢复方法 - 显式生成
    [SqlTemplate("UPDATE {{table}} SET is_deleted = false, deleted_at = NULL WHERE id = @id")]
    Task<int> RestoreAsync(long id);
    
    // 包含已删除的查询
    [IncludeDeleted]
    Task<List<User>> GetAllIncludingDeletedAsync();
}
```

---

## 🔧 实现方案

### Phase 1: 基础软删除（2小时）

#### Step 1.1: 创建特性类
**文件**: `src/Sqlx/Annotations/SoftDeleteAttribute.cs`

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class SoftDeleteAttribute : Attribute
{
    public string FlagColumn { get; set; } = "IsDeleted";
    public string? TimestampColumn { get; set; }
    public string? DeletedByColumn { get; set; }
}
```

#### Step 1.2: 创建`[IncludeDeleted]`特性
**文件**: `src/Sqlx/Annotations/IncludeDeletedAttribute.cs`

```csharp
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class IncludeDeletedAttribute : Attribute
{
}
```

#### Step 1.3: 检测软删除配置
**位置**: `CodeGenerationService.cs`

```csharp
private static SoftDeleteConfig? GetSoftDeleteConfig(INamedTypeSymbol entityType)
{
    var attr = entityType.GetAttributes()
        .FirstOrDefault(a => a.AttributeClass?.Name == "SoftDeleteAttribute");
    
    if (attr == null) return null;
    
    return new SoftDeleteConfig
    {
        FlagColumn = attr.NamedArguments
            .FirstOrDefault(a => a.Key == "FlagColumn")
            .Value.Value?.ToString() ?? "IsDeleted",
        TimestampColumn = attr.NamedArguments
            .FirstOrDefault(a => a.Key == "TimestampColumn")
            .Value.Value?.ToString(),
        DeletedByColumn = attr.NamedArguments
            .FirstOrDefault(a => a.Key == "DeletedByColumn")
            .Value.Value?.ToString()
    };
}
```

#### Step 1.4: SELECT查询自动过滤
**位置**: `SqlTemplateEngine.ProcessWherePlaceholder`

```csharp
// 如果实体有[SoftDelete]且方法没有[IncludeDeleted]
if (softDeleteConfig != null && !hasIncludeDeletedAttribute)
{
    var flagColumn = ConvertToSnakeCase(softDeleteConfig.FlagColumn);
    
    // 如果已有WHERE条件，添加AND
    if (existingWhereClause != "1=1")
    {
        return $"{existingWhereClause} AND {flagColumn} = false";
    }
    else
    {
        return $"{flagColumn} = false";
    }
}
```

#### Step 1.5: DELETE转换为UPDATE
**位置**: `CodeGenerationService.cs`

```csharp
// 检测DELETE操作
if (sql.Contains("DELETE", StringComparison.OrdinalIgnoreCase) && softDeleteConfig != null)
{
    // 转换为UPDATE
    var flagColumn = ConvertToSnakeCase(softDeleteConfig.FlagColumn);
    var timestampColumn = softDeleteConfig.TimestampColumn != null 
        ? ConvertToSnakeCase(softDeleteConfig.TimestampColumn) 
        : null;
    
    // 构建SET子句
    var setClause = $"{flagColumn} = true";
    if (timestampColumn != null)
    {
        setClause += $", {timestampColumn} = {GetCurrentTimestampSql(dialect)}";
    }
    
    // 替换DELETE为UPDATE
    sql = sql.Replace("DELETE FROM", $"UPDATE {tableName} SET {setClause} WHERE id IN (SELECT id FROM {tableName} WHERE", 
                     StringComparison.OrdinalIgnoreCase);
    sql += ")";
}
```

---

### Phase 2: 增强功能（1小时）

#### Step 2.1: 智能方法名识别
生成器自动识别以下方法名模式：
- `SoftDeleteAsync` / `SoftDelete` - 执行软删除
- `RestoreAsync` / `Restore` - 恢复已删除
- `HardDeleteAsync` / `HardDelete` - 物理删除（绕过软删除）

#### Step 2.2: DeletedBy支持
如果配置了`DeletedByColumn`：

```csharp
// 检查方法参数是否有deletedBy
var deletedByParam = method.Parameters
    .FirstOrDefault(p => p.Name.Contains("deletedBy", StringComparison.OrdinalIgnoreCase));

if (deletedByParam != null && softDeleteConfig.DeletedByColumn != null)
{
    setClause += $", {deletedByColumn} = @{deletedByParam.Name}";
}
```

---

## 🧪 TDD测试计划

### Red Phase Tests (创建失败测试)

#### Test 1: SELECT自动过滤
```csharp
[TestMethod]
public void SoftDelete_SELECT_Should_Filter_Deleted_Records()
{
    var source = @"
        [SoftDelete]
        public class User {
            public long Id { get; set; }
            public bool IsDeleted { get; set; }
        }
        
        [SqlTemplate(""SELECT * FROM {{table}}"")]
        Task<List<User>> GetAllAsync();
    ";
    
    var sql = GetGeneratedSql(source);
    
    // 应该包含WHERE is_deleted = false
    StringAssert.Contains(sql, "is_deleted = false");
}
```

#### Test 2: DELETE转换为UPDATE
```csharp
[TestMethod]
public void SoftDelete_DELETE_Should_Convert_To_UPDATE()
{
    var source = @"
        [SoftDelete(TimestampColumn = ""DeletedAt"")]
        public class User { ... }
        
        [SqlTemplate(""DELETE FROM {{table}} WHERE id = @id"")]
        Task<int> DeleteAsync(long id);
    ";
    
    var sql = GetGeneratedSql(source);
    
    // 应该转换为UPDATE
    StringAssert.Contains(sql, "UPDATE");
    StringAssert.Contains(sql, "is_deleted = true");
    Assert.IsFalse(sql.Contains("DELETE FROM"));
}
```

#### Test 3: IncludeDeleted绕过过滤
```csharp
[TestMethod]
public void IncludeDeleted_Should_Not_Filter()
{
    var source = @"
        [SoftDelete]
        public class User { ... }
        
        [SqlTemplate(""SELECT * FROM {{table}}"")]
        [IncludeDeleted]
        Task<List<User>> GetAllIncludingDeletedAsync();
    ";
    
    var sql = GetGeneratedSql(source);
    
    // 不应该包含is_deleted过滤
    Assert.IsFalse(sql.Contains("is_deleted"));
}
```

#### Test 4: WHERE子句组合
```csharp
[TestMethod]
public void SoftDelete_Should_Combine_With_Existing_WHERE()
{
    var source = @"
        [SoftDelete]
        public class User { ... }
        
        [SqlTemplate(""SELECT * FROM {{table}} WHERE age > @age"")]
        Task<List<User>> GetActiveUsersAsync(int age);
    ";
    
    var sql = GetGeneratedSql(source);
    
    // 应该是 WHERE age > @age AND is_deleted = false
    StringAssert.Contains(sql, "age > @age");
    StringAssert.Contains(sql, "AND is_deleted = false");
}
```

#### Test 5: 多数据库支持
```csharp
[TestMethod]
public void SoftDelete_Timestamp_Should_Use_Database_Function()
{
    // PostgreSQL: NOW()
    // SQL Server: GETDATE()
    // MySQL: NOW()
    // SQLite: datetime('now')
    // Oracle: SYSDATE
}
```

---

## 📊 实施检查清单

### Phase 1: 基础功能
- [ ] 创建`SoftDeleteAttribute.cs`
- [ ] 创建`IncludeDeletedAttribute.cs`
- [ ] 添加软删除配置检测逻辑
- [ ] SELECT查询自动添加过滤
- [ ] DELETE转换为UPDATE
- [ ] 支持TimestampColumn
- [ ] TDD红灯测试（5个）
- [ ] TDD绿灯实现
- [ ] 单元测试通过

### Phase 2: 增强功能
- [ ] 智能方法名识别
- [ ] DeletedBy支持
- [ ] HardDelete绕过软删除
- [ ] 文档和示例

---

## ⚠️ 注意事项

### 1. WHERE子句处理
需要区分三种情况：
- 无WHERE子句 → 添加`WHERE is_deleted = false`
- 有WHERE子句 → 添加`AND is_deleted = false`
- 有`[IncludeDeleted]` → 不添加过滤

### 2. DELETE语句解析
需要正确解析：
- `DELETE FROM table WHERE ...`
- `DELETE FROM table`
- 带子查询的DELETE

### 3. 性能考虑
软删除字段应该：
- 添加索引（文档建议）
- 考虑分区表策略（大数据量）

### 4. 数据迁移
建议在文档中提供：
- 添加软删除字段的迁移脚本
- 现有数据的默认值设置

---

## 🎯 成功标准

- ✅ 5个TDD测试全部通过
- ✅ 支持3种主要数据库（PostgreSQL, SQL Server, SQLite）
- ✅ SELECT自动过滤已删除记录
- ✅ DELETE自动转换为UPDATE
- ✅ `[IncludeDeleted]`正确绕过过滤
- ✅ WHERE子句正确组合
- ✅ AOT友好（无反射）

---

## 📝 文档输出

完成后需要更新：
1. `SOFT_DELETE_USAGE.md` - 使用指南
2. `BUSINESS_FOCUS_IMPROVEMENT_PLAN.md` - 标记完成
3. `PROGRESS.md` - 更新进度
4. 示例代码

---

## 🚀 下一步

完成软删除后，建议继续：
1. 审计字段特性 `[AuditFields]`（与软删除相似，可复用逻辑）
2. 乐观锁特性 `[ConcurrencyCheck]`

预计总用时: 2-3小时

