# Insert返回ID - 集成和优化测试计划

**日期**: 2025-10-25  
**优先级**: ⭐⭐ 中  
**预计时间**: 1-2小时  

---

## 🎯 目标

1. **功能组合测试** - 确保`[ReturnInsertedId]`/`[ReturnInsertedEntity]`与其他特性正确集成
2. **GC优化验证** - 确保生成代码最小化内存分配
3. **边界情况测试** - 测试异常场景和边界条件

---

## 📋 测试清单

### Phase 1: 功能组合测试 (6个测试)

#### 1.1 ReturnInsertedId + AuditFields
```csharp
[TestMethod]
public void ReturnInsertedId_WithAuditFields_Should_SetTimestamps()
{
    // Entity: [AuditFields] + INSERT method with [ReturnInsertedId]
    // 预期: 
    // - SQL包含CreatedAt = CURRENT_TIMESTAMP
    // - 返回ID正确
}
```

#### 1.2 ReturnInsertedEntity + AuditFields
```csharp
[TestMethod]
public void ReturnInsertedEntity_WithAuditFields_Should_ReturnCompleteEntity()
{
    // Entity: [AuditFields] + INSERT method with [ReturnInsertedEntity]
    // 预期:
    // - RETURNING包含所有列（包括CreatedAt）
    // - 返回实体包含CreatedAt值
}
```

#### 1.3 ReturnInsertedId + SoftDelete
```csharp
[TestMethod]
public void ReturnInsertedId_WithSoftDelete_Should_NotAffectInsert()
{
    // Entity: [SoftDelete] + INSERT method with [ReturnInsertedId]
    // 预期:
    // - INSERT正常执行（软删除不影响INSERT）
    // - 返回ID正确
    // - IsDeleted默认为false（如果有该列）
}
```

#### 1.4 ReturnInsertedEntity + ConcurrencyCheck
```csharp
[TestMethod]
public void ReturnInsertedEntity_WithConcurrencyCheck_Should_ReturnVersion()
{
    // Entity: [ConcurrencyCheck] + INSERT method with [ReturnInsertedEntity]
    // 预期:
    // - RETURNING包含Version列
    // - Version初始值为1（或INSERT时设置的值）
}
```

#### 1.5 ReturnInsertedId + 全部特性
```csharp
[TestMethod]
public void ReturnInsertedId_WithAllFeatures_Should_Work()
{
    // Entity: [SoftDelete] + [AuditFields] + [ConcurrencyCheck]
    // Method: [ReturnInsertedId]
    // 预期:
    // - SQL正确组合所有特性
    // - 返回ID正确
}
```

#### 1.6 ReturnInsertedEntity + 全部特性
```csharp
[TestMethod]
public void ReturnInsertedEntity_WithAllFeatures_Should_ReturnComplete()
{
    // Entity: [SoftDelete] + [AuditFields] + [ConcurrencyCheck]
    // Method: [ReturnInsertedEntity]
    // 预期:
    // - RETURNING包含所有必要列
    // - 返回实体完整
}
```

---

### Phase 2: GC优化验证 (4个测试)

#### 2.1 验证字符串内插最小化
```csharp
[TestMethod]
public void ReturnInsertedId_Should_UsePrecomputedSql()
{
    // 验证:
    // - CommandText使用$""插值（编译时优化）
    // - 无运行时string.Concat或string.Format
    // - 检查生成代码中无不必要的字符串分配
}
```

#### 2.2 验证参数创建优化
```csharp
[TestMethod]
public void ReturnInsertedEntity_Should_MinimizeObjectAllocation()
{
    // 验证:
    // - 使用IDbCommand.CreateParameter()（池化）
    // - 无不必要的临时对象
    // - 无boxing/unboxing
}
```

#### 2.3 验证无反射使用（AOT友好）
```csharp
[TestMethod]
public void ReturnInsertedId_Should_BeAotFriendly()
{
    // 验证（已在Phase 1完成，此处加强）:
    // - 无GetType()调用
    // - 无typeof()动态查找
    // - 无Reflection相关API
}
```

#### 2.4 验证异步模式正确性
```csharp
[TestMethod]
public void ReturnInsertedEntity_Async_Should_AvoidBlocking()
{
    // 验证:
    // - 使用ExecuteReaderAsync而非ExecuteReader
    // - 使用ReadAsync而非Read
    // - 无Task.Wait()或.Result
}
```

---

### Phase 3: 边界和异常测试 (4个测试)

#### 3.1 空值处理
```csharp
[TestMethod]
public void ReturnInsertedId_WithNullableProperties_Should_HandleNulls()
{
    // 场景: Entity包含nullable属性
    // 预期: DBNull.Value正确设置
}
```

#### 3.2 数据库不支持RETURNING
```csharp
[TestMethod]
public void ReturnInsertedId_UnsupportedDatabase_Should_Warn()
{
    // 场景: 使用不支持RETURNING的数据库（如老版本MySQL）
    // 预期: 
    // - 编译警告（通过Roslyn Analyzer）
    // - 或运行时优雅降级
}
```

#### 3.3 ID类型不匹配
```csharp
[TestMethod]
public void ReturnInsertedId_WrongIdType_Should_HandleConversion()
{
    // 场景: ID是long但数据库返回int
    // 预期: 正确类型转换或清晰错误消息
}
```

#### 3.4 并发插入
```csharp
[TestMethod]
public void ReturnInsertedId_ConcurrentInserts_Should_ReturnCorrectIds()
{
    // 场景: 多个线程同时插入
    // 预期: 每个线程返回正确的ID（无混淆）
}
```

---

## 🔧 实施步骤

### 步骤1: 创建功能组合测试 (45分钟)
**文件**: `tests/Sqlx.Tests/InsertReturning/Integration_Tests.cs`

### 步骤2: 创建GC优化测试 (30分钟)
**文件**: `tests/Sqlx.Tests/InsertReturning/GC_Optimization_Tests.cs`

### 步骤3: 创建边界测试 (30分钟)
**文件**: `tests/Sqlx.Tests/InsertReturning/Edge_Case_Tests.cs`

### 步骤4: 运行并修复 (15分钟)
- 运行所有新测试
- 修复发现的问题
- 确保无回归

---

## ✅ 成功标准

- ✅ 14/14测试通过
- ✅ 与所有现有特性无冲突
- ✅ 生成代码GC友好（无不必要分配）
- ✅ AOT兼容性维持
- ✅ 无回归（所有现有测试通过）

---

## 📊 预期结果

**测试增加**: 841 → 855 (+14)  
**进度提升**: 72% → 73% (+1%)  
**完成功能**: Insert返回ID完全就绪（包含所有边界情况）

---

**创建时间**: 2025-10-25  
**状态**: 准备开始  
**下一步**: 创建Integration_Tests.cs

