using Microsoft.Data.Sqlite;
using SqlxDemo.Models;
using Sqlx.Annotations;

namespace SqlxDemo.Services;

/// <summary>
/// 部门服务接口 - 用于演示源生成功能
/// </summary>
public interface IDepartmentService
{
    /// <summary>
    /// 根据预算获取部门
    /// </summary>
    [Sqlx("SELECT * FROM [department] WHERE [budget] > @min_budget")]
    Task<IList<Department>> GetDepartmentsByBudgetAsync(decimal min_budget);

    /// <summary>
    /// 根据ID获取部门
    /// </summary>
    [Sqlx("SELECT * FROM [department] WHERE [id] = @dept_id")]
    Task<Department?> GetDepartmentByIdAsync(int dept_id);

    /// <summary>
    /// 获取所有部门
    /// </summary>
    [Sqlx("SELECT * FROM [department]")]
    Task<IList<Department>> GetAllDepartmentsAsync();

    /// <summary>
    /// 更新部门预算 - 演示 SqlExecuteType
    /// </summary>
    [SqlExecuteType(SqlOperation.Update, "department")]
    [Sqlx("UPDATE [department] SET [budget] = @budget WHERE [id] = @dept_id")]
    Task<int> UpdateDepartmentBudgetAsync(int dept_id, decimal budget);

    /// <summary>
    /// 创建新部门 - 演示 SqlExecuteType Insert
    /// </summary>
    [SqlExecuteType(SqlOperation.Insert, "department")]
    [Sqlx("INSERT INTO [department] ([name], [budget], [manager_id]) VALUES (@name, @budget, @manager_id); SELECT last_insert_rowid();")]
    Task<int> CreateDepartmentAsync(string name, decimal budget, int? manager_id);
}

