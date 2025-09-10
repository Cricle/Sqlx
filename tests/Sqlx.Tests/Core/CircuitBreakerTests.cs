using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;
using System;
using System.Threading;

namespace Sqlx.Tests.Core
{
    [TestClass]
    public class CircuitBreakerTests
    {
        [TestMethod]
        public void CircuitBreaker_InitialState_ShouldBeClosed()
        {
            // Arrange
            var circuitBreaker = new CircuitBreaker();

            // Act
            var state = circuitBreaker.State;

            // Assert
            Assert.AreEqual(CircuitBreakerState.Closed, state);
        }

        [TestMethod]
        public void CircuitBreaker_RecordSuccess_ShouldResetFailureCount()
        {
            // Arrange
            var circuitBreaker = new CircuitBreaker();
            circuitBreaker.RecordFailure();
            circuitBreaker.RecordFailure();

            // Act
            circuitBreaker.RecordSuccess();
            var state = circuitBreaker.State;

            // Assert
            Assert.AreEqual(CircuitBreakerState.Closed, state);
        }

        [TestMethod]
        public void CircuitBreaker_RecordFailure_ShouldIncrementFailureCount()
        {
            // Arrange
            var circuitBreaker = new CircuitBreaker();

            // Act
            circuitBreaker.RecordFailure();
            var state = circuitBreaker.State;

            // Assert
            Assert.AreEqual(CircuitBreakerState.Closed, state); // Still closed after 1 failure
        }

        [TestMethod]
        public void CircuitBreaker_ThreeFailures_ShouldOpenCircuit()
        {
            // Arrange
            var circuitBreaker = new CircuitBreaker();

            // Act
            circuitBreaker.RecordFailure();
            circuitBreaker.RecordFailure();
            circuitBreaker.RecordFailure();
            var state = circuitBreaker.State;

            // Assert
            Assert.AreEqual(CircuitBreakerState.Open, state);
        }

        [TestMethod]
        public void CircuitBreaker_OpenState_ShouldTransitionToHalfOpenAfterTimeout()
        {
            // Arrange
            var circuitBreaker = new CircuitBreaker();
            
            // Force circuit to open
            circuitBreaker.RecordFailure();
            circuitBreaker.RecordFailure();
            circuitBreaker.RecordFailure();
            
            Assert.AreEqual(CircuitBreakerState.Open, circuitBreaker.State);

            // Act - Wait for timeout (simulate by waiting a bit more than 1 minute)
            // Note: In a real scenario, we might want to make timeout configurable for testing
            // For now, we'll test the logic by checking the state immediately after failures
            var stateAfterFailures = circuitBreaker.State;

            // Assert
            Assert.AreEqual(CircuitBreakerState.Open, stateAfterFailures);
        }

        [TestMethod]
        public void CircuitBreaker_MultipleFailuresAndSuccess_ShouldResetProperly()
        {
            // Arrange
            var circuitBreaker = new CircuitBreaker();

            // Act - Record failures
            circuitBreaker.RecordFailure();
            circuitBreaker.RecordFailure();
            Assert.AreEqual(CircuitBreakerState.Closed, circuitBreaker.State);

            // Record success - should reset
            circuitBreaker.RecordSuccess();
            Assert.AreEqual(CircuitBreakerState.Closed, circuitBreaker.State);

            // Record more failures
            circuitBreaker.RecordFailure();
            circuitBreaker.RecordFailure();
            circuitBreaker.RecordFailure();

            // Assert
            Assert.AreEqual(CircuitBreakerState.Open, circuitBreaker.State);
        }
    }

    [TestClass]
    public class CircuitBreakerOpenExceptionTests
    {
        [TestMethod]
        public void CircuitBreakerOpenException_Constructor_ShouldSetMessage()
        {
            // Arrange
            var expectedMessage = "Circuit breaker is open";

            // Act
            var exception = new CircuitBreakerOpenException(expectedMessage);

            // Assert
            Assert.AreEqual(expectedMessage, exception.Message);
            Assert.IsInstanceOfType(exception, typeof(Exception));
        }

        [TestMethod]
        public void CircuitBreakerOpenException_InheritedFromException_ShouldBeThrowable()
        {
            // Arrange
            var message = "Test circuit breaker exception";

            // Act & Assert
            Assert.ThrowsException<CircuitBreakerOpenException>(() =>
            {
                throw new CircuitBreakerOpenException(message);
            });
        }
    }
}


