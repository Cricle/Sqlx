# SqlTemplate 重构提案：分离模板定义与参数值

## 问题描述

当前的 `SqlTemplate` 设计存在概念混乱：

```csharp
// 当前设计 - 混合了模板定义和运行时值
public readonly record struct SqlTemplate(string Sql, IReadOnlyDictionary<string, object?> Parameters)
```

**主要问题：**
1. **概念混乱**：模板应该只是定义，不应包含具体参数值
2. **不利于重用**：每次参数不同都要创建新实例
3. **命名误导**：叫"Template"但实际包含执行状态
4. **违反单一职责**：既是模板又是执行结果

## 正确的设计原则

### 1. 模板分离原则
- **SqlTemplate**：纯模板定义，只包含SQL结构
- **SqlExecution**：执行实例，包含模板+参数值
- **SqlRenderer**：渲染器，将模板+参数转为最终SQL

### 2. 职责清晰
- **模板定义**：可重用，不可变，线程安全
- **参数绑定**：运行时提供，每次执行可不同
- **SQL渲染**：按需生成，支持多种输出格式

## 重构方案

### 方案A：最小化重构（推荐）

```csharp
// 1. 重命名当前SqlTemplate为ParameterizedSql
public readonly record struct ParameterizedSql(string Sql, IReadOnlyDictionary<string, object?> Parameters)
{
    public static readonly ParameterizedSql Empty = new(string.Empty, new Dictionary<string, object?>());
}

// 2. 新的SqlTemplate只包含模板定义
public readonly record struct SqlTemplate(string Sql)
{
    public static readonly SqlTemplate Empty = new(string.Empty);
    
    // 执行时绑定参数
    public ParameterizedSql Execute(object? parameters = null) => 
        new(Sql, ExtractParameters(parameters));
    
    public ParameterizedSql Execute(Dictionary<string, object?> parameters) => 
        new(Sql, parameters);
    
    // 流式绑定
    public SqlTemplateBuilder Bind() => new(this);
}

// 3. 流式参数绑定器
public sealed class SqlTemplateBuilder
{
    private readonly SqlTemplate _template;
    private readonly Dictionary<string, object?> _parameters = new();
    
    internal SqlTemplateBuilder(SqlTemplate template) => _template = template;
    
    public SqlTemplateBuilder Param<T>(string name, T value)
    {
        _parameters[name] = value;
        return this;
    }
    
    public ParameterizedSql Build() => new(_template.Sql, _parameters);
}
```

### 方案B：完全重构

```csharp
// 纯模板定义
public sealed class SqlTemplate
{
    public string Sql { get; }
    public IReadOnlySet<string> ParameterNames { get; }
    
    private SqlTemplate(string sql, IReadOnlySet<string> parameterNames)
    {
        Sql = sql;
        ParameterNames = parameterNames;
    }
    
    public static SqlTemplate Parse(string sql) => 
        new(sql, ExtractParameterNames(sql));
    
    public SqlExecution Execute(object? parameters = null) => 
        new(this, ExtractParameters(parameters));
}

// 执行实例
public sealed class SqlExecution
{
    public SqlTemplate Template { get; }
    public IReadOnlyDictionary<string, object?> Parameters { get; }
    
    internal SqlExecution(SqlTemplate template, IReadOnlyDictionary<string, object?> parameters)
    {
        Template = template;
        Parameters = parameters;
    }
    
    public string ToSql() => SqlRenderer.Render(this);
    public (string Sql, IReadOnlyDictionary<string, object?> Parameters) ToParameterized() => 
        (Template.Sql, Parameters);
}
```

## 迁移策略

### 第一阶段：保持兼容性
1. 重命名 `SqlTemplate` 为 `ParameterizedSql`
2. 创建新的 `SqlTemplate`（纯模板）
3. 添加 `[Obsolete]` 标记到旧方法

### 第二阶段：渐进式迁移
1. 更新所有内部使用
2. 提供迁移指南
3. 更新示例和文档

### 第三阶段：清理
1. 移除过时的API
2. 优化性能
3. 完善测试

## 使用示例对比

### 当前用法（有问题）
```csharp
// 每次都创建新模板 - 浪费
var template1 = SqlTemplate.Create("SELECT * FROM Users WHERE Id = @id", new { id = 1 });
var template2 = SqlTemplate.Create("SELECT * FROM Users WHERE Id = @id", new { id = 2 });
```

### 新用法（正确）
```csharp
// 模板定义一次
var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Id = @id");

// 重复使用，只绑定不同参数
var execution1 = template.Execute(new { id = 1 });
var execution2 = template.Execute(new { id = 2 });

// 或者流式绑定
var execution3 = template.Bind()
    .Param("id", 3)
    .Build();
```

## 性能优势

1. **模板缓存**：模板可以全局缓存，减少解析开销
2. **零拷贝**：参数绑定不需要重新解析SQL
3. **内存效率**：模板实例可以共享
4. **类型安全**：编译时检查参数名称

## 兼容性保证

- 保持现有API的向后兼容
- 提供自动迁移工具
- 详细的迁移文档和示例
- 渐进式升级路径

## 结论

这个重构将使SqlTemplate回归其本质：**纯粹的模板定义**，同时提供更好的性能、可维护性和用户体验。
