using System.Diagnostics;

namespace Foundry.Tests.Common.Helpers;

/// <summary>
/// Extensions for measuring query execution time in tests.
/// Use these to verify queries complete within acceptable time limits
/// and to detect N+1 queries or missing indexes.
/// </summary>
public static class QueryPerformanceExtensions
{
    /// <summary>
    /// Executes an async operation and returns both the result and elapsed time.
    /// </summary>
    public static async Task<(T Result, TimeSpan Elapsed)> MeasureAsync<T>(
        Func<Task<T>> operation)
    {
        Stopwatch sw = Stopwatch.StartNew();
        T? result = await operation();
        sw.Stop();
        return (result, sw.Elapsed);
    }

    /// <summary>
    /// Executes an async operation and returns the elapsed time.
    /// </summary>
    public static async Task<TimeSpan> MeasureAsync(Func<Task> operation)
    {
        Stopwatch sw = Stopwatch.StartNew();
        await operation();
        sw.Stop();
        return sw.Elapsed;
    }

    /// <summary>
    /// Executes an operation multiple times and returns timing statistics.
    /// Useful for detecting inconsistent query times that may indicate N+1 issues.
    /// </summary>
    public static async Task<QueryTimingStats> MeasureMultipleAsync(
        Func<Task> operation,
        int iterations = 5,
        int warmupIterations = 1)
    {
        // Warmup iterations (not measured)
        for (int i = 0; i < warmupIterations; i++)
        {
            await operation();
        }

        List<TimeSpan> times = new List<TimeSpan>();
        for (int i = 0; i < iterations; i++)
        {
            TimeSpan elapsed = await MeasureAsync(operation);
            times.Add(elapsed);
        }

        return new QueryTimingStats(times);
    }
}

/// <summary>
/// Statistics from multiple query timing measurements.
/// </summary>
public sealed class QueryTimingStats
{
    public IReadOnlyList<TimeSpan> Times { get; }
    public TimeSpan Min { get; }
    public TimeSpan Max { get; }
    public TimeSpan Average { get; }
    public TimeSpan Median { get; }
    public double StandardDeviationMs { get; }

    /// <summary>
    /// Coefficient of variation (standard deviation / mean).
    /// High values (>0.5) may indicate inconsistent performance (possible N+1).
    /// </summary>
    public double CoefficientOfVariation { get; }

    public QueryTimingStats(IReadOnlyList<TimeSpan> times)
    {
        Times = times;
        Min = times.Min();
        Max = times.Max();
        Average = TimeSpan.FromMilliseconds(times.Average(t => t.TotalMilliseconds));

        List<TimeSpan> sorted = times.OrderBy(t => t).ToList();
        int mid = sorted.Count / 2;
        Median = sorted.Count % 2 == 0
            ? TimeSpan.FromMilliseconds((sorted[mid - 1].TotalMilliseconds + sorted[mid].TotalMilliseconds) / 2)
            : sorted[mid];

        double avgMs = Average.TotalMilliseconds;
        double variance = times.Sum(t => Math.Pow(t.TotalMilliseconds - avgMs, 2)) / times.Count;
        StandardDeviationMs = Math.Sqrt(variance);
        CoefficientOfVariation = avgMs > 0 ? StandardDeviationMs / avgMs : 0;
    }

    public override string ToString() =>
        $"Min: {Min.TotalMilliseconds:F2}ms, Max: {Max.TotalMilliseconds:F2}ms, " +
        $"Avg: {Average.TotalMilliseconds:F2}ms, Median: {Median.TotalMilliseconds:F2}ms, " +
        $"StdDev: {StandardDeviationMs:F2}ms, CV: {CoefficientOfVariation:F2}";
}
