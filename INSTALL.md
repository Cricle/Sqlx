# Sqlx Installation Guide

> **完整的安装和配置指南**

---

## 📋 目录

- [系统要求](#系统要求)
- [安装Sqlx核心库](#安装sqlx核心库)
- [安装Visual Studio Extension](#安装visual-studio-extension)
- [验证安装](#验证安装)
- [配置和设置](#配置和设置)
- [故障排除](#故障排除)
- [卸载](#卸载)

---

## 系统要求

### Sqlx核心库

#### 最低要求
```
✅ .NET 6.0 SDK 或更高
✅ C# 10 或更高
✅ 任何支持.NET的操作系统
   - Windows 10/11
   - Linux (Ubuntu 20.04+, CentOS 8+等)
   - macOS 11+
```

#### 推荐配置
```
✅ .NET 8.0 SDK
✅ C# 12
✅ Visual Studio 2022 或 VS Code
✅ 8GB RAM
✅ SSD存储
```

#### 支持的数据库
```
✅ SQLite 3.x
✅ MySQL 5.7+, 8.0+
✅ PostgreSQL 12+
✅ SQL Server 2019+
✅ Oracle 19c+
```

---

### Visual Studio Extension

#### 必需
```
✅ Visual Studio 2022 (17.0 或更高)
   - Community Edition ✅
   - Professional Edition ✅
   - Enterprise Edition ✅
✅ Windows 10 (1903或更高) 或 Windows 11
✅ .NET Framework 4.7.2 或更高
✅ 100MB 可用磁盘空间
```

#### 推荐
```
✅ Visual Studio 2022 (17.8+)
✅ 深色主题（更好的语法着色效果）
✅ 4GB+ RAM
✅ 1080p+ 显示器（更好的工具窗口体验）
```

#### 不支持
```
❌ Visual Studio 2019
❌ Visual Studio 2017
❌ Visual Studio Code（计划中）
❌ JetBrains Rider（计划中）
```

---

## 安装Sqlx核心库

### 方法1: .NET CLI（推荐）

#### 步骤1: 创建或打开项目
```bash
# 创建新项目
dotnet new console -n MyApp
cd MyApp

# 或打开现有项目
cd path/to/your/project
```

#### 步骤2: 安装NuGet包
```bash
# 安装Sqlx核心库
dotnet add package Sqlx

# 安装源代码生成器（必需）
dotnet add package Sqlx.Generator
```

#### 步骤3: 验证安装
```bash
# 列出已安装的包
dotnet list package

# 应该看到:
# Sqlx              0.4.0
# Sqlx.Generator    0.4.0
```

#### 步骤4: 构建项目
```bash
dotnet build
```

---

### 方法2: Visual Studio NuGet管理器

#### 步骤1: 打开NuGet包管理器
```
1. 打开Visual Studio 2022
2. 打开你的项目
3. 右键点击项目 > "Manage NuGet Packages"
```

#### 步骤2: 搜索并安装
```
1. 点击 "Browse" 标签
2. 搜索 "Sqlx"
3. 选择 "Sqlx"
4. 点击 "Install"
5. 重复步骤2-4，安装 "Sqlx.Generator"
```

#### 步骤3: 接受许可证
```
点击 "I Accept" 接受MIT许可证
```

#### 步骤4: 等待安装完成
```
查看 "Output" 窗口确认安装成功
```

---

### 方法3: Package Manager Console

#### 步骤1: 打开控制台
```
Tools > NuGet Package Manager > Package Manager Console
```

#### 步骤2: 运行命令
```powershell
Install-Package Sqlx
Install-Package Sqlx.Generator
```

#### 步骤3: 验证
```powershell
Get-Package | Where-Object {$_.Id -like "*Sqlx*"}
```

---

### 方法4: 手动编辑.csproj

#### 步骤1: 打开.csproj文件
```xml
<!-- 在Visual Studio中右键项目 > Edit Project File -->
<!-- 或直接用文本编辑器打开 .csproj -->
```

#### 步骤2: 添加PackageReference
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <!-- 添加这两行 -->
    <PackageReference Include="Sqlx" Version="0.4.0" />
    <PackageReference Include="Sqlx.Generator" Version="0.4.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

#### 步骤3: 恢复包
```bash
dotnet restore
```

---

## 安装Visual Studio Extension

### 方法1: 从VSIX文件安装（推荐）

#### 步骤1: 下载VSIX
```
访问: https://github.com/Cricle/Sqlx/releases
下载最新版本的 Sqlx.Extension.vsix
```

#### 步骤2: 关闭Visual Studio
```
确保关闭所有Visual Studio实例
```

#### 步骤3: 安装VSIX
```
1. 双击下载的 Sqlx.Extension.vsix 文件
2. 在安装向导中点击 "Install"
3. 选择要安装到的VS版本（如果有多个）
4. 等待安装完成
5. 点击 "Close"
```

#### 步骤4: 启动Visual Studio
```
启动Visual Studio 2022
```

#### 步骤5: 验证安装
```
1. Extensions > Manage Extensions
2. 点击 "Installed" 标签
3. 搜索 "Sqlx"
4. 应该看到 "Sqlx Visual Studio Extension"
```

---

### 方法2: 从Visual Studio Marketplace（即将上线）

#### 步骤1: 打开Extensions管理器
```
Extensions > Manage Extensions
```

#### 步骤2: 搜索
```
1. 在搜索框输入 "Sqlx"
2. 点击搜索
```

#### 步骤3: 安装
```
1. 找到 "Sqlx Visual Studio Extension"
2. 点击 "Download"
3. 关闭Visual Studio以完成安装
4. 重新启动Visual Studio
```

---

### 方法3: 命令行安装

#### 使用VSIXInstaller
```powershell
# 找到VSIXInstaller路径
$vsixInstaller = "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\VSIXInstaller.exe"

# 安装VSIX
& $vsixInstaller /quiet Sqlx.Extension.vsix
```

---

## 验证安装

### 验证Sqlx核心库

#### 测试1: 创建简单示例
```csharp
using Sqlx;
using Sqlx.Annotations;

// 如果这些using语句没有红线，说明安装成功
[SqlDefine(SqlDialect.SQLite)]
public interface ITestRepository
{
    [SqlTemplate("SELECT * FROM test")]
    Task<List<string>> GetAllAsync();
}
```

#### 测试2: 构建项目
```bash
dotnet build
```

应该看到:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

#### 测试3: 检查生成的代码
```
在VS Solution Explorer中:
> Dependencies
  > Analyzers
    > Sqlx.Generator
      > [应该看到生成的文件]
```

---

### 验证Visual Studio Extension

#### 测试1: 检查Tools菜单
```
1. 打开Visual Studio
2. 点击 "Tools" 菜单
3. 应该看到 "Sqlx" 菜单项
4. 鼠标悬停应该看到子菜单:
   - SQL Preview
   - Generated Code
   - Query Tester
   - Repository Explorer
   - (其他10个窗口)
```

#### 测试2: 测试语法着色
```csharp
// 创建一个.cs文件并输入:
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
Task<User> GetUserAsync(int id);

// 检查:
// - "SELECT", "FROM", "WHERE" 应该是蓝色
// - "@id" 应该是青绿色
// - 如果没有颜色，Extension可能没有正确安装
```

#### 测试3: 测试IntelliSense
```csharp
// 输入:
[SqlTemplate("SELECT {{ ")]
//                    ↑ 在这里应该弹出IntelliSense

// 应该看到:
// - columns
// - table
// - values
// - 等等...
```

#### 测试4: 打开工具窗口
```
1. Tools > Sqlx > SQL Preview
2. 应该打开一个新窗口
3. 如果没有，查看 Output 窗口的错误信息
```

---

## 配置和设置

### Sqlx核心库配置

#### 配置数据库方言
```csharp
// SQLite
[SqlDefine(SqlDialect.SQLite)]

// MySQL
[SqlDefine(SqlDialect.MySql)]

// PostgreSQL
[SqlDefine(SqlDialect.PostgreSql)]

// SQL Server
[SqlDefine(SqlDialect.SqlServer)]

// Oracle
[SqlDefine(SqlDialect.Oracle)]
```

#### 配置连接字符串（示例）

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=app.db"
  }
}
```

**依赖注入配置:**
```csharp
// Program.cs
services.AddScoped<IDbConnection>(sp => 
    new SqliteConnection(
        configuration.GetConnectionString("DefaultConnection")
    )
);

services.AddScoped<IUserRepository, UserRepository>();
```

---

### Visual Studio Extension配置

#### 当前版本
```
目前Extension是零配置的，安装后立即可用。
所有功能使用默认设置。
```

#### 未来版本（v0.6+）
```
计划添加的配置选项:
- 自定义语法着色颜色
- 自定义快捷键
- 工具窗口默认位置
- IntelliSense触发行为
- 代码片段自定义
```

#### 当前可调整项
```
Visual Studio > Tools > Options > Fonts and Colors
可以自定义Sqlx的颜色方案
```

---

## 故障排除

### 问题1: 代码未生成

**症状:**
```
编译时没有错误，但代码似乎没有生成
```

**解决方法:**
```bash
# 1. 清理解决方案
dotnet clean

# 2. 删除bin和obj文件夹
rm -rf bin obj

# 3. 恢复包
dotnet restore

# 4. 重新构建
dotnet build

# 5. 在VS中查看生成的文件
# Solution Explorer > Dependencies > Analyzers > Sqlx.Generator
```

---

### 问题2: Extension未显示

**症状:**
```
Extension已安装，但Tools菜单中没有Sqlx
```

**解决方法:**
```
# 方法1: 重置VS
Tools > Import and Export Settings > Reset all settings

# 方法2: 重新安装Extension
1. Extensions > Manage Extensions
2. 卸载Sqlx Extension
3. 重启VS
4. 重新安装VSIX

# 方法3: 检查Extension是否启用
Extensions > Manage Extensions > Installed
确保Sqlx Extension已启用
```

---

### 问题3: IntelliSense不工作

**症状:**
```
输入 {{ 或 @ 时没有自动提示
```

**解决方法:**
```
# 1. 确保在SqlTemplate字符串内
[SqlTemplate("SELECT {{ |")]  ✅ 正确位置
                     ↑

# 2. 手动触发
按 Ctrl+Space

# 3. 检查Extension是否正常加载
View > Output > 选择 "Extensions"
查找错误信息

# 4. 重启Visual Studio
```

---

### 问题4: 语法着色不工作

**症状:**
```
SqlTemplate字符串没有颜色高亮
```

**解决方法:**
```
# 1. 确保在SqlTemplate属性中
[SqlTemplate("SELECT * FROM users")]  ✅ 会着色
var sql = "SELECT * FROM users";      ❌ 不会着色

# 2. 重启Visual Studio

# 3. 检查主题
Tools > Options > Environment > General > Color theme
尝试切换到 Dark 或 Blue 主题

# 4. 重置Extension
见"问题2"的解决方法
```

---

### 问题5: NuGet包冲突

**症状:**
```
NU1605: Detected package downgrade
```

**解决方法:**
```xml
<!-- 在.csproj中添加 -->
<PropertyGroup>
  <NoWarn>$(NoWarn);NU1605</NoWarn>
</PropertyGroup>

<!-- 或更新包到一致版本 -->
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
```

---

### 问题6: VSIX安装失败

**症状:**
```
双击VSIX时提示错误
```

**解决方法:**
```
# 1. 确保VS已关闭
关闭所有Visual Studio实例

# 2. 以管理员权限运行
右键 Sqlx.Extension.vsix > 以管理员身份运行

# 3. 检查VS版本
确保VS版本是2022 (17.0+)

# 4. 修复VS安装
Visual Studio Installer > Modify > Repair

# 5. 查看安装日志
%TEMP%\VSIXInstaller_*.log
```

---

## 卸载

### 卸载Sqlx核心库

#### 方法1: .NET CLI
```bash
dotnet remove package Sqlx
dotnet remove package Sqlx.Generator
```

#### 方法2: Visual Studio
```
1. 右键项目 > Manage NuGet Packages
2. 点击 "Installed" 标签
3. 找到 Sqlx
4. 点击 "Uninstall"
5. 重复步骤3-4卸载 Sqlx.Generator
```

#### 方法3: 手动编辑.csproj
```xml
<!-- 删除这些行 -->
<PackageReference Include="Sqlx" Version="0.4.0" />
<PackageReference Include="Sqlx.Generator" Version="0.4.0">
  ...
</PackageReference>
```

然后运行:
```bash
dotnet restore
```

---

### 卸载Visual Studio Extension

#### 方法1: Extensions管理器
```
1. Extensions > Manage Extensions
2. 点击 "Installed" 标签
3. 找到 "Sqlx Visual Studio Extension"
4. 点击 "Uninstall"
5. 重启Visual Studio完成卸载
```

#### 方法2: 命令行
```powershell
# 找到VSIXInstaller
$vsixInstaller = "C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\VSIXInstaller.exe"

# 卸载（需要知道Extension ID）
& $vsixInstaller /uninstall:Sqlx.Extension

# 或者卸载所有版本
& $vsixInstaller /uninstall:Sqlx.Extension /all
```

---

## 升级

### 升级Sqlx核心库

#### 检查更新
```bash
# 检查过时的包
dotnet list package --outdated

# 或使用NuGet
dotnet list package --outdated --include-transitive
```

#### 升级到最新版本
```bash
# 升级Sqlx
dotnet add package Sqlx

# 升级Sqlx.Generator
dotnet add package Sqlx.Generator
```

#### 升级到特定版本
```bash
dotnet add package Sqlx --version 0.5.0
dotnet add package Sqlx.Generator --version 0.5.0
```

---

### 升级Visual Studio Extension

#### 自动更新（Marketplace版本）
```
Extensions > Manage Extensions > Updates
如果有新版本，点击 "Update"
```

#### 手动更新（VSIX版本）
```
1. 下载新版VSIX
2. 双击安装（会自动覆盖旧版）
3. 重启Visual Studio
```

---

## 快速开始检查清单

### Sqlx核心库安装
```
☐ .NET SDK已安装（6.0+）
☐ 项目已创建
☐ Sqlx包已安装
☐ Sqlx.Generator包已安装
☐ 项目可成功构建
☐ 生成的代码可见
```

### VS Extension安装
```
☐ Visual Studio 2022已安装
☐ VSIX已下载
☐ VS已关闭
☐ VSIX已安装
☐ VS已重启
☐ Tools菜单有Sqlx
☐ IntelliSense正常工作
☐ 语法着色正常工作
☐ 工具窗口可打开
```

---

## 获取帮助

### 文档
```
📚 在线文档: https://cricle.github.io/Sqlx/
📖 快速开始: docs/QUICK_START_GUIDE.md
🎓 完整教程: TUTORIAL.md
❓ FAQ: FAQ.md
🔧 故障排除: TROUBLESHOOTING.md
```

### 社区支持
```
🐛 报告问题: https://github.com/Cricle/Sqlx/issues
💬 讨论: https://github.com/Cricle/Sqlx/discussions
⭐ GitHub: https://github.com/Cricle/Sqlx
```

---

## 下一步

安装完成后，建议：

1. **阅读快速开始**
   ```
   docs/QUICK_START_GUIDE.md
   ```

2. **完成教程**
   ```
   TUTORIAL.md - 10课从入门到精通
   ```

3. **查看示例**
   ```
   samples/FullFeatureDemo/
   samples/TodoWebApi/
   ```

4. **浏览API参考**
   ```
   docs/API_REFERENCE.md
   ```

5. **加入社区**
   ```
   GitHub Discussions
   ```

---

**祝您使用愉快！** 🚀

如有任何问题，请查阅文档或在GitHub上提问。

**Happy Coding!** 😊


