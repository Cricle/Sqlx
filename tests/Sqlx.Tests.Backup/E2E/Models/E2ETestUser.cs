// <copyright file="E2ETestUser.cs" company="Sqlx">
// Copyright (c) Sqlx. All rights reserved.
// </copyright>

using Sqlx.Annotations;

namespace Sqlx.Tests.E2E.Models;

/// <summary>
/// Test entity for basic CRUD operations in E2E tests.
/// </summary>
[Sqlx]
public class E2ETestUser
{
    /// <summary>
    /// Gets or sets the user ID.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the user name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user age.
    /// </summary>
    public int Age { get; set; }

    /// <summary>
    /// Gets or sets the user email.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the user is active.
    /// </summary>
    public bool IsActive { get; set; }
}
