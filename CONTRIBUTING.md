# 🤝 Sqlx 贡献指南

<div align="center">

**感谢您对 Sqlx 项目的关注和贡献！**

[![贡献者](https://img.shields.io/badge/欢迎-新贡献者-green?style=for-the-badge)]()
[![代码质量](https://img.shields.io/badge/代码质量-严格把关-blue?style=for-the-badge)]()
[![社区](https://img.shields.io/badge/社区-友好互助-orange?style=for-the-badge)]()

**无论是 Bug 报告、功能建议、代码贡献还是文档改进，我们都非常欢迎！**

</div>

---

## 📋 贡献方式

<table>
<tr>
<td width="25%">

### 🐛 Bug 报告
- 详细描述问题
- 提供重现步骤
- 附上环境信息
- 期待的行为说明

</td>
<td width="25%">

### 💡 功能建议
- 清晰的需求描述
- 使用场景说明
- 可能的实现方案
- API 设计建议

</td>
<td width="25%">

### 🔧 代码贡献
- 修复 Bug
- 新增功能
- 性能优化
- 重构改进

</td>
<td width="25%">

### 📚 文档贡献
- 错误修正
- 内容补充
- 示例改进
- 翻译工作

</td>
</tr>
</table>

---

## 🐛 报告问题

### 提交 Issue 前的检查

1. **🔍 搜索现有 Issues** - 检查 [Issues](https://github.com/Cricle/Sqlx/issues) 是否已有相同问题
2. **📖 查阅文档** - 确认不是使用方法问题，参考 [文档中心](docs/)
3. **🧪 最新版本验证** - 确认问题在最新版本中仍然存在

### Bug 报告模板

创建新 Issue 时，请包含以下信息：

```markdown
## 🐛 Bug 描述
简洁清晰地描述遇到的问题

## 🔄 重现步骤
1. 执行操作 A
2. 调用方法 B
3. 观察结果 C

## ✅ 期待的行为
描述您期待的正确行为

## 💻 环境信息
- Sqlx 版本: v2.0.2
- .NET 版本: .NET 8.0
- 数据库: SQL Server 2022
- 操作系统: Windows 11
- IDE: Visual Studio 2022

## 📝 补充信息
- 错误消息
- 堆栈跟踪
- 相关代码片段
- 屏幕截图（如适用）
```

---

## 💡 功能建议

### 建议格式

```markdown
## 🎯 功能需求
清晰描述您希望添加的功能

## 🎬 使用场景
解释该功能的实际应用场景

## 💡 建议的 API 设计
```csharp
// 示例 API 设计
public interface INewFeature
{
    Task<Result> DoSomethingAsync();
}
```

## 🔧 可能的实现方案
- 方案 A: xxx
- 方案 B: xxx

## 📊 影响评估
- 对性能的影响
- 对现有 API 的影响
- 向后兼容性
```

---

## 🔧 代码贡献

### 开发环境设置

#### 1. Fork 和克隆仓库

```bash
# Fork 仓库到您的 GitHub 账户，然后克隆
git clone https://github.com/YOUR_USERNAME/Sqlx.git
cd Sqlx

# 添加上游仓库
git remote add upstream https://github.com/Cricle/Sqlx.git
```

#### 2. 环境要求

- **.NET 8.0 SDK** - [下载地址](https://dotnet.microsoft.com/download)
- **Visual Studio 2022** 或 **VS Code** 或 **JetBrains Rider**
- **Git** - 版本控制工具

#### 3. 项目构建

```bash
# 恢复依赖
dotnet restore

# 构建项目
dotnet build

# 运行测试
dotnet test

# 生成代码覆盖率报告
dotnet test --collect:"XPlat Code Coverage"
```

### 开发工作流

#### 1. 创建功能分支

```bash
# 确保主分支是最新的
git checkout main
git pull upstream main

# 创建新的功能分支
git checkout -b feature/new-awesome-feature

# 或者 bug 修复分支
git checkout -b fix/bug-description
```

#### 2. 开发和测试

```bash
# 进行开发...

# 运行相关测试
dotnet test tests/Sqlx.Tests/

# 运行代码风格检查
dotnet format --verify-no-changes

# 运行完整的 CI 流程（本地）
./scripts/build.ps1
```

#### 3. 提交代码

```bash
# 添加更改
git add .

# 提交更改（使用有意义的提交消息）
git commit -m "feat: 添加新的批量操作支持

- 实现 BatchUpdateWithCondition 方法
- 添加相应的单元测试
- 更新文档说明

Closes #123"

# 推送到您的 fork
git push origin feature/new-awesome-feature
```

### 代码规范

#### C# 编码规范

我们遵循 Microsoft 的 C# 编码规范，项目中已配置 StyleCop 和 EditorConfig：

```csharp
// ✅ 推荐的代码风格
public class UserService : IUserService
{
    private readonly DbConnection _connection;

    public UserService(DbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async Task<User> GetUserByIdAsync(int id)
    {
        // 实现...
    }
}

// ✅ 异步方法命名
public async Task<IList<User>> GetUsersAsync()
{
    // 实现...
}

// ✅ 注释规范
/// <summary>
/// 根据用户 ID 获取用户信息
/// </summary>
/// <param name="id">用户 ID</param>
/// <returns>用户信息，如果不存在则返回 null</returns>
public async Task<User?> GetUserByIdAsync(int id)
{
    // 实现...
}
```

#### 测试编写规范

```csharp
[TestClass]
public class UserServiceTests
{
    [TestMethod]
    public async Task GetUserByIdAsync_ValidId_ReturnsUser()
    {
        // Arrange
        var userId = 1;
        var expectedUser = new User { Id = userId, Name = "Test User" };
        
        // Act
        var result = await _userService.GetUserByIdAsync(userId);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(expectedUser.Id, result.Id);
        Assert.AreEqual(expectedUser.Name, result.Name);
    }

    [TestMethod]
    public async Task GetUserByIdAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        var invalidId = -1;
        
        // Act
        var result = await _userService.GetUserByIdAsync(invalidId);
        
        // Assert
        Assert.IsNull(result);
    }
}
```

### Pull Request 流程

#### 1. 创建 Pull Request

- 导航到您的 fork 在 GitHub 上的页面
- 点击 "New pull request"
- 选择正确的分支进行比较
- 填写 PR 模板

#### 2. PR 描述模板

```markdown
## 📝 更改描述
简要描述本次 PR 的目的和更改内容

## 🎯 相关 Issue
Closes #123
Related to #456

## 🧪 测试
- [ ] 添加了新的单元测试
- [ ] 现有测试全部通过
- [ ] 进行了手动测试

## 📚 文档
- [ ] 更新了相关文档
- [ ] 添加了 XML 注释
- [ ] 更新了 README（如需要）

## ✅ 检查清单
- [ ] 代码遵循项目编码规范
- [ ] 提交消息清晰有意义
- [ ] 分支是从最新的 main 分支创建的
- [ ] 解决了所有 merge 冲突
```

#### 3. 代码审查

- 维护者会审查您的代码
- 可能会要求进行修改
- 请及时响应反馈
- 所有检查通过后会合并

---

## 📚 文档贡献

### 文档类型

| 文档类型 | 描述 | 位置 |
|----------|------|------|
| **API 文档** | XML 注释，自动生成 | 代码文件中 |
| **用户指南** | 使用说明和教程 | `docs/` 目录 |
| **示例代码** | 完整的示例项目 | `samples/` 目录 |
| **README** | 项目介绍和快速开始 | 根目录 |

### 文档编写规范

#### Markdown 规范

```markdown
# 主标题使用 H1

## 章节标题使用 H2

### 小节标题使用 H3

- 使用 - 作为列表标识符
- 保持一致的缩进

1. 有序列表使用数字
2. 按顺序编号

```csharp
// 代码块使用三个反引号，并指定语言
public void Example()
{
    // 注释...
}
```

> 💡 **提示**: 使用引用块提供重要信息

**加粗重要内容** 和 *斜体强调*
```

#### 中文文档规范

- 使用简体中文
- 技术术语保持英文（如 Primary Constructor）
- 代码注释使用中文
- 保持语言简洁明了

---

## 🚀 发布流程

### 版本管理

我们遵循 [语义化版本](https://semver.org/lang/zh-CN/) 规范：

- **主版本号 (MAJOR)**: 不兼容的 API 修改
- **次版本号 (MINOR)**: 向下兼容的功能性新增
- **修订号 (PATCH)**: 向下兼容的问题修正

### 发布步骤

```bash
# 1. 确保在 main 分支且是最新版本
git checkout main
git pull origin main

# 2. 更新版本号（在 Directory.Build.props 中）
# 3. 更新 CHANGELOG.md

# 4. 创建发布标签
git tag v2.1.0
git push origin v2.1.0

# 5. GitHub Actions 会自动：
#    - 运行完整测试套件
#    - 构建 NuGet 包
#    - 发布到 NuGet.org
#    - 创建 GitHub Release
```

---

## 🎯 贡献者指南

### 首次贡献者

如果您是首次贡献开源项目，建议从以下开始：

1. **🐛 修复小 Bug** - 查看标有 `good first issue` 的 Issues
2. **📚 改进文档** - 修正错别字、补充说明
3. **🧪 添加测试** - 提高代码覆盖率
4. **💡 功能建议** - 参与 Issues 讨论

### 进阶贡献者

1. **🔧 核心功能开发** - 实现新的 SQL 方言支持
2. **⚡ 性能优化** - 改进代码生成效率
3. **🏗️ 架构改进** - 重构和模块化
4. **📊 工具开发** - Visual Studio 扩展等

### 维护者职责

项目维护者会：

- 及时回应 Issues 和 PRs
- 进行代码审查
- 管理发布流程
- 维护项目质量

---

## 🏆 贡献者认可

### 贡献统计

我们会在以下地方认可贡献者：

- **README.md** - 主要贡献者列表
- **CHANGELOG.md** - 版本更新贡献者
- **GitHub Contributors** - 自动统计
- **Release Notes** - 特殊贡献说明

### 贡献类型

| 类型 | 图标 | 说明 |
|------|------|------|
| 💻 | 代码 | 功能开发、Bug 修复 |
| 📖 | 文档 | 文档编写、改进 |
| 🐛 | Bug 报告 | 发现并报告问题 |
| 💡 | 想法 | 功能建议、设计思路 |
| 🎨 | 设计 | UI/UX 设计 |
| 🔧 | 工具 | 开发工具、CI/CD |

---

## 📞 联系方式

### 获取帮助

- 🐛 **[GitHub Issues](https://github.com/Cricle/Sqlx/issues)** - Bug 报告和功能请求
- 💬 **[GitHub Discussions](https://github.com/Cricle/Sqlx/discussions)** - 技术讨论和问答
- 📧 **邮件联系** - 私密问题或安全相关问题

### 社区参与

- 🌟 **Star 项目** - 表示支持和关注
- 👀 **Watch 项目** - 获取更新通知
- 🔄 **分享项目** - 推荐给其他开发者

---

<div align="center">

**🎉 感谢您对 Sqlx 项目的贡献！**

**每一个贡献都让 Sqlx 变得更好，让 .NET 社区受益**

**[⬆ 返回顶部](#-sqlx-贡献指南)**

</div>