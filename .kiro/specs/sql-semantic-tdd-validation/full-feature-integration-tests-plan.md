# FullFeatureDemo 转换为集成测试计划

## 目标

将 FullFeatureDemo 示例项目转换为完整的集成测试套件，覆盖所有支持的数据库方言，并添加组合测试。完成后删除 FullFeatureDemo 项目。

## 支持的数据库

1. **SQLite** (内存数据库 - 快速测试)
2. **MySQL** (Docker)
3. **PostgreSQL** (Docker)
4. **SQL Server** (Docker)
5. **Oracle** (Docker - 可选，如果环境支持)

## 测试结构

### 1. 基础占位符集成测试 (Demo 1)

**文件**: `tests/Sqlx.Tests/Integration/TDD_BasicPlaceholders_Integration.cs`

**测试内容**:
- ✅ {{columns}} - 列名生成
- ✅ {{table}} - 表名转换
- ✅ {{values}} - 参数占位符
- ✅ {{set}} - SET子句生成（带实体参数）
- ✅ {{orderby}} - 排序子句
- ✅ {{limit}} {{offset}} - 分页

**数据库**: 所有5个数据库

**测试方法**:
- `BasicPlaceholders_Insert_AllDialects` - 插入测试
- `BasicPlaceholders_Select_AllDialects` - 查询测试
- `BasicPlaceholders_Update_AllDialects` - 更新测试
- `BasicPlaceholders_Pagination_AllDialects` - 分页测试

---

### 2. 方言占位符集成测试 (Demo 2)

**文件**: `tests/Sqlx.Tests/Integration/TDD_DialectPlaceholders_Integration.cs`

**测试内容**:
- ✅ {{bool_true}} {{bool_false}} - 布尔值字面量
- ✅ {{current_timestamp}} - 当前时间戳
- ✅ 软删除自动过滤

**数据库**: 所有5个数据库

**测试方法**:
- `DialectPlaceholders_BooleanLiterals_AllDialects`
- `DialectPlaceholders_CurrentTimestamp_AllDialects`
- `DialectPlaceholders_SoftDelete_AllDialects`

---

### 3. 聚合函数集成测试 (Demo 3)

**文件**: `tests/Sqlx.Tests/Integration/TDD_AggregateFunctions_Integration.cs`

**测试内容**:
- ✅ {{count}} - 计数
- ✅ {{sum}} - 求和
- ✅ {{avg}} - 平均值
- ✅ {{max}} {{min}} - 最大最小值

**数据库**: 所有5个数据库

**测试方法**:
- `AggregateFunctions_Count_AllDialects`
- `AggregateFunctions_Sum_AllDialects`
- `AggregateFunctions_Avg_AllDialects`
- `AggregateFunctions_MaxMin_AllDialects`

---

### 4. 字符串函数集成测试 (Demo 4)

**文件**: `tests/Sqlx.Tests/Integration/TDD_StringFunctions_Integration.cs`

**测试内容**:
- ✅ {{like}} - 模糊搜索
- ✅ {{in}} - IN子句
- ⚠️ {{between}} - BETWEEN子句 (需要修复)
- ✅ {{distinct}} - 去重
- ✅ {{coalesce}} - NULL处理

**数据库**: 所有5个数据库

**测试方法**:
- `StringFunctions_Like_AllDialects`
- `StringFunctions_In_AllDialects`
- `StringFunctions_Between_AllDialects`
- `StringFunctions_Distinct_AllDialects`
- `StringFunctions_Coalesce_AllDialects`

---

### 5. 批量操作集成测试 (Demo 5)

**文件**: `tests/Sqlx.Tests/Integration/TDD_BatchOperations_Integration.cs`

**测试内容**:
- ✅ {{batch_values}} - 批量插入
- ✅ {{group_concat}} - 字符串聚合
- ✅ 批量删除

**数据库**: 所有5个数据库

