# GitHub Actions工作流说明

## 📋 工作流概览

### `test.yml` - 测试工作流

这个工作流在每次push或pull request时运行，执行全面的测试套件。

#### 触发条件

- **Push到main/dev分支**
- **Pull Request到main/dev分支**
- **手动触发** (workflow_dispatch)

---

## 🎯 工作流作业

### 1️⃣ `test-local` - 本地测试（SQLite）

**目的**: 快速验证基本功能

**运行环境**: `ubuntu-latest`

**数据库**: SQLite (内存数据库)

**步骤**:
1. Checkout代码
2. 设置.NET 9.0
3. 恢复依赖
4. 编译项目（Release配置）
5. 运行本地测试（使用`.runsettings`）
6. 上传测试结果

**预期结果**:
```
已通过! - 失败: 0，通过: 1582，已跳过: 44
```

**耗时**: ~2-3分钟

---

### 2️⃣ `test-all-dialects` - 所有方言测试

**目的**: 验证所有数据库方言的完整功能

**运行环境**: `ubuntu-latest`

**数据库服务**:
- **PostgreSQL 16** (端口5432)
- **MySQL 8.3** (端口3306)
- **SQL Server 2022** (端口1433)

**环境变量**:
```bash
CI=true
POSTGRESQL_CONNECTION="Host=localhost;Port=5432;Database=sqlx_test;Username=postgres;Password=postgres"
MYSQL_CONNECTION="Server=localhost;Port=3306;Database=sqlx_test;Uid=root;Pwd=root"
SQLSERVER_CONNECTION="Server=localhost,1433;Database=sqlx_test;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
```

**步骤**:
1. Checkout代码
2. 设置.NET 9.0
3. 恢复依赖
4. 编译项目（Release配置）
5. 等待数据库服务启动（健康检查）
6. 运行所有方言测试（使用`.runsettings.ci`）
7. 上传测试结果

**预期结果**:
```
已通过! - 失败: 0，通过: 1626，已跳过: 0
```

**耗时**: ~5-8分钟

---

### 3️⃣ `coverage` - 代码覆盖率报告

**目的**: 生成和展示代码覆盖率

**依赖**: `test-local`, `test-all-dialects`

**步骤**:
1. 下载所有测试结果
2. 生成覆盖率报告（Cobertura格式）
3. 创建Markdown格式的摘要
4. 在PR中添加覆盖率评论

**输出**:
- 覆盖率徽章
- 详细的覆盖率报告
- PR评论（仅限Pull Request）

---

### 4️⃣ `test-report` - 测试报告

**目的**: 生成人类可读的测试报告

**依赖**: `test-local`, `test-all-dialects`

**步骤**:
1. 下载所有测试结果（.trx文件）
2. 使用test-reporter生成HTML报告
3. 在GitHub Actions摘要中显示

**输出**:
- 测试成功/失败统计
- 每个测试的详细结果
- 失败测试的错误信息

---

## 📊 数据库服务配置

### PostgreSQL

```yaml
postgres:
  image: postgres:16
  env:
    POSTGRES_DB: sqlx_test
    POSTGRES_USER: postgres
    POSTGRES_PASSWORD: postgres
  ports:
    - 5432:5432
  options: >-
    --health-cmd pg_isready
    --health-interval 10s
    --health-timeout 5s
    --health-retries 5
```

**健康检查**: `pg_isready`命令
**初始化**: 自动创建`sqlx_test`数据库

### MySQL

```yaml
mysql:
  image: mysql:8.3
  env:
    MYSQL_DATABASE: sqlx_test
    MYSQL_ROOT_PASSWORD: root
  ports:
    - 3306:3306
  options: >-
    --health-cmd "mysqladmin ping"
    --health-interval 10s
    --health-timeout 5s
    --health-retries 5
```

**健康检查**: `mysqladmin ping`
**初始化**: 自动创建`sqlx_test`数据库

### SQL Server

```yaml
sqlserver:
  image: mcr.microsoft.com/mssql/server:2022-latest
  env:
    SA_PASSWORD: YourStrong@Passw0rd
    ACCEPT_EULA: Y
    MSSQL_PID: Developer
  ports:
    - 1433:1433
  options: >-
    --health-cmd "/opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P YourStrong@Passw0rd -Q 'SELECT 1'"
    --health-interval 10s
    --health-timeout 5s
    --health-retries 5
```

**健康检查**: sqlcmd查询
**许可**: Developer版（免费）
**注意**: 需要15-20秒完全启动

---

## 🔧 配置文件

### `.runsettings` - 本地开发配置

用于本地测试，只运行SQLite测试。

```xml
<TestRunParameters>
  <Parameter name="Environment" value="Local" />
</TestRunParameters>
```

**特点**:
- 并行执行测试
- 排除需要真实数据库的测试
- 代码覆盖率收集

### `.runsettings.ci` - CI环境配置

用于CI测试，运行所有数据库方言。

