# Sqlx 测试指南

## 本地运行完整测试

### 1. 启动数据库容器

在项目根目录运行：

```bash
docker-compose up -d
```

等待所有数据库启动（约30秒）：

```bash
docker-compose ps
```

### 2. 设置环境变量

**Windows (PowerShell):**
```powershell
$env:POSTGRESQL_CONNECTION="Host=localhost;Port=5432;Database=sqlx_test;Username=postgres;Password=postgres"
$env:MYSQL_CONNECTION="Server=localhost;Port=3306;Database=sqlx_test;Uid=root;Pwd=root"
$env:SQLSERVER_CONNECTION="Server=localhost;Database=sqlx_test;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
```

**Linux/macOS:**
```bash
export POSTGRESQL_CONNECTION="Host=localhost;Port=5432;Database=sqlx_test;Username=postgres;Password=postgres"
export MYSQL_CONNECTION="Server=localhost;Port=3306;Database=sqlx_test;Uid=root;Pwd=root"
export SQLSERVER_CONNECTION="Server=localhost;Database=sqlx_test;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=True"
```

### 3. 运行测试

```bash
dotnet test --settings .runsettings
```

### 4. 停止数据库容器

```bash
docker-compose down
```

## 测试配置

- **并行级别**: ClassLevel（类级并行，避免同一测试类方法并发冲突）
- **超时时间**: 20分钟
- **代码覆盖率**: 自动收集

## 注意事项

- 本地和CI使用完全相同的配置（`.runsettings`）
- 所有测试都应该通过，不应该有跳过的测试
- 如果数据库连接失败，测试会被标记为 Inconclusive 而不是 Failed
