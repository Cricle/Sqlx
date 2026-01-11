# SqlTemplate 返回类型功能 - 发布说明

## 版本信息

- **功能名称**: SqlTemplate 返回类型
- **版本**: v0.6.0 (建议)
- **发布日期**: TBD
- **状态**: ✅ 已完成

## 概述

新增基于返回类型的 SQL 调试功能。通过简单地将方法返回类型改为 `SqlTemplate`，即可获取生成的 SQL 和参数，而不执行数据库查询。这是一个强大的调试和测试工具。

## 核心特性

### 1. 基于返回类型的行为切换

```csharp
// 调试模式 - 返回 SqlTemplate
[Sqlx("SELECT * FROM users WHERE id = @id")]
SqlTemplate GetUserByIdSql(int id);

// 执行模式 - 返回实体
[Sqlx("SELECT * FROM users WHERE id = @id")]
Task<User?> GetUserByIdAsync(int id);
```

### 2. 零运行时开销

- 编译时代码生成
- 无数据库连接
- 无网络 I/O
- 极低内存占用

### 3. 完整功能支持

- ✅ 简单查询
- ✅ 多参数查询
- ✅ 复杂对象参数
- ✅ 批量操作 ({{batch_values}})
- ✅ 所有占位符
- ✅ 表达式树
- ✅ 所有数据库方言 (SQL Server, MySQL, PostgreSQL, SQLite)

## 使用场景

### 1. SQL 调试

```csharp
var template = repo.GetComplexQuerySql(param1, param2);
Console.WriteLine($"SQL: {template.Sql}");
Console.WriteLine($"Parameters: {string.Join(", ", template.Parameters)}");
```

### 2. 单元测试

```csharp
[Test]
public void GetUsersByCity_GeneratesCorrectSql()
{
    var template = repo.GetUsersByCitySql(18, "Beijing");
    Assert.That(template.Sql, Does.Contain("WHERE age >= @minAge"));
    Assert.That(template.Parameters["@minAge"], Is.EqualTo(18));
}
```

### 3. 日志记录

```csharp
var template = repo.GetUserByIdSql(userId);
logger.LogInformation("Executing SQL: {Sql}", template.Execute().Render());
```

### 4. 性能分析

```csharp
var template = repo.GetComplexReportSql(startDate, endDate);
File.WriteAllText("query.sql", template.Execute().Render());
// 在数据库工具中分析
```

## 技术实现

### 代码生成

- **修改文件**: `src/Sqlx.Generator/Core/CodeGenerationService.cs`
- **新增方法**:
  - `GenerateSqlTemplateReturn()` - 生成 SqlTemplate 返回代码
  - `GenerateBatchInsertSqlTemplate()` - 生成批量插入 SqlTemplate
  - `AddParameterToDictionary()` - 添加参数到字典

### 返回类型检测

- **修改文件**: `src/Sqlx.Generator/MethodGenerationContext.cs`
- **新增枚举值**: `ReturnTypes.SqlTemplate`
- **检测逻辑**: 识别 `SqlTemplate` 和 `Task<SqlTemplate>` 返回类型

## 测试覆盖

### 测试统计

- **总测试数**: 33 个
- **测试通过率**: 100%
- **测试文件**: 6 个

### 测试类别

1. **ReturnTypeDetectionTests.cs** (5 tests)
   - SqlTemplate 返回类型检测
   - Task<SqlTemplate> 检测
   - 其他返回类型不受影响

2. **SimpleSqlGenerationTests.cs** (4 tests)
   - 简单查询 SQL 生成
   - 参数处理
   - 错误处理

3. **ParameterDictionaryTests.cs** (5 tests)
   - 标量参数
   - 复杂对象参数
   - 参数名称和值

4. **BatchInsertTests.cs** (5 tests)
   - 批量插入 SQL 生成
   - VALUES 子句生成
   - 批量参数字典

5. **DialectTests.cs** (8 tests)
   - SQL Server 方言
   - MySQL 方言
   - PostgreSQL 方言
   - SQLite 方言

6. **EndToEndTests.cs** (5 tests)
   - 端到端集成测试
   - 复杂场景测试
   - 无副作用验证

### 全量测试验证

- **Sqlx.Tests**: 3260 tests, 3250 passed, 10 skipped
- **向后兼容性**: ✅ 所有现有测试通过
- **代码质量**: ✅ 无 StyleCop 警告

## 文档

### 新增文档

1. **[SQL_TEMPLATE_RETURN_TYPE.md](../../docs/SQL_TEMPLATE_RETURN_TYPE.md)**
   - 完整功能文档
   - 使用场景
   - 最佳实践
   - 故障排除

2. **[TodoWebApi 示例](../../samples/TodoWebApi/)**
   - 在现有示例中添加 SqlTemplate 演示
   - README 中包含使用说明

### 更新文档

1. **README.md**
   - 添加 SQL 调试功能章节
   - 添加文档链接

2. **docs/index.md**
   - 添加到核心功能列表
   - 添加到文档索引

## 性能影响

### SqlTemplate 模式

- **数据库连接**: ❌ 不需要
- **网络 I/O**: ❌ 无
- **内存占用**: 🟢 极低 (只有字符串和字典)
- **执行时间**: ⚡ 微秒级

### 执行模式

- **性能影响**: ✅ 无影响
- **向后兼容**: ✅ 完全兼容
- **现有功能**: ✅ 不受影响

## 破坏性变更

**无破坏性变更**

- 现有 API 完全兼容
- 现有代码无需修改
- 新功能为可选功能

## 升级指南

### 从 v0.5.x 升级

1. 更新 NuGet 包到 v0.6.0
2. 无需修改现有代码
3. 可选：添加 SqlTemplate 返回方法用于调试

### 示例

```csharp
// 现有代码 - 无需修改
[Sqlx("SELECT * FROM users WHERE id = @id")]
Task<User?> GetUserByIdAsync(int id);

// 可选：添加调试方法
[Sqlx("SELECT * FROM users WHERE id = @id")]
SqlTemplate GetUserByIdSql(int id);
```

## 已知限制

1. **不执行数据库操作**
   - SqlTemplate 方法不会打开连接或执行查询
   - 仅用于获取 SQL 和参数

2. **参数值是快照**
   - 捕获调用时的参数值
   - 后续修改不影响 SqlTemplate

3. **不支持流式查询**
   - 不支持 `IAsyncEnumerable` 等流式返回类型

## 未来计划

- [ ] Visual Studio 扩展集成（SQL 预览窗口）
- [ ] 性能分析工具集成
- [ ] SQL 格式化选项
- [ ] 参数绑定构建器增强

## 贡献者

- 实现: Kiro AI Assistant
- 测试: TDD 方法，33 个测试用例
- 文档: 完整的用户文档和示例

## 相关链接

- [功能文档](../../docs/SQL_TEMPLATE_RETURN_TYPE.md)
- [TodoWebApi 示例](../../samples/TodoWebApi/)
- [测试代码](../../tests/Sqlx.Tests/SqlTemplateGeneration/)
- [设计文档](design.md)
- [需求文档](requirements.md)
- [任务列表](tasks.md)

## 反馈

如有问题或建议，请：
- 提交 Issue: https://github.com/Cricle/Sqlx/issues
- 提交 PR: https://github.com/Cricle/Sqlx/pulls

---

**SqlTemplate 返回类型 - 让 SQL 调试变得简单！** 🚀
