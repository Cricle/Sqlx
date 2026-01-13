using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;
using System.Data;

namespace Sqlx.Tests;

/// <summary>
/// Comprehensive strict SQL generation tests that verify exact SQL output for all placeholders and dialects.
/// </summary>
[TestClass]
public class SqlGenerationStrictTests
{
    private static readonly IReadOnlyList<ColumnMeta> UserColumns = new[]
    {
        new ColumnMeta("id", "Id", DbType.Int64, false),
        new ColumnMeta("user_name", "UserName", DbType.String, false),
        new ColumnMeta("email", "Email", DbType.String, true),
        new ColumnMeta("is_active", "IsActive", DbType.Boolean, false),
        new ColumnMeta("created_at", "CreatedAt", DbType.DateTime, false),
    };

    private static readonly SqlDialect[] AllDialects = new[]
    {
        SqlDefine.SQLite,
        SqlDefine.MySql,
        SqlDefine.PostgreSql,
        SqlDefine.SqlServer,
        SqlDefine.Oracle,
        SqlDefine.DB2,
    };

    private static (string left, string right) GetQuotes(SqlDialect dialect) => dialect.DatabaseType switch
    {
        "SQLite" or "SqlServer" => ("[", "]"),
        "MySql" => ("`", "`"),
        _ => ("\"", "\""),
    };

    #region {{table}} Placeholder Tests

    [TestMethod]
    [DataRow("SQLite", "[users]")]
    [DataRow("MySql", "`users`")]
    [DataRow("PostgreSql", "\"users\"")]
    [DataRow("SqlServer", "[users]")]
    [DataRow("Oracle", "\"users\"")]
    [DataRow("DB2", "\"users\"")]
    public void Table_AllDialects_ExactQuoting(string dialectName, string expected)
    {
        var dialect = GetDialect(dialectName);
        var context = new PlaceholderContext(dialect, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}}", context);
        
