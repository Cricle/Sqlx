# VS Extension Phase 2 P1 实施计划

> **状态**: 🚧 进行中
> **版本**: v0.3.0
> **优先级**: P1

---

## 🎯 Phase 2 P1 目标

### 核心功能 (2个)

#### 1. 📋 占位符智能提示 (IntelliSense)
**目标**: 在 SqlTemplate 字符串中提供智能提示

**功能**:
- 输入 `{{` 时自动提示所有可用占位符
- 占位符参数提示（如 `--exclude`, `--param` 等）
- SQL 关键字自动完成
- 参数名提示（基于方法签名）
- 实时错误检测

**技术实现**:
- `ICompletionSource` 接口
- `ICompletionSourceProvider`
- `CompletionSet` 生成
- Roslyn 语义分析

---

#### 2. 📝 SQL 执行日志窗口
**目标**: 实时记录和显示 SQL 执行情况

**功能**:
- 实时显示所有执行的 SQL
- 执行时间统计
- 参数值显示
- 错误和异常记录
- 性能警告
- 过滤和搜索
- 导出日志

**技术实现**:
- 工具窗口 (ToolWindowPane)
- ListView/DataGrid 显示
- 日志收集服务
- 与 Sqlx 运行时集成

---

## 📊 详细设计

### 1. 占位符智能提示

#### 可用占位符列表
```
基础占位符:
- {{columns}}         - 所有列
- {{table}}          - 表名
- {{values}}         - 所有值
- {{set}}            - SET子句
- {{where}}          - WHERE子句
- {{limit}}          - LIMIT子句
- {{offset}}         - OFFSET子句
- {{orderby}}        - ORDER BY子句
- {{batch_values}}   - 批量值

参数修饰符:
- --exclude Id       - 排除列
- --param name       - 参数化
- --value 10         - 固定值
- --from source      - 来源对象
```

#### 提示触发条件
1. 输入 `{{` - 显示所有占位符
2. 输入占位符后空格 - 显示参数修饰符
3. 输入 `@` - 显示方法参数
4. 输入 SQL 关键字首字母 - 显示关键字

#### UI 设计
```
┌─────────────────────────────────┐
│ {{columns}}                     │
│ {{table}}                       │
│ {{values}}                      │
│ {{set}}                         │
│ {{where}}                       │
│ {{limit}}                       │
│ {{offset}}                      │
│ {{orderby}}                     │
│ {{batch_values}}                │
└─────────────────────────────────┘
    ↑
[SqlTemplate("SELECT {{█")]
```

---

### 2. SQL 执行日志窗口

#### 日志条目结构
```csharp
public class SqlExecutionLog
{
    public DateTime Timestamp { get; set; }
    public string Method { get; set; }
    public string Sql { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    public long ExecutionTime { get; set; }  // ms
    public int RowsAffected { get; set; }
    public bool Success { get; set; }
    public string Error { get; set; }
    public string Database { get; set; }
}
```

#### 窗口布局
```
┌────────────────────────────────────────────────────────┐
│  Sqlx SQL Execution Log                         [🔍]   │
├────────────────────────────────────────────────────────┤
│  🔍 [search...] ⚙️ Settings  📊 Stats  🗑️ Clear      │
├────────────────────────────────────────────────────────┤
│  Time    │ Method          │ SQL             │ Time   │
│  14:30:15│ GetByIdAsync    │ SELECT * FRO... │ 12ms ✅│
│  14:30:16│ InsertAsync     │ INSERT INTO ... │ 8ms  ✅│
│  14:30:17│ UpdateAsync     │ UPDATE users... │ 156ms⚠️│
│  14:30:18│ DeleteAsync     │ DELETE FROM ... │ 5ms  ✅│
│  14:30:19│ GetAllAsync     │ SELECT * FRO... │ ERROR❌│
├────────────────────────────────────────────────────────┤
│  Details:                                              │
│  Method: GetByIdAsync                                  │
│  SQL: SELECT id, name, age FROM users WHERE id = @id  │
│  Parameters: @id = 123                                 │
│  Execution Time: 12 ms                                 │
│  Rows Affected: 1                                      │
│  Status: Success ✅                                    │
└────────────────────────────────────────────────────────┘
```

#### 功能列表
```
✅ 实时日志记录
✅ 彩色状态显示
   - 绿色 ✅: 成功 (< 100ms)
   - 橙色 ⚠️: 慢查询 (100-500ms)
   - 红色 ❌: 错误或超时 (> 500ms)
✅ 详细信息面板
✅ 搜索和过滤
   - 按方法名
   - 按 SQL 内容
   - 按时间范围
   - 按状态
✅ 统计面板
   - 总执行次数
   - 平均执行时间
   - 错误率
   - 慢查询数量
✅ 操作按钮
   - 清空日志
   - 导出日志 (CSV/JSON)
   - 暂停/恢复记录
   - 查看SQL详情
   - 复制SQL
   - 性能分析
```

---

## 🔧 技术实现

### 占位符智能提示

