# Sqlx 已知问题和待办事项

**最后更新**: 2024-12-22

## 🔴 高优先级问题

### Issue #1: productdetail 表缺失

**优先级**: 🔴 高  
**影响**: 20 个测试失败  
**状态**: 待修复

**问题描述**:
多个集成测试引用 `productdetail` 表，但 `DatabaseFixture` 中没有创建这个表。

**错误信息**:
```
SQLite Error 1: 'no such table: productdetail'
```

**影响的测试**:
- `TDD_CaseExpression_Integration` (3 tests)
- `TDD_JoinOperations_Integration` (1 test)
- `TDD_SubqueriesAndSets_Integration` (2 tests)
- `TDD_WindowFunctions_Integration` (4 tests)

**修复步骤**:
1. 检查测试中使用的 `productdetail` 表结构
2. 在 `DatabaseFixture.cs` 的 `InitializeAsync` 方法中添加表创建语句
3. 确保表结构与测试期望一致
4. 重新运行测试验证

**相关文件**:
- `tests/Sqlx.Tests/Integration/DatabaseFixture.cs`
- `tests/Sqlx.Tests/TestModels/TestModels.cs`

---

### Issue #2: GetUserStatsAsync SQL 语法错误

**优先级**: 🔴 高  
**影响**: 10 个测试失败  
**状态**: 待修复

**问题描述**:
`GetUserStatsAsync` 方法生成的 SQL 包含语法错误，主要是逗号位置不正确。

**错误信息**:
```
SQLite Error 1: 'near ",": syntax error'
```

**影响的测试**:
- `ComplexQueries_GroupByWithHaving_FiltersGroups`
- `JoinOperations_LeftJoin_IncludesNullRecords`
- `JoinOperations_GroupByWithJoin_AggregatesCorrectly`

**可能的原因**:
1. SQL 模板中有多余的逗号
2. 占位符替换后产生了错误的逗号
3. 列名列表生成逻辑有问题

**修复步骤**:
1. 检查 `GetUserStatsAsync` 的 SQL 模板
2. 查看生成的 SQL（添加日志输出）
3. 修复模板或生成逻辑
4. 添加单元测试验证 SQL 生成

**相关文件**:
- `tests/Sqlx.Tests/TestModels/TestRepositories.cs`
- `src/Sqlx.Generator/Core/SqlTemplateEngine.cs`

---

### Issue #3: GetHighValueCustomersAsync 关键字冲突

**优先级**: 🔴 高  
**影响**: 1 个测试失败  
**状态**: 待修复

**问题描述**:
`GetHighValueCustomersAsync` 生成的 SQL 使用了 `table` 作为标识符，这是 SQL 关键字。

**错误信息**:
```
SQLite Error 1: 'near "table": syntax error'
```

**影响的测试**:
- `Subqueries_Exists_FiltersCorrectly`

**修复步骤**:
1. 检查 SQL 模板中的标识符
2. 确保所有标识符都被正确引用（使用方括号或反引号）
3. 更新标识符引用逻辑
4. 验证修复

**相关文件**:
- `tests/Sqlx.Tests/TestModels/TestRepositories.cs`
- `src/Sqlx.Generator/Core/SqlTemplateEngine.cs`

---

### Issue #4: DB2 参数化失败

**优先级**: 🔴 高  
**影响**: 3 个测试失败  
**状态**: 待修复

**问题描述**:
DB2 方言的参数提取逻辑不正确，导致参数化测试失败。

**错误信息**:
```
Assert.IsTrue 失败。Should extract parameters for DB2
```

**影响的测试**:
- `ParameterSafety_AllDialects_EnsuresParameterization`
- `ParameterizedQuery_AllDialects_EnforcesParameterization`
- `MixedParameterTypes_AllDialects_HandlesConsistently`

**可能的原因**:
1. DB2 占位符格式不正确
2. 参数提取正则表达式有问题
3. DB2 方言配置缺失

**修复步骤**:
1. 检查 DB2 方言的占位符定义
2. 验证参数提取逻辑
3. 添加 DB2 特定的单元测试
4. 更新方言配置

**相关文件**:
- `src/Sqlx/Dialects/Db2Dialect.cs`
- `src/Sqlx.Generator/Core/SqlTemplateEngine.cs`
- `tests/Sqlx.Tests/Core/PlaceholderSecurityTests.cs`

---

## 🟡 中优先级问题

### Issue #5: Decimal vs Double 类型不匹配

**优先级**: 🟡 中  
**影响**: 3 个测试失败  
**状态**: 待修复

**问题描述**:
测试期望 `Decimal` 类型，但数据库返回 `Double` 类型。

