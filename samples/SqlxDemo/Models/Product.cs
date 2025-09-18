using Sqlx.Annotations;

namespace SqlxDemo.Models;

/// <summary>
/// 产品实体 - 演示复杂实体映射
/// </summary>
[TableName("product")]
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    /// <summary>
    /// Lowercase alias for Name (for compatibility)
    /// </summary>
    public string Name1 { get => Name; set => Name = value; }

    public string Description { get; set; } = "";
    public string Sku { get; set; } = "";
    public decimal Price { get; set; }
    public decimal? Discount_price { get; set; }
    public int Category_id { get; set; }
    public int Stock_quantity { get; set; }
    public bool Is_active { get; set; } = true;
    public DateTime Created_at { get; set; }
    public DateTime? Updated_at { get; set; }
    public string? Image_url { get; set; }
    public double Weight { get; set; }
    public string Tags { get; set; } = "";
}

/// <summary>
/// 订单实体 - 演示枚举和状态管理
/// </summary>
[TableName("order")]
public class Order
{
    public int Id { get; set; }
    public string Order_number { get; set; } = "";
    public int User_id { get; set; }
    public decimal Total_amount { get; set; }
    public decimal Discount_amount { get; set; }
    public decimal Shipping_cost { get; set; }
    public int Status { get; set; }
    public DateTime Created_at { get; set; }
    public DateTime? Shipped_at { get; set; }
    public DateTime? Delivered_at { get; set; }
    public string Shipping_address { get; set; } = "";
    public string Billing_address { get; set; } = "";
    public string? Notes { get; set; }
}

