using System;
using Sqlx.Annotations;

namespace SqlxDemo.Models;

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

