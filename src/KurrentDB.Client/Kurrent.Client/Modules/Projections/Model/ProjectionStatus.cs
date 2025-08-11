namespace Kurrent.Client.Projections;

public enum ProjectionStatus {
    Unspecified = 0,
    Starting    = 1,
    Running     = 2,
    Stopping    = 3,
    Stopped     = 4,
    Faulted     = 5,
    Suspended   = 7,
}
