# 安装指南

本指南详细介绍如何在不同环境中安装和配置 Sqlx。

## 📋 系统要求

### .NET 版本要求

| .NET 版本 | 支持状态 | 说明 |
|-----------|----------|------|
| .NET 8.0+ | ✅ 推荐 | 最佳性能和功能支持 |
| .NET 7.0 | ✅ 完全支持 | 稳定支持 |
| .NET 6.0 | ✅ 完全支持 | LTS 版本，推荐生产环境 |
| .NET 5.0 | ✅ 支持 | 基础功能支持 |
| .NET Core 3.1 | ✅ 支持 | 需要 C# 9.0+ |
| .NET Standard 2.0 | ✅ 支持 | 兼容 .NET Framework 4.7.2+ |

### 开发环境要求

- **Visual Studio 2022** (推荐) 或 **Visual Studio Code**
- **.NET SDK** 对应版本
- **C# 9.0** 或更高版本（支持源代码生成器）

## 📦 安装方法

### 方法1: 使用 .NET CLI (推荐)

```bash
# 添加 Sqlx 包到项目
dotnet add package Sqlx

# 或者指定版本
dotnet add package Sqlx --version 1.0.0
```

### 方法2: 使用 Package Manager Console

在 Visual Studio 中打开 Package Manager Console：

```powershell
# 安装最新版本
Install-Package Sqlx

# 或者指定版本
Install-Package Sqlx -Version 1.0.0
```

### 方法3: 使用 PackageReference

在项目文件 (`.csproj`) 中添加：

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Sqlx" Version="1.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
  </ItemGroup>

</Project>
```

### 方法4: 使用 Visual Studio Package Manager UI

1. 右键点击项目 → 管理 NuGet 包
2. 点击"浏览"选项卡
3. 搜索 "Sqlx"
4. 选择 Sqlx 包并点击"安装"

## 🔧 项目配置

### 必需的项目设置

```xml
<PropertyGroup>
  <!-- 启用最新 C# 语言特性 -->
  <LangVersion>latest</LangVersion>
  
  <!-- 启用可空引用类型（推荐） -->
  <Nullable>enable</Nullable>
  
  <!-- 启用源代码生成器输出（调试时有用） -->
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
  <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
```

### 数据库提供程序

根据使用的数据库，添加相应的数据库提供程序包：

#### SQL Server

```bash
dotnet add package Microsoft.Data.SqlClient
```

```xml
<PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.0" />
```

#### MySQL

```bash
dotnet add package MySql.Data
# 或者使用 Pomelo (推荐)
dotnet add package Pomelo.EntityFrameworkCore.MySql
```

#### PostgreSQL

```bash
dotnet add package Npgsql
```

#### SQLite

```bash
dotnet add package Microsoft.Data.Sqlite
```

#### Oracle

```bash
dotnet add package Oracle.ManagedDataAccess.Core
```

## 🚀 验证安装

### 创建测试项目

```bash
# 创建新的控制台项目
dotnet new console -n SqlxTest
cd SqlxTest

# 添加 Sqlx 和 SQLite 支持
dotnet add package Sqlx
dotnet add package Microsoft.Data.Sqlite
```

### 测试代码

创建 `Program.cs`：

```csharp
using Microsoft.Data.Sqlite;
using Sqlx.Annotations;
using System.Data.Common;

// 定义实体
[TableName("users")]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// 定义服务接口
public interface IUserService
{
    [Sqlx("SELECT * FROM users")]
    IList<User> GetAllUsers();
    
    [SqlExecuteType(SqlExecuteTypes.Insert, "users")]
    int CreateUser(User user);
}

// Repository 实现
[RepositoryFor(typeof(IUserService))]
public partial class UserRepository : IUserService
{
    private readonly DbConnection connection;
    
    public UserRepository(DbConnection connection)
    {
        this.connection = connection;
    }
}

// 主程序
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Sqlx 安装验证测试");
        
        // 创建内存数据库连接
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        
        // 创建测试表
        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL,
                Email TEXT NOT NULL
            )";
        command.ExecuteNonQuery();
        
        // 测试 Repository
        var userRepo = new UserRepository(connection);
        
        // 创建用户
        var user = new User { Name = "测试用户", Email = "test@example.com" };
        int result = userRepo.CreateUser(user);
        Console.WriteLine($"创建用户结果: {result}");
        
        // 查询用户
        var users = userRepo.GetAllUsers();
        Console.WriteLine($"查询到 {users.Count} 个用户");
        
        foreach (var u in users)
        {
            Console.WriteLine($"用户: {u.Name} - {u.Email}");
        }
        
        Console.WriteLine("✅ Sqlx 安装和配置成功！");
    }
}
```

### 运行测试

```bash
# 构建并运行
dotnet build
dotnet run
```

**期望输出**:
```
Sqlx 安装验证测试
创建用户结果: 1
查询到 1 个用户
用户: 测试用户 - test@example.com
✅ Sqlx 安装和配置成功！
```

## 🔍 故障排除

### 常见安装问题

#### 1. 源代码生成器不工作

**症状**: 没有生成代码，编译错误

**解决方案**:
```xml
<!-- 确保正确配置源代码生成器 -->
<PackageReference Include="Sqlx" Version="1.0.0">
  <PrivateAssets>all</PrivateAssets>
  <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
