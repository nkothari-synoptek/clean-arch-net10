using System.Diagnostics.Metrics;

namespace Common.Shared.Observability;

/// <summary>
/// Provides Meter instances for custom metrics.
/// Meter should be created once and reused throughout the application.
/// </summary>
public sealed class MetricsProvider
{
    private readonly Meter _meter;

    public MetricsProvider(string serviceName, string? serviceVersion = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        
        _meter = new Meter(
            serviceName,
            serviceVersion ?? "1.0.0");
    }

    /// <summary>
    /// Gets the Meter for creating instruments.
    /// </summary>
    public Meter Meter => _meter;

    /// <summary>
    /// Creates a counter instrument.
    /// </summary>
    /// <typeparam name="T">The numeric type (long, int, double, decimal).</typeparam>
    /// <param name="name">The counter name.</param>
    /// <param name="unit">Optional unit of measurement.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>A counter instrument.</returns>
    public Counter<T> CreateCounter<T>(
        string name,
        string? unit = null,
        string? description = null) where T : struct
    {
        return _meter.CreateCounter<T>(name, unit, description);
    }

    /// <summary>
    /// Creates a histogram instrument for recording distributions.
    /// </summary>
    /// <typeparam name="T">The numeric type (long, int, double, decimal).</typeparam>
    /// <param name="name">The histogram name.</param>
    /// <param name="unit">Optional unit of measurement.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>A histogram instrument.</returns>
    public Histogram<T> CreateHistogram<T>(
        string name,
        string? unit = null,
        string? description = null) where T : struct
    {
        return _meter.CreateHistogram<T>(name, unit, description);
    }

    /// <summary>
    /// Creates an observable gauge for point-in-time measurements.
    /// </summary>
    /// <typeparam name="T">The numeric type (long, int, double, decimal).</typeparam>
    /// <param name="name">The gauge name.</param>
    /// <param name="observeValue">Function to observe the current value.</param>
    /// <param name="unit">Optional unit of measurement.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>An observable gauge instrument.</returns>
    public ObservableGauge<T> CreateObservableGauge<T>(
        string name,
        Func<T> observeValue,
        string? unit = null,
        string? description = null) where T : struct
    {
        return _meter.CreateObservableGauge(name, observeValue, unit, description);
    }

    /// <summary>
    /// Creates an observable counter for cumulative measurements.
    /// </summary>
    /// <typeparam name="T">The numeric type (long, int, double, decimal).</typeparam>
    /// <param name="name">The counter name.</param>
    /// <param name="observeValue">Function to observe the current value.</param>
    /// <param name="unit">Optional unit of measurement.</param>
    /// <param name="description">Optional description.</param>
    /// <returns>An observable counter instrument.</returns>
    public ObservableCounter<T> CreateObservableCounter<T>(
        string name,
        Func<T> observeValue,
        string? unit = null,
        string? description = null) where T : struct
    {
        return _meter.CreateObservableCounter(name, observeValue, unit, description);
    }
}
