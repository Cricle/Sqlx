# Requirements Document

## Introduction

本规范定义了对Sqlx项目中所有支持的数据库方言进行SQL语义TDD验证的需求。目标是确保生成的SQL语句在各个数据库中语义正确，并在发现错误时进行修复。支持的数据库包括：MySQL、PostgreSQL、SQL Server、SQLite和Oracle。

## Glossary

- **Dialect_Provider**: 数据库方言提供者，负责生成特定数据库的SQL语法
- **SQL_Template_Engine**: SQL模板引擎，处理SQL模板并替换占位符
- **Identifier_Quoting**: 标识符引用，不同数据库使用不同符号包裹表名和列名
- **Parameter_Placeholder**: 参数占位符，不同数据库使用不同的参数语法
- **Upsert**: 插入或更新操作，不同数据库有不同的实现语法
- **Pagination**: 分页查询，不同数据库使用不同的LIMIT/OFFSET语法
- **Boolean_Literal**: 布尔值字面量，不同数据库表示true/false的方式不同
- **DateTime_Function**: 日期时间函数，获取当前时间的数据库特定函数
- **String_Concatenation**: 字符串连接，不同数据库使用不同的连接运算符

## Requirements

### Requirement 1: 标识符引用语法验证

**User Story:** As a developer, I want the SQL generator to use correct identifier quoting for each database, so that table and column names are properly escaped.

#### Acceptance Criteria

