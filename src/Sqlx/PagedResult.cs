// -----------------------------------------------------------------------
// <copyright file="PagedResult.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Sqlx
{
    /// <summary>
    /// Represents a paged query result with items and pagination metadata.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// Gets or sets the items in the current page.
        /// </summary>
        public List<T> Items { get; set; } = new List<T>(0);

        /// <summary>
        /// Gets or sets the current page number (1-based).
        /// </summary>
        public int PageNumber { get; set; }

        /// <summary>
        /// Gets or sets the number of items per page.
        /// </summary>
        public int PageSize { get; set; }

        /// <summary>
        /// Gets or sets the total number of items across all pages.
        /// </summary>
        public long TotalCount { get; set; }

        /// <summary>
        /// Gets the total number of pages.
        /// </summary>
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

        /// <summary>
        /// Gets a value indicating whether there is a previous page.
        /// </summary>
        public bool HasPrevious => PageNumber > 1;

        /// <summary>
        /// Gets a value indicating whether there is a next page.
        /// </summary>
        public bool HasNext => PageNumber < TotalPages;

        /// <summary>
        /// Gets the index of the first item on the current page (1-based).
        /// </summary>
        public int FirstItemIndex => TotalCount == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;

        /// <summary>
        /// Gets the index of the last item on the current page (1-based).
        /// </summary>
        public int LastItemIndex => Math.Min(PageNumber * PageSize, (int)TotalCount);
    }
}

