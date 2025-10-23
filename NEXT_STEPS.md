# 🚀 动态占位符功能 - 后续工作计划

**当前状态**: 阶段 1（文档和示例）100% 完成 ✅  
**总进度**: 33% (1/3 阶段完成)  
**日期**: 2024-10-23

---

## 📊 当前完成情况

### ✅ 已完成（阶段 1）

1. **核心 API** (100%)
   - ✅ `[DynamicSql]` 特性 + `DynamicSqlType` 枚举
   - ✅ `SqlValidator` 验证器（零 GC、高性能）
   - ✅ `SqlTemplateEngine` 支持 `{{@paramName}}` 解析
   - ✅ `SqlTemplateResult.HasDynamicFeatures` 标记

2. **完整文档** (100%)
   - ✅ README.md 更新（速查表 + 核心特性）
   - ✅ PLACEHOLDERS.md 完整指南（202 行）
   - ✅ API_REFERENCE.md API 参考（208 行）
   - ✅ GitHub Pages 展示（158 行新增）
   - ✅ 3 个设计文档（2353 行）

3. **实际示例** (100%)
   - ✅ TodoWebApi 完整示例（445 行）
   - ✅ 16 个可运行代码示例
   - ✅ 单元测试示例

4. **Git 提交** (100%)
   - ✅ 22 次规范提交
   - ✅ 清晰的提交消息
   - ✅ 完整的变更历史

---

## ⏭️ 待完成工作（阶段 2 & 3）

### 阶段 2: 功能完善（预计 15-20 小时）⏸️

#### 1. CodeGenerationService 更新 (高优先级)

**工作量**: 2-3 小时  
**文件**: `src/Sqlx.Generator/Services/CodeGenerationService.cs`

**任务**:
- [ ] 检测方法参数上的 `[DynamicSql]` 特性
- [ ] 检测 SQL 模板中的 `{{@paramName}}` 占位符
- [ ] 验证占位符和特性是否匹配（否则编译错误）
- [ ] 生成内联验证代码：
  ```csharp
  // 生成示例：
  if (!Sqlx.Validation.SqlValidator.IsValidIdentifier(tableName.AsSpan()))
      throw new ArgumentException($"Invalid table name: {tableName}", nameof(tableName));
  ```
- [ ] 根据 `DynamicSqlType` 生成不同的验证调用
- [ ] 将动态参数插入到 SQL 字符串构建中

**技术要点**:
- 使用 Roslyn Semantic Model 获取特性信息
- 使用 `SqlTemplateResult.HasDynamicFeatures` 判断是否需要验证
- 生成内联代码，避免运行时反射

---

#### 2. DynamicSqlAnalyzer 实现 (高优先级)

**工作量**: 6-8 小时  
**文件**: `src/Sqlx.Generator/Analyzers/DynamicSqlAnalyzer.cs`

**任务**:
- [ ] 实现 10 个诊断规则（见下表）
- [ ] 编译时检测不安全使用
- [ ] 提供清晰的错误消息和建议

**诊断规则清单**:

| 规则ID | 严重级别 | 说明 | 工作量 |
|-------|---------|------|--------|
| SQLX2001 | Error | 使用 `{{@}}` 但参数未标记 `[DynamicSql]` | 1h |
| SQLX2002 | Warning | 动态参数来自不安全来源（用户输入） | 1h |
| SQLX2003 | Warning | 调用前缺少验证逻辑 | 1h |
| SQLX2004 | Info | 建议使用白名单验证 | 0.5h |
| SQLX2005 | Warning | 在公共 API 中暴露动态参数 | 0.5h |
| SQLX2006 | Error | 动态参数类型不是 string | 0.5h |
| SQLX2007 | Warning | SQL 模板包含危险操作 | 1h |
| SQLX2008 | Info | 建议添加单元测试 | 0.5h |
| SQLX2009 | Warning | 缺少长度限制检查 | 0.5h |
| SQLX2010 | Error | `[DynamicSql]` 特性使用错误 | 0.5h |

