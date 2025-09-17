using SqlxDemo.Models;
using Sqlx.Annotations;
using Microsoft.Data.Sqlite;
using System.Data.Common;
using System.Linq.Expressions;

namespace SqlxDemo.Services;

/// <summary>
/// 高级功能演示服务 - 展示 ExpressionToSql、SqlExecuteType 和 DbSetType 等高级特性
/// </summary>
public interface IAdvancedFeatureService
{
    /// <summary>
    /// 使用 ExpressionToSql 进行动态查询 - 演示表达式转SQL
    /// </summary>
    [Sqlx("SELECT * FROM [user] WHERE {whereCondition} ORDER BY [name]")]
    Task<IList<User>> GetUsersByExpressionAsync([ExpressionToSql] Expression<Func<User, bool>> whereCondition);
    
    /// <summary>
    /// 使用 ExpressionToSql 的复杂查询
    /// </summary>
    [Sqlx("SELECT * FROM [user] WHERE {whereCondition} AND [is_active] = 1 ORDER BY {orderBy}")]
    Task<IList<User>> GetActiveUsersByExpressionAsync(
        [ExpressionToSql] Expression<Func<User, bool>> whereCondition,
        [ExpressionToSql] Expression<Func<User, object>> orderBy);
    
    /// <summary>
    /// 使用 SqlExecuteType 进行插入操作 - 演示操作类型标注
    /// </summary>
    [SqlExecuteType(SqlOperation.Insert, "user")]
    Task<int> CreateUserAsync(string name, string email, int age, decimal salary, int departmentId);
    
    /// <summary>
    /// 使用 SqlExecuteType 进行更新操作
    /// </summary>
    [SqlExecuteType(SqlOperation.Update, "user")]
    [Sqlx("UPDATE [user] SET [salary] = @salary, [performance_rating] = @rating WHERE [id] = @user_id")]
    Task<int> UpdateUserSalaryAsync(int userId, decimal salary, decimal rating);
    
    /// <summary>
    /// 使用 SqlExecuteType 进行删除操作
    /// </summary>
    [SqlExecuteType(SqlOperation.Delete, "user")]
    [Sqlx("DELETE FROM [user] WHERE [id] = @user_id AND [is_active] = 0")]
    Task<int> DeleteInactiveUserAsync(int userId);
    
    /// <summary>
    /// 使用 DbSetType 指定返回类型 - 演示复杂类型映射
    /// </summary>
    [Sqlx("SELECT u.[name] as UserName, d.[name] as DepartmentName, u.[salary] FROM [user] u INNER JOIN [department] d ON u.[department_id] = d.[id] WHERE u.[is_active] = 1")]
    [DbSetType(typeof(UserDepartmentView))]
    Task<IList<UserDepartmentView>> GetUserDepartmentViewAsync();
    
