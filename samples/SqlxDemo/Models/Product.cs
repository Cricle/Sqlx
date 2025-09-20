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
