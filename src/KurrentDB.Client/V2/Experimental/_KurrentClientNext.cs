// #pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
//
// // ReSharper disable CheckNamespace
//
// using KurrentDB.Client;
// using Kurrent.Client.Model;
// using Kurrent.Client.SchemaRegistry;
// using Kurrent.Client.SchemaRegistry.Serialization;
// using Kurrent.Client.SchemaRegistry.Serialization.Bytes;
// using Kurrent.Client.SchemaRegistry.Serialization.Json;
// using Kurrent.Client.SchemaRegistry.Serialization.Protobuf;
//
// namespace Kurrent.Client;
//
// [PublicAPI]
// public class KurrentClientNext : IAsyncDisposable {
// 	public KurrentClientNext(KurrentDBClientSettings settings) {
// 		Settings = settings;
// 		Settings.ConnectionName ??= $"conn-{Guid.NewGuid():D}";
//
// 		LegacyProxy = new KurrentDBClient(Settings);
//
// 		var typeMapper     = new MessageTypeMapper();
// 		var schemaExporter = new SchemaExporter();
// 		var registryClient = new KurrentRegistryClient(token => throw new NotImplementedException("Use the Connect method to establish a connection."));
//
// 		var schemaManager = new SchemaManager(registryClient, schemaExporter, typeMapper);
//
// 		SerializerProvider = new SchemaSerializerProvider([
// 			new BytesPassthroughSerializer(), // How to enforce registry policies for this serializer?
// 			new JsonSchemaSerializer(
// 				options: new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap },
// 				schemaManager: schemaManager
// 			),
// 			new ProtobufSchemaSerializer(
// 				options: new() { SchemaRegistration = SchemaRegistrationOptions.AutoMap },
// 				schemaManager: schemaManager
// 			)
// 		]);
//
// 		MetadataDecoder = Settings.MetadataDecoder;
//
// 		DataConverter = new LegacyDataConverter(
// 			SerializerProvider,
// 			Settings.MetadataDecoder,
// 			SchemaRegistryPolicy.NoRequirements
// 		);
// 	}
//
// 	KurrentDBClientSettings Settings { get; }
//
// 	internal KurrentDBClient           LegacyProxy        { get; }
// 	internal ISchemaSerializerProvider SerializerProvider { get; }
// 	internal IMetadataDecoder          MetadataDecoder    { get; }
// 	internal LegacyDataConverter       DataConverter      { get; }
//
// 	public async ValueTask DisposeAsync() =>
// 		await LegacyProxy.DisposeAsync();
// }
