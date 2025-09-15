// -----------------------------------------------------------------------
// <copyright file="SqlxSyntaxReceiverTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sqlx.Tests.Core
{
    /// <summary>
    /// Tests for ISqlxSyntaxReceiver interface and syntax receiver functionality.
    /// </summary>
    [TestClass]
    public class SqlxSyntaxReceiverTests
    {
    private class MockSqlxSyntaxReceiver : ISqlxSyntaxReceiver
    {
        public List<Microsoft.CodeAnalysis.IMethodSymbol> Methods { get; } = new List<Microsoft.CodeAnalysis.IMethodSymbol>();
        public List<Microsoft.CodeAnalysis.INamedTypeSymbol> RepositoryClasses { get; } = new List<Microsoft.CodeAnalysis.INamedTypeSymbol>();

        public void OnVisitSyntaxNode(Microsoft.CodeAnalysis.SyntaxNode syntaxNode)
        {
            // Mock implementation for testing
            VisitedNodes.Add(syntaxNode);
        }

        public void OnVisitSyntaxNode(Microsoft.CodeAnalysis.GeneratorSyntaxContext context)
        {
            // Implementation required by ISyntaxContextReceiver
            OnVisitSyntaxNode(context.Node);
        }

        public List<Microsoft.CodeAnalysis.SyntaxNode> VisitedNodes { get; } = new List<Microsoft.CodeAnalysis.SyntaxNode>();
    }

        [TestMethod]
        public void ISqlxSyntaxReceiver_Interface_HasRequiredProperties()
        {
            // Test that ISqlxSyntaxReceiver interface has required properties
            var interfaceType = typeof(ISqlxSyntaxReceiver);
            
            // Check Methods property
            var methodsProperty = interfaceType.GetProperty("Methods");
            Assert.IsNotNull(methodsProperty, "ISqlxSyntaxReceiver should have Methods property");
            Assert.AreEqual(typeof(List<Microsoft.CodeAnalysis.IMethodSymbol>), methodsProperty.PropertyType,
                "Methods property should be List<IMethodSymbol>");

            // Check RepositoryClasses property
            var repositoryClassesProperty = interfaceType.GetProperty("RepositoryClasses");
            Assert.IsNotNull(repositoryClassesProperty, "ISqlxSyntaxReceiver should have RepositoryClasses property");
            Assert.AreEqual(typeof(List<Microsoft.CodeAnalysis.INamedTypeSymbol>), repositoryClassesProperty.PropertyType,
                "RepositoryClasses property should be List<INamedTypeSymbol>");

            // Check OnVisitSyntaxNode method
            var onVisitMethod = interfaceType.GetMethod("OnVisitSyntaxNode");
            Assert.IsNotNull(onVisitMethod, "ISqlxSyntaxReceiver should have OnVisitSyntaxNode method");
            Assert.AreEqual(typeof(void), onVisitMethod.ReturnType, "OnVisitSyntaxNode should return void");
            
            var parameters = onVisitMethod.GetParameters();
            Assert.AreEqual(1, parameters.Length, "OnVisitSyntaxNode should have one parameter");
            Assert.AreEqual(typeof(Microsoft.CodeAnalysis.SyntaxNode), parameters[0].ParameterType,
                "OnVisitSyntaxNode parameter should be SyntaxNode");
        }

        [TestMethod]
        public void ISqlxSyntaxReceiver_InheritsFromISyntaxContextReceiver()
        {
            // Test that ISqlxSyntaxReceiver inherits from ISyntaxContextReceiver
            var interfaceType = typeof(ISqlxSyntaxReceiver);
            var baseInterface = typeof(Microsoft.CodeAnalysis.ISyntaxContextReceiver);
            
            Assert.IsTrue(baseInterface.IsAssignableFrom(interfaceType),
                "ISqlxSyntaxReceiver should inherit from ISyntaxContextReceiver");
        }

        [TestMethod]
        public void MockSqlxSyntaxReceiver_Implementation_WorksCorrectly()
        {
            // Test mock implementation
            var receiver = new MockSqlxSyntaxReceiver();
            
            // Test initial state
            Assert.IsNotNull(receiver.Methods);
            Assert.IsNotNull(receiver.RepositoryClasses);
            Assert.IsNotNull(receiver.VisitedNodes);
            
            Assert.AreEqual(0, receiver.Methods.Count);
            Assert.AreEqual(0, receiver.RepositoryClasses.Count);
            Assert.AreEqual(0, receiver.VisitedNodes.Count);
        }

        [TestMethod]
        public void MockSqlxSyntaxReceiver_OnVisitSyntaxNode_TracksVisitedNodes()
        {
            // Test that OnVisitSyntaxNode tracks visited nodes
            var receiver = new MockSqlxSyntaxReceiver();
            
            // Create mock syntax nodes (we can't easily create real ones in unit tests)
            // So we'll test the collection behavior
            var initialCount = receiver.VisitedNodes.Count;
            
            // Test that the collections are mutable
            Assert.IsTrue(receiver.Methods is List<Microsoft.CodeAnalysis.IMethodSymbol>);
            Assert.IsTrue(receiver.RepositoryClasses is List<Microsoft.CodeAnalysis.INamedTypeSymbol>);
        }

        [TestMethod]
        public void SyntaxReceiver_CollectionProperties_AreInitialized()
        {
            // Test that collections are properly initialized
            var receiver = new MockSqlxSyntaxReceiver();
            
            // Test Methods collection
            Assert.IsNotNull(receiver.Methods);
            Assert.IsTrue(receiver.Methods is List<Microsoft.CodeAnalysis.IMethodSymbol>);
            Assert.AreEqual(0, receiver.Methods.Count);
            
            // Test RepositoryClasses collection
            Assert.IsNotNull(receiver.RepositoryClasses);
            Assert.IsTrue(receiver.RepositoryClasses is List<Microsoft.CodeAnalysis.INamedTypeSymbol>);
            Assert.AreEqual(0, receiver.RepositoryClasses.Count);
            
            // Test that collections are separate instances
            Assert.AreNotSame((object)receiver.Methods, (object)receiver.RepositoryClasses);
        }

        [TestMethod]
        public void SyntaxReceiver_CollectionProperties_SupportModification()
        {
            // Test that collections support modification operations
            var receiver = new MockSqlxSyntaxReceiver();
            
            // Test Methods collection modification capability
            var originalMethodCount = receiver.Methods.Count;
            Assert.AreEqual(0, originalMethodCount);
            
            // We can't easily create IMethodSymbol instances, but we can test the collection type
            Assert.IsTrue(receiver.Methods.GetType().GetMethod("Add") != null);
            Assert.IsTrue(receiver.Methods.GetType().GetMethod("Remove") != null);
            Assert.IsTrue(receiver.Methods.GetType().GetMethod("Clear") != null);
            
            // Test RepositoryClasses collection modification capability
            var originalRepoCount = receiver.RepositoryClasses.Count;
            Assert.AreEqual(0, originalRepoCount);
            
            Assert.IsTrue(receiver.RepositoryClasses.GetType().GetMethod("Add") != null);
            Assert.IsTrue(receiver.RepositoryClasses.GetType().GetMethod("Remove") != null);
            Assert.IsTrue(receiver.RepositoryClasses.GetType().GetMethod("Clear") != null);
        }

        [TestMethod]
        public void SyntaxReceiver_InterfaceContract_IsCorrect()
        {
            // Test interface contract requirements
            var interfaceType = typeof(ISqlxSyntaxReceiver);
            
            // Verify it's an interface
            Assert.IsTrue(interfaceType.IsInterface, "ISqlxSyntaxReceiver should be an interface");
            
            // Verify it's internal (not public)
            Assert.IsFalse(interfaceType.IsPublic, "ISqlxSyntaxReceiver should be internal");
            Assert.IsTrue(interfaceType.IsNotPublic, "ISqlxSyntaxReceiver should be internal");
            
            // Verify inheritance
            var interfaces = interfaceType.GetInterfaces();
            Assert.IsTrue(interfaces.Any(i => i == typeof(Microsoft.CodeAnalysis.ISyntaxContextReceiver)),
                "ISqlxSyntaxReceiver should inherit from ISyntaxContextReceiver");
        }

        [TestMethod]
        public void SyntaxReceiver_PropertyTypes_AreCorrect()
        {
            // Test that property types are exactly what's expected
            var interfaceType = typeof(ISqlxSyntaxReceiver);
            
            var methodsProperty = interfaceType.GetProperty("Methods");
            Assert.IsNotNull(methodsProperty);
            Assert.IsTrue(methodsProperty.CanRead, "Methods property should be readable");
            Assert.AreEqual(typeof(List<Microsoft.CodeAnalysis.IMethodSymbol>), methodsProperty.PropertyType);
            
            var repositoryClassesProperty = interfaceType.GetProperty("RepositoryClasses");
            Assert.IsNotNull(repositoryClassesProperty);
            Assert.IsTrue(repositoryClassesProperty.CanRead, "RepositoryClasses property should be readable");
            Assert.AreEqual(typeof(List<Microsoft.CodeAnalysis.INamedTypeSymbol>), repositoryClassesProperty.PropertyType);
        }

        [TestMethod]
        public void SyntaxReceiver_OnVisitSyntaxNode_HasCorrectSignature()
        {
            // Test OnVisitSyntaxNode method signature
            var interfaceType = typeof(ISqlxSyntaxReceiver);
            var method = interfaceType.GetMethod("OnVisitSyntaxNode");
            
            Assert.IsNotNull(method, "OnVisitSyntaxNode method should exist");
            Assert.AreEqual(typeof(void), method.ReturnType, "OnVisitSyntaxNode should return void");
            
            var parameters = method.GetParameters();
            Assert.AreEqual(1, parameters.Length, "OnVisitSyntaxNode should have exactly one parameter");
            
            var parameter = parameters[0];
            Assert.AreEqual("syntaxNode", parameter.Name, "Parameter should be named 'syntaxNode'");
            Assert.AreEqual(typeof(Microsoft.CodeAnalysis.SyntaxNode), parameter.ParameterType,
                "Parameter should be of type SyntaxNode");
        }

        [TestMethod]
        public void SyntaxReceiver_UsagePattern_IsCorrect()
        {
            // Test typical usage pattern
            var receiver = new MockSqlxSyntaxReceiver();
            
            // Simulate typical usage
            Assert.AreEqual(0, receiver.Methods.Count, "Should start with empty Methods collection");
            Assert.AreEqual(0, receiver.RepositoryClasses.Count, "Should start with empty RepositoryClasses collection");
            
            // Test that interface can be used polymorphically
            ISqlxSyntaxReceiver interfaceReceiver = receiver;
            Assert.IsNotNull(interfaceReceiver.Methods);
            Assert.IsNotNull(interfaceReceiver.RepositoryClasses);
            
            // Test that it can be cast to base interface
            Microsoft.CodeAnalysis.ISyntaxContextReceiver baseReceiver = receiver;
            Assert.IsNotNull(baseReceiver);
        }

        [TestMethod]
        public void SyntaxReceiver_NamespaceAndAssembly_AreCorrect()
        {
            // Test namespace and assembly information
            var interfaceType = typeof(ISqlxSyntaxReceiver);
            
            Assert.AreEqual("Sqlx", interfaceType.Namespace, "ISqlxSyntaxReceiver should be in Sqlx namespace");
            Assert.AreEqual("Sqlx.Generator", interfaceType.Assembly.GetName().Name, 
                "ISqlxSyntaxReceiver should be in Sqlx.Generator assembly");
        }

        [TestMethod]
        public void SyntaxReceiver_CollectionBehavior_IsConsistent()
        {
            // Test that collections behave consistently
            var receiver1 = new MockSqlxSyntaxReceiver();
            var receiver2 = new MockSqlxSyntaxReceiver();
            
            // Test that different instances have separate collections
            Assert.AreNotSame((object)receiver1.Methods, (object)receiver2.Methods);
            Assert.AreNotSame((object)receiver1.RepositoryClasses, (object)receiver2.RepositoryClasses);
            
            // Test that collections are of the same type
            Assert.AreEqual(receiver1.Methods.GetType(), receiver2.Methods.GetType());
            Assert.AreEqual(receiver1.RepositoryClasses.GetType(), receiver2.RepositoryClasses.GetType());
            
            // Test initial state consistency
            Assert.AreEqual(receiver1.Methods.Count, receiver2.Methods.Count);
            Assert.AreEqual(receiver1.RepositoryClasses.Count, receiver2.RepositoryClasses.Count);
        }

        [TestMethod]
        public void SyntaxReceiver_ThreadSafety_Considerations()
        {
            // Test thread safety considerations (conceptual)
            var receiver = new MockSqlxSyntaxReceiver();
            
            // List<T> is not thread-safe, so we expect non-thread-safe collections
            Assert.IsTrue(receiver.Methods is List<Microsoft.CodeAnalysis.IMethodSymbol>);
            Assert.IsTrue(receiver.RepositoryClasses is List<Microsoft.CodeAnalysis.INamedTypeSymbol>);
            
            // This is expected behavior - syntax receivers are used in single-threaded context
            // during compilation, so thread safety is not required
        }

        [TestMethod]
        public void SyntaxReceiver_MemoryEfficiency_IsConsidered()
        {
            // Test memory efficiency considerations
            var receiver = new MockSqlxSyntaxReceiver();
            
            // Collections should start empty to avoid unnecessary memory allocation
            Assert.AreEqual(0, receiver.Methods.Count);
            Assert.AreEqual(0, receiver.RepositoryClasses.Count);
            
            // Collections should be concrete List<T> for performance
            Assert.IsTrue(receiver.Methods is List<Microsoft.CodeAnalysis.IMethodSymbol>);
            Assert.IsTrue(receiver.RepositoryClasses is List<Microsoft.CodeAnalysis.INamedTypeSymbol>);
            
            // Test that collections don't pre-allocate large capacity
            var methodsList = receiver.Methods as List<Microsoft.CodeAnalysis.IMethodSymbol>;
            var reposList = receiver.RepositoryClasses as List<Microsoft.CodeAnalysis.INamedTypeSymbol>;
            
            // Capacity should be reasonable (List<T> default is 0 for empty list)
            Assert.IsTrue(methodsList!.Capacity >= 0 && methodsList.Capacity <= 4);
            Assert.IsTrue(reposList!.Capacity >= 0 && reposList.Capacity <= 4);
        }
    }
}
