// -----------------------------------------------------------------------
// <copyright file="ReturnInsertedAttributes_Tests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Sqlx.Annotations;
using Xunit;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Tests for ReturnInsertedEntityAttribute and SetEntityIdAttribute to achieve 100% coverage
    /// </summary>
    public class ReturnInsertedAttributes_Tests
    {
        [Fact]
        public void ReturnInsertedEntityAttribute_DefaultValues_ShouldBeCorrect()
        {
            // Test default property values
            var attr = new ReturnInsertedEntityAttribute();

            Assert.Equal("Id", attr.IdColumnName);
            Assert.False(attr.UseValueTask);
            Assert.False(attr.CreateNewInstance);
        }

        [Fact]
        public void ReturnInsertedEntityAttribute_SetIdColumnName_ShouldUpdate()
        {
            // Test IdColumnName property setter
            var attr = new ReturnInsertedEntityAttribute
            {
                IdColumnName = "UserId"
            };

            Assert.Equal("UserId", attr.IdColumnName);
        }

        [Fact]
        public void ReturnInsertedEntityAttribute_SetUseValueTask_ShouldUpdate()
        {
            // Test UseValueTask property setter
            var attr = new ReturnInsertedEntityAttribute
            {
                UseValueTask = true
            };

            Assert.True(attr.UseValueTask);
        }

        [Fact]
        public void ReturnInsertedEntityAttribute_SetCreateNewInstance_ShouldUpdate()
        {
            // Test CreateNewInstance property setter
            var attr = new ReturnInsertedEntityAttribute
            {
                CreateNewInstance = true
            };

            Assert.True(attr.CreateNewInstance);
        }

        [Fact]
        public void ReturnInsertedEntityAttribute_SetAllProperties_ShouldUpdate()
        {
            // Test setting all properties
            var attr = new ReturnInsertedEntityAttribute
            {
                IdColumnName = "CustomId",
                UseValueTask = true,
                CreateNewInstance = true
            };

            Assert.Equal("CustomId", attr.IdColumnName);
            Assert.True(attr.UseValueTask);
            Assert.True(attr.CreateNewInstance);
        }

        [Fact]
        public void SetEntityIdAttribute_DefaultValues_ShouldBeCorrect()
        {
            // Test default property values
            var attr = new SetEntityIdAttribute();

            Assert.Equal("Id", attr.IdColumnName);
        }

        [Fact]
        public void SetEntityIdAttribute_SetIdColumnName_ShouldUpdate()
        {
            // Test IdColumnName property setter
            var attr = new SetEntityIdAttribute
            {
                IdColumnName = "EntityId"
            };

            Assert.Equal("EntityId", attr.IdColumnName);
        }

        [Fact]
        public void SetEntityIdAttribute_SetCustomColumnName_ShouldUpdate()
        {
            // Test setting custom column name
            var attr = new SetEntityIdAttribute
            {
                IdColumnName = "PrimaryKey"
            };

            Assert.Equal("PrimaryKey", attr.IdColumnName);
        }
    }
}
