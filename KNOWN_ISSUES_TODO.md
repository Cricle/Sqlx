# Known Issues - TODO List

## 🔴 高优先级

### Issue #1: {{distinct}} 占位符返回空列表

**文件**: `src/Sqlx.Generator/MethodGenerationContext.cs` (行 615-640)

**症状**:
- `Task<List<int>> GetDistinctAgesAsync()` 返回空列表
- 影响所有返回标量列表的方法（`List<int>`, `List<string>` 等）

**SQL 生成**: ✅ 正确
```sql
SELECT DISTINCT [age] FROM users ORDER BY [age]
```

**问题分析**:
1. SQL 查询正确执行并返回数据（手动测试验证）
2. 问题在于 C# 代码生成器读取结果时
3. 已尝试修复：直接使用 ordinal 0 读取，但仍失败
4. 可能的原因：
   - `GetDataReadExpressionWithCachedOrdinal` 方法的参数传递问题
   - 生成的代码可能有语法错误
   - Reader 循环可能没有正确执行

**下一步调试**:
1. 使用 ILSpy 或反编译工具查看实际生成的 C# 代码
2. 添加日志输出到生成器，查看生成的代码
3. 创建最小化测试用例来隔离问题
4. 检查 `WriteBeginReader` 和 `WriteEndReader` 的实现

**受影响的测试**:
- `TDD_AggregateFunctions_Integration.AggregateFunctions_Distinct_SQLite`
- `TDD_StringFunctions_Integration.StringFunctions_Distinct_SQLite`

**临时方案**: 已标记为 `[Ignore]` 和 `[TestCategory("KnownIssue")]`

---

### Issue #2: 表达式树转 SQL 生成错误

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

### Issue #4: FullFeatureDemo 项目待删除

**前置条件**: 所有功能已转换为集成测试

**步骤**:
1. 确认所有 FullFeatureDemo 功能都有对应的集成测试
2. 将 FullFeatureDemo 的模型类移到测试项目
3. 删除 `samples/FullFeatureDemo` 目录
4. 更新项目引用
5. 更新文档

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

- [ ] 修复 Issue #1: {{distinct}} 占位符
- [ ] 修复 Issue #2: 表达式树转 SQL
- [ ] 实现 Issue #3: 多数据库支持
- [ ] 完成 Issue #4: 删除 FullFeatureDemo

**目标**: 100% 测试通过率

---

**创建日期**: 2025-12-22  
**最后更新**: 2025-12-22
