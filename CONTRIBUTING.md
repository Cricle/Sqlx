# 贡献指南

感谢您对 Sqlx ORM 框架的兴趣！我们欢迎所有形式的贡献，包括但不限于代码贡献、文档改进、问题报告和功能建议。

## 🤝 如何贡献

### 1️⃣ 报告问题

如果您发现了 bug 或有功能建议，请通过 GitHub Issues 报告：

1. **搜索已有问题** - 确保问题尚未被报告
2. **使用问题模板** - 选择合适的问题模板
3. **提供详细信息** - 包括重现步骤、环境信息等
4. **添加标签** - 选择合适的标签（bug、enhancement、question 等）

#### 问题报告模板

```markdown
**问题描述**
简要描述遇到的问题

**重现步骤**
1. 首先...
2. 然后...
3. 接着...
4. 看到错误

**期望行为**
描述您期望的正确行为

**环境信息**
- OS: [例如：Windows 10]
- .NET 版本: [例如：.NET 9.0]
- Sqlx 版本: [例如：2.0.2]
- 数据库: [例如：SQL Server 2022]

**附加信息**
任何其他有助于解决问题的信息
```

### 2️⃣ 提交代码

#### 开发环境设置

```bash
# 1. Fork 项目到您的 GitHub 账户

# 2. 克隆您的 fork
git clone https://github.com/your-username/Sqlx.git
cd Sqlx

# 3. 添加上游仓库
git remote add upstream https://github.com/original-repo/Sqlx.git

# 4. 安装依赖
dotnet restore

# 5. 构建项目
dotnet build

# 6. 运行测试
dotnet test
```

#### 代码贡献流程

1. **创建功能分支**
   ```bash
   git checkout -b feature/your-feature-name
   ```

2. **编写代码**
   - 遵循项目代码规范
   - 添加必要的单元测试
   - 更新相关文档

3. **提交变更**
   ```bash
   git add .
   git commit -m "feat: 添加新功能描述"
   ```

4. **同步上游变更**
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

5. **推送分支**
   ```bash
   git push origin feature/your-feature-name
   ```

6. **创建 Pull Request**
   - 使用清晰的标题和描述
   - 链接相关的 Issues
   - 确保所有检查通过

## 📝 代码规范

### C# 编码标准

#### 命名约定
```csharp
// ✅ 类名：PascalCase
public class SqlTemplate { }

// ✅ 方法名：PascalCase
public void ExecuteQuery() { }

// ✅ 属性名：PascalCase
public string ConnectionString { get; set; }

// ✅ 字段名：camelCase，私有字段使用 _ 前缀
private readonly IDbConnection _connection;

// ✅ 参数名：camelCase
public void Execute(string parameterName) { }

// ✅ 局部变量：camelCase
var userQuery = "SELECT * FROM users";

// ✅ 常量：PascalCase
public const string DefaultConnectionString = "...";
```

#### 代码结构
```csharp
// ✅ 使用 file-scoped namespace（C# 10+）
namespace Sqlx.Core;

// ✅ 使用 Primary Constructor（C# 12+）
public class UserService(IDbConnection connection)
{
    // ✅ 字段在前
    private readonly ILogger _logger = LoggerFactory.CreateLogger<UserService>();
    
    // ✅ 属性其次
    public bool IsConnected => connection.State == ConnectionState.Open;
    
    // ✅ 方法最后
    public async Task<User> GetUserAsync(int id)
    {
        // 实现...
    }
}
```

#### 文档注释
```csharp
/// <summary>
/// 执行 SQL 模板并返回结果
/// </summary>
/// <typeparam name="T">返回类型</typeparam>
/// <param name="template">SQL 模板</param>
/// <param name="parameters">参数对象</param>
/// <returns>查询结果</returns>
/// <exception cref="ArgumentNullException">当模板为 null 时抛出</exception>
public async Task<T> ExecuteAsync<T>(SqlTemplate template, object? parameters = null)
{
    // 实现...
}
```

### 测试规范

