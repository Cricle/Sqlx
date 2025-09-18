# Sqlx 3.0 API 参考文档

本文档详细介绍Sqlx 3.0的所有公共API。

## 🏗️ 核心架构

```
Sqlx 3.0
├── ParameterizedSql        # 参数化SQL执行实例
├── SqlTemplate            # 可重用SQL模板  
├── ExpressionToSql<T>      # 类型安全查询构建器
├── SqlDefine              # 数据库方言定义
└── Extensions             # 扩展方法和工具
```

## 📋 ParameterizedSql

参数化SQL的执行实例，表示带参数的SQL语句。

### 构造方法
```csharp
public readonly record struct ParameterizedSql(string Sql, IReadOnlyDictionary<string, object?> Parameters)
```

### 静态方法
```csharp
// 使用匿名对象创建
public static ParameterizedSql Create(string sql, object? parameters)

// 使用字典创建  
public static ParameterizedSql CreateWithDictionary(string sql, Dictionary<string, object?> parameters)
```

### 实例方法
```csharp
// 渲染最终SQL（内联参数值）
public string Render()
```

### 静态属性
```csharp
// 空实例
public static readonly ParameterizedSql Empty
```

### 使用示例
```csharp
// 创建参数化SQL
var sql = ParameterizedSql.Create(
    "SELECT * FROM Users WHERE Age > @age", 
    new { age = 18 });

// 渲染最终SQL
string finalSql = sql.Render();
// 输出: SELECT * FROM Users WHERE Age > 18

// 使用字典
var sqlDict = ParameterizedSql.CreateWithDictionary(
    "SELECT * FROM Users WHERE Name = @name",
    new Dictionary<string, object?> { ["name"] = "John" });
```

---

## 📋 SqlTemplate

可重用的SQL模板，支持参数绑定和多次执行。

### 构造方法
```csharp
public readonly record struct SqlTemplate(string Sql, IReadOnlyDictionary<string, object?> Parameters)
```

### 静态方法
```csharp
// 解析SQL字符串为模板
public static SqlTemplate Parse(string sql)
```

### 静态属性
```csharp
// 空模板
public static readonly SqlTemplate Empty
```

### 实例方法
```csharp
// 执行模板（使用匿名对象参数）
public ParameterizedSql Execute(object? parameters = null)

// 执行模板（使用字典参数）
public ParameterizedSql Execute(Dictionary<string, object?> parameters)

// 创建流式参数绑定器
public SqlTemplateBuilder Bind()

// 渲染模板（等同于Execute().Render()）
public ParameterizedSql Render(object? parameters)
public ParameterizedSql Render(Dictionary<string, object?> parameters)

// 字符串表示
public override string ToString()
```

### 实例属性
```csharp
// 是否为纯模板（无预绑定参数）
public bool IsPureTemplate { get; }
```

### 使用示例
```csharp
// 创建模板
var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Age > @age AND IsActive = @active");

// 多次执行
var young = template.Execute(new { age = 18, active = true });
var senior = template.Execute(new { age = 65, active = true });

// 流式绑定
var custom = template.Bind()
    .Param("age", 25)
    .Param("active", true)
    .Build();
```

---

## 📋 SqlTemplateBuilder

流式SQL模板参数绑定器。

### 实例方法
```csharp
// 绑定单个参数
public SqlTemplateBuilder Param<T>(string name, T value)

// 批量绑定参数
public SqlTemplateBuilder Params(object? parameters)

// 构建最终的ParameterizedSql
public ParameterizedSql Build()
```

### 使用示例
```csharp
var template = SqlTemplate.Parse("SELECT * FROM Users WHERE Age > @age AND Name = @name");

var result = template.Bind()
    .Param("age", 18)
    .Param("name", "John")
    .Build();

string sql = result.Render();
```

---

## 📋 ExpressionToSql<T>

类型安全的查询构建器，支持LINQ表达式到SQL的转换。

### 静态工厂方法
```csharp
// 创建查询构建器
public static ExpressionToSql<T> Create(SqlDialect dialect)

// 便捷工厂方法
public static ExpressionToSql<T> ForSqlServer()
public static ExpressionToSql<T> ForMySql() 
public static ExpressionToSql<T> ForPostgreSQL()
public static ExpressionToSql<T> ForSQLite()
```

