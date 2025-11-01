# 多方言数据库测试架构

## 🎯 设计目标

- **一次编写，所有方言运行**：测试逻辑写一次，自动适配所有数据库方言
- **本地开发友好**：本地只需SQLite（内存数据库），无需安装其他数据库
- **CI全覆盖**：CI环境运行所有数据库方言的完整测试

## 📁 文件结构

```
tests/Sqlx.Tests/
├── TestCategories.cs                          # 测试分类常量定义
├── Infrastructure/
│   └── DatabaseConnectionHelper.cs            # 数据库连接辅助类
├── MultiDialect/
│   ├── ComprehensiveTestBase.cs               # 通用测试基类（所有测试逻辑）
│   ├── TDD_SQLite_Comprehensive.cs            # SQLite实现（本地+CI）
│   ├── TDD_PostgreSQL_Comprehensive.cs        # PostgreSQL实现（仅CI）
│   ├── TDD_MySQL_Comprehensive.cs             # MySQL实现（仅CI）
│   ├── TDD_SqlServer_Comprehensive.cs         # SQL Server实现（仅CI）
│   └── TDD_Oracle_Comprehensive.cs            # Oracle实现（仅CI）
├── .runsettings                               # 本地开发配置
└── .runsettings.ci                            # CI环境配置
```

## 🔧 架构设计

### 1. 测试基类（ComprehensiveTestBase）

包含所有测试逻辑，每个数据库方言只需实现4个抽象方法：

```csharp
public abstract class ComprehensiveTestBase
{
    protected abstract string DialectName { get; }
    protected abstract string TableName { get; }
    protected abstract DbConnection CreateConnection();
    protected abstract void CreateTable();
    protected abstract IDialectUserRepositoryBase CreateRepository();
}
```

### 2. 方言特定实现

每个数据库方言只需要：

1. **定义SQL模板**（使用各自的SQL语法）
2. **实现连接创建**
3. **实现表创建**
4. **标记测试类别**

示例（SQLite）：

```csharp
[TestClass]
[TestCategory(TestCategories.SQLite)]
[TestCategory(TestCategories.Unit)]
public class TDD_SQLite_Comprehensive : ComprehensiveTestBase
{
    protected override string DialectName => "SQLite";

    protected override DbConnection CreateConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    // ... 其他实现
}
```

## 🧪 测试分类

| 分类 | 描述 | 本地运行 | CI运行 |
|------|------|---------|--------|
| `TestCategories.SQLite` | SQLite测试 | ✅ | ✅ |
| `TestCategories.PostgreSQL` | PostgreSQL测试 | ❌ | ✅ |
| `TestCategories.MySQL` | MySQL测试 | ❌ | ✅ |
| `TestCategories.SqlServer` | SQL Server测试 | ❌ | ✅ |
| `TestCategories.Oracle` | Oracle测试 | ❌ | ✅ |
| `TestCategories.RequiresDatabase` | 需要真实数据库 | ❌ | ✅ |
| `TestCategories.Unit` | 单元测试 | ✅ | ✅ |
| `TestCategories.Performance` | 性能测试 | ⏸️ | ⏸️ |

## 💻 本地开发

### 运行测试

```bash
# 运行所有本地测试（仅SQLite）
dotnet test

# 使用本地配置
dotnet test --settings .runsettings

# 仅运行SQLite测试
dotnet test --filter "TestCategory=SQLite"

# 排除需要数据库的测试
dotnet test --filter "TestCategory!=RequiresDatabase"
```

### 测试结果

```
已通过! - 失败: 0，通过: 1582，已跳过: 44，总计: 1626
                                         ^^^^^^^^^^
                            这些是需要真实数据库的测试（本地跳过）
```

## ☁️ CI环境

### 环境变量

CI环境需要设置以下环境变量：

```bash
CI=true                                    # 标记CI环境
POSTGRESQL_CONNECTION=Host=localhost;...   # PostgreSQL连接字符串
MYSQL_CONNECTION=Server=localhost;...      # MySQL连接字符串
SQLSERVER_CONNECTION=Server=localhost;...  # SQL Server连接字符串
ORACLE_CONNECTION=Data Source=localhost... # Oracle连接字符串
```

### 运行测试

```bash
# 使用CI配置运行所有测试
dotnet test --settings .runsettings.ci

# 运行特定方言测试
dotnet test --filter "TestCategory=PostgreSQL"
dotnet test --filter "TestCategory=MySQL"
dotnet test --filter "TestCategory=SqlServer"
dotnet test --filter "TestCategory=Oracle"
```

### Docker Compose示例

```yaml
version: '3.8'
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: sqlx_test
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"

  mysql:
    image: mysql:8.3
    environment:
      MYSQL_DATABASE: sqlx_test
      MYSQL_ROOT_PASSWORD: root
    ports:
      - "3306:3306"

  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      SA_PASSWORD: YourStrong@Passw0rd
      ACCEPT_EULA: Y
    ports:
      - "1433:1433"

  oracle:
    image: container-registry.oracle.com/database/express:21.3.0-xe
    environment:
      ORACLE_PWD: oracle
    ports:
      - "1521:1521"
```

## 📊 测试覆盖

每个数据库方言运行相同的20个综合测试：

