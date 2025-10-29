# Troubleshooting Guide

> **Sqlx & Sqlx Visual Studio Extension 故障排除指南**

本指南帮助您解决使用Sqlx和VS Extension时遇到的常见问题。

---

## 📋 目录

- [Sqlx核心库问题](#sqlx核心库问题)
- [Sqlx.Generator问题](#sqlxgenerator问题)
- [VS Extension问题](#vs-extension问题)
- [性能问题](#性能问题)
- [构建和编译问题](#构建和编译问题)
- [数据库连接问题](#数据库连接问题)
- [常见错误代码](#常见错误代码)

---

## 🔧 Sqlx核心库问题

### ❌ 问题: 代码未生成

**症状:**
```csharp
// 使用了SqlTemplate但没有生成代码
[SqlTemplate("SELECT * FROM users")]
Task<User> GetUser();  // 编译错误: 缺少实现
```

**可能原因和解决方法:**

#### 1. 未安装Sqlx.Generator
```xml
<!-- 检查 .csproj 文件 -->
<ItemGroup>
  <PackageReference Include="Sqlx" Version="0.4.0" />
  <PackageReference Include="Sqlx.Generator" Version="0.4.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

**解决:**
```bash
dotnet add package Sqlx.Generator
```

#### 2. 生成器未启用
```xml
<!-- 确保配置正确 -->
<PackageReference Include="Sqlx.Generator" Version="0.4.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
</PackageReference>
```

#### 3. IDE缓存问题

**解决:**
```bash
# 清理并重新构建
dotnet clean
dotnet build

# 或在Visual Studio中
# Build > Clean Solution
# Build > Rebuild Solution
```

#### 4. 查看生成的文件

**VS 2022:**
```
Solution Explorer 
  > Dependencies 
  > Analyzers 
  > Sqlx.Generator 
  > [生成的文件]
```

---

### ❌ 问题: 运行时错误 - "Connection is null"

**症状:**
```
System.NullReferenceException: Object reference not set to an instance of an object
```

**原因:** Repository未正确注入数据库连接

**解决方法:**

#### 1. 依赖注入配置
```csharp
// ✅ 正确
services.AddScoped<IDbConnection>(sp => 
    new SqlConnection(connectionString));
services.AddScoped<IUserRepository, UserRepository>();

// ❌ 错误
services.AddScoped<IUserRepository, UserRepository>();  // 缺少连接
```

#### 2. 手动实例化
```csharp
// ✅ 正确
using var connection = new SqlConnection(connectionString);
var repository = new UserRepository(connection);

// ❌ 错误
var repository = new UserRepository(null);  // 连接为null
```

---

### ❌ 问题: SQL语法错误

**症状:**
```
System.Data.SqlClient.SqlException: Incorrect syntax near 'XXX'
```

**调试步骤:**

#### 1. 查看生成的SQL
```csharp
// 在VS Extension中打开SQL Preview窗口
// Tools > Sqlx > SQL Preview

// 或者记录SQL
[SqlTemplate("SELECT * FROM {{table}} WHERE id = @id")]
public partial Task<User> GetUserAsync(int id);

// 在运行时查看实际SQL (需要配置日志)
```

#### 2. 检查占位符使用
```csharp
// ✅ 正确
[SqlTemplate("SELECT {{columns}} FROM {{table}}")]

// ❌ 错误
[SqlTemplate("SELECT {columns} FROM {table}")]  // 单花括号
[SqlTemplate("SELECT {{ columns }} FROM {{ table }}")]  // 有空格
```

#### 3. 检查参数名称
```csharp
// ✅ 正确
[SqlTemplate("WHERE id = @id")]
Task<User> GetUser(int id);  // 参数名匹配

// ❌ 错误
[SqlTemplate("WHERE id = @userId")]
Task<User> GetUser(int id);  // 参数名不匹配
```

---

### ❌ 问题: 类型转换错误

**症状:**
```
InvalidCastException: Unable to cast object of type 'X' to type 'Y'
```

**常见原因:**

#### 1. 数据库类型与C#类型不匹配
```csharp
// ❌ 错误
public class User
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }  // 数据库是BIGINT
}

// ✅ 正确
public class User
{
    public int Id { get; set; }
    public long CreatedAtTicks { get; set; }  // 匹配BIGINT
    public DateTime CreatedAt => new DateTime(CreatedAtTicks);
}
```

#### 2. NULL值处理
```csharp
// ❌ 错误
public class User
{
    public string Name { get; set; }  // 数据库可为NULL
}

// ✅ 正确
public class User
{
    public string? Name { get; set; }  // 可空引用类型
}
```

---

## 🔨 Sqlx.Generator问题

### ❌ 问题: 生成器警告/错误

**症状:** 编译时出现 SQLX001, SQLX002 等警告

#### SQLX001: Parameter Mismatch

**错误:**
```
SQLX001: Parameter '@name' in SQL template does not match any method parameter
```

**解决:**
```csharp
// ❌ 错误
[SqlTemplate("SELECT * FROM users WHERE name = @userName")]
Task<User> GetUser(string name);  // 参数名不匹配

// ✅ 正确1: 修改参数名
[SqlTemplate("SELECT * FROM users WHERE name = @name")]
Task<User> GetUser(string name);

// ✅ 正确2: 修改SQL
[SqlTemplate("SELECT * FROM users WHERE name = @userName")]
Task<User> GetUser(string userName);
```

#### SQLX002: Invalid Placeholder

**错误:**
```
SQLX002: Unknown placeholder '{{invalid}}'
```

**解决:**
```csharp
// ❌ 错误
[SqlTemplate("SELECT {{invalid}} FROM users")]

// ✅ 正确: 使用有效占位符
[SqlTemplate("SELECT {{columns}} FROM users")]
```

支持的占位符：
- `{{columns}}`
- `{{table}}`
- `{{values}}`
- `{{set}}`
- `{{where}}`
- `{{limit}}`
- `{{offset}}`
- `{{orderby}}`
- `{{batch_values}}`

---

## 🖥️ VS Extension问题

### ❌ 问题: Extension未安装

**症状:** 在 Tools 菜单中找不到 Sqlx

**解决方法:**

#### 1. 检查是否已安装
```
Visual Studio
  > Extensions
  > Manage Extensions
  > Installed
  > 搜索 "Sqlx"
```

#### 2. 重新安装
```
1. 关闭所有Visual Studio实例
2. 双击 Sqlx.Extension.vsix
3. 按照安装向导操作
4. 重启Visual Studio
```

#### 3. 检查VS版本
```
要求: Visual Studio 2022 (17.0或更高)
检查: Help > About Microsoft Visual Studio
```

---

### ❌ 问题: 语法着色不工作

**症状:** SqlTemplate字符串没有颜色高亮

**检查项:**

#### 1. 确保在SqlTemplate中
```csharp
// ✅ 会着色
[SqlTemplate("SELECT * FROM users")]

// ❌ 不会着色
var sql = "SELECT * FROM users";
```

#### 2. 重启Visual Studio
```
File > Exit
重新打开项目
```

#### 3. 检查主题
```
Tools > Options > Environment > Fonts and Colors
确保使用支持的主题 (Dark, Light, Blue)
```

#### 4. 重置Extension
```
Tools > Options > Environment > Extensions
找到Sqlx > Disable
重启VS
再次Enable
```

---

### ❌ 问题: IntelliSense不出现

**症状:** 输入 {{ 或 @ 时没有智能提示

**解决方法:**

#### 1. 手动触发
```
按 Ctrl+Space 手动触发IntelliSense
```

#### 2. 检查位置
```csharp
// ✅ 正确: 在SqlTemplate字符串内
[SqlTemplate("SELECT {{ |")]  // 在这里按Ctrl+Space
                     ↑

// ❌ 错误: 在字符串外
[SqlTemplate("SELECT {{columns}}")] |  // 这里不会有IntelliSense
```

#### 3. 清除缓存
```
Tools > Options > Text Editor > All Languages > IntelliSense
取消勾选 "Show completion list after a character is typed"
重新勾选
```

---

### ❌ 问题: 工具窗口无法打开

**症状:** 点击 Tools > Sqlx > [窗口] 后没有反应

**解决方法:**

#### 1. 检查窗口是否已打开
```
Window > Reset Window Layout
查找窗口是否被隐藏
```

#### 2. 查看错误日志
```
View > Output
选择 "Show output from: Extensions"
查找错误信息
```

#### 3. 重新安装Extension
```bash
# 卸载
在Extensions > Manage Extensions中卸载Sqlx

# 重启VS

# 重新安装
双击 Sqlx.Extension.vsix
```

---

### ❌ 问题: SQL Preview显示错误

**症状:** SQL Preview窗口显示 "Error generating SQL"

**可能原因:**

#### 1. 代码未编译
```
Solution Explorer > 右键项目 > Build
确保项目编译成功
```

#### 2. 光标不在SqlTemplate方法上
```csharp
// 确保光标在方法定义上
[SqlTemplate("SELECT * FROM users")]
Task<User> GetUser();  // ← 光标应该在这里
```

#### 3. 使用了高级特性
```
某些复杂的SqlTemplate可能暂不支持预览
请查看生成的代码 (Tools > Sqlx > Generated Code)
```

---

## ⚡ 性能问题

### ❌ 问题: 查询很慢

**诊断步骤:**

#### 1. 使用Performance Analyzer
```
Tools > Sqlx > Performance Analyzer
查看慢查询 (>500ms)
```

#### 2. 检查生成的SQL
```
Tools > Sqlx > SQL Preview
检查是否有不必要的JOIN或子查询
```

#### 3. 添加索引
```sql
-- 检查WHERE子句中的列
CREATE INDEX idx_users_email ON users(email);
```

#### 4. 使用批量操作
```csharp
// ❌ 慢: 逐个插入
foreach (var user in users)
{
    await repository.InsertAsync(user);
}

// ✅ 快: 批量插入
await repository.BatchInsertAsync(users);
```

---

### ❌ 问题: 内存使用过高

**症状:** 应用程序内存占用持续增长

**可能原因:**

#### 1. 未释放连接
```csharp
// ❌ 错误
var connection = new SqlConnection(connectionString);
var repository = new UserRepository(connection);
// 使用repository...
// 忘记释放

// ✅ 正确
using (var connection = new SqlConnection(connectionString))
{
    var repository = new UserRepository(connection);
    // 使用repository...
}  // 自动释放
```

#### 2. 大结果集未分页
```csharp
// ❌ 可能导致内存问题
var allUsers = await repository.GetAllAsync();  // 100万行

// ✅ 使用分页
var page = await repository.GetPageAsync(pageIndex: 1, pageSize: 100);
```

---

## 🏗️ 构建和编译问题

### ❌ 问题: dotnet build 失败

**症状:**
```
error MSB4057: The target "Build" does not exist in the project
```

**原因:** 尝试用 `dotnet build` 构建VSIX项目

**解决:**
```bash
# ❌ 不要用dotnet build
dotnet build  # 会失败在Sqlx.Extension项目

# ✅ 方法1: 排除Extension项目
dotnet build --filter "Sqlx.Extension"  # 不正确语法，用下面的

# ✅ 方法2: 只构建核心库
dotnet build src/Sqlx
dotnet build src/Sqlx.Generator

# ✅ 方法3: 用MSBuild构建Extension
cd src/Sqlx.Extension
msbuild /p:Configuration=Release
```

---

### ❌ 问题: NuGet还原失败

**症状:**
```
error NU1102: Unable to find package 'XXX' with version (>= X.X.X)
```

**解决方法:**

#### 1. 清除缓存
```bash
dotnet nuget locals all --clear
dotnet restore
```

#### 2. 检查NuGet源
```bash
dotnet nuget list source
dotnet nuget add source https://api.nuget.org/v3/index.json
```

#### 3. 更新NuGet
```bash
# 更新NuGet.exe
nuget update -self

# 或在Visual Studio中
Tools > NuGet Package Manager > Package Manager Settings
点击 "Clear All NuGet Cache(s)"
```

---

### ❌ 问题: 编译警告 NU1605

**警告:**
```
NU1605: Detected package downgrade: Microsoft.VisualStudio.XXX from 17.x to 16.x
```

**解决:**
```xml
<!-- 在 .csproj 中统一版本 -->
<ItemGroup>
  <PackageReference Include="Microsoft.VisualStudio.SDK" Version="17.0.32112.339" />
  <PackageReference Include="Microsoft.VisualStudio.Text.UI" Version="17.0.491" />
  <!-- 确保所有VS SDK包版本一致 -->
</ItemGroup>
```

---

## 🗄️ 数据库连接问题

### ❌ 问题: 连接字符串错误

**症状:**
```
SqlException: Cannot open database "XXX" requested by the login
```

**检查项:**

#### 1. 连接字符串格式
```csharp
// SQL Server
var connStr = "Server=localhost;Database=mydb;User Id=sa;Password=pass;";

// SQLite
var connStr = "Data Source=mydb.db";

// MySQL
var connStr = "Server=localhost;Database=mydb;Uid=root;Pwd=pass;";

// PostgreSQL
var connStr = "Host=localhost;Database=mydb;Username=postgres;Password=pass;";
```

#### 2. 数据库是否存在
```sql
-- SQL Server
SELECT name FROM sys.databases;

-- SQLite
-- 文件是否存在？

-- MySQL
SHOW DATABASES;

-- PostgreSQL
\l
```

---

### ❌ 问题: 权限不足

**症状:**
```
SqlException: The user is not associated with a trusted SQL Server connection
```

**解决:**

#### 1. Windows身份验证
```csharp
var connStr = "Server=localhost;Database=mydb;Integrated Security=True;";
```

#### 2. SQL Server身份验证
```csharp
var connStr = "Server=localhost;Database=mydb;User Id=sa;Password=YourPassword;";
```

#### 3. 检查用户权限
```sql
-- 授予权限
GRANT SELECT, INSERT, UPDATE, DELETE ON DATABASE::mydb TO [username];
```

---

## 🔢 常见错误代码

### Extension错误

| 代码 | 描述 | 解决方法 |
|------|------|----------|
| SQLX001 | 参数不匹配 | 检查SQL参数与方法参数名是否一致 |
| SQLX002 | 无效占位符 | 使用支持的占位符 |
| SQLX003 | 语法错误 | 检查SQL语法 |
| EXT001 | 窗口加载失败 | 重启VS或重新安装Extension |
| EXT002 | IntelliSense初始化失败 | 清除VS缓存 |

### 运行时错误

| 错误 | 描述 | 解决方法 |
|------|------|----------|
| NullReferenceException | 连接或参数为null | 检查依赖注入配置 |
| SqlException | SQL语法或权限错误 | 查看SQL Preview，检查权限 |
| InvalidCastException | 类型转换失败 | 检查C#类型与数据库类型匹配 |
| TimeoutException | 查询超时 | 优化查询或增加超时时间 |

---

## 📞 获取帮助

如果以上方法都无法解决您的问题：

### 1. 查看文档
- **在线文档**: https://cricle.github.io/Sqlx/
- **API参考**: [docs/API_REFERENCE.md](docs/API_REFERENCE.md)
- **快速参考**: [QUICK_REFERENCE.md](QUICK_REFERENCE.md)

### 2. 搜索已知问题
- **GitHub Issues**: https://github.com/Cricle/Sqlx/issues
- 搜索关键词

### 3. 报告新问题
```markdown
1. 访问 https://github.com/Cricle/Sqlx/issues/new
2. 选择 Bug Report 模板
3. 填写详细信息:
   - 错误描述
   - 重现步骤
   - 环境信息
   - 错误日志
   - 截图
4. 提交
```

### 4. 社区讨论
- **Discussions**: https://github.com/Cricle/Sqlx/discussions
- 提问和交流

---

## 🛠️ 诊断工具

### 收集诊断信息

**VS Extension日志:**
```
1. View > Output
2. Show output from: Extensions
3. 查找Sqlx相关信息
```

**Event Viewer (Windows):**
```
1. Win + R > eventvwr
2. Windows Logs > Application
3. 查找Visual Studio或Sqlx错误
```

**生成诊断报告:**
```bash
# 运行环境测试脚本
.\test-build-env.ps1

# 输出包含:
# - PowerShell版本
# - VS安装信息
# - MSBuild路径
# - .NET SDK版本
# - 项目文件状态
```

---

## ✅ 预防措施

### 最佳实践

1. **定期更新**
   ```bash
   dotnet outdated
   dotnet add package Sqlx --version [latest]
   ```

2. **使用版本控制**
   ```bash
   git commit -m "Working state before update"
   ```

3. **阅读更新日志**
   - 查看 [CHANGELOG.md](CHANGELOG.md)
   - 了解破坏性更改

4. **编写测试**
   ```csharp
   [Fact]
   public async Task Test_Repository_GetUser()
   {
       // 测试确保功能正常
   }
   ```

5. **监控性能**
   - 使用Performance Analyzer
   - 设置性能基线
   - 定期审查慢查询

---

**问题解决了吗？** 🎉

如果还有问题，请不要犹豫创建Issue！我们会尽快帮助您。


