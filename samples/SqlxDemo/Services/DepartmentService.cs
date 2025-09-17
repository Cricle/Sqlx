using Microsoft.Data.Sqlite;
using SqlxDemo.Models;
using Sqlx.Annotations;

namespace SqlxDemo.Services;

/// <summary>
/// 部门服务实现 - 使用Sqlx特性和partial方法进行源生成
/// </summary>
[SqlDefine(SqlDefineTypes.SQLite)]
public partial class DepartmentService(SqliteConnection connection) : IDepartmentService
{
    // 实现接口方法，使用partial方法和Sqlx特性
    [Sqlx("SELECT * FROM [department] WHERE [budget] > @min_budget")]
    public partial Task<IList<Department>> GetDepartmentsByBudgetAsync(decimal min_budget);
    
    [Sqlx("SELECT * FROM [department] WHERE [id] = @dept_id")]
    public partial Task<Department?> GetDepartmentByIdAsync(int dept_id);
    
    [Sqlx("SELECT * FROM [department]")]
    public partial Task<IList<Department>> GetAllDepartmentsAsync();
    
    [SqlExecuteType(SqlOperation.Update, "department")]
    [Sqlx("UPDATE [department] SET [budget] = @budget WHERE [id] = @dept_id")]
    public partial Task<int> UpdateDepartmentBudgetAsync(int dept_id, decimal budget);
    
    [SqlExecuteType(SqlOperation.Insert, "department")]
    [Sqlx("INSERT INTO [department] ([name], [budget], [manager_id]) VALUES (@name, @budget, @manager_id); SELECT last_insert_rowid();")]
    public partial Task<int> CreateDepartmentAsync(string name, decimal budget, int? manager_id);
}