#### 测试命名
```csharp
[TestClass]
public class SqlTemplateTests
{
    [TestMethod]
    public void Parse_ValidSql_ReturnsTemplate()
    {
        // Arrange
        var sql = "SELECT * FROM users";
        
        // Act
        var template = SqlTemplate.Parse(sql);
        
        // Assert
        Assert.IsNotNull(template);
        Assert.AreEqual(sql, template.Sql);
    }
    
    [TestMethod]
    public void Execute_WithParameters_CreatesParameterizedSql()
    {
        // 测试实现...
    }
}
```

#### 测试结构
- **Arrange**: 准备测试数据
- **Act**: 执行被测试的操作
- **Assert**: 验证结果

#### 测试覆盖率
- 新功能必须有对应的单元测试
- 目标覆盖率：> 95%
- 包含正常情况和边界情况测试

## 📚 文档贡献

### 文档类型

1. **API 文档** - XML 注释生成
2. **用户指南** - Markdown 格式
3. **示例代码** - 完整可运行的示例
4. **架构文档** - 设计决策和架构说明

### 文档标准

#### Markdown 格式
```markdown
# 一级标题

## 二级标题

### 三级标题

#### 代码示例
\`\`\`csharp
var template = SqlTemplate.Parse("SELECT * FROM users");
\`\`\`

#### 表格
| 功能 | 描述 | 状态 |
|------|------|------|
| 模板解析 | 解析 SQL 模板 | ✅ |

#### 注意事项
> **重要**: 这是一个重要的提示

> **警告**: 这是一个警告信息
```

#### 示例代码
- 所有示例代码必须能够编译和运行
- 使用真实的场景，避免过于简化
- 包含必要的注释说明

## 🔄 发布流程

### 版本号规则
- **主版本** (Major): 重大破坏性变更
- **次版本** (Minor): 新功能，向后兼容
- **修订版本** (Patch): Bug 修复

### 发布检查清单
- [ ] 所有测试通过
- [ ] 代码覆盖率 > 95%
- [ ] 文档已更新
- [ ] CHANGELOG 已更新
- [ ] 版本号已更新
- [ ] NuGet 包描述正确

## 🏷️ 提交消息规范

我们使用 [Conventional Commits](https://www.conventionalcommits.org/) 规范：

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### 类型说明
- `feat`: 新功能
- `fix`: Bug 修复
- `docs`: 文档更新
- `style`: 代码格式化（不影响功能）
- `refactor`: 重构（不是新功能或修复）
- `perf`: 性能优化
- `test`: 添加或修改测试
- `chore`: 构建过程或辅助工具的变动

### 示例
```
feat(sqltemplate): 添加纯模板设计支持

- 重构 SqlTemplate 为纯模板定义
- 新增 ParameterizedSql 类型
- 实现模板重用机制

Closes #123
```

## 🎯 开发优先级

### 高优先级
1. **Bug 修复** - 影响核心功能的问题
2. **性能优化** - 显著的性能改进
3. **安全修复** - 安全漏洞修复

### 中优先级
1. **新功能** - 增强现有功能
2. **文档改进** - 重要文档的完善
3. **测试完善** - 提高测试覆盖率

### 低优先级
1. **代码清理** - 代码质量改进
2. **开发工具** - 开发体验改进

## 📞 联系方式

### 获取帮助
- **GitHub Discussions**: 技术讨论和问答
- **GitHub Issues**: Bug 报告和功能请求
- **邮件**: dev@sqlx.dev

### 贡献者社区
- **Slack**: [加入我们的 Slack](https://sqlx.slack.com)
- **微信群**: 联系管理员邀请加入

## 🏆 贡献者认可

我们重视每一个贡献者的努力：

- **代码贡献者**: 在 GitHub 贡献者列表中展示
- **文档贡献者**: 在文档页面感谢
- **社区贡献者**: 在社区活动中特别感谢

### 成为核心贡献者
持续贡献的开发者有机会成为核心团队成员，获得：
- 代码仓库写权限
- 参与重要决策
- 技术路线图制定

---

<div align="center">

**🤝 感谢您对 Sqlx 的贡献！**

**每一个贡献都让 Sqlx 变得更好**

</div>