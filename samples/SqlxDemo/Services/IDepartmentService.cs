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
    [Sqlx("SELECT * FROM [Department] WHERE [Budget] > @minBudget")]
    Task<IList<Department>> GetDepartmentsByBudgetAsync(decimal minBudget);

    /// <summary>
    /// 根据ID获取部门
    /// </summary>
    [Sqlx("SELECT * FROM [Department] WHERE [Id] = @deptId")]
    Task<Department?> GetDepartmentByIdAsync(int deptId);

    /// <summary>
    /// 获取所有部门
    /// </summary>
    [Sqlx("SELECT * FROM [Department]")]
    Task<IList<Department>> GetAllDepartmentsAsync();
}

