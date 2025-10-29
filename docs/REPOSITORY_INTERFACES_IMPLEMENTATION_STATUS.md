# Repository 接口实现状态

> **状态**: 🚧 开发中  
> **版本**: v0.5.0-preview  
> **日期**: 2025-10-29

---

## ✅ 已完成

### 1. 接口定义文件

已创建 5 个核心接口文件和 1 个辅助类：

```
✅ src/Sqlx/IQueryRepository.cs (14 methods)
✅ src/Sqlx/ICommandRepository.cs (10 methods)
✅ src/Sqlx/IBatchRepository.cs (6 methods)
✅ src/Sqlx/IAggregateRepository.cs (11 methods)
✅ src/Sqlx/IAdvancedRepository.cs (9 methods)
✅ src/Sqlx/PagedResult.cs (辅助类)
✅ src/Sqlx/ICrudRepository.cs (已更新为组合接口)
```

### 2. 接口组合

```csharp
// 完整接口 (50+ 方法)
IRepository<TEntity, TKey>

// 标准CRUD (24+ 方法)
ICrudRepository<TEntity, TKey>

// 只读 (25 方法)
IReadOnlyRepository<TEntity, TKey>

// 批量操作 (20 方法)
IBulkRepository<TEntity, TKey>

// 只写/CQRS (16 方法)
IWriteOnlyRepository<TEntity, TKey>
```

---

## 🚧 待修复问题

### 编译错误

1. **ExpressionToSql 特性使用错误**
   - 问题：特性只能用在参数上，不能用在方法上
   - 需要修改为参数特性或移除，由源生成器推断

2. **XML 注释格式错误**
   - `&` 需要转义为 `&amp;`
   - `<` 需要转义为 `&lt;`
   - `>` 需要转义为 `&gt;`

3. **方法重复定义警告**
   - ICrudRepository 继承了接口后，旧的方法定义重复
   - 需要移除或标记为 `[Obsolete]`

---

## 📋 待实现功能

### Phase 1: 修复编译错误
- [ ] 修复 ExpressionToSql 特性用法
- [ ] 修复 XML 注释格式
- [ ] 移除重复的方法定义
- [ ] 确保向后兼容

### Phase 2: 源生成器支持
- [ ] 实现 GetPageAsync 的源生成逻辑
- [ ] 实现 GetWhereAsync 的表达式转SQL
- [ ] 实现 UpdatePartialAsync 的动态SET子句
- [ ] 实现 UpsertAsync 的数据库特定语法
- [ ] 实现批量操作的智能分批

### Phase 3: 高级功能
- [ ] BulkCopyAsync 的数据库特定实现
- [ ] 表DDL生成
- [ ] 软删除自动过滤
- [ ] 审计字段自动填充

### Phase 4: 测试和文档
- [ ] 单元测试 (每个方法)
- [ ] 集成测试 (多数据库)
- [ ] 性能基准测试
- [ ] 完整文档和示例

---

## 🎯 下一步计划

### 1. 简化接口（短期）

先不使用表达式树，使用简单的SQL模板：

```csharp
// 简化版 - 使用字符串参数而不是表达式
Task<List<TEntity>> GetWhereAsync(string whereClause, object? parameters = null);
Task<int> UpdateWhereAsync(string whereClause, object updates, object? parameters = null);
```

### 2. 渐进增强（中期）

逐步添加表达式树支持：

```csharp
// v0.5.1 - 简单表达式
Task<List<TEntity>> GetWhereAsync(Expression<Func<TEntity, bool>> predicate);

// v0.5.2 - 复杂表达式
Task<List<TEntity>> GetWhereAsync(
    Expression<Func<TEntity, bool>> predicate,
    string? orderBy = null,
    int? limit = null
);
```

### 3. 完整实现（长期）

所有高级功能完整实现。

---

## 📊 接口方法统计

| 接口 | 方法数 | 状态 |
|------|--------|------|
| IQueryRepository | 14 | ✅ 定义完成 |
| ICommandRepository | 10 | ✅ 定义完成 |
| IBatchRepository | 6 | ✅ 定义完成 |
| IAggregateRepository | 11 | ✅ 定义完成 |
| IAdvancedRepository | 9 | ✅ 定义完成 |
| **总计** | **50** | 🚧 需要修复 |

---

## 🔧 快速修复方案

### 方案 A: 标记为预览功能

```csharp
#if PREVIEW_FEATURES
// 新接口代码
#endif
```

### 方案 B: 创建独立文件

不修改现有的 ICrudRepository，创建新的：

```
src/Sqlx/Preview/
├── IQueryRepositoryV2.cs
├── ICommandRepositoryV2.cs
└── ...
```

### 方案 C: 文档先行（推荐）

1. 保留设计文档
2. 修复编译错误
3. 逐步实现
4. 充分测试后合并

---

## 💡 建议

### 优先级

**P0 - 修复编译**
1. 移除或修复 ExpressionToSql 特性
2. 修复 XML 注释
3. 解决方法重复问题

**P1 - 基础实现**
1. GetPageAsync
2. GetByIdsAsync
3. InsertAndGetIdAsync
4. UpdatePartialAsync

**P2 - 批量操作**
1. BatchInsertAndGetIdsAsync
2. BatchUpdateAsync
3. BatchDeleteAsync

**P3 - 高级功能**
1. UpsertAsync
2. BulkCopyAsync
3. 表DDL操作

---

## 📝 开发日志

### 2025-10-29
- ✅ 创建了 5 个核心接口文件
- ✅ 创建了 PagedResult 辅助类
- ✅ 更新了 ICrudRepository 为组合接口
- ✅ 创建了设计文档 (ENHANCED_REPOSITORY_INTERFACES.md)
- 🚧 发现编译错误，需要修复

---

## 🚀 路线图

### v0.5.0 (当前)
- 接口定义
- 设计文档
- 基础实现

### v0.5.1 (下一步)
- 修复编译错误
- 实现 P1 功能
- 基本测试

### v0.5.2
- 实现 P2 功能
- 性能测试
- 完善文档

### v0.6.0
- 实现 P3 功能
- 完整测试覆盖
- 生产就绪

---

**当前重点**: 修复编译错误，使项目可以正常构建。


