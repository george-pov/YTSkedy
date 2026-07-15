namespace YTSkedy.Scheduling.Application.Platforms.Publications;

/// <summary>
/// Outcome of conditionally changing the current transient publishing attempt
/// to the operator-visible failed state.
/// </summary>
public enum MarkFailedResult
{
    Marked,
    NotFound,
    Changed
}
