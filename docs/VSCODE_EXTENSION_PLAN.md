# Sqlx Visual Studio 插件增强计划

> **版本**: 0.4.0
> **目标**: 极大提升开发者体验，简化 Sqlx 使用难度
> **最后更新**: 2025-10-29

---

## 📋 目录

1. [当前状态](#当前状态)
2. [核心目标](#核心目标)
3. [功能规划](#功能规划)
4. [实现优先级](#实现优先级)
5. [技术架构](#技术架构)
6. [开发计划](#开发计划)
7. [用户体验设计](#用户体验设计)

---

## 当前状态

### 现有内容
- ✅ 基础 VS 插件项目结构
- ✅ Package 注册和初始化
- ❌ 没有任何实际功能

### 痛点分析
用户在使用 Sqlx 时遇到的主要困难：

1. **学习曲线**
   - 需要记忆特性名称（`[SqlTemplate]`、`[SqlDefine]` 等）
   - 占位符语法不够直观（`{{columns}}`、`{{where}}` 等）
   - 不清楚哪些返回类型可用

2. **开发效率**
   - 需要手动编写接口和实现类
   - SQL 语句没有智能提示
   - 错误只在编译时发现

3. **调试困难**
   - 无法查看生成的代码
   - 不清楚 SQL 是如何生成的
   - 参数绑定错误难以发现

---

## 核心目标

### 总体目标
**让 Sqlx 的使用像 EF Core 一样简单，性能像 Dapper 一样快**

### 具体目标
1. ✅ **零学习成本** - 通过向导和模板快速上手
2. ✅ **智能提示** - 完整的 IntelliSense 支持
3. ✅ **实时验证** - 编码时即时发现错误
4. ✅ **可视化** - 直观展示生成的代码和 SQL
5. ✅ **自动化** - 减少手动编写的代码量

---

## 功能规划

### Phase 1: 基础增强（v0.5.0）🎯

#### 1.1 代码片段（Snippets）

**功能描述**: 提供丰富的代码片段模板

**具体内容**:

```csharp
// 快捷键: sqlx-repo
[SqlDefine(SqlDefineTypes.$DbType$)]
[RepositoryFor(typeof($Entity$))]
public interface I$Entity$Repository
{
    $end$
}

public partial class $Entity$Repository(DbConnection connection) : I$Entity$Repository { }

// 快捷键: sqlx-select
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE $column$ = @$param$")]
Task<$Entity$?> Get$MethodName$Async($ParamType$ $param$, CancellationToken ct = default);

// 快捷键: sqlx-insert
[SqlTemplate("INSERT INTO {{table}} ($columns$) VALUES ($values$)")]
[ReturnInsertedId]
Task<long> InsertAsync($Entity$ entity, CancellationToken ct = default);

// 快捷键: sqlx-batch
[SqlTemplate("INSERT INTO {{table}} ($columns$) VALUES {{batch_values}}")]
[BatchOperation(MaxBatchSize = $size$)]
Task<int> BatchInsertAsync(IEnumerable<$Entity$> entities, CancellationToken ct = default);

// 快捷键: sqlx-expr
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{where}}")]
Task<List<$Entity$>> Query$Name$Async(
    [ExpressionToSql] Expression<Func<$Entity$, bool>> predicate,
    CancellationToken ct = default);
```

**实现优先级**: 🔴 P0（最高）  
**预计工时**: 3天

---

#### 1.2 SqlTemplate 语法着色

**功能描述**: 为 SqlTemplate 字符串提供语法高亮和着色

**着色方案**:

```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE age >= @minAge")]
//            ^^^^^^ SQL关键字(蓝色)
//                   ^^^^^^^^^^ 占位符(橙色) 
//                                    ^^^^^ SQL关键字(蓝色)
//                                          ^^^^^^^^ 占位符(橙色)
//                                                   ^^^^^ SQL关键字(蓝色)
//                                                               ^^^^^^^ 参数(绿色)
```

**支持的元素**:

1. **SQL 关键字** (蓝色 `#569CD6`)
   - SELECT, INSERT, UPDATE, DELETE
   - FROM, WHERE, JOIN, ON
   - ORDER BY, GROUP BY, HAVING
   - LIMIT, OFFSET, DISTINCT
   - AND, OR, NOT, IN, LIKE, BETWEEN

2. **占位符** (橙色 `#CE9178`)
   - `{{columns}}`, `{{table}}`, `{{values}}`
   - `{{where}}`, `{{set}}`, `{{batch_values}}`
   - `{{limit}}`, `{{offset}}`, `{{orderby}}`

3. **参数** (绿色 `#4EC9B0`)
   - `@id`, `@name`, `@age` 等
   - `@{paramName}` 语法

4. **字符串字面量** (棕色 `#D69D85`)
   - 单引号字符串 `'value'`

5. **注释** (灰色 `#6A9955`)
   - `-- 单行注释`
   - `/* 多行注释 */`

**界面效果**:

<img src="../docs/images/syntax-highlight-preview.png" alt="语法高亮预览" />

**功能点**:

- ✅ 实时语法着色（无性能影响）
- ✅ 支持多行 SQL
- ✅ 支持嵌套字符串
- ✅ 可自定义颜色主题
- ✅ 与 VS 主题集成

**实现优先级**: 🔴 P0（最高）  
**预计工时**: 4天

---

#### 1.3 快速操作（Quick Actions）

**功能描述**: 右键菜单快速生成代码

**功能点**:

1. **从实体类生成仓储**
   ```
   右键 User.cs → Sqlx: Generate Repository
   → 自动生成 IUserRepository.cs 和 UserRepository.cs
   ```

2. **生成 CRUD 方法**
   ```
   右键接口 → Sqlx: Add CRUD Methods
   → 自动添加 8 个标准方法
   ```

3. **生成自定义查询**
   ```
   右键接口 → Sqlx: Add Query Method
   → 弹出对话框，选择查询类型（SELECT/INSERT/UPDATE/DELETE）
   → 自动生成方法和 SQL 模板
   ```

**实现优先级**: 🔴 P0
**预计工时**: 5天

---

### Phase 2: 智能增强（v0.6.0）🚀

#### 2.1 SQL 智能提示

**功能描述**: 在 SqlTemplate 中提供 SQL 智能感知

**功能点**:

1. **占位符自动完成**
   ```csharp
   [SqlTemplate("SELECT {{col↓")]
   // 自动提示: columns, table, values, where, limit, offset...
   ```

2. **表名和列名提示**
   ```csharp
   [SqlTemplate("SELECT * FROM us↓")]
   // 自动提示: users, user_profiles...（从项目中的实体类推断）
   ```

3. **SQL 关键字高亮**
   - SELECT, INSERT, UPDATE, DELETE 高亮
   - WHERE, ORDER BY, GROUP BY 高亮

**实现优先级**: 🟠 P1
**预计工时**: 8天

---

#### 2.2 参数验证和提示

**功能描述**: 实时验证 SQL 参数和方法参数的匹配

**功能点**:

1. **参数不匹配警告**
   ```csharp
   [SqlTemplate("SELECT * FROM users WHERE id = @userId")]
   Task<User?> GetByIdAsync(long id);  // ⚠️ 警告: 参数 @userId 未找到
   ```

2. **参数建议**
   ```csharp
   [SqlTemplate("SELECT * FROM users WHERE age >= @↓")]
   // 自动提示方法参数: id, name, age...
   ```

3. **类型不匹配警告**
   ```csharp
   [SqlTemplate("SELECT COUNT(*) FROM users")]
   Task<User?> CountAsync();  // ❌ 错误: 返回类型应该是 Task<long>
   ```

**实现优先级**: 🔴 P0
**预计工时**: 6天

---

#### 2.3 实时诊断和建议

**功能描述**: 在编辑器中实时显示诊断信息

**诊断类型**:

| 诊断 ID | 严重性 | 说明 | 示例 |
|---------|--------|------|------|
| SQLX001 | Error | 缺少必需特性 | 接口没有 `[SqlDefine]` |
| SQLX002 | Error | SQL 参数不匹配 | `@userId` 找不到对应参数 |
| SQLX003 | Warning | 返回类型建议 | COUNT 查询建议返回 `Task<long>` |
| SQLX004 | Info | 性能建议 | 建议使用批量操作 |
| SQLX005 | Warning | 缺少索引建议 | WHERE 子句中的列建议添加索引 |
| SQLX006 | Error | 表达式不支持 | `u => u.Age.ToString()` 不支持 |
| SQLX007 | Warning | SQL注入风险 | 检测到字符串拼接 |
| SQLX008 | Info | 占位符可优化 | 可以使用 `{{columns}}` 替换 |

**快速修复（Code Fixes）**:

```csharp
// 诊断: SQLX001 - 缺少 [SqlDefine]
public interface IUserRepository { }
// 快速修复: 添加 [SqlDefine] 特性
// → 弹出数据库类型选择菜单

// 诊断: SQLX002 - 参数不匹配
[SqlTemplate("WHERE id = @userId")]
Task<User?> GetAsync(long id);
// 快速修复1: 重命名参数为 userId
// 快速修复2: 将 SQL 中改为 @id
```

**实现优先级**: 🟠 P1
**预计工时**: 10天

---

### Phase 3: 可视化工具（v0.7.0）✨

#### 3.1 生成代码查看器

**功能描述**: 实时查看源生成器生成的代码

**界面设计**:

```
┌─────────────────────────────────────────────────┐
│  Sqlx Generated Code Viewer                    │
├─────────────────────────────────────────────────┤
│  Repository: UserRepository                     │
│  Methods: [GetByIdAsync ▼] [InsertAsync] ...   │
├─────────────────────────────────────────────────┤
│  // Generated code for GetByIdAsync             │
│  public async Task<User?> GetByIdAsync(         │
│      long id,                                    │
│      CancellationToken ct = default)            │
│  {                                               │
│      using var cmd = _connection.CreateCommand();│
│      cmd.CommandText = "SELECT id, name, age    │
│                         FROM users               │
│                         WHERE id = @id";         │
│      cmd.Parameters.AddWithValue("@id", id);    │
│      ...                                         │
│  }                                               │
├─────────────────────────────────────────────────┤
│  [Copy Code] [Go to Definition] [Refresh]      │
└─────────────────────────────────────────────────┘
```

**功能点**:
- ✅ 实时显示生成的代码
- ✅ 代码语法高亮
- ✅ 跳转到定义
- ✅ 复制代码

**实现优先级**: 🟡 P2
**预计工时**: 7天

---

#### 3.2 SQL 预览器

**功能描述**: 预览最终生成的 SQL 语句

**界面设计**:

```
┌─────────────────────────────────────────────────┐
│  SQL Preview                                     │
├─────────────────────────────────────────────────┤
│  Method: GetByIdAsync                            │
│  Database: SQLite                                │
├─────────────────────────────────────────────────┤
│  Template:                                       │
│  SELECT {{columns}} FROM {{table}} WHERE id=@id  │
├─────────────────────────────────────────────────┤
│  Generated SQL (SQLite):                         │
│  SELECT id, name, age, email, created_at         │
│  FROM users                                      │
│  WHERE id = @id                                  │
├─────────────────────────────────────────────────┤
│  Parameters:                                     │
│  • @id (Int64) = ?                              │
├─────────────────────────────────────────────────┤
│  [Copy SQL] [Test Query] [Switch Database ▼]   │
└─────────────────────────────────────────────────┘
```

**功能点**:
- ✅ 显示模板和最终 SQL
- ✅ 参数列表
- ✅ 多数据库切换预览
- ✅ 复制 SQL
- ✅ 测试查询（连接数据库执行）

**实现优先级**: 🟡 P2
**预计工时**: 8天

---

#### 3.3 实体类设计器

**功能描述**: 可视化设计实体类和关系

**界面设计**:

```
┌─────────────────────────────────────────────────┐
│  Sqlx Entity Designer                           │
├──────────────┬──────────────────────────────────┤
│  Entities    │  User                            │
│  ├─ User     │  ├─ Id: long [PK]               │
│  ├─ Order    │  ├─ Name: string                │
│  ├─ Product  │  ├─ Email: string?              │
│  └─ ...      │  ├─ Age: int                    │
│              │  ├─ IsActive: bool              │
│  [+ Add]     │  └─ CreatedAt: DateTime         │
│              │                                  │
│              │  [TableName]: users              │
│              │  [ ] SoftDelete                  │
│              │  [ ] AuditFields                 │
│              │  [ ] ConcurrencyCheck            │
│              │                                  │
│              │  [Generate Repository]           │
│              │  [Generate Migration]            │
└──────────────┴──────────────────────────────────┘
```

**功能点**:
- ✅ 可视化实体设计
- ✅ 属性类型选择
- ✅ 特性配置（TableName、SoftDelete 等）
- ✅ 生成实体类代码
- ✅ 生成数据库迁移脚本

**实现优先级**: 🟢 P3（低优先级）
**预计工时**: 15天

---

### Phase 4: 高级功能（v0.8.0）🎨

#### 4.1 数据库连接管理器

**功能描述**: 管理项目中的数据库连接

**界面设计**:

```
┌─────────────────────────────────────────────────┐
│  Database Connections                           │
├─────────────────────────────────────────────────┤
│  ☑ Development (SQLite)                         │
│     Data Source=app.db                          │
│     [Test] [Edit] [Set as Default]             │
│                                                  │
│  ☐ Staging (PostgreSQL)                         │
│     Host=localhost;Database=app_staging         │
│     [Test] [Edit]                               │
│                                                  │
│  ☐ Production (PostgreSQL)                      │
│     Host=prod.db.com;Database=app               │
│     [Test] [Edit]                               │
├─────────────────────────────────────────────────┤
│  [+ Add Connection] [Import from appsettings]  │
└─────────────────────────────────────────────────┘
```

**功能点**:
- ✅ 管理多个数据库连接
- ✅ 测试连接
- ✅ 从 appsettings.json 导入
- ✅ 设置默认连接
- ✅ 执行测试查询

**实现优先级**: 🟡 P2
**预计工时**: 6天

---

#### 4.2 查询测试工具

**功能描述**: 在 IDE 中直接测试 SQL 查询

**界面设计**:

```
┌─────────────────────────────────────────────────┐
│  Query Tester                                   │
├─────────────────────────────────────────────────┤
│  Repository: UserRepository                     │
│  Method: GetByIdAsync                           │
│  Connection: Development ▼                      │
├─────────────────────────────────────────────────┤
│  Parameters:                                     │
│  • id: [       1       ]                        │
│  • ct: [CancellationToken.None]                │
├─────────────────────────────────────────────────┤
│  [▶ Execute] [Clear] [Save Test]               │
├─────────────────────────────────────────────────┤
│  Results: (1 row, 15ms)                         │
│  {                                               │
│    "Id": 1,                                      │
│    "Name": "Alice",                              │
│    "Age": 25,                                    │
│    "Email": "alice@example.com"                  │
│  }                                               │
└─────────────────────────────────────────────────┘
```

**功能点**:
- ✅ 选择方法和连接
- ✅ 输入参数值
- ✅ 执行查询
- ✅ 显示结果（JSON 格式）
- ✅ 显示执行时间
- ✅ 保存测试用例

**实现优先级**: 🟢 P3
**预计工时**: 8天

---

#### 4.3 性能分析器

**功能描述**: 分析查询性能，提供优化建议

**界面设计**:

```
┌─────────────────────────────────────────────────┐
│  Performance Analyzer                           │
├─────────────────────────────────────────────────┤
│  Method: GetUsersByAgeAsync                     │
│  SQL: SELECT * FROM users WHERE age >= @minAge  │
├─────────────────────────────────────────────────┤
│  ⚠️ Performance Issues (2)                      │
│                                                  │
│  1. Missing Index                                │
│     Column 'age' in WHERE clause has no index   │
│     → CREATE INDEX idx_users_age ON users(age)  │
│                                                  │
│  2. SELECT *                                     │
│     Selecting all columns may be inefficient    │
│     → Use {{columns}} or specify needed columns │
├─────────────────────────────────────────────────┤
│  ✅ Good Practices (1)                          │
│                                                  │
│  1. Parameterized Query                          │
│     Using parameters prevents SQL injection     │
├─────────────────────────────────────────────────┤
│  Estimated Performance: 🟡 Medium                │
│  [Apply Fixes] [Ignore] [Learn More]           │
└─────────────────────────────────────────────────┘
```

**检查项**:
- ✅ 缺少索引警告
- ✅ SELECT * 检测
- ✅ N+1 查询检测
- ✅ 未使用占位符
- ✅ 大结果集警告（建议 LIMIT）
- ✅ 复杂 JOIN 分析

**实现优先级**: 🟢 P3
**预计工时**: 10天

---

#### 4.4 迁移脚本生成器

**功能描述**: 从实体类生成数据库迁移脚本

**功能点**:

1. **初始迁移**
   ```
   右键 Models/ → Sqlx: Generate Initial Migration
   → 生成所有表的 CREATE TABLE 脚本
   ```

2. **增量迁移**
   ```
   修改实体类 → Sqlx: Generate Migration
   → 分析变更，生成 ALTER TABLE 脚本
   ```

3. **多数据库支持**
   - SQLite
   - MySQL
   - PostgreSQL
   - SQL Server
   - Oracle

**生成示例**:

```sql
-- Migration: AddEmailToUsers_20251029_143052
-- Database: SQLite

-- Up
ALTER TABLE users ADD COLUMN email TEXT;
CREATE INDEX idx_users_email ON users(email);

-- Down
ALTER TABLE users DROP COLUMN email;
```

**实现优先级**: 🟢 P3
**预计工时**: 12天

---

### Phase 5: 团队协作（v0.9.0）👥

#### 5.1 代码审查助手

**功能描述**: 帮助团队保持代码质量

**功能点**:

1. **代码风格检查**
   - 命名规范（方法名、参数名）
   - SQL 格式化
   - 注释完整性

2. **最佳实践检查**
   - 是否使用批量操作
   - 是否添加索引
   - 是否使用事务
   - 是否处理 NULL

3. **安全检查**
   - SQL 注入风险
   - 敏感信息泄露
   - 权限检查

**实现优先级**: 🔵 P4（未来）
**预计工时**: 8天

---

#### 5.2 文档生成器

**功能描述**: 自动生成 API 文档

**生成内容**:

```markdown
# UserRepository API Documentation

## GetByIdAsync

**Description**: Get user by ID

**SQL Template**:
```sql
SELECT {{columns}} FROM users WHERE id = @id
```

**Parameters**:
- `id` (long): User ID

**Returns**: `Task<User?>` - User entity or null

**Example**:
```csharp
var user = await repo.GetByIdAsync(1);
```
```

**实现优先级**: 🔵 P4
**预计工时**: 5天

---

## 实现优先级

### P0 - 必须有（v0.5.0）🔴
1. ✅ 代码片段（Snippets）- 3天
2. ✅ SqlTemplate 语法着色 - 4天
3. ✅ 快速操作（Quick Actions）- 5天
4. ✅ 参数验证和提示 - 6天

**总计**: 18天

### P1 - 应该有（v0.6.0）🟠
5. ✅ SQL 智能提示 - 8天
6. ✅ 实时诊断和建议 - 10天

**总计**: 18天

### P2 - 最好有（v0.7.0）🟡
7. ✅ 生成代码查看器 - 7天
8. ✅ SQL 预览器 - 8天
9. ✅ 数据库连接管理器 - 6天

**总计**: 21天

### P3 - 可以有（v0.8.0）🟢
10. ✅ 实体类设计器 - 15天
11. ✅ 查询测试工具 - 8天
12. ✅ 性能分析器 - 10天
13. ✅ 迁移脚本生成器 - 12天

**总计**: 45天

### P4 - 未来（v0.9.0+）🔵
14. 代码审查助手 - 8天
15. 文档生成器 - 5天

**总计**: 13天

---

## 技术架构

### 插件架构

```
Sqlx.Extension (VS Extension)
├── Core/
│   ├── SqlxPackage.cs                 // 主包
│   ├── LanguageService.cs             // 语言服务
│   ├── DiagnosticAnalyzer.cs          // 诊断分析器
│   └── SyntaxHighlighter.cs           // 语法着色器
├── Features/
│   ├── Snippets/
│   │   └── SqlxSnippets.cs
│   ├── SyntaxColoring/
│   │   ├── SqlTemplateClassifier.cs   // 语法分类器
│   │   ├── SqlTokenTagger.cs          // 标记器
│   │   └── SqlColorProvider.cs        // 颜色提供者
│   ├── QuickActions/
│   │   ├── GenerateRepositoryAction.cs
│   │   ├── AddCrudMethodsAction.cs
│   │   └── AddQueryMethodAction.cs
│   ├── IntelliSense/
│   │   ├── SqlCompletionProvider.cs
│   │   ├── PlaceholderCompletionProvider.cs
│   │   └── ParameterCompletionProvider.cs
│   ├── Diagnostics/
│   │   ├── ParameterMismatchAnalyzer.cs
│   │   ├── ReturnTypeAnalyzer.cs
│   │   └── PerformanceAnalyzer.cs
│   ├── CodeFixes/
│   │   ├── AddAttributeCodeFix.cs
│   │   ├── RenameParameterCodeFix.cs
│   │   └── ChangeReturnTypeCodeFix.cs
│   └── ToolWindows/
│       ├── GeneratedCodeViewer.cs
│       ├── SqlPreview.cs
│       └── QueryTester.cs
└── Resources/
    ├── Icons/
    ├── Snippets/
    └── ColorSchemes/
```

### 技术栈

| 组件 | 技术 | 版本 |
|------|------|------|
| **VS SDK** | Microsoft.VisualStudio.SDK | 17.0+ |
| **语言服务** | Microsoft.VisualStudio.LanguageServices | 17.0+ |
| **Roslyn** | Microsoft.CodeAnalysis | 4.8+ |
| **UI 框架** | WPF | .NET 4.7.2+ |
| **测试** | xUnit + Moq | Latest |

---

## 开发计划

### Sprint 1: 基础设施（Week 1-2）

**目标**: 搭建插件基础架构

- [ ] 升级项目结构
- [ ] 添加必要的 NuGet 包
- [ ] 实现语言服务基础
- [ ] 设置 CI/CD 流程

**交付物**:
- ✅ 插件可以加载到 VS
- ✅ 基础架构就绪
- ✅ 测试框架搭建完成

---

### Sprint 2: 代码片段 + 语法着色（Week 3-4）

**目标**: 实现 P0 基础功能

- [ ] 实现所有代码片段
- [ ] 实现 SqlTemplate 语法着色器
  - [ ] SQL 关键字识别和着色
  - [ ] 占位符识别和着色
  - [ ] 参数识别和着色
  - [ ] 支持多行 SQL
  - [ ] 主题集成
- [ ] 编写单元测试

**交付物**:
- ✅ 10+ 代码片段
- ✅ 完整的语法着色器
- ✅ 单元测试覆盖率 > 80%

---

### Sprint 3: 快速操作 + 参数验证（Week 5-6）

**目标**: 实现代码生成和验证

- [ ] 实现"生成仓储"快速操作
- [ ] 实现"添加 CRUD 方法"快速操作
- [ ] 实现参数不匹配诊断
- [ ] 实现快速修复
- [ ] 集成测试

**交付物**:
- ✅ 3 个快速操作
- ✅ 参数验证功能
- ✅ 5+ 快速修复
- ✅ 文档更新

---

### Sprint 4-6: SQL 智能提示 + 诊断（Week 7-12）

**目标**: 实现 P1 功能

- [ ] SQL 智能提示
- [ ] 占位符自动完成
- [ ] 8 种诊断分析器
- [ ] 性能优化

---

### Sprint 7-9: 可视化工具（Week 13-18）

**目标**: 实现 P2 功能

- [ ] 生成代码查看器
- [ ] SQL 预览器
- [ ] 数据库连接管理器

---

### Sprint 10+: 高级功能（Week 19+）

**目标**: 实现 P3、P4 功能

- [ ] 按优先级逐步实现

---

## 用户体验设计

### 新用户体验（First-time User Experience）

#### 1. 欢迎页面

首次安装插件后显示：

```
┌────────────────────────────────────────────┐
│  Welcome to Sqlx!                          │
├────────────────────────────────────────────┤
│  🚀 High-performance .NET data access      │
│                                             │
│  Get Started:                               │
│  • [Create New Project]                     │
│  • [Add to Existing Project]                │
│  • [View Samples]                           │
│  • [Read Documentation]                     │
│                                             │
│  Quick Tips:                                │
│  • Type 'sqlx-repo' to create repository   │
│  • Right-click entity → Generate Repository│
│  • Use {{columns}} for automatic columns   │
│                                             │
│  [Show this on startup ☑]  [Close]        │
└────────────────────────────────────────────┘
```

---

#### 2. 引导式向导

**场景**: 创建第一个仓储

```
Step 1/4: Choose Database
┌────────────────────────────────────────────┐
│ Select your database type:                 │
│                                             │
│ ○ SQLite (recommended for getting started)│
│ ○ PostgreSQL                                │
│ ○ MySQL                                     │
│ ○ SQL Server                                │
│ ○ Oracle                                    │
│                                             │
│ [Previous] [Next]                          │
└────────────────────────────────────────────┘

Step 2/4: Configure Entity
┌────────────────────────────────────────────┐
│ Select entity class:                        │
│ [User ▼]                                    │
│                                             │
│ Table name: [users____]                     │
│                                             │
│ Advanced options:                           │
│ ☐ Enable soft delete                       │
│ ☐ Add audit fields                         │
│ ☐ Add optimistic concurrency               │
│                                             │
│ [Previous] [Next]                          │
└────────────────────────────────────────────┘

Step 3/4: Choose Methods
┌────────────────────────────────────────────┐
│ Select methods to generate:                 │
│                                             │
│ ☑ CRUD Operations (8 methods)              │
│   ├─ GetByIdAsync                          │
│   ├─ GetAllAsync                           │
│   ├─ InsertAsync                           │
│   └─ ...                                    │
│                                             │
│ ☑ Custom Queries                            │
│   [+ Add Query]                            │
│                                             │
│ [Previous] [Next]                          │
└────────────────────────────────────────────┘

Step 4/4: Review and Generate
┌────────────────────────────────────────────┐
│ Preview:                                    │
│                                             │
│ Files to create:                            │
│ • IUserRepository.cs (interface)           │
│ • UserRepository.cs (implementation)       │
│                                             │
│ Methods: 8                                  │
│ Database: SQLite                            │
│                                             │
│ [Previous] [Generate]                      │
└────────────────────────────────────────────┘
```

---

### 日常使用体验

#### 智能提示示例

```csharp
public interface IUserRepository
{
    // 输入 sqlx 触发代码片段
    [SqlTemplate("SELECT {{col|↓")]
    //                    ↑ 自动提示:
    //                      • {{columns}}
    //                      • {{table}}
    //                      • {{where}}

    // 输入参数自动提示
    [SqlTemplate("SELECT * FROM users WHERE id = @|")]
    //                                            ↑ 自动提示方法参数

    // 返回类型警告
    [SqlTemplate("SELECT COUNT(*) FROM users")]
    Task<User?> CountAsync();  // ⚠️ 警告: 建议返回 Task<long>
    //                              💡 快速修复: 更改为 Task<long>
}
```

---

### 错误处理

#### 友好的错误消息

**❌ 之前**:
```
error CS0535: 'UserRepository' does not implement interface member 'IUserRepository.GetByIdAsync(long)'
```

**✅ 现在**:
```
Sqlx: 未找到方法实现

接口 'IUserRepository' 中的方法 'GetByIdAsync' 没有实现。

可能原因:
1. 项目未重新编译（源生成器需要编译才能生成代码）
2. 接口和实现在同一文件中
3. 缺少必需的特性标记

建议操作:
• [重新生成项目] - 触发源生成器
• [检查特性] - 验证 [SqlDefine] 和 [RepositoryFor]
• [查看生成的代码] - 打开生成代码查看器

[了解更多]
```

---

## 成功指标

### 量化指标

| 指标 | 目标 | 当前 |
|------|------|------|
| 下载量 | 10,000+ | 0 |
| 评分 | 4.5+ / 5.0 | N/A |
| 活跃用户 | 1,000+ | 0 |
| 日均使用时长 | 30+ 分钟 | 0 |

### 用户满意度

**调查问题**:
1. 插件是否提升了您的开发效率？（1-5分）
2. 哪个功能最有用？
3. 还希望看到什么功能？
4. 遇到过什么问题？

**目标**: 90% 用户满意度（4分以上）

---

## 风险和挑战

### 技术风险

| 风险 | 影响 | 概率 | 缓解措施 |
|------|------|------|---------|
| VS SDK API 变更 | 高 | 中 | 定期跟进 VS 更新 |
| Roslyn API 复杂度 | 中 | 高 | 充分学习和测试 |
| 性能问题 | 高 | 中 | 性能测试和优化 |
| 多 VS 版本兼容 | 中 | 高 | 支持 VS 2022+ |

### 资源风险

| 风险 | 影响 | 概率 | 缓解措施 |
|------|------|------|---------|
| 开发时间不足 | 高 | 中 | 优先实现 P0、P1 |
| 测试覆盖不足 | 中 | 中 | 自动化测试 |
| 文档滞后 | 低 | 高 | 同步更新文档 |

---

## 总结

### 核心价值

**让 Sqlx 成为 .NET 开发者的首选数据访问方案**

通过 VS 插件，我们将：
1. ✅ **降低学习门槛** - 新手 5 分钟上手
2. ✅ **提升开发效率** - 减少 80% 手动代码
3. ✅ **提升代码可读性** - SQL 语法高亮着色
4. ✅ **保证代码质量** - 实时检测和修复
5. ✅ **增强开发体验** - 可视化和智能提示

### 核心特色

**SqlTemplate 语法着色** 🎨
- 实时高亮 SQL 关键字、占位符、参数
- 提升代码可读性 50%+
- 减少语法错误 60%+

**智能代码片段** ⚡
- 10+ 常用代码片段
- 30秒创建完整仓储
- 效率提升 95%+

**实时诊断验证** 🔍
- 编码时即时发现错误
- 智能快速修复建议
- 减少调试时间 70%+

### 路线图

```
v0.5.0 (Sprint 1-3) - 基础增强         📅 Q1 2026
  ✅ 代码片段
  ✅ SqlTemplate 语法着色 🎨
  ✅ 快速操作
  ✅ 参数验证

v0.6.0 (Sprint 4-6) - 智能增强         📅 Q2 2026
  ✅ SQL 智能提示
  ✅ 实时诊断

v0.7.0 (Sprint 7-9) - 可视化工具       📅 Q3 2026
  ✅ 代码查看器
  ✅ SQL 预览器

v0.8.0 (Sprint 10+) - 高级功能         📅 Q4 2026
  ✅ 测试工具
  ✅ 性能分析

v0.9.0 (未来)       - 团队协作         📅 2027+
  ✅ 代码审查
  ✅ 文档生成
```

---

**文档版本**: 0.4.0
**最后更新**: 2025-10-29
**维护**: Sqlx Team