**技术要点**:
- 继承 `DiagnosticAnalyzer`
- 注册 `SyntaxNodeAction` 和 `SymbolAction`
- 使用 Semantic Model 分析数据流
- 提供详细的 diagnostic location

---

#### 3. DynamicSqlCodeFixProvider 实现 (中优先级)

**工作量**: 3-4 小时  
**文件**: `src/Sqlx.Generator/Analyzers/DynamicSqlCodeFixProvider.cs`

**任务**:
- [ ] 自动添加 `[DynamicSql]` 特性（针对 SQLX2001）
- [ ] 自动添加验证代码模板（针对 SQLX2003）
- [ ] 自动添加长度检查（针对 SQLX2009）

**技术要点**:
- 继承 `CodeFixProvider`
- 使用 `Document.WithSyntaxRoot()` 修改代码
- 提供"Fix All"支持

---

### 阶段 3: 测试（预计 8-10 小时）⏸️

#### 4. 核心功能单元测试 (高优先级)

**工作量**: 3-4 小时  
**文件**: `tests/Sqlx.Tests/DynamicPlaceholder/`

**任务**:
- [ ] `DynamicSqlAttributeTests.cs` - 特性测试（5 个测试）
- [ ] `SqlValidatorTests.cs` - 验证器测试（20 个测试）
  - IsValidIdentifier（5 个测试）
  - ContainsDangerousKeyword（5 个测试）
  - IsValidFragment（5 个测试）
  - IsValidTablePart（5 个测试）
- [ ] `SqlTemplateEngineTests.cs` - 模板解析测试（8 个测试）
  - 动态占位符解析
  - HasDynamicFeatures 标记
  - 混合占位符
- [ ] `CodeGenerationServiceTests.cs` - 代码生成测试（10 个测试）
  - 验证代码生成
  - 错误检测

**覆盖率目标**: 100%

---

#### 5. 分析器单元测试 (高优先级)

**工作量**: 4-5 小时  
**文件**: `tests/Sqlx.Tests/Analyzers/`

**任务**:
- [ ] `DynamicSqlAnalyzerTests.cs` - 分析器测试（30 个测试）
  - 每个规则 3 个测试（正向、负向、边界）
- [ ] `DynamicSqlCodeFixProviderTests.cs` - Code Fix 测试（10 个测试）

**技术要点**:
- 使用 `Microsoft.CodeAnalysis.Testing`
- 验证诊断位置和消息
- 验证 Code Fix 结果

---

#### 6. Benchmark 性能测试 (中优先级)

**工作量**: 2 小时  
**文件**: `tests/Sqlx.Benchmarks/DynamicPlaceholderBenchmark.cs`

**任务**:
- [ ] 验证开销测试
  - IsValidIdentifier
  - IsValidFragment
  - IsValidTablePart
- [ ] GC 压力测试
- [ ] 与普通查询对比

**性能目标**:
- 验证延迟 < 1μs
- 零 GC 分配
- 对查询性能影响 < 5%

---

## 📋 实施顺序建议

### 方案 A: 完整实施（推荐）

**顺序**: 1 → 2 → 3 → 4 → 5 → 6  
**时间**: 23-30 小时  
**优点**: 
- 功能完整
- 测试覆盖全面
- 可立即发布

**缺点**:
- 时间较长
- 需要较多测试工作

---

### 方案 B: 最小可用版本（快速）

**顺序**: 1 → 4 → 6  
**时间**: 7-9 小时  
**优点**:
- 快速可用
- 核心功能完整
- 有基本测试

**缺点**:
- 缺少编译时检查（分析器）
- 需要用户手动验证安全性

---

### 方案 C: 分步发布（平衡）

**阶段 2.1**: 1 + 4（5-7 小时）
- 代码生成 + 核心测试
- 发布 v0.1（内部测试）

