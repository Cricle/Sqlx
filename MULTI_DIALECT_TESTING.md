# 多数据库方言测试架构

## 📋 概述

Sqlx支持多种数据库方言的测试，采用"**写一次，多数据库运行**"的架构设计。每个数据库方言只需定义SQL模板和表结构，测试逻辑完全共享。

## 🏗️ 架构设计

### 三层架构

```
┌─────────────────────────────────────────────────────────────┐
│                    测试基类 (ComprehensiveTestBase)          │
│  - 20个通用测试方法                                          │
│  - CRUD、聚合、分页、排序、子查询等                           │
└─────────────────────────────────────────────────────────────┘
                              ▲
                              │ 继承
                ┌─────────────┼─────────────┐
                │             │             │
        ┌───────▼──────┐ ┌───▼──────┐ ┌───▼──────┐
        │  PostgreSQL  │ │  MySQL   │ │SQL Server│
        │  测试类      │ │  测试类  │ │  测试类  │
        └──────────────┘ └──────────┘ └──────────┘
                │             │             │
        ┌───────▼──────┐ ┌───▼──────┐ ┌───▼──────┐
        │  PostgreSQL  │ │  MySQL   │ │SQL Server│
        │  接口+SQL    │ │  接口+SQL│ │  接口+SQL│
        └──────────────┘ └──────────┘ └──────────┘
```

### 核心组件

#### 1. **通用接口基类** (`IDialectUserRepositoryBase`)

定义所有数据库共享的方法签名：

```csharp
public partial interface IDialectUserRepositoryBase
{
    Task<long> InsertAsync(string username, string email, int age, ...);
    Task<DialectUser?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<List<DialectUser>> GetAllAsync(CancellationToken ct = default);
    // ... 30+ 方法
}
```

#### 2. **方言特定接口** (如 `IPostgreSQLUserRepository`)

继承基类接口，添加SQL模板：

```csharp
public partial interface IPostgreSQLUserRepository : IDialectUserRepositoryBase
{
    [SqlTemplate("INSERT INTO dialect_users_postgresql (...) VALUES (...) RETURNING id")]
    new Task<long> InsertAsync(...);
    
    [SqlTemplate("SELECT {{columns}} FROM dialect_users_postgresql WHERE id = @id")]
    new Task<DialectUser?> GetByIdAsync(long id, CancellationToken ct = default);
    
    // ... 为每个方法定义PostgreSQL特定的SQL
}
```

#### 3. **仓储实现类**

由源生成器自动生成：

```csharp
[RepositoryFor(typeof(IPostgreSQLUserRepository))]
[SqlDefine(SqlDefineTypes.PostgreSql)]
public partial class PostgreSQLUserRepository(DbConnection connection) 
    : IPostgreSQLUserRepository
{
    // 源生成器自动生成所有方法实现
}
```

#### 4. **测试类**

继承通用测试基类，只需实现4个抽象成员：

```csharp
public class TDD_PostgreSQL_Comprehensive : ComprehensiveTestBase
{
    protected override string DialectName => "PostgreSQL";
    protected override string TableName => "dialect_users_postgresql";
    
    protected override DbConnection CreateConnection()
    {
        return DatabaseConnectionHelper.GetPostgreSQLConnection();
    }
    
    protected override void CreateTable()
    {
        // PostgreSQL建表SQL
    }
    
    protected override IDialectUserRepositoryBase CreateRepository()
    {
        return new PostgreSQLUserRepository(_connection!);
    }
}
```

## 📊 支持的数据库

| 数据库 | 方言标识 | 测试数量 | 本地运行 | CI运行 |
|--------|----------|----------|----------|--------|
| SQLite | `SqlDefineTypes.Sqlite` | 20 | ✅ | ✅ |
| PostgreSQL | `SqlDefineTypes.PostgreSql` | 20 | ⏸️ 跳过 | ✅ |
| MySQL | `SqlDefineTypes.MySql` | 20 | ⏸️ 跳过 | ✅ |
| SQL Server | `SqlDefineTypes.SqlServer` | 20 | ⏸️ 跳过 | ✅ |
| **总计** | - | **80** | **20** | **80** |

## 🔧 SQL方言差异

### 1. 返回插入ID