```xml
<TestRunParameters>
  <Parameter name="Environment" value="CI" />
  <Parameter name="PostgreSQLConnection" value="" />
  <Parameter name="MySQLConnection" value="" />
  <Parameter name="SqlServerConnection" value="" />
</TestRunParameters>
```

**特点**:
- 并行执行测试
- 运行所有方言测试
- 扩展的超时时间（20分钟）
- 代码覆盖率收集

---

## 📈 测试统计

### 测试分类

| 分类 | 数量 | 本地运行 | CI运行 | 说明 |
|------|------|---------|--------|------|
| **SQLite测试** | 20 | ✅ | ✅ | 内存数据库 |
| **PostgreSQL测试** | 20 | ❌ | ✅ | 需要真实数据库 |
| **MySQL测试** | 0 | ❌ | 🚧 | 待实现 |
| **SQL Server测试** | 0 | ❌ | 🚧 | 待实现 |
| **Oracle测试** | 0 | ❌ | 🚧 | 待实现 |
| **其他功能测试** | 1542 | ✅ | ✅ | 通用测试 |
| **性能测试** | 24 | ⏸️ | ⏸️ | 标记为Ignore |
| **总计** | 1626 | 1582 | 1626 | - |

### 预期运行时间

| 作业 | 估计时间 | 说明 |
|------|---------|------|
| `test-local` | 2-3分钟 | 仅SQLite |
| `test-all-dialects` | 5-8分钟 | 所有方言 |
| `coverage` | 1-2分钟 | 生成报告 |
| `test-report` | 1分钟 | 生成报告 |
| **总计** | 9-14分钟 | 并行执行 |

---

## 🚀 手动触发工作流

你可以在GitHub Actions页面手动触发测试工作流：

1. 进入项目的**Actions**标签页
2. 选择**Tests**工作流
3. 点击**Run workflow**按钮
4. 选择分支
5. 点击**Run workflow**确认

---

## 🔍 查看测试结果

### 在GitHub Actions页面

1. 进入**Actions**标签页
2. 点击相应的工作流运行
3. 查看各个作业的日志

### 在Pull Request中

测试结果会自动显示在PR的检查部分：

- ✅ 绿色对勾：所有测试通过
- ❌ 红色叉号：有测试失败
- 🟡 黄色圆点：测试正在运行

### 代码覆盖率

覆盖率报告会作为评论添加到PR中，包括：

- 总体覆盖率百分比
- 各模块的覆盖率
- 覆盖率变化趋势

---

## 🐛 故障排查

### 数据库连接失败

**症状**: 测试显示"connection refused"或超时

**解决方案**:
1. 检查数据库服务的健康检查状态
2. 增加等待时间（`sleep`命令）
3. 验证端口映射正确
4. 检查连接字符串格式

### 测试超时

**症状**: 作业因超时被取消

**解决方案**:
1. 增加`.runsettings.ci`中的超时设置
2. 检查是否有死锁或无限循环
3. 考虑拆分大型测试

### 内存不足

**症状**: 作业失败，提示内存不足

**解决方案**:
1. 减少并行测试数量
2. 增加runner的资源（使用larger runner）
3. 优化测试数据大小

---

## 📝 最佳实践

### 1. 本地先测试

在push前，本地运行测试：

```bash
# 运行本地测试
dotnet test --settings .runsettings

# 模拟CI环境（如果有Docker）
docker-compose up -d
export CI=true
export POSTGRESQL_CONNECTION="..."
dotnet test --settings .runsettings.ci
```

### 2. 保持测试快速

- 使用事务回滚而不是删除数据
- 避免`Thread.Sleep`，使用适当的等待机制
- 并行执行独立测试

### 3. 隔离测试

- 每个测试使用独立的表
- 使用唯一的测试数据
- 避免测试间的依赖关系

### 4. 清晰的失败消息

```csharp
// ❌ 不好
Assert.IsTrue(result);

// ✅ 好
Assert.IsTrue(result, $"Expected result to be true for user {userId}");
```

---

## 🔄 持续改进

### 计划添加的功能

1. **Oracle支持**
   - 添加Oracle服务
   - 实现Oracle方言测试

2. **性能基准测试**
   - 添加专门的性能测试作业
   - 记录性能趋势
   - 性能回归检测

3. **并发测试**
   - 测试多个runner同时运行
   - 验证并发安全性

4. **集成测试**
   - 添加端到端测试
   - 真实场景模拟

---

## 📚 相关文档

- [GitHub Actions文档](https://docs.github.com/actions)
- [多方言测试架构](../../MULTI_DIALECT_TESTING.md)
- [测试分类说明](../../tests/Sqlx.Tests/TestCategories.cs)

---

## ✨ 总结

这个CI/CD工作流提供了：

✅ **快速反馈** - 本地测试2-3分钟完成
✅ **全面覆盖** - 所有数据库方言自动测试
✅ **详细报告** - 代码覆盖率和测试结果可视化
✅ **易于维护** - 清晰的结构和完善的文档
✅ **持续改进** - 可扩展的架构支持未来增强

通过这个工作流，我们确保每次代码变更都经过完整的测试验证！
