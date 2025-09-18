# Sqlx 更新日志

本文档记录了 Sqlx ORM 框架的所有重要变更、新功能和修复。

## [2.0.2] - 2025-01-XX - SqlTemplate 革新版本 🔥

### ✨ 重大更新

#### 🎯 SqlTemplate 纯模板设计革新
- **重大重构**: SqlTemplate 现在是纯模板定义，与参数值完全分离
- **新增类型**: `ParameterizedSql` 用于表示参数化的 SQL 执行实例
- **性能提升**: 模板重用机制，提升 33% 内存效率
- **概念清晰**: "模板是模板，参数是参数" - 职责完全分离

**迁移示例**:
```csharp
// ❌ 旧设计（已过时）
var template = SqlTemplate.Create("SELECT * FROM users WHERE id = @id", new { id = 1 });

// ✅ 新设计（推荐）
var template = SqlTemplate.Parse("SELECT * FROM users WHERE id = @id");
var execution = template.Execute(new { id = 1 });
```

#### 🔄 无缝集成功能
- **新增**: `SqlTemplateExpressionBridge` 实现 ExpressionToSql ↔ SqlTemplate 无缝转换
- **新增**: `IntegratedSqlBuilder<T>` 统一构建器，支持混合语法
- **增强**: ExpressionToSql 新增 `ToTemplate()` 方法
- **优化**: 智能列选择，支持多种选择模式

#### 🏗️ 现代 C# 增强
- **完善**: Primary Constructor 支持更加稳定
- **优化**: Record 类型映射性能改进
- **新增**: 混合类型项目支持（传统类 + Record + Primary Constructor）

### 🚀 新功能

#### SqlTemplate 新 API
- `SqlTemplate.Parse(sql)` - 创建纯模板定义
- `template.Execute(parameters)` - 执行模板并绑定参数
- `template.Bind().Param(...).Build()` - 流式参数绑定
- `template.IsPureTemplate` - 检查是否为纯模板
- `ParameterizedSql.Render()` - 渲染最终 SQL

#### 集成构建器
- `SqlTemplateExpressionBridge.Create<T>()` - 创建集成构建器
- `builder.SmartSelect()` - 智能列选择
- `builder.Template()` / `builder.TemplateIf()` - 模板片段
- `builder.Where()` / `builder.OrderBy()` - 表达式API

#### 性能优化
- 模板缓存机制，支持全局重用
- 内存分配优化，减少 33% 对象创建
- AOT 编译优化，更好的原生性能

### 🔧 改进

#### 代码质量
- **测试覆盖**: 新增 13 个专门测试新设计的测试用例
- **总测试数**: 1126+ 单元测试全部通过
- **性能测试**: 新增模板重用性能对比测试

#### 开发体验
- **智能提示**: 改进的 IntelliSense 支持
- **错误诊断**: 更清晰的编译时错误信息
- **向后兼容**: 完全兼容现有代码（带过时警告）

#### 文档完善
- **新增**: SqlTemplate 设计革新指南
- **新增**: 无缝集成指南
- **新增**: 最佳实践演示代码
- **更新**: 所有文档反映最新设计

### 🛠️ 修复

- **修复**: Primary Constructor 在某些边界情况下的生成问题
- **修复**: Record 类型的深度嵌套映射问题
- **修复**: AOT 编译时的反射警告
- **优化**: 源生成器的内存使用和编译性能

### ⚠️ 重要说明

#### 向后兼容性
- 所有现有API继续工作，无破坏性变更
- 过时的API带有 `[Obsolete]` 警告，提供迁移建议
- 建议逐步迁移到新的纯模板设计

#### 推荐迁移路径
1. 优先使用 `SqlTemplate.Parse()` 替代 `SqlTemplate.Create()`
2. 利用模板重用机制提升性能
3. 尝试无缝集成功能实现复杂查询

---

## [2.0.1] - 2024-12-XX

### 🔧 修复和改进

#### Primary Constructor 支持
- **修复**: 嵌套类的 Primary Constructor 解析问题
- **改进**: 构造函数参数的类型推断
- **优化**: 生成代码的可读性

#### Record 类型优化  
- **修复**: 只读属性的映射问题
- **改进**: init 属性的支持
- **优化**: Record 继承链的处理

#### 性能优化
- **优化**: 表达式编译缓存机制
- **改进**: 字符串拼接性能
- **减少**: 不必要的装箱操作

### 📚 文档更新
- 新增 Primary Constructor 详细指南
- 完善 Record 类型使用示例
- 更新性能基准测试数据

---

## [2.0.0] - 2024-11-XX - 首个正式版本 🎉

### 🚀 核心功能

#### 源生成器技术
- **零反射**: 编译时生成，运行时原生性能
- **类型安全**: 编译期 SQL 语法和类型验证
- **智能推断**: 方法名自动推断 SQL 操作类型

#### ExpressionToSql 引擎
- **LINQ 支持**: 表达式到 SQL 的类型安全转换
- **多数据库**: SQL Server、MySQL、PostgreSQL、SQLite、Oracle、DB2
- **动态查询**: 支持复杂的查询构建

#### SqlTemplate 引擎
- **模板语法**: 支持条件、循环、函数等高级语法
- **参数化查询**: 自动生成参数化 SQL，防止注入
- **方言适配**: 自动适配不同数据库语法

### 🏗️ 现代 C# 支持

#### C# 12+ 特性
- **Primary Constructor**: 完整支持主构造函数
- **Record 类型**: 原生支持不可变数据类型
- **Required 成员**: 支持必需属性和字段

#### AOT 兼容性
- **原生编译**: 完整支持 .NET 9 AOT
- **零反射**: 生成的代码无反射调用
- **小体积**: 优化的输出，适合容器化部署

### 📦 包结构
- **Sqlx**: 核心运行时库
- **Sqlx.Generator**: 源生成器组件

### 🎯 设计目标达成
- **高性能**: 相比 EF Core 提升 3-26 倍性能
- **易用性**: 声明式API，最小化样板代码
- **可维护性**: 清晰的代码结构和错误提示

---

## [1.x.x] - 开发版本

### 实验性功能
- 初始的源生成器原型
- 基础的表达式转换功能
- 原始的模板引擎实现

---

## 🔮 未来计划

### 3.0.0 计划功能
- **GraphQL 集成**: 自动生成 GraphQL 解析器
- **缓存层**: 内置查询结果缓存
- **分布式支持**: 分库分表支持
- **实时查询**: SignalR 集成的实时数据更新

### 持续改进
- **性能优化**: 持续的基准测试和优化
- **数据库支持**: 新增数据库方言支持
- **开发工具**: Visual Studio 扩展和 CLI 工具

---

## 📝 版本说明

### 版本号规则
- **主版本** (Major): 重大架构变更或破坏性更新
- **次版本** (Minor): 新功能添加，向后兼容
- **修订版本** (Patch): Bug 修复和小改进

### 支持策略
- **当前版本**: 完整支持和新功能开发
- **前一版本**: 重要 Bug 修复和安全更新
- **更早版本**: 仅关键安全修复

### 获取更新
- **NuGet**: 通过 NuGet 包管理器自动更新
- **GitHub**: 关注 GitHub Releases 获取最新信息
- **文档**: 查看在线文档了解新功能

---

<div align="center">

**📋 查看完整的更新历史，了解 Sqlx 的发展历程**

**🔔 关注我们的 GitHub 获取最新更新通知**

</div>