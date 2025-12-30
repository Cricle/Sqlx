using System;
using System.ComponentModel.DataAnnotations;
using Sqlx.Annotations;

namespace FullDemo.Models;

// ==================== 1. 基础用户模型 ====================
[TableName("users")]
public class User
{
    [Key]
    public long Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = "";

    [Required]
    [StringLength(200)]
    public string Email { get; set; } = "";

    public int Age { get; set; }

    public decimal Balance { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;
}

// ==================== 2. 带软删除的产品模型 ====================
[TableName("products")]
[SoftDelete(FlagColumn = "is_deleted")]
public class Product
{
    [Key]
    public long Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = "";

    [Required]
    [StringLength(50)]
    public string Category { get; set; } = "";

    public decimal Price { get; set; }

    public int Stock { get; set; }

    // 软删除标记（自动管理）
    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// ==================== 3. 带审计字段的订单模型 ====================
[TableName("orders")]
[AuditFields(
    CreatedAtColumn = "created_at",
    CreatedByColumn = "created_by",
    UpdatedAtColumn = "updated_at",
    UpdatedByColumn = "updated_by")]
public class Order
{
    [Key]
    public long Id { get; set; }

    public long UserId { get; set; }

    public decimal TotalAmount { get; set; }

    [StringLength(50)]
    public string Status { get; set; } = "Pending";

    // 审计字段（自动管理）
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

// ==================== 4. 带乐观锁的账户模型 ====================
[TableName("accounts")]
public class Account
{
    [Key]
    public long Id { get; set; }

    [Required]
    [StringLength(50)]
    public string AccountNo { get; set; } = "";

    public decimal Balance { get; set; }

    // 乐观锁版本号
    [Sqlx.Annotations.ConcurrencyCheck]
    public long Version { get; set; }
}

// ==================== 5. 日志模型（批量操作） ====================
[TableName("logs")]
public class Log
{
    [Key]
    public long Id { get; set; }

    [StringLength(20)]
    public string Level { get; set; } = "INFO";

    public string Message { get; set; } = "";

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

// ==================== 6. 分类模型 ====================
[TableName("categories")]
public class Category
{
    [Key]
    public long Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = "";

    [StringLength(10)]
    public string Code { get; set; } = "";

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

// ==================== DTO 模型 ====================

/// <summary>用户统计（复杂查询）</summary>
public class UserStats
{
    public long UserId { get; set; }
    public string UserName { get; set; } = "";
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
}

/// <summary>产品详情（JOIN查询）</summary>
public class ProductDetail
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal Price { get; set; }
    public string CategoryName { get; set; } = "";
}
