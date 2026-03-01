
namespace Foundry.Tests.Common.Helpers;

/// <summary>
/// FluentAssertions extensions for query performance validation.
/// </summary>
public static class QueryPerformanceAssertionsExtensions
{
    /// <summary>
    /// Asserts that the average query time is below the specified threshold.
    /// </summary>
    public static QueryTimingStats HaveAverageBelow(
        this QueryTimingStats stats,
        TimeSpan threshold,
        string because = "")
    {
        stats.Average.Should().BeLessThan(threshold,
            $"average query time should be below {threshold}{(string.IsNullOrEmpty(because) ? "" : $" because {because}")}. Stats: {stats}");
        return stats;
    }

    /// <summary>
    /// Asserts that the maximum query time is below the specified threshold.
    /// </summary>
    public static QueryTimingStats HaveMaxBelow(
        this QueryTimingStats stats,
        TimeSpan threshold,
        string because = "")
    {
        stats.Max.Should().BeLessThan(threshold,
            $"maximum query time should be below {threshold}{(string.IsNullOrEmpty(because) ? "" : $" because {because}")}. Stats: {stats}");
        return stats;
    }

    /// <summary>
    /// Asserts that query times are consistent (low coefficient of variation).
    /// High variation may indicate N+1 queries or inconsistent database state.
    /// </summary>
    /// <param name="stats">The timing statistics</param>
    /// <param name="maxCoefficientOfVariation">Maximum acceptable CV (0.5 = 50% variation from mean)</param>
    /// <param name="because">Reason for the assertion</param>
    public static QueryTimingStats HaveConsistentTiming(
        this QueryTimingStats stats,
        double maxCoefficientOfVariation = 0.5,
        string because = "")
    {
        stats.CoefficientOfVariation.Should().BeLessThanOrEqualTo(maxCoefficientOfVariation,
            $"query times should be consistent (CV <= {maxCoefficientOfVariation}){(string.IsNullOrEmpty(because) ? "" : $" because {because}")}. " +
            $"High variation may indicate N+1 queries. Stats: {stats}");
        return stats;
    }

    /// <summary>
    /// Asserts that all query times are below the specified threshold.
    /// </summary>
    public static QueryTimingStats HaveAllTimesBelow(
        this QueryTimingStats stats,
        TimeSpan threshold,
        string because = "")
    {
        List<TimeSpan> violations = stats.Times.Where(t => t >= threshold).ToList();
        violations.Should().BeEmpty(
            $"all query times should be below {threshold}{(string.IsNullOrEmpty(because) ? "" : $" because {because}")}. Stats: {stats}");
        return stats;
    }

    /// <summary>
    /// Asserts that queries scale linearly with data size.
    /// Compares timing ratio against data size ratio to detect O(n²) or worse scaling.
    /// </summary>
    /// <param name="stats">Stats from query with larger dataset</param>
    /// <param name="smallDataStats">Stats from query with smaller dataset</param>
    /// <param name="dataSizeRatio">Ratio of large dataset to small dataset (e.g., 10 if large has 10x more records)</param>
    /// <param name="maxScalingFactor">Maximum acceptable timing ratio relative to linear scaling</param>
    /// <param name="because">Reason for the assertion</param>
    public static QueryTimingStats ScaleLinearlyComparedTo(
        this QueryTimingStats stats,
        QueryTimingStats smallDataStats,
        double dataSizeRatio,
        double maxScalingFactor = 2.0,
        string because = "")
    {
        double timeRatio = stats.Average.TotalMilliseconds / smallDataStats.Average.TotalMilliseconds;
        double expectedRatio = dataSizeRatio;
        double actualScalingFactor = timeRatio / expectedRatio;

        actualScalingFactor.Should().BeLessThanOrEqualTo(maxScalingFactor,
            $"query should scale linearly (timing ratio ~{expectedRatio}x for {dataSizeRatio}x data)" +
            $"{(string.IsNullOrEmpty(because) ? "" : $" because {because}")}. " +
            $"Timing ratio was {timeRatio:F1}x ({actualScalingFactor:F1}x worse than linear). " +
            $"This may indicate N+1 queries or missing indexes. " +
            $"Small data avg: {smallDataStats.Average}, Large data avg: {stats.Average}");

        return stats;
    }
}