        Assert.AreEqual($"SELECT * FROM {expected}", template.Sql);
    }

    [TestMethod]
    public void Table_ReservedWordTable_AllDialects()
    {
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "order", UserColumns);
            var template = SqlTemplate.Prepare("SELECT * FROM {{table}}", context);
            Assert.AreEqual($"SELECT * FROM {l}order{r}", template.Sql, $"Failed for {dialect.DatabaseType}");
        }
    }

    [TestMethod]
    public void Table_SpecialCharacterTable_AllDialects()
    {
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "user-data", UserColumns);
            var template = SqlTemplate.Prepare("SELECT * FROM {{table}}", context);
            Assert.AreEqual($"SELECT * FROM {l}user-data{r}", template.Sql, $"Failed for {dialect.DatabaseType}");
        }
    }

    #endregion

    #region {{columns}} Placeholder Tests

    [TestMethod]
    public void Columns_AllColumns_AllDialects()
    {
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
            
            var expected = $"SELECT {l}id{r}, {l}user_name{r}, {l}email{r}, {l}is_active{r}, {l}created_at{r} FROM {l}users{r}";
            Assert.AreEqual(expected, template.Sql, $"Failed for {dialect.DatabaseType}");
        }
    }

    [TestMethod]
    public void Columns_ExcludeById_AllDialects()
    {
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare("SELECT {{columns --exclude Id}} FROM {{table}}", context);
            
            var expected = $"SELECT {l}user_name{r}, {l}email{r}, {l}is_active{r}, {l}created_at{r} FROM {l}users{r}";
            Assert.AreEqual(expected, template.Sql, $"Failed for {dialect.DatabaseType}");
        }
    }

    [TestMethod]
    public void Columns_ExcludeByColumnName_AllDialects()
    {
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare("SELECT {{columns --exclude id}} FROM {{table}}", context);
            
            var expected = $"SELECT {l}user_name{r}, {l}email{r}, {l}is_active{r}, {l}created_at{r} FROM {l}users{r}";
            Assert.AreEqual(expected, template.Sql, $"Failed for {dialect.DatabaseType}");
        }
    }

    [TestMethod]
    public void Columns_ExcludeMultiple_AllDialects()
    {
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare("SELECT {{columns --exclude Id,Email,CreatedAt}} FROM {{table}}", context);
            
            var expected = $"SELECT {l}user_name{r}, {l}is_active{r} FROM {l}users{r}";
            Assert.AreEqual(expected, template.Sql, $"Failed for {dialect.DatabaseType}");
        }
    }

    [TestMethod]
    public void Columns_SingleColumn_AllDialects()
    {
        var singleColumn = new[] { new ColumnMeta("id", "Id", DbType.Int64, false) };
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "items", singleColumn);
            var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
            
            Assert.AreEqual($"SELECT {l}id{r} FROM {l}items{r}", template.Sql, $"Failed for {dialect.DatabaseType}");
        }
    }

    [TestMethod]
    public void Columns_ReservedWordColumns_AllDialects()
    {
        var reservedColumns = new[]
        {
            new ColumnMeta("select", "Select", DbType.String, false),
            new ColumnMeta("from", "From", DbType.String, false),
            new ColumnMeta("where", "Where", DbType.String, false),
            new ColumnMeta("order", "Order", DbType.String, false),
        };
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "keywords", reservedColumns);
            var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
            
            var expected = $"SELECT {l}select{r}, {l}from{r}, {l}where{r}, {l}order{r} FROM {l}keywords{r}";
            Assert.AreEqual(expected, template.Sql, $"Failed for {dialect.DatabaseType}");
        }
    }

    [TestMethod]
    public void Columns_ExcludeAllButOne_AllDialects()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
        };
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "items", columns);
            var template = SqlTemplate.Prepare("SELECT {{columns --exclude Id}} FROM {{table}}", context);
            
            Assert.AreEqual($"SELECT {l}name{r} FROM {l}items{r}", template.Sql, $"Failed for {dialect.DatabaseType}");
        }
    }

    #endregion

    #region {{values}} Placeholder Tests

    [TestMethod]
    public void Values_AllColumns_AllDialects()
    {
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare("INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})", context);
            
            var expected = $"INSERT INTO {l}users{r} ({l}id{r}, {l}user_name{r}, {l}email{r}, {l}is_active{r}, {l}created_at{r}) VALUES (@id, @user_name, @email, @is_active, @created_at)";
            Assert.AreEqual(expected, template.Sql, $"Failed for {dialect.DatabaseType}");
        }
    }

    [TestMethod]
    public void Values_ExcludeId_AllDialects()
    {
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})", context);
            
            var expected = $"INSERT INTO {l}users{r} ({l}user_name{r}, {l}email{r}, {l}is_active{r}, {l}created_at{r}) VALUES (@user_name, @email, @is_active, @created_at)";
            Assert.AreEqual(expected, template.Sql, $"Failed for {dialect.DatabaseType}");
        }
    }

    [TestMethod]
    public void Values_ExcludeMultiple_AllDialects()
    {
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare("INSERT INTO {{table}} ({{columns --exclude Id,CreatedAt}}) VALUES ({{values --exclude Id,CreatedAt}})", context);
            
            var expected = $"INSERT INTO {l}users{r} ({l}user_name{r}, {l}email{r}, {l}is_active{r}) VALUES (@user_name, @email, @is_active)";
            Assert.AreEqual(expected, template.Sql, $"Failed for {dialect.DatabaseType}");
        }
    }

    [TestMethod]
    public void Values_SingleColumn_AllDialects()
    {
        var singleColumn = new[] { new ColumnMeta("name", "Name", DbType.String, false) };
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "items", singleColumn);
            var template = SqlTemplate.Prepare("INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})", context);
            
            Assert.AreEqual($"INSERT INTO {l}items{r} ({l}name{r}) VALUES (@name)", template.Sql, $"Failed for {dialect.DatabaseType}");
        }
    }

    [TestMethod]
    public void Values_ParameterNamesMatchColumnNames()
    {
        var columns = new[]
        {
            new ColumnMeta("user_id", "UserId", DbType.Int64, false),
            new ColumnMeta("first_name", "FirstName", DbType.String, false),
            new ColumnMeta("last_name", "LastName", DbType.String, false),
        };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        var template = SqlTemplate.Prepare("INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})", context);
        
        Assert.AreEqual("INSERT INTO [users] ([user_id], [first_name], [last_name]) VALUES (@user_id, @first_name, @last_name)", template.Sql);
    }

    #endregion

    #region {{set}} Placeholder Tests

    [TestMethod]
    public void Set_AllColumns_AllDialects()
    {
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set}} WHERE id = @id", context);
            
            var expected = $"UPDATE {l}users{r} SET {l}id{r} = @id, {l}user_name{r} = @user_name, {l}email{r} = @email, {l}is_active{r} = @is_active, {l}created_at{r} = @created_at WHERE id = @id";
            Assert.AreEqual(expected, template.Sql, $"Failed for {dialect.DatabaseType}");
        }
    }

    [TestMethod]
    public void Set_ExcludeId_AllDialects()
    {
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id", context);
            
            var expected = $"UPDATE {l}users{r} SET {l}user_name{r} = @user_name, {l}email{r} = @email, {l}is_active{r} = @is_active, {l}created_at{r} = @created_at WHERE id = @id";
            Assert.AreEqual(expected, template.Sql, $"Failed for {dialect.DatabaseType}");
        }
    }

    [TestMethod]
    public void Set_ExcludeMultiple_AllDialects()
    {
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id,CreatedAt}} WHERE id = @id", context);
            
            var expected = $"UPDATE {l}users{r} SET {l}user_name{r} = @user_name, {l}email{r} = @email, {l}is_active{r} = @is_active WHERE id = @id";
            Assert.AreEqual(expected, template.Sql, $"Failed for {dialect.DatabaseType}");
        }
    }

    [TestMethod]
    public void Set_SingleColumn_AllDialects()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
        };
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "items", columns);
            var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id", context);
            
            Assert.AreEqual($"UPDATE {l}items{r} SET {l}name{r} = @name WHERE id = @id", template.Sql, $"Failed for {dialect.DatabaseType}");
        }
    }

    [TestMethod]
    public void Set_ColumnEqualsParameter_Format()
    {
        var columns = new[]
        {
            new ColumnMeta("user_name", "UserName", DbType.String, false),
            new ColumnMeta("updated_at", "UpdatedAt", DbType.DateTime, false),
        };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set}}", context);
        
        Assert.AreEqual("UPDATE [users] SET [user_name] = @user_name, [updated_at] = @updated_at", template.Sql);
    }

    #endregion

    #region {{limit}} Placeholder Tests - Static

    [TestMethod]
    public void Limit_StaticCount_AllDialects()
    {
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} {{limit --count 10}}", context);
            
            Assert.IsTrue(template.Sql.EndsWith("LIMIT 10"), $"Failed for {dialect.DatabaseType}: {template.Sql}");
        }
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(10)]
    [DataRow(100)]
    [DataRow(1000)]
    [DataRow(0)]
    public void Limit_StaticCount_VariousValues(int count)
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare($"SELECT * FROM {{{{table}}}} {{{{limit --count {count}}}}}", context);
        
        Assert.AreEqual($"SELECT * FROM [users] LIMIT {count}", template.Sql);
    }

    #endregion

    #region {{limit}} Placeholder Tests - Dynamic

    [TestMethod]
    public void Limit_DynamicParam_AllDialects()
    {
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} {{limit --param pageSize}}", context);
            
            var rendered = template.Render(new Dictionary<string, object?> { ["pageSize"] = 25 });
            Assert.IsTrue(rendered.EndsWith("LIMIT 25"), $"Failed for {dialect.DatabaseType}: {rendered}");
        }
    }

    [TestMethod]
    [DataRow(1)]
    [DataRow(10)]
    [DataRow(50)]
    [DataRow(100)]
    [DataRow(500)]
    public void Limit_DynamicParam_VariousValues(int limit)
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} {{limit --param limit}}", context);
        
        var rendered = template.Render(new Dictionary<string, object?> { ["limit"] = limit });
        Assert.AreEqual($"SELECT * FROM [users] LIMIT {limit}", rendered);
    }

    [TestMethod]
    public void Limit_DynamicParam_NullValue_ReturnsEmpty()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} {{limit --param limit}}", context);
        
        var rendered = template.Render(new Dictionary<string, object?> { ["limit"] = null });
        Assert.AreEqual("SELECT * FROM [users] ", rendered);
    }

    #endregion

    #region {{offset}} Placeholder Tests - Static

    [TestMethod]
    public void Offset_StaticCount_AllDialects()
    {
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} LIMIT 10 {{offset --count 20}}", context);
            
            Assert.IsTrue(template.Sql.EndsWith("OFFSET 20"), $"Failed for {dialect.DatabaseType}: {template.Sql}");
        }
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(10)]
    [DataRow(100)]
    [DataRow(1000)]
    public void Offset_StaticCount_VariousValues(int offset)
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare($"SELECT * FROM {{{{table}}}} LIMIT 10 {{{{offset --count {offset}}}}}", context);
        
        Assert.AreEqual($"SELECT * FROM [users] LIMIT 10 OFFSET {offset}", template.Sql);
    }

    #endregion

    #region {{offset}} Placeholder Tests - Dynamic

    [TestMethod]
    public void Offset_DynamicParam_AllDialects()
    {
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} LIMIT 10 {{offset --param skip}}", context);
            
            var rendered = template.Render(new Dictionary<string, object?> { ["skip"] = 30 });
            Assert.IsTrue(rendered.EndsWith("OFFSET 30"), $"Failed for {dialect.DatabaseType}: {rendered}");
        }
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(20)]
    [DataRow(100)]
    [DataRow(500)]
    public void Offset_DynamicParam_VariousValues(int offset)
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} LIMIT 10 {{offset --param offset}}", context);
        
        var rendered = template.Render(new Dictionary<string, object?> { ["offset"] = offset });
        Assert.AreEqual($"SELECT * FROM [users] LIMIT 10 OFFSET {offset}", rendered);
    }

    [TestMethod]
    public void Offset_DynamicParam_NullValue_ReturnsEmpty()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} LIMIT 10 {{offset --param offset}}", context);
        
        var rendered = template.Render(new Dictionary<string, object?> { ["offset"] = null });
        Assert.AreEqual("SELECT * FROM [users] LIMIT 10 ", rendered);
    }

    #endregion

    #region {{where}} Placeholder Tests - Dynamic

    [TestMethod]
    public void Where_DynamicParam_AllDialects()
    {
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} WHERE {{where --param filter}}", context);
            
            var rendered = template.Render(new Dictionary<string, object?> { ["filter"] = "is_active = 1" });
            Assert.IsTrue(rendered.Contains("WHERE is_active = 1"), $"Failed for {dialect.DatabaseType}: {rendered}");
        }
    }

    [TestMethod]
    public void Where_ComplexCondition_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} WHERE {{where --param predicate}}", context);
        
        var rendered = template.Render(new Dictionary<string, object?> { ["predicate"] = "is_active = 1 AND email IS NOT NULL AND created_at > '2024-01-01'" });
        Assert.AreEqual("SELECT [id], [user_name], [email], [is_active], [created_at] FROM [users] WHERE is_active = 1 AND email IS NOT NULL AND created_at > '2024-01-01'", rendered);
    }

    [TestMethod]
    public void Where_WithOrCondition_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.MySql, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} WHERE {{where --param filter}}", context);
        
        var rendered = template.Render(new Dictionary<string, object?> { ["filter"] = "user_name = 'admin' OR email LIKE '%@example.com'" });
        Assert.AreEqual("SELECT `id`, `user_name`, `email`, `is_active`, `created_at` FROM `users` WHERE user_name = 'admin' OR email LIKE '%@example.com'", rendered);
    }

    [TestMethod]
    public void Where_WithInClause_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} WHERE {{where --param filter}}", context);
        
        var rendered = template.Render(new Dictionary<string, object?> { ["filter"] = "id IN (1, 2, 3, 4, 5)" });
        Assert.AreEqual("SELECT \"id\", \"user_name\", \"email\", \"is_active\", \"created_at\" FROM \"users\" WHERE id IN (1, 2, 3, 4, 5)", rendered);
    }

    [TestMethod]
    public void Where_WithBetween_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.SqlServer, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} WHERE {{where --param filter}}", context);
        
        var rendered = template.Render(new Dictionary<string, object?> { ["filter"] = "created_at BETWEEN '2024-01-01' AND '2024-12-31'" });
        Assert.AreEqual("SELECT [id], [user_name], [email], [is_active], [created_at] FROM [users] WHERE created_at BETWEEN '2024-01-01' AND '2024-12-31'", rendered);
    }

    [TestMethod]
    public void Where_EmptyString_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} WHERE {{where --param filter}}", context);
        
        var rendered = template.Render(new Dictionary<string, object?> { ["filter"] = "" });
        Assert.AreEqual("SELECT [id], [user_name], [email], [is_active], [created_at] FROM [users] WHERE ", rendered);
    }

    #endregion

    #region Combined Limit + Offset Tests (Pagination)

    [TestMethod]
    public void Pagination_StaticLimitOffset_AllDialects()
    {
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} {{limit --count 10}} {{offset --count 20}}", context);
            
            Assert.IsTrue(template.Sql.Contains("LIMIT 10") && template.Sql.Contains("OFFSET 20"), 
                $"Failed for {dialect.DatabaseType}: {template.Sql}");
        }
    }

    [TestMethod]
    public void Pagination_DynamicLimitOffset_AllDialects()
    {
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} {{limit --param pageSize}} {{offset --param skip}}", context);
            
            var rendered = template.Render(new Dictionary<string, object?> { ["pageSize"] = 15, ["skip"] = 45 });
            Assert.IsTrue(rendered.Contains("LIMIT 15") && rendered.Contains("OFFSET 45"), 
                $"Failed for {dialect.DatabaseType}: {rendered}");
        }
    }

    [TestMethod]
    [DataRow(1, 0, "LIMIT 1 OFFSET 0")]
    [DataRow(10, 0, "LIMIT 10 OFFSET 0")]
    [DataRow(10, 10, "LIMIT 10 OFFSET 10")]
    [DataRow(20, 40, "LIMIT 20 OFFSET 40")]
    [DataRow(50, 100, "LIMIT 50 OFFSET 100")]
    [DataRow(100, 500, "LIMIT 100 OFFSET 500")]
    public void Pagination_VariousPageSizes_ExactSql(int pageSize, int skip, string expectedSuffix)
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} {{limit --param pageSize}} {{offset --param skip}}", context);
        
        var rendered = template.Render(new Dictionary<string, object?> { ["pageSize"] = pageSize, ["skip"] = skip });
        Assert.AreEqual($"SELECT * FROM [users] {expectedSuffix}", rendered);
    }

    #endregion

    #region Complete CRUD Operations - All Dialects

    [TestMethod]
    public void CRUD_Select_AllDialects_ExactSql()
    {
        var testCases = new[]
        {
            (SqlDefine.SQLite, "SELECT [id], [user_name], [email], [is_active], [created_at] FROM [users]"),
            (SqlDefine.MySql, "SELECT `id`, `user_name`, `email`, `is_active`, `created_at` FROM `users`"),
            (SqlDefine.PostgreSql, "SELECT \"id\", \"user_name\", \"email\", \"is_active\", \"created_at\" FROM \"users\""),
            (SqlDefine.SqlServer, "SELECT [id], [user_name], [email], [is_active], [created_at] FROM [users]"),
            (SqlDefine.Oracle, "SELECT \"id\", \"user_name\", \"email\", \"is_active\", \"created_at\" FROM \"users\""),
            (SqlDefine.DB2, "SELECT \"id\", \"user_name\", \"email\", \"is_active\", \"created_at\" FROM \"users\""),
        };

        foreach (var (dialect, expected) in testCases)
        {
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
            Assert.AreEqual(expected, template.Sql, $"Failed for {dialect.DatabaseType}");
        }
    }

    [TestMethod]
    public void CRUD_Insert_AllDialects_ExactSql()
    {
        var testCases = new[]
        {
            (SqlDefine.SQLite, "INSERT INTO [users] ([user_name], [email], [is_active], [created_at]) VALUES (@user_name, @email, @is_active, @created_at)"),
            (SqlDefine.MySql, "INSERT INTO `users` (`user_name`, `email`, `is_active`, `created_at`) VALUES (@user_name, @email, @is_active, @created_at)"),
            (SqlDefine.PostgreSql, "INSERT INTO \"users\" (\"user_name\", \"email\", \"is_active\", \"created_at\") VALUES (@user_name, @email, @is_active, @created_at)"),
            (SqlDefine.SqlServer, "INSERT INTO [users] ([user_name], [email], [is_active], [created_at]) VALUES (@user_name, @email, @is_active, @created_at)"),
            (SqlDefine.Oracle, "INSERT INTO \"users\" (\"user_name\", \"email\", \"is_active\", \"created_at\") VALUES (@user_name, @email, @is_active, @created_at)"),
            (SqlDefine.DB2, "INSERT INTO \"users\" (\"user_name\", \"email\", \"is_active\", \"created_at\") VALUES (@user_name, @email, @is_active, @created_at)"),
        };

        foreach (var (dialect, expected) in testCases)
        {
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare("INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}})", context);
            Assert.AreEqual(expected, template.Sql, $"Failed for {dialect.DatabaseType}");
        }
    }

    [TestMethod]
    public void CRUD_Update_AllDialects_ExactSql()
    {
        var testCases = new[]
        {
            (SqlDefine.SQLite, "UPDATE [users] SET [user_name] = @user_name, [email] = @email, [is_active] = @is_active, [created_at] = @created_at WHERE id = @id"),
            (SqlDefine.MySql, "UPDATE `users` SET `user_name` = @user_name, `email` = @email, `is_active` = @is_active, `created_at` = @created_at WHERE id = @id"),
            (SqlDefine.PostgreSql, "UPDATE \"users\" SET \"user_name\" = @user_name, \"email\" = @email, \"is_active\" = @is_active, \"created_at\" = @created_at WHERE id = @id"),
            (SqlDefine.SqlServer, "UPDATE [users] SET [user_name] = @user_name, [email] = @email, [is_active] = @is_active, [created_at] = @created_at WHERE id = @id"),
            (SqlDefine.Oracle, "UPDATE \"users\" SET \"user_name\" = @user_name, \"email\" = @email, \"is_active\" = @is_active, \"created_at\" = @created_at WHERE id = @id"),
            (SqlDefine.DB2, "UPDATE \"users\" SET \"user_name\" = @user_name, \"email\" = @email, \"is_active\" = @is_active, \"created_at\" = @created_at WHERE id = @id"),
        };

        foreach (var (dialect, expected) in testCases)
        {
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id}} WHERE id = @id", context);
            Assert.AreEqual(expected, template.Sql, $"Failed for {dialect.DatabaseType}");
        }
    }

    [TestMethod]
    public void CRUD_Delete_AllDialects_ExactSql()
    {
        var testCases = new[]
        {
            (SqlDefine.SQLite, "DELETE FROM [users] WHERE id = @id"),
            (SqlDefine.MySql, "DELETE FROM `users` WHERE id = @id"),
            (SqlDefine.PostgreSql, "DELETE FROM \"users\" WHERE id = @id"),
            (SqlDefine.SqlServer, "DELETE FROM [users] WHERE id = @id"),
            (SqlDefine.Oracle, "DELETE FROM \"users\" WHERE id = @id"),
            (SqlDefine.DB2, "DELETE FROM \"users\" WHERE id = @id"),
        };

        foreach (var (dialect, expected) in testCases)
        {
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare("DELETE FROM {{table}} WHERE id = @id", context);
            Assert.AreEqual(expected, template.Sql, $"Failed for {dialect.DatabaseType}");
        }
    }

    #endregion

    #region Complex Query Combinations

    [TestMethod]
    public void Complex_SelectWithWhereAndPagination_AllDialects()
    {
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare(
                "SELECT {{columns}} FROM {{table}} WHERE {{where --param filter}} ORDER BY id {{limit --param limit}} {{offset --param offset}}",
                context);
            
            var rendered = template.Render(new Dictionary<string, object?>
            {
                ["filter"] = "is_active = 1",
                ["limit"] = 10,
                ["offset"] = 20
            });
            
            Assert.IsTrue(rendered.Contains($"FROM {l}users{r}"), $"Table failed for {dialect.DatabaseType}");
            Assert.IsTrue(rendered.Contains("WHERE is_active = 1"), $"Where failed for {dialect.DatabaseType}");
            Assert.IsTrue(rendered.Contains("LIMIT 10"), $"Limit failed for {dialect.DatabaseType}");
            Assert.IsTrue(rendered.Contains("OFFSET 20"), $"Offset failed for {dialect.DatabaseType}");
        }
    }

    [TestMethod]
    public void Complex_SelectWithMultiplePlaceholders_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "SELECT {{columns --exclude Email}} FROM {{table}} WHERE {{where --param filter}} {{limit --count 50}}",
            context);
        
        var rendered = template.Render(new Dictionary<string, object?> { ["filter"] = "is_active = 1" });
        Assert.AreEqual(
            "SELECT [id], [user_name], [is_active], [created_at] FROM [users] WHERE is_active = 1 LIMIT 50",
            rendered);
    }

    [TestMethod]
    public void Complex_InsertWithReturning_PostgreSql()
    {
        var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns --exclude Id}}) VALUES ({{values --exclude Id}}) RETURNING id",
            context);
        
        Assert.AreEqual(
            "INSERT INTO \"users\" (\"user_name\", \"email\", \"is_active\", \"created_at\") VALUES (@user_name, @email, @is_active, @created_at) RETURNING id",
            template.Sql);
    }

    [TestMethod]
    public void Complex_UpdateWithDynamicWhere_ExactSql()
    {
        var context = new PlaceholderContext(SqlDefine.MySql, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "UPDATE {{table}} SET {{set --exclude Id,CreatedAt}} WHERE {{where --param condition}}",
            context);
        
        var rendered = template.Render(new Dictionary<string, object?> { ["condition"] = "id = @id AND is_active = 1" });
        Assert.AreEqual(
            "UPDATE `users` SET `user_name` = @user_name, `email` = @email, `is_active` = @is_active WHERE id = @id AND is_active = 1",
            rendered);
    }

    [TestMethod]
    public void Complex_SelectCount_AllDialects()
    {
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare("SELECT COUNT(*) FROM {{table}}", context);
            
            Assert.AreEqual($"SELECT COUNT(*) FROM {l}users{r}", template.Sql, $"Failed for {dialect.DatabaseType}");
        }
    }

    [TestMethod]
    public void Complex_SelectDistinct_AllDialects()
    {
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare("SELECT DISTINCT {{columns --exclude Id}} FROM {{table}}", context);
            
            var expected = $"SELECT DISTINCT {l}user_name{r}, {l}email{r}, {l}is_active{r}, {l}created_at{r} FROM {l}users{r}";
            Assert.AreEqual(expected, template.Sql, $"Failed for {dialect.DatabaseType}");
        }
    }

    #endregion

    #region Edge Cases and Boundary Tests

    [TestMethod]
    public void Edge_EmptyColumns_ThrowsOrEmpty()
    {
        var emptyColumns = Array.Empty<ColumnMeta>();
        var context = new PlaceholderContext(SqlDefine.SQLite, "empty", emptyColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        // Empty columns should produce empty column list
        Assert.AreEqual("SELECT  FROM [empty]", template.Sql);
    }

    [TestMethod]
    public void Edge_ExcludeAllColumns_EmptyResult()
    {
        var columns = new[] { new ColumnMeta("id", "Id", DbType.Int64, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "items", columns);
        var template = SqlTemplate.Prepare("SELECT {{columns --exclude Id}} FROM {{table}}", context);
        
        Assert.AreEqual("SELECT  FROM [items]", template.Sql);
    }

    [TestMethod]
    public void Edge_ExcludeNonExistentColumn_NoEffect()
    {
        var columns = new[] { new ColumnMeta("id", "Id", DbType.Int64, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, "items", columns);
        var template = SqlTemplate.Prepare("SELECT {{columns --exclude NonExistent}} FROM {{table}}", context);
        
        Assert.AreEqual("SELECT [id] FROM [items]", template.Sql);
    }

    [TestMethod]
    public void Edge_MixedCaseExclude_CaseInsensitive()
    {
        var columns = new[]
        {
            new ColumnMeta("id", "Id", DbType.Int64, false),
            new ColumnMeta("name", "Name", DbType.String, false),
        };
        var context = new PlaceholderContext(SqlDefine.SQLite, "items", columns);
        
        // Test various case combinations
        var template1 = SqlTemplate.Prepare("SELECT {{columns --exclude ID}} FROM {{table}}", context);
        var template2 = SqlTemplate.Prepare("SELECT {{columns --exclude id}} FROM {{table}}", context);
        var template3 = SqlTemplate.Prepare("SELECT {{columns --exclude Id}} FROM {{table}}", context);
        
        Assert.AreEqual("SELECT [name] FROM [items]", template1.Sql);
        Assert.AreEqual("SELECT [name] FROM [items]", template2.Sql);
        Assert.AreEqual("SELECT [name] FROM [items]", template3.Sql);
    }

    [TestMethod]
    public void Edge_NoPlaceholders_PassThrough()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT * FROM users WHERE id = @id", context);
        
        Assert.AreEqual("SELECT * FROM users WHERE id = @id", template.Sql);
    }

    [TestMethod]
    public void Edge_MultipleSamePlaceholders_AllReplaced()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} UNION SELECT {{columns}} FROM {{table}}", context);
        
        Assert.AreEqual(
            "SELECT [id], [user_name], [email], [is_active], [created_at] FROM [users] UNION SELECT [id], [user_name], [email], [is_active], [created_at] FROM [users]",
            template.Sql);
    }

    [TestMethod]
    public void Edge_PlaceholderWithExtraSpaces_Handled()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns   --exclude   Id}} FROM {{table}}", context);
        
        Assert.AreEqual("SELECT [user_name], [email], [is_active], [created_at] FROM [users]", template.Sql);
    }

    [TestMethod]
    public void Edge_VeryLongTableName_Handled()
    {
        var longTableName = "this_is_a_very_long_table_name_that_might_be_used_in_some_databases";
        var columns = new[] { new ColumnMeta("id", "Id", DbType.Int64, false) };
        var context = new PlaceholderContext(SqlDefine.SQLite, longTableName, columns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        Assert.AreEqual($"SELECT [id] FROM [{longTableName}]", template.Sql);
    }

    [TestMethod]
    public void Edge_ColumnWithUnderscore_Handled()
    {
        var columns = new[]
        {
            new ColumnMeta("user_first_name", "UserFirstName", DbType.String, false),
            new ColumnMeta("user_last_name", "UserLastName", DbType.String, false),
        };
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", columns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        Assert.AreEqual("SELECT [user_first_name], [user_last_name] FROM [users]", template.Sql);
    }

    [TestMethod]
    public void Edge_ManyColumns_AllIncluded()
    {
        var columns = Enumerable.Range(1, 20)
            .Select(i => new ColumnMeta($"col{i}", $"Col{i}", DbType.String, false))
            .ToArray();
        var context = new PlaceholderContext(SqlDefine.SQLite, "wide_table", columns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        var expectedColumns = string.Join(", ", Enumerable.Range(1, 20).Select(i => $"[col{i}]"));
        Assert.AreEqual($"SELECT {expectedColumns} FROM [wide_table]", template.Sql);
    }

    #endregion

    #region Error Handling Tests

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Error_WhereWithoutParam_Throws()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} WHERE {{where}}", context);
        // Should throw when trying to render without --param option
        template.Render(new Dictionary<string, object?>());
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Error_LimitDynamicWithoutParam_Throws()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} {{limit --param pageSize}}", context);
        // Should throw when rendering without providing the parameter
        template.Render(new Dictionary<string, object?>());
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Error_OffsetDynamicWithoutParam_Throws()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} {{offset --param skip}}", context);
        // Should throw when rendering without providing the parameter
        template.Render(new Dictionary<string, object?>());
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void Error_WhereMissingParameter_Throws()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} WHERE {{where --param filter}}", context);
        // Should throw when rendering without providing the parameter
        template.Render(new Dictionary<string, object?>());
    }

    #endregion

    #region Render vs Sql Property Tests

    [TestMethod]
    public void RenderVsSql_StaticOnly_SameResult()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} {{limit --count 10}}", context);
        
        // For static-only templates, Sql and Render should produce same result
        Assert.AreEqual(template.Sql, template.Render(null));
        Assert.AreEqual(template.Sql, template.Render(new Dictionary<string, object?>()));
    }

    [TestMethod]
    public void RenderVsSql_WithDynamic_DifferentResult()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} {{limit --param limit}}", context);
        
        // Sql property should have placeholder marker for dynamic parts
        Assert.IsFalse(template.Sql.Contains("LIMIT 10"));
        
        // Render should produce complete SQL
        var rendered = template.Render(new Dictionary<string, object?> { ["limit"] = 10 });
        Assert.IsTrue(rendered.Contains("LIMIT 10"));
    }

    [TestMethod]
    public void Render_MultipleCalls_ConsistentResults()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} {{limit --param limit}} {{offset --param offset}}", context);
        
        var params1 = new Dictionary<string, object?> { ["limit"] = 10, ["offset"] = 0 };
        var params2 = new Dictionary<string, object?> { ["limit"] = 20, ["offset"] = 40 };
        
        var result1a = template.Render(params1);
        var result2 = template.Render(params2);
        var result1b = template.Render(params1);
        
        Assert.AreEqual(result1a, result1b, "Same parameters should produce same result");
        Assert.AreNotEqual(result1a, result2, "Different parameters should produce different results");
    }

    #endregion

    #region Special SQL Patterns

    [TestMethod]
    public void Pattern_SelectById_AllDialects()
    {
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}} WHERE id = @id", context);
            
            Assert.IsTrue(template.Sql.StartsWith($"SELECT {l}id{r}"), $"Failed for {dialect.DatabaseType}");
            Assert.IsTrue(template.Sql.EndsWith("WHERE id = @id"), $"Failed for {dialect.DatabaseType}");
        }
    }

    [TestMethod]
    public void Pattern_SelectWithJoin_TablePlaceholder()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT u.* FROM {{table}} u JOIN orders o ON u.id = o.user_id", context);
        
        Assert.AreEqual("SELECT u.* FROM [users] u JOIN orders o ON u.id = o.user_id", template.Sql);
    }

    [TestMethod]
    public void Pattern_Subquery_TablePlaceholder()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users", UserColumns);
        var template = SqlTemplate.Prepare("SELECT * FROM {{table}} WHERE id IN (SELECT user_id FROM orders)", context);
        
        Assert.AreEqual("SELECT * FROM [users] WHERE id IN (SELECT user_id FROM orders)", template.Sql);
    }

    [TestMethod]
    public void Pattern_InsertSelect_MixedPlaceholders()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "users_backup", UserColumns);
        var template = SqlTemplate.Prepare("INSERT INTO {{table}} ({{columns}}) SELECT {{columns}} FROM users", context);
        
        Assert.AreEqual(
            "INSERT INTO [users_backup] ([id], [user_name], [email], [is_active], [created_at]) SELECT [id], [user_name], [email], [is_active], [created_at] FROM users",
            template.Sql);
    }

    [TestMethod]
    public void Pattern_Upsert_PostgreSql()
    {
        var context = new PlaceholderContext(SqlDefine.PostgreSql, "users", UserColumns);
        var template = SqlTemplate.Prepare(
            "INSERT INTO {{table}} ({{columns}}) VALUES ({{values}}) ON CONFLICT (id) DO UPDATE SET {{set --exclude Id}}",
            context);
        
        Assert.AreEqual(
            "INSERT INTO \"users\" (\"id\", \"user_name\", \"email\", \"is_active\", \"created_at\") VALUES (@id, @user_name, @email, @is_active, @created_at) ON CONFLICT (id) DO UPDATE SET \"user_name\" = @user_name, \"email\" = @email, \"is_active\" = @is_active, \"created_at\" = @created_at",
            template.Sql);
    }

    #endregion

    #region Dialect-Specific Quoting Verification

    [TestMethod]
    public void Quoting_SQLite_BracketStyle()
    {
        var context = new PlaceholderContext(SqlDefine.SQLite, "test", UserColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        Assert.IsTrue(template.Sql.Contains("[id]"));
        Assert.IsTrue(template.Sql.Contains("[test]"));
        Assert.IsFalse(template.Sql.Contains("`"));
        Assert.IsFalse(template.Sql.Contains("\""));
    }

    [TestMethod]
    public void Quoting_MySql_BacktickStyle()
    {
        var context = new PlaceholderContext(SqlDefine.MySql, "test", UserColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        Assert.IsTrue(template.Sql.Contains("`id`"));
        Assert.IsTrue(template.Sql.Contains("`test`"));
        Assert.IsFalse(template.Sql.Contains("["));
        Assert.IsFalse(template.Sql.Contains("\""));
    }

    [TestMethod]
    public void Quoting_PostgreSql_DoubleQuoteStyle()
    {
        var context = new PlaceholderContext(SqlDefine.PostgreSql, "test", UserColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        Assert.IsTrue(template.Sql.Contains("\"id\""));
        Assert.IsTrue(template.Sql.Contains("\"test\""));
        Assert.IsFalse(template.Sql.Contains("["));
        Assert.IsFalse(template.Sql.Contains("`"));
    }

    [TestMethod]
    public void Quoting_SqlServer_BracketStyle()
    {
        var context = new PlaceholderContext(SqlDefine.SqlServer, "test", UserColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        Assert.IsTrue(template.Sql.Contains("[id]"));
        Assert.IsTrue(template.Sql.Contains("[test]"));
        Assert.IsFalse(template.Sql.Contains("`"));
        Assert.IsFalse(template.Sql.Contains("\""));
    }

    [TestMethod]
    public void Quoting_Oracle_DoubleQuoteStyle()
    {
        var context = new PlaceholderContext(SqlDefine.Oracle, "test", UserColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        Assert.IsTrue(template.Sql.Contains("\"id\""));
        Assert.IsTrue(template.Sql.Contains("\"test\""));
        Assert.IsFalse(template.Sql.Contains("["));
        Assert.IsFalse(template.Sql.Contains("`"));
    }

    [TestMethod]
    public void Quoting_DB2_DoubleQuoteStyle()
    {
        var context = new PlaceholderContext(SqlDefine.DB2, "test", UserColumns);
        var template = SqlTemplate.Prepare("SELECT {{columns}} FROM {{table}}", context);
        
        Assert.IsTrue(template.Sql.Contains("\"id\""));
        Assert.IsTrue(template.Sql.Contains("\"test\""));
        Assert.IsFalse(template.Sql.Contains("["));
        Assert.IsFalse(template.Sql.Contains("`"));
    }

    #endregion

    #region Parameter Format Tests

    [TestMethod]
    public void Parameters_AlwaysUseAtPrefix()
    {
        foreach (var dialect in AllDialects)
        {
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare("INSERT INTO {{table}} ({{columns}}) VALUES ({{values}})", context);
            
            Assert.IsTrue(template.Sql.Contains("@id"), $"Failed for {dialect.DatabaseType}");
            Assert.IsTrue(template.Sql.Contains("@user_name"), $"Failed for {dialect.DatabaseType}");
            Assert.IsTrue(template.Sql.Contains("@email"), $"Failed for {dialect.DatabaseType}");
        }
    }

    [TestMethod]
    public void Parameters_SetClauseFormat()
    {
        foreach (var dialect in AllDialects)
        {
            var (l, r) = GetQuotes(dialect);
            var context = new PlaceholderContext(dialect, "users", UserColumns);
            var template = SqlTemplate.Prepare("UPDATE {{table}} SET {{set --exclude Id}}", context);
            
            // Verify format: [column] = @column
            Assert.IsTrue(template.Sql.Contains($"{l}user_name{r} = @user_name"), $"Failed for {dialect.DatabaseType}");
            Assert.IsTrue(template.Sql.Contains($"{l}email{r} = @email"), $"Failed for {dialect.DatabaseType}");
        }
    }

    #endregion

    #region Helper Methods

    private static SqlDialect GetDialect(string name) => name switch
    {
        "SQLite" => SqlDefine.SQLite,
        "MySql" => SqlDefine.MySql,
        "PostgreSql" => SqlDefine.PostgreSql,
        "SqlServer" => SqlDefine.SqlServer,
        "Oracle" => SqlDefine.Oracle,
        "DB2" => SqlDefine.DB2,
        _ => throw new ArgumentException($"Unknown dialect: {name}")
    };

    #endregion
}