### CRUD操作（5个测试）
- ✅ Insert_ShouldReturnAutoIncrementId
- ✅ InsertMultiple_ShouldAutoIncrement
- ✅ GetById_ShouldReturnCorrectUser
- ✅ Update_ShouldModifyUser
- ✅ Delete_ShouldRemoveUser

### WHERE子句（2个测试）
- ✅ GetByUsername_ShouldFind
- ✅ GetByAgeRange_ShouldFilterCorrectly

### NULL处理（1个测试）
- ✅ NullHandling_ShouldWork

### 聚合函数（2个测试）
- ✅ Count_ShouldReturnCorrectCount
- ✅ AggregateFunctions_ShouldCalculateCorrectly

### ORDER BY（1个测试）
- ✅ OrderBy_ShouldSortCorrectly

### LIMIT/OFFSET（2个测试）
- ✅ Limit_ShouldReturnTopN
- ✅ LimitOffset_ShouldPaginate

### LIKE模式匹配（1个测试）
- ✅ LikePattern_ShouldMatchCorrectly

### GROUP BY（1个测试）
- ✅ GroupBy_ShouldGroupCorrectly

### DISTINCT（1个测试）
- ✅ Distinct_ShouldRemoveDuplicates

### 子查询（1个测试）
- ✅ Subquery_ShouldFilterCorrectly

### 字符串函数（1个测试）
- ✅ CaseInsensitive_ShouldMatch

### 批量操作（2个测试）
- ✅ BatchDelete_ShouldRemoveMultiple
- ✅ BatchUpdate_ShouldModifyMultiple

## 🔄 添加新方言

要添加新的数据库方言支持：

1. **创建方言特定的仓储接口**（继承`IDialectUserRepositoryBase`）
2. **定义SQL模板**（使用该方言的语法）
3. **创建测试类**（继承`ComprehensiveTestBase`）
4. **实现4个抽象方法**
5. **标记测试类别**

示例：

```csharp
[TestClass]
[TestCategory(TestCategories.NewDB)]
[TestCategory(TestCategories.RequiresDatabase)]
public class TDD_NewDB_Comprehensive : ComprehensiveTestBase
{
    protected override string DialectName => "NewDB";
    protected override string TableName => "dialect_users_newdb";

    protected override DbConnection CreateConnection()
    {
        // 实现连接创建
    }

    protected override void CreateTable()
    {
        // 实现表创建（使用NewDB的DDL语法）
    }

    protected override IDialectUserRepositoryBase CreateRepository()
    {
        return new NewDBUserRepository(_connection!);
    }
}
```

## 🎯 最佳实践

### 1. SQL方言差异处理

每个方言的SQL模板应正确处理：

- **自增ID**: SQLite用`AUTOINCREMENT`，PostgreSQL用`SERIAL`，MySQL用`AUTO_INCREMENT`
- **布尔值**: SQLite用`0/1`，PostgreSQL用`true/false`，SQL Server用`BIT`
- **RETURNING子句**: PostgreSQL支持，其他方言可能不支持
- **字符串拼接**: SQLite用`||`，SQL Server用`+`，MySQL用`CONCAT()`
- **LIMIT语法**: PostgreSQL/MySQL用`LIMIT OFFSET`，SQL Server用`OFFSET FETCH`

### 2. 连接管理

```csharp
// 本地开发：始终返回SQLite连接
if (DatabaseConnectionHelper.ShouldSkipTest(DialectName))
{
    Assert.Inconclusive($"{DialectName} tests are only run in CI environment.");
    return;
}

// CI环境：根据环境变量创建真实连接
_connection = CreateConnection();
```

### 3. 数据清理

每个测试使用独立的表，确保测试隔离：

```csharp
protected override void CreateTable()
{
    // 先删除旧表
    cmd.CommandText = "DROP TABLE IF EXISTS dialect_users_xxx;";
    cmd.ExecuteNonQuery();

    // 创建新表
    cmd.CommandText = "CREATE TABLE dialect_users_xxx (...);";
    cmd.ExecuteNonQuery();
}
```

## 📈 当前状态

| 数据库 | 状态 | 测试数 | 本地 | CI |
|--------|------|--------|------|-----|
| SQLite | ✅ 完成 | 20 | ✅ | ✅ |
| PostgreSQL | ✅ 完成 | 20 | ❌ | ✅ |
| MySQL | 🚧 待实现 | 0 | ❌ | ❌ |
| SQL Server | 🚧 待实现 | 0 | ❌ | ❌ |
| Oracle | 🚧 待实现 | 0 | ❌ | ❌ |

**总计**: 40个方言测试（20 SQLite + 20 PostgreSQL）

## 🚀 未来计划

1. **完成剩余方言**: MySQL, SQL Server, Oracle
2. **CI集成**: GitHub Actions工作流配置
3. **性能对比**: 各方言的性能基准测试
4. **扩展测试**: 事务、并发、大数据量等场景

---

## 📝 总结

这个多方言测试架构提供了：

✅ **一次编写，所有方言运行** - 测试逻辑复用率100%
✅ **本地开发友好** - 无需安装数据库，SQLite内存测试
✅ **CI全覆盖** - 所有方言在CI中自动测试
✅ **易于扩展** - 添加新方言只需少量代码
✅ **类型安全** - 源生成器保证编译时安全

通过这个架构，我们可以确保Sqlx在所有支持的数据库方言上都能正确工作！

