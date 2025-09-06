// -----------------------------------------------------------------------
// <copyright file="UserModelsTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.RepositoryExample.Tests;

using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for user models and related classes.
/// </summary>
public class UserModelsTests
{
    [Fact]
    public void User_Properties_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var user = new User();
        var id = 123;
        var name = "Test User";
        var email = "test@example.com";
        var createdAt = DateTime.Now;

        // Act
        user.Id = id;
        user.Name = name;
        user.Email = email;
        user.CreatedAt = createdAt;

        // Assert
        user.Id.Should().Be(id);
        user.Name.Should().Be(name);
        user.Email.Should().Be(email);
        user.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void User_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.Id.Should().Be(0);
        user.Name.Should().Be(string.Empty);
        user.Email.Should().Be(string.Empty);
        user.CreatedAt.Should().Be(default(DateTime));
    }

    [Theory]
    [InlineData(UserSearchType.Name)]
    [InlineData(UserSearchType.Email)]
    [InlineData(UserSearchType.NameAndEmail)]
    [InlineData(UserSearchType.FullText)]
    public void UserSearchType_AllValues_ShouldBeDefined(UserSearchType searchType)
    {
        // Arrange & Act & Assert
        Enum.IsDefined(typeof(UserSearchType), searchType).Should().BeTrue();
    }

    [Fact]
    public void UserStatistics_Properties_ShouldSetAndGetCorrectly()
    {
        // Arrange
        var stats = new UserStatistics();
        var totalUsers = 100;
        var usersCreatedToday = 5;
        var usersCreatedThisWeek = 20;
        var usersCreatedThisMonth = 80;
        var mostCommonEmailDomain = "example.com";
        var averageUsersPerDay = 3.5;
        var firstUserCreated = DateTime.Now.AddDays(-30);
        var lastUserCreated = DateTime.Now;

        // Act
        stats.TotalUsers = totalUsers;
        stats.UsersCreatedToday = usersCreatedToday;
        stats.UsersCreatedThisWeek = usersCreatedThisWeek;
        stats.UsersCreatedThisMonth = usersCreatedThisMonth;
        stats.MostCommonEmailDomain = mostCommonEmailDomain;
        stats.AverageUsersPerDay = averageUsersPerDay;
        stats.FirstUserCreated = firstUserCreated;
        stats.LastUserCreated = lastUserCreated;

        // Assert
        stats.TotalUsers.Should().Be(totalUsers);
        stats.UsersCreatedToday.Should().Be(usersCreatedToday);
        stats.UsersCreatedThisWeek.Should().Be(usersCreatedThisWeek);
        stats.UsersCreatedThisMonth.Should().Be(usersCreatedThisMonth);
        stats.MostCommonEmailDomain.Should().Be(mostCommonEmailDomain);
        stats.AverageUsersPerDay.Should().Be(averageUsersPerDay);
        stats.FirstUserCreated.Should().Be(firstUserCreated);
        stats.LastUserCreated.Should().Be(lastUserCreated);
    }

    [Fact]
    public void UserStatistics_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var stats = new UserStatistics();

        // Assert
        stats.TotalUsers.Should().Be(0);
        stats.UsersCreatedToday.Should().Be(0);
        stats.UsersCreatedThisWeek.Should().Be(0);
        stats.UsersCreatedThisMonth.Should().Be(0);
        stats.MostCommonEmailDomain.Should().Be(string.Empty);
        stats.AverageUsersPerDay.Should().Be(0);
        stats.FirstUserCreated.Should().BeNull();
        stats.LastUserCreated.Should().BeNull();
    }

    [Fact]
    public void UserWithMetadata_InheritsFromUser_AndHasAdditionalProperties()
    {
        // Arrange
        var userWithMetadata = new UserWithMetadata();
        var lastLoginAt = DateTime.Now.AddHours(-2);
        var isActive = true;
        var role = "Admin";
        var loginAttempts = 3;
        var metadata = "{\"theme\": \"dark\"}";

        // Act
        userWithMetadata.Id = 1;
        userWithMetadata.Name = "Test User";
        userWithMetadata.Email = "test@example.com";
        userWithMetadata.CreatedAt = DateTime.Now;
        userWithMetadata.LastLoginAt = lastLoginAt;
        userWithMetadata.IsActive = isActive;
        userWithMetadata.Role = role;
        userWithMetadata.LoginAttempts = loginAttempts;
        userWithMetadata.Metadata = metadata;

        // Assert
        userWithMetadata.Should().BeOfType<UserWithMetadata>();
        userWithMetadata.Should().BeAssignableTo<User>();
        userWithMetadata.LastLoginAt.Should().Be(lastLoginAt);
        userWithMetadata.IsActive.Should().Be(isActive);
        userWithMetadata.Role.Should().Be(role);
        userWithMetadata.LoginAttempts.Should().Be(loginAttempts);
        userWithMetadata.Metadata.Should().Be(metadata);
    }

    [Fact]
    public void UserWithMetadata_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var userWithMetadata = new UserWithMetadata();

        // Assert
        userWithMetadata.LastLoginAt.Should().BeNull();
        userWithMetadata.IsActive.Should().BeTrue();
        userWithMetadata.Role.Should().Be("User");
        userWithMetadata.LoginAttempts.Should().Be(0);
        userWithMetadata.Metadata.Should().Be("{}");
    }

    [Fact]
    public void PaginationInfo_Properties_ShouldCalculateCorrectly()
    {
        // Arrange
        var pageNumber = 2;
        var pageSize = 10;
        var totalItems = 25;

        // Act
        var paginationInfo = new PaginationInfo
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalItems = totalItems
        };

        // Assert
        paginationInfo.PageNumber.Should().Be(pageNumber);
        paginationInfo.PageSize.Should().Be(pageSize);
        paginationInfo.TotalItems.Should().Be(totalItems);
        paginationInfo.TotalPages.Should().Be(3); // Math.Ceiling(25 / 10.0) = 3
        paginationInfo.HasPreviousPage.Should().BeTrue(); // Page 2 > 1
        paginationInfo.HasNextPage.Should().BeTrue(); // Page 2 < 3
        paginationInfo.StartIndex.Should().Be(10); // (2 - 1) * 10 = 10
        paginationInfo.EndIndex.Should().Be(19); // Min(10 + 10 - 1, 25 - 1) = 19
    }

    [Fact]
    public void PaginationInfo_FirstPage_ShouldCalculateCorrectly()
    {
        // Arrange & Act
        var paginationInfo = new PaginationInfo
        {
            PageNumber = 1,
            PageSize = 5,
            TotalItems = 12
        };

        // Assert
        paginationInfo.TotalPages.Should().Be(3); // Math.Ceiling(12 / 5.0) = 3
        paginationInfo.HasPreviousPage.Should().BeFalse(); // Page 1
        paginationInfo.HasNextPage.Should().BeTrue(); // Page 1 < 3
        paginationInfo.StartIndex.Should().Be(0); // (1 - 1) * 5 = 0
        paginationInfo.EndIndex.Should().Be(4); // Min(0 + 5 - 1, 12 - 1) = 4
    }

    [Fact]
    public void PaginationInfo_LastPage_ShouldCalculateCorrectly()
    {
        // Arrange & Act
        var paginationInfo = new PaginationInfo
        {
            PageNumber = 3,
            PageSize = 5,
            TotalItems = 12
        };

        // Assert
        paginationInfo.TotalPages.Should().Be(3); // Math.Ceiling(12 / 5.0) = 3
        paginationInfo.HasPreviousPage.Should().BeTrue(); // Page 3 > 1
        paginationInfo.HasNextPage.Should().BeFalse(); // Page 3 = 3
        paginationInfo.StartIndex.Should().Be(10); // (3 - 1) * 5 = 10
        paginationInfo.EndIndex.Should().Be(11); // Min(10 + 5 - 1, 12 - 1) = 11
    }

    [Fact]
    public void PagedResult_Constructor_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var items = new List<User> { new User { Id = 1, Name = "User 1" } };
        var paginationInfo = new PaginationInfo { PageNumber = 1, PageSize = 10, TotalItems = 1 };

        // Act
        var pagedResult = new PagedResult<User>(items, paginationInfo);

        // Assert
        pagedResult.Items.Should().BeSameAs(items);
        pagedResult.PaginationInfo.Should().BeSameAs(paginationInfo);
    }

    [Fact]
    public void PagedResult_Constructor_WithNullItems_ShouldThrowArgumentNullException()
    {
        // Arrange
        var paginationInfo = new PaginationInfo();

        // Act & Assert
        var act = () => new PagedResult<User>(null!, paginationInfo);
        act.Should().Throw<ArgumentNullException>().WithParameterName("items");
    }

    [Fact]
    public void PagedResult_Constructor_WithNullPaginationInfo_ShouldThrowArgumentNullException()
    {
        // Arrange
        var items = new List<User>();

        // Act & Assert
        var act = () => new PagedResult<User>(items, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("paginationInfo");
    }

    [Fact]
    public void UserValidationResult_DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var validationResult = new UserValidationResult();

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().NotBeNull().And.BeEmpty();
        validationResult.Warnings.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void UserValidationResult_AddError_ShouldAddErrorAndSetInvalid()
    {
        // Arrange
        var validationResult = new UserValidationResult { IsValid = true };
        var error = "Test error";

        // Act
        validationResult.AddError(error);

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(error);
    }

    [Fact]
    public void UserValidationResult_AddError_WithNullOrWhitespace_ShouldNotAddError()
    {
        // Arrange
        var validationResult = new UserValidationResult { IsValid = true };

        // Act
        validationResult.AddError(null!);
        validationResult.AddError("");
        validationResult.AddError("   ");

        // Assert
        validationResult.IsValid.Should().BeTrue(); // Should remain true
        validationResult.Errors.Should().BeEmpty();
    }

    [Fact]
    public void UserValidationResult_AddWarning_ShouldAddWarning()
    {
        // Arrange
        var validationResult = new UserValidationResult();
        var warning = "Test warning";

        // Act
        validationResult.AddWarning(warning);

        // Assert
        validationResult.Warnings.Should().Contain(warning);
    }

    [Fact]
    public void UserValidationResult_AddWarning_WithNullOrWhitespace_ShouldNotAddWarning()
    {
        // Arrange
        var validationResult = new UserValidationResult();

        // Act
        validationResult.AddWarning(null!);
        validationResult.AddWarning("");
        validationResult.AddWarning("   ");

        // Assert
        validationResult.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void UserValidationResult_MultipleErrorsAndWarnings_ShouldHandleCorrectly()
    {
        // Arrange
        var validationResult = new UserValidationResult { IsValid = true };

        // Act
        validationResult.AddError("Error 1");
        validationResult.AddError("Error 2");
        validationResult.AddWarning("Warning 1");
        validationResult.AddWarning("Warning 2");

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().HaveCount(2);
        validationResult.Errors.Should().Contain("Error 1");
        validationResult.Errors.Should().Contain("Error 2");
        validationResult.Warnings.Should().HaveCount(2);
        validationResult.Warnings.Should().Contain("Warning 1");
        validationResult.Warnings.Should().Contain("Warning 2");
    }
}
