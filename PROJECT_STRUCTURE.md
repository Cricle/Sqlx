# Sqlx 项目结构说明

本文档详细说明了 Sqlx 项目的组织结构和各个组件的作用。

## 📁 顶级目录结构

```
Sqlx/
├── src/Sqlx/                    # 🔧 核心库 - 高性能源代码生成器
├── samples/                     # 📚 示例项目
├── tests/                       # 🧪 测试项目
├── docs/                        # 📖 文档中心
├── LICENSE.txt                  # 📄 MIT 许可证
├── README.md                    # 📋 项目主页
├── Sqlx.sln                     # 🔧 Visual Studio 解决方案
├── Directory.Build.props        # 🔧 MSBuild 全局属性
├── Directory.Packages.props     # 📦 中央包管理
├── nuget.config                 # 📦 NuGet 配置
└── stylecop.json               # 🎨 代码风格配置
```

## 🔧 核心库 (src/Sqlx/)

核心库包含 Sqlx 的所有源代码生成逻辑：

```
src/Sqlx/
├── AbstractGenerator.cs         # 抽象生成器基类
├── AbstractGenerator.AttributeHelpers.cs  # 属性处理辅助方法
├── AbstractGenerator.DocHelpers.cs        # 文档生成辅助方法
├── CSharpGenerator.cs          # C# 代码生成器主类
├── CSharpGenerator.SyntaxReceiver.cs      # 语法接收器
├── MethodGenerationContext.cs  # 方法生成上下文
├── Annotations/                # 🏷️ 注解和属性
│   ├── RepositoryForAttribute.cs
│   ├── SqlxAttribute.cs
│   ├── TableNameAttribute.cs
│   └── SqlDefineAttribute.cs
├── Core/                       # 🧠 核心功能模块
│   ├── RepositoryGenerator.cs  # Repository 生成器
│   ├── TypeAnalyzer.cs        # 类型分析器
│   ├── SqlOperationInferrer.cs # SQL 操作推断器
│   ├── CodeGenerator.cs       # 代码生成器
│   ├── RepositoryMethodGenerator.cs # Repository 方法生成器
│   ├── DatabaseDialectFactory.cs    # 数据库方言工厂
│   ├── MySqlDialectProvider.cs      # MySQL 方言提供程序
│   ├── SqlServerDialectProvider.cs  # SQL Server 方言提供程序
│   ├── PostgreSqlDialectProvider.cs # PostgreSQL 方言提供程序
│   └── SQLiteDialectProvider.cs     # SQLite 方言提供程序
└── SqlGen/                     # 🔧 SQL 生成相关
    ├── SqlDefine.cs           # SQL 方言定义
    ├── SqlDefineTypes.cs      # SQL 方言类型枚举
    └── SqlExecuteTypes.cs     # SQL 执行类型枚举
```

### 关键组件说明

#### 源代码生成器
- **AbstractGenerator.cs**: 所有生成器的基类，提供通用的生成逻辑
- **CSharpGenerator.cs**: 主要的 C# 代码生成器，处理 Repository 模式的实现
- **CSharpGenerator.SyntaxReceiver.cs**: 语法树接收器，识别需要处理的代码

#### 核心功能
- **RepositoryGenerator.cs**: 负责生成 Repository 接口的实现
- **TypeAnalyzer.cs**: 分析 .NET 类型并转换为数据库类型
- **SqlOperationInferrer.cs**: 根据方法名推断 SQL 操作类型
- **DatabaseDialectFactory.cs**: 工厂类，根据配置提供对应的数据库方言

#### 数据库方言支持
支持四种主流数据库方言：
- **MySQL**: 使用反引号包装列名，@ 参数前缀
- **SQL Server**: 使用方括号包装列名，@ 参数前缀  
- **PostgreSQL**: 使用双引号包装列名，$ 参数前缀
- **SQLite**: 使用方括号包装列名，@ 参数前缀

## 📚 示例项目 (samples/)

```
samples/
└── RepositoryExample/          # Repository 模式完整示例
    ├── Program.cs             # 主程序入口
    ├── Models/                # 数据模型
    │   ├── User.cs
    │   └── Product.cs
    ├── Services/              # 服务接口
    │   ├── IUserService.cs
    │   └── IProductService.cs
    ├── Repositories/          # Repository 实现
    │   ├── UserRepository.cs
    │   └── ProductRepository.cs
    └── RepositoryExample.csproj
```

