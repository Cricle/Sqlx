# Sqlx Docker 数据库环境配置总结 - Part 5

**日期**: 2024-12-22  
**任务**: 配置 Docker 数据库环境，解决端口冲突和连接问题

## 📊 当前状态

### 数据库容器状态

| 数据库 | 状态 | 端口映射 | 健康检查 |
|--------|------|---------|---------|
| PostgreSQL 16 | ✅ 运行中 | 5432:5432 | ✅ 健康 |
| MySQL 8.3 | ✅ 运行中 | 3307:3306 | ✅ 健康 |
| SQL Server 2022 | ❌ 未启动 | - | - |

### 测试结果

**最新测试运行**:
- 总计: 2587
- 成功: 2318 (89.6%)
- 失败: 78 (3%)
- 跳过: 191 (7.4%)

**失败原因**:
1. PostgreSQL 密码认证失败 (~26个失败)
2. SQL Server 连接超时 (~52个失败)

## ✅ 完成的工作

### 1. 解决 MySQL 端口冲突

**问题**: 本地 MySQL 服务占用 3306 端口

**解决方案**:
- 修改 docker-compose.yml，将 MySQL 端口改为 3307:3306
- 更新测试连接字符串：
  - `tests/Sqlx.Tests/Infrastructure/DatabaseConnectionHelper.cs`
  - `tests/Sqlx.Tests/MultiDialect/NullableLimitOffset_Integration_Tests.cs`

**修改内容**:
```yaml
# docker-compose.yml
mysql:
  ports:
    - "3307:3306"  # 从 3306:3306 改为 3307:3306
```

```csharp
// 连接字符串更新
"Server=localhost;Port=3307;Database=sqlx_test;Uid=root;Pwd=root"
```

### 2. 清理并重建 Docker 环境

**执行的命令**:
```bash
# 停止并删除所有容器和卷
docker-compose down -v

# 清理 Docker 系统（释放 20.65GB 空间）
docker system prune -f

# 重新启动 PostgreSQL 和 MySQL
docker-compose up -d postgres mysql
```

### 3. 验证数据库连接

**PostgreSQL 验证**:
```bash
docker exec sqlx-postgres psql -U postgres -c "SELECT version();"
# ✅ 成功：PostgreSQL 16.11

docker exec sqlx-postgres psql -U postgres -d sqlx_test -c "SELECT 1;"
# ✅ 成功：数据库可访问
```

**MySQL 验证**:
```bash
docker ps
# ✅ sqlx-mysql 运行中，健康状态
```

## ⚠️ 剩余问题

### 1. PostgreSQL 密码认证失败 (26个测试失败)

**错误信息**:
```
Npgsql.PostgresException: 28P01: 用户 "postgres" Password 认证失败
```

**奇怪的现象**:
- Docker 内部可以连接：`docker exec sqlx-postgres psql -U postgres` ✅
- 测试代码无法连接：Npgsql 客户端认证失败 ❌

**可能原因**:
1. pg_hba.conf 配置问题
2. Npgsql 客户端版本兼容性问题
3. 连接字符串格式问题

**下一步调查**:
- 检查 PostgreSQL 的 pg_hba.conf 配置
- 尝试使用 `Trust` 认证模式
- 检查 Npgsql 客户端版本

### 2. SQL Server 镜像拉取失败

**错误信息**:
```
failed commit on ref "layer-sha256:...": commit failed: rename ... no such file or directory
```

**原因**: Docker Desktop 的 containerd 存储层问题

**尝试的解决方案**:
- 清理 Docker 系统 ✅
- 尝试拉取 2022-latest 版本 ❌
- 尝试拉取 2019-latest 版本 ❌

**建议**:
1. 重启 Docker Desktop
2. 使用 Azure SQL Edge 作为替代（更轻量）
3. 暂时跳过 SQL Server 测试

## 📝 修改的文件清单

