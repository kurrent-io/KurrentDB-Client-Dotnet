namespace KurrentDB.Client;

#pragma warning disable CS8509
public enum SchemaDataFormat {
	/// <summary>
	/// The data format is not specified.
	/// </summary>
	Unspecified = 0,

	/// <summary>
	/// The data is in JSON format.
	/// </summary>
	Json = 1,

	/// <summary>
	/// The data is in raw bytes format.
	/// </summary>
	Bytes = 4
}