</PackageReference>
```

#### 2. C# 版本不兼容

**症状**: 编译错误，不支持源代码生成器

**解决方案**:
```xml
<PropertyGroup>
  <LangVersion>9.0</LangVersion> <!-- 或 latest -->
</PropertyGroup>
```

#### 3. Visual Studio 不识别生成的代码

**症状**: IntelliSense 不工作，红色下划线

**解决方案**:
1. 重启 Visual Studio
2. 清理并重新构建解决方案
3. 删除 `bin` 和 `obj` 文件夹后重新构建

#### 4. .NET Framework 兼容性问题

**症状**: .NET Framework 项目中无法使用

**解决方案**:
```xml
<!-- 确保 .NET Framework 4.7.2 或更高版本 -->
<PropertyGroup>
  <TargetFramework>net472</TargetFramework>
  <LangVersion>latest</LangVersion>
</PropertyGroup>
```

### 验证安装状态

#### 检查包是否正确安装

```bash
# 列出项目中的所有包
dotnet list package

# 检查特定包
dotnet list package | grep Sqlx
```

#### 检查生成的文件

启用编译器生成文件输出后，检查 `Generated/Sqlx/` 目录：

```bash
# Windows
dir Generated\Sqlx\

# Linux/macOS
ls Generated/Sqlx/
```

#### 检查源代码生成器状态

在 Visual Studio 中：
1. 解决方案资源管理器
2. 展开项目 → Dependencies → Analyzers
3. 应该看到 "Sqlx" 分析器

## 🌍 不同环境配置

### ASP.NET Core 项目

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Sqlx" Version="1.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.0" />
  </ItemGroup>

</Project>
```

**Program.cs** 配置:
```csharp
var builder = WebApplication.CreateBuilder(args);

// 配置数据库连接
builder.Services.AddScoped<DbConnection>(provider => 
    new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

// 注册 Repository
builder.Services.AddScoped<IUserService, UserRepository>();

var app = builder.Build();
app.Run();
```

### Blazor 项目

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWasm">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Sqlx" Version="1.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
    <!-- Blazor WebAssembly 通常使用 HTTP 客户端访问 API -->
  </ItemGroup>

</Project>
```

### 控制台应用

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Sqlx" Version="1.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.Data.Sqlite" Version="8.0.0" />
  </ItemGroup>

</Project>
```

### WPF 应用

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Sqlx" Version="1.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.Data.SqlClient" Version="5.1.0" />
  </ItemGroup>

</Project>
```

## 📈 性能优化配置

### 发布配置

```xml
<PropertyGroup Condition="'$(Configuration)'=='Release'">
  <!-- 启用 AOT 编译 -->
  <PublishAot>true</PublishAot>
  
  <!-- 启用裁剪 -->
  <PublishTrimmed>true</PublishTrimmed>
  
  <!-- 优化设置 -->
  <Optimize>true</Optimize>
  <DebugType>none</DebugType>
</PropertyGroup>
```

### NativeAOT 支持

```xml
<PropertyGroup>
  <PublishAot>true</PublishAot>
  <InvariantGlobalization>true</InvariantGlobalization>
</PropertyGroup>
```

## 🔄 升级指南

### 从旧版本升级

```bash
# 检查当前版本
dotnet list package | grep Sqlx

# 升级到最新版本
dotnet add package Sqlx

# 或指定特定版本
dotnet add package Sqlx --version 2.0.0
```

### 破坏性变更检查

升级前请查看 [变更日志](../CHANGELOG.md) 了解可能的破坏性变更。

## 📚 下一步

安装完成后，您可以：

1. 阅读 [快速入门指南](getting-started.md)
2. 查看 [基础示例](examples/basic-examples.md)
3. 了解 [Repository 模式](repository-pattern.md)
4. 探索 [高级功能](examples/advanced-examples.md)

## 🆘 需要帮助？

如果遇到安装问题：

1. 查看 [常见问题 FAQ](troubleshooting/faq.md)
2. 搜索 [GitHub Issues](https://github.com/Cricle/Sqlx/issues)
3. 提交新的 [问题报告](https://github.com/Cricle/Sqlx/issues/new)

---

欢迎使用 Sqlx！开始您的高性能数据访问之旅吧！ 🚀
