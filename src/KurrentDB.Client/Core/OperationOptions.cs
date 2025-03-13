namespace KurrentDB.Client;

/// <summary>
/// A class representing the options to apply to an individual operation.
/// </summary>
public class OperationOptions {

	/// <summary>
	/// Maximum time that the operation will be run
	/// </summary>
	public TimeSpan? Deadline { get; set; }

	/// <summary>
	/// The <see cref="UserCredentials"/> for the operation.
	/// </summary>
	public UserCredentials? UserCredentials { get; set; }

}
