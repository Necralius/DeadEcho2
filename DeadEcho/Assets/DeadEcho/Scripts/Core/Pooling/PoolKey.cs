using System;

namespace Game.Core.Pooling
{
    [Serializable]
    public struct PoolKey : IEquatable<PoolKey>
    {
        public string Id;

        public PoolKey(string id) => Id = id;

        public bool Equals(PoolKey other) => string.Equals(Id, other.Id, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is PoolKey other && Equals(other);
        public override int GetHashCode() => Id != null ? Id.GetHashCode() : 0;
        public override string ToString() => Id ?? "<null>";
    }
}