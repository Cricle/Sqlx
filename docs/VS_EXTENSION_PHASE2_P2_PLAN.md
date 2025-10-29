# VS Extension Phase 2 P2 实施计划

> **状态**: 🚧 进行中  
> **版本**: v0.4.0  
> **优先级**: P2  
> **预计时间**: 1-2周

---

## 🎯 Phase 2 P2 目标

### 核心功能 (3个) - 高级可视化

#### 1. 📐 SQL模板可视化编辑器
**目标**: 提供图形化的SQL模板设计工具

#### 2. 📊 性能分析器
**目标**: 实时监控和分析查询性能

#### 3. 🗺️ 实体映射查看器
**目标**: 可视化实体与数据库表的映射关系

---

## 📐 1. SQL模板可视化编辑器

### 概述
提供一个可视化的SQL模板编辑器，让开发者通过拖拽和配置的方式构建SQL模板，而不是手写字符串。

### 功能特性

#### 1.1 可视化设计界面
```
┌──────────────────────────────────────────────────┐
│  SQL Template Visual Editor                      │
├──────────────────────────────────────────────────┤
│  ┌────────────┐  ┌────────────────────────────┐ │
│  │ Components │  │   Design Surface           │ │
│  │            │  │                            │ │
│  │ □ SELECT   │  │  ┌──────────────────────┐ │ │
│  │ □ INSERT   │  │  │ SELECT Operation     │ │ │
│  │ □ UPDATE   │  │  ├──────────────────────┤ │ │
│  │ □ DELETE   │  │  │ {{columns}}          │ │ │
│  │ □ WHERE    │  │  │ FROM {{table}}       │ │ │
│  │ □ JOIN     │  │  │ WHERE id = @id       │ │ │
│  │ □ ORDER BY │  │  └──────────────────────┘ │ │
│  │ □ LIMIT    │  │                            │ │
│  └────────────┘  └────────────────────────────┘ │
├──────────────────────────────────────────────────┤
│  Property Editor                                 │
│  ┌────────────────────────────────────────────┐ │
│  │ Placeholder: {{columns}}                   │ │
│  │ Modifiers: [x] --exclude Id               │ │
│  │            [ ] --from                      │ │
│  │ Parameters: @id (long)                     │ │
│  └────────────────────────────────────────────┘ │
├──────────────────────────────────────────────────┤
│  Generated Code Preview                          │
│  [SqlTemplate("SELECT {{columns}} FROM...")]    │
└──────────────────────────────────────────────────┘
```

#### 1.2 组件库
- **基础操作**: SELECT, INSERT, UPDATE, DELETE
- **子句**: FROM, WHERE, JOIN, GROUP BY, ORDER BY, HAVING
- **占位符**: columns, table, values, set, where, etc.
- **函数**: COUNT, SUM, AVG, MIN, MAX
- **特殊**: CASE WHEN, UNION, SUBQUERY

#### 1.3 智能配置
- 占位符参数配置
- 参数类型推断
- 自动验证
- 错误提示

#### 1.4 实时预览
- SQL语句实时生成
- 语法高亮显示
- 参数替换预览
- 执行计划预估

### 技术实现

#### UI组件
```csharp
public class TemplateVisualizerWindow : ToolWindowPane
{
    private TemplateVisualizerControl control;
    
    // 组件面板
    private ComponentPalette componentPalette;
    
    // 设计画布
    private DesignSurface designSurface;
    
    // 属性编辑器
    private PropertyEditor propertyEditor;
    
    // 代码预览
    private CodePreview codePreview;
}
```

#### 数据模型
```csharp
public class SqlTemplateModel
{
    public string Operation { get; set; }  // SELECT, INSERT, etc.
    public List<PlaceholderInfo> Placeholders { get; set; }
    public List<ParameterInfo> Parameters { get; set; }
    public List<ClauseInfo> Clauses { get; set; }
    
    public string GenerateCode()
    {
        // 生成 [SqlTemplate("...")] 代码
    }
}
```

---

## 📊 2. 性能分析器

### 概述
实时收集和分析SQL执行性能数据，提供可视化的性能报告和优化建议。

### 功能特性

