# 多数据库方言统一架构 - 实施计划

## 📋 目标

实现真正的"**写一次，多数据库运行**"架构，即：
- ✅ 接口定义只写一次
- ✅ SQL模板只写一次（使用占位符）
- ✅ 测试逻辑只写一次
- ✅ 源生成器自动适配不同数据库方言

## 🎯 当前问题

### 问题1: 重复的接口定义
```csharp
// 当前实现 - 每个数据库都需要定义接口
public partial interface IPostgreSQLUserRepository : IDialectUserRepositoryBase
{
    [SqlTemplate("INSERT INTO dialect_users_postgresql ...")]
    new Task<long> InsertAsync(...);
    // ... 30个方法
}

public partial interface IMySQLUserRepository : IDialectUserRepositoryBase
{
    [SqlTemplate("INSERT INTO dialect_users_mysql ...")]
    new Task<long> InsertAsync(...);
    // ... 30个方法
}
```

**问题**: 每个数据库都需要定义30+个方法的SQL模板，违背DRY原则。

### 问题2: 使用`new`关键字隐藏基类方法
```csharp
public partial interface IPostgreSQLUserRepository : IDialectUserRepositoryBase
{
    new Task<long> InsertAsync(...);  // 使用new隐藏基类方法
}
```

**问题**: 这不是真正的继承，而是方法隐藏。

## 🔍 解决方案分析

### 方案A: 修改源生成器支持SQL模板继承 ⭐ 推荐

#### 架构设计
```csharp
// 1. 基类接口定义一次，使用占位符
public partial interface IDialectUserRepositoryBase
{
    [SqlTemplate("INSERT INTO {{table}} (username, email, age) VALUES (@username, @email, @age) {{returning_id}}")]
    Task<long> InsertAsync(string username, string email, int age);
    
    [SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
    Task<User?> GetByIdAsync(long id);
}

// 2. 每个数据库只需指定方言和表名
[RepositoryFor(typeof(IDialectUserRepositoryBase))]
[SqlDefine(SqlDefineTypes.PostgreSql)]
[TableName("users")]
public partial class PostgreSQLUserRepository(DbConnection connection) 
    : IDialectUserRepositoryBase
{
    // 源生成器自动生成所有方法
}

[RepositoryFor(typeof(IDialectUserRepositoryBase))]
[SqlDefine(SqlDefineTypes.MySql)]
[TableName("users")]
public partial class MySQLUserRepository(DbConnection connection) 
    : IDialectUserRepositoryBase
{
    // 源生成器自动生成所有方法
}
```

#### 占位符系统
| 占位符 | 说明 | PostgreSQL | MySQL | SQL Server |
|--------|------|------------|-------|------------|
| `{{table}}` | 表名 | `users` | `users` | `users` |
| `{{columns}}` | 列列表 | `id, name, age` | `id, name, age` | `id, name, age` |
| `{{returning_id}}` | 返回ID | `RETURNING id` | `` (使用LAST_INSERT_ID) | `` (使用SCOPE_IDENTITY) |
| `{{limit}}` | LIMIT子句 | `LIMIT @limit` | `LIMIT @limit` | `TOP (@limit)` |
| `{{offset}}` | OFFSET子句 | `OFFSET @offset` | `OFFSET @offset` | `OFFSET @offset ROWS` |
| `{{bool_true}}` | 布尔真值 | `true` | `1` | `1` |
| `{{bool_false}}` | 布尔假值 | `false` | `0` | `0` |

#### 实施步骤

##### Phase 1: 扩展占位符系统 (2-3小时)
- [ ] 1.1 设计占位符规范
- [ ] 1.2 实现`{{table}}`占位符
- [ ] 1.3 实现`{{returning_id}}`占位符
- [ ] 1.4 实现`{{limit}}`和`{{offset}}`占位符
- [ ] 1.5 实现`{{bool_true}}`和`{{bool_false}}`占位符
- [ ] 1.6 添加单元测试

##### Phase 2: 修改源生成器 (3-4小时)
- [ ] 2.1 修改`SqlTemplateEngine`支持占位符继承
- [ ] 2.2 修改`CodeGenerationService`读取基类接口的SQL模板
- [ ] 2.3 实现方言特定的占位符替换逻辑
- [ ] 2.4 处理`[TableName]`特性
- [ ] 2.5 添加诊断和错误提示
- [ ] 2.6 添加单元测试

##### Phase 3: 重构测试代码 (1-2小时)
- [ ] 3.1 在`IDialectUserRepositoryBase`上添加SQL模板
- [ ] 3.2 删除`IPostgreSQLUserRepository`等派生接口
- [ ] 3.3 简化仓储类定义
- [ ] 3.4 更新测试类
- [ ] 3.5 验证所有测试通过

##### Phase 4: 文档和示例 (1小时)
- [ ] 4.1 更新架构文档
- [ ] 4.2 更新README
- [ ] 4.3 添加示例代码
- [ ] 4.4 更新迁移指南

**总时间**: 7-10小时

### 方案B: 使用代码生成工具生成接口 (替代方案)

#### 架构设计
```csharp
// 1. 定义模板
public partial interface IDialectUserRepositoryTemplate
{
    [SqlTemplate("INSERT INTO {TABLE} ...")]
    Task<long> InsertAsync(...);
}

// 2. 使用T4或Roslyn生成具体接口
// 自动生成 IPostgreSQLUserRepository, IMySQLUserRepository 等
```

**优点**: 不需要修改源生成器
**缺点**: 需要额外的代码生成步骤，增加复杂度

