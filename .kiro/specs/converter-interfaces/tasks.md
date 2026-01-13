# Implementation Tasks

## Task 1: 定义核心接口和类型

- [x] 在 `src/Sqlx` 中创建 `ColumnMeta.cs`，定义 `record ColumnMeta(string Name, string PropertyName, DbType DbType, bool IsNullable)`
- [x] 在 `src/Sqlx` 中创建 `IEntityProvider.cs`，定义 `IEntityProvider` 接口（Type EntityType, IReadOnlyList<ColumnMeta> Columns）
- [x] 在 `src/Sqlx` 中创建 `IParameterBinder.cs`，定义 `IParameterBinder<TEntity>` 接口（BindEntity方法）
- [x] 在 `src/Sqlx` 中创建 `IResultReader.cs`，定义 `IResultReader<TEntity>` 接口（Read方法返回IEnumerable）
- [x] ~~在 `src/Sqlx` 中创建 `ParameterMeta.cs`~~ (已删除 - 设计中定义但实现中未使用)

## Task 2: 实现PlaceholderProcessor基础设施

- [x] 在 `src/Sqlx` 中创建 `PlaceholderType.cs`，定义 `enum PlaceholderType { Static, Dynamic }`
- [x] 在 `src/Sqlx` 中创建 `IPlaceholderHandler.cs`，定义接口（Name, GetType(options), Process方法）
- [x] 在 `src/Sqlx` 中创建 `PlaceholderHandlerBase.cs`，实现基类（ParseOption, HasOption, GetDynamicParameterValue, ParseParamOption, ParseCountOption, ParseExcludeOption, GetFilteredColumns，使用GeneratedRegex）
- [x] 在 `src/Sqlx` 中创建 `PlaceholderContext.cs`，定义上下文类（SqlDialect, TableName, Columns, DynamicParameters等）

## Task 3: 实现内置占位符处理器

- [x] 在 `src/Sqlx/Placeholders` 中创建 `ColumnsPlaceholderHandler.cs`（Static，支持--exclude）
- [x] 在 `src/Sqlx/Placeholders` 中创建 `ValuesPlaceholderHandler.cs`（Static，支持--exclude）
- [x] 在 `src/Sqlx/Placeholders` 中创建 `SetPlaceholderHandler.cs`（Static，支持--exclude）
- [x] 在 `src/Sqlx/Placeholders` 中创建 `TablePlaceholderHandler.cs`（Static）
- [x] 在 `src/Sqlx/Placeholders` 中创建 `WherePlaceholderHandler.cs`（Dynamic，--param）
- [x] 在 `src/Sqlx/Placeholders` 中创建 `LimitPlaceholderHandler.cs`（--count静态，--param动态）
- [x] 在 `src/Sqlx/Placeholders` 中创建 `OffsetPlaceholderHandler.cs`（--count静态，--param动态）

## Task 4: 实现PlaceholderProcessor

- [x] 在 `src/Sqlx` 中创建 `PlaceholderProcessor.cs`，实现Prepare和Render方法
- [x] 实现ContainsDynamicPlaceholders静态方法（使用GeneratedRegex）
- [x] 实现ExtractParameters方法（从SQL提取@param参数）
- [x] 注册所有内置处理器
- [x] 实现RegisterHandler扩展方法

## Task 5: 增强SqlTemplate

- [x] 修改 `src/Sqlx/SqlTemplate.cs`，添加Template, PreparedSql, Sql, HasDynamicPlaceholders, StaticParameters属性
- [x] 实现Prepare方法（调用PlaceholderProcessor.Prepare）
- [x] 实现Render方法（调用PlaceholderProcessor.Render）
- [x] 删除ParameterizedSql依赖，简化API
- [x] 删除SqlTemplateExtensions.cs（功能已整合到SqlTemplate）

## Task 6: 定义源生成器特性

- [x] 在 `src/Sqlx/Annotations` 中创建 `SqlxEntityAttribute.cs`（生成EntityProvider和ResultReader）
- [x] 在 `src/Sqlx/Annotations` 中创建 `SqlxParameterAttribute.cs`（生成ParameterBinder）
- [x] 确保特性支持[IgnoreDataMember]排除属性（源生成器实现）
- [x] 确保特性支持[Column]自定义列名（源生成器实现）
- [x] 删除无用的Annotations（BatchOperation, SoftDelete, DynamicSql, AuditFields, ConcurrencyCheck, SqlxPage, IncludeDeleted, Sqlx）

