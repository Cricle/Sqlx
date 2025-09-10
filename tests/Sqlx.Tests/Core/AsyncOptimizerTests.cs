using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Sqlx.Core;

namespace Sqlx.Tests.Core
{
    [TestClass]
    public class AsyncOptimizerTests
    {
        #region AsyncOptimizer Tests

        [TestMethod]
        public void AsyncOptimizer_GetCompletedTask_ShouldReturnCompletedTask()
        {
            // Act
            var task = AsyncOptimizer.GetCompletedTask();

            // Assert
            Assert.IsNotNull(task);
            Assert.IsTrue(task.IsCompleted);
            Assert.IsFalse(task.IsFaulted);
            Assert.IsFalse(task.IsCanceled);
        }

        [TestMethod]
        public void AsyncOptimizer_GetCompletedTaskWithResult_ShouldReturnCompletedTaskWithCorrectResult()
        {
            // Arrange
            var expectedResult = 42;

            // Act
            var task = AsyncOptimizer.GetCompletedTask(expectedResult);

            // Assert
            Assert.IsNotNull(task);
            Assert.IsTrue(task.IsCompleted);
            Assert.IsFalse(task.IsFaulted);
            Assert.IsFalse(task.IsCanceled);
            Assert.AreEqual(expectedResult, task.Result);
        }

        [TestMethod]
        public void AsyncOptimizer_GetCompletedTaskWithStringResult_ShouldReturnCompletedTaskWithCorrectResult()
        {
            // Arrange
            var expectedResult = "test result";

            // Act
            var task = AsyncOptimizer.GetCompletedTask(expectedResult);

            // Assert
            Assert.IsNotNull(task);
            Assert.IsTrue(task.IsCompleted);
            Assert.AreEqual(expectedResult, task.Result);
        }

        [TestMethod]
        public async Task AsyncOptimizer_ConfigureAwaitOptimized_ShouldConfigureCorrectly()
        {
            // Arrange
            var task = Task.CompletedTask;

            // Act & Assert - Should not throw and complete successfully
            await task.ConfigureAwaitOptimized();
        }

        [TestMethod]
        public async Task AsyncOptimizer_ConfigureAwaitOptimizedWithResult_ShouldConfigureCorrectly()
        {
            // Arrange
            var expectedResult = 123;
            var task = Task.FromResult(expectedResult);

            // Act
            var result = await task.ConfigureAwaitOptimized();

            // Assert
            Assert.AreEqual(expectedResult, result);
        }

        [TestMethod]
        public void AsyncOptimizer_IsCancelledSafe_WithNonCancelledToken_ShouldReturnFalse()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;

            // Act
            var result = cancellationToken.IsCancelledSafe();

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void AsyncOptimizer_IsCancelledSafe_WithCancelledToken_ShouldReturnTrue()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            var result = cts.Token.IsCancelledSafe();

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void AsyncOptimizer_ThrowIfCancelledOptimized_WithNonCancelledToken_ShouldNotThrow()
        {
            // Arrange
            var cancellationToken = CancellationToken.None;

            // Act & Assert - Should not throw
            cancellationToken.ThrowIfCancelledOptimized();
        }

        [TestMethod]
        [ExpectedException(typeof(OperationCanceledException))]
        public void AsyncOptimizer_ThrowIfCancelledOptimized_WithCancelledToken_ShouldThrow()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act
            cts.Token.ThrowIfCancelledOptimized();
        }

        #endregion

        #region AsyncCodeGenerator Tests

        [TestMethod]
        public void AsyncCodeGenerator_GenerateOptimizedAsyncSignature_WithTaskReturn_ShouldGenerateValueTaskSignature()
        {
            // Arrange
            var sb = new IndentedStringBuilder(string.Empty);
            var returnType = "Task<int>";
            var methodName = "GetDataAsync";
            var parameters = "string id, CancellationToken cancellationToken";

            // Act
            AsyncCodeGenerator.GenerateOptimizedAsyncSignature(sb, returnType, methodName, parameters);

            // Assert
            var result = sb.ToString();
            Assert.IsTrue(result.Contains("ValueTask<int>"));
            Assert.IsTrue(result.Contains("GetDataAsync"));
            Assert.IsTrue(result.Contains("async"));
        }