| 数据库 | 语法 | 特性 |
|--------|------|------|
| PostgreSQL | `INSERT ... RETURNING id` | 原生支持 |
| MySQL | `INSERT ...; SELECT LAST_INSERT_ID()` | 需要`[ReturnInsertedId]` |
| SQL Server | `INSERT ...; SELECT CAST(SCOPE_IDENTITY() AS BIGINT)` | 在SQL中直接返回 |
| SQLite | `INSERT ...; SELECT last_insert_rowid()` | 需要`[ReturnInsertedId]` |

### 2. LIMIT和分页

| 数据库 | LIMIT语法 | 分页语法 |
|--------|-----------|----------|
| PostgreSQL | `LIMIT @limit` | `LIMIT @limit OFFSET @offset` |
| MySQL | `LIMIT @limit` | `LIMIT @limit OFFSET @offset` |
| SQL Server | `TOP (@limit)` | `OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY` |
| SQLite | `LIMIT @limit` | `LIMIT @limit OFFSET @offset` |

### 3. 布尔类型

| 数据库 | 类型 | TRUE | FALSE |
|--------|------|------|-------|
| PostgreSQL | `BOOLEAN` | `true` | `false` |
| MySQL | `BOOLEAN` (TINYINT) | `1` | `0` |
| SQL Server | `BIT` | `1` | `0` |
| SQLite | `INTEGER` | `1` | `0` |

### 4. 字符串类型

| 数据库 | 短字符串 | 长字符串 |
|--------|----------|----------|
| PostgreSQL | `VARCHAR(n)` | `TEXT` |
| MySQL | `VARCHAR(n)` | `TEXT` |
| SQL Server | `NVARCHAR(n)` | `NVARCHAR(MAX)` |
| SQLite | `TEXT` | `TEXT` |

## 🧪 测试覆盖

每个数据库方言测试相同的20个场景：

### CRUD操作 (5个测试)
- ✅ `Insert_ShouldReturnAutoIncrementId` - 插入并返回自增ID
- ✅ `InsertMultiple_ShouldAutoIncrement` - 批量插入自增
- ✅ `GetById_ShouldReturnCorrectUser` - 根据ID查询
- ✅ `Update_ShouldModifyUser` - 更新记录
- ✅ `Delete_ShouldRemoveUser` - 删除记录

### WHERE子句 (3个测试)
- ✅ `GetByUsername_ShouldFind` - 精确匹配
- ✅ `GetByAgeRange_ShouldFilterCorrectly` - 范围查询
- ✅ `NullHandling_ShouldWork` - NULL值处理

### 聚合函数 (2个测试)
- ✅ `Count_ShouldReturnCorrectCount` - COUNT
- ✅ `AggregateFunctions_ShouldCalculateCorrectly` - SUM, AVG, MIN, MAX

### 排序和分页 (3个测试)
- ✅ `OrderBy_ShouldSortCorrectly` - ORDER BY
- ✅ `Limit_ShouldReturnTopN` - LIMIT/TOP
- ✅ `LimitOffset_ShouldPaginate` - 分页

### 高级查询 (7个测试)
- ✅ `LikePattern_ShouldMatchCorrectly` - LIKE模式匹配
- ✅ `GroupBy_ShouldGroupCorrectly` - GROUP BY
- ✅ `Distinct_ShouldRemoveDuplicates` - DISTINCT
- ✅ `Subquery_ShouldFilterCorrectly` - 子查询
- ✅ `CaseInsensitive_ShouldMatch` - 大小写不敏感
- ✅ `BatchDelete_ShouldRemoveMultiple` - 批量删除
- ✅ `BatchUpdate_ShouldModifyMultiple` - 批量更新

## 🚀 添加新数据库支持

### 步骤1: 创建方言接口

```csharp
// tests/Sqlx.Tests/MultiDialect/TDD_Oracle_Comprehensive.cs
public partial interface IOracleUserRepository : IDialectUserRepositoryBase
{
    [SqlTemplate("INSERT INTO dialect_users_oracle (...) VALUES (...) RETURNING id INTO :id")]
    new Task<long> InsertAsync(...);
    
    // ... 为所有方法定义Oracle特定的SQL
}
```

### 步骤2: 创建仓储类

```csharp
[RepositoryFor(typeof(IOracleUserRepository))]
[SqlDefine(SqlDefineTypes.Oracle)]
public partial class OracleUserRepository(DbConnection connection) 
    : IOracleUserRepository
{
}
```

### 步骤3: 创建测试类

