namespace Kurrent.Client.Projections;

public enum ProjectionMode {
    Unspecified = 0,
    Continuous  = 1,
    OneTime     = 2,
    Transient   = 3
}

// public enum ProjectionStatus {
//     Unspecified = 0,
//     Starting    = 1,
//     Running     = 2,
//     Stopping    = 3,
//     Stopped     = 4,
//     Faulted     = 5,
//     Suspended   = 7,
// }

// public enum PhaseState {
//     Unknown,
//     Stopped,
//     Starting,
//     Running,
// }
//
// public enum PhaseSubscriptionState {
//     Unknown = 0,
//     Unsubscribed,
//     Subscribing,
//     Subscribed,
//     Failed
// }
