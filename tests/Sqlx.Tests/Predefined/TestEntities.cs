// -----------------------------------------------------------------------
// <copyright file="TestEntities.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Sqlx.Annotations;

namespace Sqlx.Tests.Predefined
{
    // Test with record type (user requirement: support record)
    [TableName("users")]
    public record User
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    // Test with regular class
    [TableName("products")]
    public class Product
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Test struct value type (user requirement: support struct return values)
    public struct UserStats
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public double AverageAge { get; set; }
    }
}