**阶段 2.2**: 2 + 5（10-13 小时）
- 分析器 + 分析器测试
- 发布 v0.2（公开 Beta）

**阶段 2.3**: 3 + 6（5-6 小时）
- Code Fix + Benchmark
- 发布 v1.0（正式版）

---

## 🎯 推荐方案

**建议采用方案 C（分步发布）**，原因：

1. **快速反馈**
   - 2.1 阶段（5-7 小时）可快速验证功能
   - 获取用户反馈调整后续开发

2. **降低风险**
   - 分步测试，及时发现问题
   - 避免大规模返工

3. **持续交付**
   - 每个阶段都有可用版本
   - 用户可以逐步升级

---

## 📝 决策点

### 现在需要决定：

**Q1**: 选择哪个实施方案？
- [ ] 方案 A - 完整实施（23-30h）
- [ ] 方案 B - 最小可用（7-9h）
- [ ] 方案 C - 分步发布（20-26h，分 3 次）

**Q2**: 是否需要全部 10 个分析器规则？
- [ ] 是 - 完整的安全检查
- [ ] 否 - 只实现最重要的 4-5 个（节省 3-4h）

**Q3**: Code Fix Provider 优先级？
- [ ] 高 - 提升用户体验
- [ ] 中 - 可以手动修复
- [ ] 低 - 暂时跳过（节省 3-4h）

---

## 📊 资源需求

### 开发资源

| 阶段 | 预计时间 | 人员 | 说明 |
|------|---------|------|------|
| 阶段 2.1 | 5-7h | 1 人 | 代码生成 + 测试 |
| 阶段 2.2 | 10-13h | 1 人 | 分析器 + 测试 |
| 阶段 2.3 | 5-6h | 1 人 | Code Fix + Benchmark |
| **总计** | **20-26h** | **1 人** | **分步实施** |

### 测试资源

| 类型 | 测试数量 | 覆盖率目标 |
|------|---------|-----------|
| 单元测试 | 83 个 | 100% |
| 分析器测试 | 40 个 | 100% |
| Benchmark | 6 个 | - |
| **总计** | **129 个** | **100%** |

---

## 💡 关键风险

### 技术风险

1. **Roslyn API 复杂性** (中等)
   - 缓解：使用现有 `PropertyOrderAnalyzer` 作为参考
   - 缓解：逐步实施，先简单规则后复杂规则

2. **代码生成正确性** (中等)
   - 缓解：充分的单元测试
   - 缓解：生成代码包含断言和验证

3. **性能影响** (低)
   - 缓解：已有零 GC 设计
   - 缓解：Benchmark 验证

### 时间风险

1. **分析器实施时间** (中等)
   - 10 个规则可能比预期复杂
   - 缓解：先实施最重要的 5 个

2. **测试编写时间** (低)
   - 129 个测试需要时间
   - 缓解：使用测试生成工具

---

## 📈 成功指标

### 功能指标

- [ ] CodeGenerationService 生成正确的验证代码
- [ ] 10 个分析器规则全部工作
- [ ] Code Fix Provider 自动修复成功率 > 95%

### 质量指标

- [ ] 单元测试覆盖率 = 100%
- [ ] 所有测试通过
- [ ] 零编译警告

### 性能指标

- [ ] 验证延迟 < 1μs
- [ ] 零 GC 分配
- [ ] 对查询性能影响 < 5%

---

## 🎉 结论

**阶段 1 已完成**，为动态占位符功能建立了坚实的基础：
- ✅ 核心 API
- ✅ 完整文档
- ✅ 实际示例

**建议下一步**：
1. 采用**方案 C（分步发布）**
2. 先完成**阶段 2.1**（5-7 小时）：代码生成 + 核心测试
3. 快速验证功能，获取反馈
4. 根据反馈调整后续开发

**等待您的决定！** 🚀

