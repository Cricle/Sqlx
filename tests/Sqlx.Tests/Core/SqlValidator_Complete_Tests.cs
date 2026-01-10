// -----------------------------------------------------------------------
// <copyright file="SqlValidator_Complete_Tests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Sqlx.Annotations;
using Sqlx.Validation;
using Xunit;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Complete tests for SqlValidator to achieve 100% coverage
    /// </summary>
    public class SqlValidator_Complete_Tests
    {
        [Fact]
        public void IsValidIdentifier_WithValidIdentifier_ShouldReturnTrue()
        {
            Assert.True(SqlValidator.IsValidIdentifier("TableName"));
            Assert.True(SqlValidator.IsValidIdentifier("_table"));
            Assert.True(SqlValidator.IsValidIdentifier("table123"));
            Assert.True(SqlValidator.IsValidIdentifier("Table_Name_123"));
        }

        [Fact]
        public void IsValidIdentifier_WithEmptyString_ShouldReturnFalse()
        {
            Assert.False(SqlValidator.IsValidIdentifier(""));
        }

        [Fact]
        public void IsValidIdentifier_WithTooLongString_ShouldReturnFalse()
        {
            var longString = new string('a', 129);
            Assert.False(SqlValidator.IsValidIdentifier(longString));
        }

        [Fact]
        public void IsValidIdentifier_StartingWithDigit_ShouldReturnFalse()
        {
            Assert.False(SqlValidator.IsValidIdentifier("123table"));
        }

        [Fact]
        public void IsValidIdentifier_WithSpecialCharacters_ShouldReturnFalse()
        {
            Assert.False(SqlValidator.IsValidIdentifier("table-name"));
            Assert.False(SqlValidator.IsValidIdentifier("table.name"));
            Assert.False(SqlValidator.IsValidIdentifier("table name"));
            Assert.False(SqlValidator.IsValidIdentifier("table@name"));
        }

        [Fact]
        public void ContainsDangerousKeyword_WithDangerousKeywords_ShouldReturnTrue()
        {
            Assert.True(SqlValidator.ContainsDangerousKeyword("DROP TABLE users"));
            Assert.True(SqlValidator.ContainsDangerousKeyword("TRUNCATE users"));
            Assert.True(SqlValidator.ContainsDangerousKeyword("ALTER TABLE"));
            Assert.True(SqlValidator.ContainsDangerousKeyword("EXEC sp_"));
            Assert.True(SqlValidator.ContainsDangerousKeyword("SELECT * -- comment"));
            Assert.True(SqlValidator.ContainsDangerousKeyword("/* comment */"));
            Assert.True(SqlValidator.ContainsDangerousKeyword("SELECT *; DROP TABLE"));
        }

        [Fact]
        public void ContainsDangerousKeyword_WithSafeSQL_ShouldReturnFalse()
        {
            Assert.False(SqlValidator.ContainsDangerousKeyword("SELECT * FROM users"));
            Assert.False(SqlValidator.ContainsDangerousKeyword("WHERE id = 1"));
            Assert.False(SqlValidator.ContainsDangerousKeyword("ORDER BY name"));
        }

        [Fact]
        public void IsValidFragment_WithValidFragment_ShouldReturnTrue()
        {
            Assert.True(SqlValidator.IsValidFragment("id = 1"));
            Assert.True(SqlValidator.IsValidFragment("name LIKE '%test%'"));
            Assert.True(SqlValidator.IsValidFragment("age > 18 AND status = 'active'"));
        }

        [Fact]
        public void IsValidFragment_WithEmptyString_ShouldReturnFalse()
        {
            Assert.False(SqlValidator.IsValidFragment(""));
        }

        [Fact]
        public void IsValidFragment_WithTooLongString_ShouldReturnFalse()
        {
            var longString = new string('a', 4097);
            Assert.False(SqlValidator.IsValidFragment(longString));
        }

        [Fact]
        public void IsValidFragment_WithDangerousKeywords_ShouldReturnFalse()
        {
            Assert.False(SqlValidator.IsValidFragment("id = 1; DROP TABLE users"));
            Assert.False(SqlValidator.IsValidFragment("id = 1 -- comment"));
        }

        [Fact]
        public void IsValidTablePart_WithValidTablePart_ShouldReturnTrue()
        {
            Assert.True(SqlValidator.IsValidTablePart("users"));
            Assert.True(SqlValidator.IsValidTablePart("2024"));
            Assert.True(SqlValidator.IsValidTablePart("table123"));
        }

        [Fact]
        public void IsValidTablePart_WithEmptyString_ShouldReturnFalse()
        {
            Assert.False(SqlValidator.IsValidTablePart(""));
        }

        [Fact]
        public void IsValidTablePart_WithTooLongString_ShouldReturnFalse()
        {
            var longString = new string('a', 65);
            Assert.False(SqlValidator.IsValidTablePart(longString));
        }

        [Fact]
        public void IsValidTablePart_WithSpecialCharacters_ShouldReturnFalse()
        {
            Assert.False(SqlValidator.IsValidTablePart("table_name"));
            Assert.False(SqlValidator.IsValidTablePart("table-name"));
            Assert.False(SqlValidator.IsValidTablePart("table.name"));
            Assert.False(SqlValidator.IsValidTablePart("table name"));
        }

        [Fact]
        public void Validate_WithIdentifierType_ShouldValidateAsIdentifier()
        {
            Assert.True(SqlValidator.Validate("TableName", DynamicSqlType.Identifier));
            Assert.False(SqlValidator.Validate("123table", DynamicSqlType.Identifier));
        }

        [Fact]
        public void Validate_WithFragmentType_ShouldValidateAsFragment()
        {
            Assert.True(SqlValidator.Validate("id = 1", DynamicSqlType.Fragment));
            Assert.False(SqlValidator.Validate("DROP TABLE", DynamicSqlType.Fragment));
        }

        [Fact]
        public void Validate_WithTablePartType_ShouldValidateAsTablePart()
        {
            Assert.True(SqlValidator.Validate("users", DynamicSqlType.TablePart));
            Assert.False(SqlValidator.Validate("table_name", DynamicSqlType.TablePart));
        }

        [Fact]
        public void Validate_WithInvalidType_ShouldReturnFalse()
        {
            Assert.False(SqlValidator.Validate("anything", (DynamicSqlType)999));
        }
    }
}
