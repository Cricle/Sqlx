// -----------------------------------------------------------------------
// <copyright file="User.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.ComponentModel.DataAnnotations.Schema;
using Sqlx.Annotations;

namespace ComprehensiveExample.Models;

/// <summary>
/// 传统类：用户实体类，演示基本的数据模型
/// </summary>
[TableName("users")]
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;
    public int? DepartmentId { get; set; }
}

/// <summary>
/// 传统类：部门实体类，演示关联关系
/// </summary>
[TableName("departments")]
public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Record 类型 (C# 9+)：产品实体，演示现代语法支持
/// </summary>
[TableName("products")]
public record Product(int Id, string Name, decimal Price, int CategoryId)
{
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// 传统类演示：订单实体
/// </summary>
[TableName("orders")]
public class Order
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; } = DateTime.Now;
    public decimal TotalAmount { get; set; }
}

/// <summary>
/// Record 演示：订单项
/// </summary>
[TableName("order_items")]
public record OrderItem(int OrderId, int ProductId, int Quantity, decimal UnitPrice)
{
    public decimal TotalPrice => Quantity * UnitPrice;
}