using Kurrent.Client.Registry;

namespace Kurrent.Client;

public static class SystemConstants {
    public static class SystemEventSchemaNames {
        public static readonly SchemaName StreamDeleted   = "$streamDeleted";
        public static readonly SchemaName StatsCollection = "$statsCollected";
        public static readonly SchemaName StreamMetadata  = "$metadata";
        public static readonly SchemaName Settings        = "$settings";
    }

    /// <summary>
    /// Roles used by the system.
    /// </summary>
    public static class SystemRoles {
        /// <summary>
        /// The $admins role.
        /// </summary>
        public const string Administrators = "$admins";

        /// <summary>
        /// The $ops role.
        /// </summary>
        public const string Operations = "$ops";

        /// <summary>
        /// The $all role.
        /// </summary>
        public const string All = "$all";
    }
}
