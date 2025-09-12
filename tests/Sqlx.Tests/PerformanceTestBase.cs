// -----------------------------------------------------------------------
// <copyright file="PerformanceTestBase.cs" company="Cricle">
// Copyright (c) Cricle. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Sqlx.Tests;

/// <summary>
/// Base class for performance tests with benchmarking utilities.
/// </summary>
[TestClass]
[TestCategory("Performance")]
public abstract class PerformanceTestBase : TestBase
{
    /// <summary>
    /// Default number of warmup iterations.
    /// </summary>
    protected const int DefaultWarmupIterations = 100;

    /// <summary>
    /// Default number of measurement iterations.
    /// </summary>
    protected const int DefaultMeasurementIterations = 1000;

    /// <summary>
    /// Measures the performance of an action with warmup and multiple iterations.
    /// </summary>
    /// <param name="action">The action to measure.</param>
    /// <param name="warmupIterations">Number of warmup iterations.</param>
    /// <param name="measurementIterations">Number of measurement iterations.</param>
    /// <returns>Performance measurement results.</returns>
    protected PerformanceMeasurement MeasurePerformance(
        Action action,
        int warmupIterations = DefaultWarmupIterations,
        int measurementIterations = DefaultMeasurementIterations)
    {
        // Warmup phase
        for (int i = 0; i < warmupIterations; i++)
        {
            action();
        }

        // Force garbage collection before measurement
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        // Measurement phase
        var measurements = new List<long>(measurementIterations);
        var stopwatch = new Stopwatch();

        for (int i = 0; i < measurementIterations; i++)
        {
            stopwatch.Restart();
            action();
            stopwatch.Stop();
            measurements.Add(stopwatch.ElapsedTicks);
        }

        return new PerformanceMeasurement(measurements);
    }

    /// <summary>
    /// Asserts that a performance measurement meets the specified criteria.
    /// </summary>
    /// <param name="measurement">The performance measurement.</param>
    /// <param name="maxAverageMs">Maximum allowed average time in milliseconds.</param>
    /// <param name="maxP95Ms">Maximum allowed P95 time in milliseconds.</param>
    protected void AssertPerformance(PerformanceMeasurement measurement, double maxAverageMs, double? maxP95Ms = null)
    {
        WriteTestOutput($"Performance Results:");
        WriteTestOutput($"  Average: {measurement.AverageMs:F2}ms");
        WriteTestOutput($"  Median:  {measurement.MedianMs:F2}ms");
        WriteTestOutput($"  P95:     {measurement.P95Ms:F2}ms");
        WriteTestOutput($"  P99:     {measurement.P99Ms:F2}ms");
        WriteTestOutput($"  Min:     {measurement.MinMs:F2}ms");
        WriteTestOutput($"  Max:     {measurement.MaxMs:F2}ms");

        Assert.IsTrue(measurement.AverageMs <= maxAverageMs,
            $"Average time {measurement.AverageMs:F2}ms exceeds maximum {maxAverageMs}ms");

        if (maxP95Ms.HasValue)
        {
            Assert.IsTrue(measurement.P95Ms <= maxP95Ms.Value,
                $"P95 time {measurement.P95Ms:F2}ms exceeds maximum {maxP95Ms.Value}ms");
        }
    }

    /// <summary>
    /// Compares the performance of two actions.
    /// </summary>
    /// <param name="baselineAction">The baseline action.</param>
    /// <param name="optimizedAction">The optimized action.</param>
    /// <param name="expectedImprovementFactor">Expected improvement factor (e.g., 2.0 for 2x faster).</param>
    /// <param name="iterations">Number of iterations for each action.</param>
    protected void ComparePerformance(
        Action baselineAction,
        Action optimizedAction,
        double expectedImprovementFactor,
        int iterations = DefaultMeasurementIterations)
    {
        var baselineMeasurement = MeasurePerformance(baselineAction, iterations / 10, iterations);
        var optimizedMeasurement = MeasurePerformance(optimizedAction, iterations / 10, iterations);

        var actualImprovementFactor = baselineMeasurement.AverageMs / optimizedMeasurement.AverageMs;

        WriteTestOutput($"Performance Comparison:");
        WriteTestOutput($"  Baseline:  {baselineMeasurement.AverageMs:F2}ms");
        WriteTestOutput($"  Optimized: {optimizedMeasurement.AverageMs:F2}ms");
        WriteTestOutput($"  Improvement: {actualImprovementFactor:F2}x (expected: {expectedImprovementFactor:F2}x)");

        Assert.IsTrue(actualImprovementFactor >= expectedImprovementFactor,
            $"Performance improvement {actualImprovementFactor:F2}x is less than expected {expectedImprovementFactor:F2}x");
    }
}

/// <summary>
/// Represents the results of a performance measurement.
/// </summary>
public class PerformanceMeasurement
{
    private readonly List<long> measurements;
    private readonly double ticksToMs = 1000.0 / Stopwatch.Frequency;

    /// <summary>
    /// Initializes a new instance of the <see cref="PerformanceMeasurement"/> class.
    /// </summary>
    /// <param name="measurements">The measurement data in ticks.</param>
    public PerformanceMeasurement(List<long> measurements)
    {
        this.measurements = measurements.OrderBy(x => x).ToList();
    }

    /// <summary>
    /// Gets the average time in milliseconds.
    /// </summary>
    public double AverageMs => measurements.Average() * ticksToMs;

    /// <summary>
    /// Gets the median time in milliseconds.
    /// </summary>
    public double MedianMs => GetPercentile(50) * ticksToMs;

    /// <summary>
    /// Gets the 95th percentile time in milliseconds.
    /// </summary>
    public double P95Ms => GetPercentile(95) * ticksToMs;

    /// <summary>
    /// Gets the 99th percentile time in milliseconds.
    /// </summary>
    public double P99Ms => GetPercentile(99) * ticksToMs;

    /// <summary>
    /// Gets the minimum time in milliseconds.
    /// </summary>
    public double MinMs => measurements.Min() * ticksToMs;

    /// <summary>
    /// Gets the maximum time in milliseconds.
    /// </summary>
    public double MaxMs => measurements.Max() * ticksToMs;

    /// <summary>
    /// Gets the standard deviation in milliseconds.
    /// </summary>
    public double StandardDeviationMs
    {
        get
        {
            var average = measurements.Average();
            var sumOfSquares = measurements.Sum(x => Math.Pow(x - average, 2));
            return Math.Sqrt(sumOfSquares / measurements.Count) * ticksToMs;
        }
    }

    private double GetPercentile(double percentile)
    {
        if (measurements.Count == 0) return 0;

        var index = (percentile / 100.0) * (measurements.Count - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);

        if (lower == upper) return measurements[lower];

        var weight = index - lower;
        return measurements[lower] * (1 - weight) + measurements[upper] * weight;
    }
}