namespace KurrentDB.Client.Model;

public enum SchemaDataFormat {
    Unspecified = 0,
    Json        = 1,
    Protobuf    = 2,
    Avro        = 3,
    Bytes       = 4
}