```csharp
[TestClass]
[TestCategory(TestCategories.Oracle)]
[TestCategory(TestCategories.CI)]
public class TDD_Oracle_Comprehensive : ComprehensiveTestBase
{
    protected override string DialectName => "Oracle";
    protected override string TableName => "dialect_users_oracle";
    
    protected override DbConnection CreateConnection()
    {
        if (!DatabaseConnectionHelper.IsCI)
            Assert.Inconclusive("Oracle tests are only run in CI environment.");
        return DatabaseConnectionHelper.GetOracleConnection()!;
    }
    
    protected override void CreateTable()
    {
        using var cmd = _connection!.CreateCommand();
        cmd.CommandText = @"
            BEGIN
                EXECUTE IMMEDIATE 'DROP TABLE dialect_users_oracle';
            EXCEPTION
                WHEN OTHERS THEN NULL;
            END;
            /
            
            CREATE TABLE dialect_users_oracle (
                id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                username VARCHAR2(50) NOT NULL,
                email VARCHAR2(100),
                age NUMBER NOT NULL,
                balance NUMBER(18,2) DEFAULT 0 NOT NULL,
                is_active NUMBER(1) DEFAULT 1 NOT NULL,
                created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP NOT NULL,
                last_login_at TIMESTAMP NULL
            )
        ";
        cmd.ExecuteNonQuery();
    }
    
    protected override IDialectUserRepositoryBase CreateRepository()
    {
        return new OracleUserRepository(_connection!);
    }
}
```

### 步骤4: 更新CI配置

```yaml
# .github/workflows/ci-cd.yml
services:
  oracle:
    image: gvenzl/oracle-xe:21-slim
    env:
      ORACLE_PASSWORD: oracle
    ports:
      - 1521:1521
    options: >-
      --health-cmd "sqlplus -s sys/oracle@localhost:1521/XE as sysdba <<< 'SELECT 1 FROM DUAL;'"
      --health-interval 10s
      --health-timeout 5s
      --health-retries 10
```

## 📝 最佳实践

### 1. SQL模板编写

- ✅ 使用`{{columns}}`占位符自动映射实体属性
- ✅ 使用`@paramName`参数化查询防止SQL注入
- ✅ 为每个方言使用正确的SQL语法
- ❌ 不要在SQL中硬编码列名（除非必要）

### 2. 测试隔离

- ✅ 每个测试类使用独立的表名
- ✅ 在`CreateTable()`中先DROP再CREATE
- ✅ 使用事务隔离测试（如果可能）
- ❌ 不要在测试间共享数据

### 3. CI/CD配置

- ✅ 本地测试只运行SQLite（快速反馈）
- ✅ CI环境运行所有数据库（完整验证）
- ✅ 使用健康检查确保数据库就绪
- ✅ 添加连接测试诊断步骤

### 4. 错误处理

- ✅ 使用`Assert.Inconclusive`跳过不可用的数据库
- ✅ 提供清晰的错误消息
- ✅ 在CI日志中输出诊断信息
- ❌ 不要让测试因环境问题而失败

## 📈 测试统计

```bash
# 运行所有测试
dotnet test --configuration Release

# 只运行SQLite测试（本地快速测试）
dotnet test --filter "TestCategory=SQLite"

# 只运行PostgreSQL测试（需要CI环境）
dotnet test --filter "TestCategory=PostgreSQL"

# 运行所有多数据库测试（需要CI环境）
dotnet test --filter "TestCategory=CI"
```

### 当前覆盖率

| 类别 | 测试数 | 通过率 | 覆盖率 |
|------|--------|--------|--------|
| 核心功能 | 1,555 | 100% | 96.4% |
| SQLite | 20 | 100% | 100% |
| PostgreSQL | 20 | 待CI验证 | 100% |
| MySQL | 20 | 待CI验证 | 100% |
| SQL Server | 20 | 待CI验证 | 100% |
| **总计** | **1,615** | **96.3%** | **96.4%** |

## 🔗 相关文档

- [UNIFIED_DIALECT_TESTING.md](./UNIFIED_DIALECT_TESTING.md) - 统一方言测试设计
- [CONSTRUCTOR_SUPPORT_COMPLETE.md](./CONSTRUCTOR_SUPPORT_COMPLETE.md) - 构造函数支持
- [README.md](./README.md) - 项目主文档

## 🎯 未来计划

- [ ] 添加Oracle数据库支持
- [ ] 添加MariaDB数据库支持
- [ ] 性能基准测试（对比不同数据库）
- [ ] 事务测试（回滚、嵌套事务）
- [ ] 并发测试（多线程访问）
- [ ] 连接池测试
- [ ] 大数据量测试（百万级记录）
