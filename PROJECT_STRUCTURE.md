# Sqlx 项目结构

## 📁 项目组织

```
Sqlx/
├── src/                          # 源代码
│   ├── Sqlx/                     # 核心库
│   │   ├── Annotations/          # 特性定义
│   │   │   ├── ExpressionToSqlAttribute.cs
│   │   │   ├── RepositoryForAttribute.cs
│   │   │   ├── SqlDefineAttribute.cs
│   │   │   ├── SqlTemplateAttribute.cs
│   │   │   ├── SqlxAttribute.cs
│   │   │   └── TableNameAttribute.cs
│   │   ├── ExpressionToSql.cs
│   │   ├── ExpressionToSqlBase.cs
│   │   ├── ParameterizedSql.cs
│   │   ├── SqlDefine.cs
│   │   └── SqlTemplate.cs
│   │
│   └── Sqlx.Generator/           # 源代码生成器
│       ├── Analyzers/            # Roslyn分析器
│       │   ├── PropertyOrderAnalyzer.cs
│       │   └── PropertyOrderCodeFixProvider.cs
│       ├── Core/                 # 核心代码生成逻辑
│       │   ├── CodeGenerationService.cs
│       │   ├── SharedCodeGenerationUtilities.cs
│       │   └── ... (20个核心文件)
│       ├── SqlGen/               # SQL生成相关
│       └── CSharpGenerator.cs    # 主生成器
│
├── tests/                        # 测试项目
│   ├── Sqlx.Tests/               # 单元测试 (474个测试)
│   │   ├── Core/                 # 核心功能测试 (34个文件)
│   │   ├── Integration/          # 集成测试
│   │   ├── Performance/          # 性能测试
│   │   └── ...
│   ├── Sqlx.Benchmarks/          # 性能基准测试
│   │   ├── Benchmarks/
│   │   │   ├── TracingOverheadBenchmark.cs
│   │   │   ├── QueryBenchmark.cs
│   │   │   ├── ComplexQueryBenchmark.cs
│   │   │   └── CrudBenchmark.cs
│   │   ├── Models/
│   │   └── Repositories/
│   └── TEST_SUMMARY.md           # 测试覆盖率总结
│
├── samples/                      # 示例项目
│   └── TodoWebApi/               # 完整的Todo Web API示例
│       ├── Controllers/
│       ├── Models/
│       ├── Services/
│       ├── wwwroot/              # 前端资源
│       └── README.md
│
├── docs/                         # 文档
│   ├── web/                      # GitHub Pages
│   │   └── index.html
│   ├── README.md                 # 文档中心
│   ├── QUICK_START_GUIDE.md      # 快速开始
│   ├── QUICK_REFERENCE.md        # 快速参考
│   ├── PLACEHOLDERS.md           # 占位符参考
│   ├── BEST_PRACTICES.md         # 最佳实践
│   ├── ADVANCED_FEATURES.md      # 高级特性
│   ├── API_REFERENCE.md          # API参考
│   ├── PARTIAL_METHODS_GUIDE.md  # Partial方法指南
│   ├── MULTI_DATABASE_TEMPLATE_ENGINE.md  # 多数据库支持
│   ├── FRAMEWORK_COMPATIBILITY.md # 框架兼容性
│   ├── MIGRATION_GUIDE.md        # 迁移指南
│   └── CHANGELOG.md              # 变更日志
│
├── scripts/                      # 脚本工具
│   ├── build.ps1                 # 构建脚本
│   └── README.md
│
├── README.md                     # 项目主README
├── FORCED_TRACING_SUMMARY.md     # 性能优化总结
├── LICENSE.txt                   # MIT许可证
├── Sqlx.sln                      # Visual Studio解决方案
├── Directory.Build.props         # 全局MSBuild属性
├── Directory.Packages.props      # 中央包管理
└── stylecop.json                 # 代码风格配置
```

---

## 🎯 核心项目说明

### 1. Sqlx (核心库)

**目标框架**: `net9.0`, `net8.0`, `net6.0`, `netstandard2.0`

**主要功能**:
- 表达式转SQL
- SQL模板引擎
- 参数化SQL
- 数据库方言支持
- 特性定义

**关键文件**:
- `ExpressionToSql.cs` - LINQ表达式转SQL实现
- `SqlTemplate.cs` - SQL模板引擎
- `SqlDefine.cs` - SQL方言定义
- `Annotations/` - 所有特性定义

---

### 2. Sqlx.Generator (源代码生成器)

**目标框架**: `netstandard2.0`

**主要功能**:
- 编译时代码生成
- Repository方法实现
- SQL占位符处理
- Activity追踪代码生成
- Partial方法生成
- Roslyn分析器和代码修复

**关键目录**:
- `Core/` - 代码生成核心逻辑
- `Analyzers/` - PropertyOrderAnalyzer (SQLX001)
- `SqlGen/` - SQL生成相关

