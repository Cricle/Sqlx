using System;

namespace FullFeatureDemo;

// 1. 基础用户模型
public class User
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public int Age { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
}

// 2. 带软删除的产品模型
public class Product
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    
    // 软删除标记
    public bool IsDeleted { get; set; }
}

// 3. 带审计字段的订单模型
public class Order
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "";
    
    // 审计字段
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

// 4. 带乐观锁的账户模型
public class Account
{
    public long Id { get; set; }
    public string AccountNo { get; set; } = "";
    public decimal Balance { get; set; }
    
    // 乐观锁版本号
    public long Version { get; set; }
}

// 5. 日志模型（批量操作）
public class Log
{
    public long Id { get; set; }
    public string Level { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime Timestamp { get; set; }
}

// 6. 用户统计（复杂查询）
public class UserStats
{
    public long UserId { get; set; }
    public string UserName { get; set; } = "";
    public int OrderCount { get; set; }
    public decimal TotalSpent { get; set; }
}

// 7. 产品详情（JOIN查询）
public class ProductDetail
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public decimal Price { get; set; }
    public string CategoryName { get; set; } = "";
}

