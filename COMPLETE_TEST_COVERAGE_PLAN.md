# 完整测试覆盖任务计划

## 📋 用户需求

1. ✅ **所有测试必须通过** - All tests must pass
2. ✅ **完全覆盖所有方法** - Complete method coverage
3. ✅ **支持record类型** - Support record types
4. ✅ **支持结构体返回值** - Support struct return values
5. ✅ **可以调整源生成代码** - Can modify source generator

## 📊 当前状态

### 已完成
- ✅ 创建测试框架 `PredefinedInterfacesTests.cs`
- ✅ 定义测试实体:
  - `User` (record类型)
  - `Product` (class类型)
  - `UserStats` (struct结构体)
- ✅ 定义测试仓储
  - UserCrudRepository
  - UserQueryRepository
  - UserCommandRepository
  - UserAggregateRepository
  - UserBatchRepository
  - ProductRepository
- ✅ 现有1412个测试全部通过

### 待完成 (197个编译错误需要修复)

#### 1. 源生成器修复 - Record类型支持 🔴 **关键任务**

**问题**: 源生成器不正确处理record类型
- record类型有内部属性 `EqualityContract`
- record类型使用`init`而不是`set`
- 生成器需要过滤掉`EqualityContract`属性

**修复位置**: `src/Sqlx.Generator/SqlGen/ObjectMap.cs`
```csharp
Properties = ElementSymbol is INamedTypeSymbol namedTypeSymbol
    ? namedTypeSymbol.GetMembers().OfType<IPropertySymbol>()
        .Where(p => p.CanBeReferencedByName && 
                    p.Name != "EqualityContract" &&  // ← 添加这个过滤
                    !p.IsStatic)
        .ToList()
    : new List<IPropertySymbol>();
```

#### 2. 源生成器修复 - 结构体返回值支持 🔴 **关键任务**

**问题**: 方法返回值不支持struct类型（如`UserStats`）

**需要修改的位置**:
- `src/Sqlx.Generator/Core/CodeGenerationService.cs`
  - 修改类型检测逻辑支持struct
  - 修改实体构造逻辑支持struct

**实现建议**:
```csharp
// 检测是否为值类型
if (returnTypeSymbol.IsValueType)
{
    // 生成struct初始化代码
    // 例如: new UserStats { Field1 = value1, Field2 = value2 }
}
```

#### 3. 标量类型返回值支持 🟡 **重要任务**

**问题**: `GetDistinctValuesAsync` 等方法返回 `List<string>`

**需要实现**:
- 检测标量返回类型（string, int, long, DateTime等）
- 生成简单的标量值读取代码
- 不尝试映射到实体属性

**受影响的方法**:
- `IQueryRepository.GetDistinctValuesAsync` - 返回 `List<string>`
- 潜在的其他自定义标量返回方法

#### 4. 未实现方法的特殊处理 🟡 **重要任务**

以下方法需要特殊实现逻辑：

**需要双查询**:
- `IQueryRepository.GetPageAsync` - COUNT(*) + SELECT

**需要数据库特定语法**:
- `ICommandRepository.UpsertAsync` - MERGE/INSERT ON CONFLICT
- `IBatchRepository.BatchUpdateAsync` - CASE WHEN批量更新
- `IBatchRepository.BatchUpsertAsync` - 批量UPSERT
- `IBatchRepository.BatchExistsAsync` - 返回多个布尔值

**需要手动实现**:
- `IAdvancedRepository.*` - 所有Raw SQL方法
- `ISchemaRepository.*` - 所有Schema检查方法
- `IMaintenanceRepository.RebuildIndexesAsync`
- `IMaintenanceRepository.UpdateStatisticsAsync`
- `IMaintenanceRepository.ShrinkTableAsync`

#### 5. 完整测试用例编写 🟢 **测试任务**

为每个接口编写完整的集成测试：

