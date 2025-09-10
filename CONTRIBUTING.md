# 贡献指南

感谢您对 Sqlx 项目的关注！我们欢迎各种形式的贡献，无论是错误报告、功能建议、文档改进还是代码贡献。

## 📋 目录

- [开始之前](#开始之前)
- [如何贡献](#如何贡献)
- [开发环境设置](#开发环境设置)
- [代码规范](#代码规范)
- [测试指南](#测试指南)
- [提交代码](#提交代码)
- [问题报告](#问题报告)
- [功能建议](#功能建议)

## 🚀 开始之前

在开始贡献之前，请：

1. ⭐ **Star** 这个项目，表示您的支持
2. 🍴 **Fork** 这个仓库到您的 GitHub 账户
3. 📖 阅读本贡献指南和项目文档
4. 💬 在相关 Issue 中讨论您的想法（如果适用）

## 🤝 如何贡献

### 代码贡献
- 修复 bug
- 添加新功能
- 性能优化
- 代码重构

### 文档贡献
- 改进文档说明
- 添加示例代码
- 翻译文档
- 修正拼写错误

### 其他贡献
- 报告 bug
- 提出功能建议
- 参与讨论
- 测试和反馈

## 🛠️ 开发环境设置

### 系统要求

- **.NET 6.0 SDK** 或更高版本
- **Visual Studio 2022** 或 **VS Code** （推荐）
- **Git** 版本控制工具

### 克隆项目

```bash
# 克隆您 fork 的仓库
git clone https://github.com/[YourUsername]/Sqlx.git
cd Sqlx

# 添加上游仓库
git remote add upstream https://github.com/Cricle/Sqlx.git
```

### 本地构建

```bash
# 还原 NuGet 包
dotnet restore

# 构建项目
dotnet build

# 运行测试
dotnet test

# 运行示例项目
dotnet run --project samples/RepositoryExample/RepositoryExample.csproj
```

### IDE 配置

#### Visual Studio 2022
1. 打开 `Sqlx.sln` 解决方案文件
2. 确保已安装最新的 .NET SDK
3. 启用源生成器支持（默认已启用）

#### VS Code
1. 安装 C# 扩展
2. 安装 .NET 扩展包
3. 确保 OmniSharp 服务正常运行

## 📝 代码规范

### C# 编码标准

我们遵循 [Microsoft C# 编码约定](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions)：

```csharp
// ✅ 正确的命名和格式
public class UserRepository : IUserService
{
    private readonly DbConnection _connection;
    
    public UserRepository(DbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }
    
    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        // 实现逻辑
    }
}
```

### 代码风格

- 使用 4 个空格缩进（不使用 Tab）
- 每行最大长度 120 字符
- 使用 PascalCase 命名公共成员
- 使用 camelCase 命名私有字段（带 `_` 前缀）
- 使用 var 当类型明显时
- 始终使用大括号，即使是单行语句

### EditorConfig

项目包含 `.editorconfig` 文件，确保代码格式一致：

```ini
root = true

[*.cs]
indent_style = space
indent_size = 4
end_of_line = crlf
insert_final_newline = true
trim_trailing_whitespace = true
```

## 🧪 测试指南

### 测试类型

1. **单元测试** (`tests/Sqlx.Tests/`)
   - 测试源代码生成逻辑
   - 测试 SQL 方言处理
   - 测试类型分析功能

2. **集成测试** (`tests/Sqlx.IntegrationTests/`)
   - 测试与真实数据库的交互
   - 测试多数据库方言的兼容性
   - 测试性能基准

### 编写测试

```csharp
[TestClass]
public class UserRepositoryTests
{
    [TestMethod]
    public async Task GetByIdAsync_ValidId_ReturnsUser()
    {
        // Arrange
        var expectedUser = new User { Id = 1, Name = "Test User" };
        
        // Act
        var result = await repository.GetByIdAsync(1);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedUser.Name, result.Name);
    }
}
```

### 运行测试

```bash
# 运行所有测试
dotnet test

# 运行特定测试项目
dotnet test tests/Sqlx.Tests/

# 运行测试并生成覆盖率报告
dotnet test --collect:"XPlat Code Coverage"
```

### 测试要求

- 新功能必须包含相应的测试
- 测试覆盖率应保持在 80% 以上
- 集成测试应支持所有支持的数据库方言
- 性能测试应验证性能不倒退

## 📤 提交代码

### Git 工作流

我们使用 **GitHub Flow** 工作流：

```bash
# 1. 同步最新代码
git checkout main
git pull upstream main

# 2. 创建功能分支
git checkout -b feature/your-feature-name

# 3. 进行开发
# ... 编码、测试 ...

# 4. 提交变更
git add .
git commit -m "feat: add new feature description"

# 5. 推送到您的 fork
git push origin feature/your-feature-name

# 6. 创建 Pull Request
```

### 提交信息规范

我们使用 [Conventional Commits](https://www.conventionalcommits.org/) 规范：

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

**类型说明：**
- `feat`: 新功能
- `fix`: 错误修复
- `docs`: 文档更新
- `style`: 代码格式调整
- `refactor`: 代码重构
- `test`: 测试相关
- `chore`: 构建过程或工具变更

**示例：**
```
feat(generator): add PostgreSQL dialect support

Add support for PostgreSQL-specific SQL generation including:
- Double-quoted column names
- Dollar-sign parameter prefixes
- SERIAL type handling

Closes #123
```

### Pull Request 指南

创建 PR 时，请确保：

1. **标题清晰明确**，描述所做的更改
2. **详细描述**变更内容和原因
3. **链接相关 Issue**（如果有）
4. **确保所有测试通过**
5. **更新相关文档**
6. **保持小而专注**的变更范围

**PR 模板：**
```markdown
## 变更描述
简要描述这个 PR 的目的和实现方式。

## 变更类型
- [ ] Bug 修复
- [ ] 新功能
- [ ] 性能优化
- [ ] 文档更新
- [ ] 代码重构

## 测试清单
- [ ] 添加了新的测试
- [ ] 所有现有测试通过
- [ ] 手动测试通过

## 相关 Issue
Closes #[issue number]

## 其他说明
如有必要，添加其他相关信息。
```

## 🐛 问题报告

发现 bug？请创建详细的问题报告：

### 报告模板

```markdown
## Bug 描述
清晰简洁地描述 bug 的现象。

## 重现步骤
1. 进入 '...'
2. 点击 '....'
3. 滚动到 '....'
4. 看到错误

## 预期行为
描述您期望发生的情况。

## 实际行为
描述实际发生的情况。

## 环境信息
- OS: [例如 Windows 10]
- .NET 版本: [例如 .NET 6.0]
- Sqlx 版本: [例如 1.0.0]
- 数据库: [例如 SQL Server 2019]

## 其他信息
添加任何其他有用的信息，如错误截图、日志等。
```

### 错误分类

- **Critical**: 导致应用崩溃或数据损坏
- **High**: 核心功能无法使用
- **Medium**: 功能受限但有变通方案
- **Low**: 轻微问题或改进建议

## 💡 功能建议

有新想法？我们很乐意听到您的建议！

### 建议模板

```markdown
## 功能描述
简要描述您想要的功能。

## 问题背景
解释这个功能要解决的问题。

## 建议的解决方案
描述您希望的实现方式。

## 替代方案
描述您考虑过的其他解决方案。

## 其他信息
添加任何其他相关信息或截图。
```

### 功能评估标准

我们会根据以下标准评估功能建议：

- **价值**: 对用户的价值和影响范围
- **复杂度**: 实现的技术复杂度
- **兼容性**: 与现有功能的兼容性
- **维护成本**: 长期维护的成本

## 🏆 认可贡献者

我们重视每一个贡献者！贡献者将会：

- 在项目 README 中被列出
- 获得项目的 Contributor 徽章
- 参与项目的重要决策讨论
- 获得社区的认可和感谢

## 📞 联系我们

有问题或需要帮助？

- 📧 **GitHub Issues**: [提交问题](https://github.com/Cricle/Sqlx/issues)
- 💬 **Discussions**: [参与讨论](https://github.com/Cricle/Sqlx/discussions)
- 📖 **Wiki**: [查看文档](https://github.com/Cricle/Sqlx/wiki)

## 📜 行为准则

参与此项目即表示您同意遵守我们的行为准则：

- 尊重所有参与者
- 保持建设性和友好的讨论
- 接受不同的观点和经验水平
- 专注于对社区最有益的事情

---

再次感谢您的贡献！您的参与让 Sqlx 变得更好。 🚀