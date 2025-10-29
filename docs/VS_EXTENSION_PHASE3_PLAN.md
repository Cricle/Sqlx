# VS Extension Phase 3 实施计划

> **状态**: ⏳ 计划中  
> **版本**: v1.0.0  
> **优先级**: P3 (高级功能)  
> **预计时间**: 2-3周

---

## 🎯 Phase 3 目标

### 核心功能 (2个) - 高级调试

#### 1. 🐛 SQL 断点和调试
**目标**: 在 SqlTemplate 中设置断点，实时查看SQL执行状态

#### 2. ⏱️ SQL 监视窗口
**目标**: 实时监视SQL变量、参数和执行结果

---

## 🐛 1. SQL 断点和调试

### 概述
提供类似传统断点的SQL调试体验，允许开发者在SQL执行前后暂停，查看和修改参数。

### 功能特性

#### 1.1 断点设置
```csharp
// 在SqlTemplate上设置断点
[SqlTemplate("SELECT * FROM users WHERE id = @id")]
//           ↑ 点击左侧边距设置断点
public async Task<User> GetByIdAsync(long id);
```

#### 1.2 断点管理器
```
┌──────────────────────────────────────────┐
│ SQL Breakpoint Manager           [⚙️]   │
├──────────────────────────────────────────┤
│ Active Breakpoints:                      │
│ ┌────────────────────────────────────┐  │
│ │ ● GetByIdAsync:12                  │  │
│ │   SELECT * FROM users WHERE...     │  │
│ │   Condition: id > 100              │  │
│ │                                    │  │
│ │ ● GetAllAsync:25                   │  │
│ │   SELECT * FROM users              │  │
│ │   Hit Count: 3                     │  │
│ └────────────────────────────────────┘  │
├──────────────────────────────────────────┤
│ [Add] [Remove] [Enable All] [Disable]   │
└──────────────────────────────────────────┘
```

#### 1.3 断点类型
```
✅ 行断点
   - 在SqlTemplate行设置

✅ 条件断点  
   - id > 100
   - name == "test"
   - 参数满足条件时触发

✅ 命中计数断点
   - 第N次执行时触发
   - 每N次触发一次

✅ 日志断点
   - 不暂停执行
   - 仅记录日志信息
```

#### 1.4 调试信息显示
```
断点触发时显示：
┌──────────────────────────────────────┐
│ 🔴 SQL Breakpoint Hit                │
├──────────────────────────────────────┤
│ Method: GetByIdAsync                 │
│ Line: 12                             │
│                                      │
│ SQL:                                 │
│ SELECT id, name, email               │
│ FROM users                           │
│ WHERE id = @id                       │
│                                      │
│ Parameters:                          │
│ @id = 123 (long)                    │
│                                      │
│ Generated SQL:                       │
│ SELECT id, name, email               │
│ FROM users                           │
│ WHERE id = 123                       │
│                                      │
│ [▶️ Continue] [⏩ Step] [⏹️ Stop]    │
└──────────────────────────────────────┘
```

### 技术实现

#### VS调试器集成
```csharp
// 断点提供器
public class SqlBreakpointProvider : IVsBreakpointProvider
{
    // 设置断点
    public void SetBreakpoint(IVsTextLines textBuffer, int line);
    
    // 移除断点
    public void RemoveBreakpoint(int breakpointId);
    
    // 断点触发回调
    public void OnBreakpointHit(SqlBreakpointInfo info);
}

// 断点信息
public class SqlBreakpointInfo
{
    public string Method { get; set; }
    public string Sql { get; set; }
    public Dictionary<string, object> Parameters { get; set; }
    public string Condition { get; set; }
    public int HitCount { get; set; }
}
```

#### 执行拦截
```csharp
// 在生成的代码中注入拦截点
public async Task<User> GetByIdAsync(long id)
{
    // 断点检查
    if (SqlBreakpointManager.HasBreakpoint("GetByIdAsync", 12))
    {
        var breakpointInfo = new SqlBreakpointInfo
        {
            Method = "GetByIdAsync",
            Sql = generatedSql,
            Parameters = new Dictionary<string, object> { ["id"] = id }
        };
        
        // 触发断点
        SqlBreakpointManager.TriggerBreakpoint(breakpointInfo);
    }
    
    // 执行SQL
    return await ...;
}
```

