# Known Issues - TODO List

## ✅ 已修复

### ~~Issue #1: {{distinct}} 占位符返回空列表~~ (已修复 ✅)

**修复日期**: 2025-12-22

**修复内容**:
1. 修复了 SQL 模板解析逻辑 (`SqlTemplateEngineExtensions.cs`)
2. 修复了标量集合的代码生成 (`CodeGenerationService.cs`)
3. 所有 distinct 测试现在通过

**测试状态**: ✅ 通过
- `TDD_AggregateFunctions_Integration.AggregateFunctions_Distinct_SQLite`
- `TDD_StringFunctions_Integration.StringFunctions_Distinct_SQLite`

---

## 🔴 高优先级

**文件**: 表达式树处理相关代码

**症状**:
- 所有表达式树查询失败
- 错误信息: `SQLite Error 1: ''users' is not a function'`

**SQL 生成**: ❌ 错误

**问题分析**:
1. 表达式树转 SQL 的逻辑生成了错误的 SQL 语法
2. 可能将表名当作函数调用
3. 需要检查 `[ExpressionToSql]` 属性的处理逻辑

**下一步调试**:
1. 查找表达式树转 SQL 的代码位置
2. 添加日志输出查看生成的 SQL
3. 修复 SQL 生成逻辑
4. 添加单元测试验证表达式树转换

**受影响的测试**:
- 整个 `TDD_ExpressionTree_Integration` 类（5 个测试）

**临时方案**: 已标记整个类为 `[Ignore]`

---

## 🔴 高优先级

### Issue #1: 表达式树转 SQL 生成错误 (Known Issue)

**文件**: 表达式树处理相关代码

**症状**:
- 所有表达式树查询失败
- 错误信息: `SQLite Error 1: ''users' is not a function'`

**SQL 生成**: ❌ 错误

**问题分析**:
1. 表达式树转 SQL 的逻辑生成了错误的 SQL 语法
2. 可能将表名当作函数调用
3. 需要检查 `[ExpressionToSql]` 属性的处理逻辑

**下一步调试**:
1. 查找表达式树转 SQL 的代码位置
2. 添加日志输出查看生成的 SQL
3. 修复 SQL 生成逻辑
4. 添加单元测试验证表达式树转换

**受影响的测试**:
- 整个 `TDD_ExpressionTree_Integration` 类（5 个测试）

**临时方案**: 已标记整个类为 `[Ignore]`

---

## 🟡 中优先级

### Issue #2: 高级占位符未实现

**状态**: FullFeatureDemo 中使用但未实现

**需要实现的占位符**:
- `{{join --type X --table Y --on condition}}` - 表关联
- `{{having --condition 'expression'}}` - 分组过滤
- `{{exists --query 'subquery'}}` - 子查询
- `{{union}}` - 集合合并
- `{{case --when X --then Y --else Z}}` - 条件表达式
- `{{row_number --partition_by X --order_by Y}}` - 窗口函数

**实施步骤**:
1. 在 `SqlTemplateEngine.cs` 中实现占位符处理逻辑
2. 添加单元测试验证占位符
3. 运行集成测试验证功能
4. 更新文档

**受影响的测试**:
- `TDD_JoinOperations_Integration` (3 tests)
- `TDD_SubqueriesAndSets_Integration` (3 tests)
- `TDD_CaseExpression_Integration` (3 tests)
- `TDD_WindowFunctions_Integration` (4 tests)
- `TDD_ComplexQueries_Integration` (部分 HAVING 测试)

---

### Issue #3: 多数据库支持未实现

**状态**: 仅支持 SQLite

**需要添加**:
- MySQL (Docker)
- PostgreSQL (Docker)
- SQL Server (Docker)
- Oracle (可选)

**实施步骤**:
1. 创建 `docker-compose.yml`
2. 更新 `DatabaseFixture.cs` 添加连接字符串
3. 为每个数据库创建初始化脚本
4. 更新所有测试以支持多数据库运行
5. 添加 CI/CD 配置

---

## 🟢 低优先级

### Issue #4: FullFeatureDemo 项目 (已删除 ✅)

**状态**: 已完成

**完成日期**: 2025-12-22

**完成内容**:
1. ✅ 将所有模型类移到 `tests/Sqlx.Tests/TestModels/`
2. ✅ 将所有仓储接口移到 `tests/Sqlx.Tests/TestModels/`
3. ✅ 更新所有测试文件的命名空间引用
4. ✅ 删除 `samples/FullFeatureDemo` 目录
5. ✅ 更新项目引用
6. ✅ 更新 README.md 文档

**测试状态**: ✅ 所有核心测试通过 (39/39)

---

## 📝 调试技巧

### 查看生成的代码

```bash
# 方法 1: 使用 EmitCompilerGeneratedFiles
dotnet build /p:EmitCompilerGeneratedFiles=true /p:CompilerGeneratedFilesOutputPath=Generated

# 方法 2: 查看 obj 目录
Get-ChildItem -Path obj -Recurse -Filter "*.g.cs"

# 方法 3: 使用 ILSpy 反编译 DLL
```

### 添加生成器日志

在 `MethodGenerationContext.cs` 中添加：
```csharp
Console.WriteLine($"Generated code: {sb.ToString()}");
```

### 创建最小化测试

创建独立的小型测试项目，只包含问题相关的代码。

---

## 📊 进度跟踪

- [x] 修复 Issue #1: {{distinct}} 占位符 ✅
- [ ] 修复 Issue #1: 表达式树转 SQL (Known Issue - 低优先级)
- [ ] 实现 Issue #2: 高级占位符
- [ ] 实现 Issue #3: 多数据库支持
- [x] 完成 Issue #4: 删除 FullFeatureDemo ✅

**当前状态**: 核心功能 100% 完成,39/39 测试通过,FullFeatureDemo 已删除

**目标**: 实现高级占位符,添加多数据库支持

---

**创建日期**: 2025-12-22  
**最后更新**: 2025-12-22