#### 2.1 性能监控面板
```
┌──────────────────────────────────────────────────┐
│  SQL Performance Analyzer                  [⚙️]  │
├──────────────────────────────────────────────────┤
│  📊 Summary (Last 1 Hour)                        │
│  ┌──────────────────────────────────────────┐   │
│  │ Total Queries: 1,234                     │   │
│  │ Avg Time: 45ms                           │   │
│  │ Slow Queries: 23 (>500ms)                │   │
│  │ Failed: 3 (0.2%)                         │   │
│  └──────────────────────────────────────────┘   │
├──────────────────────────────────────────────────┤
│  📈 Performance Chart                            │
│  ┌────────────────────────────────────────┐     │
│  │  Execution Time (ms)                   │     │
│  │  500│              ▄▄                  │     │
│  │  400│         ▄▄▄▄██                  │     │
│  │  300│    ▄▄▄▄█████████▄               │     │
│  │  200│▄▄▄█████████████████▄▄           │     │
│  │  100│██████████████████████████       │     │
│  │    0└────────────────────────────────┘ │     │
│  │      10:00  10:15  10:30  10:45  11:00│     │
│  └────────────────────────────────────────┘     │
├──────────────────────────────────────────────────┤
│  ⚠️ Slow Queries (>500ms)                       │
│  ┌────────────────────────────────────────┐     │
│  │ GetAllUsersAsync      856ms  10:45:12  │     │
│  │ UpdateBatchAsync      723ms  10:42:33  │     │
│  │ SearchUsersAsync      654ms  10:38:21  │     │
│  └────────────────────────────────────────┘     │
├──────────────────────────────────────────────────┤
│  💡 Optimization Suggestions                     │
│  • Add index on users.email (3 slow queries)    │
│  • Consider pagination for GetAllUsersAsync      │
│  • Batch size too large in UpdateBatchAsync      │
└──────────────────────────────────────────────────┘
```

#### 2.2 性能指标
- **执行时间**: 平均值、中位数、P95、P99
- **查询频率**: 每秒查询数 (QPS)
- **失败率**: 错误查询百分比
- **慢查询**: 超过阈值的查询
- **资源使用**: CPU、内存、IO

#### 2.3 时间序列分析
- 实时性能曲线
- 历史趋势对比
- 峰值检测
- 异常告警

#### 2.4 优化建议
- 缺失索引检测
- N+1查询识别
- 大批量操作警告
- 查询计划分析

### 技术实现

#### 数据收集
```csharp
public class PerformanceCollector
{
    public void RecordExecution(
        string method,
        string sql,
        long executionTime,
        bool success)
    {
        var metric = new PerformanceMetric
        {
            Timestamp = DateTime.Now,
            Method = method,
            Sql = sql,
            ExecutionTime = executionTime,
            Success = success
        };
        
        _buffer.Add(metric);
        AnalyzePerformance(metric);
    }
}
```

#### 实时分析
```csharp
public class PerformanceAnalyzer
{
    // 检测慢查询
    public List<SlowQuery> DetectSlowQueries(int threshold = 500);
    
    // 统计指标
    public PerformanceStats CalculateStats(TimeSpan period);
    
    // 生成建议
    public List<OptimizationSuggestion> GenerateSuggestions();
}
```

---

## 🗺️ 3. 实体映射查看器

### 概述
可视化展示C#实体类与数据库表、列之间的映射关系，帮助开发者理解和验证ORM映射。

### 功能特性

#### 3.1 映射关系图
```
┌──────────────────────────────────────────────────┐
│  Entity Mapping Viewer                           │
├──────────────────────────────────────────────────┤
│  ┌─────────────────┐      ┌──────────────────┐  │
│  │  User (C#)      │──────│  users (Table)   │  │
│  ├─────────────────┤      ├──────────────────┤  │
│  │ long Id         │─────→│ id BIGINT        │  │
│  │ string Name     │─────→│ name VARCHAR(50) │  │
│  │ int Age         │─────→│ age INT          │  │
│  │ string Email    │─────→│ email VARCHAR    │  │
│  │ bool IsActive   │─────→│ is_active BIT    │  │
│  │ DateTime Created│─────→│ created_at DATETIME│ │
│  └─────────────────┘      └──────────────────┘  │
├──────────────────────────────────────────────────┤
│  Mapping Details                                 │
│  ┌────────────────────────────────────────────┐ │
│  │ Property: Name                             │ │
│  │ Column: name                               │ │
│  │ Type Mapping: string → VARCHAR(50)        │ │
│  │ Nullable: No                               │ │
│  │ Primary Key: No                            │ │
│  │ Default Value: None                        │ │
│  └────────────────────────────────────────────┘ │
├──────────────────────────────────────────────────┤
│  ✅ Validation Results                           │
│  • All properties mapped correctly               │
│  • No orphan columns                             │
│  ⚠️ Warning: Email field may need length limit │
└──────────────────────────────────────────────────┘
```