## Task 7: 实现EntityProvider源生成器

- [x] 在 `src/Sqlx.Generator` 中创建 `EntityProviderGenerator.cs`
- [x] 生成静态Default实例
- [x] 生成缓存的_entityType静态字段
- [x] 生成缓存的_columns静态字段
- [x] 支持[Column]特性读取列名
- [x] 支持[IgnoreDataMember]排除属性

## Task 8: 实现ParameterBinder源生成器

- [x] 在 `src/Sqlx.Generator` 中创建 `ParameterBinderGenerator.cs`
- [x] 生成静态Default实例
- [x] 生成BindEntity方法（直接属性访问，无反射）
- [x] 支持[Column]特性读取参数名
- [x] 支持[IgnoreDataMember]排除属性

## Task 9: 实现ResultReader源生成器

- [x] 在 `EntityProviderGenerator.cs` 中同时生成ResultReader
- [x] 生成静态Default实例
- [x] 生成Read方法（返回IEnumerable<T>，yield return）
- [x] 内联实体构建逻辑
- [x] 支持[Column]特性读取列名

## Task 10: 更新Repository源生成器

- [x] 创建 `RepositoryGenerator.cs` 源生成器
- [x] 生成静态PlaceholderContext字段
- [x] 生成静态SqlTemplate字段（调用Prepare）
- [x] 生成方法实现（使用Render、参数绑定、ResultReader）
- [x] 支持Activity跟踪（#if !SQLX_DISABLE_ACTIVITY）
- [x] 支持拦截器（#if !SQLX_DISABLE_INTERCEPTOR）
- [x] 支持参数记录（#if !SQLX_DISABLE_ACTIVITY_PARAMS）

## Task 11: 实现Activity跟踪

- [x] 在生成的代码中添加Activity.Current?.AddEvent获取
- [x] 设置固定标签：db.system, db.operation(sqlx.execute), db.has_transaction
- [x] 设置参数标签：遍历DynamicParameters设置db.param.{name}
- [x] 成功时设置db.rows_affected（集合结果）
- [x] 失败时调用SetStatus(ActivityStatusCode.Error, ex.Message)
- [x] finally中设置db.duration_ms, db.statement.template, db.statement.prepared, db.statement

## Task 12: 实现分析器

- [x] 在 `src/Sqlx.Generator` 中创建 `SqlxAnalyzer.cs`
- [x] 实现SQLX001：缺少[SqlxEntity]或[SqlxParameter]特性警告
- [x] 实现SQLX002：未知占位符错误
- [x] 实现SQLX003：建议添加[Column]特性提示

## Task 13: 清理和重构

- [x] 删除 `ParameterizedSql.cs`（功能已整合到SqlTemplate）
- [x] 删除 `SqlTemplateExtensions.cs`
- [x] 删除 `IBatchRepository.cs`（使用了已删除的BatchOperationAttribute）
- [x] 更新 `ICrudRepository.cs`（移除IRepository接口）
- [x] ~~更新 `SqlValidator.cs`~~ (已删除 - 未被使用)
- [x] 更新 `ExpressionToSql.cs` 和 `ExpressionToSqlBase.cs`（ToTemplate返回string）
- [x] 删除旧的测试目录 `tests/Sqlx.Tests`
- [x] 删除 `ParameterMeta.cs`（设计中定义但实现中未使用）
- [x] 删除 `PagedResult.cs`（未被使用）
- [x] 删除 `SqlValidator.cs`（未被使用）

## Task 14: 编写单元测试

- [x] 测试ColumnMeta record
- [x] 测试SqlDialect方言配置
- [x] 测试PlaceholderProcessor.Prepare方法
- [x] 测试PlaceholderProcessor.Render方法
- [x] 测试SqlTemplate类
- [x] 测试各个占位符处理器（ColumnsPlaceholderHandler, ValuesPlaceholderHandler, SetPlaceholderHandler, TablePlaceholderHandler, WherePlaceholderHandler, LimitPlaceholderHandler, OffsetPlaceholderHandler）
  - _Requirements: 4.2, 4.3, 4.4, 4.5_

