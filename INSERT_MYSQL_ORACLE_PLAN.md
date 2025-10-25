# Insert MySQL/Oracle 支持实施计划

**日期**: 2025-10-25  
**优先级**: ⭐⭐⭐ 高  
**预计时间**: 3-4小时  

---

## 🎯 目标

扩展`[ReturnInsertedId]`和`[ReturnInsertedEntity]`支持MySQL和Oracle数据库。

---

## 📊 当前状态

### 已支持
- ✅ PostgreSQL: `RETURNING id` / `RETURNING *`
- ✅ SQLite: `RETURNING id` / `RETURNING *`
- ✅ SQL Server: `OUTPUT INSERTED.id` / `OUTPUT INSERTED.*`

### 待支持
- ⏳ MySQL: `LAST_INSERT_ID()`
- ⏳ Oracle: `RETURNING ... INTO`

---

## 📋 MySQL实施方案

### 方案1: LAST_INSERT_ID() (推荐)
```sql
-- 执行INSERT
INSERT INTO product (name, price) VALUES (@name, @price);

-- 执行SELECT获取ID
SELECT LAST_INSERT_ID();
```

**优点**:
- 简单可靠
- MySQL官方推荐
- 支持所有MySQL版本

**实施**:
1. 检测到MySQL方言时，不修改INSERT SQL
2. 生成代码执行两个命令：
   - `ExecuteNonQuery()` 执行INSERT
   - `ExecuteScalar()` 执行 `SELECT LAST_INSERT_ID()`

### 方案2: 多语句执行
```sql
INSERT INTO product (name, price) VALUES (@name, @price);
SELECT LAST_INSERT_ID();
```

**优点**:
- 单次数据库往返
- 性能稍好

**缺点**:
- 需要启用`AllowMultipleStatements`（安全风险）
- 不是所有MySQL连接库都支持

**决策**: 使用方案1（更安全、更兼容）

### ReturnInsertedEntity for MySQL

MySQL不支持`RETURNING *`，需要：
1. INSERT获取ID
2. 执行SELECT查询完整实体

```csharp
// Step 1: INSERT + LAST_INSERT_ID
var insertedId = ExecuteScalar("INSERT ... ; SELECT LAST_INSERT_ID()");

// Step 2: SELECT完整实体
return ExecuteQuery("SELECT * FROM table WHERE id = @id", insertedId);
```

---

## 📋 Oracle实施方案

### RETURNING INTO 语法
```sql
-- Oracle单行INSERT
INSERT INTO product (name, price) 
VALUES (:name, :price)
RETURNING id INTO :out_id;

-- Oracle批量INSERT (需要BULK COLLECT)
INSERT INTO product (name, price) 
VALUES (:name, :price)
RETURNING id BULK COLLECT INTO :out_ids;
```

**注意事项**:
1. Oracle使用`:param`而非`@param`
2. 需要声明OUT参数
3. ReturnInsertedEntity需要列出所有列（不支持`*`）

### ReturnInsertedEntity for Oracle
```sql
INSERT INTO product (name, price) 
VALUES (:name, :price)
RETURNING id, name, price, created_at INTO :out_id, :out_name, :out_price, :out_created_at;
```

**实施挑战**:
- 需要知道所有列名
- 需要创建多个OUT参数
- 参数映射复杂

**简化方案**:
类似MySQL，使用两步：
1. INSERT + RETURNING id INTO :out_id
2. SELECT * FROM table WHERE id = :out_id

---

## 🧪 TDD测试计划

### MySQL Tests (6个测试)

#### 1. ReturnInsertedId - MySQL
```csharp
[TestMethod]
public void ReturnInsertedId_MySQL_Should_UseLAST_INSERT_ID()
{
    // 预期生成:
    // 1. ExecuteNonQuery(INSERT...)
    // 2. ExecuteScalar(SELECT LAST_INSERT_ID())
}
```

