# Visual Studio 扩展增强计划 v2.0

> **目标**: 让用户更容易使用 Sqlx，更直观地调试和查看生成的代码

---

## 🎯 核心设计理念

### 三大支柱
1. **📊 可视化** - 让不可见的变得可见
2. **🔍 调试友好** - 实时查看生成的 SQL 和代码
3. **⚡ 开发效率** - 一键操作，零配置

---

## ✅ 已完成功能 (Phase 1)

| 功能 | 状态 | 价值 |
|------|------|------|
| 🎨 语法着色 | ✅ | 代码可读性 +50% |
| ⚡ 快速操作 | ✅ | 生成仓储/CRUD方法 |
| 📦 代码片段 | ✅ | 12+ 模板快速展开 |
| 🔍 参数验证 | ✅ | 实时诊断和修复 |

---

## 🚀 Phase 2: 可视化与调试增强

### 1. 📊 SQL 预览窗口 (核心功能)

**功能描述**: 实时显示 SqlTemplate 生成的最终 SQL

#### 设计方案

**工具窗口位置**:
```
View → Other Windows → Sqlx SQL Preview
```

**界面布局**:
```
┌─────────────────────────────────────────┐
│  Sqlx SQL Preview                  [📌] │
├─────────────────────────────────────────┤
│  Method: GetUserByIdAsync               │
│  Template: SELECT {{columns}} FROM ...  │
├─────────────────────────────────────────┤
│  🎯 SQLite (当前数据库)                  │
│  ┌───────────────────────────────────┐  │
│  │ SELECT id, name, age, email       │  │
│  │ FROM users                        │  │
│  │ WHERE id = @id AND deleted = 0    │  │
│  └───────────────────────────────────┘  │
│                                         │
│  🎯 MySQL (切换查看)                    │
│  ┌───────────────────────────────────┐  │
│  │ SELECT `id`, `name`, `age`, `email` │
│  │ FROM `users`                       │  │
│  │ WHERE `id` = @id AND `deleted` = 0 │  │
│  └───────────────────────────────────┘  │
│                                         │
│  📋 Copy SQL  🔄 Refresh  💾 Export    │
└─────────────────────────────────────────┘
```

**核心功能**:
- ✅ 光标移动到方法时自动更新
- ✅ 显示所有支持的数据库方言
- ✅ 语法高亮显示
- ✅ 一键复制 SQL
- ✅ 参数占位符高亮
- ✅ 导出到文件

**技术实现**:
```csharp
// 1. 监听编辑器光标变化
// 2. 解析 SqlTemplate 属性
// 3. 调用 Sqlx.Generator 的模板引擎
// 4. 渲染到工具窗口
```

**价值**:
- 🎯 实时看到生成的 SQL
- 🎯 多数据库对比
- 🎯 快速调试模板问题

---

### 2. 🔬 生成代码查看器 (核心功能)

**功能描述**: 查看 Roslyn 源生成器生成的实际代码

**工具窗口位置**:
```
View → Other Windows → Sqlx Generated Code
```

**界面布局**:
```
┌──────────────────────────────────────────────┐
│  Sqlx Generated Code              [📌][🔄]  │
├──────────────────────────────────────────────┤
│  📁 UserRepository (展开)                    │
│    ├─ 📄 UserRepository.g.cs (生成的实现)    │
│    ├─ 📄 GetByIdAsync.g.cs (方法实现)        │
│    └─ 📄 InsertAsync.g.cs (方法实现)         │
│                                              │
│  📝 GetByIdAsync.g.cs                        │
│  ┌────────────────────────────────────────┐ │
│  │ public async Task<User?> GetByIdAsync  │ │
│  │ (                                      │ │
│  │     long id,                           │ │
│  │     CancellationToken ct = default     │ │
│  │ )                                      │ │
│  │ {                                      │ │
│  │     using var cmd = connection.        │ │
│  │         CreateCommand();               │ │
│  │     cmd.CommandText = "SELECT id, ...";│ │
│  │     cmd.Parameters.AddWithValue(...);  │ │
│  │     // ... 完整生成的代码              │ │
│  │ }                                      │ │
│  └────────────────────────────────────────┘ │
│                                              │
│  📋 Copy Code  📂 Open in Editor  💾 Save   │
└──────────────────────────────────────────────┘
```

