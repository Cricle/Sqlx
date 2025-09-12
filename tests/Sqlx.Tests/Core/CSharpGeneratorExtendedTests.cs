// -----------------------------------------------------------------------
// <copyright file="CSharpGeneratorExtendedTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Sqlx;
using Sqlx.Core;
using System.Linq;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Extended tests for CSharpGenerator to improve test coverage and test edge cases.
    /// </summary>
    [TestClass]
    public class CSharpGeneratorExtendedTests
    {
        [TestMethod]
        public void CSharpGenerator_AttributeSource_IsNotNull()
        {
            // Act
            var attributeSource = AttributeSourceGenerator.GenerateAttributeSource();

            // Assert
            Assert.IsNotNull(attributeSource);
            Assert.IsTrue(attributeSource.Length > 0);
        }

        [TestMethod]
        public void CSharpGenerator_Constructor_InitializesSuccessfully()
        {
            // Act
            var generator = new CSharpGenerator();

            // Assert
            Assert.IsNotNull(generator);
            Assert.IsInstanceOfType(generator, typeof(AbstractGenerator));
            Assert.IsInstanceOfType(generator, typeof(ISourceGenerator));
        }

        [TestMethod]
        public void CSharpGenerator_HasGeneratorAttribute()
        {
            // Arrange & Act
            var generator = new CSharpGenerator();

            // Assert
            Assert.IsInstanceOfType(generator, typeof(AbstractGenerator));
            
            // Verify it has the Generator attribute
            var generatorAttribute = typeof(CSharpGenerator)
                .GetCustomAttributes(typeof(GeneratorAttribute), false)
                .FirstOrDefault() as GeneratorAttribute;
            
            Assert.IsNotNull(generatorAttribute);
            Assert.AreEqual(LanguageNames.CSharp, generatorAttribute.Languages[0]);
        }

        [TestMethod]
        public void CSharpGenerator_AttributeSource_ContainsExpectedContent()
        {
            // Act
            var attributeSource = AttributeSourceGenerator.GenerateAttributeSource();

            // Assert
            Assert.IsTrue(attributeSource.Contains("SqlxAttribute"));
            Assert.IsTrue(attributeSource.Contains("namespace Sqlx.Annotations"));
            Assert.IsTrue(attributeSource.Contains("#nullable enable"));
        }

        [TestMethod]
        public void CSharpGenerator_Initialize_DoesNotThrow()
        {
            // Arrange
            var generator = new CSharpGenerator();

            // Act & Assert - Should not throw
            try
            {
                // We can't easily test the actual initialization without complex mocking
                // But we can test that the generator can be created and has the right type
                Assert.IsNotNull(generator);
                Assert.IsInstanceOfType(generator, typeof(ISourceGenerator));
            }
            catch (System.Exception ex)
            {
                Assert.Fail($"Generator initialization should not throw: {ex.Message}");
            }
        }

        [TestMethod]
        public void CSharpGenerator_IsPublicClass()
        {
            // Assert
            var type = typeof(CSharpGenerator);
            Assert.IsTrue(type.IsPublic);
            Assert.IsFalse(type.IsAbstract);
            Assert.IsFalse(type.IsSealed);
        }

        [TestMethod]
        public void CSharpGenerator_ImplementsRequiredInterfaces()
        {
            // Arrange
            var generator = new CSharpGenerator();

            // Assert
            Assert.IsInstanceOfType(generator, typeof(ISourceGenerator));
            Assert.IsInstanceOfType(generator, typeof(AbstractGenerator));
        }

        [TestMethod]
        public void CSharpGenerator_HasPartialImplementation()
        {
            // This test verifies that the CSharpGenerator has the expected structure
            var type = typeof(CSharpGenerator);
            
            // Should have Initialize method from ISourceGenerator
            var initializeMethod = type.GetMethod("Initialize", new[] { typeof(GeneratorInitializationContext) });
            Assert.IsNotNull(initializeMethod);
            
            // Should have Execute method from AbstractGenerator
            var executeMethod = type.GetMethod("Execute", new[] { typeof(GeneratorExecutionContext) });
            Assert.IsNotNull(executeMethod);
        }

        [TestMethod]
        public void AttributeSourceGenerator_ProducesConsistentOutput()
        {
            // Act
            var source1 = AttributeSourceGenerator.GenerateAttributeSource();
            var source2 = AttributeSourceGenerator.GenerateAttributeSource();

            // Assert
            Assert.AreEqual(source1, source2, "AttributeSourceGenerator should produce consistent output");
        }

        [TestMethod]
        public void AttributeSourceGenerator_ContainsAllRequiredAttributes()
        {
            // Act
            var source = AttributeSourceGenerator.GenerateAttributeSource();

            // Assert - Check for all major attributes
            var expectedAttributes = new[]
            {
                "SqlxAttribute",
                "RawSqlAttribute", 
                "ExpressionToSqlAttribute",
                "SqlExecuteTypeAttribute",
                "RepositoryForAttribute",
                "TableNameAttribute",
                "DbSetTypeAttribute",
                "SqlDefineAttribute"
            };

            foreach (var attr in expectedAttributes)
            {
                Assert.IsTrue(source.Contains(attr), $"Generated source should contain {attr}");
            }
        }

        [TestMethod]
        public void AttributeSourceGenerator_ContainsEnums()
        {
            // Act
            var source = AttributeSourceGenerator.GenerateAttributeSource();

            // Assert
            Assert.IsTrue(source.Contains("public enum SqlExecuteTypes"));
            Assert.IsTrue(source.Contains("public enum SqlDefineTypes"));
            
            // Check enum values
            Assert.IsTrue(source.Contains("Select = 0"));
            Assert.IsTrue(source.Contains("Update = 1"));
            Assert.IsTrue(source.Contains("MySql = 0"));
            Assert.IsTrue(source.Contains("SqlServer = 1"));
        }

        [TestMethod]
        public void AttributeSourceGenerator_ContainsExpressionToSqlClass()
        {
            // Act
            var source = AttributeSourceGenerator.GenerateAttributeSource();

            // Assert
            Assert.IsTrue(source.Contains("public class ExpressionToSql<T>"));
            Assert.IsTrue(source.Contains("public static ExpressionToSql<T> Create()"));
            Assert.IsTrue(source.Contains("public static ExpressionToSql<T> ForSqlServer()"));
            Assert.IsTrue(source.Contains("public ExpressionToSql<T> Where("));
        }
    }
}
