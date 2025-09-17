using Sqlx.Annotations;

namespace SqlxDemo.Models;

/// <summary>
/// 产品实体 - 演示复杂实体映射
/// </summary>
[TableName("product")]
public class Product
{
    public int id { get; set; }
    public string name { get; set; } = "";
    public string description { get; set; } = "";
    public string sku { get; set; } = "";
    public decimal price { get; set; }
    public decimal? discount_price { get; set; }
    public int category_id { get; set; }
    public int stock_quantity { get; set; }
    public bool is_active { get; set; } = true;
    public DateTime created_at { get; set; }
    public DateTime? updated_at { get; set; }
    public string? image_url { get; set; }
    public double weight { get; set; }
    public string tags { get; set; } = "";
}

/// <summary>
/// 订单实体 - 演示枚举和状态管理
/// </summary>
[TableName("order")]
public class Order
{
    public int id { get; set; }
    public string order_number { get; set; } = "";
    public int user_id { get; set; }
    public decimal total_amount { get; set; }
    public decimal discount_amount { get; set; }
    public decimal shipping_cost { get; set; }
    public int status { get; set; }
    public DateTime created_at { get; set; }
    public DateTime? shipped_at { get; set; }
    public DateTime? delivered_at { get; set; }
    public string shipping_address { get; set; } = "";
    public string billing_address { get; set; } = "";
    public string? notes { get; set; }
}

