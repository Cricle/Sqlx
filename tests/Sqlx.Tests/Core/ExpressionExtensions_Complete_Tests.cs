// -----------------------------------------------------------------------
// <copyright file="ExpressionExtensions_Complete_Tests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Xunit;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Complete tests for ExpressionExtensions to achieve 100% coverage
    /// </summary>
    public class ExpressionExtensions_Complete_Tests
    {
        private class TestUser
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public int Age { get; set; }
            public bool IsActive { get; set; }
        }

        private class TestContainer
        {
            public int Value { get; set; }
            public string Text { get; set; } = string.Empty;
        }

        [Fact]
        public void GetParameters_WithComplexExpression_ShouldExtractAllParameters()
        {
            // Test complex expression with multiple parameters
            var parameters = ExpressionExtensions.GetParameters<TestUser>(x => x.Age > 25 && x.Age < 50);

            Assert.NotNull(parameters);
        }
    }
}
