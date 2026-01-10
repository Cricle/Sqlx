# CI/CD 工作流说明

## 工作流概览

### 1. CI/CD Pipeline (`ci-cd.yml`)
**触发条件**: Push 到 main/develop 分支，PR 到 main 分支，创建 v* tag

**测试策略**:
- **单元测试** (10分钟): 快速测试，不依赖真实数据库，使用 SQLite 内存数据库
  - 过滤器: `TestCategory!=Integration&TestCategory!=E2E&TestCategory!=CI`
  
- **集成测试** (15分钟): SQLite 集成测试
  - 过滤器: `TestCategory=Integration&TestCategory!=PostgreSQL&TestCategory!=MySQL&TestCategory!=SqlServer`
  
- **多方言测试** (10分钟): 仅在 push 到 main 分支时运行
  - 过滤器: `TestCategory=CI`
  - 包含 PostgreSQL, MySQL, SQL Server 的关键测试

**优化点**:
- 分离快速和慢速测试
- PR 只运行单元测试和 SQLite 集成测试
- 多方言测试只在 main 分支运行
- 数据库初始化只在需要时执行

### 2. Full Multi-Dialect Tests (`full-test.yml`)
**触发条件**: 手动触发或每周日凌晨2点

**测试策略**:
- 运行所有测试，包括所有数据库方言
- 用于完整的回归测试
- 超时时间: 45分钟

## 测试分类

使用 `TestCategory` 属性对测试进行分类:

- `Integration`: 集成测试，需要数据库
- `E2E`: 端到端测试
- `CI`: 多方言关键测试（在 CI 中运行）
- `SQLite`: SQLite 特定测试
- `PostgreSQL`: PostgreSQL 特定测试
- `MySQL`: MySQL 特定测试
- `SqlServer`: SQL Server 特定测试

## 数据库服务优化

### 镜像优化
- PostgreSQL: 使用 `postgres:16-alpine` (更小更快)
- MySQL: 保持 `mysql:8.3`
- SQL Server: 保持 `mcr.microsoft.com/mssql/server:2022-latest`

### 健康检查优化
- 检查间隔: 从 10s 降至 5s
- 检查超时: 从 5s 降至 3s
- 启动等待期: PostgreSQL 10s, MySQL 15s, SQL Server 20s
- 添加 PostgreSQL shared memory 优化

### 数据库初始化优化
- **并行初始化**: 三个数据库同时初始化，而非串行
- **智能等待**: 使用循环检查代替固定等待时间
- **快速失败**: 添加连接超时参数 (`-l 1`, `-l 5`)
- **静默输出**: 减少不必要的日志输出

### 预期改进
- 数据库启动: 从 30s 固定等待降至 10-20s 动态等待
- 数据库初始化: 从串行 30-60s 降至并行 15-30s
- **总体节省**: 约 30-50 秒

### 优化前
- 所有测试一起运行: ~20-30分钟
- 经常超时

### 优化后
- 单元测试: ~3-5分钟
- 集成测试 (SQLite): ~5-8分钟
- 多方言测试: ~5-8分钟
- **总计 (PR)**: ~10-15分钟
- **总计 (Main)**: ~15-20分钟

## 本地开发

使用 `.runsettings` (10分钟超时，类级别并行):
```bash
dotnet test --settings .runsettings
```

## CI 环境

使用 `.runsettings.ci` (15分钟超时，方法级别并行):
```bash
dotnet test --settings .runsettings.ci
```

## 手动运行特定测试

```bash
# 只运行单元测试
dotnet test --filter "TestCategory!=Integration&TestCategory!=E2E"

# 只运行 SQLite 集成测试
dotnet test --filter "TestCategory=Integration&TestCategory!=PostgreSQL&TestCategory!=MySQL&TestCategory!=SqlServer"

# 只运行 PostgreSQL 测试
dotnet test --filter "TestCategory=PostgreSQL"

# 运行 CI 关键测试
dotnet test --filter "TestCategory=CI"
```
