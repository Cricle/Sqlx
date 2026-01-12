# Placeholder Reference

Complete guide to all Sqlx placeholders.

## Overview

Placeholders are special tokens in SQL templates that Sqlx replaces with actual SQL code at compile time. They automatically adapt to different database dialects.

## Core Placeholders

### `{{table}}`

Inserts the table name with dialect-specific quoting.

**Example:**
```csharp
[SqlTemplate("SELECT * FROM {{table}}")]
```

**Generated SQL:**
- SQLite: `SELECT * FROM [users]`
- PostgreSQL: `SELECT * FROM "users"`
- MySQL: ``SELECT * FROM `users` ``
- SQL Server: `SELECT * FROM [users]`

---

### `{{columns}}`

Lists all entity properties as column names (snake_case).

**Example:**
```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}}")]
```

**Generated SQL:**
```sql
SELECT id, name, email, age, is_active, created_at FROM users
```

**Options:**

#### `--exclude`
Exclude specific columns:
```csharp
[SqlTemplate("SELECT {{columns --exclude Password Salt}} FROM {{table}}")]
```

#### `--only`
Include only specific columns:
```csharp
[SqlTemplate("SELECT {{columns --only Id Name Email}} FROM {{table}}")]
```

#### `--regex`
Filter columns using regular expressions:
```csharp
// Select only columns starting with "User"
[SqlTemplate("SELECT {{columns --regex ^User.*}} FROM {{table}}")]

// Combine with --exclude
[SqlTemplate("SELECT {{columns --regex ^User.* --exclude UserPassword}} FROM {{table}}")]
```

**Example with regex:**
```csharp
public class UserProfile
{
    public long Id { get; set; }
    public string UserName { get; set; }
    public string UserEmail { get; set; }
    public int UserAge { get; set; }
    public string ProfileBio { get; set; }
    public string ProfileAvatar { get; set; }
}

[SqlTemplate("SELECT {{columns --regex ^User.*}} FROM {{table}}")]
// Generates: SELECT user_name, user_email, user_age FROM user_profile
```

---

### `{{values}}`

Generates parameter placeholders for INSERT statements.

**Example:**
```csharp
[SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
```

**Generated SQL:**
```sql
INSERT INTO users (name, email, age) VALUES (@name, @email, @age)
```

**Note:** Always use `--exclude Id` for auto-increment primary keys.

---

### `{{set}}`

Generates SET clause for UPDATE statements.

**Example:**
```csharp
[SqlTemplate("UPDATE {{table}} SET {{set --exclude Id CreatedAt}} WHERE id = @id")]
```

**Generated SQL:**
```sql
UPDATE users SET name = @name, email = @email, age = @age WHERE id = @id
```

**Options:**

#### `--exclude`
Exclude columns from SET clause:
```csharp
{{set --exclude Id CreatedAt UpdatedAt}}
```

#### `--only`
Include only specific columns:
```csharp
{{set --only Name Email}}
```

---

## Query Modifiers

### `{{where}}`

Generates WHERE clause from expression parameters.

**Basic Usage:**
```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{where}}")]
Task<List<User>> QueryAsync([ExpressionToSql] Expression<Func<User, bool>> predicate);
```

**Usage:**
```csharp
var adults = await repo.QueryAsync(u => u.Age >= 18 && u.IsActive);
```

**Generated SQL:**
```sql
SELECT id, name, age FROM users WHERE age >= @p0 AND is_active = @p1
```

**New Syntax - Specify Parameter:**
```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{where --param predicate}}")]
Task<List<User>> QueryAsync([ExpressionToSql] Expression<Func<User, bool>> predicate);
```

This explicitly specifies which parameter to use for the WHERE clause, useful when you have multiple parameters.

---

## Query Modifiers

### `{{limit}}`

Generates LIMIT clause for pagination.

**Examples:**

```csharp
// Static limit
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{limit 10}}")]

// Dynamic limit with parameter
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{limit --param pageSize}}")]
Task<List<User>> GetPagedAsync(int pageSize);
```

**Generated SQL:**
```sql
SELECT * FROM users LIMIT @pageSize
```

---

### `{{offset}}`

Generates OFFSET clause for pagination.

**Example:**
```csharp
[SqlTemplate(@"
    SELECT {{columns}} 
    FROM {{table}} 
    ORDER BY id 
    {{limit --param pageSize}} 
    {{offset --param skip}}
")]
Task<List<User>> GetPagedAsync(int pageSize, int skip);
```

**Generated SQL:**
```sql
SELECT * FROM users ORDER BY id LIMIT @pageSize OFFSET @skip
```

---

## Dialect-Specific Placeholders

### `{{bool_true}}` / `{{bool_false}}`

Boolean literals that adapt to database dialect.

**Example:**
```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE is_active = {{bool_true}}")]
```

**Generated SQL by Database:**
- SQLite: `WHERE is_active = 1`
- PostgreSQL: `WHERE is_active = true`
- MySQL: `WHERE is_active = 1`
- SQL Server: `WHERE is_active = 1`

---

### `{{current_timestamp}}`

Current timestamp function for each database.

**Example:**
```csharp
[SqlTemplate("UPDATE {{table}} SET updated_at = {{current_timestamp}} WHERE id = @id")]
```

**Generated SQL by Database:**
- SQLite: `SET updated_at = CURRENT_TIMESTAMP`
- PostgreSQL: `SET updated_at = CURRENT_TIMESTAMP`
- MySQL: `SET updated_at = NOW()`
- SQL Server: `SET updated_at = GETDATE()`