    /// <summary>
    /// 批量操作演示
    /// </summary>
    [Sqlx(@"INSERT INTO [user] ([name], [email], [age], [salary], [department_id], [is_active], [hire_date]) 
            VALUES (@name1, @email1, @age1, @salary1, @dept_id1, 1, @hire_date1),
                   (@name2, @email2, @age2, @salary2, @dept_id2, 1, @hire_date2),
                   (@name3, @email3, @age3, @salary3, @dept_id3, 1, @hire_date3)")]
    Task<int> CreateMultipleUsersAsync(
        string name1, string email1, int age1, decimal salary1, int deptId1, DateTime hireDate1,
        string name2, string email2, int age2, decimal salary2, int deptId2, DateTime hireDate2,
        string name3, string email3, int age3, decimal salary3, int deptId3, DateTime hireDate3);
}

/// <summary>
/// 高级功能服务实现
/// </summary>
public partial class AdvancedFeatureService : IAdvancedFeatureService
{
    private readonly DbConnection connection;

    public AdvancedFeatureService(DbConnection connection)
    {
        this.connection = connection;
    }

    [Sqlx("SELECT * FROM [user] WHERE {whereCondition} ORDER BY [name]")]
    public partial Task<IList<User>> GetUsersByExpressionAsync([ExpressionToSql] Expression<Func<User, bool>> whereCondition);
    
    [Sqlx("SELECT * FROM [user] WHERE {whereCondition} AND [is_active] = 1 ORDER BY {orderBy}")]
    public partial Task<IList<User>> GetActiveUsersByExpressionAsync(
        [ExpressionToSql] Expression<Func<User, bool>> whereCondition,
        [ExpressionToSql] Expression<Func<User, object>> orderBy);
    
    [SqlExecuteType(SqlOperation.Insert, "user")]
    public partial Task<int> CreateUserAsync(string name, string email, int age, decimal salary, int departmentId);
    
    [SqlExecuteType(SqlOperation.Update, "user")]
    [Sqlx("UPDATE [user] SET [salary] = @salary, [performance_rating] = @rating WHERE [id] = @user_id")]
    public partial Task<int> UpdateUserSalaryAsync(int userId, decimal salary, decimal rating);
    
    [SqlExecuteType(SqlOperation.Delete, "user")]
    [Sqlx("DELETE FROM [user] WHERE [id] = @user_id AND [is_active] = 0")]
    public partial Task<int> DeleteInactiveUserAsync(int userId);
    
    [Sqlx("SELECT u.[name] as UserName, d.[name] as DepartmentName, u.[salary] FROM [user] u INNER JOIN [department] d ON u.[department_id] = d.[id] WHERE u.[is_active] = 1")]
    [DbSetType(typeof(UserDepartmentView))]
    public partial Task<IList<UserDepartmentView>> GetUserDepartmentViewAsync();
    
    [Sqlx(@"INSERT INTO [user] ([name], [email], [age], [salary], [department_id], [is_active], [hire_date]) 
            VALUES (@name1, @email1, @age1, @salary1, @dept_id1, 1, @hire_date1),
                   (@name2, @email2, @age2, @salary2, @dept_id2, 1, @hire_date2),
                   (@name3, @email3, @age3, @salary3, @dept_id3, 1, @hire_date3)")]
    public partial Task<int> CreateMultipleUsersAsync(
        string name1, string email1, int age1, decimal salary1, int deptId1, DateTime hireDate1,
        string name2, string email2, int age2, decimal salary2, int deptId2, DateTime hireDate2,
        string name3, string email3, int age3, decimal salary3, int deptId3, DateTime hireDate3);
}

/// <summary>
/// 自定义SQL方言演示 - 演示 SqlDefine 自定义参数
/// </summary>
[SqlDefine("[", "]", "'", "'", "@")]  // 自定义方言参数
public partial class CustomDialectService
{
    private readonly DbConnection connection;

    public CustomDialectService(DbConnection connection)
    {
        this.connection = connection;
    }

    /// <summary>
    /// 使用自定义方言的查询
    /// </summary>
    [Sqlx("SELECT * FROM [user] WHERE [department_id] = @dept_id AND [is_active] = 1")]
    public partial Task<IList<User>> GetUsersByDepartmentCustomAsync(int deptId);
}

/// <summary>
/// RepositoryFor 属性演示 - 演示仓储模式自动关联
/// </summary>
public partial class RepositoryDemoService : IRepositoryDemoService
{
    private readonly DbConnection connection;

    public RepositoryDemoService(DbConnection connection)
    {
        this.connection = connection;
    }

    [Sqlx("SELECT COUNT(*) FROM [user] WHERE [department_id] = @dept_id")]
    public partial Task<int> GetUserCountAsync(int deptId);
    
    [Sqlx("SELECT AVG([salary]) FROM [user] WHERE [department_id] = @dept_id AND [is_active] = 1")]
    public partial Task<decimal> GetAverageSalaryAsync(int deptId);
}

/// <summary>
/// 仓储服务接口
/// </summary>
public interface IRepositoryDemoService
{
    Task<int> GetUserCountAsync(int deptId);
    Task<decimal> GetAverageSalaryAsync(int deptId);
}

/// <summary>
/// 用户部门视图 - 演示 DbSetType 用法
/// </summary>
public class UserDepartmentView
{
    public string UserName { get; set; } = "";
    public string DepartmentName { get; set; } = "";
    public decimal Salary { get; set; }
}