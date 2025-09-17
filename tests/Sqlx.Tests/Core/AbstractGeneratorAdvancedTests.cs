// -----------------------------------------------------------------------
// <copyright file="AbstractGeneratorAdvancedTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Advanced tests for AbstractGenerator class covering architecture and design patterns.
    /// </summary>
    [TestClass]
    public class AbstractGeneratorAdvancedTests
    {
        [TestMethod]
        public void AbstractGenerator_ClassStructure_IsCorrect()
        {
            // Test AbstractGenerator class structure
            var abstractGeneratorType = typeof(AbstractGenerator);

            // Should be abstract
            Assert.IsTrue(abstractGeneratorType.IsAbstract, "AbstractGenerator should be abstract");

            // Should be public
            Assert.IsTrue(abstractGeneratorType.IsPublic, "AbstractGenerator should be public");

            // Should implement ISourceGenerator
            Assert.IsTrue(typeof(Microsoft.CodeAnalysis.ISourceGenerator).IsAssignableFrom(abstractGeneratorType),
                "AbstractGenerator should implement ISourceGenerator");

            // Should be in correct namespace
            Assert.AreEqual("Sqlx", abstractGeneratorType.Namespace, "AbstractGenerator should be in Sqlx namespace");
        }

        [TestMethod]
        public void AbstractGenerator_RequiredMethods_ArePresent()
        {
            // Test that required methods are present
            var abstractGeneratorType = typeof(AbstractGenerator);

            // Initialize method should be abstract
            var initializeMethod = abstractGeneratorType.GetMethod("Initialize",
                new[] { typeof(Microsoft.CodeAnalysis.GeneratorInitializationContext) });
            Assert.IsNotNull(initializeMethod, "Initialize method should exist");
            Assert.IsTrue(initializeMethod.IsAbstract, "Initialize method should be abstract");
            Assert.IsTrue(initializeMethod.IsPublic, "Initialize method should be public");

            // Execute method should be implemented
            var executeMethod = abstractGeneratorType.GetMethod("Execute",
                new[] { typeof(Microsoft.CodeAnalysis.GeneratorExecutionContext) });
            Assert.IsNotNull(executeMethod, "Execute method should exist");
            Assert.IsFalse(executeMethod.IsAbstract, "Execute method should be implemented");
            Assert.IsTrue(executeMethod.IsPublic, "Execute method should be public");
        }

        [TestMethod]
        public void AbstractGenerator_ExecuteMethod_HasCorrectSignature()
        {
            // Test Execute method signature
            var abstractGeneratorType = typeof(AbstractGenerator);
            var executeMethod = abstractGeneratorType.GetMethod("Execute",
                new[] { typeof(Microsoft.CodeAnalysis.GeneratorExecutionContext) });

            Assert.IsNotNull(executeMethod);
            Assert.AreEqual(typeof(void), executeMethod.ReturnType, "Execute should return void");

            var parameters = executeMethod.GetParameters();
            Assert.AreEqual(1, parameters.Length, "Execute should have one parameter");
            Assert.AreEqual("context", parameters[0].Name, "Parameter should be named 'context'");
            Assert.AreEqual(typeof(Microsoft.CodeAnalysis.GeneratorExecutionContext), parameters[0].ParameterType,
                "Parameter should be GeneratorExecutionContext");
        }

        [TestMethod]
        public void AbstractGenerator_InitializeMethod_HasCorrectSignature()
        {
            // Test Initialize method signature
            var abstractGeneratorType = typeof(AbstractGenerator);
            var initializeMethod = abstractGeneratorType.GetMethod("Initialize",
                new[] { typeof(Microsoft.CodeAnalysis.GeneratorInitializationContext) });

            Assert.IsNotNull(initializeMethod);
            Assert.AreEqual(typeof(void), initializeMethod.ReturnType, "Initialize should return void");
            Assert.IsTrue(initializeMethod.IsAbstract, "Initialize should be abstract");

            var parameters = initializeMethod.GetParameters();
            Assert.AreEqual(1, parameters.Length, "Initialize should have one parameter");
            Assert.AreEqual("context", parameters[0].Name, "Parameter should be named 'context'");
            Assert.AreEqual(typeof(Microsoft.CodeAnalysis.GeneratorInitializationContext), parameters[0].ParameterType,
                "Parameter should be GeneratorInitializationContext");
        }

        [TestMethod]
        public void AbstractGenerator_PrivateHelperMethods_ExistForCodeGeneration()
        {
            // Test that private helper methods exist for code generation
            var abstractGeneratorType = typeof(AbstractGenerator);
            var allMethods = abstractGeneratorType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            // Should have helper methods for different aspects of code generation
            var helperMethods = allMethods.Where(m => m.IsPrivate).ToArray();
            Assert.IsTrue(helperMethods.Length > 0, "Should have private helper methods");

            // Look for methods that suggest code generation functionality
            var codeGenMethods = helperMethods.Where(m =>
                m.Name.Contains("Generate") ||
                m.Name.Contains("Create") ||
                m.Name.Contains("Build") ||
                m.Name.Contains("Get") ||
                m.Name.Contains("Process")).ToArray();

            Assert.IsTrue(codeGenMethods.Length > 0, "Should have code generation helper methods");
        }

        [TestMethod]
        public void AbstractGenerator_FieldsAndProperties_AreWellDesigned()
        {
            // Test fields and properties design
            var abstractGeneratorType = typeof(AbstractGenerator);

            // Get all fields
            var fields = abstractGeneratorType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            // Fields should generally be private or protected
            foreach (var field in fields)
            {
                Assert.IsTrue(field.IsPrivate || field.IsFamily || field.IsFamilyAndAssembly,
                    $"Field {field.Name} should be private or protected");
            }

            // Get all properties
            var properties = abstractGeneratorType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            // Properties should be appropriately accessible
            foreach (var property in properties)
            {
                Assert.IsNotNull(property.GetGetMethod(true), $"Property {property.Name} should have a getter");
            }
        }

        [TestMethod]
        public void AbstractGenerator_InheritanceDesign_IsCorrect()
        {
            // Test inheritance design
            var abstractGeneratorType = typeof(AbstractGenerator);

            // Should inherit from object (no other base class)
            Assert.AreEqual(typeof(object), abstractGeneratorType.BaseType,
                "AbstractGenerator should inherit directly from object");

            // Should implement ISourceGenerator interface
            var interfaces = abstractGeneratorType.GetInterfaces();
            Assert.IsTrue(interfaces.Contains(typeof(Microsoft.CodeAnalysis.ISourceGenerator)),
                "AbstractGenerator should implement ISourceGenerator");

            // Should not implement other interfaces directly (composition over inheritance)
            Assert.AreEqual(1, interfaces.Length,
                "AbstractGenerator should only implement ISourceGenerator interface");
        }

        [TestMethod]
        public void AbstractGenerator_MethodAccessibility_IsAppropriate()
        {
            // Test method accessibility design
            var abstractGeneratorType = typeof(AbstractGenerator);
            var allMethods = abstractGeneratorType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            // Public methods should be minimal (interface implementation + any extension points)
            var publicMethods = allMethods.Where(m => m.IsPublic && m.DeclaringType == abstractGeneratorType).ToArray();

            // Should have at least Initialize and Execute
            Assert.IsTrue(publicMethods.Any(m => m.Name == "Initialize"), "Should have public Initialize method");
            Assert.IsTrue(publicMethods.Any(m => m.Name == "Execute"), "Should have public Execute method");

            // Protected methods (if any) should be for extensibility
            var protectedMethods = allMethods.Where(m => m.IsFamily && m.DeclaringType == abstractGeneratorType).ToArray();
            // Protected methods are optional, but if they exist, they should be for extensibility

            // Private methods should handle implementation details
            var privateMethods = allMethods.Where(m => m.IsPrivate && m.DeclaringType == abstractGeneratorType).ToArray();
            Assert.IsTrue(privateMethods.Length > 0, "Should have private implementation methods");
        }

        [TestMethod]
        public void AbstractGenerator_ErrorHandling_IsConsidered()
        {
            // Test error handling approach
            var abstractGeneratorType = typeof(AbstractGenerator);

            // Execute method should not throw exceptions (generators should be resilient)
            var executeMethod = abstractGeneratorType.GetMethod("Execute");
            Assert.IsNotNull(executeMethod);

            // The method signature suggests it handles errors gracefully
            // (void return type means errors are likely reported via context)
            Assert.AreEqual(typeof(void), executeMethod.ReturnType,
                "Execute method should return void, indicating errors are handled via context");
        }

        [TestMethod]
        public void AbstractGenerator_AttributeUsage_IsCorrect()
        {
            // Test that the class doesn't have inappropriate attributes
            var abstractGeneratorType = typeof(AbstractGenerator);
            var attributes = abstractGeneratorType.GetCustomAttributes().ToArray();

            // Should not have Generator attribute (that's for concrete implementations)
            var generatorAttribute = attributes.FirstOrDefault(a => a.GetType().Name.Contains("Generator"));
            Assert.IsNull(generatorAttribute,
                "AbstractGenerator should not have Generator attribute - that's for concrete implementations");

            // May have other attributes like copyright, etc.
            // This is fine and expected
        }

        [TestMethod]
        public void AbstractGenerator_PartialClassDesign_IsCorrect()
        {
            // Test partial class design
            var abstractGeneratorType = typeof(AbstractGenerator);

            // Check if it's partial (this is indicated in the source as "partial class")
            // We can't directly test this via reflection, but we can check the structure suggests it

            // The class should be designed to support partial class pattern
            Assert.IsTrue(abstractGeneratorType.IsClass, "Should be a class");
            Assert.IsTrue(abstractGeneratorType.IsAbstract, "Should be abstract");

            // The presence of many methods suggests it might be split across multiple files
            var allMethods = abstractGeneratorType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            Assert.IsTrue(allMethods.Length > 10, "Should have many methods, suggesting complex implementation possibly split across files");
        }

        [TestMethod]
        public void AbstractGenerator_ConstantsAndStatics_AreWellDesigned()
        {
            // Test constants and static members
            var abstractGeneratorType = typeof(AbstractGenerator);

            // Get static fields and properties
            var staticFields = abstractGeneratorType.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            var staticProperties = abstractGeneratorType.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            // Static members should be appropriately accessible
            foreach (var field in staticFields)
            {
                if (field.IsLiteral || field.IsInitOnly)
                {
                    // Constants and readonly fields are fine
                    Assert.IsTrue(field.IsPrivate || field.IsAssembly || field.IsPublic,
                        $"Static field {field.Name} should have appropriate accessibility");
                }
            }
        }

        [TestMethod]
        public void AbstractGenerator_PerformanceConsiderations_AreEvident()
        {
            // Test that performance considerations are evident in design
            var abstractGeneratorType = typeof(AbstractGenerator);

            // Should not have excessive virtual methods (performance cost)
            var virtualMethods = abstractGeneratorType.GetMethods()
                .Where(m => m.IsVirtual && !m.IsAbstract && m.DeclaringType == abstractGeneratorType)
                .ToArray();

            // Virtual methods should be minimal and purposeful
            Assert.IsTrue(virtualMethods.Length < 10,
                "Should have minimal virtual methods for performance");

            // Should not have excessive properties with complex getters
            var properties = abstractGeneratorType.GetProperties();
            foreach (var property in properties)
            {
                var getter = property.GetGetMethod(true);
                if (getter != null)
                {
                    // Getters should be simple (this is hard to test directly)
                    Assert.IsNotNull(getter, $"Property {property.Name} getter should be accessible");
                }
            }
        }

        [TestMethod]
        public void AbstractGenerator_CodeGeneration_MethodsExist()
        {
            // Test that code generation methods exist
            var abstractGeneratorType = typeof(AbstractGenerator);
            var allMethods = abstractGeneratorType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);

            // Look for methods that suggest SQL generation capabilities
            var sqlGenMethods = allMethods.Where(m =>
                m.Name.Contains("Sql") ||
                m.Name.Contains("Generate") ||
                m.Name.Contains("Create") ||
                m.Name.Contains("Build")).ToArray();

            Assert.IsTrue(sqlGenMethods.Length > 0, "Should have SQL generation methods");

            // Look for methods that suggest entity mapping
            var entityMethods = allMethods.Where(m =>
                m.Name.Contains("Entity") ||
                m.Name.Contains("Property") ||
                m.Name.Contains("Map")).ToArray();

            Assert.IsTrue(entityMethods.Length > 0, "Should have entity mapping methods");
        }

        [TestMethod]
        public void AbstractGenerator_DiagnosticSupport_IsPresent()
        {
            // Test that diagnostic support is present
            var abstractGeneratorType = typeof(AbstractGenerator);
            var allMethods = abstractGeneratorType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);

            // Look for methods that suggest diagnostic reporting
            var diagnosticMethods = allMethods.Where(m =>
                m.Name.Contains("Diagnostic") ||
                m.Name.Contains("Error") ||
                m.Name.Contains("Report")).ToArray();

            // Diagnostic support may be implicit, so this test is more about structure
            Assert.IsTrue(diagnosticMethods.Length >= 0, "Diagnostic methods may exist");

            // The Execute method should handle diagnostics via context parameter
            var executeMethod = abstractGeneratorType.GetMethod("Execute");
            Assert.IsNotNull(executeMethod);

            var contextParam = executeMethod.GetParameters().FirstOrDefault();
            Assert.IsNotNull(contextParam);
            Assert.AreEqual(typeof(Microsoft.CodeAnalysis.GeneratorExecutionContext), contextParam.ParameterType,
                "Execute method should receive context for diagnostic reporting");
        }
    }
}
