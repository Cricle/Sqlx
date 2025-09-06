// -----------------------------------------------------------------------
// <copyright file="UserModels.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.RepositoryExample;

using System;
using System.Collections.Generic;

/// <summary>
/// User search types for advanced querying.
/// </summary>
public enum UserSearchType
{
    /// <summary>
    /// Search by name only.
    /// </summary>
    Name,
    
    /// <summary>
    /// Search by email only.
    /// </summary>
    Email,
    
    /// <summary>
    /// Search by both name and email.
    /// </summary>
    NameAndEmail,
    
    /// <summary>
    /// Full-text search across all fields.
    /// </summary>
    FullText
}

/// <summary>
/// User statistics data transfer object.
/// </summary>
public class UserStatistics
{
    /// <summary>
    /// Gets or sets the total number of users.
    /// </summary>
    public int TotalUsers { get; set; }
    
    /// <summary>
    /// Gets or sets the number of users created today.
    /// </summary>
    public int UsersCreatedToday { get; set; }
    
    /// <summary>
    /// Gets or sets the number of users created this week.
    /// </summary>
    public int UsersCreatedThisWeek { get; set; }
    
    /// <summary>
    /// Gets or sets the number of users created this month.
    /// </summary>
    public int UsersCreatedThisMonth { get; set; }
    
    /// <summary>
    /// Gets or sets the most common email domain.
    /// </summary>
    public string MostCommonEmailDomain { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the average users created per day.
    /// </summary>
    public double AverageUsersPerDay { get; set; }
    
    /// <summary>
    /// Gets or sets the date of the first user creation.
    /// </summary>
    public DateTime? FirstUserCreated { get; set; }
    
    /// <summary>
    /// Gets or sets the date of the last user creation.
    /// </summary>
    public DateTime? LastUserCreated { get; set; }
}

/// <summary>
/// Extended user model with additional metadata.
/// </summary>
public class UserWithMetadata : User
{
    /// <summary>
    /// Gets or sets the user's last login date.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }
    
    /// <summary>
    /// Gets or sets whether the user is active.
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the user's role.
    /// </summary>
    public string Role { get; set; } = "User";
    
    /// <summary>
    /// Gets or sets the number of login attempts.
    /// </summary>
    public int LoginAttempts { get; set; }
    
    /// <summary>
    /// Gets or sets additional user metadata as JSON.
    /// </summary>
    public string Metadata { get; set; } = "{}";
}

/// <summary>
/// Pagination information for query results.
/// </summary>
public class PaginationInfo
{
    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    public int PageNumber { get; set; }
    
    /// <summary>
    /// Gets or sets the page size.
    /// </summary>
    public int PageSize { get; set; }
    
    /// <summary>
    /// Gets or sets the total number of items.
    /// </summary>
    public int TotalItems { get; set; }
    
    /// <summary>
    /// Gets the total number of pages.
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    
    /// <summary>
    /// Gets a value indicating whether there is a previous page.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;
    
    /// <summary>
    /// Gets a value indicating whether there is a next page.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
    
    /// <summary>
    /// Gets the starting item index for the current page (0-based).
    /// </summary>
    public int StartIndex => (PageNumber - 1) * PageSize;
    
    /// <summary>
    /// Gets the ending item index for the current page (0-based).
    /// </summary>
    public int EndIndex => Math.Min(StartIndex + PageSize - 1, TotalItems - 1);
}

/// <summary>
/// Paginated result wrapper for query results.
/// </summary>
/// <typeparam name="T">The type of items in the result.</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PagedResult{T}"/> class.
    /// </summary>
    /// <param name="items">The items in the current page.</param>
    /// <param name="paginationInfo">The pagination information.</param>
    public PagedResult(IList<T> items, PaginationInfo paginationInfo)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
        PaginationInfo = paginationInfo ?? throw new ArgumentNullException(nameof(paginationInfo));
    }
    
    /// <summary>
    /// Gets the items in the current page.
    /// </summary>
    public IList<T> Items { get; }
    
    /// <summary>
    /// Gets the pagination information.
    /// </summary>
    public PaginationInfo PaginationInfo { get; }
}

/// <summary>
/// User validation result.
/// </summary>
public class UserValidationResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the validation passed.
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// Gets or sets the validation errors.
    /// </summary>
    public List<string> Errors { get; set; } = new List<string>();
    
    /// <summary>
    /// Gets or sets the validation warnings.
    /// </summary>
    public List<string> Warnings { get; set; } = new List<string>();
    
    /// <summary>
    /// Adds an error to the validation result.
    /// </summary>
    /// <param name="error">The error message.</param>
    public void AddError(string error)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            Errors.Add(error);
            IsValid = false;
        }
    }
    
    /// <summary>
    /// Adds a warning to the validation result.
    /// </summary>
    /// <param name="warning">The warning message.</param>
    public void AddWarning(string warning)
    {
        if (!string.IsNullOrWhiteSpace(warning))
        {
            Warnings.Add(warning);
        }
    }
}