## Task 15: 编写集成测试

- [x] 测试完整的Repository生成和执行流程
  - 创建测试实体类和Repository接口
  - 验证生成的代码能正确编译和执行
  - _Requirements: 5.1, 5.2, 5.3, 6.1, 6.2, 6.3_
- [x] 测试多数据库方言支持
  - 验证SQLite, MySQL, PostgreSQL, SqlServer方言生成正确SQL
  - _Requirements: 4.5_
- [x] 测试Activity跟踪功能
  - 验证Activity事件和标签正确设置
  - _Requirements: 9.4_
- [x] 测试拦截器功能
  - 验证OnExecuting, OnExecuted, OnExecuteFail正确调用
  - _Requirements: 9.1, 9.2, 9.3, 9.5_

## Task 16: 更新文档和示例

- [x] 更新README.md添加新特性使用说明
  - 说明[SqlxEntity]和[SqlxParameter]特性用法
  - 说明占位符系统用法
  - _Requirements: 8.1, 8.2, 8.3, 8.4_
- [x] 更新samples/TodoWebApi示例

## Task 17: 增强IResultReader接口（设计文档要求）

- [x] 在IResultReader接口中添加异步方法 `IAsyncEnumerable<TEntity> ReadAsync(DbDataReader reader, CancellationToken cancellationToken = default)`
  - _Requirements: 3.2, 3.3_
- [x] 更新EntityProviderGenerator生成异步ReadAsync方法
  - 使用`[EnumeratorCancellation]`特性
  - 使用`await reader.ReadAsync(cancellationToken)`
  - _Requirements: 3.2, 3.3, 5.1_

## Task 18: 验证AOT兼容性

- [x] 创建AOT兼容性测试
  - 验证生成的代码不使用System.Reflection命名空间
  - 验证生成的代码不使用dynamic关键字
  - 验证生成的代码不使用Type.GetType()或类似运行时类型解析
  - _Requirements: 5.4, 5.5, 5.6_

## Task 19: 重构SqlDialect为可扩展架构

- [x] 将SqlDialect从record struct重构为abstract class
  - 创建基类SqlDialect，包含所有可扩展的虚方法
  - 实现字符串函数：Concat, Upper, Lower, Trim, LTrim, RTrim, Length, Substring, Replace, Coalesce
  - 实现日期/时间函数：CurrentTimestamp, CurrentDate, CurrentTime, DatePart, DateAdd, DateDiff
  - 实现数值函数：Abs, Round, Ceiling, Floor, Mod
  - 实现聚合函数：Count, Sum, Avg, Min, Max
  - 实现分页：Limit, Offset, Paginate
  - 实现空值处理：IfNull, NullIf
  - 实现类型转换：Cast
  - 实现条件表达式：CaseWhen, Iif
  - 实现LastInsertedId属性
- [x] 创建具体方言实现类
  - SqlServerDialect：使用LEN, ISNULL, IIF, TOP, OFFSET FETCH, GETDATE等
  - MySqlDialect：使用CHAR_LENGTH, IFNULL, IF, CONCAT函数, NOW等
  - PostgreSqlDialect：使用||连接, COALESCE, CASE WHEN, ::类型转换, CURRENT_TIMESTAMP等
  - SQLiteDialect：使用||连接, IFNULL, STRFTIME, JULIANDAY等
  - OracleDialect：使用||连接, NVL, FETCH FIRST, SYSTIMESTAMP, ADD_MONTHS等
  - DB2Dialect：使用CONCAT函数, FETCH FIRST, CURRENT TIMESTAMP, TIMESTAMPDIFF等
- [x] 保持向后兼容
  - SqlDefine静态类保持不变，提供预定义方言实例
  - 添加GetConcatFunction方法的[Obsolete]标记，指向新的Concat方法
- [x] 更新ExpressionToSqlBase使用新的Concat方法
- [x] 更新SqlDialectTests测试新的方言方法
  - 测试所有方言的字符串函数
  - 测试所有方言的日期/时间函数
  - 测试所有方言的分页语法
  - 测试所有方言的空值处理
  - 测试所有方言的条件表达式
  - 测试所有方言的类型转换
