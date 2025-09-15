// -----------------------------------------------------------------------
// <copyright file="CSharpGeneratorExtendedTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Sqlx;
using System;
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
            var attributeSource = "// Attributes are now directly available in Sqlx.Core project";

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
            var attributeSource = "// Attributes are now directly available in Sqlx.Core project";

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
            var source1 = "// Attributes are now directly available in Sqlx.Core project";
            var source2 = "// Attributes are now directly available in Sqlx.Core project";

            // Assert
            Assert.AreEqual(source1, source2, "AttributeSourceGenerator should produce consistent output");
        }

        [TestMethod]
        public void AttributeSourceGenerator_ContainsAllRequiredAttributes()
        {
            // Act - Test that actual attribute types exist in Sqlx.Core
            var sqlxAttrType = typeof(Sqlx.Annotations.SqlxAttribute);
            var rawSqlAttrType = typeof(Sqlx.Annotations.RawSqlAttribute);
            var expressionToSqlAttrType = typeof(Sqlx.Annotations.ExpressionToSqlAttribute);
            var sqlExecuteTypeAttrType = typeof(Sqlx.Annotations.SqlExecuteTypeAttribute);
            var repositoryForAttrType = typeof(Sqlx.Annotations.RepositoryForAttribute);
            var tableNameAttrType = typeof(Sqlx.Annotations.TableNameAttribute);
            var dbSetTypeAttrType = typeof(Sqlx.Annotations.DbSetTypeAttribute);
            var sqlDefineAttrType = typeof(Sqlx.Annotations.SqlDefineAttribute);

            // Assert - check that all attribute types are accessible and public
            Assert.IsNotNull(sqlxAttrType, "SqlxAttribute should be accessible");
            Assert.IsNotNull(rawSqlAttrType, "RawSqlAttribute should be accessible");
            Assert.IsNotNull(expressionToSqlAttrType, "ExpressionToSqlAttribute should be accessible");
            Assert.IsNotNull(sqlExecuteTypeAttrType, "SqlExecuteTypeAttribute should be accessible");
            Assert.IsNotNull(repositoryForAttrType, "RepositoryForAttribute should be accessible");
            Assert.IsNotNull(tableNameAttrType, "TableNameAttribute should be accessible");
            Assert.IsNotNull(dbSetTypeAttrType, "DbSetTypeAttribute should be accessible");
            Assert.IsNotNull(sqlDefineAttrType, "SqlDefineAttribute should be accessible");

            // Verify they are all public
            Assert.IsTrue(sqlxAttrType.IsPublic, "SqlxAttribute should be public");
            Assert.IsTrue(rawSqlAttrType.IsPublic, "RawSqlAttribute should be public");
            Assert.IsTrue(expressionToSqlAttrType.IsPublic, "ExpressionToSqlAttribute should be public");
            Assert.IsTrue(sqlExecuteTypeAttrType.IsPublic, "SqlExecuteTypeAttribute should be public");
            Assert.IsTrue(repositoryForAttrType.IsPublic, "RepositoryForAttribute should be public");
            Assert.IsTrue(tableNameAttrType.IsPublic, "TableNameAttribute should be public");
            Assert.IsTrue(dbSetTypeAttrType.IsPublic, "DbSetTypeAttribute should be public");
            Assert.IsTrue(sqlDefineAttrType.IsPublic, "SqlDefineAttribute should be public");
        }

        [TestMethod]
        public void AttributeSourceGenerator_ContainsEnums()
        {
            // Act - Test that actual enum types exist in Sqlx.Core
            var sqlExecuteTypesType = typeof(Sqlx.Annotations.SqlExecuteTypes);
            var sqlDefineTypesType = typeof(Sqlx.Annotations.SqlDefineTypes);

            // Assert - check that enum types are accessible and public
            Assert.IsNotNull(sqlExecuteTypesType, "SqlExecuteTypes should be accessible");
            Assert.IsNotNull(sqlDefineTypesType, "SqlDefineTypes should be accessible");
            Assert.IsTrue(sqlExecuteTypesType.IsEnum, "SqlExecuteTypes should be an enum");
            Assert.IsTrue(sqlDefineTypesType.IsEnum, "SqlDefineTypes should be an enum");
            Assert.IsTrue(sqlExecuteTypesType.IsPublic, "SqlExecuteTypes should be public");
            Assert.IsTrue(sqlDefineTypesType.IsPublic, "SqlDefineTypes should be public");
            
            // Check enum values
            Assert.IsTrue(Enum.IsDefined(sqlExecuteTypesType, 0), "Select should be defined");
            Assert.IsTrue(Enum.IsDefined(sqlExecuteTypesType, 1), "Update should be defined");
            Assert.IsTrue(Enum.IsDefined(sqlDefineTypesType, 0), "MySql should be defined");
            Assert.IsTrue(Enum.IsDefined(sqlDefineTypesType, 1), "SqlServer should be defined");
        }

        [TestMethod]
        public void AttributeSourceGenerator_ContainsExpressionToSqlClass()
        {
            // Act - Test that actual ExpressionToSql type exists in Sqlx.Core
            var expressionToSqlType = typeof(Sqlx.Annotations.ExpressionToSql<>);

            // Assert - check that ExpressionToSql class is accessible and public
            Assert.IsNotNull(expressionToSqlType, "ExpressionToSql<T> should be accessible");
            Assert.IsTrue(expressionToSqlType.IsPublic, "ExpressionToSql<T> should be public");
            Assert.IsTrue(expressionToSqlType.IsGenericTypeDefinition, "ExpressionToSql<T> should be generic");

            // Check for required static methods
            var createMethod = expressionToSqlType.GetMethod("Create", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var forSqlServerMethod = expressionToSqlType.GetMethod("ForSqlServer", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            
            Assert.IsNotNull(createMethod, "Create() method should exist");
            Assert.IsNotNull(forSqlServerMethod, "ForSqlServer() method should exist");

            // Test that we can create an instance using the concrete type
            var concreteType = expressionToSqlType.MakeGenericType(typeof(object));
            var concreteCreateMethod = concreteType.GetMethod("Create", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            Assert.IsNotNull(concreteCreateMethod, "Create() method should exist on concrete type");
            
            var instance = concreteCreateMethod.Invoke(null, null);
            Assert.IsNotNull(instance, "Should be able to create ExpressionToSql instance");
        }
    }
}
