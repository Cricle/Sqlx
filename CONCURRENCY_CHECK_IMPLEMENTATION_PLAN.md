# 乐观锁特性 - 实施计划

**优先级**: ⭐⭐⭐ 高  
**预计用时**: 2-3小时  
**用户价值**: 高（防止并发更新冲突）

---

## 🎯 目标

实现`[ConcurrencyCheck]`特性，自动为UPDATE操作添加乐观锁检查：
- UPDATE时自动检查version字段
- 检测并发冲突（version不匹配）
- 自动递增version
- 返回受影响行数（0表示冲突）

---

## 📋 功能需求

### 1. 特性定义

```csharp
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class ConcurrencyCheckAttribute : Attribute
{
}
```

### 2. 使用示例

```csharp
public class Product
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    
    [ConcurrencyCheck]
    public int Version { get; set; }
}

public interface IProductRepository
{
    // UPDATE时自动添加：
    // WHERE id = @id AND version = @version
    // SET ..., version = version + 1
    [SqlTemplate("UPDATE {{table}} SET name = @name, price = @price WHERE id = @id")]
    Task<int> UpdateAsync(Product entity);  // 返回0表示并发冲突
}
```

---

## 🔧 实现方案

### Phase 1: 核心实现 (1.5小时)

#### Step 1.1: 创建特性类
**文件**: `src/Sqlx/Annotations/ConcurrencyCheckAttribute.cs`

#### Step 1.2: 检测乐观锁字段
**位置**: `CodeGenerationService.cs`

```csharp
private static string? GetConcurrencyCheckColumn(INamedTypeSymbol entityType)
{
    // 遍历所有属性，找到标记[ConcurrencyCheck]的属性
    foreach (var member in entityType.GetMembers())
    {
        if (member is IPropertySymbol property)
        {
            var hasConcurrencyCheck = property.GetAttributes()
                .Any(a => a.AttributeClass?.Name == "ConcurrencyCheckAttribute" ||
                         a.AttributeClass?.Name == "ConcurrencyCheck");
            
            if (hasConcurrencyCheck)
            {
                return property.Name;  // 返回属性名，如"Version"
            }
        }
    }
    
    return null;
}
```

#### Step 1.3: UPDATE时添加乐观锁检查
**位置**: `CodeGenerationService.cs`

```csharp
if (concurrencyColumn != null && processedSql.IndexOf("UPDATE", StringComparison.OrdinalIgnoreCase) >= 0)
{
    processedSql = AddConcurrencyCheck(processedSql, concurrencyColumn, method);
}
```

#### Step 1.4: 修改UPDATE语句
```csharp
private static string AddConcurrencyCheck(string sql, string versionColumn, IMethodSymbol method)
{
    // 1. 在SET子句中添加: version = version + 1
    // 2. 在WHERE子句中添加: AND version = @version
    
    var versionCol = ConvertToSnakeCase(versionColumn);
    
    // 找到WHERE子句
    var whereIndex = sql.IndexOf("WHERE", StringComparison.OrdinalIgnoreCase);
    if (whereIndex > 0)
    {
        // 在SET子句末尾添加version递增
        var beforeWhere = sql.Substring(0, whereIndex).TrimEnd();
        var afterWhere = sql.Substring(whereIndex);
        
        // 在WHERE子句添加version检查
        var newSql = $"{beforeWhere}, {versionCol} = {versionCol} + 1 {afterWhere}";
        
        // 在WHERE子句末尾添加AND version = @version
        newSql = newSql + $" AND {versionCol} = @{versionColumn.ToLower()}";
        
        return newSql;
    }
    
    return sql;
}
```

---

## 🧪 TDD测试计划

### Red Phase Tests

#### Test 1: UPDATE应该递增version
```csharp
[TestMethod]
public void ConcurrencyCheck_UPDATE_Should_Increment_Version()
{
    var source = @"
        public class Product {
            public long Id { get; set; }
            [ConcurrencyCheck]
            public int Version { get; set; }
        }
        
        [SqlTemplate(""UPDATE {{table}} SET name = @name WHERE id = @id"")]
        Task<int> UpdateAsync(Product entity);
    ";
    
    var sql = GetGeneratedSql(source);
    
    // 应该包含 version = version + 1
    StringAssert.Contains(sql, "version = version + 1");
}
```

#### Test 2: UPDATE应该检查version
```csharp
[TestMethod]
public void ConcurrencyCheck_UPDATE_Should_Check_Version()
{
    var source = @"
        public class Product {
            public long Id { get; set; }
            [ConcurrencyCheck]
            public int Version { get; set; }
        }
        
        [SqlTemplate(""UPDATE {{table}} SET name = @name WHERE id = @id"")]
        Task<int> UpdateAsync(Product entity);
    ";
    
    var sql = GetGeneratedSql(source);
    
    // 应该包含 AND version = @version
    Assert.IsTrue(
        sql.Contains("AND version = @version") || 
        sql.Contains("WHERE") && sql.Contains("version"),
        "应该在WHERE子句检查version");
}
```