---

## ⏱️ 2. SQL 监视窗口

### 概述
提供实时监视SQL相关变量、参数和执行结果的窗口，类似VS的Watch Window。

### 功能特性

#### 2.1 监视窗口布局
```
┌──────────────────────────────────────────────┐
│ SQL Watch Window                      [⚙️]   │
├──────────────────────────────────────────────┤
│ Name            │ Value       │ Type         │
├─────────────────┼─────────────┼──────────────┤
│ @id             │ 123         │ long         │
│ @name           │ "John"      │ string       │
│ generatedSql    │ "SELECT..." │ string       │
│ executionTime   │ 45ms        │ TimeSpan     │
│ rowsAffected    │ 1           │ int          │
│ result          │ User {...}  │ User         │
│ result.Id       │ 123         │ long         │
│ result.Name     │ "John Doe"  │ string       │
├──────────────────────────────────────────────┤
│ [+ Add Watch]                                │
└──────────────────────────────────────────────┘
```

#### 2.2 支持的监视项
```
✅ SQL参数
   - 方法参数值
   - 运行时类型
   - 参数变化历史

✅ 生成的SQL
   - 模板SQL
   - 参数替换后SQL
   - 格式化显示

✅ 执行结果
   - 返回值
   - 行数影响
   - 执行时间

✅ 性能指标
   - CPU时间
   - 内存使用
   - IO统计

✅ 表达式求值
   - C#表达式
   - SQL表达式
   - 自定义计算
```

#### 2.3 实时更新
```
监视项自动更新：
- SQL执行前
- SQL执行中
- SQL执行后

更新频率：
- 实时 (默认)
- 手动刷新
- 定时刷新 (可配置)
```

#### 2.4 高级功能
```
✅ 表达式求值
   - result.Name.Length
   - parameters.Count
   - executionTime.TotalMilliseconds > 100

✅ 格式化显示
   - JSON格式
   - XML格式
   - 表格格式

✅ 导出功能
   - 导出快照
   - 对比功能
   - 历史记录

✅ 过滤和搜索
   - 按名称
   - 按类型
   - 按值
```

### 技术实现

#### 监视服务
```csharp
public interface ISqlWatchService
{
    // 添加监视项
    void AddWatch(string name, object value, Type type);
    
    // 更新监视项
    void UpdateWatch(string name, object newValue);
    
    // 移除监视项
    void RemoveWatch(string name);
    
    // 求值表达式
    object EvaluateExpression(string expression);
    
    // 监视项变化事件
    event EventHandler<WatchItemChangedEventArgs> WatchItemChanged;
}
```

#### 窗口实现
```csharp
public class SqlWatchWindow : ToolWindowPane
{
    private DataGrid watchGrid;
    private ISqlWatchService watchService;
    
    public SqlWatchWindow()
    {
        Caption = "Sqlx SQL Watch";
        watchService = new SqlWatchService();
        InitializeUI();
    }
    
    private void InitializeUI()
    {
        // 创建DataGrid显示监视项
        watchGrid = new DataGrid
        {
            AutoGenerateColumns = false,
            ItemsSource = watchService.WatchItems
        };
        
        // 添加列：Name, Value, Type
        // 实现编辑、添加、删除功能
    }
}
```

#### 表达式求值器
```csharp
public class SqlExpressionEvaluator
{
    // 使用Roslyn编译表达式
    public object Evaluate(string expression, Dictionary<string, object> context)
    {
        var script = CSharpScript.Create(
            expression,
            globalsType: typeof(WatchContext));
            
        return await script.RunAsync(new WatchContext(context));
    }
}

public class WatchContext
{
    public Dictionary<string, object> Variables { get; set; }
    
    // 动态访问变量
    public object this[string name] => Variables[name];
}
```

---

## 📊 实施计划

### Week 1: 断点基础设施

#### Day 1-2: VS调试器集成
- [ ] IVsBreakpointProvider接口实现
- [ ] 断点数据模型
- [ ] 断点管理器

#### Day 3-4: 断点UI
- [ ] 断点标记（编辑器左侧边距）
- [ ] 断点管理窗口
- [ ] 断点属性对话框

#### Day 5: 条件断点
- [ ] 条件表达式解析
- [ ] 条件求值
- [ ] 命中计数逻辑

---