**IQueryRepository** (12个方法):
- [x] GetByIdAsync - 基础测试已有
- [x] GetAllAsync - 基础测试已有
- [x] ExistsAsync - 基础测试已有
- [ ] GetByIdsAsync - 需要添加
- [ ] GetTopAsync - 需要添加
- [ ] GetRangeAsync - 需要添加
- [ ] GetPageAsync - 需要添加
- [ ] GetWhereAsync - 需要添加
- [ ] GetFirstWhereAsync - 需要添加
- [ ] ExistsWhereAsync - 需要添加
- [ ] GetRandomAsync - 需要添加

**ICommandRepository** (12个方法):
- [x] DeleteAsync - 基础测试已有
- [ ] InsertAsync - 需要添加
- [ ] InsertAndGetIdAsync - 需要添加
- [ ] InsertAndGetEntityAsync - 需要添加
- [ ] UpdateAsync - 需要添加
- [ ] UpdatePartialAsync - 需要添加
- [ ] UpdateWhereAsync - 需要添加
- [ ] DeleteWhereAsync - 需要添加
- [ ] UpsertAsync - 需要添加
- [ ] SoftDeleteAsync - 需要添加
- [ ] RestoreAsync - 需要添加
- [ ] PurgeDeletedAsync - 需要添加

**IAggregateRepository** (15个方法):
- [x] CountAsync - 基础测试已有
- [x] SumAsync - 基础测试已有
- [x] AvgAsync - 基础测试已有
- [ ] CountWhereAsync - 需要添加
- [ ] CountByAsync - 需要添加
- [ ] SumWhereAsync - 需要添加
- [ ] AvgWhereAsync - 需要添加
- [ ] MaxIntAsync - 需要添加
- [ ] MaxLongAsync - 需要添加
- [ ] MaxDecimalAsync - 需要添加
- [ ] MaxDateTimeAsync - 需要添加
- [ ] MinIntAsync - 需要添加
- [ ] MinLongAsync - 需要添加
- [ ] MinDecimalAsync - 需要添加
- [ ] MinDateTimeAsync - 需要添加

**IBatchRepository** (8个方法):
- [x] BatchInsertAsync - 基础测试已有
- [ ] BatchInsertAndGetIdsAsync - 需要添加
- [ ] BatchUpdateAsync - 需要添加
- [ ] BatchUpdateWhereAsync - 需要添加
- [ ] BatchDeleteAsync - 需要添加
- [ ] BatchSoftDeleteAsync - 需要添加
- [ ] BatchUpsertAsync - 需要添加
- [ ] BatchExistsAsync - 需要添加

**ICrudRepository** (继承自IQueryRepository + ICommandRepository):
- [ ] 所有继承方法的测试
- [ ] CountAsync - 重写方法测试

**总计**: 约60个主要方法需要完整测试覆盖

## 🎯 执行计划

### Phase 1: 修复源生成器 (最高优先级) 🔴

**估计时间**: 2-3小时
**目标**: 197个编译错误降到0

1. ✅ **修复record类型支持**
   - 文件: `src/Sqlx.Generator/SqlGen/ObjectMap.cs`
   - 过滤 `EqualityContract` 属性
   - 处理 `init` 访问器

2. ✅ **修复struct返回值支持**
   - 文件: `src/Sqlx.Generator/Core/CodeGenerationService.cs`
   - 添加值类型检测
   - 生成struct初始化代码

3. ✅ **测试编译**
   - 验证所有197个错误已修复
   - 确保现有1412个测试仍然通过

### Phase 2: 实现标量类型支持 (高优先级) 🟡

**估计时间**: 1-2小时

1. ✅ **恢复GetDistinctValuesAsync**
   - 取消注释方法
   - 修改生成器支持标量返回类型

2. ✅ **测试标量方法**
   - 验证返回`List<string>`正确
   - 添加测试用例

### Phase 3: 实现特殊方法 (中优先级) 🟡

**估计时间**: 3-4小时

1. **GetPageAsync** - 双查询实现
2. **UpsertAsync** - 数据库特定语法
3. **BatchExistsAsync** - 多布尔值返回
4. **BatchUpdateAsync** - CASE WHEN批量更新

### Phase 4: 完整测试覆盖 (测试优先级) 🟢

