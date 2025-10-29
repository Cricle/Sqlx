# Sqlx 文档中心

欢迎来到Sqlx文档中心！这里包含了使用Sqlx所需的所有文档。

---

## 📚 文档导航

### 🚀 快速开始

- **[快速开始指南](QUICK_START_GUIDE.md)** - 5分钟上手Sqlx
  - 安装配置
  - 基础示例
  - 常见问题

### 📖 核心文档

- **[API参考](API_REFERENCE.md)** - 完整的API文档
  - 特性 (Attributes)
  - 占位符系统
  - 仓储接口
  - CRUD接口

- **[占位符指南](PLACEHOLDERS.md)** - 强大的占位符系统详解
  - `{{columns}}` - 列选择
  - `{{table}}` - 表名
  - `{{where}}` - WHERE条件
  - `{{limit}}`/`{{offset}}` - 分页
  - `{{orderby}}` - 排序
  - `{{batch_values}}` - 批量插入
  - 多数据库支持

- **[高级特性](ADVANCED_FEATURES.md)** - 高级功能和最佳实践
  - SoftDelete（软删除）
  - AuditFields（审计字段）
  - ConcurrencyCheck（乐观锁）
  - 表达式树转SQL
  - 批量操作
  - 事务支持
  - 拦截器
  
- **[最佳实践](BEST_PRACTICES.md)** - 推荐的使用模式
  - 性能优化建议
  - 安全性最佳实践
  - 错误处理
  - 测试策略

### 🛠️ Visual Studio 插件

- **[VS 扩展开发计划](VSCODE_EXTENSION_PLAN.md)** - 完整的扩展功能规划
  - 语法着色（✅ 已完成）
  - 代码片段（✅ 已完成）
  - 快速操作（✅ 已完成）
  - 参数验证（✅ 已完成）
  - IntelliSense（规划中）
  - 生成代码查看器（规划中）

- **[扩展开发总结](EXTENSION_SUMMARY.md)** - P0 功能完成报告
- **[语法高亮实现](SYNTAX_HIGHLIGHTING_IMPLEMENTATION.md)** - 技术实现细节
- **[构建说明](../src/Sqlx.Extension/BUILD.md)** - 如何构建扩展
- **[测试指南](../src/Sqlx.Extension/TESTING_GUIDE.md)** - 功能测试清单

---

## 🌐 在线资源

- **[GitHub Pages](https://cricle.github.io/Sqlx/web/)** - 在线文档和演示
- **[GitHub仓库](https://github.com/Cricle/Sqlx)** - 源代码和问题跟踪
- **[NuGet包](https://www.nuget.org/packages/Sqlx/)** - 下载和版本历史

---

## 📦 示例项目

查看[samples目录](../samples/)中的完整示例：

- **[FullFeatureDemo](../samples/FullFeatureDemo/)** - 演示所有Sqlx功能
- **[TodoWebApi](../samples/TodoWebApi/)** - 真实Web API示例

---

## 🤝 贡献

发现文档有误或需要改进？欢迎[提交Issue](https://github.com/Cricle/Sqlx/issues)或[Pull Request](https://github.com/Cricle/Sqlx/pulls)！

---

## 📄 许可证

Sqlx采用[MIT许可证](../LICENSE.txt)。

---

<div align="center">

**Sqlx - 让数据访问回归简单，让性能接近极致！** 🚀

</div>