1. WHEN generating SQL for MySQL, THE Dialect_Provider SHALL use backticks (`) to wrap identifiers
2. WHEN generating SQL for PostgreSQL, THE Dialect_Provider SHALL use double quotes (") to wrap identifiers
3. WHEN generating SQL for SQL Server, THE Dialect_Provider SHALL use square brackets ([]) to wrap identifiers
4. WHEN generating SQL for SQLite, THE Dialect_Provider SHALL use square brackets ([]) or double quotes (") to wrap identifiers
5. WHEN generating SQL for Oracle, THE Dialect_Provider SHALL use double quotes (") to wrap identifiers

### Requirement 2: 参数占位符语法验证

**User Story:** As a developer, I want the SQL generator to use correct parameter placeholders for each database, so that parameterized queries work correctly.

#### Acceptance Criteria

1. WHEN generating SQL for MySQL, THE Dialect_Provider SHALL use @param or ? as parameter placeholders
2. WHEN generating SQL for PostgreSQL, THE Dialect_Provider SHALL use $1, $2 positional parameters or @param named parameters
3. WHEN generating SQL for SQL Server, THE Dialect_Provider SHALL use @param as parameter placeholders
4. WHEN generating SQL for SQLite, THE Dialect_Provider SHALL use @param, :param, or ? as parameter placeholders
5. WHEN generating SQL for Oracle, THE Dialect_Provider SHALL use :param as parameter placeholders

### Requirement 3: 分页语法验证

**User Story:** As a developer, I want the SQL generator to produce correct pagination syntax for each database, so that LIMIT/OFFSET queries work correctly.

#### Acceptance Criteria

1. WHEN generating pagination for MySQL, THE Dialect_Provider SHALL use LIMIT n OFFSET m syntax
2. WHEN generating pagination for PostgreSQL, THE Dialect_Provider SHALL use LIMIT n OFFSET m syntax
3. WHEN generating pagination for SQL Server, THE Dialect_Provider SHALL use OFFSET m ROWS FETCH NEXT n ROWS ONLY syntax (requires ORDER BY)
4. WHEN generating pagination for SQLite, THE Dialect_Provider SHALL use LIMIT n OFFSET m syntax
5. WHEN generating pagination for Oracle, THE Dialect_Provider SHALL use OFFSET m ROWS FETCH NEXT n ROWS ONLY syntax
6. IF MySQL pagination has OFFSET without LIMIT, THEN THE Dialect_Provider SHALL throw an ArgumentException

### Requirement 4: INSERT返回ID语法验证

**User Story:** As a developer, I want the SQL generator to produce correct INSERT RETURNING syntax for each database, so that auto-generated IDs can be retrieved.

#### Acceptance Criteria

1. WHEN generating INSERT with returning ID for MySQL, THE Dialect_Provider SHALL append SELECT LAST_INSERT_ID() after INSERT
2. WHEN generating INSERT with returning ID for PostgreSQL, THE Dialect_Provider SHALL use RETURNING id clause
3. WHEN generating INSERT with returning ID for SQL Server, THE Dialect_Provider SHALL use OUTPUT INSERTED.Id clause
4. WHEN generating INSERT with returning ID for SQLite, THE Dialect_Provider SHALL append SELECT last_insert_rowid() after INSERT
5. WHEN generating INSERT with returning ID for Oracle, THE Dialect_Provider SHALL use RETURNING id INTO :variable clause

### Requirement 5: Upsert语法验证

**User Story:** As a developer, I want the SQL generator to produce correct UPSERT syntax for each database, so that insert-or-update operations work correctly.

#### Acceptance Criteria

1. WHEN generating UPSERT for MySQL, THE Dialect_Provider SHALL use ON DUPLICATE KEY UPDATE syntax
2. WHEN generating UPSERT for PostgreSQL, THE Dialect_Provider SHALL use ON CONFLICT ... DO UPDATE SET syntax with EXCLUDED keyword
3. WHEN generating UPSERT for SQL Server, THE Dialect_Provider SHALL use MERGE statement syntax
4. WHEN generating UPSERT for SQLite, THE Dialect_Provider SHALL use ON CONFLICT ... DO UPDATE SET syntax with excluded keyword
5. WHEN generating UPSERT for Oracle, THE Dialect_Provider SHALL use MERGE statement syntax with DUAL table

### Requirement 6: 布尔值字面量验证

**User Story:** As a developer, I want the SQL generator to produce correct boolean literals for each database, so that boolean comparisons work correctly.

#### Acceptance Criteria

1. WHEN generating boolean TRUE for MySQL, THE Dialect_Provider SHALL return "1"
2. WHEN generating boolean FALSE for MySQL, THE Dialect_Provider SHALL return "0"
3. WHEN generating boolean TRUE for PostgreSQL, THE Dialect_Provider SHALL return "true"
4. WHEN generating boolean FALSE for PostgreSQL, THE Dialect_Provider SHALL return "false"
5. WHEN generating boolean TRUE for SQL Server, THE Dialect_Provider SHALL return "1"
6. WHEN generating boolean FALSE for SQL Server, THE Dialect_Provider SHALL return "0"
7. WHEN generating boolean TRUE for SQLite, THE Dialect_Provider SHALL return "1"
8. WHEN generating boolean FALSE for SQLite, THE Dialect_Provider SHALL return "0"

### Requirement 7: 当前日期时间函数验证

**User Story:** As a developer, I want the SQL generator to produce correct current datetime functions for each database, so that timestamp operations work correctly.

#### Acceptance Criteria

1. WHEN getting current datetime for MySQL, THE Dialect_Provider SHALL return "NOW()"
2. WHEN getting current datetime for PostgreSQL, THE Dialect_Provider SHALL return "CURRENT_TIMESTAMP"
3. WHEN getting current datetime for SQL Server, THE Dialect_Provider SHALL return "GETDATE()"
4. WHEN getting current datetime for SQLite, THE Dialect_Provider SHALL return "datetime('now')"
5. WHEN getting current datetime for Oracle, THE Dialect_Provider SHALL return "SYSDATE"

### Requirement 8: 字符串连接语法验证

**User Story:** As a developer, I want the SQL generator to produce correct string concatenation syntax for each database, so that string operations work correctly.

#### Acceptance Criteria

1. WHEN concatenating strings for MySQL, THE Dialect_Provider SHALL use CONCAT(a, b, c) function
2. WHEN concatenating strings for PostgreSQL, THE Dialect_Provider SHALL use a || b || c operator
3. WHEN concatenating strings for SQL Server, THE Dialect_Provider SHALL use a + b + c operator
4. WHEN concatenating strings for SQLite, THE Dialect_Provider SHALL use a || b || c operator
5. WHEN concatenating strings for Oracle, THE Dialect_Provider SHALL use a || b || c operator
6. IF concatenating single expression, THEN THE Dialect_Provider SHALL return the expression unchanged
7. IF concatenating empty array, THEN THE Dialect_Provider SHALL return empty string

### Requirement 9: 数据类型映射验证

**User Story:** As a developer, I want the SQL generator to map .NET types to correct database types for each database, so that schema generation works correctly.

#### Acceptance Criteria

1. WHEN mapping Int32 type, THE Dialect_Provider SHALL return appropriate integer type for each database
2. WHEN mapping Int64 type, THE Dialect_Provider SHALL return appropriate bigint type for each database
3. WHEN mapping String type, THE Dialect_Provider SHALL return appropriate varchar/text type for each database
4. WHEN mapping DateTime type, THE Dialect_Provider SHALL return appropriate datetime/timestamp type for each database
5. WHEN mapping Boolean type, THE Dialect_Provider SHALL return appropriate boolean/bit/integer type for each database
6. WHEN mapping Guid type, THE Dialect_Provider SHALL return appropriate uuid/uniqueidentifier/char type for each database
7. WHEN mapping Decimal type, THE Dialect_Provider SHALL return appropriate decimal/numeric type for each database

### Requirement 10: 批量INSERT语法验证

**User Story:** As a developer, I want the SQL generator to produce correct batch INSERT syntax for each database, so that bulk operations work efficiently.

#### Acceptance Criteria

1. WHEN generating batch INSERT for MySQL, THE Dialect_Provider SHALL use multi-row VALUES syntax
2. WHEN generating batch INSERT for PostgreSQL, THE Dialect_Provider SHALL use multi-row VALUES syntax with positional parameters
3. WHEN generating batch INSERT for SQL Server, THE Dialect_Provider SHALL use multi-row VALUES syntax
4. WHEN generating batch INSERT for SQLite, THE Dialect_Provider SHALL use multi-row VALUES syntax
5. FOR ALL batch INSERT operations, THE Dialect_Provider SHALL generate correct parameter placeholders for each row

### Requirement 11: 日期时间格式化验证

**User Story:** As a developer, I want the SQL generator to format datetime values correctly for each database, so that date literals are valid.

#### Acceptance Criteria

1. WHEN formatting datetime for MySQL, THE Dialect_Provider SHALL use 'yyyy-MM-dd HH:mm:ss' format
2. WHEN formatting datetime for PostgreSQL, THE Dialect_Provider SHALL use 'yyyy-MM-dd HH:mm:ss.fff'::timestamp format
3. WHEN formatting datetime for SQL Server, THE Dialect_Provider SHALL use 'yyyy-MM-dd HH:mm:ss.fff' format
4. WHEN formatting datetime for SQLite, THE Dialect_Provider SHALL use 'yyyy-MM-dd HH:mm:ss.fff' format
5. WHEN formatting datetime for Oracle, THE Dialect_Provider SHALL use TO_DATE or TO_TIMESTAMP function

### Requirement 12: LIMIT/OFFSET子句生成验证

**User Story:** As a developer, I want the SQL generator to produce correct parameterized LIMIT/OFFSET clauses for each database, so that pagination with parameters works correctly.

#### Acceptance Criteria

1. WHEN generating LIMIT/OFFSET clause for MySQL, THE Dialect_Provider SHALL return "LIMIT @limit OFFSET @offset" and set requiresOrderBy to false
2. WHEN generating LIMIT/OFFSET clause for PostgreSQL, THE Dialect_Provider SHALL return "LIMIT @limit OFFSET @offset" and set requiresOrderBy to false
3. WHEN generating LIMIT/OFFSET clause for SQL Server, THE Dialect_Provider SHALL return "OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY" and set requiresOrderBy to true
4. WHEN generating LIMIT/OFFSET clause for SQLite, THE Dialect_Provider SHALL return "LIMIT @limit OFFSET @offset" and set requiresOrderBy to false
5. WHEN generating LIMIT/OFFSET clause for Oracle, THE Dialect_Provider SHALL return "OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY" and set requiresOrderBy to true

### Requirement 13: SELECT查询语法验证

**User Story:** As a developer, I want the SQL generator to produce correct SELECT statements for each database, so that query operations work correctly.

#### Acceptance Criteria

1. WHEN generating SELECT with {{columns}} placeholder, THE SQL_Template_Engine SHALL expand to all entity column names in snake_case
2. WHEN generating SELECT with {{columns --exclude col1 col2}} placeholder, THE SQL_Template_Engine SHALL exclude specified columns
3. WHEN generating SELECT with {{columns --only col1 col2}} placeholder, THE SQL_Template_Engine SHALL include only specified columns
4. WHEN generating SELECT with {{table}} placeholder, THE SQL_Template_Engine SHALL replace with table name from TableName attribute in snake_case
5. WHEN generating SELECT with WHERE clause, THE SQL_Template_Engine SHALL preserve parameterized conditions
6. FOR ALL SELECT statements, THE SQL_Template_Engine SHALL use proper identifier quoting for the target database

### Requirement 14: INSERT语句语法验证

**User Story:** As a developer, I want the SQL generator to produce correct INSERT statements for each database, so that data insertion works correctly.

#### Acceptance Criteria

1. WHEN generating INSERT with {{columns}} and {{values}} placeholders, THE SQL_Template_Engine SHALL generate matching column and parameter lists
2. WHEN generating INSERT with {{columns --exclude Id}}, THE SQL_Template_Engine SHALL exclude the Id column for auto-increment scenarios
3. WHEN generating INSERT with {{values}} placeholder, THE SQL_Template_Engine SHALL generate @ParameterName format parameters
4. FOR ALL INSERT statements, THE SQL_Template_Engine SHALL use proper identifier quoting for the target database
5. WHEN generating INSERT for batch operations, THE SQL_Template_Engine SHALL generate multi-row VALUES syntax

### Requirement 15: UPDATE语句语法验证

**User Story:** As a developer, I want the SQL generator to produce correct UPDATE statements for each database, so that data modification works correctly.

#### Acceptance Criteria

1. WHEN generating UPDATE with {{set}} placeholder, THE SQL_Template_Engine SHALL generate column=@Parameter pairs
2. WHEN generating UPDATE with {{set --exclude Id CreatedAt}}, THE SQL_Template_Engine SHALL exclude specified columns from SET clause
3. WHEN generating UPDATE with {{set --only col1 col2}}, THE SQL_Template_Engine SHALL include only specified columns in SET clause
4. FOR ALL UPDATE statements, THE SQL_Template_Engine SHALL use proper identifier quoting for the target database
5. WHEN generating UPDATE with WHERE clause, THE SQL_Template_Engine SHALL preserve parameterized conditions

### Requirement 16: DELETE语句语法验证

**User Story:** As a developer, I want the SQL generator to produce correct DELETE statements for each database, so that data deletion works correctly.

#### Acceptance Criteria

1. WHEN generating DELETE with {{table}} placeholder, THE SQL_Template_Engine SHALL replace with correct table name
2. WHEN generating DELETE with WHERE clause, THE SQL_Template_Engine SHALL preserve parameterized conditions
3. FOR ALL DELETE statements, THE SQL_Template_Engine SHALL use proper identifier quoting for the target database
4. WHEN generating soft delete, THE SQL_Template_Engine SHALL generate UPDATE statement setting deleted flag

### Requirement 17: {{table}}占位符验证

**User Story:** As a developer, I want the {{table}} placeholder to correctly resolve table names for each database, so that table references work correctly.

#### Acceptance Criteria

1. WHEN processing {{table}} placeholder, THE SQL_Template_Engine SHALL get table name from TableName attribute
2. WHEN processing {{table}} placeholder, THE SQL_Template_Engine SHALL convert PascalCase to snake_case
3. WHEN processing {{table}} placeholder for MySQL, THE SQL_Template_Engine SHALL wrap with backticks if needed
4. WHEN processing {{table}} placeholder for PostgreSQL, THE SQL_Template_Engine SHALL wrap with double quotes if needed
5. WHEN processing {{table}} placeholder for SQL Server, THE SQL_Template_Engine SHALL wrap with square brackets if needed
6. WHEN processing {{table}} placeholder for SQLite, THE SQL_Template_Engine SHALL wrap with square brackets or double quotes if needed

### Requirement 18: {{columns}}占位符验证

**User Story:** As a developer, I want the {{columns}} placeholder to correctly generate column lists for each database, so that column references work correctly.

#### Acceptance Criteria

1. WHEN processing {{columns}} placeholder, THE SQL_Template_Engine SHALL generate comma-separated column names from entity properties
2. WHEN processing {{columns}} placeholder, THE SQL_Template_Engine SHALL convert PascalCase property names to snake_case column names
3. WHEN processing {{columns --exclude col1 col2}}, THE SQL_Template_Engine SHALL exclude specified columns from the list
4. WHEN processing {{columns --only col1 col2}}, THE SQL_Template_Engine SHALL include only specified columns in the list
5. FOR ALL column names, THE SQL_Template_Engine SHALL use proper identifier quoting for the target database

### Requirement 19: {{values}}占位符验证

**User Story:** As a developer, I want the {{values}} placeholder to correctly generate parameter placeholders for each database, so that parameterized inserts work correctly.

#### Acceptance Criteria

1. WHEN processing {{values}} placeholder, THE SQL_Template_Engine SHALL generate parameter placeholders matching {{columns}}
2. WHEN processing {{values}} placeholder for MySQL, THE SQL_Template_Engine SHALL use @ParameterName format
3. WHEN processing {{values}} placeholder for PostgreSQL, THE SQL_Template_Engine SHALL use $1, $2 positional format or @ParameterName
4. WHEN processing {{values}} placeholder for SQL Server, THE SQL_Template_Engine SHALL use @ParameterName format
5. WHEN processing {{values}} placeholder for SQLite, THE SQL_Template_Engine SHALL use @ParameterName format
6. WHEN {{columns}} has --exclude or --only options, THE {{values}} placeholder SHALL match the same columns

### Requirement 20: {{set}}占位符验证

**User Story:** As a developer, I want the {{set}} placeholder to correctly generate SET clauses for each database, so that UPDATE statements work correctly.

#### Acceptance Criteria

1. WHEN processing {{set}} placeholder, THE SQL_Template_Engine SHALL generate column=@Parameter pairs
2. WHEN processing {{set --exclude col1 col2}}, THE SQL_Template_Engine SHALL exclude specified columns
3. WHEN processing {{set --only col1 col2}}, THE SQL_Template_Engine SHALL include only specified columns
4. FOR ALL SET clauses, THE SQL_Template_Engine SHALL use proper identifier quoting for the target database
5. FOR ALL SET clauses, THE SQL_Template_Engine SHALL use comma separator between assignments

### Requirement 21: {{orderby}}占位符验证

**User Story:** As a developer, I want the {{orderby}} placeholder to correctly generate ORDER BY clauses for each database, so that sorting works correctly.

#### Acceptance Criteria

1. WHEN processing {{orderby column_name}} placeholder, THE SQL_Template_Engine SHALL generate ORDER BY column_name
2. WHEN processing {{orderby column_name --desc}} placeholder, THE SQL_Template_Engine SHALL generate ORDER BY column_name DESC
3. WHEN processing {{orderby column_name --asc}} placeholder, THE SQL_Template_Engine SHALL generate ORDER BY column_name ASC
4. WHEN processing multiple {{orderby}} placeholders, THE SQL_Template_Engine SHALL combine them with comma separator
5. FOR ALL ORDER BY clauses, THE SQL_Template_Engine SHALL use proper identifier quoting for the target database

### Requirement 22: {{limit}}和{{offset}}占位符验证

**User Story:** As a developer, I want the {{limit}} and {{offset}} placeholders to correctly generate pagination clauses for each database, so that result limiting works correctly.

#### Acceptance Criteria

1. WHEN processing {{limit}} placeholder for MySQL/PostgreSQL/SQLite, THE SQL_Template_Engine SHALL generate LIMIT @limit
2. WHEN processing {{limit}} placeholder for SQL Server, THE SQL_Template_Engine SHALL generate TOP (@limit) or use FETCH NEXT
3. WHEN processing {{offset}} placeholder for MySQL/PostgreSQL/SQLite, THE SQL_Template_Engine SHALL generate OFFSET @offset
4. WHEN processing {{offset}} placeholder for SQL Server, THE SQL_Template_Engine SHALL generate OFFSET @offset ROWS
5. WHEN processing {{limit_offset}} placeholder, THE SQL_Template_Engine SHALL generate combined pagination clause appropriate for each database

### Requirement 23: {{bool_true}}和{{bool_false}}占位符验证

**User Story:** As a developer, I want the {{bool_true}} and {{bool_false}} placeholders to correctly generate boolean literals for each database, so that boolean comparisons work correctly.

#### Acceptance Criteria

1. WHEN processing {{bool_true}} for MySQL, THE SQL_Template_Engine SHALL generate "1" or "TRUE"
2. WHEN processing {{bool_false}} for MySQL, THE SQL_Template_Engine SHALL generate "0" or "FALSE"
3. WHEN processing {{bool_true}} for PostgreSQL, THE SQL_Template_Engine SHALL generate "true"
4. WHEN processing {{bool_false}} for PostgreSQL, THE SQL_Template_Engine SHALL generate "false"
5. WHEN processing {{bool_true}} for SQL Server, THE SQL_Template_Engine SHALL generate "1"
6. WHEN processing {{bool_false}} for SQL Server, THE SQL_Template_Engine SHALL generate "0"
7. WHEN processing {{bool_true}} for SQLite, THE SQL_Template_Engine SHALL generate "1"
8. WHEN processing {{bool_false}} for SQLite, THE SQL_Template_Engine SHALL generate "0"

### Requirement 24: {{current_timestamp}}占位符验证

**User Story:** As a developer, I want the {{current_timestamp}} placeholder to correctly generate current timestamp functions for each database, so that timestamp operations work correctly.

#### Acceptance Criteria

1. WHEN processing {{current_timestamp}} for MySQL, THE SQL_Template_Engine SHALL generate "NOW()" or "CURRENT_TIMESTAMP"
2. WHEN processing {{current_timestamp}} for PostgreSQL, THE SQL_Template_Engine SHALL generate "CURRENT_TIMESTAMP"
3. WHEN processing {{current_timestamp}} for SQL Server, THE SQL_Template_Engine SHALL generate "GETDATE()"
4. WHEN processing {{current_timestamp}} for SQLite, THE SQL_Template_Engine SHALL generate "datetime('now')" or "CURRENT_TIMESTAMP"
5. WHEN processing {{current_timestamp}} for Oracle, THE SQL_Template_Engine SHALL generate "SYSDATE" or "SYSTIMESTAMP"

### Requirement 25: {{returning_id}}占位符验证

**User Story:** As a developer, I want the {{returning_id}} placeholder to correctly generate ID returning clauses for each database, so that auto-generated IDs can be retrieved.

#### Acceptance Criteria

1. WHEN processing {{returning_id}} for MySQL, THE SQL_Template_Engine SHALL generate empty string (uses LAST_INSERT_ID separately)
2. WHEN processing {{returning_id}} for PostgreSQL, THE SQL_Template_Engine SHALL generate "RETURNING id"
3. WHEN processing {{returning_id}} for SQL Server, THE SQL_Template_Engine SHALL generate empty string (uses OUTPUT INSERTED or SCOPE_IDENTITY)
4. WHEN processing {{returning_id}} for SQLite, THE SQL_Template_Engine SHALL generate empty string (uses last_insert_rowid separately)
5. WHEN processing {{returning_id}} for Oracle, THE SQL_Template_Engine SHALL generate "RETURNING id INTO :variable"

### Requirement 26: {{concat}}占位符验证

**User Story:** As a developer, I want the {{concat}} placeholder to correctly generate string concatenation syntax for each database, so that string operations work correctly.

#### Acceptance Criteria

1. WHEN processing {{concat}} for MySQL, THE SQL_Template_Engine SHALL generate CONCAT(a, b, c) function
2. WHEN processing {{concat}} for PostgreSQL, THE SQL_Template_Engine SHALL generate a || b || c operator
3. WHEN processing {{concat}} for SQL Server, THE SQL_Template_Engine SHALL generate a + b + c operator
4. WHEN processing {{concat}} for SQLite, THE SQL_Template_Engine SHALL generate a || b || c operator
5. WHEN processing {{concat}} for Oracle, THE SQL_Template_Engine SHALL generate a || b || c operator

### Requirement 27: 动态占位符{{@param}}验证

**User Story:** As a developer, I want dynamic placeholders to be validated and processed correctly for each database, so that dynamic SQL is safe and correct.

#### Acceptance Criteria

1. WHEN processing {{@tableName}} dynamic placeholder, THE SQL_Template_Engine SHALL validate identifier format (letters, numbers, underscore only)
2. WHEN processing {{@whereClause}} dynamic placeholder with DynamicSqlType.Fragment, THE SQL_Template_Engine SHALL validate against dangerous SQL keywords
3. WHEN processing {{@suffix}} dynamic placeholder with DynamicSqlType.TablePart, THE SQL_Template_Engine SHALL validate alphanumeric only
4. IF dynamic placeholder validation fails, THEN THE SQL_Template_Engine SHALL throw ArgumentException with descriptive message
5. FOR ALL dynamic placeholders, THE SQL_Template_Engine SHALL require [DynamicSql] attribute on the parameter

### Requirement 28: JOIN语法验证

**User Story:** As a developer, I want the SQL generator to produce correct JOIN syntax for each database, so that table joins work correctly.

#### Acceptance Criteria

1. WHEN generating INNER JOIN, THE SQL_Template_Engine SHALL produce valid INNER JOIN syntax for all databases
2. WHEN generating LEFT JOIN, THE SQL_Template_Engine SHALL produce valid LEFT JOIN syntax for all databases
3. WHEN generating RIGHT JOIN for SQLite, THE SQL_Template_Engine SHALL either convert to LEFT JOIN or report unsupported
4. WHEN generating FULL OUTER JOIN, THE SQL_Template_Engine SHALL produce valid syntax or report unsupported for databases that don't support it
5. FOR ALL JOIN operations, THE SQL_Template_Engine SHALL use proper identifier quoting for the target database

### Requirement 29: 聚合函数语法验证

**User Story:** As a developer, I want the SQL generator to produce correct aggregate function syntax for each database, so that aggregate operations work correctly.

#### Acceptance Criteria

1. WHEN generating COUNT(*), THE SQL_Template_Engine SHALL produce valid COUNT syntax for all databases
2. WHEN generating SUM(column), THE SQL_Template_Engine SHALL produce valid SUM syntax for all databases
3. WHEN generating AVG(column), THE SQL_Template_Engine SHALL produce valid AVG syntax for all databases
4. WHEN generating MAX(column), THE SQL_Template_Engine SHALL produce valid MAX syntax for all databases
5. WHEN generating MIN(column), THE SQL_Template_Engine SHALL produce valid MIN syntax for all databases
6. FOR ALL aggregate functions, THE SQL_Template_Engine SHALL use proper identifier quoting for column names

### Requirement 30: GROUP BY和HAVING语法验证

**User Story:** As a developer, I want the SQL generator to produce correct GROUP BY and HAVING syntax for each database, so that grouping operations work correctly.

#### Acceptance Criteria

1. WHEN generating GROUP BY clause, THE SQL_Template_Engine SHALL produce valid GROUP BY syntax for all databases
2. WHEN generating HAVING clause, THE SQL_Template_Engine SHALL produce valid HAVING syntax for all databases
3. WHEN generating GROUP BY with multiple columns, THE SQL_Template_Engine SHALL use comma separator
4. FOR ALL GROUP BY and HAVING clauses, THE SQL_Template_Engine SHALL use proper identifier quoting for column names


### Requirement 31: 混合占位符场景验证

**User Story:** As a developer, I want the SQL generator to correctly handle multiple placeholders in a single SQL template, so that complex queries work correctly.

#### Acceptance Criteria

1. WHEN processing template with {{table}}, {{columns}}, and {{orderby}} together, THE SQL_Template_Engine SHALL correctly expand all placeholders
2. WHEN processing template with {{columns --exclude Id}} and {{values}} together, THE SQL_Template_Engine SHALL ensure values match excluded columns
3. WHEN processing template with {{set --exclude Id CreatedAt}} and WHERE clause, THE SQL_Template_Engine SHALL correctly generate UPDATE statement
4. WHEN processing template with multiple {{orderby}} placeholders, THE SQL_Template_Engine SHALL combine them correctly with comma separator
5. WHEN processing template with {{limit_offset}} and {{orderby}}, THE SQL_Template_Engine SHALL ensure ORDER BY comes before pagination for SQL Server

### Requirement 32: 边界条件验证 - 空值和NULL处理

**User Story:** As a developer, I want the SQL generator to correctly handle NULL values and empty inputs, so that edge cases work correctly.

#### Acceptance Criteria

1. WHEN generating SQL with NULL parameter value, THE SQL_Template_Engine SHALL use IS NULL or proper NULL handling
2. WHEN generating pagination with limit=0, THE SQL_Template_Engine SHALL generate valid SQL that returns no rows
3. WHEN generating pagination with offset=0, THE SQL_Template_Engine SHALL generate valid SQL starting from first row
4. WHEN generating {{columns}} for entity with no properties, THE SQL_Template_Engine SHALL handle gracefully or report error
5. WHEN generating {{set}} with all columns excluded, THE SQL_Template_Engine SHALL report error or handle gracefully
6. IF table name is empty or null, THEN THE SQL_Template_Engine SHALL throw ArgumentException

### Requirement 33: 边界条件验证 - 大数值和极限值

**User Story:** As a developer, I want the SQL generator to correctly handle large numbers and extreme values, so that boundary cases work correctly.

#### Acceptance Criteria

1. WHEN generating pagination with very large limit (e.g., Int32.MaxValue), THE SQL_Template_Engine SHALL generate valid SQL
2. WHEN generating pagination with very large offset, THE SQL_Template_Engine SHALL generate valid SQL
3. WHEN generating batch INSERT with maximum batch size, THE SQL_Template_Engine SHALL generate valid SQL
4. WHEN generating SQL with very long column names (128+ characters), THE SQL_Template_Engine SHALL handle according to database limits
5. WHEN generating SQL with very long table names, THE SQL_Template_Engine SHALL handle according to database limits

### Requirement 34: 边界条件验证 - 特殊字符处理

**User Story:** As a developer, I want the SQL generator to correctly handle special characters in identifiers and values, so that special cases work correctly.

#### Acceptance Criteria

1. WHEN table name contains reserved SQL keywords, THE SQL_Template_Engine SHALL properly quote the identifier
2. WHEN column name contains spaces, THE SQL_Template_Engine SHALL properly quote the identifier
3. WHEN column name contains special characters (e.g., #, $, @), THE SQL_Template_Engine SHALL properly escape or quote
4. WHEN table name starts with number, THE SQL_Template_Engine SHALL properly quote the identifier
5. WHEN identifier contains Unicode characters, THE SQL_Template_Engine SHALL handle according to database support

### Requirement 35: 异常处理验证 - 无效输入

**User Story:** As a developer, I want the SQL generator to properly report errors for invalid inputs, so that debugging is easier.

#### Acceptance Criteria

1. IF SQL template contains unknown placeholder, THEN THE SQL_Template_Engine SHALL throw descriptive exception
2. IF SQL template contains malformed placeholder syntax, THEN THE SQL_Template_Engine SHALL throw descriptive exception
3. IF {{columns --exclude}} references non-existent column, THEN THE SQL_Template_Engine SHALL throw descriptive exception
4. IF {{columns --only}} references non-existent column, THEN THE SQL_Template_Engine SHALL throw descriptive exception
5. IF dynamic placeholder parameter is not marked with [DynamicSql], THEN THE SQL_Template_Engine SHALL throw compile-time error

### Requirement 36: 异常处理验证 - SQL注入防护

**User Story:** As a developer, I want the SQL generator to prevent SQL injection attacks, so that the application is secure.

#### Acceptance Criteria

1. WHEN dynamic placeholder contains DROP keyword, THE SQL_Template_Engine SHALL reject the input
2. WHEN dynamic placeholder contains DELETE keyword without proper context, THE SQL_Template_Engine SHALL reject the input
3. WHEN dynamic placeholder contains comment markers (-- or /*), THE SQL_Template_Engine SHALL reject the input
4. WHEN dynamic placeholder contains EXEC or EXECUTE keyword, THE SQL_Template_Engine SHALL reject the input
5. WHEN dynamic placeholder contains semicolon for statement termination, THE SQL_Template_Engine SHALL reject the input
6. FOR ALL parameterized queries, THE SQL_Template_Engine SHALL use proper parameter binding instead of string concatenation

### Requirement 37: 数据类型验证 - 数值类型

**User Story:** As a developer, I want the SQL generator to correctly handle all numeric types, so that numeric operations work correctly.

#### Acceptance Criteria

1. WHEN mapping Int16 (short) type, THE Dialect_Provider SHALL return appropriate smallint type
2. WHEN mapping Int32 (int) type, THE Dialect_Provider SHALL return appropriate integer type
3. WHEN mapping Int64 (long) type, THE Dialect_Provider SHALL return appropriate bigint type
4. WHEN mapping Single (float) type, THE Dialect_Provider SHALL return appropriate real/float type
5. WHEN mapping Double type, THE Dialect_Provider SHALL return appropriate double precision/float type
6. WHEN mapping Decimal type, THE Dialect_Provider SHALL return appropriate decimal/numeric type with precision

### Requirement 38: 数据类型验证 - 字符串类型

**User Story:** As a developer, I want the SQL generator to correctly handle all string types, so that text operations work correctly.

#### Acceptance Criteria

1. WHEN mapping String type, THE Dialect_Provider SHALL return appropriate varchar/nvarchar type
2. WHEN mapping Char type, THE Dialect_Provider SHALL return appropriate char type
3. WHEN mapping String with MaxLength attribute, THE Dialect_Provider SHALL respect the length constraint
4. WHEN mapping String for SQL Server, THE Dialect_Provider SHALL use NVARCHAR for Unicode support
5. WHEN mapping String for MySQL, THE Dialect_Provider SHALL use VARCHAR with appropriate charset

### Requirement 39: 数据类型验证 - 日期时间类型

**User Story:** As a developer, I want the SQL generator to correctly handle all date/time types, so that temporal operations work correctly.

#### Acceptance Criteria

1. WHEN mapping DateTime type, THE Dialect_Provider SHALL return appropriate datetime/timestamp type
2. WHEN mapping DateTimeOffset type, THE Dialect_Provider SHALL return appropriate datetimeoffset/timestamptz type
3. WHEN mapping TimeSpan type, THE Dialect_Provider SHALL return appropriate time/interval type
4. WHEN mapping DateOnly type (.NET 6+), THE Dialect_Provider SHALL return appropriate date type
5. WHEN mapping TimeOnly type (.NET 6+), THE Dialect_Provider SHALL return appropriate time type

### Requirement 40: 数据类型验证 - 特殊类型

**User Story:** As a developer, I want the SQL generator to correctly handle special types, so that all data types work correctly.

#### Acceptance Criteria

1. WHEN mapping Guid type, THE Dialect_Provider SHALL return appropriate uuid/uniqueidentifier type
2. WHEN mapping byte[] type, THE Dialect_Provider SHALL return appropriate binary/blob type
3. WHEN mapping Enum type, THE Dialect_Provider SHALL return appropriate integer or string type based on configuration
4. WHEN mapping Nullable<T> type, THE Dialect_Provider SHALL handle NULL values correctly
5. WHEN mapping custom types with TypeConverter, THE Dialect_Provider SHALL use the converter

### Requirement 41: 特殊场景 - 软删除

**User Story:** As a developer, I want the SQL generator to correctly implement soft delete patterns, so that data can be logically deleted.

#### Acceptance Criteria

1. WHEN entity has [SoftDelete] attribute, THE SQL_Template_Engine SHALL generate UPDATE instead of DELETE
2. WHEN entity has IsDeleted column, THE SQL_Template_Engine SHALL set IsDeleted=true for soft delete
3. WHEN entity has DeletedAt column, THE SQL_Template_Engine SHALL set DeletedAt to current timestamp
4. WHEN querying soft-deleted entities, THE SQL_Template_Engine SHALL add WHERE IsDeleted=false by default
5. WHEN using [IncludeDeleted] attribute, THE SQL_Template_Engine SHALL include soft-deleted records

### Requirement 42: 特殊场景 - 审计字段

**User Story:** As a developer, I want the SQL generator to correctly handle audit fields, so that data changes are tracked.

#### Acceptance Criteria

1. WHEN entity has [AuditFields] attribute, THE SQL_Template_Engine SHALL auto-populate CreatedAt on INSERT
2. WHEN entity has [AuditFields] attribute, THE SQL_Template_Engine SHALL auto-populate UpdatedAt on UPDATE
3. WHEN entity has CreatedBy column, THE SQL_Template_Engine SHALL populate with current user if available
4. WHEN entity has UpdatedBy column, THE SQL_Template_Engine SHALL populate with current user if available
5. FOR ALL audit operations, THE SQL_Template_Engine SHALL use database-specific timestamp functions

### Requirement 43: 特殊场景 - 乐观并发控制

**User Story:** As a developer, I want the SQL generator to correctly implement optimistic concurrency, so that concurrent updates are handled correctly.

#### Acceptance Criteria

1. WHEN entity has [ConcurrencyCheck] attribute on a column, THE SQL_Template_Engine SHALL include it in WHERE clause for UPDATE
2. WHEN entity has RowVersion/Timestamp column, THE SQL_Template_Engine SHALL use it for concurrency check
3. WHEN UPDATE affects 0 rows due to concurrency conflict, THE SQL_Template_Engine SHALL enable detection of this condition
4. FOR SQL Server, THE SQL_Template_Engine SHALL use ROWVERSION type for concurrency
5. FOR PostgreSQL, THE SQL_Template_Engine SHALL use xmin system column or custom version column

### Requirement 44: 特殊场景 - 事务支持

**User Story:** As a developer, I want the SQL generator to produce SQL that works correctly within transactions, so that data integrity is maintained.

#### Acceptance Criteria

1. WHEN generating batch operations, THE SQL_Template_Engine SHALL produce SQL that can be executed atomically
2. WHEN generating INSERT with RETURNING, THE SQL_Template_Engine SHALL ensure it works within transaction
3. WHEN generating UPSERT operations, THE SQL_Template_Engine SHALL ensure atomicity
4. FOR ALL generated SQL, THE SQL_Template_Engine SHALL avoid implicit commits that break transactions

### Requirement 45: 特殊场景 - 子查询

**User Story:** As a developer, I want the SQL generator to correctly handle subqueries, so that complex queries work correctly.

#### Acceptance Criteria

1. WHEN generating IN clause with subquery, THE SQL_Template_Engine SHALL produce valid syntax for all databases
2. WHEN generating EXISTS clause with subquery, THE SQL_Template_Engine SHALL produce valid syntax for all databases
3. WHEN generating scalar subquery in SELECT, THE SQL_Template_Engine SHALL produce valid syntax for all databases
4. WHEN generating correlated subquery, THE SQL_Template_Engine SHALL produce valid syntax for all databases
5. FOR ALL subqueries, THE SQL_Template_Engine SHALL use proper identifier quoting

### Requirement 46: 特殊场景 - CASE表达式

**User Story:** As a developer, I want the SQL generator to correctly handle CASE expressions, so that conditional logic works correctly.

#### Acceptance Criteria

1. WHEN generating simple CASE expression, THE SQL_Template_Engine SHALL produce valid syntax for all databases
2. WHEN generating searched CASE expression, THE SQL_Template_Engine SHALL produce valid syntax for all databases
3. WHEN generating nested CASE expressions, THE SQL_Template_Engine SHALL produce valid syntax for all databases
4. WHEN generating CASE with NULL handling, THE SQL_Template_Engine SHALL produce valid syntax for all databases
5. FOR ALL CASE expressions, THE SQL_Template_Engine SHALL use proper identifier quoting

### Requirement 47: 特殊场景 - 窗口函数

**User Story:** As a developer, I want the SQL generator to correctly handle window functions, so that analytical queries work correctly.

#### Acceptance Criteria

1. WHEN generating ROW_NUMBER() OVER, THE SQL_Template_Engine SHALL produce valid syntax for all databases
2. WHEN generating RANK() and DENSE_RANK(), THE SQL_Template_Engine SHALL produce valid syntax for all databases
3. WHEN generating LAG() and LEAD(), THE SQL_Template_Engine SHALL produce valid syntax for all databases
4. WHEN generating SUM() OVER with PARTITION BY, THE SQL_Template_Engine SHALL produce valid syntax for all databases
5. IF database does not support window functions, THEN THE SQL_Template_Engine SHALL report unsupported feature

### Requirement 48: 特殊场景 - CTE (Common Table Expressions)

**User Story:** As a developer, I want the SQL generator to correctly handle CTEs, so that complex recursive queries work correctly.

#### Acceptance Criteria

1. WHEN generating WITH clause for CTE, THE SQL_Template_Engine SHALL produce valid syntax for all databases
2. WHEN generating recursive CTE, THE SQL_Template_Engine SHALL produce valid syntax for databases that support it
3. WHEN generating multiple CTEs, THE SQL_Template_Engine SHALL produce valid syntax with comma separation
4. IF database does not support CTEs, THEN THE SQL_Template_Engine SHALL report unsupported feature
5. FOR ALL CTEs, THE SQL_Template_Engine SHALL use proper identifier quoting

### Requirement 49: 特殊场景 - JSON操作

**User Story:** As a developer, I want the SQL generator to correctly handle JSON operations, so that JSON data can be queried and manipulated.

#### Acceptance Criteria

1. WHEN generating JSON_VALUE for SQL Server, THE SQL_Template_Engine SHALL produce valid syntax
2. WHEN generating jsonb operators for PostgreSQL, THE SQL_Template_Engine SHALL produce valid syntax
3. WHEN generating JSON_EXTRACT for MySQL, THE SQL_Template_Engine SHALL produce valid syntax
4. WHEN generating json_extract for SQLite, THE SQL_Template_Engine SHALL produce valid syntax
5. IF database does not support JSON operations, THEN THE SQL_Template_Engine SHALL report unsupported feature

### Requirement 50: 特殊场景 - 全文搜索

**User Story:** As a developer, I want the SQL generator to correctly handle full-text search, so that text search operations work correctly.

#### Acceptance Criteria

1. WHEN generating MATCH AGAINST for MySQL, THE SQL_Template_Engine SHALL produce valid syntax
2. WHEN generating to_tsvector/to_tsquery for PostgreSQL, THE SQL_Template_Engine SHALL produce valid syntax
3. WHEN generating CONTAINS/FREETEXT for SQL Server, THE SQL_Template_Engine SHALL produce valid syntax
4. WHEN generating FTS5 queries for SQLite, THE SQL_Template_Engine SHALL produce valid syntax
5. IF database does not support full-text search, THEN THE SQL_Template_Engine SHALL report unsupported feature
