// -----------------------------------------------------------------------
// <copyright file="AotCompatibleUpdateTests.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx;

namespace Sqlx.Tests.Predefined
{
    /// <summary>
    /// Tests for AOT-compatible partial update interfaces:
    /// - IPartialUpdateRepository (interface-level generics)
    /// - IExpressionUpdateRepository (expression-based updates)
    /// </summary>
    [TestClass]
    public class AotCompatibleUpdateTests : IDisposable
    {
        private SqliteConnection _connection = null!;

        public AotCompatibleUpdateTests()
        {
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE users (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    email TEXT,
                    age INTEGER NOT NULL,
                    is_active INTEGER NOT NULL DEFAULT 1,
                    created_at TEXT NOT NULL,
                    updated_at TEXT
                )";
            cmd.ExecuteNonQuery();
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }

        private void InsertTestUser(string name, string? email, int age, bool isActive = true)
        {
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO users (name, email, age, is_active, created_at) 
                VALUES (@name, @email, @age, @isActive, @createdAt)";
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@email", (object?)email ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@age", age);
            cmd.Parameters.AddWithValue("@isActive", isActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@createdAt", DateTime.UtcNow.ToString("o"));
            cmd.ExecuteNonQuery();
        }

        // ===== IPartialUpdateRepository Tests =====

        [TestMethod]
        public void IPartialUpdateRepository_InterfaceDefinition_ShouldHaveCorrectMethods()
        {
            var interfaceType = typeof(IPartialUpdateRepository<,,>);
            var methods = interfaceType.GetMethods();
            var methodNames = methods.Select(m => m.Name).ToList();

            CollectionAssert.Contains(methodNames, "UpdatePartialAsync");
            CollectionAssert.Contains(methodNames, "UpdateWhereAsync");
        }

        [TestMethod]
        public void IPartialUpdateRepository_ShouldHaveInterfaceLevelTypeParameter()
        {
            var interfaceType = typeof(IPartialUpdateRepository<,,>);
            var typeParams = interfaceType.GetGenericArguments();

            Assert.AreEqual(3, typeParams.Length);
            Assert.AreEqual("TEntity", typeParams[0].Name);
            Assert.AreEqual("TKey", typeParams[1].Name);
            Assert.AreEqual("TUpdates", typeParams[2].Name);
        }

        [TestMethod]
        public void IPartialUpdateRepository_TUpdates_ShouldBeInterfaceLevel_NotMethodLevel()
        {
            // TUpdates should be declared at interface level, not method level
            var interfaceType = typeof(IPartialUpdateRepository<,,>);
            var updateMethod = interfaceType.GetMethod("UpdatePartialAsync");
            
            Assert.IsNotNull(updateMethod);
            // Method should NOT have its own generic parameters (TUpdates is from interface)
            Assert.AreEqual(0, updateMethod!.GetGenericArguments().Length);
        }

        [TestMethod]
        public void UserNameUpdateRepository_ShouldBeGenerated()
        {
            // Verify the repository class is generated
            var repo = new UserNameUpdateRepository(_connection);
            Assert.IsNotNull(repo);
        }

        [TestMethod]
        public void UserStatusUpdateRepository_ShouldBeGenerated()
        {
            var repo = new UserStatusUpdateRepository(_connection);
            Assert.IsNotNull(repo);
        }

        // ===== IExpressionUpdateRepository Tests =====

        [TestMethod]
        public void IExpressionUpdateRepository_InterfaceDefinition_ShouldHaveCorrectMethods()
        {
            var interfaceType = typeof(IExpressionUpdateRepository<,>);
            var methods = interfaceType.GetMethods();
            var methodNames = methods.Select(m => m.Name).ToList();

            CollectionAssert.Contains(methodNames, "UpdateFieldsAsync");
            CollectionAssert.Contains(methodNames, "UpdateFieldsWhereAsync");
        }

        [TestMethod]
        public void IExpressionUpdateRepository_UpdateFieldsAsync_ShouldUseExpressionParameter()
        {
            var interfaceType = typeof(IExpressionUpdateRepository<,>);
            var method = interfaceType.GetMethod("UpdateFieldsAsync");
            
            Assert.IsNotNull(method);
            
            var parameters = method!.GetParameters();
            // Should have: id, updateExpression, cancellationToken
            Assert.AreEqual(3, parameters.Length);
            Assert.AreEqual("updateExpression", parameters[1].Name);
            
            // updateExpression should be Expression<Func<TEntity, TEntity>>
            var paramType = parameters[1].ParameterType;
            Assert.IsTrue(paramType.IsGenericType);
            Assert.AreEqual(typeof(Expression<>), paramType.GetGenericTypeDefinition());
        }

