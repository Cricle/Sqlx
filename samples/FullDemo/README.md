# Sqlx Full Demo

这是一个完整的 Sqlx 功能演示项目，展示了所有支持的数据库类型和核心功能。

## 支持的数据库

- ✅ **SQLite** - 内置支持，无需外部依赖
- ✅ **PostgreSQL** - 需要运行 PostgreSQL 服务器
- ✅ **MySQL** - 需要运行 MySQL 服务器
- ⚠️ **SQL Server** - 代码已准备，需要 SQL Server 实例
- ⚠️ **Oracle** - 代码已准备，需要 Oracle 实例
- ⚠️ **DB2** - 代码已准备，需要 DB2 实例

## 演示的功能

### 1. 基础 CRUD 操作 (ICrudRepository)
- `GetByIdAsync` - 按 ID 获取
- `GetAllAsync` - 获取所有（带分页）
- `InsertAsync` - 插入
- `UpdateAsync` - 更新
- `DeleteAsync` - 删除
- `CountAsync` - 计数
- `ExistsAsync` - 存在检查

### 2. 表达式查询
- `GetWhereAsync(x => x.Age > 18)` - Lambda 表达式查询
- `ExistsWhereAsync(x => x.IsActive)` - 表达式存在检查
- `GetFirstWhereAsync(x => x.Name == "Alice")` - 获取第一个匹配项

### 3. 聚合函数 (IAggregateRepository)
- `CountAsync` - 计数
- `SumAsync` - 求和
- `AvgAsync` - 平均值
- `MaxAsync` - 最大值
- `MinAsync` - 最小值

### 4. 软删除 (SoftDelete)
- `SoftDeleteAsync` - 软删除（设置标记）
- `RestoreAsync` - 恢复
- `GetDeletedAsync` - 获取已删除项

### 5. 审计字段 (AuditFields)
- 自动设置 `CreatedAt`, `CreatedBy`
- 自动更新 `UpdatedAt`, `UpdatedBy`

### 6. 乐观锁 (ConcurrencyCheck)
- 版本号检查
- 并发冲突检测

### 7. 批量操作 (IBatchRepository)
- `BatchInsertAsync` - 批量插入
- `BatchUpdateAsync` - 批量更新
- `BatchDeleteAsync` - 批量删除

## 运行测试

### SQLite（默认）
```bash
dotnet run
# 或
dotnet run sqlite
```

### PostgreSQL
首先使用 Podman 启动 PostgreSQL：
```bash
podman run -d --name postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 postgres:16
```

然后运行测试：
```bash
dotnet run postgresql
# 或指定连接参数
dotnet run postgresql --pg-host localhost --pg-port 5432 --pg-user postgres --pg-password postgres
```

### MySQL
首先使用 Podman 启动 MySQL：
```bash
podman run -d --name mysql -e MYSQL_ROOT_PASSWORD=root -p 3306:3306 mysql:8
```

然后运行测试：
```bash
dotnet run mysql
# 或指定连接参数
dotnet run mysql --mysql-host localhost --mysql-port 3306 --mysql-user root --mysql-password root
```

### 运行所有数据库测试
```bash
dotnet run all
```

## 环境变量

也可以通过环境变量配置数据库连接：

### PostgreSQL
- `PG_HOST` - 主机地址（默认：localhost）
- `PG_PORT` - 端口（默认：5432）
- `PG_DATABASE` - 数据库名（默认：fulldemo）
- `PG_USER` - 用户名（默认：postgres）
- `PG_PASSWORD` - 密码（默认：postgres）

### MySQL
- `MYSQL_HOST` - 主机地址（默认：localhost）
- `MYSQL_PORT` - 端口（默认：3306）
- `MYSQL_DATABASE` - 数据库名（默认：fulldemo）
- `MYSQL_USER` - 用户名（默认：root）
- `MYSQL_PASSWORD` - 密码（默认：root）

## 项目结构

```
FullDemo/
├── Database/               # 数据库初始化器
│   ├── SQLiteInitializer.cs
│   ├── PostgreSQLInitializer.cs
│   └── MySQLInitializer.cs
├── Models/                 # 实体模型
│   └── Models.cs
├── Repositories/           # 仓储接口和实现
│   ├── Interfaces.cs      # 通用接口定义
│   ├── SQLite/
│   │   └── Implementations.cs
│   ├── PostgreSQL/
│   │   └── Implementations.cs
│   └── MySQL/
│       └── Implementations.cs
├── Tests/                  # 测试类
│   ├── UserRepositoryTests.cs
│   ├── ProductRepositoryTests.cs
│   ├── OrderRepositoryTests.cs
│   ├── AccountRepositoryTests.cs
│   └── LogRepositoryTests.cs
├── Program.cs              # 主程序入口
├── FullDemo.csproj         # 项目文件
└── README.md               # 本文件
```

## 多数据库方言支持

Sqlx 使用 `[SqlDefine]` 属性来指定数据库方言，源生成器会根据不同方言生成适当的 SQL：

```csharp
// SQLite 实现
[SqlDefine(SqlDefineTypes.SQLite)]
[RepositoryFor(typeof(IUserRepository))]
public partial class SQLiteUserRepository : IUserRepository { }

// PostgreSQL 实现
[SqlDefine(SqlDefineTypes.PostgreSql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class PostgreSQLUserRepository : IUserRepository { }

// MySQL 实现
[SqlDefine(SqlDefineTypes.MySql)]
[RepositoryFor(typeof(IUserRepository))]
public partial class MySQLUserRepository : IUserRepository { }
```

## 主要区别

| 特性 | SQLite | PostgreSQL | MySQL |
|------|--------|------------|-------|
| 参数前缀 | `@` | `$` | `@` |
| 标识符引用 | `[column]` | `"column"` | `` `column` `` |
| 布尔值 | `0/1` | `TRUE/FALSE` | `TRUE/FALSE` |
| 自增主键 | `AUTOINCREMENT` | `SERIAL` | `AUTO_INCREMENT` |
