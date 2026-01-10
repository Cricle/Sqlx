// -----------------------------------------------------------------------
// <copyright file="RemainingAnnotations_Tests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using Sqlx.Annotations;
using Xunit;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Tests for remaining annotation attributes to achieve 100% coverage
    /// </summary>
    public class RemainingAnnotations_Tests
    {
        [Fact]
        public void DynamicSqlAttribute_DefaultType_ShouldBeIdentifier()
        {
            var attr = new DynamicSqlAttribute();
            Assert.Equal(DynamicSqlType.Identifier, attr.Type);
        }

        [Fact]
        public void DynamicSqlAttribute_SetType_ShouldUpdate()
        {
            var attr = new DynamicSqlAttribute { Type = DynamicSqlType.Fragment };
            Assert.Equal(DynamicSqlType.Fragment, attr.Type);
        }

        [Fact]
        public void DynamicSqlAttribute_SetTypeToTablePart_ShouldUpdate()
        {
            var attr = new DynamicSqlAttribute { Type = DynamicSqlType.TablePart };
            Assert.Equal(DynamicSqlType.TablePart, attr.Type);
        }

        [Fact]
        public void DynamicSqlType_IdentifierValue_ShouldBeZero()
        {
            Assert.Equal(0, (int)DynamicSqlType.Identifier);
        }

        [Fact]
        public void DynamicSqlType_FragmentValue_ShouldBeOne()
        {
            Assert.Equal(1, (int)DynamicSqlType.Fragment);
        }

        [Fact]
        public void DynamicSqlType_TablePartValue_ShouldBeTwo()
        {
            Assert.Equal(2, (int)DynamicSqlType.TablePart);
        }

        [Fact]
        public void ReturnInsertedIdAttribute_DefaultValues_ShouldBeCorrect()
        {
            var attr = new ReturnInsertedIdAttribute();
            Assert.Equal("Id", attr.IdColumnName);
            Assert.False(attr.UseValueTask);
        }

        [Fact]
        public void ReturnInsertedIdAttribute_SetIdColumnName_ShouldUpdate()
        {
            var attr = new ReturnInsertedIdAttribute { IdColumnName = "UserId" };
            Assert.Equal("UserId", attr.IdColumnName);
        }

        [Fact]
        public void ReturnInsertedIdAttribute_SetUseValueTask_ShouldUpdate()
        {
            var attr = new ReturnInsertedIdAttribute { UseValueTask = true };
            Assert.True(attr.UseValueTask);
        }

        [Fact]
        public void ReturnInsertedIdAttribute_SetAllProperties_ShouldUpdate()
        {
            var attr = new ReturnInsertedIdAttribute
            {
                IdColumnName = "CustomId",
                UseValueTask = true
            };
            Assert.Equal("CustomId", attr.IdColumnName);
            Assert.True(attr.UseValueTask);
        }

        [Fact]
        public void TableNameAttribute_Constructor_ShouldSetTableName()
        {
            var attr = new TableNameAttribute("Users");
            Assert.Equal("Users", attr.TableName);
        }

        [Fact]
        public void TableNameAttribute_ConstructorWithNull_ShouldThrowArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new TableNameAttribute(null!));
        }

        [Fact]
        public void TableNameAttribute_TableNameProperty_ShouldBeReadOnly()
        {
            var attr = new TableNameAttribute("TestTable");
            Assert.Equal("TestTable", attr.TableName);
            
            // Verify it's a get-only property by checking it can't be set
            var property = typeof(TableNameAttribute).GetProperty("TableName");
            Assert.NotNull(property);
            Assert.Null(property!.SetMethod);
        }
    }
}