**估计时间**: 4-6小时

1. **编写60+测试用例**
   - 每个方法至少2-3个测试
   - 正常情况 + 边界情况 + 异常情况

2. **record vs class 对比测试**
   - 验证两种类型都正确工作

3. **struct返回值测试**
   - 验证结构体返回值正确

4. **数据库方言测试**
   - SQLite, MySQL, PostgreSQL, SQL Server, Oracle

### Phase 5: 验证和优化 (最终阶段) ✅

**估计时间**: 1-2小时

1. **运行所有测试**
   - 目标: 100%通过率
   - 当前: 1412通过 → 目标: 1472+通过

2. **性能测试**
   - 批量操作性能
   - 大数据集测试

3. **文档更新**
   - 更新README
   - 更新API文档
   - 添加使用示例

## 📈 成功指标

| 指标 | 当前 | 目标 | 状态 |
|------|------|------|------|
| 测试通过数 | 1412 | 1472+ | ⏳ 进行中 |
| 测试失败数 | 0 | 0 | ✅ 达成 |
| 编译错误 | 197 | 0 | 🔴 需要修复 |
| 方法覆盖率 | ~40% | 100% | ⏳ 进行中 |
| record支持 | ❌ | ✅ | 🔴 待实现 |
| struct支持 | ❌ | ✅ | 🔴 待实现 |
| 标量返回值 | ❌ | ✅ | 🔴 待实现 |

## 🔧 技术债务

1. **源生成器不支持record类型** - Phase 1修复
2. **源生成器不支持struct返回值** - Phase 1修复
3. **缺少标量类型返回值支持** - Phase 2修复
4. **部分方法需要手动实现** - Phase 3实现
5. **测试覆盖率不足** - Phase 4补充

## 📝 下一步行动

### 立即执行 (今天)
1. ✅ 修复 `ObjectMap.cs` - 过滤 EqualityContract
2. ✅ 修复 `CodeGenerationService.cs` - 支持struct
3. ✅ 编译测试 - 验证197个错误消失
4. ✅ 运行现有测试 - 确保1412个测试仍然通过

### 短期计划 (本周)
5. ✅ 恢复GetDistinctValuesAsync
6. ✅ 实现标量类型支持
7. ✅ 实现GetPageAsync双查询
8. ✅ 编写50%测试用例

### 中期计划 (下周)
9. ✅ 实现所有特殊方法
10. ✅ 完成100%测试覆盖
11. ✅ 性能优化
12. ✅ 文档更新

## 💡 实现建议

### 源生成器修复示例

**修复record类型** (`ObjectMap.cs`):
```csharp
Properties = ElementSymbol is INamedTypeSymbol namedTypeSymbol
    ? namedTypeSymbol.GetMembers().OfType<IPropertySymbol>()
        .Where(p => p.CanBeReferencedByName && 
                    p.Name != "EqualityContract" &&
                    !p.IsStatic &&
                    !p.IsIndexer)
        .ToList()
    : new List<IPropertySymbol>();
```

**支持struct返回值** (`CodeGenerationService.cs`):
```csharp
if (entityType.TypeKind == TypeKind.Struct)
{
    // Generate struct initialization
    sb.AppendLine($"var __entity__ = new {entityType.ToDisplayString()}");
    sb.AppendLine("{");
    // ... property assignments
    sb.AppendLine("};");
}
```

**支持标量返回值**:
```csharp
if (IsScalarType(returnType))
{
    // Simple scalar value reading
    sb.AppendLine($"var value = reader.GetString(0);");
    sb.AppendLine("__result__.Add(value);");
}
```

## 📊 预期结果

完成所有Phase后：
- ✅ 0个编译错误
- ✅ 1472+个测试全部通过
- ✅ 100%方法覆盖
- ✅ record, class, struct全部支持
- ✅ 标量返回值支持
- ✅ 所有预定义接口完全可用

---

**文档创建时间**: 2025-10-29  
**状态**: Phase 1 待开始  
**下一个里程碑**: 修复197个编译错误