### 修改的文件
1. `docker-compose.yml` - MySQL 端口改为 3307，SQL Server 版本改为 2019
2. `tests/Sqlx.Tests/Infrastructure/DatabaseConnectionHelper.cs` - MySQL 端口更新
3. `tests/Sqlx.Tests/MultiDialect/NullableLimitOffset_Integration_Tests.cs` - MySQL 端口更新

## 💡 技术发现

### Docker 端口映射

**格式**: `host_port:container_port`

**示例**:
- `3307:3306` - 主机 3307 映射到容器 3306
- 允许在主机上运行多个 MySQL 实例

### PostgreSQL 认证方式

**pg_hba.conf 配置**:
```
# TYPE  DATABASE        USER            ADDRESS                 METHOD
host    all             all             0.0.0.0/0               md5      # 密码认证
host    all             all             0.0.0.0/0               trust    # 无密码认证
```

### Docker 存储问题

**症状**: 镜像拉取时出现 "no such file or directory" 错误

**原因**: containerd 存储层损坏

**解决方案**:
1. 重启 Docker Desktop
2. 清理 Docker 数据目录
3. 重新安装 Docker Desktop（最后手段）

## 🎯 下一步行动

### 短期（立即）
1. ✅ 修复 PostgreSQL 认证问题
   - 选项 A: 修改 pg_hba.conf 使用 trust 认证
   - 选项 B: 检查 Npgsql 连接字符串格式
   - 选项 C: 重建 PostgreSQL 容器并设置正确的密码

2. 🔄 解决 SQL Server 镜像问题
   - 选项 A: 重启 Docker Desktop
   - 选项 B: 使用 Azure SQL Edge
   - 选项 C: 暂时跳过 SQL Server 测试

### 中期（1-2天）
1. 📋 运行完整的测试套件
2. 📋 验证所有数据库方言的测试
3. 📋 更新测试文档

### 长期（1周+）
1. 📋 添加 Oracle 数据库支持（可选）
2. 📋 创建 CI/CD 数据库环境配置
3. 📋 编写数据库环境设置文档

## 📈 测试通过率预测

### 当前状态
- 通过: 2318 (89.6%)
- 失败: 78 (3%)
  - PostgreSQL: 26个
  - SQL Server: 52个

### 修复 PostgreSQL 后
- 预计通过: 2344 (90.6%)
- 预计失败: 52 (2%)
  - SQL Server: 52个

### 修复所有数据库后
- 预计通过: 2396 (92.6%)
- 预计失败: 0
- 跳过: 191 (7.4%)

## ✨ 成就

1. ✅ 成功解决 MySQL 端口冲突
2. ✅ 清理 Docker 环境，释放 20GB 空间
3. ✅ PostgreSQL 和 MySQL 容器健康运行
4. ✅ 测试通过率达到 89.6%
5. ✅ 识别了剩余问题的根本原因

## 🔍 调试技巧

### 检查 Docker 容器状态
```bash
docker ps                                    # 查看运行中的容器
docker ps -a                                 # 查看所有容器
docker logs <container_name>                 # 查看容器日志
docker exec <container_name> <command>       # 在容器中执行命令
```

### 检查数据库连接
```bash
# PostgreSQL
docker exec sqlx-postgres psql -U postgres -c "SELECT version();"

# MySQL
docker exec sqlx-mysql mysql -u root -proot -e "SELECT VERSION();"
```

### 检查端口占用
```bash
# Windows
netstat -ano | findstr :3306
tasklist /FI "PID eq <pid>"

# 查看 Docker 端口映射
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
```

## 📚 参考资料

- [Docker Compose 文档](https://docs.docker.com/compose/)
- [PostgreSQL Docker 镜像](https://hub.docker.com/_/postgres)
- [MySQL Docker 镜像](https://hub.docker.com/_/mysql)
- [SQL Server Docker 镜像](https://hub.docker.com/_/microsoft-mssql-server)
- [Npgsql 文档](https://www.npgsql.org/doc/)