#### 1. CompletionSource
```csharp
internal class SqlxCompletionSource : ICompletionSource
{
    private ITextBuffer _textBuffer;
    private IClassificationTypeRegistryService _registry;

    public void AugmentCompletionSession(
        ICompletionSession session,
        IList<CompletionSet> completionSets)
    {
        // 1. 检测光标位置
        // 2. 判断是否在 SqlTemplate 属性中
        // 3. 判断触发类型 (占位符/参数/关键字)
        // 4. 生成相应的补全列表
        // 5. 添加到 completionSets
    }
}
```

#### 2. CompletionSourceProvider
```csharp
[Export(typeof(ICompletionSourceProvider))]
[ContentType("CSharp")]
[Name("SqlxCompletion")]
internal class SqlxCompletionSourceProvider : ICompletionSourceProvider
{
    [Import]
    internal IClassificationTypeRegistryService ClassificationRegistry { get; set; }

    public ICompletionSource TryCreateCompletionSource(ITextBuffer textBuffer)
    {
        return new SqlxCompletionSource(textBuffer, ClassificationRegistry);
    }
}
```

#### 3. Completion Items
```csharp
private IEnumerable<Completion> GetPlaceholderCompletions()
{
    return new[]
    {
        new Completion("{{columns}}", "{{columns}}", "All columns", null, null),
        new Completion("{{table}}", "{{table}}", "Table name", null, null),
        new Completion("{{values}}", "{{values}}", "All values", null, null),
        // ...
    };
}
```

---

### SQL 执行日志

#### 1. 日志窗口
```csharp
[Guid("A1B2C3D4-5E6F-7890-ABCD-000000000005")]
public class SqlExecutionLogWindow : ToolWindowPane
{
    private SqlExecutionLogControl _control;

    public SqlExecutionLogWindow() : base(null)
    {
        this.Caption = "Sqlx SQL Execution Log";
        this._control = new SqlExecutionLogControl();
        this.Content = this._control;
    }

    public void AddLog(SqlExecutionLog log)
    {
        this._control.AddLog(log);
    }
}
```

#### 2. 日志收集服务
```csharp
public interface ISqlxLogService
{
    void LogExecution(
        string method,
        string sql,
        Dictionary<string, object> parameters,
        long executionTime,
        int rowsAffected,
        bool success,
        string error = null);

    event EventHandler<SqlExecutionLog> LogAdded;
}
```

#### 3. 与 Sqlx 集成
```csharp
// 在生成的代码中添加日志记录
public async Task<User> GetByIdAsync(long id)
{
    var startTime = Stopwatch.GetTimestamp();
    try
    {
        // 执行查询
        var result = await ...;

        // 记录成功
        LogService.LogExecution(
            "GetByIdAsync",
            generatedSql,
            parameters,
            elapsedMs,
            1,
            true);

        return result;
    }
    catch (Exception ex)
    {
        // 记录错误
        LogService.LogExecution(
            "GetByIdAsync",
            generatedSql,
            parameters,
            elapsedMs,
            0,
            false,
            ex.Message);
        throw;
    }
}
```

---

## 📊 实施计划

### Week 1: 占位符智能提示

#### Day 1-2: 基础框架
- [ ] 创建 CompletionSource 和 Provider
- [ ] 实现上下文检测
- [ ] 测试基础触发

#### Day 3-4: 占位符提示
- [ ] 实现占位符列表生成
- [ ] 添加图标和描述
- [ ] 测试所有占位符

#### Day 5: 参数和关键字
- [ ] SQL 关键字提示
- [ ] 方法参数提示
- [ ] 参数修饰符提示

---

### Week 2: SQL 执行日志

#### Day 1-2: 日志窗口 UI
- [ ] 创建工具窗口
- [ ] 设计 UI 布局
- [ ] 实现日志列表显示

#### Day 3-4: 日志服务
- [ ] 创建日志服务接口
- [ ] 实现日志收集
- [ ] 添加过滤和搜索

#### Day 5: 集成和测试
- [ ] 与 Sqlx 生成器集成
- [ ] 添加统计功能
- [ ] 导出功能
- [ ] 性能优化

---

## 📈 成功指标

### 占位符智能提示
- ✅ 提示响应时间 < 100ms
- ✅ 准确率 > 95%
- ✅ 支持所有 Sqlx 占位符
- ✅ 上下文感知

### SQL 执行日志
- ✅ 日志延迟 < 10ms
- ✅ 支持10000+条日志
- ✅ 搜索响应 < 500ms
- ✅ UI 流畅不卡顿

---

## 💡 额外功能 (可选)

### 占位符智能提示增强
- [ ] 占位符参数验证
- [ ] 实时错误提示
- [ ] 智能补全顺序（基于使用频率）
- [ ] 代码片段集成

### SQL 执行日志增强
- [ ] 查询性能分析
- [ ] 慢查询警告
- [ ] 执行计划显示
- [ ] 历史统计图表
- [ ] 日志持久化
- [ ] 多数据库支持

---

## 📚 相关文档

- `docs/VS_EXTENSION_ENHANCEMENT_PLAN.md` - 完整计划
- `src/Sqlx.Extension/VS_EXTENSION_IMPLEMENTATION_STATUS.md` - 实现状态
- `PHASE2_COMPLETE_SUMMARY.md` - Phase 2 P0 总结

---

**当前状态**: ✅ SQL 着色修复完成，准备开始 P1 实施
**下一步**: 实现占位符智能提示


