using OneOf;

namespace Kurrent.Client.Registry;

[GenerateOneOf]
public partial class SchemaIdentifier : OneOfBase<SchemaName, SchemaVersionId>, IEquatable<SchemaIdentifier> {
    public bool IsSchemaName      => Value is SchemaName;
    public bool IsSchemaVersionId => Value is SchemaVersionId;

    public SchemaName      AsSchemaName      => AsT0;
    public SchemaVersionId AsSchemaVersionId => AsT1;

    public bool Equals(SchemaIdentifier? other) {
        if (other is null) return false;

        if (IsSchemaName != other.IsSchemaName) return false;

        return IsSchemaName
            ? AsSchemaName.Value == other.AsSchemaName.Value
            : AsSchemaVersionId.Value == other.AsSchemaVersionId.Value;
    }

    public override bool Equals(object? obj) =>
        obj is SchemaIdentifier other && Equals(other);

    public override int GetHashCode() {
        return IsSchemaName
            ? HashCode.Combine(typeof(SchemaName).GetHashCode(), AsSchemaName.Value.GetHashCode())
            : HashCode.Combine(typeof(SchemaVersionId).GetHashCode(), AsSchemaVersionId.Value.GetHashCode());
    }

    public static bool operator ==(SchemaIdentifier? left, SchemaIdentifier? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(SchemaIdentifier? left, SchemaIdentifier? right) =>
        !(left == right);

    public static readonly SchemaIdentifierEqualityComparer Comparer = new();

    public class SchemaIdentifierEqualityComparer : IEqualityComparer<SchemaIdentifier> {
        public bool Equals(SchemaIdentifier? x, SchemaIdentifier? y) {
            if (ReferenceEquals(x, y)) return true;
            if (x is null || y is null) return false;

            // Both must be of the same type (either both SchemaName or both SchemaVersionId)
            if (x.IsSchemaName != y.IsSchemaName) return false;

            return x.IsSchemaName
                ? x.AsSchemaName.Value == y.AsSchemaName.Value
                : x.AsSchemaVersionId.Value == y.AsSchemaVersionId.Value;
        }

        public int GetHashCode(SchemaIdentifier? obj) {
            if (obj is null) return 0;

            return obj.IsSchemaName
                ? obj.AsSchemaName.Value.GetHashCode()
                : obj.AsSchemaVersionId.Value.GetHashCode();
        }
    }
}
