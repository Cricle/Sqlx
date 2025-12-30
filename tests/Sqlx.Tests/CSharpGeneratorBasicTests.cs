// -----------------------------------------------------------------------
// <copyright file="CSharpGeneratorBasicTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;

namespace Sqlx.Tests
{
    /// <summary>
    /// Basic tests for CSharpGenerator class to improve coverage.
    /// </summary>
    [TestClass]
    public class CSharpGeneratorBasicTests
    {
        [TestMethod]
        public void Constructor_CreatesInstance()
        {
            // Act
            var generator = new CSharpGenerator();

            // Assert
            Assert.IsNotNull(generator);
        }

        [TestMethod]
        public void Generator_InheritsFromAbstractGenerator()
        {
            // Arrange
            var generator = new CSharpGenerator();

            // Assert
            Assert.IsInstanceOfType<AbstractGenerator>(generator);
        }

        [TestMethod]
        public void Generator_IsSourceGenerator()
        {
            // Arrange
            var generator = new CSharpGenerator();

            // Assert
            Assert.IsInstanceOfType<Microsoft.CodeAnalysis.ISourceGenerator>(generator);
        }
    }
}