        [TestMethod]
        public void AsyncCodeGenerator_GenerateOptimizedAsyncSignature_WithPlainTaskReturn_ShouldGenerateValueTaskSignature()
        {
            // Arrange
            var sb = new IndentedStringBuilder(string.Empty);
            var returnType = "Task";
            var methodName = "ProcessAsync";
            var parameters = "CancellationToken cancellationToken";

            // Act
            AsyncCodeGenerator.GenerateOptimizedAsyncSignature(sb, returnType, methodName, parameters);

            // Assert
            var result = sb.ToString();
            Assert.IsTrue(result.Contains("ValueTask"));
            Assert.IsTrue(result.Contains("ProcessAsync"));
            Assert.IsTrue(result.Contains("async"));
        }

        [TestMethod]
        public void AsyncCodeGenerator_GenerateOptimizedAsyncSignature_WithNonTaskReturn_ShouldKeepOriginalType()
        {
            // Arrange
            var sb = new IndentedStringBuilder(string.Empty);
            var returnType = "void";
            var methodName = "ProcessData";
            var parameters = "string data";

            // Act
            AsyncCodeGenerator.GenerateOptimizedAsyncSignature(sb, returnType, methodName, parameters);

            // Assert
            var result = sb.ToString();
            Assert.IsTrue(result.Contains("void"));
            Assert.IsTrue(result.Contains("ProcessData"));
        }

        [TestMethod]
        public void AsyncCodeGenerator_GenerateOptimizedAsyncBody_ShouldIncludeCancellationCheck()
        {
            // Arrange
            var sb = new IndentedStringBuilder(string.Empty);
            var operationCode = "return await SomeMethodAsync();";

            // Act
            AsyncCodeGenerator.GenerateOptimizedAsyncBody(sb, operationCode, true);

            // Assert
            var result = sb.ToString();
            Assert.IsTrue(result.Contains("ThrowIfCancelledOptimized"));
            Assert.IsTrue(result.Contains("try"));
            Assert.IsTrue(result.Contains("catch (OperationCanceledException)"));
            Assert.IsTrue(result.Contains("ConfigureAwaitOptimized"));
        }

        [TestMethod]
        public void AsyncCodeGenerator_GenerateHotPathOptimization_ShouldIncludeBothPaths()
        {
            // Arrange
            var sb = new IndentedStringBuilder(string.Empty);
            var condition = "data != null";
            var syncPath = "return ProcessSync(data);";
            var asyncPath = "return await ProcessAsync(data);";

            // Act
            AsyncCodeGenerator.GenerateHotPathOptimization(sb, condition, syncPath, asyncPath);

            // Assert
            var result = sb.ToString();
            Assert.IsTrue(result.Contains("Hot path optimization"));
            Assert.IsTrue(result.Contains(condition));
            Assert.IsTrue(result.Contains("Synchronous fast path"));
            Assert.IsTrue(result.Contains(syncPath));
            Assert.IsTrue(result.Contains("Async path"));
            Assert.IsTrue(result.Contains(asyncPath));
        }

        #endregion

        #region TaskOptimizer Tests

        [TestMethod]
        public async Task TaskOptimizer_ExecuteOptimized_WithThreadPool_ShouldExecuteOnThreadPool()
        {
            // Arrange
            var executed = false;
            var taskFactory = new Func<Task>(() =>
            {
                executed = true;
                return Task.CompletedTask;
            });

            // Act
            var task = TaskOptimizer.ExecuteOptimized(taskFactory, preferThreadPool: true);
            await task;

            // Assert
            Assert.IsTrue(executed);
            Assert.IsTrue(task.IsCompleted);
        }