**核心功能**:
- ✅ 树形显示所有生成的文件
- ✅ 语法高亮显示生成的代码
- ✅ 跳转到源定义
- ✅ 比较不同版本的生成代码
- ✅ 搜索生成的代码
- ✅ 导出到单独文件

**技术实现**:
```csharp
// 读取编译器生成的文件
// 路径: obj/Debug/generated/Sqlx.Generator/...
```

**价值**:
- 🎯 理解 Sqlx 如何工作
- 🎯 调试生成逻辑问题
- 🎯 学习最佳实践

---

### 3. 🧪 查询测试工具 (高价值功能)

**功能描述**: 在 IDE 中直接测试 SQL 查询

**工具窗口位置**:
```
View → Other Windows → Sqlx Query Tester
```

**界面布局**:
```
┌──────────────────────────────────────────────┐
│  Sqlx Query Tester                     [▶️]  │
├──────────────────────────────────────────────┤
│  📌 Connection String:                       │
│  ┌────────────────────────────────────────┐ │
│  │ Data Source=app.db              [Test] │ │
│  └────────────────────────────────────────┘ │
│                                              │
│  🎯 Method: GetUserByIdAsync                 │
│                                              │
│  📝 Parameters:                              │
│  ┌────────────────────────────────────────┐ │
│  │ @id (long):      [    123         ]    │ │
│  │ @ct (CancellationToken): [default]     │ │
│  └────────────────────────────────────────┘ │
│                                              │
│  💻 Generated SQL:                           │
│  ┌────────────────────────────────────────┐ │
│  │ SELECT id, name, age, email            │ │
│  │ FROM users WHERE id = 123              │ │
│  └────────────────────────────────────────┘ │
│                                              │
│  ▶️ Execute   ⏱️ 12.3 ms   ✅ Success       │
│                                              │
│  📊 Results (1 row):                         │
│  ┌────────────────────────────────────────┐ │
│  │ Id   │ Name    │ Age │ Email           │ │
│  │ 123  │ Alice   │ 25  │ alice@email.com │ │
│  └────────────────────────────────────────┘ │
│                                              │
│  📋 Copy Results  💾 Export CSV  📊 Details │
└──────────────────────────────────────────────┘
```

**核心功能**:
- ✅ 连接字符串管理和测试
- ✅ 参数输入界面
- ✅ 执行查询
- ✅ 结果集显示（表格）
- ✅ 执行时间统计
- ✅ 错误详情显示
- ✅ 导出结果（CSV/JSON）
- ✅ 历史记录

**高级功能**:
- 🎯 **并发测试**: 测试多次执行的性能
- 🎯 **参数集测试**: 批量测试多组参数
- 🎯 **性能分析**: 执行计划和性能指标

**技术实现**:
```csharp
// 1. 解析方法定义和参数
// 2. 连接数据库
// 3. 执行查询
// 4. 显示结果
```

**价值**:
- 🎯 无需启动应用即可测试
- 🎯 快速验证 SQL 正确性
- 🎯 性能分析

---

### 4. 🗺️ 仓储导航器 (便利功能)

**功能描述**: 快速浏览和导航项目中的所有 Sqlx 仓储

**工具窗口位置**:
```
View → Other Windows → Sqlx Repository Explorer
```

**界面布局**:
```
┌──────────────────────────────────────────┐
│  Sqlx Repository Explorer         [🔍]   │
├──────────────────────────────────────────┤
│  🔍 Search: [user...            ]        │
│                                          │
│  📊 Statistics:                          │
│    • 8 Repositories                     │
│    • 47 Methods                         │
│    • 5 Database Types                   │
│                                          │
│  📁 Repositories:                        │
│  ├─ 👤 IUserRepository (SQLite)         │
│  │   ├─ 🔍 GetByIdAsync                 │
│  │   ├─ 📝 GetAllAsync                  │
│  │   ├─ ➕ InsertAsync                   │
│  │   ├─ ✏️ UpdateAsync                   │
│  │   └─ ❌ DeleteAsync                   │
│  │                                       │
│  ├─ 📦 IProductRepository (MySQL)       │
│  │   ├─ 🔍 GetByIdAsync                 │
│  │   ├─ 📋 GetByCategoryAsync           │
│  │   └─ 💰 GetByPriceRangeAsync         │
│  │                                       │
│  └─ 📋 IOrderRepository (PostgreSQL)    │
│      ├─ 🔍 GetByIdAsync                 │
│      └─ 👤 GetByUserIdAsync             │
│                                          │
│  右键菜单:                               │
│    • 📂 Go to Definition                │
│    • 🔬 View Generated Code             │
│    • 📊 View SQL Preview                │
│    • 🧪 Test Query                      │
│    • ➕ Add CRUD Methods                │
└──────────────────────────────────────────┘
```

