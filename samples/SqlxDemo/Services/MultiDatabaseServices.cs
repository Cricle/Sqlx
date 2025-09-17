using Microsoft.Data.Sqlite;
using SqlxDemo.Models;
using Sqlx.Annotations;

namespace SqlxDemo.Services;

/// <summary>
/// MySQL用户服务 - 演示多数据库方言支持，使用主构造函数
/// </summary>
[SqlDefine(SqlDefineTypes.MySql)]
public partial class MySqlUserService(SqliteConnection connection) : IUserService
{
    // 演示中使用SQLite连接，但生成MySQL语法：使用 `column` 和 @param
    [Sqlx("SELECT * FROM `user` WHERE `is_active` = 1")]
    public partial Task<IList<User>> GetActiveUsersAsync();

    [Sqlx("SELECT * FROM `user` WHERE `id` = @id")]
    public partial Task<User?> GetUserByIdAsync(int id);

    [Sqlx("SELECT * FROM `user` WHERE `age` BETWEEN @min_age AND @max_age")]
    public partial Task<IList<User>> GetUsersByAgeRangeAsync(int minAge, int maxAge);
    
    [Sqlx("SELECT COUNT(*) FROM `user` WHERE `department_id` = @dept_id")]
    public partial Task<int> GetUserCountByDepartmentAsync(int deptId);
}

/// <summary>
/// SQL Server用户服务 - 演示多数据库方言支持，使用主构造函数
/// </summary>
[SqlDefine(SqlDefineTypes.SqlServer)]
public partial class SqlServerUserService(SqliteConnection connection) : IUserService
{
    // 演示中使用SQLite连接，但生成SQL Server语法：使用 [column] 和 @param
    [Sqlx("SELECT * FROM [user] WHERE [is_active] = 1")]
    public partial Task<IList<User>> GetActiveUsersAsync();
    
    [Sqlx("SELECT * FROM [user] WHERE [id] = @id")]
    public partial Task<User?> GetUserByIdAsync(int id);
    
    [Sqlx("SELECT * FROM [user] WHERE [age] BETWEEN @min_age AND @max_age")]
    public partial Task<IList<User>> GetUsersByAgeRangeAsync(int minAge, int maxAge);
    
    [Sqlx("SELECT COUNT(*) FROM [user] WHERE [department_id] = @dept_id")]
    public partial Task<int> GetUserCountByDepartmentAsync(int deptId);
}

/// <summary>
/// PostgreSQL用户服务 - 演示多数据库方言支持，使用主构造函数
/// </summary>
[SqlDefine(SqlDefineTypes.PostgreSql)]
public partial class PostgreSqlUserService(SqliteConnection connection) : IUserService
{
    // 演示中使用SQLite连接，但生成PostgreSQL语法：使用 "column" 和 $param
    [Sqlx("SELECT * FROM \"user\" WHERE \"is_active\" = 1")]
    public partial Task<IList<User>> GetActiveUsersAsync();
    
    [Sqlx("SELECT * FROM \"user\" WHERE \"id\" = $1")]
    public partial Task<User?> GetUserByIdAsync(int id);
    
    [Sqlx("SELECT * FROM \"user\" WHERE \"age\" BETWEEN $1 AND $2")]
    public partial Task<IList<User>> GetUsersByAgeRangeAsync(int minAge, int maxAge);
    
    [Sqlx("SELECT COUNT(*) FROM \"user\" WHERE \"department_id\" = $1")]
    public partial Task<int> GetUserCountByDepartmentAsync(int deptId);
}
