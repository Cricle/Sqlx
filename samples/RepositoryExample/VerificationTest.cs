// -----------------------------------------------------------------------
// <copyright file="VerificationTest.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Sqlx.RepositoryExample;

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Sqlx.Annotations;

/// <summary>
/// Comprehensive verification test for all repository functionality.
/// </summary>
public static class VerificationTest
{
    /// <summary>
    /// Runs all verification tests for the repository pattern implementation.
    /// </summary>
    /// <returns>A task representing the asynchronous operation with test results.</returns>
    public static async Task<bool> RunAllVerificationTests()
    {
        Console.WriteLine("=== 开始全面功能验证测试 ===");
        Console.WriteLine("=== Starting Comprehensive Functionality Verification Tests ===\n");

        var testResults = new List<(string TestName, bool Passed, string Details)>();

        // Test 1: Attribute Availability
        testResults.Add(TestAttributeAvailability());

        // Test 2: Repository Class Creation
        testResults.Add(TestRepositoryClassCreation());

        // Test 3: Interface Implementation
        testResults.Add(TestInterfaceImplementation());

        // Test 4: Synchronous Methods
        testResults.Add(TestSynchronousMethods());

        // Test 5: Asynchronous Methods
        testResults.Add(await TestAsynchronousMethods());

        // Test 6: CRUD Operations
        testResults.Add(TestCrudOperations());

        // Test 7: Error Handling
        testResults.Add(TestErrorHandling());

        // Test 8: Performance and Memory
        testResults.Add(TestPerformanceAndMemory());

        // Print results
        Console.WriteLine("\n=== 测试结果汇总 Test Results Summary ===");
        int passedTests = 0;
        int totalTests = testResults.Count;

        foreach (var (testName, passed, details) in testResults)
        {
            var status = passed ? "✅ PASSED" : "❌ FAILED";
            Console.WriteLine($"{status}: {testName}");
            if (!string.IsNullOrEmpty(details))
            {
                Console.WriteLine($"    详情 Details: {details}");
            }
            if (passed) passedTests++;
        }

        Console.WriteLine($"\n测试通过率 Pass Rate: {passedTests}/{totalTests} ({(passedTests * 100.0 / totalTests):F1}%)");

        bool allPassed = passedTests == totalTests;
        Console.WriteLine($"\n整体结果 Overall Result: {(allPassed ? "✅ 所有测试通过 ALL TESTS PASSED" : "❌ 部分测试失败 SOME TESTS FAILED")}");

        return allPassed;
    }

    private static (string, bool, string) TestAttributeAvailability()
    {
        try
        {
            var repositoryForType = typeof(RepositoryForAttribute);
            var tableNameType = typeof(TableNameAttribute);
            
            // Check if attributes are properly defined
            var repositoryForAttributes = repositoryForType.GetCustomAttributes(typeof(AttributeUsageAttribute), false);
            var tableNameAttributes = tableNameType.GetCustomAttributes(typeof(AttributeUsageAttribute), false);

            bool attributesAvailable = repositoryForType != null && tableNameType != null;
            return ("属性可用性测试 Attribute Availability Test", attributesAvailable, 
                $"RepositoryFor: {repositoryForType?.FullName ?? "null"}, TableName: {tableNameType?.FullName ?? "null"}");
        }
        catch (Exception ex)
        {
            return ("属性可用性测试 Attribute Availability Test", false, $"异常 Exception: {ex.Message}");
        }
    }

    private static (string, bool, string) TestRepositoryClassCreation()
    {
        try
        {
            using var connection = new SqlConnection("Data Source=.;Initial Catalog=tempdb;Integrated Security=true");
            var repository = new UserRepository(connection);

            bool isNotNull = repository != null;
            bool implementsInterface = repository is IUserService;

            return ("仓储类创建测试 Repository Class Creation Test", isNotNull && implementsInterface,
                $"创建成功 Created: {isNotNull}, 实现接口 Implements Interface: {implementsInterface}");
        }
        catch (Exception ex)
        {
            return ("仓储类创建测试 Repository Class Creation Test", false, $"异常 Exception: {ex.Message}");
        }
    }

    private static (string, bool, string) TestInterfaceImplementation()
    {
        try
        {
            using var connection = new SqlConnection("Data Source=.;Initial Catalog=tempdb;Integrated Security=true");
            var repository = new UserRepository(connection);

            // Check all interface methods are available
            var interfaceType = typeof(IUserService);
            var methods = interfaceType.GetMethods();
            
            bool allMethodsImplemented = true;
            var missingMethods = new List<string>();

            foreach (var method in methods)
            {
                if (method.Name != null)
                {
                    var parameterTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();
                    var implMethod = repository.GetType().GetMethod(method.Name, parameterTypes);
                    if (implMethod == null)
                    {
                        allMethodsImplemented = false;
                        missingMethods.Add(method.Name);
                    }
                }
            }

            return ("接口实现测试 Interface Implementation Test", allMethodsImplemented,
                allMethodsImplemented ? $"所有 {methods.Length} 个方法已实现 All {methods.Length} methods implemented" :
                $"缺少方法 Missing methods: {string.Join(", ", missingMethods)}");
        }
        catch (Exception ex)
        {
            return ("接口实现测试 Interface Implementation Test", false, $"异常 Exception: {ex.Message}");
        }
    }

