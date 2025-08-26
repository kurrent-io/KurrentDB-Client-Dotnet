using System.Net.Http.Headers;
using System.Text;
using static System.Convert;

namespace KurrentDB.Client;

/// <summary>
/// Represents either a username/password pair or a JWT token used for authentication and
/// authorization to perform operations on the KurrentDB.
/// </summary>
public class UserCredentials {
	// ReSharper disable once InconsistentNaming
	static readonly UTF8Encoding UTF8NoBom = new(false);

	public static readonly UserCredentials Empty = new UserCredentials {
		Username      = null,
		Password      = null,
		Authorization = new(
			Constants.Headers.BasicScheme,
			ToBase64String(UTF8NoBom.GetBytes(":"))
		)
	};

	UserCredentials() { }

	/// <summary>
	/// Constructs a new <see cref="UserCredentials"/>.
	/// </summary>
	public UserCredentials(string username, string password) {
		Username = username;
		Password = password;

		Authorization = new(
			Constants.Headers.BasicScheme,
			ToBase64String(UTF8NoBom.GetBytes($"{username}:{password}"))
		);
	}

	/// <summary>
	/// Constructs a new <see cref="UserCredentials"/>.
	/// </summary>
	public UserCredentials(string bearerToken) =>
		Authorization = new(Constants.Headers.BearerScheme, bearerToken);

	AuthenticationHeaderValue Authorization { get; init; } = null!;

	/// <summary>
	/// The username
	/// </summary>
	public string? Username { get; private init; }

	/// <summary>
	/// The password
	/// </summary>
	public string? Password { get; private init; }

	/// <inheritdoc />
	public override string ToString() =>
		Authorization.ToString();

	/// <summary>
	/// Implicitly convert a <see cref="UserCredentials"/> to a <see cref="string"/>.
	/// </summary>
	public static implicit operator string(UserCredentials self) =>
		self.ToString();
}