        [TestMethod]
        public async Task TaskOptimizer_ExecuteOptimized_WithoutThreadPool_ShouldExecuteDirectly()
        {
            // Arrange
            var executed = false;
            var taskFactory = new Func<Task>(() =>
            {
                executed = true;
                return Task.CompletedTask;
            });

            // Act
            var task = TaskOptimizer.ExecuteOptimized(taskFactory, preferThreadPool: false);
            await task;

            // Assert
            Assert.IsTrue(executed);
            Assert.IsTrue(task.IsCompleted);
        }

        [TestMethod]
        public async Task TaskOptimizer_ExecuteOptimizedWithResult_WithThreadPool_ShouldReturnCorrectResult()
        {
            // Arrange
            var expectedResult = 42;
            var taskFactory = new Func<Task<int>>(() => Task.FromResult(expectedResult));

            // Act
            var task = TaskOptimizer.ExecuteOptimized(taskFactory, preferThreadPool: true);
            var result = await task;

            // Assert
            Assert.AreEqual(expectedResult, result);
            Assert.IsTrue(task.IsCompleted);
        }

        [TestMethod]
        public async Task TaskOptimizer_ExecuteOptimizedWithResult_WithoutThreadPool_ShouldReturnCorrectResult()
        {
            // Arrange
            var expectedResult = "test result";
            var taskFactory = new Func<Task<string>>(() => Task.FromResult(expectedResult));

            // Act
            var task = TaskOptimizer.ExecuteOptimized(taskFactory, preferThreadPool: false);
            var result = await task;

            // Assert
            Assert.AreEqual(expectedResult, result);
            Assert.IsTrue(task.IsCompleted);
        }

        [TestMethod]
        public async Task TaskOptimizer_ContinueWithOptimized_ShouldExecuteContinuation()
        {
            // Arrange
            var initialResult = 10;
            var continuationExecuted = false;
            var task = Task.FromResult(initialResult);

            // Act
            var continuationTask = task.ContinueWithOptimized(t =>
            {
                continuationExecuted = true;
                Assert.AreEqual(initialResult, t.Result);
            });

            await continuationTask;

            // Assert
            Assert.IsTrue(continuationExecuted);
            Assert.IsTrue(continuationTask.IsCompleted);
        }

        [TestMethod]
        public async Task TaskOptimizer_ContinueWithOptimized_WithFaultedTask_ShouldNotExecuteContinuation()
        {
            // Arrange
            var continuationExecuted = false;
            var faultedTask = Task.FromException<int>(new InvalidOperationException("Test exception"));

            // Act
            var continuationTask = faultedTask.ContinueWithOptimized(t =>
            {
                continuationExecuted = true;
            });

            // Wait for continuation to complete (it should complete immediately without executing)
            await Task.Delay(100);

            // Assert
            Assert.IsFalse(continuationExecuted);
        }

        #endregion

        #region Integration Tests

        [TestMethod]
        public async Task AsyncOptimizer_IntegrationTest_ShouldWorkTogether()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var result = 0;

            // Act - Create an optimized async operation
            var task = TaskOptimizer.ExecuteOptimized(async () =>
            {
                cts.Token.ThrowIfCancelledOptimized();
                await AsyncOptimizer.GetCompletedTask().ConfigureAwaitOptimized();
                result = 42;
                return AsyncOptimizer.GetCompletedTask(result);
            });

            var finalTask = await task;
            await finalTask.ConfigureAwaitOptimized();

            // Assert
            Assert.AreEqual(42, result);
        }

        [TestMethod]
        public async Task AsyncOptimizer_CancellationIntegrationTest_ShouldHandleCancellation()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var exceptionThrown = false;

            // Act
            try
            {
                await TaskOptimizer.ExecuteOptimized(async () =>
                {
                    cts.Token.ThrowIfCancelledOptimized();
                    await AsyncOptimizer.GetCompletedTask().ConfigureAwaitOptimized();
                });
            }
            catch (OperationCanceledException)
            {
                exceptionThrown = true;
            }

            // Assert
            Assert.IsTrue(exceptionThrown);
        }

        #endregion
    }
}
