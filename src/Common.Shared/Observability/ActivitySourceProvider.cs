using System.Diagnostics;

namespace Common.Shared.Observability;

/// <summary>
/// Provides ActivitySource instances for distributed tracing.
/// ActivitySource should be created once and reused throughout the application.
/// </summary>
public sealed class ActivitySourceProvider
{
    private readonly ActivitySource _activitySource;

    public ActivitySourceProvider(string serviceName, string? serviceVersion = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceName);
        
        _activitySource = new ActivitySource(
            serviceName,
            serviceVersion ?? "1.0.0");
    }

    /// <summary>
    /// Gets the ActivitySource for creating activities.
    /// </summary>
    public ActivitySource ActivitySource => _activitySource;

    /// <summary>
    /// Starts a new activity with the specified name and kind.
    /// </summary>
    /// <param name="name">The activity name.</param>
    /// <param name="kind">The activity kind.</param>
    /// <returns>The started activity or null if not sampled.</returns>
    public Activity? StartActivity(string name, ActivityKind kind = ActivityKind.Internal)
    {
        return _activitySource.StartActivity(name, kind);
    }

    /// <summary>
    /// Starts a new activity with tags.
    /// </summary>
    /// <param name="name">The activity name.</param>
    /// <param name="kind">The activity kind.</param>
    /// <param name="tags">The tags to add to the activity.</param>
    /// <returns>The started activity or null if not sampled.</returns>
    public Activity? StartActivity(
        string name,
        ActivityKind kind,
        params (string Key, object? Value)[] tags)
    {
        var activity = _activitySource.StartActivity(name, kind);
        
        if (activity != null)
        {
            foreach (var (key, value) in tags)
            {
                activity.SetTag(key, value);
            }
        }

        return activity;
    }

    /// <summary>
    /// Records an exception in the current activity.
    /// </summary>
    /// <param name="exception">The exception to record.</param>
    public static void RecordException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var activity = Activity.Current;
        if (activity != null)
        {
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            
            // Add exception details as tags
            activity.SetTag("exception.type", exception.GetType().FullName);
            activity.SetTag("exception.message", exception.Message);
            activity.SetTag("exception.stacktrace", exception.StackTrace);
        }
    }

    /// <summary>
    /// Sets the status of the current activity.
    /// </summary>
    /// <param name="status">The status code.</param>
    /// <param name="description">Optional status description.</param>
    public static void SetStatus(ActivityStatusCode status, string? description = null)
    {
        var activity = Activity.Current;
        activity?.SetStatus(status, description);
    }
}
