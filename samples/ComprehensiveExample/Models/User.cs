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
    public int Id { get; set; }
    public decimal TotalPrice => Quantity * UnitPrice;
}

/// <summary>
/// Primary Constructor 演示：客户信息 (C# 12+)
/// </summary>
[TableName("customers")]
public class Customer(int id, string name, string email, DateTime birthDate)
{
    public int Id { get; } = id;
    public string Name { get; } = name;
    public string Email { get; } = email;
    public DateTime BirthDate { get; } = birthDate;
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;
    public decimal TotalSpent { get; set; } = 0m;
    public int? ReferredBy { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime LastLoginAt { get; set; } = DateTime.MinValue;
    public bool IsVip { get; set; } = false;
}

/// <summary>
/// Primary Constructor + Record 组合演示：审计日志 (C# 12+)
/// </summary>
[TableName("audit_logs")]
public record AuditLog(string Action, string EntityType, string EntityId, string UserId)
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string OldValues { get; set; } = string.Empty;
    public string NewValues { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public string UserAgent { get; set; } = string.Empty;
}

/// <summary>
/// 传统类：产品分类
/// </summary>
[TableName("categories")]
public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Record 演示：商品库存
/// </summary>
[TableName("inventory")]
public record InventoryItem(int ProductId, int Quantity, decimal ReorderLevel)
{
    public int Id { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.Now;
    public string WarehouseLocation { get; set; } = string.Empty;
    public decimal ReservedQuantity { get; set; } = 0;
    public decimal AvailableQuantity => Quantity - ReservedQuantity;
}

/// <summary>
/// 枚举：客户状态
/// </summary>
public enum CustomerStatus
{
    Active = 1,
    Inactive = 2,
    Suspended = 3,
    Deleted = 4
}
