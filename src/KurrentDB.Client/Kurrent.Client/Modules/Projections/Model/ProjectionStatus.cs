namespace Kurrent.Client.Projections;

public enum ProjectionStatus {
    Unspecified    = 0,
    Creating       = 1,
    Loading        = 2,
    Loaded         = 3,
    Preparing      = 4,
    Prepared       = 5,
    Starting       = 6,
    LoadingStopped = 7,
    Running        = 8,
    Stopping       = 9,
    Aborting       = 10,
    Stopped        = 11,
    Completed      = 12,
    Aborted        = 13,
    Faulted        = 14,
    Deleting       = 15
}