**性能优化特性**:
- ✅ 硬编码索引访问 (`reader.GetInt32(0)`)
- ✅ 智能IsDBNull检查 (只对nullable类型)
- ✅ 命令自动释放 (finally块)
- ✅ Activity追踪和指标
- ✅ Partial方法拦截点
- ✅ 条件编译支持

---

### 3. Sqlx.Tests (单元测试)

**测试框架**: MSTest  
**总测试数**: 474个  
**通过率**: 100%

**测试覆盖**:
- **Core/** (34个文件) - 核心功能测试
  - 代码生成测试
  - 占位符系统测试
  - 多数据库支持测试
  - 性能优化验证
  - 边界测试

- **Integration/** - 集成测试
- **Performance/** - 性能测试
- **Comprehensive/** - 全面测试
- **Simplified/** - 简化测试

详细信息见: `tests/TEST_SUMMARY.md`

---

### 4. Sqlx.Benchmarks (性能基准测试)

**框架**: BenchmarkDotNet

**基准测试**:
- `TracingOverheadBenchmark.cs` - 追踪开销测试
- `QueryBenchmark.cs` - 查询性能测试
- `ComplexQueryBenchmark.cs` - 复杂查询测试
- `CrudBenchmark.cs` - CRUD操作测试

**性能对比**:
- Raw ADO.NET: 6.434 μs
- **Sqlx: 7.371 μs** (比Dapper快20%)
- Dapper: 9.241 μs

详细信息见: `FORCED_TRACING_SUMMARY.md`

---

### 5. 示例项目

#### TodoWebApi
完整的Todo管理Web API，展示:
- 完整的RESTful API
- CRUD操作
- 所有占位符使用
- Activity追踪
- Partial方法自定义拦截
- 多种查询场景
- 批量操作示例

---

## 📚 文档组织

### 新手入门
1. `README.md` - 项目主页
2. `docs/QUICK_START_GUIDE.md` - 5分钟快速开始
3. `docs/QUICK_REFERENCE.md` - 一页纸速查表
4. `samples/TodoWebApi/` - 完整示例

### 核心文档
- `docs/PLACEHOLDERS.md` - 占位符完整参考
- `docs/BEST_PRACTICES.md` - 最佳实践
- `docs/API_REFERENCE.md` - API参考
- `docs/PARTIAL_METHODS_GUIDE.md` - Partial方法详解

### 高级主题
- `docs/ADVANCED_FEATURES.md` - 高级特性
- `docs/MULTI_DATABASE_TEMPLATE_ENGINE.md` - 多数据库支持
- `docs/FRAMEWORK_COMPATIBILITY.md` - 框架兼容性
- `docs/MIGRATION_GUIDE.md` - 迁移指南

### 性能和测试
- `FORCED_TRACING_SUMMARY.md` - 性能优化总结
- `tests/TEST_SUMMARY.md` - 测试覆盖率报告
- `tests/Sqlx.Benchmarks/README.md` - Benchmark指南

---

## 🔧 构建和测试

### 编译整个解决方案
```bash
dotnet build Sqlx.sln
```

### 运行所有测试
```bash
dotnet test Sqlx.sln
```

### 运行Benchmark
```bash
cd tests/Sqlx.Benchmarks
dotnet run -c Release
```

### 打包NuGet
```bash
dotnet pack src/Sqlx/Sqlx.csproj -c Release
dotnet pack src/Sqlx.Generator/Sqlx.Generator.csproj -c Release
```

---

## 📦 NuGet包结构

### Sqlx
- **PackageId**: Sqlx
- **包含**: 核心库、特性定义
- **依赖**: 无

### Sqlx.Generator
- **PackageId**: Sqlx.Generator
- **包含**: 源代码生成器、Roslyn分析器
- **依赖**: Microsoft.CodeAnalysis.CSharp

---

## 🎯 项目特点

### 架构设计
- ✅ 清晰的分层架构
- ✅ 核心库与生成器分离
- ✅ 完善的测试覆盖
- ✅ 丰富的文档和示例

### 代码质量
- ✅ 474个单元测试 (100%通过)
- ✅ StyleCop代码风格检查
- ✅ Roslyn分析器 (SQLX001)
- ✅ 完整的XML文档注释

### 性能
- ✅ 比Dapper快20%
- ✅ 比Dapper少46%内存分配
- ✅ 零反射开销
- ✅ 完整的Activity追踪

---

## 📖 相关链接

- [GitHub仓库](https://github.com/Cricle/Sqlx)
- [NuGet包](https://www.nuget.org/packages/Sqlx)
- [在线文档](https://cricle.github.io/Sqlx/)
- [问题反馈](https://github.com/Cricle/Sqlx/issues)

---

**最后更新**: 2025-10-22  
**版本**: 1.0.0

