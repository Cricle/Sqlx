# Sqlx业务优先改进 - 当前状态总结

**更新时间**: 2025-10-25

---

## 🎯 总体目标

让`Sqlx`更关注业务逻辑而非SQL编写，通过：
1. ✅ Expression参数支持（替代手写WHERE子句）
2. ✅ EF Core风格特性（软删除、审计字段、乐观锁）
3. ✅ 集合支持增强（IN查询、批量操作、自动分批）
4. ✅ AOT友好、低GC、编译时优化

---

## 📊 总体进度

### Phase 1: Core Improvements (5项)

| 功能 | 状态 | 进度 | 说明 |
|------|------|------|------|
| **Insert返回ID** | 🟢 70% | 实现中 | [ReturnInsertedId]已完成，剩余[ReturnInsertedEntity] |
| Expression参数支持 | ⚪ 0% | 待开始 | 核心功能，高优先级 |
| 软删除特性 | ⚪ 0% | 待开始 | EF Core风格 |
| 审计字段特性 | ⚪ 0% | 待开始 | EF Core风格 |
| 乐观锁特性 | ⚪ 0% | 待开始 | EF Core风格 |

### Phase 2: Collection Enhancements (4项)

| 功能 | 状态 | 进度 |
|------|------|------|
| `{{values @param}}`占位符 | ⚪ 0% | 待开始 |
| Expression `Contains()` | ⚪ 0% | 待开始 |
| `IEnumerable`参数 | ⚪ 0% | 待开始 |
| 自动分批 | ⚪ 0% | 待开始 |

---

## 🏆 当前里程碑: Insert返回ID (70%完成)

### ✅ 已完成 (TDD绿灯阶段)

**功能**: `[ReturnInsertedId]` 特性

**测试结果**: ✅ 4/4测试通过 (100%)

**支持的数据库**:
- ✅ PostgreSQL (`RETURNING id`)
- ✅ SQL Server (`OUTPUT INSERTED.id`)
- ✅ SQLite (`RETURNING id`)
- 🔄 MySQL (预留，待实现)
- 🔄 Oracle (预留，待实现)

**代码示例**:
```csharp
public interface IUserRepository
{
    [SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
    [ReturnInsertedId]
    Task<long> InsertAndGetIdAsync(User entity);
}

// 生成的SQL (PostgreSQL):
// INSERT INTO users (name, email) VALUES (@name, @email) RETURNING id
```

**技术指标**:
- ✅ AOT友好（零反射）
- ✅ 类型安全（编译时检查）
- ✅ 多数据库方言支持
- ✅ ExecuteScalar高效获取ID

---

### 🔄 进行中

**当前任务**: 无（绿灯阶段已完成，等待下一步指示）

**可选的后续任务**:
1. `[ReturnInsertedEntity]` - 返回完整实体 (预计2h)
2. `[SetEntityId]` - 就地修改entity.Id (预计1.5h)
3. MySQL/Oracle完整支持 (预计4.5h)

---

### ⏳ 待完成

| 功能 | 优先级 | 预计时间 | 备注 |
|------|--------|----------|------|
| `[ReturnInsertedEntity]` | 高 | 2h | TDD实现，返回完整实体 |
| `[SetEntityId]` | 中 | 1.5h | 就地修改entity |
| MySQL支持 | 高 | 2h | 使用LAST_INSERT_ID() |
| Oracle支持 | 中 | 2.5h | RETURNING INTO语法 |
| GC优化验证 | 低 | 1h | 性能测试 |
| 功能组合测试 | 中 | 2h | 与其他特性配合 |

**总剩余时间**: 11小时

---

## 📝 关键文件

| 文件 | 用途 | 状态 |
|------|------|------|
| `BUSINESS_FOCUS_IMPROVEMENT_PLAN.md` | 总体改进计划 | ✅ 已完成v2 |
| `TDD_INSERT_RETURN_ID_PROGRESS.md` | Insert返回ID实施文档 | ✅ 绿灯完成 |
| `NEXT_STEPS_INSERT_RETURN_ID.md` | 后续计划 | ✅ 刚创建 |
| `src/Sqlx/Annotations/ReturnInsertedIdAttribute.cs` | 特性定义 | ✅ 已实现 |
| `src/Sqlx.Generator/Core/CodeGenerationService.cs` | 源生成器核心 | ✅ 已修改 |
| `tests/Sqlx.Tests/InsertReturning/TDD_Phase1_RedTests.cs` | TDD测试 | ✅ 4/4通过 |

---

## 🎯 推荐下一步行动

### 选项A: 完成Insert返回ID系列 (推荐)
**优点**: 
- 完整的功能集
- 用户可以立即使用
- 可作为其他特性的参考实现

**时间**: 4.5小时（核心功能）

**任务**:
1. 实现`[ReturnInsertedEntity]` (2h)
2. 实现`[SetEntityId]` (1.5h)  
3. 实现MySQL支持 (1h)

### 选项B: 开始Expression参数支持 (影响更大)
**优点**:
- 核心功能，影响面最广
- 是其他特性的基础（软删除、条件查询等）
- 用户价值最高

**时间**: 预计6-8小时

**挑战**:
- Expression树解析较复杂
- 需要支持多种表达式类型
- SQL注入防护要求高

### 选项C: 实现软删除/审计字段特性 (快速见效)
**优点**:
- 相对简单（可参考Insert返回ID的实现）
- 业务价值高
- 可快速交付

**时间**: 每个特性2-3小时

---

## 💡 建议

**如果追求快速完整交付**: 选择选项A（完成Insert返回ID）

**如果追求最大业务价值**: 选择选项B（Expression参数）

**如果追求快速多个功能**: 选择选项C（软删除+审计字段）

---

## 📈 技术债务

### 当前系统中的已知限制：

1. **主键列名硬编码为`id`**
   - 将来应支持自定义：`[ReturnInsertedId(IdColumn = "UserId")]`

2. **不支持复合主键**
   - 将来应支持：`Task<(int, long)> InsertAsync(...)`

3. **不支持GUID主键**
   - PostgreSQL: `gen_random_uuid()`
   - SQL Server: `NEWID()`

4. **不支持批量插入返回ID列表**
   - 将来应支持：`Task<long[]> InsertManyAsync(IEnumerable<User>)`

这些限制不影响当前功能的使用，可以在后续版本中增强。

---

## 🚀 系统性能

**当前性能指标**（基于Insert返回ID实现）:
- ✅ 零反射调用（AOT友好）
- ✅ 编译时类型检查
- ✅ 直接ExecuteScalar（无额外开销）
- 🔄 GC分配待验证（预期接近Dapper）

**性能对比目标**:
- 吞吐量：≥ Dapper 95%
- GC分配：≤ Dapper 110%
- 启动时间：编译时生成，零启动开销

---

## 📞 下一步决策点

**请选择下一步方向**:

1. **A**: 完成Insert返回ID所有功能 (ReturnInsertedEntity + SetEntityId + MySQL)
2. **B**: 开始Expression参数支持实现
3. **C**: 实现软删除和审计字段特性  
4. **其他**: 请指定优先级

**当前状态**: ✅ TDD绿灯完成，等待下一步指示

