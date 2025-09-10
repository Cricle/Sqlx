using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sqlx.Core;
using System;
using System.Data;

namespace Sqlx.Tests.Core
{
    [TestClass]
    public class ConnectionHealthTests
    {
        [TestMethod]
        public void ConnectionHealth_DefaultConstructor_ShouldSetDefaultValues()
        {
            // Act
            var health = new ConnectionHealth();

            // Assert
            Assert.IsFalse(health.IsHealthy);
            Assert.AreEqual(ConnectionState.Closed, health.State);
            Assert.AreEqual(string.Empty, health.Database);
            Assert.IsTrue(health.LastChecked <= DateTime.UtcNow);
            Assert.AreEqual(TimeSpan.Zero, health.ResponseTime);
        }

        [TestMethod]
        public void ConnectionHealth_SetProperties_ShouldUpdateValues()
        {
            // Arrange
            var health = new ConnectionHealth();
            var testTime = TimeSpan.FromMilliseconds(123.45);
            var checkTime = DateTime.UtcNow.AddMinutes(-1);

            // Act
            health.IsHealthy = true;
            health.State = ConnectionState.Open;
            health.Database = "TestDatabase";
            health.LastChecked = checkTime;
            health.ResponseTime = testTime;

            // Assert
            Assert.IsTrue(health.IsHealthy);
            Assert.AreEqual(ConnectionState.Open, health.State);
            Assert.AreEqual("TestDatabase", health.Database);
            Assert.AreEqual(checkTime, health.LastChecked);
            Assert.AreEqual(testTime, health.ResponseTime);
        }

        [TestMethod]
        public void ConnectionHealth_ToString_ShouldReturnFormattedString()
        {
            // Arrange
            var health = new ConnectionHealth
            {
                IsHealthy = true,
                State = ConnectionState.Open,
                Database = "MyDatabase",
                ResponseTime = TimeSpan.FromMilliseconds(50.75)
            };

            // Act
            var result = health.ToString();

            // Assert
            Assert.IsTrue(result.Contains("Healthy: True"));
            Assert.IsTrue(result.Contains("State: Open"));
            Assert.IsTrue(result.Contains("Database: MyDatabase"));
            Assert.IsTrue(result.Contains("ResponseTime: 50.8ms"));
        }

        [TestMethod]
        public void ConnectionHealth_ToString_WithUnhealthyConnection_ShouldShowCorrectStatus()
        {
            // Arrange
            var health = new ConnectionHealth
            {
                IsHealthy = false,
                State = ConnectionState.Broken,
                Database = "FailedDB",
                ResponseTime = TimeSpan.FromSeconds(5)
            };

            // Act
            var result = health.ToString();

            // Assert
            Assert.IsTrue(result.Contains("Healthy: False"));
            Assert.IsTrue(result.Contains("State: Broken"));
            Assert.IsTrue(result.Contains("Database: FailedDB"));
            Assert.IsTrue(result.Contains("5000.0ms"));
        }
    }

    [TestClass]
    public class ConnectionMetricsTests
    {
        [TestMethod]
        public void ConnectionMetrics_DefaultConstructor_ShouldSetDefaultValues()
        {
            // Act
            var metrics = new ConnectionMetrics();

            // Assert
            Assert.AreEqual(0, metrics.TotalConnections);
            Assert.AreEqual(0, metrics.ActiveConnections);
            Assert.AreEqual(0, metrics.FailedConnections);
            Assert.AreEqual(TimeSpan.Zero, metrics.AverageConnectionTime);
            Assert.IsTrue(metrics.LastUpdated <= DateTime.UtcNow);
        }

        [TestMethod]
        public void ConnectionMetrics_SetProperties_ShouldUpdateValues()
        {
            // Arrange
            var metrics = new ConnectionMetrics();
            var avgTime = TimeSpan.FromMilliseconds(75.5);
            var updateTime = DateTime.UtcNow.AddMinutes(-5);

            // Act
            metrics.TotalConnections = 100;
            metrics.ActiveConnections = 15;
            metrics.FailedConnections = 3;
            metrics.AverageConnectionTime = avgTime;
            metrics.LastUpdated = updateTime;

            // Assert
            Assert.AreEqual(100, metrics.TotalConnections);
            Assert.AreEqual(15, metrics.ActiveConnections);
            Assert.AreEqual(3, metrics.FailedConnections);
            Assert.AreEqual(avgTime, metrics.AverageConnectionTime);
            Assert.AreEqual(updateTime, metrics.LastUpdated);
        }

        [TestMethod]
        public void ConnectionMetrics_ToString_ShouldReturnFormattedString()
        {
            // Arrange
            var metrics = new ConnectionMetrics
            {
                TotalConnections = 50,
                ActiveConnections = 10,
                FailedConnections = 2,
                AverageConnectionTime = TimeSpan.FromMilliseconds(125.75)
            };

            // Act
            var result = metrics.ToString();

            // Assert
            Assert.IsTrue(result.Contains("Total: 50"));
            Assert.IsTrue(result.Contains("Active: 10"));
            Assert.IsTrue(result.Contains("Failed: 2"));
            Assert.IsTrue(result.Contains("AvgTime: 125.8ms"));
        }

        [TestMethod]
        public void ConnectionMetrics_ToString_WithZeroValues_ShouldShowZeros()
        {
            // Arrange
            var metrics = new ConnectionMetrics();

            // Act
            var result = metrics.ToString();

            // Assert
            Assert.IsTrue(result.Contains("Total: 0"));
            Assert.IsTrue(result.Contains("Active: 0"));
            Assert.IsTrue(result.Contains("Failed: 0"));
            Assert.IsTrue(result.Contains("AvgTime: 0.0ms"));
        }

        [TestMethod]
        public void ConnectionMetrics_ToString_WithHighValues_ShouldFormatCorrectly()
        {
            // Arrange
            var metrics = new ConnectionMetrics
            {
                TotalConnections = 10000,
                ActiveConnections = 500,
                FailedConnections = 25,
                AverageConnectionTime = TimeSpan.FromSeconds(2.5)
            };

            // Act
            var result = metrics.ToString();

            // Assert
            Assert.IsTrue(result.Contains("Total: 10000"));
            Assert.IsTrue(result.Contains("Active: 500"));
            Assert.IsTrue(result.Contains("Failed: 25"));
            Assert.IsTrue(result.Contains("2500.0ms"));
        }
    }
}
