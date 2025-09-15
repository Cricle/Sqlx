// -----------------------------------------------------------------------
// <copyright file="MessagesExtendedTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis;
using Sqlx;
using System.Linq;
using System.Reflection;

namespace Sqlx.Tests;

[TestClass]
public class MessagesExtendedTests
{
    [TestMethod]
    [DataRow("SP0001")]
    [DataRow("SP0002")]
    [DataRow("SP0003")]
    [DataRow("SP0004")]
    [DataRow("SP0005")]
    [DataRow("SP0006")]
    [DataRow("SP0007")]
    [DataRow("SP0008")]
    [DataRow("SP0009")]
    [DataRow("SP0010")]
    [DataRow("SP0011")]
    [DataRow("SP0012")]
    [DataRow("SP0013")]
    [DataRow("SP0014")]
    [DataRow("SP0015")]
    public void Messages_ContainsWellFormattedErrorMessages(string expectedId)
    {
        // Arrange
        var messagesType = typeof(Messages);
        var property = messagesType.GetProperty(expectedId, BindingFlags.Public | BindingFlags.Static);

        // Act
        var diagnostic = property?.GetValue(null) as DiagnosticDescriptor;

        // Assert
        Assert.IsNotNull(diagnostic, $"Diagnostic {expectedId} should exist");
        Assert.AreEqual(expectedId, diagnostic.Id);
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Title.ToString()));
        Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.MessageFormat.ToString()));
        Assert.IsTrue(diagnostic.IsEnabledByDefault);
    }

    [TestMethod]
    public void Messages_AllDiagnostics_HaveCorrectProperties()
    {
        // Arrange
        var messagesType = typeof(Messages);
        var diagnosticProperties = messagesType.GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(DiagnosticDescriptor))
            .ToList();

        // Act & Assert
        Assert.IsTrue(diagnosticProperties.Count >= 15, "Should have at least 15 diagnostic messages");

        foreach (var property in diagnosticProperties)
        {
            var diagnostic = property.GetValue(null) as DiagnosticDescriptor;
            Assert.IsNotNull(diagnostic, $"Property {property.Name} should return a valid DiagnosticDescriptor");

            // Check ID format
            Assert.IsTrue(diagnostic.Id.StartsWith("SP"), $"Diagnostic ID {diagnostic.Id} should start with 'SP'");
            Assert.AreEqual(6, diagnostic.Id.Length, $"Diagnostic ID {diagnostic.Id} should be 6 characters long");

            // Check that title and message are not empty
            Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Title.ToString()),
                $"Diagnostic {diagnostic.Id} should have a non-empty title");
            Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.MessageFormat.ToString()),
                $"Diagnostic {diagnostic.Id} should have a non-empty message format");

            // Check category
            Assert.IsFalse(string.IsNullOrWhiteSpace(diagnostic.Category),
                $"Diagnostic {diagnostic.Id} should have a category");

            // Check severity
            Assert.AreNotEqual(DiagnosticSeverity.Hidden,
diagnostic.DefaultSeverity, $"Diagnostic {diagnostic.Id} should have a meaningful severity level");
        }
    }

    [TestMethod]
    public void Messages_AllDiagnostics_HaveUniqueIds()
    {
        // Arrange
        var messagesType = typeof(Messages);
        var diagnosticProperties = messagesType.GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(DiagnosticDescriptor))
            .ToList();

        // Act
        var diagnosticIds = diagnosticProperties
            .Select(p => (p.GetValue(null) as DiagnosticDescriptor)?.Id)
            .Where(id => id != null)
            .ToList();

        // Assert
        var uniqueIds = diagnosticIds.Distinct().ToList();
        Assert.AreEqual(diagnosticIds.Count, uniqueIds.Count, "All diagnostic IDs should be unique");
    }

    [TestMethod]
    public void Messages_DiagnosticSeverities_AreAppropriate()
    {
        // Arrange & Act
        var sp0001 = Messages.SP0001; // Internal analyzer error
        var sp0002 = Messages.SP0002; // Invalid parameter type
        var sp0003 = Messages.SP0003; // Missing return type
        var sp0004 = Messages.SP0004; // Invalid SQL syntax
        var sp0005 = Messages.SP0005; // Entity mapping error
        var sp0006 = Messages.SP0006; // Async method missing CancellationToken

        // Assert
        Assert.AreEqual(DiagnosticSeverity.Error, sp0001.DefaultSeverity, "Internal errors should be Error severity");
        Assert.AreEqual(DiagnosticSeverity.Error, sp0002.DefaultSeverity, "Invalid parameter type should be Error severity");
        Assert.AreEqual(DiagnosticSeverity.Error, sp0003.DefaultSeverity, "Missing return type should be Error severity");
        Assert.AreEqual(DiagnosticSeverity.Error, sp0004.DefaultSeverity, "Invalid SQL syntax should be Error severity");
        Assert.AreEqual(DiagnosticSeverity.Error, sp0005.DefaultSeverity, "Entity mapping error should be Error severity");
        Assert.AreEqual(DiagnosticSeverity.Error, sp0006.DefaultSeverity, "Missing CancellationToken should be Error severity");
    }

    [TestMethod]
    public void Messages_DiagnosticCategories_AreConsistent()
    {
        // Arrange
        var messagesType = typeof(Messages);
        var diagnosticProperties = messagesType.GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(DiagnosticDescriptor))
            .ToList();

        // Act
        var categories = diagnosticProperties
            .Select(p => (p.GetValue(null) as DiagnosticDescriptor)?.Category)
            .Distinct()
            .Where(c => !string.IsNullOrEmpty(c))
            .ToList();

        // Assert
        Assert.IsTrue(categories.Count <= 3, "Should have a reasonable number of categories");
        Assert.IsTrue(categories.Contains("Sqlx") || categories.Contains("Internal"),
            "Should contain expected categories");
    }

    [TestMethod]
    public void Messages_ParameterizedMessages_HaveCorrectFormat()
    {
        // Arrange & Act
        var sp0002 = Messages.SP0002; // Parameter type '{0}' is not supported
        var sp0004 = Messages.SP0004; // SQL command contains invalid syntax: '{0}'
        var sp0005 = Messages.SP0005; // Cannot map entity '{0}' to database table

        // Assert
        Assert.IsTrue(sp0002.MessageFormat.ToString().Contains("{0}"),
            "SP0002 should have a parameter placeholder");
        Assert.IsTrue(sp0004.MessageFormat.ToString().Contains("{0}"),
            "SP0004 should have a parameter placeholder");
        Assert.IsTrue(sp0005.MessageFormat.ToString().Contains("{0}"),
            "SP0005 should have a parameter placeholder");
    }

    [TestMethod]
    public void Messages_HelpMessages_AreInformative()
    {
        // Arrange
        var messagesType = typeof(Messages);
        var diagnosticProperties = messagesType.GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(DiagnosticDescriptor))
            .ToList();

        // Act & Assert
        foreach (var property in diagnosticProperties)
        {
            var diagnostic = property.GetValue(null) as DiagnosticDescriptor;
            Assert.IsNotNull(diagnostic);

            var description = diagnostic.Description.ToString();
            if (!string.IsNullOrEmpty(description))
            {
                Assert.IsTrue(description.Length > 10,
                    $"Description for {diagnostic.Id} should be informative (current: '{description}')");
            }
        }
    }

    [TestMethod]
    public void Messages_AllDiagnostics_AreEnabledByDefault()
    {
        // Arrange
        var messagesType = typeof(Messages);
        var diagnosticProperties = messagesType.GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(DiagnosticDescriptor))
            .ToList();

        // Act & Assert
        foreach (var property in diagnosticProperties)
        {
            var diagnostic = property.GetValue(null) as DiagnosticDescriptor;
            Assert.IsNotNull(diagnostic);
            Assert.IsTrue(diagnostic.IsEnabledByDefault,
                $"Diagnostic {diagnostic.Id} should be enabled by default");
        }
    }

    [TestMethod]
    public void Messages_Class_IsInternalAndStatic()
    {
        // Arrange
        var messagesType = typeof(Messages);

        // Act & Assert
        Assert.IsTrue(messagesType.IsClass, "Messages should be a class");
        Assert.IsTrue(messagesType.IsAbstract && messagesType.IsSealed, "Messages should be static");
        Assert.IsFalse(messagesType.IsPublic, "Messages should be internal");
    }

    [TestMethod]
    public void Messages_Properties_AreStaticAndReadonly()
    {
        // Arrange
        var messagesType = typeof(Messages);
        var diagnosticProperties = messagesType.GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(DiagnosticDescriptor))
            .ToList();

        // Act & Assert
        foreach (var property in diagnosticProperties)
        {
            Assert.AreEqual(true,
property.GetGetMethod()?.IsStatic, $"Property {property.Name} should be static");
            Assert.IsNull(property.GetSetMethod(),
                $"Property {property.Name} should be readonly");
        }
    }
}