### Week 2: 执行拦截和监视

#### Day 1-2: 执行拦截
- [ ] 生成代码注入
- [ ] 断点检查逻辑
- [ ] 断点触发处理

#### Day 3-4: 监视窗口
- [ ] 窗口UI实现
- [ ] 监视服务
- [ ] 数据绑定

#### Day 5: 表达式求值
- [ ] Roslyn脚本集成
- [ ] 表达式解析
- [ ] 上下文管理

---

### Week 3: 高级功能和测试

#### Day 1-2: 高级功能
- [ ] 日志断点
- [ ] 断点导入/导出
- [ ] 快捷键支持

#### Day 3-4: 集成测试
- [ ] 断点功能测试
- [ ] 监视窗口测试
- [ ] 性能测试

#### Day 5: 文档和发布
- [ ] 用户文档
- [ ] 示例代码
- [ ] 发布准备

---

## 🎯 成功指标

### 断点功能
- ✅ 支持所有断点类型
- ✅ 断点响应 < 100ms
- ✅ 准确的触发条件
- ✅ 稳定可靠

### 监视窗口
- ✅ 实时更新 < 50ms
- ✅ 支持复杂表达式
- ✅ 数据展示清晰
- ✅ 性能影响 < 5%

---

## ⚠️ 技术挑战

### 挑战 1: VS调试器集成
```
问题：需要深度集成VS调试基础设施
解决方案：
- 使用IVsDebugger接口
- 研究VS SDK调试示例
- 参考其他语言扩展实现
```

### 挑战 2: 源代码生成器特性
```
问题：Roslyn源代码生成器在编译时运行
解决方案：
- 在生成代码中注入断点检查
- 使用属性标记断点位置
- 运行时动态检查
```

### 挑战 3: 表达式求值
```
问题：需要在运行时求值C#表达式
解决方案：
- 使用Microsoft.CodeAnalysis.CSharp.Scripting
- 构建表达式上下文
- 安全的沙箱环境
```

---

## 💡 替代方案

### 如果VS调试器集成太复杂

#### 方案 A: 轻量级断点
```
不依赖VS调试器：
- 自定义断点系统
- 弹出对话框暂停
- 手动继续执行
```

#### 方案 B: 日志增强
```
强化SQL执行日志：
- 更详细的日志记录
- 条件日志
- 日志过滤和搜索
- 时间线视图
```

#### 方案 C: 跟踪视图
```
SQL执行跟踪：
- 完整调用栈
- 参数变化历史
- 执行时间线
- 可视化流程图
```

---

## 🚀 发布计划

### v1.0.0-rc1 (Release Candidate 1)
```
✅ 基本断点功能
✅ 简单监视窗口
✅ 核心调试体验
```

### v1.0.0-rc2 (Release Candidate 2)
```
✅ 条件断点
✅ 表达式求值
✅ 高级监视功能
```

### v1.0.0 (Production Release)
```
✅ 所有功能完整
✅ 充分测试
✅ 完整文档
✅ 性能优化
```

---

## 📚 参考资料

### VS SDK文档
```
- IVsDebugger接口
- IVsBreakpointManager
- IVsExpression Evaluator
- Debugger Extensibility
```

### 相关项目
```
- Python Tools for Visual Studio
- Node.js Tools for Visual Studio
- C# Interactive Window
```

---

## 🎯 Phase 3 优先级评估

### 高优先级 (P0)
```
⏳ 基本断点功能
⏳ 简单监视窗口
```

### 中优先级 (P1)
```
⏳ 条件断点
⏳ 表达式求值
```

### 低优先级 (P2)
```
⏳ 高级调试功能
⏳ 自定义可视化器
```

---

## 💬 社区反馈

在实施Phase 3之前，建议：
1. 发布Phase 2功能
2. 收集用户反馈
3. 评估调试功能需求
4. 根据反馈调整优先级

---

## 🔮 未来展望

### Phase 3之后
```
🔮 AI辅助调试
🔮 远程调试支持
🔮 分布式跟踪
🔮 生产环境监控
🔮 性能分析器增强
```

---

**当前状态**: ⏳ Phase 3 详细计划完成  
**建议**: 先发布 Phase 2 (v0.4.0)，收集反馈后再决定是否实施 Phase 3  
**替代方案**: 增强现有功能，专注于用户体验优化


