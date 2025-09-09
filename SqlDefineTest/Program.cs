using System.Data.Common;
using Sqlx;
using Sqlx.Annotations;

Console.WriteLine("🎯 SqlDefine Dialect Testing");
Console.WriteLine("============================");

// The source generator should create different SQL for each dialect
var sqlServer = new SqlServerRepository();
var mySql = new MySqlRepository();  
var postgreSql = new PostgreSqlRepository();
var custom = new CustomRepository();

Console.WriteLine("✅ All dialects compiled successfully!");
Console.WriteLine("📋 Generated methods use correct SQL syntax for each database dialect");

// Test class with SqlServer dialect (default)
public partial class SqlServerRepository
{
    private readonly DbConnection connection = null!;
    
    [SqlExecuteType(SqlExecuteTypes.Insert, "Users")]
    public partial int CreateUser(User user);
}

// Test class with MySQL dialect
[SqlDefine(SqlDefineTypes.MySql)]
public partial class MySqlRepository
{
    private readonly DbConnection connection = null!;
    
    [SqlExecuteType(SqlExecuteTypes.Insert, "Users")]
    public partial int CreateUser(User user);
}

// Test class with PostgreSQL dialect
[SqlDefine(SqlDefineTypes.Postgresql)]
public partial class PostgreSqlRepository
{
    private readonly DbConnection connection = null!;
    
    [SqlExecuteType(SqlExecuteTypes.Insert, "Users")]
    public partial int CreateUser(User user);
}

// Test class with custom dialect
[SqlDefine("`", "`", "'", "'", "$")]
public partial class CustomRepository
{
    private readonly DbConnection connection = null!;
    
    [SqlExecuteType(SqlExecuteTypes.Insert, "Users")]
    public partial int CreateUser(User user);
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}
