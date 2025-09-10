// -----------------------------------------------------------------------
// <copyright file="User.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Sqlx.Annotations;

namespace ComprehensiveExample.Models;

/// <summary>
/// 用户实体类，演示基本的数据模型
/// </summary>
[TableName("users")]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public int? DepartmentId { get; set; }
}

/// <summary>
/// 部门实体类，演示关联关系
/// </summary>
[TableName("departments")]
public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// 用户统计信息，演示复合查询结果
/// </summary>
public class UserStats
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
}
