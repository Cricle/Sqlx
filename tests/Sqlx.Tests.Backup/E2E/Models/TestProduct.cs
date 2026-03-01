// <copyright file="TestProduct.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

namespace Sqlx.Tests.E2E.Models;

/// <summary>
/// Test entity for JOIN operations.
/// </summary>
public class TestProduct
{
    /// <summary>
    /// Gets or sets the product ID.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product price.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the stock quantity.
    /// </summary>
    public int Stock { get; set; }

    /// <summary>
    /// Gets or sets the product category.
    /// </summary>
    public string Category { get; set; } = string.Empty;
}
