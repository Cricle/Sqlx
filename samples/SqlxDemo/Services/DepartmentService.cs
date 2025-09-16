using Microsoft.Data.Sqlite;
using SqlxDemo.Models;
using Sqlx.Annotations;

namespace SqlxDemo.Services;

/// <summary>
/// 部门服务实现 - 使用Sqlx特性和partial方法进行源生成
/// </summary>
public partial class DepartmentService(SqliteConnection connection) : IDepartmentService
{
    // 实现接口方法，使用partial方法和Sqlx特性
    [Sqlx("SELECT * FROM [Department] WHERE [Budget] > @minBudget")]
    public partial Task<IList<Department>> GetDepartmentsByBudgetAsync(decimal minBudget);
    
    [Sqlx("SELECT * FROM [Department] WHERE [Id] = @deptId")]
    public partial Task<Department?> GetDepartmentByIdAsync(int deptId);
    
    [Sqlx("SELECT * FROM [Department]")]
    public partial Task<IList<Department>> GetAllDepartmentsAsync();
}
