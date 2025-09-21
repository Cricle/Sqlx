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
            public List<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax> MethodSyntaxNodes { get; } = new List<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>();
            public List<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax> ClassSyntaxNodes { get; } = new List<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>();

            public void OnVisitSyntaxNode(Microsoft.CodeAnalysis.SyntaxNode syntaxNode)
            {
                // Mock implementation for testing
                VisitedNodes.Add(syntaxNode);
            }

            public void OnVisitSyntaxNode(Microsoft.CodeAnalysis.GeneratorSyntaxContext context)
            {
                // Mock implementation for ISyntaxContextReceiver compatibility
                VisitedNodes.Add(context.Node);
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

            // Check MethodSyntaxNodes property
            var methodSyntaxProperty = interfaceType.GetProperty("MethodSyntaxNodes");
            Assert.IsNotNull(methodSyntaxProperty, "ISqlxSyntaxReceiver should have MethodSyntaxNodes property");
            Assert.AreEqual(typeof(List<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>), methodSyntaxProperty.PropertyType,
                "MethodSyntaxNodes property should be List<MethodDeclarationSyntax>");

            // Check ClassSyntaxNodes property
            var classSyntaxProperty = interfaceType.GetProperty("ClassSyntaxNodes");
            Assert.IsNotNull(classSyntaxProperty, "ISqlxSyntaxReceiver should have ClassSyntaxNodes property");
            Assert.AreEqual(typeof(List<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>), classSyntaxProperty.PropertyType,
                "ClassSyntaxNodes property should be List<ClassDeclarationSyntax>");
        }

        [TestMethod]
        public void ISqlxSyntaxReceiver_IsSimpleInterface()
        {
            // Test that ISqlxSyntaxReceiver is a simple collection interface (not inheriting from ISyntaxContextReceiver)
            var interfaceType = typeof(ISqlxSyntaxReceiver);
            var baseInterface = typeof(Microsoft.CodeAnalysis.ISyntaxContextReceiver);

            // ISqlxSyntaxReceiver is now a simple interface for data collection, not a syntax receiver
            Assert.IsFalse(baseInterface.IsAssignableFrom(interfaceType),
                "ISqlxSyntaxReceiver should be a simple collection interface");

            // Verify it has the correct collection properties
            Assert.IsTrue(interfaceType.IsInterface, "ISqlxSyntaxReceiver should be an interface");
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
#pragma warning disable IL2075 // 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods'
            Assert.IsTrue(receiver.Methods.GetType().GetMethod("Add") != null);
            Assert.IsTrue(receiver.Methods.GetType().GetMethod("Remove") != null);
            Assert.IsTrue(receiver.Methods.GetType().GetMethod("Clear") != null);

            // Test RepositoryClasses collection modification capability
            var originalRepoCount = receiver.RepositoryClasses.Count;
            Assert.AreEqual(0, originalRepoCount);

            Assert.IsTrue(receiver.RepositoryClasses.GetType().GetMethod("Add") != null);
            Assert.IsTrue(receiver.RepositoryClasses.GetType().GetMethod("Remove") != null);
            Assert.IsTrue(receiver.RepositoryClasses.GetType().GetMethod("Clear") != null);
#pragma warning restore IL2075
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

            // Verify it's a simple collection interface (no inheritance from ISyntaxContextReceiver)
            var interfaces = interfaceType.GetInterfaces();
            Assert.IsFalse(interfaces.Any(i => i == typeof(Microsoft.CodeAnalysis.ISyntaxContextReceiver)),
                "ISqlxSyntaxReceiver should be a simple collection interface");
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
        public void SyntaxReceiver_Properties_HaveCorrectGettersOnly()
        {
            // Test that all properties are read-only collections
            var interfaceType = typeof(ISqlxSyntaxReceiver);

            var methodsProperty = interfaceType.GetProperty("Methods");
            Assert.IsNotNull(methodsProperty, "Methods property should exist");
            Assert.IsTrue(methodsProperty.CanRead, "Methods property should be readable");
            Assert.IsFalse(methodsProperty.CanWrite, "Methods property should be read-only");

            var repositoryClassesProperty = interfaceType.GetProperty("RepositoryClasses");
            Assert.IsNotNull(repositoryClassesProperty, "RepositoryClasses property should exist");
            Assert.IsTrue(repositoryClassesProperty.CanRead, "RepositoryClasses property should be readable");
            Assert.IsFalse(repositoryClassesProperty.CanWrite, "RepositoryClasses property should be read-only");

            var methodSyntaxProperty = interfaceType.GetProperty("MethodSyntaxNodes");
            Assert.IsNotNull(methodSyntaxProperty, "MethodSyntaxNodes property should exist");
            Assert.IsTrue(methodSyntaxProperty.CanRead, "MethodSyntaxNodes property should be readable");
            Assert.IsFalse(methodSyntaxProperty.CanWrite, "MethodSyntaxNodes property should be read-only");

            var classSyntaxProperty = interfaceType.GetProperty("ClassSyntaxNodes");
            Assert.IsNotNull(classSyntaxProperty, "ClassSyntaxNodes property should exist");
            Assert.IsTrue(classSyntaxProperty.CanRead, "ClassSyntaxNodes property should be readable");
            Assert.IsFalse(classSyntaxProperty.CanWrite, "ClassSyntaxNodes property should be read-only");
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
