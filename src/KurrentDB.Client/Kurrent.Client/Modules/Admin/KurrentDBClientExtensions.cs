// using System.Text.Json;
// using System.Text.Json.Serialization;
// using Kurrent.Client.Model;
//
// namespace KurrentDB.Client;
//
// /// <summary>
// ///  A set of extension methods for an <see cref="KurrentDBClient"/>.
// /// </summary>
// public static partial class KurrentDBClientExtensions {
// 	static readonly JsonSerializerOptions SystemSettingsJsonSerializerOptions = new JsonSerializerOptions {
// 		Converters = {
// 			SystemSettingsJsonConverter.Instance
// 		},
// 	};
//
// 	/// <summary>
// 	/// Writes <see cref="SystemSettings"/> to the $settings stream.
// 	/// </summary>
// 	/// <param name="dbClient"></param>
// 	/// <param name="settings"></param>
// 	/// <param name="deadline"></param>
// 	/// <param name="userCredentials"></param>
// 	/// <param name="cancellationToken"></param>
// 	/// <returns></returns>
// 	/// <exception cref="ArgumentNullException"></exception>
// 	public static Task SetSystemSettingsAsync(
// 		this KurrentDBClient dbClient,
// 		SystemSettings settings,
// 		TimeSpan? deadline = null, UserCredentials? userCredentials = null,
// 		CancellationToken cancellationToken = default) {
// 		// if (dbClient == null) throw new ArgumentNullException(nameof(dbClient));
// 		// return dbClient.AppendToStreamAsync(SystemStreams.SettingsStream, StreamState.Any,
// 		// [
// 		// 	new EventData(Uuid.NewUuid(), SystemEventTypes.Settings,
// 		// 			JsonSerializer.SerializeToUtf8Bytes(settings, SystemSettingsJsonSerializerOptions))
// 		// ], deadline: deadline, userCredentials: userCredentials, cancellationToken: cancellationToken);
//
// 		throw new NotImplementedException("This method is not implemented yet. Please use the KurrentDBClient to set system settings.");
// 	}
// }
//
// /// <summary>
// /// A class representing default access control lists.
// /// </summary>
// public sealed class SystemSettings {
// 	/// <summary>
// 	/// Default access control list for new user streams.
// 	/// </summary>
// 	public StreamAcl? UserStreamAcl { get; }
//
// 	/// <summary>
// 	/// Default access control list for new system streams.
// 	/// </summary>
// 	public StreamAcl? SystemStreamAcl { get; }
//
// 	/// <summary>
// 	/// Constructs a new <see cref="SystemSettings"/>.
// 	/// </summary>
// 	/// <param name="userStreamAcl"></param>
// 	/// <param name="systemStreamAcl"></param>
// 	public SystemSettings(StreamAcl? userStreamAcl = null, StreamAcl? systemStreamAcl = null) {
// 		UserStreamAcl   = userStreamAcl;
// 		SystemStreamAcl = systemStreamAcl;
// 	}
//
// 	bool Equals(SystemSettings other)
// 		=> Equals(UserStreamAcl, other.UserStreamAcl) && Equals(SystemStreamAcl, other.SystemStreamAcl);
//
// 	/// <inheritdoc />
// 	public override bool Equals(object? obj)
// 		=> ReferenceEquals(this, obj) || obj is SystemSettings other && Equals(other);
//
// 	/// <summary>
// 	/// Compares left and right for equality.
// 	/// </summary>
// 	/// <param name="left"></param>
// 	/// <param name="right"></param>
// 	/// <returns>True if left is equal to right.</returns>
// 	public static bool operator ==(SystemSettings? left, SystemSettings? right) => Equals(left, right);
//
// 	/// <summary>
// 	/// Compares left and right for inequality.
// 	/// </summary>
// 	/// <param name="left"></param>
// 	/// <param name="right"></param>
// 	/// <returns>True if left is not equal to right.</returns>
// 	public static bool operator !=(SystemSettings? left, SystemSettings? right) => !Equals(left, right);
//
// 	/// <inheritdoc />
// 	public override int GetHashCode() => HashCode.Hash.Combine(UserStreamAcl?.GetHashCode())
// 		.Combine(SystemStreamAcl?.GetHashCode());
// }
//
// class SystemSettingsJsonConverter : JsonConverter<SystemSettings> {
// 	public static readonly SystemSettingsJsonConverter Instance = new SystemSettingsJsonConverter();
//
// 	public override SystemSettings Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
// 		if (reader.TokenType != JsonTokenType.StartObject)
// 			throw new InvalidOperationException();
//
// 		StreamAcl? system = null, user = null;
//
// 		while (reader.Read()) {
// 			if (reader.TokenType == JsonTokenType.EndObject) {
// 				break;
// 			}
//
// 			if (reader.TokenType != JsonTokenType.PropertyName) {
// 				throw new InvalidOperationException();
// 			}
//
// 			switch (reader.GetString()) {
// 				case SystemStreamAcl:
// 					if (!reader.Read()) {
// 						throw new InvalidOperationException();
// 					}
//
// 					system = StreamAclJsonConverter.Instance.Read(ref reader, typeof(StreamAcl), options);
// 					break;
// 				case UserStreamAcl:
// 					if (!reader.Read()) {
// 						throw new InvalidOperationException();
// 					}
//
// 					user = StreamAclJsonConverter.Instance.Read(ref reader, typeof(StreamAcl), options);
// 					break;
// 			}
// 		}
//
// 		return new SystemSettings(user, system);
// 	}
//
// 	public override void Write(Utf8JsonWriter writer, SystemSettings value, JsonSerializerOptions options) {
// 		writer.WriteStartObject();
// 		if (value.UserStreamAcl != null) {
// 			writer.WritePropertyName(UserStreamAcl);
// 			StreamAclJsonConverter.Instance.Write(writer, value.UserStreamAcl, options);
// 		}
//
// 		if (value.SystemStreamAcl != null) {
// 			writer.WritePropertyName(SystemStreamAcl);
// 			StreamAclJsonConverter.Instance.Write(writer, value.SystemStreamAcl, options);
// 		}
//
// 		writer.WriteEndObject();
// 	}
//
// 		///<summary>
// 	/// The user default acl stream
// 	///</summary>
// 	public const string UserStreamAcl = "$userStreamAcl";
//
// 	///<summary>
// 	/// the system stream defaults acl stream
// 	///</summary>
// 	public const string SystemStreamAcl = "$systemStreamAcl";
// }