### 示例功能
- 完整的 Repository 模式演示
- 多数据库支持示例（MySQL、SQL Server、PostgreSQL、SQLite）
- CRUD 操作演示
- 事务处理示例
- 异步操作示例

## 🧪 测试项目 (tests/)

```
tests/
├── Sqlx.Tests/                # 单元测试
│   ├── CodeGenerationTestBase.cs    # 测试基类
│   ├── SqlDefineTests.cs           # SQL 方言测试
│   ├── RepositoryGenerationTests.cs # Repository 生成测试
│   └── Sqlx.Tests.csproj
└── Sqlx.IntegrationTests/     # 集成测试
    ├── DatabaseIntegrationTests.cs  # 数据库集成测试
    ├── MySqlIntegrationTests.cs    # MySQL 专项测试
    ├── SqlServerIntegrationTests.cs # SQL Server 专项测试
    ├── PostgreSqlIntegrationTests.cs # PostgreSQL 专项测试
    ├── SQLiteIntegrationTests.cs   # SQLite 专项测试
    └── Sqlx.IntegrationTests.csproj
```

### 测试覆盖
- **单元测试**: 测试源代码生成逻辑的正确性
- **集成测试**: 测试与真实数据库的交互
- **性能测试**: 验证性能优化的效果
- **方言测试**: 确保各数据库方言的正确性

## 📖 文档中心 (docs/)

```
docs/
├── README.md                   # 文档导航
├── getting-started.md          # 快速入门指南
├── installation.md             # 安装指南  
├── repository-pattern.md       # Repository 模式指南
├── sqldefine-tablename.md      # 数据库方言和表名配置
├── expression-to-sql.md        # LINQ 表达式转 SQL
├── BEST_PRACTICES.md          # 最佳实践指南
├── OPTIMIZATION_GUIDE.md      # 性能优化指南
├── PERFORMANCE_GUIDE.md       # 性能与基准参考
├── TESTING_GUIDE.md           # 测试指南
├── RELEASE_CHECKLIST.md       # 发布检查清单
├── api/                       # API 参考文档
│   └── attributes.md          # 属性参考
├── examples/                  # 示例和教程
│   └── basic-examples.md      # 基础示例
└── troubleshooting/           # 故障排除
    └── faq.md                 # 常见问题
```

### 文档类型
- **用户指南**: 面向开发者的使用说明
- **API 参考**: 详细的 API 文档
- **最佳实践**: 代码质量和性能优化建议
- **故障排除**: 常见问题和解决方案

## 🔧 配置文件

### Directory.Build.props
全局 MSBuild 属性，统一配置：
- 目标框架版本
- C# 语言版本
- 可空引用类型
- 警告处理

### Directory.Packages.props  
中央包管理，统一控制：
- NuGet 包版本
- 分析器包
- 测试框架包

### nuget.config
NuGet 配置：
- 包源配置
- 包还原设置

## 🚀 构建和部署

### 本地开发
```bash
# 克隆项目
git clone https://github.com/Cricle/Sqlx.git
cd Sqlx

# 还原依赖
dotnet restore

# 构建项目
dotnet build

# 运行测试
dotnet test

# 运行示例
dotnet run --project samples/RepositoryExample/RepositoryExample.csproj
```

### CI/CD 流程
项目使用 GitHub Actions 进行持续集成：
- 自动构建和测试
- 多平台兼容性测试
- NuGet 包自动发布
- 性能回归测试

## 📦 包发布

### NuGet 包结构
```
Sqlx.nupkg
├── lib/
│   └── netstandard2.0/
│       └── Sqlx.dll
├── analyzers/
│   └── dotnet/
│       └── cs/
│           └── Sqlx.dll
└── build/
    └── Sqlx.props
```

### 版本管理
- 遵循语义化版本控制 (SemVer)
- 自动化版本号生成
- 发布说明自动生成

---

**需要更多信息？** 查看 [docs/](docs/) 目录中的详细文档，或在 [GitHub Issues](https://github.com/Cricle/Sqlx/issues) 中提问。