---

### `{{wrap column}}`

Wraps identifier with dialect-specific quotes.

**Example:**
```csharp
[SqlTemplate("SELECT {{wrap user_id}} FROM {{table}}")]
```

**Generated SQL by Database:**
- SQLite: `SELECT [user_id] FROM users`
- PostgreSQL: `SELECT "user_id" FROM users`
- MySQL: ``SELECT `user_id` FROM users``
- SQL Server: `SELECT [user_id] FROM users`

---

## Advanced Placeholders

### `{{batch_values}}`

Generates multi-row VALUES for batch inserts.

**Example:**
```csharp
[SqlTemplate("INSERT INTO {{table}} (name, age) VALUES {{batch_values}}")]
[BatchOperation(MaxBatchSize = 500)]
Task<int> BatchInsertAsync(IEnumerable<User> users);
```

**Generated SQL:**
```sql
INSERT INTO users (name, age) VALUES 
    (@name_0, @age_0),
    (@name_1, @age_1),
    (@name_2, @age_2)
```

---

### `{{upsert}}`

Generates UPSERT (INSERT ... ON CONFLICT) statement.

**Example:**
```csharp
[SqlTemplate("{{upsert --key email}}")]
Task<int> UpsertAsync(User user);
```

**Generated SQL by Database:**

**PostgreSQL:**
```sql
INSERT INTO users (email, name, age) 
VALUES (@email, @name, @age)
ON CONFLICT (email) 
DO UPDATE SET name = EXCLUDED.name, age = EXCLUDED.age
```

**MySQL:**
```sql
INSERT INTO users (email, name, age) 
VALUES (@email, @name, @age)
ON DUPLICATE KEY UPDATE name = VALUES(name), age = VALUES(age)
```

**SQLite:**
```sql
INSERT INTO users (email, name, age) 
VALUES (@email, @name, @age)
ON CONFLICT (email) 
DO UPDATE SET name = excluded.name, age = excluded.age
```

---

## Placeholder Options

### Common Options

| Option | Description | Example |
|--------|-------------|---------|
| `--exclude` | Exclude columns | `{{columns --exclude Id Password}}` |
| `--only` | Include only specific columns | `{{columns --only Id Name}}` |
| `--regex` | Filter columns by regex pattern | `{{columns --regex ^User.*}}` |
| `--param` | Use parameter name | `{{limit --param pageSize}}` or `{{where --param predicate}}` |

### Multiple Options

You can combine options:

```csharp
[SqlTemplate(@"
    SELECT {{columns --exclude Password Salt}} 
    FROM {{table}} 
    WHERE created_at > @startDate
    ORDER BY created_at DESC
    {{limit --param count}}
")]
```

---

## Best Practices

### ✅ DO

**Use placeholders for repetitive SQL generation:**
```csharp
[SqlTemplate("SELECT {{columns}} FROM {{table}}")]
```

**Exclude auto-increment IDs in INSERT:**
```csharp
[SqlTemplate("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})")]
```

**Use dialect placeholders for portability:**
```csharp
[SqlTemplate("WHERE is_active = {{bool_true}}")]
```

### ❌ DON'T

**Don't use placeholders for simple WHERE clauses:**
```csharp
// ❌ Wrong
[SqlTemplate("SELECT {{columns}} FROM {{table}} {{where id=@id}}")]

// ✅ Correct
[SqlTemplate("SELECT {{columns}} FROM {{table}} WHERE id = @id")]
```

**Don't hardcode boolean values:**
```csharp
// ❌ Wrong
[SqlTemplate("WHERE is_active = 1")]

// ✅ Correct
[SqlTemplate("WHERE is_active = {{bool_true}}")]
```

**Don't hardcode table names:**
```csharp
// ❌ Wrong
[SqlTemplate("SELECT * FROM users")]

// ✅ Correct
[SqlTemplate("SELECT {{columns}} FROM {{table}}")]
```

---

## Placeholder Cheat Sheet

| Placeholder | Use Case | Example |
|-------------|----------|---------|
| `{{table}}` | Table name | `FROM {{table}}` |
| `{{columns}}` | All columns | `SELECT {{columns}}` |
| `{{columns --exclude Id}}` | Columns except Id | `INSERT ({{columns --exclude Id}})` |
| `{{columns --regex ^User.*}}` | Columns matching regex | `SELECT {{columns --regex ^User.*}}` |
| `{{values}}` | Parameter placeholders | `VALUES ({{values}})` |
| `{{set}}` | UPDATE SET clause | `UPDATE {{table}} SET {{set}}` |
| `{{where}}` | Expression WHERE | `{{where}}` with `[ExpressionToSql]` |
| `{{where --param name}}` | WHERE with specific param | `{{where --param predicate}}` |
| `{{limit}}` | Limit rows | `{{limit --param count}}` |
| `{{offset}}` | Skip rows | `{{offset --param skip}}` |
| `{{bool_true}}` | Boolean true | `WHERE active = {{bool_true}}` |
| `{{current_timestamp}}` | Current time | `SET updated = {{current_timestamp}}` |
| `{{wrap col}}` | Quote identifier | `SELECT {{wrap user_id}}` |

---

## See Also

- [Quick Start Guide](QUICK_START.md)
- [API Reference](API_REFERENCE.md)
- [Best Practices](BEST_PRACTICES.md)
- [Multi-Database Guide](MULTI_DATABASE.md)
