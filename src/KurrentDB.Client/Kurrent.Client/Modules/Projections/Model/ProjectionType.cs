namespace Kurrent.Client.Projections;

public enum ProjectionType {
    Unspecified = 0,

    /// <summary>
    /// A projection that is defined by the user.
    /// </summary>
    User,

    /// <summary>
    /// A system projection that is built-in and managed by KurrentDB, such as
    /// the $by_category projection or the $streams projection.
    /// </summary>
    System
}