**错误信息**:
```
Assert.AreEqual 失败。应为: <3000 (System.Decimal)>，实际为: <3000 (System.Double)>
```

**影响的测试**:
- `ComplexQueries_OrderStatsByStatus_AggregatesCorrectly`

**修复方案**:
1. **方案 A**: 统一使用 `decimal` 类型
   - 更新模型定义
   - 更新数据库映射
2. **方案 B**: 在测试中使用类型转换
   - 更新断言逻辑
   - 添加类型转换辅助方法

**修复步骤**:
1. 确定统一的数值类型策略
2. 更新相关模型和测试
3. 验证所有数值类型测试

**相关文件**:
- `tests/Sqlx.Tests/TestModels/TestModels.cs`
- `tests/Sqlx.Tests/Integration/TDD_ComplexQueries_Integration.cs`

---

### Issue #6: 数据库连接配置

**优先级**: 🟡 中  
**影响**: 60 个测试失败（预期）  
**状态**: 需要配置

**问题描述**:
PostgreSQL 和 SQL Server 的集成测试因为数据库未配置而失败。

**错误信息**:
- PostgreSQL: `28P01: 用户 "postgres" Password 验证失败`
- SQL Server: `在与 SQL Server 建立连接时出现与网络相关的或特定于实例的错误`

**影响的测试**:
- `NullableLimitOffset_PostgreSQL_Tests` (所有测试)
- `NullableLimitOffset_SqlServer_Tests` (所有测试)

**解决方案**:

**方案 A: 本地数据库**
```bash
# PostgreSQL
docker run -d --name sqlx-postgres \
  -e POSTGRES_PASSWORD=your_password \
  -p 5432:5432 postgres:latest

# SQL Server
docker run -d --name sqlx-sqlserver \
  -e 'ACCEPT_EULA=Y' \
  -e 'SA_PASSWORD=YourStrong@Passw0rd' \
  -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
```

**方案 B: Docker Compose**
创建 `docker-compose.test.yml`:
```yaml
version: '3.8'
services:
  postgres:
    image: postgres:latest
    environment:
      POSTGRES_PASSWORD: test_password
    ports:
      - "5432:5432"
  
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: Y
      SA_PASSWORD: Test@1234
    ports:
      - "1433:1433"
```

**方案 C: CI 环境跳过**
在 CI 环境中跳过这些测试：
```bash
dotnet test --filter "TestCategory!=RequiresDatabase"
```

**相关文件**:
- `tests/Sqlx.Tests/MultiDialect/NullableLimitOffset_Integration_Tests.cs`
- `docker-compose.yml` (需要创建)

---

## 🟢 低优先级问题

### Issue #7: 跳过的测试分析

**优先级**: 🟢 低  
**影响**: 191 个测试被跳过  
**状态**: 待分析

**问题描述**:
有 191 个测试被标记为跳过，需要分析原因并决定是否启用。

**分析步骤**:
1. 列出所有被跳过的测试
2. 分类跳过原因：
   - 功能未实现
   - 已知问题
   - 平台限制
   - 性能测试
3. 评估每个测试的价值
4. 决定是否启用

**相关命令**:
```bash
# 列出所有跳过的测试
dotnet test --list-tests --filter "TestCategory=Skip"
```

---

## 📊 问题统计

| 优先级 | 问题数 | 影响测试数 | 状态 |
|--------|--------|------------|------|
| 🔴 高 | 4 | 34 | 待修复 |
| 🟡 中 | 2 | 63 | 待修复/配置 |
| 🟢 低 | 1 | 191 | 待分析 |
| **总计** | **7** | **288** | - |

## 🎯 修复路线图

### 第一阶段 (本周)
- [ ] 修复 Issue #1: productdetail 表缺失
- [ ] 修复 Issue #2: GetUserStatsAsync SQL 语法错误
- [ ] 修复 Issue #3: GetHighValueCustomersAsync 关键字冲突
- [ ] 目标: 测试通过率提升到 92%

### 第二阶段 (下周)
- [ ] 修复 Issue #4: DB2 参数化失败
- [ ] 修复 Issue #5: Decimal vs Double 类型不匹配
- [ ] 目标: 测试通过率提升到 93%

### 第三阶段 (未来)
- [ ] 配置 Issue #6: 数据库连接
- [ ] 分析 Issue #7: 跳过的测试
- [ ] 目标: 测试通过率提升到 95%+

## 📝 贡献指南

如果你想帮助修复这些问题：

1. 选择一个问题
2. 在 GitHub 上创建 Issue（如果还没有）
3. Fork 仓库并创建分支
4. 实现修复并添加测试
5. 提交 Pull Request

---

**维护者**: Sqlx Team  
**联系方式**: [GitHub Issues](https://github.com/your-repo/sqlx/issues)
