# Sqlx 项目结构

📁 本文档描述了 Sqlx 项目的完整目录结构和组织方式。

## 🏗️ 整体结构

```
Sqlx/
├── 📂 src/                          # 源代码
│   └── Sqlx/                        # 核心库
├── 📂 samples/                      # 示例项目
│   ├── ComprehensiveExample/        # 综合功能演示 ✅
│   ├── GettingStarted/             # 快速入门 ✅
│   ├── PrimaryConstructorExample/   # 主构造函数支持 ✅
│   ├── NewFeatures/                # 新功能演示 ✅
│   ├── SimpleExample/              # 简单示例 🔧
│   └── RealWorldExample/           # 真实世界示例 🔧
├── 📂 tests/                       # 测试项目
│   ├── Sqlx.Tests/                 # 单元测试 ✅
│   └── Sqlx.PerformanceTests/      # 性能测试 🔧
├── 📂 docs/                        # 文档
│   ├── archive/                    # 归档文档
│   ├── development/                # 开发文档
│   ├── contributing/               # 贡献指南
│   ├── databases/                  # 数据库相关文档
│   └── reports/                    # 报告
├── 📂 scripts/                     # 构建脚本
├── 📂 artifacts/                   # 构建产物
└── 📄 配置文件                     # 项目配置
```

## 📋 项目状态图例

- ✅ **完全正常**: 编译成功，功能完整
- 🔧 **需要修复**: 有编译错误或功能问题
- 📝 **文档完整**: 包含完整的README和示例
- 🚀 **已优化**: 项目结构已标准化

## 🎯 核心组件

### 📦 src/Sqlx/ - 核心库
- **状态**: ✅ 完全正常
- **描述**: Sqlx 的核心功能实现
- **包含**: 源代码生成器、核心API、扩展方法

### 🎓 samples/ - 示例项目

#### ComprehensiveExample ✅📝🚀
- **功能**: 展示 Sqlx 的所有主要功能
- **特色**: CRUD操作、复杂查询、Department管理
- **文档**: 完整的README和代码注释

#### GettingStarted ✅📝🚀  
- **功能**: 快速入门教程
- **特色**: 基础CRUD、简单查询
- **适合**: 初学者和快速上手

#### PrimaryConstructorExample ✅📝🚀
- **功能**: 现代C#语法支持演示
- **特色**: Records、Primary Constructors、C# 12特性
- **技术**: 展示最新语言特性的兼容性

#### NewFeatures ✅📝🚀
- **功能**: 新功能演示集合
- **包含**: 批量操作、属性映射、综合示例
- **结构**: 新增的完整项目结构

#### SimpleExample 🔧
- **状态**: 需要修复编译错误
- **问题**: 源代码生成器相关错误

#### RealWorldExample 🔧
- **状态**: 需要修复编译错误  
- **问题**: 分部方法实现缺失

### 🧪 tests/ - 测试项目

#### Sqlx.Tests ✅
- **状态**: 编译成功，支持 .NET 6.0 和 8.0
- **覆盖**: 核心功能的全面测试

#### Sqlx.PerformanceTests 🔧
- **状态**: 需要修复重复定义错误
- **用途**: 性能基准测试

## 📚 文档结构

### docs/ 目录组织

```
docs/
├── archive/                 # 项目归档文档
│   ├── FEATURE_SUMMARY.md
│   ├── FINAL_*.md           # 项目完成文档
│   └── VERSION_INFO.md      
├── development/             # 开发相关文档
│   ├── NUGET_RELEASE_CHECKLIST.md
│   ├── RELEASE_NOTES.md
│   └── GETTING_STARTED_DBBATCH.md
├── contributing/            # 贡献指南
├── databases/              # 数据库文档
└── reports/                # 各种报告
```

## 🛠️ 配置文件

- **Sqlx.sln**: 解决方案文件，包含所有项目
- **Directory.Build.props**: 全局构建属性
- **Directory.Packages.props**: 中央包管理
- **nuget.config**: NuGet配置
- **stylecop.json**: 代码风格配置

## 🚀 构建和部署

### artifacts/ 目录
- **用途**: 存放构建产物、NuGet包、测试结果
- **内容**: 
  - NuGet包 (.nupkg, .snupkg)
  - 测试结果文件
  - 临时数据库文件

### scripts/ 目录
- **build.ps1**: 主构建脚本
- **quick-test.ps1**: 快速测试脚本
- **setup-dev.ps1**: 开发环境设置

## 📈 项目改进计划

### 已完成 ✅
1. 清理根目录文档混乱
2. 标准化示例项目结构
3. 修复解决方案文件配置
4. 完善工作示例的文档

### 待完成 🔧
1. 修复 SimpleExample 和 RealWorldExample
2. 解决 PerformanceTests 的重复定义问题
3. 统一所有项目的配置标准
4. 完善CI/CD配置

## 💡 最佳实践

### 示例项目标准
- 独立的项目文件 (.csproj)
- 完整的 README.md 文档
- 标准化的项目配置
- 清晰的代码结构和注释

### 文档组织
- 按用途分类存放
- 保持根目录整洁
- 归档过时文档
- 维护最新的结构说明

---

🎯 **目标**: 创建一个清晰、可维护、易于理解的项目结构，方便开发者快速上手和贡献代码。

