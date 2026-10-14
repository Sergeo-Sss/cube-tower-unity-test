using System;

namespace CubeTower.Core.Cubes
{
    /// <summary>
    /// Strongly typed identifier of a cube colour.
    /// Gameplay only operates on identifiers; visuals are resolved by the presentation layer,
    /// so the config may come from any data source (ScriptableObject, JSON, remote config).
    /// </summary>
    public readonly struct CubeColorId : IEquatable<CubeColorId>
    {
        public CubeColorId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Cube colour id must not be empty.", nameof(value));

            Value = value.Trim();
        }

        public string Value { get; }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public static bool TryCreate(string value, out CubeColorId id)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                id = default;
                return false;
            }

            id = new CubeColorId(value);
            return true;
        }

        public bool Equals(CubeColorId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is CubeColorId other && Equals(other);

        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);

        public override string ToString() => Value ?? "<none>";

        public static bool operator ==(CubeColorId left, CubeColorId right) => left.Equals(right);

        public static bool operator !=(CubeColorId left, CubeColorId right) => !left.Equals(right);
    }
}
