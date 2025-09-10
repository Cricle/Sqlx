# Sqlx 项目结构

## 📁 项目概览

```
Sqlx/
├── src/Sqlx/                    # 🔧 核心库
│   ├── Core/                    # 核心功能模块
│   ├── Annotations/             # 特性标注
│   ├── SqlGen/                  # SQL生成器
│   └── ...                      # 其他核心文件
├── samples/                     # 📚 示例项目
│   ├── RepositoryExample/       # Repository模式完整示例
│   ├── ComprehensiveDemo/       # 综合功能演示
│   ├── PerformanceBenchmark/    # 性能基准测试
│   ├── CompilationTests/        # 编译测试
│   └── RepositoryExample.Tests/ # 示例测试项目
├── tests/                       # 🧪 单元测试
│   ├── Sqlx.Tests/             # 核心测试套件
│   └── Sqlx.IntegrationTests/  # 集成测试
├── tools/                       # 🛠️ 开发工具
│   ├── SqlxMigration/          # 迁移工具
│   └── SqlxPerformanceAnalyzer/ # 性能分析工具
├── extensions/                  # 🎨 IDE扩展
│   └── Sqlx.VisualStudio/      # Visual Studio扩展
└── docs/                        # 📖 文档
    ├── api/                     # API参考
    ├── examples/                # 示例文档
    ├── databases/               # 数据库支持文档
    └── troubleshooting/         # 故障排除
```

## 🎯 核心项目

### `src/Sqlx/` - 核心库
- **作用**: Sqlx的核心功能实现
- **内容**: 源代码生成器、特性标注、SQL生成等
- **目标**: NuGet包发布

## 📚 示例项目

### `samples/RepositoryExample/` - Repository模式示例
- **作用**: 完整的Repository模式演示
- **特色**: 
  - 真实数据库操作
  - SQLite和SQL Server支持
  - 性能对比演示
  - 泛型Repository支持

### `samples/ComprehensiveDemo/` - 综合功能演示
- **作用**: 展示Sqlx的所有企业级功能
- **特色**:
  - 智能缓存系统
  - 高级连接管理
  - 批量操作演示
  - 拦截器使用

### `samples/PerformanceBenchmark/` - 性能基准测试
- **作用**: 性能测试和基准对比
- **特色**:
  - 原始性能测试
  - 缓存效果验证
  - 内存使用分析

### `samples/CompilationTests/` - 编译测试
- **作用**: 验证代码生成和编译功能
- **特色**:
  - Entity Framework集成测试
  - 扩展方法测试
  - 连接管理测试

## 🧪 测试项目

### `tests/Sqlx.Tests/` - 核心测试套件
- **作用**: 核心功能的单元测试
- **覆盖**: 源代码生成、SQL生成、类型映射等

### `samples/RepositoryExample.Tests/` - 示例测试
- **作用**: Repository示例的集成测试
- **特色**: 真实数据库测试场景

## 🛠️ 开发工具

### `tools/SqlxMigration/` - 迁移工具
- **作用**: 从Dapper/EF Core迁移到Sqlx
- **功能**:
  - 代码分析
  - 自动迁移
  - 验证工具
  - Repository生成

### `tools/SqlxPerformanceAnalyzer/` - 性能分析工具
- **作用**: SQL查询性能监控和优化
- **功能**:
  - 实时性能分析
  - 慢查询识别
  - 基准测试
  - 持续监控

## 🎨 IDE扩展

### `extensions/Sqlx.VisualStudio/` - Visual Studio扩展
- **作用**: 提供IDE内的开发支持
- **功能**:
  - SQL语法高亮
  - IntelliSense支持
  - 代码生成工具
  - 诊断分析

## 📖 文档结构

### 核心文档
- `getting-started.md` - 快速入门指南
- `installation.md` - 安装配置指南
- `repository-pattern.md` - Repository模式详解
- `expression-to-sql.md` - ExpressionToSql功能
- `sqldefine-tablename.md` - 多数据库支持

### API参考
- `api/attributes.md` - 完整属性参考

### 示例文档
- `examples/basic-examples.md` - 基础示例

### 故障排除
- `troubleshooting/faq.md` - 常见问题解答

## 🚀 快速开始

1. **安装核心库**:
   ```bash
   dotnet add package Sqlx
   ```

2. **运行示例**:
   ```bash
   # Repository模式示例
   dotnet run --project samples/RepositoryExample
   
   # 综合功能演示
   dotnet run --project samples/ComprehensiveDemo
   
   # 性能基准测试
   dotnet run --project samples/PerformanceBenchmark
   ```

3. **查看文档**:
   - 阅读 `docs/getting-started.md`
   - 参考 `docs/repository-pattern.md`

## 🔧 开发和贡献

1. **构建项目**:
   ```bash
   dotnet build
   ```

2. **运行测试**:
   ```bash
   dotnet test
   ```

3. **使用工具**:
   ```bash
   # 安装迁移工具
   dotnet tool install --global Sqlx.Migration.Tool
   
   # 安装性能分析工具
   dotnet tool install --global Sqlx.Performance.Analyzer
   ```

## 📊 项目统计

- ✅ **核心库**: 1个项目，完整功能
- 📚 **示例项目**: 4个项目，涵盖所有主要功能
- 🧪 **测试项目**: 2个项目，高覆盖率
- 🛠️ **开发工具**: 2个工具，企业级功能
- 🎨 **IDE扩展**: 1个扩展，开发体验优化
- 📖 **文档**: 完整的使用指南和API参考

---

**Sqlx** - 高性能、类型安全的 .NET 数据库访问库！ ⚡