**核心功能**:
- ✅ 扫描项目中所有仓储
- ✅ 分类显示（按数据库类型）
- ✅ 方法列表和图标
- ✅ 快速搜索
- ✅ 统计信息
- ✅ 右键快捷操作

**价值**:
- 🎯 项目概览
- 🎯 快速导航
- 🎯 一站式操作

---

### 5. 📋 占位符智能提示 (IntelliSense 增强)

**功能描述**: 在 SqlTemplate 字符串中提供智能提示

**效果演示**:
```csharp
[SqlTemplate("SELECT {{col|  <-- 这里弹出提示
╔══════════════════════════════════╗
║ Sqlx Placeholders               ║
╠══════════════════════════════════╣
║ {{columns}}    - 列名列表        ║
║ {{table}}      - 表名            ║
║ {{where}}      - WHERE 条件      ║
║ {{values}}     - VALUES 列表     ║
║ {{set}}        - SET 子句        ║
║ {{limit}}      - LIMIT 子句      ║
║ {{offset}}     - OFFSET 子句     ║
║ {{orderby}}    - ORDER BY 子句   ║
║ {{batch_values}} - 批量 VALUES   ║
╚══════════════════════════════════╝
```

**智能提示功能**:
- ✅ 占位符自动完成
- ✅ SQL 关键字提示
- ✅ 参数名提示（基于方法签名）
- ✅ 表名提示（基于 Entity）
- ✅ 列名提示（基于 Entity 属性）

**高级功能**:
```csharp
[SqlTemplate("SELECT {{columns}} FROM users WHERE name = @n|
                                                        ↑
                提示: @name (string) - 来自方法参数
```

**技术实现**:
```csharp
// 实现 ICompletionSource
// 监听输入触发器 ({{, @)
// 提供上下文感知的建议
```

**价值**:
- 🎯 减少拼写错误
- 🎯 提高编码速度
- 🎯 学习占位符系统

---

### 6. 🎨 模板可视化编辑器 (高级功能)

**功能描述**: 可视化方式构建 SQL 模板

**界面布局**:
```
┌────────────────────────────────────────────────┐
│  Sqlx Template Builder                    [💾] │
├────────────────────────────────────────────────┤
│  🎯 Template Type: SELECT ▼                    │
│                                                │
│  📋 Entity: User ▼                             │
│                                                │
│  ✅ Columns:                                   │
│  ☑ id        ☑ name      ☑ age               │
│  ☑ email     ☐ password  ☐ salt              │
│                                                │
│  🔍 WHERE Conditions:                          │
│  ┌──────────────────────────────────────────┐ │
│  │ ☑ id = @id                               │ │
│  │ ☑ deleted = 0  (SoftDelete)              │ │
│  │ [+ Add Condition]                        │ │
│  └──────────────────────────────────────────┘ │
│                                                │
│  📊 ORDER BY: [name ▲] [+ Add]                │
│                                                │
│  📄 Generated Template:                        │
│  ┌──────────────────────────────────────────┐ │
│  │ SELECT {{columns}}                        │ │
│  │ FROM {{table}}                            │ │
│  │ WHERE id = @id                            │ │
│  │ ORDER BY name ASC                         │ │
│  └──────────────────────────────────────────┘ │
│                                                │
│  💻 Generated Code:                            │
│  ┌──────────────────────────────────────────┐ │
│  │ [SqlTemplate("SELECT {{columns}} ...")]   │ │
│  │ Task<User?> GetByIdAsync(long id, ...);   │ │
│  └──────────────────────────────────────────┘ │
│                                                │
│  📋 Copy Template  📂 Insert to Editor  [OK]  │
└────────────────────────────────────────────────┘
```

**核心功能**:
- ✅ 可视化选择操作类型
- ✅ 拖拽选择列
- ✅ 可视化构建 WHERE 条件
- ✅ 自动生成方法签名
- ✅ 实时预览
- ✅ 插入到编辑器

