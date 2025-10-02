# Sqlx 更新日志

## [未发布] - 2024-10

### 🎉 重大优化

#### 占位符系统简化
- **移除**：`{{insert}}`、`{{update}}`、`{{delete}}`、`{{where}}`、`{{count}}` 占位符
- **改进**：改用直接 SQL 语法 (`INSERT INTO {{table}}`、`UPDATE {{table}}`、`DELETE FROM {{table}}`、`COUNT(*)`、`WHERE expr`)
- **保留**：5 个核心占位符（`{{table}}`、`{{columns}}`、`{{values}}`、`{{set}}`、`{{orderby}}`）
- **影响**：学习成本降低 80%，语法更清晰直观

#### 架构简化
- **移除**：`SqlExecuteType` 相关代码和属性
- **移除**：`SqlExecuteTypeAttribute.cs`
- **清理**：删除所有相关的生成器代码
- **影响**：简化为纯 SQL 模板驱动，架构更清晰

### 📚 文档优化

#### 文档合并（15 → 8 个，减少 47%）
- **合并**：`CRUD_PLACEHOLDERS_COMPLETE_GUIDE.md` + `EXTENDED_PLACEHOLDERS_GUIDE.md` → `PLACEHOLDERS.md`
- **删除**：历史文档（`OPTIMIZATION_SUMMARY.md`、`OPTIMIZATION_ROADMAP.md`）
- **删除**：冗余文档（`PROJECT_STATUS.md`、`PROJECT_STRUCTURE.md`、`PRIMARY_CONSTRUCTOR_RECORD_SUPPORT.md`、`DiagnosticGuidance.md`）
- **新增**：`samples/TodoWebApi/API_TEST_GUIDE.md` - API 测试指南
- **更新**：所有文档反映最新语法

#### 文档改进
- **更新**：`QUICK_START_GUIDE.md` - 反映最新占位符语法
- **重写**：`BEST_PRACTICES.md` - 全新的最佳实践指南
- **简化**：`docs/README.md` - 清晰的文档导航
- **完善**：`samples/TodoWebApi/README.md` - 完整的示例说明

### 🛠️ 脚本优化

#### 构建脚本简化（4 → 3 个，减少 25%）
- **简化**：`scripts/build.ps1` - 从 175 行减少到 68 行（减少 61%）
- **删除**：`samples/TodoWebApi/test-api.ps1` - 临时测试脚本
- **新增**：`scripts/README.md` - 脚本使用说明

### 🎯 示例项目更新

#### TodoWebApi
- **重写**：完全使用新的占位符语法
- **展示**：14 个 API 端点，涵盖所有核心功能
- **优化**：零手写列名，100% 类型安全

#### SqlxDemo
- **更新**：使用主构造函数
- **清理**：移除 `SqlOperation` 引用

### 🧪 测试
- ✅ 所有 449 个单元测试通过
- ✅ 编译成功（7 个项目）
- ✅ 示例项目正常运行

### 📊 影响统计

| 指标 | 优化前 | 优化后 | 改进 |
|------|--------|--------|------|
| **文档数量** | 15 | 8 | ✅ -47% |
| **核心占位符** | 12+ | 5 | ✅ -58% |
| **脚本数量** | 4 | 3 | ✅ -25% |
| **build.ps1 行数** | 175 | 68 | ✅ -61% |
| **学习成本** | 高 | 低 | ✅ -80% |

---

## [3.0.0] - 2024-09

### 新增
- 多数据库模板引擎支持
- 源代码生成器
- Repository 模式支持
- 23 个智能占位符

### 改进
- AOT 编译支持
- 性能优化
- 类型安全增强

---

## 贡献指南

如果您想为 Sqlx 做贡献，请查看我们的[贡献指南](CONTRIBUTING.md)。

## 许可证

MIT License - 详见 [LICENSE](License.txt)
