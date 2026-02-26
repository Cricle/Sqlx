// <copyright file="TestOrder.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests.E2E.Models;

/// <summary>
/// Test entity for complex query operations.
/// </summary>
public class TestOrder
{
    /// <summary>
    /// Gets or sets the order ID.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public long UserId { get; set; }

    /// <summary>
    /// Gets or sets the order amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Gets or sets the order status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the order date.
    /// </summary>
    public DateTime OrderDate { get; set; }
}
