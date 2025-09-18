# Sqlx 项目结构说明

本文档详细说明了 Sqlx ORM 框架的项目组织结构、各组件职责以及相互关系。

## 📁 整体项目结构

```
Sqlx/
├── 📂 src/                          # 源代码目录
│   ├── 📦 Sqlx/                     # 核心运行时库
│   │   ├── 🔧 Annotations/          # 特性和注解定义
│   │   ├── 🔄 ExpressionToSql*      # LINQ 表达式转 SQL
│   │   ├── 📋 SqlTemplate*          # 革新的模板引擎
│   │   ├── ⚡ ParameterizedSql.cs   # 参数化 SQL 执行实例
│   │   ├── 🌐 SqlDefine.cs          # 数据库方言定义
│   │   └── 🛠️ ValueStringBuilder.cs # 高性能字符串构建器
│   └── 📦 Sqlx.Generator/           # 源生成器
│       ├── 🧠 Core/                 # 核心生成逻辑
│       ├── 🏗️ AbstractGenerator/    # 生成器基类和接口
│       ├── 💻 CSharpGenerator/       # C# 代码生成器
│       └── 🔍 SqlGen/               # SQL 生成相关工具
├── 📂 samples/                      # 示例和演示
│   └── 📝 SqlxDemo/                 # 完整功能演示项目
├── 📂 tests/                        # 测试套件
│   └── 🧪 Sqlx.Tests/               # 单元测试和集成测试
├── 📂 docs/                         # 文档中心
├── 📂 scripts/                      # 构建和开发脚本
└── 📋 配置文件                       # 项目配置
    ├── Directory.Build.props        # 全局构建属性
    ├── Directory.Packages.props     # 中央包管理
    ├── Sqlx.sln                     # 解决方案文件
    └── stylecop.json               # 代码样式规则
```

## 🧩 核心组件详解

### 📦 Sqlx (运行时库)

**职责**: 提供运行时所需的所有核心功能和类型定义

#### 🔧 Annotations 目录
```
Annotations/
├── SqlExecuteTypeAttribute.cs      # SQL 操作类型特性
├── SqlxAttribute.cs                # 主要的 Sqlx 标记特性
├── ExpressionToSqlAttribute.cs     # 表达式转 SQL 特性
├── RepositoryForAttribute.cs       # Repository 自动生成特性
├── SqlTemplateAttribute.cs         # SQL 模板特性
├── AnyPlaceholderAttribute.cs      # 任意占位符特性
└── RequiredMembersAttribute.cs     # 必需成员特性（兼容性）
```

**设计原则**:
- 纯声明式特性，无复杂逻辑
- 源生成器驱动的开发模式
- 编译时验证和代码生成

#### 🔄 ExpressionToSql 组件
```
ExpressionToSql.cs                  # 主要的表达式转换器
ExpressionToSql.Create.cs           # 创建和配置方法
ExpressionToSqlBase.cs              # 基础转换逻辑
```

**核心功能**:
- LINQ 表达式到 SQL 的类型安全转换
- 多数据库方言支持
- 动态查询构建
- 与 SqlTemplate 的无缝集成

#### 📋 SqlTemplate 组件（革新设计）
```
SqlTemplate.cs                      # 纯模板定义核心类
SqlTemplateAdvanced.cs              # 高级模板功能（条件、循环）
SqlTemplateEnhanced.cs              # 增强功能和扩展方法
SqlTemplateExpressionBridge.cs      # 与 ExpressionToSql 集成
ParameterizedSql.cs                 # 参数化 SQL 执行实例
```

**设计突破**:
- **纯模板设计**: 模板定义与参数值完全分离
- **高性能重用**: 一个模板可多次执行，节省 33% 内存
- **类型安全**: 编译时检查，AOT 友好
- **无缝集成**: 与 ExpressionToSql 完美融合

### 📦 Sqlx.Generator (源生成器)

**职责**: 编译时代码生成，将特性标记的方法转换为高性能实现

#### 🧠 Core 目录
```
Core/
├── AbstractGenerator.cs            # 生成器基类
├── CSharpGenerator.cs              # C# 代码生成器
├── SqlGeneration/                  # SQL 生成相关
├── DiagnosticService.cs            # 诊断和错误报告
└── MethodGenerationContext.cs     # 方法生成上下文
```

**核心职责**:
- 解析特性标记的方法
- 生成高性能、零反射的数据访问代码
- 编译时错误检查和诊断
- 支持 Primary Constructor 和 Record 类型

#### 🏗️ 代码生成流程
```
特性解析 → AST 分析 → SQL 生成 → C# 代码生成 → 编译集成
     ↓           ↓          ↓           ↓            ↓
   语法检查   类型推断   方言适配   代码优化    错误报告
```

## 📋 配置文件体系

### Directory.Build.props (全局配置)
```xml
<!-- 语言和框架设置 -->
<LangVersion>12.0</LangVersion>
<Nullable>enable</Nullable>

<!-- AOT 支持 -->
<IsAotCompatible>true</IsAotCompatible>
<EnableTrimAnalyzer>true</EnableTrimAnalyzer>

<!-- 包元数据 -->
<Company>Sqlx Team</Company>
<PackageLicenseExpression>MIT</PackageLicenseExpression>
```