### SELECT 相关方法
```csharp
// 选择指定列
public ExpressionToSql<T> Select(params string[] cols)
public ExpressionToSql<T> Select<TResult>(Expression<Func<T, TResult>> selector)
public ExpressionToSql<T> Select(params Expression<Func<T, object>>[] selectors)
```

### WHERE 相关方法
```csharp
// 添加WHERE条件
public ExpressionToSql<T> Where(Expression<Func<T, bool>> predicate)

// 添加AND条件（等同于Where）
public ExpressionToSql<T> And(Expression<Func<T, bool>> predicate)
```

### ORDER BY 相关方法
```csharp
// 升序排序
public ExpressionToSql<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)

// 降序排序  
public ExpressionToSql<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
```

### 分页方法
```csharp
// 限制返回行数
public ExpressionToSql<T> Take(int count)

// 跳过指定行数
public ExpressionToSql<T> Skip(int count)
```

### INSERT 相关方法
```csharp
// 创建INSERT操作
public ExpressionToSql<T> Insert()

// 指定插入列（AOT友好，推荐）
public ExpressionToSql<T> InsertInto(Expression<Func<T, object>> selector)

// 自动推断所有列（使用反射，不推荐AOT）
public ExpressionToSql<T> InsertIntoAll()

// 指定插入值
public ExpressionToSql<T> Values(params object[] values)

// 添加多行值
public ExpressionToSql<T> AddValues(params object[] values)

// INSERT SELECT
public ExpressionToSql<T> InsertSelect(string sql)
public ExpressionToSql<T> InsertSelect<TSource>(ExpressionToSql<TSource> query)
```

### UPDATE 相关方法
```csharp
// 创建UPDATE操作
public ExpressionToSql<T> Update()

// 设置列值
public ExpressionToSql<T> Set<TValue>(Expression<Func<T, TValue>> selector, TValue value)

// 使用表达式设置列值
public ExpressionToSql<T> Set<TValue>(Expression<Func<T, TValue>> selector, Expression<Func<T, TValue>> valueExpression)
```

### DELETE 相关方法
```csharp
// 创建DELETE操作
public ExpressionToSql<T> Delete()

// 创建DELETE操作并添加WHERE条件
public ExpressionToSql<T> Delete(Expression<Func<T, bool>> predicate)
```

### GROUP BY 相关方法
```csharp
// 添加GROUP BY子句
public GroupedExpressionToSql<T, TKey> GroupBy<TKey>(Expression<Func<T, TKey>> keySelector)

// 添加HAVING条件
public ExpressionToSql<T> Having(Expression<Func<T, bool>> predicate)
```

### 输出方法
```csharp
// 转换为SQL字符串
public string ToSql()

// 转换为可重用模板
public SqlTemplate ToTemplate()

// 生成WHERE子句部分
public string ToWhereClause()

// 生成额外子句（GROUP BY, HAVING, ORDER BY, LIMIT, OFFSET）
public string ToAdditionalClause()
```

### 配置方法
```csharp
// 启用参数化查询模式
public ExpressionToSql<T> UseParameterizedQueries()
```

### 使用示例
```csharp
// SELECT查询
var selectQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Select(u => new { u.Name, u.Email })
    .Where(u => u.Age > 18)
    .OrderBy(u => u.Name)
    .Take(10);

string selectSql = selectQuery.ToSql();

// INSERT操作
var insertQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .InsertInto(u => new { u.Name, u.Email })
    .Values("John", "john@example.com");

string insertSql = insertQuery.ToSql();

// UPDATE操作
var updateQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Update()
    .Set(u => u.Name, "New Name")
    .Where(u => u.Id == 1);

string updateSql = updateQuery.ToSql();

// DELETE操作
var deleteQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .Delete(u => u.IsActive == false);

string deleteSql = deleteQuery.ToSql();
```

---

## 📋 GroupedExpressionToSql<T, TKey>

分组查询对象，支持聚合操作。

### 实例方法
```csharp
// 选择分组结果的投影
public ExpressionToSql<TResult> Select<TResult>(Expression<Func<IGrouping<TKey, T>, TResult>> selector)

// 添加HAVING条件
public GroupedExpressionToSql<T, TKey> Having(Expression<Func<IGrouping<TKey, T>, bool>> predicate)

// 输出SQL
public string ToSql()
public SqlTemplate ToTemplate()
```