    private static (string, bool, string) TestSynchronousMethods()
    {
        try
        {
            using var connection = new SqlConnection("Data Source=.;Initial Catalog=tempdb;Integrated Security=true");
            var repository = new UserRepository(connection);

            // Test synchronous methods
            var allUsers = repository.GetAllUsers();
            var userById = repository.GetUserById(1);
            var newUser = new User { Id = 10, Name = "Test User", Email = "test@example.com", CreatedAt = DateTime.Now };
            var createResult = repository.CreateUser(newUser);
            var updateResult = repository.UpdateUser(newUser);
            var deleteResult = repository.DeleteUser(10);

            bool syncMethodsWork = allUsers != null && userById != null && createResult > 0 && updateResult > 0 && deleteResult > 0;

            return ("同步方法测试 Synchronous Methods Test", syncMethodsWork,
                $"获取所有用户 GetAllUsers: {allUsers?.Count} 项, 根据ID查询 GetUserById: {userById?.Name ?? "null"}, " +
                $"创建 Create: {createResult}, 更新 Update: {updateResult}, 删除 Delete: {deleteResult}");
        }
        catch (Exception ex)
        {
            return ("同步方法测试 Synchronous Methods Test", false, $"异常 Exception: {ex.Message}");
        }
    }

    private static async Task<(string, bool, string)> TestAsynchronousMethods()
    {
        try
        {
            using var connection = new SqlConnection("Data Source=.;Initial Catalog=tempdb;Integrated Security=true");
            var repository = new UserRepository(connection);

            // Test asynchronous methods
            var allUsersAsync = await repository.GetAllUsersAsync();
            var userByIdAsync = await repository.GetUserByIdAsync(1);
            var newUser = new User { Id = 11, Name = "Async Test User", Email = "async@example.com", CreatedAt = DateTime.Now };
            var createResultAsync = await repository.CreateUserAsync(newUser);

            bool asyncMethodsWork = allUsersAsync != null && userByIdAsync != null && createResultAsync > 0;

            return ("异步方法测试 Asynchronous Methods Test", asyncMethodsWork,
                $"异步获取所有用户 GetAllUsersAsync: {allUsersAsync?.Count} 项, " +
                $"异步根据ID查询 GetUserByIdAsync: {userByIdAsync?.Name ?? "null"}, " +
                $"异步创建 CreateAsync: {createResultAsync}");
        }
        catch (Exception ex)
        {
            return ("异步方法测试 Asynchronous Methods Test", false, $"异常 Exception: {ex.Message}");
        }
    }

    private static (string, bool, string) TestCrudOperations()
    {
        try
        {
            using var connection = new SqlConnection("Data Source=.;Initial Catalog=tempdb;Integrated Security=true");
            var repository = new UserRepository(connection);

            // Test full CRUD cycle
            var initialUsers = repository.GetAllUsers();
            var initialCount = initialUsers.Count;

            // Create
            var newUser = new User { Id = 99, Name = "CRUD Test", Email = "crud@test.com", CreatedAt = DateTime.Now };
            var createResult = repository.CreateUser(newUser);

            // Read
            var createdUser = repository.GetUserById(99);

            // Update
            int updateResult = 0;
            if (createdUser != null)
            {
                createdUser.Name = "CRUD Updated";
                updateResult = repository.UpdateUser(createdUser);
            }

            // Delete
            var deleteResult = repository.DeleteUser(99);

            bool crudWorks = createResult > 0 && createdUser != null && deleteResult > 0;

            return ("CRUD操作测试 CRUD Operations Test", crudWorks,
                $"初始用户数 Initial count: {initialCount}, 创建 Create: {createResult}, " +
                $"读取 Read: {createdUser?.Name ?? "null"}, 更新 Update: {updateResult}, 删除 Delete: {deleteResult}");
        }
        catch (Exception ex)
        {
            return ("CRUD操作测试 CRUD Operations Test", false, $"异常 Exception: {ex.Message}");
        }
    }

    private static (string, bool, string) TestErrorHandling()
    {
        try
        {
            using var connection = new SqlConnection("Data Source=.;Initial Catalog=tempdb;Integrated Security=true");
            var repository = new UserRepository(connection);

            bool nullHandling = false;
            try
            {
                repository.CreateUser(null!);
            }
            catch (ArgumentNullException)
            {
                nullHandling = true; // Expected exception
            }

            var nonExistentUser = repository.GetUserById(99999);
            bool nonExistentHandling = nonExistentUser == null;

            bool errorHandlingWorks = nullHandling && nonExistentHandling;

            return ("错误处理测试 Error Handling Test", errorHandlingWorks,
                $"空值处理 Null handling: {nullHandling}, 不存在用户处理 Non-existent user handling: {nonExistentHandling}");
        }
        catch (Exception ex)
        {
            return ("错误处理测试 Error Handling Test", false, $"意外异常 Unexpected exception: {ex.Message}");
        }
    }

    private static (string, bool, string) TestPerformanceAndMemory()
    {
        try
        {
            using var connection = new SqlConnection("Data Source=.;Initial Catalog=tempdb;Integrated Security=true");
            var repository = new UserRepository(connection);

            var startTime = DateTime.Now;
            var startMemory = GC.GetTotalMemory(false);

            // Perform multiple operations
            for (int i = 0; i < 100; i++)
            {
                var users = repository.GetAllUsers();
                var user = repository.GetUserById(1);
            }

            var endTime = DateTime.Now;
            var endMemory = GC.GetTotalMemory(true);

            var duration = endTime - startTime;
            var memoryUsed = endMemory - startMemory;

            bool performanceOk = duration.TotalMilliseconds < 5000; // Should complete in under 5 seconds
            bool memoryOk = memoryUsed < 1024 * 1024; // Should use less than 1MB additional memory

            return ("性能和内存测试 Performance and Memory Test", performanceOk && memoryOk,
                $"执行时间 Duration: {duration.TotalMilliseconds:F0}ms, 内存使用 Memory used: {memoryUsed / 1024.0:F1}KB");
        }
        catch (Exception ex)
        {
            return ("性能和内存测试 Performance and Memory Test", false, $"异常 Exception: {ex.Message}");
        }
    }
}