**支持的模板类型**:
- SELECT (单个/列表)
- INSERT
- UPDATE
- DELETE
- COUNT
- EXISTS

**价值**:
- 🎯 降低学习曲线
- 🎯 快速构建复杂查询
- 🎯 避免语法错误

---

### 7. 📊 性能分析器 (调试工具)

**功能描述**: 分析查询性能和优化建议

**工具窗口位置**:
```
View → Other Windows → Sqlx Performance Analyzer
```

**界面布局**:
```
┌──────────────────────────────────────────────────┐
│  Sqlx Performance Analyzer              [▶️][⏸️] │
├──────────────────────────────────────────────────┤
│  🎯 Profiling: UserRepository.GetByIdAsync       │
│                                                  │
│  📊 Statistics (100 executions):                 │
│  ┌────────────────────────────────────────────┐ │
│  │ Average:  12.3 ms                          │ │
│  │ Median:   11.8 ms                          │ │
│  │ Min:      8.2 ms                           │ │
│  │ Max:      45.7 ms  ⚠️ Outlier             │ │
│  │ Std Dev:  3.2 ms                           │ │
│  └────────────────────────────────────────────┘ │
│                                                  │
│  📈 Execution Timeline:                          │
│  ┌────────────────────────────────────────────┐ │
│  │     ▂▃▂▃▂▃▅▄▃▂▃▄▃▂█▃▂▃▄▃▂▃▄▃▂▃▄           │ │
│  │     0ms  ─────────────────────────→ 50ms   │ │
│  └────────────────────────────────────────────┘ │
│                                                  │
│  💡 Optimization Suggestions:                    │
│  ┌────────────────────────────────────────────┐ │
│  │ ⚠️ Missing Index on 'users.id'             │ │
│  │    Recommendation: CREATE INDEX idx_...    │ │
│  │                                             │ │
│  │ ✅ Using parameterized queries             │ │
│  │ ✅ Connection pooling enabled              │ │
│  └────────────────────────────────────────────┘ │
│                                                  │
│  🔍 Query Plan:                                  │
│  ┌────────────────────────────────────────────┐ │
│  │ 1. Table Scan on 'users' (cost: 10)       │ │
│  │    Rows: 1000, Filtered: 1                │ │
│  │    🔴 Warning: Full table scan             │ │
│  └────────────────────────────────────────────┘ │
│                                                  │
│  💾 Export Report  📋 Copy Details  🔄 Retest   │
└──────────────────────────────────────────────────┘
```

**核心功能**:
- ✅ 执行时间统计
- ✅ 性能图表
- ✅ 异常值检测
- ✅ 优化建议
- ✅ 查询计划分析
- ✅ 对比不同实现

**价值**:
- 🎯 发现性能瓶颈
- 🎯 优化建议
- 🎯 数据驱动决策

---

### 8. 🔗 实体-表映射查看器 (便利工具)

**功能描述**: 可视化显示实体和数据库表的映射关系

**界面布局**:
```
┌─────────────────────────────────────────────┐
│  Sqlx Entity Mapping Viewer          [🔄]  │
├─────────────────────────────────────────────┤
│  Entity: User                               │
│  Table:  users (SQLite)                     │
│                                             │
│  📊 Property Mapping:                       │
│  ┌───────────────────────────────────────┐ │
│  │ C# Property    │ DB Column  │ Type    │ │
│  ├───────────────────────────────────────┤ │
│  │ Id             → id         │ INTEGER │ │
│  │ Name           → name       │ TEXT    │ │
│  │ Age            → age        │ INTEGER │ │
│  │ Email          → email      │ TEXT    │ │
│  │ CreatedAt      → created_at │ TEXT    │ │
│  │ UpdatedAt      → updated_at │ TEXT    │ │
│  │ IsDeleted      → deleted    │ INTEGER │ │
│  └───────────────────────────────────────┘ │
│                                             │
│  🏷️ Attributes:                             │
│  • [SoftDelete] ✅                          │
│  • [AuditFields] ✅                         │
│  • [Table("users")] ✅                      │
│                                             │
│  📝 SQL Schema:                             │
│  ┌───────────────────────────────────────┐ │
│  │ CREATE TABLE users (                  │ │
│  │   id INTEGER PRIMARY KEY,             │ │
│  │   name TEXT NOT NULL,                 │ │
│  │   age INTEGER,                        │ │
│  │   email TEXT UNIQUE,                  │ │
│  │   created_at TEXT DEFAULT CURRENT_...,│ │
│  │   updated_at TEXT,                    │ │
│  │   deleted INTEGER DEFAULT 0           │ │
│  │ );                                    │ │
│  └───────────────────────────────────────┘ │
│                                             │
│  🧪 Generate Schema  📋 Copy  💾 Export    │
└─────────────────────────────────────────────┘
```