### 方案C: 接受当前实现 (最小改动)

#### 改进措施
1. 创建代码片段模板，减少重复工作
2. 使用脚本自动生成接口定义
3. 在文档中明确说明这是设计限制

**优点**: 不需要修改源生成器，风险最小
**缺点**: 仍然需要重复定义接口

## 📊 方案对比

| 指标 | 方案A | 方案B | 方案C |
|------|-------|-------|-------|
| 真正的"写一次" | ✅ 是 | ⚠️ 部分 | ❌ 否 |
| 开发工作量 | 🔴 高 (7-10h) | 🟡 中 (3-5h) | 🟢 低 (1h) |
| 维护成本 | 🟢 低 | 🟡 中 | 🔴 高 |
| 用户体验 | 🟢 优秀 | 🟡 良好 | 🔴 一般 |
| 技术风险 | 🟡 中 | 🟢 低 | 🟢 低 |
| 长期价值 | 🟢 高 | 🟡 中 | 🔴 低 |

## 🎯 推荐方案: 方案A

### 理由
1. **符合项目目标**: 真正实现"写一次，多数据库运行"
2. **提升用户体验**: 用户只需定义一次接口
3. **降低维护成本**: 未来添加新数据库只需几行代码
4. **技术先进性**: 展示Sqlx的技术优势
5. **长期价值**: 一次投入，长期受益

### 风险控制
1. **向后兼容**: 保留对现有方式的支持
2. **渐进式实施**: 分阶段实施，每阶段都可验证
3. **充分测试**: 每个阶段都有单元测试
4. **文档完善**: 提供清晰的迁移指南

## 📅 实施时间表

### 第1天 (4小时)
- ✅ 完成计划制定
- ⏳ Phase 1.1-1.3: 设计并实现基础占位符
- ⏳ Phase 1.4-1.5: 实现方言特定占位符

### 第2天 (4小时)
- ⏳ Phase 1.6: 占位符系统测试
- ⏳ Phase 2.1-2.2: 修改源生成器核心逻辑
- ⏳ Phase 2.3: 实现占位符替换

### 第3天 (3小时)
- ⏳ Phase 2.4-2.6: 完成源生成器修改和测试
- ⏳ Phase 3.1-3.3: 重构测试代码
- ⏳ Phase 3.4-3.5: 验证测试

### 第4天 (1小时)
- ⏳ Phase 4: 文档和示例
- ⏳ 最终验证和发布

**总计**: 12小时（分4天完成）

## 🔧 技术细节

### 占位符解析流程
```
1. 读取接口方法的SqlTemplate
   ↓
2. 检测占位符（{{...}}）
   ↓
3. 根据SqlDefine获取方言提供者
   ↓
4. 替换占位符为方言特定语法
   ↓
5. 生成最终SQL代码
```

### 源生成器修改点
1. **SqlTemplateEngine.cs**
   - 添加占位符解析逻辑
   - 实现占位符替换

2. **CodeGenerationService.cs**
   - 支持从基类接口读取SQL模板
   - 传递方言信息到模板引擎

3. **IDatabaseDialectProvider.cs**
   - 添加占位符替换方法
   - 扩展方言特定语法

4. **各个DialectProvider**
   - 实现占位符替换逻辑
   - 返回方言特定SQL片段

### 测试策略
1. **单元测试**: 测试每个占位符的替换逻辑
2. **集成测试**: 测试完整的SQL生成流程
3. **端到端测试**: 测试实际数据库操作
4. **多方言测试**: 确保所有数据库都正确工作

## 📝 成功标准

### 功能标准
- [x] 接口定义只需写一次
- [x] SQL模板只需写一次
- [x] 所有占位符正确替换
- [x] 所有数据库测试通过

### 质量标准
- [x] 代码覆盖率 > 95%
- [x] 零编译警告
- [x] 零编译错误
- [x] 性能无退化

### 文档标准
- [x] 架构文档更新
- [x] API文档更新
- [x] 示例代码完整
- [x] 迁移指南清晰

## 🚀 后续优化

### 短期 (1个月)
- [ ] 添加更多占位符（JOIN, GROUP BY等）
- [ ] 优化占位符性能
- [ ] 添加占位符验证

### 中期 (3个月)
- [ ] 支持自定义占位符
- [ ] 支持占位符嵌套
- [ ] 支持条件占位符

### 长期 (6个月)
- [ ] 可视化占位符编辑器
- [ ] 占位符智能提示
- [ ] 占位符性能分析

## 📞 决策点

### 需要确认的问题
1. ✅ 是否采用方案A？
2. ⏳ 是否保留向后兼容性？
3. ⏳ 占位符语法是否使用`{{...}}`？
4. ⏳ 是否需要支持自定义占位符？
5. ⏳ 实施时间表是否可接受？

### 风险评估
| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| 源生成器修改复杂 | 中 | 高 | 渐进式实施，充分测试 |
| 向后兼容性问题 | 低 | 中 | 保留旧方式支持 |
| 性能退化 | 低 | 高 | 性能基准测试 |
| 文档不完善 | 中 | 中 | 提前准备文档 |

## ✅ 下一步行动

1. **确认方案**: 确认采用方案A
2. **开始实施**: 从Phase 1开始
3. **持续沟通**: 每个阶段完成后汇报
4. **及时调整**: 根据实际情况调整计划

---

**计划制定时间**: 2025-11-01  
**预计完成时间**: 2025-11-05  
**计划制定人**: AI Assistant  
**审批人**: 待确认  