### 使用示例
```csharp
var groupQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer)
    .GroupBy(u => u.Department)
    .Select(g => new 
    { 
        Department = g.Key,
        Count = g.Count(),
        AvgAge = g.Average(u => u.Age)
    })
    .Having(g => g.Count() > 5);

string sql = groupQuery.ToSql();
```

---

## 📋 SqlDefine

数据库方言定义，提供预定义的数据库支持。

### 静态属性
```csharp
// SQL Server方言: [column] with @ parameters
public static readonly SqlDialect SqlServer

// MySQL方言: `column` with @ parameters
public static readonly SqlDialect MySql

// PostgreSQL方言: "column" with $ parameters  
public static readonly SqlDialect PostgreSql
public static readonly SqlDialect PgSql  // 别名

// SQLite方言: [column] with $ parameters
public static readonly SqlDialect SQLite
public static readonly SqlDialect Sqlite  // 别名

// Oracle方言: "column" with : parameters
public static readonly SqlDialect Oracle

// DB2方言
public static readonly SqlDialect DB2
```

### 使用示例
```csharp
// 使用不同数据库方言
var sqlServerQuery = ExpressionToSql<User>.Create(SqlDefine.SqlServer);
var mysqlQuery = ExpressionToSql<User>.Create(SqlDefine.MySql);
var postgresQuery = ExpressionToSql<User>.Create(SqlDefine.PostgreSql);
var sqliteQuery = ExpressionToSql<User>.Create(SqlDefine.SQLite);
```

---

## 📋 SqlDialect

数据库方言配置，定义SQL语法规则。

### 构造方法
```csharp
public record SqlDialect(
    string ColumnPrefix,     // 列名前缀
    string ColumnSuffix,     // 列名后缀  
    string StringPrefix,     // 字符串前缀
    string StringSuffix,     // 字符串后缀
    string ParameterPrefix   // 参数前缀
)
```

### 实例方法
```csharp
// 包装列名
public string WrapColumn(string columnName)

// 包装字符串值
public string WrapString(string value)

// 生成参数名
public string FormatParameter(string parameterName)
```

---

## 📋 枚举类型

### SqlDialectType
```csharp
public enum SqlDialectType
{
    SqlServer = 0,
    MySql = 1, 
    PostgreSql = 2,
    SQLite = 3,
    Oracle = 4,
    DB2 = 5
}
```

### SqlOperation
```csharp
public enum SqlOperation
{
    Select,
    Insert, 
    Update,
    Delete
}
```

---

## 📋 扩展方法

### ExpressionToSql 扩展
```csharp
// 生成INSERT SQL
public static string ToInsertSql<T>(this ExpressionToSql<T> expression)

// 生成UPDATE SQL
public static string ToUpdateSql<T>(this ExpressionToSql<T> expression)

// 生成DELETE SQL  
public static string ToDeleteSql<T>(this ExpressionToSql<T> expression)

// 生成SELECT SQL
public static string ToSelectSql<T>(this ExpressionToSql<T> expression)

// 创建各种构建器
public static ExpressionToSql<T> CreateInsertBuilder<T>()
public static ExpressionToSql<T> CreateUpdateBuilder<T>()
public static ExpressionToSql<T> CreateDeleteBuilder<T>()
public static ExpressionToSql<T> CreateSelectBuilder<T>()
```

---

## 🎯 最佳实践

### 1. 选择合适的API
- **简单查询**: 使用 `ParameterizedSql.Create`
- **重复使用**: 使用 `SqlTemplate.Parse`
- **动态构建**: 使用 `ExpressionToSql<T>.Create`

### 2. AOT 兼容性
```csharp
// ✅ 推荐：显式指定列
.InsertInto(u => new { u.Name, u.Email })

// ❌ 避免：在AOT场景使用反射
.InsertIntoAll()
```

### 3. 性能优化
```csharp
// ✅ 模板重用
var template = SqlTemplate.Parse(sql);
var result1 = template.Execute(params1);
var result2 = template.Execute(params2);

// ✅ 参数化查询
var query = ExpressionToSql<T>.Create(dialect)
    .UseParameterizedQueries()
    .Where(predicate);
```

这就是Sqlx 3.0的完整API参考。所有API都经过精心设计，确保类型安全、AOT兼容和高性能。