**核心功能**:
- ✅ 属性-列映射显示
- ✅ 类型转换查看
- ✅ 特性标记显示
- ✅ 生成 DDL 语句
- ✅ 验证映射一致性

**价值**:
- 🎯 理解数据映射
- 🎯 快速生成建表语句
- 🎯 发现映射问题

---

## 📐 Phase 3: 高级调试功能

### 9. 🐛 断点和监视 (深度集成)

**功能描述**: 在生成的代码中设置断点和监视

**核心功能**:
- 在 SqlTemplate 方法上设置"逻辑断点"
- 断点触发时显示：
  - 生成的 SQL
  - 参数值
  - 执行时间
  - 结果集预览

**界面**:
```
断点命中: UserRepository.GetByIdAsync

┌─────────────────────────────────────┐
│ 📍 Breakpoint Hit                   │
├─────────────────────────────────────┤
│ SQL: SELECT id, name, age           │
│      FROM users WHERE id = 123      │
│                                     │
│ Parameters:                         │
│   @id = 123 (Int64)                │
│                                     │
│ Execution Time: 12.3 ms             │
│                                     │
│ Result: User {                      │
│   Id = 123,                         │
│   Name = "Alice",                   │
│   Age = 25                          │
│ }                                   │
│                                     │
│ [Continue] [Step Over] [Stop]       │
└─────────────────────────────────────┘
```

---

### 10. 📝 SQL 执行日志查看器

**功能描述**: 记录和查看所有 SQL 执行

**界面布局**:
```
┌──────────────────────────────────────────────┐
│  Sqlx Execution Log               [Clear][⏸] │
├──────────────────────────────────────────────┤
│  🔍 Filter: [             ] 🔄 Auto-scroll ✅ │
│                                              │
│  ⏰ Time      │ 💾 Method         │ ⏱ Time  │
│  ────────────┼──────────────────┼─────────  │
│  12:34:56.123│ GetByIdAsync     │ 12.3 ms ✅│
│  12:34:56.145│ GetAllAsync      │ 45.7 ms ✅│
│  12:34:56.201│ InsertAsync      │ 8.9 ms ✅ │
│  12:34:56.215│ GetByIdAsync     │ 234 ms ⚠️ │
│                                              │
│  Selected: GetByIdAsync (12:34:56.123)       │
│  ┌────────────────────────────────────────┐ │
│  │ SQL: SELECT id, name FROM users        │ │
│  │      WHERE id = 123                    │ │
│  │                                        │ │
│  │ Parameters: @id = 123                  │ │
│  │ Result: 1 row                          │ │
│  │ Connection: Data Source=app.db         │ │
│  └────────────────────────────────────────┘ │
│                                              │
│  📊 Statistics  💾 Export  🔍 Filter  🗑 Clear│
└──────────────────────────────────────────────┘
```

---

## 🎯 功能优先级

### P0 - 核心增强 (必须实现)
1. 📊 **SQL 预览窗口** - 最直观的调试工具
2. 🔬 **生成代码查看器** - 理解 Sqlx 工作原理
3. 🧪 **查询测试工具** - 快速验证功能

### P1 - 高价值功能
4. 🗺️ **仓储导航器** - 项目概览
5. 📋 **占位符智能提示** - 提升编码体验
6. 📝 **SQL 执行日志** - 调试支持

### P2 - 增强功能
7. 🎨 **模板可视化编辑器** - 降低学习曲线
8. 📊 **性能分析器** - 性能优化
9. 🔗 **实体映射查看器** - 理解映射

### P3 - 高级功能
10. 🐛 **断点和监视** - 深度调试

---

## 🎨 UI/UX 设计原则

### 1. 一致性
- 统一的图标系统
- 统一的配色方案
- 统一的交互模式

### 2. 即时反馈
- 实时更新预览
- 加载状态指示
- 错误友好提示

### 3. 键盘友好
- 快捷键支持
- Tab 导航
- 命令面板集成