#### Test 3: 无WHERE子句应该创建
```csharp
[TestMethod]
public void ConcurrencyCheck_UPDATE_Without_WHERE_Should_Add_WHERE()
{
    var source = @"
        public class Product {
            [ConcurrencyCheck]
            public int Version { get; set; }
        }
        
        [SqlTemplate(""UPDATE {{table}} SET name = @name"")]
        Task<int> UpdateAsync(Product entity);
    ";
    
    var sql = GetGeneratedSql(source);
    
    // 应该添加WHERE version = @version
    StringAssert.Contains(sql, "WHERE");
    StringAssert.Contains(sql, "version");
}
```

#### Test 4: 返回受影响行数
```csharp
[TestMethod]
public void ConcurrencyCheck_Should_Return_Affected_Rows()
{
    var source = @"
        public class Product {
            [ConcurrencyCheck]
            public int Version { get; set; }
        }
        
        [SqlTemplate(""UPDATE {{table}} SET name = @name WHERE id = @id"")]
        Task<int> UpdateAsync(Product entity);
    ";
    
    var generatedCode = GetCSharpGeneratedOutput(source);
    
    // 应该使用ExecuteNonQuery并返回受影响行数
    Assert.IsTrue(
        generatedCode.Contains("ExecuteNonQuery") || 
        generatedCode.Contains("ExecuteNonQueryAsync"),
        "应该执行并返回受影响行数");
}
```

#### Test 5: 与审计字段组合
```csharp
[TestMethod]
public void ConcurrencyCheck_Should_Work_With_AuditFields()
{
    var source = @"
        [AuditFields]
        public class Product {
            public long Id { get; set; }
            [ConcurrencyCheck]
            public int Version { get; set; }
            public DateTime UpdatedAt { get; set; }
        }
        
        [SqlTemplate(""UPDATE {{table}} SET name = @name WHERE id = @id"")]
        Task<int> UpdateAsync(Product entity);
    ";
    
    var sql = GetGeneratedSql(source);
    
    // 应该同时包含version递增和updated_at
    StringAssert.Contains(sql, "version = version + 1");
    StringAssert.Contains(sql, "updated_at");
}
```

---

## 📊 实施检查清单

### Phase 1: 乐观锁核心
- [ ] 创建`ConcurrencyCheckAttribute.cs`
- [ ] 检测乐观锁字段
- [ ] UPDATE添加version递增
- [ ] UPDATE添加version检查
- [ ] 处理无WHERE子句的情况
- [ ] TDD红灯测试（5个）
- [ ] TDD绿灯实现
- [ ] 单元测试通过

### Phase 2: 集成测试
- [ ] 与审计字段组合测试
- [ ] 与软删除组合测试
- [ ] 完整测试套件通过

---

## ⚠️ 注意事项

### 1. WHERE子句处理
需要处理3种情况：
- 有WHERE子句：追加`AND version = @version`
- 无WHERE子句：创建`WHERE version = @version`
- 已有version条件：不重复添加

### 2. 参数检测
需要检查entity参数是否包含version属性：
```csharp
var entityParam = method.Parameters.FirstOrDefault(p => p.Type == entityType);
```

### 3. SET子句插入位置
version递增应该在审计字段之后插入（如果有）。

### 4. 执行顺序
1. 审计字段（UpdatedAt）
2. 乐观锁（version + 1）
3. version检查（WHERE）

---

## 🎯 成功标准

- ✅ 5个TDD测试全部通过
- ✅ UPDATE自动递增version
- ✅ UPDATE自动检查version
- ✅ 返回受影响行数（0=冲突）
- ✅ 与审计字段/软删除兼容
- ✅ AOT友好（无反射）

---

## 📝 SQL示例

### 基本乐观锁
```sql
-- 原始
UPDATE product SET name = @name WHERE id = @id

-- 修改后
UPDATE product SET name = @name, version = version + 1 
WHERE id = @id AND version = @version
```

### 与审计字段组合
```sql
-- 原始
UPDATE product SET name = @name WHERE id = @id

-- 修改后（审计字段+乐观锁）
UPDATE product SET name = @name, updated_at = NOW(), version = version + 1 
WHERE id = @id AND version = @version
```

---

## 🚀 预期用时

- 特性类创建：10分钟
- TDD红灯测试：30分钟
- 核心实现：60分钟
- 测试调试：30分钟
- 集成测试：30分钟

**总计**: ~2.5小时

---

**创建时间**: 2025-10-25  
**状态**: 准备开始实施

