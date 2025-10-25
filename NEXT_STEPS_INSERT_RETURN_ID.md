# Insert返回ID功能 - 后续实施计划

## ✅ 已完成（TDD绿灯）

- [x] `[ReturnInsertedId]` 特性
  - PostgreSQL: `RETURNING id`
  - SQL Server: `OUTPUT INSERTED.id`
  - SQLite: `RETURNING id`
  - AOT友好实现
  - 4/4测试通过

## 🔄 待实施功能

### 1. `[ReturnInsertedEntity]` 特性 (高优先级)
**用途**: 返回完整的新插入实体（包含数据库生成的所有字段）

```csharp
public interface IUserRepository
{
    [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
    [ReturnInsertedEntity]
    Task<User> InsertAndGetEntityAsync(User entity);
}
```

**技术要点**:
- PostgreSQL: `INSERT ... RETURNING *`
- SQL Server: `INSERT ... OUTPUT INSERTED.*`
- 需要映射返回的所有列到实体
- 复用现有的实体映射逻辑

**预计工作量**: 2小时

---

### 2. `[SetEntityId]` 特性 (中优先级)
**用途**: 就地修改传入的entity，设置其Id属性

```csharp
public interface IUserRepository
{
    [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
    [SetEntityId]
    Task InsertAsync(User entity); // entity.Id会被自动设置
}
```

**技术要点**:
- 检测实体的Id属性
- 生成代码：`entity.Id = Convert.ToInt64(scalarResult);`
- 需要验证实体有可写的Id属性

**预计工作量**: 1.5小时

---

### 3. MySQL支持 (高优先级)
**当前状态**: 预留但未实现

**技术方案**:
- MySQL不支持RETURNING子句
- 需要两步操作：
  1. 执行INSERT
  2. 执行 `SELECT LAST_INSERT_ID()`
  
```csharp
// 生成的代码示例
__cmd__.CommandText = @"INSERT INTO users (name) VALUES (@name)";
__cmd__.ExecuteNonQuery();

// 然后获取ID
__cmd__.CommandText = "SELECT LAST_INSERT_ID()";
var scalarResult = __cmd__.ExecuteScalar();
__result__ = Convert.ToInt64(scalarResult);
```

**挑战**:
- 需要两次数据库调用
- 需要确保在同一个连接/事务中
- 性能略低于RETURNING

**预计工作量**: 2小时（含测试）

---

### 4. Oracle支持 (中优先级)
**当前状态**: 预留但未实现

**技术方案**:
Oracle支持RETURNING，但语法不同：

```sql
INSERT INTO users (name) VALUES (:name) RETURNING id INTO :out_id
```

需要：
1. 修改SQL添加 `RETURNING id INTO :out_id`
2. 创建输出参数 `:out_id`
3. 从输出参数读取值

```csharp
// 生成的代码示例
__cmd__.CommandText = @"INSERT INTO users (name) VALUES (:name) RETURNING id INTO :out_id";

var outParam = __cmd__.CreateParameter();
outParam.ParameterName = ":out_id";
outParam.Direction = ParameterDirection.Output;
outParam.DbType = DbType.Int64;
__cmd__.Parameters.Add(outParam);

__cmd__.ExecuteNonQuery();
__result__ = Convert.ToInt64(outParam.Value);
```

**挑战**:
- 需要处理输出参数
- Oracle参数语法不同（`:` 而不是 `@`）

**预计工作量**: 2.5小时（含测试）

---

### 5. GC优化 (低优先级，但重要)
**目标**: 零额外GC分配

**优化点**:
1. ✅ 已使用 `Convert.ToInt64()` 避免装箱
2. ✅ 已使用 `ExecuteScalar()` 而不是 `ExecuteReader()`
3. 🔄 支持 `ValueTask<long>` 返回类型（需测试验证）
4. 🔄 考虑使用 `Span<T>` 处理字符串（如果适用）

**验证方法**:
- 使用 BenchmarkDotNet 测量GC分配
- 与Dapper对比
- 目标：0 Gen0, 0 Gen1, 0 Gen2

**预计工作量**: 1小时（性能测试+优化）

---

### 6. 功能组合测试 (中优先级)
**目标**: 确保与其他特性配合工作

**测试场景**:
1. `[ReturnInsertedId]` + `[AuditFields]` - 自动填充CreatedAt后返回ID
2. `[ReturnInsertedId]` + `[SoftDelete]` - 确保不冲突
3. `[ReturnInsertedEntity]` + `[AuditFields]` - 返回包含审计字段的完整实体
4. 批量插入 + 返回ID列表

**预计工作量**: 2小时

---

## 📊 总体时间估算

| 功能 | 优先级 | 预计时间 | 依赖 |
|------|--------|----------|------|
| ReturnInsertedEntity | 高 | 2h | 无 |
| SetEntityId | 中 | 1.5h | 无 |
| MySQL支持 | 高 | 2h | 无 |
| Oracle支持 | 中 | 2.5h | 无 |
| GC优化验证 | 低 | 1h | 前4项完成 |
| 功能组合测试 | 中 | 2h | AuditFields, SoftDelete实现 |

**总计**: 11小时（分多个阶段完成）

---

## 🎯 推荐实施顺序

### Phase 1: 核心功能完善 (4.5小时)
1. `[ReturnInsertedEntity]` (2h) - TDD实现
2. `[SetEntityId]` (1.5h) - TDD实现  
3. MySQL支持 (2h) - 补充测试

### Phase 2: 扩展支持 (2.5小时)
4. Oracle支持 (2.5h) - 补充测试

### Phase 3: 优化和集成 (3小时)
5. GC优化验证 (1h) - 性能测试
6. 功能组合测试 (2h) - 集成测试

---

## 💡 技术债务提醒

1. **参数命名**: 当前假设主键列名为 `id`，将来应支持自定义：
   ```csharp
   [ReturnInsertedId(IdColumn = "UserId")]
   Task<long> InsertAsync(User user);
   ```

2. **复合主键**: 暂不支持复合主键，将来可扩展：
   ```csharp
   [ReturnInsertedId(IdColumns = new[] { "TenantId", "UserId" })]
   Task<(int TenantId, long UserId)> InsertAsync(User user);
   ```

3. **GUID主键**: 需要特殊处理UUID：
   ```csharp
   Task<Guid> InsertAsync(User user); // PostgreSQL gen_random_uuid()
   ```

4. **批量插入返回ID**: 需要返回ID数组：
   ```csharp
   [ReturnInsertedId]
   Task<long[]> InsertManyAsync(IEnumerable<User> users);
   ```

---

## ✅ 当前状态总结

**已实现**: 基础的`[ReturnInsertedId]`功能（PostgreSQL, SQL Server, SQLite）
**代码质量**: ✅ AOT友好, ✅ 测试覆盖, ✅ 多数据库
**性能**: ✅ 零反射, 🔄 GC优化待验证

**下一个里程碑**: 实现`[ReturnInsertedEntity]`返回完整实体

