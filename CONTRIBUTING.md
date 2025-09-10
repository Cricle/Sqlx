# 贡献指南

感谢您对 Sqlx 项目的关注！我们欢迎各种形式的贡献，包括但不限于代码、文档、测试、问题报告和功能建议。

## 🚀 快速开始

### 开发环境要求

- **.NET SDK 6.0 或 8.0+**
- **Visual Studio 2022** 或 **Visual Studio Code**
- **Git**

### 项目设置

1. **Fork 项目**
   ```bash
   git clone https://github.com/your-username/Sqlx.git
   cd Sqlx
   ```

2. **安装依赖**
   ```bash
   dotnet restore
   ```

3. **构建项目**
   ```bash
   dotnet build
   ```

4. **运行测试**
   ```bash
   dotnet test
   ```

## 🧪 测试要求

在提交任何代码更改之前，请确保：

### 必须通过的测试
```bash
# 核心单元测试
dotnet test tests/Sqlx.Tests --filter "TestCategory!=Integration"

# 集成测试
dotnet test tests/Sqlx.IntegrationTests

# 性能测试
dotnet test tests/Sqlx.PerformanceTests

# 示例测试
dotnet test samples/RepositoryExample.Tests
```

### 测试覆盖率要求
- 新增代码应该有 **80%+ 的测试覆盖率**
- 核心功能必须有 **100% 的测试覆盖率**
- 所有公共 API 必须有对应的测试

## 📝 代码规范

### C# 编码标准

1. **命名规范**
   - 类名: `PascalCase`
   - 方法名: `PascalCase`
   - 属性名: `PascalCase`
   - 字段名: `_camelCase` (私有字段使用下划线前缀)
   - 参数名: `camelCase`

2. **代码格式**
   - 使用 4 个空格缩进
   - 每行最大 120 字符
   - 大括号独占一行

3. **注释要求**
   ```csharp
   /// <summary>
   /// 简要描述方法的功能
   /// </summary>
   /// <param name="parameter">参数描述</param>
   /// <returns>返回值描述</returns>
   public string ExampleMethod(string parameter)
   {
       // 实现逻辑
   }
   ```

### 测试代码规范

1. **测试命名**
   ```csharp
   [TestMethod]
   public void MethodName_Condition_ExpectedResult()
   {
       // 测试实现
   }
   ```

2. **测试结构 (AAA模式)**
   ```csharp
   [TestMethod]
   public void ExampleTest()
   {
       // Arrange - 准备测试数据
       var input = CreateTestData();
       
       // Act - 执行被测试方法
       var result = SystemUnderTest.Method(input);
       
       // Assert - 验证结果
       Assert.AreEqual(expected, result);
   }
   ```

## 🔄 贡献流程

### 1. 创建 Issue

在开始编码之前，请先创建一个 Issue 来描述：
- 🐛 **Bug 报告**: 包含重现步骤、期望行为、实际行为
- ✨ **功能请求**: 详细描述新功能的用途和实现思路
- 📚 **文档改进**: 说明需要改进的文档内容

### 2. 创建分支

```bash
# 从 main 分支创建新分支
git checkout -b feature/your-feature-name

# 或者修复 bug
git checkout -b fix/issue-description
```

### 3. 开发和测试

1. **编写代码**
   - 遵循项目的编码规范
   - 添加必要的注释和文档

2. **编写测试**
   - 为新功能添加单元测试
   - 为复杂功能添加集成测试
   - 确保所有测试通过

3. **运行完整测试套件**
   ```bash
   dotnet test
   ```

### 4. 提交代码

1. **提交信息规范**
   ```bash
   # 功能添加
   git commit -m "feat: add new feature description"
   
   # Bug 修复
   git commit -m "fix: resolve issue with specific problem"
   
   # 测试添加
   git commit -m "test: add tests for component functionality"
   
   # 文档更新
   git commit -m "docs: update API documentation"
   ```

2. **推送分支**
   ```bash
   git push origin feature/your-feature-name
   ```

### 5. 创建 Pull Request

1. **PR 标题**: 简洁明了地描述更改
2. **PR 描述**: 包含以下内容
   - 更改的详细说明
   - 相关 Issue 链接
   - 测试覆盖情况
   - 破坏性更改说明（如有）

3. **PR 检查清单**
   - [ ] 所有测试通过
   - [ ] 代码遵循项目规范
   - [ ] 添加了必要的测试
   - [ ] 更新了相关文档
   - [ ] 没有破坏现有功能

## 🎯 贡献类型

### 代码贡献

1. **核心功能开发**
   - 新的 SQL 方言支持
   - 性能优化改进
   - 新的工具类和辅助方法

2. **测试改进**
   - 增加测试覆盖率
   - 性能基准测试
   - 集成测试场景

3. **Bug 修复**
   - 修复已知问题
   - 改进错误处理
   - 提升稳定性

### 文档贡献

1. **API 文档**
   - 方法和类的详细说明
   - 使用示例和最佳实践

2. **教程和指南**
   - 入门教程
   - 高级用法指南
   - 故障排除文档

3. **示例代码**
   - 实际应用场景示例
   - 性能优化示例

## 🔍 代码审查

### 审查标准

1. **功能性**
   - 代码是否正确实现了预期功能
   - 是否处理了边界条件和异常情况

2. **性能**
   - 是否存在性能瓶颈
   - 内存使用是否合理

3. **可维护性**
   - 代码是否易于理解和维护
   - 是否遵循了项目的架构模式

4. **测试**
   - 测试覆盖率是否充分
   - 测试是否有效验证了功能

### 审查流程

1. **自动检查**: CI 流程会自动运行测试和代码分析
2. **人工审查**: 项目维护者会审查代码质量和设计
3. **反馈和改进**: 根据审查意见进行必要的修改
4. **合并**: 审查通过后合并到主分支

## 🏷️ 版本发布

### 语义化版本

项目遵循 [语义化版本](https://semver.org/) 规范：
- **MAJOR**: 不兼容的 API 更改
- **MINOR**: 向后兼容的功能添加
- **PATCH**: 向后兼容的问题修复

### 发布流程

1. **功能冻结**: 确定发布范围
2. **测试验证**: 全面测试新功能和回归测试
3. **文档更新**: 更新 CHANGELOG 和相关文档
4. **版本标记**: 创建版本标签和发布说明

## 🤝 社区准则

### 行为准则

1. **尊重他人**: 保持友善和专业的交流
2. **建设性反馈**: 提供有建设性的意见和建议
3. **协作精神**: 愿意帮助他人和接受帮助
4. **质量优先**: 优先考虑代码质量和用户体验

### 沟通渠道

- **GitHub Issues**: 问题报告和功能请求
- **Pull Requests**: 代码审查和讨论
- **Discussions**: 一般性讨论和问答

## 📚 有用资源

- [测试指南](docs/TESTING_GUIDE.md) - 详细的测试编写指南
- [性能指南](docs/PERFORMANCE_GUIDE.md) - 性能优化最佳实践
- [API 文档](docs/api/) - 详细的 API 参考文档

## 🙏 致谢

感谢所有为 Sqlx 项目做出贡献的开发者！您的贡献使这个项目变得更好。

---

如果您有任何问题或建议，请随时通过 GitHub Issues 联系我们。我们期待您的贡献！