### 4. 深色主题适配
- 自动跟随 VS 主题
- 高对比度支持

---

## 📊 效果预期

### 开发效率提升
| 场景 | 原始耗时 | 使用扩展后 | 提升 |
|------|---------|-----------|------|
| 编写查询方法 | 5分钟 | 1分钟 | **80%** |
| 调试SQL问题 | 15分钟 | 3分钟 | **80%** |
| 测试查询 | 10分钟 | 2分钟 | **80%** |
| 查看生成代码 | 5分钟 | 30秒 | **90%** |
| 性能优化 | 30分钟 | 10分钟 | **67%** |

### 学习曲线
- **初学者**: 从 2小时 → **30分钟**
- **中级用户**: 快速掌握高级特性
- **高级用户**: 深度调试和性能优化

---

## 🚀 实施计划

### Phase 2.1 (4-6周)
- ✅ SQL 预览窗口
- ✅ 生成代码查看器
- ✅ 仓储导航器

### Phase 2.2 (3-4周)
- ✅ 查询测试工具
- ✅ SQL 执行日志
- ✅ 占位符智能提示

### Phase 2.3 (4-5周)
- ✅ 模板可视化编辑器
- ✅ 性能分析器
- ✅ 实体映射查看器

### Phase 3 (6-8周)
- ✅ 断点和监视集成
- ✅ 高级调试功能
- ✅ 性能监控

---

## 🎯 成功指标

### 技术指标
- ✅ 窗口响应时间 < 100ms
- ✅ SQL 预览延迟 < 500ms
- ✅ 代码查看器加载 < 1s
- ✅ 内存占用 < 100MB

### 用户指标
- ✅ 安装量 > 10,000
- ✅ 用户评分 > 4.5/5
- ✅ 使用频率 > 80%
- ✅ 用户反馈 > 90% 正面

---

## 📝 技术架构

### 核心组件

```
┌─────────────────────────────────────────┐
│         Sqlx.Extension.Core             │
├─────────────────────────────────────────┤
│                                         │
│  ┌─────────────┐  ┌─────────────────┐  │
│  │ UI Layer    │  │ Tool Windows    │  │
│  │ - WPF/XAML  │  │ - SQL Preview   │  │
│  │ - MVVM      │  │ - Code Viewer   │  │
│  └─────────────┘  │ - Query Tester  │  │
│                   │ - Navigator     │  │
│  ┌─────────────┐  └─────────────────┘  │
│  │ Services    │                        │
│  │ - Parser    │  ┌─────────────────┐  │
│  │ - Generator │  │ IntelliSense    │  │
│  │ - Analyzer  │  │ - Completion    │  │
│  └─────────────┘  │ - QuickInfo     │  │
│                   │ - Signature     │  │
│  ┌─────────────┐  └─────────────────┘  │
│  │ Roslyn API  │                        │
│  │ - Workspace │  ┌─────────────────┐  │
│  │ - Syntax    │  │ Diagnostics     │  │
│  │ - Semantics │  │ - Analyzers     │  │
│  └─────────────┘  │ - CodeFixes     │  │
│                   └─────────────────┘  │
│                                         │
│  ┌─────────────────────────────────┐   │
│  │    VS SDK Integration          │   │
│  │    - IVsTextView               │   │
│  │    - IVsOutputWindow           │   │
│  │    - IVsDebugger               │   │
│  └─────────────────────────────────┘   │
└─────────────────────────────────────────┘
```

---

## 📚 相关文档

- [VS Extension 开发指南](./VS_EXTENSION_DEV_GUIDE.md)
- [Sqlx Generator API](../src/Sqlx.Generator/README.md)
- [UI 设计规范](./UI_DESIGN_SPEC.md)
- [测试计划](./TESTING_PLAN.md)

---

## 🎉 总结

通过这些增强功能，Sqlx VS Extension 将成为：

1. **最直观的 ORM 开发工具**
   - 实时查看生成的 SQL 和代码
   - 可视化调试和测试

2. **最高效的开发助手**
   - 减少 80% 的调试时间
   - 提升 300% 的开发效率

3. **最易学的数据访问框架**
   - 从 2 小时降低到 30 分钟
   - 可视化学习 Sqlx 概念

---

**让 Sqlx 成为最易用、最直观的 .NET 数据访问解决方案！** 🚀