### Directory.Packages.props (依赖管理)
```xml
<!-- 中央包版本管理 -->
<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>

<!-- 按类别组织的包版本 -->
<ItemGroup Label="Runtime">
<ItemGroup Label="Compiler and Analyzers">
<ItemGroup Label="Testing">
```

## 🧪 测试体系

### 测试组织结构
```
tests/Sqlx.Tests/
├── 🧪 Core/                        # 核心功能测试
│   ├── SqlTemplateTests.cs         # 基础模板测试
│   ├── SqlTemplateNewDesignTests.cs # 新设计专项测试
│   ├── ExpressionToSqlTests.cs     # 表达式转换测试
│   └── PerformanceTests.cs         # 性能基准测试
├── 🔗 Integration/                 # 集成测试
│   ├── SeamlessIntegrationTests.cs # 无缝集成测试
│   └── DatabaseTests.cs            # 数据库集成测试
├── 🛠️ Generator/                   # 源生成器测试
│   ├── CodeGenerationTests.cs      # 代码生成测试
│   └── DiagnosticTests.cs          # 诊断功能测试
└── 📊 TestResults/                 # 测试结果和覆盖率
```

### 测试覆盖率目标
- **整体覆盖率**: > 95%
- **核心功能**: 100%
- **边界情况**: 完整覆盖
- **性能回归**: 自动化基准测试

## 📝 示例项目

### SqlxDemo 功能演示
```
samples/SqlxDemo/
├── 📱 Program.cs                   # 主程序入口
├── 🏗️ Models/                      # 数据模型定义
│   ├── User.cs                     # Record 类型示例
│   ├── Department.cs               # Primary Constructor 示例
│   └── Product.cs                  # 传统类示例
├── 🔧 Services/                    # 服务层演示
│   ├── SqlTemplateBestPracticesDemo.cs    # 最佳实践
│   ├── SeamlessIntegrationDemo.cs         # 无缝集成
│   ├── SqlTemplateRefactoredDemo.cs       # 重构演示
│   └── AdvancedFeaturesDemo.cs            # 高级特性
└── 📋 README.md                    # 示例说明文档
```

## 📚 文档结构

### 分层文档体系
```
docs/
├── 🚀 快速开始
│   ├── README.md                   # 文档导航中心
│   └── NEW_FEATURES_QUICK_START.md # 快速特性指南
├── 🏗️ 核心功能
│   ├── SQL_TEMPLATE_GUIDE.md       # 模板引擎指南
│   ├── expression-to-sql.md        # ExpressionToSql 指南
│   └── SEAMLESS_INTEGRATION_GUIDE.md # 无缝集成指南
├── 🆕 最新特性
│   ├── SQLTEMPLATE_DESIGN_FIXED.md # 模板设计革新
│   └── PRIMARY_CONSTRUCTOR_RECORD_SUPPORT.md # 现代 C# 支持
├── 🔧 高级主题
│   ├── ADVANCED_FEATURES_GUIDE.md  # 高级特性指南
│   ├── OPTIMIZATION_ROADMAP.md     # 性能优化指南
│   └── DiagnosticGuidance.md       # 诊断和指导
└── 📖 参考手册
    ├── SQLX_COMPLETE_FEATURE_GUIDE.md # 完整特性参考
    └── MIGRATION_GUIDE.md          # 迁移指南
```

## 🔄 开发工作流

### 开发环境设置
```bash
# 1. 克隆项目
git clone https://github.com/your-org/Sqlx.git

# 2. 还原依赖
dotnet restore

# 3. 构建项目
dotnet build

# 4. 运行测试
dotnet test

# 5. 运行示例
cd samples/SqlxDemo
dotnet run
```

### 构建脚本
```
scripts/
├── build.ps1                       # 完整构建脚本
├── quick-test.ps1                  # 快速测试脚本
└── setup-dev.ps1                   # 开发环境设置
```

## 🎯 设计原则

### 1. 分离关注点
- **运行时** (Sqlx): 纯运行时逻辑，无编译时依赖
- **编译时** (Sqlx.Generator): 源生成和静态分析
- **示例** (SqlxDemo): 功能演示和最佳实践

### 2. 性能优先
- **零反射**: 编译时生成，运行时直接调用
- **内存效率**: 对象重用，减少分配
- **AOT 兼容**: 支持原生编译

### 3. 开发体验
- **类型安全**: 编译时检查，智能提示
- **错误友好**: 清晰的错误信息和建议
- **文档完整**: 从快速开始到高级特性

### 4. 可扩展性
- **插件架构**: 支持自定义生成器
- **多数据库**: 统一接口，特定优化
- **向前兼容**: 平滑升级路径

---

这个项目结构体现了现代 .NET 开发的最佳实践，通过清晰的职责分离和强大的工具链支持，为开发者提供了高性能、类型安全且易于使用的 ORM 解决方案。