#### 3.2 映射信息
- **实体到表**: 类名 → 表名
- **属性到列**: 属性名 → 列名
- **类型转换**: C#类型 → SQL类型
- **约束映射**: 主键、外键、唯一、非空
- **特殊映射**: 自增、默认值、计算列

#### 3.3 交互功能
- 点击属性查看详细映射
- 双击跳转到代码
- 右键菜单: 复制SQL、生成迁移
- 拖拽重新排列

#### 3.4 验证功能
- 映射完整性检查
- 类型兼容性验证
- 命名约定检查
- 孤立列检测

### 技术实现

#### 映射分析
```csharp
public class EntityMappingAnalyzer
{
    public EntityMappingInfo AnalyzeEntity(Type entityType)
    {
        var mapping = new EntityMappingInfo
        {
            EntityType = entityType,
            TableName = GetTableName(entityType),
            PropertyMappings = new List<PropertyMapping>()
        };
        
        foreach (var prop in entityType.GetProperties())
        {
            mapping.PropertyMappings.Add(new PropertyMapping
            {
                PropertyName = prop.Name,
                PropertyType = prop.PropertyType,
                ColumnName = GetColumnName(prop),
                SqlType = MapToSqlType(prop.PropertyType),
                Constraints = GetConstraints(prop)
            });
        }
        
        return mapping;
    }
}
```

#### 可视化渲染
```csharp
public class MappingVisualizer : UserControl
{
    // 绘制实体框
    private void DrawEntityBox(EntityMappingInfo entity);
    
    // 绘制表框
    private void DrawTableBox(TableInfo table);
    
    // 绘制映射线
    private void DrawMappingLines(List<PropertyMapping> mappings);
    
    // 处理交互
    private void HandleClick(Point position);
}
```

---

## 📊 实施计划

### Week 1: SQL模板可视化编辑器

#### Day 1-2: 基础UI框架
- [ ] 创建ToolWindowPane
- [ ] 设计主要布局
- [ ] 组件面板
- [ ] 设计画布

#### Day 3-4: 组件和交互
- [ ] 实现拖拽功能
- [ ] 组件库
- [ ] 属性编辑器
- [ ] 事件处理

#### Day 5: 代码生成
- [ ] 模板模型
- [ ] 代码生成器
- [ ] 实时预览
- [ ] 保存/加载

---

### Week 2: 性能分析器 & 实体映射

#### Day 1-3: 性能分析器
- [ ] 创建ToolWindowPane
- [ ] 数据收集器
- [ ] 性能指标计算
- [ ] 图表绘制
- [ ] 优化建议引擎

#### Day 4-5: 实体映射查看器
- [ ] 创建ToolWindowPane
- [ ] 映射分析器
- [ ] 可视化渲染
- [ ] 交互功能
- [ ] 验证功能

---

## 📈 成功指标

### 模板编辑器
- ✅ 支持所有常用SQL操作
- ✅ 拖拽响应 < 50ms
- ✅ 代码生成准确率 100%
- ✅ 支持导入/导出

### 性能分析器
- ✅ 实时监控延迟 < 100ms
- ✅ 支持10000+条记录
- ✅ 图表刷新 < 200ms
- ✅ 准确的优化建议

### 实体映射
- ✅ 支持所有常用类型
- ✅ 渲染响应 < 100ms
- ✅ 验证准确率 100%
- ✅ 支持导航到代码

---

## 💡 额外功能 (可选)

### 模板编辑器
- [ ] 模板库和共享
- [ ] AI辅助生成
- [ ] 模板测试
- [ ] 版本控制

### 性能分析器
- [ ] 性能基线对比
- [ ] 告警通知
- [ ] 报表导出
- [ ] 与CI/CD集成

### 实体映射
- [ ] 数据库逆向工程
- [ ] 迁移脚本生成
- [ ] 多数据库支持
- [ ] 映射冲突解决

---

## 📚 相关文档

- `docs/VS_EXTENSION_ENHANCEMENT_PLAN.md` - 完整计划
- `src/Sqlx.Extension/VS_EXTENSION_IMPLEMENTATION_STATUS.md` - 实施状态
- `PHASE2_P1_COMPLETE.md` - Phase 2 P1 总结

---

**当前状态**: ✅ 计划完成，准备开始实施  
**下一步**: 实现SQL模板可视化编辑器


