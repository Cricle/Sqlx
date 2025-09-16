// -----------------------------------------------------------------------
// <copyright file="MessagesAdvancedTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Reflection;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Advanced tests for Messages class covering all diagnostic descriptors and error scenarios.
    /// </summary>
    [TestClass]
    public class MessagesAdvancedTests
    {
        [TestMethod]
        public void Messages_AllDiagnosticDescriptors_HaveValidProperties()
        {
            // Get all static DiagnosticDescriptor properties from Messages class
            var messagesType = typeof(Messages);
            var diagnosticProperties = messagesType.GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(DiagnosticDescriptor))
                .ToArray();

            Assert.IsTrue(diagnosticProperties.Length > 0, "No diagnostic descriptors found in Messages class");

            foreach (var property in diagnosticProperties)
            {
                var descriptor = (DiagnosticDescriptor)property.GetValue(null)!;
                
                // Validate basic properties
                Assert.IsNotNull(descriptor, $"Descriptor {property.Name} is null");
                Assert.IsNotNull(descriptor.Id, $"Descriptor {property.Name} has null Id");
                Assert.IsTrue(descriptor.Id.Length > 0, $"Descriptor {property.Name} has empty Id");
                Assert.IsNotNull(descriptor.Title, $"Descriptor {property.Name} has null Title");
                Assert.IsNotNull(descriptor.MessageFormat, $"Descriptor {property.Name} has null MessageFormat");
                Assert.IsNotNull(descriptor.Category, $"Descriptor {property.Name} has null Category");
                Assert.IsNotNull(descriptor.Description, $"Descriptor {property.Name} has null Description");
                
                // Validate Id format (should be SP followed by digits)
                Assert.IsTrue(descriptor.Id.StartsWith("SP"), $"Descriptor {property.Name} Id should start with 'SP'");
                Assert.IsTrue(descriptor.Id.Length >= 5, $"Descriptor {property.Name} Id should be at least 5 characters");
                
                // Validate severity
                Assert.IsTrue(Enum.IsDefined(typeof(DiagnosticSeverity), descriptor.DefaultSeverity),
                    $"Descriptor {property.Name} has invalid severity");
            }
        }

        [TestMethod]
        public void Messages_SP0001_HasCorrectProperties()
        {
            // Test SP0001 - Internal analyzer error
            var descriptor = Messages.SP0001;
            
            Assert.AreEqual("SP0001", descriptor.Id);
            Assert.AreEqual("No stored procedure attribute", descriptor.Title.ToString());
            Assert.AreEqual("Internal analyzer error.", descriptor.MessageFormat.ToString());
            Assert.AreEqual("Internal", descriptor.Category);
            Assert.AreEqual(DiagnosticSeverity.Error, descriptor.DefaultSeverity);
            Assert.IsTrue(descriptor.IsEnabledByDefault);
            Assert.AreEqual("Internal analyzer error occurred during code generation.", descriptor.Description.ToString());
        }

        [TestMethod]
        public void Messages_SP0002_HasCorrectProperties()
        {
            // Test SP0002 - Invalid parameter type
            var descriptor = Messages.SP0002;
            
            Assert.AreEqual("SP0002", descriptor.Id);
            Assert.AreEqual("Invalid parameter type", descriptor.Title.ToString());
            Assert.AreEqual("Parameter type '{0}' is not supported.", descriptor.MessageFormat.ToString());
            Assert.AreEqual("Sqlx", descriptor.Category);
            Assert.AreEqual(DiagnosticSeverity.Error, descriptor.DefaultSeverity);
            Assert.IsTrue(descriptor.IsEnabledByDefault);
            Assert.AreEqual("The parameter type is not supported by Sqlx code generation.", descriptor.Description.ToString());
        }

        [TestMethod]
        public void Messages_SP0003_HasCorrectProperties()
        {
            // Test SP0003 - Missing return type
            var descriptor = Messages.SP0003;
            
            Assert.AreEqual("SP0003", descriptor.Id);
            Assert.AreEqual("Missing return type", descriptor.Title.ToString());
            Assert.AreEqual("Method must have a valid return type.", descriptor.MessageFormat.ToString());
            Assert.AreEqual("Sqlx", descriptor.Category);
            Assert.AreEqual(DiagnosticSeverity.Error, descriptor.DefaultSeverity);
            Assert.IsTrue(descriptor.IsEnabledByDefault);
            Assert.AreEqual("The method must specify a valid return type for code generation.", descriptor.Description.ToString());
        }

        [TestMethod]
        public void Messages_SP0004_HasCorrectProperties()
        {
            // Test SP0004 - Invalid SQL syntax
            var descriptor = Messages.SP0004;
            
            Assert.AreEqual("SP0004", descriptor.Id);
            Assert.AreEqual("Invalid SQL syntax", descriptor.Title.ToString());
            Assert.AreEqual("SQL command contains invalid syntax: '{0}'.", descriptor.MessageFormat.ToString());
            Assert.AreEqual("Sqlx", descriptor.Category);
            Assert.AreEqual(DiagnosticSeverity.Error, descriptor.DefaultSeverity);
            Assert.IsTrue(descriptor.IsEnabledByDefault);
            Assert.AreEqual("The SQL command contains syntax errors that cannot be processed.", descriptor.Description.ToString());
        }

        [TestMethod]
        public void Messages_SP0005_HasCorrectProperties()
        {
            // Test SP0005 - Entity mapping error
            var descriptor = Messages.SP0005;
            
            Assert.AreEqual("SP0005", descriptor.Id);
            Assert.AreEqual("Entity mapping error", descriptor.Title.ToString());
            Assert.AreEqual("Cannot map entity '{0}' to database table.", descriptor.MessageFormat.ToString());
            Assert.AreEqual("Sqlx", descriptor.Category);
            Assert.AreEqual(DiagnosticSeverity.Error, descriptor.DefaultSeverity);
            Assert.IsTrue(descriptor.IsEnabledByDefault);
            Assert.AreEqual("The entity type cannot be mapped to a database table structure.", descriptor.Description.ToString());
        }

        [TestMethod]
        public void Messages_SP0006_HasCorrectProperties()
        {
            // Test SP0006 - Async method missing CancellationToken
            var descriptor = Messages.SP0006;
            
            Assert.AreEqual("SP0006", descriptor.Id);
            Assert.AreEqual("Async method missing CancellationToken", descriptor.Title.ToString());
            Assert.AreEqual("Async method should accept CancellationToken parameter.", descriptor.MessageFormat.ToString());
            Assert.AreEqual("Sqlx", descriptor.Category);
            Assert.AreEqual(DiagnosticSeverity.Error, descriptor.DefaultSeverity);
            Assert.IsTrue(descriptor.IsEnabledByDefault);
            Assert.AreEqual("Async methods should include a CancellationToken parameter for proper cancellation support.", descriptor.Description.ToString());
        }

        [TestMethod]
        public void Messages_SP0007_HasCorrectProperties()
        {
            // Test SP0007 - No RawSqlAttribute or SqlxAttribute tag
            var descriptor = Messages.SP0007;
            
            Assert.AreEqual("SP0007", descriptor.Id);
            Assert.AreEqual("No SqlxAttribute tag", descriptor.Title.ToString());
            Assert.AreEqual("No command text", descriptor.MessageFormat.ToString());
            Assert.AreEqual("Sqlx", descriptor.Category);
            Assert.AreEqual(DiagnosticSeverity.Error, descriptor.DefaultSeverity);
            Assert.IsTrue(descriptor.IsEnabledByDefault);
            Assert.AreEqual("The method must have a SqlxAttribute to specify the SQL command (RawSqlAttribute has been merged into SqlxAttribute).", descriptor.Description.ToString());
        }

        [TestMethod]
        public void Messages_SP0008_HasCorrectProperties()
        {
            // Test SP0008 - Execute no query return must be int or Task<int>
            var descriptor = Messages.SP0008;
            
            Assert.AreEqual("SP0008", descriptor.Id);
            Assert.AreEqual("Execute no query return must be int or Task<int>", descriptor.Title.ToString());
            Assert.AreEqual("Return type error", descriptor.MessageFormat.ToString());
            Assert.AreEqual("Sqlx", descriptor.Category);
            Assert.AreEqual(DiagnosticSeverity.Error, descriptor.DefaultSeverity);
            Assert.IsTrue(descriptor.IsEnabledByDefault);
            Assert.AreEqual("Methods with ExecuteNoQueryAttribute must return int or Task<int> to represent the number of affected rows.", descriptor.Description.ToString());
        }

        [TestMethod]
        public void Messages_SP0009_HasCorrectProperties()
        {
            // Test SP0009 - Repository interface not found
            var descriptor = Messages.SP0009;
            
            Assert.AreEqual("SP0009", descriptor.Id);
            Assert.AreEqual("Repository interface not found", descriptor.Title.ToString());
            Assert.AreEqual("Repository interface '{0}' could not be found.", descriptor.MessageFormat.ToString());
            Assert.AreEqual("Sqlx", descriptor.Category);
            Assert.AreEqual(DiagnosticSeverity.Error, descriptor.DefaultSeverity);
            Assert.IsTrue(descriptor.IsEnabledByDefault);
            Assert.AreEqual("The specified repository interface does not exist or is not accessible.", descriptor.Description.ToString());
        }

        [TestMethod]
        public void Messages_SP0010_HasCorrectProperties()
        {
            // Test SP0010 - Table name not specified
            var descriptor = Messages.SP0010;
            
            Assert.AreEqual("SP0010", descriptor.Id);
            Assert.AreEqual("Table name not specified", descriptor.Title.ToString());
            Assert.AreEqual("Table name must be specified for entity '{0}'.", descriptor.MessageFormat.ToString());
            Assert.AreEqual("Sqlx", descriptor.Category);
            Assert.AreEqual(DiagnosticSeverity.Error, descriptor.DefaultSeverity);
            Assert.IsTrue(descriptor.IsEnabledByDefault);
            Assert.AreEqual("The entity type must have a table name specified via TableNameAttribute or convention.", descriptor.Description.ToString());
        }

        [TestMethod]
        public void Messages_SP0011_HasCorrectProperties()
        {
            // Test SP0011 - Primary key not found
            var descriptor = Messages.SP0011;
            
            Assert.AreEqual("SP0011", descriptor.Id);
            Assert.AreEqual("Primary key not found", descriptor.Title.ToString());
            Assert.AreEqual("Entity '{0}' does not have a primary key property.", descriptor.MessageFormat.ToString());
            Assert.AreEqual("Sqlx", descriptor.Category);
            Assert.AreEqual(DiagnosticSeverity.Error, descriptor.DefaultSeverity);
            Assert.IsTrue(descriptor.IsEnabledByDefault);
            Assert.AreEqual("The entity should have a property named 'Id' or marked with a key attribute.", descriptor.Description.ToString());
        }

        [TestMethod]
        public void Messages_SP0012_HasCorrectProperties()
        {
            // Test SP0012 - Duplicate method name
            var descriptor = Messages.SP0012;
            
            Assert.AreEqual("SP0012", descriptor.Id);
            Assert.AreEqual("Duplicate method name", descriptor.Title.ToString());
            Assert.AreEqual("Method name '{0}' is already defined in this repository.", descriptor.MessageFormat.ToString());
            Assert.AreEqual("Sqlx", descriptor.Category);
            Assert.AreEqual(DiagnosticSeverity.Error, descriptor.DefaultSeverity);
            Assert.IsTrue(descriptor.IsEnabledByDefault);
            Assert.AreEqual("Repository methods must have unique names within the same interface.", descriptor.Description.ToString());
        }

        [TestMethod]
        public void Messages_SP0013_HasCorrectProperties()
        {
            // Test SP0013 - Invalid connection type
            var descriptor = Messages.SP0013;
            
            Assert.AreEqual("SP0013", descriptor.Id);
            Assert.AreEqual("Invalid connection type", descriptor.Title.ToString());
            Assert.AreEqual("Connection type '{0}' is not supported.", descriptor.MessageFormat.ToString());
            Assert.AreEqual("Sqlx", descriptor.Category);
            Assert.AreEqual(DiagnosticSeverity.Error, descriptor.DefaultSeverity);
            Assert.IsTrue(descriptor.IsEnabledByDefault);
            Assert.AreEqual("The connection type must implement IDbConnection or DbConnection.", descriptor.Description.ToString());
        }

        [TestMethod]
        public void Messages_SP0014_HasCorrectProperties()
        {
            // Test SP0014 - SqlDefine configuration error
            var descriptor = Messages.SP0014;
            
            Assert.AreEqual("SP0014", descriptor.Id);
            Assert.AreEqual("SqlDefine configuration error", descriptor.Title.ToString());
            Assert.AreEqual("SqlDefine configuration is invalid: '{0}'.", descriptor.MessageFormat.ToString());
            Assert.AreEqual("Sqlx", descriptor.Category);
            Assert.AreEqual(DiagnosticSeverity.Error, descriptor.DefaultSeverity);
            Assert.IsTrue(descriptor.IsEnabledByDefault);
            Assert.AreEqual("The SqlDefine attribute contains invalid database dialect configuration.", descriptor.Description.ToString());
        }

        [TestMethod]
        public void Messages_SP0015_HasCorrectProperties()
        {
            // Test SP0015 - Code generation failed
            var descriptor = Messages.SP0015;
            
            Assert.AreEqual("SP0015", descriptor.Id);
            Assert.AreEqual("Code generation failed", descriptor.Title.ToString());
            Assert.AreEqual("Code generation failed for method '{0}': {1}.", descriptor.MessageFormat.ToString());
            Assert.AreEqual("Sqlx", descriptor.Category);
            Assert.AreEqual(DiagnosticSeverity.Error, descriptor.DefaultSeverity);
            Assert.IsTrue(descriptor.IsEnabledByDefault);
            Assert.AreEqual("An error occurred during code generation for the specified method.", descriptor.Description.ToString());
        }

        [TestMethod]
        public void Messages_DiagnosticIds_AreUnique()
        {
            // Test that all diagnostic IDs are unique
            var messagesType = typeof(Messages);
            var diagnosticProperties = messagesType.GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(DiagnosticDescriptor))
                .ToArray();

            var ids = diagnosticProperties
                .Select(p => ((DiagnosticDescriptor)p.GetValue(null)!).Id)
                .ToList();

            var duplicates = ids.GroupBy(id => id)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            Assert.AreEqual(0, duplicates.Count, 
                $"Duplicate diagnostic IDs found: {string.Join(", ", duplicates)}");
        }

        [TestMethod]
        public void Messages_DiagnosticIds_FollowConvention()
        {
            // Test that all diagnostic IDs follow the SP#### convention
            var messagesType = typeof(Messages);
            var diagnosticProperties = messagesType.GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(DiagnosticDescriptor))
                .ToArray();

            foreach (var property in diagnosticProperties)
            {
                var descriptor = (DiagnosticDescriptor)property.GetValue(null)!;
                
                Assert.IsTrue(descriptor.Id.StartsWith("SP"), 
                    $"Diagnostic ID {descriptor.Id} should start with 'SP'");
                
                Assert.IsTrue(descriptor.Id.Length >= 5, 
                    $"Diagnostic ID {descriptor.Id} should be at least 5 characters");
                
                Assert.IsTrue(char.IsDigit(descriptor.Id[2]) && 
                             char.IsDigit(descriptor.Id[3]) && 
                             char.IsDigit(descriptor.Id[4]),
                    $"Diagnostic ID {descriptor.Id} should have 4 digits after 'SP'");
            }
        }

        [TestMethod]
        public void Messages_Categories_AreValid()
        {
            // Test that diagnostic categories are valid
            var validCategories = new[] { "Sqlx", "Internal" };
            var messagesType = typeof(Messages);
            var diagnosticProperties = messagesType.GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(DiagnosticDescriptor))
                .ToArray();

            foreach (var property in diagnosticProperties)
            {
                var descriptor = (DiagnosticDescriptor)property.GetValue(null)!;
                
                Assert.IsTrue(validCategories.Contains(descriptor.Category),
                    $"Diagnostic {descriptor.Id} has invalid category '{descriptor.Category}'. Valid categories: {string.Join(", ", validCategories)}");
            }
        }

        [TestMethod]
        public void Messages_MessageFormats_SupportFormatting()
        {
            // Test that message formats with placeholders work correctly
            var formattableDescriptors = new[]
            {
                (Messages.SP0002, new[] { "string" }),
                (Messages.SP0004, new[] { "SELECT * FROM" }),
                (Messages.SP0005, new[] { "User" }),
                (Messages.SP0009, new[] { "IUserRepository" }),
                (Messages.SP0010, new[] { "User" }),
                (Messages.SP0011, new[] { "User" }),
                (Messages.SP0012, new[] { "GetUser" }),
                (Messages.SP0013, new[] { "SqlConnection" }),
                (Messages.SP0014, new[] { "Invalid dialect" }),
                (Messages.SP0015, new[] { "GetUser", "Null reference exception" })
            };

            foreach (var (descriptor, args) in formattableDescriptors)
            {
                try
                {
                    var formattedMessage = string.Format(descriptor.MessageFormat.ToString(), args);
                    Assert.IsNotNull(formattedMessage);
                    Assert.IsTrue(formattedMessage.Length > 0);
                    
                    // Verify that placeholders were replaced
                    foreach (var arg in args)
                    {
                        Assert.IsTrue(formattedMessage.Contains(arg.ToString()),
                            $"Formatted message should contain argument '{arg}'");
                    }
                }
                catch (FormatException ex)
                {
                    Assert.Fail($"Format exception for {descriptor.Id}: {ex.Message}");
                }
            }
        }

        [TestMethod]
        public void Messages_AllDescriptors_AreEnabledByDefault()
        {
            // Test that all descriptors are enabled by default
            var messagesType = typeof(Messages);
            var diagnosticProperties = messagesType.GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(DiagnosticDescriptor))
                .ToArray();

            foreach (var property in diagnosticProperties)
            {
                var descriptor = (DiagnosticDescriptor)property.GetValue(null)!;
                Assert.IsTrue(descriptor.IsEnabledByDefault,
                    $"Descriptor {descriptor.Id} should be enabled by default");
            }
        }

        [TestMethod]
        public void Messages_AllDescriptors_HaveErrorSeverity()
        {
            // Test that all descriptors have Error severity (as per current implementation)
            var messagesType = typeof(Messages);
            var diagnosticProperties = messagesType.GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(DiagnosticDescriptor))
                .ToArray();

            foreach (var property in diagnosticProperties)
            {
                var descriptor = (DiagnosticDescriptor)property.GetValue(null)!;
                Assert.AreEqual(DiagnosticSeverity.Error, descriptor.DefaultSeverity,
                    $"Descriptor {descriptor.Id} should have Error severity");
            }
        }

        [TestMethod]
        public void Messages_DescriptorCount_IsComplete()
        {
            // Test that we have the expected number of descriptors
            var messagesType = typeof(Messages);
            var diagnosticProperties = messagesType.GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(DiagnosticDescriptor))
                .ToArray();

            // We expect at least 15 descriptors (SP0001 through SP0015)
            Assert.IsTrue(diagnosticProperties.Length >= 15,
                $"Expected at least 15 diagnostic descriptors, found {diagnosticProperties.Length}");
        }
    }
}
