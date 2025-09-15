# Sqlx 项目结构

本文档描述了 Sqlx 项目的组织结构和文件布局。

## 目录结构

```
Sqlx/
├── src/                           # 源代码
│   ├── Sqlx/                      # 主库项目
│   │   ├── Annotations/           # 特性和注解
│   │   ├── ExpressionToSql*.cs    # 表达式转SQL核心功能
│   │   ├── SqlDefine.cs           # SQL方言定义
│   │   ├── SqlTemplate.cs         # SQL模板功能
│   │   └── ValueStringBuilder.cs  # 性能优化的字符串构建器
│   └── Sqlx.Generator/            # 源码生成器
│       ├── Core/                  # 生成器核心逻辑
│       ├── CSharpGenerator.cs     # C#代码生成
│       └── SqlGen/                # SQL生成相关
│
├── tests/                         # 测试项目
│   └── Sqlx.Tests/               # 单元测试和集成测试
│       ├── Core/                  # 核心功能测试
│       ├── Benchmarks/            # 性能基准测试
│       ├── Integration/           # 集成测试
│       └── Performance/           # 性能测试
│
├── SqlxCompleteDemo/              # 完整功能演示项目
│   ├── Program.cs                 # 主程序
│   ├── SimpleDemo.cs              # 简单用法演示
│   └── README.md                  # 演示说明
│
├── docs/                          # 文档
│   ├── ADVANCED_FEATURES_GUIDE.md # 高级功能指南
│   ├── MIGRATION_GUIDE.md         # 迁移指南
│   ├── PROJECT_STATUS.md          # 项目状态
│   └── expression-to-sql.md       # 表达式转SQL文档
│
├── scripts/                       # 构建和开发脚本
│   ├── build.ps1                  # 构建脚本
│   ├── quick-test.ps1             # 快速测试脚本
│   └── setup-dev.ps1              # 开发环境设置
│
└── artifacts/                     # 构建产物
    └── *.nupkg                    # NuGet包
```

## 解决方案结构

### 核心项目
- **Sqlx** - 主要的库项目，包含表达式转SQL的核心功能
- **Sqlx.Generator** - Roslyn源码生成器，用于自动生成数据访问代码

### 测试项目
- **Sqlx.Tests** - 包含所有单元测试、集成测试和性能测试

### 示例项目
- **SqlxCompleteDemo** - 完整的功能演示，展示所有主要特性的使用方法

### 解决方案文件夹
- **Tests** - 包含所有测试相关项目
- **Samples** - 包含示例和演示项目
- **Solution Items** - 解决方案级别的配置文件
- **Documentation** - 文档文件
- **Scripts** - 构建和开发脚本

## 构建配置

- 使用 `Directory.Build.props` 进行统一的项目配置
- 使用 `Directory.Packages.props` 进行中央包版本管理
- 支持 Debug 和 Release 配置
- 目标框架：.NET Standard 2.0 (库项目)，.NET 9.0 (演示和测试项目)

## 开发工作流

1. **构建**: `dotnet build`
2. **测试**: `dotnet test`
3. **运行演示**: `dotnet run --project SqlxCompleteDemo`
4. **打包**: 使用 `scripts/build.ps1`

## 主要功能模块

### ExpressionToSql
- **ExpressionToSql.cs** - 主要的表达式转SQL类
- **ExpressionToSqlBase.cs** - 基础功能和共享逻辑
- **ExpressionToSql.Create.cs** - 工厂方法和创建逻辑

### SQL方言支持
- **SqlDefine.cs** - 数据库方言定义和配置
- 支持 SQL Server、MySQL、PostgreSQL、Oracle、SQLite、DB2

### 代码生成
- **CSharpGenerator.cs** - 主要的C#代码生成器
- **Core/** - 类型分析、语法处理等核心逻辑
- **SqlGen/** - SQL相关的代码生成

这个结构确保了项目的清晰组织、易于维护和扩展。
