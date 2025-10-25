# 审计字段特性 - 实施计划

**优先级**: ⭐⭐⭐ 高  
**预计用时**: 2-3小时  
**用户价值**: 高（常见业务需求）

---

## 🎯 目标

实现`[AuditFields]`特性，自动为INSERT/UPDATE操作添加审计字段：
- INSERT：自动设置CreatedAt, CreatedBy
- UPDATE：自动设置UpdatedAt, UpdatedBy
- 支持自定义字段名
- 支持多数据库时间函数

---

## 📋 功能需求

### 1. 特性定义

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class AuditFieldsAttribute : Attribute
{
    /// <summary>
    /// 创建时间字段名（默认: "CreatedAt"）
    /// </summary>
    public string CreatedAtColumn { get; set; } = "CreatedAt";
    
    /// <summary>
    /// 创建人字段名（默认: null，不启用）
    /// </summary>
    public string? CreatedByColumn { get; set; }
    
    /// <summary>
    /// 更新时间字段名（默认: "UpdatedAt"）
    /// </summary>
    public string UpdatedAtColumn { get; set; } = "UpdatedAt";
    
    /// <summary>
    /// 更新人字段名（默认: null，不启用）
    /// </summary>
    public string? UpdatedByColumn { get; set; }
}
```

### 2. 使用示例

```csharp
[AuditFields(CreatedByColumn = "CreatedBy", UpdatedByColumn = "UpdatedBy")]
public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public interface IUserRepository
{
    // INSERT - 自动设置 created_at = NOW(), created_by = @createdBy
    [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
    Task<int> InsertAsync(User entity);
    
    // UPDATE - 自动设置 updated_at = NOW(), updated_by = @updatedBy
    [SqlTemplate("UPDATE {{table}} SET {{set}} WHERE id = @id")]
    Task<int> UpdateAsync(User entity);
}
```

---

## 🔧 实现方案

### Phase 1: INSERT审计 (1小时)

#### Step 1.1: 创建特性类
**文件**: `src/Sqlx/Annotations/AuditFieldsAttribute.cs`

#### Step 1.2: 检测审计配置
**位置**: `CodeGenerationService.cs`

```csharp
private static AuditFieldsConfig? GetAuditFieldsConfig(INamedTypeSymbol entityType)
{
    var attr = entityType.GetAttributes()
        .FirstOrDefault(a => a.AttributeClass?.Name == "AuditFieldsAttribute");
    
    if (attr == null) return null;
    
    return new AuditFieldsConfig
    {
        CreatedAtColumn = GetAttributeNamedArgument(attr, "CreatedAtColumn", "CreatedAt"),
        CreatedByColumn = GetAttributeNamedArgument(attr, "CreatedByColumn", null),
        UpdatedAtColumn = GetAttributeNamedArgument(attr, "UpdatedAtColumn", "UpdatedAt"),
        UpdatedByColumn = GetAttributeNamedArgument(attr, "UpdatedByColumn", null)
    };
}
```

#### Step 1.3: INSERT时添加审计字段
**位置**: `CodeGenerationService.cs`

```csharp
if (auditConfig != null && processedSql.IndexOf("INSERT", StringComparison.OrdinalIgnoreCase) >= 0)
{
    processedSql = AddAuditFieldsToInsert(processedSql, auditConfig, dialect);
}
```

#### Step 1.4: 修改INSERT语句
```csharp
private static string AddAuditFieldsToInsert(string sql, AuditFieldsConfig config, string dialect)
{
    // INSERT INTO table (col1, col2) VALUES (val1, val2)
    // 变为:
    // INSERT INTO table (col1, col2, created_at, created_by) 
    // VALUES (val1, val2, NOW(), @createdBy)
    
    var createdAtCol = ConvertToSnakeCase(config.CreatedAtColumn);
    var timestampSql = GetCurrentTimestampSql(dialect);
    
    // 解析SQL并添加字段
    // ...
}
```

---

### Phase 2: UPDATE审计 (1小时)

#### Step 2.1: UPDATE时添加审计字段
```csharp
if (auditConfig != null && processedSql.IndexOf("UPDATE", StringComparison.OrdinalIgnoreCase) >= 0)
{
    processedSql = AddAuditFieldsToUpdate(processedSql, auditConfig, dialect);
}
```

#### Step 2.2: 修改UPDATE语句
```csharp
private static string AddAuditFieldsToUpdate(string sql, AuditFieldsConfig config, string dialect)
{
    // UPDATE table SET col1 = val1 WHERE ...
    // 变为:
    // UPDATE table SET col1 = val1, updated_at = NOW(), updated_by = @updatedBy WHERE ...
    
    var updatedAtCol = ConvertToSnakeCase(config.UpdatedAtColumn);
    var timestampSql = GetCurrentTimestampSql(dialect);
    
    // 在SET子句末尾添加
    // ...
}
```

---

## 🧪 TDD测试计划

### Red Phase Tests

#### Test 1: INSERT设置CreatedAt
```csharp
[TestMethod]
public void AuditFields_INSERT_Should_Set_CreatedAt()
{
    var source = @"
        [AuditFields]
        public class User {
            public long Id { get; set; }
            public DateTime CreatedAt { get; set; }
        }
        
        [SqlTemplate(""INSERT INTO {{table}} (name) VALUES (@name)"")]
        Task<int> InsertAsync(string name);
    ";
    
    var sql = GetGeneratedSql(source);
    
    // 应该包含 created_at = NOW()
    StringAssert.Contains(sql, "created_at");
    StringAssert.Contains(sql, "NOW()");
}
```

#### Test 2: INSERT设置CreatedBy
```csharp
[TestMethod]
public void AuditFields_INSERT_Should_Set_CreatedBy()
{
    var source = @"
        [AuditFields(CreatedByColumn = ""CreatedBy"")]
        public class User { ... }
        
        Task<int> InsertAsync(User entity, string createdBy);
    ";
    
    var sql = GetGeneratedSql(source);
    
    // 应该包含 created_by = @createdBy
    StringAssert.Contains(sql, "created_by");
}
```

#### Test 3: UPDATE设置UpdatedAt
```csharp
[TestMethod]
public void AuditFields_UPDATE_Should_Set_UpdatedAt()
{
    var source = @"
        [AuditFields]
        public class User { ... }
        
        [SqlTemplate(""UPDATE {{table}} SET name = @name WHERE id = @id"")]
        Task<int> UpdateAsync(long id, string name);
    ";
    
    var sql = GetGeneratedSql(source);
    
    // 应该在SET子句添加 updated_at = NOW()
    StringAssert.Contains(sql, "updated_at");
    StringAssert.Contains(sql, "NOW()");
}
```

#### Test 4: UPDATE设置UpdatedBy
```csharp
[TestMethod]
public void AuditFields_UPDATE_Should_Set_UpdatedBy()
{
    var source = @"
        [AuditFields(UpdatedByColumn = ""UpdatedBy"")]
        public class User { ... }
        
        Task<int> UpdateAsync(User entity, string updatedBy);
    ";
    
    var sql = GetGeneratedSql(source);
    
    // 应该包含 updated_by = @updatedBy
    StringAssert.Contains(sql, "updated_by");
}
```

#### Test 5: 多数据库支持
```csharp
[TestMethod]
public void AuditFields_Should_Support_Multiple_Databases()
{
    // PostgreSQL: NOW()
    // SQL Server: GETDATE()
    // MySQL: NOW()
    // SQLite: datetime('now')
    // Oracle: SYSDATE
}
```

#### Test 6: 与软删除组合
```csharp
[TestMethod]
public void AuditFields_Should_Work_With_SoftDelete()
{
    var source = @"
        [AuditFields]
        [SoftDelete]
        public class User { ... }
        
        // DELETE转UPDATE时，应该同时设置updated_at
    ";
}
```

---

## 📊 实施检查清单

### Phase 1: INSERT审计
- [ ] 创建`AuditFieldsAttribute.cs`
- [ ] 添加审计字段配置检测
- [ ] INSERT语句解析逻辑
- [ ] 添加审计字段到INSERT
- [ ] CreatedBy参数支持
- [ ] TDD红灯测试（3个）
- [ ] TDD绿灯实现
- [ ] 单元测试通过

### Phase 2: UPDATE审计
- [ ] UPDATE语句解析逻辑
- [ ] 添加审计字段到UPDATE
- [ ] UpdatedBy参数支持
- [ ] TDD测试（3个）
- [ ] 单元测试通过

### Phase 3: 集成测试
- [ ] 多数据库测试
- [ ] 与软删除组合测试
- [ ] 文档和示例

---

## ⚠️ 注意事项

### 1. SQL解析复杂度
INSERT/UPDATE语句格式多样：
- `INSERT INTO table (cols) VALUES (vals)`
- `INSERT INTO table VALUES (vals)`  // 无列名
- `UPDATE table SET col1 = val1, col2 = val2 WHERE ...`
- `UPDATE table SET col1 = val1 WHERE ...`  // 单列

需要健壮的解析逻辑。

### 2. 参数检测
CreatedBy/UpdatedBy需要检查方法参数：
- 参数名匹配（`createdBy`, `updatedBy`）
- 参数类型（`string`, `long`, `Guid`等）
- 自动添加参数绑定

### 3. 字段排除
审计字段应该从`{{columns}}`和`{{values}}`占位符中排除：
- CreatedAt, UpdatedAt自动设置，不应由用户提供
- CreatedBy, UpdatedBy由参数提供

### 4. 与现有功能组合
- 软删除：DELETE转UPDATE时也应设置UpdatedAt
- Insert返回ID：不冲突
- Expression：不冲突

---

## 🎯 成功标准

- ✅ 6个TDD测试全部通过
- ✅ 支持PostgreSQL, SQL Server, SQLite
- ✅ INSERT自动设置CreatedAt
- ✅ UPDATE自动设置UpdatedAt
- ✅ CreatedBy/UpdatedBy参数支持
- ✅ 与软删除正确组合
- ✅ AOT友好（无反射）

---

## 📝 文档输出

完成后需要更新：
1. `AUDIT_FIELDS_USAGE.md` - 使用指南
2. `BUSINESS_FOCUS_IMPROVEMENT_PLAN.md` - 标记完成
3. `PROGRESS.md` - 更新进度
4. 示例代码

---

## 🚀 下一步

完成审计字段后，建议继续：
1. 乐观锁特性 `[ConcurrencyCheck]` (2-3h)
2. 集合支持增强 (3-4h)

预计总用时: 2-3小时

