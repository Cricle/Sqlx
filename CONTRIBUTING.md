# Contributing to Sqlx

> **感谢您考虑为Sqlx项目贡献！**

我们欢迎任何形式的贡献，包括但不限于：Bug报告、功能建议、代码贡献、文档改进等。

---

## 📋 目录

- [行为准则](#行为准则)
- [如何贡献](#如何贡献)
  - [报告Bug](#报告bug)
  - [建议功能](#建议功能)
  - [代码贡献](#代码贡献)
  - [文档改进](#文档改进)
- [开发环境设置](#开发环境设置)
- [代码规范](#代码规范)
- [提交规范](#提交规范)
- [Pull Request流程](#pull-request流程)
- [项目结构](#项目结构)
- [测试指南](#测试指南)
- [发布流程](#发布流程)

---

## 🤝 行为准则

### 我们的承诺

为了营造一个开放和友好的环境，我们作为贡献者和维护者承诺：无论年龄、体型、残疾、种族、性别特征、性别认同和表达、经验水平、教育程度、社会经济地位、国籍、个人外貌、种族、宗教或性认同和性取向如何，参与我们的项目和社区都将为所有人提供无骚扰的体验。

### 我们的标准

**积极行为示例：**
- 使用友好和包容的语言
- 尊重不同的观点和经验
- 优雅地接受建设性批评
- 关注对社区最有利的事情
- 对其他社区成员表示同情

**不可接受的行为示例：**
- 使用性化的语言或图像
- 挑衅、侮辱/贬损性评论，以及人身或政治攻击
- 公开或私下骚扰
- 未经明确许可发布他人的私人信息
- 其他在专业环境中可能被合理认为不适当的行为

---

## 🎯 如何贡献

### 报告Bug

**好的Bug报告**应该包含：

1. **使用描述性标题** - 清楚地说明问题
2. **重现步骤** - 详细的步骤列表
3. **预期行为** - 你期望发生什么
4. **实际行为** - 实际发生了什么
5. **环境信息** - 操作系统、.NET版本、VS版本等
6. **截图** - 如果适用
7. **额外上下文** - 任何其他相关信息

**Bug报告模板：**

```markdown
## 问题描述
[清晰简洁的描述]

## 重现步骤
1. 打开 '...'
2. 点击 '...'
3. 滚动到 '...'
4. 看到错误

## 预期行为
[描述你期望发生什么]

## 实际行为
[描述实际发生了什么]

## 截图
[如果适用，添加截图]

## 环境信息
- OS: [例如 Windows 11]
- Visual Studio: [例如 VS 2022 17.8]
- .NET Version: [例如 .NET 6.0]
- Sqlx Version: [例如 v0.5.0]

## 额外上下文
[添加任何其他相关信息]
```

**提交Bug报告：**
- 访问 [GitHub Issues](https://github.com/Cricle/Sqlx/issues)
- 点击 "New Issue"
- 选择 "Bug Report" 模板
- 填写信息并提交

---

### 建议功能

**好的功能建议**应该包含：

1. **清晰的用例** - 为什么需要这个功能
2. **详细描述** - 功能应该如何工作
3. **替代方案** - 你考虑过的其他方法
4. **额外上下文** - 截图、原型等

**功能建议模板：**

```markdown
## 功能描述
[清晰简洁的描述]

## 问题/动机
[这个功能解决什么问题？为什么需要它？]

## 建议的解决方案
[你希望如何实现？]

## 替代方案
[你考虑过的其他方法]

## 额外上下文
[截图、原型、示例代码等]
```

**提交功能建议：**
- 访问 [GitHub Discussions](https://github.com/Cricle/Sqlx/discussions)
- 点击 "New Discussion"
- 选择 "Ideas" 类别
- 填写信息并提交

---

### 代码贡献

#### 开始之前

1. **检查现有Issue** - 确保没有重复工作
2. **创建Issue** - 如果不存在，创建一个新Issue讨论你的想法
3. **等待反馈** - 在开始大量工作前，等待维护者的反馈

#### 贡献流程

1. **Fork项目**
   ```bash
   # 在GitHub上Fork仓库
   # 然后克隆你的Fork
   git clone https://github.com/YOUR_USERNAME/Sqlx.git
   cd Sqlx
   ```

2. **创建分支**
   ```bash
   # 创建功能分支
   git checkout -b feature/your-feature-name
   
   # 或者Bug修复分支
   git checkout -b fix/bug-description
   ```

3. **进行更改**
   - 编写代码
   - 遵循代码规范
   - 添加测试
   - 更新文档

4. **测试**
   ```bash
   # 运行所有测试
   dotnet test
   
   # 运行特定项目的测试
   dotnet test tests/Sqlx.Tests
   ```

5. **提交**
   ```bash
   git add .
   git commit -m "feat: add amazing feature"
   ```

6. **推送**
   ```bash
   git push origin feature/your-feature-name
   ```

7. **创建Pull Request**
   - 访问你的Fork在GitHub上
   - 点击 "New Pull Request"
   - 填写PR模板
   - 提交

---

### 文档改进

**文档贡献包括：**
- 修复拼写/语法错误
- 改进现有文档的清晰度
- 添加缺失的文档
- 翻译文档
- 添加代码示例

**文档位置：**
```
docs/                    # 主要文档
├── README.md            # 文档索引
├── QUICK_START_GUIDE.md # 快速开始
├── API_REFERENCE.md     # API参考
├── BEST_PRACTICES.md    # 最佳实践
├── ADVANCED_FEATURES.md # 高级功能
└── web/                 # GitHub Pages
    └── index.html       # 在线文档

README.md                # 项目主页
CHANGELOG.md             # 变更日志
CONTRIBUTING.md          # 本文件
```

**文档风格指南：**
- 使用Markdown格式
- 使用清晰的标题层级
- 添加代码示例
- 包含实用的截图
- 保持简洁明了
- 使用友好的语气

---

## 🛠️ 开发环境设置

### 先决条件

```
✅ Visual Studio 2022 (17.0+)
✅ .NET SDK 6.0+
✅ .NET Framework 4.7.2+
✅ Git
✅ Windows 10/11
```

### 设置步骤

#### 1. 克隆仓库
```bash
git clone https://github.com/Cricle/Sqlx.git
cd Sqlx
```

#### 2. 恢复依赖
```bash
dotnet restore
```

#### 3. 构建解决方案
```bash
# 核心库和生成器
dotnet build

# VS Extension (需要用MSBuild)
cd src/Sqlx.Extension
msbuild /p:Configuration=Debug
cd ../..
```

#### 4. 运行测试
```bash
dotnet test
```

#### 5. 打开Visual Studio
```bash
# 打开解决方案
start Sqlx.sln
```

### 项目结构

```
Sqlx/
├── src/
│   ├── Sqlx/                    # 核心库
│   │   ├── Annotations/         # 特性注解
│   │   ├── ICrudRepository.cs   # CRUD接口
│   │   ├── IQueryRepository.cs  # 查询接口
│   │   ├── ... (其他接口)
│   │   └── Sqlx.csproj
│   │
│   ├── Sqlx.Generator/          # 源代码生成器
│   │   ├── CSharpGenerator.cs   # 主生成器
│   │   ├── Core/                # 核心逻辑
│   │   └── Sqlx.Generator.csproj
│   │
│   └── Sqlx.Extension/          # VS扩展
│       ├── SyntaxColoring/      # 语法着色
│       ├── IntelliSense/        # 智能提示
│       ├── QuickActions/        # 快速操作
│       ├── Diagnostics/         # 诊断分析
│       ├── ToolWindows/         # 工具窗口
│       ├── Commands/            # 命令
│       ├── Debugging/           # 调试支持
│       └── Sqlx.Extension.csproj
│
├── tests/
│   ├── Sqlx.Tests/              # 单元测试
│   └── Sqlx.Benchmarks/         # 性能测试
│
├── samples/
│   ├── SqlxDemo/                # 基础示例
│   ├── FullFeatureDemo/         # 完整功能演示
│   └── TodoWebApi/              # Web API示例
│
├── docs/                        # 文档
├── scripts/                     # 构建脚本
└── .github/workflows/           # CI/CD
```

---

## 📝 代码规范

### C# 代码风格

**遵循标准C#约定：**

```csharp
// ✅ 好的示例
public class UserRepository : IQueryRepository<User, int>
{
    private readonly DbConnection _connection;

    public UserRepository(DbConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        if (id <= 0)
            throw new ArgumentOutOfRangeException(nameof(id));

        // 实现...
    }
}

// ❌ 不好的示例
public class userRepository : IQueryRepository<User,int>
{
    private DbConnection connection;
    
    public async Task<User?> GetByIdAsync(int id)
    {
        //实现...
    }
}
```

**命名约定：**
- **PascalCase**: 类、方法、属性、公共字段
- **camelCase**: 参数、局部变量
- **_camelCase**: 私有字段
- **UPPER_CASE**: 常量

**特性：**
```csharp
// ✅ 使用
[SqlTemplate("SELECT * FROM users WHERE id = @id")]

// ✅ 详细
[SqlTemplate(
    "SELECT {{columns}} FROM {{table}} WHERE id = @id",
    Description = "Get user by ID"
)]
```

**注释：**
```csharp
/// <summary>
/// 根据ID获取用户
/// </summary>
/// <param name="id">用户ID</param>
/// <returns>用户实体或null</returns>
public async Task<User?> GetByIdAsync(int id)
{
    // 实现逻辑
}
```

### 代码质量

**必须：**
- ✅ 所有公共API都有XML注释
- ✅ 参数验证
- ✅ 异常处理
- ✅ 单元测试覆盖
- ✅ 遵循SOLID原则

**推荐：**
- 使用 `async`/`await`
- 使用 `nullable` 引用类型
- 使用现代C#特性（pattern matching, records等）
- 保持方法简短（<50行）
- 避免深层嵌套（<4层）

---

## 📋 提交规范

### Commit Message格式

使用 [Conventional Commits](https://www.conventionalcommits.org/)：

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Type类型：**
- `feat`: 新功能
- `fix`: Bug修复
- `docs`: 文档更改
- `style`: 代码格式（不影响功能）
- `refactor`: 重构
- `perf`: 性能优化
- `test`: 测试相关
- `chore`: 构建/工具相关
- `ci`: CI/CD相关

**示例：**

```bash
# 简单提交
git commit -m "feat: add query builder support"

# 详细提交
git commit -m "feat(repository): add pagination support

- Add PagedResult<T> class
- Add GetPageAsync method to IQueryRepository
- Update documentation
- Add unit tests

Closes #123"

# Bug修复
git commit -m "fix(generator): handle nullable reference types

Previously, the generator would fail when encountering nullable
reference types. This commit adds proper handling.

Fixes #456"

# 破坏性更改
git commit -m "feat!: change repository interface signature

BREAKING CHANGE: IRepository methods now return Task<T> instead of T
Migration guide available in docs/MIGRATION.md"
```

---

## 🔄 Pull Request流程

### PR检查清单

**提交PR前：**

```
☐ 代码遵循项目风格指南
☐ 已添加/更新相关测试
☐ 所有测试通过
☐ 已更新文档
☐ Commit messages遵循规范
☐ 代码已自我审查
☐ 已添加代码注释（特别是复杂逻辑）
☐ 文档拼写和语法正确
☐ 更新了CHANGELOG.md（如适用）
```

### PR模板

```markdown
## 描述
[简要描述此PR的更改]

## 相关Issue
Closes #[issue number]

## 更改类型
- [ ] Bug修复 (非破坏性更改，修复问题)
- [ ] 新功能 (非破坏性更改，添加功能)
- [ ] 破坏性更改 (修复或功能，会导致现有功能不按预期工作)
- [ ] 文档更新

## 测试
[描述你添加或修改的测试]

## 检查清单
- [ ] 代码遵循项目风格
- [ ] 已自我审查
- [ ] 已添加代码注释
- [ ] 已更新文档
- [ ] 无新的警告
- [ ] 已添加测试
- [ ] 所有测试通过
- [ ] 已更新CHANGELOG

## 截图（如适用）
[添加截图]

## 额外说明
[任何其他相关信息]
```

### PR审查流程

1. **自动检查** - CI/CD运行测试
2. **代码审查** - 维护者审查代码
3. **讨论修改** - 根据反馈进行调整
4. **批准** - 维护者批准PR
5. **合并** - 合并到主分支

**审查重点：**
- 代码质量和风格
- 测试覆盖
- 文档完整性
- 性能影响
- 向后兼容性

---

## 🧪 测试指南

### 测试结构

```
tests/
├── Sqlx.Tests/
│   ├── Unit/                    # 单元测试
│   │   ├── RepositoryTests.cs
│   │   ├── GeneratorTests.cs
│   │   └── ...
│   ├── Integration/             # 集成测试
│   │   ├── DatabaseTests.cs
│   │   └── ...
│   └── Fixtures/                # 测试辅助
│
└── Sqlx.Benchmarks/             # 性能测试
    ├── QueryBenchmarks.cs
    └── ...
```

### 编写测试

**单元测试示例：**

```csharp
using Xunit;

public class QueryRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsEntity()
    {
        // Arrange
        var repository = new MockRepository();
        var expectedId = 1;

        // Act
        var result = await repository.GetByIdAsync(expectedId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedId, result.Id);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetByIdAsync_InvalidId_ThrowsException(int invalidId)
    {
        // Arrange
        var repository = new MockRepository();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => repository.GetByIdAsync(invalidId)
        );
    }
}
```

### 运行测试

```bash
# 所有测试
dotnet test

# 特定项目
dotnet test tests/Sqlx.Tests

# 带覆盖率
dotnet test /p:CollectCoverage=true

# 性能测试
cd tests/Sqlx.Benchmarks
dotnet run -c Release
```

---

## 🚀 发布流程

**仅适用于维护者**

1. **更新版本号**
   - `Directory.Build.props`
   - `src/Sqlx.Extension/source.extension.vsixmanifest`

2. **更新CHANGELOG.md**

3. **创建Git标签**
   ```bash
   git tag -a v0.x.0 -m "Release v0.x.0"
   git push origin v0.x.0
   ```

4. **GitHub Release**
   - CI/CD自动创建

5. **NuGet发布**
   - CI/CD自动发布

6. **VS Marketplace**
   - 手动上传VSIX

详见 [HOW_TO_RELEASE.md](HOW_TO_RELEASE.md)

---

## ❓ 问题和帮助

### 获取帮助

- **文档**: https://cricle.github.io/Sqlx/
- **Discussions**: https://github.com/Cricle/Sqlx/discussions
- **Issues**: https://github.com/Cricle/Sqlx/issues

### 常见问题

**Q: 我的PR需要多久才能被审查？**
A: 通常在1-3个工作日内。如果超过1周，请在PR中留言。

**Q: 如何报告安全漏洞？**
A: 请不要公开创建Issue，而是发送邮件至 [维护者邮箱]。

**Q: 我可以贡献什么？**
A: 查看标有 `good first issue` 或 `help wanted` 的Issue。

**Q: 代码风格检查失败怎么办？**
A: 运行 `dotnet format` 自动格式化代码。

---

## 🙏 致谢

感谢所有贡献者！

[![Contributors](https://contrib.rocks/image?repo=Cricle/Sqlx)](https://github.com/Cricle/Sqlx/graphs/contributors)

**特别感谢：**
- 所有提交Bug报告的用户
- 所有提出功能建议的用户
- 所有贡献代码的开发者
- 所有改进文档的编辑者

---

## 📜 许可证

通过贡献代码，您同意您的贡献将根据 [MIT License](LICENSE.txt) 进行许可。

---

**再次感谢您的贡献！** 🎉

您的参与使Sqlx变得更好！


