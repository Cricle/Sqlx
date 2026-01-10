# CI/CD 工作流说明

## 工作流概览

### 1. CI/CD Pipeline (`ci-cd.yml`)
**触发条件**: Push 到 main/develop 分支，PR 到 main 分支，创建 v* tag

**测试策略 - 矩阵并行执行**:

#### Job 1: 单元测试 (5-8分钟)
- 快速测试，不依赖真实数据库
- 使用 SQLite 内存数据库
- 过滤器: `TestCategory!=Integration&TestCategory!=E2E&TestCategory!=CI`

#### Job 2-5: 集成测试 (并行执行，每个 8-12分钟)
使用矩阵策略，每个 job 只启动一个数据库服务：
- **SQLite**: 集成测试，无需外部数据库
- **PostgreSQL**: PostgreSQL 特定测试 + CI 关键测试
- **MySQL**: MySQL 特定测试 + CI 关键测试  
- **SQL Server**: SQL Server 特定测试 + CI 关键测试

**优势**:
- ✅ 并行执行，总时间取决于最慢的 job
- ✅ 每个 job 只启动一个数据库，启动快
- ✅ 失败隔离，某个数据库失败不影响其他
- ✅ 资源利用率高，GitHub Actions 并发执行

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

### 矩阵策略 - 关键优化
- **每个 job 只启动一个数据库**: 大幅减少启动时间
- **并行执行**: 4个集成测试 job 同时运行
- **失败隔离**: 某个数据库测试失败不影响其他
- **资源高效**: 充分利用 GitHub Actions 并发能力

### 镜像优化
- **PostgreSQL**: `postgres:16-alpine` (Alpine Linux, 更小更快)
- **MySQL**: `mysql:8.0` (稳定版本，优化配置)
- **SQL Server**: `mcr.microsoft.com/mssql/server:2019-latest` (更轻量的2019版本)

### 健康检查优化
- 检查间隔: 5s
- 检查超时: 3s
- 启动等待期: PostgreSQL 10s, MySQL 15s, SQL Server 30s

### 预期改进
- **单个 job 数据库启动**: 10-30s (vs 之前 60-90s 启动3个数据库)
- **总执行时间**: 取决于最慢的 job (~10-12分钟)
- **并行优势**: 4个数据库测试同时进行，而非串行

### 优化前 (串行执行)
- 启动3个数据库: ~60-90秒
- 初始化3个数据库: ~30-60秒
- 运行所有测试: ~15-20分钟
- **总计**: ~20-30分钟

### 优化后 (矩阵并行)
- 单元测试 job: ~5-8分钟
- 集成测试 jobs (并行):
  - SQLite: ~5-8分钟
  - PostgreSQL: ~10-12分钟 (包含启动+初始化+测试)
  - MySQL: ~10-12分钟
  - SQL Server: ~10-12分钟
- **总计 (墙上时间)**: ~10-12分钟 (并行执行)

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