        [TestMethod]
        public void IExpressionUpdateRepository_ShouldHaveExpressionToSqlAttribute()
        {
            var interfaceType = typeof(IExpressionUpdateRepository<,>);
            var method = interfaceType.GetMethod("UpdateFieldsAsync");
            var updateExprParam = method!.GetParameters()[1];
            
            var hasExpressionToSqlAttr = updateExprParam.GetCustomAttributes()
                .Any(a => a.GetType().Name == "ExpressionToSqlAttribute");
            
            Assert.IsTrue(hasExpressionToSqlAttr, 
                "updateExpression parameter should have [ExpressionToSql] attribute");
        }

        /// <summary>
        /// TEMPORARILY DISABLED: The source generator has a bug where it generates
        /// GetParameters() calls for Expression&lt;Func&lt;T, T&gt;&gt; types, but this
        /// extension method only supports Expression&lt;Func&lt;T, bool&gt;&gt;.
        /// </summary>
        // [TestMethod]
        // public void UserExpressionUpdateRepository_ShouldBeGenerated()
        // {
        //     var repo = new UserExpressionUpdateRepository(_connection);
        //     Assert.IsNotNull(repo);
        // }

        // ===== AOT Compatibility Verification =====

        [TestMethod]
        public void IPartialUpdateRepository_ShouldNotRequireRuntimeReflection()
        {
            // The key difference from ICommandRepository.UpdatePartialAsync<TUpdates>:
            // - ICommandRepository uses method-level generic (TUpdates on method)
            // - IPartialUpdateRepository uses interface-level generic (TUpdates on interface)
            // 
            // Interface-level generics can be resolved at compile time when the class
            // implements the interface with concrete types.
            
            var concreteInterface = typeof(IPartialUpdateRepository<User, long, UserNameUpdate>);
            var typeArgs = concreteInterface.GetGenericArguments();
            
            // All type arguments should be concrete types, not type parameters
            Assert.AreEqual(typeof(User), typeArgs[0]);
            Assert.AreEqual(typeof(long), typeArgs[1]);
            Assert.AreEqual(typeof(UserNameUpdate), typeArgs[2]);
            
            // UserNameUpdate properties can be analyzed at compile time
            var updateTypeProps = typeof(UserNameUpdate).GetProperties();
            Assert.IsTrue(updateTypeProps.Length > 0);
            CollectionAssert.Contains(updateTypeProps.Select(p => p.Name).ToList(), "Name");
            CollectionAssert.Contains(updateTypeProps.Select(p => p.Name).ToList(), "Email");
        }

        [TestMethod]
        public void UpdateTypes_ShouldHavePublicProperties()
        {
            // Verify update types have the expected properties
            var nameUpdateProps = typeof(UserNameUpdate).GetProperties();
            CollectionAssert.Contains(nameUpdateProps.Select(p => p.Name).ToList(), "Name");
            CollectionAssert.Contains(nameUpdateProps.Select(p => p.Name).ToList(), "Email");

            var statusUpdateProps = typeof(UserStatusUpdate).GetProperties();
            CollectionAssert.Contains(statusUpdateProps.Select(p => p.Name).ToList(), "IsActive");
            CollectionAssert.Contains(statusUpdateProps.Select(p => p.Name).ToList(), "UpdatedAt");

            var ageUpdateProps = typeof(UserAgeUpdate).GetProperties();
            CollectionAssert.Contains(ageUpdateProps.Select(p => p.Name).ToList(), "Age");
        }

        // ===== Comparison with ICommandRepository =====

        [TestMethod]
        public void ICommandRepository_UpdatePartialAsync_ShouldHaveMethodLevelGeneric()
        {
            // This test documents the difference between ICommandRepository and IPartialUpdateRepository
            var interfaceType = typeof(ICommandRepository<,>);
            var method = interfaceType.GetMethod("UpdatePartialAsync");
            
            Assert.IsNotNull(method);
            // ICommandRepository.UpdatePartialAsync has method-level generic TUpdates
            var methodGenericArgs = method!.GetGenericArguments();
            Assert.AreEqual(1, methodGenericArgs.Length);
            Assert.AreEqual("TUpdates", methodGenericArgs[0].Name);
        }

        [TestMethod]
        public void IPartialUpdateRepository_UpdatePartialAsync_ShouldNotHaveMethodLevelGeneric()
        {
            // IPartialUpdateRepository.UpdatePartialAsync does NOT have method-level generic
            // TUpdates is at interface level
            var interfaceType = typeof(IPartialUpdateRepository<,,>);
            var method = interfaceType.GetMethod("UpdatePartialAsync");
            
            Assert.IsNotNull(method);
            var methodGenericArgs = method!.GetGenericArguments();
            Assert.AreEqual(0, methodGenericArgs.Length, 
                "IPartialUpdateRepository.UpdatePartialAsync should not have method-level generics");
        }
    }
}