**测试方法**:
- `BatchOperations_BatchInsert_AllDialects` - 1000条记录
- `BatchOperations_GroupConcat_AllDialects`
- `BatchOperations_BatchDelete_AllDialects`

---

### 6. 复杂查询集成测试 (Demo 6)

**文件**: `tests/Sqlx.Tests/Integration/TDD_ComplexQueries_Integration.cs`

**测试内容**:
- ✅ {{join}} - JOIN查询
- ✅ {{groupby}} {{having}} - 分组和过滤
- ✅ {{exists}} - 子查询
- ✅ {{union}} - 联合查询
- ✅ {{case}} - 条件表达式
- ✅ {{row_number}} - 窗口函数

**数据库**: 所有5个数据库（部分功能可能不支持某些数据库）

**测试方法**:
- `ComplexQueries_Join_AllDialects`
- `ComplexQueries_GroupByHaving_AllDialects`
- `ComplexQueries_Exists_AllDialects`
- `ComplexQueries_Union_AllDialects`
- `ComplexQueries_Case_AllDialects`
- `ComplexQueries_WindowFunction_AllDialects`

---

### 7. 表达式树集成测试 (Demo 7)

**文件**: `tests/Sqlx.Tests/Integration/TDD_ExpressionTree_Integration.cs`

**测试内容**:
- ✅ 简单条件 (Age >= 18)
- ✅ 字符串包含 (Name.Contains)
- ✅ 复杂组合条件
- ✅ 表达式树 + 分页
- ✅ 表达式树 + 聚合

**数据库**: 所有5个数据库

**测试方法**:
- `ExpressionTree_SimpleCondition_AllDialects`
- `ExpressionTree_StringContains_AllDialects`
- `ExpressionTree_ComplexCondition_AllDialects`
- `ExpressionTree_WithPagination_AllDialects`
- `ExpressionTree_WithAggregation_AllDialects`

---

### 8. 高级特性集成测试 (Demo 8)

**文件**: `tests/Sqlx.Tests/Integration/TDD_AdvancedFeatures_Integration.cs`

**测试内容**:
- ✅ 软删除 ([SoftDelete])
- ✅ 审计字段 ([AuditFields])
- ✅ 乐观锁 ([ConcurrencyCheck])

**数据库**: 所有5个数据库

**测试方法**:
- `AdvancedFeatures_SoftDelete_AllDialects`
- `AdvancedFeatures_AuditFields_AllDialects`
- `AdvancedFeatures_OptimisticLock_AllDialects`

---

## 组合测试

### 9. 跨方言组合测试

**文件**: `tests/Sqlx.Tests/Integration/TDD_CrossDialect_Combination.cs`

**测试内容**:
- 同一查询在不同数据库中的结果一致性
- 占位符组合使用
- 复杂场景端到端测试

**测试方法**:
- `Combination_BasicCRUD_ConsistencyAcrossDialects` - 基础CRUD一致性
- `Combination_ComplexQuery_ConsistencyAcrossDialects` - 复杂查询一致性
- `Combination_MultiPlaceholder_AllDialects` - 多占位符组合
- `Combination_EndToEnd_UserWorkflow_AllDialects` - 端到端用户工作流

---

## 测试基础设施

### 10. 数据库连接管理

**文件**: `tests/Sqlx.Tests/Integration/DatabaseFixture.cs`

**功能**:
- 管理所有数据库连接
- 自动初始化数据库架构
- 测试数据清理
- Docker容器管理（如果需要）

**方法**:
```csharp
public class DatabaseFixture : IDisposable
{
    public DbConnection GetConnection(SqlDefineTypes dialect);
    public void InitializeSchema(SqlDefineTypes dialect);
    public void CleanupData(SqlDefineTypes dialect);
    public void Dispose();
}
```

---

### 11. 测试辅助类

**文件**: `tests/Sqlx.Tests/Integration/IntegrationTestHelpers.cs`