#### 2. ReturnInsertedEntity - MySQL  
```csharp
[TestMethod]
public void ReturnInsertedEntity_MySQL_Should_Use_INSERT_Then_SELECT()
{
    // 预期生成:
    // 1. INSERT + LAST_INSERT_ID
    // 2. SELECT * WHERE id = @lastInsertId
}
```

#### 3. MySQL与AuditFields集成
```csharp
[TestMethod]
public void MySQL_ReturnInsertedId_WithAuditFields_Should_Work()
{
    // 验证审计字段正确注入
}
```

### Oracle Tests (6个测试)

#### 4. ReturnInsertedId - Oracle
```csharp
[TestMethod]
public void ReturnInsertedId_Oracle_Should_Use_RETURNING_INTO()
{
    // 预期生成:
    // INSERT ... RETURNING id INTO :out_id
}
```

#### 5. ReturnInsertedEntity - Oracle
```csharp
[TestMethod]
public void ReturnInsertedEntity_Oracle_Should_Use_Two_Step()
{
    // 预期生成:
    // 1. INSERT ... RETURNING id INTO :out_id
    // 2. SELECT * WHERE id = :out_id
}
```

#### 6. Oracle参数格式
```csharp
[TestMethod]
public void Oracle_Should_Use_Colon_Parameters()
{
    // 验证使用 :param 而非 @param
}
```

---

## 🔧 实施步骤

### 步骤1: 创建TDD红灯测试 (45分钟)
**文件**: `tests/Sqlx.Tests/InsertReturning/TDD_MySQL_Oracle_RedTests.cs`

### 步骤2: 修改CodeGenerationService (90分钟)

**2.1 更新AddReturningClauseForInsert**
- MySQL: 返回原SQL（不修改）
- Oracle: 添加`RETURNING id INTO :out_id`

**2.2 更新GenerateActualDatabaseExecution**
- 检测MySQL方言
  - `hasReturnInsertedId`: 生成两步代码（INSERT + LAST_INSERT_ID）
  - `hasReturnInsertedEntity`: 生成三步代码（INSERT + ID + SELECT）
  
- 检测Oracle方言
  - `hasReturnInsertedId`: 使用RETURNING INTO
  - `hasReturnInsertedEntity`: 两步（RETURNING id + SELECT）

**2.3 参数格式转换（Oracle）**
- 添加辅助方法`ConvertToOracleParameters`
- `@param` → `:param`

### 步骤3: 运行测试和调试 (60分钟)
- 确保12/12测试通过
- 验证与现有特性集成
- 确保无回归

### 步骤4: 文档更新 (15分钟)
- 更新README
- 添加MySQL/Oracle使用示例

---

## ⚠️ 注意事项

### MySQL
1. **连接字符串**: 可能需要特殊配置
2. **自增ID**: 确保表有AUTO_INCREMENT列
3. **并发**: LAST_INSERT_ID()是连接安全的

### Oracle
1. **参数格式**: `:param` vs `@param`
2. **序列**: Oracle传统使用序列，现代版本支持IDENTITY
3. **OUT参数**: 需要正确设置参数方向

### 兼容性
- MySQL: 5.7+
- Oracle: 12c+ (IDENTITY列) 或任意版本（序列）

---

## ✅ 成功标准

- ✅ 12/12 MySQL/Oracle测试通过
- ✅ 与所有现有特性集成无冲突
- ✅ 代码生成符合数据库最佳实践
- ✅ 无回归（所有846个现有测试通过）
- ✅ 文档完整

---

## 📊 预期结果

**测试增加**: 846 → 858 (+12)  
**进度提升**: 73% → 74% (+1%)  
**数据库覆盖**: 3/5 → 5/5 (100%)

---

## 🚀 后续优化（可选）

1. **MySQL多语句支持**（需要配置）
2. **Oracle BULK COLLECT**（批量操作）
3. **自动检测序列名**（Oracle传统模式）

---

**创建时间**: 2025-10-25  
**状态**: 准备开始  
**下一步**: 创建TDD红灯测试