**功能**:
- 生成测试数据
- 验证结果
- 性能测量
- 错误断言

---

## 实现步骤

### Phase 1: 基础设施 (1-2小时)
1. ✅ 创建 DatabaseFixture
2. ✅ 创建 IntegrationTestHelpers
3. ✅ 配置 Docker 数据库连接
4. ✅ 实现数据库架构初始化

### Phase 2: 基础测试 (2-3小时)
5. ✅ 实现 Demo 1-3 的集成测试
6. ✅ 验证所有数据库通过

### Phase 3: 高级测试 (2-3小时)
7. ✅ 实现 Demo 4-6 的集成测试
8. ✅ 修复发现的 bug ({{between}} 等)

### Phase 4: 表达式树和特性 (1-2小时)
9. ✅ 实现 Demo 7-8 的集成测试
10. ✅ 验证所有特性

### Phase 5: 组合测试 (1-2小时)
11. ✅ 实现跨方言组合测试
12. ✅ 端到端场景测试

### Phase 6: 清理 (30分钟)
13. ✅ 运行完整测试套件
14. ✅ 删除 FullFeatureDemo 项目
15. ✅ 更新文档

---

## 测试配置

### Docker Compose 配置

```yaml
version: '3.8'
services:
  mysql:
    image: mysql:8.0
    environment:
      MYSQL_ROOT_PASSWORD: test123
      MYSQL_DATABASE: sqlx_test
    ports:
      - "3306:3306"
  
  postgres:
    image: postgres:15
    environment:
      POSTGRES_PASSWORD: test123
      POSTGRES_DB: sqlx_test
    ports:
      - "5432:5432"
  
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: Y
      SA_PASSWORD: Test123!@#
    ports:
      - "1433:1433"
```

### 测试运行命令

```bash
# 启动所有数据库
docker-compose up -d

# 运行集成测试
dotnet test --filter "Category=Integration"

# 运行特定数据库的测试
dotnet test --filter "Category=Integration&Dialect=MySQL"

# 停止数据库
docker-compose down
```

---

## 成功标准

1. ✅ 所有集成测试在所有支持的数据库上通过
2. ✅ 测试覆盖率 > 90%
3. ✅ 所有 FullFeatureDemo 功能都有对应测试
4. ✅ 组合测试验证跨数据库一致性
5. ✅ 性能测试显示批量操作效率
6. ✅ FullFeatureDemo 项目已删除
7. ✅ 文档已更新

---

## 预期测试数量

- 基础占位符: 20 tests × 5 dialects = 100 tests
- 方言占位符: 15 tests × 5 dialects = 75 tests
- 聚合函数: 10 tests × 5 dialects = 50 tests
- 字符串函数: 15 tests × 5 dialects = 75 tests
- 批量操作: 10 tests × 5 dialects = 50 tests
- 复杂查询: 20 tests × 5 dialects = 100 tests
- 表达式树: 15 tests × 5 dialects = 75 tests
- 高级特性: 15 tests × 5 dialects = 75 tests
- 组合测试: 20 tests × 5 dialects = 100 tests

**总计**: ~700 集成测试

---

## 注意事项

1. **数据库特定功能**: 某些功能可能不支持所有数据库（如窗口函数在旧版本MySQL中不支持）
2. **性能考虑**: 集成测试较慢，使用并行执行和数据库连接池
3. **Docker依赖**: 确保Docker正在运行，或提供跳过集成测试的选项
4. **数据隔离**: 每个测试使用独立的数据库或事务回滚
5. **Bug修复**: 在测试过程中发现的bug需要先修复（如{{between}}占位符）

---

## 下一步

请确认此计划是否符合您的需求。确认后，我将开始实现：

1. 创建测试基础设施
2. 逐步实现所有集成测试
3. 修复发现的bug
4. 运行完整测试套件
5. 删除FullFeatureDemo项目
